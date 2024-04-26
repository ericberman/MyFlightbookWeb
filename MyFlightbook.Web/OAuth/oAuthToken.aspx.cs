using OAuthAuthorizationServer.Services;
using System;

/******************************************************
 * 
 * Copyright (c) 2015-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class OAuth_oAuthToken : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        OAuthServiceCall.ProcessRequest();
    }
}