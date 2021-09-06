using DotNetOpenAuth.Messaging;
using MyFlightbook;
using MyFlightbook.Airports;
using MyFlightbook.Image;
using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2007-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.PublicPages
{
    public partial class ViewPublicFlight : System.Web.UI.Page
    {
        const string szParamComponents = "show";
        const string szMap = "map";
        const string szAirports = "airports";
        const string szDetails = "details";
        const string szPix = "pictures";
        const string szVids = "videos";

        private void RedirectForComponents()
        {
            UriBuilder uriBuilder = new UriBuilder(String.Format(CultureInfo.InvariantCulture, "{0}://{1}{2}", Request.Url.Scheme, Request.Url.Host, Request.Url.AbsolutePath));

            System.Collections.Specialized.NameValueCollection nvc = HttpUtility.ParseQueryString(Request.Url.Query);
            nvc.Remove(szParamComponents);

            List<string> lstParts = new List<string>();
            if (ckShowMaps.Checked)
                lstParts.Add(szMap);
            if (ckShowAirports.Checked)
                lstParts.Add(szAirports);
            if (ckShowDetails.Checked)
                lstParts.Add(szDetails);
            if (ckShowPictures.Checked)
                lstParts.Add(szPix);
            if (ckShowVids.Checked)
                lstParts.Add(szVids);

            // Add the parameters for what to show
            // But if *ALL* of the pieces are shown, just remove the query parameters and show everything.
            if (lstParts.Any() && lstParts.Count != 5)
                nvc.Add(szParamComponents, String.Join(",", lstParts));

            foreach (string key in nvc.Keys)
                uriBuilder.AppendQueryArgument(key, nvc[key]);

            Response.Redirect(uriBuilder.Uri.ToString());
        }

        private const string szVSLogbook = "vsLEToView";

        private LogbookEntry PublicFlight
        {
            get { return (LogbookEntry)ViewState[szVSLogbook]; }
            set { ViewState[szVSLogbook] = value; }
        }

        private void ShowMap(LogbookEntry le)
        {
            double distance = 0.0;
            bool fHasPath = le.Telemetry != null && le.Telemetry.HasPath;
            ListsFromRoutesResults result = null;
            if (le.Route.Length > 0 || fHasPath) // show a map.
            {
                result = AirportList.ListsFromRoutes(le.Route);
                MfbGoogleMap1.Map.Airports = result.Result;
                MfbGoogleMap1.Map.Options.fShowRoute = ckShowRoute.Checked;
                MfbGoogleMap1.Map.Options.fAutofillPanZoom= (result.Result.Count == 0);
                MfbGoogleMap1.Map.AllowDupeMarkers = false;
                lnkZoomOut.NavigateUrl = MfbGoogleMap1.ZoomToFitScript;
                lnkZoomOut.Visible = !result.MasterList.LatLongBox().IsEmpty;

                // display flight path, if available.
                if (ckShowPath.Checked && le.Telemetry.HasPath)
                {
                    MfbGoogleMap1.Map.Path = le.Telemetry.Path();
                    distance = le.Telemetry.Distance();
                    rowKML.Visible = true;
                }
            }

            MfbGoogleMap1.Map.Images = ckShowImages.Checked ? mfbIlFlight.Images.ImageArray.ToArray() : Array.Empty<MFBImageInfo>();

            bool fForceDynamicMap = util.GetIntParam(Request, "dm", 0) != 0;
            bool fHasGeotaggedImages = false;
            if (le.FlightImages != null)
                foreach (MFBImageInfo mfbii in le.FlightImages)
                    fHasGeotaggedImages = fHasGeotaggedImages || mfbii.Location != null;

            // By default, show only a static map (cut down on dynamic map hits)
            if (fForceDynamicMap || fHasGeotaggedImages || fHasPath)
                MfbGoogleMap1.Mode = MyFlightbook.Mapping.GMap_Mode.Dynamic;
            else
            {
                MfbGoogleMap1.Mode = MyFlightbook.Mapping.GMap_Mode.Static;
                popmenu.Visible = false;
                lnkZoomOut.Visible = mfbAirportServices1.Visible = false;
            }

            if (result != null)
            {
                mfbAirportServices1.GoogleMapID = MfbGoogleMap1.MapID;
                mfbAirportServices1.AddZoomLink = (MfbGoogleMap1.Mode == MyFlightbook.Mapping.GMap_Mode.Dynamic);
                mfbAirportServices1.SetAirports(result.MasterList.GetNormalizedAirports());
            }

            lblDistance.Text = le.GetPathDistanceDescription(distance);
            pnlDistance.Visible = lblDistance.Text.Length > 0;
        }

        private void SetComponentsDisplay(string szComponents)
        {
            // display only selected components, if necessary
            if (!String.IsNullOrEmpty(szComponents))
            {
                // turn off the header/footer to display only the requested components
                Master.HasFooter = Master.HasHeader = Master.HasTitle = FullPageBottom.Visible = false;

                HashSet<string> hsComps = new HashSet<string>(szComponents.Split(','));

                if (hsComps.Any())
                {
                    divMap.Visible = ckShowMaps.Checked = hsComps.Contains(szMap);
                    mfbAirportServices1.Visible = ckShowAirports.Checked = hsComps.Contains(szAirports) && divMap.Visible;
                    divImages.Visible = ckShowPictures.Checked = hsComps.Contains(szPix);
                    mfbVideoEntry1.Visible = ckShowVids.Checked = hsComps.Contains(szVids);
                    pnlDetails.Visible = pnlFB.Visible = ckShowDetails.Checked = hsComps.Contains(szDetails);
                }
            }
        }

        private void SetImages(LogbookEntryCore le)
        {
            if (this.Master.IsMobileSession())
                mfbIlAirplane.Columns = mfbIlFlight.Columns = 1;

            mfbIlFlight.Key = hdnID.Value;
            mfbIlFlight.Refresh();

            mfbIlAirplane.Key = le.AircraftID.ToString(CultureInfo.InvariantCulture);
            mfbIlAirplane.AltText = le.TailNumDisplay;

            UserAircraft ua = new UserAircraft(le.User);
            Aircraft ac = ua.GetUserAircraftByID(le.AircraftID) ?? new Aircraft(le.AircraftID);
            mfbIlAirplane.DefaultImage = ac.DefaultImage;
            mfbIlAirplane.Refresh();

            List<MFBImageInfo> lst = new List<MFBImageInfo>(mfbIlFlight.Images.ImageArray);
            lst.AddRange(mfbIlAirplane.Images.ImageArray);
            imgsliderFlights.Images = lst;
            imgsliderFlights.Visible = lst.Count > 0;

            mfbVideoEntry1.Videos.Clear();
            foreach (VideoRef vid in le.Videos)
                mfbVideoEntry1.Videos.Add(vid);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                string szRedirect = string.Empty;

                string szComponents = util.GetStringParam(Request, szParamComponents);
                int id = util.GetIntParam(Request, "id", LogbookEntry.idFlightNone);

                if (id != -1)
                {
                    Response.Redirect(String.Format(CultureInfo.InvariantCulture, "{0}/{1}{2}", Request.Path, id, String.IsNullOrEmpty(szComponents) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "?{0}={1}", szParamComponents, szComponents)), true);
                    return;
                }
                else if (!String.IsNullOrEmpty(Request.PathInfo) && Int32.TryParse(Request.PathInfo.Substring(1), out id))
                    id = Convert.ToInt32(Request.PathInfo.Substring(1), CultureInfo.InvariantCulture);

                LogbookEntry le = new LogbookEntry();
                // load the flight, redirect home on any error.
                if (id > 0 && le.FLoadFromDB(id, User.Identity.Name, LogbookEntry.LoadTelemetryOption.MetadataOrDB, true))
                {
                    PublicFlight = le;

                    hdnID.Value = id.ToString(CultureInfo.InvariantCulture);

                    if (!le.fIsPublic && (String.Compare(le.User, User.Identity.Name, StringComparison.OrdinalIgnoreCase) != 0)) // not public and this isn't the owner...
                        szRedirect = "~/public/MapRoute2.aspx?sm=1&Airports=" + HttpUtility.UrlEncode(le.Route);

                    SetComponentsDisplay(szComponents);

                    lblComments.Text = le.Comment.Linkify();

                    fbComment.URI = Branding.PublicFlightURL(id);

                    string szRoute = le.Route;
                    lnkRoute.Text = HttpUtility.HtmlEncode(szRoute);
                    lnkRoute.NavigateUrl = "~/Public/MapRoute2.aspx?sm=1&Airports=" + HttpUtility.UrlEncode(szRoute);

                    Profile pf = MyFlightbook.Profile.GetUser(le.User);
                    lnkUser.Text = HttpUtility.HtmlEncode(pf.UserFullName);
                    lnkUser.NavigateUrl = pf.PublicFlightsURL(Request.Url.Host).AbsoluteUri;
                    btnEdit.Visible = popmenu.Visible = (le.User == User.Identity.Name);

                    lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithDash, le.Date.ToShortDateString(), HttpUtility.HtmlEncode(le.TailNumDisplay));

                    SetImages(le);

                    string szDescription = le.SocialMediaComment.Length > 0 ? le.SocialMediaComment : Resources.LogbookEntry.PublicFlightHeader;
                    this.Master.Title = szDescription;
                    this.Master.AddMeta("description", szDescription);
                    mfbMiniFacebook.FlightEntry = le;

                    lblError.Text = string.Empty;

                    this.Master.SelectedTab = tabID.tabHome;

                    ShowMap(le);
                }
                else
                    szRedirect = "~/Default.aspx";

                if (szRedirect.Length > 0)
                {
                    Response.Redirect(szRedirect);
                }
            }
        }

        protected void btnEdit_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Member/LogbookNew.aspx/" + hdnID.Value);
        }

        protected void lnkViewKML_Click(object sender, EventArgs e)
        {
            int id = hdnID.Value.SafeParseInt(-1);
            if (id <= 0)
                return;

            LogbookEntry le = new LogbookEntry(id, User.Identity.Name, LogbookEntry.LoadTelemetryOption.LoadAll);
            if (le == null || String.IsNullOrEmpty(le.FlightData))
                return;

            using (FlightData fd = new FlightData())
            {
                fd.ParseFlightData(le.FlightData);
                if (fd.HasLatLongInfo)
                {
                    {
                        DataSourceType dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.KML);
                        Response.Clear();
                        Response.ContentType = dst.Mimetype;
                        Response.AddHeader("Content-Disposition", String.Format(CultureInfo.CurrentCulture, "attachment;filename=FlightData{0}.{1}", le.FlightID, dst.DefaultExtension));
                        fd.WriteKMLData(Response.OutputStream);
                        Response.End();
                    }
                }
            }
        }

        #region Map options
        protected void ckShowAirports_CheckedChanged(object sender, EventArgs e)
        {
            ShowMap(PublicFlight);
        }

        protected void ckShowPath_CheckedChanged(object sender, EventArgs e)
        {
            ShowMap(PublicFlight);
        }

        protected void ckShowRoute_CheckedChanged(object sender, EventArgs e)
        {
            ShowMap(PublicFlight);
        }

        protected void ckShowImages_CheckedChanged(object sender, EventArgs e)
        {
            ShowMap(PublicFlight);
        }
        #endregion

        protected void ckShowDetails_CheckedChanged(object sender, EventArgs e)
        {
            RedirectForComponents();
        }

        protected void ckShowPictures_CheckedChanged(object sender, EventArgs e)
        {
            RedirectForComponents();
        }

        protected void ckShowVids_CheckedChanged(object sender, EventArgs e)
        {
            RedirectForComponents();
        }

        protected void ckShowAirports_CheckedChanged1(object sender, EventArgs e)
        {
            RedirectForComponents();
        }

        protected void ckShowMaps_CheckedChanged(object sender, EventArgs e)
        {
            RedirectForComponents();
        }
    }
}