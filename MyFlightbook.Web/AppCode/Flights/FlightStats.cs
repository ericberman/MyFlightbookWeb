using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2008-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightStats
{
    [Serializable]
    public class FlightStats
    {
        private const string szCacheKeyRecentStats = "FlightStatusCacheKey";

        public int NumFlights { get; set; }
        public int NumUsers { get; set; }
        public int NumDays { get; set; }

        public int NumAircraft { get; set; }
        public int NumModels { get; set; }
        public int NumFlightsTotal { get; set; }

        private readonly List<LogbookEntry> m_lstFlights = new List<LogbookEntry>();

        public IEnumerable<LogbookEntry> RecentPublicFlights { get { return m_lstFlights; } }

        private const int MaxDays = 7;

        public FlightStats() { }

        private static string CacheKey()
        {
            return szCacheKeyRecentStats;
        }

        private static FlightStats CachedStats()
        {
            return (HttpRuntime.Cache == null) ? null : (FlightStats)HttpRuntime.Cache[CacheKey()];
        }

        /// <summary>
        /// Updates recent flights for the specified flight, removing it if it is no longer public
        /// </summary>
        /// <param name="le"></param>
        public static void RefreshForFlight(LogbookEntryBase le)
        {
            FlightStats fs = CachedStats();

            if (fs != null && fs.RecentPublicFlights != null && !le.fIsPublic)
                fs.m_lstFlights.RemoveAll(l => l.FlightID == le.FlightID);
        }

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
                        fs.m_lstFlights.Add(le);
                    });

                dbh.CommandText = "SELECT count(distinct(f.username)) AS numUsers, count(f.idflight) AS numFlights FROM flights f WHERE f.Date > ?dateMin AND f.Date < ?dateMax";
                dbh.CommandArgs.Parameters.Clear();
                dbh.ReadRow((comm) =>
                {
                    comm.Parameters.AddWithValue("dateMin", DateTime.Now.AddDays(-MaxDays));
                    comm.Parameters.AddWithValue("dateMax", DateTime.Now.AddDays(1));
                },
                    (dr) =>
                    {
                        fs.NumDays = MaxDays;
                        fs.NumFlights = Convert.ToInt32(dr["numFlights"], CultureInfo.InvariantCulture);
                        fs.NumUsers = Convert.ToInt32(dr["numUsers"], CultureInfo.InvariantCulture);
                    });

                // Get a few more interesting stats
                dbh.CommandText = @"SELECT (SELECT COUNT(*) FROM flights) AS numFlights, (SELECT COUNT(*) FROM aircraft WHERE instancetype=1) AS numaircraft, (SELECT COUNT(*) FROM models) AS nummodels";
                dbh.CommandArgs.Timeout = 300;  // use a long timeout
                dbh.ReadRow((comm) => { }, (dr) =>
                {
                    fs.NumFlightsTotal = Convert.ToInt32(dr["numFlights"], CultureInfo.InvariantCulture);
                    fs.NumAircraft = Convert.ToInt32(dr["numAircraft"], CultureInfo.InvariantCulture);
                    fs.NumModels = Convert.ToInt32(dr["numModels"], CultureInfo.InvariantCulture);
                });

                if (HttpRuntime.Cache != null)
                    HttpRuntime.Cache.Add(CacheKey(), fs, null, DateTime.Now.AddMinutes(30), System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null);
            }
            return fs;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.HomePageStats, NumUsers, NumFlights, NumDays);
        }
    }
}