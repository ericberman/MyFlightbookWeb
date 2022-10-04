using MyFlightbook.Currency;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2009-2022 MyFlightbook LLC
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
                UserFullName = (szFirst + Resources.LocalizedText.LocalizedSpace + szLast).Trim();
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

        private static void AddLogToArray(ArrayList al, MySqlDataReader dr)
        {
            MaintenanceLog ml = new MaintenanceLog();
            ml.InitFromDataReader(dr);
            al.Add(ml);
        }

        /// <summary>
        /// Get a log of all the changes made to a specific aircraft
        /// </summary>
        /// <param name="aircraftid">The ID of the aircraft</param>
        /// <returns>An array of all edits to that aircraft</returns>
        public static MaintenanceLog[] ChangesByAircraftID(int aircraftid)
        {
            ArrayList al = new ArrayList();
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["GetMaintenanceLog"], "m.idaircraft = ?idAircraft"));
            dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("idAircraft", aircraftid); },
                (dr) => { AddLogToArray(al, dr); });

            return (MaintenanceLog[])al.ToArray(typeof(MaintenanceLog));
        }

        public static Aircraft[] AircraftMaintainedByUser(string szUser)
        {
            Aircraft[] rgar = null;

            // short-circuit a call to the database if szUser is empty - we know there will be no result.
            if (String.IsNullOrEmpty(szUser))
                return rgar;

            string szRestrict = @"INNER JOIN useraircraft ON aircraft.idAircraft = useraircraft.idAircraft 
INNER JOIN maintenancelog ON maintenancelog.user = useraircraft.userName AND maintenancelog.idaircraft = aircraft.idaircraft
WHERE useraircraft.userName = ?UserName AND (flags & 0x0008) = 0";
            string szQ = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["AircraftForUserCore"], "useraircraft.flags", "''", "''", "''", szRestrict);
            ArrayList alar = new ArrayList();

            DBHelper dbh = new DBHelper(szQ);
            if (!dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("UserName", szUser); },
                (dr) => { alar.Add(new Aircraft(dr)); }))
                throw new MyFlightbookException("Error getting aircraft maintained by user " + szUser + " " + szQ + "\r\n" + dbh.LastError);

            rgar = (Aircraft[])alar.ToArray(typeof(Aircraft));

            return rgar;
        }

        public static IEnumerable<CurrencyStatusItem> AircraftInspectionWarningsForUser(string szUser, IEnumerable<DeadlineCurrency> aircraftDeadlines)
        {
            Aircraft[] rgar = AircraftMaintainedByUser(szUser);
            List<CurrencyStatusItem> arcs = new List<CurrencyStatusItem>();

            List<DeadlineCurrency> lstDeadlines = (aircraftDeadlines == null) ? new List<DeadlineCurrency>() : new List<DeadlineCurrency>(aircraftDeadlines);

            if (rgar != null)
            {
                int maxWindow = Profile.GetUser(szUser).GetPreferenceForKey(MFBConstants.keyWindowAircraftMaintenance, MFBConstants.DefaultMaintenanceWindow);
                foreach (Aircraft ar in rgar)
                {
                    MaintenanceRecord mr = ar.Maintenance;

                    AddPendingInspection(arcs, ar.TailNumber + Resources.Aircraft.CurrencyAltimeter, mr.NextAltimeter, ar.AircraftID, 30, maxWindow);
                    AddPendingInspection(arcs, ar.TailNumber + Resources.Aircraft.CurrencyAnnual, mr.NextAnnual, ar.AircraftID, 30, maxWindow);
                    AddPendingInspection(arcs, ar.TailNumber + Resources.Aircraft.CurrencyELT, mr.NextELT, ar.AircraftID, 30, maxWindow);
                    AddPendingInspection(arcs, ar.TailNumber + Resources.Aircraft.CurrencyPitot, mr.NextStatic, ar.AircraftID, 30, maxWindow);
                    AddPendingInspection(arcs, ar.TailNumber + Resources.Aircraft.CurrencyXPonder, mr.NextTransponder, ar.AircraftID, 30, maxWindow);
                    AddPendingInspection(arcs, ar.TailNumber + Resources.Aircraft.CurrencyVOR, mr.NextVOR, ar.AircraftID, 4, maxWindow);
                    AddPendingInspection(arcs, ar.TailNumber + Resources.Aircraft.CurrencyRegistration, mr.RegistrationExpiration, ar.AircraftID, 30, maxWindow);

                    arcs.AddRange(DeadlineCurrency.CurrencyForDeadlines(lstDeadlines.FindAll(dc => ar.AircraftID == dc.AircraftID && (maxWindow < 0 || dc.Expiration.Subtract(DateTime.Now).TotalDays <= maxWindow))));
                }
            }

            return arcs;
        }

        private static void AddPendingInspection(List<CurrencyStatusItem> arcs, string szLabel, DateTime dt, int idAircraft, int warningThreshold, int window)
        {
            if (!dt.HasValue())
                return;

            int daysUntilDue = (int) Math.Ceiling(dt.Subtract(DateTime.Now).TotalDays);

            if (daysUntilDue < 0)
                arcs.Add(new CurrencyStatusItem(szLabel, dt.ToShortDateString(), CurrencyState.NotCurrent, String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.CurrencyOverdue, Math.Abs(daysUntilDue).ToString(CultureInfo.CurrentCulture))) { AssociatedResourceID = idAircraft, CurrencyGroup = CurrencyStatusItem.CurrencyGroups.Aircraft });
            else if (daysUntilDue <= window || window < 0)
                arcs.Add(new CurrencyStatusItem(szLabel, dt.ToShortDateString(), daysUntilDue < warningThreshold ? CurrencyState.GettingClose : CurrencyState.OK, String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.CurrencyDue, daysUntilDue)) { AssociatedResourceID = idAircraft, CurrencyGroup = CurrencyStatusItem.CurrencyGroups.Aircraft });
        }
    }
}