using DotNetOpenAuth.OAuth2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Text;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.SocialMedia
{
    public static class SocialMediaLinks
    {
        /// <summary>
        /// Returns the Uri to view a flight (useful for FB/Twitter/Etc.)
        /// </summary>
        /// <param name="le">The flight to be shared</param>
        /// <param name="szHost">Hostname (if not provided, uses current brand)</param>
        /// <returns></returns>
        public static Uri ShareFlightUri(LogbookEntryBase le, string szHost = null)
        {
            if (le == null)
                throw new ArgumentNullException("le");
            return String.Format(CultureInfo.InvariantCulture, "~/Public/ViewPublicFlight.aspx/{0}?v={1}", le.FlightID, (new Random()).Next(10000)).ToAbsoluteURL("https", szHost ?? Branding.CurrentBrand.HostName);
        }

        /// <summary>
        /// Returns a Uri to send a flight (i.e., to another pilot)
        /// </summary>
        /// <param name="szEncodedShareKey">Encoded key that can be decrypted to share the flight</param>
        /// <param name="szHost">Hostname (if not provided, uses current brand)</param>
        /// <param name="szTarget">Target, if provided; otherwises, uses LogbookNew</param>
        /// <returns></returns>
        public static Uri SendFlightUri(string szEncodedShareKey, string szHost = null, string szTarget = null)
        {
            if (szEncodedShareKey == null)
                throw new ArgumentNullException("szEncodedShareKey");
            return String.Format(CultureInfo.InvariantCulture, "{0}?src={1}", szTarget ?? "~/Member/LogbookNew.aspx", HttpUtility.UrlEncode(szEncodedShareKey)).ToAbsoluteURL("https", szHost ?? Branding.CurrentBrand.HostName);
        }
    }

    /// <summary>
    /// Interface to be implemented by an object that can be posted on social media
    /// </summary>
    public interface IPostable
    {
        /// <summary>
        /// The comment for the post
        /// </summary>
        string SocialMediaComment { get; }

        /// <summary>
        /// The Uri, if any, for the post.  MUST BE FULLY QUALIFIED ABSOLUTE URI; can use current branding, if needed.
        /// </summary>
        Uri SocialMediaItemUri(string szHost = null);

        /// <summary>
        /// The image, if any, for the post.
        /// </summary>
        MyFlightbook.Image.MFBImageInfo SocialMediaImage(string szHost = null);

        /// <summary>
        /// Indicates if the item can be posted.
        /// </summary>
        bool CanPost { get; }
    }

    [DataContract]
    public class FacebookResponseError
    {
        [DataMember(Name = "error")]
        public FacebookError Error { get; set; }
    }

    [DataContract]
    public class FacebookError
    {
        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "code")]
        public int Code { get; set; }

        [DataMember(Name = "error_subcode")]
        public int ErrorSubcode { get; set; }
    }

    public static class MFBFacebook
    {
        public const string keyFlush = "Flush";
        public const string keySessionKey = "Facebook_session_key";
        public const string keyUserID = "Facebook_userId";
        public const string keySSessionExpiration = "Facebook_session_expires";
        public const string keyFBService = "fbService";

        public const string OAuthParam = "fbOAuth";

        public static string FACEBOOK_API_KEY { get { return LocalConfig.SettingForKey("FacebookAccessID"); } }
        public static string FACEBOOK_SECRET { get { return LocalConfig.SettingForKey("FacebookClientSecret"); } }
        private const string oAuth2AuthorizeEndpoint = "https://graph.facebook.com/oauth/authorize?scope=publish_actions";
        private const string oAuth2TokenEndpoint = "https://graph.facebook.com/oauth/access_token";


        public static FacebookResponseError ParseJSonError(string szJSon)
        {
            return szJSon.DeserialiseFromJSON<FacebookResponseError>();
        }

        private static AuthorizationServerDescription Description()
        {
            AuthorizationServerDescription desc = new AuthorizationServerDescription();
            desc.AuthorizationEndpoint = new Uri(oAuth2AuthorizeEndpoint);
            desc.ProtocolVersion = ProtocolVersion.V20;
            desc.TokenEndpoint = new Uri(oAuth2TokenEndpoint);
            return desc;
        }

        private static WebServerClient Client()
        {
            WebServerClient client = new WebServerClient(Description(), MFBFacebook.FACEBOOK_API_KEY, MFBFacebook.FACEBOOK_SECRET);
            return client;
        }

        /// <summary>
        /// Redirects to Dropbox for oAuth2 authorization.
        /// </summary>
        /// <param name="szCallbackUri">The redirect URL after the user authorizes</param>
        public static void Authorize()
        {
            HttpRequest Request = HttpContext.Current.Request;
            Uri uriCallback = new Uri(String.Format(CultureInfo.InvariantCulture, "{0}://{1}/logbook/Member/EditProfile.aspx/pftPrefs?{2}=1",
                Request.IsLocal ? "http" : "https",
                Request.Url.Host,
                OAuthParam));
            Client().RequestUserAuthorization(null, uriCallback);
        }

        /// <summary>
        /// Exchange for a longer-lived token, per https://developers.facebook.com/docs/facebook-login/access-tokens/expiration-and-extension
        /// </summary>
        /// <param name="authState">The existing authorization state; updated in place</param>
        /// <returns>True if anything (access token or expiration) is updated</returns>
        public static bool ExchangeToken(IAuthorizationState authState)
        {
            if (authState == null)
                throw new ArgumentNullException("authState");

            if (String.IsNullOrEmpty(authState.AccessToken))
                throw new MyFlightbookException("authState access token is null");

            bool fResult = false;

            using (HttpClient httpClient = new HttpClient())
            {
                string szURLExchange = String.Format(CultureInfo.InvariantCulture, "{0}?grant_type=fb_exchange_token&client_id={1}&client_secret={2}&fb_exchange_token={3}",
                    oAuth2TokenEndpoint,
                    HttpUtility.UrlEncode(MFBFacebook.FACEBOOK_API_KEY),
                    HttpUtility.UrlEncode(MFBFacebook.FACEBOOK_SECRET),
                    HttpUtility.UrlEncode(authState.AccessToken));
                string szResult = httpClient.GetStringAsync(new Uri(szURLExchange)).Result;
                if (!String.IsNullOrEmpty(szResult))
                {
                    try
                    {
                        Dictionary<string, string> values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(szResult);
                        // var values = HttpUtility.ParseQueryString(szResult); // this seems to have stopped working as of 4/8/2017.
                        string szNewAccess = values["access_token"];
                        DateTime dtNewExpiration = DateTime.Now.ToUniversalTime().AddSeconds(Convert.ToInt64(values["expires_in"], CultureInfo.InvariantCulture));
                        fResult = authState.AccessToken.CompareOrdinal(szNewAccess) != 0 || !authState.AccessTokenExpirationUtc.HasValue || authState.AccessTokenExpirationUtc.Value.CompareTo(dtNewExpiration) != 0;
                        authState.AccessToken = szNewAccess;
                        authState.AccessTokenExpirationUtc = dtNewExpiration;
                    }
                    catch (FormatException) { }
                    catch (NullReferenceException) { }
                }
            }
            return fResult;
        }

        /// <summary>
        /// Convert an authoriztion token for an access token.
        /// </summary>
        /// <param name="Request">The http request</param>
        /// <returns>The granted access token</returns>
        public static IAuthorizationState ConvertToken(HttpRequest Request)
        {
            WebServerClient consumer = new WebServerClient(Description(), MFBFacebook.FACEBOOK_API_KEY, MFBFacebook.FACEBOOK_SECRET);
            consumer.ClientCredentialApplicator = ClientCredentialApplicator.PostParameter(MFBFacebook.FACEBOOK_SECRET);
            IAuthorizationState grantedAccess = consumer.ProcessUserAuthorization(new HttpRequestWrapper(Request));

            // Exchange for a longer lived token
            ExchangeToken(grantedAccess);

            return grantedAccess;
        }

        public static void NotifyFacebookNotSetUp(string szUser)
        {
            MyFlightbook.Profile pf = MyFlightbook.Profile.GetUser(szUser);
            util.NotifyUser(String.Format(CultureInfo.CurrentCulture, Resources.WebService.NotifyFacebookSetup, Branding.CurrentBrand.AppName),
            String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.EnableFacebook, pf.UserFullName),
            new MailAddress(pf.Email, pf.UserFullName),
            false, false);

        }
    }

    /*
    /// <summary>
    /// ISocialMediaPostTarget implementer that can post to Facebook.
    /// </summary>
    public class FacebookPoster : ISocialMediaPostTarget
    {
        public bool PostToSocialMedia(IPostable o, string szUser, string szHost = null)
        {
            if (o == null)
                throw new ArgumentNullException("o");

            if (!o.CanPost)
                return false;

            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException("szUser");

            if (String.IsNullOrEmpty(szHost))
                szHost = Branding.CurrentBrand.HostName;

            Profile pf = Profile.GetUser(szUser);

            // Check for user not configured
            if (pf.FacebookAccessToken == null || String.IsNullOrEmpty(pf.FacebookAccessToken.AccessToken))
                return false;

            // NOTE: We need to update the version code below periodically as the API updates
            const string szUrl = "https://graph.facebook.com/v2.10/me/feed";

            MultipartFormDataContent form = new MultipartFormDataContent();

            Uri uriItem = o.SocialMediaItemUri(szHost);
            MyFlightbook.Image.MFBImageInfo img = o.SocialMediaImage(szHost);

            // Add in the main parameters:
            Dictionary<string, string> dictParams = new Dictionary<string, string>() {
                { "access_token", pf.FacebookAccessToken.AccessToken},
                {"message", string.Empty}
            };

            if (uriItem != null)
                dictParams.Add("link", uriItem.AbsoluteUri);

            if (img != null)
                dictParams.Add("picture", img.URLFullImage.ToAbsoluteURL("http", Branding.CurrentBrand.HostName).ToString());

            foreach (string key in dictParams.Keys)
            {
                StringContent sc = new StringContent(dictParams[key]);
                sc.Headers.ContentDisposition = (new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data") { Name = key });
                sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain") { CharSet = "ISO-8859-1" };
                form.Add(sc);
            }

            // The remainder can run on a background thread, since we don't do anything that requires a result from here.
            new System.Threading.Thread(() =>
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        HttpResponseMessage response = httpClient.PostAsync(new Uri(szUrl), form).Result;
                        string szResult = response.Content.ReadAsStringAsync().Result;

                        if (response.IsSuccessStatusCode)
                        {
                            // do a refresh, to extend as much as possible
                            if (MFBFacebook.ExchangeToken(pf.FacebookAccessToken))
                                pf.FCommit();
                        }
                        else
                            HandleFBFailure(szUser, szResult);
                    }
                    catch (MyFlightbookException) { }
                    catch (System.ArgumentNullException) { }
                    finally
                    {
                        form.Dispose();
                    }
                }
            }).Start();

            return true;
        }

        private void HandleFBFailure(string szUser, string szResult)
        {
            // see https://developers.facebook.com/docs/authentication/access-token-expiration/ for what we're trying to do here.
            try
            {
                FacebookResponseError fberr = MFBFacebook.ParseJSonError(szResult);
                if (fberr.Error != null)
                {
                    if (fberr.Error.Code == 190)
                    {
                        // De-authorize
                        Profile pf = Profile.GetUser(szUser);
                        pf.FacebookAccessToken = null;
                        pf.FCommit();

                        // let them know that there is a problem.
                        util.NotifyUser(String.Format(CultureInfo.CurrentCulture, Resources.WebService.NotifyFacebookSetup, Branding.CurrentBrand.AppName),
                            String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.ReauthFacebook,
                                pf.UserFullName,
                                fberr.Error.Code,
                                fberr.Error.ErrorSubcode,
                                fberr.Error.Message),
                            new MailAddress(pf.Email, pf.UserFullName),
                            false, false);
                    }
                    else
                    {
                        // send the raw result so we can debug.
                        util.NotifyAdminEvent("Facebook posting error", String.Format(CultureInfo.CurrentCulture, "user: {0}. FB Response: {1}", szUser, szResult), ProfileRoles.maskSiteAdminOnly);
                    }
                }
            }
            catch (MyFlightbookException) { }
        }
    }
    */

    /// <summary>
    /// ISocialMediaPostTarget implementer that can post to Twitter.
    /// </summary>
    public static class TwitterPoster
    {
        // private const string urlUpdate = "https://api.twitter.com/1.1/statuses/update.json";

        /// <summary>
        /// The content to tweet, limited to the requisite 140 chars
        /// </summary>
        /// <param name="o">The IPostable object</param>
        /// <param name="szHost">host to use, branding used if null</param>
        /// <returns>The content of the tweet</returns>
        public static string TweetContent(IPostable o, string szHost)
        {
            if (o == null)
                throw new ArgumentNullException("o");

            if (!o.CanPost)
                return string.Empty;

            Uri uriItem = o.SocialMediaItemUri(szHost);
            string szUri = uriItem == null ? string.Empty : uriItem.AbsoluteUri;

            int cch = 140;
            StringBuilder sb = new StringBuilder(cch);
            cch -= szUri.Length + 1;
            sb.Append(o.SocialMediaComment.LimitTo(cch));
            if (szUri.Length > 0)
                sb.AppendFormat(CultureInfo.CurrentCulture, " {0}", szUri);

            return sb.ToString();
        }

        /*
        public bool PostToSocialMedia(IPostable o, string szUser, string szHost = null)
        {
            if (o == null)
                throw new ArgumentNullException("o");

            if (!o.CanPost)
                return false;

            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException("szUser");

            if (String.IsNullOrEmpty(szHost))
                szHost = Branding.CurrentBrand.HostName;

            Profile pf = Profile.GetUser(szUser);

            // Check for user not configured
            if (pf.TwitterAccessToken == null || String.IsNullOrEmpty(pf.TwitterAccessToken))
                return false;

            oAuthTwitter oAuth = new oAuthTwitter() { Token = pf.TwitterAccessToken, TokenSecret = pf.TwitterAccessSecret };
            string result = oAuth.oAuthWebRequest(oAuthTwitter.Method.POST, urlUpdate, "status=" + HttpUtility.UrlEncode(TweetContent(o, szHost).ToString()));
            return result.Contains("created_at");
        }
        */
    }

    public static class TwitterConstants
    {
        // public const string CallBackPageFormat = "~/Member/PostFlight.aspx";
    }

    public static class GooglePlusConstants
    {
        public static string ClientID { get { return LocalConfig.SettingForKey("GooglePlusAccessID"); } }
        public static string ClientSecret { get { return LocalConfig.SettingForKey("GooglePlusClientSecret"); } }
        public static string APIKey { get { return LocalConfig.SettingForKey("GooglePlusAPIKey"); } }
        public static string MapsKey { get { return LocalConfig.SettingForKey("GoogleMapsKey"); } }
    }

    public static class SocialNetworkAuthorization
    {
        public const string keyArrayRedirects = "SocialNetowrkingRedirectArrays";
        public const string DefaultRedirPage = "~/Member/LogbookNew.aspx";
        public const string DefaultRedirPageMini = "~/Member/MiniRecents.aspx";

        public static ArrayList RedirectList
        {
            get
            {
                ArrayList al = (ArrayList)HttpContext.Current.Session[keyArrayRedirects];

                if (al == null)
                {
                    al = new ArrayList();
                    HttpContext.Current.Session[keyArrayRedirects] = al;
                }

                return al;
            }
        }

        /// <summary>
        /// Redirects to the topmost URL on the stack
        /// </summary>
        /// <param name="szDefaultRedir">The default URL if the stack is empty</param>
        /// <returns>The URL to which to redirect</returns>
        public static string PopRedirect(string szDefaultRedir)
        {
            ArrayList al = RedirectList;
            string sz = string.Empty;
            if (al.Count == 0)
                sz = szDefaultRedir;
            else
            {
                sz = al[al.Count - 1].ToString();
                al.RemoveAt(al.Count - 1);
            }
            return sz;
        }

        public static void PushRedirect(string sz)
        {
            RedirectList.Add(sz);
        }

        public static void ClearRedirect()
        {
            RedirectList.Clear();
        }
    }

}
