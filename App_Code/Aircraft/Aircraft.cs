using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using JouniHeikniemi.Tools.Text;
using MyFlightbook.Image;
using MySql.Data.MySqlClient;

/******************************************************
 * 
 * Copyright (c) 2009-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Instance types for an aircraft.  Meaning it's a real aircraft or one of a set of different flavors of aircraft.
    /// Does NOT have a 0 value, since that doesn't correspond to anything int he database.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum AircraftInstanceTypes
    {
        RealAircraft = 1, Mintype = RealAircraft,
        UncertifiedSimulator,
        CertifiedIFRSimulator,
        CertifiedIFRAndLandingsSimulator,
        CertifiedATD,
        MaxType = CertifiedATD
    };

    /// <summary>
    /// An instance of an aircraft - one of the AircraftInstanceTypes along with the attributes of that instance (e.g., certification level, ATD vs. FTD, etc.)
    /// </summary>
    [Serializable]
    public class AircraftInstance
    {
        #region Properties
        /// <summary>
        /// The instance type (Real aircraft, uncertified, etc.)
        /// </summary>
        public AircraftInstanceTypes InstanceType { get; set; }

        /// <summary>
        /// Returns a short name for any non-real-aircraft (e.g., "ATD", "FTD/Sim", or "Uncertified Sim")
        /// </summary>
        /// <param name="ait">The instance type</param>
        /// <returns></returns>
        public static string ShortNameForInstanceType(AircraftInstanceTypes ait)
        {
            switch (ait)
            {
                case AircraftInstanceTypes.CertifiedATD:
                    return Resources.Aircraft.InstanceTypeATD;
                case AircraftInstanceTypes.CertifiedIFRAndLandingsSimulator:
                case AircraftInstanceTypes.CertifiedIFRSimulator:
                    return Resources.Aircraft.InstanceTypeFTD;
                case AircraftInstanceTypes.UncertifiedSimulator:
                    return Resources.Aircraft.InstanceTypeUncertified;
                case AircraftInstanceTypes.RealAircraft:
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// InstanceType as an integer.
        /// </summary>
        public int InstanceTypeInt
        {
            get { return (int)InstanceType; }
            set { InstanceType = (AircraftInstanceTypes)value; }
        }

        /// <summary>
        /// Display name for the type
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Do instrument approaches in this count?
        /// </summary>
        public bool IsCertifiedIFR { get; set; }

        /// <summary>
        /// Do landings in this count?
        /// </summary>
        public bool IsCertifiedLanding { get; set; }

        /// <summary>
        /// Is this full-motion?
        /// </summary>
        public bool IsFullMotion { get; set; }

        /// <summary>
        /// What is the FAA Sim level?
        /// </summary>
        public string FAASimLevel { get; set; }

        /// <summary>
        /// Is this an ATD?
        /// </summary>
        public bool IsATD { get; set; }

        /// <summary>
        /// Is this an FTD?
        /// </summary>
        public bool IsFTD { get; set; }

        /// <summary>
        /// Is this a real aircraft?
        /// </summary>
        public bool IsRealAircraft
        {
            get { return InstanceType == AircraftInstanceTypes.RealAircraft; }
        }

        /// <summary>
        /// Is this other than a real aircraft?
        /// </summary>
        public bool IsSim
        {
            get { return !IsRealAircraft; }
        }
        #endregion

        #region Constructors
        public AircraftInstance() {  }

        protected AircraftInstance(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            InstanceType = (AircraftInstanceTypes)Convert.ToInt32(dr["ID"], CultureInfo.InvariantCulture);
            DisplayName = dr["Description"].ToString();
            IsCertifiedIFR = Convert.ToBoolean(dr["IsCertifiedIFR"], CultureInfo.InvariantCulture);
            IsCertifiedLanding = Convert.ToBoolean(dr["IsCertifiedLanding"], CultureInfo.InvariantCulture);
            IsFullMotion = Convert.ToBoolean(dr["IsFullMotion"], CultureInfo.InvariantCulture);
            FAASimLevel = dr["FAASimLevel"].ToString();
            IsATD = Convert.ToBoolean(dr["IsATD"], CultureInfo.InvariantCulture);
            IsFTD = Convert.ToBoolean(dr["IsFTD"], CultureInfo.InvariantCulture);
        }
        #endregion

        /// <summary>
        /// Get an array of known instance types in the system; this is cached, so it is safe to call frequently.
        /// </summary>
        static public AircraftInstance[] GetInstanceTypes()
        {
            const string szCacheKey = "aiCacheKey";

            if (HttpRuntime.Cache != null &&
                HttpRuntime.Cache[szCacheKey] != null)
                return (AircraftInstance[])HttpRuntime.Cache[szCacheKey];

            List<AircraftInstance> al = new List<AircraftInstance>();

            DBHelper dbh = new DBHelper("SELECT * FROM aircraftinstancetypes");
            if (!dbh.ReadRows((comm) => { }, (dr) => { al.Add(new AircraftInstance(dr)); }))
                throw new MyFlightbookException("Error getting instance types:\r\n" + dbh.LastError);

            if (HttpRuntime.Cache != null)
                HttpRuntime.Cache[szCacheKey] = al.ToArray();

            return al.ToArray();
        }
    }

    /// <summary>
    /// Provides stats for an aircraft: how many people fly it, how many flights are recorded, and, if for a specified user, the dates of the first/last flight in it.
    /// </summary>
    [Serializable]
    public class AircraftStats
    {
        private int m_cFlightsUser = 0;
        private int m_cFlightsTotal = 0;
        private int m_cUsers = 0;

        #region Properties
        /// <summary>
        /// The ID of the aircraft for which stats are desired
        /// </summary>
        public int AircraftID { get; set; }

        /// <summary>
        /// The name of the user for which stats are desired
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// The number of flights for the specified airplane by the specified user
        /// </summary>
        public int UserFlights
        {
            get { return m_cFlightsUser; }
        }

        /// <summary>
        /// The number of users who have flights with this aircraft
        /// </summary>
        public int Users
        {
            get { return m_cUsers; }
        }

        public string[] UserNames { get; set; }

        /// <summary>
        /// The number of flights for the specified airplane, period.
        /// </summary>
        public int Flights
        {
            get { return m_cFlightsTotal; }
        }

        /// <summary>
        /// Date of the earliest date for the user in this aircraft, if known
        /// </summary>
        public DateTime? EarliestDate { get; set; }

        /// <summary>
        /// Date of the latest date for the user in this aircraft, if known
        /// </summary>
        public DateTime? LatestDate { get; set; }

        /// <summary>
        /// Linked display for the stats for the flight.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public LinkedString UserStatsDisplay
        {
            get
            {
                if (UserFlights == 0)
                    return new LinkedString(Resources.Aircraft.AircraftStatsNeverFlown, null);
                else return
                        new LinkedString(String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.AircraftStatsFlown, UserFlights,
                  EarliestDate.HasValue && LatestDate.HasValue ? String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.AircraftStatsDate, EarliestDate.Value.ToShortDateString(), LatestDate.Value.ToShortDateString()) : string.Empty),
                    String.IsNullOrEmpty(User) ? null : String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx?ft=Totals&fq={0}", HttpUtility.UrlEncode(new FlightQuery() { UserName = User, AircraftIDList = new int[] { AircraftID } }.ToBase64CompressedJSONString())));
            }
        }
        #endregion

        #region Constructors
        private void InitFromDataReader(MySqlDataReader dr)
        {
            m_cFlightsTotal = Convert.ToInt32(dr["numFlights"], CultureInfo.InvariantCulture);
            m_cFlightsUser = Convert.ToInt32(dr["flightsForUser"], CultureInfo.InvariantCulture);
            m_cUsers = Convert.ToInt32(dr["numUsers"], CultureInfo.InvariantCulture);
            UserNames = dr["userNames"].ToString().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            object o = dr["EarliestDate"];
            if (o != DBNull.Value)
                EarliestDate = (DateTime)o;
            o = dr["LatestDate"];
            if (o != DBNull.Value)
                LatestDate = (DateTime)o;
            o = dr["idaircraft"];
            if (o != DBNull.Value)
                AircraftID = Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Creates a new AircraftStats object
        /// </summary>
        public AircraftStats()
        {
            User = string.Empty;
            AircraftID = Aircraft.idAircraftUnknown;
            EarliestDate = new DateTime?();
            LatestDate = new DateTime?();
        }

        public AircraftStats(string szUser, int aircraftID) : this()
        {
            GetStats(new int[] { aircraftID }, szUser, (dr) =>
            {
                InitFromDataReader(dr);
                User = szUser;
            });
        }

        protected AircraftStats(MySqlDataReader dr) : this()
        {
            InitFromDataReader(dr);
        }
        #endregion

        #region Getting stats for aircraft
        private static void GetStats(IEnumerable<int> lstAircraftIDs, string szUser, Action<MySqlDataReader> readStats)
        {
            List<string> lstIds = new List<string>();
            foreach (int id in lstAircraftIDs)
                lstIds.Add(id.ToString(CultureInfo.InvariantCulture));

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"SELECT
                    ac.idaircraft,
                    COUNT(DISTINCT(f1.idFlight)) AS numFlights,
                    COUNT(DISTINCT(ua.username)) AS numUsers,
                    GROUP_CONCAT(DISTINCT ua.username SEPARATOR ';') AS userNames,
                    COUNT(DISTINCT(f2.idflight)) AS flightsForUser,
                    MIN(f2.date) AS EarliestDate,
                    MAX(f2.date) AS LatestDate
                FROM aircraft ac
                    LEFT JOIN flights f1 ON ac.idaircraft=f1.idaircraft
                    LEFT JOIN flights f2 ON f2.username=?user AND f1.idflight=f2.idflight
                    LEFT JOIN useraircraft ua ON ac.idaircraft=ua.idaircraft
                WHERE ac.idaircraft  IN ({0})
                GROUP BY ac.idaircraft", String.Join(",", lstIds)));

            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("user", szUser); },
                (dr) => { readStats(dr); });
        }

        /// <summary>
        /// Returns the stats for the specified set of aircraft ID's
        /// </summary>
        /// <param name="lstAircraftIDs">The list of aircraft IDs for which stats are requested</param>
        /// <param name="szUser">The user for whom to return per-user stats.</param>
        /// <returns>An IDictionary mapping IDs to stats</returns>
        public static IDictionary<int, AircraftStats> StatsForAircraft(IEnumerable<int> lstAircraftIDs, string szUser = null)
        {
            if (szUser == null)
                szUser = string.Empty;

            Dictionary<int, AircraftStats> dict = new Dictionary<int, AircraftStats>();

            if (lstAircraftIDs == null || lstAircraftIDs.Count() == 0)
                return dict;

            GetStats(lstAircraftIDs, szUser, (dr) =>
            {
                AircraftStats acs = new AircraftStats(dr) { User = szUser };
                dict[acs.AircraftID] = acs;
            });

            return dict;
        }

        /// <summary>
        /// Returns the stats for the specified set of aircraft
        /// </summary>
        /// <param name="lstAc">The list of aircraft for which stats are requested</param>
        /// <param name="szUser">The user for whom to return per-user stats.</param>
        /// <returns>An IDictionary mapping IDs to stats</returns>
        public static IDictionary<int, AircraftStats> StatsForAircraft(IEnumerable<Aircraft> lstAc, string szUser = null)
        {
            if (lstAc == null)
                throw new ArgumentNullException("lstAc");
            List<int> lstIds = new List<int>();
            foreach (Aircraft ac in lstAc)
                lstIds.Add(ac.AircraftID);

            return StatsForAircraft(lstIds, szUser);
        }

        /// <summary>
        /// Returns stats for a single specified aircraft, and optionally a specific user
        /// </summary>
        /// <param name="idAircraft"></param>
        /// <param name="szUser">The user</param>
        /// <returns>Stats for that user</returns>
        public static AircraftStats StatsForAircraft(int idAircraft, string szUser = null)
        {
            IDictionary<int, AircraftStats> d = StatsForAircraft(new int[] { idAircraft }, szUser);
            return d.ContainsKey(idAircraft) ? d[idAircraft] : new AircraftStats();
        }
        #endregion

        public static void PopulateStatsForAircraft(IEnumerable<Aircraft> lstAc, string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException("szUser");
            if (lstAc == null)
                throw new ArgumentNullException("lstAc");
            IDictionary<int, AircraftStats> dict = StatsForAircraft(lstAc, szUser);

            foreach (Aircraft ac in lstAc)
                if (dict.ContainsKey(ac.AircraftID))
                    ac.Stats = dict[ac.AircraftID];
        }
    }

    /// <summary>
    /// Can a given aircraft model or manufacturer be a real aircraft, or must it be a sim, or can it be a sim or anonymous?
    /// E.g., a Frasca can only be a Sim, but "Generic" can be a sim or anonymous (but not real).
    /// </summary>
    public enum AllowedAircraftTypes { Any = 0, SimulatorOnly = 1, SimOrAnonymous = 2 };

    /// <summary>
    /// Represents a specific aircraft in the system.
    /// </summary>
    [Serializable]
    public class Aircraft
    {
        public const int idAircraftUnknown = -1;
        public const int maxTailLength = 10;

        #region Backing fields for properties
        private string m_szTail = string.Empty;
        private AircraftInstanceTypes m_instanceType = AircraftInstanceTypes.RealAircraft;

        private MaintenanceRecord m_mr;

        private Boolean m_fIsTailwheel = false;
        private IList<MFBImageInfo> rgImages = null;
        #endregion

        #region Flags
        private const UInt32 acMaskRole = 0x00000007;
        private const UInt32 acMaskSuppressFavorite = 0x00000008;

        public enum PilotRole { None = 0, PIC, SIC, CFI };
        #endregion

        #region Properties
        /// <summary>
        /// The maintenance record for the aircraft
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public MaintenanceRecord Maintenance
        {
            get { return m_mr; }
        }

        /// <summary>
        /// The log of maintenance changes to this aircraft.
        /// </summary>
        private IEnumerable<MaintenanceLog> MaintenanceChanges { get; set; }

        /// <summary>
        /// The instancetype of the aircraft (i.e., real airplane or simulator type), expressed as an integer (for webservice)
        /// </summary>
        public int InstanceTypeID
        {
            get { return (int)m_instanceType; }
            set { m_instanceType = (AircraftInstanceTypes)value; }
        }

        /// <summary>
        /// Is this a new aircraft?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsNew
        {
            get { return AircraftID == idAircraftUnknown; }
        }

        /// <summary>
        /// The instancetype of the aircraft (i.e., real airplane or simulator type), expressed as an AircraftInstanceTypes
        /// </summary>
        public AircraftInstanceTypes InstanceType
        {
            get { return m_instanceType; }
            set { m_instanceType = value; }
        }

        #region user stats
        /// <summary>
        /// Stats for the aircraft?  CAN BE NULL!  NOT SERIALIZED - we want to keep this abstracted.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public AircraftStats Stats { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public DateTime? FirstFlightDate
        {
            get { return (Stats == null) ? null : Stats.EarliestDate; }
        }

        [Newtonsoft.Json.JsonIgnore]
        public DateTime? LastFlightDate
        {
            get { return (Stats == null) ? null : Stats.LatestDate; }
        }

        [Newtonsoft.Json.JsonIgnore]
        public int FlightCount
        {
            get { return (Stats == null) ? 0 : Stats.UserFlights; }
        }

        [Newtonsoft.Json.JsonIgnore]
        public bool HasBeenFlown
        {
            get { return (Stats != null && Stats.EarliestDate.HasValue);}
        }

        [Newtonsoft.Json.JsonIgnore]
        public LinkedString StatsDisplay
        {
            get { return (Stats == null) ? new LinkedString() : Stats.UserStatsDisplay; }
        }
        #endregion

        /// <summary>
        /// Human readable description of the instance type; read-only.  NOT PERSISTED!
        /// </summary>
        public string InstanceTypeDescription { get; set; }

        #region Maintenance Helpers
        /// <summary>
        /// Date of the last VOR check
        /// </summary>
        public DateTime LastVOR
        {
            get { return Maintenance.LastVOR; }
            set { Maintenance.LastVOR = value; }
        }

        /// <summary>
        /// Date of the last altimeter check
        /// </summary>
        public DateTime LastAltimeter
        {
            get { return Maintenance.LastAltimeter; }
            set { Maintenance.LastAltimeter = value; }
        }

        /// <summary>
        /// Date of the last transponder test
        /// </summary>
        public DateTime LastTransponder
        {
            get { return Maintenance.LastTransponder; }
            set { Maintenance.LastTransponder = value; }
        }

        /// <summary>
        /// Date of the last ELT test
        /// </summary>
        public DateTime LastELT
        {
            get { return Maintenance.LastELT; }
            set { Maintenance.LastELT = value; }
        }

        /// <summary>
        /// Date of last static test
        /// </summary>
        public DateTime LastStatic
        {
            get { return Maintenance.LastStatic; }
            set { Maintenance.LastStatic = value; }
        }

        /// <summary>
        /// Date of last 100-hr inspection
        /// </summary>
        public Decimal Last100
        {
            get { return Maintenance.Last100; }
            set { Maintenance.Last100 = value; }
        }

        /// <summary>
        /// Hobbs of last Oil change
        /// </summary>
        public Decimal LastOilChange
        {
            get { return Maintenance.LastOilChange; }
            set { Maintenance.LastOilChange = value; }
        }

        /// <summary>
        /// Last new engine (i.e., how many hours does this one have on it?)
        /// </summary>
        public Decimal LastNewEngine
        {
            get { return Maintenance.LastNewEngine; }
            set { Maintenance.LastNewEngine = value; }
        }

        /// <summary>
        /// Date of the last annual
        /// </summary>
        public DateTime LastAnnual
        {
            get { return Maintenance.LastAnnual; }
            set { Maintenance.LastAnnual = value; }
        }
        #endregion

        #region Model attributes
        /// <summary>
        /// Read-only: is the airplane a tailwheel airplane?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsTailwheel
        {
            get { return m_fIsTailwheel; }
        }

        /// <summary>
        /// True if the aircraft has a glass upgrade
        /// </summary>
        public Boolean IsGlass { get; set; }

        #region Images
        /// <summary>
        /// Images associated with the aircraft.  For efficiency, this MUST BE EXPLICITLY POPULATED AND SET.
        /// </summary>
        public Collection<MFBImageInfo> AircraftImages
        {
            get { return rgImages == null ? new Collection<MFBImageInfo>() : new Collection<MFBImageInfo>(rgImages); }
        }

        /// <summary>
        /// Does this have a sample image?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool HasSampleImage { get { return AircraftImages != null && AircraftImages.Count > 0; } }

        /// <summary>
        /// URL, if any, to sample image thumbnail
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string SampleImageThumbnail { get { return HasSampleImage ? AircraftImages[0].URLThumbnail : string.Empty; } }

        /// <summary>
        /// URL, if any, to full-sized sample image
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string SampleImageFull { get { return HasSampleImage ? AircraftImages[0].URLFullImage : string.Empty; } }

        /// <summary>
        /// Comment for the sample image
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string SampleImageComment { get { return HasSampleImage ? AircraftImages[0].Comment : string.Empty; } }

        /// <summary>
        /// Fill AircraftImages with images from the flight.
        /// </summary>
        public void PopulateImages()
        {
            ImageList il = new ImageList(MFBImageInfo.ImageClass.Aircraft, AircraftID.ToString(CultureInfo.InvariantCulture));
            il.Refresh(szDefault: DefaultImage);
            rgImages = il.ImageArray;
        }
        #endregion

        #region Flags
        private UInt32 Flags { get; set; }

        /// <summary>
        /// Initializes the flags for this aircraft from another aircraft
        /// </summary>
        /// <param name="ac">The source aircraft</param>
        public void CopyFlags(Aircraft ac)
        {
            if (ac == null)
                throw new ArgumentNullException("ac");
            Flags = ac.Flags;
        }

        /// <summary>
        /// Read-only access to the flags, used for persistence.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public UInt32 FlagsValue
        { get { return Flags; } }

        #endregion

        /// <summary>
        /// The database row ID of the aircraft, -1 if unknown or new
        /// DO NOT SET THIS except when de-serializing; use the default value (-1) for new aircraft, otherwise re-use the existing aircraft ID.
        /// </summary>
        public int AircraftID { get; set; }

        #region Display
        /// <summary>
        /// Returns the common name for this model of aircraft.  DOES NOT GET PERSISTED if you commit the aircraft object
        /// </summary>
        public string ModelCommonName { get; set; }

        /// <summary>
        /// The tailnumber for the aircraft
        /// </summary>
        public string TailNumber
        {
            get { return m_szTail.ToUpper(CultureInfo.InvariantCulture); }
            set { m_szTail = String.IsNullOrEmpty(value) ? value : value.ToUpper(CultureInfo.InvariantCulture); }
        }

        /// <summary>
        /// Reference to the make/model info for the aircraft.  -1 if unknown or not revealed.
        /// </summary>
        public int ModelID { get; set; }

        /// <summary>
        /// Model Description + Common Name)
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string LongModelDescription
        {
            get { return String.Format(CultureInfo.CurrentCulture, "{0}, {1}", ModelDescription, ModelCommonName).Trim(); }
        }

        /// <summary>
        /// Tail Number + LongModelDescription (= Model Description + Common Name)
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string LongDescription
        {
            get { return String.Format(CultureInfo.CurrentCulture, "{0} ({1})", TailNumber, LongModelDescription); }
        }

        [Newtonsoft.Json.JsonIgnore]
        public string ICAO { get; set; }

        /// <summary>
        /// Returns a display tailnumber, mapping anonymous as appropriate
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string DisplayTailnumber
        {
            get { return IsAnonymous ? String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.AnonymousTemplate, ModelDescription) : TailNumber; }
        }

        /// <summary>
        /// Returns DisplayTailnumber + the model
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string DisplayTailnumberWithModel
        {
            get { return IsAnonymous ? DisplayTailnumber : this.ToString(); }
        }

        /// <summary>
        /// Hack for backwards compatibility with existing mobile apps: provide a tailnumber that is still anonymous (begins with "#")
        /// but which is human legible.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string HackDisplayTailnumber
        {
            get { return String.Format(CultureInfo.InvariantCulture, "{0}{1}", CountryCodePrefix.szAnonPrefix, DisplayTailnumber); }
        }

        public override string ToString()
        {
            return LongDescription;
        }

        /// <summary>
        /// Returns a human readable model description for the aircraft.  
        /// DO NOT SET THIS except when de-serializing, as it will not be written back to the DB (must go through the model table instead)
        /// </summary>
        public string ModelDescription { get; set; }

        /// <summary>
        /// Helper property for category/class display
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string CategoryClassDisplay
        {
            get { return MakeModel.GetModel(ModelID).CategoryClassDisplay; }
        }
        /// <summary>
        /// Get the ID of the most recently used aircraft for the user, either from cookie or from session
        /// </summary>
        /// <returns>The ID of the most recently used aircraft</returns>
        #endregion

        static public int LastTail
        {
            get
            {
                try
                {
                    if (HttpContext.Current.Session[MFBConstants.keyLastTail] != null)
                        return Convert.ToInt32(HttpContext.Current.Session[MFBConstants.keyLastTail], CultureInfo.InvariantCulture);
                    if (HttpContext.Current.Request.Cookies[MFBConstants.keyLastTail] != null && HttpContext.Current.Request.Cookies[MFBConstants.keyLastTail].Value != null)
                        return Convert.ToInt32(HttpContext.Current.Request.Cookies[MFBConstants.keyLastTail].Value, CultureInfo.InvariantCulture);
                }
                catch (System.FormatException)
                {
                }
                return 0;
            }
        }

        /// <summary>
        /// The error from the most recent validation
        /// </summary>
        public string ErrorString { get; set; }

        #region Properties here belong in other groups, but F**king Microsoft's WSDL generator for Windows Phone is ORDER DEPENDENT!!!  How broken
        /// <summary>
        /// Hide this aircraft from the list of aircraft by default?
        /// </summary>
        public bool HideFromSelection
        {
            get { return (Flags & acMaskSuppressFavorite) != 0; }
            set { Flags = (Flags & ~acMaskSuppressFavorite) | (value ? acMaskSuppressFavorite : 0); }
        }
        /// <summary>
        /// The version of this aircraft (0 by default).  Disambiguates multiple aircraft with same tailnumber (e.g., for tailnumber re-use)
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// The default image (by thumbnail) to use, if present
        /// </summary>
        public string DefaultImage { get; set; }

        /// <summary>
        /// Is there a default role for this pilot in this aircraft?
        /// </summary>
        public PilotRole RoleForPilot
        {
            get { return (PilotRole)(Flags & acMaskRole); }
            set { Flags = (Flags & ~acMaskRole) | (UInt32)value; }
        }

        public DateTime RegistrationDue
        {
            get { return Maintenance.RegistrationExpiration; }
            set { Maintenance.RegistrationExpiration = value; }
        }

        /// <summary>
        /// Locked aircraft cannot be edited.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public Boolean IsLocked { get; set; }

        /// <summary>
        /// Any public (shared) notes about the aircraft
        /// </summary>
        public string PublicNotes { get; set; }

        /// <summary>
        /// Any private (per-user) notes about the aircraft
        /// </summary>
        public string PrivateNotes { get; set; }

        /// <summary>
        /// Is this an anonymous aircraft?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsAnonymous
        {
            get { return InstanceType == AircraftInstanceTypes.RealAircraft && TailNumber.StartsWith(CountryCodePrefix.szAnonPrefix, StringComparison.OrdinalIgnoreCase); }
        }
        /// <summary>
        /// Optional date of the glass upgrade.
        /// </summary>
        public DateTime? GlassUpgradeDate { get; set; }
        #endregion
        #endregion

        #endregion

        #region Constructors
        /// <summary>
        /// Create a new aircraft
        /// </summary>
        public Aircraft()
        {
            ModelID = MakeModel.UnknownModel;
            PublicNotes = PrivateNotes = string.Empty;
            IsLocked = false;
            AircraftID = idAircraftUnknown;
            ErrorString = ModelCommonName = ModelDescription = InstanceTypeDescription = string.Empty;
            MaintenanceChanges = new List<MaintenanceLog>();
            m_mr = new MaintenanceRecord();
        }

        internal protected Aircraft(MySqlDataReader dr) : this()
        {
            InitFromDataReader(dr);
        }

        /// <summary>
        /// Create a new aircraft from the database, loading based on aircraft ID
        /// </summary>
        /// <param name="id"></param>
        public Aircraft(int id) : this()
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["AircraftForUserCore"].ToString(), 0, "''", "''", " WHERE aircraft.idAircraft=?idAircraft") + " LIMIT 1");
            dbh.ReadRow(
                (comm) => { comm.Parameters.AddWithValue("idAircraft", id); },
                (dr) => { InitFromDataReader(dr); });
        }

        /// <summary>
        /// Create a new aircraft from the database, loading by tail number.
        /// </summary>
        /// <param name="szTail"></param>
        public Aircraft(string szTail) : this()
        {
            LoadAircraftByTailNum(szTail);
        }

        /// <summary>
        /// Given a row in a result set, initializes an aircraft.
        /// </summary>
        /// <param name="dr">The data row containing the logbook entry</param>
        protected void InitFromDataReader(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");

            AircraftID = Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture);
            TailNumber = dr["TailNumber"].ToString();
            Version = Convert.ToInt16(dr["version"], CultureInfo.InvariantCulture);
            ModelID = Convert.ToInt32(dr["idModel"], CultureInfo.InvariantCulture);
            InstanceTypeID = Convert.ToInt32(dr["InstanceType"], CultureInfo.InvariantCulture);

            LastAnnual = Convert.ToDateTime(util.ReadNullableField(dr, "LastAnnual", DateTime.MinValue), CultureInfo.InvariantCulture);
            LastVOR = Convert.ToDateTime(util.ReadNullableField(dr, "LastVOR", DateTime.MinValue), CultureInfo.InvariantCulture);
            LastAltimeter = Convert.ToDateTime(util.ReadNullableField(dr, "LastAltimeter", DateTime.MinValue), CultureInfo.InvariantCulture);
            LastTransponder = Convert.ToDateTime(util.ReadNullableField(dr, "LastTransponder", DateTime.MinValue), CultureInfo.InvariantCulture);
            LastStatic = Convert.ToDateTime(util.ReadNullableField(dr, "LastPitotStatic", DateTime.MinValue), CultureInfo.InvariantCulture);
            LastELT = Convert.ToDateTime(util.ReadNullableField(dr, "LastELT", DateTime.MinValue), CultureInfo.InvariantCulture);
            Last100 = Convert.ToDecimal(dr["Last100"], CultureInfo.InvariantCulture);
            LastOilChange = Convert.ToDecimal(dr["LastOil"], CultureInfo.InvariantCulture);
            LastNewEngine = Convert.ToDecimal(dr["LastEngine"], CultureInfo.InvariantCulture);
            RegistrationDue = Convert.ToDateTime(util.ReadNullableField(dr, "RegistrationDue", DateTime.MinValue), CultureInfo.InvariantCulture);
            m_fIsTailwheel = Convert.ToBoolean(dr["fTailwheel"], CultureInfo.InvariantCulture);
            IsGlass = Convert.ToBoolean(dr["HasGlassUpgrade"], CultureInfo.InvariantCulture);
            GlassUpgradeDate = (DateTime?) util.ReadNullableField(dr, "GlassUpgradeDate", null);
            IsLocked = Convert.ToBoolean(dr["IsLocked"], CultureInfo.InvariantCulture);

            InstanceTypeDescription = dr["InstanceTypeDesc"].ToString();
            ModelDescription = dr["Model"].ToString();
            ModelCommonName = dr["ModelCommonName"].ToString();
            ICAO = dr["Family"].ToString();
            if (String.IsNullOrEmpty(ICAO))
                ICAO = ModelDescription;
            Flags = UInt32.Parse(dr["Flags"].ToString(), CultureInfo.InvariantCulture);
            PublicNotes = Convert.ToString(dr["PublicNotes"], CultureInfo.InvariantCulture) ?? string.Empty;
            PrivateNotes = Convert.ToString(dr["UserNotes"], CultureInfo.InvariantCulture) ?? string.Empty;
            DefaultImage = Convert.ToString(dr["DefaultImage"], CultureInfo.InvariantCulture) ?? string.Empty;
        }

        public static IEnumerable<Aircraft> AircraftFromIDs(IEnumerable<int> ids)
        {
            string szIDs = String.Join(",", ids);
            DBHelperCommandArgs dba = new DBHelperCommandArgs(String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["AircraftForUserCore"].ToString(), 0, "''", "''", String.Format(CultureInfo.InvariantCulture, " WHERE aircraft.idAircraft in ({0})", szIDs)));
            List<Aircraft> lst = new List<Aircraft>();
            new DBHelper(dba).ReadRows(
                (comm) => { },
                (dr) => { lst.Add(new Aircraft(dr)); }
                );
            return lst;
        }
        #endregion

        #region Database
        /// <summary>
        /// Saves the flight into the DB, doing an insert or an update as appropriate.
        /// DO NOT CALL THIS DIRECTLY in general - it does NOT do any of the niceties around looking for collisions with existing aircraft.
        /// </summary>
        private void CommitToDB()
        {
            const string szCommitTemplate = @"{0} aircraft SET tailnumber = ?tailNumber, version=?version, idmodel = ?idModel, InstanceType = ?instanceType, 
                LastAnnual = ?LastAnnual, LastVOR = ?LastVOR, LastAltimeter = ?LastAltimeter, LastTransponder = ?LastTransponder, LastPitotStatic=?LastPitotStatic,  
                HasGlassUpgrade = ?HasGlass, GlassUpgradeDate=?glassUpgradeDate, PublicNotes=?PublicNotes, LastElt=?LastElt, Last100=?Last100, LastOil=?LastOil, LastEngine=?LastEngine, RegistrationDue=?RegistrationDue, IsLocked=?locked {1} ";
            string szQ = String.Format(CultureInfo.InvariantCulture, szCommitTemplate, IsNew ? "INSERT INTO" : "UPDATE", IsNew ? string.Empty : "WHERE idaircraft = ?idAircraft");

            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery(szQ,
                (comm) =>
                {
                    comm.Parameters.AddWithValue("tailnumber", TailNumber);
                    comm.Parameters.AddWithValue("idmodel", ModelID);
                    comm.Parameters.AddWithValue("InstanceType", InstanceTypeID);
                    comm.Parameters.AddWithValue("HasGlass", IsGlass);
                    comm.Parameters.AddWithValue("glassUpgradeDate", GlassUpgradeDate.HasValue ? GlassUpgradeDate.Value : (DateTime?)null);
                    comm.Parameters.AddWithValue("version", Version);

                    comm.Parameters.AddWithValue("LastAnnual", LastAnnual);
                    comm.Parameters.AddWithValue("LastVOR", LastVOR);
                    comm.Parameters.AddWithValue("LastAltimeter", LastAltimeter);
                    comm.Parameters.AddWithValue("LastTransponder", LastTransponder);
                    comm.Parameters.AddWithValue("LastPitotStatic", LastStatic);
                    comm.Parameters.AddWithValue("LastElt", LastELT);
                    comm.Parameters.AddWithValue("Last100", Last100);
                    comm.Parameters.AddWithValue("LastOil", LastOilChange);
                    comm.Parameters.AddWithValue("LastEngine", LastNewEngine);
                    comm.Parameters.AddWithValue("RegistrationDue", RegistrationDue);
                    comm.Parameters.AddWithValue("PublicNotes", PublicNotes.LimitTo(4094));
                    comm.Parameters.AddWithValue("locked", IsLocked);

                    if (!IsNew)
                        comm.Parameters.AddWithValue("idAircraft", AircraftID);
                });
            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException("Error saving aircraft - " + szQ + "\r\n" + dbh.LastError);

            if (IsNew)
            {
                AircraftID = dbh.LastInsertedRowId;

                foreach (MaintenanceLog ml in MaintenanceChanges)
                    ml.AircraftID = this.AircraftID;
            }

            // if we got here, then everything above must have succeeded - save any pending maintenance changes
            foreach (MaintenanceLog ml in MaintenanceChanges)
                ml.FAddToLog();

            MaintenanceChanges = new List<MaintenanceLog>(); // clear out the changes since they've been saved.
        }

        /// <summary>
        /// Commits changes to the aircraft to the db.
        /// </summary>
        public void CommitForUser(string szUser)
        {
            if (!IsValid())
                throw new MyFlightbookException(ErrorString);

            // Check for a tailnumber collision with a DIFFERENT existing aircraft
            // Then check to see if THIS ID is in the list (if the list has any hits at all)
            // 4 possible cases:
            //  (a) This is NEW, Aircraft ID is in the list - CAN'T HAPPEN
            //  (b) This is NEW, Aircraft ID is NOT in the list - MATCH TO THE FIRST HIT AND RETURN
            //  (c) This is EXISTING, Aircraft ID is in the list - WE JUST FOUND OURSELVES; CONTINUE AND UPDATE
            //  (d) This is EXISTING, Aircraft ID is NOT in the list - IF WE WEREN'T IN THE LIST, WE MUST HAVE CHANGED OUR TAILNUMBER TO AN EXISTING AIRCRAFT; MATCH TO THE FIRST HIT AND RETURN
            // (b) and (d) reduce to basically mean that if we are NOT in the list, we should initialize to the first hit and return.
            //
            // There are two additional wrinkles to this:
            //  (b.2): the model specified on this aircraft does NOT match the model specified on the first match in the list.  We will use the model of the existing
            //         aircraft, but should notify the user of this fact, IF the existing aircraft is being used in flights; if not, we can continue and overwrite it.
            //  (c.2): the model does not match the specified model.  We are CHANGING the model of the aircraft, and thus need to notify all users of this aircraft of this fact.  
            //  (d.2): is the same as (b.2), but since the aircraft already exists, we'll keep both aircraft (and just initialize this object instance to the other one); no notifications necessary
            //  
            //  In any of these cases, we want to check if the existing aircraft has any flights; if not, then we can override the existing aircraft's model
            List<Aircraft> lstSimilarTails = Aircraft.AircraftMatchingTail(TailNumber);
            Aircraft acMatch = null;
            if (lstSimilarTails.Count > 0)
            {
                // prefer an exact match over 1st-hit match
                foreach (Aircraft ac in lstSimilarTails)
                    if (ac.AircraftID == AircraftID)
                        acMatch = ac;
                if (acMatch == null)
                    acMatch = lstSimilarTails[0];

                // (b) and (b.2)
                if (this.AircraftID == Aircraft.idAircraftUnknown)
                {
                    int modelIDRequested = this.ModelID;
                    bool fReuseExisting = NotifyNewAircraftModelIgnored(acMatch, szUser);
                    this.LoadAircraftByTailNum(acMatch.TailNumber);
                    if (fReuseExisting)
                        return;
                    else
                        this.ModelID = modelIDRequested;    // continue on to overwrite existing definition.
                }
                else // (c) or (d) - this is an existing aircraft
                {
                    if (this.AircraftID == acMatch.AircraftID)
                    {
                        // was found in list - case (c) and (c.2)
                        // Since this is a MODIFY, we will let the change go through and notify the other users (ignoring the return result)
                        NotifyModelChanged(acMatch, szUser);
                    }
                    else
                    {
                        // (d) and (d.2)
                        // This specific aircraft ID was NOT found in list, we're colliding with another aircraft.  Load that aircraft and return
                        this.LoadAircraftByTailNum(acMatch.TailNumber);
                        return;
                    }
                }
            }

            // If we're here, it's time to save the changes
            CommitToDB();
        }

        /// <summary>
        /// Commits the aircraft without specifying a user
        /// </summary>
        public void Commit()
        {
            CommitForUser(string.Empty);
        }

        /// <summary>
        /// Adds the aircraft for the specified user (will add the aircraft to the user's aircraft list)
        /// </summary>
        /// <param name="szUser">Username for whom the airplane is being added.</param>
        public void Commit(string szUser)
        {
            CommitForUser(szUser);

            // id should now be set to the id of the aircraft
            if (AircraftID < 0)
                throw new MyFlightbookException("Somehow aircraftID is less than 0 after commit!");

            UserAircraft ua = new UserAircraft(szUser);
            ua.InvalidateCache();
            ua.FAddAircraftForUser(this);
        }
        #endregion

        /// <summary>
        /// Save the ID of the last tail in a cookie
        /// </summary>
        /// <param name="szValue">The ID of the most recently used aircraft</param>
        static public void SaveLastTail(int ID)
        {
            // Save the aircraft ID in a cookie
            HttpContext.Current.Response.Cookies[MFBConstants.keyLastTail].Value = ID.ToString(CultureInfo.InvariantCulture);
            HttpContext.Current.Response.Cookies[MFBConstants.keyLastTail].Expires = DateTime.Now.AddYears(5);
            HttpContext.Current.Session[MFBConstants.keyLastTail] = ID.ToString(CultureInfo.InvariantCulture);
        }

        #region Validation
        private const string szRegexValidTail = "^[a-zA-Z0-9]+-?[a-zA-Z0-9]+-?[a-zA-Z0-9]+$";

        /// <summary>
        /// Admin utility to quickly find all invalid aircraft (since examining them one at a time is painfully slow and pounds the database)
        /// KEEP IN SYNC WITH IsValid!!
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Aircraft> AdminAllInvalidAircraft()
        {
            List<Aircraft> lst = new List<Aircraft>();

            List<string> lstNakedPrefix = new List<string>();
            foreach (CountryCodePrefix cc in CountryCodePrefix.CountryCodes())
                lstNakedPrefix.Add(cc.NormalizedPrefix);

            string szNaked = String.Join("', '", lstNakedPrefix);

            const string szQInvalidAircraftRestriction = @"WHERE
(aircraft.idmodel < 0 OR models.idmodel IS NULL)
OR (aircraft.tailnumber = '') OR (LENGTH(aircraft.tailnumber) <= 2) OR (LENGTH(aircraft.tailnumber) > {0})
OR (aircraft.tailnumber LIKE '{2}%' AND aircraft.tailnumber <> CONCAT('{2}', LPAD(aircraft.idmodel, 6, '0')))
OR (aircraft.tailnumber NOT LIKE '{2}%' AND aircraft.tailnumber NOT RLIKE '{1}')
OR (aircraft.instancetype = 1 AND aircraft.tailnumber LIKE '{3}%')
OR (aircraft.instancetype <> 1 AND aircraft.tailnumber NOT LIKE '{3}%')
OR (models.fSimOnly = 1 AND aircraft.instancetype={4})
OR (models.fSimOnly = 2 AND aircraft.InstanceType={4} AND aircraft.tailnumber NOT LIKE '{2}%')
OR (REPLACE(aircraft.tailnumber, '-', '') IN ('{5}'))";

            string szQInvalid = String.Format(CultureInfo.InvariantCulture, szQInvalidAircraftRestriction, 
                maxTailLength, 
                szRegexValidTail, 
                CountryCodePrefix.szAnonPrefix, 
                CountryCodePrefix.szSimPrefix,
                (int) AircraftInstanceTypes.RealAircraft,
                szNaked);

            string szQ = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["AircraftForUserCore"].ToString(), 0, "''", "''", szQInvalid);

            DBHelper dbh = new DBHelper(szQ);
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new Aircraft(dr)); });

            // set the error for each one
            foreach (Aircraft ac in lst)
                ac.IsValid(true);

            return lst;
        }

        /// <summary>
        /// Tests the aircraft for validity prior to commitment
        /// KEEP IN SYNC WITH AdminAllInvalidAircraft()
        /// </summary>
        /// <param name="fCheckMake">True to check that the make is valid (can be slow - hits DB)</param>
        /// <returns>True for valid aircraft</returns>
        public Boolean IsValid(Boolean fCheckMake = true)
        {
            ErrorString = "";

            if (String.IsNullOrEmpty(TailNumber))
            {
                ErrorString = Resources.Aircraft.errNoTail;
                return false;
            }

            if (TailNumber.Length <= 2)
            {
                ErrorString = String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.errInvalidTailShort, TailNumber);
                return false;
            }

            if (TailNumber.Length > maxTailLength)
            {
                ErrorString = Resources.Aircraft.errInvalidTailLong;
                return false;
            }

            // Anonymous aircraft have tightly prescribed tailnumbers
            if (IsAnonymous)
            {
                if (String.Compare(TailNumber, AnonymousTailnumberForModel(ModelID), StringComparison.OrdinalIgnoreCase) != 0)
                    ErrorString = Resources.Aircraft.errBadAnonymousName;
            }
            else
            {
                Regex r = new Regex(szRegexValidTail, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                Match mt = r.Match(TailNumber);
                if (mt.Captures.Count != 1 || String.Compare(TailNumber, mt.Captures[0].Value, StringComparison.OrdinalIgnoreCase) != 0)
                    ErrorString = Resources.Aircraft.errInvalidChars;
            }

            CountryCodePrefix cc = CountryCodePrefix.BestMatchCountryCode(TailNumber);
            if (cc != null && TailNumber.Length <= cc.Prefix.Length)
                ErrorString = String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.errInvalidAfterCountryCode, cc.Prefix);

            /*
            // we allow aircraft with no country code tailnumber
            if (cc.IsUnknown || cc == CountryCodePrefix.UnknownCountry)
                ErrorString = String.Format(CultureInfo.CurrentCulture, "Aircraft tailnumber does not include country prefix.");
             * */

            if (ModelID < 0)
            {
                ErrorString = Resources.Aircraft.errNoMakeModel;
                return false;
            }

            bool fIsRealAircraft = InstanceType == AircraftInstanceTypes.RealAircraft;

            if (fIsRealAircraft && cc.IsSim)
                ErrorString = String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.errSimPrefixReserved, cc.Prefix);

            if (!fIsRealAircraft && !cc.IsSim)
                ErrorString = String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.errMissingSimPrefix, CountryCodePrefix.szSimPrefix);

            // validate the model by loading the make model; a bad modelID will throw an exception.
            if (fCheckMake)
            {
                try
                {
                    MakeModel m = MakeModel.GetModel(this.ModelID);
                    if (m.MakeModelID == MakeModel.UnknownModel)
                        throw new MyFlightbookException("Bad Model");

                    // Check for allowed aircraft type
                    if (fIsRealAircraft)
                    {
                        switch (m.AllowedTypes)
                        {
                            case AllowedAircraftTypes.Any:
                                break;
                            case AllowedAircraftTypes.SimulatorOnly:
                                ErrorString = String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.errMakeIsSimOnly, m.DisplayName);
                                break;
                            case AllowedAircraftTypes.SimOrAnonymous:
                                if (!IsAnonymous)
                                    ErrorString = Resources.Aircraft.errMakeIsAnonOnly;
                                break;
                        }
                    }
                }
                catch (MyFlightbookException)
                {
                    ErrorString = String.Format(CultureInfo.CurrentCulture, "Invalid model ID {0}", this.ModelID);
                }
            }

            return ErrorString.Length == 0;
        }

        /// <summary>
        /// Validates the aircraft, fixing the tail if needed for anonymous/sim aircraft
        /// </summary>
        /// <returns>true if all valid</returns>
        public Boolean FixTailAndValidate()
        {
            if (IsAnonymous)
                TailNumber = Aircraft.AnonymousTailnumberForModel(ModelID);
            if (InstanceType != AircraftInstanceTypes.RealAircraft)
                TailNumber = Aircraft.SuggestSims(ModelID, InstanceType)[0].TailNumber;

            return IsValid(true);
        }
        #endregion

        /// <summary>
        /// Human readable display of available versions of the aircraft
        /// </summary>
        /// <returns>Empty string if only one version, otherwise a human-readable list of alternatives.</returns>
        private string ListAlternativeVersions()
        {
            List<Aircraft> lst = Aircraft.AircraftMatchingTail(TailNumber);
            if (lst.Count < 2)
                return string.Empty;

            StringBuilder sb = new StringBuilder("\r\n" + Resources.Aircraft.alternateVersionsList + "\r\n");
            lst.ForEach((ac) => sb.AppendFormat("\r\n - {0}", ac.LongDescription));
            sb.Append("\r\n");

            return sb.ToString();
        }

        #region Admin functions
        /// <summary>
        /// If this is a new aircraft being saved which matches on tailnumber to an existing one with a different model, notifies the user that the existing model was re-used.
        /// </summary>
        /// <param name="acMatch">The aircraft in the system that was matched</param>
        /// <param name="szUser">The name of the user; no notification is sent if this is null or empty</param>
        /// <returns>true if there is a model conflict and the matching aircraft should be re-used; false if the there is no conflict or if the matching aircraft should be overwritten</returns>
        private bool NotifyNewAircraftModelIgnored(Aircraft acMatch, string szUser)
        {
            if (this.ModelID == acMatch.ModelID)
                return false;

            // see if we need to use the matching aircraft's model
            AircraftStats acs = new AircraftStats(String.Empty, acMatch.AircraftID);
            if (acs.Users != 0)
            {
                if (!String.IsNullOrEmpty(szUser))
                {
                    Profile pf = Profile.GetUser(szUser);
                    string szNotification = String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.Aircraft.AircraftModelNewModelIgnored),
                        pf.UserFullName,
                        this.TailNumber,
                        new MakeModel(this.ModelID).DisplayName,
                        new MakeModel(acMatch.ModelID).DisplayName,
                        ListAlternativeVersions());

                    string szSubject = String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.ModelCollisionSubjectLine, this.TailNumber, Branding.CurrentBrand.AppName);
                    util.NotifyUser(szSubject, szNotification, new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// If this is an existing aircraft and its model is being changed, notifies any other users of that aircraft that the model has changed.
        /// No mail is sent if:
        /// a) the models match
        /// b) there are no flights in the aircraft
        /// c) there is only one user of the aircraft, and that is the calling user
        /// </summary>
        /// <param name="acMatch">The aircraft in the system that was matched</param>
        /// <param name="szUser">The name of the user; no notification is sent if this is null or empty</param>
        private void NotifyModelChanged(Aircraft acMatch, string szUser)
        {
            if (this.ModelID == acMatch.ModelID)
                return;

            // See if there's even an issue
            // If it's not used in any flights, or there's exactly one user (the owner), no problem.
            AircraftStats acs = new AircraftStats(String.Empty, acMatch.AircraftID);
            if (acs.Flights == 0 || (acs.Users == 1 && String.Compare(szUser, acs.UserNames[0], StringComparison.CurrentCultureIgnoreCase) == 0))
                return;

            // Notify the admin here - model changed, I want to see it.
            string szSubject = String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.ModelCollisionSubjectLine, this.TailNumber, Branding.CurrentBrand.AppName);
            string szNotificationTemplate = Branding.ReBrand(Resources.Aircraft.AircraftModelChangedNotification);
            string szMakeMatch = new MakeModel(acMatch.ModelID).DisplayName;
            string szMakeThis = new MakeModel(this.ModelID).DisplayName;

            // Admin events can merge duplicate models, so detect that
            if (String.Compare(szMakeMatch, szMakeThis, StringComparison.CurrentCultureIgnoreCase) == 0)
                return;

            util.NotifyAdminEvent(szSubject, String.Format(CultureInfo.CurrentCulture, "User: {0}\r\n\r\n{1}\r\n\r\nMessage that was sent to other users:\r\n\r\n{2}", Profile.GetUser(szUser).DetailedName,
                String.Format(CultureInfo.InvariantCulture,"http://{0}{1}?id={2}&a=1", Branding.CurrentBrand.HostName, VirtualPathUtility.ToAbsolute("~/Member/EditAircraft.aspx"), AircraftID),
                String.Format(CultureInfo.CurrentCulture,szNotificationTemplate, "(username)", this.TailNumber, szMakeMatch, szMakeThis)), ProfileRoles.maskCanManageData);

            // If we're here, then there are other users - need to notify all of them of the change.
            if (!String.IsNullOrEmpty(szUser))
            {
                foreach (string szName in acs.UserNames)
                {
                    // Don't send to the person initiating the change
                    if (String.Compare(szName, szUser, StringComparison.CurrentCultureIgnoreCase) == 0)
                        continue;

                    Profile pf = Profile.GetUser(szName);
                    util.NotifyUser(szSubject, String.Format(CultureInfo.CurrentCulture,szNotificationTemplate, pf.UserFullName, this.TailNumber, szMakeMatch, szMakeThis), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);
                }
            }
        }

        /// <summary>
        /// Admin function to merge an old aircraft into a new one
        /// </summary>
        /// <param name="acMaster">The TARGET aircraft</param>
        /// <param name="ac">The aircraft being merged - this one will be DELETED (but a tombstone will remain)</param>
        public static void AdminMergeDupeAircraft(Aircraft acMaster, Aircraft ac)
        {
            if (ac == null)
                throw new ArgumentNullException("ac");
            if (acMaster == null)
                throw new ArgumentNullException("acMaster");
            if (ac.AircraftID == acMaster.AircraftID)
                return;

            // merge the aircraft into the master.  This will merge maintenance and images.
            acMaster.MergeWith(ac, true);

            // map all future references to this aircraft to the new master
            AircraftTombstone act = new AircraftTombstone(ac.AircraftID, acMaster.AircraftID);
            act.Commit();

            // It's slower than doing a simple "UPDATE Flights f SET f.idAircraft=?idAircraftNew WHERE f.idAircraft=?idAircraftOld",
            // but first find all of the users with flights in the old aircraft and then call ReplaceAircraftForUser on each one
            // This ensures that useraircraft is correctly updated.
            DBHelperCommandArgs dba = new DBHelperCommandArgs("SELECT DISTINCT username FROM flights WHERE idAircraft=?idAircraftOld");
            dba.AddWithValue("idAircraftNew", acMaster.AircraftID);
            dba.AddWithValue("idAircraftOld", ac.AircraftID);
            // Remap any flights that use the old aircraft
            DBHelper dbh = new DBHelper(dba);

            List<string> lstAffectedUsers = new List<string>();
            dbh.ReadRows((comm) => { }, (dr) => { lstAffectedUsers.Add((string)dr["username"]); });

            foreach (string szUser in lstAffectedUsers)
                new UserAircraft(szUser).ReplaceAircraftForUser(acMaster, ac, true);
                
            // remap any club aircraft and scheduled events.
            dbh.CommandText = "UPDATE clubaircraft ca SET ca.idaircraft=?idAircraftNew WHERE ca.idAircraft=?idAircraftOld";
            dbh.DoNonQuery();
            dbh.CommandText = "UPDATE scheduledEvents se SET se.resourceid=?idAircraftNew WHERE se.resourceid=?idAircraftOld";
            dbh.DoNonQuery();

            dbh.CommandText = "UPDATE useraircraft ua SET ua.idAircraft=?idAircraftNew WHERE ua.idAircraft=?idAircraftOld";
            dbh.DoNonQuery();

            // And fix up any pre-existing tombstones that point to this aircraft
            dbh.CommandText = "UPDATE aircrafttombstones SET idMappedAircraft=?idAircraftNew WHERE idMappedAircraft=?idAircraftOld";
            dbh.DoNonQuery();

            // Finally, it should now be safe to delete the aircraft
            dbh.CommandText = "DELETE FROM aircraft WHERE idAircraft=?idAircraftOld";
            dbh.DoNonQuery();
        }

        /// <summary>
        /// Admin function to make this instance the default instance for the tailnumber.
        /// </summary>
        public void MakeDefault()
        {
            Aircraft acCurrentDefault = AircraftMatchingTail(this.TailNumber)[0];   // should never crash, since we should at least get this aircraft back
            if (AircraftID == acCurrentDefault.AircraftID)  // already the default - nothing to do.
                return;

            int currentVersion = Version;
            Version = 0;
            CommitToDB();

            if (acCurrentDefault.Version == 0)  // if we're replacing the lowest verison number, 
            {
                acCurrentDefault.Version = currentVersion;
                acCurrentDefault.CommitToDB();
            }
        }

        /// <summary>
        /// Admin function to clone an aircraft and migrate the specified users to the new aircraft
        /// </summary>
        /// <param name="idModelTarget">The model for the cloned aircraft</param>
        /// <param name="lstUsersToMigrate">The users that should migrate to the new aircraft</param>
        public void Clone(int idModelTarget, ReadOnlyCollection<string> lstUsersToMigrate)
        {
            if (lstUsersToMigrate == null)
                throw new ArgumentNullException("lstUsersToMigrate");

            if (idModelTarget == MakeModel.UnknownModel)
                throw new MyFlightbookException("Can't clone to empty model");

            Aircraft acOriginal = new Aircraft(this.AircraftID);

            if (idModelTarget == acOriginal.ModelID)
                throw new MyFlightbookException("Can't clone to same model");

            AircraftStats acsOriginal = new AircraftStats(string.Empty, AircraftID);
            MakeModel mmOriginal = MakeModel.GetModel(acOriginal.ModelID);
            MakeModel mmNew = MakeModel.GetModel(idModelTarget);

            AircraftID = idAircraftUnknown;
            m_mr = new MaintenanceRecord();
            MaintenanceChanges = new List<MaintenanceLog>();
            ModelID = idModelTarget;

            // Find the highest existing version and increment one beyond that.
            List<Aircraft> lstAc = AircraftMatchingTail(this.TailNumber);
            lstAc.ForEach((ac) => { if (ac.Version > this.Version) this.Version = ac.Version; });
            Version++;
            CommitToDB();

            foreach (string szUser in lstUsersToMigrate)
            {
                UserAircraft ua = new UserAircraft(szUser);
                ua.ReplaceAircraftForUser(this, acOriginal, true);
            }

            string szAlternatives = ListAlternativeVersions();

            // Notify all users of the aircraft about the change
            foreach (string sz in acsOriginal.UserNames)
            {
                Profile pf = Profile.GetUser(sz);
                string szEmailNotification =
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture,Resources.EmailTemplates.AircraftTailSplit,
                    pf.UserFullName,
                    acOriginal.TailNumber,
                    mmOriginal.DisplayName,
                    mmNew.DisplayName,
                    lstUsersToMigrate.Contains(sz) ? mmNew.DisplayName : mmOriginal.DisplayName,
                    szAlternatives));

                util.NotifyUser(String.Format(CultureInfo.CurrentCulture,Resources.Aircraft.ModelCollisionSubjectLine, this.TailNumber, Branding.CurrentBrand.AppName), szEmailNotification, new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);
            }
        }

        /// <summary>
        /// In case two aircraft in the system really should be one, this will merge the two aircraft, updating the maintenance records as well
        /// NOTE: this does NOT update any flights, nor does it update any user aircraft.
        /// </summary>
        /// <param name="ac">The aircraft to merge into this one</param>
        /// <param name="fIgnoreModelMismatch">whether to ignore mismatched model ID's</param>
        public void MergeWith(Aircraft ac, bool fIgnoreModelMismatch)
        {
            // do nothing if we're merging with self.
            if (this.AircraftID == ac.AircraftID)
                return;

            if (!fIgnoreModelMismatch && ac.ModelID != this.ModelID)
                throw new MyFlightbookException("Can't merge two aircraft that are not the same make/model");
            if (ac.InstanceType != this.InstanceType)
                throw new MyFlightbookException("Can't merge two aircraft that are not the same instancetype");

            // Update to the latest of any maintenance
            this.Last100 = Math.Max(this.Last100, ac.Last100);
            this.LastAltimeter = this.LastAltimeter.LaterDate(ac.LastAltimeter);
            this.LastAnnual = this.LastAnnual.LaterDate(ac.LastAnnual);
            this.LastELT = this.LastELT.LaterDate(ac.LastELT);
            this.LastNewEngine = Math.Max(this.LastNewEngine, ac.LastNewEngine);
            this.LastOilChange = Math.Max(this.LastOilChange, ac.LastOilChange);
            this.LastStatic = this.LastStatic.LaterDate(ac.LastStatic);
            this.LastTransponder = this.LastTransponder.LaterDate(ac.LastTransponder);
            this.LastVOR = this.LastVOR.LaterDate(ac.LastVOR);

            this.Commit();

            // Migrate any maintenance updates
            new DBHelper().DoNonQuery("UPDATE maintenancelog SET idAircraft=?idaircraftNew WHERE idAircraft=?idaircraftOld",
                (comm) =>
                {
                    comm.Parameters.AddWithValue("idaircraftNew", this.AircraftID);
                    comm.Parameters.AddWithValue("idaircraftOld", ac.AircraftID);
                });

            // Migrate any images
            ImageList ilSrc = new ImageList(MFBImageInfo.ImageClass.Aircraft, ac.AircraftID.ToString());
            ilSrc.Refresh();

            foreach (MFBImageInfo mfbii in ilSrc.ImageArray)
                mfbii.MoveImage(this.AircraftID.ToString());

            this.PopulateImages();
        }
        #endregion

        #region Maintenance management
        /// <summary>
        /// Write an entry to the maintenance log for the current airplane if something has changed
        /// </summary>
        /// <param name="sz1">The old value</param>
        /// <param name="sz2">The new value</param>
        /// <param name="szMessage">The message to log if something has changed</param>
        /// <param name="szComment">Any additional comments provided by the user</param>
        /// <returns>A log entry if there is a change to record</returns>
        private MaintenanceLog LogIfChanged(object o1, object o2, string szMessage, string szComment, string szUser)
        {
            string sz1, sz2;
            Boolean fDelete = false;

            if (o1 is Decimal && o2 is Decimal)
            {
                Decimal d1 = (Decimal)o1;
                Decimal d2 = (Decimal)o2;

                if (d1 == d2)
                    return null;
                sz1 = d1.ToString("0.0", CultureInfo.CurrentCulture);
                sz2 = d2.ToString("0.0", CultureInfo.CurrentCulture);

                fDelete = (d2 == 0.0M);
            }
            else if (o1 is DateTime && o2 is DateTime)
            {
                DateTime dt1 = (DateTime)o1;
                DateTime dt2 = (DateTime)o2;
                sz1 = dt1.ToShortDateString();
                sz2 = dt2.ToShortDateString();

                if (String.Compare(sz1, sz2, StringComparison.CurrentCultureIgnoreCase) == 0)
                    return null;

                fDelete = (dt2.CompareTo(DateTime.MinValue) == 0 || dt2.Year < 1000);
            }
            else
                throw new MyFlightbookException("Objects are not compatible type");

            MaintenanceLog ml = new MaintenanceLog();
            ml.AircraftID = AircraftID;
            ml.ChangeDate = DateTime.Now;
            ml.Description = fDelete ? Resources.Aircraft.MaintenanceDelete + szMessage : Resources.Aircraft.MaintenanceUpdate + szMessage + " (" + sz2 + ")";
            ml.Comment = szComment;
            ml.User = szUser;
            return ml;
        }

        private void AddToArrayIfNotNull(List<MaintenanceLog> al, MaintenanceLog ml)
        {
            if (ml != null)
                al.Add(ml);
        }

        /// <summary>
        /// Updates the maintenance for the aircraft, generating appropriate log entries
        /// </summary>
        /// <returns>True if any changes were made</returns>
        public void UpdateMaintenanceForUser(MaintenanceRecord mr, string szUser)
        {
            if (mr == null)
                throw new ArgumentNullException("mr");

            List<MaintenanceLog> rgml = new List<MaintenanceLog>();

            AddToArrayIfNotNull(rgml, LogIfChanged(LastAltimeter, mr.LastAltimeter, Resources.Aircraft.InspectionLogAltimeter, "", szUser));
            AddToArrayIfNotNull(rgml, LogIfChanged(LastAnnual, mr.LastAnnual, Resources.Aircraft.InspectionLogAnnual, "", szUser));
            AddToArrayIfNotNull(rgml, LogIfChanged(LastELT, mr.LastELT, Resources.Aircraft.InspectionLogELT, "", szUser));
            AddToArrayIfNotNull(rgml, LogIfChanged(LastStatic, mr.LastStatic, Resources.Aircraft.InspectionLogPitotStatic, "", szUser));
            AddToArrayIfNotNull(rgml, LogIfChanged(LastTransponder, mr.LastTransponder, Resources.Aircraft.InspectionLogTransponder, "", szUser));
            AddToArrayIfNotNull(rgml, LogIfChanged(LastVOR, mr.LastVOR, Resources.Aircraft.InspectionLogVOR, "", szUser));
            AddToArrayIfNotNull(rgml, LogIfChanged(LastNewEngine, mr.LastNewEngine, Resources.Aircraft.InspectionLogEngine, "", szUser));
            AddToArrayIfNotNull(rgml, LogIfChanged(Last100, mr.Last100, Resources.Aircraft.InspectionLog100hr, "", szUser));
            AddToArrayIfNotNull(rgml, LogIfChanged(LastOilChange, mr.LastOilChange, Resources.Aircraft.InspectionLogOil, "", szUser));
            AddToArrayIfNotNull(rgml, LogIfChanged(RegistrationDue, mr.RegistrationExpiration, Resources.Aircraft.RegistrationRenewal, string.Empty, szUser));

            MaintenanceChanges = rgml;

            // now that we've logged the diffs, update the maintenance record with the new data
            m_mr = mr;
        }
        #endregion

        #region static utilities to query/retrieve aircraft
        /// <summary>
        /// Query to get aircraft by tailnumber, returns lowest version to highest.  Normalizes for hyphens.
        /// </summary>
        private static string AircraftByTailQuery
        {
            get { return String.Format(CultureInfo.InvariantCulture,ConfigurationManager.AppSettings["AircraftForUserCore"].ToString(), 0, "''", "''", "WHERE REPLACE(UPPER(tailnumber), '-', '')=?tailNum"); }
        }

        /// <summary>
        /// Returns a list of aircraft based on a list of tailnumbers NOT SQL SAFE!!!
        /// You MUST strip any non-alphanumeric character from the tailnumber prior to submission.
        /// </summary>
        /// <param name="lstTails">The list of tailnumbers</param>
        /// <returns>The list of matching tailnumbers</returns>
        public static List<Aircraft> AircraftByTailListQuery(IEnumerable<string> lstTails)
        {
            string szQ = String.Format(CultureInfo.InvariantCulture,ConfigurationManager.AppSettings["AircraftForUserCore"].ToString(), 0, "''", "''", String.Format(CultureInfo.InvariantCulture,"WHERE REPLACE(UPPER(tailnumber), '-', '') IN ('{0}')", String.Join("', '", lstTails)));

            List<Aircraft> lst = new List<Aircraft>();

            DBHelper dbh = new DBHelper(szQ);
            dbh.ReadRows(
                (comm) => { },
                (dr) => { lst.Add(new Aircraft(dr)); });

            return lst;
        }

        /// <summary>
        /// Loads the specified aircraft by tailnumber
        /// </summary>
        /// <param name="szTail">Fully qualified tail number</param>
        /// <returns>True for success</returns>
        private Boolean LoadAircraftByTailNum(string szTail)
        {
            DBHelper dbh = new DBHelper(AircraftByTailQuery);
            return dbh.ReadRow(
                (comm) => { comm.Parameters.AddWithValue("tailNum", NormalizeTail(szTail)); },
                (dr) => { InitFromDataReader(dr); });
        }

        /// <summary>
        /// Finds all aircraft with the specified tailnumber, including different versions.
        /// </summary>
        /// <param name="szTail">Tailnumber to match.  Hyphens will be stripped</param>
        /// <returns>The aircraft (could be more than one) that matches.</returns>
        public static List<Aircraft> AircraftMatchingTail(string szTail)
        {
            List<Aircraft> lstAc = new List<Aircraft>();
            DBHelper dbh = new DBHelper(AircraftByTailQuery);
            dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("tailNum", NormalizeTail(szTail)); },
                (dr) => { lstAc.Add(new Aircraft(dr)); });
            return lstAc;
        }

        /// <summary>
        /// Return all matching sims
        /// </summary>
        /// <param name="idmodel">Only retrieve sims that match this model, -1 for none</param>
        /// <param name="fAllSims">True for all sims, else restrict to instanceType</param>
        /// <param name="instanceType">Restrict on a particular instance type; ignored if fAllSims is true</param>
        /// <returns>All matching aircraft in the system</returns>
        public static Collection<Aircraft> GetSims(int idmodel, Boolean fAllSims, AircraftInstanceTypes instanceType)
        {
            // get ALL of the aircraft.
            Aircraft[] rgua = (new UserAircraft("")).GetAircraftForUser(UserAircraft.AircraftRestriction.AllSims, idmodel);

            List<Aircraft> lstAc = new List<Aircraft>(rgua);

            lstAc.RemoveAll(ac => (idmodel != MakeModel.UnknownModel && ac.ModelID != idmodel) ||
                                  (!fAllSims && ac.InstanceType != instanceType) ||
                                  !ac.TailNumber.StartsWith(CountryCodePrefix.SimCountry.Prefix, StringComparison.OrdinalIgnoreCase));
            lstAc.Sort((ac1, ac2) => { return string.Compare(ac1.ModelCommonName, ac2.ModelCommonName, StringComparison.CurrentCultureIgnoreCase); });

            return new Collection<Aircraft>(lstAc);
        }
        #endregion

        private static Regex rNormalChars = new Regex("[^a-zA-Z0-9#]", RegexOptions.Compiled);

        /// <summary>
        /// Removes all of the dashes/hyphens from a tailnumber, except optionally the one separating the country code from the rest of the registration
        /// Note: if the country code already contains a hyphen (dash), such as "F-OG", then we just use that dash; otherwise, we append it.
        /// E.g., Canadian aircraft C1234 would become C-1234 but FOG1234 would become F-OG1234
        /// </summary>
        /// <param name="szTail">The tailnumber</param>
        /// <param name="cc">The tailnumber's country code (optional) - if specified, controls hyphenation of resulting tail.  E.g., C-FABC becomes CFABC for comparison purposes, but if the 
        /// canadian country code (which indicates hyphenation) is passed, this will normalize to "C-FABC".
        /// </param>
        /// <returns></returns>
        public static string NormalizeTail(string szTail, CountryCodePrefix cc = null)
        {
            if (String.IsNullOrEmpty(szTail))
                return szTail;

            // strip all hyphens/dashes/etc
            string szResult = rNormalChars.Replace(szTail.ToUpperInvariant(), string.Empty);

            // if a country code is specified, follow it's lead w.r.t. prefix
            if (cc != null)
                szResult = Regex.Replace(szResult, "^" + cc.NormalizedPrefix, cc.HyphenatedPrefix);

            return szResult;
        }

        /// <summary>
        /// Returns a normalized tail using 
        /// </summary>
        public string NormalizedTail
        {
            get { return NormalizeTail(TailNumber, CountryCodePrefix.BestMatchCountryCode(TailNumber)); }
        }

        /// <summary>
        /// Returns the tailnumber for an anonymous aircraft of a given model
        /// </summary>
        /// <param name="idmodel">The requested model</param>
        /// <returns>A unique and consistent tailnumber</returns>
        public static string AnonymousTailnumberForModel(int idmodel)
        {
            return String.Format(CultureInfo.InvariantCulture,"{0}{1:D6}", CountryCodePrefix.szAnonPrefix, idmodel);
        }

        /// <summary>
        /// Returns a suggested aircraft for the specified model/instancetype
        /// </summary>
        /// <param name="idmodel">id of the model</param>
        /// <param name="instancetype">Instance type</param>
        /// <returns>The suggested NEW aircraft</returns>
        public static Aircraft SuggestTail(int idmodel, AircraftInstanceTypes instancetype)
        {
            MakeModel m = new MakeModel(idmodel);
            Regex r = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled);
            string szTailBase = CountryCodePrefix.szSimPrefix + r.Replace(m.Model, "");
            if (szTailBase.Length > Aircraft.maxTailLength)
                szTailBase = szTailBase.Substring(0, Aircraft.maxTailLength);

            // no aircraft matching this model/instance type was found
            // Try creating one based on the model name
            Aircraft ac = null;
            int i = 0;
            string szTail = "";

            do
            {
                string szNumber = (i == 0) ? string.Empty : i.ToString(CultureInfo.InvariantCulture);
                if (szTailBase.Length + szNumber.Length > Aircraft.maxTailLength)
                    szTail = szTailBase.Substring(0, Aircraft.maxTailLength - szNumber.Length) + szNumber;
                else
                    szTail = szTailBase + szNumber;

                ac = new Aircraft(szTail);
                i++;
            }
            while (ac.AircraftID != Aircraft.idAircraftUnknown);

            ac.TailNumber = szTail;
            ac.ModelID = idmodel;
            ac.InstanceType = instancetype;
            ac.ModelDescription = m.Model;
            return ac;
        }

        /// <summary>
        /// Suggests existing sims, or a new one, to match the specified model/instance type
        /// </summary>
        /// <param name="idmodel">The model</param>
        /// <param name="instancetype">The instance type</param>
        /// <returns></returns>
        public static Collection<Aircraft> SuggestSims(int idmodel, AircraftInstanceTypes instancetype)
        {
            // First see if there is an existing aircraft that is this model/instancetype
            Collection<Aircraft> rgac = Aircraft.GetSims(idmodel, false, instancetype);
            if (rgac.Count > 0)
                return rgac;
            else
            {
                rgac = new Collection<Aircraft>();
                rgac.Add(SuggestTail(idmodel, instancetype));
                return rgac;
            }
        }

        #region Looking up registrations
        /// <summary>
        /// Given a tailnumber, returns a link to the FAA or similar registrar.
        /// </summary>
        /// <param name="szTailNumber">The tailnumber</param>
        public static string LinkForTailnumberRegistry(string szTailNumber)
        {
            CountryCodePrefix cc = CountryCodePrefix.BestMatchCountryCode(szTailNumber);

            switch (cc.RegistrationURLTemplateMode)
            {
                default:
                case CountryCodePrefix.RegistrationTemplateMode.NoSearch:
                    return cc.RegistrationURLTemplate ?? string.Empty;
                case CountryCodePrefix.RegistrationTemplateMode.SuffixOnly:
                    return String.Format(CultureInfo.InvariantCulture,cc.RegistrationURLTemplate, NormalizeTail(szTailNumber).Substring(cc.NormalizedPrefix.Length));
                case CountryCodePrefix.RegistrationTemplateMode.WholeTail:
                    return String.Format(CultureInfo.InvariantCulture,cc.RegistrationURLTemplate, NormalizeTail(szTailNumber));
                case CountryCodePrefix.RegistrationTemplateMode.WholeWithDash:
                    return String.Format(CultureInfo.InvariantCulture, cc.RegistrationURLTemplate, NormalizeTail(szTailNumber, cc));
            }
        }

        public string LinkForTailnumberRegistry()
        {
            return Aircraft.LinkForTailnumberRegistry(TailNumber);
        }
        #endregion
    }

    /// <summary>
    /// A group of aircraft with a title
    /// </summary>
    public class AircraftGroup
    {
        /// <summary>
        /// Indicates how to group the aircraft
        /// </summary>
        public enum GroupMode { All, Activity, CategoryClass, Model, ICAO, Recency }

        #region Properties
        public IEnumerable<Aircraft> MatchingAircraft { get; set; }

        public string GroupTitle { get; set; }
        #endregion

        public AircraftGroup()
        {
            MatchingAircraft = new List<Aircraft>();
            GroupTitle = string.Empty;
        }

        private static string PropertyNameFromMode(GroupMode gm)
        {
            switch (gm)
            {
                default:
                    throw new ArgumentOutOfRangeException("Unknown groupmode: " + gm.ToString());
                case GroupMode.Recency:
                    return "HasBeenFlown";
                case GroupMode.All:
                    return string.Empty;
                case GroupMode.ICAO:
                    return "ICAO";
                case GroupMode.Activity:
                    return "HideFromSelection";
                case GroupMode.Model:
                    return "LongModelDescription";
                case GroupMode.CategoryClass:
                    return "CategoryClassDisplay";
            }
        }

        public static IEnumerable<AircraftGroup> AssignToGroups(IEnumerable<Aircraft> lstSrc, GroupMode gm)
        {
            List<AircraftGroup> lstDst = new List<AircraftGroup>();

            IDictionary<string, IEnumerable<Aircraft>> d = util.GroupByProperty<Aircraft>(lstSrc, PropertyNameFromMode(gm));

            // fix up the headers
            switch (gm)
            {
                default:
                    throw new ArgumentOutOfRangeException("Unknown groupmode: " + gm.ToString());
                case GroupMode.All:
                    lstDst.Add(new AircraftGroup() { GroupTitle = string.Empty, MatchingAircraft = d[string.Empty] });
                    break;
                case GroupMode.Recency:
                    if (d.ContainsKey(true.ToString()))
                    {
                        // HasBeenFlown, so Stats and dates are both present
                        List<Aircraft> lstActive = new List<Aircraft>(d[true.ToString()]);
                        lstActive.Sort((ac1, ac2) =>
                        {
                            if (ac1.Stats.LatestDate.Value.CompareTo(ac2.Stats.LatestDate) == 0)
                                return ac1.TailNumber.CompareCurrentCulture(ac2.TailNumber);
                            else
                                return ac2.Stats.LatestDate.Value.CompareTo(ac1.Stats.LatestDate);
                        });
                        lstDst.Add(new AircraftGroup() { GroupTitle = Resources.Aircraft.AircraftGroupFlown, MatchingAircraft = lstActive });
                    }
                    if (d.ContainsKey(false.ToString()))
                            lstDst.Add(new AircraftGroup() { GroupTitle = Resources.Aircraft.AircraftGroupUnflown, MatchingAircraft = d[false.ToString()] });
                    break;
                case GroupMode.Activity:
                    if (d.ContainsKey(false.ToString()))
                        lstDst.Add(new AircraftGroup() { GroupTitle = Resources.Aircraft.AircraftGroupActive, MatchingAircraft = d[false.ToString()] });
                    if (d.ContainsKey(true.ToString()))
                        lstDst.Add(new AircraftGroup() { GroupTitle = Resources.Aircraft.AircraftGroupInactive, MatchingAircraft = d[true.ToString()] });
                    break;
                case GroupMode.Model:
                case GroupMode.CategoryClass:
                case GroupMode.ICAO:
                    foreach (string key in d.Keys)
                        lstDst.Add(new AircraftGroup() { GroupTitle = key, MatchingAircraft = d[key] });
                    lstDst.Sort((ag1, ag2) => { return ag1.GroupTitle.CompareCurrentCulture(ag2.GroupTitle); });
                    break;
            }

            return lstDst;
        }
    }

    #region Import
    /// <summary>
    /// For importing, this is a placeholder object for a potential aircraft to an aircraft
    /// Note that this does NOT inherit from Aircraft because we set the BestMatchAircraft after the fact.
    /// </summary>
    [Serializable]
    public class AircraftImportMatchRow : IComparable
    {
        public enum MatchState { MatchedExisting, UnMatched, MatchedInProfile, JustAdded }
        static private Regex rNormalize = new Regex("[^a-zA-Z0-9#]*", RegexOptions.Compiled);

        #region Comparable
        public int CompareTo(object obj)
        {
            AircraftImportMatchRow aim = (AircraftImportMatchRow)obj;
            if (State == aim.State)
                return TailNumber.CompareCurrentCultureIgnoreCase(aim.TailNumber);
            else
                return ((int)State) - ((int)aim.State);
        }
        #endregion

        #region Constructors
        public AircraftImportMatchRow()
        {
            TailNumber = ModelGiven = NormalizedModelGiven = string.Empty;
            BestMatchAircraft = null;
            MatchingModels = null;
            SuggestedModel = SpecifiedModel = null;
            State = AircraftImportMatchRow.MatchState.UnMatched;
            ID = -1;
        }

        public AircraftImportMatchRow(string szTail, string modelGiven) : this()
        {
            TailNumber = szTail;
            ModelGiven = modelGiven;
            NormalizedModelGiven = NormalizeModel(modelGiven);
        }
        #endregion

        public static string NormalizeModel(string sz)
        {
            return rNormalize.Replace(sz, string.Empty).ToUpperInvariant();
        }

        /// <summary>
        /// Looks for items that are technically unmatched but now are found in the user's profile - switch them to "JustAdded"
        /// We are looking for anything that is UnMatched but which is in the user's aircraft list; this inconsistency can most easily be explained as "JustAdded"
        /// </summary>
        /// <param name="lstMatches">The current set of matches</param>
        /// <param name="szUser">The username against which to match</param>
        public static void RefreshRecentlyAddedAircraftForUser(IEnumerable<AircraftImportMatchRow> lstMatches, string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException("szUser", "AddAllExistingAircraftForUser - no user specified");
            if (lstMatches == null)
                throw new ArgumentNullException("lstMatches");

            UserAircraft ua = new UserAircraft(szUser);
            Aircraft[] rgac = ua.GetAircraftForUser();
            foreach (AircraftImportMatchRow mr in lstMatches)
            {
                if (mr.State == MatchState.UnMatched)
                {
                    string szNormal = rNormalize.Replace(mr.TailNumber, string.Empty);
                    if (Array.Exists<Aircraft>(rgac, ac => String.Compare(rNormalize.Replace(ac.TailNumber, string.Empty), szNormal, StringComparison.CurrentCultureIgnoreCase) == 0))
                        mr.State = AircraftImportMatchRow.MatchState.JustAdded;
                }
            }
        }

        #region Properties
        /// <summary>
        /// Tailnumber to which we want to match
        /// </summary>
        public string TailNumber { get; set; }

        /// <summary>
        /// The model AS SPECIFIED by the user
        /// </summary>
        public string ModelGiven { get; set; }

        /// <summary>
        /// The normalized model name (i.e., non-alphanumeric chars stripped)
        /// </summary>
        public string NormalizedModelGiven { get; set; }

        /// <summary>
        /// The best match we've found so far, could be null
        /// </summary>
        public Aircraft BestMatchAircraft { get; set; }

        /// <summary>
        /// A set of potential models which match
        /// </summary>
        public Collection<MakeModel> MatchingModels { get; set; }

        /// <summary>
        /// The model that was suggested automatically by the system
        /// </summary>
        public MakeModel SuggestedModel { get; set; }

        /// <summary>
        /// The model that was specified by the user (i.e., overridden)
        /// </summary>
        public MakeModel SpecifiedModel { get; set; }

        /// <summary>
        /// The state of the match - new aircraft, in profile already, or existing?
        /// </summary>
        public MatchState State { get; set; }

        /// <summary>
        /// Unique ID for the AircraftImportMatchRow.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Instance type description that is safe to bind too (checks for null)
        /// </summary>
        public string InstanceTypeDescriptionDisplay
        {
            get { return (BestMatchAircraft == null) ? string.Empty : BestMatchAircraft.InstanceTypeDescription; }
        }

        /// <summary>
        /// Model description that is safe to bind too (checks for null)
        /// </summary>
        public string SpecifiedModelDisplay
        {
            get { return (SpecifiedModel == null) ? string.Empty : SpecifiedModel.DisplayName; }
        }
        #endregion
    }

    [Serializable]
    public class AircraftAdminModelMapping
    {
        #region properties
        public Aircraft aircraft { get; set; }
        public MakeModel currentModel { get; set; }
        public MakeModel targetModel { get; set; }
        #endregion

        public AircraftAdminModelMapping()
        {
            aircraft = null;
            currentModel = targetModel = null;
        }

        // Commits the mapping - NO EMAIL sent, also very efficient on the database (simple update)
        public void CommitChange()
        {
            DBHelper dbh = new DBHelper("UPDATE aircraft SET idmodel=?newmodel WHERE idaircraft=?idac");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("newmodel", targetModel.MakeModelID);
                comm.Parameters.AddWithValue("idac", aircraft.AircraftID);
            });
        }

        public static IEnumerable<AircraftAdminModelMapping> MapModels(Stream s)
        {
            if (s == null)
                throw new ArgumentNullException("s");

            List<AircraftAdminModelMapping> lstMappings = new List<AircraftAdminModelMapping>();

            using (CSVReader reader = new CSVReader(s))
            {
                try
                {
                    int iColAircraftID = -1;
                    int iColTargetModelID = -1;

                    string[] rgCols = reader.GetCSVLine(true);

                    if (rgCols == null)
                        throw new MyFlightbookValidationException("No column headers found.");

                    for (int i = 0; i < rgCols.Length; i++)
                    {
                        string sz = rgCols[i];
                        if (String.Compare(sz, "idaircraft", StringComparison.OrdinalIgnoreCase) == 0)
                            iColAircraftID = i;
                        if (String.Compare(sz, "idModelProper", StringComparison.OrdinalIgnoreCase) == 0)
                            iColTargetModelID = i;
                    }

                    if (iColAircraftID < 0)
                        throw new MyFlightbookValidationException("No \"idaircraft\" column found.");
                    if (iColTargetModelID < 0)
                        throw new MyFlightbookValidationException("No \"idModelProper\" column found.");

                    while ((rgCols = reader.GetCSVLine()) != null)
                    {
                        int idAircraft = Convert.ToInt32(rgCols[iColAircraftID], CultureInfo.InvariantCulture);
                        int idTargetModel = Convert.ToInt32(rgCols[iColTargetModelID], CultureInfo.InvariantCulture);
                        Aircraft ac = new Aircraft(idAircraft);
                        if (ac.AircraftID != Aircraft.idAircraftUnknown && ac.ModelID != idTargetModel)
                        {
                            AircraftAdminModelMapping amm = new AircraftAdminModelMapping();
                            amm.aircraft = ac;
                            amm.currentModel = MakeModel.GetModel(ac.ModelID);
                            amm.targetModel = MakeModel.GetModel(idTargetModel);
                            lstMappings.Add(amm);
                        }
                    }
                }
                catch (CSVReaderInvalidCSVException ex)
                {
                    throw new MyFlightbookException(ex.Message, ex);
                }
            }

            return lstMappings;
        }
    }

    /// <summary>
    /// Results from parsing the CSV file
    /// </summary>
    [Serializable]
    public class AircraftImportParseContext
    {
        #region Properties
        private List<AircraftImportMatchRow> _matchResults = new List<AircraftImportMatchRow>();

        /// <summary>
        /// The AircraftImportMatchRow results from the import
        /// </summary>
        public Collection<AircraftImportMatchRow> MatchResults
        {
            get { return new Collection<AircraftImportMatchRow>(_matchResults); }
        }

        private List<string> _tailsFound = new List<string>();

        /// <summary>
        /// The tails that were found (to avoid duplicates)
        /// </summary>
        private Collection<string> TailsFound
        {
            get { return new Collection<string>(_tailsFound); }
        }

        /// <summary>
        /// User for whom we are importing
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// True if rows were found.
        /// </summary>
        public bool RowsFound { get; set; }

        #region internal - column indices
        private int iColTail = -1;  // column index for tailnumber
        private int iColModel = -1; // column index for model
        private int iColPrivatenotes = -1; // column index for private notes
        private int iColFrequentlyUsed = -1; // column index for frequently used
        private int iColAircraftID = -1; // column index for aircraft ID
        #endregion

        #region Subsets of matches
        /// <summary>
        /// Returns all unmatched aircraft
        /// </summary>
        public Collection<AircraftImportMatchRow> AllUnmatched
        {
            get { return new Collection<AircraftImportMatchRow>(_matchResults.FindAll(mr => mr.State == AircraftImportMatchRow.MatchState.UnMatched)); }
        }

        /// <summary>
        /// All candidates that were not found in the user's profile.
        /// </summary>
        public Collection<AircraftImportMatchRow> AllMissing
        {
            get { return new Collection<AircraftImportMatchRow>(_matchResults.FindAll(mr => mr.State == AircraftImportMatchRow.MatchState.UnMatched || mr.State == AircraftImportMatchRow.MatchState.MatchedExisting)); }
        }
        
        /// <summary>
        /// Returns unmatched or just-added aircraft (I.e., ones to display)
        /// </summary>
        public Collection<AircraftImportMatchRow> UnmatchedOrJustAdded
        {
            get { return new Collection<AircraftImportMatchRow>(_matchResults.FindAll(mr => mr.State == AircraftImportMatchRow.MatchState.UnMatched || mr.State == AircraftImportMatchRow.MatchState.JustAdded)); }
        }

        /// <summary>
        /// Returns all aircraft that are not unmatched (matched to existing aircraft, or to profile, or just added)
        /// </summary>
        public Collection<AircraftImportMatchRow> AllMatched
        {
            get { return new Collection<AircraftImportMatchRow>(_matchResults.FindAll(mr => mr.State != AircraftImportMatchRow.MatchState.UnMatched)); }
        }
        #endregion

        static private Regex rNormalize = new Regex("[^a-zA-Z0-9]*", RegexOptions.Compiled);
        private AircraftInstance[] m_rgAircraftInstances = null;
        #endregion

        #region Initialization/constructors
        /// <summary>
        /// Adds a match candidate to the context, ignoring dupes
        /// </summary>
        /// <param name="szTail">The tailnumber</param>
        /// <param name="szModelGiven">The given model</param>
        /// <param name="fRequireModel">True (default) if a model MUST be provided.</param>
        public AircraftImportMatchRow AddMatchCandidate(string szTail, string szModelGiven, bool fRequireModel = true)
        {
            // Look for missing data.  If both are empty, that's OK - just continue
            if (String.IsNullOrEmpty(szTail) && String.IsNullOrEmpty(szModelGiven))
                return null;

            // But if only one is empty, that's a problem.
            if (String.IsNullOrEmpty(szTail) || (fRequireModel && String.IsNullOrEmpty(szModelGiven)))
                throw new MyFlightbookException(Resources.Aircraft.ImportNotValidCSV);

            string szTailNormal = rNormalize.Replace(szTail, string.Empty);

            /* Ignore this if 
             * a) (sim or anonymous) AND there is already a matchrow containing this tail AND model OR
             * b) NOT sim or anonymous AND tail is already in TailsFound
             */
            if (CountryCodePrefix.IsNakedAnon(szTail) || CountryCodePrefix.IsNakedSim(szTail))
            {
                if (MatchResults.FirstOrDefault<AircraftImportMatchRow>(matchrow => { return matchrow.TailNumber.CompareOrdinalIgnoreCase(szTail) == 0 && matchrow.ModelGiven.CompareOrdinalIgnoreCase(szModelGiven) == 0; }) != null)
                    return null;
            }
            else if (TailsFound.Contains(szTailNormal))
                return null;

            RowsFound = true;

            AircraftImportMatchRow mr = new AircraftImportMatchRow(szTail, szModelGiven);
            MatchResults.Add(mr);
            if (!String.IsNullOrEmpty(szTailNormal))
                TailsFound.Add(szTailNormal);
            return mr;
        }

        private void InitHeaders(string[] rgszRow)
        {
            if (rgszRow == null)
                return;

            if (rgszRow.Length < 2)
                throw new MyFlightbookException(Resources.Aircraft.ImportNotValidCSV);

            for (int i = 0; i < rgszRow.Length; i++)
            {
                if (rgszRow[i].CompareCurrentCultureIgnoreCase(ImportFlights.CSVImporter.TailNumberColumnName) == 0)
                    iColTail = i;
                if (rgszRow[i].CompareCurrentCultureIgnoreCase(ImportFlights.CSVImporter.ModelColumnName) == 0)
                    iColModel = i;
                if (rgszRow[i].CompareCurrentCultureIgnoreCase("Private Notes") == 0)
                    iColPrivatenotes = i;
                if (rgszRow[i].CompareCurrentCultureIgnoreCase("Frequently Used") == 0)
                    iColFrequentlyUsed = i;
                if (rgszRow[i].CompareCurrentCultureIgnoreCase("Aircraft ID") == 0)
                    iColAircraftID = i;
            }

            if (iColTail < 0)
                throw new MyFlightbookException(Resources.Aircraft.ImportNoTailNumberColumnFound);
            if (iColModel < 0)
                throw new MyFlightbookException(Resources.Aircraft.ImportNoModelColumnFound);
        }

        private void InitFromCSV(Stream CSVToParse)
        {
            string[] rgszRow;
            bool fFirstRow = true;  // detect list separator on first read row.

            UserAircraft ua = String.IsNullOrEmpty(Username) ? null : new UserAircraft(Username);

            using (CSVReader csvr = new CSVReader(CSVToParse))
            {
                try
                {
                    InitHeaders(csvr.GetCSVLine(true));
                    while ((rgszRow = csvr.GetCSVLine(fFirstRow)) != null)
                    {
                        string szTail = rgszRow[iColTail].Trim().ToUpper(CultureInfo.CurrentCulture);
                        string szModelGiven = rgszRow[iColModel].Trim();

                        // trim anything after a comma, if necessary
                        int iComma = szModelGiven.IndexOf(",", StringComparison.CurrentCultureIgnoreCase);
                        if (iComma > 0)
                            szModelGiven = szModelGiven.Substring(0, iComma);

                        AircraftImportMatchRow mr = AddMatchCandidate(szTail, szModelGiven);

                        if (mr != null && iColAircraftID >= 0 && ua != null)
                        {
                            int idAircraft = Convert.ToInt32(rgszRow[iColAircraftID], CultureInfo.InvariantCulture);
                            Aircraft ac = ua.GetUserAircraftByID(idAircraft);
                            if (ac != null && Aircraft.NormalizeTail(szTail).CompareCurrentCultureIgnoreCase(Aircraft.NormalizeTail(ac.TailNumber)) == 0)   // double check that the tails match too!
                            {
                                mr.BestMatchAircraft = ac;
                                bool fChanged = false;
                                if (iColFrequentlyUsed >= 0)
                                {
                                    bool newVal = !rgszRow[iColFrequentlyUsed].SafeParseBoolean();  // meaning is inverted - internally it's "hide", externally it's "show" (i.e., frequently used).
                                    fChanged = (newVal != ac.HideFromSelection);
                                    ac.HideFromSelection = newVal;
                                }
                                if (iColPrivatenotes >= 0)
                                {
                                    fChanged = fChanged || ac.PrivateNotes.CompareCurrentCultureIgnoreCase(rgszRow[iColPrivatenotes]) != 0;
                                    ac.PrivateNotes = rgszRow[iColPrivatenotes];
                                }

                                if (fChanged)
                                    ua.FAddAircraftForUser(ac);
                            }
                        }
                    }
                }
                catch (CSVReaderInvalidCSVException ex)
                {
                    throw new MyFlightbookException(ex.Message);
                }
                catch (MyFlightbookException)
                {
                    throw;
                }
            }
        }

        public AircraftImportParseContext()
        {
            RowsFound = false;
            m_rgAircraftInstances = AircraftInstance.GetInstanceTypes();
        }

        /// <summary>
        /// Creates a new AirportImportParseResults object, initializing it from the specified CSV string.
        /// </summary>
        /// <param name="szCSV">String representing the aircraft to import, in CSV format.</param>
        public AircraftImportParseContext(string szCSVToParse, string szUser) : this()
        {
            Username = szUser;
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(szCSVToParse)))
                InitFromCSV(ms);
        }
        #endregion

        public void CleanUpBestMatchAircraft()
        {
            _matchResults.RemoveAll(mr => mr.BestMatchAircraft == null);
        }

        protected void SetModelMatch(AircraftImportMatchRow mr, AircraftImportMatchRow.MatchState ms)
        {
            if (mr == null)
                throw new ArgumentNullException("mr");
            mr.State = ms;
            mr.BestMatchAircraft.InstanceTypeDescription = m_rgAircraftInstances[mr.BestMatchAircraft.InstanceTypeID - 1].DisplayName;
            mr.SpecifiedModel = mr.SuggestedModel = MakeModel.GetModel(mr.BestMatchAircraft.ModelID);
        }

        /// <summary>
        /// After loading up the tailnumber/modelname pairs, this sets up best matches and sets the status for each match
        /// </summary>
        /// <param name="szUser">The user for whom we are doing this.</param>
        public void ProcessParseResultsForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException("szUser", "ProcessParseResultsForUser - no user specified");

            // Now, get a list of user aircraft and of all potential matching aircraft (at most 2 DB hits, rather than 1 per aircraft)
            List<Aircraft> lstUserAircraft = new List<Aircraft>(new UserAircraft(szUser).GetAircraftForUser());
            List<Aircraft> lstAllAircraft = Aircraft.AircraftByTailListQuery(_tailsFound);
            lstAllAircraft.Sort((ac1, ac2) => { return String.Compare(ac1.TailNumber, ac2.TailNumber, StringComparison.OrdinalIgnoreCase) == 0 ? ac1.Version - ac2.Version : String.Compare(ac1.TailNumber, ac2.TailNumber, StringComparison.OrdinalIgnoreCase); });

            List<AircraftImportMatchRow> lstMatchesToDelete = new List<AircraftImportMatchRow>();
            List<AircraftImportMatchRow> lstMatchesToAdd = new List<AircraftImportMatchRow>();

            // Now make a second pass through the list, looking for:
            // a) If it's already in your profile - awesome, easy.
            // b) if it's already in the system - easy, if the model matches
            // c) if it's not in the system - (delayed) best match from the specified model
            foreach (AircraftImportMatchRow mr in MatchResults)
            {
                // check if this aircraft is ALREADY in the user's profile
                mr.BestMatchAircraft = lstUserAircraft.Find(ac => String.Compare(rNormalize.Replace(ac.TailNumber, string.Empty), rNormalize.Replace(mr.TailNumber, string.Empty), StringComparison.CurrentCultureIgnoreCase) == 0);
                if (mr.BestMatchAircraft != null)
                {
                    SetModelMatch(mr, AircraftImportMatchRow.MatchState.MatchedInProfile);
                    continue;
                }

                // If not in the profile, see if it is in the list of ALL aircraft
                List<Aircraft> lstExistingMatches = lstAllAircraft.FindAll(ac => String.Compare(rNormalize.Replace(ac.TailNumber, string.Empty), rNormalize.Replace(mr.TailNumber, string.Empty), StringComparison.OrdinalIgnoreCase) == 0);
                if (lstExistingMatches != null && lstExistingMatches.Count > 0)
                {
                    lstMatchesToDelete.Add(mr);
                    foreach (Aircraft ac in lstExistingMatches)
                    {
                        AircraftImportMatchRow mr2 = new AircraftImportMatchRow(ac.TailNumber, mr.ModelGiven) { BestMatchAircraft = ac };
                        SetModelMatch(mr2, AircraftImportMatchRow.MatchState.MatchedExisting);
                        lstMatchesToAdd.Add(mr2);
                    }
                    continue;
                }

                // No match, make a best guess based on the provided model information
                mr.BestMatchAircraft = new Aircraft();
                mr.BestMatchAircraft.TailNumber = mr.TailNumber;
                mr.BestMatchAircraft.ModelID = MakeModel.UnknownModel;
                mr.BestMatchAircraft.InstanceTypeDescription = m_rgAircraftInstances[0].DisplayName;
            }

            // Now delete the ones that had multiple versions and add the individual versions
            foreach (AircraftImportMatchRow mr in lstMatchesToDelete)
                MatchResults.Remove(mr);
            _matchResults.AddRange(lstMatchesToAdd);

            // Assign each MatchRow a unique ID
            for (int i = 0; i < MatchResults.Count; i++)
                MatchResults[i].ID = i;

            // And sort
            _matchResults.Sort();

            // Finally, assign a reasonable model for each candidate
            Collection<MakeModel> makes = MakeModel.MatchingMakes();
            foreach (AircraftImportMatchRow mr in _matchResults)
            {
                if (mr.State != AircraftImportMatchRow.MatchState.UnMatched)
                    continue;

                if (!String.IsNullOrEmpty(mr.ModelGiven))
                {
                    mr.MatchingModels = MakeModel.MatchingMakes(makes, mr.NormalizedModelGiven);
                    if (mr.MatchingModels.Count > 0)
                    {
                        mr.SuggestedModel = mr.SpecifiedModel = mr.MatchingModels[0];
                        mr.BestMatchAircraft.ModelID = mr.SuggestedModel.MakeModelID;
                    }
                }

                mr.BestMatchAircraft.FixTailAndValidate();
                mr.BestMatchAircraft.InstanceTypeDescription = m_rgAircraftInstances[mr.BestMatchAircraft.InstanceTypeID - 1].DisplayName;
            }
        }

        /// <summary>
        /// Bulk Adds all of the aircraft to import that hit existing aircraft
        /// </summary>
        /// <param name="szUser">The user on whose behalf we are doing this.</param>
        public void AddAllExistingAircraftForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException("szUser", "AddAllExistingAircraftForUser - no user specified");

            UserAircraft ua = new UserAircraft(szUser);
            foreach (AircraftImportMatchRow mr in _matchResults)
            {
                if (mr.State == AircraftImportMatchRow.MatchState.MatchedExisting && mr.BestMatchAircraft.Version == 0) // only take the 0th version when doing a bulk import.  TODO: Is this right?  We show alternatives!
                {
                    ua.FAddAircraftForUser(mr.BestMatchAircraft);
                    mr.State = AircraftImportMatchRow.MatchState.MatchedInProfile;
                }
            }
        }

        /// <summary>
        /// Bulk adds all NEW aircraft (i.e., don't hit existing aircraft) into the user's profile
        /// </summary>
        /// <param name="szUser">The username on whose behalf we are doing this.</param>
        /// <returns>True for success</returns>
        public bool AddAllNewAircraftForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException("szUser", "AddAllNewAircraftForUser - no user specified");

            bool fErrorsFound = false;
            foreach (AircraftImportMatchRow mr in AllUnmatched)
            {
                if (mr.State == AircraftImportMatchRow.MatchState.UnMatched && String.IsNullOrEmpty(mr.BestMatchAircraft.ErrorString))
                {
                    mr.BestMatchAircraft.Commit(szUser);
                    mr.State = AircraftImportMatchRow.MatchState.MatchedInProfile;
                }
                else
                    fErrorsFound = true;
            }

            return !fErrorsFound;
        }
    }
    #endregion

    /// <summary>
    /// Keeps track of deleted aircraft
    /// </summary>
    public class AircraftTombstone
    {
        #region properties
        /// <summary>
        /// The ID of the old aircraft (now deleted)
        /// </summary>
        public int OldAircraftID { get; set; }

        /// <summary>
        /// The ID of the new aircraft
        /// </summary>
        public int NewAircraftID { get; set; }
        #endregion

        #region constructors
        public AircraftTombstone() { }

        /// <summary>
        /// Creates a new tombstone mapping the specified old ID to the new ID
        /// </summary>
        /// <param name="idAircraftOld">The id of the aircraft to tombstone</param>
        /// <param name="idAircraftNew">The id of the new aircraft</param>
        public AircraftTombstone(int idAircraftOld, int idAircraftNew) : this()
        {
            OldAircraftID = idAircraftOld;
            NewAircraftID = idAircraftNew;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="idAircraftOld"></param>
        public AircraftTombstone(int idAircraftOld) : this()
        {
            OldAircraftID = NewAircraftID = idAircraftOld;

            DBHelper dbh = new DBHelper("SELECT * FROM aircrafttombstones WHERE idDeletedAircraft=?idOld");

            dbh.ReadRow(
                (comm) => { comm.Parameters.AddWithValue("idOld", idAircraftOld); },
                (dr) =>
                {
                    OldAircraftID = Convert.ToInt32(dr["idDeletedAircraft"], CultureInfo.InvariantCulture);
                    NewAircraftID = Convert.ToInt32(dr["idMappedAircraft"], CultureInfo.InvariantCulture);
                });
        }
        #endregion

        public bool IsValid()
        {
            return OldAircraftID != NewAircraftID && OldAircraftID > 0 && NewAircraftID > 0;
        }

        /// <summary>
        /// Maps the specified aircraft ID to a new one, if necessary
        /// </summary>
        /// <param name="idAircraft">The aircraft that could be mapped</param>
        /// <returns>The mapped ID, or the original if it's fine.</returns>
        public static int MapAircraftID(int idAircraft)
        {
            AircraftTombstone act = new AircraftTombstone(idAircraft);
            return act.IsValid() ? act.NewAircraftID : idAircraft;
        }

        /// <summary>
        /// Save the tombstone to the db.  ALWAYS DOES AN INSERT, WILL FAIL if called a second time.
        /// </summary>
        public void Commit()
        {
            if (!IsValid())
                return;

            AircraftTombstone act = new AircraftTombstone(OldAircraftID);
            if (act.IsValid())
                return;

            new DBHelper().DoNonQuery("INSERT INTO aircrafttombstones SET idDeletedAircraft=?idOld, idMappedAircraft=?idNew",
                (comm) =>
                {
                    comm.Parameters.AddWithValue("idOld", OldAircraftID);
                    comm.Parameters.AddWithValue("idNew", NewAircraftID);
                });
        }
    }
}