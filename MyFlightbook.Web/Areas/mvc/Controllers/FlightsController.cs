using MyFlightbook.Airports;
using MyFlightbook.Charting;
using MyFlightbook.Currency;
using MyFlightbook.Histogram;
using MyFlightbook.Mapping;
using MyFlightbook.Telemetry;
using MyFlightbook.Web.Sharing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/
namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    /// <summary>
    /// Flights controller - handles stuff related to the guts of the logbook including:
    ///  * Logbook table
    ///  * Totals
    ///  * Currency
    ///  * Analysis
    /// </summary>
    public class FlightsController : FlightControllerBase
    {
        #region Web Services
        #region Logbook analysis charting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateChartForData(string fqJSON, string selectedBucket, string fieldToGraph, bool fUseHHMM, bool fLinkItems = true, string skID = null)
        {
            return SafeOp(() =>
            {
                FlightQuery fq = FlightQuery.FromJSON(fqJSON);
                CheckCanViewFlights(fq.UserName, User.Identity.IsAuthenticated ? User.Identity.Name : string.Empty, ShareKey.ShareKeyWithID(skID));
                HistogramManager hm = LogbookEntryDisplay.GetHistogramManager(FlightResultManager.FlightResultManagerForUser(fq.UserName).ResultsForQuery(fq).Flights, fq.UserName);
                return ChartForData(hm, selectedBucket, fieldToGraph, fUseHHMM, Request["fIncludeAverage"] != null, fLinkItems);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult GetAnalysisForUser(FlightQuery fq, bool fLinkItems = true)
        {
            return SafeOp(() =>
            {
                if (fq == null)
                    throw new ArgumentNullException(nameof(fq));
                CheckCanViewFlights(fq.UserName, User.Identity.Name);

                return AnalysisForUser(LogbookEntryDisplay.GetHistogramManager(FlightResultManager.FlightResultManagerForUser(fq.UserName).ResultsForQuery(fq).Flights, fq.UserName), fq, fLinkItems, fLinkItems);
            });
        }
        #endregion

        #region Telemetry analysis charting
        [HttpPost]
        [Authorize]
        public ActionResult UpdateTelemetryChart(int idFlight, string xData, string yData, string y2Data, double y1Scale, double y2Scale, bool fAsAdmin)
        {
            return SafeOp(() =>
            {
                // Anyone can *say* they're admin, so treat the flag above as a request.  Trust but verify
                LogbookEntry le = GetFlightToView(idFlight, fAsAdmin);

                using (FlightData fd = new FlightData())
                {
                    if (!fd.ParseFlightData(le))
                    {
                        throw new InvalidOperationException(fd.ErrorString);
                    }
                    GoogleChartData gcd = new GoogleChartData()
                    {
                        SlantAngle = 0,
                        Height = 500,
                        Width = 800,
                        Title = string.Empty,
                        LegendType = GoogleLegendType.bottom,
                        XDataType = GoogleColumnDataType.date,
                        YDataType = GoogleColumnDataType.number,
                        Y2DataType = GoogleColumnDataType.number,
                        ContainerID = "chartAnalysis"
                    };
                    fd.Data.PopulateGoogleChart(gcd, xData, yData, y2Data, y1Scale, y2Scale, out double max, out double min, out double max2, out double min2);
                    ViewBag.ChartData = gcd;
                    ViewBag.maxY = max > double.MinValue && !String.IsNullOrEmpty(gcd.YLabel) ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ChartMaxX, gcd.YLabel, max) : string.Empty;
                    ViewBag.minY = min < double.MaxValue && !String.IsNullOrEmpty(gcd.YLabel) ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ChartMinX, gcd.YLabel, min) : string.Empty;
                    ViewBag.maxY2 = max2 > double.MinValue && !String.IsNullOrEmpty(gcd.Y2Label) ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ChartMaxX, gcd.Y2Label, max2) : string.Empty;
                    ViewBag.minY2 = min2 < double.MaxValue && !String.IsNullOrEmpty(gcd.Y2Label) ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ChartMinX, gcd.Y2Label, min2) : string.Empty;
                    return PartialView("_telemetryAnalysisResult");
                }
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult GetTelemetryAnalysisForUser(int idFlight, bool fAsAdmin)
        {
            return SafeOp(() =>
            {
                // Anyone can *say* they're admin, so treat the flag above as a request.  Trust but verify
                LogbookEntry le = GetFlightToView(idFlight, fAsAdmin);

                using (FlightData fd = new FlightData())
                {
                    if (!fd.ParseFlightData(le))
                    {
                        throw new InvalidOperationException(fd.ErrorString);
                    }
                    ViewBag.defaultX = fd.Data.Columns.Contains("UTC DATETIME") ? "UTC DATETIME" : (fd.Data.Columns.Contains("DATE") ? "DATE" : (fd.Data.Columns.Contains("TIME") ? "TIME" : (fd.Data.Columns.Contains("SAMPLE") ? "SAMPLE" : "")));
                    ViewBag.minIndex = 0;
                    ViewBag.maxIndex = Math.Max(fd.Data.Rows.Count - 1, 0);

                    ViewBag.yCols = fd.Data.YValCandidates;
                    ViewBag.xCols = fd.Data.XValCandidates;
                    ViewBag.idFlight = idFlight;
                    ViewBag.hasCrop = le.GetCropRange(out int _, out int _);
                    ViewBag.fAsAdmin = fAsAdmin;

                    return PartialView("_telemetryAnalysis");
                }
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult ApplyCropForFlight(int idFlight, int start, int end)
        {
            return SafeOp(() =>
            {
                LogbookEntry le = new LogbookEntry(idFlight, User.Identity.Name, LogbookEntryCore.LoadTelemetryOption.LoadAll);
                if (le.LastError != LogbookEntryCore.ErrorCode.None)
                    throw new UnauthorizedAccessException(le.ErrorString);

                le.CropInRange(start, end);
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult ResetCropForFlight(int idFlight)
        {
            return SafeOp(() =>
            {
                LogbookEntry le = new LogbookEntry(idFlight, User.Identity.Name, LogbookEntryCore.LoadTelemetryOption.LoadAll);
                if (le.LastError != LogbookEntryCore.ErrorCode.None)
                    throw new UnauthorizedAccessException(le.ErrorString);

                le.ResetCrop();
                return new EmptyResult();
            });
        }
        #endregion

        #region Download/Backup
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> SaveToCloud()
        {
            return await SafeOp(async () =>
            {
                string szResult = string.Empty;
                if (Enum.TryParse(Request["saveCloud"] ?? string.Empty, true, out CloudStorage.StorageID sid))
                {
                    bool fIncludeImages = Request["includeImages"] != null;
                    LogbookBackup lb = new LogbookBackup(User.Identity.Name, fIncludeImages);
                    szResult = await lb.BackupToCloudService(sid, fIncludeImages);
                }
                else
                    throw new InvalidOperationException("Invalid cloud storage identifier: " + Request["saveCloud"] ?? string.Empty);

                if (!String.IsNullOrEmpty(szResult))
                    throw new InvalidOperationException(szResult);

                return new EmptyResult();
            });
        }
        #endregion

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult DeleteFlightForUser(string rgFlights)
        {
            return SafeOp(() =>
            {
                foreach (int id in rgFlights.ToInts())
                {
                    if (!LogbookEntryBase.FDeleteEntry(id, User.Identity.Name))
                        throw new UnauthorizedAccessException();
                }
                return new EmptyResult();
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult RevokeSig(int idSignedFlight, string signedFlightOwner)
        {
            return SafeOp(() =>
            {
                LogbookEntry le = new LogbookEntry();
                if (le.FLoadFromDB(idSignedFlight, signedFlightOwner))
                {
                    le.RevokeSignature(User.Identity.Name);
                    return new EmptyResult();
                }
                else
                    throw new UnauthorizedAccessException();
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GetFlightsForResult(string fqJSON, string targetUser, string viewingUser, string sortExpr, SortDirection sortDir, string pageRequest, int pageSize, bool readOnly, string skID = null, string selectedFlights = null, bool miniMode = false)
        {
            return SafeOp(() =>
            {
                FlightQuery fq = FlightQuery.FromJSON(fqJSON);
                if (!String.IsNullOrEmpty(selectedFlights))
                {
                    fq.EnumeratedFlights = new HashSet<int>(selectedFlights.ToInts());
                    fq.Refresh();
                }
                FlightResult fr = FlightResultManager.FlightResultManagerForUser(targetUser).ResultsForQuery(fq);
                FlightResultRange range = fr.GetResultRange(pageSize, FlightRangeType.Search, sortExpr, sortDir, 0, pageRequest);
                return LogbookTableContentsForResults(fq, targetUser, viewingUser, readOnly, fr, range, ShareKey.ShareKeyWithID(skID), miniMode);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult GetTotalsForUser(string userName, string viewingUser = null, bool linkItems = true, bool grouped = true, FlightQuery fq = null, bool fUpdatePref = false)
        {
            return SafeOp(() =>
            {
                CheckCanViewFlights(userName, viewingUser);
                UserTotals ut = new UserTotals() { Username = userName };
                if (fUpdatePref && ut.DefaultGroupModeForUser != grouped)
                    ut.DefaultGroupModeForUser = grouped;
                return TotalsForUser(null, userName, linkItems, grouped, fq);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult GetCurrencyForUser(string userName, string viewingUser, bool linkItems = true, bool useInlineFormatting = false)
        {
            return SafeOp(() =>
            {
                CheckCanViewFlights(userName, viewingUser);
                return CurrencyForUser(null, userName, linkItems, useInlineFormatting);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult RawTelemetryAsTable(int idFlight)
        {
            return SafeOp(() =>
            {
                LogbookEntry le = GetFlightToView(idFlight, false);
                if (le.LastError != LogbookEntryCore.ErrorCode.None)
                    throw new UnauthorizedAccessException(le.ErrorString);

                using (FlightData fd = new FlightData())
                {
                    if (!fd.ParseFlightData(le))
                    {
                        throw new InvalidOperationException(fd.ErrorString);
                    }
                    TelemetryDataTable tdt = fd.Data;
                    return Content(tdt.RenderHtmlTable(), "text/html");
                }
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateDisplayPrefsForUser(bool fCompact, bool fInlineImages, int defaultPageSize)
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                pf.SetPreferenceForKey(MFBConstants.keyPrefCompact, fCompact, !fCompact);
                pf.SetPreferenceForKey(MFBConstants.keyPrefInlineImages, fInlineImages, !fInlineImages);
                int pageSize = Math.Min(Math.Max(defaultPageSize, 1), 99);
                pf.SetPreferenceForKey(MFBConstants.keyPrefFlightsPerPage, pageSize, pageSize == MFBConstants.DefaultFlightsPerPage);
                return new EmptyResult();
            });
        }

        [HttpPost]
        public ActionResult PublicFlightRows(string uid, int skip, int pageSize)
        {
            return SafeOp(() =>
            {
                string szUser = MyFlightbook.Profile.EncryptedUserName(uid);
                IEnumerable<LogbookEntry> rgle = LogbookEntryBase.GetPublicFlightsForUser(szUser, skip, pageSize);

                // Not skipping any, but still no flights found - there must not be any flights!
                // just report an error
                if (skip == 0 && !rgle.Any())
                    throw new InvalidOperationException(Resources.LogbookEntry.PublicFlightNoneFound);

                ViewBag.flights = rgle;
                return PartialView("_publicFlights");
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PushToCloudahoy(int idFlight, FlightData.SpeedUnitTypes speedUnits = FlightData.SpeedUnitTypes.MetersPerSecond, FlightData.AltitudeUnitTypes altUnits = FlightData.AltitudeUnitTypes.Meters)
        {
            return await SafeOp(async () =>
            {
                LogbookEntryDisplay led = new LogbookEntryDisplay(idFlight, User.Identity.Name, LogbookEntryCore.LoadTelemetryOption.LoadAll);
                if (led.LastError != LogbookEntryCore.ErrorCode.None)
                    throw new UnauthorizedAccessException();
                bool f = await led.PushToCloudAhoy(speedUnits, altUnits, !Branding.CurrentBrand.MatchesHost(Request.Url.Host));
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PushToFlySto(int idFlight, FlightData.SpeedUnitTypes speedUnits = FlightData.SpeedUnitTypes.MetersPerSecond, FlightData.AltitudeUnitTypes altUnits = FlightData.AltitudeUnitTypes.Meters)
        {
            return await SafeOp(async () =>
            {
                LogbookEntryDisplay led = new LogbookEntryDisplay(idFlight, User.Identity.Name, LogbookEntryCore.LoadTelemetryOption.LoadAll);
                if (led.LastError != LogbookEntryCore.ErrorCode.None)
                    throw new UnauthorizedAccessException();
                string szLogID = await led.PushToFlySto(speedUnits, altUnits);
                return Content(String.IsNullOrEmpty(szLogID) ? "https://www.flysto.net" : String.Format(CultureInfo.InvariantCulture, "https://www.flysto.net/logs/{0}", szLogID));
            });
        }
        #endregion

        #region Child views
        #region Currency
        [ChildActionOnly]
        public ActionResult CurrencyForUser(IEnumerable<CurrencyStatusItem> rgcsi = null, string userName = null, bool linkItems = true, bool useInlineFormatting = false)
        {
            string szUser = String.IsNullOrEmpty(userName) ? User.Identity.Name : userName;
            ViewBag.rgcsi = rgcsi ?? CurrencyStatusItem.GetCurrencyItemsForUser(szUser);
            ViewBag.linkItems = linkItems;
            ViewBag.userInlineFormatting = useInlineFormatting;
            return PartialView("_currency");
        }
        #endregion

        #region Totals
        [ChildActionOnly]
        public ActionResult TotalsForUser(IEnumerable<TotalsItem> rgti = null, string userName = null, bool linkItems = true, bool grouped = true, FlightQuery fq = null)
        {
            string szUser = String.IsNullOrEmpty(userName) ? User.Identity.Name : userName;
            fq = fq ?? new FlightQuery(szUser);
            if (fq.UserName.CompareCurrentCultureIgnoreCase(szUser) != 0)
                throw new UnauthorizedAccessException("Query for totals doesn't match user");
            if (rgti == null)
            {
                UserTotals ut = new UserTotals(szUser, fq, true);
                ut.DataBind();
                rgti = ut.Totals;
            }
            ViewBag.rgti = rgti;
            ViewBag.linkItems= linkItems;
            ViewBag.grouped = grouped;
            ViewBag.fUseHHMM = MyFlightbook.Profile.GetUser(szUser).UsesHHMM;
            return PartialView("_totals");
        }
        #endregion

        #region Analysis
        [ChildActionOnly]
        public ActionResult AnalysisForUser(HistogramManager hm = null, FlightQuery fq = null, bool linkItems = true, bool canDownload = false, ShareKey sk = null)
        {
            fq = fq ?? new FlightQuery(User.Identity.Name);
            CheckCanViewFlights(fq.UserName, User.Identity.Name, sk);
            if (hm == null)
                hm = LogbookEntryDisplay.GetHistogramManager(FlightResultManager.FlightResultManagerForUser(fq.UserName).ResultsForQuery(fq).Flights, fq.UserName);

            ViewBag.hm = hm;
            ViewBag.canDownload = canDownload;
            ViewBag.useHHMM = MyFlightbook.Profile.GetUser(fq.UserName).UsesHHMM;
            ViewBag.query = fq;
            ViewBag.linkItems = linkItems;
            ViewBag.sk = sk;
            return PartialView("_analysis");
        }

        [ChildActionOnly]
        public ActionResult ChartForData(HistogramManager hm, string selectedBucket, string fieldToGraph, bool fUseHHMM, bool fIncludeAverage = false, bool fLinkItems = true)
        {
            if (hm == null)
                throw new ArgumentNullException(nameof(hm));

            GoogleChartData gcd = new GoogleChartData()
            {
                ChartType = GoogleChartType.ColumnChart,
                Chart2Type = GoogleSeriesType.line,
                SlantAngle = 90,
                Height = 340,
                Title = string.Empty,
                LegendType = GoogleLegendType.bottom,
                XDataType = GoogleColumnDataType.date,
                YDataType = GoogleColumnDataType.number,
                Y2DataType = GoogleColumnDataType.number,
                ShowAverage = fIncludeAverage,
                AverageFormatString = Resources.LocalizedText.AnalysisAverageFormatString,
                Width = 800,
                ContainerID = "chartAnalysis"
            };

            BucketManager bm = hm.PopulateChart(gcd, selectedBucket, fieldToGraph, fUseHHMM, fIncludeAverage);

            if (!fLinkItems)
                gcd.ClickHandlerJS = string.Empty;

            ViewBag.ChartData = gcd;
            ViewBag.yearlySummary = (bm is YearMonthBucketManager ybm && ybm.Buckets.Any()) ? ybm.ToYearlySummary() : Array.Empty<MonthsOfYearData>();
            ViewBag.fUseHHMM = fUseHHMM;
            ViewBag.linkItems = fLinkItems;
            ViewBag.bm = bm;
            ViewBag.hm = hm;
            ViewBag.hv = hm.Values.FirstOrDefault(h => h.DataField.CompareOrdinal(fieldToGraph) == 0);
            ViewBag.rawData = bm.RenderHTML(hm, fLinkItems);
            return PartialView("_analysisResult");
        }
        #endregion

        #region Flights Table and context menu
        [ChildActionOnly]
        public ActionResult SignatureBlock(LogbookEntryDisplay led, bool fUseHHMM, bool fInteractive = false)
        {
            ViewBag.led = led;
            ViewBag.fUseHHMM = fUseHHMM;
            ViewBag.fInteractive = fInteractive;
            return PartialView("_signatureBlock");
        }

        [ChildActionOnly]
        public ActionResult FlightContextMenu(LogbookEntry le, string contextParams)
        {
            ViewBag.le = le ?? throw new ArgumentNullException(nameof(le));
            ViewBag.contextParams = contextParams ?? string.Empty;
            return PartialView("_flightContextMenu");
        }

        [ChildActionOnly]
        public ActionResult LogbookTableContentsForResults(
            FlightQuery fq,
            string targetUser,
            string viewingUser,
            bool readOnly,
            FlightResult fr,
            FlightResultRange currentRange,
            ShareKey sk = null,
            bool miniMode = false
            )
        {
            ViewBag.query = fq ?? throw new ArgumentNullException(nameof(fq), "Flight query passed to content for results is null!");
            ViewBag.flightResults = fr ?? throw new ArgumentNullException(nameof(fr));

            ViewBag.student = CheckCanViewFlights(targetUser, User.Identity.Name, sk);

            ViewBag.readOnly = readOnly;

            Profile pfTarget = MyFlightbook.Profile.GetUser(targetUser ?? throw new ArgumentNullException(nameof(targetUser)));
            ViewBag.pfTarget = pfTarget;
            ViewBag.pfViewer = MyFlightbook.Profile.GetUser(viewingUser);

            ViewBag.currentRange = currentRange ?? throw new ArgumentNullException(nameof(currentRange));

            UserAircraft ua = new UserAircraft(pfTarget.UserName);
            Dictionary<int, Aircraft> dictAircraft = new Dictionary<int, Aircraft>();
            foreach (LogbookEntry le in fr.FlightsInRange(currentRange))
            {
                if (!dictAircraft.ContainsKey(le.AircraftID))
                {
                    Aircraft ac = ua[le.AircraftID];
                    /*
                    // Issue #1268 Diagnostic: we are sometimes failing when getting an aircraft by ID
                    // Populate aircraft here rather than in the view, and send an alert if things are amiss, with diagnostics that might (hopefully?) help figure out what's going wrong
                    // TODO: Remove (most of) this code when we figure it out.  This is a decently fast way to ensure we've populated aircraft images, though, so keep that?
                    if (ac == null) // this should never, ever happen, but per issue #1268 it is!!
                    {
                        int oldCount = ua.Count;
                        // try again - in case it's a simple cache miss.  Then notify.
                        ua.InvalidateCache();
                        int newCount = ua.Count;
                        ac = ua[le.AircraftID];
                        if (ac == null) 
                        {
                            // yikes!  Wasn't just a cache miss - try loading it from database, adding it if that succeeds
                            ac = new Aircraft(le.AircraftID);
                            if (ac.AircraftID != le.AircraftID)
                                util.NotifyAdminEvent("Issue #1268 - aircraft used in flight does not exist!!!", String.Format(CultureInfo.CurrentCulture, "User: {0}, idFlight: {1}, idAircraft: {2} ({3} - {4}), useraircraftCount was {5}, now {6}", fq.UserName, le.FlightID, le.AircraftID, le.TailNumDisplay ?? string.Empty, le.ModelDisplay ?? string.Empty, oldCount, newCount), ProfileRoles.maskSiteAdminOnly);
                            else
                            {
                                ua.FAddAircraftForUser(new Aircraft(le.AircraftID));
                                util.NotifyAdminEvent("Issue #1268 - aircraft used in flight is not in useraircraft - now added to user's profile!!!", String.Format(CultureInfo.CurrentCulture, "User: {0}, idFlight: {1}, idAircraft: {2} ({3} - {4}), useraircraftCount was {5}, now {6}", fq.UserName, le.FlightID, le.AircraftID, le.TailNumDisplay ?? string.Empty, le.ModelDisplay ?? string.Empty, oldCount, newCount), ProfileRoles.maskSiteAdminOnly);
                            }
                        }
                        else
                            util.NotifyAdminEvent("Issue #1268 - aircraft used in flight is not in useraircraft!!!", String.Format(CultureInfo.CurrentCulture, "User: {0}, idFlight: {1}, idAircraft: {2} ({3} - {4}), useraircraftCount was {5}, now {6}", fq.UserName, le.FlightID, le.AircraftID, le.TailNumDisplay ?? string.Empty, le.ModelDisplay ?? string.Empty, oldCount, newCount), ProfileRoles.maskSiteAdminOnly);
                    }
                    */

                    if (!ac.ImagesHaveBeenFilled)
                        ac.PopulateImages();
                    dictAircraft[le.AircraftID] = ac;
                }
            }
            ViewBag.dictAircraft = dictAircraft;

            ViewBag.sk = sk;
            ViewBag.miniMode = miniMode;
            ViewBag.contextParams = GetContextParams(fq, fr, currentRange);

            return PartialView("_logbookTableContents");
        }

        [ChildActionOnly]
        public ActionResult LogbookTableForResults(
            FlightQuery fq,
            string targetUser,
            bool readOnly = false,
            FlightResult fr = null, 
            ShareKey sk = null, 
            FlightResultRange currentRange = null,
            bool miniMode = false)
        {
            ViewBag.readOnly = readOnly;
            Profile pfTarget = MyFlightbook.Profile.GetUser(targetUser ?? throw new ArgumentNullException(nameof(targetUser)));
            ViewBag.pfTarget = pfTarget;
            ViewBag.pfViewer = User.Identity.IsAuthenticated ? MyFlightbook.Profile.GetUser(User.Identity.Name) : pfTarget;

            if (util.GetIntParam(Request, "dupesOnly", 0) != 0)
            {
                fq = new FlightQuery(targetUser) {
                    EnumeratedFlights = new HashSet<int>(LogbookEntryBase.DupeCandidatesForUser(targetUser)) };
                // force a reset of the cached results since we're changing the query
                currentRange = null;
                fr = null;
            }

            ViewBag.query = fq ?? throw new ArgumentNullException(nameof(fq));

            ViewBag.flightResults = (fr = fr ?? FlightResultManager.FlightResultManagerForUser(targetUser).ResultsForQuery(fq));
            // See if we just entered a new flight and scroll to it as needed
            int flightsPerPage = FlightsPerPageForUser(pfTarget);
            if (Session[MFBConstants.keySessLastNewFlight] != null)
            {
                currentRange = fr.RangeContainingFlight(flightsPerPage, (int)Session[MFBConstants.keySessLastNewFlight]);
                Session[MFBConstants.keySessLastNewFlight] = null;
            } else if (currentRange == null)
            {
                string sortExpr = Request["se"] ?? fr.CurrentSortKey;
                SortDirection sortDir = Enum.TryParse(Request["so"], out SortDirection sd) ? sd : fr.CurrentSortDir;
                currentRange = fr.GetResultRange(flightsPerPage, int.TryParse(Request["pg"], out int page) ? FlightRangeType.Page : FlightRangeType.First, sortExpr, sortDir, page);
            }
            ViewBag.currentRange = currentRange;
            ViewBag.sk = sk;
            ViewBag.miniMode = miniMode;
            return PartialView("_logbookTable");
        }
        #endregion
        #endregion

        #region Visible Endpoints
        #region Shared Logbook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DownloadFlyingStats(string fqJSON, string selectedBucket, string skID = null)
        {
            FlightQuery fq = FlightQuery.FromJSON(fqJSON);
            CheckCanViewFlights(fq.UserName, User.Identity.IsAuthenticated ? User.Identity.Name : string.Empty, ShareKey.ShareKeyWithID(skID));
            HistogramManager hm = LogbookEntryDisplay.GetHistogramManager(FlightResultManager.FlightResultManagerForUser(fq.UserName).ResultsForQuery(fq).Flights, fq.UserName);
            BucketManager bm = hm.SupportedBucketManagers.FirstOrDefault(b => b.DisplayName.CompareOrdinal(selectedBucket) == 0);
            bm.ScanData(hm);
            string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}-{3}", Branding.CurrentBrand.AppName, Resources.LocalizedText.DownloadFlyingStatsFilename, MyFlightbook.Profile.GetUser(fq.UserName).UserFullName, DateTime.Now.YMDString().Replace(" ", "-"));
            return File(bm.RenderCSV(hm).UTF8Bytes(), "text/csv", RegexUtility.UnSafeFileChars.Replace(szFilename, string.Empty) + ".csv");
        }

        private ViewResult SharedLogbookForQuery(string g, FlightQuery fq = null, bool fPropDeleteClicked = false, string propToDelete = null)
        {
            if (string.IsNullOrEmpty(g))
                throw new UnauthorizedAccessException();

            ShareKey sk = ShareKey.ShareKeyWithID(g) ?? throw new UnauthorizedAccessException("Invalid share key: " + g);
            ViewBag.sk = sk;

            fq = fq ?? new FlightQuery(sk.Username);

            if (fq.UserName.CompareCurrentCultureIgnoreCase(sk.Username) != 0)
                throw new UnauthorizedAccessException();

            fq.Refresh();   // make sure we have all the right queryfilter items, etc.
            ViewBag.query = fq;

            if (fPropDeleteClicked)
                fq.ClearRestriction(propToDelete ?? string.Empty);

            if (sk.CanViewVisitedAirports)
            {
                IEnumerable<VisitedAirport> rgva = VisitedAirport.VisitedAirportsForQuery(fq);
                ViewBag.rgva = rgva;
                GoogleMap map = new GoogleMap("divMapVisited", GMap_Mode.Dynamic) { Airports = new AirportList[] { new AirportList(rgva) },  };
                map.Options.fShowRoute = false;
                ViewBag.map = map;
                ViewBag.regions = VisitedAirport.VisitedCountriesAndAdmins(rgva ?? Array.Empty<VisitedAirport>());
            }

            if (sk.CanViewCurrency)
                ViewBag.rgcsi = CurrencyStatusItem.GetCurrencyItemsForUser(sk.Username);

            if (sk.CanViewTotals)
            {
                UserTotals ut = new UserTotals(sk.Username, fq, true);
                ut.DataBind();
                ViewBag.rgti = ut.Totals;
                ViewBag.grouped = ut.DefaultGroupModeForUser;
            }

            if (sk.CanViewFlights)
            {
                ViewBag.canDownload = false;
                FlightResult fr = FlightResultManager.FlightResultManagerForUser(sk.Username).ResultsForQuery(fq);
                ViewBag.flightResult = fr;
                ViewBag.hm = LogbookEntryDisplay.GetHistogramManager(fr.Flights, sk.Username);
            }

            return View("sharedLogbook");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SharedLogbook(string g, string fqShared = null, bool fPropDeleteClicked = false, string propToDelete = null)
        {
            // fq here is from the form, so it is raw JSON, if present.
            return SharedLogbookForQuery(g, String.IsNullOrEmpty(fqShared) ? null : FlightQuery.FromJSON(fqShared), fPropDeleteClicked, propToDelete);
        }

        public ActionResult SharedLogbook(string g, string fq = null)
        {
            // fq here is in the URL so it is base64compressed json
            return SharedLogbookForQuery(g, fq == null ? null : FlightQuery.FromBase64CompressedJSON(fq));
        }
        #endregion

        #region Student Logbook
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DownloadLogbookForUser(string szUserName)
        {
            // for now, only allow owner or instructor to call this - i.e., no skID
            if (string.IsNullOrEmpty(szUserName))
                throw new ArgumentNullException(nameof(szUserName));
            Profile student = CheckCanViewFlights(szUserName, User.Identity.Name) ?? throw new UnauthorizedAccessException(Resources.LogbookEntry.errNotAuthorizedToViewLogbook);
            LogbookBackup lb = new LogbookBackup(student);
            return File(lb.WriteLogbookCSVToBytes(), "text/csv", lb.BackupFilename());
        }

        private ViewResult StudentLogbookForQuery(string student, FlightQuery fq = null, bool fPropDeleteClicked = false, string propToDelete = null)
        {
            if (String.IsNullOrEmpty(student))
                throw new UnauthorizedAccessException();

            ViewBag.targetUser = CheckCanViewFlights(student, User.Identity.Name) ?? throw new UnauthorizedAccessException(Resources.LogbookEntry.errNotAuthorizedToViewLogbook);
            ViewBag.viewer = MyFlightbook.Profile.GetUser(User.Identity.Name);
            fq = fq ?? new FlightQuery(student);
            if (fq.UserName.CompareOrdinalIgnoreCase(student) != 0)
                throw new UnauthorizedAccessException();
            fq.Refresh();
            if (fPropDeleteClicked)
                fq.ClearRestriction(propToDelete ?? string.Empty);

            ViewBag.query = fq;
            ViewBag.rgcsi = CurrencyStatusItem.GetCurrencyItemsForUser(student);
            UserTotals ut = new UserTotals(student, fq, true);
            ut.DataBind();
            ViewBag.rgti = ut.Totals;
            ViewBag.grouped = ut.DefaultGroupModeForUser;

            return View("studentlogbook");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult StudentLogbook(string student, string fqShared, bool fPropDeleteClicked = false, string propToDelete = null)
        {
            return StudentLogbookForQuery(student, String.IsNullOrEmpty(fqShared) ? null : FlightQuery.FromJSON(fqShared), fPropDeleteClicked, propToDelete);
        }

        [Authorize]
        public ActionResult StudentLogbook(string student, string fq = null) 
        {
            return StudentLogbookForQuery(student, String.IsNullOrEmpty(fq) ? null : FlightQuery.FromBase64CompressedJSON(fq));
        }
        #endregion

        #region Public flights
        [HttpGet]
        public ActionResult MyFlights(string uid = null)
        {
            string szUser = MyFlightbook.Profile.EncryptedUserName(uid);

            Profile pf = MyFlightbook.Profile.GetUser(szUser) ?? throw new UnauthorizedAccessException();
            ViewBag.Title = String.IsNullOrEmpty(pf.UserName) ?
                Branding.ReBrand(Resources.LogbookEntry.PublicFlightPageHeaderAll) :
                String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.PublicFlightPageHeader, pf.UserFullName);
            ViewBag.uid = uid;
            return View("myFlights");
        }
        #endregion

        #region Flight Details
        #region Details helpers
        private LogbookEntryDisplay GetFlightToView(int idFlight, bool fAdminMode)
        {
            // Anyone can *say* they're admin, so treat the flag above as a request.  Trust but verify
            bool fIsAdmin = fAdminMode && MyFlightbook.Profile.GetUser(User.Identity.Name).CanSupport;

            // Check to see if the requested flight belongs to the current user, or if they're authorized.
            string szFlightOwner = LogbookEntryBase.OwnerForFlight(idFlight);
            if (String.IsNullOrEmpty(szFlightOwner))
                throw new UnauthorizedAccessException(Resources.LogbookEntry.errNoSuchFlight);

            // Can view if we are an admin or if we own the flight
            if (!fIsAdmin && szFlightOwner.CompareCurrentCulture(User.Identity.Name) != 0)
                CheckCanViewFlights(szFlightOwner, User.Identity.Name);

            // If we're here, we can view the flight
            return new LogbookEntryDisplay(idFlight, szFlightOwner, LogbookEntryCore.LoadTelemetryOption.LoadAll);
        }

        private FlightQuery ProcessDetailsQuery(string szFlightOwner, string fqCompressedJSON, bool fPropDeleteClicked, string propToDelete)
        {
            FlightQuery query = String.IsNullOrEmpty(fqCompressedJSON) ? new FlightQuery(szFlightOwner) : FlightQuery.FromBase64CompressedJSON(fqCompressedJSON);
            if (query.UserName.CompareCurrentCulture(szFlightOwner) != 0)
                query = new FlightQuery(szFlightOwner);

            if (fPropDeleteClicked)
                query.ClearRestriction(propToDelete ?? string.Empty);

            query.Refresh();
            ViewBag.fq = query;
            return query;
        }

        private void ProcessDetailsFlightDataAndMap(LogbookEntry le)
        {
            using (FlightData fd = new FlightData())
            {
                if (fd.NeedsComputing)
                {
                    if (!fd.ParseFlightData(le))
                        ViewBag.pathError = fd.ErrorString;
                }

                ListsFromRoutesResults lfrr = AirportList.ListsFromRoutes(le.Route);
                ViewBag.lfrr = lfrr;
                GoogleMap gmap = new GoogleMap("divMapDetails", fd.HasLatLongInfo && fd.Data.Rows.Count > 1 ? GMap_Mode.Dynamic : GMap_Mode.Static)
                {
                    Airports = lfrr.Result,
                    Path = fd.GetPath(),
                    Images = le.FlightImages
                };
                gmap.Options.fShowMarkers = true;
                gmap.Options.fShowRoute = true;
                ViewBag.Map = gmap;
                ViewBag.distanceDisplay = le.GetPathDistanceDescription(fd.ComputePathDistance());
            }
        }
        private FileContentResult DownloadTelemetryForFlight(LogbookEntry le, DownloadFormat downloadFormat, FlightData.SpeedUnitTypes speedUnits = FlightData.SpeedUnitTypes.Knots, FlightData.AltitudeUnitTypes altUnits = FlightData.AltitudeUnitTypes.Feet)
        {
            using (FlightData fd = new FlightData())
            {
                byte[] rgb = fd.WriteToFormat(le, downloadFormat, speedUnits, altUnits, out DataSourceType dst);
                string szFileName = RegexUtility.UnSafeFileChars.Replace(String.Format(CultureInfo.InvariantCulture, "Data{0}-{1}-{2}", Branding.CurrentBrand.AppName, MyFlightbook.Profile.GetUser(le.User).UserFullName, le.Date.YMDString()), string.Empty) + "." + dst.DefaultExtension;
                return File(rgb, dst.Mimetype, szFileName);

            }
        }
        #endregion

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DownloadTelemetry(int idFlight, DownloadFormat downloadFormat, FlightData.SpeedUnitTypes speedUnits = FlightData.SpeedUnitTypes.MetersPerSecond, FlightData.AltitudeUnitTypes altUnits = FlightData.AltitudeUnitTypes.Meters, bool asAdmin = false)
        {
            return DownloadTelemetryForFlight(GetFlightToView(idFlight, asAdmin), downloadFormat, speedUnits, altUnits);
        }

        [Authorize]
        public ActionResult Details(int id = -1, string fq = null, int a = 0, int d = 0, string tabID = null, bool fPropDeleteClicked = false, string propToDelete = null)
        {
            if (id <= 0)
                throw new UnauthorizedAccessException();

            bool fIsAdmin = a != 0 && MyFlightbook.Profile.GetUser(User.Identity.Name).CanSupport;
            ViewBag.fIsAdmin = fIsAdmin;
            // GetFlightToView will throw if we're not authorized
            LogbookEntryDisplay led = GetFlightToView(id, fIsAdmin);
            led.PopulateImages(true);
            ViewBag.led = led;
            string szFlightOwner = led.User;

            // d = direct download - always in original format
            if (d != 0)
                return DownloadTelemetryForFlight(led, DownloadFormat.Original);

            Profile pfFlightOwner = MyFlightbook.Profile.GetUser(szFlightOwner);
            ViewBag.useHHMM = MyFlightbook.Profile.GetUser(User.Identity.Name).UsesHHMM;
            ViewBag.pf = pfFlightOwner;

            // Set up the return link
            bool isViewingStudent = !fIsAdmin && szFlightOwner.CompareCurrentCulture(User.Identity.Name) != 0;
            ViewBag.returnLink = isViewingStudent ?
                String.Format(CultureInfo.InvariantCulture, "~/mvc/flights/studentlogbook?student={0}", szFlightOwner).ToAbsolute() :
                "~/mvc/flights".ToAbsolute();
            ViewBag.returnLinkText = isViewingStudent ?
                String.Format(CultureInfo.CurrentCulture, Resources.Profile.ReturnToStudent, pfFlightOwner.UserFullName) :
                Resources.LogbookEntry.flightDetailsReturnToLogbook;

            // Set up the query, possibly from the passed query, possibly handling any deleted items.
            FlightQuery query = ProcessDetailsQuery(szFlightOwner, fq, fPropDeleteClicked, propToDelete);
            string fqNew = query.ToBase64CompressedJSONString();

            // Get neighbors - this will also preheat the cache
            int[] neighbors = FlightResultManager.FlightResultManagerForUser(szFlightOwner).ResultsForQuery(query).NeighborsOfFlight(id);
            ViewBag.prevDest = neighbors[0] > 0 ? String.Format(CultureInfo.InvariantCulture, "~/mvc/flights/details/{0}?fq={1}{2}", neighbors[0], fqNew, a == 0 ? string.Empty : "&a=1").ToAbsolute() : string.Empty;
            ViewBag.nextDest = neighbors[1] > 0 ? String.Format(CultureInfo.InvariantCulture, "~/mvc/flights/details/{0}?fq={1}{2}", neighbors[1], fqNew, a == 0 ? string.Empty : "&a=1").ToAbsolute() : string.Empty;

            ViewBag.defaultPane = tabID ?? "Flight";

            ProcessDetailsFlightDataAndMap(led);

            return View("flightDetails");
        }
        #endregion

        #region Mini mode
        [Authorize]
        public ActionResult MiniRecents()
        {
            util.SetMobile(true);
            ViewBag.pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            FlightQuery fq = new FlightQuery(User.Identity.Name);
            ViewBag.fq = fq;
            ViewBag.flightResults = FlightResultManager.FlightResultManagerForUser(User.Identity.Name).ResultsForQuery(fq);
            return View("miniRecents");
        }

        [Authorize]
        public ActionResult MiniTotals()
        {
            util.SetMobile(true);
            ViewBag.pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            FlightQuery fq = new FlightQuery(User.Identity.Name);
            ViewBag.fq = fq;
            UserTotals ut = new UserTotals(User.Identity.Name, fq, true);
            ut.DataBind();
            ViewBag.rgti = ut.Totals;
            ViewBag.grouped = ut.DefaultGroupModeForUser;
            ViewBag.rgcsi = CurrencyStatusItem.GetCurrencyItemsForUser(User.Identity.Name);
            return View("miniTotals");
        }

        [Authorize]
        public ActionResult MiniLogbook()
        {
            util.SetMobile(true);
            return View("miniLogbook");
        }
        #endregion

        #region Backup and Export
        public ActionResult Export(string user, string pass, int csv = 0)
        {
            if (!Request.IsSecureConnection)
                throw new UnauthorizedAccessException("Attempt to call export on an insecure connection");

            // ValidateUser will throw an exception if unauthorized
            ValidateUser(user, pass, out string fixedUser);
            LogbookBackup lb = new LogbookBackup(fixedUser);
            ViewBag.logbookBackup = lb;
            return (csv == 0) ? View("export") : (ActionResult) File(lb.WriteLogbookCSVToBytes(), "text/csv", lb.BackupFilename());
        }

        [Authorize]
        public ActionResult DownloadCSV()
        {
            LogbookBackup lb = new LogbookBackup(User.Identity.Name);
            return File(lb.WriteLogbookCSVToBytes(), "text/csv", lb.BackupFilename());
        }

        [Authorize]
        public ActionResult DownloadImages()
        {
            LogbookBackup lb = new LogbookBackup(User.Identity.Name);
            return File(lb.WriteZipOfImagesToBytes(), "text/csv", LogbookBackup.BackupImagesFilename(fDateStamp: true));
        }

        [Authorize]
        public ActionResult Download()
        {
            return View("download");
        }
        #endregion

        // GET: mvc/Flights
        #region Main Logbook
        private ViewResult MainLogbookInternal(FlightQuery fq, bool fPropDeleteClicked = false, string propToDelete = null)
        {
            if (fq == null)
                throw new ArgumentNullException(nameof(fq));

            if (fq.UserName.CompareOrdinal(User.Identity.Name) != 0)
                throw new UnauthorizedAccessException("invalid query - incorrect username");

            fq.Refresh();
            if (fPropDeleteClicked)
                fq.ClearRestriction(propToDelete ?? string.Empty);

            ViewBag.fq = fq;
            ViewBag.grouped = new UserTotals(User.Identity.Name, fq, true).DefaultGroupModeForUser;
            ViewBag.flightResult = FlightResultManager.FlightResultManagerForUser(User.Identity.Name).ResultsForQuery(fq);
            return View("logbook");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Index(string fq, bool fPropDeleteClicked = false, string propToDelete = null)
        {
            return MainLogbookInternal(FlightQuery.FromJSON(fq), fPropDeleteClicked, propToDelete);
        }

        [Authorize]
        public ActionResult Index(string fq = null)
        {
            FlightQuery q = String.IsNullOrEmpty(fq) ? new FlightQuery(User.Identity.Name) : FlightQuery.FromBase64CompressedJSON(fq);
            // update based on any passed parameters
            q.InitPassedQueryItems(Request["s"], Request["ap"], util.GetIntParam(Request, "y", -1), util.GetIntParam(Request, "m", -1), util.GetIntParam(Request, "w", -1), util.GetIntParam(Request, "d", -1), Request["tn"], Request["mn"], Request["icao"], Request["cc"]);
            return MainLogbookInternal(q);
        }
        #endregion
        #endregion
    }
}