using MyFlightbook;
using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2015-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class DefaultMini : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Keep it mobile for this session!
        this.Master.SetMobile(true);
        lblHeader.Text = User.Identity.IsAuthenticated ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageWelcomeBack, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFirstName) : String.Format(CultureInfo.CurrentCulture, Resources.Profile.WelcomeTitle, Branding.CurrentBrand.AppName);
    }
}
