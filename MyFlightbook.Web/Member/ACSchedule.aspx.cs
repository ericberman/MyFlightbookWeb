using MyFlightbook;
using MyFlightbook.Clubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_ACSchedule : System.Web.UI.Page
{
    #region Properties
    /// <summary>
    /// The aircraft being scheduled
    /// </summary>
    public int AircraftID { get; set; }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            mvStatus.SetActiveView(vwNoClub);   // default

            AircraftID = util.GetIntParam(Request, "ac", Aircraft.idAircraftUnknown);

            if (AircraftID != Aircraft.idAircraftUnknown && Page.User.Identity.IsAuthenticated && !String.IsNullOrEmpty(Page.User.Identity.Name))
            {
                IEnumerable<Club> lstClubsForAircraft = null;
                IEnumerable<Club> lstClubsForUserInAircraft = Club.ClubsForAircraft(AircraftID, Page.User.Identity.Name);
                Aircraft ac = new Aircraft(AircraftID);
                lblTailNumber.Text = lblTailNumber2.Text = lblTailNumber3.Text = ac.DisplayTailnumber;
                if (lstClubsForUserInAircraft.Count() > 0)
                {
                    mvStatus.SetActiveView(vwMember);
                    rptSchedules.DataSource = lstClubsForUserInAircraft;
                    rptSchedules.DataBind();

                    // If *any* club has policy PrependsScheduleWithOwnerName, set the default text for it
                    foreach (Club c in lstClubsForUserInAircraft)
                    {
                        if (c.PrependsScheduleWithOwnerName)
                        {
                            mfbEditAppt1.DefaultTitle = MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UserFullName;
                            break;
                        }
                    }
                }
                else if ((lstClubsForAircraft = Club.ClubsForAircraft(AircraftID)).Count() > 0)   // if the aircraft belongs to a club but you don't, show those clubs
                {
                    mvStatus.SetActiveView(vwNotMember);
                    rptClubsForAircraft.DataSource = lstClubsForAircraft;
                    rptClubsForAircraft.DataBind();
                }
            }
        }
    }

    protected void rptSchedules_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e != null)
            ((Controls_SchedSummary)e.Item.FindControl("schedSummary")).Refresh();
    }
}