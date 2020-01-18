using MyFlightbook;
using MyFlightbook.Airports;
using MyFlightbook.Geography;
using MyFlightbook.Instruction;
using MyFlightbook.Telemetry;
using System;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_FlightAnalysis : System.Web.UI.Page
{
    private enum DownloadFormat { Original, CSV, KML, GPX};
    private const string keyCurrentFlight = "CurFlight";
    private const string keyParsedData = "ParsedFlightData";
    private const string szFormatDateTime = "M/d/yyyy hh:mm:ss";
    private LogbookEntry m_le;
    private FlightData m_fd = new FlightData();

    /// <summary>
    /// Loads the specified flight into m_le, cached to avoid excess DB thrashing.
    /// </summary>
    /// <param name="idFlight">ID of the flight to load</param>
    protected void LoadLogbookEntry(int idFlight)
    {
        string szCacheKey = KeyCacheFlight(idFlight);

        Title = (string) GetLocalResourceObject("PageResource1.Title");

        m_le = null;
        if (!IsPostBack || Session[szCacheKey] == null)
        {
            m_le = new LogbookEntry();

            bool fIsAdmin = (util.GetIntParam(Request, "a", 0) != 0 && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanSupport);
            if (m_le.FLoadFromDB(idFlight, User.Identity.Name, LogbookEntry.LoadTelemetryOption.LoadAll, true))
            {
                if (!m_le.HasFlightData)
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.errNoDataForFlight, idFlight));

                // check to see if you own the flight.  You own it if:
                // a) Admin
                // b) username=Page identity, or 
                // c) authorized by student.
                if (!fIsAdmin && String.Compare(m_le.User, User.Identity.Name, StringComparison.Ordinal) != 0)
                {
                    // check for authorized by student
                    CFIStudentMap sm = new CFIStudentMap(Page.User.Identity.Name);
                    InstructorStudent student = sm.GetInstructorStudent(sm.Students, m_le.User);
                    if (student == null || !student.CanViewLogbook)
                        throw new MyFlightbookException(Resources.SignOff.ViewStudentLogbookUnauthorized);
                }

                Session[szCacheKey] = m_le;
            }
            else
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.errCantLoadFlight, idFlight.ToString(CultureInfo.InvariantCulture), m_le.ErrorString));
        }
        else
            m_le = (LogbookEntry)Session[szCacheKey];
    }

    /// <summary>
    /// Loads the data for the specified flight, parsing the CSV file, and thus initializing m_fd.  This is cached, so it's OK to call multiple times.
    /// </summary>
    /// <param name="idFlight">ID of the flight with data to load</param>
    protected void LoadData(LogbookEntryBase le)
    {
        if (le == null)
            throw new ArgumentNullException("le");
        string szCacheKey = KeyCacheData(le.FlightID);

        TelemetryDataTable dt = (TelemetryDataTable)Session[szCacheKey];
        if (dt != null)
            m_fd.Data = dt;

        if (m_fd.NeedsComputing)
        {
            if (!m_fd.ParseFlightData(le.FlightData) && (lblErr.Text = m_fd.ErrorString.Replace("\r\n", "<br />")).Length > 0)
                pnlErrors.Visible = true;

            if (m_fd.Data != null)
            {
                Session[szCacheKey] = m_fd.Data; // cache the results.

                // now set up the chart
                cmbXAxis.Items.Clear();
                cmbYAxis1.Items.Clear();
                cmbYAxis2.Items.Clear();
                cmbYAxis2.Items.Add(new ListItem(Resources.FlightData.GraphNoData, ""));
                cmbYAxis2.SelectedIndex = 0;

                foreach (DataColumn dc in m_fd.Data.Columns)
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
                    if (m_fd.Data.Columns["DATE"] != null)
                        cmbXAxis.SelectedValue = "DATE";
                    else if (m_fd.Data.Columns["TIME"] != null)
                        cmbXAxis.SelectedValue = "TIME";
                    else if (m_fd.Data.Columns["SAMPLE"] != null)
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
        }
    }

    protected string KeyCacheFlight(int idFlight)
    {
        return keyCurrentFlight + idFlight.ToString(CultureInfo.InvariantCulture);
    }

    protected string KeyCacheData(int idFlight)
    {
        return keyParsedData + idFlight.ToString(CultureInfo.InvariantCulture);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        int idFlight = util.GetIntParam(Request, "id", LogbookEntry.idFlightNone);
        if (idFlight == LogbookEntry.idFlightNone)
            throw new MyFlightbookException("No valid ID passed");

        Master.SelectedTab = tabID.tabLogbook;

        if (!IsPostBack)
        {
            // On a GET request, discard the cache and regenerate it.
            Session[KeyCacheData(idFlight)] = null;
            Session[KeyCacheFlight(idFlight)] = null;
        }

        // Set up the object, either from cache or from db
        LoadLogbookEntry(idFlight);

        // for debugging, have a download option that skips all the rest
        if (util.GetIntParam(Request, "d", 0) != 0)
        {
            Response.Clear();
            Response.ContentType = "application/octet-stream";
            // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
            string szFilename = String.Format(CultureInfo.InvariantCulture, "Data{0}-{1}-{2}", Branding.CurrentBrand.AppName, MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
            string szDisposition = String.Format(CultureInfo.InvariantCulture, "inline;filename={0}.csv", System.Text.RegularExpressions.Regex.Replace(szFilename, "[^0-9a-zA-Z-]", ""));
            Response.AddHeader("Content-Disposition", szDisposition);
            Response.Write(m_le.FlightData);
            Response.End();
            return;
        }

        LoadData(m_le);

        // Set up any maps.
        mfbGoogleMapManager1.Map.SetAirportList(new AirportList(m_le.Route));
        mfbGoogleMapManager1.ShowMarkers = true;
        if (m_fd.HasLatLongInfo && m_fd.Data.Rows.Count > 1)
        {
            pnlMap.Visible = true;
            mfbGoogleMapManager1.Map.PathVarName = PathLatLongArrayID;
            mfbGoogleMapManager1.Map.Path = m_fd.GetPath();
            cmbFormat.Items[(int)DownloadFormat.KML].Enabled = true;
            cmbFormat.Items[(int)DownloadFormat.GPX].Enabled = true;
        }
        else
        {
            cmbFormat.Items[(int)DownloadFormat.KML].Enabled = false;
            cmbFormat.Items[(int)DownloadFormat.GPX].Enabled = false;
        }
        lnkZoomToFit.NavigateUrl = mfbGoogleMapManager1.ZoomToFitScript;

        // Compute and store the distances, but only the first time since it doesn't change.
        if (!IsPostBack)
        {
            lblDistance.Text = m_le.GetPathDistanceDescription(m_fd.ComputePathDistance());
            pnlDistance.Visible = lblDistance.Text.Length > 0;
            lblFlightDate.Text = m_le.Date.ToShortDateString();
            lblFlightDesc.Text = String.Format(CultureInfo.CurrentCulture, "{0} {1} {2}", m_le.TailNumDisplay ?? string.Empty, m_le.Route, m_le.Comment);
        }

        UpdateChart();
    }

    protected string PathLatLongArrayID
    {
        get {return "rgll";}
    }

    #region chart management
    protected void UpdateChart()
    {
        if (m_fd.Data == null)
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

        foreach (DataRow dr in m_fd.Data.Rows)
        {
            gcData.XVals.Add(dr[cmbXAxis.SelectedValue]);

            if (!String.IsNullOrEmpty(cmbYAxis1.SelectedValue))
                gcData.YVals.Add(dr[cmbYAxis1.SelectedValue]);
            if (cmbYAxis2.SelectedValue.Length > 0 && cmbYAxis2.SelectedValue != cmbYAxis1.SelectedValue)
                gcData.Y2Vals.Add(dr[cmbYAxis2.SelectedValue]);
        }
        gcData.TickSpacing = 1; // Math.Max(1, m_fd.Data.Rows.Count / 20);

        // Clear out the grid.
        gvData.DataSource = null;
        gvData.DataBind();
        apcRaw.LazyLoad = true;

        int idx = mfbAccordionProxyExtender.IndexForProxyID(apcRaw.ID);
        mfbAccordionProxyExtender.SetJavascriptForControl(apcRaw, idx == AccordionCtrl.SelectedIndex, idx);
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

    protected void OnRowDatabound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            Image i = (Image) e.Row.FindControl("imgPin");
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
                        LatLong ll = (LatLong) drow[KnownColumnNames.POS];
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

    protected void btnDownload_Click(object sender, EventArgs e)
    {
        m_fd.AltitudeUnits = (FlightData.AltitudeUnitTypes)Convert.ToInt32(cmbAltUnits.SelectedValue, CultureInfo.InvariantCulture);
        m_fd.SpeedUnits = (FlightData.SpeedUnitTypes)Convert.ToInt32(cmbSpeedUnits.SelectedValue, CultureInfo.InvariantCulture);
        m_fd.FlightID = m_le.FlightID;

        DataSourceType dst = null;
        Action writeData = null;

        switch ((DownloadFormat) cmbFormat.SelectedIndex)
        {
            case DownloadFormat.Original:
                dst = DataSourceType.BestGuessTypeFromText(m_le.FlightData);
                writeData = () => { Response.Write(m_le.FlightData); };
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

    protected void apcRaw_ControlClicked(object sender, EventArgs e)
    {
        apcRaw.LazyLoad = false;
        int idx = mfbAccordionProxyExtender.IndexForProxyID(apcRaw.ID);
        mfbAccordionProxyExtender.SetJavascriptForControl(apcRaw, true, idx);
        AccordionCtrl.SelectedIndex = idx;

        LoadData(m_le);

        // see if we need to hide the "Position" column
        bool fHasPositionColumn = false;
        foreach (DataColumn dc in m_fd.Data.Columns)
        {
            if (dc.ColumnName.CompareOrdinalIgnoreCase(KnownColumnNames.POS) == 0)
                fHasPositionColumn = true;
        }
        if (!fHasPositionColumn)
            gvData.Columns.RemoveAt(1);

        gvData.DataSource = m_fd.Data;

        gvData.DataBind();
    }
}
