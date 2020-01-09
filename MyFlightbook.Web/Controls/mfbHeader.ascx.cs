using MyFlightbook;
using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2009-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbHeader : System.Web.UI.UserControl
{
    public Boolean IsMobile {get; set;}

    public tabID SelectedTab
    {
        get { return XMLNav1.SelectedItem; }
        set { XMLNav1.SelectedItem = value; }
    }

    public TabList TabList
    {
        get { return XMLNav1.TabList; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // fix up the appropriate app name
            lnkDownload.Text = Branding.ReBrand(Resources.LocalizedText.HeaderDownload);
            lnkDownloadIPhone.Text = Branding.ReBrand(Resources.LocalizedText.HeaderDownloadIOS);
            lnkDownloadAndroid.Text = Branding.ReBrand(Resources.LocalizedText.HeaderDownloadAndroid);
            lnkDownloadWindowsPhone.Text = Branding.ReBrand(Resources.LocalizedText.HeaderDownloadWP7);
            lnkLogo.ImageUrl = Branding.CurrentBrand.LogoURL;
            pnlDonate.Visible = Page.User.Identity.IsAuthenticated;
            lnkDonate.Text = Branding.ReBrand(Resources.LocalizedText.DonateSolicitation);

            if (Request != null && Request.UserAgent != null)
            {
                string s = Request.UserAgent.ToUpperInvariant();

                if (s.Contains("IPAD") || s.Contains("IPHONE"))
                    mvXSell.SetActiveView(vwIOS);

                if (s.Contains("DROID"))
                    mvXSell.SetActiveView(vwDroid);

                if (s.Contains("WINDOWS PHONE"))
                    mvXSell.SetActiveView(vwW7Phone);
            }

            mvLoginStatus.SetActiveView(Page.User.Identity.IsAuthenticated ? vwSignedIn : vwNotSignedIn);
            if (Page.User.Identity.IsAuthenticated)
            {
                Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
                lblUser.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LoginStatusWelcome, pf.UserFirstName);
                lblMemberSince.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberSinceShort, pf.CreationDate);
                lblLastLogin.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberLastLogonShort, pf.LastLogon);
                lblLastActivity.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberLastActivityShort, pf.LastActivity);
                itemLastActivity.Visible = pf.LastActivity.Date.CompareTo(pf.LastLogon.Date) != 0;
            }
        }
    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/Member/LogbookNew.aspx?s=" + System.Web.HttpUtility.UrlEncode(mfbSearchbox.SearchText));
    }
}
