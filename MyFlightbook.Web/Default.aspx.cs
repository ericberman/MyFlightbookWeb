using System;

/******************************************************
 * 
 * Copyright (c) 2007-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_Home : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // This is now in MVC; keep this as a redirect so that https://{hostname}/logbook continues to work.
        Response.Redirect("~/mvc/pub");
    }
}
