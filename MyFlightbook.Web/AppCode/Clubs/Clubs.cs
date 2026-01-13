using Ganss.Xss;
using MyFlightbook.Airports;
using MyFlightbook.CSV;
using MyFlightbook.Currency;
using MyFlightbook.Instruction;
using MyFlightbook.Payments;
using MyFlightbook.Schedule;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2014-2026 MyFlightbook LLC
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
        private const int policyMaskDoubleBookShift = 5;             // shift above 5 bits to the right to get the policy mask for double booking
        private const UInt32 policyMaskAddModifyNotification = 0x00000180;
        private const int policyMaskAddModifyNotificationShift = 7; // Shift above 7 bits to the right to get the policy mask for add/modify notification
        private const UInt32 policyFlagShowHeadshots = 0x00000200;
        private const UInt32 policyFlagShowMobileNumber = 0x00000400;
        private const UInt32 policyFlagOnlyAdminsEdit = 0x00000800;

        /// <summary>
        /// Returns database bitmask to use to check for suppression of photo sharing, mobile sharing, or both.
        /// </summary>
        /// <param name="fPhoto"></param>
        /// <param name="fMobile"></param>
        /// <returns></returns>
        public static UInt32 maskForSharing(bool fPhoto, bool fMobile)
        {
            return (fPhoto ? policyFlagShowHeadshots : 0) | (fMobile ? policyFlagShowMobileNumber : 0);
        }

        public enum DeleteNoficiationPolicy { None = 0x00, Admins = 0x01, WholeClub = 0x02 }
        public enum AddModifyNotificationPolicy { None = 0x00, Admins = 0x01, WholeClub = 0x02 }
        public enum DoubleBookPolicy { None = 0x00, Admins = 0x01, WholeClub = 0x02 }

        public enum EditPolicy { AllMembers, OwnersAndAdmins, AdminsOnly }

        public const int TrialPeriod = 31;  // 30 days, with some buffer.

        /// <summary>
        /// OK = fine
        /// Promotional = 30 day trial window
        /// Expired = inactive due to trial period ending.  Like inactive, but paying will re-activate.
        /// Inactive = inactive, period, end of story (i.e., paying doesn't reactivate it)
        /// </summary>
        public enum ClubStatus { OK, Promotional, Expired, Inactive }

        #region Authorization
        public static ClubStatus StatusForUser(string szUser)
        {
            return String.IsNullOrEmpty(szUser) ? ClubStatus.Inactive : (EarnedGratuity.UserQualifies(szUser, Gratuity.GratuityTypes.CreateClub) ? ClubStatus.OK : ClubStatus.Promotional);
        }
        #endregion

        #region properties
        /// <summary>
        /// ID for the club
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Is this a new club?
        /// </summary>
        public bool IsNew { get { return ID == ClubIDNew; } }

        /// <summary>
        /// Username of the user who created the club
        /// </summary>
        public string Creator { get; set; }

        /// <summary>
        /// Date/time that the club was created
        /// </summary>
        public DateTime CreationDate { get; set; }

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
        public string Name { get; set; }

        /// <summary>
        /// Description of the club
        /// </summary>
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Description { get; set; }

        /// <summary>
        /// URL for the club, as provided by the user.  This is NOT stored as a URL because it may not pass syntax muster, and we just want to use it as is.
        /// </summary>
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string ProvidedLink { get; set; }

        /// <summary>
        /// Returns a fixed URL (includes HTTP if needed)
        /// </summary>
        public string Link
        {
            get
            {
                return (ProvidedLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase)) ? ProvidedLink : "http://" + ProvidedLink;
            }
        }

        /// <summary>
        /// City where the club is located
        /// </summary>
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string City { get; set; }

        /// <summary>
        /// State or province
        /// </summary>
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string StateProvince { get; set; }

        /// <summary>
        /// Country
        /// </summary>
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Country { get; set; }

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
            get { return VirtualPathUtility.ToAbsolute("~/mvc/club/details/" + ID.ToString(CultureInfo.InvariantCulture)); }
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
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string ContactPhone { get; set; }

        /// <summary>
        /// Home airport
        /// </summary>
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string HomeAirportCode { get; set; }

        /// <summary>
        /// The club's home airport (can be null)
        /// </summary>
        public airport HomeAirport { get; set; }

        /// <summary>
        /// Timezone information for the home airport
        /// </summary>
        public TimeZoneInfo TimeZone { get; set; }

        /// <summary>
        /// Shortcut to set timezone by ID or quickly get the ID
        /// </summary>
        public string TimeZoneID
        {
            get { return TimeZone?.Id; }
            set
            {
                try { TimeZone = TimeZoneInfo.FindSystemTimeZoneById(value); }
                catch (TimeZoneNotFoundException)
                {
                    TimeZone = null;
                }
            }
        }

        /// <summary>
        /// Most recent error
        /// </summary>
        public string LastError { get; set; }

        private List<ClubAircraft> m_clubAircraft;
        private List<ClubMember> m_clubMembers;

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
        /// True if all members can edit all appointments - this shouldn't be used directly, use EditingPolicy instead
        /// </summary>
        private bool RestrictEditingToOwnersAndAdmins
        {
            get { return (Policy & policyFlagRestrictApptEditing) == policyFlagRestrictApptEditing; }
            set { Policy = value ? (Policy | policyFlagRestrictApptEditing) : (Policy & ~policyFlagRestrictApptEditing); }
        }

        /// <summary>
        /// True if only admins can make ANY edits.  (I.e., members can't even edit their own) - this shouldn't be used directly, use EditingPolicy instead
        /// </summary>
        private bool OnlyAdminsCanEdit
        {
            get { return (Policy & policyFlagOnlyAdminsEdit) == policyFlagOnlyAdminsEdit; }
            set { Policy = value ? (Policy | policyFlagOnlyAdminsEdit) : (Policy & ~policyFlagOnlyAdminsEdit); }
        }

        /// <summary>
        /// What editing policy is used by this club?
        /// </summary>
        public EditPolicy EditingPolicy
        {
            get { return OnlyAdminsCanEdit ? EditPolicy.AdminsOnly : RestrictEditingToOwnersAndAdmins ? EditPolicy.OwnersAndAdmins : EditPolicy.AllMembers; }
            set
            {
                OnlyAdminsCanEdit = value == EditPolicy.AdminsOnly;
                RestrictEditingToOwnersAndAdmins = value == EditPolicy.OwnersAndAdmins;
            }
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
        /// Convenience property, since the policy flag is set to *hide* (not show) headshots
        /// </summary>
        public bool ShowHeadshots
        {
            get { return !HideHeadshots; }
            set { HideHeadshots = !value; }
        }

        /// <summary>
        /// Indicates whether or not to show headshots of other club members.
        /// </summary>
        public bool HideHeadshots {
            get { return (Policy & policyFlagShowHeadshots) == policyFlagShowHeadshots; }
            set { Policy = value ? (Policy | policyFlagShowHeadshots) : (Policy & ~policyFlagShowHeadshots); }
        }

        /// <summary>
        /// Convenience property, since the policy flag is set to *hide* (not show) mobile number
        /// </summary>
        public bool ShowMobileNumbers
        {
            get { return !HideMobileNumbers; }
            set { HideMobileNumbers = !value; }
        }

        /// <summary>
        /// Indicates whether or not to expose members' mobile phone #'s.
        /// </summary>
        public bool HideMobileNumbers
        {
            get { return (Policy & policyFlagShowMobileNumber) == policyFlagShowMobileNumber; }
            set { Policy = value ? (Policy | policyFlagShowMobileNumber) : (Policy & ~policyFlagShowMobileNumber); }
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
            get { return (DeleteNoficiationPolicy)((Policy & policyMaskDeleteNotification) >> policyMaskDeleteShift); }
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
            Creator = Name = Description = ProvidedLink = City = StateProvince = Country = ContactPhone = HomeAirportCode = string.Empty;
            CreationDate = DateTime.MinValue;
            Status = ClubStatus.OK;
            LastError = string.Empty;
            TimeZone = null;
            Policy = 0x0;
        }

        protected Club(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));

            ID = Convert.ToInt32(dr["idclub"], CultureInfo.InvariantCulture);
            Creator = dr["creator"].ToString();
            CreationDate = Convert.ToDateTime(dr["creationDate"], CultureInfo.InvariantCulture);
            Status = (ClubStatus)Convert.ToInt32(dr["clubStatus"], CultureInfo.InvariantCulture);
            Name = dr["clubname"].ToString();
            Description = util.ReadNullableString(dr, "clubdesc");
            ProvidedLink = util.ReadNullableString(dr, "clubURL");
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
            return String.Format(CultureInfo.InvariantCulture, "{0} ({1})", Name, HomeAirport?.Code ?? HomeAirportCode ?? string.Empty);
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
                if (TimeZone == null)
                    throw new MyFlightbookValidationException(Resources.Schedule.errNoTimezone);
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
                // Sanitize any HTML!!
                Description = new HtmlSanitizer().Sanitize(Description);

                // Fix up the airport.
                if (!String.IsNullOrEmpty(HomeAirportCode))
                {
                    AirportList al = new AirportList(HomeAirportCode);
                    List<airport> lst = new List<airport>(al.GetAirportList());
                    airport ap = lst.FirstOrDefault(a => a.IsPort);
                    HomeAirportCode = ap?.Code ?? HomeAirportCode ?? string.Empty;
                }

                const string szSetCore = @"creator=?szcreator, clubname=?name, clubStatus=?status, clubdesc=?description, clubURL=?url, clubCity=?city, 
                            clubStateProvince=?state, clubCountry=?country, clubContactPhone=?contact, clubHomeAirport=?airport, clubTimeZoneID=?tzID, clubTimeZoneOffset=?tzOffset, policyFlags=?pflag";

                const string szQInsertT = "INSERT INTO clubs SET creationDate=Now(), {0}";
                const string szQUpdateT = "UPDATE clubs SET {0} WHERE idclub=?idClub";

                bool fNew = IsNew;

                DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, IsNew ? szQInsertT : szQUpdateT, szSetCore));

                fResult = dbh.DoNonQuery((comm) =>
                    {
                        comm.Parameters.AddWithValue("szcreator", Creator);
                        comm.Parameters.AddWithValue("name", Name.LimitTo(90));
                        comm.Parameters.AddWithValue("description", Description.LimitTo(40000));
                        comm.Parameters.AddWithValue("status", (int)Status);
                        comm.Parameters.AddWithValue("url", ProvidedLink.LimitTo(255));
                        comm.Parameters.AddWithValue("city", City.LimitTo(45));
                        comm.Parameters.AddWithValue("state", StateProvince.LimitTo(45));
                        comm.Parameters.AddWithValue("country", Country.LimitTo(45));
                        comm.Parameters.AddWithValue("contact", ContactPhone.LimitTo(45));
                        comm.Parameters.AddWithValue("airport", HomeAirportCode.LimitTo(45).ToUpperInvariant());
                        comm.Parameters.AddWithValue("idClub", ID);
                        comm.Parameters.AddWithValue("tzID", TimeZone.Id);
                        comm.Parameters.AddWithValue("tzOffset", (int)TimeZone.BaseUtcOffset.TotalMinutes);
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
            return String.Format(CultureInfo.InvariantCulture, "(c.policyFlags & {0}) = 0 AND clubStatus NOT IN ({1}, {2})", policyFlagPrivateClub, (int)ClubStatus.Inactive, (int)ClubStatus.Expired);
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
                c = (Club)util.GlobalCache.Get(szKey);
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
            util.GlobalCache.Remove(szKey);
            if (c != null)
                util.GlobalCache.Set(c.CacheKey, c, DateTimeOffset.UtcNow.AddMinutes(30));
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

        public static void UncacheClubsForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                return;

            IEnumerable<Club> lst = AllClubsForUser(szUser);
            foreach (Club c in lst)
                Club.ClearCachedClub(c.ID);
        }

        public static IEnumerable<Club> ClubsForAircraft(int idaircraft, string szUser = null)
        {
            List<Club> lst = new List<Club>();
            string szQ = String.Format(CultureInfo.InvariantCulture, @"SELECT c.*, ap.*, 0 AS dist, '' AS FriendlyName 
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
            return Members.FirstOrDefault(cm => cm.UserName.CompareOrdinalIgnoreCase(szUser) == 0);
        }

        /// <summary>
        /// Check if the specified user is an admin of the club.
        /// </summary>
        /// <param name="szUser">Username</param>
        /// <returns>True if they are in the admin list.</returns>
        public bool HasAdmin(string szUser)
        {
            return Admins.FirstOrDefault(cm => cm.UserName.CompareOrdinalIgnoreCase(szUser) == 0) != null;
        }

        /// <summary>
        /// Check if the specified user is a member of the club.
        /// </summary>
        /// <param name="szUser">Username</param>
        /// <returns>True if they are in the admin list.</returns>
        public bool HasMember(string szUser)
        {
            return MembersAsList.Exists(cm => cm.UserName.CompareOrdinalIgnoreCase(szUser) == 0);
        }

        public void NotifyAdd(ScheduledEvent s, string addingUser)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

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

            lst.RemoveAll(pf => pf is ClubMember cm && cm.IsInactive);

            s.LocalTimeZone = TimeZone;    // ensure that we're using the right timezone
            if (s.ResourceAircraft == null)
                s.ResourceAircraft = MemberAircraft.FirstOrDefault(ca => ca.AircraftID.ToString(CultureInfo.InvariantCulture).CompareOrdinal(s.ResourceID) == 0);

            string szBody = Resources.Schedule.ResourceBooked
                .Replace("<% Creator %>", pfCreator.UserFullName)
                .Replace("<% Resource %>", s.ResourceAircraft == null ? string.Empty : s.ResourceAircraft.DisplayTailnumber)
                .Replace("<% ScheduleDetail %>", String.Format(CultureInfo.CurrentCulture, "{0:d} {1:t} ({2}){3}", s.LocalStart, s.LocalStart, s.DurationDisplay, String.IsNullOrEmpty(s.Body) ? string.Empty : String.Format(CultureInfo.CurrentCulture, " - {0}", s.Body)));
            string szSubject = Branding.ReBrand(Resources.Schedule.CreateNotificationSubject);

            foreach (Profile pf in lst)
                util.NotifyUser(szSubject, szBody, new MailAddress(pf.Email, pf.UserFullName), false, false);
        }

        public void NotifyOfChange(ScheduledEvent sOriginal, ScheduledEvent sNew, string changingUser)
        {
            // No notification if no change was actually made
            if (sOriginal == sNew)
                return;

            if (sOriginal == null)
                throw new ArgumentNullException(nameof(sOriginal));
            if (sNew == null)
                throw new ArgumentNullException(nameof(sNew));

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

            lst.RemoveAll(pf => pf is ClubMember cm && cm.IsInactive);

            // check if no notifications required
            if (lst.Count == 0)
                return;

            sOriginal.LocalTimeZone = TimeZone;    // ensure that we're using the right timezone
            if (sOriginal.ResourceAircraft == null)
                sOriginal.ResourceAircraft = MemberAircraft.FirstOrDefault(ca => ca.AircraftID.ToString(CultureInfo.InvariantCulture).CompareOrdinal(sOriginal.ResourceID) == 0);

            Profile pfChanging = Profile.GetUser(changingUser);

            string szBody = Resources.Schedule.ResourceChangedByOther
                .Replace("<% Deleter %>", pfChanging.UserFullName)
                .Replace("<% Resource %>", sOriginal.ResourceAircraft == null ? string.Empty : sOriginal.ResourceAircraft.DisplayTailnumber)
                .Replace("<% ScheduleDetail %>", String.Format(CultureInfo.CurrentCulture, "{0:d} {1:t} ({2}){3}", sOriginal.LocalStart, sOriginal.LocalStart, sOriginal.DurationDisplay, String.IsNullOrEmpty(sOriginal.Body) ? string.Empty : String.Format(CultureInfo.CurrentCulture, " - {0}", sOriginal.Body)))
                .Replace("<% ScheduleDetailNew %>", String.Format(CultureInfo.CurrentCulture, "{0:d} {1:t} ({2}){3}", sNew.LocalStart, sNew.LocalStart, sNew.DurationDisplay, String.IsNullOrEmpty(sNew.Body) ? string.Empty : String.Format(CultureInfo.CurrentCulture, " - {0}", sNew.Body)))
                .Replace("<% TimeStamp %>", string.Empty /* DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) */);
            string szSubject = Branding.ReBrand(Resources.Schedule.ChangeNotificationSubject);

            foreach (Profile pf in lst)
                util.NotifyUser(szSubject, szBody, new MailAddress(pf.Email, pf.UserFullName), false, false);
        }

        public void NotifyOfDelete(ScheduledEvent s, string deletingUser)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

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

            lst.RemoveAll(pf => pf is ClubMember cm && cm.IsInactive);

            // And add the owner if they didn't delete it themselves - but only if not already in list above
            if (s.OwningUser.CompareOrdinalIgnoreCase(deletingUser) != 0 && !lst.Exists(pf => pf.UserName.CompareOrdinalIgnoreCase(s.OwningUser) == 0))
                lst.Add(Profile.GetUser(s.OwningUser));

            if (lst.Count > 0)
            {
                s.LocalTimeZone = TimeZone;    // ensure that we're using the right timezone
                if (s.ResourceAircraft == null)
                    s.ResourceAircraft = MemberAircraft.FirstOrDefault(ca => ca.AircraftID.ToString(CultureInfo.InvariantCulture).CompareOrdinal(s.ResourceID) == 0);

                Profile pfDeleting = Profile.GetUser(deletingUser);
                string szBody = Resources.Schedule.AppointmentDeleted
                    .Replace("<% Deleter %>", pfDeleting.UserFullName)
                    .Replace("<% Resource %>", s.ResourceAircraft == null ? string.Empty : s.ResourceAircraft.DisplayTailnumber)
                    .Replace("<% ScheduleDetail %>", String.Format(CultureInfo.CurrentCulture, "{0:d} {1:t} ({2}){3}", s.LocalStart, s.LocalStart, s.DurationDisplay, String.IsNullOrEmpty(s.Body) ? string.Empty : String.Format(CultureInfo.CurrentCulture, " - {0}", s.Body)))
                    .Replace("<% TimeStamp %>", string.Empty /* DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) */);
                string szSubject = Branding.ReBrand(Resources.Schedule.DeleteNotificationSubject);

                lst.ForEach((pf) => { util.NotifyUser(szSubject, szBody, new MailAddress(pf.Email, pf.UserFullName), false, false); });
            }
        }

        /// <summary>
        /// Contact a member of the club (from another member)
        /// </summary>
        /// <param name="szSender">Username of sender</param>
        /// <param name="szTarget">Username of target</param>
        /// <param name="szSubject">Subject of message - must not be empty</param>
        /// <param name="szText">Body of message - must not be empty</param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void ContactMember(string szSender, string szTarget, string szSubject, string szText)
        {
            if (String.IsNullOrEmpty(szSubject) || String.IsNullOrEmpty(szText))
                throw new InvalidOperationException(Resources.Club.errNoMessageToSend);

            if (String.IsNullOrEmpty(szTarget))
                throw new InvalidOperationException("No known user " + szTarget + " (should not happen)");

            Profile pf = Profile.GetUser(szTarget);
            Profile pfSender = Profile.GetUser(szSender);
            using (MailMessage msg = new MailMessage())
            {
                MailAddress maFrom = new MailAddress(pf.Email, pf.UserFullName);
                msg.To.Add(new MailAddress(pf.Email, pf.UserFullName));
                msg.From = new MailAddress(Branding.CurrentBrand.EmailAddress, String.Format(CultureInfo.CurrentCulture, Resources.SignOff.EmailSenderAddress, Branding.CurrentBrand.AppName, pf.UserFullName));
                msg.ReplyToList.Add(new MailAddress(pfSender.Email, pfSender.UserFullName));
                msg.Subject = szSubject;
                msg.Body = szText;
                msg.IsBodyHtml = false;
                util.SendMessage(msg);
            }
        }

        /// <summary>
        /// Contact club admins as a guest
        /// </summary>
        /// <param name="szUser">Username of sender</param>
        /// <param name="idClub">ID of club</param>
        /// <param name="szMessage">Message to send</param>
        /// <param name="fAlsoRequestJoin">If true, also generates formal request emails.</param>
        public static void ContactClubAdmins(string szUser, int idClub, string szMessage, bool fAlsoRequestJoin)
        {
            if (string.IsNullOrEmpty(szUser))
                throw new UnauthorizedAccessException("You must be signed in");

            Profile pf = Profile.GetUser(szUser);
            Club c = ClubWithID(idClub) ?? throw new InvalidOperationException("No such club with ID " + idClub.ToString(CultureInfo.InvariantCulture));
            if (String.IsNullOrWhiteSpace(szMessage))
                throw new InvalidOperationException(Resources.Club.errNoContactMessageBody);

            IEnumerable<ClubMember> lst = ClubMember.AdminsForClub(c.ID);
            using (MailMessage msg = new MailMessage())
            {
                MailAddress maFrom = new MailAddress(pf.Email, pf.UserFullName);
                msg.From = new MailAddress(Branding.CurrentBrand.EmailAddress, String.Format(CultureInfo.CurrentCulture, Resources.SignOff.EmailSenderAddress, Branding.CurrentBrand.AppName, pf.UserFullName));
                msg.ReplyToList.Add(maFrom);
                foreach (ClubMember cm in lst)
                    msg.To.Add(new MailAddress(cm.Email, cm.UserFullName));
                msg.Subject = String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.Club.ContactSubjectTemplate), c.Name);
                msg.Body = szMessage + "\r\n\r\n" + String.Format(CultureInfo.CurrentCulture, Resources.Club.MessageSenderTemplate, pf.UserFullName, pf.Email);
                msg.IsBodyHtml = false;
                util.SendMessage(msg);
            }
            if (fAlsoRequestJoin)
                foreach (ClubMember admin in lst)
                    new CFIStudentMapRequest(pf.UserName, admin.Email, CFIStudentMapRequest.RoleType.RoleRequestJoinClub, c).Send();
        }
        #endregion

        #region Upcoming Appointments
        public enum SummaryMode { Club, Aircraft, User }

        public void MapAircraftAndUsers(IEnumerable<ScheduledEvent> lst)
        {
            if (lst == null)
                return;

            foreach (ScheduledEvent se in lst)
            {
                if (int.TryParse(se.ResourceID, out int idAircraft))
                    se.ResourceAircraft = MemberAircraft.FirstOrDefault(ca => ca.AircraftID == idAircraft) ?? new Aircraft(idAircraft); // handle scenario where the aircraft isn't in MemberAircraft (e.g., has been removed from the club)?
                se.OwnerProfile = Members.FirstOrDefault(cm => String.Compare(cm.UserName, se.OwningUser, StringComparison.Ordinal) == 0);
            }
        }

        public IEnumerable<ScheduledEvent> GetUpcomingEvents(int limit, string resource = null, string owner = null)
        {
            List<ScheduledEvent> lst = ScheduledEvent.UpcomingAppointments(ID, TimeZone, limit, resource, owner);

            // Fix up the aircraft, owner names
            MapAircraftAndUsers(lst);

            return lst;
        }
        #endregion

        #region Reporting
        private static void SendReport(ClubMember cm, string szReportName, string szReportPrefix, string szPath)
        {
            if (cm == null)
                throw new ArgumentNullException(nameof(cm));
            using (MailMessage msg = new MailMessage())
            {
                Brand brand = Branding.CurrentBrand;
                msg.From = new MailAddress(brand.EmailAddress, brand.AppName);
                msg.To.Add(new MailAddress(cm.Email, cm.UserFullName));
                msg.Subject = szReportName;
                msg.IsBodyHtml = true;

                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    string szURL = String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Branding.CurrentBrand.HostName, VirtualPathUtility.ToAbsolute(szPath));
                    byte[] rgdata = wc.DownloadData(szURL);
                    string szReportBody = Encoding.UTF8.GetString(rgdata);
                    msg.Body = $@"
<html>
<head>
<link href=""https://{Branding.CurrentBrand.HostName}{MFBConstants.BaseCssVars(false)}"" rel=""stylesheet"" type=""text/css"" />
<link href=""https://{Branding.CurrentBrand.HostName}{MFBConstants.BaseStylesheet.ToAbsolute()}"" rel=""stylesheet"" type=""text/css"" />
</head>
<body>
<p>{HttpUtility.HtmlEncode(szReportPrefix)}</p>
<div>{szReportBody}</div>
</body>";
                }

                util.SendMessage(msg);
            }
        }

        public static void SendMonthlyClubReports()
        {
            DateTime startDate = new DateTime(DateTime.Now.AddDays(-1).Year, DateTime.Now.AddDays(-1).Month, 1);
            string szDateMonth = startDate.ToString("MMM yyyy", CultureInfo.CurrentCulture);

            foreach (ClubMember cm in ClubMember.AllClubOfficers())
            {
                Club club = Club.ClubWithID(cm.ClubID);
                if (club == null)
                    continue;

                if (cm.IsTreasurer)
                    SendReport(cm,
                        String.Format(CultureInfo.CurrentCulture, Resources.Club.ClubReportEmailSubject, szDateMonth, Resources.Club.ClubReportFlying, club.Name),
                        String.Format(CultureInfo.CurrentCulture, Resources.Club.ClubReportEmailBodyTemplate, Resources.Club.ClubReportFlying, club.Name, szDateMonth),
                        String.Format(CultureInfo.InvariantCulture, "~/mvc/club/FlyingReportForEmail?idClub={0}&szUser={1}", club.ID, cm.UserName));

                if (cm.IsMaintanenceOfficer)
                    SendReport(cm,
                        String.Format(CultureInfo.CurrentCulture, Resources.Club.ClubReportEmailSubject, szDateMonth, Resources.Club.ClubReportMaintenance, club.Name),
                        String.Format(CultureInfo.CurrentCulture, Resources.Club.ClubReportEmailBodyTemplate, Resources.Club.ClubReportMaintenance, club.Name, szDateMonth),
                        String.Format(CultureInfo.InvariantCulture, "~/mvc/club/MaintenanceReportForEmail?idClub={0}&szUser={1}", club.ID, cm.UserName));

                if (cm.IsInsuranceOfficer)
                    SendReport(cm,
                        String.Format(CultureInfo.CurrentCulture, Resources.Club.ClubReportEmailSubject, szDateMonth, Resources.Club.ClubReportInsurance, club.Name),
                        String.Format(CultureInfo.CurrentCulture, Resources.Club.ClubReportEmailBodyTemplate, Resources.Club.ClubReportInsurance, club.Name, szDateMonth),
                        String.Format(CultureInfo.InvariantCulture, "~/mvc/club/InsuranceReportForEmail?idClub={0}&szUser={1}", club.ID, cm.UserName));
            }
        }
        #endregion
    }

    /// <summary>
    /// A member of a flying club.
    /// </summary>
    [Serializable]
    public class ClubMember : Profile
    {
        /// <summary>
        /// The role of the member within the club - indicates level of privileges.  LIMITED TO LOWER 8 BITS of the "Role" field in database.  These are mutually exclusive.
        /// </summary>
        public enum ClubMemberRole { Member, Admin, Owner }

        private const UInt32 RoleMask = 0xFF;

        // Officers.  These are NOT mutually exclusive, but are limited to 0xFF00 mask
        private const UInt32 MaintenanceOfficerMask = 0x0100;
        private const UInt32 TreasurerMask = 0x0200;
        private const UInt32 InsuranceOfficerMask = 0x0400;

        private const UInt32 InactiveMask = 0x00010000;   // Is the user inactive.

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
        /// Name of the office the member holds in the club.  E.g., "Chief Pilot".
        /// </summary>
        public string ClubOffice { get; set; }

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
                List<string> lst = new List<string>();
                switch (RoleInClub)
                {
                    case ClubMemberRole.Admin:
                        lst.Add(Resources.Club.RoleManager);
                        break;
                    case ClubMemberRole.Owner:
                        lst.Add(Resources.Club.RoleOwner);
                        break;
                    default:
                    case ClubMemberRole.Member:
                        lst.Add(Resources.Club.RoleMember);
                        break;
                }

                if (IsMaintanenceOfficer)
                    lst.Add(Resources.Club.RoleMaintenanceOfficer);
                if (IsTreasurer)
                    lst.Add(Resources.Club.RoleTreasurer);
                if (IsInsuranceOfficer)
                    lst.Add(Resources.Club.RoleInsuranceOfficer);

                return String.Join(CultureInfo.CurrentCulture.TextInfo.ListSeparator + Resources.LocalizedText.LocalizedSpace, lst);
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

        /// <summary>
        /// Is this member a maintenance officer?
        /// </summary>
        public bool IsMaintanenceOfficer { get; set; }

        /// <summary>
        /// Is this member a treasurer?
        /// </summary>
        public bool IsTreasurer { get; set; }

        /// <summary>
        /// Is this member responsible for insurance?
        /// </summary>
        public bool IsInsuranceOfficer { get; set; }

        public bool IsOfficer { get { return IsTreasurer || IsInsuranceOfficer || IsMaintanenceOfficer; } }

        public bool IsInactive { get; set; }

        protected UInt32 ConsolidatedRoleFlags
        {
            get { return ((UInt32)RoleInClub) | (IsMaintanenceOfficer ? MaintenanceOfficerMask : 0) | (IsTreasurer ? TreasurerMask : 0) | (IsInsuranceOfficer ? InsuranceOfficerMask : 0) | (IsInactive ? InactiveMask : 0); }
        }
        #endregion

        #region initialization
        public ClubMember() : base()
        {
            ClubID = Club.ClubIDNew;
            RoleInClub = ClubMemberRole.Member;
            IsMaintanenceOfficer = IsTreasurer = IsInsuranceOfficer = false;
            ClubOffice = LastError = string.Empty;
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
                throw new ArgumentNullException(nameof(dr));
            ClubID = Convert.ToInt32(dr["idclub"], CultureInfo.InvariantCulture);
            UInt32 roleFlags = Convert.ToUInt32(dr["role"], CultureInfo.InvariantCulture);
            RoleInClub = (ClubMemberRole)(roleFlags & RoleMask);
            IsMaintanenceOfficer = ((roleFlags & MaintenanceOfficerMask) != 0);
            IsTreasurer = ((roleFlags & TreasurerMask) != 0);
            IsInsuranceOfficer = ((roleFlags & InsuranceOfficerMask) != 0);
            IsInactive = ((roleFlags & InactiveMask) != 0);
            JoinedDate = Convert.ToDateTime(dr["joindate"], CultureInfo.InvariantCulture);
            LastError = string.Empty;
            ClubOffice = (string)util.ReadNullableField(dr, "cluboffice", string.Empty);
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
                "UPDATE clubmembers SET role=?role, cluboffice=?office WHERE idclub=?id AND username=?user");
            bool fResult = dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("id", ClubID);
                comm.Parameters.AddWithValue("user", UserName);
                comm.Parameters.AddWithValue("role", (int)ConsolidatedRoleFlags);
                comm.Parameters.AddWithValue("office", String.IsNullOrWhiteSpace(ClubOffice) ? null : ClubOffice.LimitTo(254));
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
        /// Returns a list of all club officers (insurance, maintenance, treasurer) across ALL clubs.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ClubMember> AllClubOfficers()
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"SELECT cm.*, u.*
                FROM clubmembers cm
                INNER JOIN users u ON cm.username=u.username
                LEFT JOIN usersinroles uir ON (u.Username=uir.Username AND uir.ApplicationName='Logbook')
                WHERE (cm.role & ~0x{0}) <> 0
                ORDER BY u.Lastname ASC, u.FirstName ASC, u.Username ASC", RoleMask.ToString("x", CultureInfo.InvariantCulture)));
            List<ClubMember> lst = new List<ClubMember>();
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new ClubMember(dr)); });
            return lst;
        }

        /// <summary>
        /// Determines if the two users are members of at least one club in common.  Users always share with themselves, so if both specified users are the same, this returns true.
        /// </summary>
        /// <param name="szUser1"></param>
        /// <param name="szUser2"></param>
        /// <returns></returns>
        public static bool CheckUsersShareClub(string szUser1, string szUser2, bool fPhoto, bool fMobile)
        {
            bool fResult = false;
            if (String.IsNullOrEmpty(szUser1) || String.IsNullOrEmpty(szUser2))
                return fResult;

            // same user is always true, avoid the database hit.
            if (szUser1.CompareOrdinal(szUser2) == 0)
                return true;

            string szCacheKey = "clubPeersFor" + szUser1;

            ICacheService cache = util.GlobalCache;

            if (!(cache.Get(szCacheKey) is HashSet<string> hsPeers))
            {
                hsPeers = new HashSet<string>();
                DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"SELECT DISTINCT
                    cm.username
                FROM
                    clubs c
                        INNER JOIN
                    clubmembers cm ON cm.idclub = c.idclub
                        INNER JOIN
                    clubmembers cm2 ON cm2.idclub = c.idclub
                WHERE
	                (c.policyFlags & 0x{0} = 0) AND
                    cm2.username = ?u1;", Club.maskForSharing(fPhoto, fMobile).ToString("x", CultureInfo.InvariantCulture)));

                dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("u1", szUser1); },
                    (dr) => { hsPeers.Add(dr["username"].ToString()); });

                // We are likely to get a few requests all at once, and then none for a while, so cache this for 15 minutes.
                cache.Set(szCacheKey, hsPeers, DateTimeOffset.UtcNow.AddMinutes(15));
            }
            return hsPeers.Contains(szUser2);
        }

        /// <summary>
        /// Gets the list of all maintenance officers for the club
        /// </summary>
        /// <param name="idclub">The club</param>
        /// <returns>The list of relevant members</returns>
        public static IEnumerable<ClubMember> MaintenanceOfficersForClub(int idclub)
        {
            List<ClubMember> lst = MembersForClub(idclub);
            lst.RemoveAll(cm => !cm.IsMaintanenceOfficer);
            return lst;
        }

        /// <summary>
        /// Gets the list of all treasurers for the club
        /// </summary>
        /// <param name="idclub">The club</param>
        /// <returns>The list of relevant members</returns>
        public static IEnumerable<ClubMember> TreasurersForClub(int idclub)
        {
            List<ClubMember> lst = MembersForClub(idclub);
            lst.RemoveAll(cm => !cm.IsTreasurer);
            return lst;
        }

        /// <summary>
        /// Gets the list of all insurance officers for the club
        /// </summary>
        /// <param name="idclub">The club</param>
        /// <returns>The list of relevant members</returns>
        public static IEnumerable<ClubMember> InsuranceOfficersForClub(int idclub)
        {
            List<ClubMember> lst = MembersForClub(idclub);
            lst.RemoveAll(cm => !cm.IsInsuranceOfficer);
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
            return MembersForClub(idclub).Exists(cm => cm.UserName.CompareOrdinalIgnoreCase(szuser) == 0);
        }

        /// <summary>
        /// Is the user a member of the specified club?
        /// </summary>
        /// <param name="idclub">The club ID</param>
        /// <param name="szuser">The username</param>
        /// <returns>True if they are in the list with a non-member role</returns>
        public static bool IsAdmin(int idclub, string szuser)
        {
            return AdminsForClub(idclub).FirstOrDefault<ClubMember>(cm => cm.UserName.CompareOrdinalIgnoreCase(szuser) == 0) != null;
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
                throw new ArgumentNullException(nameof(dr));
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

            bool fResult = FDeleteAircraftFromClub(ClubID, AircraftID, out string szError);
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
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, Aircraft.szAircraftForUserCore, "ca.idclub, ca.description, ca.highWaterMark, 0", "''", "''", "''", " INNER JOIN clubaircraft ca ON aircraft.idaircraft=ca.idaircraft WHERE ca.idclub=?clubid "));
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

            if (!lst.Any())
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
                    ClubAircraft ca = lst.First(ca2 => ca2.AircraftID == Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture));
                    if (ca != null)
                    {
                        ca.HighestRecordedHobbs = (Decimal)util.ReadNullableField(dr, "MaxHobbs", 0.0M);
                        ca.HighestRecordedTach = (Decimal)util.ReadNullableField(dr, "MaxTach", 0.0M);
                    }
                });
        }
        #endregion
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
            User = user ?? throw new ArgumentNullException(nameof(user));
            PilotStatusItems = new ProfileCurrency(user).CurrencyForUser();
        }
        #endregion

        #region ReportForClub helpers.
        private static void PopulateMemberInsurance(Dictionary<string, ClubInsuranceReportItem> d, int idClub, int monthInterval)
        {
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
        }

        private static void PopulateMemberFlights(Dictionary<string, ClubInsuranceReportItem> d, int idClub, IList<Aircraft> lstAc)
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"SELECT
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
ORDER BY username asc;"));
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("clubid", idClub); },
                (dr) => {
                    ClubInsuranceReportItem ciri = d[(string)dr["username"]];
                    int idAircraft = Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture);
                    decimal timeInAircraft = Convert.ToDecimal(dr["TimeInAircraft"], CultureInfo.InvariantCulture);
                    Aircraft ac = lstAc.FirstOrDefault(ac2 => ac2.AircraftID == idAircraft) ?? throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Unknown aircraft {0} for club {1} in club report", idAircraft, idClub));
                    ciri.TotalsByClubAircraft[ac.DisplayTailnumber] = timeInAircraft;
                });
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

            PopulateMemberInsurance(d, idClub, monthInterval);

            List<Aircraft> lstAc = new List<Aircraft>(c.MemberAircraft);

            PopulateMemberFlights(d, idClub, lstAc);

            List<ClubInsuranceReportItem> lst = new List<ClubInsuranceReportItem>(d.Values);
            lst.Sort((c1, c2) => { return c1.User.UserFullName.CompareCurrentCultureIgnoreCase(c2.User.UserFullName); });

            return lst;
        }
    }

    public class ClubFlyingReportItem
    {
        #region Properties
        public int FlightID { get; set; }
        public DateTime FlightDate { get; set; }
        public string Aircraft { get; set; }
        public string PilotName { get; set; }
        public string Route { get; set; }
        public bool IsInstruction { get; set; }
        public string FlightRules { get; set; }
        public int Passengers { get; set; }
        public decimal TotalTime { get; set; }
        public decimal HobbsStart { get; set; }
        public decimal HobbsEnd { get; set; }
        public decimal TotalHobbs { get; set; }
        public decimal TachStart { get; set; }
        public decimal TachEnd { get; set; }

        public decimal TotalTach { get; set; }

        public DateTime? FlightStart { get; set; }
        public DateTime? FlightEnd { get; set; }

        public decimal TotalFlight { get; set; }
        public DateTime? EngineStart { get; set; }
        public DateTime? EngineEnd { get; set; }

        public decimal TotalEngine { get; set; }

        public decimal OilAdded { get; set; }
        public decimal OilAdded2 { get; set; }
        public decimal OilLevel { get; set; }
        public decimal FuelAdded { get; set; }
        public decimal FuelRemaining { get; set; }
        public decimal FuelCost { get; set; }
        #endregion

        protected ClubFlyingReportItem() { }

        /// <summary>
        /// Generates a flying report for the specified club
        /// </summary>
        /// <param name="idClub">ID of the club</param>
        /// <param name="dateStart">Lower bound of date range</param>
        /// <param name="dateEnd">Upper bound of date range</param>
        /// <param name="szUser">If non-empty, only shows flights for this user; otherwise, all club members</param>
        /// <param name="idAircraft">If positive, only shows flights for this aircraft, otherwise all club aircraft</param>
        public static IEnumerable<ClubFlyingReportItem> ReportForClub(int idClub, DateTime dateStart, DateTime dateEnd, string szUser, int idAircraft)
        {
            const string szQTemplate = @"SELECT f.idflight, f.date AS Date, f.TotalFlightTime AS 'Total Time', f.Route, f.HobbsStart AS 'Hobbs Start', f.HobbsEnd AS 'Hobbs End', u.username AS 'Username', u.Firstname, u.LastName, u.Email, ac.Tailnumber AS 'Aircraft',  
fp.decValue AS 'Tach Start', fp2.decValue AS 'Tach End',
f.dtFlightStart AS 'Flight Start', f.dtFlightEnd AS 'Flight End', 
f.dtEngineStart AS 'Engine Start', f.dtEngineEnd AS 'Engine End',
IF (YEAR(f.dtFlightEnd) > 1 AND YEAR(f.dtFlightStart) > 1, (UNIX_TIMESTAMP(f.dtFlightEnd)-UNIX_TIMESTAMP(f.dtFlightStart))/3600, 0) AS 'Total Flight',
IF (YEAR(f.dtEngineEnd) > 1 AND YEAR(f.dtEngineStart) > 1, (UNIX_TIMESTAMP(f.dtEngineEnd)-UNIX_TIMESTAMP(f.dtEngineStart))/3600, 0) AS 'Total Engine',
GREATEST(CAST(f.HobbsEnd AS decimal(10, 2)) - CAST(f.HobbsStart AS decimal(10,2)), 0) AS 'Total Hobbs', 
fp2.decValue - fp.decValue AS 'Total Tach',
fp3.decValue AS 'Oil Added',
fp4.decValue AS 'Fuel Added',
fp5.decValue AS 'Fuel Cost',
fp6.decValue AS 'Oil Level',
fp7.decValue AS 'Oil Added 2nd',
fp8.decValue AS 'Fuel Remaining',
IF(fppart91.StringValue is null, if(fppart121.StringValue is null, if(fppart135.StringValue is null, '', 'Part 131'), 'Part 121'), 'Part 91') AS FlightRules,
COALESCE(fpPassCount.intValue, 0) as 'Passengers',
IF(f.dualReceived > 0 OR f.cfi > 0, true, false) AS IsInstruction
FROM flights f 
INNER JOIN clubmembers cm ON f.username = cm.username
INNER JOIN users u ON u.username=cm.username
INNER JOIN clubs c ON c.idclub=cm.idclub
INNER JOIN clubaircraft ca ON (ca.idaircraft=f.idaircraft AND c.idclub=ca.idclub)
INNER JOIN aircraft ac ON (ca.idaircraft=ac.idaircraft AND c.idclub=ca.idclub)
LEFT JOIN flightproperties fp on (fp.idflight=f.idflight AND fp.idproptype=95)
LEFT JOIN flightproperties fp2 on (fp2.idflight=f.idflight AND fp2.idproptype=96)
LEFT JOIN flightproperties fp3 on (fp3.idflight=f.idflight AND fp3.idproptype=365)
LEFT JOIN flightproperties fp4 on (fp4.idflight=f.idflight AND fp4.idproptype=94)
LEFT JOIN flightproperties fp5 on (fp5.idflight=f.idflight AND fp5.idproptype=662)
LEFT JOIN flightproperties fp6 on (fp6.idflight=f.idflight AND fp6.idproptype=650)
LEFT JOIN flightproperties fp7 on (fp7.idflight=f.idflight AND fp7.idproptype=418)
LEFT JOIN flightproperties fp8 on (fp8.idflight=f.idflight AND fp8.idproptype=72)
LEFT JOIN flightproperties fpPassCount on (fpPassCount.idflight=f.idflight and fpPassCount.idproptype=316)
LEFT JOIN flightproperties fppart91 on (fppart91.idflight=f.idflight AND fppart91.idproptype=155)
LEFT JOIN flightproperties fppart121 on (fppart121.idflight=f.idflight AND fppart121.idproptype=153)
LEFT JOIN flightproperties fppart135 on (fppart135.idflight=f.idflight AND fppart135.idproptype=154)
WHERE
c.idClub = ?idClub AND
f.date >= GREATEST(?startDate, cm.joindate, c.creationDate) AND
f.date <= ?endDate {0} {1}
ORDER BY f.DATE ASC";

            List<ClubFlyingReportItem> lst = new List<ClubFlyingReportItem>();
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szQTemplate, String.IsNullOrEmpty(szUser) ? string.Empty : " AND cm.username=?user ", idAircraft <= 0 ? string.Empty : " AND ca.idaircraft=?aircraftid"));
            dbh.ReadRows((comm) =>
            {
                comm.Parameters.AddWithValue("idclub", idClub);
                comm.Parameters.AddWithValue("startDate", dateStart);
                comm.Parameters.AddWithValue("endDate", dateEnd);
                if (!String.IsNullOrEmpty(szUser))
                    comm.Parameters.AddWithValue("user", szUser);
                if (idAircraft > 0)
                    comm.Parameters.AddWithValue("aircraftid", idAircraft);
            }, (dr) =>
            {
                lst.Add(new ClubFlyingReportItem()
                {
                    FlightID = Convert.ToInt32(dr["idflight"], CultureInfo.InvariantCulture),
                    FlightDate = Convert.ToDateTime(dr["Date"], CultureInfo.InvariantCulture),
                    Aircraft = (string)dr["Aircraft"],
                    PilotName = new Profile()
                    {
                        FirstName = (string)dr["Firstname"],
                        LastName = (string)dr["LastName"],
                        Email = (string)dr["Email"]
                    }.UserFullName,
                    Route = (string)dr["Route"],
                    IsInstruction = Convert.ToBoolean(dr["IsInstruction"], CultureInfo.InvariantCulture),
                    FlightRules = (string)dr["FlightRules"],
                    Passengers = Convert.ToInt32(dr["Passengers"], CultureInfo.InvariantCulture),
                    TotalTime = Convert.ToDecimal(dr["Total Time"], CultureInfo.InvariantCulture),
                    HobbsStart = Convert.ToDecimal(dr["Hobbs Start"], CultureInfo.InvariantCulture),
                    HobbsEnd = Convert.ToDecimal(dr["Hobbs End"], CultureInfo.InvariantCulture),
                    TotalHobbs = Convert.ToDecimal(dr["Total Hobbs"], CultureInfo.InvariantCulture),
                    TachStart = Convert.ToDecimal(util.ReadNullableField(dr, "Tach Start", 0.0M), CultureInfo.InvariantCulture),
                    TachEnd = Convert.ToDecimal(util.ReadNullableField(dr, "Tach End", 0.0M), CultureInfo.InvariantCulture),
                    TotalTach = Convert.ToDecimal(util.ReadNullableField(dr, "Total Tach", 0.0M), CultureInfo.InvariantCulture),
                    FlightStart = (dr["Flight Start"] == System.DBNull.Value) ? (DateTime?)null : Convert.ToDateTime(dr["Flight Start"], CultureInfo.InvariantCulture),
                    FlightEnd = (dr["Flight End"] == System.DBNull.Value) ? (DateTime?)null : Convert.ToDateTime(dr["Flight End"], CultureInfo.InvariantCulture),
                    TotalFlight = Convert.ToDecimal(dr["Total Flight"], CultureInfo.InvariantCulture),
                    EngineStart = (dr["Engine Start"] == System.DBNull.Value) ? (DateTime?)null : Convert.ToDateTime(dr["Engine Start"], CultureInfo.InvariantCulture),
                    EngineEnd = (dr["Engine End"] == System.DBNull.Value) ? (DateTime?)null : Convert.ToDateTime(dr["Engine End"], CultureInfo.InvariantCulture),
                    TotalEngine = Convert.ToDecimal(dr["Total Engine"], CultureInfo.InvariantCulture),
                    OilAdded = Convert.ToDecimal(util.ReadNullableField(dr, "Oil Added", 0.0M), CultureInfo.InvariantCulture),
                    OilAdded2 = Convert.ToDecimal(util.ReadNullableField(dr, "Oil Added 2nd", 0.0M), CultureInfo.InvariantCulture),
                    OilLevel = Convert.ToDecimal(util.ReadNullableField(dr, "Oil Level", 0.0M), CultureInfo.InvariantCulture),
                    FuelAdded = Convert.ToDecimal(util.ReadNullableField(dr, "Fuel Added", 0.0M), CultureInfo.InvariantCulture),
                    FuelRemaining = Convert.ToDecimal(util.ReadNullableField(dr, "Fuel Remaining", 0.0M), CultureInfo.InvariantCulture),
                    FuelCost = Convert.ToDecimal(util.ReadNullableField(dr, "Fuel Cost", 0.0M), CultureInfo.InvariantCulture)
                });
            });

            return lst;
        }
    }

    #region Reporting
    public abstract class ClubReport
    {
        protected int ClubID { get; set; }

        protected ClubReport(int idClub)
        {
            ClubID = idClub;
        }

        public abstract byte[] RefreshCSV();
    }

    public class ClubMaintenanceReport : ClubReport
    {
        public IEnumerable<ClubAircraft> Items
        {
            get
            {
                return Club.ClubWithID(ClubID, true).MemberAircraft;
            }
        }

        public ClubMaintenanceReport(int idClub) : base(idClub) { }

        public override byte[] RefreshCSV()
        {
            IEnumerable<ClubAircraft> rgItems = Items;
            using (DataTable dt = new DataTable() { Locale = CultureInfo.CurrentCulture })
            {
                // add the header columns from the gridview
                dt.Columns.Add(new DataColumn(Resources.Aircraft.AircraftHeader, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ClubAircraftTime, typeof(string)));
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
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderNotes, typeof(string)));

                foreach (ClubAircraft ca in rgItems)
                {
                    DataRow dr = dt.NewRow();

                    dr[0] = ca.DisplayTailnumber;
                    dr[1] = ca.HighWater.FormatDecimal(false);
                    dr[2] = (ca.LastAnnual.HasValue() ? ca.LastAnnual.ToShortDateString() : string.Empty);
                    dr[3] = (ca.Maintenance.NextAnnual.HasValue() ? ca.Maintenance.NextAnnual.ToShortDateString() : string.Empty);
                    dr[4] = (ca.LastTransponder.HasValue() ? ca.LastTransponder.ToShortDateString() : string.Empty);
                    dr[5] = (ca.Maintenance.NextTransponder.HasValue() ? ca.Maintenance.NextTransponder.ToShortDateString() : string.Empty);
                    dr[6] = (ca.LastAnnual.HasValue() ? ca.LastStatic.ToShortDateString() : string.Empty);
                    dr[7] = (ca.Maintenance.NextStatic.HasValue() ? ca.Maintenance.NextStatic.ToShortDateString() : string.Empty);
                    dr[8] = (ca.LastAnnual.HasValue() ? ca.LastAltimeter.ToShortDateString() : string.Empty);
                    dr[9] = (ca.Maintenance.NextAltimeter.HasValue() ? ca.Maintenance.NextAltimeter.ToShortDateString() : string.Empty);
                    dr[10] = (ca.LastAnnual.HasValue() ? ca.LastELT.ToShortDateString() : string.Empty);
                    dr[11] = (ca.Maintenance.NextELT.HasValue() ? ca.Maintenance.NextELT.ToShortDateString() : string.Empty);
                    dr[12] = (ca.LastVOR.HasValue() ? ca.LastVOR.ToShortDateString() : string.Empty);
                    dr[13] = (ca.Maintenance.NextVOR.HasValue() ? ca.Maintenance.NextVOR.ToShortDateString() : string.Empty);
                    dr[14] = ca.Last100.FormatDecimal(false);
                    dr[15] = ca.Maintenance.Next100.FormatDecimal(false);
                    dr[16] = ca.LastOilChange.FormatDecimal(false);
                    dr[17] = ca.Maintenance.LastOilChange.FormatDecimal(false);
                    dr[18] = ca.Maintenance.LastOilChange.FormatDecimal(false);
                    dr[19] = ca.Maintenance.LastOilChange.FormatDecimal(false);
                    dr[20] = ca.LastNewEngine.FormatDecimal(false);
                    dr[21] = (ca.RegistrationDue.HasValue() ? ca.RegistrationDue.ToShortDateString() : string.Empty);
                    dr[22] = DeadlineCurrency.CoalescedDeadlinesForAircraft(null, ca.AircraftID);
                    dr[23] = ca.PublicNotes;

                    dt.Rows.Add(dr);
                }

                return CsvWriter.WriteToBytes(dt, true, true);
            }
        }
    }

    public class ClubInsuranceReport : ClubReport
    {
        protected int MonthInterval { get; set; }
        public ClubInsuranceReport(int idClub, int monthInterval) : base(idClub)
        {
            MonthInterval = monthInterval;
        }

        public IEnumerable<ClubInsuranceReportItem> Items
        {
            get { return ClubInsuranceReportItem.ReportForClub(ClubID, MonthInterval); }
        }

        public override byte[] RefreshCSV()
        {
            IEnumerable<ClubInsuranceReportItem> rgItems = Items;
            using (DataTable dt = new DataTable() { Locale = CultureInfo.CurrentCulture })
            {
                // add the header columns from the gridview
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderPilotName, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderInsurancePilotStatus, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderInsuranceFlightsInPeriod, typeof(int)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderInsuranceLastFlightInClubPlane, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderInsuranceTotalTime, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportheaderInsuranceComplexTime, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportheaderInsuranceHighPerformance, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportheaderInsuranceTimeInClubAircraft, typeof(string)));

                foreach (ClubInsuranceReportItem item in rgItems)
                {
                    DataRow dr = dt.NewRow();

                    dr[0] = item.User.UserFullName;
                    StringBuilder sbStatus = new StringBuilder();
                    foreach (CurrencyStatusItem cs in item.PilotStatusItems)
                        sbStatus.AppendFormat(CultureInfo.CurrentCulture, "{0}: {1}\r\n\r\n", cs.Attribute, cs.Value);
                    dr[1] = sbStatus.ToString();
                    dr[2] = item.FlightsInInterval;
                    dr[3] = item.MostRecentFlight?.ToShortDateString() ?? string.Empty;
                    dr[4] = item.TotalTime.FormatDecimal(false, true);
                    dr[5] = item.ComplexTime.FormatDecimal(false, true);
                    dr[6] = item.HighPerformanceTime.FormatDecimal(false, true);

                    StringBuilder sbAircraftTime = new StringBuilder();
                    foreach (string key in item.TotalsByClubAircraft.Keys)
                        sbAircraftTime.AppendFormat(CultureInfo.CurrentCulture, "{0} {1}\r\n\r\n", key, item.TotalsByClubAircraft[key].FormatDecimal(false, true));
                    dr[7] = sbAircraftTime.ToString();

                    dt.Rows.Add(dr);
                }

                return CsvWriter.WriteToBytes(dt, true, true);
            }
        }
    }

    public class ClubFlyingReport : ClubReport
    {
        protected string User { get; set; }
        protected int AircraftID { get; set; }
        protected DateTime StartDate { get; set; }
        protected DateTime EndDate { get; set; }

        public ClubFlyingReport(int idClub, DateTime dateStart, DateTime dateEnd, string szUser, int idAircraft) : base(idClub)
        {
            User = szUser;
            StartDate = dateStart;
            EndDate = dateEnd;
            User = szUser;
            AircraftID = idAircraft;
        }

        public IEnumerable<ClubFlyingReportItem> Items
        {
            get
            {
                return ClubFlyingReportItem.ReportForClub(ClubID, StartDate, EndDate, User, AircraftID);
            }
        }

        public byte[] RefreshKML()
        {
            List<int> lstIds = new List<int>();
            foreach (ClubFlyingReportItem cfri in Items)
                lstIds.Add(cfri.FlightID);

            using (MemoryStream ms = new MemoryStream())
            {
                if (lstIds.Count == 0)
                {
                    using (Telemetry.KMLWriter kw = new Telemetry.KMLWriter(ms))
                    {
                        kw.BeginKML();
                        kw.EndKML();
                    }
                }
                else
                {
                    VisitedAirport.AllFlightsAsKML(new FlightQuery(), ms, out string szErr, lstIds);
                    if (!String.IsNullOrEmpty(szErr))
                        throw new MyFlightbookException("Error writing KML to stream: " + szErr);
                }
                return ms.ToArray();
            }
        }

        public override byte[] RefreshCSV()
        {
            IEnumerable<ClubFlyingReportItem> rgItems = Items;
            using (DataTable dt = new DataTable() { Locale = CultureInfo.CurrentCulture })
            {
                // add the header columns from the gridview
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderDate, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderMonth, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderAircraft, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderPilotName, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderInstruction, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderFlightRules, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderPaxCount, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderRoute, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderTotalTime, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderHobbsStart, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderHobbsEnd, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderTotalHobbs, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderTachStart, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderTachEnd, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderTotalTach, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderFlightStart, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderFlightEnd, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderTotalFlight, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderEngineStart, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderEngineEnd, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderTotalEngine, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderOilAdded, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderOilAdded2ndEngine, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderOilLevel, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderFuelAdded, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderFuelRemaining, typeof(string)));
                dt.Columns.Add(new DataColumn(Resources.Club.ReportHeaderFuelCost, typeof(string)));

                foreach (ClubFlyingReportItem item in rgItems)
                {
                    DataRow dr = dt.NewRow();

                    dr[0] = item.FlightDate.ToShortDateString();
                    dr[1] = String.Format(CultureInfo.InvariantCulture, "{0}-{1} ({2})", item.FlightDate.Year, item.FlightDate.Month.ToString("00", CultureInfo.InvariantCulture), item.FlightDate.ToString("MMM", CultureInfo.CurrentCulture));
                    dr[2] = item.Aircraft;
                    dr[3] = item.PilotName;
                    dr[4] = (item.IsInstruction ? 1 : 0).FormatBooleanInt();
                    dr[5] = item.FlightRules;
                    dr[6] = item.Passengers.FormatInt();
                    dr[7] = item.Route;
                    dr[8] = item.TotalTime.FormatDecimal(false);
                    dr[9] = item.HobbsStart.FormatDecimal(false);
                    dr[10] = item.HobbsEnd.FormatDecimal(false);
                    dr[11] = item.TotalHobbs.FormatDecimal(false);
                    dr[12] = item.TachStart.FormatDecimal(false);
                    dr[13] = item.TachEnd.FormatDecimal(false);
                    dr[14] = item.TotalTach.FormatDecimal(false);
                    dr[15] = item.FlightStart.HasValue ? item.FlightStart.Value.UTCFormattedStringOrEmpty(false) : string.Empty;
                    dr[16] = item.FlightEnd.HasValue ? item.FlightEnd.Value.UTCFormattedStringOrEmpty(false) : string.Empty; ;
                    dr[17] = item.TotalFlight.FormatDecimal(false);
                    dr[18] = item.EngineStart.HasValue ? item.EngineStart.Value.UTCFormattedStringOrEmpty(false) : string.Empty; ;
                    dr[19] = item.EngineEnd.HasValue ? item.EngineEnd.Value.UTCFormattedStringOrEmpty(false) : string.Empty; ;
                    dr[20] = item.TotalEngine.FormatDecimal(false);
                    dr[21] = item.OilAdded.FormatDecimal(false);
                    dr[22] = item.OilAdded2.FormatDecimal(false);
                    dr[23] = item.OilLevel.FormatDecimal(false);
                    dr[24] = item.FuelAdded.FormatDecimal(false);
                    dr[25] = item.FuelRemaining.FormatDecimal(false);
                    dr[26] = item.FuelCost == 0 ? string.Empty : item.FuelCost.ToString("C", CultureInfo.CurrentCulture);

                    dt.Rows.Add(dr);
                }

                return CsvWriter.WriteToBytes(dt, true, true);
            }
        }
    }
    #endregion

    public interface IReportable
    {
        void Refresh(int clubID);
    }
}