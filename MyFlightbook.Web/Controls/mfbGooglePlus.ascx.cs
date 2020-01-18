using System;
using System.Globalization;
using System.Web;
using MyFlightbook;
using MyFlightbook.SocialMedia;

/******************************************************
 * 
 * Copyright (c) 2012-2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbGooglePlus : System.Web.UI.UserControl
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    public Uri AuthURL
    {
        get 
        {
            return new Uri(String.Format(CultureInfo.InvariantCulture, "https://accounts.google.com/o/oauth2/auth?response_type=code&client_id={0}&redirect_uri={1}&scope={2}", 
            GooglePlusConstants.ClientID, 
            String.Format(CultureInfo.InvariantCulture, "http://{0}{1}", HttpContext.Current.Request.Url.Host.CompareOrdinalIgnoreCase("localhost") == 0 ? "localhost" : Branding.CurrentBrand.HostName, VirtualPathUtility.ToAbsolute("~/Member/GPOAuthRedir.aspx")),
            HttpUtility.UrlEncode("https://www.googleapis.com/auth/plus.me")
            ));
        }
    }
}