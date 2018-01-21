using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Text;
using System.Web;
using MySql.Data.MySqlClient;

/******************************************************
 * 
 * Copyright (c) 2008-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    public enum CFPPropertyType { cfpInteger, cfpDecimal, cfpBoolean, cfpDate, cfpDateTime, cfpString, cfpCurrency };

    /// <summary>
    /// Describes a flight property, including it's data type, display characteristics, and any flagged semantics.
    /// </summary>
    [Serializable]
    public class CustomPropertyType
    {
        /// <summary>
        /// ID's of known properties.  We do this as a nested static class to encapsulate it.
        /// Most of these are flagged as known properties in the database so that they get pulled for currency and other computations,
        /// others are still here but not flagged; they are used either for display purposes or for property synthesis (e.g., when importing).
        /// </summary>
        public enum KnownProperties
        {
            // Known properties
            // It's a bit more fragile than a flag, as it is sensitive to renumbering/deletion of properties, and can only apply to a single property
            // But as a practical matter renumbering/deletion doesn't happen and these are unlikely to expand beyond the single property.
            IDPropInvalid = -1,
            None = 0,
            IDPropTachStart = 95,
            IDPropTachEnd = 96,
            IDBlockOut = 187,
            IDBlockIn = 186,
            IDPropCheckridePPL = 39,
            IDPropCheckrideIFR = 40,
            IDPropCheckrideCommercial = 42,
            IDPropCheckrideATP = 45,
            IDPropCheckrideSport = 89,
            IDPropSolo = 77,                    // Not flagged as a known property
            IDPropWaterLandings = 36,           // Not flagged as a known property
            IDPropWaterTakeoffs = 74,           // Not flagged as a known property
            IDPropNVLandings = 51,              // Not flagged as a known property
            IDPropFlightReview = 164,           // Not flagged as a known property
            IDPropIPC = 41,                     // Not flagged as a known property
            IDPropPassengerNames = 120,         // Not flagged as a known property
            IDPropPassengerCount = 316,         // Not flagged as a known property
            IDPropScheduledArrival = 503,       // Not flagged as a known property
            IDPropScheduledDeparture = 502,     // Not flagged as a known property
            IDPropFlightNumber = 156,           // Not flagged as a known property
            IDPropPilotFlyingTime = 529,        // Not flagged as a known property
            IDPropInstructorName = 92,          // Not flagged as a known property
            IDPropCheckRide = 43,               // Not flagged as a known property
            IDPropStudentName = 166,            // Not flagged as a known property - just used for copying to instructor's flight.
            IDPropCheckrideRecreational = 131,  
            IDPropCheckrideCFI = 176,
            IDPropCheckrideCFII = 177,
            IDPropCheckrideMEI = 225,
            IDPropNightTakeoff = 73,
            IDPropNameOfPIC = 183,              // Note: this one isn't flagged as a known property, just used for logbookentrydisplay to print.
            IDPropNameOfSIC = 184,              // Not flagged as a known property
            IDPropDutiesOfPIC = 185,
            IDPropPICUS = 279,
            IDPropInstructorOnBoard = 288,
            IDPropMaximumAltitude = 321,
            IDPropDutyTimeStart = 332,
            IDPropDutyTimeEnd = 333,
            IDPropNVGoggleOperations = 355,
            IDPropNVGoggleProficiencyCheck = 55,
            IDPropNVGoggleTime = 26,
            IDPropNVFLIRTime = 68,
            IDPropCAP5Checkride = 232,
            IDPropCAP91Checkride = 258,
            IDPropFlightEngineerTime = 257,
            IDPropTakeoffTowered = 357,
            IDPropTakeoffToweredNight = 358,
            IDPropLandingTowered = 245,
            IDPropLandingToweredNight = 244,
            IDPropGroundInstructionGiven = 198,
            IDPropGroundInstructionReceived = 158,
            IDProp135293Knowledge = 427,
            IDProp135293Competency = 428,
            IDProp135299FlightCheck = 429,
            IDPropFrontSeatTime = 69,
            IDPropBackSeatTime = 70,
            IDPropHoistOperations = 118,
            IDPropHighAltitudeLandings = 504,
            IDPropUASKnowledgeTest10773 = 527,
            IDPropUASTrainingCourse10774 = 528,
            IDPropMotorgliderSelfLaunch = 227,
            IDPropGliderTowedLaunch = 222,
            IDPropGliderMaxAltitude = 223,
            IDPropGliderReleaseAltitude = 224,
            IDPropPilotMonitoring = 560,
            IDPropMonitoredDayLandings = 562,
            IDPropMonitoredNightLandings = 563,
            IDPropMonitoredNightTakeoffs = 565,
            IDPropNightTouchAndGo = 397,
            IDPropFMSApproaches = 583,
            IDPropCFIITime = 192,
            IDPropInstrumentInstructionTime = 542
        }

        internal static class CFPPropertyFlag
        {
            // Flags indicating semantics for a given property.
            // TODO: Some of these should probably move to known property ID's because they are unique to a single property.
            public const UInt32 cfpFlagNone = 0x00000000;
            public const UInt32 cfpFlagBFR = 0x00000001;
            public const UInt32 cfpFlagIPC = 0x00000002;
            public const UInt32 cfpFlagUADescending = 0x00000004;
            public const UInt32 cfpFlagUAAscending = 0x00000008;
            public const UInt32 cfpFlagNightVisionTakeoff = 0x00000010;
            public const UInt32 cfpFlagNightVisionLanding = 0x00000020;
            public const UInt32 cfpFlagNightVisionHover = 0x00000040;
            public const UInt32 cfpFlagNightVisionDepartureAndArrival = 0x00000080;
            public const UInt32 cfpFlagNightVisionTransitions = 0x00000100;
            public const UInt32 cfpFlagNightVisionProficiencyCheck = 0x00000200;
            public const UInt32 cfpFlagGliderInstrumentManeuvers = 0x00000400;
            public const UInt32 cfpFlagGliderInstrumentManeuversPassengers = 0x00000800;
            public const UInt32 cfpFlagIsApproach = 0x00001000;
            public const UInt32 cfpNosum = 0x00002000;
            public const UInt32 cfpFlagNightTakeOff = 0x00004000;
            public const UInt32 cfpFlagNightVisionTime = 0x00008000;
            public const UInt32 cfpFlagPICProfCheck = 0x00010000;
            public const UInt32 cfpFlagAnyNightVision = (cfpFlagNightVisionTime | cfpFlagNightTakeOff | cfpFlagNightVisionDepartureAndArrival | cfpFlagNightVisionHover | cfpFlagNightVisionLanding | cfpFlagNightVisionProficiencyCheck | cfpFlagNightVisionTakeoff | cfpFlagNightVisionTransitions);
            public const UInt32 cfpFlagBaseCheck = 0x00020000;
            public const UInt32 cfpFlagSolo = 0x00040000;
            public const UInt32 cfpFlagGliderGroundLaunch = 0x00080000;
            public const UInt32 cfpFlagExcludeMRU = 0x00100000;
            public const UInt32 cfpFlagBasicDecimal = 0x00200000;
            public const UInt32 cfpFlagsUASLaunch = 0x00400000;
            public const UInt32 cfpFlagsUASRecovery = 0x00800000;
            public const UInt32 cfpFlagKnownProperty = 0x01000000; // Generic "known property" to pull in for currency, ratings progress.
        };

        private const string szAppCacheKey = "keyCustomPropertyTypes";

        #region properties.
        /// <summary>
        /// ID for the proptype
        /// </summary>
        public int PropTypeID { get; set; }

        /// <summary>
        /// Title for the property
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Title in sorting order.  Enables things like Block Out or Tach Start to sort before Block In or Tach End.
        /// </summary>
        public string SortKey { get; set; }

        /// <summary>
        /// Is this a "favorite" for the current user?
        /// </summary>
        public Boolean IsFavorite { get; set; }

        /// <summary>
        /// Format string for the property
        /// </summary>
        public string FormatString { get; set; }

        /// <summary>
        /// Type of property
        /// </summary>
        public CFPPropertyType Type { get; set; }

        /// <summary>
        /// Description of the property and how to use it
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Flags for this property type - e.g., does it count as a BFR or an IPC?
        /// </summary>
        public UInt32 Flags { get; set; }

        /// <summary>
        /// For string properties, contains previously used values
        /// </summary>
        public string[] PreviousValues { get; set; }

        #region Attributes from flags
        /// <summary>
        /// Is this property a BFR event?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsBFR
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagBFR) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this property a Proficiency check as per 61.58?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsPICProficiencyCheck6158
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagPICProfCheck) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this property an IPC event?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsIPC
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagIPC) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this property an approach of some kind?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsApproach
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagIsApproach) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this an unusual attitude, ascending?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsUnusualAttitudeAscending
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagUAAscending) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this an unusual attitude, descending?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsUnusualAttitudeDescending
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagUADescending) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this maneuvers in a glider?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsGliderInstrumentManeuvers
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagGliderInstrumentManeuvers) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this instrument maneuvers with passengers in a glider?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsGliderInstrumentManeuversPassengers
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagGliderInstrumentManeuversPassengers) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this a night-time takeoff?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsNightTakeOff
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagNightTakeOff) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this a night vision proficiency check?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsNightVisionProficiencyCheck
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagNightVisionProficiencyCheck) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this night vision time?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsNightVisionTime
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagNightVisionTime) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this a night-vision related property?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsNightVisionAnything
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagAnyNightVision) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this a base-check (i.e., flight with an instructor that isn't specifically a BFR or other flight review
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsBaseCheck
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagBaseCheck) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this a UAS (Unmanned Aerial System) Launch?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsUASLaunch
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagsUASLaunch) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this a UAS (Unmanned Aerial System) Recovery?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsUASRecovery
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagsUASRecovery) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is solo time recorded?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsSolo
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagSolo) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Does this property represent a glider ground launch?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsGliderGroundLaunch
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagGliderGroundLaunch) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// If this is a property that is typically used only once, exclude it from the MRU list.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsExcludedFromMRU
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagExcludeMRU) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this a "known" property - one where the ID is specifically known?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsKnownProperty
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagKnownProperty) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this property a decimal but not a time?  (i.e., a distance or a weight)
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsBasicDecimal
        {
            get { return Type == CFPPropertyType.cfpDecimal && ((Flags & CFPPropertyFlag.cfpFlagBasicDecimal) != CFPPropertyFlag.cfpFlagNone); }
        }
        #endregion
        #endregion
        /// <summary>
        /// Is this valid?
        /// </summary>
        /// <returns></returns>
        public Boolean IsValid()
        {
            return (PropTypeID != (int) CustomPropertyType.KnownProperties.IDPropInvalid);
        }

        #region Constructors
        public CustomPropertyType()
        {
            FormatString = Title = SortKey = String.Empty;
            Type = CFPPropertyType.cfpInteger;
            Flags = CFPPropertyFlag.cfpFlagNone;
            PropTypeID = (int) KnownProperties.IDPropInvalid;
            PreviousValues = new string[0];
        }

        public CustomPropertyType(int id) : this()
        {
            DBHelper dbh = new DBHelper(String.Format(@"
SELECT cpt.*, COALESCE(l.Text, cpt.Title) AS LocTitle, 
    COALESCE(l2.Text, cpt.FormatString) AS LocFormatString, 
    COALESCE(l3.Text, cpt.Description) AS LocDescription,
    0 AS IsFavorite,
    '' AS prevValues 
FROM custompropertytypes cpt 
LEFT JOIN LocText l ON (l.idTableID=2 AND l.idItemID=cpt.idPropType AND l.LangId=?langID) 
LEFT JOIN locText l2 ON (l2.idTableID=3 AND l2.idItemID=cpt.idPropType AND l2.LangID=?langID) 
LEFT JOIN locText l3 ON (l3.idTableID=4 AND l3.idItemID=cpt.idPropType AND l3.LangID=?langID) 
WHERE idPropType = {0} ORDER BY Title ASC", id));

            if (!dbh.ReadRow(
                (comm) => { comm.Parameters.AddWithValue("langID", System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName); },
                (dr) => { InitFromDataReader(dr); }))
                throw new MyFlightbookException(dbh.LastError);
        }

        public CustomPropertyType(CustomPropertyType.KnownProperties id) : this((int)id) { }

        public CustomPropertyType(MySqlDataReader dr) : this()
        {
            InitFromDataReader(dr);
        }

        private void InitFromDataReader(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            PropTypeID = Convert.ToInt32(dr["idPropType"], CultureInfo.InvariantCulture);
            Title = dr["LocTitle"].ToString();
            FormatString = dr["LocFormatString"].ToString();
            Type = (CFPPropertyType)Convert.ToInt32(dr["Type"], CultureInfo.InvariantCulture);
            SortKey = Convert.ToString(dr["SortKey"], CultureInfo.CurrentCulture);
            if (String.IsNullOrEmpty(SortKey))
                SortKey = Title;
            Flags = (UInt32)Convert.ToInt32(dr["Flags"], CultureInfo.InvariantCulture);
            IsFavorite = Convert.ToBoolean(dr["IsFavorite"], CultureInfo.InvariantCulture);
            Description = Convert.ToString(dr["LocDescription"], CultureInfo.InvariantCulture);
            string szPreviousVals = Convert.ToString(dr["prevValues"], CultureInfo.InvariantCulture);
            if (!String.IsNullOrEmpty(szPreviousVals))
            {
                PreviousValues = szPreviousVals.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                Array.Sort<string>(PreviousValues);
            }
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}{1} - {2}", PropTypeID, IsFavorite ? "*" : " ", Title);
        }

        private static void AppendIfFlagged(StringBuilder sb, UInt32 flag, uint flagToTest, string szText)
        {
            if ((flag & flagToTest) != 0)
                sb.Append(szText);
        }

        /// <summary>
        /// Admin only - provides a human readable display of the various flags.  So it's OK that it's not localized
        /// </summary>
        /// <param name="t">The type</param>
        /// <param name="cfp">The flags</param>
        /// <returns>Human-readable description</returns>
        public static string AdminFlagsDesc(CFPPropertyType t, UInt32 cfp)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat(CultureInfo.CurrentCulture, "{0}: ", t.ToString().Substring(3));

            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagBFR, "BFR;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagGliderInstrumentManeuvers, "Glider instrument maneuvers;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagGliderInstrumentManeuversPassengers, "Glider instrument maneuvers with passengers;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagIPC, "IPC;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagIsApproach, "Approach;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagNightTakeOff, "Night Takeoff;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagNightVisionDepartureAndArrival, "Night vision departure/arrival;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagNightVisionHover, "Night vision hover;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagNightVisionLanding, "Night vision landing;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagNightVisionProficiencyCheck, "Night Vision proficiency check;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagNightVisionTakeoff, "Night vision takeoff;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagNightVisionTransitions, "Night vision transitions;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagNightVisionTime, "Night vision time;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagUAAscending, "Unusual Attitudes (Ascending);");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagUADescending, "Unusual attitudes Descending;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagPICProfCheck, "Part 61.58 PIC Proficiency Check;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpNosum, "No Sum;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagSolo, "Solo;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagExcludeMRU, "Exclude from MRU;");
            if (t == CFPPropertyType.cfpDecimal && (cfp & CFPPropertyFlag.cfpFlagBasicDecimal) != 0) sb.Append("Decimal, but not a time;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagsUASLaunch, "UAS Launch;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagsUASRecovery, "UAS Recovery;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagKnownProperty, "Known Property");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagGliderGroundLaunch, "Glider GroundLaunch");

            string sz = sb.ToString();

            if (sz.EndsWith(";", StringComparison.Ordinal))
                sz = sz.Remove(sz.Length - 1);
            if (sz.EndsWith(": ", StringComparison.Ordinal))
                sz = sz.Remove(sz.Length - 2);
            return sz;
        }

        private static HttpApplicationState AppCache
        {
            get { return (HttpContext.Current != null && HttpContext.Current.Application != null) ? HttpContext.Current.Application : null; }
        }

        private static System.Web.SessionState.HttpSessionState Session
        {
            get { return (HttpContext.Current != null && HttpContext.Current.Session != null) ? HttpContext.Current.Session : null; }
        }

        public static void FlushCache()
        {
            HttpApplicationState appCache = CustomPropertyType.AppCache;
            if (appCache != null)
                appCache.Remove(szAppCacheKey);
        }

        private static string SessionKey(string szUser)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}{1}", szAppCacheKey, szUser);
        }

        public static void FlushUserCache(string szUser)
        {
            if (Session != null)
                Session.Remove(SessionKey(szUser));
        }

        /// <summary>
        /// Return a list of custom property types with ID's in the list
        /// </summary>
        /// <param name="lstIds">The list of the id's</param>
        /// <returns>An array of custom property types</returns>
        public static IEnumerable<CustomPropertyType> GetCustomPropertyTypes(IEnumerable<int> lstIds)
        {
            List<CustomPropertyType> lstResult = new List<CustomPropertyType>();
            if (lstIds == null)
                return lstResult;

            List<CustomPropertyType> lstAll = new List<CustomPropertyType>(GetCustomPropertyTypes());
            foreach (int id in lstIds)
            {
                CustomPropertyType cpt = lstAll.Find(cpt2 => cpt2.PropTypeID == id);
                if (cpt != null)
                    lstResult.Add(cpt);
            }
            return lstResult;
        }

        /// <summary>
        /// Get custom property types for the specified user, or the global list (if szUser is null or empty)
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <param name="fForceLoad">true to bypass cache (force hit of database); ignored if szUser is null or empty</param>
        /// <returns>An array of custom property types</returns>
        public static CustomPropertyType[] GetCustomPropertyTypes(string szUser = null, bool fForceLoad = false)
        {
            List<CustomPropertyType> ar = new List<CustomPropertyType>();
            System.Web.SessionState.HttpSessionState sess = Session;
            HttpApplicationState appcache = AppCache;
            string szSessKey = string.Empty;

            // if szUser is null or empty, we want to cache this in the application cache (shared across all users)
            // otherwise, we store it in the session cache
            // So first we check if is cached and return it if so.
            if (String.IsNullOrEmpty(szUser))
            {
                if (appcache != null && appcache[szAppCacheKey] != null)
                    return (CustomPropertyType[])appcache[szAppCacheKey];
            }
            else
            {
                szSessKey = SessionKey(szUser);
                if (!fForceLoad && sess != null && sess[szSessKey] != null)
                    return (CustomPropertyType[])sess[szSessKey];
            }

            // CustomPropsForUserQuery will work with an empty szUser, but it is SLOW, so if no user is specified, use a faster query.
            string szQ;
            if (String.IsNullOrEmpty(szUser))
                szQ = @"SELECT cpt.*,
COALESCE(l.Text, cpt.Title) AS LocTitle, 
COALESCE(l2.Text, cpt.FormatString) AS LocFormatString,
COALESCE(l3.Text, cpt.Description) AS LocDescription,
0 AS IsFavorite,
'' AS prevValues 
FROM custompropertytypes cpt 
LEFT JOIN LocText l ON (l.idTableID=2 AND l.idItemID=cpt.idPropType AND l.LangId=?langID) 
LEFT JOIN locText l2 ON (l2.idTableID=3 AND l2.idItemID=cpt.idPropType AND l2.LangID=?LangID)
LEFT JOIN locText l3 ON (l3.idTableID=4 AND l3.idItemID=cpt.idPropType AND l3.LangID=?LangID)
ORDER BY IF(SortKey='', Title, SortKey) ASC";
            else
            {
                Profile pf = Profile.GetUser(szUser);
                szQ = String.Format(ConfigurationManager.AppSettings["CustomPropsForUserQuery"].ToString(), pf.BlacklistedProperties.Count == 0 ? string.Empty : String.Format(" AND fp.idPropType NOT IN ('{0}') ", String.Join("', '", pf.BlacklistedProperties)));
            }

            DBHelper dbh = new DBHelper(szQ);

            if (!dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("langID", System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);
                    comm.Parameters.AddWithValue("uname", szUser);
                },
                (dr) => { ar.Add(new CustomPropertyType(dr)); }))
                throw new MyFlightbookException(dbh.LastError);

            CustomPropertyType[] rgcpt = ar.ToArray();

            // cache it.
            if (String.IsNullOrEmpty(szUser))
            {
                if (appcache != null)
                    appcache[szAppCacheKey] = rgcpt;
            }
            else
            {
                if (sess != null)
                    sess[szSessKey] = rgcpt;
            }

            return rgcpt;
        }
    }

    public class IndexedPropertyCollection
    {
        private Dictionary<int, List<CustomFlightProperty>> m_dict;

        public IEnumerable<CustomFlightProperty> PropertiesForFlight(int idFlight)
        {
            if (m_dict.ContainsKey(idFlight))
                return m_dict[idFlight];
            return new CustomFlightProperty[0];
        }
        /// <summary>
        /// Returns an IDictionary of the specified properties, keyed on flight ID
        /// </summary>
        /// <param name="lst">An enumerable set of custom flight properties</param>
        /// <returns></returns>
        public IndexedPropertyCollection(IEnumerable<CustomFlightProperty> lst)
        {
            if (lst == null)
                throw new ArgumentNullException("lst");
            m_dict = new Dictionary<int, List<CustomFlightProperty>>();
            foreach (CustomFlightProperty cfp in lst)
            {
                if (!m_dict.ContainsKey(cfp.FlightID))
                    m_dict[cfp.FlightID] = new List<CustomFlightProperty>();
                m_dict[cfp.FlightID].Add(cfp);
            }
        }
    }

    /// <summary>
    /// An actual instance of a CustomPropertyType - a flight property.  This is ALWAYS tied to a particular flight.
    /// </summary>
    [Serializable]
    public class CustomFlightProperty
    {
        private const string szSelectBase = @"
SELECT fdc.*, 
cpt.*, 
COALESCE(l.Text, cpt.Title) AS LocTitle, 
COALESCE(l2.Text, cpt.FormatString) AS LocFormatString, 
COALESCE(l3.Text, cpt.Description) AS LocDescription,
0 AS IsFavorite, '' AS prevValues 
FROM flightproperties fdc 
INNER JOIN custompropertytypes cpt ON fdc.idPropType=cpt.idPropType 
LEFT JOIN LocText l ON (l.idTableID=2 AND l.idItemID=cpt.idPropType AND l.LangId=?langID) 
LEFT JOIN locText l2 ON (l2.idTableID=3 AND l2.idItemID=cpt.idPropType AND l2.LangID=?langID) 
LEFT JOIN locText l3 ON (l3.idTableID=4 AND l3.idItemID=cpt.idPropType AND l3.LangID=?langID)
{0}";
        private const string szDeleteBase = "DELETE FROM flightproperties WHERE idProp=?idProp";
        private const string szReplaceBase = "REPLACE INTO flightproperties SET idFlight=?idFlight, idPropType=?idProptype, intValue=?IntValue, DecValue=?DecValue, DateValue=?DateValue, StringValue=?StringValue";

        private Boolean boolValue = true;
        private CustomPropertyType m_cpt = new CustomPropertyType();

        public const int idCustomFlightPropertyNew = -1;

        #region Properties
        /// <summary>
        /// The primary key for this property
        /// </summary>
        public int PropID { get; set; }

        /// <summary>
        /// The flight with which this property is associated
        /// </summary>
        public int FlightID { get; set; }

        /// <summary>
        /// The ID of the propertytype this represents
        /// </summary>
        public int PropTypeID { get; set; }

        /// <summary>
        /// The integer value for this property, if appropriate
        /// </summary>
        public int IntValue { get; set; }

        /// <summary>
        /// The boolean value for this property, if appropriate
        /// </summary>
        public Boolean BoolValue
        {
            get { return boolValue; }
            set { boolValue = value; }
        }

        /// <summary>
        /// The decimal value for this property, if appropriate
        /// </summary>
        public Decimal DecValue { get; set; }

        /// <summary>
        /// The date or date-time for this property, if appropriate
        /// </summary>
        public DateTime DateValue { get; set; }

        /// <summary>
        /// Any textual value for this property, if appropriate.
        /// </summary>
        public string TextValue { get; set; }

        /// <summary>
        /// Return a string containing just the relevant value - read-only
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string ValueString
        {
            get
            {
                switch (this.PropertyType.Type)
                {
                    case CFPPropertyType.cfpBoolean:
                        return boolValue ? Resources.LogbookEntry.PropertyYes : Resources.LogbookEntry.PropertyNo;
                    case CFPPropertyType.cfpDate:
                        return DateValue.ToShortDateString();
                    case CFPPropertyType.cfpDateTime:
                        return DateValue.ToShortDateString() + Resources.LocalizedText.LocalizedSpace + DateValue.ToShortTimeString();
                    case CFPPropertyType.cfpDecimal:
                        return DecValue.ToString("0.0#", CultureInfo.CurrentCulture);
                    case CFPPropertyType.cfpCurrency:
                        return DecValue.ToString("0.00", CultureInfo.CurrentCulture);
                    case CFPPropertyType.cfpInteger:
                        return IntValue.ToString(CultureInfo.CurrentCulture);
                    case CFPPropertyType.cfpString:
                        return TextValue;
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Return a string containing just the relevant value - read-only, culture invariant
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string ValueStringInvariant
        {
            get
            {
                switch (this.PropertyType.Type)
                {
                    case CFPPropertyType.cfpBoolean:
                        return boolValue ? "true" : "false";
                    case CFPPropertyType.cfpDate:
                        return DateValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    case CFPPropertyType.cfpDateTime:
                        return DateValue.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    case CFPPropertyType.cfpDecimal:
                        return DecValue.ToString("0.0#", CultureInfo.InvariantCulture);
                    case CFPPropertyType.cfpCurrency:
                        return DecValue.ToString("0.00", CultureInfo.InvariantCulture);
                    case CFPPropertyType.cfpInteger:
                        return IntValue.ToString(CultureInfo.InvariantCulture);
                    case CFPPropertyType.cfpString:
                        return TextValue;
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Formatted Display string (i.e., including label) for the property - read-only
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string DisplayString
        {
            get { return string.Format(CultureInfo.CurrentCulture, PropertyType.FormatString, ValueString); }
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "ID: {0} TypeID: {1} Val: \"{2}\"", this.PropID, this.PropTypeID, DisplayString);
        }

        /// <summary>
        /// Cached customproperty type for quick reading.  THIS IS CACHED from the property type; it doesn't get written.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public CustomPropertyType PropertyType
        {
            get { return m_cpt; }
        }

        /// <summary>
        /// Is this a new/unsaved property?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsNew
        {
            get { return PropID == idCustomFlightPropertyNew; }
        }

        /// <summary>
        /// Does this property already exist in the database?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsExisting
        {
            get { return !IsNew; }
        }

        /// <summary>
        /// Is the current value for the property the default value?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsDefaultValue
        {
            get
            {
                switch (PropertyType.Type)
                {
                    case CFPPropertyType.cfpBoolean:
                        return !BoolValue;
                    case CFPPropertyType.cfpInteger:
                        return (IntValue == 0);
                    case CFPPropertyType.cfpCurrency:
                    case CFPPropertyType.cfpDecimal:
                        return DecValue == 0.0M;
                    case CFPPropertyType.cfpDate:
                    case CFPPropertyType.cfpDateTime:
                        return DateValue.CompareTo(DateTime.MinValue) == 0;
                    case CFPPropertyType.cfpString:
                        return String.IsNullOrEmpty(TextValue);
                }
                return false;
            }
        }
        #endregion

        #region Creation
        public CustomFlightProperty()
        {
            TextValue = string.Empty;
            DateValue = DateTime.MinValue;
            IntValue = 0;
            DecValue = 0.0M;
            BoolValue = false;
            FlightID = LogbookEntry.idFlightNone;
            PropID = idCustomFlightPropertyNew;
            PropTypeID = (int) CustomPropertyType.KnownProperties.IDPropInvalid;
        }
        
        /// <summary>
        /// Creates a new custom flight property of the specified type
        /// </summary>
        /// <param name="cpt">The custompropertytype</param>
        public CustomFlightProperty(CustomPropertyType cpt) : this()
        {

            if (cpt != null)
            {
                m_cpt = cpt;
                PropTypeID = cpt.PropTypeID;
            }
        }

        protected CustomFlightProperty(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            try
            {

                if (dr["DateValue"] != null && dr["DateValue"].ToString().Length > 0)
                    DateValue = DateTime.SpecifyKind(Convert.ToDateTime(dr["DateValue"], CultureInfo.InvariantCulture), DateTimeKind.Utc);
                else
                    DateValue = DateTime.MinValue;

                PropID = Convert.ToInt32(dr["idProp"], CultureInfo.InvariantCulture);
                FlightID = Convert.ToInt32(dr["idFlight"], CultureInfo.InvariantCulture);
                PropTypeID = Convert.ToInt32(dr["idPropType"], CultureInfo.InvariantCulture);
                IntValue = Convert.ToInt32(dr["intValue"], CultureInfo.InvariantCulture);
                boolValue = (IntValue != 0);
                DecValue = Convert.ToDecimal(dr["DecValue"], CultureInfo.InvariantCulture);
                TextValue = Convert.ToString(dr["StringValue"], CultureInfo.InvariantCulture);

                // Get the propertytype values as well.
                m_cpt = new CustomPropertyType(dr);
            }
            catch (Exception ex)
            {
                throw new MyFlightbookException("Exception reading custom property from DR: " + ex.Message, ex, string.Empty);
            }
        }
        #endregion

        #region Database
        public Boolean FIsValid()
        {
            return (FlightID >= 0 && PropTypeID >= 0);
        }

        public Boolean FCommit()
        {
            Boolean fResult = false;

            if (!FIsValid())
                return fResult;

            // In case the custompropertytype has not been initialized (possible, for example, from web service), initialize it now
            if (!m_cpt.IsValid() || m_cpt.PropTypeID != this.PropTypeID)
                m_cpt = new CustomPropertyType(this.PropTypeID);

            // Don't save properties that have default values; there's no value to doing so.
            if (IsDefaultValue)
                return true;

            DBHelper dbh = new DBHelper("");
            dbh.DoNonQuery(
                (comm) =>
                {
                    comm.CommandText = szReplaceBase;
                    comm.Parameters.AddWithValue("idFlight", FlightID);
                    comm.Parameters.AddWithValue("idPropType", PropTypeID);

                    // Booleans and ints share the same underlying storage
                    comm.Parameters.AddWithValue("IntValue", (m_cpt.Type == CFPPropertyType.cfpBoolean) ? (boolValue ? 1 : 0) : IntValue);
                    comm.Parameters.AddWithValue("DecValue", DecValue);
                    comm.Parameters.AddWithValue("DateValue", DateValue);
                    comm.Parameters.AddWithValue("StringValue", TextValue.LimitTo(127));
                });

            PropID = (PropID >= 0) ? PropID : dbh.LastInsertedRowId; // set the property ID to the previous ID or else the newly inserted one

            return true;
        }

        public void DeleteProperty()
        {
            if (PropID >= 0)
            {
                DBHelper dbh = new DBHelper(String.Format(szDeleteBase, PropID));
                if (!dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idProp", PropID); }))
                    throw new MyFlightbookException(String.Format("Error attempting to delete property: {0} parameters - (idProp = {1}): {2}", dbh.CommandText, PropID, dbh.LastError));
            }
        }
        #endregion

        /// <summary>
        /// Initializes the value from a string value.  The PropertyType MUST be set.  Throws an exception if there is a problem
        /// </summary>
        /// <param name="szVal">The string representation of the value.</param>
        public void InitFromString(String szVal)
        {
            if (szVal == null)
                throw new ArgumentNullException("szVal");
            szVal = szVal.Trim();
            switch (PropertyType.Type)
            {
                case CFPPropertyType.cfpBoolean:
                    char ch1st = szVal.ToUpperInvariant()[0];
                    if (ch1st == 'Y')
                        BoolValue = true;
                    else if (ch1st == 'N')
                        BoolValue = false;
                    else
                        BoolValue = Convert.ToBoolean(szVal, CultureInfo.InvariantCulture);
                    break;
                case CFPPropertyType.cfpInteger:
                    IntValue = Convert.ToInt32(szVal, CultureInfo.InvariantCulture);
                    break;
                case CFPPropertyType.cfpCurrency:
                case CFPPropertyType.cfpDecimal:
                    DecValue = szVal.SafeParseDecimal();
                    break;
                case CFPPropertyType.cfpDate:
                case CFPPropertyType.cfpDateTime:
                    DateValue = DateTime.Parse(szVal, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                    if (PropertyType.Type == CFPPropertyType.cfpDateTime)
                        DateValue = DateTime.SpecifyKind(DateValue, DateTimeKind.Utc);
                    break;
                case CFPPropertyType.cfpString:
                    TextValue = szVal;
                    break;
            }
        }

        public static CustomFlightProperty[] LoadPropertiesForFlight(int flightID)
        {
            ArrayList ar = new ArrayList();

            if (flightID > 0)
            {
                DBHelper dbh = new DBHelper(String.Format(szSelectBase, "WHERE idFlight=?idFlight"));

                if (!dbh.ReadRows(
                    (comm) =>
                    {
                        comm.Parameters.AddWithValue("idFlight", flightID);
                        comm.Parameters.AddWithValue("langID", System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);
                    },
                    (dr) => { ar.Add(new CustomFlightProperty(dr)); }
                    ))
                    throw new MyFlightbookException(dbh.LastError);
            }

            return (CustomFlightProperty[])ar.ToArray(typeof(CustomFlightProperty));
        }

        /// <summary>
        /// Initializes the custompropertytype (PropertyType) object from the specified list of properties
        /// </summary>
        /// <param name="rgcpt">The array of custompropertytype objects</param>
        public void InitPropertyType(IEnumerable<CustomPropertyType> rgcpt)
        {
            if (rgcpt == null)
                throw new ArgumentNullException("rgcpt");
            foreach (CustomPropertyType cpt in rgcpt)
            {
                if (cpt.PropTypeID == this.PropTypeID)
                {
                    m_cpt = cpt;
                    return;
                }
            }
        }

        /// <summary>
        /// Performs a pseudo-idempotency clean up to avoid duplciate properties on a flight.
        /// We can get duplicates if we have an incoming property with a propID of -1 (unknown/new)
        /// but already have a property with the same flight and proptype on the flight.
        /// The fix is to adjust these new properties to be an update of the existing property by setting the
        /// propID to the existing one.  That way, when it commits it will overwrite the existing one with the new
        /// value rather than creating a dupe.
        /// </summary>
        /// <param name="rgPropsExisting">The array of existing properties for the flight</param>
        /// <param name="rgPropsNew">The array of new properties</param>
        public static void FixUpDuplicateProperties(CustomFlightProperty[] rgPropsExisting, CustomFlightProperty[] rgPropsNew)
        {
            if (rgPropsExisting == null || rgPropsNew == null)
                return;
            if (rgPropsExisting.Length == 0 || rgPropsNew.Length == 0)
                return;

            foreach (CustomFlightProperty cfpNew in rgPropsNew)
            {
                if (cfpNew.PropID == CustomFlightProperty.idCustomFlightPropertyNew)
                {
                    foreach (CustomFlightProperty cfpExisting in rgPropsExisting)
                    {
                        if (cfpExisting.PropTypeID == cfpNew.PropTypeID)
                        {
                            cfpNew.PropID = cfpExisting.PropID;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns all events with a non-zero flag for the specified user.
        /// </summary>
        /// <param name="szUser">Username that owns the events</param>
        /// <returns>An array of matching ProfileEvent objects</returns>
        public static IEnumerable<CustomFlightProperty> GetFlaggedEvents(string szUser)
        {
            List<CustomFlightProperty> lst = new List<CustomFlightProperty>();
            DBHelper dbh = new DBHelper(@"SELECT fp.*, cp.*, '' AS LocTitle, '' AS LocFormatString, '' AS LocDescription, '' AS prevValues, 0 AS IsFavorite
FROM flights f
INNER JOIN flightproperties fp ON f.idFlight = fp.idFlight
INNER JOIN custompropertytypes cp ON fp.idPropType = cp.idPropType
WHERE f.username =?User AND (cp.Flags <> 0) AND ((fp.IntValue <> 0) OR (fp.DecValue <> 0.0) OR (cp.Type = 4))
ORDER BY f.Date Desc");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("User", szUser); }, (dr) => { lst.Add(new CustomFlightProperty(dr)); });
            return lst;
        }

        /// <summary>
        /// Returns all events with a non-zero flag for the specified user.
        /// </summary>
        /// <param name="szUser">Username that owns the events</param>
        /// <returns>An array of matching ProfileEvent objects</returns>
        public static IEnumerable<CustomFlightProperty> GetAllPropertiesForUser(string szUser)
        {
            List<CustomFlightProperty> lst = new List<CustomFlightProperty>();
            if (String.IsNullOrEmpty(szUser))
                return lst;
            DBHelper dbh = new DBHelper(@"SELECT fp.*, cp.*, '' AS LocTitle, '' AS LocFormatString, '' AS LocDescription, '' AS prevValues, 0 AS IsFavorite
FROM flights f
INNER JOIN flightproperties fp ON f.idFlight = fp.idFlight
INNER JOIN custompropertytypes cp ON fp.idPropType = cp.idPropType
WHERE f.username =?User
ORDER BY f.Date Desc");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("User", szUser); }, (dr) => { lst.Add(new CustomFlightProperty(dr)); });
            return lst;
        }

        /// <summary>
        /// Return a list of previously used text property values for the specified user (for autocomplete).
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <returns>A dictionary, keyed on property type id, each containing a list of previously used strings.</returns>
        public static Dictionary<int, List<string>> PreviouslyUsedTextValuesForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                return null;

            const string szQ = @"SELECT cp.title, fp.idPropType AS PropTypeID, GROUP_CONCAT(DISTINCT fp.StringValue SEPARATOR '\t') AS PrevVals 
FROM Flightproperties fp
INNER JOIN CustomPropertyTypes cp ON fp.idProptype=cp.idProptype
INNER JOIN Flights f ON fp.idFlight=f.idflight
WHERE cp.Type=5 AND f.username=?user
GROUP BY fp.idPropType;";

            DBHelper dbh = new DBHelper(szQ);
            Dictionary<int, List<string>> d = new Dictionary<int, List<string>>();

            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("user", szUser); },
                (dr) => { d[Convert.ToInt32(dr["PropTypeID"])] = new List<string>(dr["PrevVals"].ToString().Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries)); });

            return d;
        }
    }

    /// <summary>
    /// A flight property coupled with date and some flight properties.  
    /// Called a "ProfileEvent" because it (used to) include mostly events that were not necessarily coupled with flights 
    /// (the most recent flight review for a pilot used to be in the profile).  But these are used to:
    ///    a) Show warnings / expirations that are not strictly flight-experience related (e.g., flight reviews)
    ///    b) Display a list of BFRs and IPCs to the user
    /// </summary>
    public class ProfileEvent : CustomFlightProperty, IComparable
    {
        #region properties
        /// <summary>
        /// Date of the event
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// The model of aircraft in which the event occured
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// The type of aircraft in which the event occured.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The category of aircraft in which the event occured
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Was this event in a Robinson R22 (for SFAR 73)
        /// </summary>
        public bool IsR22 { get; set; }

        /// <summary>
        /// Was this event in a Robinson R44 (for SFAR 73)
        /// </summary>
        public bool IsR44 { get; set; }
        #endregion

        #region Instantiation/Initialization
        public ProfileEvent()
            : base()
        {
            Date = DateTime.MinValue;
            Category = Type = Model = string.Empty;
            IsR22 = IsR44 = false;
        }

        protected ProfileEvent(MySqlDataReader dr)
            : base(dr)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            Date = Convert.ToDateTime(dr["DateOfFlight"], CultureInfo.InvariantCulture);
            Category = Convert.ToString(dr["Category"], CultureInfo.InvariantCulture);
            Model = dr["model"].ToString();
            Type = dr["typename"].ToString();
            IsR22 = Convert.ToBoolean(dr["IsR22"], CultureInfo.InvariantCulture);
            IsR44 = Convert.ToBoolean(dr["IsR44"], CultureInfo.InvariantCulture);
        }
        #endregion

        #region Getting ProfileEvents
        private static List<ProfileEvent> GetFlightEvents(string szUser, UInt32 pg)
        {
            List<ProfileEvent> ar = new List<ProfileEvent>();

            string szQueryBase = @"
SELECT f.date AS DateOfFlight, 
    f.idFlight AS FlightID, 
    cc.Category AS Category, 
    fp.*, 
    cp.*, 
    mo.*,
    COALESCE(l.Text, cp.Title) AS LocTitle,
    COALESCE(l2.Text, cp.FormatString) AS LocFormatString,
    IF (man.idManufacturer = 20 AND mo.idcategoryclass=7 AND (mo.model LIKE 'R%22' OR mo.typename LIKE 'R%22'), 1, 0) AS IsR22,
    IF (man.idManufacturer = 20 AND mo.idcategoryclass=7 AND (mo.model LIKE 'R%44' OR mo.typename LIKE 'R%44'), 1, 0) AS IsR44,
    0 AS IsFavorite,
    '' AS LocDescription, '' AS prevValues
FROM flights f 
INNER JOIN flightproperties fp ON f.idFlight=fp.idFlight 
INNER JOIN custompropertytypes cp ON fp.idPropType=cp.idPropType 
INNER JOIN aircraft ac ON ac.idaircraft = f.idaircraft 
INNER JOIN models mo ON ac.idmodel = mo.idmodel 
INNER JOIN manufacturers man ON mo.idManufacturer=man.idManufacturer
INNER JOIN categoryclass cc ON mo.idcategoryclass = cc.idCatClass 
LEFT JOIN locText l ON (l.idTableID=2 AND l.idItemID=cp.idPropType AND l.LangId=?langID)
LEFT JOIN locText l2 ON (l2.idTableID=3 AND l2.idItemID=cp.idPropType AND l2.LangID=?LangID)
WHERE f.username=?User AND (cp.Flags & {0}) <> 0 AND ((fp.IntValue <> 0) OR (fp.DecValue <> 0.0) OR (cp.Type=4)) 
ORDER BY f.Date Desc";

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szQueryBase, (ulong) pg));
            if (!dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("User", szUser);
                    comm.Parameters.AddWithValue("LangID", System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);
                },
                (dr) =>
                {
                    ar.Add(new ProfileEvent(dr));
                }))
                throw new MyFlightbookException(dbh.LastError);

            return ar;
        }

        /// <summary>
        /// Returns all events that are IPCs
        /// </summary>
        /// <param name="szUser">Username that owns the events</param>
        /// <returns>An array of matching ProfileEvent objects</returns>
        public static ProfileEvent[] GetIPCEvents(string szUser)
        {
            return GetFlightEvents(szUser, CustomPropertyType.CFPPropertyFlag.cfpFlagIPC).ToArray();
        }

        public static ProfileEvent[] GetBFREvents(string szUser, ProfileEvent pfeInsert = null)
        {
            List<ProfileEvent> ar = GetFlightEvents(szUser, CustomPropertyType.CFPPropertyFlag.cfpFlagBFR);

            if (pfeInsert != null)
                ar.Add(pfeInsert);

            ar.Sort();

            return ar.ToArray();
        }
        #endregion

        #region IComparable implementation
        public int CompareTo(object obj)
        {
            return Date.CompareTo(((ProfileEvent)obj).Date);
        }
        #endregion

        public override string ToString()
        {
            return DisplayString;
        }
    }
}