using MyFlightbook.Airports;
using MyFlightbook.Geography;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;


/******************************************************
 * 
 * Copyright (c) 2009-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Admin
{
    public partial class AdminAirportGeocoder : AdminPage
    {
        #region WebServices
        [System.Web.Services.WebMethod]
        [System.Web.Script.Services.ScriptMethod]
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

        [System.Web.Services.WebMethod]
        [System.Web.Script.Services.ScriptMethod]
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

            foreach (AdminAirport ap in rgap)
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

        protected void gvEdit_RowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                HyperLink l = (HyperLink)e.Row.FindControl("lnkZoomCode");
                airport ap = (airport)e.Row.DataItem;
                l.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:clickAndZoom(new google.maps.LatLng({0}, {1}));", ap.LatLong.Latitude, ap.LatLong.Longitude);

                if (!String.IsNullOrWhiteSpace(ap.Country))
                    e.Row.BackColor = System.Drawing.Color.LightGray;
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