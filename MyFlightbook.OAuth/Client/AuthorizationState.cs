using Newtonsoft.Json;
using System;
using System.Collections.Generic;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.OAuth
{
    /// <summary>
    /// Replacement for DotNetOpenAuth.OAuth2.IAuthorizationState: MyFlightbook's credentials for a service where MyFlightbook is the OAuth CLIENT.
    /// Same member names, so call sites don't change.
    /// </summary>
    public interface IAuthorizationState
    {
        Uri Callback { get; set; }
        string RefreshToken { get; set; }
        string AccessToken { get; set; }
        DateTime? AccessTokenIssueDateUtc { get; set; }
        DateTime? AccessTokenExpirationUtc { get; set; }
        HashSet<string> Scope { get; }
        bool IsDeleted { get; set; }
    }

    /// <summary>
    /// Drop-in replacement for DotNetOpenAuth.OAuth2.AuthorizationState.
    ///
    /// JSON COMPATIBILITY: the property names MUST match DNOA's exactly (Callback, RefreshToken, AccessToken, AccessTokenExpirationUtc,
    /// AccessTokenIssueDateUtc, Scope, IsDeleted).  These objects are persisted as JSON in user preferences and in the GoogleDrive/OneDrive/
    /// CloudAhoy/Dropbox columns, serialized without TypeNameHandling, so old JSON deserializes into this class as-is and vice versa.
    /// </summary>
    [Serializable]
    public class AuthorizationState : IAuthorizationState
    {
        public AuthorizationState() { }

        public AuthorizationState(IEnumerable<string> scopes) : this()
        {
            if (scopes != null)
                Scope.UnionWith(scopes);
        }

        public Uri Callback { get; set; }

        public string RefreshToken { get; set; }

        public string AccessToken { get; set; }

        public DateTime? AccessTokenExpirationUtc { get; set; }

        public DateTime? AccessTokenIssueDateUtc { get; set; }

        /// <summary>
        /// Getter-only: Json.NET populates the existing set, exactly as it did with DNOA's private-setter HashSet.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public HashSet<string> Scope { get; } = new HashSet<string>(OAuthScopes.ScopeStringComparer);

        /// <summary>
        /// True if the user declined authorization (DNOA set this and returned a token-less state; we do the same)
        /// </summary>
        public bool IsDeleted { get; set; }
    }

    /// <summary>
    /// Replacement for DotNetOpenAuth.OAuth2.ClientType.  Numeric values MUST stay as-is: they're persisted in allowedoauthclients.ClientType.
    /// </summary>
    public enum ClientType
    {
        /// <summary>Has a secret (server-side apps)</summary>
        Confidential = 0,
        /// <summary>No secret (mobile/desktop/browser apps); must use PKCE</summary>
        Public = 1
    }

    /// <summary>
    /// Replacement for DotNetOpenAuth.Messaging.ProtocolException on the client side: thrown when a remote OAuth server rejects a request
    /// (e.g., a refresh fails) or can't be reached.
    /// </summary>
    [Serializable]
    public class OAuthProtocolException : Exception
    {
        /// <summary>The OAuth "error" code from the server's response, if any (e.g., "invalid_grant")</summary>
        public string Error { get; private set; }

        /// <summary>The raw body of the server's error response, if there was one (for callers that parse provider-specific error details)</summary>
        public string ResponseBody { get; set; }

        public OAuthProtocolException() : base() { }
        public OAuthProtocolException(string message) : base(message) { }
        public OAuthProtocolException(string message, Exception innerException) : base(message, innerException) { }
        public OAuthProtocolException(string error, string message) : base(message) { Error = error; }
        public OAuthProtocolException(string error, string message, Exception innerException) : base(message, innerException) { Error = error; }
        protected OAuthProtocolException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
