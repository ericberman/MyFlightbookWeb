using JouniHeikniemi.Tools.Text;
using MyFlightbook;
using MyFlightbook.Airports;
using MyFlightbook.Geography;
using MyFlightbook.Mapping;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2010-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_EditAirports : System.Web.UI.Page
{
    #region Webservices
    private static void CheckAdmin()
    {
        if (HttpContext.Current == null || HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
            throw new UnauthorizedAccessException("You must be authenticated to make this call");

        Profile pf = MyFlightbook.Profile.GetUser(HttpContext.Current.User.Identity.Name);
        if (!pf.CanManageData)
            throw new UnauthorizedAccessException("You must be an admin to make this call");
    }

    /// <summary>
    /// Deletes a user airport that matches a built-in airport
    /// </summary>
    /// <returns>0 if unknown.</returns>
    [WebMethod(EnableSession = true)]
    public static void DeleteDupeUserAirport(string idDelete, string idMap, string szUser, string szType)
    {
        CheckAdmin();

        airport apToDelete = new airport(idDelete, "(None)", 0, 0, szType, string.Empty, 0, szUser);

        if (apToDelete.FDelete(true))
        {
            if (!String.IsNullOrEmpty(szUser))
            {
                DBHelper dbh = new DBHelper("UPDATE flights SET route=REPLACE(route, ?idDelete, ?idMap) WHERE username=?user AND route LIKE CONCAT('%', ?idDelete, '%')");
                if (!dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("idDelete", idDelete);
                    comm.Parameters.AddWithValue("idMap", idMap);
                    comm.Parameters.AddWithValue("user", szUser);
                }))
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Error mapping from {0} to {1} in flights for user {2}: {3}", idDelete, idMap, szUser, dbh.LastError));
            }
        }
        else
            throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Error deleting airport {0}: {1}", apToDelete.Code, apToDelete.ErrorText));
    }

    /// <summary>
    /// Sets the preferred flag for an airport
    /// </summary>
    [WebMethod(EnableSession = true)]
    public static void SetPreferred(string szCode, string szType, bool fPreferred)
    {
        CheckAdmin();

        AdminAirport ap = AdminAirport.AirportWithCodeAndType(szCode, szType);
        if (ap == null)
            throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Airport {0} (type {1}) not found", szCode, szType));

        ap.SetPreferred(fPreferred);
    }

    /// <summary>
    /// Makes a user-defined airport native (i.e., eliminates the source username; accepted as a "true" airport)
    /// </summary>
    [WebMethod(EnableSession = true)]
    public static void MakeNative(string szCode, string szType)
    {
        CheckAdmin();

        AdminAirport ap = AdminAirport.AirportWithCodeAndType(szCode, szType);
        if (ap == null)
            throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Airport {0} (type {1}) not found", szCode, szType));

        ap.MakeNative();
    }

    /// <summary>
    /// Copies the latitude/longitude from the source airport to the target airport.
    /// </summary>
    [WebMethod(EnableSession = true)]
    public static void MergeWith(string szCodeTarget, string szTypeTarget, string szCodeSource)
    {
        CheckAdmin();

        AdminAirport apTarget = AdminAirport.AirportWithCodeAndType(szCodeTarget, szTypeTarget);
        if (apTarget == null)
            throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Target Airport {0} (type {1}) not found", szCodeTarget, szTypeTarget));

        AdminAirport apSource = AdminAirport.AirportWithCodeAndType(szCodeSource, szTypeTarget);
        if (apSource == null)
            throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Source Airport {0} (type {1}) not found", szCodeSource, szTypeTarget));

        apTarget.MergeFrom(apSource);
    }
    #endregion


    private airport[] m_rgAirportsForUser = null;

    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.SelectedTab = tabID.mptAddAirports;
        Title = (string)GetLocalResourceObject("PageResource2.Title");

        if (!IsPostBack)
            initForm();
        else
        {
            if (txtLat.Text.Length > 0 && txtLong.Text.Length > 0)
            {
                double lat, lon;

                if (double.TryParse(txtLat.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out lat) &&
                    double.TryParse(txtLong.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out lon))
                {
                    MfbGoogleMapManager1.Map.MapCenter = new LatLong(lat, lon);
                    MfbGoogleMapManager1.Map.ZoomFactor = GMap_ZoomLevels.AirportAndVicinity;
                }
            }
        }

        if (IsAdmin)
        {
            Master.Page.Header.Controls.Add(new LiteralControl(@"<style>
.sidebarRight
{
    width: 1200px;
}
</style>"));
            ScriptManager.GetCurrent(this).AsyncPostBackTimeout = 1500;  // use a long timeout
        }

        util.SetValidationGroup(pnlEdit, "EditAirport");

        MfbGoogleMapManager1.Map.ClickHandler = "function (point) {clickForAirport(point.latLng);}";
        MfbGoogleMapManager1.Map.SetAirportList(new AirportList(""));
        
        StringBuilder sbInitVars = new StringBuilder();
        sbInitVars.AppendFormat(CultureInfo.InvariantCulture, "var elCode = document.getElementById('{0}');\r\n", txtCode.ClientID);
        sbInitVars.AppendFormat(CultureInfo.InvariantCulture, "var elName = document.getElementById('{0}');\r\n", txtName.ClientID);
        sbInitVars.AppendFormat(CultureInfo.InvariantCulture, "var elNameWE = '{0}';\r\n", wmeName.ClientID);
        sbInitVars.AppendFormat(CultureInfo.InvariantCulture, "var elCodeWE = '{0}';\r\n", wmeCode.ClientID);
        sbInitVars.AppendFormat(CultureInfo.InvariantCulture, "var elType = document.getElementById('{0}');\r\n", cmbType.ClientID);
        sbInitVars.AppendFormat(CultureInfo.InvariantCulture, "var elLat = document.getElementById('{0}');\r\n", txtLat.ClientID);
        sbInitVars.AppendFormat(CultureInfo.InvariantCulture, "var elLon = document.getElementById('{0}');\r\n", txtLong.ClientID);
        sbInitVars.Append("$(document).ready(function() {centerToText();})\r\n");

        Page.ClientScript.RegisterStartupScript(GetType(), "CenterLatLong", sbInitVars.ToString(), true);

        txtLat.Attributes["onChange"] = "javascript:centerToText();";
        txtLong.Attributes["onChange"] = "javascript:centerToText();";
        cmbType.Attributes["onChange"] = "javascript:centerToText();";

        RefreshMyAirports();
    }

    protected string CacheKeyUserAirports
    {
        get { return "AirportsForUser" + Page.User.Identity.Name; }
    }

    protected bool IsAdmin
    {
        get { return util.GetStringParam(Request, "a").Length > 0 && Page.User.Identity.IsAuthenticated && (MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanManageData); }
    }

    protected void RefreshMyAirports()
    {
        Boolean fAdmin = IsAdmin;

        if (!IsPostBack || ((m_rgAirportsForUser = (airport[])Cache[CacheKeyUserAirports]) == null))
            Cache[CacheKeyUserAirports] = m_rgAirportsForUser = airport.AirportsForUser(Page.User.Identity.Name, fAdmin).ToArray();

        if (fAdmin)
            this.Master.SelectedTab = tabID.admAirports;

        // show the last column (username) if admin mode
        gvMyAirports.Columns[gvMyAirports.Columns.Count - 1].Visible = fAdmin;

        if (m_rgAirportsForUser.Length == 0)
            pnlMyAirports.Visible = false;
        else
        {
            pnlMyAirports.Visible = true;
            gvMyAirports.DataSource = m_rgAirportsForUser;
            gvMyAirports.DataBind();
            if (m_rgAirportsForUser.Count() > 30)
            {
                pnlMyAirports.ScrollBars = ScrollBars.Vertical;
                pnlMyAirports.Height = Unit.Pixel(400);
                pnlMyAirports.Width = Unit.Pixel(600);
            }
        }
    }

    protected void gvMyAirports_RowDataBound(Object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            HyperLink l = (HyperLink) e.Row.FindControl("lnkZoomCode");
            airport ap = (airport) e.Row.DataItem;
            l.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:updateForAirport('{0}', '{1}', '{2}', {3}, {4});", ap.Code, ap.Name.Replace("'", "\\'").Replace("\"", "\\\""), ap.FacilityTypeCode, ap.LatLong.LatitudeString, ap.LatLong.LongitudeString);
        }
    }

    protected void gvMyAirports_RowCommand(Object sender, CommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (String.Compare(e.CommandName, "_Delete", StringComparison.OrdinalIgnoreCase) == 0)
        {
            foreach (airport ap in m_rgAirportsForUser)
                if (String.Compare(ap.Code, e.CommandArgument.ToString(), StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (ap.FDelete())
                    {
                        Cache.Remove(CacheKeyUserAirports);
                        RefreshMyAirports();
                    }
                    else
                    {
                        lblErr.Text = ap.ErrorText;
                    }
                }
        }
    }

    protected void initForm()
    {
        txtCode.Text = txtName.Text = string.Empty;
        txtLat.Text = txtLong.Text = string.Empty;
        cmbType.SelectedIndex = 0;

        pnlAdminImport.Visible = rowAdmin.Visible = IsAdmin;
    }

    protected void btnAdd_Click(object sender, EventArgs e)
    {
        double lat, lon;

        if (!double.TryParse(txtLat.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out lat) ||
            !double.TryParse(txtLong.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out lon))
        {
            lblErr.Text = Resources.Airports.errInvalidLatLong;
            return;
        }

        bool fAdmin = (IsAdmin && ckAsAdmin.Checked);
        airport ap = new airport(txtCode.Text.ToUpper(CultureInfo.InvariantCulture), txtName.Text, lat, lon, cmbType.SelectedValue, cmbType.SelectedItem.Text, 0.0, fAdmin ? string.Empty : Page.User.Identity.Name);

        if (fAdmin && ap.Code.CompareOrdinalIgnoreCase("TBD") == 0)
        {
            lblErr.Text = Resources.Airports.errTBDIsInvalidCode;
            return;
        }

        lblErr.Text = "";

        if (ap.FCommit(fAdmin, fAdmin))
        {
            // Check to see if this looks like a duplicate - if so, submit it for review
            List<airport> lstDupes = new List<airport>(airport.AirportsNearPosition(ap.LatLong.Latitude, ap.LatLong.Longitude, 20, ap.FacilityTypeCode.CompareCurrentCultureIgnoreCase("H") == 0));
            lstDupes.RemoveAll(a => !a.IsPort || a.Code.CompareCurrentCultureIgnoreCase(ap.Code) == 0 || a.DistanceFromPosition > 3);
            if (lstDupes.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.CurrentCulture, "User: {0}, Airport: {1} ({2}) {3} {4}\r\n\r\nCould match:\r\n", ap.UserName, ap.Code, ap.FacilityTypeCode, ap.Name, ap.LatLong.ToDegMinSecString());
                foreach (airport a in lstDupes)
                    sb.AppendFormat(CultureInfo.CurrentCulture, "{0} - ({1}) {2} {3}\r\n", a.Code, a.FacilityTypeCode, a.Name, a.LatLong.ToDegMinSecString());
                util.NotifyAdminEvent("New airport created - needs review", sb.ToString(), ProfileRoles.maskCanManageData);
            }

            initForm();
            Cache.Remove(CacheKeyUserAirports);

            if (!fAdmin || ckShowAllUserAirports.Checked)
                RefreshMyAirports();
            else
            {
                gvMyAirports.DataSource = null;
                gvMyAirports.DataBind();
            }

            if (fAdmin)
                UpdateImportData();
        }
        else
            lblErr.Text = ap.ErrorText;
    }

    #region Admin - BulkAirportImport
    private const string szVSKeyListToImport = "keyListOfAirportCandidates";
    private List<airportImportCandidate> ImportedAirportCandidates
    {
        get { return (List<airportImportCandidate>) (ViewState[szVSKeyListToImport] ?? (ViewState[szVSKeyListToImport] = new List<airportImportCandidate>()));}
    }

    private string GetCol(string[] rgsz, int icol)
    {
        if (icol < 0 || icol > rgsz.Length)
            return string.Empty;
        return rgsz[icol];
    }

    protected void UpdateCandidateStatus(List<airportImportCandidate> lst)
    {
        StringBuilder sbCodes = new StringBuilder();

        lst.ForEach((aic) =>
            {
                sbCodes.AppendFormat(" {0} ", aic.FAA);
                sbCodes.AppendFormat(" {0} ", aic.IATA);
                sbCodes.AppendFormat(" {0} ", aic.ICAO);
            });
        AirportList al = new AirportList(sbCodes.ToString());
        lst.ForEach((aic) => { aic.CheckStatus(al); });
    }

    protected class ImportContext
    {
        public int iColFAA  { get; set; }
        public int iColICAO  { get; set; }
        public int iColIATA  { get; set; }
        public int iColName  { get; set; }
        public int iColLatitude  { get; set; }
        public int iColLongitude  { get; set; }
        public int iColLatLong  { get; set; }
        public int iColType  { get; set; }

        public ImportContext()
        {
            iColFAA = -1;
            iColICAO = -1;
            iColIATA = -1;
            iColName = -1;
            iColLatitude = -1;
            iColLongitude = -1;
            iColLatLong = -1;
            iColType = -1;
        }

        public void InitFromHeader(string[] rgCols)
        {
            if (rgCols == null)
                throw new ArgumentNullException("rgCols");

            for (int i = 0; i < rgCols.Length; i++)
            {
                string sz = rgCols[i];
                if (String.Compare(sz, "FAA", StringComparison.OrdinalIgnoreCase) == 0)
                    iColFAA = i;
                if (String.Compare(sz, "ICAO", StringComparison.OrdinalIgnoreCase) == 0)
                    iColICAO = i;
                if (String.Compare(sz, "IATA", StringComparison.OrdinalIgnoreCase) == 0)
                    iColIATA = i;
                if (String.Compare(sz, "Name", StringComparison.OrdinalIgnoreCase) == 0)
                    iColName = i;
                if (String.Compare(sz, "Latitude", StringComparison.OrdinalIgnoreCase) == 0)
                    iColLatitude = i;
                if (String.Compare(sz, "Longitude", StringComparison.OrdinalIgnoreCase) == 0)
                    iColLongitude = i;
                if (String.Compare(sz, "LatLong", StringComparison.OrdinalIgnoreCase) == 0)
                    iColLatLong = i;
                if (String.Compare(sz, "Type", StringComparison.OrdinalIgnoreCase) == 0)
                    iColType = i;
            }

            if (iColFAA == -1 && iColIATA == -1 && iColICAO == -1)
                throw new MyFlightbookException("No airportid codes found");
            if (iColName == -1)
                throw new MyFlightbookException("No name column found");
            if (iColLatLong == -1 && iColLatitude == -1 && iColLongitude == -1)
                throw new MyFlightbookException("No position found");
        }
    }

    protected void btnImport_Click(object sender, EventArgs e)
    {
        if (!fileUploadAirportList.HasFile)
            return;

        if (!ckShowAllUserAirports.Checked)
        {
            gvMyAirports.DataSource = null;
            gvMyAirports.DataBind();
        }

        ImportContext ic = new ImportContext();

        using (CSVReader csvReader = new CSVReader(fileUploadAirportList.FileContent))
        {
            try
            {
                ic.InitFromHeader(csvReader.GetCSVLine(true));
            }
            catch (CSVReaderInvalidCSVException ex)
            {
                lblUploadErr.Text = ex.Message;
                return;
            }
            catch (MyFlightbookException ex)
            {
                lblUploadErr.Text = ex.Message;
                return;
            }

            List<airportImportCandidate> lst = ImportedAirportCandidates;
            lst.Clear();
            string[] rgCols = null;

            while ((rgCols = csvReader.GetCSVLine()) != null)
            {
                airportImportCandidate aic = new airportImportCandidate()
                {
                    FAA = GetCol(rgCols, ic.iColFAA).Replace("-", ""),
                    IATA = GetCol(rgCols, ic.iColIATA).Replace("-", ""),
                    ICAO = GetCol(rgCols, ic.iColICAO).Replace("-", ""),
                    Name = GetCol(rgCols, ic.iColName),
                    FacilityTypeCode = GetCol(rgCols, ic.iColType)
                };
                if (String.IsNullOrEmpty(aic.FacilityTypeCode))
                    aic.FacilityTypeCode = "A";     // default to airport
                aic.Name = GetCol(rgCols, ic.iColName);
                aic.Code = "(TBD)";
                string szLat = GetCol(rgCols, ic.iColLatitude);
                string szLon = GetCol(rgCols, ic.iColLongitude);
                string szLatLong = GetCol(rgCols, ic.iColLatLong);
                aic.LatLong = null;

                if (!String.IsNullOrEmpty(szLatLong))
                {
                    // see if it is decimal; if so, we'll fall through.
                    if (Regex.IsMatch(szLatLong, "[NEWS]", RegexOptions.IgnoreCase))
                        aic.LatLong = DMSAngle.LatLonFromDMSString(GetCol(rgCols, ic.iColLatLong));
                    else
                    {
                        string[] rgsz = szLatLong.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (rgsz.Length == 2)
                        {
                            szLat = rgsz[0];
                            szLon = rgsz[1];
                        }
                    }
                }
                if (aic.LatLong == null)
                {
                    aic.LatLong = new LatLong();
                    double d;
                    if (double.TryParse(szLat, out d))
                        aic.LatLong.Latitude = d;
                    else
                        aic.LatLong.Latitude = new DMSAngle(szLat).Value;

                    if (double.TryParse(szLon, out d))
                        aic.LatLong.Longitude = d;
                    else
                        aic.LatLong.Longitude = new DMSAngle(szLon).Value;
                }

                lst.Add(aic);
            }

            UpdateCandidateStatus(lst);

            bool fHideKHack = util.GetIntParam(Request, "khack", 0) == 0;
            lst.RemoveAll(aic => aic.IsOK || (fHideKHack && aic.IsKHack));

            gvImportResults.DataSource = lst;
            gvImportResults.DataBind();
            pnlImportResults.Visible = true;
        }
    }

    private void PopulateAirport(Control plc, airport ap, airportImportCandidate.MatchStatus ms, airport aicBase)
    {
        if (ap == null)
            return;

        Panel p = new Panel();
        plc.Controls.Add(p);
        Label lbl = new Label();
        p.Controls.Add(lbl);
        lbl.Text = ap.ToString();
        p.Controls.Add(new LiteralControl("<br />"));
        if (!airportImportCandidate.StatusIsOK(ms))
        {
            p.BackColor = System.Drawing.Color.LightGray;
            Label lblStatus = new Label();
            p.Controls.Add(lblStatus);
            lblStatus.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Admin.ImportAirportStatusTemplate, ms.ToString());
            lblStatus.ForeColor = System.Drawing.Color.Red;
            if (aicBase != null && ap.LatLong != null && aicBase.LatLong != null)
            {
                Label lblDist = new Label();
                p.Controls.Add(lblDist);
                lblDist.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Admin.ImportAirportDistanceTemplate, aicBase.DistanceFromAirport(ap));
            }
        }
        HyperLink h = new HyperLink();
        p.Controls.Add(h);
        h.Text = ap.LatLong.ToDegMinSecString();
        h.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:updateForAirport('{0}', '{1}', '{2}', {3}, {4});", ap.Code, ap.Name.JavascriptEncode(), ap.FacilityTypeCode, ap.LatLong.Latitude, ap.LatLong.Longitude);
        if (!String.IsNullOrEmpty(ap.UserName))
            p.Controls.Add(new LiteralControl(String.Format(CultureInfo.InvariantCulture, "{0}<br />", ap.UserName)));
    }

    private void SetUpImportButtons(Control row, airportImportCandidate aic, bool fAllowBlast)
    {
        ((Button)row.FindControl("btnAddFAA")).Visible = !String.IsNullOrEmpty(aic.FAA) && (fAllowBlast || aic.FAAMatch == null);
        ((Button)row.FindControl("btnAddIATA")).Visible = !String.IsNullOrEmpty(aic.IATA) && (fAllowBlast || aic.IATAMatch == null);
        ((Button)row.FindControl("btnAddICAO")).Visible = !String.IsNullOrEmpty(aic.ICAO) && (fAllowBlast || aic.ICAOMatch == null);

        // don't offer to fix distances over maxDistanceToFix, to avoid accidentally stomping on airports somewhere else in the world.
        const double maxDistanceToFix = 50.0;
        ((Button)row.FindControl("btnFixLocationFAA")).Visible = (aic.FAAMatch != null && aic.MatchStatusFAA == airportImportCandidate.MatchStatus.InDBWrongLocation && aic.DistanceFromAirport(aic.FAAMatch) < maxDistanceToFix);
        ((Button)row.FindControl("btnFixLocationIATA")).Visible = (aic.IATAMatch != null && aic.MatchStatusIATA == airportImportCandidate.MatchStatus.InDBWrongLocation && aic.DistanceFromAirport(aic.IATAMatch) < maxDistanceToFix);
        ((Button)row.FindControl("btnFixLocationICAO")).Visible = (aic.ICAOMatch != null && aic.MatchStatusICAO == airportImportCandidate.MatchStatus.InDBWrongLocation && aic.DistanceFromAirport(aic.ICAOMatch) < maxDistanceToFix);

        // And don't offer to fix type if that's not the error
        ((Button)row.FindControl("btnFixTypeFAA")).Visible = (aic.FAAMatch != null && aic.MatchStatusFAA == airportImportCandidate.MatchStatus.WrongType && aic.DistanceFromAirport(aic.FAAMatch) < maxDistanceToFix);
        ((Button)row.FindControl("btnFixTypeIATA")).Visible = (aic.IATAMatch != null && aic.MatchStatusIATA == airportImportCandidate.MatchStatus.WrongType && aic.DistanceFromAirport(aic.IATAMatch) < maxDistanceToFix);
        ((Button)row.FindControl("btnFixTypeICAO")).Visible = (aic.ICAOMatch != null && aic.MatchStatusICAO == airportImportCandidate.MatchStatus.WrongType && aic.DistanceFromAirport(aic.ICAOMatch) < maxDistanceToFix);

        // And don't offer to fix type if that's not the error
        ((Button)row.FindControl("btnOverwriteFAA")).Visible = aic.FAAMatch != null && (aic.MatchStatusFAA == airportImportCandidate.MatchStatus.WrongType || aic.DistanceFromAirport(aic.FAAMatch) >= maxDistanceToFix);
        ((Button)row.FindControl("btnOverwriteIATA")).Visible = aic.IATAMatch != null && (aic.MatchStatusIATA == airportImportCandidate.MatchStatus.WrongType || aic.DistanceFromAirport(aic.IATAMatch) >= maxDistanceToFix);
        ((Button)row.FindControl("btnOverwriteICAO")).Visible = aic.ICAOMatch != null && (aic.MatchStatusICAO == airportImportCandidate.MatchStatus.WrongType || aic.DistanceFromAirport(aic.ICAOMatch) >= maxDistanceToFix);
    }

    protected void gvImportResults_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            airportImportCandidate aic = (airportImportCandidate) e.Row.DataItem;

            Label lblProposed = ((Label)e.Row.FindControl("lblProposed"));
            StringBuilder sb = new StringBuilder();
            if (!String.IsNullOrEmpty(aic.FAA))
                sb.AppendFormat(CultureInfo.InvariantCulture, "FAA: {0}<br />", aic.FAA);
            if (!String.IsNullOrEmpty(aic.IATA))
                sb.AppendFormat(CultureInfo.InvariantCulture, "IATA: {0}<br />", aic.IATA);
            if (!String.IsNullOrEmpty(aic.ICAO))
                sb.AppendFormat(CultureInfo.InvariantCulture, "ICAO: {0}<br />", aic.ICAO);
            lblProposed.Text = sb.ToString();

            PopulateAirport(e.Row.FindControl("plcFAAMatch"), aic.FAAMatch, aic.MatchStatusFAA, aic);
            PopulateAirport(e.Row.FindControl("plcIATAMatch"), aic.IATAMatch, aic.MatchStatusIATA, aic);
            PopulateAirport(e.Row.FindControl("plcICAOMatch"), aic.ICAOMatch, aic.MatchStatusICAO, aic);
            PopulateAirport(e.Row.FindControl("plcAirportProposed"), aic, airportImportCandidate.MatchStatus.NotApplicable, null);

            SetUpImportButtons(e.Row, aic, util.GetIntParam(Request, "blast", 0) != 0);

            if (aic.IsOK)
                e.Row.BackColor = System.Drawing.Color.LightGreen;
        }
    }

    protected enum AirportImportRowCommand { FixLocation, FixType, AddAirport, Overwrite};

    protected void gvImportResults_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        GridViewRow grow = (GridViewRow)((WebControl)e.CommandSource).NamingContainer;
        int iRow = grow.RowIndex;

        airportImportCandidate aic = ImportedAirportCandidates[iRow];

        AirportImportRowCommand airc = (AirportImportRowCommand) Enum.Parse(typeof(AirportImportRowCommand), e.CommandName);

        CheckBox ckUseMap = (CheckBox)grow.FindControl("ckUseMap");
        if (ckUseMap.Checked)
        {
            aic.LatLong.Latitude = Convert.ToDouble(txtLat.Text, System.Globalization.CultureInfo.InvariantCulture);
            aic.LatLong.Longitude = Convert.ToDouble(txtLong.Text, System.Globalization.CultureInfo.InvariantCulture);
        }

        airport ap = null;
        switch (e.CommandArgument.ToString())
        {
            case "FAA":
                ap = aic.FAAMatch;
                break;
            case "ICAO":
                ap = aic.ICAOMatch;
                break;
            case "IATA":
                ap = aic.IATAMatch;
                break;
        }

        switch (airc)
        {
            case AirportImportRowCommand.FixLocation:
                ap.LatLong = aic.LatLong;
                ap.FCommit(true, false);
                break;
            case AirportImportRowCommand.FixType:
                ap.FDelete(true);   // delete the existing one before we update - otherwise REPLACE INTO will not succeed (because we are changing the REPLACE INTO primary key, which includes Type)
                ap.FacilityTypeCode = aic.FacilityTypeCode;
                ap.FCommit(true, true); // force this to be treated as a new airport
                break;
            case AirportImportRowCommand.Overwrite:
            case AirportImportRowCommand.AddAirport:
                if (airc == AirportImportRowCommand.Overwrite)
                    ap.FDelete(true);   // delete the existing airport

                switch (e.CommandArgument.ToString())
                {
                    case "FAA":
                        aic.Code = aic.FAA;
                        break;
                    case "ICAO":
                        aic.Code = aic.ICAO;
                        break;
                    case "IATA":
                        aic.Code = aic.IATA;
                        break;
                }
                aic.Code = Regex.Replace(aic.Code, "[^a-zA-Z0-9]", string.Empty);
                aic.FCommit(true, true);
                break;
        }

        UpdateImportData();
    }

    protected void UpdateImportData()
    {
        // Now update the grid to reflect the changes.  We leave OK rows in this time.
        UpdateCandidateStatus(ImportedAirportCandidates);
        gvImportResults.DataSource = ImportedAirportCandidates;
        gvImportResults.DataBind();
    }
    #endregion

    #region ADMIN - dupe management
    protected void btnRefreshDupes_Click(object sender, EventArgs e)
    {
        gvDupes.DataSourceID = sqlDSUserDupes.ID;
        gvDupes.DataBind();
        pnlDupeAirports.Visible = true;
        pnlMyAirports.Visible = false;
    }
    protected string DeleteDupeScript(string user, string codeDelete, string codeMap, string type)
    { 
        return String.Format(CultureInfo.InvariantCulture, "deleteDupeUserAirport('{0}', '{1}', '{2}', '{3}', this); return false;", user, codeDelete, codeMap, type);
    }

    protected string SetPreferredScript(string szCode, string szType)
    {
        return String.Format(CultureInfo.InvariantCulture, "javascript:setPreferred('{0}', '{1}', this); return false;", szCode, szType);
    }

    protected string MakeNativeScript(string szCode, string szType)
    {
        return String.Format(CultureInfo.InvariantCulture, "javascript:makeNative('{0}', '{1}', this); return false; ", szCode, szType);
    }

    protected string MergeWithScript(string szCodeTarget, string szTypeTarget, string szCodeSrc)
    {
        return String.Format(CultureInfo.InvariantCulture, "javascript:mergeWith('{0}', '{1}', '{2}', this); return false;", szCodeTarget, szTypeTarget, szCodeSrc);
    }

    protected void sqlDSUserDupes_Selecting(object sender, SqlDataSourceSelectingEventArgs e)
    {
        if (e != null)
            e.Command.CommandTimeout = 600; // give up to 10 minutes - this can be slow.
    }
    #endregion
}
