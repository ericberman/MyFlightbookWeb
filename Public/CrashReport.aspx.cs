using System;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2011-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_CrashReport : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        String szErr = "OK";

        string szFile = Request.Form["filename"] ?? string.Empty;
        string szStack = Request.Form["stacktrace"] ?? string.Empty;

        util.NotifyAdminEvent("Crash report received", szStack, ProfileRoles.maskSiteAdminOnly);

        Response.Clear();
        Response.ContentType = "text/plain; charset=utf-8";
        Response.Write(szErr);
    }
}