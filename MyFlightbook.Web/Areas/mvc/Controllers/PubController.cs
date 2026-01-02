using MyFlightbook.Airports;
using MyFlightbook.Clubs;
using MyFlightbook.Encryptors;
using MyFlightbook.Image;
using MyFlightbook.Mapping;
using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2007-2025 MyFlightbook LLC
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
                return Redirect("~/mvc/pub");

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

        #region Year in Review
        private static string YearInReviewPrefix(int year) => $"ReviewYear{year}";

        private static int DefaultYear(int year)
        {
            return (year < 0) ? DateTime.Now.Date.AddDays(-1).Year : year;
        }

        [HttpGet]
        public ActionResult YearInReviewPub(string uid, int year = -1)
        {
            try
            {
                string[] rgsz = new SharedDataEncryptor(string.Empty).Decrypt(uid).SplitCommas();
                ViewBag.user = rgsz.Length == 2 ? rgsz[1] : throw new UnauthorizedAccessException();
                // Limit the sharing to the explicitly shared year
                if (rgsz[0].CompareCurrentCultureIgnoreCase(YearInReviewPrefix(DefaultYear(year))) != 0)
                    throw new UnauthorizedAccessException();
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                throw new UnauthorizedAccessException();
            }
            ViewBag.year = DefaultYear(year);
            return View("yearInReview");
        }

        [HttpGet]
        [Authorize]
        public ActionResult YearInReview(int year = -1)
        {
            return RedirectToAction("YearInReviewPub", new { uid = new SharedDataEncryptor(string.Empty).Encrypt($"{YearInReviewPrefix(DefaultYear(year))},{User.Identity.Name}"), year = DefaultYear(year) });
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

        [HttpPost]
        public ActionResult CrashReport(string szAppToken, string stacktrace)
        {
            if (!MFBWebService.IsAuthorizedService(szAppToken))
                throw new UnauthorizedAccessException();

            util.NotifyAdminEvent("Crash report received", stacktrace ?? string.Empty, ProfileRoles.maskSiteAdminOnly);

            return Content("OK");
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

        public ActionResult PrivacyUsage()
        {
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrivacyPolicyHeader, Branding.CurrentBrand.AppName);
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LocalizedText.PrivacyDataFeatures);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Contact(string name, string email, string subject, string message, int noCap)
        {
            double score = -1.0;
            try
            {
                if (!string.IsNullOrEmpty(LocalConfig.SettingForKey("recaptchaKey")) && (score = await RecaptchaUtil.ValidateRecaptcha(Request["g-recaptcha-response"], "Contact", Request.Url.Host)) < 0.5)
                    throw new InvalidOperationException(Resources.LocalizedText.ValidationRecaptchaFailed);
            }
            catch (HttpRequestException ex)
            {
                message += $"\r\n\r\nCaptcha failed - {ex.Message}";
                // couldn't reach google to validate the captcha - go ahead and eat the error; better to allow the occassional bot than to disallow a user.
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentNullException)
            {
                ViewBag.subject = subject;
                ViewBag.message = message;
                ViewBag.noCap = noCap;
                ViewBag.error = ex.Message;
                return View("contact");
            }

            util.ContactUs(User.Identity.Name, name, email, subject, message, Request.Files, score, (ConfigurationManager.AppSettings["UseOOF"] ?? string.Empty).CompareCurrentCultureIgnoreCase("yes") == 0);
            ViewBag.success = true;
            ViewBag.showReturn = (noCap == 0);

            return View("contact");
        }

        public ActionResult RSS(string uid, string HTML = null, string t = null)
        {
            if (uid == null)
                return View("rss");

            string szUser = string.Empty;
            string szDebug = string.Empty;
            if (String.IsNullOrEmpty(uid))
            {
                if (User.Identity.IsAuthenticated)
                {
                    szDebug = "Using cached credentials...";
                    szUser = User.Identity.Name;
                }
                else
                    throw new UnauthorizedAccessException();
            }
            else
            {
                SharedDataEncryptor ec = new SharedDataEncryptor("mfb");
                szDebug = "original uid=" + Request.Params["uid"] + " fixed szUid=" + uid + " and szUser=" + szUser + " and timestamp = " + DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString();
                szUser = ec.Decrypt(uid);
            }

            ViewBag.userName = szUser;
            ViewBag.debug = szDebug;
            ViewBag.fTotals = ((t ?? string.Empty).CompareCurrentCulture("1") == 0);
            return PartialView((HTML ?? string.Empty).CompareCurrentCultureIgnoreCase("1") == 0 ? "_rssHTML" : "_rssXML");
        }

        [HttpGet]
        public ActionResult Contact()
        {
            return View("contact");
        }

        [HttpGet]
        public ActionResult SessionExpired()
        {
            return View("sessionExpired");
        }

        public ActionResult Shunt()
        {
            return View("shunt");
        }

        public ActionResult UnAuth()
        {
            return View("newUnauthHome");
        }

        /// <summary>
        /// Returns the home page for the site, redirecting to mini if needed.
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            string s = Request["m"] ?? string.Empty;
            if (!String.IsNullOrEmpty(s))
                util.SetMobile(s.CompareCurrentCultureIgnoreCase("no") != 0);

            // redirect to a mobile view if this is from a mobile device UNLESS cookies suggest to do otherwise.
            bool fShouldBeMobile = Request.IsMobileSession() && (Request.Cookies[MFBConstants.keyClassic]?.Value ?? "yes").CompareCurrentCultureIgnoreCase("yes") != 0;
            if (fShouldBeMobile)
            {
                util.SetMobile(true);
                return View("homemini");
            }

            return View(User.Identity.IsAuthenticated ? "homeAuth" : "newUnauthHome");
        }
    }
}