using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 * 
 * Tests for the OAuth CLIENT pieces (MyFlightbook calling other services): AuthorizationState JSON compatibility with
 * DotNetOpenAuth, and OAuthClientProtocol's DNOA-equivalent semantics.
 *
*******************************************************/

namespace MyFlightbook.OAuth.Server.Tests
{
    [TestClass]
    public class OAuthClientProtocolTests
    {
        static void Check(bool cond, string what) { Assert.IsTrue(cond, what); }

        [TestMethod]
        public void AuthorizationStateReadsDnoaJson()
        {
            AuthorizationState s = JsonConvert.DeserializeObject<AuthorizationState>(LegacyFixtures.DnoaAuthorizationStateJson);
            Check(s.AccessToken == "at-123" && s.RefreshToken == "rt-456", "tokens");
            Check(s.Callback == new Uri("https://myflightbook.com/logbook/mvc/oAuth/OneDriveRedir?x=1"), "callback");
            Check(s.AccessTokenIssueDateUtc == new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc) && s.AccessTokenExpirationUtc == new DateTime(2026, 1, 2, 4, 4, 5, DateTimeKind.Utc), "dates");
            Check(s.Scope.SetEquals(new[] { "files.readwrite", "offline_access" }), "scopes");

            // Through the Profile prefs path: object -> dynamic -> reserialize -> T
            object asDynamic = JsonConvert.DeserializeObject(LegacyFixtures.DnoaAuthorizationStateJson);
            AuthorizationState viaPrefs = JsonConvert.DeserializeObject<AuthorizationState>(JsonConvert.SerializeObject(asDynamic));
            Check(viaPrefs.RefreshToken == "rt-456" && viaPrefs.Scope.Count == 2, "prefs round-trip");
        }

        [TestMethod]
        public void AuthorizationStateWritesDnoaShape()
        {
            // Rollback safety: new JSON has exactly DNOA's property names and values
            AuthorizationState s = JsonConvert.DeserializeObject<AuthorizationState>(LegacyFixtures.DnoaAuthorizationStateJson);
            JObject mine = JObject.Parse(JsonConvert.SerializeObject(s));
            JObject dnoa = JObject.Parse(LegacyFixtures.DnoaAuthorizationStateJson);
            Check(new HashSet<string>(mine.Properties().Select(p => p.Name)).SetEquals(dnoa.Properties().Select(p => p.Name)), "same property names: " + String.Join(",", mine.Properties().Select(p => p.Name)));
            foreach (JProperty p in dnoa.Properties())
                Check(JToken.DeepEquals(p.Value, mine[p.Name]), "same value for " + p.Name + ": " + mine[p.Name]);
        }

        [TestMethod]
        public void TokenResponseSemanticsMatchDnoa()
        {
            DateTime now = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);
            AuthorizationState s = new AuthorizationState(new[] { "files.readwrite" }) { AccessToken = "old", RefreshToken = "keepme" };

            OAuthClientProtocol.ApplyTokenResponse(s, "{\"access_token\":\"new\",\"expires_in\":3600,\"scope\":\"something.else\"}", now);
            Check(s.AccessToken == "new" && s.AccessTokenIssueDateUtc == now && s.AccessTokenExpirationUtc == now.AddHours(1), "access token + dates");
            Check(s.RefreshToken == "keepme", "refresh token kept when the server doesn't send one");
            Check(s.Scope.SetEquals(new[] { "files.readwrite" }), "existing scope not overwritten by the access token's scope");

            OAuthClientProtocol.ApplyTokenResponse(s, "{\"access_token\":\"newer\",\"expires_in\":\"7200\",\"refresh_token\":\"rotated\"}", now);
            Check(s.RefreshToken == "rotated" && s.AccessTokenExpirationUtc == now.AddHours(2), "rotated refresh token; quoted expires_in");

            OAuthClientProtocol.ApplyTokenResponse(s, "{\"access_token\":\"x\"}", now);
            Check(s.AccessTokenExpirationUtc == null, "no expires_in -> no expiration (DNOA)");

            AuthorizationState fresh = new AuthorizationState();
            OAuthClientProtocol.ApplyTokenResponse(fresh, "{\"access_token\":\"a\",\"scope\":\"one two\"}", now);
            Check(fresh.Scope.SetEquals(new[] { "one", "two" }) && fresh.RefreshToken == null, "empty scope adopts response scope; no refresh token stays null");

            bool threw = false;
            try { OAuthClientProtocol.ApplyTokenResponse(fresh, "{\"error\":\"invalid_grant\"}", now); } catch (OAuthProtocolException ex) { threw = ex.Error == "invalid_grant"; }
            Check(threw, "missing access_token throws OAuthProtocolException with the error");
            threw = false;
            try { OAuthClientProtocol.ApplyTokenResponse(fresh, "<html>oops</html>", now); } catch (OAuthProtocolException) { threw = true; }
            Check(threw, "non-JSON throws OAuthProtocolException");
        }

        [TestMethod]
        public void TokenErrors()
        {
            OAuthProtocolException ex = OAuthClientProtocol.TokenError(400, "{\"error\":\"invalid_grant\",\"error_description\":\"Token has been expired or revoked.\"}", new Uri("https://oauth2.googleapis.com/token"));
            Check(ex.Error == "invalid_grant" && ex.Message.Contains("oauth2.googleapis.com") && ex.Message.Contains("revoked"), ex.Message);
            Check(ex.ResponseBody.Contains("invalid_grant"), "raw body kept for provider-specific handlers (ThrowGDriveError, HandleInvalidAuth)");
            ex = OAuthClientProtocol.TokenError(502, "Bad gateway");
            Check(ex.Error == null && ex.Message.Contains("502") && ex.Message.Contains("Bad gateway"), ex.Message);
        }

        [TestMethod]
        public void StateCookie()
        {
            Uri cb = new Uri("https://myflightbook.com/logbook/mvc/oAuth/GDriveRedir?gdriveauth=1");
            string state = OAuthClientProtocol.NewState();
            Check(state.Length >= 20 && state != OAuthClientProtocol.NewState(), "random state");
            string v = OAuthClientProtocol.StateCookieValue(state, cb);
            Check(OAuthClientProtocol.TryParseStateCookie(v, out string s2, out Uri cb2) && s2 == state && cb2 == cb, "round trip");
            Check(!OAuthClientProtocol.TryParseStateCookie(null, out _, out _) && !OAuthClientProtocol.TryParseStateCookie("abc", out _, out _) && !OAuthClientProtocol.TryParseStateCookie("abc|!!!", out _, out _), "garbage rejected");
            Check(OAuthClientProtocol.StateMatches(state, s2) && !OAuthClientProtocol.StateMatches(state + "x", s2) && !OAuthClientProtocol.StateMatches(null, s2) && !OAuthClientProtocol.StateMatches("", ""), "matching");
        }

        [TestMethod]
        public void AuthorizationRequest()
        {
            Uri u = OAuthClientProtocol.AuthorizationRequestUri(new Uri("https://accounts.google.com/o/oauth2/v2/auth?access_type=offline&prompt=consent"), "my id",
                new Uri("https://myflightbook.com/logbook/mvc/oAuth/GDriveRedir?gdriveauth=1"), new[] { "a", "b" }, "st");
            string q = u.Query;
            Check(q.StartsWith("?access_type=offline&prompt=consent&response_type=code&", StringComparison.Ordinal), "existing query kept: " + q);
            Check(q.Contains("client_id=my%20id") && q.Contains("scope=a%20b") && q.Contains("state=st") && q.Contains("redirect_uri=https%3A%2F%2Fmyflightbook.com%2Flogbook%2Fmvc%2FoAuth%2FGDriveRedir%3Fgdriveauth%3D1"), q);
            u = OAuthClientProtocol.AuthorizationRequestUri(new Uri("https://www.dropbox.com/oauth2/authorize"), "id", new Uri("https://x.example.com/cb"), null, "st", new Dictionary<string, string>() { { "token_access_type", "offline" } });
            Check(!u.Query.Contains("scope=") && u.Query.StartsWith("?response_type=code&", StringComparison.Ordinal) && u.Query.EndsWith("&token_access_type=offline", StringComparison.Ordinal), u.Query);
        }

        [TestMethod]
        public void BasicHeaderIsDnoaStyle()
        {
            string h = OAuthClientProtocol.BasicAuthorizationHeader("client", "s3cr+t/=");
            Check(h == "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("client:s3cr+t/=")), "raw id:secret, not URL-encoded (as DNOA sent it)");
        }
    }
}
