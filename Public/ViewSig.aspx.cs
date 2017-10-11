using MyFlightbook;
using System;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_ViewSig : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        int idFlight = util.GetIntParam(Request, "id", LogbookEntry.idFlightNew);
        if (idFlight != LogbookEntry.idFlightNew)
        {
            LogbookEntry le = new LogbookEntry();
            le.FlightID = idFlight;
            le.LoadDigitalSig();

            if (le.DigitizedSignature != null && le.DigitizedSignature.Length > 0)
            {
                Response.ContentType = "image/png";
                Response.Clear();
                Response.BinaryWrite(le.DigitizedSignature);
                Response.End();
            }
        }
    }
}