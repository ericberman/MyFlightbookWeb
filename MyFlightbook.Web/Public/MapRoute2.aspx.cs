using MyFlightbook.Airports;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Mapping
{
    public partial class MapRoute : Page
    {
        /// <summary>
        /// Determines if the calling referrer is authorized to call us without being signed in
        /// </summary>
        /// <returns></returns>
        private static bool IsValidUnauthedCaller()
        {
            if (HttpContext.Current == null || HttpContext.Current.Request == null)
                return false;
            if (HttpContext.Current.Request.IsLocal)
                return true;
            if (HttpContext.Current.Request.UrlReferrer == null)
                return false;
            if (HttpContext.Current.Request.UrlReferrer.Host.EndsWith(Branding.CurrentBrand.HostName, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        [System.Web.Services.WebMethod(EnableSession = true)]
        public static airport[] AirportsInBoundingBox(double latSouth, double lonWest, double latNorth, double lonEast, bool fIncludeHeliports = false)
        {
            return (IsValidUnauthedCaller()) ? airport.AirportsWithinBounds(latSouth, lonWest, latNorth, lonEast, fIncludeHeliports).ToArray() : Array.Empty<airport>();
        }


        protected void Page_Load(object sender, EventArgs e)
        {
            this.Master.SelectedTab = tabID.mptRoute;
            lblPageHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.Airports.MapRouteHeader, Branding.CurrentBrand.AppName);

            bool viewHist = User.Identity.IsAuthenticated && MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData && util.GetStringParam(Request, "hist").Length > 0;

            if (!IsPostBack)
            {
                if (!String.IsNullOrEmpty(Request.QueryString["Airports"]))
                {
                    txtAirports.Text = HttpUtility.HtmlEncode(Request.QueryString["Airports"]).Replace("&gt;", ">").Replace("&#10;", "\r\n").Replace("&#13;", " ");
                    txtAirports.Rows = Math.Min(Math.Max((txtAirports.Text.Length / 70) + 1, 1), 6);
                    mfbAirportServices1.GoogleMapID = MfbGoogleMapManager1.MapID;
                }

                if (viewHist)
                {
                    pnlRestrictToFlown.Visible = true;

                    if (Session[txtAirports.Text] != null)
                        CurrentVisitedRoute = (VisitedRoute)Session[txtAirports.Text];
                }

                btnOptimizeRoute.Visible = (util.GetStringParam(Request, "tsp").Length > 0);
            }

            string szDescription = txtAirports.Text.Length > 0 ? txtAirports.Text : lblPageHeader.Text;
            this.Master.AddMeta("description", szDescription);
            this.Master.Title = szDescription;

            LogbookEntry le = new LogbookEntry() { Route = txtAirports.Text };
            lblDistance.Text = le.GetPathDistanceDescription(null);
            pnlDistance.Visible = lblDistance.Text.Length > 0;

            ScriptManager.GetCurrent(Page).RegisterAsyncPostBackControl(btnMapEm);

            MfbGoogleMapManager1.Mode = (util.GetIntParam(Request, "sm", 0) != 0) ? GMap_Mode.Static : GMap_Mode.Dynamic;
            mfbAirportServices1.AddZoomLink = (MfbGoogleMapManager1.Mode == GMap_Mode.Dynamic);
            if (viewHist && CurrentVisitedRoute != null)
                ViewHistorical();
            else
                MapAirports(txtAirports.Text);

            MfbGoogleMapManager1.Visible = !String.IsNullOrWhiteSpace(txtAirports.Text);    // cut down on pointless mapping.
        }

        protected void btnMapEm_Click(object sender, EventArgs e)
        {
            Response.Redirect(Request.Url.AbsolutePath + "?Airports=" + HttpUtility.UrlEncode(txtAirports.Text) + (btnOptimizeRoute.Visible ? "&tsp=1" : string.Empty));
        }

        private void SetAirportsInMap(IEnumerable<AirportList> lst)
        {
            MfbGoogleMapManager1.Map.Airports = lst ?? throw new ArgumentNullException(nameof(lst));
            MfbGoogleMapManager1.Map.Options.fAutofillPanZoom = (!lst.Any());
            lnkZoomOut.NavigateUrl = MfbGoogleMapManager1.ZoomToFitScript;
        }


        protected void MapAirports(string szAirports)
        {
            ListsFromRoutesResults result = AirportList.ListsFromRoutes(szAirports);
            SetAirportsInMap(result.Result);

            // and add the table to the page underneath the map
            mfbAirportServices1.SetAirports(result.MasterList.GetNormalizedAirports());

            lnkZoomOut.Visible = !result.MasterList.LatLongBox().IsEmpty;
            pnlMetars.Visible = result != null && result.Result != null && result.Result.Count > 0;
        }

        protected void btnMetars_Click(object sender, EventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            METAR.RefreshForRoute(txtAirports.Text);
            btnMetars.Visible = false;
        }

        #region Visited Routes
        protected VisitedRoute CurrentVisitedRoute
        {
            get
            {
                VisitedRoute vr;
                if (ViewState["visitedRoutes"] == null)
                    ViewState["visitedRoutes"] = vr = new VisitedRoute(txtAirports.Text);
                else
                    vr = (VisitedRoute)ViewState["visitedRoutes"];
                return vr;
            }

            set { ViewState["visitedRoutes"] = value; }
        }

        protected void ViewHistorical()
        {
            UpdateStatus();

            // Get all of the images from the flown segments
            MfbGoogleMapManager1.Map.Images = CurrentVisitedRoute.GetImagesOnFlownSegments();
            SetAirportsInMap(CurrentVisitedRoute.ComputeFlownSegments());
        }

        protected void RedirectHistorical()
        {
            Session[txtAirports.Text] = CurrentVisitedRoute;
            Response.Redirect(String.Format(CultureInfo.InvariantCulture, "{0}?Airports={1}&hist=true", Request.Url.AbsolutePath, HttpUtility.UrlEncode(txtAirports.Text)));
        }

        protected void btnSeeWhatsBeenFlown_Click(object sender, EventArgs e)
        {
            CurrentVisitedRoute.Refresh(10);
            RedirectHistorical();
        }

        protected void btnShowFlownSegmentDetail_Click(object sender, EventArgs e)
        {
            //   litFlownStatus.Text = CurrentVisitedRoute.ToString().Replace("\r\n", "<br />");
            gvFlownSegments.DataSource = CurrentVisitedRoute.SerializedSegments;
            gvFlownSegments.DataBind();
        }

        protected void UpdateStatus()
        {
            lblVRStatus.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Admin.VisitedRoutesExaminedTemplate, CurrentVisitedRoute.SearchedSegmentsCount, CurrentVisitedRoute.TotalSegmentCount);
        }

        protected void btnDownloadXML_Click(object sender, EventArgs e)
        {
            Response.Clear();
            Response.ContentType = "text/xml";
            Response.AddHeader("Content-Disposition", "inline;filename=visitedroutes.xml");
            Response.Write(CurrentVisitedRoute.SerializeXML());
            Response.End();
        }

        protected void btnUploadXML_Click(object sender, EventArgs e)
        {
            if (fuXMLFlownRoutes.HasFile)
            {
                string sz = string.Empty;
                using (System.IO.StreamReader sr = new System.IO.StreamReader(fuXMLFlownRoutes.FileContent))
                    sz = sr.ReadToEnd();

                CurrentVisitedRoute = sz.DeserializeFromXML<VisitedRoute>();

                RedirectHistorical();
            }
        }
        protected void gvFlownSegments_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                FlownSegment fs = (FlownSegment)e.Row.DataItem;
                if (!fs.HasMatch || fs.MatchingFlight == null)
                    return;

                HyperLink h = (HyperLink)e.Row.FindControl("lnkFlight");
                h.NavigateUrl = ResolveClientUrl(String.Format(CultureInfo.InvariantCulture, "~/Member/FlightDetail.aspx/{0}?a=1", fs.MatchingFlight.FlightID));
                h.Text = fs.MatchingFlight.Date.ToShortDateString();

                Controls_mfbImageList mfbIl = (Controls_mfbImageList)e.Row.FindControl("mfbilFlights");
                mfbIl.Key = fs.MatchingFlight.FlightID.ToString(CultureInfo.InvariantCulture);
                mfbIl.Refresh();
            }
        }
        #endregion

        protected void btnOptimizeRoute_Click(object sender, EventArgs e)
        {
            List<airport> lst = new List<airport>(AirportList.ListsFromRoutes(txtAirports.Text).Result[0].UniqueAirports);
            if (lst.Count == 0)
                return;
            IEnumerable<IFix> path = TravelingSalesman.ShortestPath(lst);
            txtAirports.Text = HttpUtility.HtmlEncode(String.Join(" ", path.Select(ap => ap.Code)));
            btnMapEm_Click(sender, e);
        }
    }
}