using MyFlightbook.CloudStorage;
using System;

/******************************************************
 * 
 * Copyright (c) 2016-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_OneDriveRedir : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Session[OneDrive.TokenSessionKey] = new OneDrive().ConvertToken(Request);
        Response.Redirect("~/Member/EditProfile.aspx/pftPrefs?1dOAuth=1");
    }
}