using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
 * The protocol half of MyFlightbook's OAuth 2.0 CLIENT (where MyFlightbook calls Google, Dropbox, CloudAhoy, etc.),
 * replacing DotNetOpenAuth's WebServerClient.  Framework-neutral; OAuthClientBase (in the web project) supplies the
 * System.Web glue (cookie, redirect, reading the callback).
 *
 * Behavior deliberately mirrors DNOA 4.3 so that existing integrations see the same requests:
 *   - Authorization request: response_type=code, client_id, redirect_uri, scope (space-delimited, omitted if empty), state.
 *   - Code exchange: client_id/client_secret in the POST body (OAuthClientBase.ConvertToken used DNOA's PostParameter applicator).
 *   - Refresh: preemptive HTTP Basic "id:secret" (raw, not URL-encoded), credentials NOT in the body (DNOA's default NetworkCredential applicator).
 *   - Response handling (DNOA ClientBase.UpdateAuthorizationWithResponse): AccessToken and issue/expiration dates replaced;
 *     RefreshToken replaced ONLY if the server sent a new one; Scope set from the response only if the state had none.
 *   - Any failure (HTTP error, unreachable server, malformed response) throws OAuthProtocolException, as DNOA threw ProtocolException.
 *
*******************************************************/

namespace MyFlightbook.OAuth
{
    public static class OAuthClientProtocol
    {
        /// <summary>
        /// Cookie holding the anti-XSRF state (and the exact redirect URI used) between the authorization redirect and the callback.
        /// </summary>
        public const string StateCookieName = "mfb-oauth-client-state";

        private const char cookieSeparator = '|';

        #region State
        public static string NewState()
        {
            byte[] rgb = new byte[16];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                rng.GetBytes(rgb);
            return rgb.ToBase64URL();
        }

        /// <summary>
        /// The cookie stores the callback too, so the code exchange uses EXACTLY the redirect_uri that the authorization request used.
        /// </summary>
        public static string StateCookieValue(string state, Uri callback)
        {
            if (String.IsNullOrEmpty(state))
                throw new ArgumentNullException(nameof(state));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            return state + cookieSeparator + Encoding.UTF8.GetBytes(callback.AbsoluteUri).ToBase64URL();
        }

        public static bool TryParseStateCookie(string cookieValue, out string state, out Uri callback)
        {
            state = null;
            callback = null;
            if (String.IsNullOrEmpty(cookieValue))
                return false;
            int i = cookieValue.IndexOf(cookieSeparator);
            if (i <= 0 || i == cookieValue.Length - 1)
                return false;
            try
            {
                string href = Encoding.UTF8.GetString(cookieValue.Substring(i + 1).FromBase64URL());
                if (!Uri.TryCreate(href, UriKind.Absolute, out callback))
                    return false;
            }
            catch (FormatException)
            {
                return false;
            }
            state = cookieValue.Substring(0, i);
            return true;
        }

        /// <summary>
        /// Constant-time comparison of the returned state with the expected one
        /// </summary>
        public static bool StateMatches(string returned, string expected)
        {
            if (String.IsNullOrEmpty(returned) || String.IsNullOrEmpty(expected))
                return false;
            byte[] a = Encoding.UTF8.GetBytes(returned), b = Encoding.UTF8.GetBytes(expected);
            int diff = a.Length ^ b.Length;
            for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
        #endregion

        #region Requests
        /// <summary>
        /// Builds the URL to send the user to for authorization.
        /// </summary>
        /// <param name="authorizationEndpoint">The provider's authorization endpoint; may already carry a query string</param>
        /// <param name="clientId">Our client ID with the provider</param>
        /// <param name="callback">The redirect URI</param>
        /// <param name="scopes">Scopes; omitted if null/empty</param>
        /// <param name="state">The anti-XSRF state</param>
        /// <param name="additionalParameters">Optional extra parameters (e.g., code_challenge)</param>
        public static Uri AuthorizationRequestUri(Uri authorizationEndpoint, string clientId, Uri callback, IEnumerable<string> scopes, string state, IDictionary<string, string> additionalParameters = null)
        {
            if (authorizationEndpoint == null)
                throw new ArgumentNullException(nameof(authorizationEndpoint));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            List<KeyValuePair<string, string>> p = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("response_type", "code"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("redirect_uri", callback.AbsoluteUri)
            };
            string scope = OAuthScopes.JoinScopes(scopes);
            if (!String.IsNullOrEmpty(scope))
                p.Add(new KeyValuePair<string, string>("scope", scope));
            if (!String.IsNullOrEmpty(state))
                p.Add(new KeyValuePair<string, string>("state", state));
            if (additionalParameters != null)
                p.AddRange(additionalParameters);

            string baseHref = authorizationEndpoint.AbsoluteUri;
            StringBuilder sb = new StringBuilder(baseHref);
            bool fHasQuery = baseHref.IndexOf('?') >= 0;
            foreach (KeyValuePair<string, string> kv in p)
            {
                if (kv.Value == null)
                    continue;
                sb.Append(fHasQuery ? '&' : '?');
                fHasQuery = true;
                sb.Append(Uri.EscapeDataString(kv.Key)).Append('=').Append(Uri.EscapeDataString(kv.Value));
            }
            return new Uri(sb.ToString());
        }

        /// <summary>
        /// HTTP Basic client authentication exactly as DNOA sent it: base64(UTF8("id:secret")), not URL-encoded.
        /// </summary>
        public static string BasicAuthorizationHeader(string clientId, string clientSecret)
        {
            return "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes((clientId ?? string.Empty) + ":" + (clientSecret ?? string.Empty)));
        }

        /// <summary>
        /// Exchanges an authorization code for tokens.  Client credentials go in the POST body.
        /// </summary>
        public static async Task<AuthorizationState> ExchangeCodeAsync(Uri tokenEndpoint, string code, Uri callback, string clientId, string clientSecret)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            Dictionary<string, string> form = new Dictionary<string, string>()
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", callback.AbsoluteUri },
                { "client_id", clientId }
            };
            if (!String.IsNullOrEmpty(clientSecret))
                form["client_secret"] = clientSecret;

            string body = await PostTokenRequestAsync(tokenEndpoint, form, null).ConfigureAwait(false);
            AuthorizationState result = new AuthorizationState() { Callback = callback };
            ApplyTokenResponse(result, body, DateTime.UtcNow);
            return result;
        }

        /// <summary>
        /// Refreshes the access token IN PLACE, as DNOA's RefreshAuthorization did.  HTTP Basic client authentication (or client_id alone if there's no secret).
        /// </summary>
        public static async Task RefreshAsync(IAuthorizationState state, Uri tokenEndpoint, string clientId, string clientSecret)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (String.IsNullOrEmpty(state.RefreshToken))
                throw new ArgumentException("No refresh token", nameof(state));

            Dictionary<string, string> form = new Dictionary<string, string>()
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", state.RefreshToken }
            };
            string basic = null;
            if (String.IsNullOrEmpty(clientSecret))
                form["client_id"] = clientId;
            else
                basic = BasicAuthorizationHeader(clientId, clientSecret);

            string body = await PostTokenRequestAsync(tokenEndpoint, form, basic).ConfigureAwait(false);
            ApplyTokenResponse(state, body, DateTime.UtcNow);
        }

        /// <summary>
        /// POSTs to a token endpoint and returns the (successful) response body.  Throws OAuthProtocolException on any failure.
        /// </summary>
        public static async Task<string> PostTokenRequestAsync(Uri tokenEndpoint, IDictionary<string, string> form, string authorizationHeader)
        {
            if (tokenEndpoint == null)
                throw new ArgumentNullException(nameof(tokenEndpoint));
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            Dictionary<string, string> headers = String.IsNullOrEmpty(authorizationHeader) ? null : new Dictionary<string, string>() { { "Authorization", authorizationHeader } };
            try
            {
                using (FormUrlEncodedContent content = new FormUrlEncodedContent(form))
                {
                    content.Headers.ContentType.CharSet = "utf-8";  // as DNOA sent it
                    return (string)await SharedHttpClient.GetResponseForAuthenticatedUri(tokenEndpoint, null, HttpMethod.Post, content, (response) =>
                    {
                        string body = response.Content.ReadAsStringAsync().Result;
                        if (!response.IsSuccessStatusCode)
                            throw TokenError((int)response.StatusCode, body, tokenEndpoint);
                        return body;
                    }, headers).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is OperationCanceledException || ex is IOException)
            {
                // DNOA wrapped transport failures in ProtocolException too
                throw new OAuthProtocolException(null, String.Format(CultureInfo.InvariantCulture, "Error communicating with {0}: {1}", tokenEndpoint.Host, ex.Message), ex);
            }
        }
        #endregion

        #region Responses
        /// <summary>
        /// Applies a successful token response to the state, with DNOA's semantics (see file header).
        /// </summary>
        public static void ApplyTokenResponse(IAuthorizationState state, string json, DateTime utcNow)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            JObject o;
            try
            {
                o = JObject.Parse(json ?? string.Empty);
            }
            catch (JsonException ex)
            {
                throw new OAuthProtocolException(null, "Token endpoint returned a response that isn't JSON", ex);
            }

            string accessToken = StringValue(o, "access_token");
            if (String.IsNullOrEmpty(accessToken))
                throw new OAuthProtocolException(StringValue(o, "error"), "Token endpoint response has no access_token" + (o["error"] == null ? string.Empty : ": " + StringValue(o, "error")));

            state.AccessToken = accessToken;
            state.AccessTokenIssueDateUtc = utcNow;
            string expiresIn = StringValue(o, "expires_in");
            state.AccessTokenExpirationUtc = (expiresIn != null && double.TryParse(expiresIn, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)) ? utcNow.AddSeconds(seconds) : (DateTime?)null;

            // The server MAY issue a new refresh token; if it doesn't, keep the one we have.
            string refreshToken = StringValue(o, "refresh_token");
            if (refreshToken != null)
                state.RefreshToken = refreshToken;

            // The response's scope describes the access token; only adopt it if we didn't already know the grant's scope.
            string scope = StringValue(o, "scope");
            if (state.Scope.Count == 0 && scope != null)
                state.Scope.UnionWith(OAuthScopes.SplitScopes(scope));
        }

        /// <summary>
        /// Builds the exception for a failed token request, pulling the RFC 6749 error/error_description out of the body when present.
        /// </summary>
        public static OAuthProtocolException TokenError(int statusCode, string body, Uri endpoint = null)
        {
            string error = null, description = null;
            try
            {
                JObject o = JObject.Parse(body ?? string.Empty);
                error = StringValue(o, "error");
                description = StringValue(o, "error_description");
            }
            catch (JsonException) { }

            string where = endpoint == null ? "Token endpoint" : endpoint.Host;
            string message = String.Format(CultureInfo.InvariantCulture, "{0} returned HTTP {1}{2}{3}", where, statusCode,
                error == null ? string.Empty : ": " + error,
                description == null ? (error == null && !String.IsNullOrEmpty(body) ? " - " + (body.Length > 200 ? body.Substring(0, 200) : body) : string.Empty) : " (" + description + ")");
            return new OAuthProtocolException(error, message) { ResponseBody = body };
        }

        /// <summary>Token fields arrive as strings or numbers (expires_in is often a number; some servers quote it)</summary>
        private static string StringValue(JObject o, string name)
        {
            JToken t = o[name];
            if (t == null || t.Type == JTokenType.Null)
                return null;
            return t.Type == JTokenType.String ? (string)t : t.ToString(Formatting.None);
        }
        #endregion
    }
}
