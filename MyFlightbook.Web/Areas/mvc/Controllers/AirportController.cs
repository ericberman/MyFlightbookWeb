using MyFlightbook.Airports;
using MyFlightbook.Mapping;
using MyFlightbook.Telemetry;
using MyFlightbook.Weather.ADDS;
using MyFlightbook.Web.Sharing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AirportController : AdminControllerBase
    {
        #region Add/Edit Airports
        #region Add/Edit web services
        /// <summary>
        /// Adds an airport or navaid for the user.  Returns one of 3 things:
        ///  - An exception, if something goes wrong
        ///  - An HTML table enumerating potential duplicates, if fForceAdd is false and there are potential dupes
        ///  - An empty result, which indicates success.
        /// </summary>
        /// <param name="Code"></param>
        /// <param name="FacilityName"></param>
        /// <param name="TypeCode"></param>
        /// <param name="Latitude"></param>
        /// <param name="Longitude"></param>
        /// <param name="fAsAdmin">Indicates if you are trying to add as an admin (no sourceusername; makes the airport native)</param>
        /// <param name="fForceAdd">True to ignore potential duplicates</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [Authorize]
        [HttpPost]
        public ActionResult AddAirport(string Code, string FacilityName, string TypeCode, double Latitude, double Longitude, bool fAsAdmin, bool fForceAdd)
        {
            return SafeOp(() =>
            {
                fAsAdmin = fAsAdmin && MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData;

                airport ap = new airport(Code, FacilityName, Latitude, Longitude, TypeCode, string.Empty, 0, fAsAdmin ? string.Empty : User.Identity.Name);
                if (!ap.FValidate())
                    throw new InvalidOperationException(ap.ErrorText);

                List<airport> lstDupes = lstDupes = ap.IsPort ? new List<airport>(airport.AirportsNearPosition(ap.LatLong.Latitude, ap.LatLong.Longitude, 20, ap.FacilityTypeCode.CompareCurrentCultureIgnoreCase("H") == 0)) : new List<airport>();
                lstDupes.RemoveAll(a => !a.IsPort || a.Code.CompareCurrentCultureIgnoreCase(ap.Code) == 0 || a.DistanceFromPosition > 3);

                if (lstDupes.Count != 00 && !fForceAdd)
                {
                    ViewBag.dupes = lstDupes;
                    return PartialView("_dupeAirportList");
                }

                // if we are here, then the airport is valid and either has no dupes OR is a force add.
                if (ap.FCommit(fAsAdmin, fAsAdmin))
                {
                    if (lstDupes.Count != 0)
                        ap.ADMINReviewPotentialDupe(lstDupes);
                    return new EmptyResult();
                }
                else
                    throw new InvalidOperationException(ap.ErrorText);
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult UpdateUserAirportList(bool fAdmin)
        {
            fAdmin = fAdmin && MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData;

            return AirportTableForUser(fAdmin);
        }

        [Authorize]
        [HttpPost]
        public ActionResult DeleteAirport(string Code, string TypeCode, bool fAdmin)
        {
            fAdmin = fAdmin && MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData;

            return SafeOp(() =>
            {
                airport.DeleteAirportForUser(User.Identity.Name, Code, TypeCode, fAdmin);
                return AirportTableForUser(fAdmin);
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult ImportUploadedFile(bool fAllowBlast)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("No file uploaded");
                List<airportImportCandidate> lst = new List<airportImportCandidate>(airportImportCandidate.Candidates(Request.Files[0].InputStream, GetIntParam("khack", 0) == 0));

                ViewBag.importCandidates = lst;
                ViewBag.allowBlast = fAllowBlast;
                return PartialView("_importResultsTable");
            });
        }

        [Authorize]
        [HttpPost]
        public string BulkImportUploadedFile()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return String.Format(CultureInfo.CurrentCulture, "{0:#,##0} airports added", AdminAirport.BulkImportAirports(Request.Files[0].InputStream));
            });
        }
        #endregion

        [ChildActionOnly]
        public ActionResult UserAirportList(bool fAdmin)
        {
            return AirportTableForUser(fAdmin);
        }

        [ChildActionOnly]
        public ActionResult ImportCandidate(airport ap, airportImportCandidate.MatchStatus ms, airportImportCandidate aicBase)
        {
            ViewBag.ap = ap;
            ViewBag.ms = ms;
            ViewBag.aicBase = aicBase;
            return PartialView("_airportImportCandidate");
        }

        private PartialViewResult AirportTableForUser(bool fAdmin)
        {
            fAdmin = fAdmin && MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData;
            ViewBag.isAdminMode = fAdmin;
            ViewBag.userAirports = airport.AirportsForUser(User.Identity.Name, fAdmin);
            return PartialView("_userAirports");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Edit()
        {
            ViewBag.isAdminMode = (GetIntParam("a", 0) != 0) && MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData;
            GoogleMap googleMap = new GoogleMap("divAirport", GMap_Mode.Dynamic)
            {
                ClickHandler = "function (point) {clickForAirport(point.latLng);}"
            };
            googleMap.SetAirportList(new AirportList(String.Empty));
            ViewBag.Map = googleMap;
            ViewBag.allowBlast = GetIntParam("blast", 0) != 0;
            return View("addEditAirport");
        }

        #region Admin airport management
        [Authorize]
        [HttpPost]
        public ActionResult UseGuess(string szCode, string szTypeCode, string szCountry, string szAdmin)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                if (String.IsNullOrEmpty(szCode))
                    throw new ArgumentNullException(nameof(szCode));
                List<airport> lst = new List<airport>();
                lst.AddRange(airport.AirportsMatchingCodes(new string[] { szCode }));
                lst.RemoveAll(ap1 => ap1.FacilityTypeCode.CompareOrdinalIgnoreCase(szTypeCode) != 0);
                if (lst.Count == 1)
                    lst[0].SetLocale(szCountry, szAdmin);
                return new EmptyResult();
            });
        }

        /// <summary>
        /// Finds potentially duplicate airports for review that can be reviewed to coalesce or mark one as preferred
        /// </summary>
        /// <param name="start">Starting point</param>
        /// <param name="count">Number to return</param>
        /// <param name="dupeSeed">If provided, limits to dupes near this "seed" airport.</param>
        /// <returns>Array of airport clusters, where each cluster is a list of airports that looke like aliases of one another</returns>
        [Authorize]
        [HttpPost]
        public ActionResult GetDupeAirports(int start, int count, string dupeSeed)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return Json(AdminAirport.GetDupeAirports(start, count, dupeSeed));
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult AirportImportCommand(airportImportCandidate aic, string source, string szCommand)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return Json(airportImportCandidate.AirportImportRowCommand(aic, source, szCommand));
            });
        }
        #endregion
        #endregion

        #region Admin - Airport Geocoder
        [Authorize]
        [HttpPost]
        public ActionResult SuggestCountries(string prefixText, int count)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return Json(AdminAirport.SuggestCountries(prefixText, count));
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult SuggestAdmin(string prefixText, int count)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return Json(AdminAirport.SuggestAdmin1(prefixText, count));
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult GeotTagFromGPX()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("No file uploaded");

                AdminAirport.GeoLocateFromGPX(Request.Files[0].InputStream, out string szAudit, out string szCommands);
                return Json(new { audit = szAudit, commands = szCommands });
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult GeotTagTable(string queryID, int start, int count)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                IEnumerable<AdminAirport> lst = (IEnumerable<AdminAirport>)Session[queryID] ?? throw new InvalidOperationException("Invalid query ID - possibly expired session?  Try refreshing");
                ViewBag.queryID = queryID;
                ViewBag.start = start;
                ViewBag.end = Math.Min(start + count, lst.Count());
                ViewBag.rgap = lst;
                ViewBag.pageSize = 15;
                ViewBag.resultCount = lst.Count();
                return PartialView("_airportsNeedingCodingTable");
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Geocoder(string countryRestriction, bool? fEmptyCountry, string admin1Restriction, bool? fEmptyAdmin1, int startAt, int count)
        {
            CheckAuth(ProfileRoles.maskCanManageData);
            // treat empty country and admin1 as requesting empty (null) values
            if (String.IsNullOrWhiteSpace(countryRestriction) && String.IsNullOrEmpty(admin1Restriction))
                fEmptyCountry = fEmptyAdmin1 = true;

            IEnumerable<AdminAirport> lst = AdminAirport.AirportsMatchingGeoReference(fEmptyCountry == null ? countryRestriction : null, fEmptyAdmin1 == null ? admin1Restriction : null, startAt, count);
            foreach (AdminAirport ap in lst)
            {
                if (String.IsNullOrWhiteSpace(ap.Country) || String.IsNullOrEmpty(ap.Admin1))
                {
                    IEnumerable<airport> rgNearby = airport.AirportsNearPosition(ap.LatLong.Latitude, ap.LatLong.Longitude, 10, true);
                    foreach (airport apClosest in rgNearby)
                    {
                        if (!String.IsNullOrEmpty(apClosest.Country))
                        {
                            ap.NearestTagged = apClosest;
                            break;
                        }
                    }
                }
            }
            string queryID = String.Format(CultureInfo.InvariantCulture, "geo{0}-{1}-{2}-{3}-{4}-{5}", countryRestriction, fEmptyCountry.HasValue ? "true" : "null", admin1Restriction, fEmptyAdmin1.HasValue ? "true" : "null", startAt, count);
            Session[queryID] = lst;
            ViewBag.queryID = queryID;
            ViewBag.rgap = lst;
            ViewBag.start = 0;  // index WITHIN this set of airports
            ViewBag.end = lst.Count();  // number of airports actually availalbe within the dataset
            ViewBag.nextStart = (lst.Count() < count) ? 0 : startAt + count;    // index to start for the next set of results.  Start where this one left off unless we got less than count (indicating exhaustion of list), in which case go back to 0

            ViewBag.countryRestriction = countryRestriction;
            ViewBag.fEmptyCountry = (fEmptyCountry ?? false) ? "checked" : string.Empty;
            ViewBag.admin1Restriction = admin1Restriction;
            ViewBag.fEmptyAdmin1 = (fEmptyAdmin1 ?? false) ? "checked" : string.Empty;
            ViewBag.count = count;

            GoogleMap googleMap = new GoogleMap("divFoundAirports", GMap_Mode.Dynamic);
            googleMap.Options.fAutofillPanZoom = googleMap.Options.fAutofillHeliports = true;
            googleMap.Options.fShowRoute = false;
            googleMap.SetAirportList(new AirportList(lst));
            googleMap.Options.fAutofillPanZoom = (fEmptyCountry.HasValue && fEmptyCountry.Value);

            ViewBag.map = googleMap;
            return View("adminGeocoder");
        }

        [Authorize]
        public ActionResult Geocoder()
        {
            CheckAuth(ProfileRoles.maskCanManageData);

            ViewBag.start = ViewBag.nextStart = 0;
            ViewBag.countryRestriction = string.Empty;
            ViewBag.fEmptyCountry = string.Empty;
            ViewBag.admin1Restriction = string.Empty;
            ViewBag.fEmptyAdmin1 = string.Empty;
            ViewBag.count = 250;

            GoogleMap googleMap = new GoogleMap("divFoundAirports", GMap_Mode.Dynamic);
            googleMap.Options.fAutofillPanZoom = googleMap.Options.fAutofillHeliports = true;
            googleMap.Options.fShowRoute = false;

            ViewBag.map = googleMap;
            return View("adminGeocoder");
        }
        #endregion

        #region Duplicate Management
        /// <summary>
        /// Deletes a user airport that matches a built-in airport
        /// </summary>
        /// <returns>0 if unknown.</returns>
        [Authorize]
        [HttpPost]
        public ActionResult DeleteDupeUserAirport(string idDelete, string idMap, string szUser, string szType)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                AdminAirport.DeleteUserAirport(idDelete, idMap, szUser, szType);
                return new EmptyResult();
            });
        }

        /// <summary>
        /// Sets the preferred flag for an airport
        /// </summary>
        [Authorize]
        [HttpPost]
        public ActionResult SetPreferred(string szCode, string szType, bool fPreferred)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                AdminAirport ap = AdminAirport.AirportWithCodeAndType(szCode, szType) ?? throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Airport {0} (type {1}) not found", szCode, szType));
                ap.SetPreferred(fPreferred);
                return new EmptyResult();
            });
        }

        /// <summary>
        /// Makes a user-defined airport native (i.e., eliminates the source username; accepted as a "true" airport)
        /// </summary>
        [Authorize]
        [HttpPost]
        public ActionResult MakeNative(string szCode, string szType)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                AdminAirport ap = AdminAirport.AirportWithCodeAndType(szCode, szType) ?? throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Airport {0} (type {1}) not found", szCode, szType));
                ap.MakeNative();
                return new EmptyResult();
            });
        }

        /// <summary>
        /// Copies the latitude/longitude from the source airport to the target airport.
        /// </summary>
        [Authorize]
        [HttpPost]
        public ActionResult MergeWith(string szCodeTarget, string szTypeTarget, string szCodeSource)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                AdminAirport apTarget = AdminAirport.AirportWithCodeAndType(szCodeTarget, szTypeTarget) ?? throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Target Airport {0} (type {1}) not found", szCodeTarget, szTypeTarget));
                AdminAirport apSource = AdminAirport.AirportWithCodeAndType(szCodeSource, szTypeTarget) ?? throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Source Airport {0} (type {1}) not found", szCodeSource, szTypeTarget));
                apTarget.MergeFrom(apSource);
                return new EmptyResult();
            });
        }
        #endregion

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
                Response.StatusCode = (int)HttpStatusCode.ExpectationFailed;
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
        [HttpPost]
        public string EstimateDistance(FlightQuery fq, bool df = false, string skID = null)
        {
            return SafeOp(() =>
            {
                if (fq == null)
                    throw new ArgumentNullException(nameof(fq));

                CheckCanViewData(fq, skID);

                double distance = LogbookEntryDisplay.DistanceFlownByUser(fq, df, out string szErr);

                return (String.IsNullOrEmpty(szErr)) ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.VisitedAirportsDistanceEstimate, distance) : szErr;
            });
        }

        /// <summary>
        /// Returns the distance (airport-to-airport) flown on a route, in nm
        /// </summary>
        /// <param name="route">The route field</param>
        /// <returns>An integer in nm for the estimated distance</returns>
        [Authorize]
        [HttpPost]
        public ActionResult GetDistanceFlown(string route)
        {
            return SafeOp(() =>
            {
                return Json(String.IsNullOrEmpty(route) ? 0 : (int)new AirportList(route).DistanceForRoute());
            });
        }

        private void CheckCanViewData(FlightQuery fq, string skID = null)
        {
            // Can view airport data if:
            // a) Authenticated AND your data OR
            if (User.Identity.IsAuthenticated && fq.UserName.CompareOrdinal(User.Identity.Name) == 0)
                return;

            // b) valid sharekey that gives visited airports AND matches the query's user.
            ShareKey sk = skID == null ? null : ShareKey.ShareKeyWithID(skID);
            if (sk == null || !sk.CanViewVisitedAirports || sk.Username.CompareOrdinal(fq.UserName) != 0)
                throw new UnauthorizedAccessException("You aren't authorized to view this data");
        }

        public ActionResult DownloadKML(string fq, string id = null)
        {
            FlightQuery query = FlightQuery.FromBase64CompressedJSON(fq);

            CheckCanViewData(query, id);

            using (MemoryStream ms = new MemoryStream())
            {
                DataSourceType dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.KML);
                string error = string.Empty;
                FlightData.AllFlightsAsKML(query, new AirportList(LogbookEntryDisplay.AllRoutesAsOne(query)), ms, out error);
                return File(ms.ToArray(), dst.Mimetype, String.Format(CultureInfo.CurrentCulture, "{0}-Visited.{1}", Branding.CurrentBrand.AppName, dst.DefaultExtension));
            }
        }

        public ActionResult DownloadVisited(string fq, string id = null)
        {
            FlightQuery query = FlightQuery.FromBase64CompressedJSON(fq);
            CheckCanViewData(query, id);

            IEnumerable<VisitedAirport> rgva = VisitedAirport.VisitedAirportsFromVisitors(LogbookEntryDisplay.GetPotentialVisitsForQuery(User.Identity.Name, query));

            return File(VisitedAirport.DownloadVisitedTable(rgva), "text/csv", String.Format(CultureInfo.InvariantCulture, "{0}-Visited-{1}.csv", Branding.CurrentBrand.AppName, DateTime.Now.YMDString()));
        }

        [ChildActionOnly]
        public PartialViewResult VisitedAirportTable(IEnumerable<VisitedAirport> rgva, bool fAllowSearch = true)
        {
            ViewBag.rgva = rgva;
            ViewBag.fAllowSearch = fAllowSearch;
            return PartialView("_visitedAirportTable");
        }

        private ViewResult VisitedAirportViewForQuery(FlightQuery fq)
        {
            ViewBag.query = fq;
            IEnumerable<VisitedAirport> rgva = VisitedAirport.VisitedAirportsFromVisitors(LogbookEntryDisplay.GetPotentialVisitsForQuery(User.Identity.Name, fq));
            ViewBag.visitedAirports = rgva;
            AirportList alMatches = new AirportList(rgva);
            bool fShowRoute = GetIntParam("path", 0) != 0;
            GoogleMap map = new GoogleMap("divMapVisited", GMap_Mode.Dynamic)
            {
                Airports = AirportList.PathsForQuery(fq, alMatches, fShowRoute)
            };
            map.Options.fShowRoute = fShowRoute;
            ViewBag.Map = map;

            return View("visitedAirports");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize]
        public ActionResult VisitedAirports(string fqJSON, bool fPropDeleteClicked = false, string propToDelete = null, bool df = false)
        {
            FlightQuery fq = String.IsNullOrEmpty(fqJSON) ? new FlightQuery(User.Identity.Name) : JsonConvert.DeserializeObject<FlightQuery>(fqJSON);

            if (fq.UserName.CompareOrdinal(User.Identity.Name) != 0)
                throw new UnauthorizedAccessException();

            if (fPropDeleteClicked)
                fq.ClearRestriction(propToDelete ?? string.Empty);
            ViewBag.df = df;

            return VisitedAirportViewForQuery(fq);
        }

        [HttpGet]
        [Authorize]
        public ActionResult VisitedAirports(string fq)
        {
            FlightQuery _fq = String.IsNullOrEmpty(fq) ? new FlightQuery(User.Identity.Name) : FlightQuery.FromBase64CompressedJSON(fq);
            if (_fq.UserName.CompareOrdinal(User.Identity.Name) != 0)
                throw new UnauthorizedAccessException();
            ViewBag.df = GetIntParam("df", 0) != 0;
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

                locations = new VisitedLocations(VisitedAirport.VisitedAirportsFromVisitors(LogbookEntryDisplay.GetPotentialVisitsForQuery(User.Identity.Name, fq)));
            }

            ViewBag.DataToMap = JsonConvert.SerializeObject(locations, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore });
            ViewBag.TimelineData = JsonConvert.SerializeObject(locations.BuildTimeline());
            return View("visitedMap");
        }
        #endregion

        [HttpPost]
        public ActionResult AirportsInBoundingBox(double latSouth, double lonWest, double latNorth, double lonEast, bool fIncludeHeliports = false)
        {
            IEnumerable<airport> result = Request.IsLocal || (Request.UrlReferrer?.Host ?? string.Empty).EndsWith(Branding.CurrentBrand.HostName, StringComparison.OrdinalIgnoreCase) ?
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

            // Dedupe and remove everything that isn't an airport.
            List<airport> lst = new List<airport>(new AirportList(airports ?? Array.Empty<airport>()).UniqueAirports);
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
            VisitedRoute vr = (VisitedRoute)Session[sessKeyXMLDownload];
            return (vr == null) ? (ActionResult)Content("No visited routes to download") : File(vr.SerializeXML().UTF8Bytes(), "text/xml", "visitedroutes.xml");
        }

        [HttpPost]
        public ActionResult VisitedRoutesFromFile()
        {
            if (!MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            if (Request.Files.Count == 0)
                throw new InvalidOperationException("No file uploaded");

            string sz = string.Empty;
            using (StreamReader sr = new StreamReader(Request.Files[0].InputStream))
                sz = sr.ReadToEnd();

            VisitedRoute vr = sz.DeserializeFromXML<VisitedRoute>();
            return Content(JsonConvert.SerializeObject(vr));
        }

        [Authorize]
        [HttpPost]
        public ActionResult VisitedRoutesForRoute(string szRoute, int maxSegments = 10)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                VisitedRoute vr = new VisitedRoute(szRoute);
                vr.Refresh(maxSegments);
                return Json(vr);
            });
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

        [HttpPost]
        [Authorize]
        public ActionResult METARAutoCompletion(string prefixText, int count)
        {
            return SafeOp(() =>
            {
                List<string> lst = new List<string>();
                if ((prefixText ?? string.Empty).Length > 3)
                {
                    prefixText = prefixText.Substring(1);   // remove leading slash
                    IEnumerable<METAR> rg = ADDSService.LatestMETARSForAirports(prefixText, false);
                    foreach (METAR m in rg)
                        lst.Add(m.raw_text);

                    if (lst.Count > count)
                        lst.RemoveRange(count, lst.Count - count);
                }
                return Json(lst);
            });
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
            map.Options.MapID = LocalConfig.SettingForKey("GoogleMapID");
            return PartialView("_mapContainer");
        }

        // GET: mvc/Airport
        public ActionResult Index()
        {
            return Redirect("MapRoute");
        }
    }
}