using System;
using System.Collections.Generic;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2012-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Injection
{
    public class ConcreteBrand : Brand
    {
        public ConcreteBrand(BrandID brandID) : base(brandID)
        {
        }

        static public readonly List<Brand> KnownBrands = new List<Brand>()
        {
            new ConcreteBrand(BrandID.brandMyFlightbook)
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
            new ConcreteBrand(BrandID.brandMyFlightbookStaging)
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
            new ConcreteBrand(BrandID.brandNone)
            {
                AppName = string.Empty,
                HostName = "myflightbook.com",
                Root = "/logbook",
                IconHRef = "~/Images/x.gif",
                LogoHRef = "~/Images/x.gif",
                StyleSheet = string.Empty,
                EmailAddress = "noreply@mg.myflightbook.com",
                AWSBucket = "mfbimages",
                AWSETSPipelineConfigKey = "ETSPipelineID"
            }
        };

        #region cached footer links
        // Because the set of potential links and their branding doesn't change from request to request (other than RSS), only do the rebranding once and use that as a base to return later
        private IReadOnlyDictionary<FooterLinkKey, BrandLink> _defFooterLinksMobile = null;
        private IDictionary<FooterLinkKey, BrandLink> _defFooterLinksFull = null;

        private IReadOnlyDictionary<FooterLinkKey, BrandLink> DefaultFooterLinksMobile
        {
            get
            {
                if (_defFooterLinksMobile == null)
                    _defFooterLinksMobile = new Dictionary<FooterLinkKey, BrandLink>()
                { { FooterLinkKey.Classic, new BrandLink() { Name = Resources.LocalizedText.footerClassicView, LinkRef = "~/mvc/pub?m=no" } } };
                return _defFooterLinksMobile;
            }
        }

        private IDictionary<FooterLinkKey, BrandLink> DefaultFooterLinks
        {
            get
            {
                if (_defFooterLinksFull == null)
                    _defFooterLinksFull = new Dictionary<FooterLinkKey, BrandLink>()
                    {
                        { FooterLinkKey.About, new BrandLink() { Name = Branding.ReBrand(Resources.LocalizedText.AboutTitle, this), LinkRef = "~/mvc/pub/about"} },
                        { FooterLinkKey.Privacy, new BrandLink() { Name = Resources.LocalizedText.footerPrivacy, LinkRef = "~/mvc/pub/Privacy"} },
                        { FooterLinkKey.Terms, new BrandLink() {Name = Resources.LocalizedText.footerTerms, LinkRef = "~/mvc/pub/TandC" } },
                        { FooterLinkKey.Developers, new BrandLink() {Name = Resources.LocalizedText.footerDevelopers, LinkRef = "~/mvc/oauth" } },
                        { FooterLinkKey.Contact, new BrandLink() {Name = Resources.LocalizedText.footerContact, LinkRef = "~/mvc/pub/contact" } },
                        { FooterLinkKey.FAQ, new BrandLink() {Name = Resources.LocalizedText.footerFAQ, LinkRef = "~/mvc/faq" } },
                        { FooterLinkKey.Videos, new BrandLink() {Name = Resources.LocalizedText.footerVideos, OpenInNewPage=true, LinkRef = VideoRef } },
                        { FooterLinkKey.Blog , new BrandLink() { Name=Resources.LocalizedText.footerBlog, OpenInNewPage = true, LinkRef = BlogAddress } },
                        { FooterLinkKey.Mobile, new BrandLink() {Name = Resources.LocalizedText.footerMobileAccess, LinkRef = "~/mvc/pub?m=yes" } },
                        { FooterLinkKey.Facebook, new BrandLink() {Name = Branding.ReBrand(Resources.LocalizedText.FollowOnFacebook, this), OpenInNewPage = true, LinkRef = FacebookFeed, ImageRef = "~/images/f_logo_20.png" } },
                        { FooterLinkKey.Twitter, new BrandLink() { Name = Branding.ReBrand(Resources.LocalizedText.FollowOnTwitter, this), OpenInNewPage = true, LinkRef = TwitterFeed, ImageRef = "~/images/twitter_round_20.png" } },
                        { FooterLinkKey.Swag, new BrandLink() {Name = Branding.ReBrand(Resources.LocalizedText.BuySwag, this), OpenInNewPage = true, LinkRef = SwagRef } }
                    };
                return new Dictionary<FooterLinkKey, BrandLink>(_defFooterLinksFull);
            }
        }
        #endregion

        /// <summary>
        /// Returns the set of footer links that are appropriate for this brand
        /// </summary>
        /// <param name="szUser"></param>
        /// <returns></returns>
        public override IReadOnlyDictionary<FooterLinkKey, BrandLink> FooterLinks(bool fIsMobile, bool fIsSecure)
        {
            // only one brand link in mobile layout
            if (fIsMobile)
                return DefaultFooterLinksMobile;

            IDictionary<FooterLinkKey, BrandLink> d = DefaultFooterLinks;

            // only offer RSS feed on a secure, authenticated connection.
            if (fIsSecure)
            {
                d[FooterLinkKey.RSS] = new BrandLink()
                {
                    ImageRef = "~/images/xml.gif",
                    Name = Resources.LocalizedText.RSSTitle,
                    LinkRef = "~/mvc/pub/rss"
                };
            }

            return (IReadOnlyDictionary<FooterLinkKey, BrandLink>)d;
        }
    }
}