using MyFlightbook;
using System;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class MasterPage : System.Web.UI.MasterPage
{
    /// <summary>
    /// Which layout to use?
    /// </summary>
    public enum LayoutMode
    {
        /// <summary>
        /// MyFlightbook "Classic" - has a yellow top form with secondary navigation on the left
        /// </summary>
        Classic,
        /// <summary>
        /// MyFlightbook "Accordion" - puts everything into the topform, but no yellow background, no secondary nav
        /// </summary>
        Accordion
    }

    #region properties
    private const string szKeyIsNakedSession = "IsNakedSession";
    protected bool IsNaked
    {
        get { return (Session[szKeyIsNakedSession] != null && (Boolean)Session[szKeyIsNakedSession] == true); }
        set { Session[szKeyIsNakedSession] = value; }
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
            Sidebar.CurrentTab = value;
            Sidebar.TabList = t.ChildTabList(tidTop);
            bool fHasSecondaryNav = Sidebar.TabList.Tabs.Count() > 0;
            pnlTopForm.Visible = pnlTopForm.Visible || fHasSecondaryNav;  // if sidebar is visible, DEFINITELY want pnlTopForm visible, if sidebar isn't visible, don't necessarily want to make it visible
            SidebarPlaceholder.Visible = pnlTopForm.Visible && fHasSecondaryNav && !IsNaked;
        }
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

    private Controls_mfbLogbookSidebar m_sidebar = null;
    public Controls_mfbLogbookSidebar Sidebar
    {
        get
        {
            if (m_sidebar == null)
                SetUpSidebar();
            return m_sidebar;
        }
    }

    protected PlaceHolder SidebarPlaceholder
    {
        get { return plcSecondaryNavSide; }
    }

    public bool ShowSponsoredAd
    {
        get { return SponsoredAd1.Visible; }
        set { SponsoredAd1.Visible = value; }
    }

    /// <summary>
    /// Print the side-by-side nav at the top?
    /// </summary>
    private bool m_noPrint = false;
    public bool SuppressTopNavPrint
    {
        get { return m_noPrint; }
        set
        {
            m_noPrint = value;
            SetLayout(Layout);  // force the correct CSS class
        }
    }

    public Boolean SuppressMobileViewport { get; set; }

    public bool IsIOSORAndroid { get; set; }

    public string PrintingCSS
    {
        get { return cssPrinting.Href; }
        set { cssPrinting.Href = value; cssPrinting.Visible = !String.IsNullOrEmpty(value); }
    }

    private void SetLayout(LayoutMode lm)
    {
        switch (lm)
        {
            default:
            case LayoutMode.Classic:
                {
                    pnlTopForm.Visible = true;
                    SidebarPlaceholder.Visible = true;
                    pnlSideBarRight.CssClass = "sidebarRight";
                    pnlTitle.CssClass = string.Empty;
                }
                break;
            case LayoutMode.Accordion:
                {
                    pnlTopForm.Visible = true;
                    SidebarPlaceholder.Visible = false;
                    pnlSideBarRight.CssClass = "centeredBody";
                    pnlTitle.CssClass = "centeredBody";
                }
                break;
        }

        if (SuppressTopNavPrint)
            pnlTopForm.CssClass += " noprint";
    }

    private LayoutMode m_Mode = LayoutMode.Classic;
    public LayoutMode Layout
    {
        get { return m_Mode; }
        set { SetLayout(m_Mode = value); }
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

    /// <summary>
    /// Switches to/from mobile state (overriding default detection) by setting the appropriate session variables.
    /// </summary>
    /// <param name="fMobile">True for the mobile state</param>
    public void SetMobile(Boolean fMobile)
    {
        if (fMobile)
        {
            Response.Cookies[MFBConstants.keyClassic].Value = null; // let autodetect do its thing next time...
            Request.Cookies[MFBConstants.keyClassic].Value = null;
            Session[MFBConstants.keyLite] = Boolean.TrueString; // ...but keep it lite for the session
        }
        else
        {
            Response.Cookies[MFBConstants.keyClassic].Value = "yes"; // override autodetect
            Request.Cookies[MFBConstants.keyClassic].Value = "yes";
            Session[MFBConstants.keyLite] = null; // and hence there should be no need for a session variable.
        }
        mfbHeader.IsMobile = MfbFooter.IsMobile = fMobile;
    }

    public Boolean IsMobileSession()
    {
        return ((Request.IsMobileDevice() && (Request.Cookies[MFBConstants.keyClassic] == null || Request.Cookies[MFBConstants.keyClassic].Value != "yes")) ||
                (Session[MFBConstants.keyLite] != null && Session[MFBConstants.keyLite].ToString() == Boolean.TrueString));
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

        util.SetCulture(Request.UserLanguages[0]);

        SetUpSidebar();
    }

    protected void SetUpSidebar()
    {
        // set up the sidebar
        Panel p = new Panel();
        SidebarPlaceholder.Controls.Add(p);
        p.CssClass = "sidebarLeft";
        m_sidebar = (Controls_mfbLogbookSidebar)LoadControl("~/Controls/mfbLogbookSidebar.ascx");
        p.Controls.Add(Sidebar);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";

        string szRequestedLayout = util.GetStringParam(Request, "lm");
        if (!String.IsNullOrEmpty(szRequestedLayout))
        {
            LayoutMode m;
            if (Enum.TryParse<LayoutMode>(szRequestedLayout, out m))
                Layout = m;
        }

        if (!IsPostBack)
        {
            if (ShuntState.IsShunted && String.IsNullOrEmpty(Request["noshunt"]))
                Response.Redirect("~/Shunt.aspx");

            // if "m=no" is passed in, override mobile detection and force classic view
            if (util.GetStringParam(Request, "m") == "no")
                SetMobile(false);

            bool fResetCookieAccept = util.GetIntParam(Request, "declinecookie", 0) != 0;
            bool fCookiesAccepted = Request.Cookies[MFBConstants.keyCookiePrivacy] != null;
            if (fResetCookieAccept && fCookiesAccepted)
                Response.Cookies[MFBConstants.keyCookiePrivacy].Expires = DateTime.Now.AddDays(-1);
            pnlCookies.Visible = !fCookiesAccepted || fResetCookieAccept;
            lnkPrivacy.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrivacyPolicyHeader, Branding.CurrentBrand.AppName);

            lnkAppleIcon.Href = ResolveUrl("~/images/apple-touch-icon.png");
            cssMain.Href = "~/Public/stylesheet.css".ToAbsoluteURL(Request).ToString() + "?v=8";    // to enable forced reload
            cssMobile.Visible = mfbHeader.IsMobile = MfbFooter.IsMobile = IsMobileSession();
            cssMobile.Href = ResolveUrl("~/Public/CSS/MobileSheet.css?v=7");
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

            IsNaked = util.GetIntParam(Request, "naked", 0) != 0 || (Session["IsNaked"] != null && ((bool) Session["IsNaked"]) == true);
            if (IsNaked)
                pnlCookies.Visible = SidebarPlaceholder.Visible = mfbHeader.Visible = MfbFooter.Visible = SponsoredAd1.Visible = false;

            bool fIsImpersonating = ProfileRoles.IsImpersonating(Page.User.Identity.Name);
            pnlImpersonation.Visible = fIsImpersonating;
            if (fIsImpersonating)
            {
                lblAdmin.Text = ProfileRoles.OriginalAdmin;
                lblImpersonated.Text = Page.User.Identity.Name;
            }

            if (Request.IsLocal)
                GoogleAnalytics1.Visible = false;
        }

        if (IsIOSORAndroid) // set in Page INIT
        {
            metaFormat.Visible = true;
            metaViewport.Visible = !SuppressMobileViewport;
        }
    }

    protected void btnStopImpersonating_Click(object sender, EventArgs e)
    {
        ProfileRoles.StopImpersonating();
        pnlImpersonation.Visible = false;
        Response.Redirect("~/Member/Admin.aspx");
    }

    protected void btnAcceptCookies_Click(object sender, EventArgs e)
    {
        pnlCookies.Visible = false;
        Response.Cookies[MFBConstants.keyCookiePrivacy].Value = "yes";
        Response.Cookies[MFBConstants.keyCookiePrivacy].Expires = DateTime.Now.AddYears(100);
    }

    protected void lnkPrivacy_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/Public/Privacy.aspx");
    }
}
