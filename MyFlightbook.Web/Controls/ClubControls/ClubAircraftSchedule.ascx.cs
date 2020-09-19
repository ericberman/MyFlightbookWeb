using MyFlightbook.Clubs;
using MyFlightbook.Schedule;
using System;

/******************************************************
 * 
 * Copyright (c) 2015-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_ClubControls_ClubAircraftSchedule : System.Web.UI.UserControl
{
    private ClubAircraft m_ca;

    public ClubAircraft Aircraft
    {
        get { return m_ca; }
        set
        {
            m_ca = value;
            fvClubaircraft.DataSource = new ClubAircraft[1] { value };
            fvClubaircraft.DataBind();
        }
    }

    public ScheduleDisplayMode Mode { get; set; }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}