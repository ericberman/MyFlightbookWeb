using MyFlightbook.Airports;
using System.Collections.Generic;
using MyFlightbook.Mapping;
using System;
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