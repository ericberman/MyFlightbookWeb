using MyFlightbook.Airports;
using MyFlightbook.Currency;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2018-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.RatingsProgress
{
    public class RecentAchievementMilestone : MilestoneItem
    {
        public FlightQuery Query { get; set; }

        public string QueryLinkTemplate { get; set; }

        public RecentAchievementMilestone(string szTitle, MilestoneType type, int threshold) : base(szTitle, string.Empty, string.Empty, type, threshold)
        {
            QueryLinkTemplate = "~/Member/LogbookNew.aspx?fq={0}";
        }

        public string TargetLink
        {
            get
            {
                if (MatchingEventID > 0)    // links to a specific flight
                    return VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, "~/Member/FlightDetail.aspx/{0}", MatchingEventID));
                else if (Query != null)
                    return VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, QueryLinkTemplate, HttpUtility.UrlEncode(Query.ToBase64CompressedJSONString())));

                return string.Empty;
            }
        }
    }

    public class FastestTimeToTotal
    {
        #region Properties
        /// <summary>
        /// If present, the start of the fastest period to a total
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// If present, the end of the fastest period to a total
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// The threshold we are seeking
        /// </summary>
        public decimal Threshold { get; private set; }

        /// <summary>
        /// Returns the shortest timespan that we've seen.
        /// </summary>
        public TimeSpan ShortestSpan
        {
            get { return (StartDate.HasValue && EndDate.HasValue) ? EndDate.Value.Subtract(StartDate.Value) : TimeSpan.MaxValue; }
        }

        private decimal currentTotal { get; set; } = 0;

        private readonly Queue<ExaminerFlightRow> queue = new Queue<ExaminerFlightRow>();

        public bool IsMet { get { return ShortestSpan.CompareTo(TimeSpan.MaxValue) < 0; } }
        #endregion

        public override string ToString()
        {
            return IsMet ? String.Format(CultureInfo.CurrentCulture, Resources.Achievements.FastestHrsBase, Threshold) :
                String.Format(CultureInfo.CurrentCulture, Resources.Achievements.FastestHrsNotAchieved, Threshold);
        }

        public RecentAchievementMilestone ToMilestone(string Username)
        {
            RecentAchievementMilestone ra = new RecentAchievementMilestone(ToString(), MilestoneItem.MilestoneType.AchieveOnce, (int)Threshold);

            if (IsMet)
            {
                if (StartDate != null && EndDate != null)
                {
                    ra.Query = new FlightQuery(Username) { DateRange = FlightQuery.DateRanges.Custom, DateMin = StartDate.Value, DateMax = EndDate.Value };
                    ra.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.FastestHrs, Threshold, ShortestSpan.TotalDays, StartDate.Value, EndDate.Value);
                }
                ra.AddEvent(1);
            }
            return ra;
        }

        public FastestTimeToTotal(int threshold)
        {
            Threshold = threshold;
            StartDate = EndDate = null;
        }

        /// <summary>
        /// Assumes we're going in 
        /// </summary>
        /// <param name="le"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            queue.Enqueue(cfr); // queue it up
            currentTotal += cfr.Total;

            // We've crossed the threshold - update the start/end dates, if needed, and then dequeue until we are back under the threshold
            if (currentTotal > Threshold)
            {
                do
                {
                    // cfr has the oldest date
                    DateTime dtStart = cfr.dtFlight;
                    ExaminerFlightRow efr = queue.Dequeue();
                    currentTotal -= efr.Total;  // take it out of the current total.
                    DateTime dtEnd = efr.dtFlight;
                    if (dtEnd.Subtract(dtStart).CompareTo(ShortestSpan) < 0)
                    {
                        StartDate = dtStart;
                        EndDate = dtEnd;
                    }
                } while (currentTotal > Threshold);
            }
        }
    }

    /// <summary>
    /// Pseudo-milestone progress class for "cool achievements you've done in the past x period of time."
    /// </summary>
    public class RecentAchievements : MilestoneProgress
    {
        #region Properties
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        // flying streak
        protected DateTime? LastFlyingDayOfStreak { get; set; }
        protected DateTime? FirstFlyingDayOfStreak { get; set; }
        protected DateTime? LastDayOfCurrentStreak { get; set; }
        protected DateTime? FirstDayOfCurrentStreak { get; set; }
        protected int FlyingDayStreak { get { return LastFlyingDayOfStreak.HasValue && FirstFlyingDayOfStreak.HasValue ? LastFlyingDayOfStreak.Value.Subtract(FirstFlyingDayOfStreak.Value).Days + 1 : 0; } }
        protected int CurrentFlyingDayStreak { get { return LastDayOfCurrentStreak.HasValue && FirstDayOfCurrentStreak.HasValue ? LastDayOfCurrentStreak.Value.Subtract(FirstDayOfCurrentStreak.Value).Days + 1 : 0; } }

        /// <summary>
        /// If true, compute date range dynamically based on flights seen; if false, only look at flights in specified date range.
        /// </summary>
        public bool AutoDateRange { get; set; }

        // # of distinct dates with flights, flights per day
        protected Dictionary<string, int> FlightDates { get; private set; } = new Dictionary<string, int>();
        protected Dictionary<string, int> FlightLandings { get; private set; } = new Dictionary<string, int>();

        /// <summary>
        /// Airports visited on a flight.
        /// </summary>
        protected Dictionary<string, string> AirportsByDate { get; private set; } = new Dictionary<string, string>();
        protected int MaxFlightsPerDay { get; set; }
        protected int MaxLandingsPerDay { get; set; }

        /// <summary>
        /// Total Number of flights
        /// </summary>
        protected int NumFlights { get; set; }
        
        // Longest flight
        protected decimal LongestFlightLength { get; set; }

        // Furthest flight
        protected double FurthestFlightDistance { get; set; }
        
        // Distinct aircraft/models
        protected HashSet<int> DistinctAircraft { get; private set; } = new HashSet<int>();
        protected HashSet<int> DistinctModels { get; private set; } = new HashSet<int>();
        protected HashSet<string> DistinctICAO { get; private set; } = new HashSet<string>();

        // Airports
        protected HashSet<string> Airports { get; private set; } = new HashSet<string>();

        protected Dictionary<string, HashSet<string>> GeoRegions { get; private set; } = new Dictionary<string, HashSet<string>>();

        protected int MostAirportsFlightCount { get; set; }

        protected FastestTimeToTotal fs100 { get; set; } = new FastestTimeToTotal(100);
        protected FastestTimeToTotal fs1000 { get; set; } = new FastestTimeToTotal(1000);

        #region MilestoneItems
        /// <summary>
        /// Longest streak of consecutive flying dates
        /// </summary>
        protected RecentAchievementMilestone miLongestStreak { get; set; }

        /// <summary>
        /// Longest streak of consecutive no-fly days
        /// </summary>
        protected RecentAchievementMilestone miLongestNoFlyStreak { get; set; }

        /// <summary>
        /// Number of distinct days in period in which flights occured
        /// </summary>
        protected RecentAchievementMilestone miFlyingDates { get; set; }

        protected RecentAchievementMilestone miFlightCount { get; set; }

        /// <summary>
        /// Most flights in a day
        /// </summary>
        protected RecentAchievementMilestone miMostFlightsInDay { get; set; }

        /// <summary>
        /// Most landings in a day
        /// </summary>
        protected RecentAchievementMilestone miMostLandingsInDay { get; set; }

        /// <summary>
        /// Most airports in a single day
        /// </summary>
        protected RecentAchievementMilestone miMostAirportsDay { get; set; }

        /// <summary>
        /// Most countries in a single day
        /// </summary>
        protected RecentAchievementMilestone miMostCountriesDay { get; set; }

        /// <summary>
        /// Most states/provinces/etc in a single day
        /// </summary>
        protected RecentAchievementMilestone miMostAdmin1sDay { get; set; }

        /// <summary>
        /// Longest Flight (total time)
        /// </summary>
        protected RecentAchievementMilestone miLongestFlight { get; set; }

        /// <summary>
        /// Furthest flight (distance, based on airport route)
        /// </summary>
        protected RecentAchievementMilestone miFurthestFlight { get; set; }

        /// <summary>
        /// Distinct aircraft flown
        /// </summary>
        protected RecentAchievementMilestone miAircraft { get; set; }

        /// <summary>
        /// Distinct models flown
        /// </summary>
        protected RecentAchievementMilestone miModels { get; set; }

        /// <summary>
        /// Number of airports visited
        /// </summary>
        protected RecentAchievementMilestone miAirports { get; set; }

        /// <summary>
        /// Most airports in a single flight
        /// </summary>
        protected RecentAchievementMilestone miMostAirportsFlight { get; set; }

        /// <summary>
        /// Countries visited
        /// </summary>
        protected RecentAchievementMilestone miCountries { get; set; }

        /// <summary>
        /// Countries visited
        /// </summary>
        protected RecentAchievementMilestone miAdmin1 { get; set; }
        #endregion
        #endregion

        #region Flight Counts
        public int FlightCountOnDate(DateTime dt)
        {
            return FlightDates.TryGetValue(dt.YMDString(), out int cFlights) ? cFlights : 0;
        }

        public int FlownDaysInPeriod { get { return FlightDates.Count; } }
        #endregion

        /// <summary>
        /// Creates a new recentachievements
        /// </summary>
        /// <param name="dtStart">Start Date of period</param>
        /// <param name="dtEnd">End date of period</param>
        public RecentAchievements(DateTime dtStart, DateTime dtEnd) : base()
        {
            StartDate = dtStart.Date;
            EndDate = dtEnd.Date;

            miFlightCount = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsFlightsLogged, MilestoneItem.MilestoneType.Count, 1);
            miLongestStreak = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementFlyingStreakTitle, MilestoneItem.MilestoneType.Count, 1);
            miLongestNoFlyStreak = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsNoFlyingStreakTitle, MilestoneItem.MilestoneType.Count, 1);
            miFlyingDates = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsFlyingDayCountTitle, MilestoneItem.MilestoneType.Count, 1);
            miMostFlightsInDay = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementMostFlightsInDayTitle, MilestoneItem.MilestoneType.Count, 2);
            miMostLandingsInDay = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementMostLandingsInDayTitle, MilestoneItem.MilestoneType.Count, 2);
            miLongestFlight = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsLongestFlightTitle, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miFurthestFlight = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsFurthestFlightTitle, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miAircraft = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsDistinctAircraftTitle, MilestoneItem.MilestoneType.Count, 1);
            miModels = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsDistinctModelsTitle, MilestoneItem.MilestoneType.Count, 1);
            miAirports = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsAirportsVisitedTitle, MilestoneItem.MilestoneType.Count, 1) { QueryLinkTemplate = "~/Member/Airports.aspx?fq={0}" };
            miMostAirportsFlight = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsAirportsOnFlightTitle, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miCountries = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsCountriesVisited, MilestoneItem.MilestoneType.Count, 2);
            miAdmin1 = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsAdmin1Visited, MilestoneItem.MilestoneType.Count, 2);
            miMostAirportsDay = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementMostAirportsInDayTitle, MilestoneItem.MilestoneType.Count, 2);
            miMostCountriesDay = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementMostCountriesDayTitle, MilestoneItem.MilestoneType.Count, 2);
            miMostAdmin1sDay = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementMostAdmin1sDayTitle, MilestoneItem.MilestoneType.Count, 2);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                // Finalize any milestones
                miLongestStreak.Progress = FlyingDayStreak;
                if (FirstFlyingDayOfStreak.HasValue && LastFlyingDayOfStreak.HasValue) {
                    miLongestStreak.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementFlyingStreak, (int)miLongestStreak.Progress, FirstFlyingDayOfStreak.Value, LastFlyingDayOfStreak.Value);
                    miLongestStreak.Query = new FlightQuery(Username) { DateRange = FlightQuery.DateRanges.Custom, DateMin = FirstFlyingDayOfStreak.Value, DateMax = LastFlyingDayOfStreak.Value };
                }

                // No fly streak is trickier - you need to measure the flights you didn't see!
                DateTime dtNoFlyStart = StartDate;
                List<string> lstFlightDays = new List<string>(FlightDates.Keys) { EndDate.AddDays(1).YMDString() };
                lstFlightDays.Sort();
                foreach (string szKey in lstFlightDays)
                {
                    DateTime dt = Convert.ToDateTime(szKey, CultureInfo.InvariantCulture).AddDays(-1);
                    int cDaysNoFly = dt.Subtract(dtNoFlyStart).Days + 1;
                    if (cDaysNoFly > miLongestNoFlyStreak.Progress)  // biggest missing stretch so far...
                    {
                        miLongestNoFlyStreak.Progress = cDaysNoFly;
                        miLongestNoFlyStreak.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsNoFlyingStreak, cDaysNoFly, dtNoFlyStart, dt);
                        miLongestNoFlyStreak.Query = new FlightQuery(Username) { DateRange = FlightQuery.DateRanges.Custom, DateMin = dtNoFlyStart, DateMax = dt };
                    }
                    dtNoFlyStart = Convert.ToDateTime(szKey, CultureInfo.InvariantCulture).AddDays(1);
                }

                miFlightCount.MatchingEventText = ((int) miFlightCount.Progress).ToString("#,##0", CultureInfo.CurrentCulture);

                miFlyingDates.Progress = FlightDates.Count;
                int DaysInPeriod = EndDate.Subtract(StartDate).Days + 1;
                miFlyingDates.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsFlyingDayCount, FlightDates.Count, DaysInPeriod, (FlightDates.Count * 100.0) / DaysInPeriod);
                miFlyingDates.Query = new FlightQuery(Username) { DateRange = FlightQuery.DateRanges.Custom, DateMin = StartDate, DateMax = EndDate };

                miAircraft.Progress = DistinctAircraft.Count;
                miModels.Progress = DistinctModels.Count;
                miAircraft.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsDistinctAircraft, DistinctAircraft.Count);
                miModels.MatchingEventText = (DistinctModels.Count == DistinctICAO.Count) ? String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsDistinctModels, DistinctModels.Count) :
                String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsDistinctModelsAndFamilies, DistinctModels.Count, DistinctICAO.Count);

                miAirports.Progress = Airports.Count;
                miAirports.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsAirportsVisited, Airports.Count);

                // If multiple countries, then we'll say how many countries visited.
                // Also show most states/provinces/etc. for whichever country has that distinction
                miCountries.Progress = GeoRegions.Count;
                miCountries.MatchingEventText = GeoRegions.Count.ToString("#,##0", CultureInfo.CurrentCulture);
                foreach (string country in GeoRegions.Keys)
                {
                    int cAdmin1 = GeoRegions[country].Count;
                    if (cAdmin1 > miAdmin1.Progress)
                    {
                        miAdmin1.Progress = cAdmin1;
                        miAdmin1.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsAdmin1VisitedDisplay, cAdmin1, country);
                    }
                }

                miAirports.Query = miFlyingDates.Query; // both use the entire time period.

                List<MilestoneItem> l = new List<MilestoneItem>()
                {
                    miFlightCount,
                    miFlyingDates,
                    miLongestStreak,
                    miLongestNoFlyStreak,
                    miMostFlightsInDay,
                    miMostLandingsInDay,
                    miLongestFlight,
                    miFurthestFlight,
                    miAirports,
                    miMostAirportsFlight,
                    miMostAirportsDay,
                    miCountries,
                    miAdmin1,
                    miMostCountriesDay,
                    miMostAdmin1sDay,
                    miAircraft,
                    miModels,
                    fs100.ToMilestone(Username),
                    fs1000.ToMilestone(Username)
                };

                l.RemoveAll(mi => !mi.IsSatisfied);

                return new Collection<MilestoneItem>(l);
            }
        }

        private static int MatchAirports(IEnumerable<airport> rgap, HashSet<string> airports, Dictionary<string, HashSet<string>> geoRegions)
        {
            int cUniqueAirports = 0;
            HashSet<string> hsThisFlight = new HashSet<string>();
            foreach (airport ap in rgap)
            {
                // Dedupe as we go based on latitude/longitude, ignoring non-ports.  
                // We don't actually need the facility name here - so we can just round off the latitude/longitude and distinguish by type code.
                // Note: this can differ slightly from Visited Airports counts because for achievements, we're ignoring flights in training devices; visited airports doesn't ignore them.
                if (ap.IsPort)
                {
                    string szHash = ap.GeoHashKey;
                    if (!airports.Contains(szHash))
                    {
                        airports.Add(ap.GeoHashKey);
                        cUniqueAirports++;
                    }
                    hsThisFlight.Add(szHash);

                    string szCountry = ap.CountryDisplay;
                    string szAdmin1 = ap.Admin1Display;

                    // Keep track of visited countries/regions
                    if (!String.IsNullOrWhiteSpace(szCountry))
                    {
                        if (!geoRegions.ContainsKey(szCountry))
                            geoRegions[szCountry] = new HashSet<string>();

                        if (!String.IsNullOrWhiteSpace(szAdmin1) && !geoRegions[szCountry].Contains(szAdmin1))
                            geoRegions[szCountry].Add(szAdmin1);
                    }
                }
            }

            return hsThisFlight.Count;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            DateTime dtFlight = cfr.dtFlight.Date;

            if (AutoDateRange)
            {
                StartDate = StartDate.EarlierDate(dtFlight);
                EndDate = EndDate.LaterDate(dtFlight);
            }

            // ignore anything not in a real aircraft or outside of our date range
            if (!cfr.fIsRealAircraft || dtFlight.CompareTo(StartDate) < 0 || dtFlight.CompareTo(EndDate) > 0)
                return;

            miFlightCount.AddEvent(1);

            string szDateKey = dtFlight.YMDString();

            // Initialize the current streak, if needed
            if (FirstDayOfCurrentStreak == null || LastDayOfCurrentStreak == null)
                FirstDayOfCurrentStreak = LastDayOfCurrentStreak = dtFlight;

            // Extend the current streak if this flight is either on the first date or on a day before the first date; if it isn't one of those, then end the current streak
            FirstDayOfCurrentStreak = dtFlight.CompareTo(FirstDayOfCurrentStreak.Value.Date) == 0 || dtFlight.CompareTo(FirstDayOfCurrentStreak.Value.AddDays(-1).Date) == 0
                ? (DateTime?)dtFlight
                : (LastDayOfCurrentStreak = dtFlight);

            int cDaysCurrentStreak = CurrentFlyingDayStreak;

            if (cDaysCurrentStreak > 1 && cDaysCurrentStreak > FlyingDayStreak)
            {
                FirstFlyingDayOfStreak = FirstDayOfCurrentStreak;
                LastFlyingDayOfStreak = LastDayOfCurrentStreak;
            }
                  
            // Distinct flights on dates
            FlightDates[szDateKey] = FlightDates.ContainsKey(szDateKey) ? FlightDates[szDateKey] + 1 : 1;

            if (FlightDates[szDateKey] > MaxFlightsPerDay)
            {
                int cFlights = FlightDates[szDateKey];
                MaxFlightsPerDay = cFlights;
                miMostFlightsInDay.Progress = cFlights;
                miMostFlightsInDay.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementMostFlightsInDay, cFlights, cfr.dtFlight);
                miMostFlightsInDay.Query = new FlightQuery(Username) { DateRange = FlightQuery.DateRanges.Custom, DateMin = cfr.dtFlight, DateMax = cfr.dtFlight };
            }

            FlightLandings[szDateKey] = FlightLandings.ContainsKey(szDateKey) ? FlightLandings[szDateKey] + cfr.cLandingsThisFlight : cfr.cLandingsThisFlight;

            if (FlightLandings[szDateKey] > MaxLandingsPerDay)
            {
                int cLandings = FlightLandings[szDateKey];
                MaxLandingsPerDay = cLandings;
                miMostLandingsInDay.Progress = cLandings;
                miMostLandingsInDay.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementMostLandingsInDay, cLandings, cfr.dtFlight);
                miMostLandingsInDay.Query = new FlightQuery(Username) { DateRange = FlightQuery.DateRanges.Custom, DateMin = cfr.dtFlight, DateMax = cfr.dtFlight };
            }

            // Longest flight
            if (cfr.Total > LongestFlightLength)
            {
                LongestFlightLength = cfr.Total;
                miLongestFlight.Progress = 1;
                miLongestFlight.MatchingEventID = cfr.flightID;
                miLongestFlight.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsLongestFlight, cfr.Total, dtFlight);
            }

            // Distinct aircraft/models
            DistinctAircraft.Add(cfr.idAircraft);
            DistinctModels.Add(cfr.idModel);
            DistinctICAO.Add(cfr.szFamily);

            // Furthest Flight & airport computations on a flight-by-flight basis (rather than date-of-flight)
            AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);

            double distance = al.DistanceForRoute();
            if (distance > FurthestFlightDistance)
            {
                FurthestFlightDistance = distance;
                miFurthestFlight.Progress = 1;
                miFurthestFlight.MatchingEventID = cfr.flightID;
                miFurthestFlight.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsFurthestFlight, distance, dtFlight);
            }

            int cAirportsThisFlight = MatchAirports(al.UniqueAirports, Airports, GeoRegions); // Using the *shared* Airports/GeoRegions, since we want to build that up
            if (cAirportsThisFlight > MostAirportsFlightCount)
            {
                MostAirportsFlightCount = cAirportsThisFlight;
                miMostAirportsFlight.MatchingEventID = cfr.flightID;
                miMostAirportsFlight.Progress = 1;
                miMostAirportsFlight.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsAirportsOnFlight, cAirportsThisFlight, dtFlight.ToShortDateString());
                miMostAirportsFlight.Query = new FlightQuery(Username) { DateRange = FlightQuery.DateRanges.Custom, DateMax = dtFlight, DateMin = dtFlight };
            }

            // Append the set of airports for this day and see if there are any single-date (vs. single-flight) achievements)
            AirportsByDate[szDateKey] = AirportsByDate.TryGetValue(szDateKey, out var routeSoFar)
                ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, routeSoFar, cfr.Route)
                : cfr.Route;

            // See if the day has broken any records
            al = AirportListOfRoutes.CloneSubset(AirportsByDate[szDateKey], true);
            HashSet<string> airports = new HashSet<string>();
            Dictionary<string, HashSet<string>> geoRegions = new Dictionary<string, HashSet<string>>();
            cAirportsThisFlight = MatchAirports(al.UniqueAirports, airports, geoRegions);    // Using a *local* airports/GeoRegions.
            if (cAirportsThisFlight > miMostAirportsDay.Progress)
            {
                miMostAirportsDay.Progress = cAirportsThisFlight;
                miMostAirportsDay.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementMostAirportsInDay, cAirportsThisFlight, dtFlight);
                miMostAirportsDay.Query = new FlightQuery(Username) { DateRange = FlightQuery.DateRanges.Custom, DateMax = dtFlight, DateMin = dtFlight };
            }
            if (geoRegions.Count > miMostCountriesDay.Progress)
            {
                miMostCountriesDay.Progress = geoRegions.Count;
                miMostCountriesDay.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementMostCountriesDay, geoRegions.Count, dtFlight);
                miMostCountriesDay.Query = new FlightQuery(Username) { DateRange = FlightQuery.DateRanges.Custom, DateMax = dtFlight, DateMin = dtFlight };
            }
            // Most states/countries in a day needs to be within a single country
            foreach (HashSet<string> hsAdmin1 in geoRegions.Values)
            {
                if (hsAdmin1.Count > miMostAdmin1sDay.Progress)
                {
                    miMostAdmin1sDay.Progress = hsAdmin1.Count;
                    miMostAdmin1sDay.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementMostAdmin1sDay, hsAdmin1.Count, dtFlight);
                    miMostAdmin1sDay.Query = new FlightQuery(Username) { DateRange = FlightQuery.DateRanges.Custom, DateMax = dtFlight, DateMin = dtFlight };
                }
            }

            fs100.ExamineFlight(cfr);
            fs1000.ExamineFlight(cfr);
        }
    }
}