using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2012-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Provides stats for an aircraft: how many people fly it, how many flights are recorded, and, if for a specified user, the dates of the first/last flight in it.
    /// </summary>
    [Serializable]
    public class AircraftStats
    {
        #region Properties
        /// <summary>
        /// The ID of the aircraft for which stats are desired
        /// </summary>
        public int AircraftID { get; set; }

        /// <summary>
        /// The name of the user for which stats are desired
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// The number of flights for the specified airplane by the specified user
        /// </summary>
        public int UserFlights { get; private set; }

        /// <summary>
        /// The number of users who have flights with this aircraft
        /// </summary>
        public int Users { get; private set; }

        public IEnumerable<string> UserNames { get; private set; }

        /// <summary>
        /// The number of flights for the specified airplane, period.
        /// </summary>
        public int Flights { get; private set; }

        /// <summary>
        /// Hours (total) in the aircraft
        /// </summary>
        public decimal Hours { get; private set; }

        /// <summary>
        /// Date of the earliest date for the user in this aircraft, if known
        /// </summary>
        public DateTime? EarliestDate { get; set; }

        /// <summary>
        /// Date of the latest date for the user in this aircraft, if known
        /// </summary>
        public DateTime? LatestDate { get; set; }

        /// <summary>
        /// Linked display for the stats for the flight.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public LinkedString UserStatsDisplay
        {
            get
            {
                if (UserFlights == 0)
                    return new LinkedString(Resources.Aircraft.AircraftStatsNeverFlown, null);
                else
                {
                    Profile pf = Profile.GetUser(User);
                    string szTotalHours = Hours > 0 ? String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.AircraftStatsHours, pf.UsesHHMM ?
                        String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.AircraftStatsHoursHHMM, Hours.ToHHMM()) :
                        String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.AircraftStatsHoursDecimal, Hours)) : string.Empty;

                    return

                        new LinkedString(String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.AircraftStatsFlown,
                                            UserFlights,
                                            EarliestDate.HasValue && LatestDate.HasValue ? String.Format(CultureInfo.CurrentCulture,
                                                    Resources.Aircraft.AircraftStatsDate,
                                                    EarliestDate.Value.ToShortDateString(), LatestDate.Value.ToShortDateString()) :
                                            string.Empty) + szTotalHours,
                    String.IsNullOrEmpty(User) ? null : String.Format(CultureInfo.InvariantCulture, "~/mvc/flights?ft=Totals&fq={0}", new FlightQuery() { UserName = User, AircraftIDList = new int[] { AircraftID } }.ToBase64CompressedJSONString()));
                }
            }
        }

        private static readonly char[] usernameSeparator = new char[] { ';' };
        #endregion

        #region Constructors
        private void InitFromDataReader(IDataReader dr)
        {
            Flights = Convert.ToInt32(dr["numFlights"], CultureInfo.InvariantCulture);
            UserFlights = Convert.ToInt32(dr["flightsForUser"], CultureInfo.InvariantCulture);
            Users = Convert.ToInt32(dr["numUsers"], CultureInfo.InvariantCulture);
            UserNames = dr["userNames"].ToString().Split(usernameSeparator, StringSplitOptions.RemoveEmptyEntries);

            object o = dr["EarliestDate"];
            if (o != DBNull.Value)
                EarliestDate = (DateTime)o;
            o = dr["LatestDate"];
            if (o != DBNull.Value)
                LatestDate = (DateTime)o;
            o = dr["idaircraft"];
            if (o != DBNull.Value)
                AircraftID = Convert.ToInt32(o, CultureInfo.InvariantCulture);
            o = dr["hours"];
            if (o != DBNull.Value)
                Hours = Convert.ToDecimal(o, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Creates a new AircraftStats object
        /// </summary>
        public AircraftStats()
        {
            User = string.Empty;
            AircraftID = Aircraft.idAircraftUnknown;
            EarliestDate = new DateTime?();
            LatestDate = new DateTime?();
        }

        public AircraftStats(string szUser, int aircraftID) : this()
        {
            GetStats(aircraftID, szUser, (dr) =>
            {
                InitFromDataReader(dr);
                User = szUser;
            });
        }

        internal AircraftStats(IDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            InitFromDataReader(dr);
        }
        #endregion

        #region Getting stats for aircraft
        private static void GetStats(int idAircraft, string szUser, Action<IDataReader> readStats)
        {
            DBHelper dbh = new DBHelper(@"SELECT 
    ac.idaircraft,
    (SELECT 
            COUNT(idFlight)
        FROM
            flights
        WHERE
            idaircraft = ?aircraftID) AS numFlights,
    (SELECT 
            COUNT(ua.username)
        FROM
            useraircraft ua
        WHERE
            ua.idaircraft = ?aircraftID) AS numUsers,
    (SELECT 
            GROUP_CONCAT(DISTINCT ua.username
                    SEPARATOR ';')
        FROM
            useraircraft ua
        WHERE
            ua.idaircraft = ?aircraftID) AS userNames,
    COUNT(DISTINCT (f2.idflight)) AS flightsForUser,
    SUM(ROUND(f2.TotalFlightTime * ?qf) / ?qf) AS hours,
    MIN(f2.date) AS EarliestDate,
    MAX(f2.date) AS LatestDate
FROM
    aircraft ac
        INNER JOIN
    flights f2 ON f2.idaircraft = ac.idaircraft
WHERE
    ac.idaircraft = ?aircraftID
        AND f2.username = ?user
");

            dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("user", szUser);
                    comm.Parameters.AddWithValue("aircraftID", idAircraft);
                    comm.Parameters.AddWithValue("qf", Profile.GetUser(szUser).MathRoundingUnit);
                },
                (dr) => { readStats(dr); }
                );
        }
        #endregion

        public static void PopulateStatsForAircraft(IEnumerable<Aircraft> lstAc, string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));
            if (lstAc == null)
                throw new ArgumentNullException(nameof(lstAc));

            foreach (Aircraft ac in lstAc)
                ac.Stats = new AircraftStats(szUser, ac.AircraftID);
        }

        /// <summary>
        /// Populate statistics for a specific user in a specific aircraft.
        /// </summary>
        /// <param name="listUsers">true to list all of the users (admin only!)</param>
        /// <returns>An enumerable of linked strings.</returns>
        public IEnumerable<LinkedString> StatsForUserInAircraft(bool listUsers)
        {
            List<LinkedString> lst = new List<LinkedString>
            {
                // Add overall stats
                new LinkedString(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.EditAircraftUserStats, Users, Flights)),

                // And add personal stats
                UserStatsDisplay
            };

            if (listUsers)
                lst.Add(new LinkedString(String.Format(CultureInfo.CurrentCulture, "Users = {0}", String.Join(", ", UserNames))));

            return lst;
        }
    }

}