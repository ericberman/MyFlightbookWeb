using MyFlightbook.Airports;
using MyFlightbook.Clubs;
using MyFlightbook.Encryptors;
using MyFlightbook.Image;
using MyFlightbook.Mapping;
using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2007-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    /// <summary>
    /// Controller for simple public pages that are nothing but HTML content; renders the localized html content into a layout page.  Examples include Privacy or Terms and Conditions
    /// </summary>
    public class PubController : Controller
    {
        #region club viewing
        public ActionResult AllClubs(int a = 0)
        {
            bool fAdmin = User.Identity.IsAuthenticated && a != 0 && MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData;
            ViewBag.Clubs = Club.AllClubs(fAdmin);
            return View("allClubs");
        }
        #endregion

        #region View public flight
        const string szMap = "map";
        const string szAirports = "airports";
        const string szDetails = "details";
        const string szPix = "pictures";
        const string szVids = "videos";

        public ActionResult DownloadKML(int id)
        {
            LogbookEntry le = new LogbookEntry();
            if (id <= 0 || !le.FLoadFromDB(id, User.Identity.Name, LogbookEntryCore.LoadTelemetryOption.LoadAll, true))
                return new EmptyResult();

            if (!le.fIsPublic && (String.Compare(le.User, User.Identity.Name, StringComparison.OrdinalIgnoreCase) != 0)) // not public and this isn't the owner...
                return new EmptyResult();

            using (FlightData fd = new FlightData())
            {
                fd.ParseFlightData(le);
                if (fd.HasLatLongInfo)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        DataSourceType dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.KML);
                        fd.WriteKMLData(ms);
                        return File(ms.ToArray(), dst.Mimetype, String.Format(CultureInfo.CurrentCulture, "FlightData{0}.{1}", le.FlightID, dst.DefaultExtension));
                    }
                }
            }
            return new EmptyResult();
        }

        /// <summary>
        /// View a public flight
        /// </summary>
        /// <param name="id">ID of the flight</param>
        /// <param name="show">String of components to show (for embedding)</param>
        /// <param name="dm">1 to force a dynamic map (otherwise, may get a static map)</param>
        /// <param name="r">1 to show the route-of-flight</param>
        /// <param name="p">1 to show a path-of-flight</param>
        /// <param name="i">1 to show images on the map</param>
        /// <returns></returns>
        public ActionResult ViewFlight(int id, string show = "", int dm = 0, int r = 1, int p = 1, int i = 1)
        {
            LogbookEntry le = new LogbookEntry();
            if (id <= 0 || !le.FLoadFromDB(id, User.Identity.Name, LogbookEntryCore.LoadTelemetryOption.MetadataOrDB, true))
                return Redirect("~/Default.aspx");

            if (!le.fIsPublic && (String.Compare(le.User, User.Identity.Name, StringComparison.OrdinalIgnoreCase) != 0)) // not public and this isn't the owner...
                return Redirect("~/mvc/Airport/MapRoute?sm=1&Airports=" + HttpUtility.UrlEncode(le.Route));

            ViewBag.flight = le;
            ViewBag.title = le.SocialMediaComment.Length > 0 ? le.SocialMediaComment : Resources.LogbookEntry.PublicFlightHeader;

            HashSet<string> hsComponentsToShow = String.IsNullOrEmpty(show) ? new HashSet<string>() { szMap, szAirports, szDetails, szPix, szVids } : new HashSet<string>(show.Split(','));
            ViewBag.showMap = hsComponentsToShow.Contains(szMap);
            ViewBag.showDetails = hsComponentsToShow.Contains(szDetails);
            ViewBag.showPix = hsComponentsToShow.Contains(szPix);
            ViewBag.showVids = hsComponentsToShow.Contains(szVids);
            ViewBag.showAirports = hsComponentsToShow.Contains(szAirports);
            ViewBag.showImagesOnMap = i > 0;
            ViewBag.showPath = p > 0;
            ViewBag.showRoute = r > 0;

            ViewBag.ForceNaked = !String.IsNullOrEmpty(show) && hsComponentsToShow.Count < 5;



            ImageList ilFlight = new ImageList(MFBImageInfoBase.ImageClass.Flight, le.FlightID.ToString(CultureInfo.InvariantCulture));
            ilFlight.Refresh();
            le.PopulateImages();

            UserAircraft ua = new UserAircraft(le.User);
            Aircraft ac = ua.GetUserAircraftByID(le.AircraftID) ?? new Aircraft(le.AircraftID);
            ImageList ilAC = new ImageList(MFBImageInfoBase.ImageClass.Aircraft, le.AircraftID.ToString(CultureInfo.InvariantCulture));
            ilAC.Refresh(false, ac.DefaultImage);

            List<MFBImageInfo> lst = new List<MFBImageInfo>(ilFlight.ImageArray);
            lst.AddRange(ilAC.ImageArray);
            ViewBag.sliderImages = lst;

            GoogleMap map = new GoogleMap("divMapOfRoute");

            // Set up the map
            double distance = 0.0;
            bool fHasPath = le.Telemetry != null && le.Telemetry.HasPath;
            ListsFromRoutesResults result = null;
            if (le.Route.Length > 0 || fHasPath) // show a map.
            {
                result = AirportList.ListsFromRoutes(le.Route);
                map.Airports = result.Result;
                map.Options.fShowRoute = r > 0;
                map.Options.fAutofillPanZoom = (result.Result.Count == 0);
                map.AllowDupeMarkers = false;

                // display flight path, if available.
                if (p > 0 && le.Telemetry.HasPath)
                {
                    map.Path = le.Telemetry.Path();
                    distance = le.Telemetry.Distance();
                }
            }

            map.Images = i > 0 ? le.FlightImages.ToArray() : Array.Empty<MFBImageInfo>();

            bool fHasGeotaggedImages = false;
            if (le.FlightImages != null)
            {
                foreach (MFBImageInfo mfbii in le.FlightImages)
                    fHasGeotaggedImages = fHasGeotaggedImages || mfbii.Location != null;
            }

            // By default, show only a static map (cut down on dynamic map hits)
            map.MapMode = (dm > 0 || fHasGeotaggedImages || fHasPath) ? GMap_Mode.Dynamic : GMap_Mode.Static;

            ViewBag.map = map;

            if (result == null)
                ViewBag.showMap = false;
            else
            {
                ViewBag.normalizedAirports = result.MasterList.GetNormalizedAirports();
                ViewBag.distanceDescription = le.GetPathDistanceDescription(distance);
            }

            ViewBag.baseURL = Request.Url.GetLeftPart(UriPartial.Path) + String.Format(CultureInfo.InvariantCulture, "?dm={0}", dm);

            return View("ViewFlight");
        }
        #endregion

        public ActionResult Unsubscribe(string u)
        {
            ViewBag.isError = false;
            try
            {
                if (String.IsNullOrEmpty(u))
                    throw new MyFlightbookException(Resources.Profile.errUnsubscribeNoEmail);
                string szUser = new UserAccessEncryptor().Decrypt(u);
                if (String.IsNullOrEmpty(szUser))
                    throw new MyFlightbookException(Resources.Profile.errUnsubscribeNoEmail);
                Profile pf = MyFlightbook.Profile.GetUser(szUser) ?? throw new MyFlightbookException(Resources.Profile.errUnsubscribeNotFound);
                pf.Subscriptions = 0;
                pf.FCommit();
                ViewBag.unsubscribeResult = String.Format(CultureInfo.CurrentCulture, Resources.Profile.UnsubscribeSuccessful, pf.Email);
            }
            catch (MyFlightbookException ex)
            {
                ViewBag.isError = true;
                ViewBag.unsubscribeResult = ex.Message;
            }
            return View("unsubscribe");
        }

        public ActionResult PropertyTable()
        {
            ViewBag.props = CustomPropertyType.GetCustomPropertyTypes();
            return View("propertyTable");
        }

        public ActionResult MobileApps(string selectedOS = "")
        {
            ViewBag.SelectedOS = selectedOS;
            return View("mobileApps");
        }

        public ActionResult TandC()
        {
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = Resources.LocalizedText.TermsAndConditionsHeader;
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LocalizedText.TermsAndConditions);
            return View("_localizedContent");
        }

        public ActionResult Privacy()
        {
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrivacyPolicyHeader, Branding.CurrentBrand.AppName);
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LocalizedText.Privacy);
            return View("_localizedContent");
        }

        public ActionResult FeatureChart()
        {
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = Resources.LocalizedText.FeaturesHeader;
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LocalizedText.FeatureTable);
            return View("_localizedContent");
        }

        public ActionResult CFISigs()
        {
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = Resources.LocalizedText.CFISigsTitle;
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LocalizedText.CFISigs);
            return View("_localizedContent");
        }

        public ActionResult ClubManual()
        {
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = Resources.LocalizedText.ClubsManualTitle;
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LocalizedText.ClubsManual);
            return View("_localizedContent");
        }

        public ActionResult CurrencyNotes()
        {
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = Resources.Currency.CurrencyImportantNotes;
            ViewBag.HTMLContent = Branding.ReBrand(Resources.Currency.CurrencyDisclaimer);
            return View("_localizedContent");
        }

        public ActionResult ImportTable()
        {
            ViewBag.defaultTab = tabID.tabLogbook;
            ViewBag.Title = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.importTableHeader, Branding.CurrentBrand.AppName);
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LogbookEntry.ImportTableDescription);
            ViewBag.PropTypeList = CustomPropertyType.GetCustomPropertyTypes();
            return View("ImportTable");
        }

        public ActionResult FlightDataKey()
        {
            ViewBag.defaultTab = tabID.tabLogbook;
            ViewBag.Title = String.Format(CultureInfo.CurrentCulture, Resources.FlightData.FlightDataHeader, Branding.CurrentBrand.AppName);
            ViewBag.HTMLContent = Branding.ReBrand(Resources.FlightData.FlightDataKey);
            ViewBag.Columns = KnownColumn.GetKnownColumns();
            return View("flightdatakey");
        }

        public ActionResult About()
        {
            ViewBag.defaultTab = tabID.tabHome;
            ViewBag.Title = Branding.ReBrand(Resources.LocalizedText.AboutTitle);
            return View("about");
        }

        public ActionResult DangerousRequest()
        {
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = Resources.LocalizedText.errContentBlockedTitle;
            ViewBag.HTMLContent = Resources.LocalizedText.ContentBlocked;
            return View("_localizedContent");
        }

        // GET: mvc/Pub - shouldn't ever call.
        public ActionResult Index()
        {
            Response.Redirect("~");
            Response.End();
            return null;
        }
    }
}