using MyFlightbook.Currency;
using MyFlightbook.ImportFlights;
using MyFlightbook.OAuth;
using MyFlightbook.StartingFlights;
using MyFlightbook.Telemetry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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

                    // Issue #1311 This can be large - use overridden JSON result; it will be treated as an opaque string at the client.
                    return Content(JsonConvert.SerializeObject(aipc));
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

        #region Import Flights
        private const string keyImportFlightSessionRawBytes = "sessImportFlightRawBytes";
        private const string keyImportFlightsImporter = "sessImportImporter";

        [Authorize]
        [HttpPost]
        public ActionResult UploadFlightsCSV()
        {
            return SafeOp(() =>
            {
                if (Request.Files.Count == 0)
                    throw new InvalidOperationException(Resources.LogbookEntry.errImportInvalidCSVFile);

                byte[] rgb = null;
                Stream s = Request.Files[0].InputStream;
                using (StreamReader sr = new StreamReader(s))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        s.CopyTo(ms);
                        rgb = ms.ToArray();
                    }
                }
                Session[keyImportFlightSessionRawBytes] = rgb;
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult ReviewAircraft()
        {
            return SafeOp(() =>
            {
                byte[] rgb = (byte[])Session[keyImportFlightSessionRawBytes];
                int cOriginalSize = rgb.Length;
                Session[keyImportFlightSessionRawBytes] = null; // don't need these any more.

                ExternalFormatConvertResults results = ExternalFormatConvertResults.ConvertToCSV(rgb);
                rgb = results.GetConvertedBytes();
                int cConvertedSize = rgb.Length;
                Session[keyImportFlightSessionRawBytes] = rgb;  // save the converted bytes instead.

                CSVImporter csvimporter = new CSVImporter();
                csvimporter.InitWithBytes(rgb, User.Identity.Name, null, null, null);
                if (csvimporter.FlightsToImport == null | !String.IsNullOrEmpty(csvimporter.ErrorMessage))
                    throw new InvalidOperationException(csvimporter.ErrorMessage);

                EventRecorder.LogCall("Import Preview - User: {user}, Upload size {cbin}, converted size {cbconvert}, flights found: {flightcount}", User.Identity.Name, cOriginalSize, cConvertedSize, csvimporter.FlightsToImport.Count);

                ViewBag.importer = csvimporter;
                ViewBag.results = results;
                return PartialView("_importFlightsReviewAircraft");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult PreviewResults(string autoFillOpt, bool isPendingOnly)
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name, true);

                byte[] rgb = (byte[])Session[keyImportFlightSessionRawBytes] ?? throw new InvalidOperationException(Resources.LogbookEntry.ImportFlightSessionExpired);

                // make a copy of the autofill options so that we don't muck up user's settings in memory
                AutoFillOptions afo = string.IsNullOrEmpty(autoFillOpt) ? null : new AutoFillOptions(AutoFillOptions.DefaultOptionsForUser(User.Identity.Name));
                if (afo != null && Enum.TryParse(autoFillOpt, true, out AutoFillOptions.TimeConversionCriteria tcc))
                {
                    afo.TimeConversion = tcc;
                    if (afo.TimeConversion != AutoFillOptions.TimeConversionCriteria.None)
                        afo.AutoFillTotal = AutoFillOptions.AutoFillTotalOption.EngineTime; // we will rewrite engine times, but not block times.
                    if (afo.TimeConversion == AutoFillOptions.TimeConversionCriteria.Preferred)
                        afo.PreferredTimeZone = pf.PreferredTimeZone;
                }

                Dictionary<int, string> errorContext = new Dictionary<int, string>();
                CSVImporter importer = new CSVImporter();
                importer.InitWithBytes(rgb, User.Identity.Name, null, (le, szContext, iRow) =>
                {
                    // ignore errors if the importer is only pending flights and the error is a logbook validation error (no tail, future date, night, etc.)
                    if (!isPendingOnly || le.LastError == LogbookEntryCore.ErrorCode.None)
                        errorContext[iRow - 1] = szContext; // if we're here, we are *either* not pending only *or* we didn't have a logbookentry validation error (e.g., could be malformed row)
                }, afo);
                Session[keyImportFlightsImporter] = importer;
                ViewBag.errorContext = errorContext;
                ViewBag.importer = importer;
                ViewBag.isPendingOnly = isPendingOnly;
                return PartialView("_importFlightsPreview");
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

        public ActionResult ImportTemplate()
        {
            const string szHeaders = @"Date,Tail Number,Model,Approaches,Hold,Landings,FS Night Landings,FS Day Landings,X-Country,Night,IMC,Simulated Instrument,Ground Simulator,Dual Received,CFI,SIC,PIC,Total Flight Time,Route,Comments";

            return File(Encoding.UTF8.GetBytes(szHeaders.Replace(",", CultureInfo.CurrentCulture.TextInfo.ListSeparator)), "text/csv", Branding.CurrentBrand.AppName + ".csv");
        }

        [Authorize]
        public ActionResult DownloadConverted()
        {
            byte[] rgb = (byte[]) Session[keyImportFlightSessionRawBytes];
            if (rgb != null)
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                return File(rgb, "text/csv", String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, pf.UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-") + ".csv");
            }
            // something went wrong - session expired?  Restart
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ImportExternal(string externalSourceID, string startDate, string endDate)
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ExternalFlightSource source = ExternalFlightSource.SourceForID(externalSourceID, ExternalFlightSource.ExternalSourcesForUser(pf));
            PendingFlight.FlushCacheForUser(User.Identity.Name);    // pre-emptively, since there may not be any context available during the async import

            string result = (source == null) ? "Unknown source ID " + externalSourceID :
                await source.GetClient(pf, Request).ImportFlights(User.Identity.Name,
                DateTime.TryParse(startDate, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dtStart) ? dtStart : (DateTime?)null,
                DateTime.TryParse(endDate, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dtEnd) ? dtEnd : (DateTime?)null,
                Request);

            if (String.IsNullOrEmpty(result))
                return Redirect("~/mvc/flightedit/pending");
            else
            {
                ViewBag.error = result;
                ViewBag.source = source;
                return View("importExternalSource");
            }
        }

        [HttpGet]
        public ActionResult DoImport()
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DoImport(bool isPendingOnly)
        {
            CSVImporter csvimporter = (CSVImporter)Session[keyImportFlightsImporter];
            if (csvimporter == null)
                return Redirect("~/mvc/import/id=csv"); // session expired

            int cFlightsAdded = 0;
            int cFlightsUpdated = 0;
            int cFlightsWithErrors = 0;

            List<string[]> items = new List<string[]>();
            bool hasPending = false;

            csvimporter.FCommit((le, fIsNew) =>
                {
                    if (String.IsNullOrEmpty(le.ErrorString))
                    {
                        items.Add(new string[] { "success", String.Format(CultureInfo.CurrentCulture, fIsNew ? Resources.LogbookEntry.ImportRowAdded : Resources.LogbookEntry.ImportRowUpdated, le.ToString()) });
                        if (fIsNew)
                            cFlightsAdded++;
                        else
                            cFlightsUpdated++;
                    }
                    else
                    {
                        PendingFlight pf = new PendingFlight(le) { User = User.Identity.Name };
                        pf.Commit();
                        hasPending = true;
                        if (!isPendingOnly)
                            items.Add(new string[] { "error", String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.ImportRowAddedPending, le.ToString(), le.ErrorString) });
                        cFlightsWithErrors++;
                    }
                },
                (le, ex) => { items.Add(new string[] { "error", String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.ImportRowNotAdded, le.ToString(), ex.Message) }); },
                true);

            List<string> lstResults = new List<string>();
            if (cFlightsAdded > 0)
                lstResults.Add(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.ImportFlightsAdded, cFlightsAdded));
            if (cFlightsUpdated > 0)
                lstResults.Add(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.ImportFlightsUpdated, cFlightsUpdated));
            if (cFlightsWithErrors > 0)
                lstResults.Add(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.ImportFlightsWithErrors, cFlightsWithErrors));
            ViewBag.importedItems = items;
            ViewBag.hasPending = hasPending;
            ViewBag.resultSummary = lstResults;
            MyFlightbook.Profile.GetUser(User.Identity.Name).SetAchievementStatus();
            Session[keyImportFlightsImporter] = null;
            Session[keyImportFlightSessionRawBytes] = null;
            return View("importResults");
        }

        [Authorize]
        // GET: mvc/Import
        public ActionResult Index(string id)
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            IList<ExternalFlightSource> externalSources = id.CompareCurrentCultureIgnoreCase("csv") == 0 ? new List<ExternalFlightSource>() : ExternalFlightSource.ExternalSourcesForUser(pf);
            ViewBag.externalSources = externalSources;
            ExternalFlightSource source = ExternalFlightSource.SourceForID(id, externalSources);
            if (source == null)
                return externalSources.Count == 0 ? /* (ActionResult) Redirect("~/member/import.aspx") */ View("importCSV") : View("importSelectSource");
            else
            {
                ViewBag.source = source;
                return View("importExternalSource");
            }
        }
        #endregion
    }
}