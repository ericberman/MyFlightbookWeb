using MyFlightbook;
using MyFlightbook.Airports;
using MyFlightbook.Image;
using MyFlightbook.Telemetry;
using System;
using System.Globalization;
using System.Linq;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2007-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_ViewPublicFlight : System.Web.UI.Page
{
    const string szParamComponents = "show";
    const string szMap = "map";
    const string szAirports = "airports";
    const string szDetails = "details";
    const string szPix = "pictures";
    const string szVids = "videos";

    private void ShowComponents(string[] rgComponents)
    {
        foreach (string sz in rgComponents)
        {
            if (string.Compare(sz, szMap, StringComparison.OrdinalIgnoreCase) == 0)
                divMap.Visible = true;
            if (string.Compare(sz, szAirports, StringComparison.OrdinalIgnoreCase) == 0)
                mfbAirportServices1.Visible = true;
            if (string.Compare(sz, szPix, StringComparison.OrdinalIgnoreCase) == 0)
                imgsliderFlights.Visible = true;
            if (string.Compare(sz, szVids, StringComparison.OrdinalIgnoreCase) == 0)
                mfbVideoEntry1.Visible = true;
            if (string.Compare(sz, szDetails, StringComparison.OrdinalIgnoreCase) == 0)
                pnlDetails.Visible = true;
        }
    }

    private void ShowMap(LogbookEntry le)
    {
        double distance = 0.0;
        if (le.Route.Length > 0 || (le.Telemetry != null && le.Telemetry.HasPath)) // show a map.
        {
            ListsFromRoutesResults result = AirportList.ListsFromRoutes(le.Route);
            MfbGoogleMap1.Map.Airports = result.Result;
            MfbGoogleMap1.Map.AutofillOnPanZoom = (result.Result.Count() == 0);
            lnkZoomOut.NavigateUrl = MfbGoogleMap1.ZoomToFitScript;
            lnkZoomOut.Visible = !result.MasterList.LatLongBox().IsEmpty;

            // display flight path, if available.
            if (le.Telemetry.HasPath)
            {
                MfbGoogleMap1.Map.Path = le.Telemetry.Path();
                distance = le.Telemetry.Distance();
                lnkViewKML.Visible = true;
            }

            mfbAirportServices1.GoogleMapID = MfbGoogleMap1.MapID;

            mfbAirportServices1.SetAirports(result.MasterList.GetNormalizedAirports());

            string szURL = Request.Url.PathAndQuery;
            lnkShowMapOnly.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", szURL, szURL.Contains("?") ? "&" : "?", "show=map");
        }

        lblDistance.Text = le.GetPathDistanceDescription(distance);
        pnlDistance.Visible = lblDistance.Text.Length > 0;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.Layout = MasterPage.LayoutMode.Accordion;

        if (!IsPostBack)
        {
            string szRedirect = string.Empty;

            int id = util.GetIntParam(Request, "id", LogbookEntry.idFlightNone);
            string szComponents = util.GetStringParam(Request, szParamComponents);

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
                hdnID.Value = id.ToString(CultureInfo.InvariantCulture);

                if (!le.fIsPublic && (String.Compare(le.User, User.Identity.Name, StringComparison.OrdinalIgnoreCase) != 0)) // not public and this isn't the owner...
                    szRedirect = "~/public/MapRoute2.aspx?Airports=" + HttpUtility.UrlEncode(le.Route);

                // display only selected components, if necessary
                if (!String.IsNullOrEmpty(szComponents))
                {
                    // turn off the header/footer to display only the requested components
                    this.Master.HasFooter = this.Master.HasHeader = false;
                    FullPageBottom.Visible = FullPageTop.Visible = false;

                    pnlDetails.Visible = divMap.Visible = mfbAirportServices1.Visible = lnkShowMapOnly.Visible = imgsliderFlights.Visible = mfbVideoEntry1.Visible = false;

                    ShowComponents(szComponents.Split(','));
                }

                lblComments.Text = le.Comment.Linkify();

                fbComment.URI = Branding.PublicFlightURL(id);

                string szRoute = le.Route;
                lnkRoute.Text = HttpUtility.HtmlEncode(szRoute);
                lnkRoute.NavigateUrl = "~/Public/MapRoute2.aspx?Airports=" + HttpUtility.UrlEncode(szRoute);

                Profile pf = MyFlightbook.Profile.GetUser(le.User);
                lnkUser.Text = pf.UserFullName;
                lnkUser.NavigateUrl = pf.PublicFlightsURL(Request.Url.Host).AbsoluteUri;
                btnEdit.Visible = (le.User == User.Identity.Name);

                lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithDash, le.Date.ToShortDateString(), le.TailNumDisplay);

                if (this.Master.IsMobileSession())
                    mfbIlAirplane.Columns = mfbIlFlight.Columns = 1;

                mfbIlFlight.Key = hdnID.Value;
                mfbIlFlight.Refresh();
                MfbGoogleMap1.Map.Images = mfbIlFlight.Images.ImageArray.ToArray();

                mfbIlAirplane.Key = le.AircraftID.ToString(CultureInfo.InvariantCulture);
                mfbIlAirplane.AltText = le.TailNumDisplay;

                UserAircraft ua = new UserAircraft(le.User);
                Aircraft ac = ua.GetUserAircraftByID(le.AircraftID) ?? new Aircraft(le.AircraftID);
                mfbIlAirplane.DefaultImage = ac.DefaultImage;
                mfbIlAirplane.Refresh();

                imgSliderAircraft.Images = mfbIlAirplane.Images.ImageArray;
                imgsliderFlights.Images = mfbIlFlight.Images.ImageArray;

                string szDescription = le.SocialMediaComment.Length > 0 ? le.SocialMediaComment : Resources.LogbookEntry.PublicFlightHeader;
                this.Master.Title = szDescription;
                this.Master.AddMeta("description", szDescription);
                mfbMiniFacebook.FlightEntry = le;

                lblError.Text = "";

                this.Master.SelectedTab = tabID.tabHome;

                mfbVideoEntry1.Videos.Clear();
                foreach (VideoRef vid in le.Videos)
                    mfbVideoEntry1.Videos.Add(vid);

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
}
