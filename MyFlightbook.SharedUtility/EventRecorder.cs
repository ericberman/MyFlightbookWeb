using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.SharedUtility.EventRecorder
{
    public static class EventRecorder
    {
        public enum MFBEventID { None, AuthUser = 1, GetAircraft, FlightsByDate, CommitFlightDEPRECATED, CreateAircraft, CreateUser, CreateUserAttemptDEPRECATED, CreateUserError, ExpiredToken, ObsoleteAPI };
        public enum MFBCountID { WSFlight, ImportedFlight };

        private static readonly ILogger wslogger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.File(System.IO.Path.GetTempPath() + "logs/MyFlightbook.log", formatProvider: CultureInfo.InvariantCulture, rollingInterval: RollingInterval.Day, encoding: System.Text.Encoding.UTF8).CreateLogger();

        public static void LogCall(string sz, params object[] list)
        {
            if (util.RequestContext.CurrentRequestUrl != null)
            {
                List<object> lstNewArgs = new List<object>() { util.RequestContext.CurrentRequestHostAddress, util.RequestContext.CurrentRequestUserAgent };
                lstNewArgs.AddRange(list);
                wslogger.Information("({ip}, {ua}) " + sz, lstNewArgs.ToArray());
            }
            else
                wslogger.Information(sz, list);
        }

        public static void WriteEvent(MFBEventID eventID, string szUser, string szDescription)
        {
            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery("INSERT INTO wsevents SET Date=?date, eventType=?eventType, user=?user, description=?description",
                (comm) =>
                {
                    comm.Parameters.AddWithValue("eventType", eventID);
                    comm.Parameters.AddWithValue("user", szUser);
                    comm.Parameters.AddWithValue("description", szDescription.LimitTo(126));
                    comm.Parameters.AddWithValue("date", DateTime.Now);
                });
        }

        public static void UpdateCount(MFBCountID id, int value)
        {
            string szQTemplate = "UPDATE EventCounts SET {0}={0}+{1} WHERE id=1";
            string szField = "";

            switch (id)
            {
                case MFBCountID.WSFlight:
                    szField = "WSCommittedFlights";
                    break;
                case MFBCountID.ImportedFlight:
                    szField = "ImportedFlights";
                    break;
                default:
                    break;
            }

            new DBHelper(String.Format(CultureInfo.InvariantCulture, szQTemplate, szField, value)).DoNonQuery();
        }

        /// <summary>
        /// Removes old webservice items of the specified events.
        /// </summary>
        /// <param name="eventID"></param>
        /// <exception cref="MyFlightbookException"></exception>
        public static int ADMINTrimOldItems(MFBEventID eventID)
        {
            // below is too slow
            // string szDelete = String.Format("DELETE w1 FROM wsevents w1 JOIN wsevents w2 ON (w1.eventType=w2.eventType AND w1.user=w2.user AND w1.eventID < w2.eventID) WHERE w1.eventType={0}", (int)eventID);
            Hashtable htUsers = new Hashtable();
            List<int> lstRowsToDelete = new List<int>();
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM wsevents w WHERE w.eventType={0} ORDER BY w.Date DESC", (int)eventID));
            if (!dbh.ReadRows(
                (comm) => { comm.CommandTimeout = 120; },
                (dr) =>
                {
                    string szUser = dr["user"].ToString();
                    DateTime dtLastEntered = Convert.ToDateTime(dr["Date"], CultureInfo.InvariantCulture);
                    if (htUsers[szUser] == null)
                        htUsers[szUser] = dtLastEntered;
                    else
                        lstRowsToDelete.Add(Convert.ToInt32(dr["eventID"], CultureInfo.InvariantCulture));
                }))
                throw new MyFlightbookException("Error trimming data: " + dbh.LastError);

            if (lstRowsToDelete.Any())
            {
                dbh.CommandText = String.Format(CultureInfo.InvariantCulture, "DELETE FROM wsevents WHERE eventID IN ({0})", String.Join(",", lstRowsToDelete));
                dbh.CommandArgs.Timeout = 300;
                dbh.DoNonQuery();
            }
            return lstRowsToDelete.Count;
        }
    }
}
