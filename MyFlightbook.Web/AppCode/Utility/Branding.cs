using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2012-2024 MyFlightbook LLC
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
        public BrandID BrandID { get; private set; }

        /// <summary>
        /// The name of the app (e.g., "MyFlightbook")
        /// </summary>
        public string AppName { get; private set; }

        /// <summary>
        /// The host name for the app (e.g., "myflightbook.com")
        /// </summary>
        public string HostName { get; private set; }

        /// <summary>
        /// The root for the app (e.g., "/logbook" on the live site).  The equivalent to "~" in relative URLs
        /// </summary>
        public string Root { get; private set; }

        /// <summary>
        /// The URL for the logo for the app (upper corner)
        /// </summary>
        public string LogoHRef { get; private set; }

        /// <summary>
        /// The URL for the icon for a browser tab
        /// </summary>
        public string IconHRef { get; private set; }

        /// <summary>
        /// The Email address used for mail that gets sent from the app
        /// </summary>
        public string EmailAddress { get; private set; }

        /// <summary>
        /// Link to Facebook feed
        /// </summary>
        public string FacebookFeed { get; private set; }

        /// <summary>
        /// Link to Twitter feed
        /// </summary>
        public string TwitterFeed { get; private set; }

        /// <summary>
        /// Link to Blog
        /// </summary>
        public string BlogAddress { get; private set; }

        /// <summary>
        /// Link to stylesheet path.
        /// </summary>
        public string StyleSheet { get; private set; }

        /// <summary>
        /// Link to any video/tutorial channel
        /// </summary>
        public string VideoRef { get; private set; }

        /// <summary>
        /// Link to any swag shop
        /// </summary>
        public string SwagRef { get; private set; }

        /// <summary>
        /// which AWS bucket to use for this brand?
        /// </summary>
        public string AWSBucket { get; private set; }

        /// <summary>
        /// Which LocalConfig key retrieves the pipeline config for this brand
        /// </summary>
        public string AWSETSPipelineConfigKey { get; private set; }
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

        static private Collection<Brand> _knownBrands;

        static public Collection<Brand> KnownBrands
        {
            get
            {
                if (_knownBrands == null)
                {
                    _knownBrands = new Collection<Brand> {
                        new Brand(BrandID.brandMyFlightbook)
                        {
                            AppName = "MyFlightbook",
                            HostName = "myflightbook.com",
                            Root = "/logbook",
                            LogoHRef = "~/Images/mfblogonew.png",
                            IconHRef = "~/Images/favicon.png",
                            StyleSheet = string.Empty,
                            EmailAddress = "noreply@mg.myflightbook.com",
                            FacebookFeed = "https://www.facebook.com/MyFlightbook",
                            // TwitterFeed = "https://twitter.com/MyFlightbook",
                            SwagRef = "https://www.cafepress.com/shop/MyFlightbookSwagShop/products?designId=134274099",
                            BlogAddress = "https://myflightbookblog.blogspot.com/",
                            VideoRef = "https://www.youtube.com/channel/UC6oqJL-aLMEagSyV0AKkIoQ?view_as=subscriber",
                            AWSBucket = "mfbimages",
                            AWSETSPipelineConfigKey = "ETSPipelineID"
                        },
                        new Brand(BrandID.brandMyFlightbookStaging)
                        {
                            AppName = "MFBStaging",
                            HostName = "staging.myflightbook.com",
                            Root = "/logbook",
                            IconHRef = "~/Images/favicon-stg.png",
                            LogoHRef = "~/Images/myflightbooknewstaging.png",
                            StyleSheet = "~/Public/CSS/staging.css",
                            EmailAddress = "noreply@mg.myflightbook.com",
                            AWSBucket = "mfb-staging",
                            AWSETSPipelineConfigKey = "ETSPipelineIDStaging"
                        },
                        new Brand(BrandID.brandNone)
                        {
                            AppName = string.Empty,
                            HostName = "myflightbook.com",
                            Root = "/logbook",
                            IconHRef = string.Empty,
                            LogoHRef = string.Empty,
                            StyleSheet = string.Empty,
                            EmailAddress = "noreply@mg.myflightbook.com",
                            AWSBucket = "mfbimages",
                            AWSETSPipelineConfigKey = "ETSPipelineID"
                        }
                    };
                }
                return _knownBrands;
            }
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
        public IDictionary<FooterLinkKey, BrandLink> FooterLinks()
        {
            // only one brand link in mobile layout
            if (HttpContext.Current != null && HttpContext.Current.Request.IsMobileSession())
                return new Dictionary<FooterLinkKey, BrandLink>()
                {
                    { FooterLinkKey.Classic, new BrandLink() { Name = Resources.LocalizedText.footerClassicView, LinkRef = "~/Default.aspx?m=no" } },
                };

            Dictionary<FooterLinkKey, BrandLink> d = new Dictionary<FooterLinkKey, BrandLink>()
            {
                { FooterLinkKey.About, new BrandLink() { Name = Branding.ReBrand(Resources.LocalizedText.AboutTitle), LinkRef = "~/mvc/pub/about"} },
                { FooterLinkKey.Privacy, new BrandLink() { Name = Resources.LocalizedText.footerPrivacy, LinkRef = "~/mvc/pub/Privacy"} },
                { FooterLinkKey.Terms, new BrandLink() {Name = Resources.LocalizedText.footerTerms, LinkRef = "~/mvc/pub/TandC" } },
                { FooterLinkKey.Developers, new BrandLink() {Name = Resources.LocalizedText.footerDevelopers, LinkRef = "~/Public/Developer.aspx" } },
                { FooterLinkKey.Contact, new BrandLink() {Name = Resources.LocalizedText.footerContact, LinkRef = "~/Public/ContactMe.aspx" } },
                { FooterLinkKey.FAQ, new BrandLink() {Name = Resources.LocalizedText.footerFAQ, LinkRef = "~/mvc/faq" } },
                { FooterLinkKey.Videos, new BrandLink() {Name = Resources.LocalizedText.footerVideos, OpenInNewPage=true, LinkRef = VideoRef } },
                { FooterLinkKey.Blog , new BrandLink() { Name=Resources.LocalizedText.footerBlog, OpenInNewPage = true, LinkRef = BlogAddress } },
                { FooterLinkKey.Mobile, new BrandLink() {Name = Resources.LocalizedText.footerMobileAccess, LinkRef = "~/DefaultMini.aspx" } },
                { FooterLinkKey.Facebook, new BrandLink() {Name = Branding.ReBrand(Resources.LocalizedText.FollowOnFacebook), OpenInNewPage = true, LinkRef = FacebookFeed, ImageRef = "~/images/f_logo_20.png" } },
                { FooterLinkKey.Twitter, new BrandLink() { Name = Branding.ReBrand(Resources.LocalizedText.FollowOnTwitter), OpenInNewPage = true, LinkRef = TwitterFeed, ImageRef = "~/images/twitter_round_20.png" } },
                { FooterLinkKey.Swag, new BrandLink() {Name = Branding.ReBrand(Resources.LocalizedText.BuySwag), OpenInNewPage = true, LinkRef = SwagRef } }
            };

            string szUser = HttpContext.Current?.User?.Identity?.Name;

            // only offer RSS feed on a secure, authenticated connection.
            if (!String.IsNullOrWhiteSpace(szUser) && HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.IsAuthenticated && HttpContext.Current.Request.IsSecureConnection)
            {
                Encryptors.SharedDataEncryptor ec = new Encryptors.SharedDataEncryptor("mfb");
                string szEncrypted = ec.Encrypt(szUser);
                BrandLink l = new BrandLink()
                {
                    ImageRef = "~/images/xml.gif",
                    Name = Resources.LocalizedText.RSSTitle,
                    LinkRef = String.Format(System.Globalization.CultureInfo.InvariantCulture, "https://{0}{1}?uid={2}", Branding.CurrentBrand.HostName, VirtualPathUtility.ToAbsolute("~/Public/RSSCurrency.aspx"), HttpUtility.UrlEncode(szEncrypted))
                };
                d[FooterLinkKey.RSS] = l;
            }

            return d;
        }
    }

    public static class Branding
    {
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
                foreach (Brand b in Brand.KnownBrands)
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
            get { return Brand.KnownBrands[(int)CurrentBrandID]; }
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
                throw new ArgumentNullException(nameof(szTemplate));
            if (brand == null)
                throw new ArgumentNullException(nameof(brand));
            string szNew = szTemplate.Replace("%APP_NAME%", brand.AppName).
                Replace("%SHORT_DATE%", System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortDatePattern).
                Replace("%DATE_TIME%", DateTime.UtcNow.UTCDateFormatString()).
                Replace("%APP_URL%", brand.HostName).
                Replace("%APP_LOGO%", String.IsNullOrWhiteSpace(brand.LogoHRef) ? string.Empty : VirtualPathUtility.ToAbsolute(brand.LogoHRef)).
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