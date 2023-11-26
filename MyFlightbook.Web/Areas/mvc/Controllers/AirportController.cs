using MyFlightbook.Airports;
using MyFlightbook.CSV;
using MyFlightbook.Mapping;
using MyFlightbook.Telemetry;
using MyFlightbook.Weather.ADDS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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
        #region AirportQuiz
        [ChildActionOnly]
        public ActionResult QuizStatusForAnswer(AirportQuiz q, AirportQuizQuestion question, int answerIndex)
        {
            if (q == null)
                throw new ArgumentNullException(nameof(q));
            if (question == null)
                throw new ArgumentNullException(nameof(question));

            bool fCorrect = question.CorrectAnswerIndex == answerIndex;
            if (fCorrect)
                q.CorrectAnswerCount++;

            ViewBag.lastAnswerCorrect = fCorrect;
            ViewBag.previousCorrectAnswer = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AirportGameCorrectAnswer, question.Answers[question.CorrectAnswerIndex].FullName);
            ViewBag.runningScore = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AirportGameAnswerStatus, q.CorrectAnswerCount);
            return PartialView("_quizStatus");
        }

        [HttpPost]
        public ActionResult NextQuestion(string quizRef, int answerIndex)
        {
            AirportQuiz q = (AirportQuiz)Session[quizRef];
            if (q == null)
            {
                Response.StatusCode = (int) HttpStatusCode.ExpectationFailed;
                Response.TrySkipIisCustomErrors = true;
                return Content(Resources.LocalizedText.AirportGameNotFound);
            }

            ViewBag.quiz = q;
            ViewBag.answeredQuestion = q.CurrentQuestion;
            ViewBag.answerIndex = answerIndex;

            if (q.CurrentQuestionIndex < q.QuestionCount)
            {
                q.GenerateQuestion();

                AirportQuizQuestion quizQuestion = q.CurrentQuestion;
                GoogleMap map = new GoogleMap("divQuizQuestion", GMap_Mode.Static);
                map.Options.MapCenter = quizQuestion.Answers[quizQuestion.CorrectAnswerIndex].LatLong;
                map.Options.ZoomFactor = GMap_ZoomLevels.Airport;
                map.Options.MapType = GMap_MapType.G_SATELLITE_MAP;
                map.StaticMapAdditionalParams = "style=feature:all|element:labels|visibility:off";

                ViewBag.map = map;
                ViewBag.progress = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AirportGameProgress, q.CurrentQuestionIndex, q.QuestionCount);
                return PartialView("_quizQuestion");
            }
            else
            {
                int cCorrectAnswers = q.CorrectAnswerCount + (answerIndex == q.CurrentQuestion.CorrectAnswerIndex ? 1 : 0); // this will get incremented in QuizStatusForAnswer, but we need it here for proper result.
                int score = Convert.ToInt32((cCorrectAnswers * 100.0) / q.QuestionCount);
                ViewBag.results = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AirportGameCompletionStatus, cCorrectAnswers, q.QuestionCount, score);

                ViewBag.snark = score == 100
                    ? Resources.LocalizedText.AirportGameSnarkPerfect
                    : score >= 70
                    ? Resources.LocalizedText.AirportGameSnark75
                    : score >= 50 ? Resources.LocalizedText.AirportGameSnark50 : Resources.LocalizedText.AirportGameSnarkPoor;

                Session.Remove(quizRef);    // clear out the session
                return PartialView("_quizSnark");
            }
        }

        [HttpPost]
        public string GetQuiz(bool fAnonymous, int bluffCount, int questionCount)
        {
            AirportQuiz quiz = new AirportQuiz() { BluffCount = bluffCount, QuestionCount = questionCount };
            quiz.Init(fAnonymous ? string.Empty : User.Identity.Name, AirportQuiz.szBusyUSAirports);
            string sessionGuid = Guid.NewGuid().ToString();
            Session[sessionGuid] = quiz;
            return sessionGuid;
        }

        [Authorize]
        public ActionResult GameAuthed()
        {
            return Redirect("Game?SkipIntro=true&anon=false");
        }

        public ActionResult Game(bool SkipIntro = false, bool anon = false, int bluffCount = 4, int questionCount = 10)
        {
            ViewBag.bluffCount = Math.Min(Math.Max(bluffCount, 4), 10);
            ViewBag.questionCount = Math.Min(Math.Max(questionCount, 1), 30);
            ViewBag.skipIntro = SkipIntro;
            ViewBag.anon = anon;
            
            return View("game");
        }
        #endregion

        #region VisitedAirport
        [Authorize]
        [HttpPost]
        public string EstimateDistance(FlightQuery fq)
        {
            if (fq == null || fq.UserName.CompareOrdinal(User.Identity.Name) != 0)
                throw new UnauthorizedAccessException();

            double distance = VisitedAirport.DistanceFlownByUser(fq, util.GetIntParam(Request, "df", 0) != 0, out string szErr);

            return (String.IsNullOrEmpty(szErr)) ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.VisitedAirportsDistanceEstimate, distance) : szErr;
        }

        [Authorize]
        public ActionResult DownloadKML(string fq)
        {
            FlightQuery query = FlightQuery.FromBase64CompressedJSON(fq);
            if (query.UserName.CompareOrdinal(User.Identity.Name) != 0)
                throw new UnauthorizedAccessException();

            using (MemoryStream ms = new MemoryStream())
            {
                DataSourceType dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.KML);
                string error = string.Empty;
                VisitedAirport.AllFlightsAsKML(query, ms, out error);
                return File(ms.ToArray(), dst.Mimetype, String.Format(CultureInfo.CurrentCulture, "{0}-Visited.{1}", Branding.CurrentBrand.AppName, dst.DefaultExtension));
            }
        }

        [Authorize]
        public ActionResult DownloadVisited(string fq)
        {
            FlightQuery query = FlightQuery.FromBase64CompressedJSON(fq);
            if (query.UserName.CompareOrdinal(User.Identity.Name) != 0)
                throw new UnauthorizedAccessException();

            IEnumerable<VisitedAirport> rgva = VisitedAirport.VisitedAirportsForQuery(query);

            using (DataTable dt = new DataTable())
            {
                dt.Locale = CultureInfo.CurrentCulture;

                // add the header columns from the gridview
                dt.Columns.Add(new DataColumn(Resources.Airports.airportCode, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Airports.airportName, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Airports.airportCountry + "*", typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Airports.airportRegion, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Airports.airportVisits, typeof(int)));
                dt.Columns.Add(new DataColumn(Resources.Airports.airportEarliestVisit, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Airports.airportLatestVisit, typeof(string)));

                foreach (VisitedAirport va in rgva)
                {
                    DataRow dr = dt.NewRow();
                    dr[0] = va.Code;
                    dr[1] = va.Airport.Name;
                    dr[2] = va.Country;
                    dr[3] = va.Admin1;
                    dr[4] = va.NumberOfVisits;
                    dr[5] = va.EarliestVisitDate.ToString("d", CultureInfo.CurrentCulture);
                    dr[6] = va.LatestVisitDate.ToString("d", CultureInfo.CurrentCulture);

                    dt.Rows.Add(dr);
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8, 1024))
                    {
                        CsvWriter.WriteToStream(sw, dt, true, true);
                        sw.Write(String.Format(CultureInfo.CurrentCulture, "\r\n\r\n\" *{0}\"", Branding.ReBrand(Resources.Airports.airportCountryDisclaimer)));
                        sw.Flush();

                        return File(ms.ToArray(), "text/csv", String.Format(CultureInfo.InvariantCulture, "{0}-Visited-{1}.csv", Branding.CurrentBrand.AppName, DateTime.Now.YMDString()));
                    }
                }
            }
        }

        private static IEnumerable<AirportList> PathsForQuery(FlightQuery fq, AirportList alMatches, bool fShowRoute)
        {
            if (!fShowRoute)
                return new AirportList[] { alMatches };

            List<AirportList> lst = new List<AirportList>();

            DBHelper dbh = new DBHelper(LogbookEntryBase.QueryCommand(fq, lto: LogbookEntryCore.LoadTelemetryOption.None));
            dbh.ReadRows((comm) => { }, (dr) =>
            {
                object o = dr["Route"];
                string szRoute = (string)(o == DBNull.Value ? string.Empty : o);

                if (!String.IsNullOrEmpty(szRoute))
                    lst.Add(alMatches.CloneSubset(szRoute));
            });
            return lst;
        }

        private ViewResult VisitedAirportViewForQuery(FlightQuery fq)
        {
            ViewBag.query = fq;
            IEnumerable<VisitedAirport> rgva = VisitedAirport.VisitedAirportsForQuery(fq);
            ViewBag.visitedAirports = rgva;
            AirportList alMatches = new AirportList(rgva);
            bool fShowRoute = util.GetIntParam(Request, "path", 0) != 0;
            GoogleMap map = new GoogleMap("divMapVisited", GMap_Mode.Dynamic)
            {
                Airports = PathsForQuery(fq, alMatches,fShowRoute)
            };
            map.Options.fShowRoute = fShowRoute;
            ViewBag.Map = map;

            return View("visitedAirports");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize]
        public ActionResult VisitedAirports(string fqJSON, bool fPropDeleteClicked = false, string propToDelete = null)
        {
            FlightQuery fq = String.IsNullOrEmpty(fqJSON) ? new FlightQuery(User.Identity.Name) : JsonConvert.DeserializeObject<FlightQuery>(fqJSON);

            if (fq.UserName.CompareOrdinal(User.Identity.Name) != 0)
                throw new UnauthorizedAccessException();

            if (fPropDeleteClicked)
                fq.ClearRestriction(propToDelete ?? string.Empty);

            return VisitedAirportViewForQuery(fq);
        }

        [HttpGet]
        [Authorize]
        public ActionResult VisitedAirports(string fq)
        {
            FlightQuery _fq = String.IsNullOrEmpty(fq) ? new FlightQuery(User.Identity.Name) : FlightQuery.FromBase64CompressedJSON(fq);
            if (_fq.UserName.CompareOrdinal(User.Identity.Name) != 0)
                throw new UnauthorizedAccessException();

            return VisitedAirportViewForQuery(_fq);
        }
        #endregion

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

            List<airport> lst = new List<airport>(airports ?? Array.Empty<airport>());
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
                if (lrr.Result.Count != 0)
                {
                    List<airport> lst = new List<airport>(lrr.Result[0].UniqueAirports);
                    if (lst.Count != 0)
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
            map.Options.fAutofillPanZoom = result.Result.Count == 0;
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
            return Redirect("MapRoute");
        }
    }
}