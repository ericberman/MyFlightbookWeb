using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook.Clubs;
using DDay.iCal;
using DDay.iCal.Serialization;
using DDay.iCal.Serialization.iCalendar;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_SchedSummary : System.Web.UI.UserControl
{
    #region properties
    public int ClubID
    {
        get { return String.IsNullOrEmpty(hdnClubID.Value) ? 0 : Convert.ToInt32(hdnClubID.Value, System.Globalization.CultureInfo.InvariantCulture); }
        set { hdnClubID.Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture); }
    }

    public string UserName { get; set; }

    public string ResourceName { get; set; }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
    }

    public void Refresh()
    {
        gvSchedSummary.DataSource = Club.ClubWithID(ClubID).GetUpcomingEvents(10, ResourceName, UserName);
        gvSchedSummary.DataBind();
    }
}