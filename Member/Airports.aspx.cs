using MyFlightbook;
using MyFlightbook.Airports;
using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2011-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_Airports : System.Web.UI.Page
{
    private VisitedAirport[] m_rgVisitedAirport = null;

    private const string szKeyVAState = "VisitedAirports";

    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.SelectedTab = tabID.mptVisited;
        Title = (string)GetLocalResourceObject("PageResource1.Title");

        if (!IsPostBack)
        {
            string szQuery = util.GetStringParam(Request, "fq");
            if (!String.IsNullOrEmpty(szQuery))
            {
                FlightQuery fq = FlightQuery.FromBase64CompressedJSON(szQuery);
                if (fq.UserName.CompareCurrentCultureIgnoreCase(User.Identity.Name) == 0)
                {
                    mfbSearchForm1.Restriction = fq;
                    mfbSearchForm1.Restriction.Refresh();
                    UpdateDescription();
                }
            }
        }

        RefreshData(!IsPostBack);
    }

    protected void RefreshData(bool fForceRefresh)
    {
        mfbSearchForm1.Restriction.UserName = User.Identity.Name;
        mfbSearchForm1.Restriction.Refresh();

        if (fForceRefresh || CurrentVisitedAirports == null)
            CurrentVisitedAirports = VisitedAirport.VisitedAirportsForQuery(mfbSearchForm1.Restriction);

        gvAirports.DataSource = CurrentVisitedAirports;
        gvAirports.DataBind();
        mfbGoogleMapManager1.Visible = CurrentVisitedAirports.Length > 0;   //  Avoid excessive map loads.

        AirportList alMatches = new AirportList(CurrentVisitedAirports);

        // get an airport list of the airports
        mfbGoogleMapManager1.Map.SetAirportList(alMatches);

        bool fIncludeRoutes = util.GetIntParam(Request, "path", 0) != 0;

        if (mfbGoogleMapManager1.Map.ShowRoute = fIncludeRoutes)
        {
            List<AirportList> lst = new List<AirportList>();

            DBHelper dbh = new DBHelper(LogbookEntry.QueryCommand(mfbSearchForm1.Restriction, lto: LogbookEntry.LoadTelemetryOption.None));
            dbh.ReadRows((comm) => { }, (dr) =>
            {
                object o = dr["Route"];
                string szRoute = (string)(o == System.DBNull.Value ? string.Empty : o);

                if (!String.IsNullOrEmpty(szRoute))
                    lst.Add(alMatches.CloneSubset(szRoute));
            });
            mfbGoogleMapManager1.Map.Airports = lst;
        }

        lnkZoomOut.NavigateUrl = mfbGoogleMapManager1.ZoomToFitScript;
        lnkZoomOut.Visible = (CurrentVisitedAirports.Length > 0);

        lblNumAirports.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.VisitedAirportsNumAirports, CurrentVisitedAirports.Length);
    }

    private VisitedAirport[] CurrentVisitedAirports
    {
        get
        {
            if (m_rgVisitedAirport == null)
                m_rgVisitedAirport = (VisitedAirport[])ViewState[szKeyVAState];
            return m_rgVisitedAirport;
        }
        set
        {
            ViewState[szKeyVAState] = m_rgVisitedAirport = value;
        }
    }

    protected int LastSortDirection
    {
        get { return Convert.ToInt32(hdnLastSortDirection.Value, CultureInfo.InvariantCulture); }
        set { hdnLastSortDirection.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    protected string LastSortExpression
    {
        get { return hdnLastSortExpression.Value; }
        set { hdnLastSortExpression.Value = value; }
    }

    protected void gvAirports_DataBound(Object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            PlaceHolder p = (PlaceHolder) e.Row.FindControl("plcZoomCode");
            HtmlAnchor a = new HtmlAnchor();
            p.Controls.Add(a);
            VisitedAirport va = (VisitedAirport)e.Row.DataItem;

            string szLink = String.Format(CultureInfo.InvariantCulture, "javascript:{0}.gmap.setCenter(new google.maps.LatLng({1}, {2}));{0}.gmap.setZoom(14);",
                mfbGoogleMapManager1.MapID, va.Airport.LatLong.Latitude, va.Airport.LatLong.Longitude);
            a.InnerText = va.Code;
            a.HRef = szLink;
        }
    }

    protected void gvAirports_Sorting(Object sender, GridViewSortEventArgs e)
    {
        if (e.SortExpression.CompareTo(LastSortExpression) != 0)
        {
            LastSortDirection = 1;
            LastSortExpression = e.SortExpression;
        }
        else if (LastSortDirection != 1)
            LastSortDirection = 1;
        else
            LastSortDirection = -1;

        int Direction = LastSortDirection;

        switch (e.SortExpression.ToUpper())
        {
            case "CODE":
                Array.Sort(CurrentVisitedAirports, delegate(VisitedAirport va1, VisitedAirport va2) { return Direction * va1.Code.CompareTo(va2.Code); });
                break;
            case "FACILITYNAME":
                Array.Sort(CurrentVisitedAirports, delegate(VisitedAirport va1, VisitedAirport va2) { return Direction * va1.FacilityName.CompareTo(va2.FacilityName); });
                break;
            case "NUMBEROFVISITS":
                Array.Sort(CurrentVisitedAirports, delegate(VisitedAirport va1, VisitedAirport va2) { return Direction * va1.NumberOfVisits.CompareTo(va2.NumberOfVisits); });
                break;
            case "EARLIESTVISITDATE":
                Array.Sort(CurrentVisitedAirports, delegate(VisitedAirport va1, VisitedAirport va2) { return Direction * va1.EarliestVisitDate.CompareTo(va2.EarliestVisitDate); });
                break;
            case "LATESTVISITDATE":
                Array.Sort(CurrentVisitedAirports, delegate(VisitedAirport va1, VisitedAirport va2) { return Direction * va1.LatestVisitDate.CompareTo(va2.LatestVisitDate); });
                break;
        }
        gvAirports.DataSource = CurrentVisitedAirports;
        gvAirports.DataBind();
    }

    protected void gvAirports_Sorted(Object sender, EventArgs e)
    {
    }

    protected void btnEstimateDistance_Click(object sender, EventArgs e)
    {
        string szErr = lblErr.Text = string.Empty;
        double distance = VisitedAirport.DistanceFlownByUser(mfbSearchForm1.Restriction, out szErr);

        if (String.IsNullOrEmpty(szErr))
        {
            btnEstimateDistance.Visible = false;
            pnlDistanceResults.Visible = true;
            lblDistanceEstimate.Text = String.Format(CultureInfo.CurrentCulture,Resources.LocalizedText.VisitedAirportsDistanceEstimate, distance);
        }
        else
            lblErr.Text = szErr;
    }

    protected void btnGetTotalKML(object sender, EventArgs e)
    {
        string szErr = string.Empty;
        DataSourceType dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.KML);
        Response.Clear();
        Response.ContentType = dst.Mimetype;
        Response.AddHeader("Content-Disposition", String.Format(CultureInfo.CurrentCulture, "attachment;filename={0}-AllFlights.{1}", Branding.CurrentBrand.AppName, dst.DefaultExtension));
        VisitedAirport.AllFlightsAsKML(mfbSearchForm1.Restriction, Response.OutputStream, out szErr);
        Response.End();
    }

    protected void btnChangeQuery_Click(object sender, EventArgs e)
    {
        mvVisitedAirports.SetActiveView(vwSearch);
    }

    public void ClearForm(object sender, FlightQueryEventArgs e)
    {
        ShowResults(sender, e);
    }

    protected void ShowResults(object sender, FlightQueryEventArgs e)
    {
        mvVisitedAirports.SetActiveView(vwVisitedAirports);
        UpdateDescription();
        RefreshData(true);
    }

    protected void UpdateDescription()
    {
        mfbQueryDescriptor1.DataSource = mfbSearchForm1.Restriction;
        mfbQueryDescriptor1.DataBind();
    }

    protected void mfbQueryDescriptor1_QueryUpdated(object sender, FilterItemClicked fic)
    {
        if (fic == null)
            throw new ArgumentNullException("fic");
        mfbSearchForm1.Restriction = mfbSearchForm1.Restriction.ClearRestriction(fic.FilterItem);
        ShowResults(sender, new FlightQueryEventArgs(mfbSearchForm1.Restriction));
    }

    protected void lnkDownloadAirports_Click(object sender, EventArgs e)
    {
        Response.Clear();
        Response.ContentType = "text/csv";
        // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
        string szFilename = String.Format(CultureInfo.InvariantCulture, "Airports-{0}-{1}-{2}", Branding.CurrentBrand.AppName, MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
        string szDisposition = String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}.csv", System.Text.RegularExpressions.Regex.Replace(szFilename, "[^0-9a-zA-Z-]", ""));
        Response.AddHeader("Content-Disposition", szDisposition);
        Response.Write('\uFEFF');   // UTF-8 BOM.
        gvAirportsDownload.DataSource = CurrentVisitedAirports;
        gvAirportsDownload.DataBind();
        Response.Write(gvAirportsDownload.CSVFromData());
        Response.End();
    }
}