using MyFlightbook.FlightProperties.Properties;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
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
    public class CustomPropertyType : IEquatable<CustomPropertyType>
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
            IDPropManeuverChandelle = 22,
            IDPropNVGoggleTime = 26,
            IDPropPowerOffStall = 27,
            IDPropSafetyPilotTime = 30,         // Not flagged as a known property
            IDPropWaterLandings = 36,           // Not flagged as a known property
            IDPropCheckridePPL = 39,
            IDPropCheckrideIFR = 40,
            IDPropIPC = 41,                     // Not flagged as a known property
            IDPropCheckrideCommercial = 42,
            IDPropCheckRide = 43,               // Not flagged as a known property
            IDPropBFR = 44,
            IDPropCheckrideATP = 45,
            IDPropNVLandings = 51,              // Not flagged as a known property
            IDPropNVGoggleProficiencyCheck = 55,
            IDPropCrew1 = 62,
            IDPropCrew2 = 63,
            IDPropNVFLIRTime = 68,
            IDPropFrontSeatTime = 69,
            IDPropBackSeatTime = 70,
            IDPropFuelConsumed = 71,
            IDPropFuelAtLanding = 72,
            IDPropNightTakeoff = 73,
            IDPropWaterTakeoffs = 74,           // Not flagged as a known property - used for LogTenPro import
            IDPropSolo = 77,                    // Not flagged as a known property (flagged as solo)
            IDPropIFRTime = 82,
            IDPropCheckrideSport = 89,
            IDPropInstructorName = 92,          // Not flagged as a known property
            IDPropTakeoffAny = 93,              // Not flagged as a known property
            IDPropTachStart = 95,
            IDPropTachEnd = 96,
            IDPropXCLessThan50nm = 100,
            IDPropInstrumentExaminer = 107,
            IDPropMilitaryCoPilottime = 110,
            IDPropHoistOperations = 118,
            IDPropPassengerNames = 120,         // Not flagged as a known property
            IDPropManeuverSlowFlight = 121,     // Not flagged as a known property
            IDPropCheckrideRecreational = 131,
            IDPropManeuverSTurns = 132,
            IDPropPart121 = 153,
            IDPropPart135 = 154,
            IDPropPart91 = 155,
            IDPropFlightNumber = 156,           // Not flagged as a known property
            IDPropGroundInstructionReceived = 158,
            IDPropCheckrideNewCatClassType = 161,
            IDPropFlightReview = 164,           // Not flagged as a known property
            IDPropStudentName = 166,            // Not flagged as a known property - just used for copying to instructor's flight.
            IDPropFlightReviewGiven = 167,
            IDPropCombatTime = 172,
            IDPropCheckrideCFI = 176,
            IDPropCheckrideCFII = 177,
            IDPropSafetyPilotName = 178,        // Not flagged as a known property
            IDPropNameOfPIC = 183,              // Note: this one isn't flagged as a known property, just used for logbookentrydisplay to print.
            IDPropNameOfSIC = 184,              // Not flagged as a known property
            IDPropDutiesOfPIC = 185,
            IDBlockIn = 186,
            IDBlockOut = 187,
            IDPropCFIITime = 192,
            IDPropDistanceFlown = 196,
            IDPropGroundInstructionGiven = 198,
            IDPropGliderTow = 202,              // Not flagged as a known property
            IDPropHover = 204,
            IDPropAutoRotate = 205,
            IDPropGliderTowedLaunch = 222,
            IDPropGliderMaxAltitude = 223,
            IDPropGliderReleaseAltitude = 224,
            IDPropCheckrideMEI = 225,
            IDPropMotorgliderSelfLaunch = 227,
            IDPropCAP5Checkride = 232,
            IDPropMissionCrewTime = 235,
            IDPropLandingToweredNight = 244,
            IDPropLandingTowered = 245,
            IDPropMaintTestPilot = 249,
            IDPropImminentDanger = 250,
            IDPropDeadhead = 255,
            IDPropFlightEngineerTime = 257,
            IDPropCAP91Checkride = 258,
            IDPropNameOfExaminer = 260,         // Not flagged as a known property
            IDPropApproachName = 267,
            IDPropPICUS = 279,
            IDPropTestPilotTime = 282,
            IDPropInstructorOnBoard = 288,
            IDPropDayFlight = 308,
            IDPropPassengerCount = 316,         // Not flagged as a known property
            IDPropMaximumAltitude = 321,
            IDPropFlightDutyTimeStart = 332,
            IDPropFlightDutyTimeEnd = 333,
            IDPropCatapult = 341,
            IDPropBolterLanding = 343,
            IDPropCrew3 = 347,
            IDPropSimRegistration = 354,
            IDPropNVGoggleOperations = 355,
            IDPropFCLP = 356,
            IDPropTakeoffTowered = 357,
            IDPropTakeoffToweredNight = 358,
            IDPropIPCGivenToStudent = 362,
            IDPropXCMoreThan50nm = 380,
            IDPropFuelBurnRate = 381,
            IDPropNightTouchAndGo = 397,
            IDPropMilitarySpecialCrew = 398,
            IDPropMilitaryKindOfFlightCode = 399,
            IDPropXCLessThan25nm = 405,
            IDPropSinglePilot = 413,
            IDPropPart91K = 414,
            IDPropFlightCost = 415,
            IDProp135293Knowledge = 427,
            IDProp135293Competency = 428,
            IDProp135299FlightCheck = 429,
            IDPropTakeoffUntoweredNight = 449,
            IDPropTaxiTime = 464,
            IDPropCarrierTouchAndGo = 468,
            IDPropCarrierArrestedLanding = 469,
            IDPropReliefCrewNames = 500,
            IDPropFlightAttendant = 501,
            IDPropScheduledDeparture = 502,     // Not flagged as a known property
            IDPropScheduledArrival = 503,       // Not flagged as a known property
            IDPropHighAltitudeLandings = 504,
            IDPropMilitaryPrimaryTime = 511,
            IDPropMilitarySecondaryTime = 512,
            IDPropUASKnowledgeTest10773 = 527,
            IDPropUASTrainingCourse10774 = 528,
            IDPropPilotFlyingTime = 529,        // Not flagged as a known property
            IDPropPilotMonitoringTime = 530,
            IDPropReliefPilotTime = 535,
            IDProp135297IPC = 537,
            IDPropInstrumentInstructionTime = 542,
            IDPropCoPilotTime = 546,
            IDPropAdditionalCrew = 558,
            IDPropAircraftRegistration = 559,
            IDPropPilotMonitoring = 560,
            IDPropMetar = 561,
            IDPropMonitoredDayLandings = 562,
            IDPropMonitoredNightLandings = 563,
            IDPropMonitoredNightTakeoffs = 565,
            IDPropMilitaryFirstPilot = 575,
            IDPropFMSApproaches = 583,
            IDPropAirborneTime = 592,
            IDPropNVUnaided = 593,
            IDPropOffshoreTakeoff = 597,
            IDPropOffshoreLanding = 598,
            IDPropMilitaryPIC = 603,
            IDPropAdditionalFlightRemarks = 607, // Not flagged as a known property
            IDPropDutyStart = 608,
            IDPropDutyEnd = 609,
            IDPropOperatorName = 615,
            IDPropFuelAtStart = 622,
            IDPropNightRating = 623,
            IDPropSequence = 630,
            IDPropMilitaryAircraftCommander = 659,
            IDPropMultiPilotTime = 664,
            IDPropRunwaysUsed = 651,
            IDPropXCMoreThan100nm = 665,
            IDPropFlightMeterStart = 666,
            IDPropFlightMeterEnd = 667,
            IDPropLessonStart = 668,
            IDPropLessonEnd = 669,
            IDPropEnhancedVisionApproach = 670,
            IDProp6155SICCheck = 677,
            IDPropFlightEngineerName = 681,
            IDPropCaptainName = 682,
            IDPropFirstOfficerName = 683,
            IDPropSpecialAuthorizationApproach = 695,
            IDProp125291IPC = 699,
            IDProp125287Competency = 700,
            IDProp125287Knowledge = 701,
            IDPropRole = 718,
            IDPropPlannedBlock = 751,
            IDPropCrew4 = 754,
            IDPropXCMoreThan400nm = 770,
            IDPropXCNoLanding = 781,
            IDPropNightIMC = 793,
            IDPropP2xDay = 810,
            IDPropP2xNight = 811,
        }

        public static class CFPPropertyFlag
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
            public const UInt32 cfpFlagNoAutoComplete = 0x02000000; // no autocomplete for this property
            public const UInt32 cfpFlagAllCaps = 0x04000000;    // convert any value for this property to all caps.
            public const UInt32 cfpFlagIsLanding = 0x08000000;
            public const UInt32 cfpFlagInitialCaps = 0x10000000;    // Default to initial caps - useful for names.
            public const UInt32 cfpFlagPrecisionApproach = 0x20000000;
        };

        private const string szAppCacheKey = "keyCustomPropertyTypes";
        private const string szAppCacheDictKey = "keyDictCustomPropertyTypes";
        private string m_shortTitle = null;

        #region properties.
        /// <summary>
        /// ID for the proptype
        /// </summary>
        [Required]
        public int PropTypeID { get; set; }

        /// <summary>
        /// Title for the property
        /// </summary>
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// Short title for the property - useful for print headers.
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        public string ShortTitle
        {
            get { return m_shortTitle ?? Title; }
            set { m_shortTitle = value; }
        }

        [JsonIgnore]
        [XmlIgnore]
        public string NakedShortTitle { get { return m_shortTitle; } }

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
        [Required]
        public string FormatString { get; set; }

        /// <summary>
        /// Type of property
        /// </summary>
        [Required]
        public CFPPropertyType Type { get; set; }

        /// <summary>
        /// Description of the property and how to use it
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// Flags for this property type - e.g., does it count as a BFR or an IPC?
        /// </summary>
        public UInt32 Flags { get; set; }

        /// <summary>
        /// For string properties, contains previously used values
        /// </summary>
        public Collection<string> PreviousValues { get; private set; }

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
        /// Is this excluded from sums?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsNoSum
        {
            get { return ((Flags & CFPPropertyFlag.cfpNosum) != CFPPropertyFlag.cfpFlagNone); }
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
        /// Is this property a precision approach of some kind?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsPrecisionApproach
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagPrecisionApproach) != CFPPropertyFlag.cfpFlagNone); }
        }

        /// <summary>
        /// Is this property a landing that contributes to total landings?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsLanding
        {
            get { return ((Flags & CFPPropertyFlag.cfpFlagIsLanding) != CFPPropertyFlag.cfpFlagNone); }
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

        /// <summary>
        /// Is this property a time?  (Complement to IsBasicDecimal for decimal values)
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsTimeValue
        {
            get { return Type == CFPPropertyType.cfpDecimal && ((Flags & CFPPropertyFlag.cfpFlagBasicDecimal) == CFPPropertyFlag.cfpFlagNone); }
        }

        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsNoAutocomplete
        {
            get { return Type == CFPPropertyType.cfpString && ((Flags & CFPPropertyFlag.cfpFlagNoAutoComplete) != CFPPropertyFlag.cfpFlagNone); }
        }

        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsAllCaps
        {
            get { return Type == CFPPropertyType.cfpString && ((Flags & CFPPropertyFlag.cfpFlagAllCaps) != CFPPropertyFlag.cfpFlagNone); }
        }
        #endregion
        #endregion
        /// <summary>
        /// Is this valid?
        /// </summary>
        /// <returns></returns>
        public Boolean IsValid()
        {
            return (PropTypeID != (int)CustomPropertyType.KnownProperties.IDPropInvalid);
        }

        /// <summary>
        /// Adds or updates a property.  Adds if proptypeid is KnownProperties.IDPropInvalid.
        /// Should only ever be called by admin tools
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public int FCommit()
        {
            if (String.IsNullOrWhiteSpace(Title))
                throw new InvalidOperationException("Title is required");
            if (String.IsNullOrWhiteSpace(FormatString))
                throw new InvalidOperationException("Format string is required");
            if (String.IsNullOrEmpty(Description))
                throw new InvalidOperationException("Description is required");

            string szQ = (PropTypeID == (int)KnownProperties.IDPropInvalid) ?
                "INSERT INTO custompropertyTypes SET Title=?title, ShortTitle=?shorttitle, SortKey=?sortkey, FormatString=?format, Type=?type, Flags=?flags, Description=?description" :
                "UPDATE custompropertyTypes SET Title=?title, ShortTitle=?shorttitle, SortKey=?sortkey, FormatString=?format, Type=?type, Flags=?flags, Description=?description WHERE idPropType=?idproptype";
            DBHelper dbh = new DBHelper(szQ);

            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("title", Title);
                comm.Parameters.AddWithValue("shorttitle", ShortTitle ?? string.Empty);
                comm.Parameters.AddWithValue("sortkey", SortKey ?? string.Empty);
                comm.Parameters.AddWithValue("format", FormatString);
                comm.Parameters.AddWithValue("type", (uint)Type);
                comm.Parameters.AddWithValue("flags", Flags);
                comm.Parameters.AddWithValue("description", Description);
                comm.Parameters.AddWithValue("idproptype", (int)PropTypeID);
            });
            util.FlushCache();  // flush everything - all user props as well as the global one.
            return dbh.LastInsertedRowId;
        }

        #region Constructors
        public CustomPropertyType()
        {
            FormatString = Title = SortKey = String.Empty;
            Type = CFPPropertyType.cfpInteger;
            Flags = CFPPropertyFlag.cfpFlagNone;
            PropTypeID = (int)KnownProperties.IDPropInvalid;
            PreviousValues = new Collection<string>();
        }

        public CustomPropertyType(int id) : this()
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"
SELECT cpt.*, cpt.Title AS LocTitle, 
    cpt.FormatString AS LocFormatString, 
    cpt.Description AS LocDescription,
    0 AS IsFavorite,
    '' AS prevValues 
FROM custompropertytypes cpt 
WHERE idPropType = {0} ORDER BY Title ASC", id));

            if (!dbh.ReadRow(
                (comm) => { },
                (dr) => { InitFromDataReader(dr); }))
                throw new MyFlightbookException(dbh.LastError);
        }

        public CustomPropertyType(CustomPropertyType.KnownProperties id) : this((int)id) { }

        public CustomPropertyType(MySqlDataReader dr) : this()
        {
            InitFromDataReader(dr);
        }

        public static readonly char[] PreviouisTextValuesSeparator = new char[] { '\t' };

        private void InitFromDataReader(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            PropTypeID = Convert.ToInt32(dr["idPropType"], CultureInfo.InvariantCulture);
            Title = dr["LocTitle"].ToString();
            FormatString = dr["LocFormatString"].ToString();
            Type = (CFPPropertyType)Convert.ToInt32(dr["Type"], CultureInfo.InvariantCulture);
            m_shortTitle = (string)util.ReadNullableField(dr, "ShortTitle", null);
            SortKey = Convert.ToString(dr["SortKey"], CultureInfo.CurrentCulture);
            if (String.IsNullOrEmpty(SortKey))
                SortKey = Title;
            Flags = (UInt32)Convert.ToInt32(dr["Flags"], CultureInfo.InvariantCulture);
            IsFavorite = Convert.ToBoolean(dr["IsFavorite"], CultureInfo.InvariantCulture);
            Description = Convert.ToString(dr["LocDescription"], CultureInfo.InvariantCulture);
            string szPreviousVals = Convert.ToString(dr["prevValues"], CultureInfo.InvariantCulture);
            PreviousValues?.Clear();    // not strictly necessary, but prevents a warning about unused PreviousValues, which of course are needed for serialization for the mobile apps
            if (!String.IsNullOrEmpty(szPreviousVals))
            {
                List<string> lst = new List<string>(szPreviousVals.Split(PreviouisTextValuesSeparator, StringSplitOptions.RemoveEmptyEntries));
                lst.Sort();
                PreviousValues = new Collection<string>(lst);
            }
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}{1} - {2}", PropTypeID, IsFavorite ? "*" : " ", Title);
        }

        #region Admin tools
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
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagPrecisionApproach, "Precision approach");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagIsLanding, "Landing;");
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
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagKnownProperty, "Known Property;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagGliderGroundLaunch, "Glider GroundLaunch;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagNoAutoComplete, "No autocomplete;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagAllCaps, "All Caps;");
            AppendIfFlagged(sb, cfp, CFPPropertyFlag.cfpFlagInitialCaps, "Initial Caps");

            string sz = sb.ToString();

            if (sz.EndsWith(";", StringComparison.Ordinal))
                sz = sz.Remove(sz.Length - 1);
            if (sz.EndsWith(": ", StringComparison.Ordinal))
                sz = sz.Remove(sz.Length - 2);
            return sz;
        }
        #endregion

        /// <summary>
        /// Returns a human readable description of the property type's unit
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        public string PropTypeUnitDescription
        {
            get
            {
                switch (Type)
                {
                    case CFPPropertyType.cfpBoolean:
                        return PropertyResource.importUnitBoolean;
                    case CFPPropertyType.cfpCurrency:
                    case CFPPropertyType.cfpDecimal:
                    case CFPPropertyType.cfpInteger:
                        return PropertyResource.importUnitNumber;
                    case CFPPropertyType.cfpDate:
                    case CFPPropertyType.cfpDateTime:
                        return PropertyResource.importUnitDate;
                    case CFPPropertyType.cfpString:
                        return PropertyResource.importUnitText;
                    default:
                        return string.Empty;
                }
            }
        }

        public static void FlushUserCache(string szUser)
        {
            IUserProfile pf = util.RequestContext.GetUser(szUser);
            pf.AssociatedData.Remove(szAppCacheKey);
        }

        /// <summary>
        /// Helper routine for customproperty types that aren't being shown to the user.  Cuts down on serialization size.
        /// </summary>
        public void StripUnnededFields()
        {
            Description = FormatString = SortKey = null;
            PreviousValues = null;
        }

        private static Dictionary<int, CustomPropertyType> PropTypeDictionary
        {
            get
            {
                Dictionary<int, CustomPropertyType> d = (Dictionary<int, CustomPropertyType>)util.GlobalCache.Get(szAppCacheDictKey);
                if (d == null || d.Count == 0)  // Issue #464
                {
                    d = new Dictionary<int, CustomPropertyType>();
                    util.GlobalCache.Set(szAppCacheDictKey, d, DateTimeOffset.UtcNow.AddMinutes(20));
                    var rgProps = GetCustomPropertyTypes();
                    foreach (CustomPropertyType cpt in rgProps)
                        d[cpt.PropTypeID] = cpt;
                }
                return d;
            }
        }

        /// <summary>
        /// Fast lookup of a single property type by ID (uses cached list).  May hit the database if cache is cold, but should otherwise be very fast.
        /// </summary>
        /// <param name="id">Id requested</param>
        /// <returns></returns>
        public static CustomPropertyType GetCustomPropertyType(int id)
        {
            if (id < 0)
                return null;

            Dictionary<int, CustomPropertyType> d = PropTypeDictionary;
            if (d == null) // shouldn't happen, but if it does, do a brute-force search - a bit slower.
                return GetCustomPropertyTypes(null, true).FirstOrDefault(proptype => proptype.PropTypeID == id);

            if (d.TryGetValue(id, out CustomPropertyType cpt))
                return cpt;

            return null;
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

            var d = PropTypeDictionary;

            if (d == null) // shouldn't happen, but if it does, do a brute-force search
                return new List<CustomPropertyType>(GetCustomPropertyTypes()).FindAll(cpt => lstIds.Contains(cpt.PropTypeID));

            foreach (int id in lstIds)
            {
                d.TryGetValue(id, out CustomPropertyType cpt);
                if (cpt != null)
                    lstResult.Add(cpt);
            }
            return lstResult;
        }

        /// <summary>
        /// Returns the subset of ALL properties ever used by the user, even ones which are excluded by MRU.
        /// Does a separate query from GetCustomPropertyTypes both for performance and to avoid caching
        /// </summary>
        /// <param name="szUser"></param>
        /// <returns>The set of properties that the user has ever used</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<CustomPropertyType> AllPreviouslyUsedPropsForUser(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));

            HashSet<int> hs = new HashSet<int>();

            DBHelper dbh = new DBHelper("SELECT DISTINCT idproptype FROM flightproperties fp INNER JOIN flights f ON fp.idflight=f.idflight WHERE f.username=?username");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("username", szUser); },
                (dr) => { hs.Add(Convert.ToInt32(dr["idproptype"], CultureInfo.InvariantCulture)); });

            List<CustomPropertyType> lst = new List<CustomPropertyType>(GetCustomPropertyTypes());

            lst.RemoveAll(cpt => !hs.Contains(cpt.PropTypeID));

            return lst;
        }

        private const string szCustomPropsForUserQuery = @"SELECT cpt.*,
           cpt.Title AS LocTitle,
           cpt.FormatString AS LocFormatString,
           cpt.Description AS LocDescription,
           IF (usedProps.numflights IS NULL OR ((cpt.Flags & 0x00100000) <> 0), 0, 1) AS IsFavorite,
           IF (((cpt.flags & 0x02000000) = 0), usedProps.prevValues, null) AS PrevValues
FROM custompropertytypes cpt 
LEFT JOIN
  (SELECT fp.idPropType AS idPropType, COUNT(f.idFlight) AS numFlights, GROUP_CONCAT(DISTINCT IF(fp.stringvalue='', NULL, fp.stringvalue) SEPARATOR '\t') AS prevValues
  FROM flightproperties fp INNER JOIN flights f ON fp.idflight=f.idFlight
  WHERE f.username=?uname {0}
  GROUP BY fp.idproptype) AS usedProps ON usedProps.idPropType=cpt.idPropType
ORDER BY IsFavorite DESC, IF(SortKey='', Title, SortKey) ASC";

        /// <summary>
        /// Get custom property types for the specified user, or the global list (if szUser is null or empty)
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <param name="fForceLoad">true to bypass cache (force hit of database); ignored if szUser is null or empty</param>
        /// <returns>An array of custom property types</returns>
        public static CustomPropertyType[] GetCustomPropertyTypes(string szUser = null, bool fForceLoad = false)
        {
            List<CustomPropertyType> ar = new List<CustomPropertyType>();

            // if szUser is null or empty, we want to cache this in the application cache (shared across all users)
            // otherwise, we store it in the user's associated data cache
            // So first we check if is cached and return it if so.
            if (String.IsNullOrEmpty(szUser))
            {
                CustomPropertyType[] rgProps = (CustomPropertyType[])util.GlobalCache.Get(szAppCacheKey);
                if (rgProps != null)
                    return rgProps;
            }
            else
            {
                if (!fForceLoad)
                {
                    // Try to return props from the user profile.
                    CustomPropertyType[] rgProps = (CustomPropertyType[])util.RequestContext.GetUser(szUser).CachedObject(szAppCacheKey);
                    if (rgProps != null)
                        return rgProps;
                }
            }

            // CustomPropsForUserQuery will work with an empty szUser, but it is SLOW, so if no user is specified, use a faster query.
            string szQ;
            if (String.IsNullOrEmpty(szUser))
                szQ = @"SELECT cpt.*,
cpt.Title AS LocTitle, 
cpt.FormatString AS LocFormatString,
cpt.Description AS LocDescription,
0 AS IsFavorite,
'' AS prevValues 
FROM custompropertytypes cpt 
ORDER BY IF(SortKey='', Title, SortKey) ASC";
            else
            {
                IUserProfile pf = util.RequestContext.GetUser(szUser);
                szQ = String.Format(CultureInfo.InvariantCulture, szCustomPropsForUserQuery, pf.BlocklistedProperties.Count == 0 ? string.Empty : String.Format(CultureInfo.InvariantCulture, " AND fp.idPropType NOT IN ('{0}') ", String.Join("', '", pf.BlocklistedProperties)));
            }

            DBHelper dbh = new DBHelper(szQ);

            if (!dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("uname", szUser);
                },
                (dr) => { ar.Add(new CustomPropertyType(dr)); }))
                throw new MyFlightbookException(dbh.LastError);

            CustomPropertyType[] rgcpt = ar.ToArray();

            // cache it.
            if (String.IsNullOrEmpty(szUser))
                util.GlobalCache.Set(szAppCacheKey, rgcpt, DateTimeOffset.UtcNow.AddHours(2));
            else
                util.RequestContext.GetUser(szUser).AssociatedData[szAppCacheKey] = rgcpt;

            return rgcpt;
        }

        #region IEquatable

        /// <summary>
        /// We take a very broad definition of equals: two property types are equal if the PropTypeID is equal.  Nothing else matters.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as CustomPropertyType);
        }

        public bool Equals(CustomPropertyType other)
        {
            return other != null &&
                   PropTypeID == other.PropTypeID;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return -1747040907 + PropTypeID.GetHashCode();
            }
        }

        public static bool operator ==(CustomPropertyType left, CustomPropertyType right)
        {
            return EqualityComparer<CustomPropertyType>.Default.Equals(left, right);
        }

        public static bool operator !=(CustomPropertyType left, CustomPropertyType right)
        {
            return !(left == right);
        }
        #endregion
    }


}
