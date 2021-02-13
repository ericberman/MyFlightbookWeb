using MyFlightbook;
using System;
using System.Globalization;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class DefaultMini : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Keep it mobile for this session!
        this.Master.SetMobile(true);
        lblHeader.Text = User.Identity.IsAuthenticated ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageWelcomeBack, HttpUtility.HtmlEncode(Profile.GetUser(User.Identity.Name).PreferredGreeting)) : String.Format(CultureInfo.CurrentCulture, Resources.Profile.WelcomeTitle, Branding.CurrentBrand.AppName);
    }
}
