using MyFlightbook.Charting;
using MyFlightbook.Histogram;
using MyFlightbook.Image;
using MyFlightbook.Instruction;
using MyFlightbook.Telemetry;
using MyFlightbook.Web.Sharing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2023-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class FlightControllerBase : AdminControllerBase
    {
        #region Check if you are authorized for a given operation
        /// <summary>
        /// Determines if the viewing user has access to view flight data for the specified target user.
        /// If unauthorized for any reason, an exception is thrown
        /// 4 allowed conditions:
        ///  a) You have a valid sharekey
        ///  b) It's your flight
        ///  c) you're an admin AND "a=1" is in the request
        ///  d) You're an instructor for the student AND you can view the logbook.
        /// </summary>
        /// <param name="targetUser">User whose data is being accessed</param>
        /// <param name="viewingUser">Viewing user (should be User.identity.name, but null is a shortcut for that)</param>
        /// <param name="sk">An optional sharekey that may provide access</param>
        /// <returns>The student profile, if appropriate, or null.  Null doesn't mean unauthorized, it just means it's not a student relationship!</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        protected InstructorStudent CheckCanViewFlights(string targetUser, string viewingUser, ShareKey sk = null)
        {
            if (String.IsNullOrEmpty(targetUser))
                throw new ArgumentNullException(nameof(targetUser));

            // three allowed conditions:
            // a) Viewing user (Authenticated or not!) has a valid sharekey for the user and can view THAT USER's flights.  This is the only unauthenticated access allowed
            if ((sk?.CanViewFlights ?? false) && (sk?.Username ?? string.Empty).CompareOrdinal(targetUser) == 0)
                return null;

            if (User.Identity.IsAuthenticated)
            {
                // Viewing user should ALWAYS be the authenticated user; null just means "use the logged user
                viewingUser = viewingUser ?? User.Identity.Name;

                if (viewingUser.CompareOrdinalIgnoreCase(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException("Supplied viewing user is different from the authenticated user - that should never happen!");

                // a) Authenticated, viewing user is target user
                if (targetUser.CompareOrdinalIgnoreCase(viewingUser) == 0)
                    return null;

                // b) Admin acting as such
                Profile pfviewer = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (pfviewer.CanSupport && util.GetIntParam(Request, "a", 0) == 1)
                    return null;

                // c) Authenticated, viewing user is an instructor of target user and user has given permission to view logbook
                InstructorStudent csm = CFIStudentMap.GetInstructorStudent(new CFIStudentMap(viewingUser).Students, targetUser);
                if (csm?.CanViewLogbook ?? false)
                    return csm;
            }

            // Otherwise, we're unauthenticated
            throw new UnauthorizedAccessException(Resources.LogbookEntry.errNotAuthorizedToViewLogbook);
        }

        /// <summary>
        /// Determine if the current user can SAVE a flight for the specified target user.
        /// 3 allowed conditions:
        ///  a) It's your flight
        ///  b) you're an admin AND "a=1" is in the request
        ///  c) You are an instructor with "CanAddLogbook" privileges for a new flight, or with a pending signature request for an existing flight.
        /// </summary>
        /// <returns>The student profile, if appropriate, or null.  Null doesn't mean unauthorized, it just means it's not a student relationship!</returns>
        /// <param name="targetUser">Name of the target use</param>
        /// <param name="le">The logbook entry to edit/save</param>
        /// <exception cref="UnauthorizedAccessException"></exception>

        protected InstructorStudent CheckCanSaveFlight(string targetUser, LogbookEntry le)
        {
            if (String.IsNullOrEmpty(targetUser))
                throw new ArgumentNullException(nameof(targetUser));
            if (le == null)
                throw new ArgumentNullException(nameof(le));

            // Admin can save flights IF "a=1" is in the request
            if (User.Identity.IsAuthenticated)
            {
                if (targetUser.CompareOrdinalIgnoreCase(User.Identity.Name) == 0)
                    return null; // all good!

                Profile pfviewer = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (pfviewer.CanSupport && util.GetIntParam(Request, "a", 0) == 1)
                    return null;

                // Instructor's can only:
                // a) ADD a NEW flight (pending or regular) IF they have "can add" privileges OR
                // b) EDIT an EXISTING flight (not pending, obviously) that is not currently validly signed AND which is awaiting a signature from THIS instructor
                InstructorStudent instructorStudent = CheckCanViewFlights(targetUser, User.Identity.Name);
                if (le.IsNewFlight && (instructorStudent?.CanAddLogbook ?? false))
                    return instructorStudent;
                if (!le.IsNewFlight && le.CanEditThisFlight(User.Identity.Name, true))
                    return instructorStudent;
            }

            // Otherwise, we're unauthenticated
            throw new UnauthorizedAccessException(Resources.LogbookEntry.errNotAuthorizedToSaveToLogbook);
        }

        protected static void ValidateUser(string user, string pass, out string fixedUser)
        {
            if (String.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                throw new UnauthorizedAccessException(Resources.LogbookEntry.errNotAuthorizedToViewLogbook);

            if (user.Contains("@"))
                user = Membership.GetUserNameByEmail(user);

            if (UserEntity.ValidateUser(user, pass).Length == 0)
                throw new UnauthorizedAccessException(Resources.LogbookEntry.errNotAuthorizedToViewLogbook);

            fixedUser = user;
        }
        #endregion

        #region Context management
        /// <summary>
        /// Builds the and returns the query string for any status parameters that we may want to preserve,
        /// such as the active query, the active sort, and the page number that we came from
        /// </summary>
        /// <param name="fq"></param>
        /// <param name="fr"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        protected string GetContextParams(FlightQuery fq, FlightResult fr, FlightResultRange range)
        {
            // Add parameters to the edit link to preserve context on return
            var dictParams = HttpUtility.ParseQueryString(Request.Url.Query);

            // Issue #458: clone and reverse are getting duplicated and the & is getting url encoded, so even edits look like clones
            dictParams.Remove("Clone");
            dictParams.Remove("Reverse");
            dictParams.Remove("src");
            dictParams.Remove("chk");
            dictParams.Remove("a");

            // clear out any others that may be defaulted
            dictParams.Remove("fq");
            dictParams.Remove("se");
            dictParams.Remove("so");
            dictParams.Remove("pg");

            // and add back from the 4 above as needed
            if (fq != null && !fq.IsDefault)
                dictParams["fq"] = fq.ToBase64CompressedJSONString();
            if (fr != null)
            {
                if (fr.CurrentSortKey.CompareCurrentCultureIgnoreCase(LogbookEntry.DefaultSortKey) != 0)
                    dictParams["se"] = fr.CurrentSortKey;
                if (fr.CurrentSortDir != LogbookEntry.DefaultSortDir)
                    dictParams["so"] = fr.CurrentSortDir.ToString();
            }
            if ((range?.PageNum ?? 0) != 0)
                dictParams["pg"] = range.PageNum.ToString(CultureInfo.InvariantCulture);
            return dictParams.ToString();
        }

        /// <summary>
        /// Sets up - as appropriate, the next/previous flight for the specified flight with the specified query.
        /// </summary>
        /// <param name="q"></param>
        /// <param name="idFlight"></param>
        /// <returns></returns>
        protected string SetUpNextPrevious(FlightQuery q, int idFlight, Profile pf)
        {
            FlightResult fr = FlightResultManager.FlightResultManagerForUser(User.Identity.Name).ResultsForQuery(q);
            string sortExpr = Request["se"] ?? fr.CurrentSortKey;
            SortDirection sortDir = Enum.TryParse(Request["so"], out SortDirection sd) ? sd : fr.CurrentSortDir;

            int flightsPerPage = FlightsPerPageForUser(pf);
            string szParam = GetContextParams(q, fr, fr.GetResultRange(flightsPerPage, int.TryParse(Request["pg"], out int page) ? FlightRangeType.Page : FlightRangeType.First, sortExpr, sortDir, page));
            if (!String.IsNullOrEmpty(szParam))
                szParam = "?" + szParam;

            // Find any next/previous values
            // "idflightplus1" is the *previous* flight for a descending date search...
            int _ = fr.IndexOfFlightID(idFlight, out int idFlightPlus1, out int idFlightMinus1);

            if (idFlightMinus1 > 0)
                ViewBag.nextFlightHref = String.Format(CultureInfo.InvariantCulture, "~/mvc/FlightEdit/flight/{0}", idFlightMinus1.ToString(CultureInfo.InvariantCulture)).ToAbsolute() + szParam;
            if (idFlightPlus1 > 0)
                ViewBag.prevFlightHref = String.Format(CultureInfo.InvariantCulture, "~/mvc/FlightEdit/flight/{0}", idFlightPlus1.ToString(CultureInfo.InvariantCulture)).ToAbsolute() + szParam;

            return szParam;
        }

        protected static int FlightsPerPageForUser(Profile pf)
        {
            return pf?.GetPreferenceForKey(MFBConstants.keyPrefFlightsPerPage, MFBConstants.DefaultFlightsPerPage) ?? MFBConstants.DefaultFlightsPerPage;
        }

        /// <summary>
        /// Handy utility to get/set the last tail used by the user
        /// </summary>
        protected static int LastTailID
        {
            get { return AircraftUtility.LastTail; }
            set { AircraftUtility.LastTail = value; }
        }

        /// <summary>
        /// Issue #1462: we sometimes have a bad "last tail" that is referencing an aircraft not in the user's account (perhaps it was deleted)
        /// Getting it gets a valid "last tail" according to the following priority:
        ///  * If the current ID is valid, return that
        ///  * Otherwise, if the last tail is in the user's aircraft list, use that
        ///  * Otherwise, return the first active aircraft in the list
        ///  * Otherwise, return the first aircraft in the list
        ///  * Otherwise, return Aircraft.idAircraftUnknown
        /// </summary>
        /// <param name="currID">The currently proposed ID</param>
        /// <param name="ua">The user's aircraft list.</param>
        protected static int BestLastTail(int currID, UserAircraft ua)
        {
            if (ua == null)
                throw new ArgumentNullException(nameof(ua));
            List<Aircraft> lst = new List<Aircraft>(ua.GetAircraftForUser(UserAircraft.AircraftRestriction.UserAircraft));
            int lastTail = LastTailID;
            return ua[currID]?.AircraftID ??
                ua[lastTail]?.AircraftID ??
                lst.FirstOrDefault(ac => !ac.HideFromSelection)?.AircraftID ??
                (lst.Count > 0 ? lst[0].AircraftID : Aircraft.idAircraftUnknown);
        }
        #endregion

        #region Editing utilities
        protected const string keyLastEndingHobbs = "LastHobbs";
        protected const string keyLastEndingTach = "LastTach";
        protected const string keyLastEndingMeter = "LastMeter";
        protected const string keyLastEndingFuel = "LastFuel";
        protected const string keyLastEntryDate = "LastEntryDate";
        protected const string keySessionInProgress = "InProgressFlight";

        /// <summary>
        /// Initializes a logbookentry from the form, checking that the viewer has SAVE permissions on the flight.
        /// All other errors/exceptions (besides authorization) are in the errorstring!
        /// </summary>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException">If the viewing user is not authorized to EDIT the flight, they are unauthorized</exception>
        protected LogbookEntry LogbookEntryFromForm()
        {
            string pendingID = Request["idPending"];
            LogbookEntry le = String.IsNullOrEmpty(pendingID) ? new LogbookEntry() : new PendingFlight(pendingID);
            le.FlightID = util.GetIntParam(Request, "idFlight", LogbookEntryCore.idFlightNew);
            le.User = Request["szTargetUser"] ?? User.Identity.Name;
            le.ErrorString = string.Empty;  // clear this out.

            // Check that you can save - and if it's an instructor/student, further check that it's a new flight.
            CheckCanSaveFlight(le.User, le);

            try
            {
                // Core fields
                le.Date = DateTime.Parse(Request["flightDate"], CultureInfo.CurrentCulture).Date;

                le.AircraftID = util.GetIntParam(Request, "flightAircraft", 0);
                le.CatClassOverride = util.GetIntParam(Request, "flightCatClassOverride", 0);

                le.Route = Request["flightRoute"];
                le.Comment = Request["flightComments"];

                le.Approaches = util.GetIntParam(Request, "flightApproaches", 0);
                le.fHoldingProcedures = Request["flightHold"] != null;
                le.Landings = util.GetIntParam(Request, "flightLandings", 0);
                le.FullStopLandings = util.GetIntParam(Request, "flightFSDayLandings", 0);
                le.NightLandings = util.GetIntParam(Request, "flightFSNightLandings", 0);

                le.CrossCountry = (Request["flightXC"] ?? string.Empty).SafeParseDecimal();
                le.Nighttime = (Request["flightNight"] ?? string.Empty).SafeParseDecimal();
                le.SimulatedIFR = (Request["flightSimIMC"] ?? string.Empty).SafeParseDecimal();
                le.IMC = (Request["flightIMC"] ?? string.Empty).SafeParseDecimal();
                le.GroundSim = (Request["flightGroundSim"] ?? string.Empty).SafeParseDecimal();
                le.Dual = (Request["flightDual"] ?? string.Empty).SafeParseDecimal();
                le.CFI = (Request["flightCFI"] ?? string.Empty).SafeParseDecimal();
                le.SIC = (Request["flightSIC"] ?? string.Empty).SafeParseDecimal();
                le.PIC = (Request["flightPIC"] ?? string.Empty).SafeParseDecimal();
                le.TotalFlightTime = (Request["flightTotal"] ?? string.Empty).SafeParseDecimal();

                le.HobbsStart = Request["flightHobbsStart"].SafeParseDecimal();
                le.HobbsEnd = Request["flightHobbsEnd"].SafeParseDecimal();

                // Datetimes have been entered in the user's preferred timezone
                TimeZoneInfo tz = MyFlightbook.Profile.GetUser(User.Identity.Name).PreferredTimeZone;

                le.EngineStart = Request["flightEngineStart"].ParseUTCDateTime(le.Date, tz);
                le.EngineEnd = Request["flightEngineEnd"].ParseUTCDateTime(le.Date, tz);
                le.FlightStart = Request["flightFlightStart"].ParseUTCDateTime(le.Date, tz);
                le.FlightEnd = Request["flightFlightEnd"].ParseUTCDateTime(le.Date, tz);

                le.CustomProperties = new CustomPropertyCollection(CustomFlightProperty.PropertiesFromJSONTuples(Request["flightPropTuples"], le.FlightID, le.Date, CultureInfo.CurrentCulture), true);

                // Each of the custom properties that is a date-time has been expressed in user's preferred timezone; need to convert to UTC
                foreach (CustomFlightProperty cfp in le.CustomProperties)
                {
                    if (cfp.PropertyType.Type == CFPPropertyType.cfpDateTime && cfp.DateValue.HasValue())
                        cfp.DateValue = DateTime.SpecifyKind(cfp.DateValue, DateTimeKind.Local).ConvertFromTimezone(tz);
                }

                // If this is from a pending flight, its saved telemetry might be in the flightPendingTelemetry hidden field.
                string cachedFlightData = (Request["flightPendingTelemetry"] ?? string.Empty);
                if (!String.IsNullOrEmpty(cachedFlightData))
                    le.FlightData = Convert.FromBase64String(cachedFlightData).Uncompress();

                IEnumerable<VideoRef> videoRefs = VideoRef.FromJSON(Request["flightVideosJSON"]);
                le.Videos.Clear();
                foreach (VideoRef vr in videoRefs)
                    le.Videos.Add(vr);

                le.PopulateImages();

                le.fIsPublic = Request["flightPublic"] != null;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                le.ErrorString = ex.Message;
            }
            return le;
        }

        protected bool CommitFlight(LogbookEntry le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));

            // ensure that the aircraft is in their profile
            UserAircraft ua = new UserAircraft(le.User);
            if (ua[le.AircraftID] == null)
            {
                Aircraft ac = new Aircraft(le.AircraftID);
                if (!ac.IsNew)
                    ua.FAddAircraftForUser(ac);
            }

            if (le.IsValid())
            {
                // if a new flight and hobbs > 0, save it for the next flight
                bool fIsNew = le.IsNewFlight;
                if (fIsNew)
                {
                    Session[keyLastEndingHobbs] = le.HobbsEnd;
                    Session[keyLastEndingTach] = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropTachEnd);
                    Session[keyLastEndingMeter] = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropFlightMeterEnd);
                    Session[keyLastEndingFuel] = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropFuelAtLanding);
                    Session[keyLastEntryDate] = le.Date; // new flight - save the date
                }

                try
                {
                    if (le.FCommit(le.HasFlightData))
                    {
                        if (le.User.CompareCurrentCultureIgnoreCase(User.Identity.Name) == 0)
                            LastTailID = le.AircraftID;

                        if (fIsNew)
                        {
                            // this should now have a flight ID - save this so that we can scroll to it.
                            Session[MFBConstants.keySessLastNewFlight] = le.FlightID;

                            // process pending images, if this was a new flight
                            foreach (MFBPendingImage pendingImage in MFBPendingImage.PendingImagesInSession(Session))
                            {
                                pendingImage.Commit(MFBImageInfoBase.ImageClass.Flight, le.FlightID.ToString(CultureInfo.InvariantCulture));
                                pendingImage.DeleteImage();     // clean it up!
                            }
                        }

                        // Badge computation may be wrong
                        MyFlightbook.Profile.GetUser(le.User).SetAchievementStatus(Achievements.Achievement.ComputeStatus.NeedsComputing);
                    }
                }
                catch (Exception ex) when (!(ex is OutOfMemoryException))
                {
                    le.ErrorString = !String.IsNullOrEmpty(le.ErrorString) ? le.ErrorString : ex?.InnerException.Message ?? ex.Message;
                }
            }

            return String.IsNullOrEmpty(le.ErrorString);
        }

        protected void AddApproachesFromRequest(LogbookEntry le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            int appchCount = util.GetIntParam(Request, "appchHelpCount", 0);

            if (Request["appchHelpAdd"] != null)
                le.Approaches += appchCount;

            CustomFlightProperty cfp = le.CustomProperties.GetEventWithTypeIDOrNew(CustomPropertyType.KnownProperties.IDPropApproachName);
            cfp.TextValue = (cfp.TextValue + Resources.LocalizedText.LocalizedSpace + new ApproachDescription(appchCount, Request["appchHelpType"] + Request["appchHelpTypeSfx"], Request["appchHelpRwy"] + Request["appchHelpRwySfx"], Request["appchHelpApt"]).ToCanonicalString()).Trim();
            le.CustomProperties.Add(cfp);
        }
        #endregion

        #region Display utilities
        protected void SetUpTelemetryChartForFlight(LogbookEntry le, string xData, string yData, string y2Data, double y1Scale, double y2Scale)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
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
            }
        }

        protected void SetUpHistogramView(HistogramManager hm, string selectedBucket, string fieldToGraph, bool fUseHHMM, bool fIncludeAverage, bool fLinkItems)
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
            ViewBag.bm = bm;
            ViewBag.hm = hm;
            ViewBag.hv = hm.Values.FirstOrDefault(h => h.DataField.CompareOrdinal(fieldToGraph) == 0);
            ViewBag.rawData = bm.RenderHTML(hm, fLinkItems);
        }

        protected static byte[] RenderFlyingStats(FlightQuery fq, string selectedBucket)
        {
            if (fq == null)
                throw new ArgumentNullException(nameof(fq));
            if (String.IsNullOrEmpty(selectedBucket))
                throw new ArgumentNullException(nameof(selectedBucket));
            HistogramManager hm = LogbookEntryDisplay.GetHistogramManager(FlightResultManager.FlightResultManagerForUser(fq.UserName).ResultsForQuery(fq).Flights, fq.UserName);
            BucketManager bm = hm.SupportedBucketManagers.FirstOrDefault(b => b.DisplayName.CompareOrdinal(selectedBucket) == 0);
            bm.ScanData(hm);
            return bm.RenderCSV(hm).UTF8Bytes();
        }
        #endregion
    }
}