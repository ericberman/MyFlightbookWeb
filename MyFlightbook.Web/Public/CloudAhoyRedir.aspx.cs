using MyFlightbook.OAuth.CloudAhoy;
using MyFlightbook;
using System;

/******************************************************
 * 
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_CloudAhoyRedir : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Page.User.Identity.IsAuthenticated)
        {
            Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
            pf.CloudAhoyToken = new CloudAhoyClient(!Branding.CurrentBrand.MatchesHost(Request.Url.Host)).ConvertToken(Request);
            pf.FCommit();
        }
        Response.Redirect("~/Member/EditProfile.aspx/pftPrefs?pane=cloudahoy");
    }
}