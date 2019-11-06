using Andri.Web;
using DotNetOpenAuth.OAuth2;
using MyFlightbook.Achievements;
using MyFlightbook.CloudStorage;
using MyFlightbook.Encryptors;
using MyFlightbook.FlightCurrency;
using MyFlightbook.Image;
using MyFlightbook.Telemetry;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using System.Web.Security;

/******************************************************
 * 
 * Copyright (c) 2009-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    public static class ProfileRoles
    {
        [FlagsAttribute]
        public enum UserRole
        {
            None = 0x0000,
            Support = 0x0001,
            DataManager = 0x0002,
            Reporter = 0x0004,
            Accountant = 0x0008,
            SiteAdmin = 0x0010
        };

        #region Bitmasks for roles
        public const uint maskCanManageData = ((uint)UserRole.SiteAdmin | (uint)UserRole.DataManager);
        public const uint maskCanReport = ((uint)UserRole.SiteAdmin | (uint)UserRole.Reporter);
        public const uint maskSiteAdminOnly = (uint)UserRole.SiteAdmin;
        public const uint maskCanManageMoney = ((uint)UserRole.SiteAdmin | (uint)UserRole.Accountant);
        public const uint maskCanSupport = ((uint)UserRole.SiteAdmin | (uint)UserRole.Support | (uint)UserRole.DataManager); // reporters cannot support
        public const uint maskCanContact = ((uint)UserRole.SiteAdmin | (uint)UserRole.Support);
        #endregion

        #region Helper routines for roles
        static public bool CanSupport(UserRole r)
        {
            return ((uint)r & maskCanSupport) != 0;
        }

        static public bool CanManageData(UserRole r)
        {
            return ((uint)r & maskCanManageData) != 0;
        }

        static public bool CanReport(UserRole r)
        {
            return ((uint)r & maskCanReport) != 0;
        }

        static public bool CanManageMoney(UserRole r)
        {
            return ((uint)r & maskCanManageMoney) != 0;
        }

        static public bool CanDoSomeAdmin(UserRole r)
        {
            return r != UserRole.None;
        }

        static public bool CanDoAllAdmin(UserRole r)
        {
            return r == UserRole.SiteAdmin;
        }
        #endregion

        /// <summary>
        /// Returns all users on the site who have some sort of admin privileges.
        /// </summary>
        /// <returns>A list of profile objects</returns>
        static public IEnumerable<ProfileBase> GetNonUsers()
        {
            List<Profile> lst = new List<Profile>();
            DBHelper dbh = new DBHelper("SELECT uir.Rolename AS Role, u.* FROM usersinroles uir INNER JOIN users u ON uir.username=u.username WHERE uir.ApplicationName='Logbook' AND uir.Rolename <> ''");
            dbh.ReadRows((comm) => { },
                (dr) => { lst.Add(new Profile(dr)); });
            return lst;
        }

        #region Impersonation
        /// <summary>
        /// Returns the username of the user who might be doing any impersonation.
        /// </summary>
        public static string OriginalAdmin
        {
            get { return (HttpContext.Current.Request.Cookies[MFBConstants.keyOriginalID] != null) ? HttpContext.Current.Request.Cookies[MFBConstants.keyOriginalID].Value : string.Empty; }
        }

        /// <summary>
        /// Returns true if the current user is being impersonated
        /// </summary>
        /// <param name="szCurrentUser">The username of the current user</param>
        /// <returns>True if the current user is being emulated by an admin</returns>
        public static bool IsImpersonating(string szCurrentUser)
        {
            bool fIsImpersonatingCookie = false;
            HttpCookie cookie = HttpContext.Current.Request.Cookies[MFBConstants.keyIsImpersonating];
            if (cookie != null)
            {
                if (!Boolean.TryParse(cookie.Value, out fIsImpersonatingCookie))
                    HttpContext.Current.Response.Cookies[MFBConstants.keyIsImpersonating].Expires = DateTime.Now.AddDays(-1);
            }

            // to be impersonating, must be both an admin and have the impersonating cookie set and be impersonating someone other than yourself.
            string szOriginalAdmin = OriginalAdmin;
            return fIsImpersonatingCookie && String.Compare(szOriginalAdmin, szCurrentUser, StringComparison.Ordinal) != 0 && Profile.GetUser(szOriginalAdmin).CanSupport;
        }

        /// <summary>
        /// If currently impersonating, stops the impersonation
        /// </summary>
        public static void StopImpersonating()
        {
            if (HttpContext.Current.Request.Cookies[MFBConstants.keyIsImpersonating] != null)
                HttpContext.Current.Response.Cookies[MFBConstants.keyIsImpersonating].Expires = DateTime.Now.AddDays(-1);
            if (HttpContext.Current.Response.Cookies[MFBConstants.keyOriginalID] != null)
            {
                string szUser = HttpContext.Current.Request.Cookies[MFBConstants.keyOriginalID].Value;
                MembershipUser mu = (szUser == null) ? null : Membership.GetUser(szUser);
                if (mu != null && mu.UserName.Length > 0)
                    FormsAuthentication.SetAuthCookie(HttpContext.Current.Request.Cookies[MFBConstants.keyOriginalID].Value, true);
                else
                    FormsAuthentication.SignOut();
                HttpContext.Current.Response.Cookies[MFBConstants.keyOriginalID].Expires = DateTime.Now.AddDays(-1);
            }
        }

        /// <summary>
        /// Starts impersonating the specified person
        /// </summary>
        /// <param name="szAdminName">The admin name to impersonate</param>
        /// <param name="szTargetName">The impersonation target</param>
        public static void ImpersonateUser(string szAdminName, string szTargetName)
        {
            HttpContext.Current.Response.Cookies[MFBConstants.keyOriginalID].Value = szAdminName;
            HttpContext.Current.Response.Cookies[MFBConstants.keyOriginalID].Expires = DateTime.Now.AddDays(30);
            FormsAuthentication.SetAuthCookie(szTargetName, true);
            HttpContext.Current.Response.Cookies[MFBConstants.keyIsImpersonating].Value = true.ToString();
            HttpContext.Current.Response.Cookies[MFBConstants.keyIsImpersonating].Expires = DateTime.Now.AddDays(30);
        }
        #endregion
    }

    /// <summary>
    /// Encapsulates a user of the system.  This is the base class.
    /// </summary>
    [Serializable]
    public abstract class ProfileBase
    {
        #region creation/initialization
        protected ProfileBase()
        {
            this.Certificate = this.Email = this.FirstName = this.LastName = this.OriginalPKID = this.OriginalEmail = this.PKID =
                this.SecurityQuestion = this.UserName = this.License = this.Address = string.Empty;
            Role = ProfileRoles.UserRole.None;
            BlacklistedProperties = new List<int>();
            LastBFRInternal = LastMedical = CertificateExpiration = EnglishProficiencyExpiration = LastEmailDate = DateTime.MinValue;
            AssociatedData = new Dictionary<string, object>();
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} - (\"{1}\" <{2}>)", UserName, UserFullName, Email);
        }

        #region Admin Roles
        /// <summary>
        /// The role for the user
        /// </summary>
        public ProfileRoles.UserRole Role { get; set; }

        /// <summary>
        /// Does this user have access to at least one admin role?
        /// </summary>
        public bool CanDoSomeAdmin { get { return ProfileRoles.CanDoSomeAdmin(Role); } }

        /// <summary>
        /// Does this user have access to ALL admin roles?
        /// </summary>
        public bool CanDoAllAdmin { get { return ProfileRoles.CanDoAllAdmin(Role); } }

        /// <summary>
        /// Can this user provide customer support?
        /// </summary>
        public bool CanSupport { get { return ProfileRoles.CanSupport(Role); } }

        /// <summary>
        /// Can this user manage data?
        /// </summary>
        public bool CanManageData { get { return ProfileRoles.CanManageData(Role); } }

        /// <summary>
        /// Can this user manage money?
        /// </summary>
        public bool CanManageMoney { get { return ProfileRoles.CanManageMoney(Role); } }

        /// <summary>
        /// Can this user see reports?
        /// </summary>
        public bool CanReport { get { return ProfileRoles.CanReport(Role); } }
        #endregion

        #region Properties
        /// <summary>
        /// Should hobbs/engine/flight times be expanded by default?
        /// </summary>
        public Boolean DisplayTimesByDefault { get; set; }

        /// <summary>
        /// Security question for user
        /// </summary>
        public string SecurityQuestion { get; set; }

        /// <summary>
        /// Bitflag for which currencies the user does/does not want
        /// </summary>
        public CurrencyOptionFlag CurrencyFlags { get; set; }

        private void setCurrencyFlag(CurrencyOptionFlag cof, bool value)
        {
            CurrencyFlags = (CurrencyOptionFlag)((value) ? (((uint)CurrencyFlags) | (uint)cof) : (((uint)CurrencyFlags) & ~(uint)cof));
        }

        private bool hasFlag(CurrencyOptionFlag cof)
        {
            return ((uint)CurrencyFlags & (uint)cof) != 0;
        }

        /// <summary>
        /// Does this user use military (AR 95-1) currency?
        /// </summary>
        public Boolean UsesArmyCurrency
        {
            get { return hasFlag(CurrencyOptionFlag.flagArmyMDSCurrency); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagArmyMDSCurrency, value); }
        }

        /// <summary>
        /// Does this user use per-model currency (i.e., currency for each make/model they fly?)
        /// </summary>
        public Boolean UsesPerModelCurrency
        {
            get { return hasFlag(CurrencyOptionFlag.flagPerModelCurrency); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagPerModelCurrency, value); }
        }

        /// <summary>
        /// Does this user show totals per-model?
        /// </summary>
        private Boolean UsesPerModelTotals
        {
            get { return hasFlag(CurrencyOptionFlag.flagShowTotalsPerModel); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagShowTotalsPerModel, value); }
        }

        /// <summary>
        /// Does this user show totals per-family?
        /// </summary>
        private Boolean UsesPerFamilyTotals
        {
            get { return hasFlag(CurrencyOptionFlag.flagsShowTotalsPerFamily); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagsShowTotalsPerFamily, value); }
        }

        /// <summary>
        /// How are totals done for this user?
        /// </summary>
        public TotalsGrouping TotalsGroupingMode
        {
            get { return UsesPerFamilyTotals ? TotalsGrouping.Family : (UsesPerModelTotals ? TotalsGrouping.Model : TotalsGrouping.CatClass); }
            set
            {
                switch (value)
                {
                    case TotalsGrouping.CatClass:
                        UsesPerFamilyTotals = false;
                        UsesPerModelTotals = false;
                        break;
                    case TotalsGrouping.Model:
                        UsesPerFamilyTotals = false;
                        UsesPerModelTotals = true;
                        break;
                    case TotalsGrouping.Family:
                        UsesPerFamilyTotals = true;
                        UsesPerModelTotals = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Suppress aircraft characteristics in totals (complex, turbine, tailwheel, etc.)
        /// </summary>
        public Boolean SuppressModelFeatureTotals
        {
            get { return hasFlag(CurrencyOptionFlag.flagSuppressModelFeatureTotals); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagSuppressModelFeatureTotals, value); }
        }

        /// <summary>
        /// Does this user use FAR 117-mandated rest periods?
        /// </summary>
        public Boolean UsesFAR117DutyTime
        {
            get { return hasFlag(CurrencyOptionFlag.flagFAR117DutyTimeCurrency); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagFAR117DutyTimeCurrency, value); }
        }

        /// <summary>
        /// If the user  uses FAR 117 mandated duty times, does it apply to all flights or only flights with a specified duty time?
        /// </summary>
        public Boolean UsesFAR117DutyTimeAllFlights
        {
            get { return hasFlag(CurrencyOptionFlag.flagFAR117IncludeAllFlights); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagFAR117IncludeAllFlights, value); }
        }

        /// <summary>
        /// Does this user use FAR 135-mandated rest periods?
        /// </summary>
        public Boolean UsesFAR135DutyTime
        {
            get { return hasFlag(CurrencyOptionFlag.flagFAR135DutyTimeCurrency); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagFAR135DutyTimeCurrency, value); }
        }

        public Boolean UsesFAR13529xCurrency
        {
            get { return hasFlag(CurrencyOptionFlag.flagUseFAR135_29xStatus); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagUseFAR135_29xStatus, value); }
        }

        public Boolean UsesFAR13526xCurrency
        {
            get { return hasFlag(CurrencyOptionFlag.flagUseFAR135_26xStatus); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagUseFAR135_26xStatus, value); }
        }

        public Boolean UsesFAR61217Currency
        {
            get { return hasFlag(CurrencyOptionFlag.flagUseFAR61217); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagUseFAR61217, value); }
        }

        /// <summary>
        /// Specifies whether to use ICAO (day-for-day) medical expiration or FAA (calendar month) expiraiton
        /// </summary>
        public Boolean UsesICAOMedical
        {
            get { return hasFlag(CurrencyOptionFlag.flagUseEASAMedical); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagUseEASAMedical, value); }
        }

        /// <summary>
        /// Use loose 61.57(c)(4) interpretation (i.e., mix/match of ATD/FTD/Real)
        /// </summary>
        public Boolean UsesLooseIFRCurrency
        {
            get { return hasFlag(CurrencyOptionFlag.flagUseLooseIFRCurrency); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagUseLooseIFRCurrency, value); }
        }

        /// <summary>
        /// Should we use Canadian style currency rules?  See http://laws-lois.justice.gc.ca/eng/regulations/SOR-96-433/FullText.html#s-401.05 and https://www.tc.gc.ca/eng/civilaviation/publications/tp185-1-10-takefive-559.htm
        /// </summary>
        public Boolean UseCanadianCurrencyRules
        {
            get { return hasFlag(CurrencyOptionFlag.flagUseCanadianCurrencyRules); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagUseCanadianCurrencyRules, value); }
        }

        /// <summary>
        /// Should we use LAPL currency rules?  See https://www.caa.co.uk/General-aviation/Pilot-licences/EASA-requirements/LAPL/LAPL-(A)-requirements/
        /// </summary>
        public Boolean UsesLAPLCurrency
        {
            get { return hasFlag(CurrencyOptionFlag.flagUseLAPLCurrency); }
            set { setCurrencyFlag(CurrencyOptionFlag.flagUseLAPLCurrency, value); }
        }

        /// <summary>
        /// Expiration for expired currency
        /// </summary>
        public CurrencyExpiration.Expiration CurrencyExpiration
        {
            get
            {
                return (CurrencyExpiration.Expiration)(((uint)CurrencyFlags & (uint)CurrencyOptionFlag.flagCurrencyExpirationMask) >> 4);
            }
            set
            {
                uint newMask = (uint)value << 4;
                CurrencyFlags = (CurrencyOptionFlag)(((uint)CurrencyFlags & ~(uint)CurrencyOptionFlag.flagCurrencyExpirationMask) | newMask);
            }
        }

        /// <summary>
        /// True if this user prefers HH:MM formats rather than decimal for recording times.
        /// </summary>
        public Boolean UsesHHMM { get; set; }

        /// <summary>
        /// Is the date of flight the local date or the UTC date of the flight start?
        /// </summary>
        public Boolean UsesUTCDateOfFlight { get; set; }

        /// <summary>
        /// True if this user tracks 2nd in command time.
        /// </summary>
        public Boolean TracksSecondInCommandTime { get; set; }

        /// <summary>
        /// Unique id for the user.  Don't set this; it's only public for serialization/object copy.
        /// </summary>
        public string PKID { get; set; }

        /// <summary>
        /// Username for the user
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Detailed name - username, display name, and email
        /// </summary>
        public string DetailedName
        {
            get { return String.Format(CultureInfo.InvariantCulture, "{0} ({1} <{2}>)", UserName, UserFullName, Email); }
        }

        /// <summary>
        /// True if user sometimes logs instructor time
        /// </summary>
        public Boolean IsInstructor { get; set; }

        /// <summary>
        /// # of calendar months (6, 12, 24, or 36) months to medical.
        /// </summary>
        public int MonthsToMedical { get; set; }

        /// <summary>
        /// Date that the most recent subscribed email was sent to this user
        /// </summary>
        public DateTime LastEmailDate { get; set; }

        /// <summary>
        /// Email to which this user is subscribed.
        /// </summary>
        public UInt32 Subscriptions { get; set; }

        /// <summary>
        /// Determines if a user is subscribed to a given feed.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public bool IsSubscribed(UInt32 f)
        {
            return ((Subscriptions & f) != 0);
        }

        /// <summary>
        /// Date of last BFR - DEPRECATED - DO NOT USE THIS 
        /// </summary>
        protected DateTime LastBFRInternal { get; set; }

        /// <summary>
        /// Date of last medical
        /// </summary>
        public DateTime LastMedical { get; set; }

        /// <summary>
        /// First name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Surname
        /// </summary>
        public string LastName { get; set; }

        #region cloud storage
        /// <summary>
        /// The drop-box access token
        /// </summary>
        public string DropboxAccessToken { get; set; }

        /// <summary>
        /// When saving to the cloud, overwrite the existing file? (vs. appending date)
        /// </summary>
        public bool OverwriteCloudBackup { get; set; }

        /// <summary>
        /// The OneDrive access token
        /// </summary>
        public IAuthorizationState OneDriveAccessToken { get; set; }

        /// <summary>
        /// The Google Drive access token
        /// </summary>
        public IAuthorizationState GoogleDriveAccessToken { get; set; }

        /// <summary>
        /// The ICloud Drive access token
        /// </summary>
        public IAuthorizationState ICloudDriveAccessToken { get; set; }

        /// <summary>
        /// The default cloud storage provider to use, if multiple are specified.
        /// </summary>
        public StorageID DefaultCloudStorage { get; set; }

        /// <summary>
        /// Returns the available cloud providers for this user, in priority order
        /// </summary>
        public IEnumerable<StorageID> AvailableCloudProviders
        {
            get
            {
                List<StorageID> lst = new List<StorageID>();

                if (!String.IsNullOrEmpty(DropboxAccessToken))
                    lst.Add(StorageID.Dropbox);
                if (OneDriveAccessToken != null && OneDriveAccessToken.RefreshToken != null)
                    lst.Add(StorageID.OneDrive);
                if (GoogleDriveAccessToken != null && GoogleDriveAccessToken.RefreshToken != null)
                    lst.Add(StorageID.GoogleDrive);
                if (ICloudDriveAccessToken != null && ICloudDriveAccessToken.RefreshToken != null)
                    lst.Add(StorageID.iCloud);
                return lst;
            }
        }

        /// <summary>
        /// What is the best Cloud Storage to use?  Uses the preferred one ("DefaultCloudStoratge") if possible, else uses the first one found.
        /// </summary>
        public StorageID BestCloudStorage
        {
            get
            {
                List<StorageID> available = new List<StorageID>(AvailableCloudProviders);

                if (available.Contains(DefaultCloudStorage))
                    return DefaultCloudStorage;
                else if (available.Count() > 0)
                    return available[0];
                else 
                    return StorageID.None;
            }
        }
        #endregion

        /// <summary>
        /// oAuth token for CloudAhoy
        /// </summary>
        public IAuthorizationState CloudAhoyToken { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// The original email address for the user, so that we can tell if it has changed.
        /// </summary>
        protected string OriginalEmail { get; set; }

        /// <summary>
        /// The original PKID for the user, so that we can tell if it has changed.
        /// </summary>
        protected string OriginalPKID { get; set; }

        /// <summary>
        /// The pilot certificate - used for CFIs.
        /// </summary>
        public String Certificate { get; set; }

        /// <summary>
        /// Date that the certificate expires
        /// </summary>
        public DateTime CertificateExpiration { get; set; }

        public string LicenseDisplay
        {
            get { return String.IsNullOrEmpty(License.Trim()) ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Profile.LicenseTemplate, License); }
        }

        public string CFIDisplay
        {
            get { return String.IsNullOrEmpty(Certificate.Trim()) ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Profile.CFILicenseTemplate, Certificate, CertificateExpiration.HasValue() ? CertificateExpiration.ToShortDateString() : Resources.Profile.CFIExpirationUnknown); }
        }

        /// <summary>
        /// Date that the English proficiency expires.
        /// </summary>
        public DateTime EnglishProficiencyExpiration { get; set; }

        /// <summary>
        /// Gets just the first name for the logged in user, logged in user name if none
        /// </summary>
        /// <param name="szUser">Logged in user name</param>
        /// <returns>First name</returns>
        public string UserFirstName
        {
            get { return GetUserName(true, false); }
        }

        /// <summary>
        /// Gets just the last name for the logged in user, logged in user name if none
        /// </summary>
        /// <param name="szUser">Logged in user name</param>
        /// <returns>Last name</returns>
        public string UserLastName
        {
            get { return GetUserName(false, true); }
        }

        /// <summary>
        /// Gets the full name for the logged in user, logged in user name if none.
        /// </summary>
        /// <param name="szUser">Logged in user name</param>
        /// <returns>Full name</returns>
        public string UserFullName
        {
            get { return GetUserName(true, true); }
        }

        /// <summary>
        /// The license number for the user
        /// </summary>
        public string License { get; set; }

        /// <summary>
        /// The address for the user
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// List of properties ID's that the user has blacklisted from the previously-used list.
        /// </summary>
        public List<int> BlacklistedProperties { get; set; }

        private Achievement.ComputeStatus m_AchievementStatus = Achievement.ComputeStatus.Never;

        /// <summary>
        /// Current status of achievement computation for the user.  
        /// </summary>
        public Achievement.ComputeStatus AchievementStatus
        {
            get { return m_AchievementStatus; }
            set { m_AchievementStatus = value; }
        }

        /// <summary>
        /// What time zone do they prefer for date/time?  CAN BE NULL!
        /// </summary>
        public string PreferredTimeZoneID { get; set; }

        public TimeZoneInfo PreferredTimeZone
        {
            get
            {
                if (String.IsNullOrEmpty(PreferredTimeZoneID))
                    return TimeZoneInfo.Utc;
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(PreferredTimeZoneID) ?? TimeZoneInfo.Utc;
                }
                catch
                {
                    return TimeZoneInfo.Utc;
                }
            }
        }

        /// <summary>
        /// Convenience dictionary of associated data for other that want to piggy back on Profile caching.
        /// </summary>
        public IDictionary<string, object> AssociatedData { get; set; }
        #endregion

        /// <summary>
        /// Get the full name for a user, not just the username
        /// </summary>
        /// <param name="szUser">Username of the requested user</param>
        /// <param name="fFirst">Whether to retrieve the first name</param>
        /// <param name="fLast">Whether to retrieve the last name</param>
        /// <returns>A string containing the full name for the user (first/last name), if available; else szUser</returns>
        private string GetUserName(Boolean fFirst, Boolean fLast)
        {
            return ProfileBase.UserNameFromFields(Email, FirstName, LastName, fFirst, fLast);
        }

        public static string UserNameFromFields(string szEmail, string szFirst, string szLast, bool fFirst, bool fLast)
        {
            string szFullName = (((fFirst) ? szFirst + Resources.LocalizedText.LocalizedSpace : string.Empty) + ((fLast) ? szLast : string.Empty)).Trim();
            if (szFullName.Length > 0)
                return szFullName;
            else
            {
                if (szEmail == null)
                    throw new ArgumentNullException("szEmail");
                // No first or last name, so use the email address to the left of the @ sign.
                int i = szEmail.IndexOf('@');

                if (i > 0) // should always be
                    szEmail = szEmail.Substring(0, i);
                return szEmail;
            }
        }

        #region Associated data helpers
        /// <summary>
        /// Safe way to get a cached object (i.e., can return null)
        /// </summary>
        /// <param name="szKey"></param>
        /// <returns></returns>
        public object CachedObject(string szKey)
        {
            object o;
            return (AssociatedData.TryGetValue(szKey, out o)) ? o : null;
        }
        #endregion
    }

    /// <summary>
    /// Intermediate class: ProfileBase, but with database and validation capabilities.
    /// </summary>
    [Serializable]
    public abstract class PersistedProfile : ProfileBase
    {
        protected PersistedProfile() : base()
        {

        }

        /// <summary>
        /// De-serializes an AuthorizationState from a JSON string, null if the string is null
        /// </summary>
        /// <param name="sz">The JSON string</param>
        /// <returns>The authorization state, or null if sz is null</returns>
        private AuthorizationState AuthStateFromString(string sz)
        {
            if (String.IsNullOrEmpty(sz))
                return null;

            if (!sz.StartsWith("{", StringComparison.Ordinal))    // legacy authtoken
            {
                return new AuthorizationState() { AccessToken = sz, AccessTokenExpirationUtc = null, AccessTokenIssueDateUtc = null, RefreshToken = null };
            }

            return JsonConvert.DeserializeObject<AuthorizationState>(sz);
        }

        #region Database and caching
        protected void InitFromDataReader(MySqlDataReader dr)
        {
            try
            {
                UserName = dr["Username"].ToString();
                OriginalEmail = Email = dr["Email"].ToString();
                FirstName = dr["FirstName"].ToString();
                LastName = dr["LastName"].ToString();
                Address = util.ReadNullableField(dr, "Address", string.Empty).ToString();
                OriginalPKID = PKID = dr["PKID"].ToString();

                DropboxAccessToken = (string)util.ReadNullableField(dr, "DropboxAccessToken", null);
                GoogleDriveAccessToken = AuthStateFromString((string) util.ReadNullableField(dr, "GoogleDriveAccessToken", null));
                OneDriveAccessToken = AuthStateFromString((string)util.ReadNullableField(dr, "OnedriveAccessToken", null));
                ICloudDriveAccessToken = AuthStateFromString((string)util.ReadNullableField(dr, "ICloudAccessToken", null));
                DefaultCloudStorage = (StorageID)Convert.ToInt32(dr["DefaultCloudDriveID"], CultureInfo.InvariantCulture);
                OverwriteCloudBackup = Convert.ToBoolean(dr["OverwriteDropbox"], CultureInfo.InvariantCulture);

                CloudAhoyToken = AuthStateFromString((string)util.ReadNullableField(dr, "CloudAhoyAccessToken", null));

                LastBFRInternal = Convert.ToDateTime(util.ReadNullableField(dr, "LastBFR", DateTime.MinValue), CultureInfo.InvariantCulture);
                LastMedical = Convert.ToDateTime(util.ReadNullableField(dr, "LastMedical", DateTime.MinValue), CultureInfo.InvariantCulture);
                EnglishProficiencyExpiration = Convert.ToDateTime(util.ReadNullableField(dr, "EnglishProficiencyExpiration", DateTime.MinValue), CultureInfo.InvariantCulture);
                MonthsToMedical = Convert.ToInt32(dr["MonthsToMedical"], CultureInfo.InvariantCulture);
                IsInstructor = Convert.ToBoolean(dr["IsInstructor"], CultureInfo.InvariantCulture);
                DisplayTimesByDefault = Convert.ToBoolean(dr["ShowTimes"], CultureInfo.InvariantCulture);
                TracksSecondInCommandTime = Convert.ToBoolean(dr["UsesSIC"], CultureInfo.InvariantCulture);
                UsesHHMM = Convert.ToBoolean(dr["UsesHHMM"], CultureInfo.InvariantCulture);
                UsesUTCDateOfFlight = Convert.ToBoolean(dr["UsesUTCDates"], CultureInfo.InvariantCulture);
                License = util.ReadNullableField(dr, "License", string.Empty).ToString();
                Certificate = util.ReadNullableField(dr, "CertificateNumber", "").ToString();
                CertificateExpiration = Convert.ToDateTime(util.ReadNullableField(dr, "CFIExpiration", DateTime.MinValue), CultureInfo.InvariantCulture);
                CurrencyFlags = (CurrencyOptionFlag)Convert.ToUInt32(dr["CurrencyFlags"], CultureInfo.InvariantCulture);
                SecurityQuestion = dr["PasswordQuestion"].ToString();

                Subscriptions = (UInt32)Convert.ToInt32(dr["EmailSubscriptions"], CultureInfo.InvariantCulture);
                LastEmailDate = Convert.ToDateTime(util.ReadNullableField(dr, "LastEmail", DateTime.MinValue), CultureInfo.InvariantCulture);

                AchievementStatus = (Achievement.ComputeStatus)Convert.ToInt16(dr["AchievementStatus"], CultureInfo.InvariantCulture);

                // IsAdmin may not always be present; default to false if not.  This is a bit of a hack.
                ProfileRoles.UserRole r = Role = ProfileRoles.UserRole.None;
                if (Enum.TryParse<ProfileRoles.UserRole>(util.ReadNullableField(dr, "Role", ProfileRoles.UserRole.None.ToString()).ToString(), out r))
                    Role = r;

                PreferredTimeZoneID = (string) util.ReadNullableField(dr, "timezone", null);

                string szBlacklist = util.ReadNullableString(dr, "PropertyBlackList");
                BlacklistedProperties = new List<int>(szBlacklist.ToInts());
            }
            catch (Exception ex)
            {
                throw new MyFlightbookException("Error reading field - " + ex.Message);
            }
        }

        protected static string GetCacheKey(string szUser)
        {
            return "Profile" + szUser.ToUpper(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Checks whether the profile is in a valid state for saving
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            if (String.IsNullOrWhiteSpace(UserName))
                return false;

            // Must have a PKID, must not have changed from loading
            if (PKID.Length == 0 || String.Compare(PKID, OriginalPKID, StringComparison.Ordinal) != 0)
                return false;

            // Changing email address is fine IF you're not stepping on another user.
            if (String.Compare(Email, OriginalEmail, StringComparison.OrdinalIgnoreCase) != 0)
            {
                if (Membership.GetUserNameByEmail(Email).Length > 0) // i.e., there exists a user with this email address already
                    return false;
            }

            return true;
        }

        protected static void CacheProfile(PersistedProfile pf)
        {
            if (pf == null || String.IsNullOrEmpty(pf.UserName))
                return;

            // Cache this for 30 minutes
            HttpRuntime.Cache.Add(GetCacheKey(pf.UserName), pf, null, DateTime.Now.AddMinutes(30), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
        }

        public Boolean FCommit()
        {
            string szQ = "";

            if (!IsValid())
                return false;

            szQ = @"UPDATE users SET Email=?Email, FirstName=?FirstName, LastName=?LastName, Address=?address, 
            DropboxAccessToken=?dropboxAccesstoken, OnedriveAccessToken=?onedrive, GoogleDriveAccessToken=?gdrive, ICloudAccessToken=?icloud, DefaultCloudDriveID=?defcloud, OverwriteDropbox=?overwriteCloud, CloudAhoyAccessToken=?cloudAhoy,
            LastBFR = ?LastBFR, LastMedical=?LastMedical, MonthsToMedical=?MonthsToMedical, IsInstructor=?IsInstructor, UsesSIC=?UsesSIC, UsesHHMM=?UsesHHMM, UsesUTCDates=?useUTCDates, License=?license, CertificateNumber=?cert, CFIExpiration=?cfiExp, 
            CurrencyFlags=?currencyFlags, ShowTimes=?showTimes, EnglishProficiencyExpiration=?engProfExpiration, EmailSubscriptions=?subscriptions, LastEmail=?lastemail, AchievementStatus=?achievementstatus, PropertyBlackList=?blacklist, timezone=?prefTimeZone
            WHERE PKID = ?PKID";

            string szErr = "";
            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery(szQ,
                (comm) =>
                {
                    comm.Parameters.AddWithValue("Email", Email);
                    comm.Parameters.AddWithValue("FirstName", FirstName.LimitTo(15));
                    comm.Parameters.AddWithValue("LastName", LastName.LimitTo(15));
                    comm.Parameters.AddWithValue("address", Address.LimitTo(254));
                    comm.Parameters.AddWithValue("dropboxAccessToken", DropboxAccessToken);
                    comm.Parameters.AddWithValue("onedrive", OneDriveAccessToken == null ? null : JsonConvert.SerializeObject(OneDriveAccessToken));
                    comm.Parameters.AddWithValue("gdrive", GoogleDriveAccessToken == null ? null : JsonConvert.SerializeObject(GoogleDriveAccessToken));
                    comm.Parameters.AddWithValue("icloud", ICloudDriveAccessToken == null ? null : JsonConvert.SerializeObject(ICloudDriveAccessToken));
                    comm.Parameters.AddWithValue("defcloud", (int)DefaultCloudStorage);
                    comm.Parameters.AddWithValue("overwriteCloud", OverwriteCloudBackup ? 1 : 0);
                    comm.Parameters.AddWithValue("cloudAhoy", CloudAhoyToken == null ? null : JsonConvert.SerializeObject(CloudAhoyToken));
                    comm.Parameters.AddWithValue("LastBFR", LastBFRInternal);
                    comm.Parameters.AddWithValue("LastMedical", LastMedical);
                    comm.Parameters.AddWithValue("MonthsToMedical", MonthsToMedical);
                    comm.Parameters.AddWithValue("IsInstructor", IsInstructor);
                    comm.Parameters.AddWithValue("UsesSIC", TracksSecondInCommandTime);
                    comm.Parameters.AddWithValue("UsesHHMM", UsesHHMM);
                    comm.Parameters.AddWithValue("useUTCDates", UsesUTCDateOfFlight);
                    comm.Parameters.AddWithValue("PKID", PKID);
                    comm.Parameters.AddWithValue("license", License.LimitTo(44));
                    comm.Parameters.AddWithValue("cert", Certificate.LimitTo(89));
                    comm.Parameters.AddWithValue("cfiExp", CertificateExpiration);
                    comm.Parameters.AddWithValue("currencyFlags", (uint)CurrencyFlags);
                    comm.Parameters.AddWithValue("showTimes", DisplayTimesByDefault);
                    comm.Parameters.AddWithValue("engProfExpiration", EnglishProficiencyExpiration);
                    comm.Parameters.AddWithValue("subscriptions", Subscriptions);
                    comm.Parameters.AddWithValue("lastemail", LastEmailDate);
                    comm.Parameters.AddWithValue("achievementstatus", (int)AchievementStatus);
                    comm.Parameters.AddWithValue("blacklist", String.Join(",", BlacklistedProperties));
                    comm.Parameters.AddWithValue("prefTimeZone", String.IsNullOrEmpty(PreferredTimeZoneID) || PreferredTimeZoneID.CompareCurrentCultureIgnoreCase("UTC") == 0 ? null : PreferredTimeZoneID);
                });
            if (dbh.LastError.Length == 0)
            {
                szErr = "";
                // save the updated object into the cache
                CacheProfile(this);
            }
            else
            {
                szErr = "Error saving profile: " + szQ + "\r\n" + dbh.LastError;
            }

            if (szErr.Length > 0)
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Profile.errGenericUpdateError, szErr));

            return (szErr.Length == 0);
        }
        #endregion
    }

    [Serializable]
    public class Profile : PersistedProfile
    {
        #region Creation
        public Profile() : base() { }

        public Profile(MySqlDataReader dr) : base()
        {
            InitFromDataReader(dr);
        }
        #endregion

        #region Getting users
        private static void LoadUserFromDB(string szUser, Action<MySqlDataReader> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            DBHelper dbh = new DBHelper("SELECT uir.Rolename AS Role, u.* FROM users u LEFT JOIN usersinroles uir ON (u.Username=uir.Username AND uir.ApplicationName='Logbook') WHERE u.Username=?UserName LIMIT 1");
            dbh.ReadRow(
                (comm) => { comm.Parameters.AddWithValue("UserName", szUser); },
                (dr) => { action(dr); }
            );
        }

        protected Boolean LoadUser(string szUser)
        {
            Boolean fResult = false;

            // don't hit the database for a query we know is going to return no results!
            if (String.IsNullOrEmpty(szUser))
                return fResult;

            // try loading from the cache first, save a DB call
            Profile pfCached = CachedProfileForUser(szUser);
            if (pfCached != null)
            {
                util.CopyObject(pfCached, this);
                // restore protected properties that are not copied by CopyObject
                OriginalPKID = pfCached.OriginalPKID;
                OriginalEmail = pfCached.OriginalEmail;
                return true;
            }

            LoadUserFromDB(szUser, (dr) =>
            {
                InitFromDataReader(dr);
                CacheProfile(this);
                fResult = true;
            });

            return fResult;
        }

        /// <summary>
        /// Retrieve a user from the cache - NO DATABASE HIT, ALWAYS RETURNS QUICKLY, CAN RETURN NULL
        /// </summary>
        /// <param name="name">username to fetch</param>
        /// <returns>null if not current in cache, else Profile</returns>
        private static Profile CachedProfileForUser(string name)
        {
            return HttpRuntime.Cache == null ? null : (Profile)HttpRuntime.Cache[GetCacheKey(name)];
        }

        /// <summary>
        /// Retrieve a user that could be cached.  CAN HIT THE DB, ALWAYS RETURNS A VALUE (BUT MAY NOT BE INITIALIZED)
        /// </summary>
        /// <param name="name">The name of the user to load</param>
        /// <param name="fForceNew">True to bypass the cache</param>
        /// <returns>A profile object from the cache, if possible</returns>
        public static Profile GetUser(string name, bool fBypassCache = false)
        {
            if (String.IsNullOrWhiteSpace(name))
                return new Profile();

            Profile pf = fBypassCache ? null : CachedProfileForUser(name);
            if (pf == null)
            {
                LoadUserFromDB(name, (dr) => { pf = new Profile(dr); });
                CacheProfile(pf);
            }
            return pf;
        }
        #endregion

        public static IEnumerable<Profile> UsersWithSubscriptions(UInt32 subscriptionMask, DateTime dtMin)
        {
            string szQ = String.Format(CultureInfo.InvariantCulture, "SELECT uir.Rolename AS Role, u.* FROM users u LEFT JOIN usersinroles uir ON (u.Username=uir.Username AND uir.ApplicationName='Logbook') WHERE (u.EmailSubscriptions & {0}) <> 0 AND (LastEmail IS NULL OR LastEmail <= '{1}')", subscriptionMask, dtMin.ToString("yyyy-MM-dd"));
            DBHelper dbh = new DBHelper(szQ);
            List<Profile> l = new List<Profile>();
            dbh.ReadRows(
                (comm) => { },
                (dr) => { l.Add(new Profile(dr)); });
            return l;
        }

        #region BFR and Medical functions
        /// <summary>
        /// Returns a pseudo-event for the last BFR.
        /// </summary>
        public ProfileEvent LastBFREvent
        {
            get
            {
                if (LastBFRInternal.CompareTo(DateTime.MinValue) == 0)
                    return null;
                else
                {
                    // BFR's used to be stored in the profile.  If so, we'll convert it to a pseudo flight property here,
                    // and attempt to de-dupe it later.
                    ProfileEvent pf = new ProfileEvent() { Date = LastBFRInternal };
                    pf.PropertyType.FormatString = Resources.Profile.BFRFromProfile;
                    return pf;
                }
            }
        }

        /// <summary>
        /// internal cache of BFR events; go easy on the database!
        /// </summary>
        private ProfileEvent[] BFREvents { get; set; }

        /// <summary>
        /// Same as BFREvents except that if BFREvents is null it hits the database first.
        /// </summary>
        private ProfileEvent[] CachedBFREvents()
        {
            if (BFREvents == null)
                BFREvents = ProfileEvent.GetBFREvents(UserName, LastBFREvent);
            return BFREvents;
        }

        /// <summary>
        /// Last BFR Date (uses flight properties).
        /// </summary>
        public DateTime LastBFR()
        {
            ProfileEvent[] rgPfe = CachedBFREvents();

            if (rgPfe.Length > 0)
                return rgPfe[rgPfe.Length - 1].Date;
            else
                return DateTime.MinValue;
        }

        /// <summary>
        /// Last BFR in a Robinson R22 (see SFAR 73)
        /// </summary>
        public DateTime LastBFRR22()
        {
            ProfileEvent[] rgPfe = CachedBFREvents();
            for (int i = rgPfe.Length - 1; i >= 0; i--)
                if (rgPfe[i].IsR22)
                    return rgPfe[i].Date;
            return DateTime.MinValue;
        }

        /// <summary>
        /// Last BFR in a Robinson R44 (see SFAR 73)
        /// </summary>
        public DateTime LastBFRR44()
        {
            ProfileEvent[] rgPfe = CachedBFREvents();
            for (int i = rgPfe.Length - 1; i >= 0; i--)
                if (rgPfe[i].IsR44)
                    return rgPfe[i].Date;
            return DateTime.MinValue;
        }

        /// <summary>
        /// Predicted date that next BFR is due
        /// </summary>
        /// <param name="bfrLast">Date of the last BFR</param>
        /// <returns>Datetime representing the date of the next BFR, Datetime.minvalue for unknown</returns>
        public DateTime NextBFR(DateTime bfrLast)
        {
            return bfrLast.AddCalendarMonths(24);
        }

        /// <summary>
        /// Predicted date that next medical is due
        /// </summary>
        /// <returns>Datetime representing the date of the next medical, datetime.minvalue for unknown.</returns>
        public DateTime NextMedical
        {
            get
            {
                if (!LastMedical.HasValue() || MonthsToMedical == 0)
                    return DateTime.MinValue;
                else
                    return UsesICAOMedical ? LastMedical.AddMonths(MonthsToMedical) : LastMedical.AddCalendarMonths(MonthsToMedical);
            }
        }
        #endregion

        #region Profile-based currency items (i.e., not related to flying - things like medical and flight reviews)
        private CurrencyStatusItem StatusForDate(DateTime dt, string szLabel, CurrencyStatusItem.CurrencyGroups rt)
        {
            if (dt.CompareTo(DateTime.MinValue) != 0)
            {
                TimeSpan ts = dt.Subtract(DateTime.Now);
                int days = (int)Math.Ceiling(ts.TotalDays);
                CurrencyState cs = (days < 0) ? CurrencyState.NotCurrent : ((ts.Days < 30) ? CurrencyState.GettingClose : CurrencyState.OK);
                return new CurrencyStatusItem(szLabel, dt.ToShortDateString(), cs, (cs == CurrencyState.GettingClose) ? String.Format(CultureInfo.CurrentCulture, Resources.Profile.ProfileCurrencyStatusClose, days) :
                                                                                   (cs == CurrencyState.NotCurrent) ? String.Format(CultureInfo.CurrentCulture, Resources.Profile.ProfileCurrencyStatusNotCurrent, -days) : string.Empty) { CurrencyGroup = rt };
            }
            else
                return null;
        }

        public IEnumerable<CurrencyStatusItem> WarningsForUser()
        {
            List<CurrencyStatusItem> rgCS = new List<CurrencyStatusItem>();

            CurrencyStatusItem csMedical = NextMedical.HasValue() ? StatusForDate(NextMedical, Resources.Currency.NextMedical, CurrencyStatusItem.CurrencyGroups.Medical) : null;
            CurrencyStatusItem csBasicMed = new Basicmed.BasicMed(UserName).Status;

            /* Scenarios for combining regular medical and basicmed.
             * 
             *   +--------------------+--------------------+--------------------+--------------------+
             *   |\_____    Medical   |                    |                    |                    |
             *   |      \______       |   Never Current    |     Expired        |      Valid         |
             *   | BasicMed    \_____ |                    |                    |                    |
             *   +--------------------+--------------------+--------------------+--------------------+
             *   |  Never Current     |    Show Nothing    |   Show Expired     |    Show Valid      |
             *   |                    |                    |     Medical        |     Medical        |
             *   +--------------------+--------------------+--------------------+--------------------+
             *   |      Expired       |        N/A         |   Show Expired     | Show Valid Medical |
             *   |                    |                    | Medical, BasicMed  | Suppress BasicMed  |
             *   +--------------------+--------------------+--------------------+--------------------+
             *   |                    |                    |    Show Valid      |    Show Valid      |
             *   |       Valid        |        N/A         |   BasicMed, note   |   Medical, show    |
             *   |                    |                    |    BasicMed only   |   BasicMed too     |
             *   +--------------------+--------------------+--------------------+--------------------+
             * 
             * 
             * a) Medical has never been valid -> by definition, neither has basic med: NO STATUS
             * b) Medical expired
             *      1) BasicMed never valid -> show only expired medical
             *      2) BasicMed expired -> show expired medical, expired basicmed (so you can tell which is easier to renew)
             *      3) BasicMed is valid -> Show valid BasicMed, that you are ONLY basicmed
             * c) Medical valid:
             *      1) BasicMed never valid -> Show only valid medical
             *      2) BasicMed expired -> show valid medical don't bother showing the expired basicmed since it's kinda pointless
             *      3) BasicMed is still valid -> show both (hey, seeing green is good)
            */

            if (csMedical != null) // (a) above - i.e., left column of chart; nothing to add if never had valid medical
            {
                if (csBasicMed == null) // b.1 and c.1 above, i.e., top row of chart - Just show medical status
                    rgCS.Add(csMedical);
                else
                {
                    switch (csMedical.Status)
                    {
                        case CurrencyState.OK:
                        case CurrencyState.GettingClose:
                            // Medical valid - c.2 and c.3 above - show medical, show basic med if valid
                            rgCS.Add(csMedical);
                            if (csBasicMed.Status != CurrencyState.NotCurrent)
                                rgCS.Add(csBasicMed);
                            break;
                        case CurrencyState.NotCurrent:
                            // Medical is not current but basicmed has been - always show basicmed, show medical only if basicmed is also expired
                            rgCS.Add(csBasicMed);
                            if (csBasicMed.Status == CurrencyState.NotCurrent)
                                rgCS.Add(csMedical);
                            break;
                    }
                }
            }

            BFREvents = null; // clear the cache - but this will let the next three calls (LastBFR/LastBFRR22/LastBFRR44) hit the DB only once.
            DateTime dtBfrLast = LastBFR();
            if (dtBfrLast.HasValue())
                rgCS.Add(StatusForDate(NextBFR(dtBfrLast), Resources.Currency.NextFlightReview, CurrencyStatusItem.CurrencyGroups.FlightReview));

            // SFAR 73 support - check if there is an expired R22/R44 BFR
            DateTime dtBfrLastR22 = LastBFRR22();
            DateTime dtBfrLastR44 = LastBFRR44();

            if (dtBfrLastR22.HasValue() || dtBfrLastR44.HasValue())
            {
                SFAR73Currency sFAR73Currency = new SFAR73Currency(UserName);   // database hit, unfortunately.  But only if you actually have R22/R44 experience!
                if (dtBfrLastR22.HasValue())
                    rgCS.Add(StatusForDate(sFAR73Currency.NextR22FlightReview(dtBfrLastR22), Resources.Currency.NextFlightReviewR22, CurrencyStatusItem.CurrencyGroups.FlightExperience));
                if (dtBfrLastR44.HasValue())
                    rgCS.Add(StatusForDate(sFAR73Currency.NextR44FlightReview(dtBfrLastR44), Resources.Currency.NextFlightReviewR44, CurrencyStatusItem.CurrencyGroups.FlightExperience));
            }

            BFREvents = null; // clear the cache again (memory).

            if (CertificateExpiration.HasValue())
                rgCS.Add(StatusForDate(CertificateExpiration, Resources.Currency.CertificateExpiration, CurrencyStatusItem.CurrencyGroups.Certificates));

            if (EnglishProficiencyExpiration.HasValue())
                rgCS.Add(StatusForDate(EnglishProficiencyExpiration, Resources.Currency.NextLanguageProficiency, CurrencyStatusItem.CurrencyGroups.Certificates));

            return rgCS;
        }
        #endregion

        #region Basic Administrative Functions (stuff that doesn't require admin privileges)
        /// <summary>
        /// Changes name and email, sending a notification if the email changes.
        /// </summary>
        /// <param name="szNewFirst">The new first name</param>
        /// <param name="szNewLast">The new last name</param>
        /// <param name="szNewEmail">The new email address</param>
        /// <param name="szNewAddress">The new mailing address</param>
        /// <exception cref="MyFlightbookException"></exception>
        public void ChangeNameAndEmail(string szNewFirst, string szNewLast, string szNewEmail, string szNewAddress)
        {
            if (szNewEmail == null)
                throw new ArgumentNullException("szNewEmail");
            string szOriginalEmail = Email;
            FirstName = szNewFirst;
            LastName = szNewLast;
            Email = szNewEmail;
            Address = szNewAddress;

            FCommit();

            if (String.Compare(Email, szOriginalEmail, StringComparison.OrdinalIgnoreCase) != 0)
            {
                string szBody = String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.ChangeEmailConfirmation, szOriginalEmail, Email);
                // Email has changed - send new email to both the old and the new address
                util.NotifyUser(Resources.Profile.EmailChangedSubjectLine, szBody, new MailAddress(szOriginalEmail, UserFullName), false, false);
                util.NotifyUser(Resources.Profile.EmailChangedSubjectLine, szBody, new MailAddress(Email, UserFullName), false, false);
            }
        }
        #endregion

        #region Achievements

        private IEnumerable<Badge> m_cachedBadges = null;

        /// <summary>
        /// Returns the most recently computed badges for the user, if valid, else null.
        /// </summary>
        public IEnumerable<Badge> CachedBadges
        {
            get
            {
                if (AchievementStatus == Achievement.ComputeStatus.UpToDate)
                {
                    if (m_cachedBadges == null)
                        m_cachedBadges = new Achievement(UserName).BadgesForUser();
                }
                else
                    m_cachedBadges = null;
                return m_cachedBadges;
            }
        }

        private readonly Object achievementLock = new Object();
        /// <summary>
        /// Sets the achievement status, saving to the DB if it has changed and if it is not "In Progress" (which by definition is a temporary state)
        /// </summary>
        /// <param name="stat"></param>
        public void SetAchievementStatus(Achievement.ComputeStatus stat)
        {
            lock (achievementLock)
            {
                bool fChanged = (stat != AchievementStatus);
                if (fChanged)
                    m_cachedBadges = null;
                AchievementStatus = stat;
                if (fChanged && stat != Achievement.ComputeStatus.InProgress)
                    FCommit();
            }
        }

        public static void InvalidateAllAchievements()
        {
            DBHelper dbh = new DBHelper("UPDATE USERS SET achievementstatus=2 WHERE achievementstatus=1");
            dbh.DoNonQuery();

            // update all of the cached profile objects
            if (HttpRuntime.Cache != null)
            {
                Cache c = HttpRuntime.Cache;
                IDictionaryEnumerator en = c.GetEnumerator();
                while (en.MoveNext())
                {
                    object o = c[en.Key.ToString()];
                    if (o.GetType() == typeof(Profile))
                        ((Profile)o).AchievementStatus = Achievement.ComputeStatus.NeedsComputing;
                }

                AirportListBadge.FlushCache();
            }
        }
        #endregion

        /// <summary>
        /// Determines if the named user is eligible to sign flights
        /// </summary>
        /// <param name="szCFIUsername">The name of the potential signer</param>
        /// <param name="szError">The error that results</param>
        /// <returns>True if they can sign</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#")]
        public bool CanSignFlights(out string szError)
        {
            szError = String.Empty;
            if (String.IsNullOrEmpty(Certificate))
            {
                szError = Resources.SignOff.errSignNoCertificate;
                return false;
            }

            if (CertificateExpiration.AddDays(1).CompareTo(DateTime.Now) < 0)
            {
                szError = Resources.SignOff.errSignExpiredCertificate;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Return a URL to this user's public flights.
        /// </summary>
        /// <param name="szHost">The host name to use in the URL</param>
        /// <returns>A URL to the user's public flights</returns>
        public Uri PublicFlightsURL(string szHost)
        {
            SharedDataEncryptor enc = new SharedDataEncryptor(MFBConstants.keyEncryptMyFlights);
            return new Uri(String.Format(CultureInfo.InvariantCulture, "https://{0}{1}?uid={2}", szHost, VirtualPathUtility.ToAbsolute("~/public/myflights.aspx"), HttpUtility.UrlEncode(enc.Encrypt(this.UserName))));
        }
    }

    public class ProfileAdmin : Profile
    {
        public static bool ValidateUser(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException("szUser");
            if (szUser.Contains("@"))
                szUser = Membership.GetUserNameByEmail(szUser);

            MembershipUser mu = Membership.GetUser(szUser);
            return (mu != null) && (mu.UserName.Length > 0);
        }

        public static MembershipUser ADMINUserFromName(string szName)
        {
            if (szName == null)
                throw new ArgumentNullException("szName");
            MembershipProvider mp = Membership.Providers["AdminMembershipProvider"];
            String szUserToReset = szName;

            // convert an email to a username, if necessary
            if (szUserToReset.Contains("@"))
                szUserToReset = mp.GetUserNameByEmail(szUserToReset);

            return mp.GetUser(szUserToReset, false);
        }

        public enum DeleteLevel {
            /// <summary>
            /// Deletes flights for the user (and their images, telemetry, and associated badges)
            /// </summary>
            OnlyFlights,
            /// <summary>
            /// Deletes the entire user account, including flights.
            /// </summary>
            EntireUser };

        private static void SendDeleteWithBackupAndThankyou(MembershipUser mu, DeleteLevel dl)
        {
            try
            {
                Profile pf = Profile.GetUser(mu.UserName);
                using (MailMessage msg = new MailMessage())
                {
                    Brand brand = Branding.CurrentBrand;
                    msg.From = new MailAddress(brand.EmailAddress, brand.AppName);
                    msg.To.Add(new MailAddress(pf.Email, pf.UserFullName));
                    msg.Subject = Branding.ReBrand(dl == DeleteLevel.EntireUser ? Resources.EmailTemplates.AccountDeletedSubject : Resources.EmailTemplates.FlightsDeletedSubject);
                    msg.Body = Branding.ReBrand(dl == DeleteLevel.EntireUser ? Resources.EmailTemplates.AccountDeletedBody : Resources.EmailTemplates.FlightsDeletedBody) + "\r\n\r\n" + Branding.ReBrand(Resources.EmailTemplates.ThankYouCloser);
                    util.AddAdminsToMessage(msg, false, ProfileRoles.maskCanSupport);
                    LogbookBackup lb = new LogbookBackup(pf);
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream(lb.LogbookDataForBackup()))
                    {
                        msg.Attachments.Add(new Attachment(ms, lb.BackupFilename(brand)));
                        util.SendMessage(msg);
                    }
                }
            }
            catch (ArgumentNullException) { }
            catch (TimeoutException) { }
            catch (InvalidOperationException) { }
        }

        private static void DeleteUserImages(DBHelper dbh)
        {
            List<MFBImageInfo> lstMfbii = new List<MFBImageInfo>();
            dbh.CommandText = @"SELECT f.idflight, f.username, i.* 
                        FROM images i 
                        LEFT JOIN flights f ON i.imagekey=f.idflight
                        WHERE (i.VirtPathID=0 AND f.username=?uname)";
            dbh.ReadRows((comm) => { }, (dr) => { lstMfbii.Add(MFBImageInfo.ImageFromDBRow(dr)); });
            foreach (MFBImageInfo mfbii in lstMfbii)
                mfbii.DeleteImage();
        }

        private static void DeleteUserFlights(DBHelper dbh)
        {
            List<TelemetryReference> lstTelemetry = new List<TelemetryReference>();
            dbh.CommandText = @"SELECT ft.* FROM flights f INNER JOIN flighttelemetry ft ON f.idflight=ft.idflight WHERE f.username=?uname";
            dbh.ReadRows((comm) => { }, (dr) => { lstTelemetry.Add(new TelemetryReference(dr)); });
            lstTelemetry.ForEach((ts) => { ts.DeleteFile(); }); // only need to delete the file; the flighttelemetry row will be deleted when we delete the flights (below), so don't need the excess DB hits.

            // Remove any flights for the user
            dbh.CommandText = "DELETE FROM flights WHERE username=?uname";
            dbh.DoNonQuery((comm) => { });
        }

        public static void DeleteForUser(MembershipUser mu, DeleteLevel dl)
        {
            if (mu == null || String.IsNullOrEmpty(mu.UserName))
                throw new MyFlightbookException("Don't try to delete anything for a user without specifying a user!!!");

            try
            {
                // See if there are multiple users (just in case).
                // If there are, delete the specified user only, and then be done.
                int cUsersWithUsername = 0;
                DBHelperCommandArgs dba = new DBHelperCommandArgs("SELECT COUNT(*) AS NumUsers FROM users WHERE username=?uname");
                dba.AddWithValue("uname", mu.UserName);
                dba.AddWithValue("pkid", mu.ProviderUserKey);
                DBHelper dbh = new DBHelper(dba);
                dbh.ReadRow((comm) => { }, (dr) => { cUsersWithUsername = Convert.ToInt32(dr["NumUsers"], CultureInfo.InvariantCulture); });

                if (cUsersWithUsername == 0)    // shouldn't happen
                    throw new MyFlightbookException("No users found with username " + mu.UserName);
                if (cUsersWithUsername > 1)    // shouldn't happen.
                    throw new MyFlightbookException("Multiple users with username " + mu.UserName);

                SendDeleteWithBackupAndThankyou(mu, dl);

                // See if the user is an owner of a club
                dbh.CommandText = "SELECT COUNT(*) AS numClubs FROM clubs c INNER JOIN clubmembers cm ON c.idclub=cm.idclub WHERE c.creator=?uname OR (cm.username=?uname AND cm.role=2)";
                int cOwnedClubs = 0;
                dbh.ReadRow((comm) => { }, (dr) => { cOwnedClubs = Convert.ToInt32(dr["numClubs"], CultureInfo.InvariantCulture); });
                if (cOwnedClubs > 0)
                    throw new MyFlightbookException("User is owner of clubs; need to delete those clubs first");

                // Remove any images for the user's flights (only works if images UseDB is true...)
                DeleteUserImages(dbh);

                DeleteUserFlights(dbh);

                PendingFlight.DeletePendingFlightsForUser(mu.UserName);

                // And any badges that have been earned
                Badge.DeleteBadgesForUser(mu.UserName, dl == DeleteLevel.OnlyFlights);

                if (dl == DeleteLevel.EntireUser)
                    DeleteEntireUserAccount(mu);
            }
            catch (Exception ex)
            {
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Exception deleting for user ({0}): {1}", dl.ToString(), ex.Message), ex);
            }
        }

        /// <summary>
        /// Deletes an entire user account.  requires that all flight data & images already have been deleted, and other validation on the membership user has been validated.
        /// </summary>
        /// <param name="mu">The membership user to delete.  MUST HAVE BEEN VALIDATED - non-null, unique username</param>
        private static void DeleteEntireUserAccount(MembershipUser mu)
        {
            DBHelperCommandArgs dba = new DBHelperCommandArgs("SELECT COUNT(*) AS NumUsers FROM users WHERE username=?uname");
            dba.AddWithValue("uname", mu.UserName);
            dba.AddWithValue("pkid", mu.ProviderUserKey);

            // Remove the user's aircraft
            DBHelper dbh = new DBHelper(dba) { CommandText = "DELETE FROM useraircraft WHERE username=?uname" };
            dbh.DoNonQuery();

            // Remove from student records
            dbh.CommandText = "DELETE FROM students WHERE StudentName=?uname";
            dbh.DoNonQuery();
            dbh.CommandText = "DELETE FROM students WHERE CFIName=?uname";
            dbh.DoNonQuery();

            // Remove from maintenance logs.
            dbh.CommandText = "DELETE FROM maintenancelog WHERE User=?uname";
            dbh.DoNonQuery();

            dbh.CommandText = "DELETE FROM customcurrency WHERE Username=?uname";
            dbh.DoNonQuery();

            dbh.CommandText = "DELETE FROM deadlines WHERE username=?uname";
            dbh.DoNonQuery();

            dbh.CommandText = "DELETE FROM airports WHERE sourceusername=?uname";
            dbh.DoNonQuery();

            dbh.CommandText = "DELETE FROM badges WHERE username=?uname";
            dbh.DoNonQuery();

            dbh.CommandText = "DELETE FROM earnedgratuities WHERE username=?uname";
            dbh.DoNonQuery();

            // Remove the user from the logs
            dbh.CommandText = "DELETE FROM wsevents WHERE user=?uname";
            dbh.DoNonQuery();

            // If they're an instructor, remove their username from the 
            dbh.CommandText = "UPDATE endorsements SET cfi='' WHERE CFI=?uname";
            dbh.DoNonQuery();

            // And delete their endorsements - flight images should have already been deleted before this method was called
            dbh.CommandText = "DELETE FROM endorsements WHERE StudentType=0 AND Student=?uname";
            dbh.DoNonQuery();
            ImageList il = new ImageList(MFBImageInfo.ImageClass.Endorsement, mu.UserName);
            il.Refresh(true);
            foreach (MFBImageInfo mfbii in il.ImageArray)
                mfbii.DeleteImage();

            // Delete basicmed records
            foreach (Basicmed.BasicMedEvent bme in Basicmed.BasicMedEvent.EventsForUser(mu.UserName))
                bme.Delete();

            // Delete from any clubs
            dbh.CommandText = "DELETE FROM clubmembers WHERE username=?uname";
            dbh.DoNonQuery();

            // And from schedules
            dbh.CommandText = "DELETE FROM scheduledevents WHERE username=?uname";
            dbh.DoNonQuery();

            // Finally, delete the user
            dbh.CommandText = "DELETE FROM users WHERE PKID=?pkid";
            dbh.DoNonQuery();
        }

        /// <summary>
        /// Returns a set of localized suggestions for security questions.
        /// </summary>
        public static IEnumerable<string> SuggestedSecurityQuestions
        {
            get { return Resources.LocalizedText.AccountQuestionSamplesList.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); }
        }

        /// <summary>
        /// Sets the first/last name for the user, sends notification email & welcome email.
        /// </summary>
        /// <param name="szUser">Username</param>
        /// <param name="szFirst">First Name</param>
        /// <param name="szLast">Last Name</param>
        /// <param name="fWebService">True if this was from the web service</param>
        static public void FinalizeUser(string szUser, string szFirst, string szLast, Boolean fWebService)
        {
            Profile pf = Profile.GetUser(szUser);
            pf.FirstName = szFirst;
            pf.LastName = szLast;
            pf.TracksSecondInCommandTime = pf.IsInstructor = true;
            pf.FCommit();

            // send welcome mail
            util.NotifyUser(String.Format(CultureInfo.CurrentCulture, Resources.Profile.WelcomeTitle, Branding.CurrentBrand.AppName), Resources.EmailTemplates.Welcomeemailhtm, new MailAddress(pf.Email, pf.UserFirstName), false, true);
            util.NotifyAdminEvent("New user created" + (fWebService ? " - WebService" : ""), String.Format(CultureInfo.CurrentCulture, "User '{0}' was created at {1}", pf.UserName, DateTime.Now.ToString("MM/dd/yyyy HH:mm", CultureInfo.InvariantCulture)), ProfileRoles.maskCanReport);
        }
    }

    [Serializable]
    public class UserEntityException : Exception
    {
        public MembershipCreateStatus ErrorCode { get; set; }

        #region Constructors
        public UserEntityException() : base()
        {
            ErrorCode = MembershipCreateStatus.Success;
        }

        public UserEntityException(string message) : base(message)
        {
            ErrorCode = MembershipCreateStatus.Success;
        }

        public UserEntityException(string message, Exception ex) : base(message, ex)
        {
            ErrorCode = MembershipCreateStatus.Success;
        }

        protected UserEntityException(System.Runtime.Serialization.SerializationInfo si, System.Runtime.Serialization.StreamingContext sc) : base(si, sc)
        {
            ErrorCode = MembershipCreateStatus.Success;
        }

        public UserEntityException(MembershipCreateStatus mcs) : base()
        {
            ErrorCode = mcs;
        }

        public UserEntityException(string message, MembershipCreateStatus mcs) : base(message)
        {
            ErrorCode = mcs;
        }

        public UserEntityException(string message, Exception ex, MembershipCreateStatus mcs) : base(message, ex)
        {
            ErrorCode = mcs;
        }
        #endregion

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("ErrorCode", ErrorCode);
        }
    }

    [Serializable]
    public class UserEntity
    {
        public string szAuthToken { get; set; }
        public string szUsername { get; set; }
        public MembershipCreateStatus mcs { get; set; }

        public UserEntity()
        {
            szUsername = szAuthToken = string.Empty;
            mcs = MembershipCreateStatus.Success;
        }

        /// <summary>
        /// Validates a user based on a username/password
        /// </summary>
        /// <param name="szUser">Username for the user</param>
        /// <param name="szPass">Password for the user</param>
        /// <returns>Username that is validated, else an empty string</returns>
        static public string ValidateUser(string szUser, string szPass)
        {
            if (Membership.ValidateUser(szUser, szPass))
                return szUser;
            else
                return string.Empty;
        }

        /// <summary>
        /// Find a new, non-conflicting username for the specified email address.  Assumes the email address is NOT a duplicate.
        /// </summary>
        /// <param name="szEmail">The email address</param>
        /// <returns>The username to use</returns>
        static public string UserNameForEmail(string szEmail)
        {
            if (szEmail == null)
                throw new ArgumentNullException("szEmail");
            //  find a unique username to propose
            int ichAt = szEmail.IndexOf('@');
            string szUserBase = (ichAt > 0) ? szEmail.Remove(ichAt) : szEmail;

            // Clean up any illegal characters (for Amazon, for example)
            szUserBase = Regex.Replace(szUserBase, "[ $&@=;:+,?\\{}^~#|<>[]", "-");

            string szUser = szUserBase;
            int i = 1;
            while (Membership.GetUser(szUser, false) != null)
                szUser = szUserBase + (i++).ToString(CultureInfo.InvariantCulture);

            return szUser;
        }

        #region new user validation
        /// <summary>
        /// Checks the password - throws an exception if there is an issue with it.
        /// </summary>
        /// <param name="szPass"></param>
        private static void ValidatePassword(string szPass)
        {
            if (String.IsNullOrEmpty(szPass))
                throw new UserEntityException(Resources.Profile.errNoPassword, MembershipCreateStatus.InvalidPassword);

            if (szPass.Length < 6 || szPass.Length > 20)
                throw new UserEntityException(Resources.Profile.errBadPasswordLength, MembershipCreateStatus.InvalidPassword);
        }

        /// <summary>
        /// Checks the email - throws an exception if there is an issue.
        /// </summary>
        /// <param name="szEmail"></param>
        private static void ValidateEmailAndUser(string szEmail, string szUser)
        {
            if (String.IsNullOrEmpty(szEmail))
                throw new UserEntityException(String.Format(CultureInfo.CurrentCulture, Resources.Profile.errInvalidEmail, string.Empty), MembershipCreateStatus.InvalidEmail);

            if (String.IsNullOrEmpty(szUser))
                throw new UserEntityException(String.Format(CultureInfo.CurrentCulture, Resources.Profile.errInvalidEmail, string.Empty), MembershipCreateStatus.InvalidUserName);

            Regex r = new Regex("\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*", RegexOptions.Compiled | RegexOptions.CultureInvariant);
            Match m = r.Match(szEmail);
            if (m.Captures.Count != 1 || m.Captures[0].Value.CompareOrdinal(szEmail) != 0)
                throw new UserEntityException(String.Format(CultureInfo.CurrentCulture, Resources.Profile.errInvalidEmail, szEmail), MembershipCreateStatus.InvalidEmail);

            Regex rgUsername = new Regex("\\w+([-+.']\\w+)*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!rgUsername.IsMatch(szUser))
                throw new UserEntityException(String.Format(CultureInfo.CurrentCulture, Resources.Profile.errInvalidEmail, szUser), MembershipCreateStatus.InvalidUserName);
        }

        /// <summary>
        /// Checks the question and answer - throws an exception if there is an issue
        /// </summary>
        /// <param name="szQuestion"></param>
        /// <param name="szAnswer"></param>
        private static void ValidateQandA(string szQuestion, string szAnswer)
        {
            if (String.IsNullOrEmpty(szQuestion))
                throw new UserEntityException(Resources.Profile.errNoQuestion, MembershipCreateStatus.InvalidQuestion);
            if (szQuestion.Length > 80)
                throw new UserEntityException(Resources.Profile.errQuestionTooLong, MembershipCreateStatus.InvalidQuestion);

            if (String.IsNullOrEmpty(szAnswer))
                throw new UserEntityException(Resources.Profile.errNoAnswer, MembershipCreateStatus.InvalidAnswer);
            if (szAnswer.Length > 80)
                throw new UserEntityException(Resources.Profile.errAnswerTooLong, MembershipCreateStatus.InvalidAnswer);
        }
        #endregion

        /// <summary>
        /// Creates a user based on a username/password
        /// </summary>
        /// <param name="szUser">Username for the user</param>
        /// <param name="szPass">Password for the user</param>
        /// <exception cref="UserEntityException"></exception>
        /// <returns>True if success</returns>
        static public UserEntity CreateUser(string szUser, string szPass, string szEmail, string szQuestion, string szAnswer)
        {

            MySqlMembershipProvider mmp = new MySqlMembershipProvider();
            NameValueCollection nvc = new NameValueCollection();
            MembershipCreateStatus result;

            // Validate - this will throw a UserEntityException if there is an issue.
            ValidateEmailAndUser(szEmail, szUser);
            ValidatePassword(szPass);
            ValidateQandA(szQuestion, szAnswer);

            // If we are here, everything has been validated
            nvc.Add("applicationName", "Online Flight Logbook");
            nvc.Add("connectionStringName", "logbookConnectionString");

            mmp.Initialize(null, nvc);

            MembershipUser mu = mmp.CreateUser(szUser, szPass, szEmail, szQuestion, szAnswer, true, Guid.NewGuid(), out result);

            return new UserEntity() { mcs = result, szUsername = mu == null ? string.Empty : mu.UserName };
        }
    }

    /// <summary>
    /// Manages a request for a password reset
    /// </summary>
    [Serializable]
    public class PasswordResetRequest
    {
        /// <summary>
        /// Default expiration, in seconds.  By default, 1-hour
        /// </summary>
        const int DefaultExpiration = 3600;

        /// <summary>
        /// Status of the request
        /// </summary>
        public enum RequestStatus { PendingOrUnused = 0, Success, Expired, Failed }

        #region properties
        /// <summary>
        /// ID for the request (GUID)
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Status of the request
        /// </summary>
        public RequestStatus Status { get; set; }

        /// <summary>
        /// Expiration of the request
        /// </summary>
        public DateTime Expiration { get; set;  }

        public string UserName { get; set; }
        #endregion

        public PasswordResetRequest()
        {
            ID = Guid.NewGuid().ToString();
            Expiration = DateTime.Now.AddSeconds(DefaultExpiration);
            Status = RequestStatus.PendingOrUnused;
        }

        /// <summary>
        /// Initializes from a pending reset request
        /// </summary>
        /// <param name="id">The ID of the pending request</param>
        /// <exception cref="ArgumentoutOfRangeException"></exception>
        public PasswordResetRequest(string id) : this()
        {
            ID = id;
            bool fFound = false;
            DBHelper dbh = new DBHelper("SELECT pr.* FROM passwordresetrequests pr INNER JOIN users u ON pr.username=u.username WHERE pr.ID=?guid");
            dbh.ReadRow((comm) => { comm.Parameters.AddWithValue("guid", ID); },
                (dr) =>
                {
                    fFound = true;
                    UserName = (string)dr["username"];
                    Expiration = Convert.ToDateTime(dr["expiration"], CultureInfo.InvariantCulture);
                    Status = (RequestStatus)Convert.ToInt32(dr["status"], CultureInfo.InvariantCulture);
                    if (Status == RequestStatus.PendingOrUnused && DateTime.Now.CompareTo(Expiration) > 0)
                        Status = RequestStatus.Expired;
                });
            if (!fFound)
                throw new ArgumentOutOfRangeException("id", "pending password reset with ID " + id + "was not found");
        }

        /// <summary>
        /// Save this to the database.
        /// </summary>
        public void FCommit()
        {
            DBHelper dbh = new DBHelper("REPLACE INTO passwordresetrequests SET username=?uname, expiration=?exp, status=?st, id=?guid");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("uname", UserName);
                comm.Parameters.AddWithValue("exp", Expiration);
                comm.Parameters.AddWithValue("st", (int)Status);
                comm.Parameters.AddWithValue("guid", ID);
            });
        }
    }

    public class InstructorStudent : Profile
    {
        public bool CanViewLogbook { get; set; }

        public bool CanAddLogbook { get; set; }

        public enum PermissionMask { None = 0x0000, ViewLogbook = 0x0001, AddLogbook = 0x0002 }

        public InstructorStudent(MySqlDataReader dr)
            : base(dr)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            uint perms = Convert.ToUInt32(dr["CanViewStudent"], CultureInfo.InvariantCulture);
            CanViewLogbook = (perms & (uint) PermissionMask.ViewLogbook) != 0;
            CanAddLogbook = (perms & (uint)PermissionMask.AddLogbook) != 0;
        }
    }
}