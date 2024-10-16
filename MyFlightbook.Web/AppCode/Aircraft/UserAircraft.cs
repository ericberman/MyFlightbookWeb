using MyFlightbook.Currency;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2009-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    [Serializable]
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
        private const string szCachedDictionary = "userAircraftDictionaryKey";

        /// <summary>
        /// Internally, we can assume this is a list.
        /// </summary>
        private List<Aircraft> CachedAircraft
        {
            get { return String.IsNullOrEmpty(User) ? null : (List<Aircraft>) Profile.GetUser(User).CachedObject(szCacheKey); }
            set
            {
                if (!String.IsNullOrEmpty(User))
                    Profile.GetUser(User).AssociatedData[szCacheKey] = value;
            }
        }

        public void InvalidateCache()
        {
            if (!String.IsNullOrEmpty(User))
            {
                Profile pf = Profile.GetUser(User);
                pf.AssociatedData.Remove(szCacheKey);
                pf.AssociatedData.Remove(szCachedDictionary);
            }
        }

        public enum AircraftRestriction { UserAircraft, AllMakeModel, AllAircraft, AllSims };

        /// <summary>
        /// Returns a list of the aircraft available for the user
        /// </summary>
        /// <param name="restriction">which set of aircraft to get?</param>
        /// <param name="idModel">The model id to restrict on (only applies for AllmakeModel)</param>
        /// <returns>A LIST of aircraft</returns>
        private List<Aircraft> GetAircraftForUserInternal(AircraftRestriction restriction, int idModel = -1)
        {
            // If no authenticated user, return immediately - don't hit the database!
            // Should never happen.
            if (String.IsNullOrEmpty(User) && restriction == AircraftRestriction.UserAircraft)
                return new List<Aircraft>();

            List<Aircraft> rgAircraft = null;

            if (restriction == AircraftRestriction.UserAircraft && (rgAircraft = CachedAircraft) != null) // don't cache in admin mode
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

            string szQ = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["AircraftForUserCore"], szFlags, szDefaultImage, szPrivateNotes, szTemplates, szRestrict);
            List<Aircraft> lst = new List<Aircraft>();

            DBHelper dbh = new DBHelper(szQ);
            if (!dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("UserName", User);
                    comm.Parameters.AddWithValue("idModel", idModel);
                },
                (dr) => { lst.Add(new Aircraft(dr)); }))
                throw new MyFlightbookException("Error getting aircraft for user: " + szQ + "\r\n" + dbh.LastError);

            if (restriction == AircraftRestriction.UserAircraft)
                CachedAircraft = lst;

            return lst;
        }

        /// <summary>
        /// Returns a list of the aircraft available for the user
        /// </summary>
        /// <param name="restriction">which set of aircraft to get?</param>
        /// <param name="idModel">The model id to restrict on (only applies for AllmakeModel)</param>
        /// <returns>An enumerable of aircraft</returns>
        public IEnumerable<Aircraft> GetAircraftForUser(AircraftRestriction restriction, int idModel = -1)
        {
            return GetAircraftForUserInternal(restriction, idModel);
        }

        public IEnumerable<Aircraft> GetAircraftForUser()
        {
            return GetAircraftForUserInternal(AircraftRestriction.UserAircraft);
        }

        /// <summary>
        /// Returns a specified aircraft in the user's list (by ID)
        /// </summary>
        /// <param name="id">The ID of the aircraft</param>
        /// <returns>The matching aircraft, if any</returns>
        public Aircraft GetUserAircraftByID(int id)
        {
            List<Aircraft> rgac = GetAircraftForUserInternal(AircraftRestriction.UserAircraft);
            return rgac.Find(ac => ac.AircraftID == id);
        }

        /// <summary>
        /// Returns a specified aircraft in the user's list (by ID)
        /// </summary>
        /// <param name="id">The ID of the aircraft</param>
        /// <returns>The matching aircraft, if any</returns>
        public Aircraft this[int id]
        {
            get { return GetUserAircraftByID(id); }
        }

        /// <summary>
        /// Returns a specified aircraft in the user's list by tail.
        /// </summary>
        /// <param name="szTail"></param>
        /// <returns>The first matching aircraft.  Might not be the right one if you have clones in your account...</returns>
        public Aircraft this[string szTail]
        {
            get
            {
                if (String.IsNullOrWhiteSpace(szTail))
                    return null;

                szTail = Aircraft.NormalizeTail(szTail).Trim().ToUpperInvariant();
                List<Aircraft> rgac = GetAircraftForUserInternal(AircraftRestriction.UserAircraft);
                return rgac.Find(ac => ac.SearchTail.CompareCurrentCultureIgnoreCase(szTail) == 0);
            }
        }


        /// <summary>
        /// The number of aircraft.
        /// </summary>
        public int Count
        {
            get { return GetAircraftForUserInternal(AircraftRestriction.UserAircraft).Count; }
        }

        public IEnumerable<Aircraft> FindMatching(Predicate<Aircraft> pred)
        {
            return GetAircraftForUserInternal(AircraftRestriction.UserAircraft).FindAll(pred);
        }

        public Aircraft Find(Predicate<Aircraft> pred)
        {
            return GetAircraftForUserInternal(AircraftRestriction.UserAircraft).Find(pred);
        }

        private readonly static LazyRegex rAlias = new LazyRegex("#ALT(?<altname>[a-zA-Z0-9-]+)#", RegexOptions.IgnoreCase);

        /// <summary>
        /// Returns a string-indexable dictionary of tails 
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, Aircraft> DictAircraftForUser()
        {
            Profile pf = Profile.GetUser(User);
            if (pf.AssociatedData.TryGetValue(szCachedDictionary, out object value))
                return (IDictionary<string, Aircraft>)value;

            Dictionary<string, Aircraft> dictReturn = new Dictionary<string, Aircraft>();

            IEnumerable<Aircraft> rgac = GetAircraftForUser();

            if (rgac != null)
            {
                foreach (Aircraft ac in rgac)
                {
                    // Issue #163: bias towards active over inactive.
                    // We add the aircraft to the dictionary if it's (a) not there, or (b) there but the existing one is inactive.
                    // I.e., if the existing aircraft in the dictionary is present and active, then we don't overwrite it.
                    string szKey = Aircraft.NormalizeTail(ac.TailNumber);
                    if (!dictReturn.ContainsKey(szKey) || dictReturn[szKey].HideFromSelection)
                        dictReturn[szKey] = ac;

                    // To support broken systems like crewtrac, which don't use standard naming, allow for "#ALTxxx#" in the private notes
                    // as a way to map.  E.g., Virgin America uses simple 3-digit aircraft IDs like "483" for N483VA.  
                    // This hack allows a private note of "#ALT483#" in N483VA to allow "483" to map to N483VA.
                    // Use with care. :)
                    MatchCollection mcAliases = rAlias.Matches(ac.PrivateNotes ?? string.Empty);
                    foreach (Match m in mcAliases)
                        dictReturn[m.Groups["altname"].Value] = ac;
                }
            }

            // One more hack for helping find the right aircraft:
            // For anonymous aircraft, it's useful to include those aircraft registrations; we can then use that to detect otherwise missing aircraft and map them to this one
            DBHelper dbh = new DBHelper("select fp.StringValue AS 'tail', f.idaircraft from flightproperties fp inner join flights f on fp.idflight=f.idflight where f.username=?user and fp.idproptype=559 group by fp.StringValue");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("user", User); }, (dr) =>
            {
                string szTail = Aircraft.NormalizeTail((string)dr["tail"]);
                if (!String.IsNullOrWhiteSpace(szTail) && !dictReturn.ContainsKey(szTail) && int.TryParse(dr["idaircraft"].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int aircraftID))
                {
                    Aircraft ac = this[aircraftID];
                    if (ac != null && !String.IsNullOrEmpty(szTail))
                            dictReturn[szTail] = ac;
                }
            });

            pf.AssociatedData[szCachedDictionary] = dictReturn;

            return dictReturn;
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
            IEnumerable<Aircraft> rgac = GetAircraftForUser();

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
            // Issue #1050: support foreign-key constraint for flights.
            // We can't use replace into because that does a delete, which violates the constraint
            // Instead, use Insert Into/On Duplicate Key
            DBHelper dbh = new DBHelper(@"INSERT INTO useraircraft (Flags, PrivateNotes, DefaultImage, TemplateIDs, userName, IDAircraft) VALUES (?acFlags, ?userNotes, ?defImg, ?templates, ?username, ?AircraftID)
ON DUPLICATE KEY UPDATE flags=?acFlags, privatenotes=?userNotes, defaultimage=?defImg, templateids=?templates");
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
                throw new ArgumentNullException(nameof(acNew));
            if (acOld == null)
                throw new ArgumentNullException(nameof(acOld));

            if (acNew.AircraftID == acOld.AircraftID)
                return;

            // Add the new aircraft first
            // don't add an aircraft that's already in the list
            if (!CheckAircraftForUser(acNew))
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
                CachedAircraft = lstAc;   // we'll nullify the cache below.
                
                LogbookEntryBase.UpdateFlightAircraftForUser(User, acOld.AircraftID, acNew.AircraftID);
                FlushStatsForUser();

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
                catch (Exception ex) when (ex is MyFlightbookException)
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
        public IEnumerable<Aircraft> FDeleteAircraftforUser(int AircraftID)
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

        /// <summary>
        /// Populates a data table with user aircraft data.  Caller owns the data table (responsible for IDisposable), ensuring that it is empty
        /// Datatable is all string or int to ensure proper formatting for user's culture
        /// </summary>
        /// <param name="dt">The data table</param>
        /// <param name="fRefreshStats">True to refresh stats for the user</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void ToDataTable(DataTable dt, bool fRefreshStats)
        {
            if (dt == null)
                throw new ArgumentNullException(nameof(dt));

            List<Aircraft> lst = GetAircraftForUserInternal(AircraftRestriction.UserAircraft);
            if (fRefreshStats)
                AircraftStats.PopulateStatsForAircraft(lst, User);  // in case not already done.

            dt.Columns.Add(new DataColumn("Aircraft ID", typeof(int)));
            dt.Columns.Add(new DataColumn("Tail Number", typeof(string)));
            dt.Columns.Add(new DataColumn("Display Tail Number", typeof(string)));
            dt.Columns.Add(new DataColumn("Training Device Kind", typeof(string)));
            dt.Columns.Add(new DataColumn("Category/Class", typeof(string)));
            dt.Columns.Add(new DataColumn("Manufacturer", typeof(string)));
            dt.Columns.Add(new DataColumn("Model", typeof(string)));
            dt.Columns.Add(new DataColumn("Type designation", typeof(string)));
            dt.Columns.Add(new DataColumn("Model full name", typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceAnnual, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceAnnualDue, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceTransponder, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceTransponderDue, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenancePitotStatic, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenancePitotStaticDue, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceAltimeter, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceAltimeterDue, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceELT, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceELTDue, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceVOR, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceVORDue, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.Maintenance100, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.Maintenance100Due, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceOil, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceOilDue25, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceOilDue50, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceOilDue100, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceEngine, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Aircraft.MaintenanceRegistration, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.Currency.deadlinesHeaderDeadlines, typeof(string)));
            dt.Columns.Add(new DataColumn("Frequently Used", typeof(string)));
            dt.Columns.Add(new DataColumn("Public Notes", typeof(string)));
            dt.Columns.Add(new DataColumn("Private Notes", typeof(string)));
            dt.Columns.Add(new DataColumn("Flight Count", typeof(int)));
            dt.Columns.Add(new DataColumn("Hours", typeof(string)));
            dt.Columns.Add(new DataColumn("First Flight", typeof(string)));
            dt.Columns.Add(new DataColumn("LastFlight", typeof(string)));

            foreach (Aircraft ac in lst)
            {
                DataRow dr = dt.NewRow();
                MakeModel m = MakeModel.GetModel(ac.ModelID);
                decimal lastOil = ac.LastOilChange;

                dr[0] = ac.AircraftID;
                dr[1] = ac.TailNumber;
                dr[2] = ac.DisplayTailnumber;
                dr[3] = ac.InstanceTypeDescription;
                dr[4] = ac.CategoryClassDisplay;
                dr[5] = m.ManufacturerDisplay;
                dr[6] = ac.ModelDescription;
                dr[7] = m.TypeName;
                dr[8] = m.ModelDisplayNameNoCatclass;
                dr[9] = ac.LastAnnual.YMDString();
                dr[10] = ac.Maintenance.NextAnnual.YMDString();
                dr[11] = ac.LastTransponder.YMDString();
                dr[12] = ac.Maintenance.NextTransponder.YMDString();
                dr[13] = ac.LastStatic.YMDString();
                dr[14] = ac.Maintenance.NextStatic.YMDString();
                dr[15] = ac.LastAltimeter.YMDString();
                dr[16] = ac.Maintenance.NextAltimeter.YMDString();
                dr[17] = ac.LastELT.YMDString();
                dr[18] = ac.Maintenance.NextELT.YMDString();
                dr[19] = ac.LastVOR.YMDString();
                dr[20] = ac.Maintenance.NextVOR.YMDString();
                dr[21] = ac.Last100.FormatDecimal(false);
                dr[22] = ac.Maintenance.Next100.FormatDecimal(false);
                dr[23] = lastOil.FormatDecimal(false);
                dr[24] = (lastOil > 0 ? lastOil + 25 : 0.0M).FormatDecimal(false);
                dr[25] = (lastOil > 0 ? lastOil + 50 : 0.0M).FormatDecimal(false);
                dr[26] = (lastOil > 0 ? lastOil + 100 : 0.0M).FormatDecimal(false);
                dr[27] = ac.LastNewEngine.FormatDecimal(false);
                dr[28] = ac.RegistrationDue.YMDString();
                dr[29] = Currency.DeadlineCurrency.CoalescedDeadlinesForAircraft(User, ac.AircraftID);
                dr[30] = ac.HideFromSelection ? string.Empty : "yes";
                dr[31] = ac.PublicNotes;
                dr[32] = ac.PrivateNotes;
                dr[33] = ac.Stats.UserFlights;
                dr[34] = ac.Stats.Hours.FormatDecimal(false);
                dr[35] = ac.Stats.EarliestDate.HasValue ? ac.Stats.EarliestDate.Value.YMDString() : string.Empty;
                dr[36] = ac.Stats.LatestDate.HasValue ? ac.Stats.LatestDate.Value.YMDString() : string.Empty;
                dt.Rows.Add(dr);
            }
        }

    }
}