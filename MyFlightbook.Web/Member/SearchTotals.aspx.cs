using MyFlightbook;
using System;

/******************************************************
 * 
 * Copyright (c) 2015-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class SearchTotals : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.SelectedTab = tabID.tabUnknown;
        Title = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName);

        if (!IsPostBack)
        {
            UpdateTotalsAndLogbook();
        }
    }


    protected void UpdateTotalsAndLogbook()
    {
        MfbLogbook1.User = User.Identity.Name;
        MfbLogbook1.Restriction = mfbSearchAndTotals1.Restriction;
        MfbLogbook1.RefreshData();
    }

    protected void UpdateGridVisibility(Boolean fShowGrid)
    {
        MfbLogbook1.Visible = lnkHideFlights.Visible = fShowGrid;
        lnkShowFlights.Visible = !fShowGrid;
    }

    protected void lnkShowFlights_Click(object sender, EventArgs e)
    {
        UpdateGridVisibility(true);
    }

    protected void lnkHideFlights_Click(object sender, EventArgs e)
    {
        UpdateGridVisibility(false);
    }

    protected void UpdateQueryResults(object sender, EventArgs e)
    {
        UpdateTotalsAndLogbook();
    }
}
