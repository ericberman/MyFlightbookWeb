using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;

/******************************************************
 * 
 * Copyright (c) 2007-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/
namespace MyFlightbook.Web
{
    public partial class MasterPage : System.Web.UI.MasterPage
    {
        #region properties
        private const string szKeyIsNakedSession = "IsNakedSession";
        protected bool IsNaked
        {
            get { return (Session[szKeyIsNakedSession] != null && (Boolean)Session[szKeyIsNakedSession] == true); }
            set { Session[szKeyIsNakedSession] = value; }
        }

        private const string szKeyIsNightSession = "IsNightSession";
        protected bool IsNight
        {
            get { return (Session[szKeyIsNightSession] != null && (Boolean)Session[szKeyIsNightSession] == true); }
            set { Session[szKeyIsNightSession] = value; }
        }

        /// <summary>
        /// Get/set the selected tab, by name.
        /// </summary>
        public tabID SelectedTab
        {
            get { return mfbHeader.SelectedTab; }
            set
            {
                TabList t = mfbHeader.TabList;
                tabID tidTop = t.TopLevelTab(value);
                mfbHeader.SelectedTab = tidTop;
                bool fHasSecondaryNav = t.ChildTabList(tidTop).Tabs.Any();
                pnlTopForm.Visible = pnlTopForm.Visible || fHasSecondaryNav;  // if sidebar is visible, DEFINITELY want pnlTopForm visible, if sidebar isn't visible, don't necessarily want to make it visible
            }
        }

        public void RefreshHeader()
        {
            mfbHeader.Refresh();
        }

        /// <summary>
        /// Boolean representing whether or not the master page shows its header
        /// </summary>
        public Boolean HasHeader
        {
            get { return mfbHeader.Visible; }
            set { mfbHeader.Visible = value; }
        }

        /// <summary>
        /// Boolean representing whether or not the master page shows its footer
        /// </summary>
        public Boolean HasFooter
        {
            get { return MfbFooter.Visible; }
            set { MfbFooter.Visible = value; }
        }

        /// <summary>
        /// Access to the page title
        /// </summary>
        public String Title
        {
            get { return Page.Header.Title; }
            set { Page.Header.Title = value; }
        }

        public bool HasTitle
        {
            get { return pnlTitle.Visible; }
            set { pnlTitle.Visible = value; }
        }

        public bool ShowSponsoredAd
        {
            get { /* return SponsoredAd1.Visible; */ return (Page.Header != null);  /* always true, but accesses page data to suppress CA1822 warning. */ }
            set { /* SponsoredAd1.Visible = value; */ }
        }

        /// <summary>
        /// Print the side-by-side nav at the top?
        /// </summary>
        public bool SuppressTopNavPrint
        {
            get { return pnlTopForm.CssClass.Contains("noprint"); }
            set
            {
                HashSet<string> hs = new HashSet<string>(pnlTopForm.CssClass.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                if (value)
                    hs.Add("noprint");
                else
                    hs.Remove("noprint");
                pnlTopForm.CssClass = String.Join(" ", hs);
            }
        }

        public Boolean SuppressMobileViewport { get; set; }

        public bool IsIOSORAndroid { get; set; }

        public string PrintingCSS
        {
            get { return cssPrinting.Href; }
            set { cssPrinting.Href = value; cssPrinting.Visible = !String.IsNullOrEmpty(value); }
        }
        #endregion

        /// <summary>
        /// Adds a meta tag to the header of the page
        /// </summary>
        /// <param name="szName">The name of the tag</param>
        /// <param name="szContent">The value of the tag</param>
        public void AddMeta(string szName, string szContent)
        {
            HtmlMeta m = new HtmlMeta();
            Page.Header.Controls.Add(m);
            m.Name = szName;
            m.Content = szContent;
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            int idBrand = util.GetIntParam(Request, "bid", -1);
            if (idBrand >= 0 && idBrand < Enum.GetNames(typeof(BrandID)).Length)
                Branding.CurrentBrandID = (BrandID)idBrand;

            if (!IsPostBack)
                Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultTitle, Branding.CurrentBrand.AppName);

            // set the right locale for the requester.
            if (Request == null || Request.UserLanguages == null || Request.UserLanguages.Length == 0)
                return;

            // Set IsIOSOrAndroid here to determine if meta tag should be sent.
            if (Request.UserAgent != null)
            {
                string szUserAgent = Request.UserAgent.ToUpperInvariant();
                if (szUserAgent.Contains("IPHONE") || szUserAgent.Contains("IPAD") || szUserAgent.Contains("ANDROID"))
                    IsIOSORAndroid = true;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.ContentType = "text/html; charset=utf-8";

            if (!IsPostBack)
            {
                if (ShuntState.IsShunted && String.IsNullOrEmpty(Request["noshunt"]))
                    Response.Redirect("~/mvc/pub/shunt");

                // if "m=no" is passed in, override mobile detection and force classic view
                if (util.GetStringParam(Request, "m") == "no")
                    util.SetMobile(false);

                if (Request.Url.GetLeftPart(UriPartial.Path).Contains("/wp-includes"))
                    throw new System.Web.HttpException(404, "Why are you probing me for wordpress, you jerks?");

                bool fResetCookieAccept = util.GetIntParam(Request, "declinecookie", 0) != 0;
                bool fCookiesAccepted = Request.Cookies[MFBConstants.keyCookiePrivacy] != null || (Page.User.Identity.IsAuthenticated && Profile.GetUser(Page.User.Identity.Name).GetPreferenceForKey<bool>(MFBConstants.keyCookiePrivacy));
                if (fResetCookieAccept && fCookiesAccepted)
                {
                    Response.Cookies[MFBConstants.keyCookiePrivacy].Expires = DateTime.Now.AddDays(-1);
                    if (Page.User.Identity.IsAuthenticated)
                        Profile.GetUser(Page.User.Identity.Name).SetPreferenceForKey(MFBConstants.keyCookiePrivacy, false);
                }
                pnlCookies.Visible = !fCookiesAccepted || fResetCookieAccept;
                lnkPrivacy.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrivacyPolicyHeader, Branding.CurrentBrand.AppName);

                lnkAppleIcon.Href = ResolveUrl("~/images/apple-touch-icon.png");
                jqueryUIModsCSS.Href = "~/Public/CSS/jqueryuimods.css?v=5".ToAbsoluteURL(Request).ToString();
                cssMain.Href = MFBConstants.BaseStylesheet.ToAbsoluteURL(Request).ToString();    // to enable forced reload
                cssMobile.Visible = Request.IsMobileSession();
                cssMobile.Href = ResolveUrl("~/Public/CSS/MobileSheet.css?v=8");
                lnkfavicon.Href = ResolveUrl(Request.IsLocal ? "~/Images/favicon-dev.png" : Branding.CurrentBrand.IconHRef);
                string szStyle = Branding.CurrentBrand.StyleSheet;
                if (szStyle.Length > 0)
                {
                    cssBranded.Href = ResolveUrl(szStyle) + "?v=1";
                    cssBranded.Visible = true;
                }

                // Record last page requested by user in application cache so that it can be reported in error reporting
                if (Page.User.Identity.IsAuthenticated)
                    Session[Page.User.Identity.Name + "-LastPage"] = String.Format(CultureInfo.InvariantCulture, "{0}:{1}{2}", Request.IsSecureConnection ? "https" : "http", Request.Url.Host, Request.Url.PathAndQuery);

                if (!Page.User.Identity.IsAuthenticated)
                    ProfileRoles.StopImpersonating();

                IsNaked = util.GetIntParam(Request, "naked", 0) != 0 || (Session["IsNaked"] != null && ((bool)Session["IsNaked"]) == true);
                if (IsNaked)
                    pnlCookies.Visible = mfbHeader.Visible = MfbFooter.Visible = /* SponsoredAd1.Visible = */ false;

                string nightRequest = util.GetStringParam(Request, "night");
                if (nightRequest.CompareCurrentCultureIgnoreCase("yes") == 0)
                    IsNight = true;
                else if (nightRequest.CompareCurrentCultureIgnoreCase("no") == 0)
                    IsNight = false;
                cssNight.Visible = IsNight;
                cssNight.Href = MFBConstants.BaseNightStylesheet;

                bool fIsImpersonating = ProfileRoles.IsImpersonating(Page.User.Identity.Name);
                pnlImpersonation.Visible = fIsImpersonating;
                if (fIsImpersonating)
                {
                    lblAdmin.Text = ProfileRoles.OriginalAdmin;
                    lblImpersonated.Text = Page.User.Identity.Name;
                }
            }

            metaFormat.Visible = IsIOSORAndroid;
            metaViewport.Visible = !SuppressMobileViewport;
        }

        protected void btnStopImpersonating_Click(object sender, EventArgs e)
        {
            ProfileRoles.StopImpersonating();
            pnlImpersonation.Visible = false;
            Response.Redirect("~/mvc/AdminUser");
        }

        protected void btnAcceptCookies_Click(object sender, EventArgs e)
        {
            pnlCookies.Visible = false;
            if (Page.User.Identity.IsAuthenticated)
                Profile.GetUser(Page.User.Identity.Name).SetPreferenceForKey(MFBConstants.keyCookiePrivacy, true);
            Response.Cookies[MFBConstants.keyCookiePrivacy].Value = "yes";
            Response.Cookies[MFBConstants.keyCookiePrivacy].Expires = DateTime.Now.AddYears(100);
        }

        protected void lnkPrivacy_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Public/Privacy.aspx");
        }
    }
}