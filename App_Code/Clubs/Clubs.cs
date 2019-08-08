using MyFlightbook.Airports;
using MyFlightbook.FlightCurrency;
using MyFlightbook.Schedule;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2014-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Clubs
{
    /// <summary>
    /// A flying club
    /// </summary>
    [Serializable]
    public class Club
    {
        private const UInt32 policyFlagRestrictApptEditing = 0x00000001;
        private const UInt32 policyFlagPrivateClub = 0x00000002;
        private const UInt32 policyFlagPrefixOwnerName = 0x00000004;
        private const UInt32 policyMaskDeleteNotification = 0x00000018;
        private const int policyMaskDeleteShift = 3; // shift above 3 bits to get the policy mask for deletion
        private const UInt32 policyMaskDoubleBooking = 0x00000060;  // who can double-book resources?
        private const int policyMaskDoubleBookShift = 5;             // shift above 5 bits to get the policy mask for double booking
        private const UInt32 policyMaskAddModifyNotification = 0x00000180;
        private const int policyMaskAddModifyNotificationShift = 7; // Shift above 7 bits to get the policy mask for add/modify notification

        public enum DeleteNoficiationPolicy { None = 0x00, Admins = 0x01, WholeClub = 0x02 }
        public enum AddModifyNotificationPolicy { None = 0x00, Admins = 0x01, WholeClub = 0x02 }
        public enum DoubleBookPolicy { None = 0x00, Admins=0x01, WholeClub = 0x02 }

        public const int TrialPeriod = 31;  // 30 days, with some buffer.

        /// <summary>
        /// OK = fine
        /// Promotional = 30 day trial window
        /// Expired = inactive due to trial period ending.  Like inactive, but paying will re-activate.
        /// Inactive = inactive, period, end of story (i.e., paying doesn't reactivate it)
        /// </summary>
        public enum ClubStatus { None, OK = None, Promotional, Expired, Inactive }

        #region properties
        /// <summary>
        /// ID for the club
        /// </summary>
        public int ID {get; set;}

        /// <summary>
        /// Is this a new club?
        /// </summary>
        public bool IsNew { get { return ID == ClubIDNew;}}

        /// <summary>
        /// Username of the user who created the club
        /// </summary>
        public string Creator {get; set;}

        /// <summary>
        /// Date/time that the club was created
        /// </summary>
        public DateTime CreationDate {get; set;}

        /// <summary>
        /// Nullable date for when the club goes inactive.
        /// </summary>
        public DateTime? ExpirationDate
        {
            get
            {
                DateTime? dt = null;
                if (Status == ClubStatus.Promotional)
                    dt = new DateTime(CreationDate.Year, CreationDate.Month, CreationDate.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(TrialPeriod);
                return dt;
            }
        }

        /// <summary>
        /// Status of the club - active, inactive, or probationary.
        /// </summary>
        public ClubStatus Status { get; set; }

        public static bool StatusIsInactive(ClubStatus status)
        {
            return status == ClubStatus.Inactive || status == ClubStatus.Expired;
        }

        public static bool StatusIsActive(ClubStatus status)
        {
            return status == ClubStatus.OK || status == ClubStatus.Promotional;
        }

        /// <summary>
        /// Does the given status allow for reading of schedule, viewing of details?
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool StatusCanRead(ClubStatus status)
        {
            return status != ClubStatus.Inactive;   // can read for expired, not for Inactive
        }

        /// <summary>
        /// Does the given status allow for writing to the schedule, editing of details?
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool StatusCanWrite(ClubStatus status)
        {
            return StatusIsActive(status); 
        }

        /// <summary>
        /// Does the given status allow for searching/display of the club?
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool StatusCanFind(ClubStatus status)
        {
            return StatusIsActive(status);
        }

        public bool CanRead
        {
            get { return StatusCanRead(Status); }
        }

        public bool CanWrite
        {
            get { return StatusCanWrite(Status); }
        }

        public bool CanFind
        {
            get { return StatusCanFind(Status); }
        }

        /// <summary>
        /// Name of the club
        /// </summary>
        public string Name {get; set;}

        /// <summary>
        /// Description of the club
        /// </summary>
        public string Description {get; set;}

        /// <summary>
        /// URL for the club
        /// </summary>
        public string URL {get; set;}

        /// <summary>
        /// Returns a fixed URL (includes HTTP if needed)
        /// </summary>
        public string Link
        {
            get
            {
                return (URL.StartsWith("http", StringComparison.CurrentCultureIgnoreCase)) ? URL : "http://" + URL;
            }
        }

        /// <summary>
        /// City where the club is located
        /// </summary>
        public string City {get; set;}

        /// <summary>
        /// State or province
        /// </summary>
        public string StateProvince {get; set;}

        /// <summary>
        /// Country
        /// </summary>
        public string Country {get; set;}

        /// <summary>
        /// Display string for location
        /// </summary>
        public string LocationString
        {
            get
            {
                string szCityProvince = String.Format(CultureInfo.InvariantCulture, Resources.LocalizedText.LocalizedJoinWithSpace, City, StateProvince).Trim();
                return String.IsNullOrEmpty(szCityProvince) ? Country.Trim() : String.IsNullOrEmpty(Country) ? szCityProvince : String.Format(CultureInfo.InvariantCulture, "{0}, {1}", szCityProvince, Country);
            }
        }

        /// <summary>
        /// Link to the details page for the club
        /// </summary>
        public string EditLink
        {
            get { return VirtualPathUtility.ToAbsolute("~/Member/ClubDetails.aspx/" + ID); }
        }

        /// <summary>
        /// True if there is contact information
        /// </summary>
        public bool HasContactInfo
        {
            get { return !String.IsNullOrEmpty(ContactPhone + City); }
        }

        /// <summary>
        /// Contact phone number
        /// </summary>
        public string ContactPhone {get; set;}

        /// <summary>
        /// Home airport
        /// </summary>
        public string HomeAirportCode {get; set;}

        /// <summary>
        /// The club's home airport (can be null)
        /// </summary>
        public airport HomeAirport { get; set; }

        /// <summary>
        /// Timezone information for the home airport
        /// </summary>
        public TimeZoneInfo TimeZone { get; set; }

        /// <summary>
        /// Most recent error
        /// </summary>
        public string LastError {get; set;}

        private List<ClubAircraft> m_clubAircraft = null;
        private List<ClubMember> m_clubMembers = null;

        /// <summary>
        /// The aircraft for the club.  This is lazy-loaded (on first access) to reduce DB hits.
        /// </summary>
        public IEnumerable<ClubAircraft> MemberAircraft
        {
            get { return m_clubAircraft ?? (m_clubAircraft = ClubAircraft.AircraftForClub(ID)); }
        }

        /// <summary>
        /// Set the member aircraft list to null to force a reload
        /// </summary>
        public void InvalidateMemberAircraft()
        {
            m_clubAircraft = null;
        }

        /// <summary>
        /// Returns the members in a generic list.  Wrap in a collection before returning.
        /// </summary>
        private List<ClubMember> MembersAsList
        {
            get { return m_clubMembers ?? (m_clubMembers = ClubMember.MembersForClub(ID)); }
        }

        /// <summary>
        /// The members for the club.  This is lazy-loaded (on first access) to reduce DB hits
        /// </summary>
        public IEnumerable<ClubMember> Members
        {
            get { return MembersAsList; }
        }

        /// <summary>
        /// Set the member list to null to force a reload.
        /// </summary>
        public void InvalidateMembers()
        {
            m_clubMembers = null;
        }

        /// <summary>
        /// Admins for the club
        /// </summary>
        private IEnumerable<ClubMember> Admins
        {
            get { return MembersAsList.FindAll(cm => cm.RoleInClub != ClubMember.ClubMemberRole.Member); }
        }

        #region Policy
        /// <summary>
        /// Bit flags for the policy
        /// </summary>
        protected UInt32 Policy { get; set; }

        /// <summary>
        /// True if all members can edit all appointments
        /// </summary>
        public bool RestrictEditingToOwnersAndAdmins
        {
            get { return (Policy & policyFlagRestrictApptEditing) == policyFlagRestrictApptEditing; }
            set { Policy = value ? (Policy | policyFlagRestrictApptEditing) : (Policy & ~policyFlagRestrictApptEditing); }
        }

        /// <summary>
        /// True if the club is private (not shown in search results
        /// </summary>
        public bool IsPrivate
        {
            get { return (Policy & policyFlagPrivateClub) == policyFlagPrivateClub; }
            set { Policy = value ? (Policy | policyFlagPrivateClub) : (Policy & ~policyFlagPrivateClub); }
        }

        /// <summary>
        /// True if all members can edit all appointments
        /// </summary>
        public bool PrependsScheduleWithOwnerName
        {
            get { return (Policy & policyFlagPrefixOwnerName) == policyFlagPrefixOwnerName; }
            set { Policy = value ? (Policy | policyFlagPrefixOwnerName) : (Policy & ~policyFlagPrefixOwnerName); }
        }

        /// <summary>
        /// Policy for who to notify on a deletion.  ALWAYS notify the owner if they didn't make the change
        /// </summary>
        public DeleteNoficiationPolicy DeleteNotifications
        {
            get { return (DeleteNoficiationPolicy) ((Policy & policyMaskDeleteNotification) >> policyMaskDeleteShift); }
            set { Policy = (Policy & ~policyMaskDeleteNotification) | (((UInt32)value) << policyMaskDeleteShift); }
        }

        /// <summary>
        /// What is the double-booking policy?  Is it allowed at all, only by admins, or by anyone?
        /// </summary>
        public DoubleBookPolicy DoubleBookRoleRestriction
        {
            get { return (DoubleBookPolicy)((Policy & policyMaskDoubleBooking) >> policyMaskDoubleBookShift); }
            set { Policy = (Policy & ~policyMaskDoubleBooking) | (((UInt32)value) << policyMaskDoubleBookShift); }
        }

        /// <summary>
        /// Policy for notification on add/modify.
        /// </summary>
        public AddModifyNotificationPolicy AddModifyNotifications
        {
            get { return (AddModifyNotificationPolicy)((Policy & policyMaskAddModifyNotification) >> policyMaskAddModifyNotificationShift); }
            set { Policy = (Policy & ~policyMaskAddModifyNotification) | (((UInt32)value) << policyMaskAddModifyNotificationShift); }
        }
        #endregion

        #region Caching
        private string CacheKey
        {
            get { return CacheKeyForID(ID); }
        }

        private static string CacheKeyForID(int id)
        {
            return "CachedClub" + id.ToString(CultureInfo.InvariantCulture);
        }
        #endregion
        #endregion

        #region initialization
        public const int ClubIDNew = -1;

	    public Club()
	    {
            ID = ClubIDNew;
            Creator = Name = Description = URL = City = StateProvince = Country = ContactPhone = HomeAirportCode = string.Empty;
            CreationDate = DateTime.MinValue;
            Status = ClubStatus.OK;
            LastError = string.Empty;
            TimeZone = null;
            Policy = 0x0;
        }

        protected Club(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException("dr");

            ID = Convert.ToInt32(dr["idclub"], CultureInfo.InvariantCulture);
            Creator = dr["creator"].ToString();
            CreationDate = Convert.ToDateTime(dr["creationDate"], CultureInfo.InvariantCulture);
            Status = (ClubStatus)Convert.ToInt32(dr["clubStatus"], CultureInfo.InvariantCulture);
            Name = dr["clubname"].ToString();
            Description = util.ReadNullableString(dr, "clubdesc");
            URL = util.ReadNullableString(dr, "clubURL");
            City = util.ReadNullableString(dr, "clubcity");
            StateProvince = util.ReadNullableString(dr, "clubStateProvince");
            Country = util.ReadNullableString(dr, "clubcountry");
            ContactPhone = util.ReadNullableString(dr, "clubcontactPhone");
            HomeAirportCode = util.ReadNullableString(dr, "clubHomeAirport");
            string szTZId = (string)dr["clubTimeZoneID"];
            int szTZOffsetDefault = Convert.ToInt32(dr["clubTimeZoneOffset"], CultureInfo.InvariantCulture);
            TimeZone = ScheduledEvent.TimeZoneForIdOrOffset(szTZId, szTZOffsetDefault);
            LastError = string.Empty;
            HomeAirport = (dr["FacilityName"] == null || dr["FacilityName"] == DBNull.Value) ? new airport() : new airport(dr);
            Policy = Convert.ToUInt32(dr["policyFlags"], CultureInfo.InvariantCulture);

            // Check if this has expired; if so, switch it to expired
            if (Status == ClubStatus.Promotional && ExpirationDate.HasValue && ExpirationDate.Value.CompareTo(DateTime.Now) < 0)
                ChangeStatus(ClubStatus.Expired);
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} ({1})", Name, HomeAirport.Code);
        }
        #endregion

        #region Database
        public bool IsValid()
        {
            LastError = string.Empty;
            bool fResult = true;
            try
            {
                if (!CanWrite)
                    throw new MyFlightbookValidationException(Branding.ReBrand(Status == ClubStatus.Expired ? Resources.Club.errClubPromoExpired : Resources.Club.errClubInactive));
                if (String.IsNullOrEmpty(Name))
                    throw new MyFlightbookValidationException(Resources.Club.errNameRequired);
                if (String.IsNullOrEmpty(Creator))
                    throw new MyFlightbookValidationException("No creator/owner specified for club - how did that happen?");
            }
            catch (MyFlightbookValidationException ex)
            {
                fResult = false;
                LastError = ex.Message;
            }
            return fResult;
        }

        public bool FCommit()
        {
            bool fResult = false;
            LastError = string.Empty;
            if (IsValid())
            {
                const string szSetCore = @"creator=?szcreator, clubname=?name, clubStatus=?status, clubdesc=?description, clubURL=?url, clubCity=?city, 
                            clubStateProvince=?state, clubCountry=?country, clubContactPhone=?contact, clubHomeAirport=?airport, clubTimeZoneID=?tzID, clubTimeZoneOffset=?tzOffset, policyFlags=?pflag";

                const string szQInsertT = "INSERT INTO clubs SET creationDate=Now(), {0}";
                const string szQUpdateT = "UPDATE clubs SET {0} WHERE idclub=?idClub";

                bool fNew = IsNew;

                DBHelper dbh = new DBHelper(String.Format(IsNew ? szQInsertT : szQUpdateT, szSetCore));

                fResult = dbh.DoNonQuery((comm) =>
                    {
                        comm.Parameters.AddWithValue("szcreator", Creator);
                        comm.Parameters.AddWithValue("name", Name.LimitTo(90));
                        comm.Parameters.AddWithValue("description", Description.LimitTo(40000));
                        comm.Parameters.AddWithValue("status", (int)Status);
                        comm.Parameters.AddWithValue("url", URL.LimitTo(255));
                        comm.Parameters.AddWithValue("city", City.LimitTo(45));
                        comm.Parameters.AddWithValue("state", StateProvince.LimitTo(45));
                        comm.Parameters.AddWithValue("country", Country.LimitTo(45));
                        comm.Parameters.AddWithValue("contact", ContactPhone.LimitTo(45));
                        comm.Parameters.AddWithValue("airport", HomeAirportCode.LimitTo(45).ToUpper());
                        comm.Parameters.AddWithValue("idClub", ID);
                        comm.Parameters.AddWithValue("tzID", TimeZone.Id);
                        comm.Parameters.AddWithValue("tzOffset", (int) TimeZone.BaseUtcOffset.TotalMinutes);
                        comm.Parameters.AddWithValue("pflag", Policy);
                    });
                if (fResult)
                {
                    if (fNew)
                    {
                        ID = dbh.LastInsertedRowId;
                        CreationDate = DateTime.Now;

                        // if we're adding a new club, add the creator as its first member.
                        ClubMember cm = new ClubMember(ID, Creator, ClubMember.ClubMemberRole.Owner);
                        if (!(fResult = cm.FCommitClubMembership()))
                            LastError = cm.LastError;
                    }
                    CacheClub(this, this.ID);
                }
                else
                    LastError = dbh.LastError;
            }
            return fResult;
        }

        public void ChangeStatus(ClubStatus s)
        {
            Status = s;
            DBHelper dbh = new DBHelper("UPDATE clubs SET clubStatus=?status WHERE idclub=?idClub");
            dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("status", (int)Status);
                    comm.Parameters.AddWithValue("idClub", ID);
                });
            LastError = dbh.LastError;
        }

        private const string szSQLGetClubsBase = @"SELECT *, 0 AS dist, '' AS FriendlyName 
        FROM clubs c 
            LEFT JOIN airports ap ON (c.clubHomeAirport=ap.AirportID OR (LENGTH(c.clubHomeAirport)=4 AND c.clubHomeAirport= CONCAT('K', ap.airportID))) AND ap.Type IN ('A', 'H', 'S')
            {0}
        GROUP BY c.idclub, c.clubHomeAirport";

        private static string PrivateInactiveRestriction()
        {
            return String.Format(CultureInfo.InvariantCulture, "(c.policyFlags & {0}) = 0 AND clubStatus NOT IN ({1}, {2})", policyFlagPrivateClub, (int) ClubStatus.Inactive, (int) ClubStatus.Expired);
        }

        private static string QueryStringWithRestriction(string szRestriction)
        {
            return string.Format(CultureInfo.InvariantCulture, szSQLGetClubsBase, String.IsNullOrEmpty(szRestriction) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "WHERE {0} ", szRestriction));
        }

        /// <summary>
        /// Load a specific club by ID.  Will use the cache unless otherwise requested
        /// </summary>
        /// <param name="id">the id of the club</param>
        /// <param name="fNoCache">True to hit the database and ignore the cache</param>
        /// <returns>The club</returns>
        public static Club ClubWithID(int id, bool fNoCache = false)
        {
            Club c = null;
            string szKey = CacheKeyForID(id);
            if (!fNoCache)
            {
                c = (Club)HttpRuntime.Cache[szKey];
                if (c != null)
                    return c;
            }
            if (c == null)
            {
                DBHelper dbh = new DBHelper(QueryStringWithRestriction("idclub=?id"));
                dbh.ReadRow((comm) => { comm.Parameters.AddWithValue("id", id); }, (dr) => { c = new Club(dr); });
            }

            CacheClub(c, id);
            return c;
        }

        /// <summary>
        /// Caches the specified club.  If club is null, removes it from the cache
        /// </summary>
        /// <param name="c">The club to cache (may be null)</param>
        /// <param name="idClub">The club's ID</param>
        private static void CacheClub(Club c, int idClub)
        {
            string szKey = CacheKeyForID(idClub);
            HttpRuntime.Cache.Remove(szKey);
            if (c != null)
                HttpRuntime.Cache.Add(c.CacheKey, c, null, DateTime.Now.AddMinutes(30), System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);
        }

        /// <summary>
        /// Clears the specified club from the cache
        /// </summary>
        /// <param name="id"></param>
        public static void ClearCachedClub(int id)
        {
            CacheClub(null, id);
        }

        /// <summary>
        /// Loads all public, active clubs
        /// </summary>
        /// <param name="fIncludePrivateAndInactive">Admin override to include private or inactive clubs</param>
        /// <returns>A list of clubs</returns>
        public static IEnumerable<Club> AllClubs(bool fIncludePrivateAndInactive)
        {
            List<Club> lst = new List<Club>();
            DBHelper dbh = new DBHelper(QueryStringWithRestriction(fIncludePrivateAndInactive ? string.Empty : PrivateInactiveRestriction()));
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new Club(dr)); });
            lst.Sort((c1, c2) => { return String.Compare(c1.Name, c2.Name, StringComparison.CurrentCultureIgnoreCase); });
            return lst;
        }

        /// <summary>
        /// Loads all clubs created by a given user
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <returns>A list of clubs</returns>
        public static IEnumerable<Club> ClubsCreatedByUser(string szUser)
        {
            List<Club> lst = new List<Club>();
            DBHelper dbh = new DBHelper(QueryStringWithRestriction("creator=?user"));
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("user", szUser); }, (dr) => { lst.Add(new Club(dr)); });
            return lst;
        }

        /// <summary>
        /// Return all clubs for the user, regardless of role with the club.
        /// </summary>
        /// <param name="szUser">Username</param>
        /// <returns>The lsit of matching clubes</returns>
        public static IEnumerable<Club> AllClubsForUser(string szUser)
        {
            List<Club> lst = new List<Club>();
            DBHelper dbh = new DBHelper("SELECT c.*, ap.*, 0 AS dist, '' AS FriendlyName FROM clubs c LEFT JOIN airports ap ON c.clubHomeAirport=ap.AirportID AND ap.Type IN ('A', 'H', 'S') LEFT JOIN clubmembers cm ON cm.idclub=c.idclub WHERE (cm.username IS NULL AND c.creator=?user) OR cm.username=?user GROUP BY c.idclub");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("user", szUser); }, (dr) => { lst.Add(new Club(dr)); });
            return lst;
        }

        public static IEnumerable<Club> ClubsForAircraft(int idaircraft, string szUser = null)
        {
            List<Club> lst = new List<Club>();
            string szQ = String.Format(@"SELECT c.*, ap.*, 0 AS dist, '' AS FriendlyName 
                FROM clubaircraft ca 
                INNER JOIN clubs c ON ca.idclub=c.idclub 
                {0}
                LEFT JOIN airports ap ON c.clubHomeAirport=ap.AirportID AND ap.Type IN ('A', 'H', 'S') 
                WHERE ca.idaircraft=?idaircraft {1}
                GROUP BY c.idclub", szUser == null ? string.Empty : "INNER JOIN clubmembers cm ON ca.idclub=cm.idclub", szUser == null ? string.Empty : "AND cm.username=?user");
            DBHelper dbh = new DBHelper(szQ);
            dbh.ReadRows((comm) => { 
                comm.Parameters.AddWithValue("idaircraft", idaircraft);
                comm.Parameters.AddWithValue("user", szUser);
            }, (dr) => { lst.Add(new Club(dr)); });
            return lst;
        }

        /// <summary>
        /// Returns the clubs near the specified airport
        /// </summary>
        /// <param name="szCode">Airport code</param>
        /// <param name="fIncludePrivateAndInactive">Admin flag to include private or inactive clubs</param>
        /// <returns>The airports within about 100nm or so of the code.</returns>
        public static IEnumerable<Club> ClubsNearAirport(string szCode, bool fIncludePrivateAndInactive)
        {
            List<Club> lst = new List<Club>();
            string szQTemplate = @"SELECT c.*, ap.*, (3440.06479*acos(cos(radians(ap.latitude))*cos(radians(ap2.latitude))*cos(radians(ap.longitude)-radians(ap2.longitude))+sin(radians(ap.latitude))*sin(radians(ap2.latitude)))) AS dist, '' AS FriendlyName 
                FROM clubs c
                LEFT JOIN airports ap ON c.clubHomeAirport=ap.AirportID AND ap.Type IN ('A', 'H', 'S') 
                LEFT JOIN airports ap2 ON ABS(ap2.latitude - ap.latitude) <= 2 AND ABS(ap2.longitude - ap.longitude) <= 2
                WHERE ap2.airportid=?code AND ap2.type IN ('A', 'H', 'S') {0}
                GROUP BY c.idclub
                ORDER BY dist ASC";
            string szQ = String.Format(CultureInfo.InvariantCulture, szQTemplate, fIncludePrivateAndInactive ? string.Empty : " AND " + PrivateInactiveRestriction());
            DBHelper dbh = new DBHelper(szQ);
            dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("code", szCode); }, 
                (dr) => {
                    Club c = new Club(dr);
                    object o = dr["dist"];
                    c.HomeAirport.DistanceFromPosition = o == DBNull.Value ? 0.0 : Convert.ToDouble(o, CultureInfo.InvariantCulture);
                    lst.Add(c);
                });
            return lst;
        }

        public bool FDelete()
        {
            CacheClub(null, this.ID);
            DBHelper dbh = new DBHelper("DELETE FROM clubs WHERE idclub=?id");
            return dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("id", ID); });
        }
        #endregion

        #region Member Management
        /// <summary>
        /// Determines if the specified user is a member of the club.  Does NOT check creator
        /// </summary>
        /// <param name="szUser">User to be tested</param>
        /// <returns>The member, null if not found</returns>
        public ClubMember GetMember(string szUser)
        {
            return Members.FirstOrDefault(cm => cm.UserName.CompareTo(szUser) == 0);
        }

        /// <summary>
        /// Check if the specified user is an admin of the club.
        /// </summary>
        /// <param name="szUser">Username</param>
        /// <returns>True if they are in the admin list.</returns>
        public bool HasAdmin(string szUser)
        {
            return Admins.FirstOrDefault(cm => cm.UserName.CompareTo(szUser) == 0) != null;
        }

        /// <summary>
        /// Check if the specified user is a member of the club.
        /// </summary>
        /// <param name="szUser">Username</param>
        /// <returns>True if they are in the admin list.</returns>
        public bool HasMember(string szUser)
        {
            return MembersAsList.Exists(cm => cm.UserName.CompareTo(szUser) == 0);
        }

        public void NotifyAdd(ScheduledEvent s, string addingUser)
        {
            Profile pfCreator = Profile.GetUser(addingUser);

            List<Profile> lst = new List<Profile>();
            switch (AddModifyNotifications)
            {
                case AddModifyNotificationPolicy.None:
                    break;
                case AddModifyNotificationPolicy.Admins:
                    lst.AddRange(Admins);
                    break;
                case AddModifyNotificationPolicy.WholeClub:
                    lst.AddRange(Members);
                    break;
            }

            s.LocalTimeZone = TimeZone;    // ensure that we're using the right timezone
            if (s.ResourceAircraft == null)
                s.ResourceAircraft = MemberAircraft.FirstOrDefault(ca => ca.AircraftID.ToString().CompareTo(s.ResourceID) == 0);

            string szBody = Resources.Schedule.ResourceBooked
                .Replace("<% Creator %>", pfCreator.UserFullName)
                .Replace("<% Resource %>", s.ResourceAircraft == null ? string.Empty : s.ResourceAircraft.DisplayTailnumber)
                .Replace("<% ScheduleDetail %>", String.Format("{0:d} {1:t} ({2}){3}", s.LocalStart, s.LocalStart, s.DurationDisplay, String.IsNullOrEmpty(s.Body) ? string.Empty : String.Format(" - {0}", s.Body)));
            string szSubject = Branding.ReBrand(Resources.Schedule.CreateNotificationSubject);

            foreach (Profile pf in lst)
                util.NotifyUser(szSubject, szBody, new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);
        }

        public void NotifyOfChange(ScheduledEvent sOriginal, ScheduledEvent sNew, string changingUser)
        {
            // No notification if no change was actually made
            if (sOriginal == sNew)
                return;

            Profile pfOwner = Profile.GetUser(sOriginal.OwningUser);

            List<Profile> lst = new List<Profile>();
            switch (AddModifyNotifications)
            {
                case AddModifyNotificationPolicy.None:
                    // ALWAYS notify the owner if you are changing their appointment, regardless of notification policy
                    if (sOriginal.OwningUser.CompareOrdinalIgnoreCase(changingUser) != 0)
                        lst.Add(pfOwner);
                    break;
                case AddModifyNotificationPolicy.Admins:
                    lst.AddRange(Admins);
                    break;
                case AddModifyNotificationPolicy.WholeClub:
                    lst.AddRange(Members);
                    break;
            }

            // check if no notifications required
            if (lst.Count == 0)
                return;

            sOriginal.LocalTimeZone = TimeZone;    // ensure that we're using the right timezone
            if (sOriginal.ResourceAircraft == null)
                sOriginal.ResourceAircraft = MemberAircraft.FirstOrDefault(ca => ca.AircraftID.ToString().CompareTo(sOriginal.ResourceID) == 0);

            Profile pfChanging = Profile.GetUser(changingUser);

            string szBody = Resources.Schedule.ResourceChangedByOther
                .Replace("<% Deleter %>", pfChanging.UserFullName)
                .Replace("<% Resource %>", sOriginal.ResourceAircraft == null ? string.Empty : sOriginal.ResourceAircraft.DisplayTailnumber)
                .Replace("<% ScheduleDetail %>", String.Format("{0:d} {1:t} ({2}){3}", sOriginal.LocalStart, sOriginal.LocalStart, sOriginal.DurationDisplay, String.IsNullOrEmpty(sOriginal.Body) ? string.Empty : String.Format(" - {0}", sOriginal.Body)))
                .Replace("<% ScheduleDetailNew %>", String.Format("{0:d} {1:t} ({2}){3}", sNew.LocalStart, sNew.LocalStart, sNew.DurationDisplay, String.IsNullOrEmpty(sNew.Body) ? string.Empty : String.Format(" - {0}", sNew.Body)))
                .Replace("<% TimeStamp %>", string.Empty /* DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) */);
            string szSubject = Branding.ReBrand(Resources.Schedule.ChangeNotificationSubject);

            foreach (Profile pf in lst)
                util.NotifyUser(szSubject, szBody, new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);
        }

        public void NotifyOfDelete(ScheduledEvent s, string deletingUser)
        {
            List<Profile> lst = new List<Profile>();
            // Add people to notify per club policy
            switch (DeleteNotifications)
            {
                case DeleteNoficiationPolicy.None:
                    break;
                case DeleteNoficiationPolicy.WholeClub:
                    lst.AddRange(Members);
                    break;
                case DeleteNoficiationPolicy.Admins:
                    lst.AddRange(Admins);
                    break;
            }

            // And add the owner if they didn't delete it themselves - but only if not already in list above
            if (s.OwningUser.CompareTo(deletingUser) != 0 && !lst.Exists(pf => pf.UserName.CompareTo(s.OwningUser) == 0))
                lst.Add(Profile.GetUser(s.OwningUser));

            if (lst.Count > 0)
            {
                s.LocalTimeZone = TimeZone;    // ensure that we're using the right timezone
                if (s.ResourceAircraft == null)
                    s.ResourceAircraft = MemberAircraft.FirstOrDefault(ca => ca.AircraftID.ToString().CompareTo(s.ResourceID) == 0);

                Profile pfDeleting = Profile.GetUser(deletingUser);
                string szBody = Resources.Schedule.AppointmentDeleted
                    .Replace("<% Deleter %>", pfDeleting.UserFullName)
                    .Replace("<% Resource %>", s.ResourceAircraft == null ? string.Empty : s.ResourceAircraft.DisplayTailnumber)
                    .Replace("<% ScheduleDetail %>", String.Format("{0:d} {1:t} ({2}){3}", s.LocalStart, s.LocalStart, s.DurationDisplay, String.IsNullOrEmpty(s.Body) ? string.Empty : String.Format(" - {0}", s.Body)))
                    .Replace("<% TimeStamp %>", string.Empty /* DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) */);
                string szSubject = Branding.ReBrand(Resources.Schedule.DeleteNotificationSubject);

                lst.ForEach((pf) => { util.NotifyUser(szSubject, szBody, new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false); });
            }
        }
        #endregion

        #region Upcoming Appointments
        public enum SummaryMode { Club, Aircraft, User }

        public IEnumerable<ScheduledEvent> GetUpcomingEvents(int limit, string resource=null, string owner=null)
        {
            List<ScheduledEvent> lst = ScheduledEvent.UpcomingAppointments(ID, TimeZone, limit, resource, owner);

            // Fix up the aircraft, owner names
            lst.ForEach((se) =>
                {
                    int idAircraft = 0;
                    if (int.TryParse(se.ResourceID, out idAircraft))
                        se.ResourceAircraft = MemberAircraft.FirstOrDefault(ca => ca.AircraftID == idAircraft);
                    se.OwnerProfile = Members.FirstOrDefault(cm => String.Compare(cm.UserName, se.OwningUser, StringComparison.Ordinal) == 0);
                });
            return lst;
        }
        #endregion
    }

    [Serializable]
    public class ClubMember : Profile
    {
        public enum ClubMemberRole {Member, Admin, Owner}

        #region properties
        /// <summary>
        /// The ID of the associated club
        /// </summary>
        public int ClubID { get; set; }

        /// <summary>
        /// The member's role in the club.
        /// </summary>
        public ClubMemberRole RoleInClub { get; set; }

        /// <summary>
        /// Can this person manage the group?
        /// </summary>
        public bool IsManager
        {
            get { return RoleInClub == ClubMemberRole.Admin || RoleInClub == ClubMemberRole.Owner; }
        }

        /// <summary>
        /// Human-readable role in club
        /// </summary>
        public string DisplayRoleInClub
        {
            get
            {
                switch (RoleInClub)
                {
                    case ClubMemberRole.Admin:
                        return Resources.Club.RoleManager;
                    case ClubMemberRole.Owner:
                        return Resources.Club.RoleOwner;
                    default:
                    case ClubMemberRole.Member:
                        return Resources.Club.RoleMember;
                }
            }
        }

        /// <summary>
        /// Date that the user joined the club
        /// </summary>
        public DateTime JoinedDate { get; set; }

        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }
        #endregion

        #region initialization
        public ClubMember() : base()
        {
            ClubID = Club.ClubIDNew;
            RoleInClub = ClubMemberRole.Member;
            LastError = string.Empty;
            JoinedDate = DateTime.MinValue;
        }

        public ClubMember(int idclub, string szUser, ClubMemberRole role) : this()
        {
            LoadUser(szUser);
            RoleInClub = role;
            ClubID = idclub;
        }

        public ClubMember(MySqlDataReader dr) : base(dr)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            ClubID = Convert.ToInt32(dr["idclub"], CultureInfo.InvariantCulture);
            RoleInClub = (ClubMemberRole)Convert.ToInt32(dr["role"], CultureInfo.InvariantCulture);
            JoinedDate = Convert.ToDateTime(dr["joindate"], CultureInfo.InvariantCulture);
            LastError = string.Empty;
        }
        #endregion

        #region Database
        private bool ValidateChange()
        {
            Club c = Club.ClubWithID(ClubID);
            if (!c.CanWrite)
            {
                LastError = Branding.ReBrand(c.Status == Club.ClubStatus.Expired ? Resources.Club.errClubPromoExpired : Resources.Club.errClubInactive);
                return false;
            }
            return true;
        }

        public bool FCommitClubMembership()
        {
            LastError = string.Empty;

            if (!ValidateChange())
                return false;

            IEnumerable<ClubMember> members = MembersForClub(ClubID);
            ClubMember cmExisting = members.FirstOrDefault<ClubMember>(cm => cm.UserName.CompareCurrentCulture(UserName) == 0);

            // We don't use REPLACE INTO here because doing so loses the original joindate
            DBHelper dbh = new DBHelper(cmExisting == null ? 
                "INSERT INTO clubmembers SET idclub=?id, username=?user, role=?role, joindate=NOW()" :
                "UPDATE clubmembers SET role=?role WHERE idclub=?id AND username=?user");
            bool fResult = dbh.DoNonQuery((comm) => 
            {
                comm.Parameters.AddWithValue("id", ClubID);
                comm.Parameters.AddWithValue("user", UserName);
                comm.Parameters.AddWithValue("role", (int)RoleInClub);
            });
            if (!fResult)
                LastError = dbh.LastError;
            else
                Club.ClearCachedClub(ClubID);
            return fResult;
        }

        public bool FDeleteClubMembership()
        {
            if (!ValidateChange())
                return false;

            return FDeleteUserFromClub(ClubID, UserName);
        }

        private static bool FDeleteUserFromClub(int idClub, string szUser)
        {
            DBHelper dbh = new DBHelper("DELETE FROM clubmembers WHERE idclub=?id AND username=?user");
            bool fResult = dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("id", idClub); comm.Parameters.AddWithValue("user", szUser); });
            if (fResult)
                Club.ClearCachedClub(idClub);
            return fResult;
        }

        public static List<ClubMember> MembersForClub(int idclub)
        {
            DBHelper dbh = new DBHelper(@"SELECT cm.*, u.* 
                        FROM clubmembers cm 
                        INNER JOIN users u ON cm.username=u.username
                        LEFT JOIN usersinroles uir ON (u.Username=uir.Username AND uir.ApplicationName='Logbook')
                        WHERE cm.idclub=?clubid
                        ORDER BY u.Lastname ASC, u.FirstName ASC, u.Username ASC");
            List<ClubMember> lst = new List<ClubMember>();
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("clubid", idclub); },
                (dr) => { lst.Add(new ClubMember(dr)); });
            return lst;
        }

        /// <summary>
        /// Gets the list of all members who are not simply members (owners, admins)
        /// </summary>
        /// <param name="idclub">The club</param>
        /// <returns>The list of relevant members</returns>
        public static IEnumerable<ClubMember> AdminsForClub(int idclub)
        {
            List<ClubMember> lst = MembersForClub(idclub);
            lst.RemoveAll(cm => cm.RoleInClub == ClubMemberRole.Member);
            return lst;
        }

        /// <summary>
        /// Is the user a member of the specified club?
        /// </summary>
        /// <param name="idclub">The club ID</param>
        /// <param name="szuser">The username</param>
        /// <returns>True if they are somewhere in the list</returns>
        public static bool IsMember(int idclub, string szuser)
        {
            return MembersForClub(idclub).Exists(cm => cm.UserName.CompareTo(szuser) == 0);
        }

        /// <summary>
        /// Is the user a member of the specified club?
        /// </summary>
        /// <param name="idclub">The club ID</param>
        /// <param name="szuser">The username</param>
        /// <returns>True if they are in the list with a non-member role</returns>
        public static bool IsAdmin(int idclub, string szuser)
        {
            return AdminsForClub(idclub).FirstOrDefault<ClubMember>(cm => cm.UserName.CompareTo(szuser) == 0) != null;
        }
        #endregion
    }

    [Serializable]
    public class ClubAircraft : Aircraft
    {
        #region properties
        /// <summary>
        /// The ID of the associated club
        /// </summary>
        public int ClubID { get; set; }

        /// <summary>
        /// A description of the plane (e.g., features, rental rates, etc.)
        /// </summary>
        public string ClubDescription { get; set; }

        /// <summary>
        /// High-water mark seen for tachometer or hobbs (as determined by time mode)
        /// </summary>
        public decimal HighWater { get; set; }

        /// <summary>
        /// Highest recorded hobbs on a given flight for this aircraft
        /// NOT SET BY DEFAULT - call RefreshClubAircraftTimes
        /// </summary>
        public decimal HighestRecordedHobbs { get; set; }

        /// <summary>
        /// Highest recorded hobbs on a given flight for this aircraft
        /// NOT SET BY DEFAULT - call RefreshClubAircraftTimes
        /// </summary>
        public decimal HighestRecordedTach { get; set; }
     
        /// <summary>
        /// The last error
        /// </summary>
        public string LastError { get; set; }
        #endregion

        #region initialization
        public ClubAircraft() : base()
        {
            ClubID = Club.ClubIDNew;
            LastError = ClubDescription = string.Empty;
            HighestRecordedHobbs = HighestRecordedTach = HighWater = 0.0M;
        }

        public ClubAircraft(MySqlDataReader dr) : base(dr)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            ClubID = Convert.ToInt32(dr["idclub"], CultureInfo.InvariantCulture);
            ClubDescription = util.ReadNullableString(dr, "description");
            HighWater = Convert.ToDecimal(dr["highWaterMark"], CultureInfo.InvariantCulture);
            HighestRecordedHobbs = HighestRecordedTach = 0.0M;
            LastError = string.Empty;
        }
        #endregion

        #region Database
        private bool ValidateChange()
        {
            Club c = Club.ClubWithID(ClubID);
            if (!c.CanWrite)
            {
                LastError = Branding.ReBrand(c.Status == Club.ClubStatus.Expired ? Resources.Club.errClubPromoExpired : Resources.Club.errClubInactive);
                return false;
            }
            return true;
        }

        public bool FSaveToClub()
        {
            LastError = string.Empty;

            if (!ValidateChange())
                return false;

            DBHelper dbh = new DBHelper("REPLACE INTO clubaircraft SET idclub=?id, idaircraft=?aircraftid, description=?desc, highWaterMark=?hwm");
            bool fResult = dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("id", ClubID);
                comm.Parameters.AddWithValue("aircraftid", AircraftID);
                comm.Parameters.AddWithValue("desc", ClubDescription.LimitTo(40000));
                comm.Parameters.AddWithValue("hwm", HighWater);
            });
            if (!fResult)
                LastError = dbh.LastError;
            else
                Club.ClearCachedClub(ClubID);
            return fResult;
        }

        public bool FDeleteFromClub()
        {
            if (!ValidateChange())
                return false;

            string szError;
            bool fResult = FDeleteAircraftFromClub(ClubID, AircraftID, out szError);
            LastError = szError;
            if (fResult)
                Club.ClearCachedClub(ClubID);
            return fResult;
        }

        private static bool FDeleteAircraftFromClub(int idClub, int idAircraft, out string szErr)
        {
            DBHelper dbh = new DBHelper("DELETE FROM clubaircraft WHERE idclub=?id AND idaircraft=?aircraftid");
            bool fResult = dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("id", idClub); comm.Parameters.AddWithValue("aircraftid", idAircraft); });
            szErr = dbh.LastError;
            return fResult;
        }

        public static List<ClubAircraft> AircraftForClub(int idclub)
        {
            DBHelper dbh = new DBHelper(String.Format(ConfigurationManager.AppSettings["AircraftForUserCore"], "ca.idclub, ca.description, ca.highWaterMark, 0", "''", "''", "''", " INNER JOIN clubaircraft ca ON aircraft.idaircraft=ca.idaircraft WHERE ca.idclub=?clubid "));
            List<ClubAircraft> lst = new List<ClubAircraft>();
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("clubid", idclub); },
                (dr) => { lst.Add(new ClubAircraft(dr)); });
            // Initialize images - at least one - for each aircraft
            lst.ForEach((ca) => { ca.PopulateImages(); });
            return lst;
        }

        /// <summary>
        /// Discovers the high-watermark tach/hobbs recorded for the club aircraft
        /// </summary>
        /// <param name="idclub">The ID of the club</param>
        /// <param name="lst">The aircraft for the club, if already loaded (will be loaded if null)</param>
        public static void RefreshClubAircraftTimes(int idclub, IEnumerable<ClubAircraft> lst)
        {
            if (lst == null)
                lst = AircraftForClub(idclub);

            if (lst.Count() == 0)
                return;

            DBHelper dbh = new DBHelper(@"SELECT ca.idaircraft, MAX(f.hobbsend) AS MaxHobbs, MAX(fp.decvalue) AS MaxTach
FROM clubaircraft ca
INNER JOIN clubs c ON ca.idclub=c.idclub
LEFT JOIN flights f ON ca.idaircraft=f.idaircraft
LEFT JOIN flightproperties fp ON (f.idflight=fp.idflight AND fp.idPropType=96)
WHERE c.idclub=?idClub
GROUP BY idaircraft");

            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("idClub", idclub); },
                (dr) =>
                {
                    ClubAircraft ca = lst.First(ca2 => ca2.AircraftID == Convert.ToInt32(dr["idaircraft"]));
                    if (ca != null)
                    {
                        ca.HighestRecordedHobbs = (Decimal) util.ReadNullableField(dr, "MaxHobbs", 0.0M);
                        ca.HighestRecordedTach = (Decimal)util.ReadNullableField(dr, "MaxTach", 0.0M);
                    }
                });
        }
        #endregion
    }

    public class ClubChangedEventsArgs : EventArgs
    {
        public Club EventClub { get; set; }

        public ClubChangedEventsArgs() : base()
        {
            EventClub = null;
        }

        public ClubChangedEventsArgs(Club c) : base()
        {
            EventClub = c;
        }
    }

    public class ClubInsuranceReportItem
    {
        #region Properties
        /// <summary>
        /// # of flights in the specified month interval
        /// </summary>
        public int FlightsInInterval { get; set; }

        /// <summary>
        /// Most recent flight by the user in a club aircraft
        /// </summary>
        public DateTime? MostRecentFlight { get; set; }

        /// <summary>
        /// Total time for the user - across all aircraft
        /// </summary>
        public decimal TotalTime { get; set; }

        /// <summary>
        /// Complex time for the user - across all aircraft
        /// </summary>
        public decimal ComplexTime { get; set; }

        /// <summary>
        /// High-performance time for the user - across all aircraft
        /// </summary>
        public decimal HighPerformanceTime { get; set; }

        /// <summary>
        /// The user
        /// </summary>
        public Profile User { get; set; }

        /// <summary>
        /// Total by club aircraft, striped by display tail number
        /// </summary>
        public IDictionary<string, decimal> TotalsByClubAircraft { get; private set; }

        /// <summary>
        /// Last flight reviews (including R22/R44 reviews), stryped by label
        /// </summary>
        public IEnumerable<CurrencyStatusItem> PilotStatusItems { get; private set; }
        #endregion

        #region Constructors
        public ClubInsuranceReportItem()
        {
            TotalsByClubAircraft = new Dictionary<string, decimal>();
            PilotStatusItems = new List<CurrencyStatusItem>();
        }

        public ClubInsuranceReportItem(Profile user) : this()
        {
            User = user;
            PilotStatusItems = user.WarningsForUser();
        }
        #endregion

        /// <summary>
        /// Generates an insurance report for the specified club
        /// </summary>
        /// <param name="idClub">The ID of the club</param>
        /// <param name="monthInterval"># of months back to look for flights</param>
        /// <returns></returns>
        public static IEnumerable<ClubInsuranceReportItem> ReportForClub(int idClub, int monthInterval = 6)
        {
            Dictionary<string, ClubInsuranceReportItem> d = new Dictionary<string, ClubInsuranceReportItem>();
            Club c = Club.ClubWithID(idClub);

            // Create one reportitem per member, adding in status items
            foreach (ClubMember cm in c.Members)
                d[cm.UserName] = new ClubInsuranceReportItem(cm);

            // Get the overall totals for each user
            string szQOverview = @"SELECT 
    cm.username,
    SUM(IF(ca.idaircraft IS NOT NULL AND f.date >= ?minDate, 1, 0)) AS flightsInClubAircraft,
    MAX(IF(ca.idaircraft IS NOT NULL, f.date, NULL)) AS latestflight,
    SUM(ROUND(f.TotalFlightTime * 60) / 60) AS totaltime,
    SUM(IF(m.fComplex, ROUND(f.TotalFlightTime * 60) / 60, 0)) AS complextime,
    SUM(IF(m.fHighPerf <> 0 OR (f.date < '1997-08-04' AND m.f200HP <> 0), ROUND(f.TotalFlightTime * 60) / 60, 0)) AS HPTime
FROM
    clubmembers cm
        INNER JOIN
    users u ON u.username = cm.username
        INNER JOIN
    clubs c ON c.idclub = cm.idclub
        LEFT JOIN
    flights f ON f.username = cm.username
        INNER JOIN
    aircraft ac ON f.idaircraft = ac.idaircraft
        INNER JOIN
    models m ON ac.idmodel = m.idmodel
        LEFT JOIN
    clubaircraft ca ON ca.idaircraft = f.idaircraft
        AND ca.idclub = c.idclub
WHERE
    c.idClub = ?clubid
GROUP BY cm.username
ORDER BY username ASC";

            DBHelper dbh = new DBHelper(szQOverview);
            dbh.ReadRows((comm) =>
            {
                comm.Parameters.AddWithValue("clubid", idClub);
                comm.Parameters.AddWithValue("minDate", DateTime.Now.AddMonths(-monthInterval));
            },
                (dr) =>
                {
                    ClubInsuranceReportItem ciri = d[(string)dr["username"]];
                    if (util.ReadNullableField(dr, "latestflight", null) != null)
                        ciri.MostRecentFlight = Convert.ToDateTime(dr["latestflight"], CultureInfo.InvariantCulture);
                    ciri.FlightsInInterval = Convert.ToInt32(dr["flightsInClubAircraft"], CultureInfo.InvariantCulture);
                    ciri.TotalTime = Convert.ToDecimal(dr["totaltime"], CultureInfo.InvariantCulture);
                    ciri.ComplexTime = Convert.ToDecimal(dr["complextime"], CultureInfo.InvariantCulture);
                    ciri.HighPerformanceTime = Convert.ToDecimal(dr["HPTime"], CultureInfo.InvariantCulture);
                });

            List<Aircraft> lstAc = new List<Aircraft>(c.MemberAircraft);

            dbh.CommandText = String.Format(CultureInfo.InvariantCulture, @"SELECT
  cm.username,
  ca.idaircraft,
  sum(round(f.totalflighttime * 60) / 60) as TimeInAircraft
FROM clubmembers cm 
INNER JOIN users u ON u.username=cm.username
INNER JOIN clubs c ON c.idclub=cm.idclub
INNER join flights f ON f.username = cm.username
inner JOIN clubaircraft ca ON ca.idaircraft=f.idaircraft and ca.idclub=c.idclub
WHERE
c.idClub = ?clubid and ca.idaircraft is not null
GROUP BY cm.username, ca.idaircraft
ORDER BY username asc;");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("clubid", idClub); },
                (dr) => {
                    ClubInsuranceReportItem ciri = d[(string)dr["username"]];
                    int idAircraft = Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture);
                    decimal timeInAircraft = Convert.ToDecimal(dr["TimeInAircraft"], CultureInfo.InvariantCulture);
                    Aircraft ac = lstAc.FirstOrDefault(ac2 => ac2.AircraftID == idAircraft);
                    if (ac == null)
                        throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Unknown aircraft {0} for club {1} in club report", idAircraft, idClub));
                    ciri.TotalsByClubAircraft[ac.DisplayTailnumber] = timeInAircraft;
                });

            List<ClubInsuranceReportItem> lst = new List<ClubInsuranceReportItem>(d.Values);
            lst.Sort((c1, c2) => { return c1.User.UserFullName.CompareCurrentCultureIgnoreCase(c2.User.UserFullName); });

            return lst;
        }

    }
}