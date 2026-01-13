using System;
using System.Collections.Generic;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2012-2026 MyFlightbook LLC
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
        brandMyFlightbook, brandMyFlightbookStaging, brandNone
    };

    public class Brand
    {
        #region properties
        /// <summary>
        /// The ID for the brand
        /// </summary>
        public BrandID BrandID { get; protected set; }

        /// <summary>
        /// The name of the app (e.g., "MyFlightbook")
        /// </summary>
        public string AppName { get; protected set; }

        /// <summary>
        /// The host name for the app (e.g., "myflightbook.com")
        /// </summary>
        public string HostName { get; protected set; }

        /// <summary>
        /// The root for the app (e.g., "/logbook" on the live site).  The equivalent to "~" in relative URLs
        /// </summary>
        public string Root { get; protected set; }

        /// <summary>
        /// The URL for the logo for the app (upper corner)
        /// </summary>
        public string LogoHRef { get; protected set; }

        /// <summary>
        /// The URL for the icon for a browser tab
        /// </summary>
        public string IconHRef { get; protected set; }

        /// <summary>
        /// The Email address used for mail that gets sent from the app
        /// </summary>
        public string EmailAddress { get; protected set; }

        /// <summary>
        /// Link to Facebook feed
        /// </summary>
        public string FacebookFeed { get; protected set; }

        /// <summary>
        /// Link to Twitter feed
        /// </summary>
        public string TwitterFeed { get; protected set; }

        /// <summary>
        /// Link to Blog
        /// </summary>
        public string BlogAddress { get; protected set; }

        /// <summary>
        /// Link to stylesheet path.
        /// </summary>
        public string StyleSheet { get; protected set; }

        /// <summary>
        /// Link to any video/tutorial channel
        /// </summary>
        public string VideoRef { get; protected set; }

        /// <summary>
        /// Link to any swag shop
        /// </summary>
        public string SwagRef { get; protected set; }

        /// <summary>
        /// which AWS bucket to use for this brand?
        /// </summary>
        public string AWSBucket { get; protected set; }

        /// <summary>
        /// Which LocalConfig key retrieves the pipeline config for this brand
        /// </summary>
        public string AWSETSPipelineConfigKey { get; protected set; }
        #endregion

        public Brand(BrandID brandID)
        {
            BrandID = brandID;
            AppName = HostName = Root = LogoHRef = EmailAddress = FacebookFeed = TwitterFeed = StyleSheet = BlogAddress = VideoRef = SwagRef = AWSBucket = AWSETSPipelineConfigKey = string.Empty;
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
                throw new ArgumentNullException(nameof(szHost));
            if (szHost.StartsWith(szPrefixToIgnore, StringComparison.OrdinalIgnoreCase))
                szHost = szHost.Substring(szPrefixToIgnore.Length);
            return String.Compare(HostName, szHost, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public enum FooterLinkKey
        {
            About, Privacy, Terms, Developers, Contact, FAQ, Videos, Blog, Mobile, Classic, Facebook, Twitter, Swag, RSS
        }

        /// <summary>
        /// Returns the set of footer links that are appropriate for this brand
        /// </summary>
        /// <param name="szUser"></param>
        /// <returns></returns>
        public virtual IReadOnlyDictionary<FooterLinkKey, BrandLink> FooterLinks(bool fIsMobile, bool fIsSecure)
        {
            throw new NotImplementedException("This should be overridden in derived classes");
        }
    }

    public static class Branding
    {
        private static Brand[] _knownBrands;
        private static string _baseStyleSheet;

        static public void InitBrands(IEnumerable<Brand> brands, string baseStyleSheet)
        {
            _knownBrands = brands.ToArray();
            _baseStyleSheet = baseStyleSheet;
        }

        private const string brandStateKey = "_brandid";

        /// <summary>
        /// The ID of the brand for the current request.  Use session if available.
        /// </summary>
        static public BrandID CurrentBrandID
        {
            get
            {
                // use a session object, if available, else key off of the hostname
                if (util.RequestContext?.GetSessionValue(brandStateKey) is BrandID bSess)
                    return bSess;

                BrandID result = BrandID.brandMyFlightbook;

                string szHost = util.RequestContext?.CurrentRequestUrl?.Host ?? string.Empty;
                foreach (Brand b in _knownBrands)
                    if (String.Compare(szHost, b.HostName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        result = b.BrandID;
                        break;
                    }

                util.RequestContext?.SetSessionValue(brandStateKey, result);

                return result;
            }
            set
            {
                util.RequestContext?.SetSessionValue(brandStateKey, value);
            }
        }

        /// <summary>
        /// The active brand, as defined by the current brandID.  Defaults to MyFlightbook.
        /// </summary>
        static public Brand CurrentBrand
        {
            get { return _knownBrands.ToArray()[(int)CurrentBrandID]; }
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
        ///  %APP_CSS%: The stylesheet for the app brand.
        /// </summary>
        /// <param name="szTemplate">The template</param>
        /// <param name="brand">The brand to use (omit for current brand)</param>
        /// <returns>The template with the appropriate substitutions</returns>
        static public string ReBrand(string szTemplate, Brand brand)
        {
            if (szTemplate == null)
                throw new ArgumentNullException(nameof(szTemplate));
            if (brand == null)
                throw new ArgumentNullException(nameof(brand));
            string szNew = szTemplate.Replace("%APP_NAME%", brand.AppName).
                Replace("%SHORT_DATE%", System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortDatePattern).
                Replace("%DATE_TIME%", DateTime.UtcNow.UTCDateFormatString()).
                Replace("%APP_URL%", brand.HostName).
                Replace("%APP_LOGO%", String.IsNullOrWhiteSpace(brand.LogoHRef) ? string.Empty : brand.LogoHRef.ToAbsolute()).
                Replace("%APP_ROOT%", brand.Root).
                Replace("%APP_CSS%", (String.IsNullOrEmpty(brand.StyleSheet) ? _baseStyleSheet : brand.StyleSheet).ToAbsolute());

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
            return new Uri(String.Format(System.Globalization.CultureInfo.InvariantCulture, "http://{0}/{1}mvc/pub/ViewFlight/{2}", CurrentBrand.HostName, CurrentBrand.Root, idFlight));
        }
    }

    /// <summary>
    /// Represents a link for the header or footer
    /// </summary>
    public class BrandLink
    {
        #region Properties
        /// <summary>
        /// Optional image to place in the link
        /// </summary>
        public string ImageRef { get; set; }

        /// <summary>
        /// Display name for the link
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Destination for the link
        /// </summary>
        public string LinkRef { get; set; }

        /// <summary>
        /// True to add target=_blank
        /// </summary>
        public bool OpenInNewPage { get; set; }

        /// <summary>
        /// Determines if there is content for this.
        /// </summary>
        public bool IsVisible { get { return !String.IsNullOrEmpty(LinkRef) && !String.IsNullOrEmpty(Name); } }
        #endregion

        public BrandLink() { }
    }
}