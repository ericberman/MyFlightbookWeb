using MyFlightbook.FlightCurrency;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2009-2019 MyFlightbook LLC
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
        public string User { get; set; }

        public UserAircraft(string szUser)
        {
            User = szUser;
        }

        private const string szCacheKey = "userAircraftKey";

        private Aircraft[] CachedAircraft
        {
            get { return String.IsNullOrEmpty(User) ? null : (Aircraft[]) Profile.GetUser(User).CachedObject(szCacheKey); }
            set
            {
                if (!String.IsNullOrEmpty(User))
                    Profile.GetUser(User).AssociatedData[szCacheKey] = value;
            }
        }

        public void InvalidateCache()
        {
            if (!String.IsNullOrEmpty(User))
                Profile.GetUser(User).AssociatedData.Remove(szCacheKey);
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
            string szTemplates = "''";

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
                    szTemplates = "useraircraft.TemplateIDs";
                    break;
                case AircraftRestriction.AllMakeModel:
                    szRestrict = " WHERE aircraft.idModel = ?idModel ";
                    break;
            }

            string szQ = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["AircraftForUserCore"].ToString(), szFlags, szDefaultImage, szPrivateNotes, szTemplates, szRestrict);
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
        /// Flushes stats for user aircraft; called when a flight is deleted, updated, or added.  No-op if no cached aircraft
        /// </summary>
        public void FlushStatsForUser()
        {
            if (CachedAircraft == null)
                return;

            IEnumerable<Aircraft> rgac = GetAircraftForUser();
            foreach (Aircraft ac in rgac)
                ac.Stats = null;
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
            DBHelper dbh = new DBHelper("REPLACE INTO useraircraft SET Flags=?acFlags, PrivateNotes=?userNotes, DefaultImage=?defImg, TemplateIDs=?templates, userName=?username, idAircraft=?AircraftID");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("username", User);
                comm.Parameters.AddWithValue("AircraftID", ac.AircraftID);
                comm.Parameters.AddWithValue("acflags", ac.FlagsValue);
                comm.Parameters.AddWithValue("userNotes", String.IsNullOrEmpty(ac.PrivateNotes) ? null : ac.PrivateNotes.LimitTo(4094));
                comm.Parameters.AddWithValue("defImg", String.IsNullOrEmpty(ac.DefaultImage) ? null : ac.DefaultImage.LimitTo(255));
                comm.Parameters.AddWithValue("templates", ac.DefaultTemplates.Count == 0 ? null : JsonConvert.SerializeObject(ac.DefaultTemplates));
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
                List<Aircraft> lstAc = new List<Aircraft>(GetAircraftForUser());
                
                // make sure we are populated with both old and new so that UpdateFlightAircraftForUser works.
                // (This can happen if you have one version of an aircraft and you go to add another version of it; 
                // they won't both be there, but the query used in UpdateFlightAircraftForUser wants them both present.
                if (!lstAc.Exists(ac => ac.AircraftID == acNew.AircraftID))
                    lstAc.Add(acNew);
                if (!lstAc.Exists(ac => ac.AircraftID == acOld.AircraftID))
                    lstAc.Add(acOld);
                CachedAircraft = lstAc.ToArray();   // we'll nullify the cache below.
                
                LogbookEntry.UpdateFlightAircraftForUser(this.User, acOld.AircraftID, acNew.AircraftID);

                // Migrate any custom currencies associated with the aircraft
                foreach (CustomCurrency cc in CustomCurrency.CustomCurrenciesForUser(User))
                {
                    List<int> lst = new List<int>(cc.AircraftRestriction);

                    for (int i = 0; i < lst.Count; i++)
                    {
                        if (lst[i] == acOld.AircraftID)
                        {
                            lst[i] = acNew.AircraftID;
                            cc.AircraftRestriction = lst;
                            cc.FCommit();
                            break;
                        }
                    }
                }

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
                {
                    InvalidateCache();
                }
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

            // And delete any custom currencies associated with the aircraft
            foreach (CustomCurrency cc in CustomCurrency.CustomCurrenciesForUser(User))
            {
                List<int> ids = new List<int>(cc.AircraftRestriction);

                if (ids.Contains(AircraftID))
                {
                    ids.Remove(AircraftID);
                    cc.AircraftRestriction = ids;
                    cc.FCommit();
                }
            }

            // we don't actually delete the aircraft; no need to do so, even if it's not used by anybody because
            // (a) we can't force caches of aircraft lists to be invalid and, 
            // (b) no harm from keeping it - if somebody re-uses the tailnumber, it will re-use the existing flight.

            return GetAircraftForUser();
        }
    }
}