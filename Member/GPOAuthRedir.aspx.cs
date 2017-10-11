using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;
using MyFlightbook.SocialMedia;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_GPOAuthRedir : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        string szCode = util.GetStringParam(Request, "code");
        if (!String.IsNullOrEmpty(szCode))
        {
            Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
            pf.GooglePlusAccessToken = szCode;
            pf.FCommit();
        }

        Response.Redirect(SocialNetworkAuthorization.PopRedirect(Master.IsMobileSession() ? SocialNetworkAuthorization.DefaultRedirPageMini : SocialNetworkAuthorization.DefaultRedirPage));
    }
}