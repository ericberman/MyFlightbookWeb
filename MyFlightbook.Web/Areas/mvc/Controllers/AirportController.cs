using MyFlightbook.Airports;
using MyFlightbook.Mapping;
using MyFlightbook.Weather.ADDS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AirportController : Controller
    {
        #region Visited Map
        [Authorize]
        public ActionResult VisitedMap(string fqs = null, string v = null)
        {
            VisitedLocations locations = (v == null) ? null : new VisitedLocations(v);

            if (v == null)
            {
                FlightQuery fq = String.IsNullOrEmpty(fqs) ? new FlightQuery(User.Identity.Name) : FlightQuery.FromBase64CompressedJSON(fqs);
                if (fq.UserName.CompareCurrentCultureIgnoreCase(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException();

                locations = new VisitedLocations(VisitedAirport.VisitedAirportsForQuery(fq));
            }

            ViewBag.DataToMap = JsonConvert.SerializeObject(locations, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore });
            ViewBag.TimelineData = JsonConvert.SerializeObject(locations.BuildTimeline());
            return View("visitedMap");
        }
        #endregion

        [HttpPost]
        public ActionResult AirportsInBoundingBox(double latSouth, double lonWest, double latNorth, double lonEast, bool fIncludeHeliports = false)
        {
            IEnumerable<airport> result = Request.IsLocal || Request.UrlReferrer.Host.EndsWith(Branding.CurrentBrand.HostName, StringComparison.OrdinalIgnoreCase) ?
                airport.AirportsWithinBounds(latSouth, lonWest, latNorth, lonEast, fIncludeHeliports) : 
                Array.Empty<airport>();

            return Json(result);
        }

        [ChildActionOnly]
        public ActionResult AirportServicesTable(airport[] airports, bool isStatic, string gmapDivID, bool showZoom = true, bool showServices = true, bool showInfo = true, bool showFBO = true, bool showMetar = true, bool showHotels = true)
        {
            ViewBag.ShowZoom = showZoom;
            ViewBag.ShowServices = showServices;
            ViewBag.ShowInfo = showInfo;
            ViewBag.ShowFBO = showFBO;
            ViewBag.ShowMetar = showMetar;
            ViewBag.ShowHotels = showHotels;
            ViewBag.IsStatic = isStatic;
            ViewBag.gmapDivID = gmapDivID;

            List<airport> lst = new List<airport>(airports);
            lst.RemoveAll(ap => !ap.IsPort);
            lst.Sort();
            ViewBag.airports = lst;

            return PartialView("_airportServices");
        }

        [ChildActionOnly]
        public ActionResult GetMetar(string id, string szAirports)
        {
            ViewBag.Id = id;
            ViewBag.airports = szAirports;
            return PartialView("_getMetar");
        }

        #region Admin in Map Routes for visited routes
        [HttpPost]
        public ActionResult RenderVisitedRoutes(string szVRJson)
        {
            VisitedRoute vr = JsonConvert.DeserializeObject<VisitedRoute>(szVRJson);
            ViewBag.visitedRoute = vr;
            return PartialView("_visitedRoutesSummary");
        }

        private const string sessKeyXMLDownload = "VisitedRoutesAsXML";

        [HttpPost]
        public ActionResult DownloadVisitedRoutes(string szVRJson)
        {
            VisitedRoute vr = JsonConvert.DeserializeObject<VisitedRoute>(szVRJson);
            Session[sessKeyXMLDownload] = vr;
            return Content("");
        }

        [HttpGet]
        public ActionResult GetVisitedRoutesDownload() 
        {
            VisitedRoute vr = (VisitedRoute) Session[sessKeyXMLDownload];
            return (vr == null) ? (ActionResult) Content("No visited routes to download") : (ActionResult) File(Encoding.UTF8.GetBytes(vr.SerializeXML()), "text/xml", "visitedroutes.xml");
        }

        [HttpPost]
        public ActionResult VisitedRoutesFromFile()
        {
            if (!MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            if (Request.Files.Count == 0)
                throw new InvalidOperationException("No file uploaded");

            HttpPostedFileBase pf = Request.Files[0];

            string sz = string.Empty;
            using (StreamReader sr = new StreamReader(pf.InputStream))
                sz = sr.ReadToEnd();

            VisitedRoute vr = sz.DeserializeFromXML<VisitedRoute>();
            return Content(JsonConvert.SerializeObject(vr));
        }
        #endregion

        /// <summary>
        /// Returns a fully interactive map page, replacing old Maproutes.aspx
        /// </summary>
        /// <param name="airports">List of airport codes (or heliports or navaids...)</param>
        /// <param name="tsp">Traveling Salesman Problem - optimizes the route for distance if non-zero</param>
        /// <param name="hist">Admin feature - loads any history if non-zero</param>
        /// <param name="sm">Non-zero for a static map</param>
        /// <returns></returns>
        public ActionResult MapRoute(string Airports = "", int tsp = 0, int hist = 0, int sm = 0)
        {
            ViewBag.Airports = Airports;
            ViewBag.travelingSalesman = tsp != 0;
            if (ViewBag.travelingSalesman)
            {
                ListsFromRoutesResults lrr = AirportList.ListsFromRoutes(Airports);
                if (lrr.Result.Any())
                {
                    List<airport> lst = new List<airport>(lrr.Result[0].UniqueAirports);
                    if (lst.Any())
                    {
                        IEnumerable<IFix> path = TravelingSalesman.ShortestPath(lst);
                        ViewBag.Airports = Airports = String.Join(" ", path.Select(ap => ap.Code));
                    }
                }
            }

            bool fHistory = hist != 0 && User.Identity.IsAuthenticated && MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData;
            ViewBag.history = fHistory;

            ViewBag.staticMap = sm != 0;
            GoogleMap map = new GoogleMap("divMappedRoutes", sm == 0 ? GMap_Mode.Dynamic : GMap_Mode.Static);
            ListsFromRoutesResults result = AirportList.ListsFromRoutes(Airports);
            map.Airports = result.Result;
            map.Options.fAutofillPanZoom = !result.Result.Any();
            ViewBag.Map = map;

            ViewBag.normalizedAirports = result.MasterList.GetNormalizedAirports();
            return View("mapRoute");
        }

        [HttpPost]
        public ActionResult METARSForRoute(string szRoute)
        {
            ViewBag.METARs = ADDSService.LatestMETARSForAirports(szRoute);
            return PartialView("_metar");
        }

        private static AirportList AirportsForTerm(string searchTerm)
        {
            AirportList alResults = new AirportList();
            alResults.InitFromSearch(searchTerm);
            return alResults;
        }

        [HttpPost]
        public ActionResult AirportResultsForText(string searchTerm, int start, int pageSize, airport[] existingResults = null)
        {
            airport[] fullSet = existingResults ?? AirportsForTerm(searchTerm).GetAirportList();

            List<airport> page = new List<airport>(fullSet).GetRange(start, Math.Min(pageSize, fullSet.Length - start));

            ViewBag.AirportsResultsList = page.ToArray();
            ViewBag.start = start;
            ViewBag.pageSize = pageSize;
            ViewBag.resultCount = fullSet.Length;
            ViewBag.searchTerm = searchTerm;

            return PartialView("_foundAirportTable");
        }

        public ActionResult FindAirports(string searchText = null)
        {
            AirportList al = AirportsForTerm(searchText);
            ViewBag.AirportsResultsList = al.GetAirportList();
            ViewBag.start = 0;
            ViewBag.pageSize = 15;
            GoogleMap googleMap = new GoogleMap("divFoundAirports", GMap_Mode.Dynamic) { AllowDupeMarkers = true };
            googleMap.Options.fShowRoute = false;
            googleMap.SetAirportList(al);
            ViewBag.Map = googleMap;
            ViewBag.SearchTerm = searchText ?? "";

            return View("findAirports");
        } 

        [ChildActionOnly]
        public ActionResult MapDiv(GoogleMap map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            if (map.Options == null || String.IsNullOrEmpty(map.Options.divContainer))
                throw new InvalidOperationException("Map must include options that include, at least, the id of a div container");

            if (User.Identity.IsAuthenticated)
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                map.Options.PathColor = pf.GetPreferenceForKey<string>(MFBConstants.keyPathColor, MFBGoogleMapOptions.DefaultPathColor);
                map.Options.RouteColor = pf.GetPreferenceForKey<string>(MFBConstants.keyRouteColor, MFBGoogleMapOptions.DefaultRouteColor);
            }

            if (String.IsNullOrEmpty(map.Options.id))
                map.Options.id = "map" + map.Options.divContainer;

            ViewBag.Map = map;
            map.Options.PathVarName = map.Options.id + "Path";
            return PartialView("_mapContainer");
        }

        // GET: mvc/Airport
        public ActionResult Index()
        {
            return View();
        }
    }
}