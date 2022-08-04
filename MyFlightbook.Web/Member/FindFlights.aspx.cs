using MyFlightbook;
using System;

/******************************************************
 * 
 * Copyright (c) 2009-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_FindFlights : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // wire up the logbook to the current user
        MfbLogbook1.User = User.Identity.Name;

        this.Master.SelectedTab = tabID.lbtFindFlights;

        if (!IsPostBack)
        {
            Master.Title = lblUserName.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName);

            Boolean fDoSearch = false;
            string szSearch = util.GetStringParam(Request, "s");
            if (szSearch.Length > 0)
                fDoSearch = true;

            string szFQParam = util.GetStringParam(Request, "fq");
            if (!String.IsNullOrEmpty(szFQParam))
            {
                fDoSearch = true;
                mfbSearchAndTotals1.Restriction = FlightQuery.FromBase64CompressedJSON(szFQParam);
            }

            string szAirport = util.GetStringParam(Request, "ap");
            if (szAirport.Length > 0)
            {
                fDoSearch = true;
                mfbSearchAndTotals1.AirportQuery = szAirport;
            }

            int month = util.GetIntParam(Request, "m", -1);
            int year = util.GetIntParam(Request, "y", -1);

            if (month >= 0 && month < 12 && year > 0)
            {
                fDoSearch = true;
                DateTime dtStart = new DateTime(year, month + 1, 1);
                DateTime dtEnd = dtStart.AddMonths(1).AddDays(-1);
                mfbSearchAndTotals1.Restriction.DateRange = FlightQuery.DateRanges.Custom;
                mfbSearchAndTotals1.Restriction.DateMin = dtStart;
                mfbSearchAndTotals1.Restriction.DateMax = dtEnd;
            }

            if (fDoSearch)
                SearchResults(szSearch);
        }

        if (Request.IsMobileSession())
            MfbLogbook1.MiniMode = true;
    }

    protected void FilterResults(object sender, EventArgs e)
    {
        MfbLogbook1.Restriction = mfbSearchAndTotals1.Restriction;
        MfbLogbook1.RefreshData();
    }

    protected void SearchResults(string szRestrict)
    {
        if (szRestrict == null)
            throw new ArgumentNullException(nameof(szRestrict));
        if (szRestrict.Length > 0)
        {
            mfbSearchAndTotals1.SimpleQuery = szRestrict;
        }
        MfbLogbook1.Restriction = mfbSearchAndTotals1.Restriction;

        MfbLogbook1.RefreshData();
    }

    protected void setViewType(Boolean fPrintView)
    {
        MfbLogbook1.Visible = Master.HasFooter = Master.HasHeader = lnkPrinterView.Visible = !fPrintView;
        lnkRegularView.Visible = fPrintView;
    }

    protected void lnkPrinterView_Click(object sender, EventArgs e)
    {
        setViewType(true);
    }

    protected void lnkRegularView_Click(object sender, EventArgs e)
    {
        setViewType(false);
    }
}
