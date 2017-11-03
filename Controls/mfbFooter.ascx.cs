using MyFlightbook;
using System;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2007-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbFooter : System.Web.UI.UserControl
{
    public Boolean IsMobile
    {
        get { return pnlMobile.Visible; }
        set { pnlMobile.Visible = value; pnlClassic.Visible = !value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            RSSCurrency1.Visible = Page.User.Identity.IsAuthenticated && Request.IsSecureConnection;
            lnkBlog.Visible = !String.IsNullOrEmpty(lnkBlog.NavigateUrl = Branding.CurrentBrand.BlogAddress);
            cellFacebook.Visible = !String.IsNullOrEmpty(lnkFacebook.NavigateUrl = Branding.CurrentBrand.FacebookFeed);
            cellTwitter.Visible = !String.IsNullOrEmpty(lnkTwitter.NavigateUrl = Branding.CurrentBrand.TwitterFeed);
            lblCopyright.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.CopyrightDisplay, DateTime.Now.Year);
            divSSLSeal.Visible = Request.IsSecureConnection;
        }
    }
}
