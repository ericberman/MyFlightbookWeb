using MyFlightbook.OAuth.Legacy;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
 * MyFlightbook's OAuth 2.1 authorization server.  Implements:
 *   - RFC 6749 / OAuth 2.1: authorization code + refresh token grants
 *   - RFC 7636 PKCE (S256; "plain" only for pre-registered back-compat)
 *   - RFC 8707 resource indicators (audience-bound tokens)
 *   - RFC 9207 iss in authorization responses
 *   - RFC 8414 authorization server metadata
 *   - RFC 7009 revocation, RFC 7662 introspection, RFC 8693 token exchange (for the MCP sidecar)
 *   - Client ID Metadata Documents (via IOAuthClientRegistry)
 *   - Acceptance of DotNetOpenAuth-era refresh tokens ("upgrade on use") and, optionally, access tokens
 *
 * Framework-neutral: inputs are NameValueCollections + headers, outputs are OAuthHttpResult.
 *
*******************************************************/

namespace MyFlightbook.OAuth.Server
{
    public sealed class ValidatedAuthorizeRequest
    {
        public OAuthClient Client { get; set; }
        public string RedirectHREF { get; set; }
        public HashSet<string> Scope { get; set; }
        public string State { get; set; }
        public string Resource { get; set; }
        public string CodeChallenge { get; set; }
        public string CodeChallengeMethod { get; set; }
    }

    public sealed class AuthorizeValidationResult
    {
        public ValidatedAuthorizeRequest Request { get; set; }

        public bool IsValid { get { return Request != null; } }

        public string Error { get; set; }
        public string ErrorDescription { get; set; }

        /// <summary>
        /// If non-null, the error should be reported by redirecting here (RFC 6749 4.1.2.1).
        /// If null (bad client_id / redirect_uri), the error MUST be shown to the user and NOT redirected.
        /// </summary>
        public string ErrorRedirectHREF { get; set; }
    }

    public class OAuthServer
    {
        public const string AccessTokenPrefix = "mfb.at.";
        public const string RefreshTokenPrefix = "mfb.rt.";
        private const string TokenExchangeGrant = "urn:ietf:params:oauth:grant-type:token-exchange";
        private const string AccessTokenTypeUrn = "urn:ietf:params:oauth:token-type:access_token";

        private readonly OAuthServerOptions options;
        private readonly IOAuthServerStore store;
        private readonly IOAuthClientRegistry clients;

        public OAuthServerOptions Options { get { return options; } }

        public OAuthServer(OAuthServerOptions options, IOAuthServerStore store, IOAuthClientRegistry clients)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.clients = clients ?? throw new ArgumentNullException(nameof(clients));
            if (String.IsNullOrEmpty(options.Issuer) || options.AuthorizationEndpoint == null || options.TokenEndpoint == null)
                throw new ArgumentException("Issuer, AuthorizationEndpoint, and TokenEndpoint are required", nameof(options));
        }

        private DateTime Now { get { return options.UtcNow(); } }

        #region Authorization endpoint
        /// <summary>
        /// Validates an authorization request (the query string of GET /Authorize).  Call this both before showing
        /// the consent page and again when the user approves (the consent form should re-post the original parameters).
        /// </summary>
        public AuthorizeValidationResult ValidateAuthorizeRequest(NameValueCollection query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            AuthorizeValidationResult result = new AuthorizeValidationResult();

            // 1. Client and redirect URI.  Errors here are NOT redirected.
            OAuthClient client;
            string redirectUri;
            try
            {
                string clientId = SingleParam(query, "client_id");
                client = String.IsNullOrEmpty(clientId) ? null : clients.FindClient(clientId);
                redirectUri = SingleParam(query, "redirect_uri");
            }
            catch (OAuthException ex)
            {
                return Fail(result, ex.Error, ex.Message, null);
            }
            if (client == null)
                return Fail(result, "invalid_client", "Unknown client", null);

            if (String.IsNullOrEmpty(redirectUri))
            {
                if (client.RedirectHREFs.Count != 1)
                    return Fail(result, "invalid_request", "redirect_uri is required", null);
                redirectUri = client.RedirectHREFs[0];
            }
            else if (!IsRedirectAllowed(client, redirectUri))
                return Fail(result, "invalid_request", String.Format(CultureInfo.InvariantCulture, "The redirect URI '{0}' is not registered for this client", redirectUri), null);

            string[] states = query.GetValues("state");
            string state = states?.FirstOrDefault();
            string errRedirect = AppendQuery(redirectUri, new Dictionary<string, string>() { { "state", state }, { "iss", options.Issuer } });

            // 2. Everything else is reported back to the client via redirect.
            try
            {
                return ValidateAuthorizeRequestCore(query, client, redirectUri, state, errRedirect, result);
            }
            catch (OAuthException ex)
            {
                return Fail(result, ex.Error, ex.Message, errRedirect);
            }
        }

        private AuthorizeValidationResult ValidateAuthorizeRequestCore(NameValueCollection query, OAuthClient client, string redirectUri, string state, string errRedirect, AuthorizeValidationResult result)
        {
            if (query.GetValues("state")?.Length > 1)
                return Fail(result, "invalid_request", "Duplicate parameter: state", errRedirect);

            if (SingleParam(query, "response_type") != "code")
                return Fail(result, "unsupported_response_type", "Only response_type=code is supported", errRedirect);

            HashSet<string> scope = OAuthScopes.SplitScopes(SingleParam(query, "scope"));
            if (scope.Count == 0 && client.IsMetadataDocumentClient)
                scope = new HashSet<string>(client.AllowedScopes, StringComparer.Ordinal);
            if (scope.Any(s => !options.ScopesSupported.Contains(s)) || !scope.IsSubsetOf(client.AllowedScopes))
                return Fail(result, "invalid_scope", "One or more requested scopes is not available to this client", errRedirect);

            if (!TryGetResource(query, out string resource, out bool _))
                return Fail(result, "invalid_target", "Unknown or invalid resource", errRedirect);

            string challenge = SingleParam(query, "code_challenge");
            string method = SingleParam(query, "code_challenge_method");
            if (String.IsNullOrEmpty(challenge))
            {
                if (client.IsPublic)
                    return Fail(result, "invalid_request", "PKCE (code_challenge) is required for public clients", errRedirect);
                challenge = method = null;
            }
            else
            {
                if (String.IsNullOrEmpty(method))
                    method = "plain";   // RFC 7636 4.3 default
                if (method != "S256" && !(method == "plain" && client.AllowPlainPkce))
                    return Fail(result, "invalid_request", "code_challenge_method must be S256", errRedirect);
                if (!IsValidPkceString(challenge))
                    return Fail(result, "invalid_request", "Malformed code_challenge", errRedirect);
            }

            result.Request = new ValidatedAuthorizeRequest()
            {
                Client = client,
                RedirectHREF = redirectUri,
                Scope = scope,
                State = state,
                Resource = resource,
                CodeChallenge = challenge,
                CodeChallengeMethod = method
            };
            return result;
        }

        /// <summary>
        /// The user approved: record consent, mint a code, and return the URL to redirect the browser to.
        /// </summary>
        public string ApproveAuthorizeRequest(ValidatedAuthorizeRequest request, string userName)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (String.IsNullOrEmpty(userName))
                throw new ArgumentNullException(nameof(userName));

            string scope = OAuthScopes.JoinScopes(request.Scope);
            store.RecordAuthorization(request.Client.ClientId, userName, scope);

            string code = NewSecret();
            store.SaveAuthorizationCode(new AuthorizationCodeRecord()
            {
                CodeHash = Hash(code),
                ClientId = request.Client.ClientId,
                UserId = userName,
                RedirectHREF = request.RedirectHREF,
                Scope = scope,
                Resource = request.Resource,
                CodeChallenge = request.CodeChallenge,
                CodeChallengeMethod = request.CodeChallengeMethod,
                ExpiresUtc = Now.Add(options.AuthorizationCodeLifetime),
                FamilyId = Guid.NewGuid().ToString("N")
            });

            return AppendQuery(request.RedirectHREF, new Dictionary<string, string>() { { "code", code }, { "state", request.State }, { "iss", options.Issuer } });
        }

        /// <summary>
        /// The user declined.  Returns the URL to redirect the browser to.
        /// </summary>
        public string DenyAuthorizeRequest(ValidatedAuthorizeRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            return AppendQuery(request.RedirectHREF, new Dictionary<string, string>() { { "error", "access_denied" }, { "state", request.State }, { "iss", options.Issuer } });
        }

        private static AuthorizeValidationResult Fail(AuthorizeValidationResult r, string error, string description, string errorRedirectBase)
        {
            r.Request = null;
            r.Error = error;
            r.ErrorDescription = description;
            r.ErrorRedirectHREF = errorRedirectBase == null ? null : AppendQuery(errorRedirectBase, new Dictionary<string, string>() { { "error", error }, { "error_description", description } });
            return r;
        }

        internal static bool IsRedirectAllowed(OAuthClient client, string redirectHREF)
        {
            if (!Uri.TryCreate(redirectHREF, UriKind.Absolute, out Uri requested) || !String.IsNullOrEmpty(requested.Fragment))
                return false;

            foreach (string registered in client.RedirectHREFs)
            {
                if (String.Equals(registered, redirectHREF, StringComparison.Ordinal))
                    return true;

                if (!Uri.TryCreate(registered, UriKind.Absolute, out Uri reg))
                    continue;

                // RFC 8252 7.3 / OAuth 2.1: loopback redirects may use any port
                if (reg.IsLoopback && requested.IsLoopback && reg.Scheme == Uri.UriSchemeHttp && requested.Scheme == Uri.UriSchemeHttp &&
                    String.Equals(reg.Host, requested.Host, StringComparison.OrdinalIgnoreCase) &&
                    String.Equals(reg.PathAndQuery, requested.PathAndQuery, StringComparison.Ordinal))
                    return true;

                // Back-compat with today's Authorize check, pre-registered clients only
                if (client.UseLegacyRedirectComparison &&
                    Uri.Compare(requested, reg, UriComponents.HostAndPort | UriComponents.PathAndQuery, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }
            return false;
        }
        #endregion

        #region Token endpoint
        /// <summary>
        /// Handles POST /OAuthToken.
        /// </summary>
        /// <param name="form">The form-urlencoded body</param>
        /// <param name="authorizationHeader">The Authorization header, if any (for client_secret_basic)</param>
        public OAuthHttpResult HandleTokenRequest(NameValueCollection form, string authorizationHeader)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));
            try
            {
                OAuthClient client = AuthenticateClient(form, authorizationHeader);
                string grantType = SingleParam(form, "grant_type");
                JObject response;
                switch (grantType)
                {
                    case "authorization_code":
                        response = AuthorizationCodeGrant(client, form);
                        break;
                    case "refresh_token":
                        response = RefreshTokenGrant(client, form);
                        break;
                    case TokenExchangeGrant:
                        response = TokenExchange(client, form);
                        break;
                    default:
                        throw new OAuthException(String.IsNullOrEmpty(grantType) ? "invalid_request" : "unsupported_grant_type", "Unsupported or missing grant_type");
                }
                return JsonResult(200, response);
            }
            catch (OAuthException ex)
            {
                OAuthHttpResult r = ErrorResult(ex);
                if (ex.StatusCode == 401 && !String.IsNullOrEmpty(authorizationHeader))
                    r.Headers["WWW-Authenticate"] = "Basic realm=\"" + options.Issuer + "\"";
                return r;
            }
        }

        private JObject AuthorizationCodeGrant(OAuthClient client, NameValueCollection form)
        {
            string code = SingleParam(form, "code");
            if (String.IsNullOrEmpty(code))
                throw new OAuthException("invalid_request", "code is required");

            AuthorizationCodeRecord rec = store.ConsumeAuthorizationCode(Hash(code), out bool wasUsed);
            if (rec == null)
                throw new OAuthException("invalid_grant", "Invalid authorization code");
            if (wasUsed)
            {
                // OAuth 2.1 4.1.3: a replayed code SHOULD revoke the tokens issued from it
                store.RevokeFamily(rec.FamilyId);
                throw new OAuthException("invalid_grant", "Authorization code already used");
            }
            if (rec.ExpiresUtc < Now)
                throw new OAuthException("invalid_grant", "Authorization code expired");
            if (!String.Equals(rec.ClientId, client.ClientId, StringComparison.Ordinal))
                throw new OAuthException("invalid_grant", "Authorization code was issued to a different client");

            string redirectUri = SingleParam(form, "redirect_uri");
            if (String.IsNullOrEmpty(redirectUri))
            {
                if (String.IsNullOrEmpty(rec.CodeChallenge))
                    throw new OAuthException("invalid_grant", "redirect_uri is required");
            }
            else if (!String.Equals(redirectUri, rec.RedirectHREF, StringComparison.Ordinal))
                throw new OAuthException("invalid_grant", "redirect_uri does not match the authorization request");

            string verifier = SingleParam(form, "code_verifier");
            if (String.IsNullOrEmpty(rec.CodeChallenge))
            {
                if (!String.IsNullOrEmpty(verifier))
                    throw new OAuthException("invalid_grant", "code_verifier supplied but no code_challenge was sent");  // OAuth 2.1 4.1.3 (downgrade protection)
            }
            else if (!VerifyPkce(rec.CodeChallenge, rec.CodeChallengeMethod, verifier))
                throw new OAuthException("invalid_grant", "PKCE verification failed");

            string resource = rec.Resource;
            if (!TryGetResource(form, out string requestedResource, out bool fResourceSupplied) || (fResourceSupplied && requestedResource != resource))
                throw new OAuthException("invalid_target", "resource does not match the authorization request");

            HashSet<string> scope = OAuthScopes.SplitScopes(rec.Scope);
            return IssueTokens(client, rec.UserId, scope, scope, resource, rec.FamilyId, null);
        }

        private JObject RefreshTokenGrant(OAuthClient client, NameValueCollection form)
        {
            string refreshToken = SingleParam(form, "refresh_token");
            if (String.IsNullOrEmpty(refreshToken))
                throw new OAuthException("invalid_request", "refresh_token is required");

            string user, resource, familyId, existingRefresh = null;
            HashSet<string> grantedScope;

            if (refreshToken.StartsWith(RefreshTokenPrefix, StringComparison.Ordinal))
            {
                string hash = Hash(refreshToken);
                TokenRecord rec = store.FindToken(hash);
                if (rec == null || rec.TokenType != OAuthTokenType.Refresh)
                    throw new OAuthException("invalid_grant", "Invalid refresh token");
                if (!String.Equals(rec.ClientId, client.ClientId, StringComparison.Ordinal))
                    throw new OAuthException("invalid_grant", "Refresh token was issued to a different client");
                if (rec.ExpiresUtc.HasValue && rec.ExpiresUtc.Value < Now)
                    throw new OAuthException("invalid_grant", "Refresh token expired");

                grantedScope = OAuthScopes.SplitScopes(rec.Scope);
                if (!store.HasActiveAuthorization(rec.ClientId, rec.UserId, rec.IssuedUtc, grantedScope))
                    throw new OAuthException("invalid_grant", "Authorization has been revoked");

                if (ShouldRotate(client))
                {
                    if (rec.RotatedUtc.HasValue || !store.MarkRotated(hash, Now))
                    {
                        DateTime rotated = rec.RotatedUtc ?? store.FindToken(hash)?.RotatedUtc ?? Now;
                        if (Now - rotated > options.RefreshTokenReuseGrace)
                        {
                            // Reuse of a rotated token outside the grace window: assume theft (OAuth 2.1 4.3.1)
                            store.RevokeFamily(rec.FamilyId);
                            throw new OAuthException("invalid_grant", "Refresh token has already been used");
                        }
                    }
                }
                else
                    existingRefresh = refreshToken;   // non-rotating: hand back the same token

                user = rec.UserId;
                resource = rec.Resource;
                familyId = rec.FamilyId;
            }
            else if (LegacyDnoaTokenReader.TryReadRefreshToken(refreshToken, store.GetLegacyCryptoKey, out LegacyTokenContents legacy))
            {
                if (String.Equals(legacy.ClientIdentifier, options.LegacyProxyClientId, StringComparison.Ordinal))
                {
                    // Minted via the old PKCE proxy: the real client isn't in the token.  Only a public client may present it,
                    // and (unlike before) it must hold a live authorization for this user - see the check below.
                    if (!client.IsPublic)
                        throw new OAuthException("invalid_grant", "Refresh token was issued to a different client");
                }
                else if (!String.Equals(legacy.ClientIdentifier, client.ClientId, StringComparison.Ordinal))
                    throw new OAuthException("invalid_grant", "Refresh token was issued to a different client");

                grantedScope = new HashSet<string>(legacy.Scope, StringComparer.Ordinal);
                if (!store.HasActiveAuthorization(client.ClientId, legacy.User, legacy.UtcIssued, grantedScope))
                    throw new OAuthException("invalid_grant", "Authorization has been revoked");

                options.OnLegacyTokenUsed?.Invoke("refresh", client.ClientId, legacy.User);
                user = legacy.User;
                resource = null;
                familyId = Guid.NewGuid().ToString("N");
                // existingRefresh stays null: upgrade on use - the client gets a new-format refresh token
            }
            else
                throw new OAuthException("invalid_grant", "Invalid refresh token");

            // Optional down-scoping of the access token (RFC 6749 6)
            HashSet<string> accessScope = grantedScope;
            string requestedScope = SingleParam(form, "scope");
            if (!String.IsNullOrEmpty(requestedScope))
            {
                accessScope = OAuthScopes.SplitScopes(requestedScope);
                if (!accessScope.IsSubsetOf(grantedScope))
                    throw new OAuthException("invalid_scope", "Requested scope exceeds the original grant");
            }

            if (!TryGetResource(form, out string requestedResource, out bool fResourceSupplied) || (fResourceSupplied && requestedResource != resource))
                throw new OAuthException("invalid_target", "resource does not match the original grant");

            return IssueTokens(client, user, grantedScope, accessScope, resource, familyId, existingRefresh);
        }

        /// <summary>
        /// RFC 8693, for the MCP sidecar: trade an access token bound to the MCP resource for a short-lived token bound to the API.
        /// This is what keeps the sidecar from "passing through" the client's token, which the MCP spec forbids.
        /// </summary>
        private JObject TokenExchange(OAuthClient client, NameValueCollection form)
        {
            if (client.IsPublic || !options.TokenExchangeClients.TryGetValue(client.ClientId, out string targetResource))
                throw new OAuthException("unauthorized_client", "This client may not use token exchange");
            if (SingleParam(form, "subject_token_type") != AccessTokenTypeUrn)
                throw new OAuthException("invalid_request", "subject_token_type must be an access token");
            string requestedType = SingleParam(form, "requested_token_type");
            if (!String.IsNullOrEmpty(requestedType) && requestedType != AccessTokenTypeUrn)
                throw new OAuthException("invalid_request", "Only access tokens can be requested");

            string target = NormalizeResource(targetResource);
            AccessTokenPrincipal subject = ReadNewAccessToken(SingleParam(form, "subject_token"));
            // The subject must be bound to some OTHER resource (e.g., the MCP endpoint) - never an API token being re-minted
            if (subject == null || subject.Resource == null || subject.Resource == target)
                throw new OAuthException("invalid_grant", "Invalid subject token");

            if (!TryGetResource(form, out string requested, out bool fSupplied) || (fSupplied && requested != target))
                throw new OAuthException("invalid_target", "This client may only exchange into " + targetResource);

            HashSet<string> scope = subject.Scope;
            string requestedScope = SingleParam(form, "scope");
            if (!String.IsNullOrEmpty(requestedScope))
            {
                scope = OAuthScopes.SplitScopes(requestedScope);
                if (!scope.IsSubsetOf(subject.Scope))
                    throw new OAuthException("invalid_scope", "Requested scope exceeds the subject token");
            }

            DateTime now = Now;
            DateTime exp = now.Add(options.ExchangedAccessTokenLifetime);
            if (subject.ExpiresUtc.HasValue && subject.ExpiresUtc.Value < exp)
                exp = subject.ExpiresUtc.Value;
            string accessToken = AccessTokenPrefix + NewSecret();
            store.SaveToken(new TokenRecord()
            {
                TokenHash = Hash(accessToken),
                TokenType = OAuthTokenType.Access,
                ClientId = subject.ClientId,     // still acting for the original client + user
                UserId = subject.User,
                Scope = OAuthScopes.JoinScopes(scope),
                Resource = target,
                FamilyId = null,
                IssuedUtc = now,
                ExpiresUtc = exp
            });

            return new JObject()
            {
                ["access_token"] = accessToken,
                ["issued_token_type"] = AccessTokenTypeUrn,
                ["token_type"] = "bearer",
                ["expires_in"] = (int)Math.Max(0, (exp - now).TotalSeconds),
                ["scope"] = OAuthScopes.JoinScopes(scope)
            };
        }

        private bool ShouldRotate(OAuthClient client)
        {
            return client.IsPublic || options.RotateConfidentialClientRefreshTokens;
        }

        private JObject IssueTokens(OAuthClient client, string user, HashSet<string> grantedScope, HashSet<string> accessScope, string resource, string familyId, string existingRefreshToken)
        {
            DateTime now = Now;
            TimeSpan lifetime = client.IsMetadataDocumentClient ? options.MetadataDocumentClientAccessTokenLifetime : options.AccessTokenLifetime;

            string accessToken = AccessTokenPrefix + NewSecret();
            store.SaveToken(new TokenRecord()
            {
                TokenHash = Hash(accessToken),
                TokenType = OAuthTokenType.Access,
                ClientId = client.ClientId,
                UserId = user,
                Scope = OAuthScopes.JoinScopes(accessScope),
                Resource = resource,
                FamilyId = familyId,
                IssuedUtc = now,
                ExpiresUtc = now.Add(lifetime)
            });

            string refreshToken = existingRefreshToken;
            if (refreshToken == null)
            {
                refreshToken = RefreshTokenPrefix + NewSecret();
                store.SaveToken(new TokenRecord()
                {
                    TokenHash = Hash(refreshToken),
                    TokenType = OAuthTokenType.Refresh,
                    ClientId = client.ClientId,
                    UserId = user,
                    Scope = OAuthScopes.JoinScopes(grantedScope),
                    Resource = resource,
                    FamilyId = familyId,
                    IssuedUtc = now,
                    ExpiresUtc = null
                });
            }

            // Same shape as DNOA's response, so existing partners parse it unchanged
            return new JObject()
            {
                ["access_token"] = accessToken,
                ["token_type"] = "bearer",
                ["expires_in"] = (int)lifetime.TotalSeconds,
                ["refresh_token"] = refreshToken,
                ["scope"] = OAuthScopes.JoinScopes(accessScope)
            };
        }

        /// <summary>
        /// client_secret_basic, client_secret_post, or (public clients) client_id alone.
        /// </summary>
        private OAuthClient AuthenticateClient(NameValueCollection form, string authorizationHeader)
        {
            string id = null, secret = null;
            bool fBasic = false;
            if (!String.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorizationHeader.Substring(6).Trim()));
                    int i = decoded.IndexOf(':');
                    if (i < 0)
                        throw new FormatException();
                    id = Uri.UnescapeDataString(decoded.Substring(0, i).Replace('+', ' '));
                    secret = decoded.Substring(i + 1);
                    fBasic = true;
                }
                catch (FormatException)
                {
                    throw new OAuthException("invalid_client", "Malformed Basic authorization header", 401);
                }
            }

            string formId = SingleParam(form, "client_id");
            string formSecret = SingleParam(form, "client_secret");
            if (fBasic)
            {
                if (!String.IsNullOrEmpty(formId) && formId != id)
                    throw new OAuthException("invalid_request", "client_id mismatch");
                if (!String.IsNullOrEmpty(formSecret))
                    throw new OAuthException("invalid_request", "Use only one client authentication method");
            }
            else
            {
                id = formId;
                secret = formSecret;
            }

            if (String.IsNullOrEmpty(id))
                throw new OAuthException("invalid_client", "Client authentication required", 401);

            OAuthClient client = clients.FindClient(id);
            if (client == null)
                throw new OAuthException("invalid_client", "Unknown client", 401);

            if (client.IsPublic)
            {
                if (!String.IsNullOrEmpty(secret))
                    throw new OAuthException("invalid_client", "Public clients must not send a secret", 401);
            }
            else
            {
                bool ok = !String.IsNullOrEmpty(secret) && !String.IsNullOrEmpty(client.ClientSecret) &&
                    (FixedTimeEquals(secret, client.ClientSecret) || (fBasic && FixedTimeEquals(Uri.UnescapeDataString(secret.Replace('+', ' ')), client.ClientSecret)));
                if (!ok)
                    throw new OAuthException("invalid_client", "Client authentication failed", 401);
            }
            return client;
        }
        #endregion

        #region Resource server: validate access tokens
        /// <summary>
        /// Validates a bearer token for a resource server.
        /// </summary>
        /// <param name="token">The bearer token</param>
        /// <param name="resource">The resource doing the asking: null/ApiResource for the MyFlightbook API, or e.g. the MCP endpoint URI.
        /// Tokens bound to one resource are NOT accepted by another (RFC 8707; MCP "MUST validate audience").</param>
        /// <returns>The principal, or null if invalid/expired/wrong audience</returns>
        public AccessTokenPrincipal ValidateAccessToken(string token, string resource = null)
        {
            if (String.IsNullOrEmpty(token))
                return null;

            string normalized = (resource == null || resource == options.ApiResource) ? null : resource;

            if (token.StartsWith(AccessTokenPrefix, StringComparison.Ordinal))
            {
                AccessTokenPrincipal p = ReadNewAccessToken(token);
                return (p != null && p.Resource == normalized) ? p : null;
            }

            // Legacy tokens predate resource indicators and were only ever for the API
            if (normalized == null && options.LegacyAccessTokenReader != null)
            {
                LegacyTokenContents legacy = options.LegacyAccessTokenReader(token);
                if (legacy == null || legacy.IsExpired(Now) || String.IsNullOrEmpty(legacy.User))
                    return null;
                options.OnLegacyTokenUsed?.Invoke("access", legacy.ClientIdentifier, legacy.User);
                AccessTokenPrincipal p = new AccessTokenPrincipal()
                {
                    User = legacy.User,
                    ClientId = legacy.ClientIdentifier,
                    IssuedUtc = legacy.UtcIssued,
                    ExpiresUtc = legacy.Lifetime.HasValue ? legacy.UtcIssued.Add(legacy.Lifetime.Value) : (DateTime?)null,
                    IsLegacy = true
                };
                p.Scope.UnionWith(legacy.Scope);
                return p;
            }
            return null;
        }

        private AccessTokenPrincipal ReadNewAccessToken(string token)
        {
            if (String.IsNullOrEmpty(token) || !token.StartsWith(AccessTokenPrefix, StringComparison.Ordinal))
                return null;
            TokenRecord rec = store.FindToken(Hash(token));
            if (rec == null || rec.TokenType != OAuthTokenType.Access || (rec.ExpiresUtc.HasValue && rec.ExpiresUtc.Value < Now))
                return null;
            AccessTokenPrincipal p = new AccessTokenPrincipal()
            {
                User = rec.UserId,
                ClientId = rec.ClientId,
                Resource = rec.Resource,
                IssuedUtc = rec.IssuedUtc,
                ExpiresUtc = rec.ExpiresUtc
            };
            p.Scope.UnionWith(OAuthScopes.SplitScopes(rec.Scope));
            return p;
        }

        /// <summary>
        /// The RFC 6750 challenge for a 401 from a resource server.  For the MCP endpoint, pass the protected-resource-metadata URL (RFC 9728 5.1).
        /// </summary>
        public static string BearerChallenge(string error = null, string requiredScope = null, Uri resourceMetadata = null)
        {
            List<string> parts = new List<string>();
            if (resourceMetadata != null)
                parts.Add("resource_metadata=\"" + resourceMetadata.AbsoluteUri + "\"");
            if (!String.IsNullOrEmpty(error))
                parts.Add("error=\"" + error + "\"");
            if (!String.IsNullOrEmpty(requiredScope))
                parts.Add("scope=\"" + requiredScope + "\"");
            return parts.Count == 0 ? "Bearer" : "Bearer " + String.Join(", ", parts);
        }
        #endregion

        #region Revocation (RFC 7009) and introspection (RFC 7662)
        public OAuthHttpResult HandleRevocationRequest(NameValueCollection form, string authorizationHeader)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));
            try
            {
                OAuthClient client = AuthenticateClient(form, authorizationHeader);
                string token = SingleParam(form, "token");
                if (String.IsNullOrEmpty(token))
                    throw new OAuthException("invalid_request", "token is required");

                if (token.StartsWith(AccessTokenPrefix, StringComparison.Ordinal) || token.StartsWith(RefreshTokenPrefix, StringComparison.Ordinal))
                {
                    TokenRecord rec = store.FindToken(Hash(token));
                    if (rec != null && String.Equals(rec.ClientId, client.ClientId, StringComparison.Ordinal))
                    {
                        if (rec.TokenType == OAuthTokenType.Refresh && rec.FamilyId != null)
                            store.RevokeFamily(rec.FamilyId);    // RFC 7009 2.1: revoking a refresh token SHOULD revoke its access tokens
                        else
                            store.RevokeToken(rec.TokenHash);
                    }
                }
                // Legacy tokens can't be individually revoked; revoking the authorization (user's app page) kills them.
                // RFC 7009: respond 200 regardless.
                return new OAuthHttpResult() { StatusCode = 200, Body = string.Empty };
            }
            catch (OAuthException ex)
            {
                return ErrorResult(ex);
            }
        }

        public OAuthHttpResult HandleIntrospectionRequest(NameValueCollection form, string authorizationHeader)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));
            try
            {
                OAuthClient client = AuthenticateClient(form, authorizationHeader);
                if (client.IsPublic || !options.IntrospectionClientIds.Contains(client.ClientId))
                    throw new OAuthException("unauthorized_client", "This client may not introspect tokens", 401);

                AccessTokenPrincipal p = ReadNewAccessToken(SingleParam(form, "token"));
                if (p == null)
                    return JsonResult(200, new JObject() { ["active"] = false });

                JObject o = new JObject()
                {
                    ["active"] = true,
                    ["scope"] = OAuthScopes.JoinScopes(p.Scope),
                    ["client_id"] = p.ClientId,
                    ["username"] = p.User,
                    ["sub"] = p.User,
                    ["token_type"] = "bearer",
                    ["iat"] = ToUnix(p.IssuedUtc),
                    ["aud"] = p.Resource ?? options.ApiResource,
                    ["iss"] = options.Issuer
                };
                if (p.ExpiresUtc.HasValue)
                    o["exp"] = ToUnix(p.ExpiresUtc.Value);
                return JsonResult(200, o);
            }
            catch (OAuthException ex)
            {
                return ErrorResult(ex);
            }
        }
        #endregion

        #region Metadata (RFC 8414)
        public OAuthHttpResult AuthorizationServerMetadata()
        {
            JObject o = new JObject()
            {
                ["issuer"] = options.Issuer,
                ["authorization_endpoint"] = options.AuthorizationEndpoint?.AbsoluteUri,
                ["token_endpoint"] = options.TokenEndpoint?.AbsoluteUri,
                ["response_types_supported"] = new JArray("code"),
                ["response_modes_supported"] = new JArray("query"),
                ["grant_types_supported"] = new JArray("authorization_code", "refresh_token", TokenExchangeGrant),
                ["code_challenge_methods_supported"] = new JArray("S256"),     // MCP clients refuse to proceed without this
                ["token_endpoint_auth_methods_supported"] = new JArray("client_secret_basic", "client_secret_post", "none"),
                ["scopes_supported"] = new JArray(options.ScopesSupported.OrderBy(s => s, StringComparer.Ordinal).ToArray()),
                ["client_id_metadata_document_supported"] = true,
                ["authorization_response_iss_parameter_supported"] = true
            };
            if (options.RevocationEndpoint != null)
            {
                o["revocation_endpoint"] = options.RevocationEndpoint.AbsoluteUri;
                o["revocation_endpoint_auth_methods_supported"] = new JArray("client_secret_basic", "client_secret_post", "none");
            }
            if (options.IntrospectionEndpoint != null)
            {
                o["introspection_endpoint"] = options.IntrospectionEndpoint.AbsoluteUri;
                o["introspection_endpoint_auth_methods_supported"] = new JArray("client_secret_basic", "client_secret_post");
            }
            OAuthHttpResult r = JsonResult(200, o);
            r.Headers["Cache-Control"] = "public, max-age=3600";
            return r;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Returns false if a resource was supplied but is invalid; true otherwise, with resource = the canonical resource or null if none supplied.
        /// </summary>
        private bool TryGetResource(NameValueCollection p, out string resource, out bool supplied)
        {
            resource = null;
            string[] values = p.GetValues("resource");
            supplied = values != null && values.Length > 0;
            if (!supplied)
                return true;
            if (values.Length > 1)
                return false;   // RFC 8707 allows several; we bind each token to exactly one
            string r = values[0];
            if (!Uri.TryCreate(r, UriKind.Absolute, out Uri u) || !String.IsNullOrEmpty(u.Fragment))
                return false;
            string match = options.Resources.FirstOrDefault(known => String.Equals(known.TrimEnd('/'), r.TrimEnd('/'), StringComparison.Ordinal));
            if (match == null)
                return false;
            resource = NormalizeResource(match);
            return true;
        }

        /// <summary>API tokens are stored with a null resource, like every legacy token</summary>
        private string NormalizeResource(string r)
        {
            return (r == null || r == options.ApiResource) ? null : r;
        }

        private static string SingleParam(NameValueCollection p, string name)
        {
            string[] values = p.GetValues(name);
            if (values == null || values.Length == 0)
                return null;
            if (values.Length > 1)
                throw new OAuthException("invalid_request", "Duplicate parameter: " + name);
            return values[0];
        }

        internal static bool VerifyPkce(string challenge, string method, string verifier)
        {
            if (!IsValidPkceString(verifier))
                return false;
            if (method == "S256")
            {
                using (SHA256 sha = SHA256.Create())
                    return FixedTimeEquals(sha.ComputeHash(Encoding.ASCII.GetBytes(verifier)).ToBase64URL(), challenge);
            }
            return method == "plain" && FixedTimeEquals(verifier, challenge);
        }

        /// <summary>RFC 7636: 43-128 chars of [A-Za-z0-9-._~]</summary>
        private static bool IsValidPkceString(string s)
        {
            if (s == null || s.Length < 43 || s.Length > 128)
                return false;
            foreach (char c in s)
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '.' || c == '_' || c == '~'))
                    return false;
            return true;
        }

        public static string Hash(string token)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
                StringBuilder sb = new StringBuilder(64);
                foreach (byte b in h)
                    sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }

        private static string NewSecret()
        {
            byte[] rgb = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                rng.GetBytes(rgb);
            return rgb.ToBase64URL();
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null)
                return false;
            byte[] x = Encoding.UTF8.GetBytes(a), y = Encoding.UTF8.GetBytes(b);
            int diff = x.Length ^ y.Length;
            for (int i = 0; i < Math.Min(x.Length, y.Length); i++)
                diff |= x[i] ^ y[i];
            return diff == 0;
        }

        private static long ToUnix(DateTime utc)
        {
            return (long)(DateTime.SpecifyKind(utc, DateTimeKind.Utc) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        internal static string AppendQuery(string href, IDictionary<string, string> parameters)
        {
            StringBuilder sb = new StringBuilder(href);
            bool hasQuery = href.IndexOf('?') >= 0;
            foreach (KeyValuePair<string, string> kv in parameters)
            {
                if (kv.Value == null)
                    continue;
                sb.Append(hasQuery ? '&' : '?');
                hasQuery = true;
                sb.Append(Uri.EscapeDataString(kv.Key)).Append('=').Append(Uri.EscapeDataString(kv.Value));
            }
            return sb.ToString();
        }

        private static OAuthHttpResult JsonResult(int status, JObject o)
        {
            OAuthHttpResult r = new OAuthHttpResult() { StatusCode = status, Body = o.ToString(Newtonsoft.Json.Formatting.None) };
            r.Headers["Cache-Control"] = "no-store";
            r.Headers["Pragma"] = "no-cache";
            return r;
        }

        private static OAuthHttpResult ErrorResult(OAuthException ex)
        {
            JObject o = new JObject() { ["error"] = ex.Error ?? "server_error" };
            if (!String.IsNullOrEmpty(ex.Message))
                o["error_description"] = ex.Message;
            return JsonResult(ex.StatusCode == 0 ? 400 : ex.StatusCode, o);
        }
        #endregion
    }
}
