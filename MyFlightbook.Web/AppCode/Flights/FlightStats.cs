using MyFlightbook.Airports;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2008-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightStatistics
{
    [Serializable]
    public class AirportStats : airport
    {
        private readonly HashSet<string> mUsers = new HashSet<string>();
        private readonly Dictionary<string, int> mModels = new Dictionary<string, int>();
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
        /// The names of the models that were used in DESCENDING order of frequency.
        /// </summary>
        public IEnumerable<string> ModelsUsed { 
            get 
            {
                List<string> lst = new List<string>(mModels.Keys);
                lst.Sort((s1, s2) => { return mModels[s2].CompareTo(mModels[s1]); });
                return lst; 
            }
        }

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
            if (!String.IsNullOrWhiteSpace(szModel))
            {
                if (!mModels.ContainsKey(szModel))
                    mModels[szModel] = 1;
                mModels[szModel]++;
            }
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
                    lstStats.Add(new LinkedString(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageRecentStatsAirports, NumUsers, NumFlights, m_lstAirports.Count, CountriesVisited), VirtualPathUtility.ToAbsolute("~/mvc/flights/myflights")));
                else
                    lstStats.Add(new LinkedString(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageRecentStatsUsersFlights, NumUsers, NumFlights), VirtualPathUtility.ToAbsolute("~/mvc/flights/myflights")));
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
                  0 AS telemetrytype,
                  '' AS metadata,
                  0 AS idModel,
                  false AS IsOverridden,
                  0 AS idcategoryclass,
                  0 AS InstanceType,
                  '' AS ShortModelDisplay,
                  '' AS FamilyDisplay
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

            new Thread(() =>
            {
                HashSet<string> hsAirportCodes = new HashSet<string>();
                HashSet<string> hsCountries = new HashSet<string>();
                Dictionary<string, AirportStats> dictVisited = new Dictionary<string, AirportStats>();

                try
                {
                    Refresh30DayPublicFlights();
                    RefreshUserCounts();
                    RefreshAircraftAndModels();

                    // Get all of the airports recorded for the last 30 days
                    DBHelper dbh = new DBHelper(@"SELECT 
    f.route,
    f.username,
    ac.tailnumber,
    CONCAT(man.manufacturer,
            ' ',
            IF(m.typename = '',
                IF(m.family = '', m.model, m.family),
                m.typename)) AS family
FROM
    flights f
        INNER JOIN
    aircraft ac ON f.idaircraft = ac.idaircraft
        INNER JOIN
    models m ON ac.idmodel = m.idmodel
        INNER JOIN
    manufacturers man ON m.idmanufacturer = man.idmanufacturer
WHERE f.Date > ?dateMin AND f.Date < ?dateMax AND ac.InstanceType = 1");
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

                        string szKey = ap.GeoHashKey;
                        if (!dictVisited.ContainsKey(szKey))
                            dictVisited.Add(szKey, new AirportStats(ap));

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
                        });

                    m_lstAirports.Clear();
                    m_lstAirports.AddRange(dictVisited.Values);

                    // Sort the list by number of visits, descending, and remove anything that never matched to an airport.
                    m_lstAirports.Sort((ap1, ap2) =>
                    {
                        bool f1 = dictVisited.ContainsKey(ap1.GeoHashKey);
                        bool f2 = dictVisited.ContainsKey(ap2.GeoHashKey);
                        if (!f1 || !f2)
                        {
                            return 0;   // should never happen, but can due to rounding between unique airports and getairportlist.  
                        }
                        return dictVisited[ap2.GeoHashKey].Visits.CompareTo(dictVisited[ap1.GeoHashKey].Visits);
                    });

                    // Popular models
                    const string szPopularModels = @"SELECT 
                    man.manufacturer, IF(m.typename='', IF(m.family = '', m.model, m.family), m.typename) AS icao, COUNT(f.idflight) AS num
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
                } catch (Exception ex) when (!(ex is OutOfMemoryException))
                {
                    util.NotifyAdminException("Error getting flight stats: " + ex.Message, ex);
                }
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

                fs.RefreshSlowData();   // will happen asynchronously.

                HttpRuntime.Cache?.Add(szCacheKeyRecentStats, fs, null, DateTime.Now.AddMinutes(60), System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null);
            }
            return fs;
        }

        public override string ToString()
        {
            return String.Join("; ", Stats);
        }
    }
}