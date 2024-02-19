using MyFlightbook.Achievements;
using MyFlightbook.Airports;
using MyFlightbook.Charting;
using MyFlightbook.Currency;
using MyFlightbook.Encryptors;
using MyFlightbook.FlightStatistics;
using MyFlightbook.Histogram;
using MyFlightbook.Instruction;
using MyFlightbook.Mapping;
using MyFlightbook.RatingsProgress;
using MyFlightbook.Web.Sharing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
        public ActionResult GetFlightsForResult(string fqJSON, 
            string targetUser, 
            string viewingUser, 
            string sortExpr, 
            SortDirection sortDir, 
            string pageRequest, 
            int pageSize, 
            bool readOnly,
            string skID = null,
            string selectedFlights = null)
        {
            return SafeOp(() =>
            {
                FlightQuery fq = FlightQuery.FromJSON(fqJSON);
                if (!String.IsNullOrEmpty(selectedFlights))
                {
                    fq.EnumeratedFlights = selectedFlights.ToInts();
                    fq.Refresh();
                }
                FlightResult fr = FlightResultManager.FlightResultManagerForUser(targetUser).ResultsForQuery(fq);
                FlightResultRange range = fr.GetResultRange(pageSize, FlightRangeType.Search, sortExpr, sortDir, 0, pageRequest);
                return LogbookTableContentsForResults(fq, targetUser, viewingUser, readOnly, fr, range, ShareKey.ShareKeyWithID(skID));
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
                string szUser = String.IsNullOrEmpty(uid) ? string.Empty : new SharedDataEncryptor(MFBConstants.keyEncryptMyFlights).Decrypt(uid);
                IEnumerable<LogbookEntry> rgle = Array.Empty<LogbookEntry>();
                if (String.IsNullOrEmpty(szUser))
                {
                    List<LogbookEntry> lst = new List<LogbookEntry>(FlightStats.GetFlightStats().RecentPublicFlights);
                    if (skip > 0)
                        lst.RemoveRange(0, Math.Min(skip, lst.Count));
                    if (lst.Count > pageSize)
                        lst.RemoveRange(pageSize, lst.Count - pageSize);
                    rgle = lst;
                }
                else
                    rgle = LogbookEntryBase.GetPublicFlightsForUser(szUser, skip, pageSize);

                // Not skipping any, but still no flights found - there must not be any flights!
                // just report an error
                if (skip == 0 && !rgle.Any())
                    throw new InvalidOperationException(Resources.LogbookEntry.PublicFlightNoneFound);

                ViewBag.flights = rgle;
                return PartialView("_publicFlights");
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
        public ActionResult FlightContextMenu(LogbookEntry le)
        {
            ViewBag.le = le ?? throw new ArgumentNullException(nameof(le));
            return PartialView("_flightContextMenu");
        }

        private void CheckCanViewFlights(string targetUser, string viewingUser, ShareKey sk = null)
        {
            if (String.IsNullOrEmpty(targetUser))
                throw new ArgumentNullException(nameof(targetUser));

            // three allowed conditions:
            // a) Viewing user (Authenticated or not!) has a valid sharekey for the user and can view THAT USER's flights.  This is the only unauthenticated access allowed
            if ((sk?.CanViewFlights ?? false) && (sk?.Username ?? string.Empty).CompareOrdinal(targetUser) == 0)
                return;

            if (User.Identity.IsAuthenticated)
            {
                // Viewing user should ALWAYS be the authenticated user; null just means "use the logged user
                viewingUser = viewingUser ?? User.Identity.Name;

                if (viewingUser.CompareOrdinal(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException("Supplied viewing user is different from the authenticated user - that should never happen!");

                // b) Authenticated, viewing user is target user
                if (targetUser.CompareOrdinal(viewingUser) == 0)
                    return;

                // c) Authenticated, viewing user is an instructor of target user and user has given permission to view logbook
                if (CFIStudentMap.GetInstructorStudent(new CFIStudentMap(viewingUser).Students, targetUser)?.CanViewLogbook ?? false)
                    return;
            }

            // Otherwise, we're unauthenticated
            throw new UnauthorizedAccessException("Not authorized to view this user's logbook data");
        }

        [ChildActionOnly]
        public ActionResult LogbookTableContentsForResults(
            FlightQuery fq,
            string targetUser,
            string viewingUser,
            bool readOnly,
            FlightResult fr,
            FlightResultRange currentRange,
            ShareKey sk = null
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
            return PartialView("_logbookTableContents");
        }

        [ChildActionOnly]
        public ActionResult LogbookTableForResults(
            FlightQuery fq,
            string targetUser,
            bool readOnly = false,
            FlightResult fr = null, 
            ShareKey sk = null, 
            FlightResultRange currentRange = null)
        {
            ViewBag.query = fq ?? throw new ArgumentNullException(nameof(fq));

            ViewBag.readOnly = readOnly;
            Profile pfTarget = MyFlightbook.Profile.GetUser(targetUser ?? throw new ArgumentNullException(nameof(targetUser)));
            ViewBag.pfTarget = pfTarget;
            ViewBag.pfViewer = User.Identity.IsAuthenticated ? MyFlightbook.Profile.GetUser(User.Identity.Name) : pfTarget;
            ViewBag.currentRange = currentRange;
            ViewBag.sk = sk;
            ViewBag.flightResults = fr ?? FlightResultManager.FlightResultManagerForUser(targetUser).ResultsForQuery(fq);
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
            string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}-{3}", Branding.CurrentBrand.AppName, Resources.LocalizedText.DownloadFlyingStatsFilename, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName, DateTime.Now.YMDString().Replace(" ", "-"));
            return File(Encoding.UTF8.GetBytes(bm.RenderCSV(hm)), "text/csv", RegexUtility.UnSafeFileChars.Replace(szFilename, string.Empty) + ".csv");
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

            if (sk.CanViewAchievements)
            {
                ViewBag.badgeSets = BadgeSet.BadgeSetsFromBadges(new Achievement(sk.Username).BadgesForUser());
                ViewBag.ra = RecentAchievements.AchievementsForDateRange(sk.Username, FlightQuery.DateRanges.AllTime);
            }

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
            string szUser = String.IsNullOrEmpty(uid) ? string.Empty : new SharedDataEncryptor(MFBConstants.keyEncryptMyFlights).Decrypt(uid);

            Profile pf = MyFlightbook.Profile.GetUser(szUser);
            ViewBag.Title = String.IsNullOrEmpty(pf.UserName) ?
                Branding.ReBrand(Resources.LogbookEntry.PublicFlightPageHeaderAll) :
                String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.PublicFlightPageHeader, pf.UserFullName);
            ViewBag.uid = uid;
            return View("myFlights");
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