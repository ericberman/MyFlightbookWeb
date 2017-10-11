using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Xml;
using MyFlightbook;
using MyFlightbook.SocialMedia;
using VolunteerApp.Twitter;
using System.Data.Sql;
using System.Data.SqlClient;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbTwitter : System.Web.UI.UserControl
{
    private const string PendingTwitterPostFlight = "PendingTwitterPostFlightID";
    private const string cookieTweet = "Tweet";

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    /// <summary>
    /// Returns the saved oAuth access token for the user
    /// </summary>
    /// <param name="szUser">Username for which to retrieve credentials</param>
    /// <param name="oAuth">The oAuth object to hold the resulting token and secret</param>
    /// <returns>True for success</returns>
    private Boolean GetUserTwitterToken(string szUser, oAuthTwitter oAuth)
    {
        Boolean fResult = false;

        try
        {
            Profile pf = MyFlightbook.Profile.GetUser(szUser);
            oAuth.Token = pf.TwitterAccessToken;
            oAuth.TokenSecret = pf.TwitterAccessSecret;
            fResult = true;
        }
        catch (Exception ex)
        {
            throw new MyFlightbookException(ex.Message);
        }

        return fResult;
    }

    /// <summary>
    /// Saves the access token and secret for the user
    /// </summary>
    /// <param name="szUser">Username of user to save tokens</param>
    /// <param name="oAuth">oAuth object containing access token and secret</param>
    /// <returns>True for success</returns>
    public void SetUserTwitterToken(string szUser, oAuthTwitter oAuth)
    {
        if (oAuth == null)
            throw new ArgumentNullException("oAuth");
        try
        {
            Profile pf = MyFlightbook.Profile.GetUser(szUser);
            pf.TwitterAccessToken = oAuth.Token;
            pf.TwitterAccessSecret = oAuth.TokenSecret;
            pf.FCommit(); // I'm skipping validation because nothing above should cause a problem.  FCommit will validate for me too.
        }
        catch (Exception ex)
        {
            throw new MyFlightbookException(ex.Message);
        }
    }

    /// <summary>
    /// Does the user have a cached access token?
    /// </summary>
    /// <returns>True if the user has a cached access token</returns>
    public Boolean FHasTwitterToken()
    {
        return MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanTweet();
    }

    /// <summary>
    /// Gets/sets the default state for whether or not to tweet by default.
    /// </summary>
    public Boolean FDefaultTwitterCheckboxState
    {
        get
        {
            return (FHasTwitterToken() && (Request.Cookies[cookieTweet] != null) && !String.IsNullOrEmpty(Request.Cookies[cookieTweet].Value));
        }
        set
        {
            if (value)
            {
                Response.Cookies[cookieTweet].Value = "yes";
                Response.Cookies[cookieTweet].Expires = DateTime.Now.AddYears(10);
            }
            else
            {
                if (Request.Cookies[cookieTweet] != null)
                {
                    Request.Cookies[cookieTweet].Value = "";
                    Request.Cookies[cookieTweet].Expires = DateTime.Now.AddDays(-5);
                }
                if (Response.Cookies[cookieTweet] != null)
                {
                    Response.Cookies[cookieTweet].Value = "";
                    Response.Cookies[cookieTweet].Expires = DateTime.Now.AddDays(-5);
                } 
            }
        }
    }

    public Uri AuthURL
    {
        get
        {
            //Redirect the user to Twitter for authorization.
            oAuthTwitter oAuth = new oAuthTwitter();
            oAuth.CallBackUrl = TwitterConstants.CallBackPageFormat.ToAbsoluteURL(Request).ToString();
            return new Uri(oAuth.AuthorizationLinkGet());
        }
    }

    /// <summary>
    /// A session based pending flight to post for this user.
    /// </summary>
    public LogbookEntry PendingFlightToPost
    {
        get
        {
            return (LogbookEntry)Session[PendingTwitterPostFlight];
        }
        set
        {
            Session[PendingTwitterPostFlight] = value;
        }
    }

    /// <summary>
    /// Post the flight to Twitter
    /// </summary>
    /// <param name="le">The logbook entry being posted</param>
    /// <returns>true for success</returns>
    public Boolean PostFlight(LogbookEntry le)
    {
        if (le == null)
            throw new ArgumentNullException("le");
        Boolean fResult = false;

        oAuthTwitter oAuth = new oAuthTwitter();
        GetUserTwitterToken(le.User, oAuth);

        if (oAuth.Token.Length == 0 && Request["oauth_token"] == null)
        {
            PendingFlightToPost = le;  // hold on to this logbook entry for post-authorization...

            //Redirect the user to Twitter for authorization.
            oAuth.CallBackUrl = "~/member/PostFlight.aspx".ToAbsoluteURL(Request).ToString();
            SocialNetworkAuthorization.PushRedirect(oAuth.AuthorizationLinkGet());
        }
        else
        {
            if (oAuth.TokenSecret.Length == 0)
            {
                oAuth.AccessTokenGet(Request["oauth_token"], Request["oauth_verifier"]);
                SetUserTwitterToken(Page.User.Identity.Name, oAuth);
            }

            if (new TwitterPoster().PostToSocialMedia(le, Page.User.Identity.Name, Request.Url.Host))
            {
                FDefaultTwitterCheckboxState = true;
                fResult = true;
            }
        }
        return fResult;
    }
}
