using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook.Clubs;
using MyFlightbook.Schedule;

public partial class Controls_ClubControls_ClubAircraftSchedule : System.Web.UI.UserControl
{
    private ClubAircraft m_ca = null;

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