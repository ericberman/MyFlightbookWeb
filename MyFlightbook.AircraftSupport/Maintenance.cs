using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2009-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// A MaintenanceRecord that for an aircraft
    /// </summary>
    [Serializable]
    public class MaintenanceRecord
    {
        #region Properties
        /// <summary>
        /// Date of the last VOR check
        /// </summary>
        public DateTime LastVOR { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Date of the last altimeter check
        /// </summary>
        public DateTime LastAltimeter { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Date of the last transponder test
        /// </summary>
        public DateTime LastTransponder { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Date of the last ELT test
        /// </summary>
        public DateTime LastELT { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Date of last static test
        /// </summary>
        public DateTime LastStatic { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Date of last 100-hr inspection
        /// </summary>
        public Decimal Last100 { get; set; } = 0.0M;

        /// <summary>
        /// Hobbs of last Oil change
        /// </summary>
        public Decimal LastOilChange { get; set; } = 0.0M;

        /// <summary>
        /// Last new engine (i.e., how many hours does this one have on it?)
        /// </summary>
        public Decimal LastNewEngine { get; set; } = 0.0M;

        /// <summary>
        /// Date of the last annual
        /// </summary>
        public DateTime LastAnnual { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Date of next annual, if known
        /// </summary>
        public DateTime NextAnnual
        {
            get { return LastAnnual.AddCalendarMonths(12); }
        }

        /// <summary>
        /// Date of next VOR, if known
        /// </summary>
        public DateTime NextVOR
        {
            get { return (LastVOR.CompareTo(DateTime.MinValue) == 0) ? DateTime.MinValue : LastVOR.AddDays(30); }
        }

        /// <summary>
        /// Date of next altimiter check, if known
        /// </summary>
        public DateTime NextAltimeter
        {
            get { return LastAltimeter.AddCalendarMonths(24); }
        }

        /// <summary>
        /// Date of next transponder check, if known
        /// </summary>
        public DateTime NextTransponder
        {
            get { return LastTransponder.AddCalendarMonths(24); }
        }

        /// <summary>
        /// Date of next pitot-static check, if known
        /// </summary>
        public DateTime NextStatic
        {
            get { return LastStatic.AddCalendarMonths(24); }
        }

        /// <summary>
        /// Date of next ELT check, if known
        /// </summary>
        public DateTime NextELT
        {
            get { return LastELT.AddCalendarMonths(12); }
        }

        /// <summary>
        /// Hobbs of next 100-hr check, if known
        /// </summary>
        public Decimal Next100
        {
            get { return (Last100 == 0.0M) ? 0.0M : Last100 + 100.0M; }
        }

        /// <summary>
        /// Date that the next registration renewal is due
        /// </summary>
        public DateTime RegistrationExpiration { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Any comments/remarks on this update (e.g., for a VOR check the place/method/error of a VOR check)
        /// </summary>
        public string Notes { get; set; } = string.Empty;
        #endregion

        public MaintenanceRecord() { }

        /// <summary>
        /// Create a copy of an existing MaintenanceRecord so that diffs can be computed.
        /// </summary>
        /// <param name="mr"></param>
        public MaintenanceRecord(MaintenanceRecord mr) : this()
        {
            if (mr != null)
                JsonConvert.PopulateObject(JsonConvert.SerializeObject(mr), this);
        }
    }

    /// <summary>
    /// Keeps track of who did what to the airplane.
    /// </summary>
    [Serializable]
    public class MaintenanceLog
    {
        public MaintenanceLog()
        {
            AircraftID = -1;
            Comment = Description = TailNumber = User = UserFullName = string.Empty;
            ChangeDate = DateTime.Now;
        }

        #region Properties
        /// <summary>
        /// The name of the user who made the change
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// The full (first/last) name of the user, if known.  Does not get committed.
        /// </summary>
        public string UserFullName { get; private set; }

        /// <summary>
        /// The full display name (username + display name, if known).  Does not get committed.
        /// </summary>
        public string FullDisplayName
        {
            get { return (UserFullName.Length > 0) ? UserFullName : User; }
        }

        /// <summary>
        /// The date of the change
        /// </summary>
        public DateTime ChangeDate { get; set; }

        /// <summary>
        /// The description of the change
        /// </summary>
        public string Description { get; set; }


        /// <summary>
        /// Additional comments
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// The tail number of the aircraft that was modified
        /// </summary>
        public string TailNumber { get; private set; }

        /// <summary>
        /// The id of the aircraft in question
        /// </summary>
        public int AircraftID { get; set; }
        #endregion

        private void InitFromDataReader(MySqlDataReader dr)
        {
            try
            {
                AircraftID = Convert.ToInt32(dr["idAircraft"], CultureInfo.InvariantCulture);
                TailNumber = dr["TailNumber"].ToString();
                Description = dr["Description"].ToString();
                ChangeDate = Convert.ToDateTime(dr["Date"], CultureInfo.InvariantCulture);
                User = dr["User"].ToString();
                string szFirst = dr["FirstName"].ToString();
                string szLast = dr["LastName"].ToString();
                UserFullName = $"{szFirst} {szLast}".Trim();
                Comment = dr["Comment"].ToString();

            }
            catch (Exception ex)
            {
                throw new MyFlightbookException("Error reading field - " + ex.Message);
            }
        }

        /// <summary>
        /// Appends this item to the maintenance log.  The log is assumed append-only; editing of entries is not supported.
        /// </summary>
        /// <returns>True for success</returns>
        public void FAddToLog()
        {
            if (String.IsNullOrWhiteSpace(Description) && String.IsNullOrWhiteSpace(Comment))
                return; // don't both logging unhelpful content-free changes.
            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery("INSERT INTO maintenancelog SET idaircraft=?idAircraft, Description=?Description, Date=?Date, User=?User, Comment = ?Comment",
                (comm) =>
                {
                    comm.Parameters.AddWithValue("idAircraft", AircraftID);
                    comm.Parameters.AddWithValue("Description", Description);
                    comm.Parameters.AddWithValue("Date", ChangeDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                    comm.Parameters.AddWithValue("User", User);
                    comm.Parameters.AddWithValue("Comment", Comment);
                });
            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException("Error adding to log: " + dbh.CommandText + "\r\n" + dbh.LastError);
        }

        private static void AddLogToArray(IList<MaintenanceLog> lst, MySqlDataReader dr)
        {
            MaintenanceLog ml = new MaintenanceLog();
            ml.InitFromDataReader(dr);
            lst.Add(ml);
        }

        private const string szMaintenanceLogBaseQuery = @"SELECT m.idaircraft as idAircraft, m.Description as Description, m.Date as Date, m.User as User, m.Comment as Comment, aircraft.tailnumber as Tailnumber, users.FirstName as firstname, users.lastname as lastname
      FROM maintenancelog m
       INNER JOIN users ON (m.User = users.Username)
       INNER JOIN aircraft ON (m.idAircraft = aircraft.idaircraft)
      WHERE {0}
      ORDER BY m.Date desc, id desc;";

        /// <summary>
        /// Get a log of all the changes made to a specific aircraft
        /// </summary>
        /// <param name="aircraftid">The ID of the aircraft</param>
        /// <returns>An array of all edits to that aircraft</returns>
        public static MaintenanceLog[] ChangesByAircraftID(int aircraftid)
        {
            List<MaintenanceLog> lst = new List<MaintenanceLog>();
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szMaintenanceLogBaseQuery, "m.idaircraft = ?idAircraft"));
            dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("idAircraft", aircraftid); },
                (dr) => { AddLogToArray(lst, dr); });

            return lst.ToArray();
        }
    }
}