using DotNetOpenAuth.OAuth2;
using DotNetOpenAuth.OAuth2.ChannelElements;
using MyFlightbook.OAuth.Legacy;
using MyFlightbook.OAuth.Server;
using OAuthAuthorizationServer.Code;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Mvc;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
 * TEMPORARY (DNOA migration): decodes a DotNetOpenAuth-issued token two ways - with DNOA itself and with
 * MyFlightbook.OAuth's LegacyDnoaTokenReader - against THIS server's real keys, and reports whether they agree
 * and whether the new server would accept the token.  Read-only; needs no new tables.
 *
 * DELETE this controller and Views/AdminOAuth at cutover (it's the only thing besides the old server that needs DNOA).
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class LegacyTokenCheckRow
    {
        public string Field { get; set; }
        public string Dnoa { get; set; }
        public string New { get; set; }
        public bool Matches { get { return String.Equals(Dnoa, New, StringComparison.Ordinal); } }
    }

    public class LegacyTokenCheckResult
    {
        public string TokenKind { get; set; }
        public string DnoaError { get; set; }
        public bool NewReaderSucceeded { get; set; }
        public List<LegacyTokenCheckRow> Rows { get; } = new List<LegacyTokenCheckRow>();
        public bool AllMatch { get { return DnoaError == null && NewReaderSucceeded && Rows.All(r => r.Matches); } }
        public List<string> Notes { get; } = new List<string>();

        /// <summary>Would the NEW server accept this token (as of now)?</summary>
        public bool? WouldAccept { get; set; }
    }

    public class AdminOAuthController : AdminControllerBase
    {
        private static string Scopes(IEnumerable<string> scopes)
        {
            return scopes == null ? null : String.Join(" ", scopes.OrderBy(s => s, StringComparer.Ordinal));
        }

        private static string Utc(DateTime d)
        {
            return DateTime.SpecifyKind(d, DateTimeKind.Utc).ToString("u", CultureInfo.InvariantCulture);
        }

        private static LegacyTokenCheckResult CheckRefreshToken(string token, string presentingClientId)
        {
            LegacyTokenCheckResult result = new LegacyTokenCheckResult() { TokenKind = "Refresh token" };
            MySqlOAuthServerStore store = new MySqlOAuthServerStore();

            int iBang = token.IndexOf('!');
            string handle = token.Substring(0, iBang);
            bool fKeyFound = store.GetLegacyCryptoKey(LegacyDnoaTokenReader.RefreshTokenKeyBucket, handle) != null;
            result.Notes.Add(String.Format(CultureInfo.InvariantCulture, "Key handle '{0}': {1} in cryptokeys", handle, fKeyFound ? "found" : "NOT FOUND"));

            IAuthorizationDescription dnoa = null;
            try
            {
                dnoa = new AuthorizationServer(new OAuth2AuthorizationServer()).DecodeRefreshToken(token);
            }
            catch (Exception ex) when (ex.IsNonFatal())
            {
                result.DnoaError = ex.GetType().Name + ": " + ex.Message;
            }

            result.NewReaderSucceeded = LegacyDnoaTokenReader.TryReadRefreshToken(token, store.GetLegacyCryptoKey, out LegacyTokenContents mine);

            result.Rows.Add(new LegacyTokenCheckRow() { Field = "Client", Dnoa = dnoa?.ClientIdentifier, New = mine?.ClientIdentifier });
            result.Rows.Add(new LegacyTokenCheckRow() { Field = "User", Dnoa = dnoa?.User, New = mine?.User });
            result.Rows.Add(new LegacyTokenCheckRow() { Field = "Scope", Dnoa = Scopes(dnoa?.Scope), New = Scopes(mine?.Scope) });
            result.Rows.Add(new LegacyTokenCheckRow() { Field = "Issued (UTC)", Dnoa = dnoa == null ? null : Utc(dnoa.UtcIssued), New = mine == null ? null : Utc(mine.UtcIssued) });

            if (mine != null)
            {
                bool fProxy = mine.ClientIdentifier == OAuth2AuthorizationServer.publicClientProxyID;
                string clientToCheck = String.IsNullOrWhiteSpace(presentingClientId) ? (fProxy ? null : mine.ClientIdentifier) : presentingClientId.Trim();

                if (clientToCheck == null)
                {
                    result.Notes.Add("This token was minted via the PKCE proxy, so it doesn't record the real client. Enter the public client's ID to check whether the new server would accept it from that client.");
                }
                else
                {
                    MFBOauth2Client client = MFBOauth2Client.GetClientByID(clientToCheck)?.FirstOrDefault();
                    if (client == null)
                    {
                        result.Notes.Add("Presenting client '" + clientToCheck + "' is not a registered client.");
                        result.WouldAccept = false;
                    }
                    else
                    {
                        // Same rules as OAuthServer.RefreshTokenGrant for legacy tokens
                        bool fClientOK = fProxy ? client.ClientType == ClientType.Public : mine.ClientIdentifier == client.ClientIdentifier;
                        bool fAuthOK = store.HasActiveAuthorization(client.ClientIdentifier, mine.User, mine.UtcIssued, mine.Scope);
                        result.Notes.Add(String.Format(CultureInfo.InvariantCulture, "Presented by '{0}' ({1}): client rule {2}; active authorization created before the token's issue time: {3}",
                            client.ClientIdentifier, client.ClientType, fClientOK ? "OK" : "FAILS", fAuthOK ? "yes" : "NO"));
                        result.WouldAccept = fClientOK && fAuthOK;
                    }
                }
            }
            return result;
        }

        private static LegacyTokenCheckResult CheckAccessToken(string token)
        {
            LegacyTokenCheckResult result = new LegacyTokenCheckResult() { TokenKind = "Access token" };

            // Same keys OAuthServiceCall uses today
            using (RSACryptoServiceProvider rsaSigning = new RSACryptoServiceProvider())
            using (RSACryptoServiceProvider rsaEncryption = new RSACryptoServiceProvider())
            {
                rsaSigning.ImportParameters(OAuth2AuthorizationServer.AuthorizationServerSigningPublicKey);
                rsaEncryption.ImportParameters(OAuth2AuthorizationServer.CreateAuthorizationServerSigningKey());

                AccessToken dnoa = null;
                try
                {
                    dnoa = new StandardAccessTokenAnalyzer(rsaSigning, rsaEncryption).DeserializeAccessToken(null, token);
                }
                catch (Exception ex) when (ex.IsNonFatal())
                {
                    result.DnoaError = ex.GetType().Name + ": " + ex.Message;
                }

                result.NewReaderSucceeded = LegacyDnoaTokenReader.TryReadAccessToken(token, rsaSigning, rsaEncryption, out LegacyTokenContents mine);

                result.Rows.Add(new LegacyTokenCheckRow() { Field = "Client", Dnoa = dnoa?.ClientIdentifier, New = mine?.ClientIdentifier });
                result.Rows.Add(new LegacyTokenCheckRow() { Field = "User", Dnoa = dnoa?.User, New = mine?.User });
                result.Rows.Add(new LegacyTokenCheckRow() { Field = "Scope", Dnoa = Scopes(dnoa?.Scope), New = Scopes(mine?.Scope) });
                result.Rows.Add(new LegacyTokenCheckRow() { Field = "Issued (UTC)", Dnoa = dnoa == null ? null : Utc(dnoa.UtcIssued), New = mine == null ? null : Utc(mine.UtcIssued) });
                result.Rows.Add(new LegacyTokenCheckRow() { Field = "Lifetime", Dnoa = dnoa?.Lifetime?.ToString(), New = mine?.Lifetime?.ToString() });

                if (mine != null)
                {
                    bool fExpired = mine.IsExpired(DateTime.UtcNow);
                    result.Notes.Add(fExpired ? "Token has EXPIRED (the new server rejects it, as DNOA does)." : "Token is unexpired.");
                    result.WouldAccept = !fExpired && !String.IsNullOrEmpty(mine.User);
                }
            }
            return result;
        }

        [Authorize]
        [HttpGet]
        public ActionResult LegacyTokenCheck()
        {
            CheckAuth(ProfileRoles.maskSiteAdminOnly);
            return View("legacyTokenCheck");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LegacyTokenCheck(string token, string presentingClientId)
        {
            CheckAuth(ProfileRoles.maskSiteAdminOnly);
            token = (token ?? string.Empty).Trim();
            ViewBag.presentingClientId = presentingClientId;
            if (token.Length == 0)
                ViewBag.error = "Paste a token.";
            else if (token.StartsWith(OAuthServer.AccessTokenPrefix, StringComparison.Ordinal) || token.StartsWith(OAuthServer.RefreshTokenPrefix, StringComparison.Ordinal))
                ViewBag.error = "That's a new-format token, not a DotNetOpenAuth one.";
            else
            {
                try
                {
                    ViewBag.result = LegacyDnoaTokenReader.LooksLikeLegacyRefreshToken(token) ? CheckRefreshToken(token, presentingClientId) : CheckAccessToken(token);
                }
                catch (Exception ex) when (ex.IsNonFatal())
                {
                    ViewBag.error = ex.Message;
                }
            }
            // Never echo the token back into the page
            return View("legacyTokenCheck");
        }
    }
}
