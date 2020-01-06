using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_Game : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        string szRedirURL = "~/public/Game.aspx";
        if (!String.IsNullOrEmpty(Request.QueryString["Url"]))
            szRedirURL = Request.QueryString["Url"];
        Response.Redirect(szRedirURL + (szRedirURL.Contains("?") ? "&" : "?") + "SkipIntro=Yes");
    }
}
