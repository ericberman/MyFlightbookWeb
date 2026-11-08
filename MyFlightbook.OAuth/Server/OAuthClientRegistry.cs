using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
 * Client lookup: pre-registered clients (allowedoauthclients) plus OAuth Client ID Metadata Documents
 * (draft-ietf-oauth-client-id-metadata-document), which is how MCP clients such as Claude identify themselves
 * when there's no prior relationship.  (Dynamic Client Registration is deprecated in MCP 2026-07-28 and not supported.)
 *
*******************************************************/

namespace MyFlightbook.OAuth.Server
{
    public sealed class MetadataDocumentOptions
    {
        /// <summary>Scopes granted to metadata-document clients.  Keep this read-only unless you decide otherwise.</summary>
        public HashSet<string> AllowedScopes { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Optional host policy, e.g. h => h == "claude.ai" || h.EndsWith(".anthropic.com").  Null = any public host.</summary>
        public Func<string, bool> IsHostAllowed { get; set; }

        public TimeSpan FetchTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public int MaxDocumentBytes { get; set; } = 32 * 1024;
        public TimeSpan MinCache { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan MaxCache { get; set; } = TimeSpan.FromHours(24);
        public TimeSpan DefaultCache { get; set; } = TimeSpan.FromHours(1);

        /// <summary>Must fit in oauthclientauthorization.ClientId</summary>
        public int MaxClientIdLength { get; set; } = 255;

        /// <summary>Called after a successful fetch, e.g. to upsert the client name for the user's "authorized apps" page</summary>
        public Action<OAuthClient> OnResolved { get; set; }

        /// <summary>Fetches a metadata document.  Default: SharedHttpClient, no redirects, FetchTimeout, MaxDocumentBytes.  Replaceable for tests.</summary>
        public Func<Uri, Task<MetadataDocumentResponse>> Fetch { get; set; }

        /// <summary>For tests: resolves a host to addresses.  Default is DNS.</summary>
        public Func<string, IPAddress[]> ResolveHost { get; set; } = h => Dns.GetHostAddresses(h);

        /// <summary>For tests</summary>
        public Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;
    }

    /// <summary>
    /// The parts of an HTTP response the resolver cares about
    /// </summary>
    public sealed class MetadataDocumentResponse
    {
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>The body, or null if it exceeded the size limit</summary>
        public string Body { get; set; }

        public TimeSpan? MaxAge { get; set; }

        public bool NoCache { get; set; }
    }

    /// <summary>
    /// Fetches, validates, and caches Client ID Metadata Documents.
    /// </summary>
    public class MetadataDocumentClientResolver
    {
        private readonly MetadataDocumentOptions options;
        private readonly Func<Uri, Task<MetadataDocumentResponse>> fetch;
        private readonly ConcurrentDictionary<string, Tuple<OAuthClient, DateTime>> cache = new ConcurrentDictionary<string, Tuple<OAuthClient, DateTime>>(StringComparer.Ordinal);

        public MetadataDocumentClientResolver(MetadataDocumentOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            fetch = options.Fetch ?? DefaultFetch;
        }

        /// <summary>True if the client_id is URL-shaped and so should be resolved as a metadata document</summary>
        public static bool IsMetadataDocumentClientId(string clientId)
        {
            return clientId != null && clientId.StartsWith("https://", StringComparison.Ordinal);
        }

        public OAuthClient Resolve(string clientId)
        {
            ValidateClientIdUrl(clientId);

            if (cache.TryGetValue(clientId, out Tuple<OAuthClient, DateTime> cached) && cached.Item2 > options.UtcNow())
                return cached.Item1;

            // Sync-over-async is acceptable here: the callers are synchronous MVC actions; ConfigureAwait(false) avoids the ASP.NET deadlock.
            Tuple<OAuthClient, TimeSpan> fetched = Task.Run(() => FetchAsync(clientId)).GetAwaiter().GetResult();
            cache[clientId] = new Tuple<OAuthClient, DateTime>(fetched.Item1, options.UtcNow().Add(fetched.Item2));
            options.OnResolved?.Invoke(fetched.Item1);
            return fetched.Item1;
        }

        private void ValidateClientIdUrl(string clientId)
        {
            if (clientId.Length > options.MaxClientIdLength)
                throw Invalid("client_id URL is too long");
            if (!Uri.TryCreate(clientId, UriKind.Absolute, out Uri u) || u.Scheme != Uri.UriSchemeHttps)
                throw Invalid("client_id must be an https URL");
            if (!String.IsNullOrEmpty(u.UserInfo) || !String.IsNullOrEmpty(u.Fragment) || !String.IsNullOrEmpty(u.Query))
                throw Invalid("client_id URL must not contain userinfo, query, or fragment");
            if (u.AbsolutePath.Length <= 1)
                throw Invalid("client_id URL must contain a path");
            if (clientId.Contains("/./") || clientId.Contains("/../") || clientId.EndsWith("/.", StringComparison.Ordinal) || clientId.EndsWith("/..", StringComparison.Ordinal))
                throw Invalid("client_id URL must not contain dot segments");
            if (u.HostNameType != UriHostNameType.Dns)
                throw Invalid("client_id URL host must be a DNS name");
            if (!u.IsDefaultPort)
                throw Invalid("client_id URL must use the default https port");
            if (options.IsHostAllowed != null && !options.IsHostAllowed(u.Host))
                throw Invalid("This client is not permitted");

            // SSRF: never fetch from internal addresses.  (Residual risk: DNS rebinding between this check and the connect.
            // On ASP.NET Core, SocketsHttpHandler.ConnectCallback can pin the vetted address and close that gap.)
            IPAddress[] addresses;
            try
            {
                addresses = options.ResolveHost(u.Host);
            }
            catch (SocketException)
            {
                throw Invalid("client_id host could not be resolved");
            }
            if (addresses == null || addresses.Length == 0 || addresses.Any(IsNonPublicAddress))
                throw Invalid("client_id host is not a public address");
        }

        private async Task<Tuple<OAuthClient, TimeSpan>> FetchAsync(string clientId)
        {
            MetadataDocumentResponse response;
            try
            {
                response = await fetch(new Uri(clientId)).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is OperationCanceledException || ex is IOException)
            {
                throw Invalid("Unable to retrieve client metadata document");
            }

            if (response == null || response.StatusCode != HttpStatusCode.OK)
                throw Invalid(String.Format(CultureInfo.InvariantCulture, "Client metadata document returned HTTP {0}", response == null ? 0 : (int)response.StatusCode));
            if (response.Body == null)
                throw Invalid("Client metadata document is too large");

            return new Tuple<OAuthClient, TimeSpan>(ParseDocument(clientId, response.Body), CacheLifetime(response));
        }

        /// <summary>
        /// The real fetch: SharedHttpClient with redirects OFF (the document must live at exactly the client_id URL, and a redirect
        /// could bounce the request to an internal address), a short timeout, and the body read incrementally up to MaxDocumentBytes.
        /// </summary>
        private async Task<MetadataDocumentResponse> DefaultFetch(Uri uri)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>()
            {
                { "Accept", "application/json" },
                { "User-Agent", "MyFlightbook-OAuth/1.0" }
            };

            return (MetadataDocumentResponse)await SharedHttpClient.GetResponseForAuthenticatedUri(uri, null, HttpMethod.Get, null, (response) =>
            {
                MetadataDocumentResponse result = new MetadataDocumentResponse()
                {
                    StatusCode = response.StatusCode,
                    MaxAge = response.Headers.CacheControl?.MaxAge,
                    NoCache = response.Headers.CacheControl != null && (response.Headers.CacheControl.NoCache || response.Headers.CacheControl.NoStore)
                };
                if (response.StatusCode != HttpStatusCode.OK)
                    return result;
                if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value > options.MaxDocumentBytes)
                    return result;  // Body stays null => too large

                using (Stream s = response.Content.ReadAsStreamAsync().Result)
                {
                    byte[] buffer = new byte[options.MaxDocumentBytes + 1];
                    int total = 0, read;
                    while (total < buffer.Length && (read = s.Read(buffer, total, buffer.Length - total)) > 0)
                        total += read;
                    if (total <= options.MaxDocumentBytes)
                        result.Body = Encoding.UTF8.GetString(buffer, 0, total);
                }
                return result;
            }, headers, fFollowRedirects: false, timeout: options.FetchTimeout, completionOption: HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        }

        internal OAuthClient ParseDocument(string clientId, string body)
        {
            JObject doc;
            try
            {
                doc = JObject.Parse(body);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                throw Invalid("Client metadata document is not valid JSON");
            }

            if ((string)doc["client_id"] != clientId)
                throw Invalid("Client metadata document client_id does not match its URL");

            string name = ((string)doc["client_name"] ?? string.Empty).Trim();
            if (name.Length == 0)
                throw Invalid("Client metadata document is missing client_name");
            if (name.Length > 100)
                name = name.Substring(0, 100);

            if (!(doc["redirect_uris"] is JArray redirects) || redirects.Count == 0)
                throw Invalid("Client metadata document is missing redirect_uris");
            List<string> lstRedirects = new List<string>();
            foreach (JToken t in redirects)
            {
                string r = t.Type == JTokenType.String ? (string)t : null;
                if (!IsAcceptableRedirect(r))
                    throw Invalid("Client metadata document has an invalid redirect URI");
                lstRedirects.Add(r);
            }

            // Only public clients for now.  (private_key_jwt would need JWKS handling.)
            string authMethod = (string)doc["token_endpoint_auth_method"];
            if (!String.IsNullOrEmpty(authMethod) && authMethod != "none")
                throw Invalid("Unsupported token_endpoint_auth_method: " + authMethod);

            if (doc["grant_types"] is JArray grants && !grants.Any(g => (string)g == "authorization_code"))
                throw Invalid("Client must support the authorization_code grant");

            OAuthClient client = new OAuthClient()
            {
                ClientId = clientId,
                ClientName = name,
                IsPublic = true,
                IsMetadataDocumentClient = true,
                UseLegacyRedirectComparison = false,
                AllowPlainPkce = false
            };
            foreach (string r in lstRedirects)
                client.RedirectHREFs.Add(r);
            client.AllowedScopes.UnionWith(options.AllowedScopes);
            return client;
        }

        private static bool IsAcceptableRedirect(string r)
        {
            if (String.IsNullOrEmpty(r) || !Uri.TryCreate(r, UriKind.Absolute, out Uri u) || !String.IsNullOrEmpty(u.Fragment))
                return false;
            if (u.Scheme == Uri.UriSchemeHttps)
                return true;
            return u.Scheme == Uri.UriSchemeHttp && u.IsLoopback;    // RFC 8252 native apps
        }

        private TimeSpan CacheLifetime(MetadataDocumentResponse response)
        {
            TimeSpan ttl = options.DefaultCache;
            if (response.NoCache)
                ttl = options.MinCache;
            else if (response.MaxAge.HasValue)
                ttl = response.MaxAge.Value;
            if (ttl < options.MinCache)
                ttl = options.MinCache;
            if (ttl > options.MaxCache)
                ttl = options.MaxCache;
            return ttl;
        }

        internal static bool IsNonPublicAddress(IPAddress a)
        {
            if (a.IsIPv4MappedToIPv6)
                a = a.MapToIPv4();
            if (IPAddress.IsLoopback(a))
                return true;
            byte[] b = a.GetAddressBytes();
            if (a.AddressFamily == AddressFamily.InterNetwork)
            {
                return b[0] == 0 || b[0] == 10 || b[0] == 127 ||
                    (b[0] == 100 && b[1] >= 64 && b[1] <= 127) ||      // CGNAT
                    (b[0] == 169 && b[1] == 254) ||                    // link-local (incl. cloud metadata 169.254.169.254)
                    (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||
                    (b[0] == 192 && b[1] == 168) ||
                    (b[0] == 192 && b[1] == 0 && b[2] == 0) ||
                    (b[0] == 198 && (b[1] == 18 || b[1] == 19)) ||
                    b[0] >= 224;                                        // multicast + reserved
            }
            if (a.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return a.IsIPv6LinkLocal || a.IsIPv6SiteLocal || a.IsIPv6Multicast ||
                    (b[0] & 0xFE) == 0xFC ||                            // fc00::/7 unique local
                    a.Equals(IPAddress.IPv6None) || a.Equals(IPAddress.IPv6Any);
            }
            return true;
        }

        private static OAuthException Invalid(string description)
        {
            return new OAuthException("invalid_client", description, 400);
        }
    }

    /// <summary>
    /// Pre-registered clients first; URL-shaped client_ids go to the metadata-document resolver.
    /// </summary>
    public class CompositeClientRegistry : IOAuthClientRegistry
    {
        private readonly Func<string, OAuthClient> registeredLookup;
        private readonly MetadataDocumentClientResolver resolver;

        public CompositeClientRegistry(Func<string, OAuthClient> registeredLookup, MetadataDocumentClientResolver resolver)
        {
            this.registeredLookup = registeredLookup ?? throw new ArgumentNullException(nameof(registeredLookup));
            this.resolver = resolver;
        }

        public OAuthClient FindClient(string clientId)
        {
            if (String.IsNullOrEmpty(clientId))
                return null;
            if (MetadataDocumentClientResolver.IsMetadataDocumentClientId(clientId))
                return resolver?.Resolve(clientId);
            return registeredLookup(clientId);
        }
    }
}
