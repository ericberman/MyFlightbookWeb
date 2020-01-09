using System;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2009-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_Totals : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // wire up the logbook to the current user
        MfbLogbook1.User = User.Identity.Name;

        this.Master.SelectedTab = tabID.lbtCurrency;

        if (!IsPostBack)
        {
            this.Master.Title = lblUserName.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName);
        }
    }

    protected void DateRangeChanged(object sender, EventArgs e)
    {
        FlightQuery fq = new FlightQuery(User.Identity.Name);
        fq.DateRange = MfbSimpleTotals1.DateRange;
        MfbLogbook1.Restriction = fq;
        MfbLogbook1.RefreshData();
    }
}
