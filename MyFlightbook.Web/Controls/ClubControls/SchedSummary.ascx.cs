using MyFlightbook.Clubs;
using MyFlightbook.Schedule;
using System;
using System.Globalization;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2020 MyFlightbook LLC
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

    protected void gvSchedSummary_RowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        if (e.Row.RowType == System.Web.UI.WebControls.DataControlRowType.DataRow)
        {
            ScheduledEvent se = (ScheduledEvent) e.Row.DataItem;
            Controls_popmenu pop = (Controls_popmenu) e.Row.FindControl("popmenu");
            ((HyperLink)pop.FindControl("lnkDownloadICS")).NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/IcalAppt.aspx?c={0}&sid={1}", ClubID, se.ID);
            ((HyperLink)pop.FindControl("lnkDownloadYahoo")).NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/IcalAppt.aspx?c={0}&sid={1}&fmt=Y", ClubID, se.ID);
            ((HyperLink)pop.FindControl("lnkDownloadGoogle")).NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/IcalAppt.aspx?c={0}&sid={1}&fmt=G", ClubID, se.ID);
        }
    }
}