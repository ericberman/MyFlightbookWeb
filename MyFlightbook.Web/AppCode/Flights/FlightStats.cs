using MyFlightbook.Airports;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2008-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightStatistics
{
    [Serializable]
    public class AirportStats : airport
    {
        private readonly HashSet<string> mUsers = new HashSet<string>();
        private readonly HashSet<string> mModels = new HashSet<string>();
        private readonly HashSet<string> mAircraft = new HashSet<string>();

        #region properties
        /// <summary>
        /// # of flights which referenced this in the period 
        /// </summary>
        public int Visits { get; set; }

        /// <summary>
        /// # of unique users who referenced this in the period
        /// </summary>
        public int Users { get { return mUsers.Count; } }

        /// <summary>
        /// # of unique models (typically ICAO code) that visited in the period
        /// </summary>
        public int Models { get { return mModels.Count; } }

        /// <summary>
        /// # of distinct aircraft that visited in the period.
        /// </summary>
        public int Aircraft { get { return mAircraft.Count; } }

        /// <summary>
        /// A display string of airport visit information.
        /// </summary>
        public string StatsDisplay
        {
            get { return String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageRecentStatsAirportInfo, Visits, Users, Aircraft, Models); }
        }
        #endregion

        public void Visit(string szUser, string szModel, string szTail)
        {
            Visits++;

            if (!String.IsNullOrEmpty(szUser))
                mUsers.Add(szUser);
            if (!String.IsNullOrEmpty(szModel))
                mModels.Add(szModel);
            if (!String.IsNullOrEmpty(szTail))
                mAircraft.Add(szTail);
        }

        #region Creation
        public AirportStats() : base() { }

        public AirportStats(airport ap) :this() 
        {
            util.CopyObject(ap, this);
        }
        #endregion
    }

    [Serializable]
    public class FlightStats
    {
        private const string szCacheKeyRecentStats = "FlightStatusCacheKey";

        #region Properties
        public int NumFlights { get; set; }
        public int NumUsers { get; set; }
        public int NumDays { get; set; }

        public int NumAircraft { get; set; }
        public int NumModels { get; set; }
        public int NumFlightsTotal { get; set; }

        public int MaxDays { get; set; } = 7;

        private readonly List<AirportStats> m_lstAirports = new List<AirportStats>();

        /// <summary>
        /// The set of airports that were visited, in descending order of frequency
        /// </summary>
        public IEnumerable<AirportStats> AirportsVisited { get { return m_lstAirports; } }

        private readonly List<string> m_lstPopularModels = new List<string>();

        /// <summary>
        /// The models that were used, by descneding order of frequency
        /// </summary>
        public IEnumerable<string> ModelsUsed { get { return m_lstPopularModels; } }

        /// <summary>
        /// The route with the longest total distance from start to finish
        /// </summary>
        public string LongestRoute { get; set; }

        /// <summary>
        /// The route that went the furthest from start to finish
        /// </summary>
        public string FurthestRoute { get; set; }

        /// <summary>
        /// The distance of the longest route
        /// </summary>
        public double LongestRouteDistance { get; set; }

        /// <summary>
        /// The distance of the furthest route
        /// </summary>
        public double FurthestRouteDistance { get; set; }

        public int CountriesVisited { get; private set; }

        private readonly List<LogbookEntry> m_lstFlights = new List<LogbookEntry>();

        public IEnumerable<LogbookEntry> RecentPublicFlights { get { return m_lstFlights; } }

        public bool HasSlowInformation { get; private set; }

        public IEnumerable<LinkedString> Stats
        {
            get
            {
                List<LinkedString> lstStats = new List<LinkedString>()
                {
                    new LinkedString(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageRecentStatsFlights, NumFlightsTotal, NumAircraft, NumModels)),
                };

                if (HasSlowInformation)
                {
                    const string szMapLinkTemplate = "~/Public/MapRoute2.aspx?Airports={0}";
                    lstStats.Add(new LinkedString(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageRecentStatsAirports, NumUsers, NumFlights, m_lstAirports.Count, CountriesVisited), VirtualPathUtility.ToAbsolute("~/Public/MyFlights.aspx")));
                    lstStats.Add(new LinkedString(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageRecentStatsLongestRoute, LongestRouteDistance), 
                        VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, szMapLinkTemplate, HttpUtility.UrlEncode(LongestRoute)))));
                    lstStats.Add(new LinkedString(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageRecentStatsFurthestRoute, FurthestRouteDistance),
                        VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, szMapLinkTemplate, HttpUtility.UrlEncode(FurthestRoute)))));
                }
                else
                {
                    lstStats.Add(new LinkedString(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageRecentStatsUsersFlights, NumUsers, NumFlights), VirtualPathUtility.ToAbsolute("~/Public/MyFlights.aspx")));
                }
                return lstStats;
            }
        }

        #endregion

        public FlightStats() { }

        private static FlightStats CachedStats()
        {
            return (HttpRuntime.Cache == null) ? null : (FlightStats)HttpRuntime.Cache[szCacheKeyRecentStats];
        }

        /// <summary>
        /// Updates recent flights for the specified flight, removing it if it is no longer public
        /// </summary>
        /// <param name="le"></param>
        public static void RefreshForFlight(LogbookEntryCore le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            FlightStats fs = CachedStats();

            if (fs != null && fs.RecentPublicFlights != null && !le.fIsPublic)
                fs.m_lstFlights.RemoveAll(l => l.FlightID == le.FlightID);
        }

        #region Updating
        private void Refresh30DayPublicFlights()
        {
            // we don't use the regular base query so that this can be super fast.  As a result, not all of the properties of the Logbook entry will be filled in.
            const string szQuery = @"
                SELECT 
                  f.*, 0 AS FlightDataLength, 
                  IF (ac.tailnumber LIKE '#%', CONCAT('(', m.model, ')'), ac.tailnumber) AS TailNumberDisplay, 
                  '' AS FlightVids,
                  0 AS HasDigitizedSignature,
                  0 AS distance,
                  '' AS flightpath,
                  0 AS telemetrytype
                FROM flights f 
                  INNER JOIN Aircraft ac ON f.idaircraft=ac.idaircraft 
                  INNER JOIN models m ON ac.idmodel=m.idmodel 
                WHERE f.Date > ?dateMin AND f.Date < ?dateMax AND f.fPublic <> 0 
                ORDER BY f.date DESC, f.idflight DESC 
                LIMIT 200";
            DBHelper dbh = new DBHelper(szQuery);

            dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("dateMin", DateTime.Now.AddDays(-30));
                    comm.Parameters.AddWithValue("dateMax", DateTime.Now.AddDays(1));
                },
                (dr) =>
                {
                    LogbookEntry le = new LogbookEntry(dr, (string)dr["username"]);
                    m_lstFlights.Add(le);
                });
        }

        private void RefreshUserCounts()
        {
            DBHelper dbh = new DBHelper("SELECT count(distinct(f.username)) AS numUsers, count(f.idflight) AS numFlights FROM flights f WHERE f.Date > ?dateMin AND f.Date < ?dateMax");
            NumDays = MaxDays;
            dbh.ReadRow((comm) =>
                {
                    comm.Parameters.AddWithValue("dateMin", DateTime.Now.AddDays(-MaxDays));
                    comm.Parameters.AddWithValue("dateMax", DateTime.Now.AddDays(1));
                },
                (dr) =>
                {
                    NumFlights = Convert.ToInt32(dr["numFlights"], CultureInfo.InvariantCulture);
                    NumUsers = Convert.ToInt32(dr["numUsers"], CultureInfo.InvariantCulture);
                });
        }

        private void RefreshAircraftAndModels()
        {

            DBHelper dbh = new DBHelper(@"SELECT (SELECT COUNT(*) FROM flights) AS numFlights, (SELECT COUNT(*) FROM aircraft WHERE instancetype=1) AS numaircraft, (SELECT COUNT(*) FROM models) AS nummodels");
            dbh.CommandArgs.Timeout = 300;  // use a long timeout
            dbh.ReadRow((comm) => { }, (dr) =>
            {
                NumFlightsTotal = Convert.ToInt32(dr["numFlights"], CultureInfo.InvariantCulture);
                NumAircraft = Convert.ToInt32(dr["numAircraft"], CultureInfo.InvariantCulture);
                NumModels = Convert.ToInt32(dr["numModels"], CultureInfo.InvariantCulture);
            });
        }

        // Handle the slower queries.  Spawns a new thread; don't bother waiting for result.
        private void RefreshSlowData()
        {
            m_lstAirports.Clear();

            FurthestRouteDistance = LongestRouteDistance = 0;
            FurthestRoute = LongestRoute = string.Empty;

            HashSet<string> hsAirportCodes = new HashSet<string>();
            HashSet<string> hsCountries = new HashSet<string>();
            Dictionary<string, AirportStats> dictVisited = new Dictionary<string, AirportStats>();
            new Thread(() =>
            {
                // Get all of the airports recorded for the last 30 days
                DBHelper dbh = new DBHelper("SELECT f.route, f.username, ac.tailnumber, m.family FROM flights f INNER JOIN aircraft ac ON f.idaircraft=ac.idaircraft INNER JOIN models m ON ac.idmodel=m.idmodel WHERE f.Date > ?dateMin AND f.Date < ?dateMax");
                dbh.ReadRows((comm) =>
                {
                    comm.Parameters.AddWithValue("dateMin", DateTime.Now.AddDays(-MaxDays));
                    comm.Parameters.AddWithValue("dateMax", DateTime.Now.AddDays(1));
                },
                (dr) =>
                {
                    foreach (string szCode in airport.SplitCodes(util.ReadNullableString(dr, "route").ToUpperInvariant()))
                        hsAirportCodes.Add(szCode);
                });

                // OK, now we have all of the airport codes - get the actual airports
                AirportList alMaster = new AirportList(String.Join(" ", hsAirportCodes));

                foreach (airport ap in alMaster.GetAirportList())
                {
                    if (!ap.IsPort)
                        continue;

                    dictVisited[ap.GeoHashKey] = new AirportStats(ap);

                    if (!String.IsNullOrWhiteSpace(ap.Country))
                        hsCountries.Add(ap.Country);
                }

                CountriesVisited = hsCountries.Count;

                // Now go through the flights a 2nd time and find longest route and the furthest route.
                dbh.ReadRows((comm) =>
                    {
                        comm.Parameters.AddWithValue("dateMin", DateTime.Now.AddDays(-MaxDays));
                        comm.Parameters.AddWithValue("dateMax", DateTime.Now.AddDays(1));
                    },
                    (dr) =>
                    {
                        string szRoute = util.ReadNullableString(dr, "route").ToUpperInvariant();
                        string szUser = util.ReadNullableString(dr, "username");
                        string szTail = util.ReadNullableString(dr, "tailnumber");
                        string szFamily = util.ReadNullableString(dr, "family");

                        AirportList al = alMaster.CloneSubset(szRoute, true);

                        foreach (airport ap in al.UniqueAirports)
                            if (dictVisited.ContainsKey(ap.GeoHashKey))
                                dictVisited[ap.GeoHashKey].Visit(szUser, szFamily, szTail);

                        double dRoute = al.DistanceForRoute();
                        double dFurthest = al.MaxDistanceFromStartingAirport();

                        if (dRoute > LongestRouteDistance && dRoute < 15000)
                        {
                            LongestRoute = szRoute;
                            LongestRouteDistance = dRoute;
                        }
                        if (dFurthest > FurthestRouteDistance && dFurthest < 15000)
                        {
                            FurthestRoute = szRoute;
                            FurthestRouteDistance = dFurthest;
                        }
                    });

                m_lstAirports.Clear();
                m_lstAirports.AddRange(dictVisited.Values);

                // Sort the list by number of visits, descending, and remove anything that never matched to an airport.
                m_lstAirports.Sort((ap1, ap2) => { return dictVisited[ap2.GeoHashKey].Visits.CompareTo(dictVisited[ap1.GeoHashKey].Visits); });

                // Popular models
                const string szPopularModels = @"SELECT 
                    man.manufacturer, IF(m.typename='', m.family, m.typename) AS icao, COUNT(f.idflight) AS num
                FROM
                    flights f
                        INNER JOIN
                    aircraft ac ON f.idaircraft = ac.idaircraft
                        INNER JOIN
                    models m ON ac.idmodel = m.idmodel
                        INNER JOIN
                    manufacturers man ON m.idmanufacturer = man.idmanufacturer
                WHERE
                    f.date > ?dateMin AND f.date <= ?dateMax
                        AND ac.InstanceType = 1
                        AND m.family <> ''
                GROUP BY icao
                ORDER BY num DESC
                LIMIT 30";

                m_lstPopularModels.Clear();
                dbh.CommandText = szPopularModels;
                dbh.ReadRows(
                    (comm) => 
                    {
                        comm.Parameters.AddWithValue("dateMin", DateTime.Now.AddDays(-MaxDays));
                        comm.Parameters.AddWithValue("dateMax", DateTime.Now.AddDays(1));
                    }, 
                    (dr) => { m_lstPopularModels.Add(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageRecentStatsModelInfo, dr["manufacturer"], dr["icao"], dr["num"])); });

                HasSlowInformation = true;
            }).Start();
        }
        #endregion

        /// <summary>
        /// Return stats for flights over the preceding N days.  This will be cached for 30 minutes
        /// </summary>
        /// <returns>A FlightStats object</returns>
        public static FlightStats GetFlightStats()
        {
            FlightStats fs = CachedStats();
            if (fs == null)
            {
                fs = new FlightStats();

                fs.Refresh30DayPublicFlights();
                fs.RefreshUserCounts();
                fs.RefreshAircraftAndModels();
                fs.RefreshSlowData();   // will happen asynchronously.

                if (HttpRuntime.Cache != null)
                    HttpRuntime.Cache.Add(szCacheKeyRecentStats, fs, null, DateTime.Now.AddMinutes(60), System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null);
            }
            return fs;
        }

        public override string ToString()
        {
            return String.Join("; ", Stats);
        }
    }
}