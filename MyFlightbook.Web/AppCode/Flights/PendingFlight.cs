using DotNetOpenAuth.OAuth2;
using MyFlightbook.OAuth.CloudAhoy;
using MyFlightbook.OAuth.FlightCrewView;
using MyFlightbook.OAuth.Leon;
using MyFlightbook.OAuth.RosterBuster;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2019-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// A flight that needs to be reviewed before it is saved; it has either not yet occured, or it has been imported but not saved
    /// As such, it may fail various validation checks, and is stored in a separate table in JSON format.
    /// </summary>
    [Serializable]
    public class PendingFlight : LogbookEntry
    {
        #region Properties
        /// <summary>
        /// The ID of the flight in the PENDING table.  Assigned on create.
        /// </summary>
        public string PendingID { get; set; }

        // Null this out to avoid pointless JSON bloat
        public override string SendFlightLink
        {
            get { return null; }
            set { }
        }

        // Null this out to avoid pointless JSON bloat
        public override string SocialMediaLink
        {
            get { return null; }
            set { }
        }
        #endregion

        #region Constructors
        public PendingFlight() : base()
        {
            PendingID = Guid.NewGuid().ToString();
        }

        public PendingFlight(string ID) : this()
        {
            PendingID = ID;
        }

        private PendingFlight(MySqlDataReader dr) : this()
        {
            User = dr["username"].ToString();
            PendingID = dr["id"].ToString();
            string jsonflight = dr["jsonflight"].ToString();
            if (jsonflight != null)
                JsonConvert.PopulateObject(jsonflight, this);
            // Populating an object doesn't fully flesh out a custom property with its type
            foreach (CustomFlightProperty cfp in this.CustomProperties)
                cfp.InitPropertyType(new CustomPropertyType[] { CustomPropertyType.GetCustomPropertyType(cfp.PropTypeID) });
            // Also fix up any missing tail mappings - aircraft may have been added to the user's account since we last saved this pending flight
        }

        public PendingFlight(LogbookEntryBase le) : this()
        {
            if (le == null)
                return;
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(le, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore }), this);
        }
        #endregion

        #region Utility
        /// <summary>
        /// Sets the AircraftID for the flight from the display tail number, if the aircraftID is not already set and if the tail number display can be found.
        /// </summary>
        public void MapTail()
        {
            if (AircraftID > 0 || String.IsNullOrEmpty(TailNumDisplay))
                return;

            UserAircraft ua = new UserAircraft(User);
            Aircraft ac = ua[TailNumDisplay];
            if (ac != null)
                AircraftID = ac.AircraftID;
            else
            {
                // Issue #1156: see if we need to add this aircraft to the user's aircraft list.
                // For simplicity, we will just add if there is precisely one match, ignoring any model hits.
                List<Aircraft> rgac = Aircraft.AircraftMatchingTail(TailNumDisplay);
                if (rgac.Count == 1)
                {
                    ac = rgac[0];
                    AircraftID = ac.AircraftID;
                    ua.FAddAircraftForUser(ac);
                }
            }
        }
        #endregion

        #region Database
        #region Caching
        private const string szCacheKeyPrefix = "pendingFlights";

        static private string CacheKeyForUser(string szUser)
        {
            return szCacheKeyPrefix + szUser;
        }

        static public void FlushCacheForUser(string szUser)
        {
            CacheForUser(szUser, null);
        }

        static private void CacheForUser(string szUser, IEnumerable<PendingFlight> flights)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            if (flights == null)
                HttpContext.Current?.Cache?.Remove(CacheKeyForUser(szUser));
            else
                HttpContext.Current?.Cache?.Add(CacheKeyForUser(szUser), flights, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, 20, 0), System.Web.Caching.CacheItemPriority.Default, null);
        }

        static private IEnumerable<PendingFlight> CachedFlightsForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser)) 
                throw new ArgumentNullException(nameof(szUser));
            return (IEnumerable<PendingFlight>)HttpContext.Current?.Cache[CacheKeyForUser(szUser)];
        }
        #endregion

        /// <summary>
        /// Get any pending flights for the specified user.  Results are cached.
        /// </summary>
        /// <param name="szUser">username for whom to retrieve flights</param>
        /// <returns>An enumerable of flights</returns>
        static public IEnumerable<PendingFlight> PendingFlightsForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            UserAircraft ua = new UserAircraft(szUser);
            List<PendingFlight> flightsToMap = new List<PendingFlight>();
            HashSet<string> missingAircraft = new HashSet<string>();

            IEnumerable<PendingFlight> cached = CachedFlightsForUser(szUser);
            if (cached != null)
                return cached;

            List<PendingFlight> lst = new List<PendingFlight>();
            DBHelper dbh = new DBHelper("SELECT * FROM pendingflights WHERE username=?user");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("user", szUser); },
                (dr) =>
                {
                    PendingFlight pf = new PendingFlight(dr);
                    // Issue #1241: for performance, delay all aircraft addition to the end
                    Aircraft ac = ua[pf.TailNumDisplay];
                    if (ac == null)
                    {
                        // Normalize the tail
                        pf.TailNumDisplay = Aircraft.NormalizeTail(pf.TailNumDisplay);
                        missingAircraft.Add(pf.TailNumDisplay);
                        flightsToMap.Add(pf);
                    }
                    else
                        pf.AircraftID = ac.AircraftID;
                    lst.Add(pf);
                });

            // Fix up any missing aircraft.  For performance we do a single AircraftTailListQuery, which finds all of them at once.
            IEnumerable<Aircraft> rgac = (missingAircraft.Count == 0) ? (IEnumerable<Aircraft>) Array.Empty<Aircraft>() : Aircraft.AircraftByTailListQuery(missingAircraft);
            foreach (Aircraft ac in rgac)
                ua.FAddAircraftForUser(ac);

            Dictionary<string, int> d = new Dictionary<string, int>();
            IEnumerable<Aircraft> userAircraft = ua.GetAircraftForUser();   // make a single call
            foreach (Aircraft ac in userAircraft)
                d[ac.NormalizedTail] = ac.AircraftID;

            // Now map those missing aircraft
            foreach (PendingFlight pf in flightsToMap)
                pf.AircraftID = d.TryGetValue(pf.TailNumDisplay, out int acid) ? acid : Aircraft.idAircraftUnknown;

            // Sort by date, desc
            lst.Sort((l1, l2) => { return CompareFlights(l1, l2, "Date", System.Web.UI.WebControls.SortDirection.Descending); });
            CacheForUser(szUser, lst);
            return lst;
        }

        /// <summary>
        /// See who has a lot of pending flights and thus might need a nudge to deal with them.
        /// </summary>
        /// <param name="threshold">Threshold for inclusion</param>
        /// <returns>A dictionary with a set of usernames and the count of their pending flights</returns>
        static public IDictionary<string, int> UsersWithLotsOfPendingFlights(int threshold)
        {
            Dictionary<string, int> d = new Dictionary<string, int>();
            DBHelper dbh = new DBHelper("SELECT username, COUNT(id) AS num FROM pendingflights GROUP BY username HAVING num > ?thresh ORDER BY num DESC");
            dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("thresh", threshold); },
                (dr) => { d[(string)dr["username"]] = Convert.ToInt32(dr["num"], System.Globalization.CultureInfo.InvariantCulture); });
            return d;
        }

        static public void DeletePendingFlightsForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));
            DBHelper dbh = new DBHelper("DELETE FROM pendingflights WHERE username=?uname");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("uname", szUser); });
            FlushCacheForUser(szUser); // flush the cache
        }

        /// <summary>
        /// Deletes this pending flight from the pending flights table
        /// </summary>
        public void Delete()
        {
            if (String.IsNullOrEmpty(User))
                throw new InvalidOperationException("User is empty for this object");
            DBHelper dbh = new DBHelper("DELETE FROM pendingflights WHERE id=?idflight");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idflight", PendingID); });
            FlushCacheForUser(User); // flush the cache
        }

        /// <summary>
        /// Saves a pending flight for later
        /// </summary>
        public void Commit()
        {
            if (String.IsNullOrWhiteSpace(User))
                throw new InvalidOperationException("No username specified for pending flight");
            if (String.IsNullOrWhiteSpace(PendingID))
                throw new InvalidOperationException("No unique ID specified for pending flight");

            FlightID = 0;   // don't set a flight ID

            // Make sure that the date-of-flight is indeed just a date
            Date = DateTime.SpecifyKind(Date.Date, DateTimeKind.Unspecified);
            // Since we're setting the pendingID in its own column, no reason to put it in the JSON as well.
            string szPending = PendingID;
            PendingID = null;
            string szJSON = JsonConvert.SerializeObject(this, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling=NullValueHandling.Ignore });
            PendingID = szPending;

            DBHelper dbh = new DBHelper("REPLACE INTO pendingflights SET username=?user, id=?idflight, jsonflight=?json");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("user", User);
                comm.Parameters.AddWithValue("json", szJSON);
                comm.Parameters.AddWithValue("idflight", PendingID);
            });
            FlushCacheForUser(User);
        }

        /// <summary>
        /// Commits the flight and deletes it from pending.
        /// </summary>
        /// <param name="fUpdateFlightData"></param>
        /// <param name="fUpdateSignature"></param>
        /// <returns></returns>
        public override bool FCommit(bool fUpdateFlightData = false, bool fUpdateSignature = false)
        {
            if (FlightID >= 0)
                FlightID = idFlightNew;    // might have been 0 to go up/down the wire.
            bool fSuccess = base.FCommit(fUpdateFlightData, fUpdateSignature);
            if (fSuccess)
                Delete();
            return fSuccess;
        }
        #endregion

        #region Import from oAuth sources
        public static async Task<string> ImportCloudAhoy(string szUser, bool fSandbox, DateTime? dtFrom, DateTime? dtTo)
        {
            if (string.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));
            return await CloudAhoyClient.ImportCloudAhoyFlights(szUser, fSandbox, dtFrom, dtTo).ConfigureAwait(true);
        }

        public static async Task<string> ImportLeon(string szUser, string currentHost, DateTime? dtFrom, DateTime? dtTo)
        {
            if (string.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));
            try
            {
                Profile pf = Profile.GetUser(szUser);
                IAuthorizationState authstate = pf.GetPreferenceForKey<AuthorizationState>(LeonClient.TokenPrefKey);
                string leonSubDomain = pf.GetPreferenceForKey<string>(LeonClient.SubDomainPrefKey);
                LeonClient c = new LeonClient(authstate, leonSubDomain, LeonClient.UseSandbox(currentHost));
                bool fNeedsRefresh = !c.CheckAccessToken();

                await c.ImportFlights(szUser, dtFrom, dtTo);
                if (fNeedsRefresh)
                    pf.SetPreferenceForKey(LeonClient.TokenPrefKey, c.AuthState, c.AuthState == null);

                return string.Empty;    // no issues!
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                return ex.Message;
            }
        }

        public static async Task<string> ImportRosterBuster(string szUser, string currentHost, DateTime? dtFrom, DateTime? dtTo)
        {
            if (string.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            Profile pf = Profile.GetUser(szUser);
            IAuthorizationState authstate = pf.GetPreferenceForKey<AuthorizationState>(RosterBusterClient.TokenPrefKey);

            RosterBusterClient rbc = new RosterBusterClient(authstate, currentHost);

            if (!rbc.CheckAccessToken())
            {
                try
                {
                    IAuthorizationState newAuth = await rbc.RefreshToken();
                    pf.SetPreferenceForKey(RosterBusterClient.TokenPrefKey, newAuth);
                }
                catch (UnauthorizedAccessException)
                {
                    pf.SetPreferenceForKey(RosterBusterClient.TokenPrefKey, null, true);
                    return Branding.ReBrand(Resources.LogbookEntry.RosterBusterRefreshFailed);
                }
            }

            try
            {
                bool _ = await rbc.GetFlights(szUser, dtFrom, dtTo);
                return string.Empty;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                return HttpUtility.HtmlEncode(ex.Message + (ex.InnerException == null ? string.Empty : ex.InnerException.Message));
            }
        }

        public static async Task<string> ImportFlightCrewView(string szUser, DateTime? dtFrom)
        {
            if (string.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));
            Profile pf = Profile.GetUser(szUser);
            if (!pf.PreferenceExists(FlightCrewViewClient.AccessTokenPrefKey))
                throw new UnauthorizedAccessException();

            try
            {
                IEnumerable<PendingFlight> _ = (await (await FlightCrewViewClient.RefreshedClientForUser(szUser)).FlightsFromDate(szUser, dtFrom ?? DateTime.MinValue));
                return string.Empty;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                return ex.Message;
            }
        }
        #endregion
    }
}