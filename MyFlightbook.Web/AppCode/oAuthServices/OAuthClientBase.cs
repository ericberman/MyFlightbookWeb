using DotNetOpenAuth.OAuth2;
using MyFlightbook.OAuth.CloudAhoy;
using MyFlightbook.OAuth.FlightCrewView;
using MyFlightbook.OAuth.Leon;
using MyFlightbook.OAuth.RosterBuster;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2019-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.OAuth
{
    public interface IExternalFlightSource
    {
        /// <summary>
        /// Abstraction to import flights into pending flights
        /// </summary>
        /// <param name="username">The user</param>
        /// <param name="startDate">Optional starting date</param>
        /// <param name="endDate">Optional ending date</param>
        /// <param name="request">(optional) some sources require the current context</param>
        /// <returns>An error string or empty for success.  Any pending flights loaded are a side-effect and are in pending flights</returns>
        Task<string> ImportFlights(string username, DateTime? startDate, DateTime? endDate, HttpRequestBase request = null);
    }

    /// <summary>
    /// A standard interface for finding and describing any oAuth source that can be a source for flights.
    /// </summary>
    public class ExternalFlightSource
    {
        public enum FlightSourceID { CSV, CloudAhoy, RosterBuster, Leon, FlightCrewView }

        #region properties
        /// <summary>
        /// A unique URL-safe identifier for the source
        /// </summary>
        public FlightSourceID ID { get; private set; }

        /// <summary>
        /// A call-to-action string to import from this source
        /// </summary>
        public string ServicePrompt { get; private set; }

        /// <summary>
        /// An absolute path to the icon/image for the source
        /// </summary>
        public string IconHRef { get; private set; }

        public bool RequiresDateRange { get; private set; }

        /// <summary>
        /// True if this service is set up for this user
        /// </summary>
        public Func<Profile, bool> AvailableForUser { get; private set; }

        /// <summary>
        /// If we've stored the high watermark date for retrieval for this service, that is returned here.
        /// </summary>
        public Func<Profile, DateTime?> FetchHighWaterDate { get; private set; }

        /// <summary>
        /// Returns an IExternalFlightsource for the service
        /// </summary>
        public Func<Profile, HttpRequestBase, IExternalFlightSource> GetClient { get; private set; }
        #endregion

        protected ExternalFlightSource() { }

        protected ExternalFlightSource(FlightSourceID id, string servicePrompt, string iconHRef, bool requiresDates, Func<Profile, bool> isAvailable, Func<Profile, DateTime?> fetchHighWater, Func<Profile, HttpRequestBase, IExternalFlightSource> getClient)
        {
            ID = id;
            ServicePrompt = servicePrompt;
            IconHRef = iconHRef;
            RequiresDateRange = requiresDates;
            AvailableForUser = isAvailable;
            GetClient = getClient;
            FetchHighWaterDate = fetchHighWater;
        }

        /// <summary>
        /// The full set of potentially available external flight sources
        /// </summary>
        private static readonly List<ExternalFlightSource> mAvailableSources = new List<ExternalFlightSource>()
        {
            new ExternalFlightSource(FlightSourceID.RosterBuster, Resources.LogbookEntry.RosterBusterImportHeader, "~/images/rb_logo.png".ToAbsolute(), false,
                (pf) => pf.PreferenceExists(RosterBusterClient.TokenPrefKey),
                (pf) => pf.GetPreferenceForKey<DateTime?>(RosterBusterClient.rbLastToDateKey, null),
                (pf, req) => new RosterBusterClient(pf.GetPreferenceForKey<AuthorizationState>(RosterBusterClient.TokenPrefKey), req.Url.Host)),
            new ExternalFlightSource(FlightSourceID.Leon, Resources.LogbookEntry.LeonImportHeader, "~/images/LeonLogo.svg".ToAbsolute(), true,
                (pf) => pf.PreferenceExists(LeonClient.TokenPrefKey),
                (pf) => null,
                (pf, req) => new LeonClient(pf.GetPreferenceForKey<AuthorizationState>(LeonClient.TokenPrefKey), pf.GetPreferenceForKey<string>(LeonClient.SubDomainPrefKey), LeonClient.UseSandbox(req.Url.Host))),
            new ExternalFlightSource(FlightSourceID.FlightCrewView, Resources.LogbookEntry.FlightCrewViewImportHeader, "~/images/flightcrewview.png".ToAbsolute(), false,
                (pf) => pf.PreferenceExists(FlightCrewViewClient.AccessTokenPrefKey),
                (pf) => pf.GetPreferenceForKey<DateTime?>(FlightCrewViewClient.LastAccessPrefKey, null),
                (pf, req) => new FlightCrewViewClient(pf.GetPreferenceForKey<AuthorizationState>(FlightCrewViewClient.AccessTokenPrefKey))),
            new ExternalFlightSource(FlightSourceID.CloudAhoy, Resources.LogbookEntry.ImportCloudAhoyImport, "~/images/CloudAhoyTrans.png".ToAbsolute(), false,
                (pf) => pf.CloudAhoyToken != null,
                (pf) => null,
                (pf, req) => new CloudAhoyClient(!Branding.CurrentBrand.MatchesHost(req.Url.Host)))
        };

        /// <summary>
        /// Returns the set of external flight sources that are available (configured) for the specified user
        /// </summary>
        /// <param name="pf">The profile for the user</param>
        /// <returns>A possibly empty set of configured external flight sources</returns>
        public static IList<ExternalFlightSource> ExternalSourcesForUser(Profile pf)
        {
            List<ExternalFlightSource> lst = new List<ExternalFlightSource>(mAvailableSources);
            lst.RemoveAll(efs => !efs.AvailableForUser(pf));
            return lst;
        }

        /// <summary>
        /// Return the specified source from a list
        /// </summary>
        /// <param name="id">The ID of the source</param>
        /// <param name="lst">The set of sources from which to search; if null, all known sources are searched</param>
        /// <returns></returns>
        public static ExternalFlightSource SourceForID(FlightSourceID id, IEnumerable<ExternalFlightSource> lst = null)
        {
            return (lst ?? mAvailableSources).FirstOrDefault(efs => efs.ID == id);
        }

        public static ExternalFlightSource SourceForID(string id, IEnumerable<ExternalFlightSource> lst = null)
        {
            return (Enum.TryParse<FlightSourceID>(id, true, out FlightSourceID flightSourceID)) ? SourceForID(flightSourceID, lst) : null;
        }
    }

    /// <summary>
    /// Base class for any oAuth2 Client
    /// </summary>
    public abstract class OAuthClientBase
    {
        private readonly string _oA2AuthEndpoint;
        private readonly string _oA2TokenEndpoint;
        private readonly string _oA2UpgradeEndpoint;
        private readonly string _oA2DisableTokenEndpoint;

        #region properties
        /// <summary>
        /// the LocalConfig key for the oAuth2 appkey
        /// </summary>
        private string m_AppKey { get; set; }

        /// <summary>
        /// the LocalConfig key for the oAuth2 secret
        /// </summary>
        private string m_AppSecret { get; set; }

        /// <summary>
        /// the oAuth2 authorization endpoint URL
        /// </summary>
        public string oAuth2AuthorizeEndpoint { get { return _oA2AuthEndpoint; } }

        /// <summary>
        /// the oAuth2 token endpoint URL
        /// </summary>
        public string oAuth2TokenEndpoint { get { return _oA2TokenEndpoint; } }

        /// <summary>
        /// Optional Uri - Upgrade token endpoint
        /// </summary>
        public string oAuth1UpgradeEndpoint { get { return _oA2UpgradeEndpoint; } }

        /// <summary>
        /// Optional Uri - Token Disable endpoing
        /// </summary>
        public string oAuthTokenDisableEndpoint { get { return _oA2DisableTokenEndpoint; } }

        /// <summary>
        /// The scopes that apply to this
        /// </summary>
        public IEnumerable<string> Scopes { get; set; }

        /// <summary>
        /// The oAuth2 AppKey (from LocalConfig)
        /// </summary>
        protected virtual string AppKey { get { return LocalConfig.SettingForKey(m_AppKey); } }

        /// <summary>
        /// The oAuth2 AppSecret (from LocalConfig)
        /// </summary>
        protected virtual string AppSecret { get { return LocalConfig.SettingForKey(m_AppSecret); } }

        /// <summary>
        /// The authorization state (i.e., oAuth Credentials) for this
        /// </summary>
        public IAuthorizationState AuthState { get; set; }

        /// <summary>
        /// The root path, including trailing slash (e.g., "MyFlightbook/"
        /// </summary>
        public string RootPath { get; set; }
        #endregion

        /// <summary>
        /// Creates a cloud storage provider using parameters for oAuth authentication and token retrieval
        /// </summary>
        /// <param name="szAppKeyKey">The LocalConfig key for the oAuth2 appeky</param>
        /// <param name="szAppSecretKey">The LocalConfig key for the oAuth2 secret</param>
        /// <param name="szOAuth2AuthEndpoint">The oAuth2 authorization endpoint URL</param>
        /// <param name="szOAuth2TokenEndpoint">The oAuth2 token endpoint URL</param>
        /// <param name="scopes">Array of scopes for oAuth</param>
        protected OAuthClientBase(string szAppKeyKey, string szAppSecretKey, string szOAuth2AuthEndpoint, string szOAuth2TokenEndpoint, string[] scopes = null, string szUpgradeEndpoint = null, string szDisableEndpoint = null)
        {
            m_AppKey = szAppKeyKey;
            m_AppSecret = szAppSecretKey;
            _oA2AuthEndpoint = szOAuth2AuthEndpoint;
            _oA2TokenEndpoint = szOAuth2TokenEndpoint;
            _oA2UpgradeEndpoint = szUpgradeEndpoint;
            _oA2DisableTokenEndpoint = szDisableEndpoint;
            Scopes = scopes;

            // Do everything with at least Tls1.2
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        private AuthorizationServerDescription Description()
        {
            AuthorizationServerDescription desc = new AuthorizationServerDescription
            {
                AuthorizationEndpoint = new Uri(oAuth2AuthorizeEndpoint),
                ProtocolVersion = ProtocolVersion.V20,
                TokenEndpoint = new Uri(oAuth2TokenEndpoint)
            };
            return desc;
        }

        private WebServerClient Client()
        {
            WebServerClient client = new WebServerClient(Description(), AppKey, AppSecret);
            return client;
        }

        /// <summary>
        /// Redirects for oAuth2 authorization.
        /// </summary>
        /// <param name="szCallbackUri">The redirect URL after the user authorizes</param>
        public void Authorize(Uri szCallbackUri)
        {
            Client().RequestUserAuthorization(Scopes, szCallbackUri);
        }

        protected static Uri RedirectUri(HttpRequestBase request, string basepath, string param)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (basepath == null)
                throw new ArgumentNullException(nameof(basepath));
            if (param == null)
                throw new ArgumentNullException(nameof(param));
            return new Uri(String.Format(CultureInfo.InvariantCulture, "{0}://{1}{2}?{3}=1",
                request.IsLocal && !request.IsSecureConnection ? "http" : "https",
                request.Url.Host,
                basepath,
                param));
        }

        protected static Uri RedirectUri(HttpRequest request, string basepath, string param)
        {
            return RedirectUri(new HttpRequestWrapper(request), basepath, param);
        }

        public void Authorize(HttpRequestBase request, string basepath, string param)
        {
            Authorize(RedirectUri(request, basepath, param));
        }

        public void Authorize(HttpRequest request, string basepath, string param)
        {
            Authorize(RedirectUri(new HttpRequestWrapper(request), basepath, param));
        }

        /// <summary>
        /// Sees if the access token can be used.  Must be:
        /// a) Present
        /// b) Have no expiration, OR be unexpired
        /// </summary>
        /// <returns>True if it's usable</returns>
        public bool CheckAccessToken()
        {
            if (AuthState == null)
                return false;

            if (String.IsNullOrEmpty(AuthState.AccessToken))
                return false;

            if (AuthState.AccessTokenExpirationUtc.HasValue && AuthState.AccessTokenExpirationUtc.Value.CompareTo(DateTime.UtcNow) < 0)
                return false;

            return true;
        }

        // code for GDriveError, ExtractResponseString here from https://stackoverflow.com/questions/25032513/how-to-get-error-message-returned-by-dotnetopenauth-oauth2-on-client-side
        protected class GDriveError
        {
            public string error { get; set; }
            public string error_description { get; set; }

            [JsonProperty("error_uri")]
            public string error_link { get; set; }

            public GDriveError()
            {
                error = error_description = error_link = string.Empty;
            }
        }

        protected static string ExtractResponseString(WebException webException)
        {
            if (webException == null || webException.Response == null)
                return null;

            if (!(webException.Response.GetResponseStream() is MemoryStream responseStream))
                return null;

            var responseBytes = responseStream.ToArray();

            var responseString = System.Text.Encoding.UTF8.GetString(responseBytes);
            return responseString;
        }

        /// <summary>
        /// Refreshes the access token if (a) there is a refresh token, and (b) there is not an unexpired accesstoken
        /// </summary>
        /// <returns>True if the update happened and was successful</returns>
        /// <exception cref="DotNetOpenAuth.Messaging.ProtocolException"
        public async Task<bool> RefreshAccessToken()
        {
            return await Task.Run<bool>(() =>
            {
                // Don't refresh if no refresh token, or if current access token is still valid
                if (CheckAccessToken() || String.IsNullOrEmpty(AuthState.RefreshToken))
                    return false;

                WebServerClient client = Client();
                client.RefreshAuthorization(AuthState); // Throws DotNetOpenAuth.Messaging.ProtocolException if failure.
                return true;
            }).ConfigureAwait(false);    // CA2007 - we don't know if we need to be on the same thread.
        }

        /// <summary>
        /// Convert an authorization token for an access token, using DotNetOpenAuth
        /// </summary>
        /// <param name="Request">The http request</param>
        /// <returns>The granted access token</returns>
        public virtual IAuthorizationState ConvertToken(HttpRequestBase Request)
        {
            WebServerClient consumer = new WebServerClient(Description(), AppKey, AppSecret)
            {
                ClientCredentialApplicator = ClientCredentialApplicator.PostParameter(AppSecret)
            };
            return consumer.ProcessUserAuthorization(Request);
        }

        /// <summary>
        /// Convert an authorization token for an access token, using DotNetOpenAuth
        /// </summary>
        /// <param name="Request">The http request</param>
        /// <returns>The granted access token</returns>
        public virtual IAuthorizationState ConvertToken(HttpRequest Request)
        {
            return ConvertToken(new HttpRequestWrapper(Request));
        }

        private static AuthorizationState ParseAuthResponse(string result, string redirEndpoint)
        {
            if (String.IsNullOrEmpty(result))
                throw new MyFlightbookValidationException("Null access token returned - invalid authorization passed?");

            // JSonConvert can't deserialize space-delimited scopes into a hashset, so we need to do that manually.  Uggh.
            dynamic d = JsonConvert.DeserializeObject<dynamic>(result);

            // REPORT Error - often it's if the URL we are redirecting to doesn't match the referrer, then it fails.  
            if (!String.IsNullOrEmpty(d.error ?? string.Empty))
                throw new MyFlightbookValidationException(d.error);

            string scopes = d.scope;
            AuthorizationState authstate = new AuthorizationState(scopes != null ? OAuthUtilities.SplitScopes(scopes) : null)
            {
                AccessToken = (string) d.access_token ?? string.Empty,
                AccessTokenIssueDateUtc = DateTime.UtcNow
            };

            string expires_in = d.expires_in;
            if (expires_in != null)
            {
                if (int.TryParse(expires_in, NumberStyles.Integer, CultureInfo.InvariantCulture, out int exp))
                    authstate.AccessTokenExpirationUtc = DateTime.UtcNow.AddSeconds(exp);
            }
            authstate.RefreshToken = d.refresh_token ?? string.Empty;
            authstate.Callback = String.IsNullOrEmpty(redirEndpoint) ? null : new Uri(redirEndpoint);
            return authstate;
        }

        /// <summary>
        /// Convert an authorization token for an access token, bypassing DotNetOpenAuth (which surprisingly sometimes doesn't work; see https://github.com/ericberman/MyFlightbookWeb/issues/178 and https://github.com/DotNetOpenAuth/DotNetOpenAuth/issues/312
        /// </summary>
        /// <param name="code">The code returned from the authorization step</param>
        /// <param name="redirEndpoint">The redirect Uri used originally</param>
        /// <returns>The granted access token</returns>
        public async Task<IAuthorizationState> ConvertToken(string redirEndpoint, string code)
        {
            if (String.IsNullOrEmpty(redirEndpoint))
                throw new ArgumentNullException(nameof(redirEndpoint));
            if (String.IsNullOrEmpty(code))
                throw new ArgumentNullException(nameof(code));

            Dictionary<string, string> dContent = new Dictionary<string, string>()
            {
                { "client_id" , AppKey},
                { "client_secret", AppSecret},
                { "redirect_uri", redirEndpoint },
                { "grant_type", "authorization_code" },
                { "code", code }
            };

            using (FormUrlEncodedContent c = new FormUrlEncodedContent(dContent))
            {
                string result = (string)await SharedHttpClient.GetResponseForAuthenticatedUri(new Uri(oAuth2TokenEndpoint), null, HttpMethod.Post, c, (response) =>
                {
                    string szResult = response.Content.ReadAsStringAsync().Result;
                    response.EnsureSuccessStatusCode();
                    return szResult;
                });

                return ParseAuthResponse(result, redirEndpoint);
            }
        }

        public async Task<bool> RevokeToken()
        {
            if (string.IsNullOrEmpty(oAuthTokenDisableEndpoint) || string.IsNullOrEmpty(AuthState.RefreshToken))
                return true;    // nothing to do.

            // Refresh if needed, before revoking.
            if (!CheckAccessToken() && !String.IsNullOrEmpty(AuthState.RefreshToken))
                AuthState = await RefreshAccessToken(AuthState.RefreshToken, AuthState.Callback.ToString());

            Dictionary<string, string> dContent = new Dictionary<string, string>()
            {
                { "refreshToken" , AuthState.RefreshToken }
            };

            using (FormUrlEncodedContent c = new FormUrlEncodedContent(dContent))
            {
                string result = (string)await SharedHttpClient.GetResponseForAuthenticatedUri(new Uri(oAuthTokenDisableEndpoint), AuthState.AccessToken, HttpMethod.Post, c, (response) =>
                {
                    string szResult = response.Content.ReadAsStringAsync().Result;
                    response.EnsureSuccessStatusCode();
                    return szResult;
                });

                return true;
            }
        }

        public async Task<bool> RevokeTokeBasicAuth()
        {
            if (string.IsNullOrEmpty(oAuthTokenDisableEndpoint) || string.IsNullOrEmpty(AuthState.RefreshToken))
                return true;    // nothing to do.

            Dictionary<string, string> dContent = new Dictionary<string, string>()
            {
                { "refreshToken" , AuthState.RefreshToken }
            };

            Dictionary<string, string> dHeaders = new Dictionary<string, string>()
            {
                { "Authorization", String.Format(CultureInfo.InvariantCulture, "Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes(String.Format(CultureInfo.InvariantCulture, "{0}:{1}", AppKey, AppSecret)))) }
            };

            using (FormUrlEncodedContent c = new FormUrlEncodedContent(dContent))
            {
                string result = (string)await SharedHttpClient.GetResponseForAuthenticatedUri(new Uri(oAuthTokenDisableEndpoint), null, HttpMethod.Post, c, (response) =>
                {
                    string szResult = response.Content.ReadAsStringAsync().Result;
                    response.EnsureSuccessStatusCode();
                    return szResult;
                }, dHeaders);

                return true;
            }
        }


        /// <summary>
        /// Convert an authorization token for an access token, bypassing DotNetOpenAuth (which surprisingly sometimes doesn't work); this includes the redirect URI
        /// </summary>
        /// <param name="redirEndpoint">The redirect endpoint from the original request</param>
        /// <param name="refreshToken">The refresh token</param>
        /// <param name="refreshEndpoint">The endpoint for the refresh; if null, uses the token conversion endpoint</param>
        /// <returns>The granted access token</returns>
        public async Task<IAuthorizationState> RefreshAccessToken(string refreshToken, string redirEndpoint, string refreshEndpoint = null)
        {
            if (String.IsNullOrEmpty(redirEndpoint))
                throw new ArgumentNullException(nameof(redirEndpoint));
            if (String.IsNullOrEmpty(refreshToken))
                throw new ArgumentNullException(nameof(refreshToken));

            Dictionary<string, string> dContent = new Dictionary<string, string>()
            {
                { "client_id" , AppKey},
                { "client_secret", AppSecret},
                { "redirect_uri", redirEndpoint },
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            };

            using (FormUrlEncodedContent c = new FormUrlEncodedContent(dContent))
            {
                string result = (string)await SharedHttpClient.GetResponseForAuthenticatedUri(new Uri(refreshEndpoint ?? oAuth2TokenEndpoint), null, HttpMethod.Post, c, (response) =>
                {
                    string szResult = response.Content.ReadAsStringAsync().Result;
                    response.EnsureSuccessStatusCode();
                    return szResult;
                });

                return ParseAuthResponse(result, redirEndpoint);
            }
        }

        /// <summary>
        /// Generate a code challenge for PKCE (Proof Key for Code Exchange)
        /// </summary>
        /// <param name="codeVerifier"></param>
        /// <returns></returns>

        private static readonly char[] trimChars = new char[] { '=' };

        protected static string GenerateCodeChallenge(string codeVerifier)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                return Convert.ToBase64String(hash).TrimEnd(trimChars).Replace('+', '-').Replace('/', '_');
            }
        }
    }

    public class OAuth2AdHocClient : OAuthClientBase
    {
        private readonly string _clientID;
        private readonly string _clientSecret;
        private readonly string _redirEndpoint;

        #region properties
        protected override string AppKey { get { return _clientID; } }

        protected override string AppSecret { get { return _clientSecret; } }

        public string clientID { get { return AppKey; } }

        public string clientSecret { get { return AppSecret; } }

        public string redirectEndpoint { get { return _redirEndpoint; } }
        #endregion

        public OAuth2AdHocClient(string clientID, string clientSecret, string authEndpoint, string tokenEndpoint, string redirEndpoint, string[] scopes = null) : base(string.Empty, string.Empty, authEndpoint, tokenEndpoint, scopes)
        {
            _clientID = clientID;
            _clientSecret = clientSecret;
            _redirEndpoint = redirEndpoint;
        }
    }
}