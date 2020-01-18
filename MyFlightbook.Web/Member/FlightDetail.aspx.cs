using MyFlightbook;
using MyFlightbook.Achievements;
using MyFlightbook.Airports;
using MyFlightbook.Geography;
using MyFlightbook.Image;
using MyFlightbook.Instruction;
using MyFlightbook.OAuth.CloudAhoy;
using MyFlightbook.Telemetry;
using MyFlightbook.Weather.ADDS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2017-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_FlightDetail : System.Web.UI.Page
{
    private enum DownloadFormat { Original, CSV, KML, GPX };

    public enum DetailsTab { Flight, Aircraft, Chart, Data, Download }

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

    private string m_szDefaultPage = "~/Member/LogbookNew.aspx";

    /// <summary>
    /// The URL to which edited flights should go
    /// </summary>
    public string TargetPage
    {
        get { return m_szDefaultPage; }
        set { m_szDefaultPage = value; }
    }

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

    private readonly FlightData m_fd = new FlightData();
    protected FlightData DataForFlight
    {
        get { return m_fd; }
    }

    /// <summary>
    /// Returns the current flight data.  Cached in the session object, initialized from the database on a cache miss.
    /// </summary>
    protected TelemetryDataTable TelemetryData
    {
        get
        {
            LogbookEntryDisplay led = CurrentFlight;

            if (m_fd.NeedsComputing)
            {
                if (!m_fd.ParseFlightData(led.FlightData) && (lblErr.Text = m_fd.ErrorString.Replace("\r\n", "<br />")).Length > 0)
                    pnlErrors.Visible = true;
            }
            return m_fd.Data;
        }
    }

    private const string szkeyVSAirportListResult = "szkeyVSALR";
    protected ListsFromRoutesResults RoutesList
    {
        get
        {
            ListsFromRoutesResults lfrr = (ListsFromRoutesResults)ViewState[szkeyVSAirportListResult];
            if (lfrr == null)
                ViewState[szkeyVSAirportListResult] = lfrr = AirportList.ListsFromRoutes(CurrentFlight.Route);
            return lfrr;
        }
    }

    private const string szKeyVSRestriction = "viewstateRestrictionKey";
    protected FlightQuery Restriction
    {
        get { return (FlightQuery)ViewState[szKeyVSRestriction]; }
        set { ViewState[szKeyVSRestriction] = value; }
    }
    #endregion

    #region Flight selection
    private string LinkForFlight(int idFlight)
    {
        return String.Format(CultureInfo.InvariantCulture, "~/Member/FlightDetail.aspx/{0}{1}", idFlight, Restriction == null || Restriction.IsDefault ? string.Empty : "?fq=" + HttpUtility.UrlEncode(Restriction.ToBase64CompressedJSONString()));
    }

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
        // Check to see if the requested flight belongs to the current user, or if they're authorized.
        // It's an extra database hit (or more, if viewing a student flight), but will let us figure out next/previous
        string szFlightOwner = LogbookEntry.OwnerForFlight(idFlight);
        if (String.IsNullOrEmpty(szFlightOwner))
            throw new MyFlightbookException(Resources.LogbookEntry.errNoSuchFlight);

        bool fIsAdmin = (util.GetIntParam(Request, "a", 0) != 0 && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanSupport);

        // Check that you own the flight, or are admin.  If not either of these, check to see if you are authorized
        if (String.Compare(szFlightOwner, Page.User.Identity.Name, StringComparison.OrdinalIgnoreCase) != 0 && !fIsAdmin)
        {
            // check for authorized by student
            CFIStudentMap sm = new CFIStudentMap(Page.User.Identity.Name);
            InstructorStudent student = sm.GetInstructorStudent(sm.Students, szFlightOwner);
            if (student == null || !student.CanViewLogbook)
                throw new MyFlightbookException(Resources.SignOff.ViewStudentLogbookUnauthorized);

            // At this point, we're viewing a student's flight.  Change the return link.
            mvReturn.SetActiveView(vwReturnStudent);
            lnkReturnStudent.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/StudentLogbook.aspx?student={0}", szFlightOwner);
            lnkReturnStudent.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.ReturnToStudent, MyFlightbook.Profile.GetUser(szFlightOwner).UserFullName);
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

    protected void mfbQueryDescriptor1_QueryUpdated(object sender, FilterItemClicked fic)
    {
        if (fic == null)
            throw new ArgumentNullException("fic");
        Restriction.ClearRestriction(fic.FilterItem);
        UpdateRestriction();
    }
    #endregion
    #endregion


    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.tabLogbook;

        if (!IsPostBack)
        {
            try
            {

                if (Request.PathInfo.Length > 0)
                {
                    try { CurrentFlightID = Convert.ToInt32(Request.PathInfo.Substring(1), CultureInfo.InvariantCulture); }
                    catch (FormatException) { CurrentFlightID = LogbookEntry.idFlightNone; }
                }
                if (CurrentFlightID == LogbookEntry.idFlightNone)
                    throw new MyFlightbookException("No valid ID passed");

                string szFQParam = util.GetStringParam(Request, "fq");
                if (!String.IsNullOrEmpty(szFQParam))
                {
                    try
                    {
                        Restriction = FlightQuery.FromBase64CompressedJSON(szFQParam);
                        Restriction.Refresh();
                    }
                    catch (ArgumentNullException) { }
                    catch (FormatException) { }
                    catch (JsonSerializationException) { }
                    catch (JsonException) { }
                }

                DetailsTab dtRequested = DetailsTab.Flight;
                if (Enum.TryParse<DetailsTab>(util.GetStringParam(Request, "tabID"), out dtRequested))
                {
                    int iTab = (int)dtRequested;
                    if (AccordionCtrl.Panes[iTab].Visible)
                        AccordionCtrl.SelectedIndex = iTab;
                }

                LogbookEntryDisplay led = CurrentFlight = LoadFlight(CurrentFlightID);
                SetUpChart(TelemetryData);
                UpdateChart();
                UpdateRestriction();

                if (Viewer.CloudAhoyToken == null || Viewer.CloudAhoyToken.AccessToken == null)
                    lnkSendCloudAhoy.Visible = false;

                lblOriginalFormat.Text = DataSourceType.DataSourceTypeFromFileType(led.Telemetry.TelemetryType).DisplayName;

                // allow selection of units if units are not implicitly known.
                switch (led.Telemetry.TelemetryType)
                {
                    case DataSourceType.FileType.GPX:
                    case DataSourceType.FileType.KML:
                    case DataSourceType.FileType.NMEA:
                    case DataSourceType.FileType.IGC:
                        cmbAltUnits.Enabled = cmbSpeedUnits.Enabled = false;
                        break;
                    default:
                        cmbAltUnits.Enabled = cmbSpeedUnits.Enabled = true;
                        break;
                }

                // shouldn't happen but sometimes does: GetUserAircraftByID returns null.  Not quite sure why.
                Aircraft ac = (new UserAircraft(led.User).GetUserAircraftByID(led.AircraftID)) ?? new Aircraft(led.AircraftID);
                fmvAircraft.DataSource = new Aircraft[] { ac };
                fmvAircraft.DataBind();

                if (String.IsNullOrEmpty(CurrentFlight.FlightData) && dtRequested != DetailsTab.Aircraft && dtRequested != DetailsTab.Flight)
                    AccordionCtrl.SelectedIndex = (int)DetailsTab.Flight;
            }
            catch (MyFlightbookException ex)
            {
                lblPageErr.Text = ex.Message;
                AccordionCtrl.Visible = mfbGoogleMapManager1.Visible = pnlMap.Visible = pnlAccordionMenuContainer.Visible = pnlFlightDesc.Visible = false;
                return;
            }

            // for debugging, have a download option that skips all the rest
            if (util.GetIntParam(Request, "d", 0) != 0 && !String.IsNullOrEmpty(CurrentFlight.FlightData))
            {
                Response.Clear();
                Response.ContentType = "application/octet-stream";
                // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
                string szFilename = String.Format(CultureInfo.InvariantCulture, "Data{0}-{1}-{2}", Branding.CurrentBrand.AppName, MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
                string szDisposition = String.Format(CultureInfo.InvariantCulture, "inline;filename={0}.csv", System.Text.RegularExpressions.Regex.Replace(szFilename, "[^0-9a-zA-Z-]", ""));
                Response.AddHeader("Content-Disposition", szDisposition);
                Response.Write(CurrentFlight.FlightData);
                Response.End();
                return;
            }
        }
        else
        {
            m_fd.Data = TelemetryData;
            UpdateChart();
        }

        if (Restriction != null && !Restriction.IsDefault)
            mfbFlightContextMenu.EditTargetFormatString = mfbFlightContextMenu.EditTargetFormatString + "?fq=" + HttpUtility.UrlEncode(Restriction.ToBase64CompressedJSONString());
        mfbFlightContextMenu.Flight = CurrentFlight;

        cmbAltUnits.SelectedValue = ((int) m_fd.AltitudeUnits).ToString(CultureInfo.InvariantCulture);
        cmbSpeedUnits.SelectedValue = ((int)m_fd.SpeedUnits).ToString(CultureInfo.InvariantCulture);
        if (!m_fd.HasDateTime)
            lnkSendCloudAhoy.Visible = false;

        // Set up any maps.
        mfbGoogleMapManager1.Map.Airports = RoutesList.Result;
        mfbGoogleMapManager1.ShowMarkers = true;
        mfbGoogleMapManager1.Map.PathVarName = PathLatLongArrayID;
        mfbGoogleMapManager1.Map.Path = m_fd.GetPath();
        if (m_fd.HasLatLongInfo && m_fd.Data.Rows.Count > 1)
        {
            cmbFormat.Items[(int)DownloadFormat.KML].Enabled = true;
            cmbFormat.Items[(int)DownloadFormat.GPX].Enabled = true;
            mfbGoogleMapManager1.Mode = MyFlightbook.Mapping.GMap_Mode.Dynamic;
            pnlMapControls.Visible = true;
        }
        else
        {
            cmbFormat.Items[(int)DownloadFormat.KML].Enabled = false;
            cmbFormat.Items[(int)DownloadFormat.GPX].Enabled = false;
            mfbGoogleMapManager1.Mode = MyFlightbook.Mapping.GMap_Mode.Static;
            pnlMapControls.Visible = false;
        }
        lnkZoomToFit.NavigateUrl = mfbGoogleMapManager1.ZoomToFitScript;

        if (!IsPostBack)
        {
            // Bind details - this will bind everything else.
            fmvLE.DataSource = new LogbookEntryDisplay[] { CurrentFlight };
            fmvLE.DataBind();
        }
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
        decimal max = decimal.MinValue;
        decimal min = decimal.MaxValue;
        decimal max2 = decimal.MinValue;
        decimal min2 = decimal.MaxValue;

        TelemetryDataTable tdt = TelemetryData;
        if (tdt == null)
            return;

        gcData.XLabel = (cmbXAxis.SelectedItem == null) ? "" : cmbXAxis.SelectedItem.Text;
        gcData.YLabel = (cmbYAxis1.SelectedItem == null) ? "" : cmbYAxis1.SelectedItem.Text;
        gcData.Y2Label = (cmbYAxis2.SelectedIndex == 0 || cmbYAxis2 == null) ? "" : cmbYAxis2.SelectedItem.Text;

        gcData.Clear();

        gcData.XDataType = gcData.GoogleTypeFromKnownColumnType(KnownColumn.GetKnownColumn(cmbXAxis.SelectedValue).Type);
        gcData.YDataType = gcData.GoogleTypeFromKnownColumnType(KnownColumn.GetKnownColumn(cmbYAxis1.SelectedValue).Type);
        gcData.Y2DataType = gcData.GoogleTypeFromKnownColumnType(KnownColumn.GetKnownColumn(cmbYAxis2.SelectedValue).Type);

        if (cmbYAxis1.SelectedItem == null || cmbYAxis2.SelectedItem == null)
        {
            gcData.Visible = false;
            return;
        }

        if (m_fd.HasLatLongInfo)
            gcData.ClickHandlerJS = String.Format(CultureInfo.InvariantCulture, "dropPin({0}[selectedItem.row], xvalue + ': ' + ((selectedItem.column == 1) ? '{1}' : '{2}') + ' = ' + value);", PathLatLongArrayID, cmbYAxis1.SelectedItem.Text, cmbYAxis2.SelectedItem.Text);

        foreach (DataRow dr in tdt.Rows)
        {
            gcData.XVals.Add(dr[cmbXAxis.SelectedValue]);

            if (!String.IsNullOrEmpty(cmbYAxis1.SelectedValue))
            {
                object o = dr[cmbYAxis1.SelectedValue];
                gcData.YVals.Add(o);
                if (gcData.YDataType == Controls_GoogleChart.GoogleColumnDataType.number)
                {
                    decimal d = Convert.ToDecimal(o, CultureInfo.InvariantCulture);
                    max = Math.Max(max, d);
                    min = Math.Min(min, d);
                }
            }
            if (cmbYAxis2.SelectedValue.Length > 0 && cmbYAxis2.SelectedValue != cmbYAxis1.SelectedValue)
            {
                object o = dr[cmbYAxis2.SelectedValue];
                gcData.Y2Vals.Add(o);
                if (gcData.Y2DataType == Controls_GoogleChart.GoogleColumnDataType.number)
                {
                    decimal d = Convert.ToDecimal(o, CultureInfo.InvariantCulture);
                    max2 = Math.Max(max2, d);
                    min2 = Math.Min(min2, d);
                }
            }
        }
        gcData.TickSpacing = 1; // Math.Max(1, m_fd.Data.Rows.Count / 20);

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
        UpdateChart();
    }

    protected void cmbYAxis2_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateChart();
    }

    protected void cmbXAxis_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateChart();
    }
    #endregion

    // No need to do anything - the postback will clear events from the map.
    protected void lnkClearEvents_Click(object sender, EventArgs e) { }

    protected void btnDownload_Click(object sender, EventArgs e)
    {
        m_fd.AltitudeUnits = (FlightData.AltitudeUnitTypes)Convert.ToInt32(cmbAltUnits.SelectedValue, CultureInfo.InvariantCulture);
        m_fd.SpeedUnits = (FlightData.SpeedUnitTypes)Convert.ToInt32(cmbSpeedUnits.SelectedValue, CultureInfo.InvariantCulture);
        m_fd.FlightID = CurrentFlightID;

        DataSourceType dst = null;
        Action writeData = null;

        switch ((DownloadFormat)cmbFormat.SelectedIndex)
        {
            case DownloadFormat.Original:
                string szData = CurrentFlight.FlightData;
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

    protected async void lnkSendCloudAhoy_Click(object sender, EventArgs e)
    {
        m_fd.AltitudeUnits = (FlightData.AltitudeUnitTypes)Convert.ToInt32(cmbAltUnits.SelectedValue, CultureInfo.InvariantCulture);
        m_fd.SpeedUnits = (FlightData.SpeedUnitTypes)Convert.ToInt32(cmbSpeedUnits.SelectedValue, CultureInfo.InvariantCulture);
        m_fd.FlightID = CurrentFlightID;

        CloudAhoyClient client = new CloudAhoyClient(!Branding.CurrentBrand.MatchesHost(Request.Url.Host)) { AuthState = Viewer.CloudAhoyToken };

        MemoryStream ms = null;
        try
        {
            switch (CurrentFlight.Telemetry.TelemetryType)
            {
                default:
                    ms = new MemoryStream();
                    m_fd.WriteGPXData(ms);
                    break;
                case DataSourceType.FileType.GPX:
                case DataSourceType.FileType.KML:
                    ms = new MemoryStream(Encoding.UTF8.GetBytes(CurrentFlight.Telemetry.RawData));
                    break;
            }

            ms.Seek(0, SeekOrigin.Begin);
            await client.PutStream(ms, CurrentFlight);
            pnlCloudAhoySuccess.Visible = true;
        }
        catch (MyFlightbookException ex)
        {
            lblCloudAhoyErr.Text = ex.Message;
        }
        finally
        {
            if (ms != null)
                ms.Dispose();
        }
    }

    #region Raw Data
    protected void OnRowDatabound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            Image i = (Image)e.Row.FindControl("imgPin");
            HyperLink h = (HyperLink)e.Row.FindControl("lnkZoom");
            if (m_fd.HasLatLongInfo)
            {
                int iRow = e.Row.DataItemIndex;

                DataRow drow = m_fd.Data.Rows[iRow];

                StringBuilder sbDesc = new StringBuilder();

                double lat = 0.0, lon = 0.0;

                foreach (DataColumn dc in m_fd.Data.Columns)
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
                                sbDesc.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}<br />", dc.ColumnName, sz);
                        }
                    }
                }

                i.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:dropPin(new google.maps.LatLng({0}, {1}), '{2}');", lat, lon, sbDesc.ToString());
                h.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:getMfbMap().gmap.setCenter(new google.maps.LatLng({0}, {1}));getMfbMap().gmap.setZoom(14);", lat, lon);
                i.ToolTip = String.Format(CultureInfo.CurrentCulture, Resources.FlightData.GraphDropPinTip, (new LatLong(lat, lon)).ToString());
            }
            else
                i.Visible = h.Visible = false;
        }
    }

    protected void apcRaw_ControlClicked(object sender, EventArgs e)
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
    #endregion

    #region Aircraft/Flight formview
    protected void fmvLE_DataBound(object sender, EventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        FormView fv = sender as FormView;
        LogbookEntryDisplay le = (LogbookEntryDisplay)fv.DataItem;
        Controls_mfbImageList mfbilFlight = (Controls_mfbImageList)fv.FindControl("mfbilFlight");
        mfbilFlight.Key = le.FlightID.ToString(CultureInfo.InvariantCulture);
        mfbilFlight.Refresh();
        mfbGoogleMapManager1.Map.Images = mfbilFlight.Images.ImageArray;

        Controls_mfbVideoEntry ve = (Controls_mfbVideoEntry)fv.FindControl("mfbVideoEntry1");
        ve.Videos.Clear();
        foreach (VideoRef vr in le.Videos)
            ve.Videos.Add(vr);

        Controls_mfbAirportServices aptSvc = (Controls_mfbAirportServices)fv.FindControl("mfbAirportServices1");
        aptSvc.GoogleMapID = mfbGoogleMapManager1.MapID;
        aptSvc.AddZoomLink = (mfbGoogleMapManager1.Mode == MyFlightbook.Mapping.GMap_Mode.Dynamic);
        aptSvc.SetAirports(RoutesList.MasterList.GetNormalizedAirports());

        ((Controls_mfbSignature)fv.FindControl("mfbSignature")).Flight = le;

        IEnumerable<Badge> cached = Viewer.CachedBadges;
        List<Badge> badges = (cached == null ? null : new List<Badge>(cached));
        if (badges != null)
        {
            Repeater rptBadges = (Repeater)fv.FindControl("rptBadges");
            rptBadges.DataSource = BadgeSet.BadgeSetsFromBadges(badges.FindAll(b => b.IDFlightEarned == le.FlightID));
            rptBadges.DataBind();
        }
    }

    protected void fmvAircraft_DataBound(object sender, EventArgs e)
    {
        FormView fv = sender as FormView;
        Controls_mfbHoverImageList hil = (Controls_mfbHoverImageList)fv.FindControl("mfbHoverImageList");
        hil.Refresh();
    }
    #endregion

    protected void btnMetars_Click(object sender, EventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        Controls_METAR m = (Controls_METAR)fmvLE.FindControl("METARDisplay");
        m.METARs = new ADDSService().LatestMETARSForAirports(CurrentFlight.Route);
    }

    protected void mfbFlightContextMenu_DeleteFlight(object sender, LogbookEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        LogbookEntryDisplay.FDeleteEntry(e.FlightID, Page.User.Identity.Name);
        Response.Redirect(TargetPage);
    }

    protected void mfbFlightContextMenu_SendFlight(object sender, LogbookEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        mfbSendFlight.SendFlight(e.FlightID);
    }
}