using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Member_SignFlight : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.tabUnknown;

        if (!IsPostBack)
        {
            string szError = String.Empty;
    
            int idFlight = util.GetIntParam(Request, "idFlight", LogbookEntry.idFlightNew);
            hdnFlightID.Value = idFlight.ToString(CultureInfo.InvariantCulture);

            // Remember the return URL, but only if it is relative (for security)
            string szReturnURL = util.GetStringParam(Request, "Ret");
            if (Uri.IsWellFormedUriString(szReturnURL, UriKind.Relative))
                hdnReturnURL.Value = szReturnURL;

            if (idFlight == LogbookEntry.idFlightNew)
                Response.Redirect(hdnReturnURL.Value);

            LogbookEntry le = new LogbookEntry();
            le.FLoadFromDB(idFlight, string.Empty, LogbookEntry.LoadTelemetryOption.None, true);

            lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.SignOff.SignFlightHeader, MyFlightbook.Profile.GetUser(le.User).UserFullName);

            if (!le.CanSignThisFlight(Page.User.Identity.Name, out szError))
                lblError.Text = szError;
            else
            {
                pnlSign.Visible = true;
                mfbSignFlight1.Flight = le;

                mfbSignFlight1.ShowCancel = hdnReturnURL.Value.Length > 0;
            }
        }

        mfbSignFlight1.CFIProfile = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
    }

    protected void GoBack(object sender, EventArgs e)
    {
        Response.Redirect(hdnReturnURL.Value.Length > 0 ? hdnReturnURL.Value : "~/Default.aspx");
    }
}