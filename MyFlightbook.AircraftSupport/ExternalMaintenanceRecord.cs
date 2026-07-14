using MyFlightbook.AircraftSupport.Maintenance.TachTime;
using MyFlightbook.AircraftSupport.Maintenance.MyTailLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2009-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.AircraftSupport.Maintenance
{
    public enum ExternalMaintenanceSourceID
    {
        Unknown = 0,
        TachTime = 1,
        MyTailLog
    }

    public delegate ExternalMaintenanceRecord MaintenanceSourceFactory(string user, int aircraftID, string json, decimal highWaterTach, decimal highWaterHobbs, DateTime timestamp);

    public static class ExternalMaintenanceRegistry
    {
        private static readonly Dictionary<ExternalMaintenanceSourceID, MaintenanceSourceFactory> _map = new Dictionary<ExternalMaintenanceSourceID, MaintenanceSourceFactory>()
        {
            { ExternalMaintenanceSourceID.Unknown, (user, aircraftID, json, highWaterTach,highWaterHobbs, timestamp) => new ExternalMaintenanceRecord() { Username = user, AircraftID = aircraftID, JSONData = json, HighWaterTach = highWaterTach, HighWaterHobbs = highWaterHobbs, LastUpdated = timestamp } },
            { ExternalMaintenanceSourceID.TachTime, (user, aircraftID, json, highWaterTach,highWaterHobbs, timestamp) => new TachTimeRecord(user, aircraftID, json) { HighWaterTach = highWaterTach, HighWaterHobbs = highWaterHobbs, LastUpdated = timestamp } },
            { ExternalMaintenanceSourceID.MyTailLog, (user, aircraftID, json, highWaterTach,highWaterHobbs, timestamp) => new MyTailLogRecord(user, aircraftID, json) {HighWaterTach = highWaterTach, HighWaterHobbs = highWaterHobbs, LastUpdated = timestamp } }
        };

        public static ExternalMaintenanceRecord Create(ExternalMaintenanceSourceID sourceID, string user, int aircraftID, string json, decimal highWaterTach, decimal highWaterHobbs, DateTime timestamp)
            => _map.TryGetValue(sourceID, out var factory)
                ? factory(user, aircraftID, json, highWaterTach, highWaterHobbs, timestamp)
                : throw new InvalidOperationException($"Unknown external maintenance source: {(int) sourceID} ({sourceID}");

        public static string SourceName(this ExternalMaintenanceSourceID emsid)
        {
            switch (emsid)
            {
                case ExternalMaintenanceSourceID.TachTime:
                    return "TachTime";
                case ExternalMaintenanceSourceID.MyTailLog:
                    return "MyTailLog";
                case ExternalMaintenanceSourceID.Unknown:
                    return emsid.ToString();
            }
            throw new InvalidOperationException($"No string available for {emsid}");
        }

        public static string TokenPreferenceKey(this ExternalMaintenanceSourceID emsid)
        {
            switch (emsid)
            {
                case ExternalMaintenanceSourceID.TachTime:
                    return "TachTimeToken";
                case ExternalMaintenanceSourceID.MyTailLog:
                    return "MyTailLogToken";
                default:
                    throw new InvalidOperationException($"No preference key available for {emsid}");
            }
        }
    }

    /// <summary>
    /// Abstraction for an external maintenance status
    /// </summary>
    public interface IExternalCurrencyStatus
    {
        string Name { get; }
        DateTime? DateDue { get; }
        DateTime? DateDone { get; }

        decimal HoursDue { get; }
        decimal HoursDone { get; }

        /// <summary>
        /// does this use hours or calendar time?
        /// </summary>
        bool UsesHours { get; }
    }

    /// <summary>
    /// A base class (abstract, but not explicitly so) that represents an external source for maintenance information for an aircraft. This could be a maintenance provider, a maintenance tracking system, or any other external entity that provides maintenance data for the aircraft.
    /// </summary>
    public class ExternalMaintenanceRecord
    {
        #region Properties
        /// <summary>
        /// The ID of the aircraft that this record tracks
        /// </summary>
        public int AircraftID { get; set; } = Aircraft.idAircraftUnknown;
        /// <summary>
        /// The user for whom this record applies
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The ID of the source of the data (e.g., TachTime)
        /// </summary>
        public ExternalMaintenanceSourceID DataSource { get; set; }

        /// <summary>
        /// The raw JSON data of the maintenance record
        /// </summary>
        public string JSONData { get; set; }

        /// <summary>
        /// The hobbs, if known, at the time of this record
        /// </summary>
        public decimal HighWaterHobbs { get; set; }

        /// <summary>
        /// The tach, if known, at the time of this record
        /// </summary>
        public decimal HighWaterTach { get; set; }

        /// <summary>
        /// The timestamp when this record was recorded/updated
        /// </summary>
        public DateTime LastUpdated { get; set; }

        public virtual object DataAsType { get { return null; } }
        #endregion

        public virtual IEnumerable<MaintenanceLog> GetMaintenanceLog() { return Array.Empty<MaintenanceLog>(); }
        public virtual IEnumerable<IExternalCurrencyStatus> GetExternalCurrencies() { return Array.Empty<IExternalCurrencyStatus>(); }

        #region Constructors
        public ExternalMaintenanceRecord()
        {
            DataSource = ExternalMaintenanceSourceID.Unknown;
        }
        #endregion

        #region database
        public bool FCommit()
        {
            DBHelper dbh = new DBHelper("REPLACE INTO externalmaintenance SET sourceID=?sourceID, idaircraft=?idaircraft, username=?username, jsonData=?jsonData, highWaterTach=?hwt, highWaterHobbs=?hwh, lastUpdated=UTC_TIMESTAMP();");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("?sourceID", (int)DataSource);
                comm.Parameters.AddWithValue("?idaircraft", AircraftID);
                comm.Parameters.AddWithValue("?username", Username);
                comm.Parameters.AddWithValue("?jsonData", JSONData);
                comm.Parameters.AddWithValue("?hwt", HighWaterTach);
                comm.Parameters.AddWithValue("?hwh", HighWaterHobbs);
            });
            return String.IsNullOrEmpty(dbh.LastError);
        }

        public bool FDelete()
        {
            DBHelper dbh = new DBHelper("DELETE FROM externalmaintenance WHERE idaircraft=?idaircraft AND username=?username");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("?username", Username);
                comm.Parameters.AddWithValue("?idaircraft", AircraftID);
            });
            return String.IsNullOrEmpty(dbh.LastError);
        }

        public static bool FDeleteForUser(string userName, ExternalMaintenanceSourceID sourceID)
        {
            if (String.IsNullOrWhiteSpace(userName))
                throw new ArgumentNullException(nameof(userName));
            DBHelper dbh = new DBHelper("DELETE FROM externalmaintenance WHERE username=?username AND sourceID=?sourceID");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("?username", userName);
                comm.Parameters.AddWithValue("?sourceID", (int)sourceID);
            });
            return String.IsNullOrEmpty(dbh.LastError);
        }

        private static ExternalMaintenanceRecord FromDataReader(IDataReader dr)
        {
            return ExternalMaintenanceRegistry.Create((ExternalMaintenanceSourceID)Convert.ToInt32(dr["sourceID"], CultureInfo.InvariantCulture),
                    (string)dr["username"],
                    Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture),
                    (string)dr["jsonData"],
                    Convert.ToDecimal(dr["highWaterTach"], CultureInfo.InvariantCulture),
                    Convert.ToDecimal(dr["highWaterHobbs"], CultureInfo.InvariantCulture),
                    DateTime.SpecifyKind(Convert.ToDateTime(dr["lastUpdated"], CultureInfo.InvariantCulture), DateTimeKind.Utc));
        }

        /// <summary>
        /// Retrieve external maintenance for a specific aircraft
        /// </summary>
        /// <param name="idAircraft"></param>
        /// <returns></returns>
        public static IEnumerable<ExternalMaintenanceRecord> GetExternalMaintenanceRecords(int idAircraft)
        {
            return GetExternalMaintenanceRecords(new int[] {idAircraft });
        }

        /// <summary>
        /// Retrieve external maintenance for any in a set of aircraft
        /// </summary>
        /// <param name="lstIds"></param>
        /// <returns></returns>
        public static IEnumerable<ExternalMaintenanceRecord> GetExternalMaintenanceRecords(IEnumerable<int> lstIds)
        {
            // short circuit the common case (from currency) of no maintained aircraft
            if (!(lstIds?.Any() ?? false))
                return Array.Empty<ExternalMaintenanceRecord>();
            List<ExternalMaintenanceRecord> lst = new List<ExternalMaintenanceRecord>();
            DBHelper db = new DBHelper($"SELECT * FROM externalmaintenance WHERE idaircraft IN ({String.Join(", ", lstIds)})");
            db.ReadRows((comm) =>  { }, (dr) => { lst.Add(FromDataReader(dr)); });
            return lst;
        }
        #endregion

        public static DateTime DateIfLater(DateTime? proposed, DateTime def)
        {
            if (proposed == null)
                return def;
            DateTime dt = DateTime.SpecifyKind(proposed.Value, DateTimeKind.Utc).Date;
            return dt.CompareTo(def) > 0 ? dt : def;
        }
    }
}
