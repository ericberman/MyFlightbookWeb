using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using VolunteerApp.Twitter;
using MyFlightbook;
using MyFlightbook.SocialMedia;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_PostFlight : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // if this is a callback that is providing the twitter access token, then update with the token.
        if (!String.IsNullOrEmpty(Request["oauth_token"]) && Page.User.Identity.IsAuthenticated)
        {
            if (String.IsNullOrEmpty(Request["oauth_verifier"]))
                throw new MyFlightbookException("oauth token passed but no oauth_verifier along with it.");

            oAuthTwitter oAuth = new oAuthTwitter();
            oAuth.AccessTokenGet(Request["oauth_token"], Request["oauth_verifier"]);
            mfbTwitter.SetUserTwitterToken(Page.User.Identity.Name, oAuth);
        }


        if (util.GetStringParam(Request, "oauth_token").Length > 0)
        {
            LogbookEntry le = mfbTwitter.PendingFlightToPost;
            if (le != null)
            {
                mfbTwitter.PostFlight(le);
                mfbTwitter.PendingFlightToPost = null; // clear it
            }
        }

        Response.Redirect(SocialNetworkAuthorization.PopRedirect(Master.IsMobileSession() ? SocialNetworkAuthorization.DefaultRedirPageMini : SocialNetworkAuthorization.DefaultRedirPage));
    }
}
