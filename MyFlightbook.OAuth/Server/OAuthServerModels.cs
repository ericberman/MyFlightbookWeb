using System;
using System.Collections.Generic;
using System.Linq;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
 * Framework-neutral model for MyFlightbook's OAuth 2.1 authorization server.
 * Nothing in this namespace depends on System.Web or MVC, so it ports to ASP.NET Core as-is.
 *
*******************************************************/

namespace MyFlightbook.OAuth.Server
{
    /// <summary>
    /// A client, either pre-registered (allowedoauthclients) or described by a Client ID Metadata Document.
    /// </summary>
    public sealed class OAuthClient
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }

        /// <summary>Public clients have no secret and MUST use PKCE</summary>
        public bool IsPublic { get; set; }

        /// <summary>Confidential clients only</summary>
        public string ClientSecret { get; set; }

        /// <summary>Registered redirect URIs, kept as strings because matching is exact-string (a Uri would normalize them)</summary>
        public IList<string> RedirectHREFs { get; } = new List<string>();

        public HashSet<string> AllowedScopes { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>True if the client_id is an HTTPS URL resolved via a Client ID Metadata Document</summary>
        public bool IsMetadataDocumentClient { get; set; }

        /// <summary>
        /// Back-compat for pre-registered clients only: today's Authorize compares host+port+path+query case-insensitively
        /// (scheme ignored).  Metadata-document clients always get exact matching.
        /// </summary>
        public bool UseLegacyRedirectComparison { get; set; }

        /// <summary>Back-compat: the existing PKCE code accepted "plain".  Never true for metadata-document clients.</summary>
        public bool AllowPlainPkce { get; set; }
    }

    public interface IOAuthClientRegistry
    {
        /// <summary>Returns the client, or null if unknown.  May throw OAuthException(invalid_client) if a metadata document can't be resolved.</summary>
        OAuthClient FindClient(string clientId);
    }

    public sealed class AuthorizationCodeRecord
    {
        public string CodeHash { get; set; }
        public string ClientId { get; set; }
        public string UserId { get; set; }
        /// <summary>Exactly as sent in the authorization request (string, for exact comparison at the token endpoint)</summary>
        public string RedirectHREF { get; set; }
        public string Scope { get; set; }
        public string Resource { get; set; }
        public string CodeChallenge { get; set; }
        public string CodeChallengeMethod { get; set; }
        public DateTime ExpiresUtc { get; set; }
        /// <summary>Tokens issued from this code share this family, so a replayed code can revoke them</summary>
        public string FamilyId { get; set; }
    }

    public enum OAuthTokenType { Access, Refresh }

    public sealed class TokenRecord
    {
        public string TokenHash { get; set; }
        public OAuthTokenType TokenType { get; set; }
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public string Scope { get; set; }
        /// <summary>RFC 8707 audience; null = the MyFlightbook API (i.e., what every pre-migration token meant)</summary>
        public string Resource { get; set; }
        /// <summary>All tokens descending from one authorization code (or one legacy refresh token) share a family</summary>
        public string FamilyId { get; set; }
        public DateTime IssuedUtc { get; set; }
        public DateTime? ExpiresUtc { get; set; }
        /// <summary>Refresh tokens only: when it was exchanged for a successor (rotation)</summary>
        public DateTime? RotatedUtc { get; set; }
    }

    /// <summary>
    /// Persistence for the server.  The MySQL implementation lives with the rest of the MyFlightbook data code.
    /// </summary>
    public interface IOAuthServerStore
    {
        void SaveAuthorizationCode(AuthorizationCodeRecord record);

        /// <summary>
        /// ATOMICALLY marks the code used and returns it.  If the code exists but was already used, returns it with wasAlreadyUsed = true.
        /// Returns null if there is no such code.
        /// </summary>
        AuthorizationCodeRecord ConsumeAuthorizationCode(string codeHash, out bool wasAlreadyUsed);

        void SaveToken(TokenRecord token);

        TokenRecord FindToken(string tokenHash);

        /// <summary>ATOMICALLY sets RotatedUtc if it is null.  Returns false if it was already rotated.</summary>
        bool MarkRotated(string tokenHash, DateTime utcNow);

        void RevokeToken(string tokenHash);

        void RevokeFamily(string familyId);

        /// <summary>Records the user's consent (oauthclientauthorization)</summary>
        void RecordAuthorization(string clientId, string userId, string scope);

        /// <summary>
        /// The DNOA IsAuthorizationValid rule: an unexpired authorization for (client, user) created before issuedUtc (+1s slop),
        /// whose granted scopes (unioned across rows) are a superset of scopes.
        /// </summary>
        bool HasActiveAuthorization(string clientId, string userId, DateTime issuedUtc, IEnumerable<string> scopes);

        /// <summary>Raw key from cryptokeys(bucket, handle), or null.  Used only for legacy refresh tokens.</summary>
        byte[] GetLegacyCryptoKey(string bucket, string handle);
    }

    public sealed class OAuthServerOptions
    {
        /// <summary>The issuer identifier (RFC 8414 / RFC 9207), e.g. https://myflightbook.com.  A string because clients compare it exactly.</summary>
        public string Issuer { get; set; }
        public Uri AuthorizationEndpoint { get; set; }
        public Uri TokenEndpoint { get; set; }
        public Uri RevocationEndpoint { get; set; }
        public Uri IntrospectionEndpoint { get; set; }

        public TimeSpan AuthorizationCodeLifetime { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>Matches what DNOA issued, so existing partners see no change</summary>
        public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromDays(14);

        /// <summary>Metadata-document (e.g., MCP) clients get short-lived access tokens; they refresh routinely</summary>
        public TimeSpan MetadataDocumentClientAccessTokenLifetime { get; set; } = TimeSpan.FromHours(1);

        /// <summary>Token-exchange tokens (MCP sidecar -> API) are very short-lived</summary>
        public TimeSpan ExchangedAccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// How long a rotated refresh token may still be presented (e.g., a client that lost the response).
        /// After this, presenting it is treated as theft and revokes the whole family.
        /// </summary>
        public TimeSpan RefreshTokenReuseGrace { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>OAuth 2.1 requires rotation (or sender-constraint) for public clients.  Confidential clients don't rotate by default (= today's behavior).</summary>
        public bool RotateConfidentialClientRefreshTokens { get; set; }

        /// <summary>RFC 8707 resources this server issues tokens for.  The first is the MyFlightbook API; others are, e.g., the MCP endpoint.</summary>
        public IList<string> Resources { get; } = new List<string>();

        public string ApiResource { get { return Resources.FirstOrDefault(); } }

        public HashSet<string> ScopesSupported { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Clients allowed to call the introspection endpoint (e.g., the MCP sidecar)</summary>
        public HashSet<string> IntrospectionClientIds { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Clients allowed to use RFC 8693 token exchange (e.g., the MCP sidecar), mapped to the resource they may exchange INTO</summary>
        public IDictionary<string, string> TokenExchangeClients { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>The DNOA-era PKCE proxy client id; refresh tokens it minted carry this id instead of the real client</summary>
        public string LegacyProxyClientId { get; set; } = "publicClientProxy";

        /// <summary>
        /// Advertise Client ID Metadata Document support in the RFC 8414 metadata.  Set this only when the registry actually has a
        /// MetadataDocumentClientResolver (i.e., when MCP clients are being accepted).
        /// </summary>
        public bool SupportsMetadataDocumentClients { get; set; }

        /// <summary>Optional: validates a legacy (DNOA) access token.  Only needed for 14 days after cutover; then set to null.</summary>
        public Func<string, Legacy.LegacyTokenContents> LegacyAccessTokenReader { get; set; }

        /// <summary>Optional hook to log legacy-token use so you can watch it drain: (kind, clientId, user)</summary>
        public Action<string, string, string> OnLegacyTokenUsed { get; set; }

        /// <summary>For tests</summary>
        public Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;
    }

    /// <summary>
    /// A protocol error, carrying the RFC 6749 error code.
    /// </summary>
    [Serializable]
    public class OAuthException : Exception
    {
        public string Error { get; private set; }
        public int StatusCode { get; private set; }

        public OAuthException() : base() { }
        public OAuthException(string message) : base(message) { }
        public OAuthException(string message, Exception inner) : base(message, inner) { }

        public OAuthException(string error, string description, int statusCode = 400) : base(description)
        {
            Error = error;
            StatusCode = statusCode;
        }

        protected OAuthException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// What a resource server learns from a valid access token
    /// </summary>
    public sealed class AccessTokenPrincipal
    {
        public string User { get; set; }
        public string ClientId { get; set; }
        public HashSet<string> Scope { get; } = new HashSet<string>(StringComparer.Ordinal);
        public string Resource { get; set; }
        public DateTime IssuedUtc { get; set; }
        public DateTime? ExpiresUtc { get; set; }
        public bool IsLegacy { get; set; }
    }

    /// <summary>
    /// An HTTP response for the endpoint layer to write out verbatim.
    /// </summary>
    public sealed class OAuthHttpResult
    {
        public int StatusCode { get; set; } = 200;
        public string ContentType { get; set; } = "application/json; charset=utf-8";
        public string Body { get; set; } = string.Empty;
        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}

namespace MyFlightbook.OAuth
{
    /// <summary>
    /// Replacement for DotNetOpenAuth.OAuth2.OAuthUtilities scope helpers (same semantics: ordinal, space-delimited).
    /// </summary>
    public static class OAuthScopes
    {
        /// <summary>DNOA compared scopes ordinally (case-sensitive)</summary>
        public static readonly StringComparer ScopeStringComparer = StringComparer.Ordinal;

        private static readonly char[] scopeDelimiter = new char[] { ' ' };

        public static HashSet<string> SplitScopes(string scopes)
        {
            HashSet<string> result = new HashSet<string>(ScopeStringComparer);
            if (!String.IsNullOrEmpty(scopes))
                result.UnionWith(scopes.Split(scopeDelimiter, StringSplitOptions.RemoveEmptyEntries));
            return result;
        }

        public static string JoinScopes(IEnumerable<string> scopes)
        {
            return scopes == null ? string.Empty : String.Join(" ", scopes.Where(s => !String.IsNullOrEmpty(s)));
        }
    }

}
