using MyFlightbook;
using System;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2007-2017 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
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
        RSSCurrency1.Visible = Page.User.Identity.IsAuthenticated && Request.IsSecureConnection;
        if (!IsPostBack)
        {
            cellFacebook.Visible = (lnkFacebook.NavigateUrl = Branding.CurrentBrand.FacebookFeed).Length > 0;
            cellTwitter.Visible = (lnkTwitter.NavigateUrl = Branding.CurrentBrand.TwitterFeed).Length > 0;
            lblCopyright.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.CopyrightDisplay, DateTime.Now.Year);
            divSSLSeal.Visible = Request.IsSecureConnection;
        }
    }
}
