using MyFlightbook.Airports;
using MyFlightbook.Geography;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;

/******************************************************
 * 
 * Copyright (c) 2009-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Admin
{
    public partial class AdminAirportGeocoder : AdminPage
    {
        #region WebServices
        #region Geocoding
        [WebMethod]
        [ScriptMethod]
        public static string[] SuggestCountries(string prefixText, int count)
        {
            if (String.IsNullOrEmpty(prefixText))
                return Array.Empty<string>();
            List<string> lst = new List<string>();
            DBHelper dbh = new DBHelper("SELECT distinct Country from Airports where Country like ?prefix ORDER BY Country ASC LIMIT ?count");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("prefix", "%" + prefixText + "%"); comm.Parameters.AddWithValue("count", count); },
                (dr) => { lst.Add((string)dr["Country"]); });

            return lst.ToArray();
        }

        [WebMethod]
        [ScriptMethod]
        public static string[] SuggestAdmin(string prefixText, int count)
        {
            if (String.IsNullOrEmpty(prefixText))
                return Array.Empty<string>();
            List<string> lst = new List<string>();
            DBHelper dbh = new DBHelper("SELECT distinct Admin1 from Airports where Admin1 like ?prefix ORDER BY Admin1 ASC LIMIT ?count");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("prefix", "%" + prefixText + "%"); comm.Parameters.AddWithValue("count", count); },
                (dr) => { lst.Add((string)dr["Admin1"]); });

            return lst.ToArray();
        }
        #endregion

        private static void CheckAdmin()
        {
            if (HttpContext.Current == null || HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
                throw new UnauthorizedAccessException("You must be authenticated to make this call");

            Profile pf = Profile.GetUser(HttpContext.Current.User.Identity.Name);
            if (!pf.CanManageData)
                throw new UnauthorizedAccessException("You must be an admin to make this call");
        }

        #region Airport Import Functions
        [WebMethod]
        [ScriptMethod]
        public static void UseGuess(string szCode, string szTypeCode, string szCountry, string szAdmin)
        {
            CheckAdmin();
            if (String.IsNullOrEmpty(szCode))
                throw new ArgumentNullException(nameof(szCode));
            List<airport> lst = new List<airport>();
            lst.AddRange(airport.AirportsMatchingCodes(new string[] { szCode }));
            lst.RemoveAll(ap1 => ap1.FacilityTypeCode.CompareOrdinalIgnoreCase(szTypeCode) != 0);
            if (lst.Count != 1)
                return;
            lst[0].SetLocale(szCountry, szAdmin);
        }

        [WebMethod]
        [ScriptMethod]
        public static bool AirportImportCommand(airportImportCandidate aic, string source, string szCommand)
        {
            CheckAdmin();

            if (aic == null)
                throw new ArgumentNullException(nameof(aic));
            if (szCommand == null)
                throw new ArgumentNullException(nameof(szCommand));

            AirportImportRowCommand airc = (AirportImportRowCommand)Enum.Parse(typeof(AirportImportRowCommand), szCommand);
            AirportImportSource ais = (AirportImportSource)Enum.Parse(typeof(AirportImportSource), source);

            airport ap = null;
            switch (ais)
            {
                case AirportImportSource.FAA:
                    ap = aic.FAAMatch;
                    break;
                case AirportImportSource.ICAO:
                    ap = aic.ICAOMatch;
                    break;
                case AirportImportSource.IATA:
                    ap = aic.IATAMatch;
                    break;
            }

            switch (airc)
            {
                case AirportImportRowCommand.FixLocation:
                    ap.LatLong = aic.LatLong;
                    ap.FCommit(true, false);
                    if (!String.IsNullOrWhiteSpace(aic.Country))
                        ap.SetLocale(aic.Country, aic.Admin1);
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

                    switch (ais)
                    {
                        case AirportImportSource.FAA:
                            aic.Code = aic.FAA;
                            break;
                        case AirportImportSource.ICAO:
                            aic.Code = aic.ICAO;
                            break;
                        case AirportImportSource.IATA:
                            aic.Code = aic.IATA;
                            break;
                    }
                    aic.Code = Regex.Replace(aic.Code, "[^a-zA-Z0-9]", string.Empty);
                    aic.FCommit(true, true);
                    if (!String.IsNullOrWhiteSpace(aic.Country))
                        aic.SetLocale(aic.Country, aic.Admin1);
                    break;
            }
            return true;
        }
        #endregion

        #region Duplicate Management
        /// <summary>
        /// Deletes a user airport that matches a built-in airport
        /// </summary>
        /// <returns>0 if unknown.</returns>
        [WebMethod(EnableSession = true)]
        public static void DeleteDupeUserAirport(string idDelete, string idMap, string szUser, string szType)
        {
            CheckAdmin();
            AdminAirport.DeleteUserAirport(idDelete, idMap, szUser, szType);
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
        #endregion

        protected StringBuilder AuditString { get; set; }

        protected StringBuilder UpdateString { get; set; }

        private const string szVSItemsToEdit = "vsItemsToEdit";
        protected IEnumerable<AdminAirport> UnReferencedAirports
        {
            get
            {
                if (ViewState[szVSItemsToEdit] == null)
                    ViewState[szVSItemsToEdit] = new List<AdminAirport>();
                return (IEnumerable<AdminAirport>)ViewState[szVSItemsToEdit];
            }
            set { ViewState[szVSItemsToEdit] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Profile pf = Profile.GetUser(Page.User.Identity.Name);
            CheckAdmin(pf.CanManageData);
            mfbGoogleMapManager.Map.Options.fAutofillPanZoom = mfbGoogleMapManager.Map.Options.fAutofillHeliports = true;
        }

        protected void RefreshPending()
        {
            gvEdit.DataSource = UnReferencedAirports;
            gvEdit.DataBind();
        }

        protected void btnViewMore_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(txtCountry.Text) && String.IsNullOrWhiteSpace(txtAdmin.Text))
                ckNoCountry.Checked = ckNoAdmin1.Checked = true;

            int start = decStart.IntValue;
            IEnumerable<AdminAirport> lst = AdminAirport.AirportsMatchingGeoReference(ckNoCountry.Checked ? null : txtCountry.Text, ckNoAdmin1.Checked ? null : txtAdmin.Text, start, decMaxAirports.IntValue);

            int results = lst.Count();
            int last = start + results;
            lblCurrentRange.Text = String.Format(CultureInfo.CurrentCulture, "Showing results {0} through {1}", start, last);
            decStart.IntValue = (results < decMaxAirports.IntValue) ? 0 : last;

            mfbGoogleMapManager.Map.SetAirportList(new AirportList(lst));
            mfbGoogleMapManager.Map.Options.fAutofillPanZoom = ckNoCountry.Checked;

            UnReferencedAirports = lst;
            RefreshPending();
        }

        protected void GeoReferenceForPoly(List<LatLong> rgll, string szCountry, string szAdmin)
        {
            if (rgll == null)
                throw new ArgumentNullException(nameof(rgll));
            if (szCountry == null)
                throw new ArgumentNullException(nameof(szCountry));

            if (!rgll.Any())
                throw new InvalidOperationException("No latitude/longitudes in rgll!");


            // Get the bounding box.
            LatLongBox llb = new LatLongBox(rgll.First());
            foreach (LatLong ll in rgll)
                llb.ExpandToInclude(ll);

            IEnumerable<airport> rgap = AdminAirport.UntaggedAirportsInBox(llb);
            GeoRegion geo = new GeoRegion(string.Empty, rgll);

            int cAirports = 0;

            foreach (airport ap in rgap)
                if (geo.ContainsLocation(ap.LatLong))
                {
                    UpdateString.AppendLine(String.Format(CultureInfo.InvariantCulture, "UPDATE airports SET Country='{0}' {1} WHERE type='{2}' AND airportID='{3}';", szCountry, String.IsNullOrWhiteSpace(szAdmin) ? string.Empty : String.Format(CultureInfo.InvariantCulture, ", admin1 = '{0}' ", szAdmin), ap.FacilityTypeCode, ap.Code));
                    ap.SetLocale(szCountry, szAdmin);
                    cAirports++;
                }

            if (cAirports > 0)
                AuditString.AppendLine(String.Format(CultureInfo.CurrentCulture, "Updated {0} airports for country {1}, region {2}", cAirports, szCountry, szAdmin));
        }

        /// <summary>
        /// Reads a GPX file derived from a SHAPE file and tags what it finds.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnLocate_Click(object sender, EventArgs e)
        {
            if (!fuGPX.HasFile)
                return;

            AuditString = new StringBuilder();
            UpdateString = new StringBuilder();
            string szActiveCountry = string.Empty;
            string szActiveAdmin1 = string.Empty;
            string szName = string.Empty;
            string szVal = string.Empty;
            bool fCountry = false;
            LatLong llActive = null;
            List<LatLong> lstPoly = new List<LatLong>();

            using (XmlReader reader = XmlReader.Create(fuGPX.FileContent)) 
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.Name.ToUpperInvariant())
                            {
                                default:
                                    break;
                                case "TRKPT":
                                    llActive = new LatLong(Convert.ToDouble(reader.GetAttribute("lat"), CultureInfo.InvariantCulture), Convert.ToDouble(reader.GetAttribute("lon"), CultureInfo.InvariantCulture));
                                    break;
                                case "TRKSEG":
                                    // We should have all of the metadata
                                    if (fCountry)
                                    {
                                        szActiveCountry = szName;
                                        szActiveAdmin1 = null;
                                    }
                                    else
                                    {
                                        szActiveAdmin1 = szName;
                                        // szActiveCountry should already be set.
                                    }
                                    lstPoly.Clear();
                                    break;
                            }
                            break;
                        case XmlNodeType.Text:
                            szVal = reader.Value;
                            break;
                        case XmlNodeType.EndElement:
                            switch (reader.Name.ToUpperInvariant())
                            {
                                default:
                                    break;
                                case "NAME":
                                    szName = szVal; // Could be a country or an admin region.
                                    break;
                                case "OGR:ADMIN":
                                    szActiveCountry = szVal;    // always the country, in our case
                                    break;
                                case "TRKPT":
                                    lstPoly.Add(llActive);
                                    llActive = null;
                                    break;
                                case "OGR:FEATURECLA":
                                    fCountry = szVal.StartsWith("Admin-0", StringComparison.CurrentCultureIgnoreCase);
                                    break;
                                case "TRKSEG":
                                    // We've completed one polygon for this region - georeference it
                                    GeoReferenceForPoly(lstPoly, szActiveCountry, szActiveAdmin1);
                                    break;
                                case "TRK":
                                    // We're done with this region
                                    szName = szActiveAdmin1 = szActiveAdmin1 = string.Empty;
                                    fCountry = false;
                                    lstPoly.Clear();
                                    break;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            lblAudit.Text = AuditString.ToString();
            lblCommands.Text = UpdateString.ToString();
        }

        protected void gvEdit_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                HyperLink l = (HyperLink)e.Row.FindControl("lnkZoomCode");
                airport ap = (airport)e.Row.DataItem;
                l.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:clickAndZoom(new google.maps.LatLng({0}, {1}));", ap.LatLong.Latitude, ap.LatLong.Longitude);

                if (!String.IsNullOrWhiteSpace(ap.Country))
                    e.Row.CssClass = "handled";

                // If this airport has a country and admin1 assigned, use that by default (save the DB hit)
                // Otherwise, find nearby airports and "guess" the same location as the closest, as a hint.
                IEnumerable<airport> rgap = (String.IsNullOrWhiteSpace(ap.Country) || String.IsNullOrEmpty(ap.Admin1)) ?
                    airport.AirportsNearPosition(ap.LatLong.Latitude, ap.LatLong.Longitude, 10, true) :
                    new airport[] { ap };

                foreach (airport apClosest in rgap)
                {
                    if (!String.IsNullOrEmpty(apClosest.Country))
                    {
                        Label lCountryHint = (Label)e.Row.FindControl("lblCountryGuess");
                        Label lAdminHint = (Label)e.Row.FindControl("lblAdmin1Guess");
                        lCountryHint.Text = apClosest.Country;
                        lAdminHint.Text = apClosest.Admin1 ?? string.Empty;
                        l = (HyperLink)e.Row.FindControl("lnkUseGuess");
                        l.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:useSuggestion('{0}', '{1}', '{2}', '{3}');",
                            ap.Code, ap.FacilityTypeCode, lCountryHint.ClientID, lAdminHint.ClientID);
                        break;
                    }
                }
            }
        }

        protected void gvEdit_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            if (e != null)
                gvEdit.PageIndex = e.NewPageIndex;

            RefreshPending();
        }

        protected void gvEdit_RowEditing(object sender, GridViewEditEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            gvEdit.EditIndex = e.NewEditIndex;
            RefreshPending();
        }

        protected void gvEdit_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            string szCode = (string)e.Keys["Code"];
            string szType = (string)e.Keys["FacilityTypeCode"];

            string szCountry = (string)e.NewValues["Country"];
            string szAdmin = (string)e.NewValues["Admin1"];

            if (String.IsNullOrWhiteSpace(szCountry) && !String.IsNullOrWhiteSpace(szAdmin))
            {
                lerr.Text = "Can't specify admin without country.";
                return;
            }

            AdminAirport ap = UnReferencedAirports.FirstOrDefault((ap2) => ap2.Code.CompareOrdinal(szCode) == 0 && ap2.FacilityTypeCode.CompareOrdinal(szType) == 0);
            ap.SetLocale(szCountry, szAdmin);
            gvEdit.EditIndex = -1;
            RefreshPending();
        }

        protected void gvEdit_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            gvEdit.EditIndex = -1;
            RefreshPending();
        }
    }
}