using System;
using System.Globalization;
using System.Text;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2026 MyFlightbook LLC
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
        public static Uri ShareFlightUri(LogbookEntryCore le, string szHost = null)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            return String.Format(CultureInfo.InvariantCulture, "~/mvc/pub/ViewFlight/{0}?v={1}", le.FlightID, (new Random()).Next(10000)).ToAbsoluteBrandedUri();
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
                throw new ArgumentNullException(nameof(szEncodedShareKey));
            return String.Format(CultureInfo.InvariantCulture, "{0}?src={1}", szTarget ?? "~/mvc/flightedit/flight", HttpUtility.UrlEncode(szEncodedShareKey)).ToAbsoluteURL("https", szHost ?? Branding.CurrentBrand.HostName);
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
        Image.MFBImageInfo SocialMediaImage(string szHost = null);

        /// <summary>
        /// Indicates if the item can be posted.
        /// </summary>
        bool CanPost { get; }
    }

    /// <summary>
    /// Obsolete class from when we supported Facebook via oAuth.
    /// </summary>
    public static class MFBFacebook
    {
        public static string FACEBOOK_API_KEY { get { return LocalConfig.SettingForKey("FacebookAccessID"); } }
        public static string FACEBOOK_SECRET { get { return LocalConfig.SettingForKey("FacebookClientSecret"); } }
    }

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
                throw new ArgumentNullException(nameof(o));

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
    }

    public static class GooglePlusConstants
    {
        public static string ClientID { get { return LocalConfig.SettingForKey("GooglePlusAccessID"); } }
        public static string ClientSecret { get { return LocalConfig.SettingForKey("GooglePlusClientSecret"); } }
        public static string APIKey { get { return LocalConfig.SettingForKey("GooglePlusAPIKey"); } }
        public static string MapsKey { get { return LocalConfig.SettingForKey("GoogleMapsKey"); } }
    }
}
