using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Globalization;
using System.IO;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using MyFlightbook;
using MyFlightbook.SocialMedia;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbFacebook : System.Web.UI.UserControl
{
    private const string PendingFacebookPostFlight = "PendingFBPostFlightID";
    private const string cookieShareFacebook = "ShareFacebook";

    protected void Page_Load(object sender, EventArgs e)
    {
    }

    /// <summary>
    /// Gets/sets the default state for whether or not to tweet by default.
    /// </summary>
    public Boolean FDefaultFacebookCheckboxState
    {
        get
        {
            return (MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanPostFacebook() && (Request.Cookies[cookieShareFacebook] != null) && !String.IsNullOrEmpty(Request.Cookies[cookieShareFacebook].Value));
        }
        set
        {
            if (value)
            {
                Response.Cookies[cookieShareFacebook].Value = "yes";
                Response.Cookies[cookieShareFacebook].Expires = DateTime.Now.AddYears(10);
            }
            else
            {
                if (Request.Cookies[cookieShareFacebook] != null)
                {
                    Request.Cookies[cookieShareFacebook].Value = "";
                    Request.Cookies[cookieShareFacebook].Expires = DateTime.Now.AddDays(-5);
                }
                if (Response.Cookies[cookieShareFacebook] != null)
                {
                    Response.Cookies[cookieShareFacebook].Value = "";
                    Response.Cookies[cookieShareFacebook].Expires = DateTime.Now.AddDays(-5);
                }
            }
        }
    }

    /// <summary>
    /// A session based pending flight to post for this user.
    /// </summary>
    public LogbookEntry PendingFlightToPost
    {
        get { return (LogbookEntry)Session[PendingFacebookPostFlight]; }
        set { Session[PendingFacebookPostFlight] = value; }
    }

    /// <summary>
    /// Posts the flight to facebook, setting up authorization as needed.
    /// </summary>
    /// <param name="le">The flight to post</param>
    /// <returns>The Facebook result, empty if not posted or error</returns>
    public void PostFlight(IPostable le)
    {
        if (le == null)
            throw new ArgumentNullException("le");

        Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
        if (pf.CanPostFacebook())
        {   
            FDefaultFacebookCheckboxState = true;
            new FacebookPoster().PostToSocialMedia(le, Page.User.Identity.Name, Request.Url.Host);
        }
        else
            MFBFacebook.NotifyFacebookNotSetUp(pf.UserName);
    }

    public void PostPendingFlight()
    {
        if (PendingFlightToPost != null)
        {
            PostFlight(PendingFlightToPost);
            PendingFlightToPost = null;
        }
    }
}
