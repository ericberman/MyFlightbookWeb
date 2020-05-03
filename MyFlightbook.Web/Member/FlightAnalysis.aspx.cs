using MyFlightbook;
using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2009-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_FlightAnalysis : System.Web.UI.Page
{ 
    protected void Page_Load(object sender, EventArgs e)
    {
        int idFlight = util.GetIntParam(Request, "id", LogbookEntry.idFlightNone);
        if (idFlight == LogbookEntry.idFlightNone)
            throw new MyFlightbookException("No valid ID passed");

        Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/FlightDetail.aspx/{0}", idFlight));
    }
}
