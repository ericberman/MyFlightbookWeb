using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyFlightbook.OAuth.Legacy;
using MyFlightbook.OAuth.Server;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

/******************************************************
 * 
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 * 
 * Tests for MyFlightbook.OAuth.Server.  Legacy-token tests use genuine DotNetOpenAuth 4.3.4 output (LegacyFixtures.cs),
 * so DNOA itself isn't needed here.
 *
*******************************************************/

namespace MyFlightbook.OAuth.Server.Tests
{
    class MemStore : IOAuthServerStore
    {
        public readonly Dictionary<string, AuthorizationCodeRecord> Codes = new Dictionary<string, AuthorizationCodeRecord>();
        public readonly HashSet<string> UsedCodes = new HashSet<string>();
        public readonly Dictionary<string, TokenRecord> Tokens = new Dictionary<string, TokenRecord>();
        public readonly List<Tuple<string, string, string, DateTime>> Auths = new List<Tuple<string, string, string, DateTime>>();
        public readonly Dictionary<string, byte[]> Keys = new Dictionary<string, byte[]>();
        public Func<DateTime> Clock;

        public void SaveAuthorizationCode(AuthorizationCodeRecord r) { Codes[r.CodeHash] = r; }
        public AuthorizationCodeRecord ConsumeAuthorizationCode(string h, out bool used)
        {
            used = false;
            if (!Codes.TryGetValue(h, out AuthorizationCodeRecord r)) return null;
            used = !UsedCodes.Add(h);
            return r;
        }
        public void SaveToken(TokenRecord t) { Tokens[t.TokenHash] = t; }
        public TokenRecord FindToken(string h) { return Tokens.TryGetValue(h, out TokenRecord t) ? t : null; }
        public bool MarkRotated(string h, DateTime now) { TokenRecord t = FindToken(h); if (t == null || t.RotatedUtc.HasValue) return false; t.RotatedUtc = now; return true; }
        public void RevokeToken(string h) { Tokens.Remove(h); }
        public void RevokeFamily(string f) { foreach (var k in Tokens.Where(kv => kv.Value.FamilyId == f).Select(kv => kv.Key).ToList()) Tokens.Remove(k); }
        public void RecordAuthorization(string c, string u, string s) { Auths.Add(Tuple.Create(c, u, s, Clock())); }
        public bool HasActiveAuthorization(string c, string u, DateTime issued, IEnumerable<string> scopes)
        {
            var rows = Auths.Where(a => a.Item1 == c && a.Item2 == u && a.Item4 < issued.AddSeconds(1)).ToList();
            if (rows.Count == 0) return false;
            var granted = new HashSet<string>(rows.SelectMany(r => OAuthScopes.SplitScopes(r.Item3)));
            return new HashSet<string>(scopes).IsSubsetOf(granted);
        }
        public void Revoke(string c, string u)
        {
            Auths.RemoveAll(a => a.Item1 == c && a.Item2 == u);
            foreach (var k in Tokens.Where(kv => kv.Value.ClientId == c && kv.Value.UserId == u).Select(kv => kv.Key).ToList()) Tokens.Remove(k);
        }
        public byte[] GetLegacyCryptoKey(string b, string h) { return Keys.TryGetValue(b + "|" + h, out byte[] k) ? k : null; }
    }

    class FakeFetcher
    {
        public Dictionary<string, Tuple<HttpStatusCode, string>> Docs = new Dictionary<string, Tuple<HttpStatusCode, string>>();
        public int Fetches;
        public Task<MetadataDocumentResponse> Fetch(Uri uri)
        {
            Fetches++;
            Tuple<HttpStatusCode, string> d;
            if (!Docs.TryGetValue(uri.AbsoluteUri, out d)) d = Tuple.Create(HttpStatusCode.NotFound, "");
            return Task.FromResult(new MetadataDocumentResponse() { StatusCode = d.Item1, Body = d.Item2, MaxAge = TimeSpan.FromMinutes(30) });
        }
    }

    [TestClass]
    public class OAuthServerTests
    {
        static void Check(bool cond, string what) { Assert.IsTrue(cond, what); }

        const string Api = "https://myflightbook.com/logbook/mvc/oAuth/OAuthResource";
        const string Mcp = "https://mcp.myflightbook.com/mcp";
        const string CimdId = "https://claude.ai/oauth/mcp-oauth-client-metadata";
        const string CimdRedirect = "https://claude.ai/api/mcp/auth_callback";

        static DateTime now = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);
        static MemStore store;
        static OAuthServer server;
        static FakeFetcher handler;
        static readonly List<string> legacyLog = new List<string>();

        static readonly Dictionary<string, OAuthClient> registered = new Dictionary<string, OAuthClient>()
        {
            { "partner", new OAuthClient() { ClientId = "partner", ClientName = "Partner", ClientSecret = "s3cr3t+/=", RedirectHREFs = { "https://partner.example.com/cb?x=1" }, AllowedScopes = { "currency", "totals", "addflight", "readflight" }, UseLegacyRedirectComparison = true } },
            { "mobileapp", new OAuthClient() { ClientId = "mobileapp", ClientName = "Mobile", IsPublic = true, RedirectHREFs = { "com.example.app:/oauth", "http://127.0.0.1/cb" }, AllowedScopes = { "currency", "totals", "readflight" }, UseLegacyRedirectComparison = true, AllowPlainPkce = true } },
            { "publicClientProxy", new OAuthClient() { ClientId = "publicClientProxy", ClientSecret = "proxysecret", RedirectHREFs = { "https://myflightbook.com/proxy" }, AllowedScopes = { "currency", "totals", "readflight", "addflight" } } },
            { "mcp-sidecar", new OAuthClient() { ClientId = "mcp-sidecar", ClientSecret = "sidecar-secret", RedirectHREFs = { "https://mcp.myflightbook.com/none" }, AllowedScopes = { } } }
        };

        static void Reset()
        {
            now = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);
            store = new MemStore() { Clock = () => now };
            handler = new FakeFetcher();
            handler.Docs[CimdId] = Tuple.Create(HttpStatusCode.OK, new JObject()
            {
                ["client_id"] = CimdId, ["client_name"] = "Claude", ["redirect_uris"] = new JArray(CimdRedirect),
                ["grant_types"] = new JArray("authorization_code", "refresh_token"), ["token_endpoint_auth_method"] = "none"
            }.ToString());
            var resolver = new MetadataDocumentClientResolver(new MetadataDocumentOptions()
            {
                AllowedScopes = { "currency", "totals", "readflight" },
                ResolveHost = h => h == "evil.example.com" ? new[] { IPAddress.Parse("169.254.169.254") } : new[] { IPAddress.Parse("160.79.104.10") },
                UtcNow = () => now,
                Fetch = handler.Fetch
            });
            var opts = new OAuthServerOptions()
            {
                Issuer = "https://myflightbook.com",
                AuthorizationEndpoint = new Uri("https://myflightbook.com/logbook/mvc/oAuth/Authorize"),
                TokenEndpoint = new Uri("https://myflightbook.com/logbook/mvc/oAuth/OAuthToken"),
                RevocationEndpoint = new Uri("https://myflightbook.com/logbook/mvc/oAuth/Revoke"),
                IntrospectionEndpoint = new Uri("https://myflightbook.com/logbook/mvc/oAuth/Introspect"),
                Resources = { Api, Mcp },
                ScopesSupported = { "currency", "totals", "addflight", "readflight", "addaircraft", "readaircraft", "visited", "images", "namedqueries" },
                IntrospectionClientIds = { "mcp-sidecar" },
                TokenExchangeClients = { { "mcp-sidecar", Api } },
                OnLegacyTokenUsed = (k, c, u) => legacyLog.Add(k + ":" + c + ":" + u),
                SupportsMetadataDocumentClients = true,
                UtcNow = () => now
            };
            server = new OAuthServer(opts, store, new CompositeClientRegistry(id => registered.TryGetValue(id, out OAuthClient c) ? c : null, resolver));
        }

        static NameValueCollection Q(params string[] kv) { var n = new NameValueCollection(); for (int i = 0; i < kv.Length; i += 2) n.Add(kv[i], kv[i + 1]); return n; }
        static string Basic(string id, string secret) => "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(Uri.EscapeDataString(id) + ":" + Uri.EscapeDataString(secret)));
        static JObject J(OAuthHttpResult r) => string.IsNullOrEmpty(r.Body) ? new JObject() : JObject.Parse(r.Body);
        static NameValueCollection QueryOf(string url)
        {
            var n = new NameValueCollection();
            foreach (string pair in new Uri(url).Query.TrimStart('?').Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int i = pair.IndexOf('=');
                n.Add(Uri.UnescapeDataString(pair.Substring(0, i)), Uri.UnescapeDataString(pair.Substring(i + 1)));
            }
            return n;
        }

        static void PkcePair(out string verifier, out string challenge)
        {
            byte[] rgb = new byte[32]; RandomNumberGenerator.Create().GetBytes(rgb);
            verifier = Convert.ToBase64String(rgb).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            using (var sha = SHA256.Create()) challenge = Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(verifier))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        static string GetCode(NameValueCollection authorizeQuery, string user = "pilot_joe")
        {
            var v = server.ValidateAuthorizeRequest(authorizeQuery);
            if (!v.IsValid) { Console.WriteLine("   (authorize failed: " + v.Error + " " + v.ErrorDescription + ")"); return null; }
            string redirect = server.ApproveAuthorizeRequest(v.Request, user);
            var q = QueryOf(redirect);
            Check(q["iss"] == "https://myflightbook.com", "RFC 9207 iss in authorization response");
            Check(q["state"] == authorizeQuery["state"], "state echoed");
            return q["code"];
        }

        [TestInitialize]
        public void Init() { Reset(); legacyLog.Clear(); }

        [TestMethod]
        public void Metadata()
        {
            Reset();
            var m = J(server.AuthorizationServerMetadata());
            Check((string)m["issuer"] == "https://myflightbook.com", "metadata issuer");
            Check(m["code_challenge_methods_supported"].Values<string>().SequenceEqual(new[] { "S256" }), "metadata advertises S256 only");
            Check((bool)m["client_id_metadata_document_supported"] && (bool)m["authorization_response_iss_parameter_supported"], "metadata: CIMD + iss");
            Check(m["registration_endpoint"] == null, "no DCR (deprecated)");
        }

        [TestMethod]
        public void ConfidentialFlow()
        {
            Reset();
            // No PKCE: back-compat for existing partners
            string code = GetCode(Q("response_type", "code", "client_id", "partner", "redirect_uri", "https://partner.example.com/cb?x=1", "scope", "currency totals", "state", "xyz"));
            Check(code != null, "confidential: code issued without PKCE");
            Check(store.Auths.Count == 1, "consent recorded");

            var r = server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "redirect_uri", "https://partner.example.com/cb?x=1"), Basic("partner", "s3cr3t+/="));
            var j = J(r);
            Check(r.StatusCode == 200, "confidential: token via client_secret_basic (" + r.Body + ")");
            Check(((string)j["access_token"]).StartsWith("mfb.at.") && ((string)j["refresh_token"]).StartsWith("mfb.rt."), "new token formats");
            Check((string)j["token_type"] == "bearer" && (int)j["expires_in"] == 14 * 86400 && (string)j["scope"] == "currency totals", "DNOA-compatible response shape");
            Check(r.Headers["Cache-Control"] == "no-store", "no-store");

            string at = (string)j["access_token"], rt = (string)j["refresh_token"];
            var p = server.ValidateAccessToken(at);
            Check(p != null && p.User == "pilot_joe" && p.Scope.SetEquals(new[] { "currency", "totals" }), "API accepts the token");
            Check(server.ValidateAccessToken(at, Mcp) == null, "MCP endpoint rejects an API token (audience)");
            Check(!store.Tokens.Values.Any(t => t.TokenHash == at || t.TokenHash == rt), "only hashes stored");

            // Replay the code -> rejected and the family is revoked
            var replay = server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "redirect_uri", "https://partner.example.com/cb?x=1"), Basic("partner", "s3cr3t+/="));
            Check(replay.StatusCode == 400 && (string)J(replay)["error"] == "invalid_grant", "code replay rejected");
            Check(server.ValidateAccessToken(at) == null, "code replay revoked tokens issued from it");

            // Fresh grant; post-style client auth; refresh does NOT rotate for confidential clients
            code = GetCode(Q("response_type", "code", "client_id", "partner", "redirect_uri", "https://partner.example.com/cb?x=1", "scope", "currency totals"));
            j = J(server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "redirect_uri", "https://partner.example.com/cb?x=1", "client_id", "partner", "client_secret", "s3cr3t+/="), null));
            rt = (string)j["refresh_token"];
            Check(rt != null, "client_secret_post works");
            now = now.AddDays(20);
            var rr = J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", rt), Basic("partner", "s3cr3t+/=")));
            Check((string)rr["refresh_token"] == rt, "confidential refresh returns the same refresh token (no rotation)");
            Check(server.ValidateAccessToken((string)rr["access_token"]) != null, "refreshed access token valid");
            var down = J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", rt, "scope", "currency"), Basic("partner", "s3cr3t+/=")));
            Check((string)down["scope"] == "currency", "down-scoping on refresh");
            var up = server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", rt, "scope", "currency addflight"), Basic("partner", "s3cr3t+/="));
            Check((string)J(up)["error"] == "invalid_scope", "up-scoping rejected");

            // Wrong secret / wrong client
            Check(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", rt), Basic("partner", "nope")).StatusCode == 401, "bad secret -> 401");
            Check((string)J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", rt), Basic("publicClientProxy", "proxysecret")))["error"] == "invalid_grant", "another client's refresh token rejected");

            // User revokes -> refresh and access die immediately
            store.Revoke("partner", "pilot_joe");
            Check((string)J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", rt), Basic("partner", "s3cr3t+/=")))["error"] == "invalid_grant", "revoked: refresh fails");
            Check(server.ValidateAccessToken((string)rr["access_token"]) == null, "revoked: access token fails immediately");
        }

        [TestMethod]
        public void PublicFlowWithRotation()
        {
            Reset();
            PkcePair(out string verifier, out string challenge);
            var noPkce = server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", "mobileapp", "redirect_uri", "com.example.app:/oauth", "scope", "currency"));
            Check(!noPkce.IsValid && noPkce.ErrorRedirectHREF != null && noPkce.ErrorRedirectHREF.StartsWith("com.example.app:/oauth?"), "public client without PKCE -> error redirected to client");

            string code = GetCode(Q("response_type", "code", "client_id", "mobileapp", "redirect_uri", "com.example.app:/oauth", "scope", "currency readflight", "code_challenge", challenge, "code_challenge_method", "S256"));
            var bad = server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "client_id", "mobileapp", "redirect_uri", "com.example.app:/oauth", "code_verifier", verifier.Substring(1) + "A"), null);
            Check((string)J(bad)["error"] == "invalid_grant", "wrong PKCE verifier rejected");

            code = GetCode(Q("response_type", "code", "client_id", "mobileapp", "redirect_uri", "com.example.app:/oauth", "scope", "currency readflight", "code_challenge", challenge, "code_challenge_method", "S256"));
            var withSecret = server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "client_id", "mobileapp", "client_secret", "x", "code_verifier", verifier), null);
            Check(withSecret.StatusCode == 200, "public client sending a (meaningless) secret is tolerated, as the old PKCE proxy did: " + withSecret.Body);

            code = GetCode(Q("response_type", "code", "client_id", "mobileapp", "redirect_uri", "com.example.app:/oauth", "scope", "currency readflight", "code_challenge", challenge, "code_challenge_method", "S256"));
            var j = J(server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "client_id", "mobileapp", "code_verifier", verifier), null));
            string rt1 = (string)j["refresh_token"];
            Check(rt1 != null, "public client: PKCE exchange without redirect_uri (OAuth 2.1)");

            var r2 = J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", rt1, "client_id", "mobileapp"), null));
            string rt2 = (string)r2["refresh_token"];
            Check(rt2 != null && rt2 != rt1, "public client refresh rotates");

            now = now.AddSeconds(30);
            var retry = J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", rt1, "client_id", "mobileapp"), null));
            Check(retry["access_token"] != null, "rotated token still usable within grace (lost response)");

            now = now.AddMinutes(5);
            var theft = J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", rt1, "client_id", "mobileapp"), null));
            Check((string)theft["error"] == "invalid_grant", "rotated token reused after grace -> rejected");
            Check((string)J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", rt2, "client_id", "mobileapp"), null))["error"] == "invalid_grant", "...and the whole family is revoked");

            // plain PKCE: allowed for this pre-registered client only
            string plain = new string('a', 50);
            Check(GetCode(Q("response_type", "code", "client_id", "mobileapp", "redirect_uri", "com.example.app:/oauth", "scope", "currency", "code_challenge", plain, "code_challenge_method", "plain")) != null, "plain PKCE allowed for flagged pre-registered client");
            Check(!server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", "mobileapp", "redirect_uri", "com.example.app:/oauth", "code_challenge", "short", "code_challenge_method", "S256")).IsValid, "malformed challenge rejected");
        }

        [TestMethod]
        public void RedirectRules()
        {
            Reset();
            var v = server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", "partner", "redirect_uri", "https://evil.example.com/cb", "scope", "currency"));
            Check(!v.IsValid && v.ErrorRedirectHREF == null, "unregistered redirect -> error shown, NOT redirected");
            v = server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", "nobody", "redirect_uri", "https://partner.example.com/cb?x=1"));
            Check(!v.IsValid && v.ErrorRedirectHREF == null && v.Error == "invalid_client", "unknown client -> not redirected");
            v = server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", "partner", "redirect_uri", "https://PARTNER.example.com/cb?x=1", "scope", "currency"));
            Check(v.IsValid, "legacy comparison (case-insensitive host) kept for pre-registered clients");
            v = server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", "partner", "scope", "currency"));
            Check(v.IsValid && v.Request.RedirectHREF == "https://partner.example.com/cb?x=1", "missing redirect_uri ok with a single registered callback");

            PkcePair(out string verifier, out string challenge);
            v = server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", "mobileapp", "redirect_uri", "http://127.0.0.1:53111/cb", "scope", "currency", "code_challenge", challenge, "code_challenge_method", "S256"));
            Check(v.IsValid, "loopback redirect: any port (RFC 8252)");
            v = server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", "mobileapp", "redirect_uri", "http://127.0.0.1:53111/other", "scope", "currency", "code_challenge", challenge, "code_challenge_method", "S256"));
            Check(!v.IsValid, "loopback redirect: path must match");

            // redirect_uri mismatch at token endpoint
            string code = GetCode(Q("response_type", "code", "client_id", "partner", "redirect_uri", "https://partner.example.com/cb?x=1", "scope", "currency"));
            var r = server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "redirect_uri", "https://partner.example.com/cb?x=2"), Basic("partner", "s3cr3t+/="));
            Check((string)J(r)["error"] == "invalid_grant", "token endpoint: redirect_uri must match exactly");
            code = GetCode(Q("response_type", "code", "client_id", "partner", "redirect_uri", "https://partner.example.com/cb?x=1", "scope", "currency"));
            r = server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code), Basic("partner", "s3cr3t+/="));
            Check((string)J(r)["error"] == "invalid_grant", "token endpoint: redirect_uri required when no PKCE");
        }

        [TestMethod]
        public void ResourceBinding()
        {
            Reset();
            PkcePair(out string verifier, out string challenge);
            string code = GetCode(Q("response_type", "code", "client_id", CimdId, "redirect_uri", CimdRedirect, "scope", "currency", "code_challenge", challenge, "code_challenge_method", "S256", "resource", Mcp));
            var mismatch = server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "client_id", CimdId, "redirect_uri", CimdRedirect, "code_verifier", verifier, "resource", Api), null);
            Check((string)J(mismatch)["error"] == "invalid_target", "token request resource must match the authorization");

            code = GetCode(Q("response_type", "code", "client_id", CimdId, "redirect_uri", CimdRedirect, "scope", "currency", "code_challenge", challenge, "code_challenge_method", "S256", "resource", Mcp));
            var j = J(server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "client_id", CimdId, "redirect_uri", CimdRedirect, "code_verifier", verifier, "resource", Mcp), null));
            string at = (string)j["access_token"];
            Check((int)j["expires_in"] == 3600, "metadata-document client gets 1h access tokens");
            Check(server.ValidateAccessToken(at, Mcp) != null, "MCP endpoint accepts MCP-bound token");
            Check(server.ValidateAccessToken(at) == null, "API rejects MCP-bound token (no passthrough)");
            Check(server.ValidateAccessToken(at, Mcp + "/") == null, "unknown resource string never matches");

            var rr = J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", (string)j["refresh_token"], "client_id", CimdId, "resource", Mcp), null));
            Check(server.ValidateAccessToken((string)rr["access_token"], Mcp) != null, "refresh keeps the resource binding");

            Check(!server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", CimdId, "redirect_uri", CimdRedirect, "code_challenge", challenge, "code_challenge_method", "S256", "resource", "https://elsewhere.example.com/")).IsValid, "unknown resource rejected at authorize");

            // Explicitly asking for the API resource is the same as asking for none
            code = GetCode(Q("response_type", "code", "client_id", "partner", "redirect_uri", "https://partner.example.com/cb?x=1", "scope", "currency", "resource", Api));
            j = J(server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "redirect_uri", "https://partner.example.com/cb?x=1", "resource", Api), Basic("partner", "s3cr3t+/=")));
            Check(server.ValidateAccessToken((string)j["access_token"]) != null, "explicit API resource works");
        }

        [TestMethod]
        public void TokenExchangeForSidecar()
        {
            Reset();
            PkcePair(out string verifier, out string challenge);
            string code = GetCode(Q("response_type", "code", "client_id", CimdId, "redirect_uri", CimdRedirect, "scope", "currency totals", "code_challenge", challenge, "code_challenge_method", "S256", "resource", Mcp));
            string mcpToken = (string)J(server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "client_id", CimdId, "redirect_uri", CimdRedirect, "code_verifier", verifier, "resource", Mcp), null))["access_token"];

            const string grant = "urn:ietf:params:oauth:grant-type:token-exchange", tt = "urn:ietf:params:oauth:token-type:access_token";
            var ex = server.HandleTokenRequest(Q("grant_type", grant, "subject_token", mcpToken, "subject_token_type", tt, "resource", Api, "scope", "currency"), Basic("mcp-sidecar", "sidecar-secret"));
            var j = J(ex);
            Check(ex.StatusCode == 200 && (string)j["issued_token_type"] == tt, "sidecar exchanges MCP token (" + ex.Body + ")");
            var p = server.ValidateAccessToken((string)j["access_token"]);
            Check(p != null && p.User == "pilot_joe" && p.ClientId == CimdId && p.Scope.SetEquals(new[] { "currency" }), "exchanged token works on API, keeps user/client, down-scoped");
            Check((int)j["expires_in"] <= 300 && j["refresh_token"] == null, "exchanged token is short-lived, no refresh");

            Check(server.HandleTokenRequest(Q("grant_type", grant, "subject_token", mcpToken, "subject_token_type", tt), Basic("partner", "s3cr3t+/=")).StatusCode == 400, "other clients may not exchange");
            Check((string)J(server.HandleTokenRequest(Q("grant_type", grant, "subject_token", (string)j["access_token"], "subject_token_type", tt), Basic("mcp-sidecar", "sidecar-secret")))["error"] == "invalid_grant", "can't exchange an API token");
            Check((string)J(server.HandleTokenRequest(Q("grant_type", grant, "subject_token", mcpToken, "subject_token_type", tt, "scope", "addflight"), Basic("mcp-sidecar", "sidecar-secret")))["error"] == "invalid_scope", "can't widen scope via exchange");
        }

        // ---- Legacy (genuine DNOA tokens; see LegacyFixtures.cs) ----
        static DateTime LegacyIssued(string refreshToken)
        {
            Check(LegacyDnoaTokenReader.TryReadRefreshToken(refreshToken, (b, h) => LegacyFixtures.RefreshKey, out LegacyTokenContents c), "fixture decodes");
            return c.UtcIssued;
        }

        [TestMethod]
        public void LegacyRefreshTokens()
        {
            Reset();
            store.Keys["https://localhost/dnoa/oauth_refresh_token|" + LegacyFixtures.RefreshKeyHandle] = LegacyFixtures.RefreshKey;
            string partnerToken = LegacyFixtures.PartnerRefreshToken, proxyToken = LegacyFixtures.ProxyRefreshTokenSue, orphanProxyToken = LegacyFixtures.ProxyRefreshTokenBob;
            DateTime issued = LegacyIssued(partnerToken);

            // Authorizations from well before the tokens were minted
            now = issued.AddYears(-2);
            store.RecordAuthorization("partner", "pilot_joe", "currency totals");
            store.RecordAuthorization("mobileapp", "pilot_sue", "currency readflight");
            now = issued.AddDays(30);

            var r = server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", partnerToken), Basic("partner", "s3cr3t+/="));
            var j = J(r);
            Check(r.StatusCode == 200 && ((string)j["refresh_token"]).StartsWith("mfb.rt."), "legacy refresh accepted and upgraded to new format (" + r.Body + ")");
            Check(server.ValidateAccessToken((string)j["access_token"])?.User == "pilot_joe", "access token from legacy refresh works");
            Check(legacyLog.Contains("refresh:partner:pilot_joe"), "legacy use logged");
            Check(J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", partnerToken), Basic("partner", "s3cr3t+/=")))["access_token"] != null, "legacy token keeps working for clients that ignore the new one");
            Check(J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", (string)j["refresh_token"]), Basic("partner", "s3cr3t+/=")))["access_token"] != null, "upgraded token works");

            Check((string)J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", partnerToken), Basic("publicClientProxy", "proxysecret")))["error"] == "invalid_grant", "legacy token from another client rejected");

            // Proxy-minted tokens
            Check((string)J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", proxyToken), Basic("partner", "s3cr3t+/=")))["error"] == "invalid_grant", "proxy token rejected from a confidential client");
            var pr = J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", proxyToken, "client_id", "mobileapp"), null));
            Check(pr["access_token"] != null, "proxy token accepted from the public client the user authorized");
            Check((string)J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", orphanProxyToken, "client_id", "mobileapp"), null))["error"] == "invalid_grant", "FIXED: proxy token with no live authorization rejected (old code accepted it)");

            // Tampering and revocation
            Check((string)J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", partnerToken.Substring(0, partnerToken.Length - 4) + "AAAA"), Basic("partner", "s3cr3t+/=")))["error"] == "invalid_grant", "tampered legacy token rejected");
            store.Revoke("partner", "pilot_joe");
            Check((string)J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", partnerToken), Basic("partner", "s3cr3t+/=")))["error"] == "invalid_grant", "legacy token dies when the user revokes the app");

            // A re-authorization AFTER the token was minted doesn't resurrect it (DNOA semantics)
            store.RecordAuthorization("partner", "pilot_joe", "currency totals");
            Check((string)J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", partnerToken), Basic("partner", "s3cr3t+/=")))["error"] == "invalid_grant", "re-authorizing doesn't resurrect pre-revocation legacy tokens");
        }

        [TestMethod]
        public void LegacyAccessTokens()
        {
            Reset();
            using (RSA rsa = RSA.Create())
            {
                rsa.FromXmlString(LegacyFixtures.RsaKeyXml);
                string tok = LegacyFixtures.PartnerAccessToken;
                server.Options.LegacyAccessTokenReader = t => LegacyDnoaTokenReader.TryReadAccessToken(t, rsa, rsa, out LegacyTokenContents c) ? c : null;
                Check(LegacyDnoaTokenReader.TryReadAccessToken(tok, rsa, rsa, out LegacyTokenContents fixture), "access fixture decodes");
                DateTime issued = fixture.UtcIssued;
                now = issued.AddDays(1);
                var p = server.ValidateAccessToken(tok);
                Check(p != null && p.IsLegacy && p.User == "pilot_joe" && p.Scope.Contains("totals"), "legacy access token accepted during cutover window");
                Check(server.ValidateAccessToken(tok, Mcp) == null, "legacy access token never accepted by MCP");
                now = issued.AddDays(15);
                Check(server.ValidateAccessToken(tok) == null, "legacy access token expires on schedule");
                server.Options.LegacyAccessTokenReader = null;
                now = issued.AddDays(1);
                Check(server.ValidateAccessToken(tok) == null, "legacy access tokens off once the reader is removed");
            }
        }

        [TestMethod]
        public void MetadataDocumentClients()
        {
            Reset();
            PkcePair(out string verifier, out string challenge);
            var v = server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", CimdId, "redirect_uri", CimdRedirect, "code_challenge", challenge, "code_challenge_method", "S256", "state", "s"));
            Check(v.IsValid && v.Request.Client.ClientName == "Claude" && v.Request.Client.IsPublic, "CIMD client resolved");
            Check(v.Request.Scope.SetEquals(new[] { "currency", "totals", "readflight" }), "no scope requested -> the configured (read-only) set");
            int fetches = handler.Fetches;
            server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", CimdId, "redirect_uri", CimdRedirect, "code_challenge", challenge, "code_challenge_method", "S256"));
            Check(handler.Fetches == fetches, "metadata document cached");

            v = server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", CimdId, "redirect_uri", CimdRedirect, "scope", "addflight", "code_challenge", challenge, "code_challenge_method", "S256"));
            Check(!v.IsValid && v.Error == "invalid_scope", "CIMD client can't get write scopes");
            v = server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", CimdId, "redirect_uri", CimdRedirect, "code_challenge", new string('a', 50), "code_challenge_method", "plain"));
            Check(!v.IsValid, "CIMD client can't use plain PKCE");
            v = server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", CimdId, "redirect_uri", "https://claude.ai/api/mcp/other", "code_challenge", challenge, "code_challenge_method", "S256"));
            Check(!v.IsValid && v.ErrorRedirectHREF == null, "CIMD redirect must be in the document (exact)");

            Action<string, string, string> expectBad = (id, doc, why) =>
            {
                if (doc != null) handler.Docs[id] = Tuple.Create(HttpStatusCode.OK, doc);
                var r = server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", id, "redirect_uri", CimdRedirect, "code_challenge", challenge, "code_challenge_method", "S256"));
                Check(!r.IsValid && r.Error == "invalid_client" && r.ErrorRedirectHREF == null, "CIMD rejected: " + why + " (" + r.ErrorDescription + ")");
            };
            expectBad("https://x.example.com/a", new JObject() { ["client_id"] = "https://x.example.com/b", ["client_name"] = "X", ["redirect_uris"] = new JArray(CimdRedirect) }.ToString(), "client_id mismatch");
            expectBad("https://x.example.com/c", new JObject() { ["client_id"] = "https://x.example.com/c", ["redirect_uris"] = new JArray(CimdRedirect) }.ToString(), "no client_name");
            expectBad("https://x.example.com/d", new JObject() { ["client_id"] = "https://x.example.com/d", ["client_name"] = "X", ["redirect_uris"] = new JArray("http://attacker.example.com/cb") }.ToString(), "non-https redirect");
            expectBad("https://x.example.com/e", new JObject() { ["client_id"] = "https://x.example.com/e", ["client_name"] = "X", ["redirect_uris"] = new JArray(CimdRedirect), ["token_endpoint_auth_method"] = "client_secret_basic" }.ToString(), "secret-based auth");
            expectBad("https://x.example.com/f", "not json", "bad JSON");
            expectBad("https://x.example.com/missing", null, "404");
            expectBad("https://evil.example.com/meta", null, "resolves to cloud metadata IP (SSRF)");
            expectBad("https://x.example.com/", null, "no path");
            expectBad("https://x.example.com:8443/m", null, "non-default port");
            expectBad("https://10.0.0.5/m", null, "IP literal");
            expectBad("https://x.example.com/m?q=1", null, "query string");

            Check(MetadataDocumentClientResolver.IsNonPublicAddress(IPAddress.Parse("::1")) && MetadataDocumentClientResolver.IsNonPublicAddress(IPAddress.Parse("fd00::1")) && MetadataDocumentClientResolver.IsNonPublicAddress(IPAddress.Parse("::ffff:192.168.1.1")) && !MetadataDocumentClientResolver.IsNonPublicAddress(IPAddress.Parse("8.8.8.8")), "address classification");

            // Full CIMD flow incl. rotation
            string code = GetCode(Q("response_type", "code", "client_id", CimdId, "redirect_uri", CimdRedirect, "scope", "currency", "code_challenge", challenge, "code_challenge_method", "S256", "resource", Mcp, "state", "abc"));
            var j = J(server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "client_id", CimdId, "redirect_uri", CimdRedirect, "code_verifier", verifier, "resource", Mcp), null));
            var rr = J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", (string)j["refresh_token"], "client_id", CimdId, "resource", Mcp), null));
            Check(rr["refresh_token"] != null && (string)rr["refresh_token"] != (string)j["refresh_token"], "CIMD client refresh rotates");
        }

        [TestMethod]
        public void IntrospectionAndRevocation()
        {
            Reset();
            string code = GetCode(Q("response_type", "code", "client_id", "partner", "redirect_uri", "https://partner.example.com/cb?x=1", "scope", "currency"));
            var j = J(server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "redirect_uri", "https://partner.example.com/cb?x=1"), Basic("partner", "s3cr3t+/=")));
            string at = (string)j["access_token"], rt = (string)j["refresh_token"];

            var i = J(server.HandleIntrospectionRequest(Q("token", at), Basic("mcp-sidecar", "sidecar-secret")));
            Check((bool)i["active"] && (string)i["username"] == "pilot_joe" && (string)i["aud"] == Api, "introspection: active");
            Check(!(bool)J(server.HandleIntrospectionRequest(Q("token", "mfb.at.nope"), Basic("mcp-sidecar", "sidecar-secret")))["active"], "introspection: unknown -> inactive");
            Check(server.HandleIntrospectionRequest(Q("token", at), Basic("partner", "s3cr3t+/=")).StatusCode == 401, "introspection: unauthorized client");

            Check(server.HandleRevocationRequest(Q("token", rt), Basic("publicClientProxy", "proxysecret")).StatusCode == 200 && server.ValidateAccessToken(at) != null, "revocation by another client: 200 but no effect");
            Check(server.HandleRevocationRequest(Q("token", rt, "token_type_hint", "refresh_token"), Basic("partner", "s3cr3t+/=")).StatusCode == 200, "revocation: 200");
            Check(server.ValidateAccessToken(at) == null, "revoking refresh token revokes its access tokens");
            Check((string)J(server.HandleTokenRequest(Q("grant_type", "refresh_token", "refresh_token", rt), Basic("partner", "s3cr3t+/=")))["error"] == "invalid_grant", "revoked refresh token unusable");
        }

        [TestMethod]
        public void MiscProtocol()
        {
            Reset();
            Check((string)J(server.HandleTokenRequest(Q("grant_type", "password", "username", "a", "password", "b"), Basic("partner", "s3cr3t+/=")))["error"] == "unsupported_grant_type", "password grant unsupported");
            Check((string)J(server.HandleTokenRequest(Q("grant_type", "client_credentials"), Basic("partner", "s3cr3t+/=")))["error"] == "unsupported_grant_type", "client_credentials dropped");
            var dup = new NameValueCollection { { "grant_type", "refresh_token" }, { "refresh_token", "a" }, { "refresh_token", "b" } };
            Check((string)J(server.HandleTokenRequest(dup, Basic("partner", "s3cr3t+/=")))["error"] == "invalid_request", "duplicate params rejected");
            var v = server.ValidateAuthorizeRequest(Q("response_type", "token", "client_id", "partner", "redirect_uri", "https://partner.example.com/cb?x=1"));
            Check(!v.IsValid && v.Error == "unsupported_response_type" && v.ErrorRedirectHREF.Contains("error=unsupported_response_type") && v.ErrorRedirectHREF.Contains("iss="), "implicit flow refused (redirected, with iss)");
            v = server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", "partner", "redirect_uri", "https://partner.example.com/cb?x=1", "scope", "currency bogus"));
            Check(!v.IsValid && v.Error == "invalid_scope", "unknown scope rejected");

            // PKCE downgrade: verifier presented for a code issued without a challenge
            string code = GetCode(Q("response_type", "code", "client_id", "partner", "redirect_uri", "https://partner.example.com/cb?x=1", "scope", "currency"));
            PkcePair(out string verifier, out string _);
            Check((string)J(server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "redirect_uri", "https://partner.example.com/cb?x=1", "code_verifier", verifier), Basic("partner", "s3cr3t+/=")))["error"] == "invalid_grant", "PKCE downgrade rejected");

            // Expired code
            code = GetCode(Q("response_type", "code", "client_id", "partner", "redirect_uri", "https://partner.example.com/cb?x=1", "scope", "currency"));
            now = now.AddMinutes(6);
            Check((string)J(server.HandleTokenRequest(Q("grant_type", "authorization_code", "code", code, "redirect_uri", "https://partner.example.com/cb?x=1"), Basic("partner", "s3cr3t+/=")))["error"] == "invalid_grant", "expired code rejected");

            // Deny
            v = server.ValidateAuthorizeRequest(Q("response_type", "code", "client_id", "partner", "redirect_uri", "https://partner.example.com/cb?x=1", "scope", "currency", "state", "q"));
            var dq = QueryOf(server.DenyAuthorizeRequest(v.Request));
            Check(dq["error"] == "access_denied" && dq["state"] == "q" && dq["iss"] != null, "deny redirects with access_denied");

            Check(OAuthServer.BearerChallenge("invalid_token", null, new Uri("https://mcp.myflightbook.com/.well-known/oauth-protected-resource/mcp")) == "Bearer resource_metadata=\"https://mcp.myflightbook.com/.well-known/oauth-protected-resource/mcp\", error=\"invalid_token\"", "RFC 9728 challenge");
        }
    }
}
