using System;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2016-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_GoogleAnalytics : System.Web.UI.UserControl
{
    protected string AnalyticsID
    {
        get { return LocalConfig.SettingForKey(Request.IsLocal ? "GoogleAnalyticsGA4Developer" : "GoogleAnalyticsGA4Production");  }
    }
    protected void Page_Load(object sender, EventArgs e)
    {

    }
}