using MyFlightbook.Airports;
using MyFlightbook.Geography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2010-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Mapping
{
    public partial class EditAirports : Page
    {
        #region Admin/Import utilities
        private const double maxDistanceToFix = 50.0;

        private static void SetUpAddButtons(Control row, airportImportCandidate aic, bool fAllowBlast)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));
            if (aic == null)
                throw new ArgumentNullException(nameof(aic));

            row.FindControl("btnAddFAA").Visible = !String.IsNullOrEmpty(aic.FAA) && (fAllowBlast || aic.FAAMatch == null);
            row.FindControl("btnAddIATA").Visible = !String.IsNullOrEmpty(aic.IATA) && (fAllowBlast || aic.IATAMatch == null);
            row.FindControl("btnAddICAO").Visible = !String.IsNullOrEmpty(aic.ICAO) && (fAllowBlast || aic.ICAOMatch == null);
        }

        private static void SetUpLocationButtons(Control row, airportImportCandidate aic)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));
            if (aic == null)
                throw new ArgumentNullException(nameof(aic));

            // don't offer to fix distances over maxDistanceToFix, to avoid accidentally stomping on airports somewhere else in the world.
            row.FindControl("btnFixLocationFAA").Visible = (aic.FAAMatch != null && aic.MatchStatusFAA == airportImportCandidate.MatchStatus.InDBWrongLocation && aic.DistanceFromAirport(aic.FAAMatch) < maxDistanceToFix);
            row.FindControl("btnFixLocationIATA").Visible = (aic.IATAMatch != null && aic.MatchStatusIATA == airportImportCandidate.MatchStatus.InDBWrongLocation && aic.DistanceFromAirport(aic.IATAMatch) < maxDistanceToFix);
            row.FindControl("btnFixLocationICAO").Visible = (aic.ICAOMatch != null && aic.MatchStatusICAO == airportImportCandidate.MatchStatus.InDBWrongLocation && aic.DistanceFromAirport(aic.ICAOMatch) < maxDistanceToFix);
        }

        private static void SetUpFixButtons(Control row, airportImportCandidate aic)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));
            if (aic == null)
                throw new ArgumentNullException(nameof(aic));

            // And don't offer to fix type if that's not the error
            row.FindControl("btnFixTypeFAA").Visible = (aic.FAAMatch != null && aic.MatchStatusFAA == airportImportCandidate.MatchStatus.WrongType && aic.DistanceFromAirport(aic.FAAMatch) < maxDistanceToFix);
            row.FindControl("btnFixTypeIATA").Visible = (aic.IATAMatch != null && aic.MatchStatusIATA == airportImportCandidate.MatchStatus.WrongType && aic.DistanceFromAirport(aic.IATAMatch) < maxDistanceToFix);
            row.FindControl("btnFixTypeICAO").Visible = (aic.ICAOMatch != null && aic.MatchStatusICAO == airportImportCandidate.MatchStatus.WrongType && aic.DistanceFromAirport(aic.ICAOMatch) < maxDistanceToFix);

            // And don't offer to fix type if that's not the error
            row.FindControl("btnOverwriteFAA").Visible = aic.FAAMatch != null && (aic.MatchStatusFAA == airportImportCandidate.MatchStatus.WrongType || aic.DistanceFromAirport(aic.FAAMatch) >= maxDistanceToFix);
            row.FindControl("btnOverwriteIATA").Visible = aic.IATAMatch != null && (aic.MatchStatusIATA == airportImportCandidate.MatchStatus.WrongType || aic.DistanceFromAirport(aic.IATAMatch) >= maxDistanceToFix);
            row.FindControl("btnOverwriteICAO").Visible = aic.ICAOMatch != null && (aic.MatchStatusICAO == airportImportCandidate.MatchStatus.WrongType || aic.DistanceFromAirport(aic.ICAOMatch) >= maxDistanceToFix);
        }

        protected static void SetUpImportButtons(Control row, airportImportCandidate aic, bool fAllowBlast)
        {
            SetUpAddButtons(row, aic, fAllowBlast);
            SetUpLocationButtons(row, aic);
            SetUpFixButtons(row, aic);
        }

        protected static void UpdateCandidateStatus(List<airportImportCandidate> lst)
        {
            if (lst == null)
                throw new ArgumentNullException(nameof(lst));
            StringBuilder sbCodes = new StringBuilder();

            lst.ForEach((aic) =>
            {
                sbCodes.AppendFormat(CultureInfo.InvariantCulture, " {0} ", aic.FAA);
                sbCodes.AppendFormat(CultureInfo.InvariantCulture, " {0} ", aic.IATA);
                sbCodes.AppendFormat(CultureInfo.InvariantCulture, " {0} ", aic.ICAO);
            });
            AirportList al = new AirportList(sbCodes.ToString());
            lst.ForEach((aic) => { aic.CheckStatus(al); });
        }
        #endregion

        private IEnumerable<airport> m_rgAirportsForUser;

        protected void Page_Load(object sender, EventArgs e)
        {
            this.Master.SelectedTab = tabID.mptAddAirports;
            Title = Resources.Airports.EditAirportsTitle;

            if (!IsPostBack)
                initForm();
            else
            {
                if (txtLat.Text.Length > 0 && txtLong.Text.Length > 0)
                {
                    if (double.TryParse(txtLat.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                        double.TryParse(txtLong.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                    {
                        MfbGoogleMapManager1.Map.Options.MapCenter = new LatLong(lat, lon);
                        MfbGoogleMapManager1.Map.Options.ZoomFactor = GMap_ZoomLevels.AirportAndVicinity;
                    }
                }
            }

            if (IsAdmin)
                ScriptManager.GetCurrent(this).AsyncPostBackTimeout = 1500;  // use a long timeout

            util.SetValidationGroup(pnlEdit, "EditAirport");

            MfbGoogleMapManager1.Map.ClickHandler = "function (point) {clickForAirport(point.latLng);}";
            MfbGoogleMapManager1.Map.SetAirportList(new AirportList(String.Empty));

            RefreshMyAirports();
        }
                
        protected bool IsAdmin
        {
            get { return util.GetStringParam(Request, "a").Length > 0 && Page.User.Identity.IsAuthenticated && (Profile.GetUser(Page.User.Identity.Name).CanManageData); }
        }

        protected void RefreshMyAirports()
        {
            bool fAdmin = IsAdmin;

            m_rgAirportsForUser = airport.AirportsForUser(Page.User.Identity.Name, fAdmin);

            if (fAdmin)
                Master.SelectedTab = tabID.admAirports;

            // show the last column (username) if admin mode
            gvMyAirports.Columns[gvMyAirports.Columns.Count - 1].Visible = fAdmin;

            if (!m_rgAirportsForUser.Any())
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
                throw new ArgumentNullException(nameof(e));
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                HyperLink l = (HyperLink)e.Row.FindControl("lnkZoomCode");
                airport ap = (airport)e.Row.DataItem;
                l.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:updateForAirport({0});", JsonConvert.SerializeObject(ap, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore }));
            }
        }

        protected void gvMyAirports_RowCommand(Object sender, CommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (String.Compare(e.CommandName, "_Delete", StringComparison.OrdinalIgnoreCase) == 0)
            {
                foreach (airport ap in m_rgAirportsForUser)
                    if (String.Compare(ap.Code, e.CommandArgument.ToString(), StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (ap.FDelete())
                            RefreshMyAirports();
                        else
                            lblErr.Text = ap.ErrorText;
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

        protected void AddAirport(bool forceAdd)
        {
            Page.Validate("EditAirport");
            if (!Page.IsValid)
                return;
            bool fValidLatLon = double.TryParse(txtLat.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat);
            fValidLatLon = double.TryParse(txtLong.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon) && fValidLatLon;
            if (!fValidLatLon)
            {
                lblErr.Text = Resources.Airports.errInvalidLatLong;
            }

            bool fAdmin = (IsAdmin && ckAsAdmin.Checked);
            airport ap = new airport(txtCode.Text.ToUpper(CultureInfo.InvariantCulture), txtName.Text, lat, lon, cmbType.SelectedValue, cmbType.SelectedItem.Text, 0.0, fAdmin ? string.Empty : Page.User.Identity.Name);

            if (fAdmin && ap.Code.CompareOrdinalIgnoreCase("TBD") == 0)
            {
                lblErr.Text = Resources.Airports.errTBDIsInvalidCode;
            }

            lblErr.Text = string.Empty;

            // Check to see if this looks like a duplicate
            List<airport> lstDupes = ap.IsPort ? new List<airport>(airport.AirportsNearPosition(ap.LatLong.Latitude, ap.LatLong.Longitude, 20, ap.FacilityTypeCode.CompareCurrentCultureIgnoreCase("H") == 0)) : new List<airport>();
            lstDupes.RemoveAll(a => !a.IsPort || a.Code.CompareCurrentCultureIgnoreCase(ap.Code) == 0 || a.DistanceFromPosition > 3);
            if (lstDupes.Any() && !forceAdd)
            {
                gvUserDupes.DataSource = lstDupes;
                gvUserDupes.DataBind();
                pnlDupeAirport.Visible = true;  // cause jQueryUI to show the dialog.
                return;
            }

            if (ap.FCommit(fAdmin, fAdmin))
            {
                if (lstDupes.Any())
                {
                    // needs review
                    StringBuilder sb = new StringBuilder();
                    StringBuilder sbViewLink = new StringBuilder();
                    sbViewLink.AppendFormat(CultureInfo.InvariantCulture, "https://{0}{1}?Airports={2}", Branding.CurrentBrand.HostName, ResolveUrl("~/mvc/Airport/MapRoute"), ap.Code); 
                    sb.AppendFormat(CultureInfo.CurrentCulture, "User: {0}, Airport: {1} ({2}) {3} {4}\r\n\r\nCould match:\r\n", ap.UserName, ap.Code, ap.FacilityTypeCode, ap.Name, ap.LatLong.ToDegMinSecString());
                    foreach (airport a in lstDupes)
                    {
                        sb.AppendFormat(CultureInfo.CurrentCulture, "{0} - ({1}) {2} {3}\r\n", a.Code, a.FacilityTypeCode, a.Name, a.LatLong.ToDegMinSecString());
                        sbViewLink.AppendFormat(CultureInfo.InvariantCulture, "+{0}", a.Code);
                    }
                    sb.AppendFormat(CultureInfo.InvariantCulture, "\r\n\r\n{0}\r\n", sbViewLink.ToString());
                    util.NotifyAdminEvent("New airport created - needs review", sb.ToString(), ProfileRoles.maskCanManageData);
                }

                initForm();

                if (!fAdmin)
                    RefreshMyAirports();

                if (fAdmin)
                    UpdateImportData();
            }
            else
                lblErr.Text = HttpUtility.HtmlEncode(ap.ErrorText);
        }

        protected void btnAddAnyway_Click(object sender, EventArgs e)
        {
            pnlDupeAirport.Visible = false;
            AddAirport(true);
        }

        protected void btnAdd_Click(object sender, EventArgs e)
        {
            AddAirport(false);
        }

        #region Admin - BulkAirportImport
        private const string szVSKeyListToImport = "keyListOfAirportCandidates";
        private List<airportImportCandidate> ImportedAirportCandidates
        {
            get { return (List<airportImportCandidate>)(ViewState[szVSKeyListToImport] ?? (ViewState[szVSKeyListToImport] = new List<airportImportCandidate>())); }
        }

        protected void btnImport_Click(object sender, EventArgs e)
        {
            if (!fileUploadAirportList.HasFile)
                return;

            // Don't show the user airports for page size
            pnlMyAirports.Visible = false;
            gvMyAirports.DataSource = null;
            gvMyAirports.DataBind();

            List<airportImportCandidate> lst = ImportedAirportCandidates;
            lst.Clear();

            try
            {
                lst.AddRange(airportImportCandidate.Candidates(fileUploadAirportList.FileContent));
            }
            catch (Exception ex) when (ex is MyFlightbookException)
            {
                lblUploadErr.Text = HttpUtility.HtmlEncode(ex.Message);
                return;
            }

            UpdateCandidateStatus(lst);

            bool fHideKHack = util.GetIntParam(Request, "khack", 0) == 0;
            lst.RemoveAll(aic => aic.IsOK || (fHideKHack && aic.IsKHack));

            gvImportResults.DataSource = lst;
            gvImportResults.DataBind();
            pnlImportResults.Visible = true;
        }

        protected void btnBulkImport_Click(object sender, EventArgs e)
        {
            if (!fileUploadAirportList.HasFile)
                return;

            try
            {
                int cAirportsAdded = AdminAirport.BulkImportAirports(fileUploadAirportList.FileContent);
                lblBulkImportResults.Text = String.Format(CultureInfo.CurrentCulture, "{0} airports added", cAirportsAdded);
            }
            catch (MyFlightbookException ex)
            {
                lblUploadErr.Text = HttpUtility.HtmlEncode(ex.Message);
            }
        }

        #region per-row variables for import
        protected string ImportVarNameForIndex(int index)
        {
            return String.Format(CultureInfo.InvariantCulture, "aic{0}", index);
        }

        protected string ContextVarNameForIndex(int index)
        {
            return String.Format(CultureInfo.InvariantCulture, "impctxt{0}", index);
        }

        protected string AjaxCallForIndex(int index, string fName)
        {
            return String.Format(CultureInfo.InvariantCulture, "return {0}(this, {1}, {2});", fName, ImportVarNameForIndex(index), ContextVarNameForIndex(index));
        }
        #endregion

        protected void gvImportResults_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                airportImportCandidate aic = (airportImportCandidate)e.Row.DataItem;

                Dictionary<string, string> dContext = new Dictionary<string, string>() {
                    { "useMapCheckID" , e.Row.FindControl("ckUseMap").ClientID },
                    { "lat" , txtLat.ClientID },
                    { "lon", txtLong.ClientID }
                };
                
                Literal litRowContext = (Literal)e.Row.FindControl("litRowContext");
                litRowContext.Text = String.Format(CultureInfo.InvariantCulture, @"<script type=""text/javascript""> var {0} = {1}; var {2} = {3}; </script>", ImportVarNameForIndex(e.Row.DataItemIndex), JsonConvert.SerializeObject(aic), ContextVarNameForIndex(e.Row.DataItemIndex), JsonConvert.SerializeObject(dContext));

                Label lblProposed = (Label)e.Row.FindControl("lblProposed");
                StringBuilder sb = new StringBuilder();
                if (!String.IsNullOrEmpty(aic.FAA))
                    sb.AppendFormat(CultureInfo.InvariantCulture, "FAA: {0}<br />", aic.FAA);
                if (!String.IsNullOrEmpty(aic.IATA))
                    sb.AppendFormat(CultureInfo.InvariantCulture, "IATA: {0}<br />", aic.IATA);
                if (!String.IsNullOrEmpty(aic.ICAO))
                    sb.AppendFormat(CultureInfo.InvariantCulture, "ICAO: {0}<br />", aic.ICAO);
                lblProposed.Text = sb.ToString();

                airportImportCandidate.PopulateAirport(e.Row.FindControl("plcFAAMatch"), aic.FAAMatch, aic.MatchStatusFAA, aic);
                airportImportCandidate.PopulateAirport(e.Row.FindControl("plcIATAMatch"), aic.IATAMatch, aic.MatchStatusIATA, aic);
                airportImportCandidate.PopulateAirport(e.Row.FindControl("plcICAOMatch"), aic.ICAOMatch, aic.MatchStatusICAO, aic);
                airportImportCandidate.PopulateAirport(e.Row.FindControl("plcAirportProposed"), aic, airportImportCandidate.MatchStatus.NotApplicable, null);

                SetUpImportButtons(e.Row, aic, util.GetIntParam(Request, "blast", 0) != 0);

                if (aic.IsOK)
                    e.Row.BackColor = System.Drawing.Color.LightGreen;
            }
        }

        protected void UpdateImportData()
        {
            // Now update the grid to reflect the changes.  We leave OK rows in this time.
            UpdateCandidateStatus(ImportedAirportCandidates);
            gvImportResults.DataSource = ImportedAirportCandidates;
            gvImportResults.DataBind();
        }
        #endregion
    }
}