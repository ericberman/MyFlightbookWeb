using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Web;
using MyFlightbook.FlightCurrency;

/******************************************************
 * 
 * Copyright (c) 2009-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Maintains a mapping of which aircraft are used by which users
    /// </summary>
    public class UserAircraft
    {
        private System.Web.SessionState.HttpSessionState m_session = null;

        public string User { get; set; }

        public UserAircraft(string szUser)
        {
            User = szUser;
            m_session = (HttpContext.Current == null) ? null : HttpContext.Current.Session;
        }

        private string SessionCacheKey
        {
            get { return "uaKey" + User; }
        }

        private Aircraft[] CachedAircraft
        {
            get { return (m_session == null) ? null : (Aircraft[])m_session[SessionCacheKey]; }
            set { if (m_session != null) m_session[SessionCacheKey] = value; }
        }

        public void InvalidateCache()
        {
            if (m_session != null)
                m_session.Remove(SessionCacheKey);
        }

        public enum AircraftRestriction { UserAircraft, AllMakeModel, AllAircraft, AllSims };

        /// <summary>
        /// Returns a list of the aircraft available for the user
        /// </summary>
        /// <param name="restriction">which set of aircraft to get?</param>
        /// <param name="idModel">The model id to restrict on (only applies for AllmakeModel)</param>
        /// <returns>An array of aircraft</returns>
        public Aircraft[] GetAircraftForUser(AircraftRestriction restriction, int idModel = -1)
        {
            // If no authenticated user, return immediately - don't hit the database!
            // Should never happen.
            if (String.IsNullOrEmpty(User) && restriction == AircraftRestriction.UserAircraft)
                return new Aircraft[0];

            Aircraft[] rgAircraft = CachedAircraft;

            if (rgAircraft != null && restriction == AircraftRestriction.UserAircraft) // don't cache in admin mode
                return rgAircraft;

            string szRestrict = "";
            string szFlags = "0";
            string szPrivateNotes = "''";
            string szDefaultImage = "''";

            switch (restriction)
            {
                case AircraftRestriction.AllAircraft:
                default:
                    break;
                case AircraftRestriction.AllSims:
                    szRestrict = String.Format(CultureInfo.InvariantCulture, " WHERE aircraft.InstanceType <> {0} {1}", (int)AircraftInstanceTypes.RealAircraft, idModel > 0 ? " AND aircraft.idModel = ?idModel " : String.Empty);
                    break;
                case AircraftRestriction.UserAircraft:
                    szRestrict = "INNER JOIN useraircraft ON aircraft.idAircraft = useraircraft.idAircraft WHERE useraircraft.userName = ?UserName";
                    szFlags = "useraircraft.flags";
                    szPrivateNotes = "useraircraft.PrivateNotes";
                    szDefaultImage = "DefaultImage";
                    break;
                case AircraftRestriction.AllMakeModel:
                    szRestrict = " WHERE aircraft.idModel = ?idModel ";
                    break;
            }

            string szQ = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["AircraftForUserCore"].ToString(), szFlags, szDefaultImage, szPrivateNotes, szRestrict);
            ArrayList alAircraft = new ArrayList();

            DBHelper dbh = new DBHelper(szQ);
            if (!dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("UserName", User);
                    comm.Parameters.AddWithValue("idModel", idModel);
                },
                (dr) => { alAircraft.Add(new Aircraft(dr)); }))
                throw new MyFlightbookException("Error getting aircraft for user: " + szQ + "\r\n" + dbh.LastError);

            rgAircraft = (Aircraft[])alAircraft.ToArray(typeof(Aircraft));

            if (restriction == AircraftRestriction.UserAircraft)
                CachedAircraft = rgAircraft;

            return rgAircraft;
        }

        public Aircraft[] GetAircraftForUser()
        {
            return GetAircraftForUser(AircraftRestriction.UserAircraft);
        }

        /// <summary>
        /// Returns a specified aircraft in the user's list (by ID)
        /// </summary>
        /// <param name="id">The ID of the aircraft</param>
        /// <returns>The matching aircraft, if any</returns>
        public Aircraft GetUserAircraftByID(int id)
        {
            Aircraft[] rgac = GetAircraftForUser();
            foreach (Aircraft ac in rgac)
                if (ac.AircraftID == id)
                    return ac;
            return null;
        }

        /// <summary>
        /// Returns a specified aircraft in the user's list by tail.
        /// </summary>
        /// <param name="szTail"></param>
        /// <returns>The first matching aircraft.  Might not be the right one if you have clones in your account...</returns>
        public Aircraft GetUserAircraftByTail(string szTail)
        {
            if (String.IsNullOrWhiteSpace(szTail))
                return null;

            szTail = Aircraft.NormalizeTail(szTail).Trim().ToUpperInvariant();
            Aircraft[] rgac = GetAircraftForUser();
            foreach (Aircraft ac in rgac)
                if (ac.TailNumber.CompareCurrentCultureIgnoreCase(szTail) == 0)
                    return ac;
            return null;
        }

        /// <summary>
        /// Checks to see if the user already knows about the specified aircraft
        /// </summary>
        /// <param name="ac">The aircraft to check</param>
        /// <returns>True if they already know about it</returns>
        public Boolean CheckAircraftForUser(Aircraft ac)
        {
            Aircraft[] rgac = GetAircraftForUser();

            if (rgac != null && ac != null)
            {
                foreach (Aircraft ac2 in rgac)
                {
                    if (ac2.AircraftID == ac.AircraftID)
                        return (new Aircraft(ac.AircraftID).AircraftID == ac2.AircraftID);  // extra DB hit, but ensures the aircraft is still valid.
                }
            }

            return false;
        }

        /// <summary>
        /// Adds/updates the aircraft for the specified user
        /// </summary>
        /// <param name="ac">The aircraft to add</param>
        /// <returns>True for success</returns>
        public void FAddAircraftForUser(Aircraft ac)
        {
            DBHelper dbh = new DBHelper((CheckAircraftForUser(ac)) ?
                "UPDATE useraircraft SET Flags=?acFlags, PrivateNotes=?userNotes, DefaultImage=?defImg WHERE userName=?username AND idAircraft=?AircraftID" :
                "INSERT INTO useraircraft SET userName = ?username, idAircraft = ?AircraftID, Flags=?acflags, PrivateNotes=?userNotes, DefaultImage=?defImg");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("username", User);
                comm.Parameters.AddWithValue("AircraftID", ac.AircraftID);
                comm.Parameters.AddWithValue("acflags", ac.FlagsValue);
                comm.Parameters.AddWithValue("userNotes", String.IsNullOrEmpty(ac.PrivateNotes) ? null : ac.PrivateNotes.LimitTo(4094));
                comm.Parameters.AddWithValue("defImg", String.IsNullOrEmpty(ac.DefaultImage) ? null : ac.DefaultImage.LimitTo(255));
            });
            if (dbh.LastError.Length == 0)
                InvalidateCache();
            else
                throw new MyFlightbookException("Error adding aircraft for user " + User + " \r\n" + dbh.LastError);
        }

        /// <summary>
        /// Replaces one aircraft for another for the user.  The old aircraft is not migrated if any existing flights use it.
        /// </summary>
        /// <param name="acNew">The new aircraft</param>
        /// <param name="acOld">The old aircraft</param>
        /// <param name="fMigrateFlights">True to migrate any existing flights.  If false, the old flights (and the old aircraft) may remain.</param>
        public void ReplaceAircraftForUser(Aircraft acNew, Aircraft acOld, bool fMigrateFlights)
        {
            if (acNew == null)
                throw new ArgumentNullException("acNew");
            if (acOld == null)
                throw new ArgumentNullException("acOld");

            if (acNew.AircraftID == acOld.AircraftID)
                return;

            // Add the new aircraft first
            FAddAircraftForUser(acNew);

            // Migrate any flights, if necessary...
            if (fMigrateFlights)
            {
                LogbookEntry.UpdateFlightAircraftForUser(this.User, acOld.AircraftID, acNew.AircraftID);

                // And migrate any deadlines associated with the aircraft
                foreach (DeadlineCurrency dc in DeadlineCurrency.DeadlinesForUser(User, acOld.AircraftID))
                {
                    dc.AircraftID = acNew.AircraftID;
                    dc.FCommit();
                }

                try
                {
                    // Then delete the old aircraft.  Ignore any errors
                    FDeleteAircraftforUser(acOld.AircraftID);
                }
                catch (MyFlightbookException)
                { }
            }
        }

        /// <summary>
        /// Deletes an aircraft from the user's list.  Does NOT remove the underlying aircraft.
        /// </summary>
        /// <param name="AircraftID">The ID of the aircraft to delete</param>
        /// <returns>The updated list of aircraft for the user.</returns>
        public Aircraft[] FDeleteAircraftforUser(int AircraftID)
        {
            if (String.IsNullOrEmpty(User))
                throw new MyFlightbookException("No user specified for Deleteaircraft");

            AircraftStats acs = new AircraftStats(User, AircraftID);

            // if the user has no flights with this aircraft, simply remove it from their list and be done
            if (acs.UserFlights != 0)
                throw new MyFlightbookException(Resources.Aircraft.errAircraftInUse);
            else
            {
                new DBHelper().DoNonQuery("DELETE FROM useraircraft WHERE userName=?username AND idAircraft=?aircraftID",
                    (comm) =>
                    {
                        comm.Parameters.AddWithValue("username", User);
                        comm.Parameters.AddWithValue("aircraftID", AircraftID);
                    }
                );
                InvalidateCache();
            }

            // Delete any deadlines associated with this aircraft
            foreach (DeadlineCurrency dc in DeadlineCurrency.DeadlinesForUser(User, AircraftID))
                dc.FDelete();

            // we don't actually delete the aircraft; no need to do so, even if it's not used by anybody because
            // (a) we can't force caches of aircraft lists to be invalid and, 
            // (b) no harm from keeping it - if somebody re-uses the tailnumber, it will re-use the existing flight.

            return GetAircraftForUser();
        }

        /// <summary>
        /// Admin function - occassionally we get duplicates in the table.  No biggie, but clean them up anyhow.
        /// </summary>
        public static int CleanUpDupes()
        {
            List<string> lstDupeIds = new List<string>();
            DBHelper dbh = new DBHelper("SELECT username, idaircraft, GROUP_CONCAT(id SEPARATOR ',') AS idList, COUNT(idaircraft) AS numInstance FROM useraircraft GROUP BY username, idaircraft HAVING numInstance > 1");
            dbh.ReadRows((comm) => { },
                (dr) =>
                {
                    string[] rgIds = (dr["idList"].ToString()).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    // Add all but the 1st element to the list of id's to delete
                    for (int i = 1; i < rgIds.Length; i++)
                        lstDupeIds.Add(rgIds[i]);
                });
            int cDupes = lstDupeIds.Count;
            if (cDupes > 0)
            {
                dbh.CommandText = String.Format(CultureInfo.InvariantCulture, "DELETE FROM useraircraft WHERE id IN ({0})", String.Join(",", lstDupeIds.ToArray()));
                dbh.DoNonQuery();
            }
            return cDupes;
        }
    }
}