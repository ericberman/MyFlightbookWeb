using MyFlightbook;
using MyFlightbook.Airports;
using MyFlightbook.Charting;
using MyFlightbook.Geography;
using MyFlightbook.Image;
using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2017-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/// <summary>
/// Base class to reduce class coupling in Member_FlightDetail
/// </summary>
public partial class Member_FlightDetailBase : Page
{
    protected enum DownloadFormat { Original, CSV, KML, GPX };

    private enum DetailsTab { Flight, Aircraft, Chart, Data, Download }

    #region some properties that don't rely on page controls
    protected FlightData DataForFlight { get; set; }

    private const string szKeyVSRestriction = "viewstateRestrictionKey";
    protected FlightQuery Restriction
    {
        get { return (FlightQuery)ViewState[szKeyVSRestriction]; }
        set { ViewState[szKeyVSRestriction] = value; }
    }

    protected string LinkForFlight(int idFlight)
    {
        return String.Format(CultureInfo.InvariantCulture, "~/Member/FlightDetail.aspx/{0}{1}", idFlight, Restriction == null || Restriction.IsDefault ? string.Empty : "?fq=" + HttpUtility.UrlEncode(Restriction.ToBase64CompressedJSONString()));
    }
    #endregion

    #region Mapping
    private const string szkeyVSAirportListResult = "szkeyVSALR";
    protected ListsFromRoutesResults RoutesList(string szRoute)
    {
        if (szRoute == null)
            throw new ArgumentNullException(nameof(szRoute));

        ListsFromRoutesResults lfrr = (ListsFromRoutesResults)ViewState[szkeyVSAirportListResult];
        if (lfrr == null)
            ViewState[szkeyVSAirportListResult] = lfrr = AirportList.ListsFromRoutes(szRoute);
        return lfrr;
    }

    /// <summary>
    /// Sets up the specified map manager
    /// </summary>
    /// <param name="mapMgr"></param>
    /// <param name="szRoute"></param>
    /// <param name="PathLatLongArrayID"></param>
    /// <returns>True if the data has latlong info and more than one row of data (i.e., is dynamic and can be downloaded</returns>
    protected bool SetUpMapManager(Controls_mfbGoogleMapMgr mapMgr, string szRoute, string PathLatLongArrayID)
    {
        if (mapMgr == null)
            throw new ArgumentNullException(nameof(mapMgr));
        if (szRoute == null)
            throw new ArgumentNullException(szRoute);

        mapMgr.Map.Airports = RoutesList(szRoute).Result;
        mapMgr.ShowMarkers = true;
        mapMgr.Map.PathVarName = PathLatLongArrayID;
        mapMgr.Map.Path = DataForFlight.GetPath();
        bool result = DataForFlight.HasLatLongInfo && DataForFlight.Data.Rows.Count > 1;
        mapMgr.Mode = result ? MyFlightbook.Mapping.GMap_Mode.Dynamic : MyFlightbook.Mapping.GMap_Mode.Static;
        return result;
    }
    #endregion

    #region Data types
    protected static string FormatNameForTelemetry(LogbookEntryCore led)
    {
        if (led == null)
            throw new ArgumentNullException(nameof(led));

        return DataSourceType.DataSourceTypeFromFileType(led.Telemetry.TelemetryType).DisplayName;
    }

    protected static bool CanSpecifyUnitsForTelemetry(LogbookEntryCore led)
    {
        if (led == null)
            throw new ArgumentNullException(nameof(led));
        switch (led.Telemetry.TelemetryType)
        {
            case DataSourceType.FileType.GPX:
            case DataSourceType.FileType.KML:
            case DataSourceType.FileType.NMEA:
            case DataSourceType.FileType.IGC:
                return false;
            default:
                return true;
        }
    }
    #endregion

    #region databinding various stuff in the form
    protected static void BindImages(Control c, LogbookEntry le, Controls_mfbGoogleMapMgr mapMgr)
    {
        if (!(c is Controls_mfbImageList mfbilFlight))
            throw new ArgumentNullException(nameof(c));
        if (le == null)
            throw new ArgumentNullException(nameof(le));
        if (mapMgr == null)
            throw new ArgumentNullException(nameof(mapMgr));

        mfbilFlight.Key = le.FlightID.ToString(CultureInfo.InvariantCulture);
        mfbilFlight.Refresh();
        mapMgr.Map.Images = mfbilFlight.Images.ImageArray;
    }

    protected static void BindVideoEntries(Control c, LogbookEntry le)
    {
        if (!(c is Controls_mfbVideoEntry ve))
            throw new ArgumentNullException(nameof(c));
        if (le == null)
            throw new ArgumentNullException(nameof(le));

        ve.Videos.Clear();
        foreach (VideoRef vr in le.Videos)
            ve.Videos.Add(vr);
    }

    protected void BindAirportServices(Control c, Controls_mfbGoogleMapMgr mapMgr, string szRoute)
    {
        if (!(c is Controls_mfbAirportServices aptsvc))
            throw new ArgumentNullException(nameof(c));
        if (mapMgr == null)
            throw new ArgumentNullException(nameof(mapMgr));
        if (szRoute == null)
            throw new ArgumentNullException(nameof(szRoute));

        aptsvc.GoogleMapID = mapMgr.MapID;
        aptsvc.AddZoomLink = (mapMgr.Mode == MyFlightbook.Mapping.GMap_Mode.Dynamic);
        aptsvc.SetAirports(RoutesList(szRoute).MasterList.GetNormalizedAirports());
    }

    protected static void BindSignature(Control c, LogbookEntryDisplay le)
    {
        if (!(c is Controls_mfbSignature sig))
            throw new ArgumentNullException(nameof(c));
        sig.Flight = le ?? throw new ArgumentNullException(nameof(le));
    }

    protected static void BindBadges(Profile pf, int idFlight, Control c)
    {
        if (pf == null)
            throw new ArgumentNullException(nameof(pf));
        if (!(c is Repeater rptBadges))
            throw new ArgumentNullException(nameof(c));

        IEnumerable<MyFlightbook.Achievements.Badge> cached = pf.CachedBadges;
        List<MyFlightbook.Achievements.Badge> badges = (cached == null ? null : new List<MyFlightbook.Achievements.Badge>(cached));
        if (badges != null)
        {
            rptBadges.DataSource = MyFlightbook.Achievements.BadgeSet.BadgeSetsFromBadges(badges.FindAll(b => b.IDFlightEarned == idFlight));
            rptBadges.DataBind();
        }
    }
    #endregion

    #region RawData Row Binding
    protected static void BindRawDataRow(FlightData fd, int iRow, Control cPin, Control cZoom)
    {
        if (fd == null)
            throw new ArgumentNullException(nameof(fd));
        if (!(cPin is Image i))
            throw new ArgumentNullException(nameof(cPin));
        if (!(cZoom is HyperLink h))
            throw new ArgumentNullException(nameof(cZoom));

        if (fd.HasLatLongInfo)
        {
            DataRow drow = fd.Data.Rows[iRow];

            List<string> lstDesc = new List<string>();

            double lat = 0.0, lon = 0.0;

            foreach (DataColumn dc in fd.Data.Columns)
            {
                bool fLat = (dc.ColumnName.CompareOrdinalIgnoreCase(KnownColumnNames.LAT) == 0);
                bool fLon = (dc.ColumnName.CompareOrdinalIgnoreCase(KnownColumnNames.LON) == 0);
                bool fPos = (dc.ColumnName.CompareOrdinalIgnoreCase(KnownColumnNames.POS) == 0);

                if (fLat)
                    lat = Convert.ToDouble(drow[KnownColumnNames.LAT], CultureInfo.InvariantCulture);

                if (fLon)
                    lon = Convert.ToDouble(drow[KnownColumnNames.LON], CultureInfo.InvariantCulture);

                if (fPos)
                {
                    LatLong ll = (LatLong)drow[KnownColumnNames.POS];
                    lat = ll.Latitude;
                    lon = ll.Longitude;
                }

                if (!(fLat || fLon || fPos))
                {
                    object o = drow[dc.ColumnName];
                    if (o != null)
                    {
                        string sz = o.ToString(); ;
                        if (!String.IsNullOrEmpty(sz))
                            lstDesc.Add(String.Format(CultureInfo.InvariantCulture, "{0}: {1}<br />", dc.ColumnName, sz));
                    }
                }
            }

            i.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:dropPin(new google.maps.LatLng({0}, {1}), '{2}');", lat, lon, String.Join("<br />", lstDesc));
            h.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:getMfbMap().gmap.setCenter(new google.maps.LatLng({0}, {1}));getMfbMap().gmap.setZoom(14);", lat, lon);
            i.ToolTip = String.Format(CultureInfo.CurrentCulture, Resources.FlightData.GraphDropPinTip, (new LatLong(lat, lon)).ToString());
        }
        else
            i.Visible = h.Visible = false;
    }
    #endregion

    #region Download
    protected void DownloadData(LogbookEntry le, FlightData m_fd, int altUnits, int speedUnits, int format)
    {
        if (m_fd == null)
            throw new ArgumentNullException(nameof(m_fd));
        if (le == null)
            throw new ArgumentNullException(nameof(le));

        m_fd.AltitudeUnits = (FlightData.AltitudeUnitTypes)altUnits;
        m_fd.SpeedUnits = (FlightData.SpeedUnitTypes)speedUnits;
        m_fd.FlightID = le.FlightID;

        DataSourceType dst = null;
        Action writeData = null;

        switch ((DownloadFormat)format)
        {
            case DownloadFormat.Original:
                string szData = le.FlightData;
                dst = DataSourceType.BestGuessTypeFromText(szData);
                writeData = () => { Response.Write(szData); };
                break;
            case DownloadFormat.CSV:
                dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.CSV);
                writeData = () => { MyFlightbook.CSV.CsvWriter.WriteToStream(Response.Output, m_fd.Data, true, true); };
                break;
            case DownloadFormat.KML:
                dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.KML);
                writeData = () => { m_fd.WriteKMLData(Response.OutputStream); };
                break;
            case DownloadFormat.GPX:
                dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.GPX);
                writeData = () => { m_fd.WriteGPXData(Response.OutputStream); };
                break;
        }

        if (dst != null && writeData != null)
        {
            Response.Clear();
            Response.ContentType = dst.Mimetype;
            Response.AddHeader("Content-Disposition", String.Format(CultureInfo.CurrentCulture, "attachment;filename=FlightData{0}.{1}", m_fd.FlightID, dst.DefaultExtension));
            writeData();
            Response.End();
        }
    }
    #endregion

    #region CloudAhoy
    protected static async System.Threading.Tasks.Task<bool> PushToCloudahoy(string szUser, LogbookEntry le, FlightData fd, int altUnits, int speedUnits, bool fSandbox)
    {
        if (fd == null)
            throw new ArgumentNullException(nameof(fd));

        fd.AltitudeUnits = (FlightData.AltitudeUnitTypes)altUnits;
        fd.SpeedUnits = (FlightData.SpeedUnitTypes)speedUnits;

        return await MyFlightbook.OAuth.CloudAhoy.CloudAhoyClient.PushCloudAhoyFlight(szUser, le, fd, fSandbox).ConfigureAwait(false);
    }
    #endregion

    #region Charting
    protected static void UpdateChart(TelemetryDataTable tdt, Controls_GoogleChart gcData, FlightData fd, string PathLatLongArrayID, ListItem xAxis, ListItem yAxis1, ListItem yAxis2, out decimal max, out decimal min, out decimal max2, out decimal min2)
    {
        max = decimal.MinValue;
        min = decimal.MaxValue;
        max2 = decimal.MinValue;
        min2 = decimal.MaxValue;

        if (tdt == null)
            return;

        if (gcData == null)
            throw new ArgumentNullException(nameof(gcData));

        if (yAxis1 == null || yAxis2 == null || xAxis == null)
        {
            gcData.Visible = false;
            return;
        }

        gcData.XLabel = xAxis.Text;
        gcData.YLabel = yAxis1.Text;
        gcData.Y2Label = yAxis2.Text;

        gcData.Clear();

        gcData.XDataType = GoogleChart.GoogleTypeFromKnownColumnType(KnownColumn.GetKnownColumn(xAxis.Value).Type);
        gcData.YDataType = GoogleChart.GoogleTypeFromKnownColumnType(KnownColumn.GetKnownColumn(yAxis1.Value).Type);
        gcData.Y2DataType = GoogleChart.GoogleTypeFromKnownColumnType(KnownColumn.GetKnownColumn(yAxis2.Value).Type);

        if (fd == null)
            throw new ArgumentNullException(nameof(fd));

        if (fd.HasLatLongInfo)
            gcData.ClickHandlerJS = String.Format(CultureInfo.InvariantCulture, "dropPin({0}[selectedItem.row], xvalue + ': ' + ((selectedItem.column == 1) ? '{1}' : '{2}') + ' = ' + value);", PathLatLongArrayID, yAxis1, yAxis2);

        foreach (DataRow dr in tdt.Rows)
        {
            gcData.XVals.Add(dr[xAxis.Value]);

            if (!String.IsNullOrEmpty(yAxis1.Value))
            {
                object o = dr[yAxis1.Value];
                gcData.YVals.Add(o);
                if (gcData.YDataType == GoogleColumnDataType.number)
                {
                    decimal d = Convert.ToDecimal(o, CultureInfo.InvariantCulture);
                    max = Math.Max(max, d);
                    min = Math.Min(min, d);
                }
            }
            if (yAxis2.Value.Length > 0 && yAxis2 != yAxis1)
            {
                object o = dr[yAxis2.Value];
                gcData.Y2Vals.Add(o);
                if (gcData.Y2DataType == GoogleColumnDataType.number)
                {
                    decimal d = Convert.ToDecimal(o, CultureInfo.InvariantCulture);
                    max2 = Math.Max(max2, d);
                    min2 = Math.Min(min2, d);
                }
            }
        }
        gcData.TickSpacing = 1; // Math.Max(1, m_fd.Data.Rows.Count / 20);
    }
    #endregion

    protected static void BindAircraftImages(Control c)
    {
        if (!(c is Controls_mfbHoverImageList hil))
            throw new ArgumentNullException(nameof(c));
        hil.Refresh();
    }

    protected static void BindMetars(Control c, string szRoute)
    {
        if (szRoute == null)
            throw new ArgumentNullException(nameof(szRoute));
        if (!(c is Controls_METAR m))
            throw new ArgumentNullException(nameof(c));
        m.RefreshForRoute(szRoute);
    }

    /// <summary>
    /// Checks to see if the viewer is an instructor of the owner of the flight; throws an exception if not.
    /// </summary>
    /// <param name="szFlightOwner">The owner of the flight</param>
    protected static void CheckCanViewFlight(string szFlightOwner, string szViewer)
    {
        if (szFlightOwner == null)
            throw new ArgumentNullException(nameof(szFlightOwner));
        if (szViewer == null)
            throw new ArgumentNullException(nameof(szViewer));

        MyFlightbook.Instruction.CFIStudentMap sm = new MyFlightbook.Instruction.CFIStudentMap(szViewer);
        InstructorStudent student = MyFlightbook.Instruction.CFIStudentMap.GetInstructorStudent(sm.Students, szFlightOwner);
        if (student == null || !student.CanViewLogbook)
            throw new MyFlightbookException(Resources.SignOff.ViewStudentLogbookUnauthorized);
    }

    #region Page Initialization
    protected int GetRequestedTabIndex()
    {
        return (Enum.TryParse<DetailsTab>(util.GetStringParam(Request, "tabID"), out DetailsTab dtRequested)) ? (int)dtRequested : DefaultTabIndex;
    }

    protected static bool TabIndexRequiresFlightData(int iTab)
    {
        DetailsTab dt = (DetailsTab)iTab;
        return dt != DetailsTab.Flight && dt != DetailsTab.Aircraft;
    }

    protected static int DefaultTabIndex { get { return (int)DetailsTab.Flight; } }

    protected void InitPassedRestriction()
    {
        string szFQParam = util.GetStringParam(Request, "fq");
        if (!String.IsNullOrEmpty(szFQParam))
        {
            try
            {
                Restriction = FlightQuery.FromBase64CompressedJSON(szFQParam);
                Restriction.Refresh();
            }
            catch (Exception ex) when (ex is ArgumentNullException || ex is FormatException || ex is Newtonsoft.Json.JsonSerializationException || ex is Newtonsoft.Json.JsonException) { }
        }
    }

    protected int InitRequestedFlightID()
    {
        return (Request.PathInfo.Length > 0 && Int32.TryParse(Request.PathInfo.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int idflight)) ? idflight : LogbookEntryCore.idFlightNone;
    }
    #endregion

}

public partial class Member_FlightDetail : Member_FlightDetailBase
{
    const string PathLatLongArrayID = "rgll";

    #region properties
    private Profile m_pfUser = null;

    protected Profile Viewer
    {
        get { return m_pfUser ?? (m_pfUser = MyFlightbook.Profile.GetUser(Page.User.Identity.Name)); }
    }

    protected string EditPath(int idFlight)
    {
        return String.Format(CultureInfo.InvariantCulture, "{0}/{1}", TargetPage, idFlight);
    }

    /// <summary>
    /// The URL to which edited flights should go
    /// </summary>
    public string TargetPage { get; set; } = "~/Member/LogbookNew.aspx";

    private int CurrentFlightID
    {
        get { return String.IsNullOrEmpty(hdnFlightID.Value) ? 0 : Convert.ToInt32(hdnFlightID.Value, CultureInfo.CurrentCulture); }
        set { hdnFlightID.Value = value.ToString(CultureInfo.CurrentCulture); }
    }

    private string KeyCacheFlight
    {
        get { return "CurFlight" + CurrentFlightID.ToString(CultureInfo.InvariantCulture); }
    }

    /// <summary>
    /// Returns the current flight.  Cached in the session object, initialized from the database on a cache miss.
    /// </summary>
    protected LogbookEntryDisplay CurrentFlight
    {
        get
        {
            if (ViewState[KeyCacheFlight] == null)
                ViewState[KeyCacheFlight] = LoadFlight(CurrentFlightID);
            return (LogbookEntryDisplay)ViewState[KeyCacheFlight];
        }
        set { ViewState[KeyCacheFlight] = value; }
    }

    /// <summary>
    /// Returns the current flight data.  Cached in the session object, initialized from the database on a cache miss.
    /// </summary>
    protected TelemetryDataTable TelemetryData
    {
        get
        {
            LogbookEntryDisplay led = CurrentFlight;

            if (DataForFlight.NeedsComputing)
            {
                if (!DataForFlight.ParseFlightData(led.FlightData) && (lblErr.Text = DataForFlight.ErrorString.Replace("\r\n", "<br />")).Length > 0)
                    pnlErrors.Visible = true;
            }
            return DataForFlight.Data;
        }
    }
    #endregion

    #region Flight selection
    protected void lnkPreviousFlight_Click(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(hdnPrevID.Value))
            Response.Redirect(LinkForFlight(Convert.ToInt32(hdnPrevID.Value, CultureInfo.InvariantCulture)));
    }

    protected void lnkNextFlight_Click(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(hdnNextID.Value))
            Response.Redirect(LinkForFlight(Convert.ToInt32(hdnNextID.Value, CultureInfo.InvariantCulture)));
    }

    protected void UpdateNextPrevious()
    {
        // Before we go, set the next/previous links
        List<int> lstFlights = new List<int>(LogbookEntry.FlightIDsForUser(CurrentFlight.User, Restriction));
        int index = lstFlights.IndexOf(CurrentFlightID);

        if (lnkNextFlight.Visible = (index > 0))
            hdnNextID.Value = lstFlights[index - 1].ToString(CultureInfo.InvariantCulture);

        if (lnkPreviousFlight.Visible = (index < lstFlights.Count - 1))
            hdnPrevID.Value = lstFlights[index + 1].ToString(CultureInfo.InvariantCulture);
    }

    private LogbookEntryDisplay LoadFlight(int idFlight)
    {
        bool fIsAdmin = (util.GetIntParam(Request, "a", 0) != 0 && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanSupport);

        // Check to see if the requested flight belongs to the current user, or if they're authorized.
        // It's an extra database hit (or more, if viewing a student flight), but will let us figure out next/previous
        string szFlightOwner = LogbookEntry.OwnerForFlight(idFlight);
        if (String.IsNullOrEmpty(szFlightOwner))
            throw new MyFlightbookException(Resources.LogbookEntry.errNoSuchFlight);

        // Check that you own the flight, or are admin.  If not either of these, check to see if you are authorized
        if (String.Compare(szFlightOwner, Page.User.Identity.Name, StringComparison.OrdinalIgnoreCase) != 0 && !fIsAdmin)
        {
            // check for authorized by student
            CheckCanViewFlight(szFlightOwner, Page.User.Identity.Name);

            // At this point, we're viewing a student's flight.  Change the return link.
            mvReturn.SetActiveView(vwReturnStudent);
            lnkReturnStudent.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/StudentLogbook.aspx?student={0}", szFlightOwner);
            lnkReturnStudent.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.ReturnToStudent, MyFlightbook.Profile.GetUser(szFlightOwner).UserFullName);
            popmenu.Visible = false;
        }

        // If we're here, we're authorized
        LogbookEntryDisplay led = new LogbookEntryDisplay(idFlight, szFlightOwner, LogbookEntry.LoadTelemetryOption.LoadAll);
        if (!led.HasFlightData)
            led.FlightData = string.Empty;

        if (String.IsNullOrEmpty(led.FlightData))
            apcChart.Visible = apcDownload.Visible = apcRaw.Visible = false;

        lblFlightDate.Text = led.Date.ToShortDateString();
        lblFlightAircraft.Text = led.TailNumDisplay ?? string.Empty;
        lblCatClass.Text = String.Format(CultureInfo.CurrentCulture, "({0})", led.CatClassDisplay);
        lblCatClass.CssClass = led.IsOverridden ? "ExceptionData" : string.Empty;
        litDesc.Text = led.CommentWithReplacedApproaches;
        lblRoute.Text = led.Route.ToUpper(CultureInfo.CurrentCulture);

        Page.Title = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.FlightDetailsTitle, led.Date);

        return led;
    }

    #region Query management
    protected void UpdateRestriction()
    {
        if (Restriction != null && !Restriction.IsDefault)
        {
            pnlFilter.Visible = true;
            mfbQueryDescriptor1.DataSource = Restriction;
            mfbQueryDescriptor1.DataBind();
        }
        else
            pnlFilter.Visible = false;
        UpdateNextPrevious();
    }

    protected void mfbQueryDescriptor1_QueryUpdated(object sender, FilterItemClickedEventArgs fic)
    {
        if (fic == null)
            throw new ArgumentNullException(nameof(fic));
        Restriction.ClearRestriction(fic.FilterItem);
        UpdateRestriction();
    }
    #endregion
    #endregion

    protected bool DoDirectDownload()
    {
        // for debugging, have a download option that skips all the rest
        if (util.GetIntParam(Request, "d", 0) != 0 && !String.IsNullOrEmpty(CurrentFlight.FlightData))
        {
            Response.DownloadToFile(CurrentFlight.FlightData, "application/octet-stream", String.Format(CultureInfo.InvariantCulture, "Data{0}-{1}-{2}", Branding.CurrentBrand.AppName, MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-"), "csv");
            return true;
        }
        return false;
    }

    protected void SetUpMaps()
    {
        // Set up any maps.
        cmbFormat.Items[(int) DownloadFormat.KML].Enabled = cmbFormat.Items[(int) DownloadFormat.GPX].Enabled = pnlMapControls.Visible = SetUpMapManager(mfbGoogleMapManager1, CurrentFlight.Route, PathLatLongArrayID);
        lnkZoomToFit.NavigateUrl = mfbGoogleMapManager1.ZoomToFitScript;
    }

    protected void SetUpDownload(LogbookEntryBase led)
    {
        if (led == null)
            throw new ArgumentNullException(nameof(led));
        if (Viewer.CloudAhoyToken == null || Viewer.CloudAhoyToken.AccessToken == null)
            lnkSendCloudAhoy.Visible = false;

        lblOriginalFormat.Text = FormatNameForTelemetry(led);

        // allow selection of units if units are not implicitly known.
        cmbAltUnits.Enabled = cmbSpeedUnits.Enabled = CanSpecifyUnitsForTelemetry(led);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.tabLogbook;

        using (DataForFlight = new FlightData())
        {
            if (!IsPostBack)
            {
                try
                {
                    CurrentFlightID = InitRequestedFlightID();
                    if (CurrentFlightID == LogbookEntryCore.idFlightNone)
                        throw new MyFlightbookException("No valid ID passed");

                    InitPassedRestriction();

                    int iTab = GetRequestedTabIndex();
                    if (AccordionCtrl.Panes[iTab].Visible)
                        AccordionCtrl.SelectedIndex = iTab;

                    LogbookEntryDisplay led = CurrentFlight = LoadFlight(CurrentFlightID);
                    SetUpChart(TelemetryData);
                    UpdateChart();
                    UpdateRestriction();

                    SetUpDownload(led);

                    // shouldn't happen but sometimes does: GetUserAircraftByID returns null.  Not quite sure why.
                    Aircraft ac = (new UserAircraft(led.User).GetUserAircraftByID(led.AircraftID)) ?? new Aircraft(led.AircraftID);
                    fmvAircraft.DataSource = new Aircraft[] { ac };
                    fmvAircraft.DataBind();

                    if (String.IsNullOrEmpty(CurrentFlight.FlightData) && TabIndexRequiresFlightData(iTab))
                        AccordionCtrl.SelectedIndex = DefaultTabIndex;
                }
                catch (MyFlightbookException ex)
                {
                    lblPageErr.Text = ex.Message;
                    AccordionCtrl.Visible = mfbGoogleMapManager1.Visible = pnlMap.Visible = pnlAccordionMenuContainer.Visible = pnlFlightDesc.Visible = false;
                    return;
                }

                if (DoDirectDownload())
                    return;
            }
            else
            {
                DataForFlight.Data = TelemetryData;
                UpdateChart();
            }

            if (Restriction != null && !Restriction.IsDefault)
                mfbFlightContextMenu.EditTargetFormatString = mfbFlightContextMenu.EditTargetFormatString + "?fq=" + HttpUtility.UrlEncode(Restriction.ToBase64CompressedJSONString());
            mfbFlightContextMenu.Flight = CurrentFlight;

            cmbAltUnits.SelectedValue = ((int)DataForFlight.AltitudeUnits).ToString(CultureInfo.InvariantCulture);
            cmbSpeedUnits.SelectedValue = ((int)DataForFlight.SpeedUnits).ToString(CultureInfo.InvariantCulture);
            if (!DataForFlight.HasDateTime)
                lnkSendCloudAhoy.Visible = false;

            SetUpMaps();

            if (!IsPostBack)
            {
                // Bind details - this will bind everything else.
                fmvLE.DataSource = new LogbookEntryDisplay[] { CurrentFlight };
                fmvLE.DataBind();
            }
        }

        // DataForFlight is disposed - set it to null!!
        DataForFlight = null;
    }

    #region chart management
    protected void SetUpChart(DataTable data)
    {
        if (data == null)
            return;

        // now set up the chart
        cmbXAxis.Items.Clear();
        cmbYAxis1.Items.Clear();
        cmbYAxis2.Items.Clear();
        cmbYAxis2.Items.Add(new ListItem(Resources.FlightData.GraphNoData, ""));
        cmbYAxis2.SelectedIndex = 0;

        foreach (DataColumn dc in data.Columns)
        {
            KnownColumn kc = KnownColumn.GetKnownColumn(dc.Caption);

            if (kc.Type.CanGraph())
                cmbXAxis.Items.Add(new ListItem(kc.FriendlyName, kc.Column));
            if (kc.Type == KnownColumnTypes.ctDec || kc.Type == KnownColumnTypes.ctFloat || kc.Type == KnownColumnTypes.ctInt)
            {
                cmbYAxis1.Items.Add(new ListItem(kc.FriendlyName, kc.Column));
                cmbYAxis2.Items.Add(new ListItem(kc.FriendlyName, kc.Column));
            }
        }

        // Select a date or time column for the X axis if possible; if not, select "SAMPLES"
        if (cmbXAxis.Items.Count > 0)
        {
            if (data.Columns["DATE"] != null)
                cmbXAxis.SelectedValue = "DATE";
            else if (data.Columns["TIME"] != null)
                cmbXAxis.SelectedValue = "TIME";
            else if (data.Columns["SAMPLE"] != null)
                cmbXAxis.SelectedValue = "SAMPLE";
            else
                cmbXAxis.SelectedIndex = 0;
        }

        // if there is something numeric for the Y axis, select it.  Otherwise, default to the first item.
        if (cmbYAxis1.Items.Count > 0)
            cmbYAxis1.SelectedIndex = 0;

        foreach (ListItem li in cmbYAxis1.Items)
        {
            KnownColumn kc = KnownColumn.GetKnownColumn(li.Value);
            if (kc.Type != KnownColumnTypes.ctDateTime && kc.Type != KnownColumnTypes.ctLatLong && kc.Column != "SAMPLE")
            {
                cmbYAxis1.SelectedValue = kc.Column;
                break;
            }
        }
    }

    protected void UpdateChart()
    {
        TelemetryDataTable tdt = TelemetryData;
        if (tdt == null)
            return;

        UpdateChart(tdt, gcData, DataForFlight, PathLatLongArrayID, cmbXAxis.SelectedItem, cmbYAxis1.SelectedItem, cmbYAxis2.SelectedItem, out decimal max, out decimal min, out decimal max2, out decimal min2);

        // Clear out the grid.
        gvData.DataSource = null;
        gvData.DataBind();
        apcRaw.LazyLoad = true;

        int idx = mfbAccordionProxyExtender.IndexForProxyID(apcRaw.ID);
        mfbAccordionProxyExtender.SetJavascriptForControl(apcRaw, idx == AccordionCtrl.SelectedIndex, idx);

        lblMaxY.Text = max > decimal.MinValue && !String.IsNullOrEmpty(gcData.YLabel) ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ChartMaxX, gcData.YLabel, max) : string.Empty;
        lblMinY.Text = min < decimal.MaxValue && !String.IsNullOrEmpty(gcData.YLabel) ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ChartMinX, gcData.YLabel, min) : string.Empty;
        lblMaxY2.Text = max2 > decimal.MinValue && !String.IsNullOrEmpty(gcData.Y2Label) ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ChartMaxX, gcData.Y2Label, max2) : string.Empty;
        lblMinY2.Text = min2 < decimal.MaxValue && !String.IsNullOrEmpty(gcData.Y2Label) ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ChartMinX, gcData.Y2Label, min2) : string.Empty;
    }

    protected void cmbYAxis1_SelectedIndexChanged(object sender, EventArgs e)
    {
        // Nothing to do - done in Page_Load
    }

    protected void cmbYAxis2_SelectedIndexChanged(object sender, EventArgs e)
    {
        // Nothing to do - done in Page_Load
    }

    protected void cmbXAxis_SelectedIndexChanged(object sender, EventArgs e)
    {
        // Nothing to do - done in Page_Load
    }
    #endregion

    // No need to do anything - the postback will clear events from the map.
    protected void lnkClearEvents_Click(object sender, EventArgs e) { }

    protected void btnDownload_Click(object sender, EventArgs e)
    {
        using (FlightData fd = new FlightData() { FlightID = CurrentFlightID })
        {
            fd.ParseFlightData(CurrentFlight.FlightData);
            DownloadData(CurrentFlight, fd, Convert.ToInt32(cmbAltUnits.SelectedValue, CultureInfo.InvariantCulture), Convert.ToInt32(cmbSpeedUnits.SelectedValue, CultureInfo.InvariantCulture), cmbFormat.SelectedIndex);
        }
    }

    protected async void lnkSendCloudAhoy_Click(object sender, EventArgs e)
    {
        using (FlightData fd = new FlightData() { FlightID = CurrentFlightID })
        {
            try
            {
                fd.ParseFlightData(CurrentFlight.FlightData);
                pnlCloudAhoySuccess.Visible = await PushToCloudahoy(Page.User.Identity.Name, CurrentFlight, fd, Convert.ToInt32(cmbAltUnits.SelectedValue, CultureInfo.InvariantCulture), Convert.ToInt32(cmbSpeedUnits.SelectedValue, CultureInfo.InvariantCulture), !Branding.CurrentBrand.MatchesHost(Request.Url.Host)).ConfigureAwait(false);
            }
            catch (MyFlightbookException ex)
            {
                lblCloudAhoyErr.Text = ex.Message;
            }
        }
    }

    #region Raw Data
    protected void OnRowDatabound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        if (e.Row.RowType == DataControlRowType.DataRow)
            BindRawDataRow(DataForFlight, e.Row.DataItemIndex, e.Row.FindControl("imgPin"), e.Row.FindControl("lnkZoom"));
    }

    protected void apcRaw_ControlClicked(object sender, EventArgs e)
    {
        using (DataForFlight = new FlightData())
        {
            apcRaw.LazyLoad = false;
            int idx = mfbAccordionProxyExtender.IndexForProxyID(apcRaw.ID);
            mfbAccordionProxyExtender.SetJavascriptForControl(apcRaw, true, idx);
            AccordionCtrl.SelectedIndex = idx;

            TelemetryDataTable tdt = TelemetryData;

            // see if we need to hide the "Position" column
            bool fHasPositionColumn = false;
            foreach (DataColumn dc in tdt.Columns)
            {
                if (dc.ColumnName.CompareOrdinalIgnoreCase(KnownColumnNames.POS) == 0)
                    fHasPositionColumn = true;
            }
            if (!fHasPositionColumn)
                gvData.Columns.RemoveAt(1);

            gvData.DataSource = tdt;
            gvData.DataBind();
        }
        DataForFlight = null;
    }
    #endregion

    #region Aircraft/Flight formview
    protected void fmvLE_DataBound(object sender, EventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        if (sender == null)
            throw new ArgumentNullException(nameof(sender));
        if (!(sender is FormView fv))
            throw new InvalidCastException("Sender is not a formview!");

        LogbookEntryDisplay le = (LogbookEntryDisplay)fv.DataItem;
        BindImages(fv.FindControl("mfbilFlight"), le, mfbGoogleMapManager1);

        BindVideoEntries(fv.FindControl("mfbVideoEntry1"), le);

        BindAirportServices(fv.FindControl("mfbAirportServices1"), mfbGoogleMapManager1, CurrentFlight.Route);

        BindSignature(fv.FindControl("mfbSignature"), le);

        BindBadges(Viewer, le.FlightID, fv.FindControl("rptBadges"));
    }

    protected void fmvAircraft_DataBound(object sender, EventArgs e)
    {

        if (sender == null)
            throw new ArgumentNullException(nameof(sender));
        FormView fv = sender as FormView;
        BindAircraftImages(fv.FindControl("mfbHoverImageList"));
    }
    #endregion

    protected void btnMetars_Click(object sender, EventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        BindMetars(fmvLE.FindControl("METARDisplay"), CurrentFlight.Route);
        fmvLE.FindControl("btnMetars").Visible = false;
    }

    protected void mfbFlightContextMenu_DeleteFlight(object sender, LogbookEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));

        LogbookEntryDisplay.FDeleteEntry(e.FlightID, Page.User.Identity.Name);
        Response.Redirect(TargetPage);
    }

    protected void mfbFlightContextMenu_SendFlight(object sender, LogbookEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));

        mfbSendFlight.SendFlight(e.FlightID);
    }
}