using MyFlightbook.Airports;
using MyFlightbook.FlightCurrency;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2018-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MilestoneProgress
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
                    return VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, "~/Public/ViewPublicFlight.aspx/{0}", MatchingEventID));
                else if (Query != null)
                    return VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, QueryLinkTemplate, HttpUtility.UrlEncode(Query.ToBase64CompressedJSONString())));

                return string.Empty;
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
        protected Dictionary<string, int> FlightDates { get; set; }
        protected int MaxFlightsPerDay { get; set; }

        /// <summary>
        /// Total Number of flights
        /// </summary>
        protected int NumFlights { get; set; }
        
        // Longest flight
        protected decimal LongestFlightLength { get; set; }

        // Furthest flight
        protected double FurthestFlightDistance { get; set; }
        
        // Distinct aircraft/models
        protected HashSet<int> DistinctAircraft { get; set; }
        protected HashSet<int> DistinctModels { get; set; }
        protected HashSet<string> DistinctICAO { get; set; }

        // Airports
        protected HashSet<string> Airports { get; set; }
        protected int MostAirportsFlightCount { get; set; }

        #region MilestoneItems
        /// <summary>
        /// Longest streak of consecutive flying dates
        /// </summary>
        protected RecentAchievementMilestone miLongestStreak { get; set; }

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
        #endregion
        #endregion

        #region Flight Counts
        public int FlightCountOnDate(DateTime dt)
        {
            int cFlights;
            return FlightDates.TryGetValue(dt.YMDString(), out cFlights) ? cFlights : 0;
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

            FlightDates = new Dictionary<string, int>();
            DistinctAircraft = new HashSet<int>();
            DistinctModels = new HashSet<int>();
            DistinctICAO = new HashSet<string>();
            Airports = new HashSet<string>();

            miFlightCount = new RecentAchievementMilestone(string.Empty, MilestoneItem.MilestoneType.Count, 1);
            miLongestStreak = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementFlyingStreakTitle, MilestoneItem.MilestoneType.Count, 1);
            miFlyingDates = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsFlyingDayCountTitle, MilestoneItem.MilestoneType.Count, 1);
            miMostFlightsInDay = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementMostFlightsInDayTitle, MilestoneItem.MilestoneType.Count, 2);
            miLongestFlight = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsLongestFlightTitle, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miFurthestFlight = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsFurthestFlightTitle, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miAircraft = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsDistinctAircraftTitle, MilestoneItem.MilestoneType.Count, 1);
            miModels = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsDistinctModelsTitle, MilestoneItem.MilestoneType.Count, 1);
            miAirports = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsAirportsVisitedTitle, MilestoneItem.MilestoneType.Count, 1) { QueryLinkTemplate = "~/Member/Airports.aspx?fq={0}" };
            miMostAirportsFlight = new RecentAchievementMilestone(Resources.Achievements.RecentAchievementsAirportsOnFlightTitle, MilestoneItem.MilestoneType.AchieveOnce, 1);
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
                miFlightCount.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.nameNumberFlights, miFlightCount.Progress);

                miFlyingDates.Progress = FlightDates.Count;
                int DaysInPeriod = EndDate.Subtract(StartDate).Days + 1;
                miFlyingDates.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsFlyingDayCount, FlightDates.Count, DaysInPeriod, (FlightDates.Count * 100.0) / DaysInPeriod);
                miFlyingDates.Query = new FlightQuery(Username) { DateRange = FlightQuery.DateRanges.Custom, DateMin = StartDate, DateMax = EndDate };

                miAircraft.Progress = DistinctAircraft.Count;
                miModels.Progress = DistinctModels.Count;
                miAircraft.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsDistinctAircraft, DistinctAircraft.Count);
                miModels.MatchingEventText = (DistinctModels.Count == DistinctICAO.Count) ? String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsDistinctModels, DistinctModels.Count) :
                String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsDistinctModelsAndFamilies, DistinctModels.Count, DistinctICAO.Count);

                miAirports.Progress = Airports.Count();
                miAirports.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsAirportsVisited, Airports.Count());
                miAirports.Query = miFlyingDates.Query; // both use the entire time period.

                List<MilestoneItem> l = new List<MilestoneItem>()
                {
                    miFlightCount,
                    miFlyingDates,
                    miLongestStreak,
                    miMostFlightsInDay,
                    miLongestFlight,
                    miFurthestFlight,
                    miAirports,
                    miMostAirportsFlight,
                    miAircraft,
                    miModels
                };

                l.RemoveAll(mi => !mi.IsSatisfied);

                return new Collection<MilestoneItem>(l);
            }
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
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
            if (dtFlight.CompareTo(FirstDayOfCurrentStreak.Value.Date) == 0 || dtFlight.CompareTo(FirstDayOfCurrentStreak.Value.AddDays(-1).Date) == 0)
                FirstDayOfCurrentStreak = dtFlight;
            else
                FirstDayOfCurrentStreak = LastDayOfCurrentStreak = dtFlight;

            int cDaysCurrentStreak = CurrentFlyingDayStreak;

            if (cDaysCurrentStreak > 1 && cDaysCurrentStreak > FlyingDayStreak)
            {
                FirstFlyingDayOfStreak = FirstDayOfCurrentStreak;
                LastFlyingDayOfStreak = LastDayOfCurrentStreak;
            }
                  
            // Distinct flights on dates
            if (FlightDates.ContainsKey(szDateKey))
                FlightDates[szDateKey] = FlightDates[szDateKey] + 1;
            else
                FlightDates[szDateKey] = 1;

            if (FlightDates[szDateKey] > MaxFlightsPerDay)
            {
                int cFlights = FlightDates[szDateKey];
                MaxFlightsPerDay = cFlights;
                miMostFlightsInDay.Progress = cFlights;
                miMostFlightsInDay.MatchingEventText = String.Format(CultureInfo.InvariantCulture, Resources.Achievements.RecentAchievementMostFlightsInDay, cFlights, cfr.dtFlight);
                miMostFlightsInDay.Query = new FlightQuery(Username) { DateRange = FlightQuery.DateRanges.Custom, DateMin = cfr.dtFlight, DateMax = cfr.dtFlight };
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

            // Furthest Flight & airport computations.
            AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);

            double distance = al.DistanceForRoute();
            if (distance > FurthestFlightDistance)
            {
                FurthestFlightDistance = distance;
                miFurthestFlight.Progress = 1;
                miFurthestFlight.MatchingEventID = cfr.flightID;
                miFurthestFlight.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsFurthestFlight, distance, dtFlight);
            }

            int cUniqueAirports = 0;
            HashSet<string> hsThisFlight = new HashSet<string>();
            foreach (airport ap in al.UniqueAirports)
            {
                // Dedupe as we go based on latitude/longitude, ignoring non-ports.  
                // We don't actually need the facility name here - so we can just round off the latitude/longitude and distinguish by type code.
                // Note: this can differ slightly from Visited Airports counts because for achievements, we're ignoring flights in training devices; visited airports doesn't ignore them.
                if (ap.IsPort)
                {
                    string szHash = ap.GeoHashKey;
                    if (!Airports.Contains(szHash))
                    {
                        Airports.Add(ap.GeoHashKey);
                        cUniqueAirports++;
                    }
                    hsThisFlight.Add(szHash);
                }
            }

            int cAirportsThisFlight = hsThisFlight.Count;
            if (cAirportsThisFlight > MostAirportsFlightCount)
            {
                MostAirportsFlightCount = cAirportsThisFlight;
                miMostAirportsFlight.MatchingEventID = cfr.flightID;
                miMostAirportsFlight.Progress = 1;
                miMostAirportsFlight.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsAirportsOnFlight, cAirportsThisFlight, dtFlight.ToShortDateString());
                miMostAirportsFlight.Query = new FlightQuery(Username) { DateRange = FlightQuery.DateRanges.Custom, DateMax = dtFlight, DateMin = dtFlight };
            }
        }
    }
}