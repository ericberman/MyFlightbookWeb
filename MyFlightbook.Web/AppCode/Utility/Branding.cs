using System;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2012-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// The various brands known to the system.
    /// </summary>
    public enum BrandID
    {
        brandMyFlightbook, brandMyFlightbookStaging
    };

    public class Brand
    {
        #region properties
        /// <summary>
        /// The ID for the brand
        /// </summary>
        public BrandID BrandID { get; set; }

        /// <summary>
        /// The name of the app (e.g., "MyFlightbook")
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// The host name for the app (e.g., "myflightbook.com")
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// The root for the app (e.g., "/logbook" on the live site).  The equivalent to "~" in relative URLs
        /// </summary>
        public string Root { get; set; }

        /// <summary>
        /// The URL for the logo for the app (upper corner)
        /// </summary>
        public string LogoURL { get; set; }

        /// <summary>
        /// The Email address used for mail that gets sent from the app
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>
        /// Link to Facebook feed
        /// </summary>
        public string FacebookFeed { get; set; }

        /// <summary>
        /// Link to Twitter feed
        /// </summary>
        public string TwitterFeed { get; set; }

        /// <summary>
        /// Link to Blog
        /// </summary>
        public string BlogAddress { get; set; }

        /// <summary>
        /// Link to stylesheet path.
        /// </summary>
        public string StyleSheet { get; set; }

        /// <summary>
        /// Link to any video/tutorial channel
        /// </summary>
        public string VideoRef { get; set; }
        #endregion

        /// <summary>
        /// Creates a Brand object
        /// </summary>
        /// <param name="ID">The ID for the brand</param>
        /// <param name="szapp">The application name</param>
        /// <param name="szhost">Host name</param>
        /// <param name="szRoot">Root for the app (analogous to "~")</param>
        /// <param name="szlogo">URL to a logo image</param>
        /// <param name="szEmail">Email address from which mail is sent</param>
        /// <param name="szFacebook">Link to the facebook feed for this brand</param>
        /// <param name="szStyleSheet">Link to the stylesheet for this brand</param>
        /// <param name="szTwitter">Link to the twitter feed for this brand</param>
        public Brand(BrandID ID, string szapp, string szhost, string szRoot, string szlogo, string szStyleSheet, string szEmail, string szFacebook, string szTwitter, string szBlog) : this(ID)
        {
            BrandID = ID;
            AppName = szapp;
            HostName = szhost;
            Root = szRoot;
            LogoURL = szlogo;
            EmailAddress = szEmail;
            FacebookFeed = szFacebook;
            TwitterFeed = szTwitter;
            StyleSheet = szStyleSheet;
            BlogAddress = szBlog;
        }

        public Brand(BrandID brandID)
        {
            BrandID = brandID;
            AppName = HostName = Root = LogoURL = EmailAddress = FacebookFeed = TwitterFeed = StyleSheet = BlogAddress = VideoRef = string.Empty;
        }

        private const string szPrefixToIgnore = "www.";

        /// <summary>
        /// Determines if the specified host name matches to this brand
        /// Ignores szPrefixToIgnore ("www.") as a prefix.
        /// </summary>
        /// <param name="szHost">The hostname (e.g., "Myflightbook.com")</param>
        /// <returns>True if it matches the host</returns>
        public bool MatchesHost(string szHost)
        {
            if (szHost == null)
                throw new ArgumentNullException("szHost");
            if (szHost.StartsWith(szPrefixToIgnore, StringComparison.OrdinalIgnoreCase))
                szHost = szHost.Substring(szPrefixToIgnore.Length);
            return String.Compare(HostName, szHost, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }

    public static class Branding
    {
        static private Brand[] knownBrands =
        {
        new Brand(BrandID.brandMyFlightbook) {
            AppName ="MyFlightbook",
            HostName = "myflightbook.com",
            Root = "/logbook",
            LogoURL = "~/Public/mfblogonew.png",
            StyleSheet = string.Empty,
            EmailAddress ="noreply@mg.myflightbook.com",
            FacebookFeed = "http://www.facebook.com/MyFlightbook",
            TwitterFeed = "http://twitter.com/MyFlightbook",
            BlogAddress = "https://myflightbookblog.blogspot.com/",
            VideoRef = "https://www.youtube.com/channel/UC6oqJL-aLMEagSyV0AKkIoQ?view_as=subscriber"
            },
        new Brand(BrandID.brandMyFlightbookStaging)
        {
            AppName = "MFBStaging",
            HostName = "staging.myflightbook.com",
            Root = "/logbook",
            LogoURL = "~/Public/myflightbooknewstaging.png",
            StyleSheet = "~/Public/CSS/staging.css",
            EmailAddress = "noreply@mg.myflightbook.com"
        }
        };

        private const string brandStateKey = "_brandid";

        /// <summary>
        /// The ID of the brand for the current request.  Use session if available.
        /// </summary>
        static public BrandID CurrentBrandID
        {
            get
            {
                if (HttpContext.Current == null)
                    return BrandID.brandMyFlightbook;

                // use a session object, if available, else key off of the hostname
                if (HttpContext.Current.Session != null)
                {
                    object o = HttpContext.Current.Session[brandStateKey];
                    if (o != null)
                        return (BrandID)o;
                }

                BrandID result = BrandID.brandMyFlightbook;

                string szHost = HttpContext.Current.Request.Url.Host;
                foreach (Brand b in knownBrands)
                    if (String.Compare(szHost, b.HostName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        result = b.BrandID;
                        break;
                    }

                if (HttpContext.Current.Session != null)
                    HttpContext.Current.Session[brandStateKey] = result;

                return result;
            }
            set
            {
                if (HttpContext.Current != null && HttpContext.Current.Session != null)
                    HttpContext.Current.Session[brandStateKey] = value;
            }
        }

        /// <summary>
        /// The active brand, as defined by the current brandID.  Defaults to MyFlightbook.
        /// </summary>
        static public Brand CurrentBrand
        {
            get { return knownBrands[(int)CurrentBrandID]; }
        }

        /// <summary>
        /// Rebrands a template with appropriate brand substitutions:
        /// Current valid placeholders are:
        ///  %APP_NAME%: the name of the app
        ///  %SHORT_DATE%: Current date format (short) - date pattern
        ///  %DATE_TIME%: Current time format (long) - sample in long format
        ///  %APP_URL%: the URL (host) for the current request.
        ///  %APP_ROOT%: The root (analogous to "~") for the app brand.
        ///  %APP_LOGO%: the URL for the app logo
        /// </summary>
        /// <param name="szTemplate">The template</param>
        /// <param name="brand">The brand to use (omit for current brand)</param>
        /// <returns>The template with the appropriate substitutions</returns>
        static public string ReBrand(string szTemplate, Brand brand)
        {
            if (szTemplate == null)
                throw new ArgumentNullException("szTemplate");
            if (brand == null)
                throw new ArgumentNullException("brand");
            string szNew = szTemplate.Replace("%APP_NAME%", brand.AppName).
                Replace("%SHORT_DATE%", System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortDatePattern).
                Replace("%DATE_TIME%", DateTime.UtcNow.UTCDateFormatString()).
                Replace("%APP_URL%", brand.HostName).
                Replace("%APP_LOGO%", VirtualPathUtility.ToAbsolute(brand.LogoURL)).
                Replace("%APP_ROOT%", brand.Root);

            return szNew;
        }

        /// <summary>
        /// Rebrands a template with appropriate brand substitutions using the CURRENT BRAND
        /// </summary>
        /// <param name="szTemplate">The template</param>
        /// <returns>The template with the appropriate substitutions</returns>
        static public string ReBrand(string szTemplate)
        {
            return ReBrand(szTemplate, CurrentBrand);
        }

        static public Uri PublicFlightURL(int idFlight)
        {
            return new Uri(String.Format(System.Globalization.CultureInfo.InvariantCulture, "http://{0}/{1}public/ViewPublicFlight.aspx/{2}", CurrentBrand.HostName, CurrentBrand.Root, idFlight));
        }
    }
}