using MyFlightbook.Currency;
using MyFlightbook.StartingFlights;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class ImportController : AdminControllerBase
    {
        #region Web services
        #region Starting Totals
        [Authorize]
        [HttpPost]
        public ActionResult StartingTotalsFormForMode(RepresentativeAircraft.RepresentativeTypeMode mode)
        {
            return SafeOp(() =>
            {
                ViewBag.startingFlights = StartingFlight.StartingFlightsForUser(User.Identity.Name, mode);
                ViewBag.mode = mode;
                return PartialView("_startingTotalsForm");
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PreviewStartingTotals(string startingTotalRows, RepresentativeAircraft.RepresentativeTypeMode mode, string startingTotalDate)
        {
            return SafeOp(() =>
            {
                IEnumerable<StartingFlight> flights = StartingFlight.DeserializeStartingFlights(startingTotalRows, User.Identity.Name, mode, DateTime.TryParse(startingTotalDate, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : DateTime.Now.AddYears(-1));
                IEnumerable<TotalsItem>[] rgti = StartingFlight.BeforeAndAfterTotalsForUser(flights, User.Identity.Name);
                ViewBag.beforeTotals = rgti[0];
                ViewBag.afterTotals = rgti[1];

                // add the flights
                return PartialView("_startingTotalsPreview");
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CommitStartingTotals(string startingTotalRows, RepresentativeAircraft.RepresentativeTypeMode mode, string startingTotalDate)
        {
            return SafeOp(() =>
            {
                StartingFlight.CommitStartingFlights(StartingFlight.DeserializeStartingFlights(startingTotalRows, User.Identity.Name, mode, DateTime.TryParse(startingTotalDate, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : DateTime.Now.AddYears(-1)));
                return new EmptyResult();
            });
        }
        #endregion

        #region Import Aircraft
        [Authorize]
        [HttpPost]
        public ActionResult UploadAircraftCSV()
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name, true);

                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("No file uploaded");

                Stream s = Request.Files[0].InputStream;
                using (StreamReader sr = new StreamReader(s))
                {
                    AircraftImportParseContext aipc = new AircraftImportParseContext(sr.ReadToEnd(), User.Identity.Name);

                    if (aipc.RowsFound && aipc.MatchResults.Count == 0)
                        throw new MyFlightbookException(Resources.Aircraft.errImportEmptyFile);

                    aipc.ProcessParseResultsForUser(User.Identity.Name);

                    return Json(aipc);
                }
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult ImportSummary(string contextJSON)
        {
            return SafeOp(() =>
            {
                if (contextJSON == null)
                    throw new ArgumentNullException(nameof(contextJSON));

                ViewBag.context = JsonConvert.DeserializeObject<AircraftImportParseContext>(contextJSON);
                return PartialView("_aircraftImportReviewExisting");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult ReviewNewAircraft(string contextJSON)
        {
            return SafeOp(() =>
            {
                if (contextJSON == null)
                    throw new ArgumentNullException(nameof(contextJSON));

                return AircraftReview(JsonConvert.DeserializeObject<AircraftImportParseContext>(contextJSON).AllUnmatched);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult AddExistingAircraft(int aircraftID)
        {
            AircraftUtility.AddExistingAircraftForUser(User.Identity.Name, aircraftID);
            return Content(Resources.Aircraft.ImportAircraftAdded);
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddAllExisting(string contextJSON)
        {
            return SafeOp(() =>
            {
                if (contextJSON == null)
                    throw new ArgumentNullException(nameof(contextJSON));

                AircraftImportParseContext aipc = JsonConvert.DeserializeObject<AircraftImportParseContext>(contextJSON);
                aipc.AddAllExistingAircraftForUser(User.Identity.Name);
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddAllNewAircraft(AircraftImportSpec[] specs, string szJsonMapping)
        {
            return SafeOp(() =>
            {
                if (specs == null)
                    throw new ArgumentNullException(nameof(specs));

                IDictionary<string, MakeModel> d = String.IsNullOrEmpty(szJsonMapping) ? new Dictionary<string, MakeModel>() : JsonConvert.DeserializeObject<Dictionary<string, MakeModel>>(szJsonMapping);

                foreach (AircraftImportSpec spec in specs)
                {
                    try
                    {
                        d = AircraftUtility.AddNewAircraft(User.Identity.Name, spec, d);
                    }
                    catch (MyFlightbookValidationException) { } // just skip for the next one.
                }

                return Json(d);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult AddNewAircraft(AircraftImportSpec spec, string szJsonMapping)
        {
            return SafeOp(() =>
            {
                IDictionary<string, MakeModel> d = String.IsNullOrEmpty(szJsonMapping) ? new Dictionary<string, MakeModel>() : JsonConvert.DeserializeObject<Dictionary<string, MakeModel>>(szJsonMapping);
                d = AircraftUtility.AddNewAircraft(User.Identity.Name, spec, d);
                return Json(d);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult ValidateAircraft(string szTail, int idModel, int instanceType)
        {
            return SafeOp(() =>
            {
                return Content(AircraftUtility.ValidateAircraft(User.Identity.Name, szTail, idModel, instanceType));
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult SuggestFullModelsWithTargets(string prefixText, int count, string contextKey)
        {
            return SafeOp(() =>
            {
                return Json(AircraftUtility.SuggestFullModelsWithTargets(User.Identity.Name, prefixText, count));
            });
        }
        #endregion
        #endregion

        #region Child Views
        #region Starting Totals
        #endregion

        #region ImportAircraft
        [ChildActionOnly]
        public ActionResult AircraftReview(IEnumerable<AircraftImportMatchRow> rgCandidates)
        {
            ViewBag.rgCandidates = rgCandidates;
            return PartialView("_aircraftImportList");
        }
        #endregion
        #endregion

        #region Endpoints
        [Authorize]
        [HttpGet]
        public ActionResult Aircraft()
        {
            return View("importAircraft");
        }

        [Authorize]
        public ActionResult StartingTotals()
        {
            return View("startingTotals");
        }

        [Authorize]
        // GET: mvc/Import
        public ActionResult Index()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}