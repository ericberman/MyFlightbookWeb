using DotNetOpenAuth.OAuth2;
using MyFlightbook.Airports;
using MyFlightbook.Geography;
using MyFlightbook.Mapping;
using MyFlightbook.OAuth;
using MyFlightbook.SolarTools;
using MyFlightbook.Telemetry;
using OAuthAuthorizationServer.Code;
using OAuthAuthorizationServer.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class PlaypenController : AdminControllerBase
    {
        #region DayNight
        #region WebServices
        public ActionResult AirportLookup(string szCode)
        {
            return SafeOp(() =>
            {
                airport[] rgAirports = new AirportList(szCode).GetAirportList();
                return rgAirports.Length != 0 ? Json(rgAirports[0].LatLong) : throw new InvalidOperationException(Resources.Airports.errNoAirportsFound);
            });
        }
        #endregion

        #region Endpoints
        [HttpGet]
        public ActionResult DayNight(double? latitude, double? longitude, DateTime? date)
        {
            GoogleMap googleMap = new GoogleMap("divAirport", GMap_Mode.Dynamic)
            {
                ClickHandler = "function (point) {clickForAirport(point.latLng);}"
            };
            googleMap.Options.ZoomFactor = GMap_ZoomLevels.US;
            ViewBag.Map = googleMap;
            if (latitude.HasValue && longitude.HasValue && date.HasValue)
            {
                DateTime dtUTC = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, 0, 0, 0, DateTimeKind.Utc);
                SunriseSunsetTimes sst = new SunriseSunsetTimes(dtUTC, latitude.Value, longitude.Value);
                ViewBag.dtUTC = dtUTC;
                ViewBag.sunriseUTC = sst.Sunrise;
                ViewBag.sunsetUTC = sst.Sunset;
                ViewBag.dtUTCDisplay = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, 0, 0, 0, DateTimeKind.Utc).ToLongDateString();
                googleMap.Options.MapCenter = new LatLong(latitude.Value, longitude.Value);
            }
            ViewBag.latitude = latitude ?? 0;
            ViewBag.longitude = longitude ?? 0;
            return View("dayNight");
        }
        #endregion
        #endregion

        #region Merge Telemetry
        #region Session Management for telemetry being merged
        private string SessionKeyBase { get { return User.Identity.Name + "telemmerge"; } }
        private string SessionTime { get { return SessionKeyBase + "time"; } }
        private string SessionAlt { get { return SessionKeyBase + "Alt"; } }
        private string SessionSpeed { get { return SessionKeyBase + "Speed"; } }

        private List<Position> Coordinates
        {
            get { return (List<Position>)Session[SessionKeyBase]; }
            set { Session[SessionKeyBase] = value; }
        }

        protected static bool FromObj(object o)
        {
            return o == null || Convert.ToBoolean(o, CultureInfo.InvariantCulture);
        }

        protected bool HasTime
        {
            get { return FromObj(Session[SessionTime]); }
            set { Session[SessionTime] = value.ToString(CultureInfo.InvariantCulture); }
        }

        protected bool HasAlt
        {
            get { return FromObj(Session[SessionAlt]); }
            set { Session[SessionAlt] = value.ToString(CultureInfo.InvariantCulture); }
        }

        protected bool HasSpeed
        {
            get { return FromObj(Session[SessionSpeed]); }
            set { Session[SessionSpeed] = value.ToString(CultureInfo.InvariantCulture); }
        }
        #endregion

        [HttpPost]
        [Authorize]
        public ActionResult UploadFiles()
        {
            return SafeOp(() =>
            {
                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("No file uploaded");

                // Weird, for some reason I can't simply enumerate across Request.Files...  Keys could be duplicate too..
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    HttpPostedFileBase pf = Request.Files[i];
                    using (FlightData fd = new FlightData())
                    {
                        using (StreamReader sr = new StreamReader(pf.InputStream))
                        {
                            if (fd.ParseFlightData(sr.ReadToEnd()))
                            {
                                if (fd.HasLatLongInfo)
                                {
                                    Coordinates.AddRange(fd.GetTrajectory());
                                    HasTime = HasTime && fd.HasDateTime;
                                    HasAlt = HasAlt && fd.HasAltitude;
                                    HasSpeed = HasSpeed && fd.HasSpeed;
                                }
                            }
                            else
                                throw new InvalidOperationException(fd.ErrorString);
                        }
                    }
                }

                return Content("~/images/kmlicon_med.png".ToAbsolute());
            });

        }

        public ActionResult MergeTelemetry()
        {
            if (Request.HttpMethod == "POST")
            {
                if (Coordinates == null || Coordinates.Count == 0)
                {
                    ViewBag.error = "No coordinates found";
                    return View("mergeTelemetry");
                }
                else
                {
                    Coordinates.Sort();
                    var dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.GPX);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (FlightData fd = new FlightData())
                        {
                            fd.WriteGPXData(ms, Coordinates, HasTime, HasAlt, HasSpeed);
                            return File(ms.ToArray(), dst.Mimetype, "MergedData." + dst.DefaultExtension);
                        }
                    }
                }
            }
            else
            {
                Coordinates = new List<Position>();
                HasAlt = HasTime = HasSpeed = true;
                return View("mergeTelemetry");
            }
        }
        #endregion

        #region oAuthClientTest
        private const string sessionKeyOAuthClient = "oauthClient";

        protected OAuth2AdHocClient CurrentClient
        {
            get
            {
                OAuth2AdHocClient client = (OAuth2AdHocClient)Session[sessionKeyOAuthClient];
                if (client == null)
                {
                    MFBOauth2Client defaultClient = null;
                    if (User.Identity.IsAuthenticated && String.IsNullOrEmpty(Request["clientID"]))
                    {
                        IEnumerable<MFBOauth2Client> clients = MFBOauth2Client.GetClientsForUser(User.Identity.Name);
                        if (clients.Any())
                            defaultClient = clients.First();
                    }

                    string clientID = Request["clientID"] ?? defaultClient?.ClientIdentifier ?? string.Empty;
                    string clientSecret = Request["clientSecret"] ?? defaultClient?.ClientSecret ?? string.Empty;
                    string authURL = Request["authTarget"] ?? "~/mvc/oAuth/Authorize".ToAbsoluteURL(Request).ToString();
                    string tokenURL = Request["tokenTarget"] ?? "~/mvc/oAuth/OAuthToken".ToAbsoluteURL(Request).ToString();
                    string redirectURL = Request["targetRedir"] ?? Request.Url.GetLeftPart(UriPartial.Path);
                    string scope = Request["scopes"] ?? "currency totals addflight readflight addaircraft readaircraft visited namedqueries images";
                    Session[sessionKeyOAuthClient] = client = new OAuth2AdHocClient(clientID, clientSecret, authURL, tokenURL, redirectURL, OAuthUtilities.SplitScopes(scope).ToArray());
                }
                return client;
            }
            set { Session[sessionKeyOAuthClient] = value; }
        }

        protected void InitViewBag()
        {
            ViewBag.client = CurrentClient;
            ViewBag.resourceURL = Request["txtResourceURL"] ?? "~/mvc/oAuth/OAuthResource".ToAbsoluteURL(Request).ToString();
            ViewBag.authorization = Request["code"] ?? string.Empty;
            ViewBag.State = Request["state"] ?? string.Empty;
            ViewBag.error = Request["error"] ?? string.Empty;
        }

        public ActionResult ClientTestBed()
        {
            if (Request["clear"] !=  null)
            {
                CurrentClient = null;
                return Redirect("ClientTestBed");
            }
            InitViewBag();
            return View("oAuthClientTest");
        }

        public async Task<ActionResult> OAuthAuthorize(string targetRedir)
        {
            OAuth2AdHocClient adhocClient = CurrentClient;

            // Use ParseQueryString because that is how you get an HttpValueCollection, on which ToString() works.
            NameValueCollection nvc = HttpUtility.ParseQueryString(string.Empty);
            nvc["code"] = Request["code"] ?? string.Empty;
            nvc["state"] = Request["state"] ?? string.Empty;

            string submitter = Request["submit"];

            try
            {
                if (submitter.CompareCurrentCultureIgnoreCase("authorize") == 0)
                {
                    // Issue #1237 - Refresh the client anew here to pick up new scopes, etc.
                    CurrentClient = null;           // clear the current client
                    adhocClient = CurrentClient;    // recreate it from the form.
                    adhocClient.Authorize(new Uri(targetRedir));
                    return null;
                }
                else if (submitter.CompareCurrentCultureIgnoreCase("token") == 0)
                {
                    adhocClient.AuthState = await adhocClient.ConvertToken(targetRedir, Request["code"]);
                    return Redirect("ClientTestBed");
                }
                else if (submitter.CompareCurrentCultureIgnoreCase("refresh") == 0)
                {
                    bool _ = await adhocClient.RefreshAccessToken();
                    return Redirect("ClientTestBed");
                }
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                nvc["error"] = ex.Message;
                return Redirect(String.Format(CultureInfo.InvariantCulture, "ClientTestBed?{0}", nvc.ToString()));
            }

            return null;
        }

        public ActionResult OAuthResource(string id)
        {
            OAuthServiceCall.ProcessRequest(id);
            return null;
        }

        #endregion

        // GET: mvc/Playpen
        public ActionResult Index()
        {
            return View();
        }
    }
}