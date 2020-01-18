using System;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2009-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_FlyingTrends : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // wire up the logbook to the current user
        MfbLogbook1.User = User.Identity.Name;

        this.Master.SelectedTab = tabID.lbtTrends;

        if (!IsPostBack)
        {
            lblUserName.Text = MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName;
            Master.Title = lblUserName.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, lblUserName.Text);
            Refresh();
        }

        // reset this on every page load since charttotals doesn't persist it.
        mfbChartTotals1.SourceData = MfbLogbook1.Data;
    }

    protected void Refresh()
    {
        MfbLogbook1.Restriction = mfbSearchForm1.Restriction;
        MfbLogbook1.RefreshData();
        mfbChartTotals1.Refresh(MfbLogbook1.Data);
        mfbQueryDescriptor.DataSource = mfbSearchForm1.Restriction;
        mfbQueryDescriptor.DataBind();
        mvTrends.SetActiveView(vwChart);
    }

    protected void mfbSearchForm1_Reset(object sender, FlightQueryEventArgs e)
    {
        Refresh();
    }

    protected void mfbSearchForm1_QuerySubmitted(object sender, FlightQueryEventArgs e)
    {
        Refresh();
    }

    protected void btnChangeQuery_Click(object sender, EventArgs e)
    {
        mvTrends.SetActiveView(vwQuery);
    }

    protected void mfbQueryDescriptor_QueryUpdated(object sender, FilterItemClicked fic)
    {
        if (fic == null)
            throw new ArgumentNullException("fic");
        mfbSearchForm1.Restriction = mfbSearchForm1.Restriction.ClearRestriction(fic.FilterItem);
        Refresh();
    }
}
