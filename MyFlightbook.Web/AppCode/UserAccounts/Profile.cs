using Andri.Web;
using DotNetOpenAuth.OAuth2;
using MyFlightbook.Achievements;
using MyFlightbook.BasicmedTools;
using MyFlightbook.CloudStorage;
using MyFlightbook.Currency;
using MyFlightbook.Encryptors;
using MyFlightbook.Image;
using MyFlightbook.Telemetry;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using System.Web.Security;

/******************************************************
 * 
 * Copyright (c) 2009-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    public static class ProfileRoles
    {
        [FlagsAttribute]
        public enum UserRoles
        {
            None = 0x0000,
            Support = 0x0001,
            DataManager = 0x0002,
            Reporter = 0x0004,
            Accountant = 0x0008,
            SiteAdmin = 0x0010
        };

        #region Bitmasks for roles
        public const uint maskCanManageData = ((uint)UserRoles.SiteAdmin | (uint)UserRoles.DataManager);
        public const uint maskCanReport = ((uint)UserRoles.SiteAdmin | (uint)UserRoles.Reporter);
        public const uint maskSiteAdminOnly = (uint)UserRoles.SiteAdmin;
        public const uint maskCanManageMoney = ((uint)UserRoles.SiteAdmin | (uint)UserRoles.Accountant);
        public const uint maskCanSupport = ((uint)UserRoles.SiteAdmin | (uint)UserRoles.Support | (uint)UserRoles.DataManager); // reporters cannot support
        public const uint maskCanContact = ((uint)UserRoles.SiteAdmin | (uint)UserRoles.Support);
        #endregion

        #region Helper routines for roles
        static public bool CanSupport(UserRoles r)
        {
            return ((uint)r & maskCanSupport) != 0;
        }

        static public bool CanManageData(UserRoles r)
        {
            return ((uint)r & maskCanManageData) != 0;
        }

        static public bool CanReport(UserRoles r)
        {
            return ((uint)r & maskCanReport) != 0;
        }

        static public bool CanManageMoney(UserRoles r)
        {
            return ((uint)r & maskCanManageMoney) != 0;
        }

        static public bool CanDoSomeAdmin(UserRoles r)
        {
            return r != UserRoles.None;
        }

        static public bool CanDoAllAdmin(UserRoles r)
        {
            return r == UserRoles.SiteAdmin;
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
                if (!String.IsNullOrEmpty(mu?.UserName))
                {
                    FormsAuthentication.SetAuthCookie(HttpContext.Current.Request.Cookies[MFBConstants.keyOriginalID].Value, true);
                    Profile pf = Profile.GetUser(mu.UserName);
                    HttpContext.Current.Session[MFBConstants.keyDecimalSettings] = pf.PreferenceExists(MFBConstants.keyDecimalSettings)
                        ? pf.GetPreferenceForKey<DecimalFormat>(MFBConstants.keyDecimalSettings)
                        : (object)null;
                    HttpContext.Current.Session[MFBConstants.keyMathRoundingUnits] = pf.MathRoundingUnit;
                }
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
            HttpContext.Current.Response.Cookies[MFBConstants.keyIsImpersonating].Value = true.ToString(CultureInfo.InvariantCulture);
            HttpContext.Current.Response.Cookies[MFBConstants.keyIsImpersonating].Expires = DateTime.Now.AddDays(30);

            Profile pf = Profile.GetUser(szTargetName);
            HttpContext.Current.Session[MFBConstants.keyDecimalSettings] = pf.PreferenceExists(MFBConstants.keyDecimalSettings)
                ? pf.GetPreferenceForKey<DecimalFormat>(MFBConstants.keyDecimalSettings)
                : (object)null;
            HttpContext.Current.Session[MFBConstants.keyMathRoundingUnits] = pf.MathRoundingUnit;
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
            Role = ProfileRoles.UserRoles.None;
            CreationDate = LastActivity = LastLogon = LastPasswordChange = 
            LastBFRInternal = LastMedical = CertificateExpiration = EnglishProficiencyExpiration = LastEmailDate = DateTime.MinValue;
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
        public ProfileRoles.UserRoles Role { get; set; }

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
        public UInt32 CurrencyFlags { get; set; }

        private void setCurrencyFlag(CurrencyOptionFlags cof, bool value)
        {
            CurrencyFlags = (UInt32)((value) ? (((uint)CurrencyFlags) | (uint)cof) : (((uint)CurrencyFlags) & ~(uint)cof));
        }

        private bool hasFlag(CurrencyOptionFlags cof)
        {
            return ((uint)CurrencyFlags & (uint)cof) != 0;
        }

        /// <summary>
        /// Does this user use military (AR 95-1) currency?
        /// </summary>
        public Boolean UsesArmyCurrency
        {
            get { return hasFlag(CurrencyOptionFlags.flagArmyMDSCurrency); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagArmyMDSCurrency, value); }
        }

        /// <summary>
        /// Does this user use per-model currency (i.e., currency for each make/model they fly?)
        /// </summary>
        public Boolean UsesPerModelCurrency
        {
            get { return hasFlag(CurrencyOptionFlags.flagPerModelCurrency); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagPerModelCurrency, value); }
        }

        /// <summary>
        /// Does this user show totals per-model?
        /// </summary>
        private Boolean UsesPerModelTotals
        {
            get { return hasFlag(CurrencyOptionFlags.flagShowTotalsPerModel); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagShowTotalsPerModel, value); }
        }

        /// <summary>
        /// Does this user show totals per-family?
        /// </summary>
        private Boolean UsesPerFamilyTotals
        {
            get { return hasFlag(CurrencyOptionFlags.flagsShowTotalsPerFamily); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagsShowTotalsPerFamily, value); }
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
            get { return hasFlag(CurrencyOptionFlags.flagSuppressModelFeatureTotals); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagSuppressModelFeatureTotals, value); }
        }

        /// <summary>
        /// Does this user use FAR 117-mandated rest periods?
        /// </summary>
        public Boolean UsesFAR117DutyTime
        {
            get { return hasFlag(CurrencyOptionFlags.flagFAR117DutyTimeCurrency); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagFAR117DutyTimeCurrency, value); }
        }

        /// <summary>
        /// If the user  uses FAR 117 mandated duty times, does it apply to all flights or only flights with a specified duty time?
        /// </summary>
        public Boolean UsesFAR117DutyTimeAllFlights
        {
            get { return hasFlag(CurrencyOptionFlags.flagFAR117IncludeAllFlights); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagFAR117IncludeAllFlights, value); }
        }

        /// <summary>
        /// Does this user use FAR 135-mandated rest periods?
        /// </summary>
        public Boolean UsesFAR135DutyTime
        {
            get { return hasFlag(CurrencyOptionFlags.flagFAR135DutyTimeCurrency); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagFAR135DutyTimeCurrency, value); }
        }

        public Boolean UsesFAR13529xCurrency
        {
            get { return hasFlag(CurrencyOptionFlags.flagUseFAR135_29xStatus); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagUseFAR135_29xStatus, value); }
        }

        public Boolean UsesFAR13526xCurrency
        {
            get { return hasFlag(CurrencyOptionFlags.flagUseFAR135_26xStatus); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagUseFAR135_26xStatus, value); }
        }

        public Boolean UsesFAR1252xxCurrency
        {
            get { return hasFlag(CurrencyOptionFlags.flagUseFAR125_2xxStatus); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagUseFAR125_2xxStatus, value); }
        }

        public Boolean UsesFAR61217Currency
        {
            get { return hasFlag(CurrencyOptionFlags.flagUseFAR61217); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagUseFAR61217, value); }
        }

        /// <summary>
        /// Specifies whether to use ICAO (day-for-day) medical expiration or FAA (calendar month) expiraiton
        /// </summary>
        public Boolean UsesICAOMedical
        {
            get { return hasFlag(CurrencyOptionFlags.flagUseEASAMedical); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagUseEASAMedical, value); }
        }

        /// <summary>
        /// Use loose 61.57(c)(4) interpretation (i.e., mix/match of ATD/FTD/Real)
        /// </summary>
        [Obsolete("61.57(c) has been revised; this is no longer valid")]
        public Boolean UsesLooseIFRCurrency
        {
            get { return hasFlag(CurrencyOptionFlags.flagUseLooseIFRCurrency); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagUseLooseIFRCurrency, value); }
        }

        /// <summary>
        /// Should we use Canadian currency rules?  See http://laws-lois.justice.gc.ca/eng/regulations/SOR-96-433/FullText.html#s-401.05 and https://www.tc.gc.ca/eng/civilaviation/publications/tp185-1-10-takefive-559.htm
        /// </summary>
        private Boolean UseCanadianCurrencyRules
        {
            get { return hasFlag(CurrencyOptionFlags.flagUseCanadianCurrencyRules); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagUseCanadianCurrencyRules, value); }
        }

        /// <summary>
        /// Should we use LAPL currency rules?  See https://www.caa.co.uk/General-aviation/Pilot-licences/EASA-requirements/LAPL/LAPL-(A)-requirements/
        /// </summary>
        private Boolean UseLAPLCurrencyRules
        {
            get { return hasFlag(CurrencyOptionFlags.flagUseLAPLCurrency); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagUseLAPLCurrency, value); }
        }

        /// <summary>
        /// Should we use Australian currency rules?  See http://classic.austlii.edu.au/au/legis/cth/consol_reg/casr1998333/s61.870.html and http://www5.austlii.edu.au/au/legis/cth/consol_reg/casr1998333/s61.395.html
        /// </summary>
        private Boolean UseAustralianCurrencyRules
        {
            get { return hasFlag(CurrencyOptionFlags.flagUseAustralianCurrency); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagUseAustralianCurrency, value); }
        }

        /// <summary>
        ///  Gets/sets the current jurisdiction for currency rules.  FAA is the default.
        /// </summary>
        public CurrencyJurisdiction CurrencyJurisdiction
        {
            get
            {
                return UseAustralianCurrencyRules ? CurrencyJurisdiction.Australia : (UseCanadianCurrencyRules ? CurrencyJurisdiction.Canada : (UseLAPLCurrencyRules ? CurrencyJurisdiction.EASA : CurrencyJurisdiction.FAA));
            }
            set
            {
                UseLAPLCurrencyRules = value == CurrencyJurisdiction.EASA;
                UseCanadianCurrencyRules = value == CurrencyJurisdiction.Canada;
                UseAustralianCurrencyRules = value == CurrencyJurisdiction.Australia;
            }
        }

        /// <summary>
        /// Determines if touch-and-go landings can qualify for night currency.  E.g., in NZ, night landings do not need to be to a full-stop.  See https://www.aviation.govt.nz/rules/rule-part/show/61/1/17
        /// </summary>
        public Boolean AllowNightTouchAndGoes
        {
            get { return hasFlag(CurrencyOptionFlags.flagAllowNightTouchAndGo); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagAllowNightTouchAndGo, value); }
        }

        /// <summary>
        /// Determines if day currency requires day landings.  E.g., in NZ, day currency requires day landings.  See https://www.aviation.govt.nz/rules/rule-part/show/61/1/17
        /// </summary>
        public Boolean OnlyDayLandingsForDayCurrency
        {
            get { return hasFlag(CurrencyOptionFlags.flagRequireDayLandingsDayCurrency); }
            set { setCurrencyFlag(CurrencyOptionFlags.flagRequireDayLandingsDayCurrency, value); }
        }

        /// <summary>
        /// Expiration for expired currency
        /// </summary>
        public CurrencyExpiration.Expiration CurrencyExpiration
        {
            get
            {
                return (CurrencyExpiration.Expiration)(((uint)CurrencyFlags & (uint)CurrencyOptionFlags.flagCurrencyExpirationMask) >> 4);
            }
            set
            {
                uint newMask = (uint)value << 4;
                CurrencyFlags = (UInt32)(((uint)CurrencyFlags & ~(uint)CurrencyOptionFlags.flagCurrencyExpirationMask) | newMask);
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

        #region account dates
        /// <summary>
        /// Date account was created
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Date of last web activity
        /// </summary>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Date of last logon
        /// </summary>
        public DateTime LastLogon { get; set; }

        /// <summary>
        /// Date of last password change
        /// </summary>
        public DateTime LastPasswordChange { get; set; }
        #endregion

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
        public DateTime LastBFRInternal { get; protected set; }

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
                else if (available.Any())
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
        /// List of properties ID's that the user has blocklisted from the previously-used list.
        /// </summary>
        public List<int> BlocklistedProperties { get; private set; } = new List<int>();

        /// <summary>
        /// Current status of achievement computation for the user.  
        /// </summary>
        public Achievement.ComputeStatus AchievementStatus { get; set; } = Achievement.ComputeStatus.Never;

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
                catch (Exception ex) when (ex is OutOfMemoryException || ex is ArgumentNullException || ex is TimeZoneNotFoundException || ex is System.Security.SecurityException || ex is InvalidTimeZoneException)
                {
                    return TimeZoneInfo.Utc;
                }
            }
        }

        /// <summary>
        /// Convenience dictionary of associated data for other that want to piggy back on Profile caching.
        /// </summary>
        public IDictionary<string, object> AssociatedData { get; private set; } = new Dictionary<string, object>();
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
                    throw new ArgumentNullException(nameof(szEmail));
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
            return (AssociatedData.TryGetValue(szKey, out object o)) ? o : null;
        }
        #endregion
    }

    /// <summary>
    /// Intermediate class: ProfileBase, but with database and validation capabilities.
    /// </summary>
    [Serializable]
    public abstract class PersistedProfile : ProfileBase
    {
        protected PersistedProfile() : base() { }

        #region Properties
        /// <summary>
        /// Strictly for serialization - gets/sets persistedprefs as a JSON string.  Gets around serialization of JObject
        /// </summary>
        protected string PersistedPrefsAsJSon { get; set; }        

        /// <summary>
        /// Convenience dictionary of preferences that is saved to the database
        /// </summary>
        [NonSerialized]
        private IDictionary<string, object> m_persistedPrefs = new Dictionary<string, object>();

        /// <summary>
        /// Safely retrieves - or recreates - persisted preferences.
        /// </summary>
        private IDictionary<string, object> PersistedPrefsDictionary
        {
            get
            {
                if (m_persistedPrefs == null)
                {
                    if (String.IsNullOrEmpty(PersistedPrefsAsJSon))
                        return m_persistedPrefs = new Dictionary<string, object>();
                    else
                        return m_persistedPrefs = JsonConvert.DeserializeObject<Dictionary<string, object>>(PersistedPrefsAsJSon);
                }
                else
                    return m_persistedPrefs;
            }
            set { m_persistedPrefs = value; }
        }

        private const string prefKeyPreferredGreeting = "preferredGreeting";
        [System.Runtime.Serialization.IgnoreDataMember]
        public string PreferredGreeting
        {
            get { return PreferenceExists(prefKeyPreferredGreeting) ? (string)GetPreferenceForKey(prefKeyPreferredGreeting) : UserFirstName; }
            set { SetPreferenceForKey(prefKeyPreferredGreeting, value, String.IsNullOrEmpty(value)); }
        }

        public IEnumerable<byte> HeadShot { get; set; }

        /// <summary>
        /// Determines if the user has a headshot
        /// </summary>
        [System.Runtime.Serialization.IgnoreDataMember]
        public bool HasHeadShot
        {
            get { return HeadShot != null && HeadShot.Any(); }
        }

        /// <summary>
        /// Returns a link to the user's headshot, if present - else default icon
        /// </summary>
        [System.Runtime.Serialization.IgnoreDataMember]
        public string HeadShotHRef
        {
            get
            {
                bool fHasHeadshot = HasHeadShot;
                return fHasHeadshot ? String.Format(CultureInfo.InvariantCulture, "~/Member/ViewUser.aspx/{0}?h={1}", UserName, HeadShot.GetHashCode()) : VirtualPathUtility.ToAbsolute("~/Public/tabimages/ProfileTab.png");
            }
        }

        private const string prefKeyCell = "mobilePhone";

        [System.Runtime.Serialization.IgnoreDataMemberAttribute]
        public string MobilePhone
        {
            get { return (string)GetPreferenceForKey(prefKeyCell); }
            set { SetPreferenceForKey(prefKeyCell, value, String.IsNullOrEmpty(value)); }
        }

        private const string prefKeyColors = "keywordColors";

        [System.Runtime.Serialization.IgnoreDataMemberAttribute]
        public IEnumerable<FlightColor> KeywordColors
        {
            get { return GetPreferenceForKey<FlightColor[]>(prefKeyColors); }
            set { SetPreferenceForKey(prefKeyColors, value, value == null || !value.Any()); }
        }

        private const string prefDOB = "dateOfBirth";

        [System.Runtime.Serialization.IgnoreDataMemberAttribute]
        public DateTime? DateOfBirth
        {
            get { return GetPreferenceForKey<DateTime?>(prefDOB); }
            set { SetPreferenceForKey(prefDOB, value, value == null || !value.HasValue || !value.Value.HasValue()); }
        }

        [System.Runtime.Serialization.IgnoreDataMemberAttribute]
        public int MathRoundingUnit
        {
            get { return GetPreferenceForKey(MFBConstants.keyMathRoundingUnits, 60); }
            set { SetPreferenceForKey(MFBConstants.keyMathRoundingUnits, value, value == 60); }
        }
        #endregion

        /// <summary>
        /// De-serializes an AuthorizationState from a JSON string, null if the string is null
        /// </summary>
        /// <param name="sz">The JSON string</param>
        /// <returns>The authorization state, or null if sz is null</returns>
        private static IAuthorizationState AuthStateFromString(string sz)
        {
            if (String.IsNullOrEmpty(sz))
                return null;

            if (!sz.StartsWith("{", StringComparison.Ordinal))    // legacy authtoken
            {
                return new AuthorizationState() { AccessToken = sz, AccessTokenExpirationUtc = null, AccessTokenIssueDateUtc = null, RefreshToken = null };
            }

            return JsonConvert.DeserializeObject<AuthorizationState>(sz);
        }

        #region Persisted Preferences
        /// <summary>
        /// Sets the specified object for persistence AND commits the object.  Removes the preference if the object is null.
        /// </summary>
        /// <param name="szKey"></param>
        /// <param name="o"></param>
        /// <param name="fClear">True to clear (remove) the preference</param>
        /// <returns>True for success</returns>
        public bool SetPreferenceForKey(string szKey, object o, bool fClear = false)
        {
            if (string.IsNullOrWhiteSpace(szKey))
                throw new ArgumentNullException(nameof(szKey));

            if (o == null || fClear)
            {
                if (PersistedPrefsDictionary.ContainsKey(szKey))
                    PersistedPrefsDictionary.Remove(szKey);
                else
                    return false;
            } else
            {
                PersistedPrefsDictionary[szKey] = o;
            }

            // update the JSon string.
            PersistedPrefsAsJSon = JsonConvert.SerializeObject(PersistedPrefsDictionary);

            return FCommit();
        }

        /// <summary>
        /// Determines if the preference exists
        /// </summary>
        /// <param name="szKey"></param>
        /// <returns></returns>
        public bool PreferenceExists(string szKey)
        {
            return (!String.IsNullOrEmpty(szKey) && PersistedPrefsDictionary.ContainsKey(szKey));
        }

        /// <summary>
        /// Retrieves the specified object (or default, if not found), casting to the specified type.  A bit slower than the non-generic version because it does an extra serialize/deserialize
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="szKey"></param>
        /// <returns></returns>
        public T GetPreferenceForKey<T>(string szKey)
        {
            if (string.IsNullOrWhiteSpace(szKey))
                throw new ArgumentNullException(nameof(szKey));

            if (PersistedPrefsDictionary.TryGetValue(szKey, out object o))
            {
                // By default, objects deserialized to dynamic.  So re-serialize it and cast.
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(o));
            }
            else
                return default;
        }

        /// <summary>
        /// Retrieves the specified non-nullable integer (or boolean)
        /// </summary>
        /// <param name="szKey"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public T GetPreferenceForKey<T>(string szKey, T defObj)
        {
            if (string.IsNullOrWhiteSpace(szKey))
                throw new ArgumentNullException(nameof(szKey));

            if (PersistedPrefsDictionary.TryGetValue(szKey, out object o))
                return (T)o;
            else
                return defObj;        
        }

        
        /// <summary>
        /// Retrieves the specified integer value. We do this because a cast of a boxed value doesn't work - e.g., if it's an Int64 that's been boxed, you have to do (int) (Int64) o; (int) o doesn't work.
        /// </summary>
        /// <param name="szKey"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        public int GetPreferenceForKey(string szKey, int defValue)
        {
            return (PreferenceExists(szKey)) ? Convert.ToInt32(PersistedPrefsDictionary[szKey], CultureInfo.InvariantCulture) : defValue;
        }

        /// <summary>
        /// Faster alternative to retrive the specified object (or default, if not found), but the return type is "dynamic", so you have to know what you're doing with the result.
        /// </summary>
        /// <param name="szKey"></param>
        /// <returns></returns>
        public dynamic GetPreferenceForKey(string szKey)
        {
            if (string.IsNullOrWhiteSpace(szKey))
                throw new ArgumentNullException(nameof(szKey));

            return PersistedPrefsDictionary.TryGetValue(szKey, out object o) ? o : default;
        }
        #endregion

        #region Database and caching
        protected void InitFromDataReader(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            try
            {
                UserName = dr["Username"].ToString();
                OriginalEmail = Email = dr["Email"].ToString();
                FirstName = dr["FirstName"].ToString();
                LastName = dr["LastName"].ToString();
                Address = util.ReadNullableField(dr, "Address", string.Empty).ToString();
                OriginalPKID = PKID = dr["PKID"].ToString();
                CreationDate = Convert.ToDateTime(dr["CreationDate"], CultureInfo.InvariantCulture);
                LastLogon = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "LastLoginDate", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);
                LastActivity = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "LastActivityDate", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);
                LastPasswordChange = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "LastPasswordChangedDate", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);

                DropboxAccessToken = (string)util.ReadNullableField(dr, "DropboxAccessToken", null);
                GoogleDriveAccessToken = AuthStateFromString((string) util.ReadNullableField(dr, "GoogleDriveAccessToken", null));
                OneDriveAccessToken = AuthStateFromString((string)util.ReadNullableField(dr, "OnedriveAccessToken", null));
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
                CurrencyFlags = (UInt32)Convert.ToUInt32(dr["CurrencyFlags"], CultureInfo.InvariantCulture);
                SecurityQuestion = dr["PasswordQuestion"].ToString();

                Subscriptions = (UInt32)Convert.ToInt32(dr["EmailSubscriptions"], CultureInfo.InvariantCulture);
                LastEmailDate = Convert.ToDateTime(util.ReadNullableField(dr, "LastEmail", DateTime.MinValue), CultureInfo.InvariantCulture);

                AchievementStatus = (Achievement.ComputeStatus)Convert.ToInt16(dr["AchievementStatus"], CultureInfo.InvariantCulture);

                // IsAdmin may not always be present; default to false if not.  This is a bit of a hack.
                ProfileRoles.UserRoles r = Role = ProfileRoles.UserRoles.None;
                if (Enum.TryParse<ProfileRoles.UserRoles>(util.ReadNullableField(dr, "Role", ProfileRoles.UserRoles.None.ToString()).ToString(), out r))
                    Role = r;

                PreferredTimeZoneID = (string) util.ReadNullableField(dr, "timezone", null);

                string szBlockList = util.ReadNullableString(dr, "PropertyBlackList");
                BlocklistedProperties.AddRange(szBlockList.ToInts());

                HeadShot = (byte[])util.ReadNullableField(dr, "HeadShot", null);

                PersistedPrefsDictionary.Clear();
                PersistedPrefsAsJSon = util.ReadNullableString(dr, "prefs");
                if (!String.IsNullOrEmpty(PersistedPrefsAsJSon))
                    PersistedPrefsDictionary = (Dictionary<string, object>) JsonConvert.DeserializeObject<Dictionary<string, object>>(PersistedPrefsAsJSon);
            }
            catch (Exception ex)
            {
                throw new MyFlightbookException("Error reading field - " + ex.Message);
            }
        }

        protected static string GetCacheKey(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));
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

        private static readonly object cachelock = new object();

        protected static void CacheProfile(ProfileBase pf)
        {
            if (pf == null || String.IsNullOrEmpty(pf.UserName))
                return;

            lock (cachelock) {
                string szKey = GetCacheKey(pf.UserName);
                // Issue #1084 - Remove the object, if it exists.
                HttpRuntime.Cache.Remove(szKey);
                // Cache this for 30 minutes
                HttpRuntime.Cache.Add(szKey, pf, null, DateTime.Now.AddMinutes(30), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
            }
        }

        public Boolean FCommit()
        {
            string szQ = "";

            if (!IsValid())
                return false;

            szQ = @"UPDATE users SET Email=?Email, FirstName=?FirstName, LastName=?LastName, Address=?address, 
            DropboxAccessToken=?dropboxAccesstoken, OnedriveAccessToken=?onedrive, GoogleDriveAccessToken=?gdrive, DefaultCloudDriveID=?defcloud, OverwriteDropbox=?overwriteCloud, CloudAhoyAccessToken=?cloudAhoy,
            LastBFR = ?LastBFR, LastMedical=?LastMedical, MonthsToMedical=?MonthsToMedical, IsInstructor=?IsInstructor, UsesSIC=?UsesSIC, UsesHHMM=?UsesHHMM, UsesUTCDates=?useUTCDates, License=?license, CertificateNumber=?cert, CFIExpiration=?cfiExp, 
            CurrencyFlags=?currencyFlags, ShowTimes=?showTimes, EnglishProficiencyExpiration=?engProfExpiration, EmailSubscriptions=?subscriptions, LastEmail=?lastemail, AchievementStatus=?achievementstatus, PropertyBlackList=?blocklist, timezone=?prefTimeZone,
            HeadShot=?hs, prefs=?prefs
            WHERE PKID = ?PKID";

            string szErr = "";
            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery(szQ,
                (comm) =>
                {
                    comm.Parameters.AddWithValue("Email", Email);
                    comm.Parameters.AddWithValue("FirstName", FirstName.LimitTo(32));
                    comm.Parameters.AddWithValue("LastName", LastName.LimitTo(32));
                    comm.Parameters.AddWithValue("address", Address.LimitTo(254));
                    comm.Parameters.AddWithValue("dropboxAccessToken", DropboxAccessToken);
                    comm.Parameters.AddWithValue("onedrive", OneDriveAccessToken == null ? null : JsonConvert.SerializeObject(OneDriveAccessToken));
                    comm.Parameters.AddWithValue("gdrive", GoogleDriveAccessToken == null ? null : JsonConvert.SerializeObject(GoogleDriveAccessToken));
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
                    comm.Parameters.AddWithValue("blocklist", String.Join(",", BlocklistedProperties));
                    comm.Parameters.AddWithValue("hs", HeadShot);
                    comm.Parameters.AddWithValue("prefs", PersistedPrefsAsJSon = JsonConvert.SerializeObject(PersistedPrefsDictionary));
                    comm.Parameters.AddWithValue("prefTimeZone", String.IsNullOrEmpty(PreferredTimeZoneID) || PreferredTimeZoneID.CompareCurrentCultureIgnoreCase("UTC") == 0 ? null : PreferredTimeZoneID);
                });
            if (dbh.LastError.Length == 0)
            {
                szErr = "";
                // save the updated object into the cache
                CacheProfile(this);

                Clubs.Club.UncacheClubsForUser(UserName);
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
                throw new ArgumentNullException(nameof(action));

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
                if (pf != null)
                    CacheProfile(pf);
            }
            return pf;
        }

        /// <summary>
        /// Uncaches the specified user, forcing a database reload next time.
        /// </summary>
        /// <param name="szUserName"></param>
        public static void UncacheUser(string szUserName)
        {
            if (szUserName == null)
                return;

            HttpRuntime.Cache.Remove(GetCacheKey(szUserName));
        }
        #endregion

        public static IEnumerable<Profile> UsersWithSubscriptions(UInt32 subscriptionMask, DateTime dtMin)
        {
            string szQ = String.Format(CultureInfo.InvariantCulture, "SELECT uir.Rolename AS Role, u.* FROM users u LEFT JOIN usersinroles uir ON (u.Username=uir.Username AND uir.ApplicationName='Logbook') WHERE (u.EmailSubscriptions & {0}) <> 0 AND (LastEmail IS NULL OR LastEmail <= '{1}')", subscriptionMask, dtMin.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            DBHelper dbh = new DBHelper(szQ);
            List<Profile> l = new List<Profile>();
            dbh.ReadRows(
                (comm) => { },
                (dr) => { l.Add(new Profile(dr)); });
            return l;
        }

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
            string szOriginalEmail = Email;
            FirstName = szNewFirst;
            LastName = szNewLast;
            Email = szNewEmail ?? throw new ArgumentNullException(nameof(szNewEmail));
            Address = szNewAddress;

            FCommit();

            if (String.Compare(Email, szOriginalEmail, StringComparison.OrdinalIgnoreCase) != 0)
            {
                string szBody = util.ApplyHtmlEmailTemplate(String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.EmailTemplates.ChangeEmailConfirmation), szOriginalEmail, Email), false);
                // Email has changed - send new email to both the old and the new address
                util.NotifyUser(Resources.Profile.EmailChangedSubjectLine, szBody, new MailAddress(szOriginalEmail, UserFullName), false, false);
                util.NotifyUser(Resources.Profile.EmailChangedSubjectLine, szBody, new MailAddress(Email, UserFullName), false, false);
            }
        }

        /// <summary>
        /// Change the security question/answer for the user.
        /// </summary>
        /// <param name="szPass">The user's current password</param>
        /// <param name="szNewQ">The proposed new question</param>
        /// <param name="szNewA">The proposed new answer</param>
        public void ChangeQAndA(string szPass, string szNewQ, string szNewA)
        {
            if (szPass == null)
                throw new ArgumentNullException(nameof(szPass));
            if (szNewQ == null)
                throw new ArgumentNullException(nameof(szNewQ));
            if (szNewA == null)
                throw new ArgumentNullException(nameof(szNewA));
            // see if we need to change question too.
            if ((szNewQ.Length > 0) ^ (szNewA.Length > 0))
                throw new MyFlightbookException(Resources.Profile.errNeedBothQandA);

            if (szNewQ.Length == 0 || szNewA.Length == 0)
                throw new MyFlightbookException(Resources.Profile.errPleaseTypeNewQandA);

            if (!Membership.Provider.ValidateUser(UserName, szPass))
                throw new MyFlightbookException(Resources.Profile.errIncorrectPassword);

            // change both password and question/answer
            if (!Membership.Provider.ChangePasswordQuestionAndAnswer(UserName, szPass, szNewQ, szNewA))
                throw new MyFlightbookException(Resources.Profile.errChangeQandAFailed);

            SecurityQuestion = szNewQ;
            FCommit(); // update the cache
        }

        /// <summary>
        /// Changes the password.
        /// </summary>
        /// <param name="szOld">The previous password - must match</param>
        /// <param name="szNew">The proposed new password</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="MyFlightbookException"></exception>
        public void ChangePassword(string szOld, string szNew)
        {
            try
            {
                if (szOld == null)
                    throw new ArgumentNullException(nameof(szOld));
                if (szNew == null)
                    throw new ArgumentNullException(nameof(szNew));

                if (szOld.Length == 0 || !Membership.ValidateUser(UserName, szOld))
                    throw new MyFlightbookException(Resources.Profile.errBadPasswordToChange);
                UserEntity.ValidatePassword(szNew);  // will throw an exception if length, etc. is wrong.
                if (!Membership.Provider.ChangePassword(UserName, szOld, szNew))
                    throw new MyFlightbookException(Resources.Profile.errChangePasswordFailed);

                // Invalidate our cache
                UncacheUser(UserName);

                util.NotifyUser(String.Format(CultureInfo.CurrentCulture, Resources.Profile.PasswordChangedSubject, Branding.CurrentBrand.AppName),
                    util.ApplyHtmlEmailTemplate(Resources.EmailTemplates.PasswordChanged, false), new MailAddress(Email, UserFullName), false, false);
            }
            catch (UserEntityException ex)
            {
                throw new MyFlightbookException(ex.Message, ex);
            }
        }
        #endregion

        #region Achievements

        private IEnumerable<Badge> m_cachedBadges;

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
        public void SetAchievementStatus(Achievement.ComputeStatus stat = Achievement.ComputeStatus.NeedsComputing)
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
        /// <param name="fGroundOnlyOrATP">True if we can allow a missing expiration</param>
        /// <param name="szError">The error that results</param>
        /// <returns>True if they can sign</returns>
        public bool CanSignFlights(out string szError, bool fGroundOnlyOrATP)
        {
            szError = String.Empty;
            if (String.IsNullOrEmpty(Certificate))
            {
                szError = Resources.SignOff.errSignNoCertificate;
                return false;
            }

            // Error to 
            // (a) lack an expiration date UNLESS the flight is ground-only OR ATP signing
            // (b) have an expiration date in the past
            if ((!CertificateExpiration.HasValue() && !fGroundOnlyOrATP) || 
                (CertificateExpiration.HasValue() && CertificateExpiration.AddDays(1).CompareTo(DateTime.Now) < 0))
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

        #region verified Email
        private const string prefVerifiedEmails = "verifiedEmails";

        private HashSet<string> verifiedEmailsAsSet { get { return GetPreferenceForKey<HashSet<string>>(prefVerifiedEmails) ?? new HashSet<string>(); } }

        public IEnumerable<string> AlternateEmailsForUser()
        {
            HashSet<string> hs = verifiedEmailsAsSet;
            hs.RemoveWhere(sz => sz.CompareOrdinalIgnoreCase(Email) == 0);
            return hs;
        }

        public void AddVerifiedEmail(string szEmail)
        {
            if (String.IsNullOrWhiteSpace(szEmail))
                throw new ArgumentNullException(nameof(szEmail));

            HashSet<string> hs = verifiedEmailsAsSet; 
            hs.Add(szEmail);
            SetPreferenceForKey(prefVerifiedEmails, hs, hs.Count == 0);
        }

        public void DeleteVerifiedEmail(string szEmail)
        {
            if (szEmail == null)
                throw new ArgumentNullException(nameof(szEmail));

            HashSet<string> hs = verifiedEmailsAsSet;
            hs.RemoveWhere(sz => sz.CompareOrdinalIgnoreCase(szEmail) == 0);
            SetPreferenceForKey(prefVerifiedEmails, hs, hs.Count == 0);
        }

        public bool IsVerifiedEmail(string szEmail)
        {
            return !String.IsNullOrWhiteSpace(szEmail) && verifiedEmailsAsSet.FirstOrDefault(sz => sz.CompareOrdinalIgnoreCase(szEmail) == 0) != null;
        }

        public void SendVerificationEmail(string szEmail, string szTargetFormatString)
        {
            if (String.IsNullOrEmpty(szEmail))
                throw new ArgumentNullException(nameof(szEmail));

            PeerRequestEncryptor enc = new PeerRequestEncryptor();
            string encLink = String.Format(CultureInfo.InvariantCulture, szTargetFormatString, HttpUtility.UrlEncode(enc.Encrypt(String.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", DateTime.Now.Ticks, UserName, szEmail))));

            using (MailMessage msg = new MailMessage())
            {
                Brand brand = Branding.CurrentBrand;
                msg.From = new MailAddress(brand.EmailAddress, brand.AppName);
                msg.To.Add(new MailAddress(szEmail, UserFullName));
                msg.Subject = Branding.ReBrand(Resources.Profile.accountVerifySubject);
                msg.IsBodyHtml = true;
                util.PopulateMessageContentWithTemplate(msg, Resources.Profile.VerifyEmail.Replace("<% email %>", szEmail).Replace("<% confirmaddress %>", encLink));

                util.SendMessage(msg);
            }
        }

        public bool VerifyEmail(string encodedResponse, out string szEmail, out string szError)
        {
            if (String.IsNullOrEmpty(encodedResponse))
                throw new ArgumentNullException(nameof(encodedResponse));

            PeerRequestEncryptor enc = new PeerRequestEncryptor();
            string szDecoded = enc.Decrypt(encodedResponse);
            szError = szEmail = string.Empty;

            if (String.IsNullOrEmpty(szDecoded))
            {
                szError = Resources.Profile.accountVerifyInvalid;
                return false;
            }

            string[] rgParts = szDecoded.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (rgParts.Length != 3 ||
                rgParts[1].CompareTo(UserName) != 0 ||
                !Int64.TryParse(rgParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks)) 
            {
                szError = Resources.Profile.accountVerifyInvalid;
                return false;
            }

            // Give up to 1 hour to verify.
            Int64 elapsedTicks = DateTime.Now.Ticks - ticks;

            if (elapsedTicks < 0 || (elapsedTicks / (3600.0 * 1000 * 10000)) > 1.0)
            {
                szError = Resources.Profile.accountVerifyEmailExpired;
                return false;
            }

            szEmail = rgParts[2];
            return true;
        }
        #endregion
    }

    public class NewUserStats
    {
        public DateTime DisplayPeriod { get; set; }
        public int NewUsers { get; set; }
        public int RunningTotal { get; set; }
        public NewUserStats()
        {
        }
        public NewUserStats(DateTime dt, int numUsers, int total)
        {
            DisplayPeriod = dt;
            NewUsers = numUsers;
            RunningTotal = total;
        }
    }

    public class ProfileAdmin : Profile
    {
        public static bool ValidateUser(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));
            if (szUser.Contains("@"))
                szUser = Membership.GetUserNameByEmail(szUser);

            MembershipUser mu = Membership.GetUser(szUser);
            return (mu != null) && (mu.UserName.Length > 0);
        }

        public static MembershipUser ADMINUserFromName(string szName)
        {
            if (szName == null)
                throw new ArgumentNullException(nameof(szName));
            MembershipProvider mp = Membership.Providers["AdminMembershipProvider"];
            String szUserToReset = szName;

            // convert an email to a username, if necessary
            if (szUserToReset.Contains("@"))
                szUserToReset = mp.GetUserNameByEmail(szUserToReset);

            return mp.GetUser(szUserToReset, false);
        }

        /// <summary>
        /// Delete all unused aircraft for the specified user
        /// </summary>
        /// <param name="szUser">The user's username</param>
        /// <returns>The number of aircraft deleted</returns>
        public static int DeleteUnusedAircraftForUser(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));
            UserAircraft ua = new UserAircraft(szUser);

            int i = 0;
            IEnumerable<Aircraft> lst = ua.GetAircraftForUser();
            foreach (Aircraft ac in lst)
            {
                try
                {
                    // FDeleteAircraftForUser throws a MyFlightbookException if the aircraft is in use.
                    ua.FDeleteAircraftforUser(ac.AircraftID);
                    i++;
                }
                catch (MyFlightbookException) { }
            }
            return i;
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
                    LogbookBackup lb = new LogbookBackup(pf) { IncludeImages = false };
                    using (FileStream fs = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.Read, Int16.MaxValue, FileOptions.DeleteOnClose))
                    {
                        lb.LogbookDataForBackup(fs);
                        fs.Seek(0, SeekOrigin.Begin);
                        msg.Attachments.Add(new Attachment(fs, lb.BackupFilename(brand), "text/csv"));
                        util.SendMessage(msg);
                    }
                }
            }
            catch (Exception ex) when (ex is ArgumentNullException || ex is TimeoutException || ex is InvalidOperationException) { }
        }

        private static void DeleteUserImages(DBHelper dbh)
        {
            List<MFBImageInfo> lstMfbii = new List<MFBImageInfo>();
            dbh.CommandText = @"SELECT f.idflight, f.username, i.* 
                        FROM images i 
                        LEFT JOIN flights f ON CAST(f.idflight AS CHAR(15) CHARACTER SET utf8mb4) COLLATE utf8mb4_unicode_ci=i.imagekey
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

        public static void DeleteForUser(string szUser, DeleteLevel dl)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));

            MembershipUser mu = Membership.GetUser(szUser, false);
            if (mu == null)
                return;

            DeleteForUser(mu, dl);
        }

        public static void DeleteFlightsForUser(string szUser)
        {
            DeleteForUser(szUser, DeleteLevel.OnlyFlights);
        }

        public static void DeleteEntireUser(string szUser)
        {
            DeleteForUser(szUser, DeleteLevel.EntireUser);
            FormsAuthentication.SignOut();
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
            il.Class = MFBImageInfo.ImageClass.OfflineEndorsement;
            il.Refresh(true);
            foreach (MFBImageInfo mfbii in il.ImageArray)
                mfbii.DeleteImage();

            // Delete basicmed records
            foreach (BasicMedEvent bme in BasicMedEvent.EventsForUser(mu.UserName))
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
        static public void FinalizeUser(string szUser, string szFirst, string szLast)
        {
            Profile pf = Profile.GetUser(szUser);
            pf.FirstName = szFirst;
            pf.LastName = szLast;
            pf.TracksSecondInCommandTime = pf.IsInstructor = true;
            pf.FCommit();

            // send welcome mail
            util.NotifyUser(String.Format(CultureInfo.CurrentCulture, Resources.Profile.WelcomeTitle, Branding.CurrentBrand.AppName), util.ApplyHtmlEmailTemplate(Resources.EmailTemplates.WelcomeEmail, true), new MailAddress(pf.Email, pf.UserFullName), false, true);
        }

        /// <summary>
        /// Report on the # of new users by year/month for all time, including running total
        /// </summary>
        /// <returns></returns>
        static public IEnumerable<NewUserStats> ADMINUserStatsByTime()
        {
            List<NewUserStats> lst = new List<NewUserStats>();
            int runningTotal = 0;
            DBHelper dbh = new DBHelper(@"SELECT
    CAST(CONCAT(YEAR(CreationDate), '-', MONTHNAME(CreationDate)) AS CHAR) AS 'DisplayPeriod',
    DATE(CreationDate) AS CDate,
    COUNT(DISTINCT(username)) AS NewUsers
FROM
	users
    GROUP BY YEAR(CreationDate), MONTH(CreationDate)
    ORDER by YEAR(CreationDate) ASC, MONTH(CreationDate) ASC;");

            dbh.ReadRows((comm) => { },
                (dr) =>
                {
                    int newUsers = Convert.ToInt32(dr["NewUsers"], CultureInfo.InvariantCulture);
                    runningTotal += newUsers;
                    lst.Add(new NewUserStats(Convert.ToDateTime(dr["CDate"], CultureInfo.InvariantCulture), newUsers, runningTotal));
                });
            return lst;
        }

        /// <summary>
        /// Report on the number of new users by date for up to the specified limit, including running totals
        /// </summary>
        /// <param name="days"></param>
        /// <returns></returns>
        static public IEnumerable<NewUserStats> ADMINDailyNewUsers(int days = 40)
        {
            int runningTotal = 0;
            List<NewUserStats> newUserStats = new List<NewUserStats>();
            DBHelper dbh = new DBHelper(@"SELECT 
    DATE(creationdate) AS CDate, COUNT(username) AS NewUsers
FROM
    users
GROUP BY CDate
ORDER BY CDate DESC
LIMIT ?days");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("days", days); },
                (dr) =>
                {
                    int newUsers = Convert.ToInt32(dr["NewUsers"], CultureInfo.InvariantCulture);
                    runningTotal += newUsers;
                    newUserStats.Add(new NewUserStats(Convert.ToDateTime(dr["CDate"], CultureInfo.InvariantCulture), newUsers, runningTotal));
                });

            return newUserStats;
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
                throw new ArgumentNullException(nameof(szEmail));
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
        public static void ValidatePassword(string szPass)
        {
            if (String.IsNullOrEmpty(szPass))
                throw new UserEntityException(Resources.Profile.errNoPassword, MembershipCreateStatus.InvalidPassword);

            if (szPass.Length < 8 || szPass.Length > 48)
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

            // Validate - this will throw a UserEntityException if there is an issue.
            ValidateEmailAndUser(szEmail, szUser);
            ValidatePassword(szPass);
            ValidateQandA(szQuestion, szAnswer);

            // If we are here, everything has been validated
            nvc.Add("applicationName", "Online Flight Logbook");
            nvc.Add("connectionStringName", "logbookConnectionString");

            mmp.Initialize(null, nvc);

            MembershipUser mu = mmp.CreateUser(szUser, szPass, szEmail, szQuestion, szAnswer, true, Guid.NewGuid(), out MembershipCreateStatus result);

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
                throw new ArgumentOutOfRangeException(nameof(id), "pending password reset with ID " + id + "was not found");
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
            Profile.UncacheUser(UserName);
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
                throw new ArgumentNullException(nameof(dr));
            uint perms = Convert.ToUInt32(dr["CanViewStudent"], CultureInfo.InvariantCulture);
            CanViewLogbook = (perms & (uint) PermissionMask.ViewLogbook) != 0;
            CanAddLogbook = (perms & (uint)PermissionMask.AddLogbook) != 0;
        }
    }
}