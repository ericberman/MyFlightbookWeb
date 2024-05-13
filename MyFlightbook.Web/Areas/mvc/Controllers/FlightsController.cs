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
using System.Web;
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
    public class FlightsController : AdminControllerBase
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
        public ActionResult UpdateTelemetryChart(int idFlight, string xData, string yData, string y2Data, double y1Scale, double y2Scale)
        {
            return SafeOp(() =>
            {
                LogbookEntry le = GetFlightToView(idFlight, false);

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
        public ActionResult GetTelemetryAnalysisForUser(int idFlight)
        {
            return SafeOp(() =>
            {
                LogbookEntry le = GetFlightToView(idFlight, false);

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
        public ActionResult GetTotalsForUser(string userName, string viewingUser = null, bool linkItems = true, bool grouped = true, FlightQuery fq = null)
        {
            return SafeOp(() =>
            {
                CheckCanViewFlights(userName, viewingUser);
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
        public async System.Threading.Tasks.Task<ActionResult> PushToCloudahoy(int idFlight, FlightData.SpeedUnitTypes speedUnits, FlightData.AltitudeUnitTypes altUnits)
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
            ViewBag.query = fq ?? throw new ArgumentNullException(nameof(fq));
            ViewBag.flightResults = fr ?? throw new ArgumentNullException(nameof(fr));

            CheckCanViewFlights(targetUser, User.Identity.Name, sk);

            ViewBag.readOnly = readOnly;

            Profile pfTarget = MyFlightbook.Profile.GetUser(targetUser ?? throw new ArgumentNullException(nameof(targetUser)));
            ViewBag.pfTarget = pfTarget;
            ViewBag.pfViewer = MyFlightbook.Profile.GetUser(viewingUser);

            ViewBag.currentRange = currentRange ?? throw new ArgumentNullException(nameof(currentRange));
            ViewBag.sk = sk;
            ViewBag.miniMode = miniMode;

            // Add parameters to the edit link to preserve context on return
            var dictParams = HttpUtility.ParseQueryString(Request.Url.Query);

            // Issue #458: clone and reverse are getting duplicated and the & is getting url encoded, so even edits look like clones
            dictParams.Remove("Clone");
            dictParams.Remove("Reverse");

            // clear out any others that may be defaulted
            dictParams.Remove("fq");
            dictParams.Remove("se");
            dictParams.Remove("so");
            dictParams.Remove("pg");

            if (!fq.IsDefault)
                dictParams["fq"] = fq.ToBase64CompressedJSONString();
            if (fr.CurrentSortKey.CompareCurrentCultureIgnoreCase(LogbookEntry.DefaultSortKey) != 0)
                dictParams["se"] = fr.CurrentSortKey;
            if (fr.CurrentSortDir!= LogbookEntry.DefaultSortDir)
                dictParams["so"] = fr.CurrentSortDir.ToString();
            if (currentRange.PageNum != 0)
                dictParams["pg"] = currentRange.PageNum.ToString(CultureInfo.InvariantCulture);
            ViewBag.contextParams = dictParams.ToString();

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
            int flightsPerPage = pfTarget.GetPreferenceForKey(MFBConstants.keyPrefFlightsPerPage, MFBConstants.DefaultFlightsPerPage);
            if (Session[MFBConstants.keySessLastNewFlight] != null)
            {
                currentRange = fr.RangeContainingFlight(flightsPerPage, (int)Session[MFBConstants.keySessLastNewFlight]);
                Session[MFBConstants.keySessLastNewFlight] = null;
            } else if (currentRange == null)
            {
                string sortExpr = Request["se"] ?? fr.CurrentSortKey;
                SortDirection sortDir = Enum.TryParse(Request["so"], out SortDirection sd) ? sd : fr.CurrentSortDir;
                currentRange = fr.GetResultRange(flightsPerPage, int.TryParse(Request["pg"], NumberStyles.Integer, CultureInfo.CurrentCulture, out int page) ? FlightRangeType.Page : FlightRangeType.First, sortExpr, sortDir, page);
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
                String.Format(CultureInfo.InvariantCulture, "~/Member/StudentLogbook.aspx?student={0}", szFlightOwner).ToAbsolute() :
                "~/Member/LogbookNew.aspx".ToAbsolute();
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
            ViewBag.pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            FlightQuery fq = new FlightQuery(User.Identity.Name);
            ViewBag.fq = fq;
            ViewBag.flightResults = FlightResultManager.FlightResultManagerForUser(User.Identity.Name).ResultsForQuery(fq);
            return View("miniRecents");
        }
        #endregion

        // GET: mvc/Flights
        public ActionResult Index()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}