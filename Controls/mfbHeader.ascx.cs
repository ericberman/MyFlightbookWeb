using MyFlightbook;
using System;

/******************************************************
 * 
 * Copyright (c) 2009-2019 MyFlightbook LLC
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

            if (Page.User.Identity.IsAuthenticated)
            {
                lblUser.Visible = true;
                lblUser.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, lblUser.Text, MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UserFirstName);
                lblCreateAccount.Visible = false;
            }
            else
            {
                lblUser.Visible = false;
                lblCreateAccount.Visible = true;
            }
        }
    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/Member/LogbookNew.aspx?s=" + System.Web.HttpUtility.UrlEncode(mfbSearchbox.SearchText));
    }
}
