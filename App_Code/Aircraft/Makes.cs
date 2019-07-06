using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using MyFlightbook.Image;

/******************************************************
 * 
 * Copyright (c) 2009-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// A very simple version of make model for sending to mobile devices - just a display name + ID.  Other than the ID, unrelated to MakeModel
    /// </summary>
    [Serializable]
    public class SimpleMakeModel
    {
        /// <summary>
        /// The ID of the model
        /// </summary>
        public int ModelID { get; set; }

        /// <summary>
        /// The human-readable description of the model
        /// </summary>
        public string Description { get; set; }

        public SimpleMakeModel()
        {
            ModelID = -1;
            Description = string.Empty;
        }

        public static SimpleMakeModel[] GetAllMakeModels()
        {
            ArrayList al = new ArrayList();
            DBHelper dbh = new DBHelper(ConfigurationManager.AppSettings["MakesAndModels"].ToString());
            if (!dbh.ReadRows(
                (comm) => { },
                (dr) =>
                {
                    SimpleMakeModel smm = new SimpleMakeModel() { Description = dr["MakeName"].ToString(), ModelID = Convert.ToInt32(dr["idmodel"], CultureInfo.InvariantCulture) };
                    al.Add(smm);
                }))
                throw new MyFlightbookException("Error in GetAllMakeModels: " + dbh.LastError);

            return (SimpleMakeModel[])al.ToArray(typeof(SimpleMakeModel));
        }
    }

    /// <summary>
    /// Stats for the user in this model
    /// </summary>
    [Serializable]
    public class MakeModelStats
    {
        #region properties
        /// <summary>
        /// Number of flights for the user.
        /// </summary>
        public int NumFlights { get; set; }

        /// <summary>
        /// First flight in this model?
        /// </summary>
        public DateTime? EarliestFlight { get; set; }

        /// <summary>
        /// Last flight in this model?
        /// </summary>
        public DateTime? LatestFlight { get; set; }
        #endregion

        #region Constructors
        public MakeModelStats()
        {
            NumFlights = 0;
            EarliestFlight = LatestFlight = null;
        }
        #endregion
    }

    /// <summary>
    /// Makes/models of airplanes
    /// </summary>
    [Serializable]
    public class MakeModel
    {
        public const int UnknownModel = -1;
        private string szSampleAircraftIDs = "";
        static readonly Regex rNormalize = new Regex("[^a-zA-Z0-9 ]*", RegexOptions.Compiled);
        static readonly Regex rNormalizeNoSpace = new Regex("[^a-zA-Z0-9]*", RegexOptions.Compiled);

        /// <summary>
        /// For turbine aircraft, indicates the type of turbine
        /// </summary>
        public enum TurbineLevel { Piston = 0, TurboProp = 1, Jet = 2, UnspecifiedTurbine = 3, Electric = 4 };

        /// <summary>
        /// The performance type for the make.  High Performance, 200hp, or not High Performance
        /// </summary>
        public enum HighPerfType { NotHighPerf = 0, HighPerf, Is200HP };

        /// <summary>
        /// Technology level for the model (or aircraft)
        /// </summary>
        public enum AvionicsTechnologyType {
            /// <summary>
            /// No specific technology (e.g., instances of this model may have steam gauges)
            /// </summary>
            None,
            /// <summary>
            /// Glass (PFD replacing 6-pack)
            /// </summary>
            Glass,
            /// <summary>
            /// Per 61.129(j) - PFD + MFD (with GPS/Moving map) + integrated 2-axis autopilot.
            /// </summary>
            TAA
        }

        public const string Date200hpHighPerformanceCutoverDate = "1997-08-04";

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} {1} {2} {3} {4}{5}{6}{7}{8}{9}{10}{11}{12}{13}",
                this.DisplayName,
                this.ICAODisplay,
                String.IsNullOrEmpty(this.ArmyMDS) ? string.Empty : "MDS: " + ArmyMDS,
                this.EngineType.ToString(),
                this.IsCertifiedSinglePilot ? "Single Pilot " : string.Empty,
                this.IsAllGlass ? "All Glass " : string.Empty,
                this.IsAllTAA ? "TAA " : string.Empty,
                this.IsComplex ? "Complex " : String.Format(CultureInfo.CurrentCulture, "{0} {1} {2}", this.IsConstantProp ? "Constant Prop " : string.Empty, this.HasFlaps ? "Flaps " : string.Empty, this.IsRetract ? "Retract " : string.Empty),
                this.IsHighPerf ? "High perf " : (this.Is200HP ? "200hp " : string.Empty),
                this.IsMotorGlider ? "Motor glider " : string.Empty,
                this.IsTailWheel ? "Tailwheel " : string.Empty,
                this.IsMotorGlider ? "Motorglider " : string.Empty,
                this.IsMultiEngineHelicopter ? "Multi-engine Helicopter " : string.Empty,
                this.AllowedTypes == AllowedAircraftTypes.Any ? string.Empty : this.AllowedTypes.ToString()).Trim();
        }

        #region Properties
        /// <summary>
        /// Full display name, including manufacturer.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string DisplayName
        {
            get
            {
                if (!String.IsNullOrEmpty(ManufacturerDisplay))
                    return String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, ManufacturerDisplay, ModelDisplayName).Trim();
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Display name of the model only (Display name minus manufacturer)
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string ModelDisplayName
        {
            get
            {
                if (!String.IsNullOrEmpty(ManufacturerDisplay) && TypeName != null && ModelName != null && CategoryClassDisplay != null)
                    return String.Format(CultureInfo.CurrentCulture, "{0} - {1}", ModelDisplayNameNoCatclass, CategoryClassDisplay).Trim();
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Basic display name of the model (Display name minus manufacturer and category class)
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string ModelDisplayNameNoCatclass
        {
            get
            {
                if (!String.IsNullOrEmpty(ManufacturerDisplay) && TypeName != null && ModelName != null && CategoryClassDisplay != null)
                    return String.Format(CultureInfo.CurrentCulture, "{0}{1}",
                        TypeName.Length > 0 ? String.Format(CultureInfo.CurrentCulture, Resources.Makes.DisplayTemplateWithType, Model, TypeName) : Model,
                        ModelName.Length > 0 ? String.Format(CultureInfo.CurrentCulture, " \"{0}\"", ModelName) : string.Empty).Trim();
                else
                    return string.Empty;
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public string ICAODisplay
        {
            get { return String.IsNullOrEmpty(FamilyName) ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Makes.ICAOTemplate, FamilyName); }
        }

        /// <summary>
        /// ID's of sample aircraft which that are of this make (read-only)
        /// This is generally the result of a group_concat when retrieving multiple models at once;
        /// the aircraft can then be scanned to find sample images.
        /// Use SampleImages to directly query the database for images for a single aircraft
        /// </summary>
        public int[] SampleAircraft()
        {
            if (szSampleAircraftIDs.Length == 0)
                return new int[0];

            string[] rgSz = szSampleAircraftIDs.Split(',');
            // discard the last 2, which could be bogus (if sample list got truncated)

            int cSamples = rgSz.Length;
            if (cSamples > 2) // can't be cut off if this short.
                cSamples = rgSz.Length - 2;

            int[] rgIds = new int[cSamples];
            for (int i = cSamples - 1; i >= 0; i--)
                rgIds[i] = Convert.ToInt32(rgSz[i], CultureInfo.InvariantCulture);
            return rgIds;
        }

        /// <summary>
        /// Directly returns sample images from the database.  This is more efficient than SampleAircraft, but only for a single model at a time.
        /// </summary>
        /// <param name="limit">Maximum number of images to return; 10 by default</param>
        /// <returns></returns>
        public IEnumerable<MFBImageInfo> SampleImages(int limit = 10)
        {
            List<MFBImageInfo> lst = new List<MFBImageInfo>();
            DBHelper dbh = new DBHelper(@"SELECT 
                                            i.*
                                        FROM
                                            images i
                                                INNER JOIN
                                            aircraft ac ON i.ImageKey = ac.idaircraft
                                        WHERE
                                            i.VirtPathID = 1 AND ac.idmodel = ?modelid
                                        LIMIT ?limit;");
            dbh.ReadRows(
                (comm) =>
                    {
                        comm.Parameters.AddWithValue("modelid", MakeModelID);
                        comm.Parameters.AddWithValue("limit", limit);
                    },
                (dr) => { lst.Add(MFBImageInfo.ImageFromDBRow(dr)); });
            return lst;
        }

        public MakeModelStats StatsForUser(string szUser)
        {
            MakeModelStats mms = new MakeModelStats();

            if (String.IsNullOrWhiteSpace(szUser))
                return mms;

            DBHelper dbh = new DBHelper(@"SELECT 
                    COUNT(idflight) AS numflights,
                    MIN(date) AS EarliestDate,
                    MAX(date) AS LatestDate
                FROM
                    flights f
                        INNER JOIN
                    aircraft ac ON f.idaircraft = ac.idaircraft
                WHERE
                    ac.idmodel = ?modelid AND username = ?user;");
            dbh.ReadRow((comm) =>
            {
                comm.Parameters.AddWithValue("user", szUser);
                comm.Parameters.AddWithValue("modelid", MakeModelID);
            },
                (dr) =>
                {
                    mms.NumFlights = Convert.ToInt32(dr["numflights"], CultureInfo.InvariantCulture);
                    mms.EarliestFlight = (DateTime?)util.ReadNullableField(dr, "EarliestDate", null);
                    mms.LatestFlight = (DateTime?)util.ReadNullableField(dr, "LatestDate", null);
                });
            return mms;
        }

        /// <summary>
        /// What sorts of aircraft can be of this model?
        /// </summary>
        public AllowedAircraftTypes AllowedTypes { get; set; }

        /// <summary>
        /// Display name of Category Class (read-only; use CategoryClassID for read/write)
        /// </summary>
        public string CategoryClassDisplay { get; set; }

        /// <summary>
        /// Display name for manufacturer (read-only; use ManufacturerID for read/write)
        /// </summary>
        public string ManufacturerDisplay { get; set; }

        /// <summary>
        /// Are all aircraft in this make/model glass cockpit?
        /// </summary>
        private Boolean IsAllGlass { get; set; }

        /// <summary>
        /// Is this a TAA per 61.129(j) (as of Aug 27, 2018)?
        /// Requires: (a) Glass PFD, (b) Glass MFD with GPS, and (c) autopilot capable of at least 2 axis.
        /// </summary>
        private Boolean IsAllTAA { get; set; }

        /// <summary>
        /// Minimum technology level for the avionics for this model
        /// </summary>
        public AvionicsTechnologyType AvionicsTechnology
        {
            get { return IsAllTAA && IsAllGlass ? AvionicsTechnologyType.TAA : (IsAllGlass ? AvionicsTechnologyType.Glass : AvionicsTechnologyType.None); }
            set
            {
                switch (value)
                {
                    case AvionicsTechnologyType.None:
                        IsAllGlass = IsAllTAA = false;
                        break;
                    case AvionicsTechnologyType.Glass:
                        IsAllGlass = true;
                        IsAllTAA = false;
                        break;
                    case AvionicsTechnologyType.TAA:
                        IsAllGlass = IsAllTAA = true;
                        break;
                }
            }
        }

        /// <summary>
        /// The Mission/design/series for this make
        /// </summary>
        public string ArmyMDS { get; set; }

        /// <summary>
        /// Error string for last operation
        /// </summary>
        public string ErrorString { get; set; }

        /// <summary>
        /// The ID for this make/model
        /// </summary>
        public int MakeModelID { get; set; }

        /// <summary>
        /// The model (e.g., "C-172")
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Friendly name for the model (e.g., "Skyhawk")
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Type name, if any (e.g., "777-200ER")
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Family name - e.g., "PA28" or "C-172"
        /// </summary>
        public string FamilyName { get; set; }

        /// <summary>
        /// ID of the category/class
        /// </summary>
        public CategoryClass.CatClassID CategoryClassID { get; set; }

        /// <summary>
        /// ID of the manufacturer
        /// </summary>
        public int ManufacturerID { get; set; }

        /// <summary>
        /// Is this airplane complex?
        /// </summary>
        public Boolean IsComplex { get; set; }

        /// <summary>
        /// Is this high-performance?
        /// </summary>
        public Boolean IsHighPerf { get; set; }

        /// <summary>
        /// Is this 200HP (was high performance until Aug 4 1997)
        /// </summary>
        public Boolean Is200HP { get; set; }

        /// <summary>
        /// The performance type for the model
        /// </summary>
        public HighPerfType PerformanceType
        {
            get
            {
                if (IsHighPerf)
                    return HighPerfType.HighPerf;
                else if (Is200HP)
                    return HighPerfType.Is200HP;
                else
                    return HighPerfType.NotHighPerf;
            }
            set
            {
                switch (value)
                {
                    case HighPerfType.NotHighPerf:
                        Is200HP = IsHighPerf = false;
                        break;
                    case HighPerfType.Is200HP:
                        Is200HP = true;
                        IsHighPerf = false;
                        break;
                    case HighPerfType.HighPerf:
                        Is200HP = false;
                        IsHighPerf = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Tailwheel airplane or tricycle?
        /// </summary>
        public Boolean IsTailWheel { get; set; }

        /// <summary>
        /// Constrant speed prop?
        /// </summary>
        public Boolean IsConstantProp { get; set; }

        /// <summary>
        /// Has flaps?
        /// </summary>
        public Boolean HasFlaps { get; set; }

        /// <summary>
        /// Has retractable gear?
        /// </summary>
        public Boolean IsRetract { get; set; }

        /// <summary>
        /// Level of turbine
        /// </summary>
        public TurbineLevel EngineType { get; set; }

        /// <summary>
        /// For type-rated turbine aircraft, indicates that this is certified for single-engine operations
        /// </summary>
        public bool IsCertifiedSinglePilot { get; set; }

        /// <summary>
        /// Is this a motorglider (TMG)?
        /// </summary>
        public Boolean IsMotorGlider { get; set; }

        /// <summary>
        /// For helicopters, is this multi-engine?
        /// </summary>
        public Boolean IsMultiEngineHelicopter { get; set; }

        /// <summary>
        /// Is this a new model or an existing one?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsNew
        {
            get { return MakeModelID == MakeModel.UnknownModel; }
        }
        #endregion

        /// <summary>
        /// Key for caching this model
        /// </summary>
        static private string CacheKey(int ID)
        {
            return "MakeModel" + ID.ToString(CultureInfo.InvariantCulture);
        }

        #region Object Creation
        /// <summary>
        /// Create a new make/model object
        /// </summary>
        public MakeModel()
        {
            MakeModelID = MakeModel.UnknownModel;
            CategoryClassID = CategoryClass.CatClassID.ASEL;
        }

        private const string szSQLSelectTemplate = @"SELECT models.*, categoryclass.CatClass as 'Category/Class', manufacturers.manufacturer 
FROM models 
INNER JOIN categoryclass ON categoryclass.idCatClass = models.idcategoryclass 
INNER JOIN manufacturers ON models.idManufacturer=manufacturers.idManufacturer WHERE {0}";

        // Create a new make/model object, initialize by ID
        public MakeModel(int id) : this()
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szSQLSelectTemplate, String.Format(CultureInfo.InvariantCulture, "models.idmodel={0} LIMIT 1", id)));
            dbh.ReadRow((comm) => { }, (dr) => { InitFromDataReader(dr); });
            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Error loading make {0}: {1}", id, dbh.LastError));
        }

        private MakeModel(MySqlDataReader dr) : this()
        {
            InitFromDataReader(dr);
        }
        #endregion

        /// <summary>
        /// Validates the integrity of the make/model object
        /// </summary>
        /// <returns></returns>
        public Boolean FIsValid()
        {
            try
            {
                CategoryClass cc = CategoryClass.CategoryClassFromID(CategoryClassID);
                Manufacturer m = new Manufacturer(ManufacturerID);
                if (ArmyMDS.Length > 40)
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Makes.errMDSTooLong, ArmyMDS));

                if (IsCertifiedSinglePilot && (!EngineType.IsTurbine() || String.IsNullOrEmpty(TypeName)))
                    throw new MyFlightbookException(Resources.Makes.errSinglePilotButNotTypeRated);
            }
            catch (MyFlightbookException ex)
            {
                ErrorString = ex.Message;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Saves the make/model to the DB, creating it if necessary
        /// </summary>
        public void Commit()
        {
            if (!FIsValid())
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Makes.errSaveMakeFailed, ErrorString));

            // Inherit the allowed aircraft types from the manufacturer if this is new or if it otherwise is unrestricted.
            if (IsNew || AllowedTypes == AllowedAircraftTypes.Any)
                AllowedTypes = new Manufacturer(ManufacturerID).AllowedTypes;

            string szQ = String.Format(CultureInfo.InvariantCulture, "{0} models SET model = ?Model, modelname = ?modelName, typename = ?typeName, family=?familyname, idcategoryclass = ?idCatClass, idmanufacturer = ?idManufacturer, " +
                        "fcomplex = ?IsComplex, fHighPerf = ?IsHighPerf, f200HP=?Is200hp, fTailwheel = ?IsTailWheel, fTurbine=?engineType, fGlassOnly=?IsGlassOnly, fTAA=?IsTaa, fRetract=?IsRetract, fCowlFlaps=?HasFlaps, " +
                        "fConstantProp=?IsConstantProp, ArmyMissionDesignSeries=?armyMDS, fSimOnly=?simOnly, fMotorGlider=?motorglider, fMultiHelicopter=?multiHeli, fCertifiedSinglePilot=?singlePilot {1}",
                        IsNew ? "INSERT INTO" : "UPDATE",
                        IsNew ? String.Empty : String.Format(CultureInfo.InvariantCulture, "WHERE idModel = {0}", MakeModelID)
                        );

            DBHelper dbh = new DBHelper(szQ);
            dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("Model", Model);
                    comm.Parameters.AddWithValue("modelName", ModelName);
                    comm.Parameters.AddWithValue("typeName", TypeName);
                    comm.Parameters.AddWithValue("familyname", FamilyName);
                    comm.Parameters.AddWithValue("idCatClass", (int)CategoryClassID);
                    comm.Parameters.AddWithValue("idManufacturer", ManufacturerID);
                    comm.Parameters.AddWithValue("IsComplex", IsComplex);
                    comm.Parameters.AddWithValue("IsHighPerf", IsHighPerf);
                    comm.Parameters.AddWithValue("Is200hp", Is200HP);
                    comm.Parameters.AddWithValue("IsTailWheel", IsTailWheel);
                    comm.Parameters.AddWithValue("engineType", EngineType);
                    comm.Parameters.AddWithValue("IsGlassOnly", IsAllGlass);
                    comm.Parameters.AddWithValue("IsTaa", IsAllTAA);
                    comm.Parameters.AddWithValue("IsRetract", IsRetract);
                    comm.Parameters.AddWithValue("HasFlaps", HasFlaps);
                    comm.Parameters.AddWithValue("IsConstantProp", IsConstantProp);
                    comm.Parameters.AddWithValue("armyMDS", ArmyMDS);
                    comm.Parameters.AddWithValue("simOnly", (int)AllowedTypes);
                    comm.Parameters.AddWithValue("motorglider", IsMotorGlider);
                    comm.Parameters.AddWithValue("multiHeli", IsMultiEngineHelicopter);
                    comm.Parameters.AddWithValue("singlePilot", IsCertifiedSinglePilot);
                });
            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, Resources.Makes.errSaveMakeFailed, szQ + "\r\n" + dbh.LastError));

            if (MakeModelID == -1)
                MakeModelID = dbh.LastInsertedRowId;

            if (MakeModelID != -1)
                HttpRuntime.Cache[CacheKey(this.MakeModelID)] = this;
        }

        private void InitFromDataReader(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            Model = dr["model"].ToString();
            ModelName = dr["modelname"].ToString();
            TypeName = dr["typename"].ToString();
            FamilyName = util.ReadNullableField(dr, "family", "").ToString();
            CategoryClassID = (CategoryClass.CatClassID)Convert.ToInt32(dr["idcategoryclass"], CultureInfo.InvariantCulture);
            ManufacturerID = Convert.ToInt32(dr["idmanufacturer"], CultureInfo.InvariantCulture);
            IsComplex = Convert.ToBoolean(dr["fcomplex"], CultureInfo.InvariantCulture);
            IsHighPerf = Convert.ToBoolean(dr["fHighPerf"], CultureInfo.InvariantCulture);
            Is200HP = Convert.ToBoolean(dr["f200HP"], CultureInfo.InvariantCulture);
            IsTailWheel = Convert.ToBoolean(dr["fTailwheel"], CultureInfo.InvariantCulture);
            IsConstantProp = Convert.ToBoolean(dr["fConstantProp"], CultureInfo.InvariantCulture);
            HasFlaps = Convert.ToBoolean(dr["fCowlFlaps"], CultureInfo.InvariantCulture);
            IsRetract = Convert.ToBoolean(dr["fRetract"], CultureInfo.InvariantCulture);
            IsAllGlass = Convert.ToBoolean(dr["fGlassOnly"], CultureInfo.InvariantCulture);
            IsAllTAA = Convert.ToBoolean(dr["fTAA"], CultureInfo.InvariantCulture);
            EngineType = (TurbineLevel)Convert.ToInt32(dr["fTurbine"], CultureInfo.InvariantCulture);
            MakeModelID = Convert.ToInt32(dr["idmodel"], CultureInfo.InvariantCulture);
            ArmyMDS = util.ReadNullableField(dr, "ArmyMissionDesignSeries", "").ToString();
            CategoryClassDisplay = util.ReadNullableField(dr, "Category/Class", "").ToString();
            ManufacturerDisplay = util.ReadNullableField(dr, "manufacturer", "").ToString();
            szSampleAircraftIDs = util.ReadNullableField(dr, "AircraftIDs", "").ToString();
            AllowedTypes = (AllowedAircraftTypes)Convert.ToInt32(dr["fSimOnly"], CultureInfo.InvariantCulture);
            IsMotorGlider = Convert.ToBoolean(dr["fMotorGlider"], CultureInfo.InvariantCulture);
            IsMultiEngineHelicopter = Convert.ToBoolean(dr["fMultiHelicopter"], CultureInfo.InvariantCulture);
            IsCertifiedSinglePilot = Convert.ToBoolean(dr["fCertifiedSinglePilot"], CultureInfo.InvariantCulture);

            HttpRuntime.Cache.Add(CacheKey(this.MakeModelID), this, null, DateTime.Now.AddHours(2), System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Low, null);
        }

        #region Find/enumerate Models
        public static Collection<MakeModel> MatchingMakes(ModelQuery mq = null)
        {
            List<MakeModel> lst = new List<MakeModel>();
            DBHelper dbh = new DBHelper((mq ?? new ModelQuery()).ModelQueryCommand());
            if (!dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new MakeModel(dr)); }))
                throw new MyFlightbookException("Error loading matching makes: " + dbh.LastError);

            return new Collection<MakeModel>(lst);
        }

        private string m_szSearchNormal = null;
        private string NormalSearchString
        {
            get
            {
                m_szSearchNormal = m_szSearchNormal ?? String.Format(CultureInfo.CurrentCulture, "{0} {1} {2} {3} {4}",
                        rNormalizeNoSpace.Replace(Model, string.Empty),
                        FamilyName,
                        rNormalizeNoSpace.Replace(ManufacturerDisplay, string.Empty),
                        rNormalizeNoSpace.Replace(ModelName, string.Empty),
                        rNormalizeNoSpace.Replace(TypeName, string.Empty)).ToUpperInvariant();
                return m_szSearchNormal;
            }
        }

        private string m_szModelNormal = null;
        private string NormalModel
        {
            get
            {
                m_szModelNormal = m_szModelNormal ?? rNormalizeNoSpace.Replace(Model, string.Empty).ToUpperInvariant();
                return m_szModelNormal;
            }
        }

        /// <summary>
        /// Given an array of makes, finds the subset that seem to match the modelname presented
        /// </summary>
        /// <param name="rgMakes">The input array of models</param>
        /// <param name="szModelName">The string (e.g., "C-172") for the modelname</param>
        /// <returns>A list of potential matches</returns>
        public static Collection<MakeModel> MatchingMakes(IEnumerable<MakeModel> rgMakes, string szModelName)
        {
            if (rgMakes == null)
                throw new ArgumentNullException("rgMakes");
            List<MakeModel> lst = new List<MakeModel>();

            foreach (MakeModel mm in rgMakes)
            {
                if (mm.NormalSearchString.Contains(szModelName))
                    lst.Add(mm);
            }
            lst.Sort((mm1, mm2) =>
                {
                    // Primary sort: allowed types (sim only, etc.)
                    if (mm1.AllowedTypes != mm2.AllowedTypes)
                        return (int)mm1.AllowedTypes - (int)mm2.AllowedTypes;

                    // Secondary - category/class WITHIN A CATEGORY.  E.g., prefer ASEL over ASES, for example, within airplane.
                    CategoryClass cc1 = CategoryClass.CategoryClassFromID(mm1.CategoryClassID);
                    CategoryClass cc2 = CategoryClass.CategoryClassFromID(mm2.CategoryClassID);
                    if (cc1.Category.CompareOrdinalIgnoreCase(cc2.Category) == 0 && mm1.CategoryClassID != mm2.CategoryClassID)
                        return (int)mm1.CategoryClassID - (int)mm2.CategoryClassID;

                    // prefer a match in the model name over a match in another part
                    bool fM1MatchesModel = mm1.NormalModel.Contains(szModelName);
                    bool fM2MatchesModel = mm2.NormalModel.Contains(szModelName);

                    if (fM1MatchesModel && !fM2MatchesModel)
                        return -1;
                    else if (!fM1MatchesModel && fM2MatchesModel)
                        return 1;
                    else if (fM1MatchesModel && fM2MatchesModel)    // both match - pick the shorter one since it's likely the more generic match (e.g., "C-172" vs. "C-172N", or R-22 vs. SR22)
                        return mm1.NormalModel.Length - mm2.NormalModel.Length;
                    else
                        return String.Compare(mm1.DisplayName, mm2.DisplayName, StringComparison.CurrentCultureIgnoreCase); // fall through to alphabetic search.
                });
            return new Collection<MakeModel>(lst);
        }

        /// <summary>
        /// Returns a list of the models for a specified manufacturer
        /// </summary>
        /// <param name="idManufacturer">The ID of the manufacturer</param>
        /// <returns>A list of MakeModel objects</returns>
        public static Collection<MakeModel> MatchingMakes(int idManufacturer)
        {
            ModelQuery mq = new ModelQuery() { ManufacturerID = idManufacturer, SortMode = ModelQuery.ModelSortMode.ModelName, SortDir = ModelQuery.ModelSortDirection.Ascending };
            return new Collection<MakeModel>(MatchingMakes(mq));
        }

        /// <summary>
        /// Checks to see if this possibly matches another make/model
        /// </summary>
        /// <param name="mm">The make/model to which it should be compared</param>
        /// <returns>True if it is a potential match</returns>
        public Boolean IsPossibleMatch(MakeModel mm)
        {
            if (mm == null)
                throw new ArgumentNullException("mm");
            string szCompare = rNormalize.Replace(mm.DisplayName, "").ToUpper(CultureInfo.CurrentCulture);

            // for now, let's see how this works if you just see if the manufacturer name is present AND the (modelname OR model) is present
            string szManufacturerNormal = rNormalize.Replace(ManufacturerDisplay, "").ToUpper(CultureInfo.CurrentCulture);
            string szModelNameNormal = rNormalize.Replace(ModelName, "").ToUpper(CultureInfo.CurrentCulture);
            string szModelNormal = rNormalize.Replace(Model, "").ToUpper(CultureInfo.CurrentCulture);

            Boolean fMatchManufacturer = szCompare.Contains(szManufacturerNormal);
            Boolean fMatchModel = (szModelNormal.Length > 0 && szCompare.Contains(szModelNormal));
            Boolean fMatchModelName = (szModelNameNormal.Length > 0 && szCompare.Contains(szModelNameNormal));
            Boolean fIsPossibleMatch = fMatchManufacturer && (fMatchModel || fMatchModelName);
            return fIsPossibleMatch;
        }

        /// <summary>
        /// Returns an array of candidates that could match the specified make/model
        /// </summary>
        /// <param name="mmProposed">The proposed make/model that could match existing make/models</param>
        /// <returns>An array of possible matches</returns>
        public MakeModel[] PossibleMatches()
        {
            List<MakeModel> al = new List<MakeModel>();

            foreach (MakeModel mm in MatchingMakes())
            {
                if (IsPossibleMatch(mm))
                    al.Add(mm);
            }

            return al.ToArray();
        }
        #endregion

        private static MakeModel CachedModel(int id)
        {
            return (MakeModel)HttpRuntime.Cache[CacheKey(id)];
        }

        /// <summary>
        /// Loads/returns the specified model, hitting the cache if available.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static MakeModel GetModel(int id)
        {
            return CachedModel(id) ?? new MakeModel(id);
        }

        /// <summary>
        /// Returns a set of models for the specified aircraft, drawing from the cache if possible else doing a SINGLE database query
        /// </summary>
        /// <param name="rgac">An enumerable set of aircraft</param>
        /// <returns>A sorted (by model) enumerable list of matching models</returns>
        public static IEnumerable<MakeModel> ModelsForAircraft(IEnumerable<Aircraft> rgac)
        {
            if (rgac == null)
                throw new ArgumentNullException("rgac");
            Dictionary<int, MakeModel> dModels = new Dictionary<int, MakeModel>();
            List<int> lstIdsToGet = new List<int>();
            foreach (Aircraft ac in rgac)
                if (!dModels.ContainsKey(ac.ModelID))
                {
                    MakeModel m = CachedModel(ac.ModelID);
                    dModels[ac.ModelID] = m ?? new MakeModel(); // if null, put in a placeholder so that we know we've seen it; we'll fill it in below.
                    if (m == null)
                        lstIdsToGet.Add(ac.ModelID);
                }

            // Now get each of the models that didn't hit the cache
            if (lstIdsToGet.Count > 0)
            {
                DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szSQLSelectTemplate, String.Format(CultureInfo.InvariantCulture, "models.idmodel IN ({0})", String.Join(",", lstIdsToGet))));
                dbh.ReadRows((comm) => { }, (dr) =>
                {
                    MakeModel mm = new MakeModel(dr);
                    dModels[mm.MakeModelID] = mm;   // overwrite the placeholder from above
                });
                if (dbh.LastError.Length > 0)
                    throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Error loading makes: {0}", dbh.LastError));
            }

            List<MakeModel> lst = new List<MakeModel>();
            foreach (int mkey in dModels.Keys)
                lst.Add(dModels[mkey]);
            lst.Sort((mm1, mm2) => { return mm1.Model.CompareCurrentCulture(mm2.Model); });
            return lst;
        }

        /// <summary>
        /// A list of human-readable attributes for this model
        /// </summary>
        public IEnumerable<string> AttributeList(AvionicsTechnologyType upgradeType = AvionicsTechnologyType.None, DateTime? upgradeDate = null)
        {
            // Get the model attributes
            List<string> lstAttributes = new List<string>();
            switch (EngineType)
            {
                case MakeModel.TurbineLevel.Jet:
                    lstAttributes.Add(Resources.Makes.IsJet);
                    break;
                case MakeModel.TurbineLevel.TurboProp:
                    lstAttributes.Add(Resources.Makes.IsTurboprop);
                    break;
                case MakeModel.TurbineLevel.UnspecifiedTurbine:
                    lstAttributes.Add(Resources.Makes.IsTurbine);
                    break;
                case MakeModel.TurbineLevel.Electric:
                    lstAttributes.Add(Resources.Makes.IsElectric);
                    break;
                default:
                    break;
            }
            if (IsTailWheel) lstAttributes.Add(Resources.Makes.IsTailwheel);
            if (IsHighPerf) lstAttributes.Add(Resources.Makes.IsHighPerf);
            if (Is200HP) lstAttributes.Add(Resources.Makes.IsLegacyHighPerf);
            if (IsMotorGlider) lstAttributes.Add(Resources.Makes.IsTMG);
            if (IsMultiEngineHelicopter) lstAttributes.Add(Resources.Makes.IsMultiHelicopter);

            AvionicsTechnologyType avionicsTechnologyType = (AvionicsTechnologyType)Math.Max((int)AvionicsTechnology, (int)upgradeType);
            switch (avionicsTechnologyType)
            {
                case AvionicsTechnologyType.None:
                    break;
                case AvionicsTechnologyType.Glass:
                    lstAttributes.Add(upgradeDate.HasValue ? String.Format(CultureInfo.CurrentCulture, "{0} ({1})", Resources.Makes.IsGlass, upgradeDate.Value.ToShortDateString()) : Resources.Makes.IsGlass);
                    break;
                case AvionicsTechnologyType.TAA:
                    lstAttributes.Add(upgradeDate.HasValue ? String.Format(CultureInfo.CurrentCulture, "{0} ({1})", Resources.Makes.IsTAA, upgradeDate.Value.ToShortDateString()) : Resources.Makes.IsTAA);
                    break;
            }
            if (IsCertifiedSinglePilot) lstAttributes.Add(Resources.Makes.IsCertifiedSinglePilot);
            if (IsComplex)
                lstAttributes.Add(Resources.Makes.IsComplex);
            else
            {
                if (IsRetract)
                    lstAttributes.Add(Resources.Makes.IsRetract);
                if (IsConstantProp)
                    lstAttributes.Add(Resources.Makes.IsConstantProp);
                if (HasFlaps)
                    lstAttributes.Add(Resources.Makes.HasFlaps);
            }

            return new ReadOnlyCollection<string>(lstAttributes);
        }

        /// <summary>
        /// Return the attribute list in a comma separated list, with parenthesis
        /// </summary>
        public string AttributeListSingleLine
        {
            get
            {
                IEnumerable<string> col = AttributeList();
                return col.Count() == 0 ? string.Empty : String.Format(CultureInfo.CurrentCulture, "({0})", String.Join(", ", col));
            }
        }
    }
    
    /// <summary>
    /// Structured search for a model
    /// </summary>
    [Serializable]
    public class ModelQuery
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ModelSortMode { ModelName, CatClass, Manufacturer };

        [JsonConverter(typeof(StringEnumConverter))]
        public enum ModelSortDirection { Ascending, Descending };

        static readonly Regex rNormalizeModel = new Regex("[- ]+", RegexOptions.Compiled);

        /// <summary>
        /// Prefix to use for modelname to force to Family.
        /// </summary>
        public const string ICAOPrefix = "ICAO:";

        #region properties
        /// <summary>
        /// Text to find anywhere in the search string
        /// </summary>
        [System.ComponentModel.DefaultValue("")]
        public string FullText { get; set; }

        /// <summary>
        /// Text to find in the manufacturer name
        /// </summary>
        [System.ComponentModel.DefaultValue("")]
        public string ManufacturerName { get; set; }

        /// <summary>
        /// Manufacturer ID (i.e., deterministic)
        /// </summary>
        [System.ComponentModel.DefaultValue(-1)]
        public int ManufacturerID { get; set; }

        /// <summary>
        /// Text to find in the model (e.g., "C-172")
        /// </summary>
        [System.ComponentModel.DefaultValue("")]
        public string Model { get; set; }

        /// <summary>
        /// Text to find in the model marketing name (e.g., "Skyhawk")
        /// </summary>
        [System.ComponentModel.DefaultValue("")]
        public string ModelName { get; set; }

        /// <summary>
        /// Text to find in the category/class
        /// </summary>
        [System.ComponentModel.DefaultValue("")]
        public string CatClass { get; set; }

        /// <summary>
        /// Text to find in the type name.
        /// </summary>
        [System.ComponentModel.DefaultValue("")]
        public string TypeName { get; set; }

        /// <summary>
        /// Maximum # of results to return; -1 for no limit
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// # of results to skip, -1 for no skip
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// True to get images.  Images are a bit slower, so this is off by default.
        /// </summary>
        public bool IncludeSampleImages { get; set; }

        /// <summary>
        /// On which field should the results be sorted?
        /// </summary>
        [System.ComponentModel.DefaultValue(ModelSortMode.ModelName)]
        public ModelSortMode SortMode { get; set; }

        /// <summary>
        /// Should the sort be ascending or descending?
        /// </summary>
        [System.ComponentModel.DefaultValue(ModelSortDirection.Ascending)]
        public ModelSortDirection SortDir { get; set; }

        /// <summary>
        /// Should we give a boost to models where the model itself matches?
        /// </summary>
        public bool PreferModelNameMatch { get; set; }
        #endregion

        #region Query utilities
        /// <summary>
        /// The MySqlCommand initialized for this query, including any and all parameters.
        /// </summary>
        public DBHelperCommandArgs ModelQueryCommand()
        {
            List<string> lstWhereTerms = new List<string>();
            List<MySqlParameter> lstParams = new List<MySqlParameter>();

            // Add each of the terms
            string[] rgTerms = FullText.Replace("-", string.Empty).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < rgTerms.Length; i++)
                AddQueryTerm(rgTerms[i], String.Format(CultureInfo.InvariantCulture, "FullText{0}", i), "REPLACE(Concat(model, ' ', manufacturers.manufacturer, ' ', typename, ' ', family, ' ', modelname, ' ', categoryclass.CatClass), '-', '')", lstWhereTerms, lstParams);

            string szPreferred = "0";
            if (PreferModelNameMatch)
            {
                string szModelMatch = String.Format(CultureInfo.InvariantCulture, "%{0}%", ConvertWildcards(FullText));
                szPreferred = "IF(model LIKE ?modelMatch, 1, 0)";
                lstParams.Add(new MySqlParameter("modelMatch", szModelMatch));
            }

            AddQueryTerm(CatClass, "qCatClass", "catclass", lstWhereTerms, lstParams);
            AddQueryTerm(rNormalizeModel.Replace(Model, string.Empty), "qModel", "REPLACE(REPLACE(model, ' ', ''), '-', '')", lstWhereTerms, lstParams);
            if (ModelName.StartsWith(ICAOPrefix, StringComparison.CurrentCultureIgnoreCase))
                AddQueryTerm(ModelName.Substring(ICAOPrefix.Length), "qFamilyName", "family", lstWhereTerms, lstParams);
            else
                AddQueryTerm(ModelName, "qModelName", "modelname", lstWhereTerms, lstParams);
            AddQueryTerm(ManufacturerName, "qMan", "manufacturer", lstWhereTerms, lstParams);
            AddQueryTerm(TypeName, "qType", "typename", lstWhereTerms, lstParams);

            if (ManufacturerID != Manufacturer.UnsavedID)
            {
                lstWhereTerms.Add(" (models.idManufacturer = ?manID) ");
                lstParams.Add(new MySqlParameter("manID", ManufacturerID));
            }

            string szHavingPredicate = String.Join(" AND ", lstWhereTerms.ToArray());

            const string szQTemplate = @"SELECT
models.*,
manufacturers.manufacturer,
categoryclass.CatClass as 'Category/Class',
{0} AS AircraftIDs,
{1} AS preferred
FROM models
  INNER JOIN manufacturers on manufacturers.idManufacturer = models.idmanufacturer
  INNER JOIN categoryclass on categoryclass.idCatClass = models.idcategoryclass
{2}
{3}
{4}
{5}";
            const string szQSamplesTemplate = @"LEFT OUTER JOIN (SELECT ac.idmodel, group_concat(DISTINCT img.imageKey separator ',') AS AircraftIDs
                    FROM Images img
                    INNER JOIN aircraft ac ON CAST(img.imageKey AS Unsigned)=ac.idaircraft
                    WHERE img.VirtPathID=1
                    GROUP BY ac.idmodel) Samples
       ON models.idmodel=Samples.idmodel";

            string szQ = String.Format(CultureInfo.InvariantCulture, szQTemplate,
                IncludeSampleImages ? "Samples.AircraftIDs" : "NULL",
                szPreferred,
                IncludeSampleImages ? szQSamplesTemplate : string.Empty,
                szHavingPredicate.Length == 0 ? string.Empty : String.Format(CultureInfo.InvariantCulture, " WHERE {0} ", szHavingPredicate),
                SortOrderFromSortModeAndDirection(SortMode, SortDir),
                (Limit > 0 && Skip >= 0) ? String.Format(CultureInfo.InvariantCulture, " LIMIT {0},{1} ", Skip, Limit) : string.Empty);

            DBHelperCommandArgs args = new DBHelperCommandArgs(szQ, lstParams);
            return args;
        }

        /// <summary>
        /// Converts "%" and "_" to "*" and "?"
        /// </summary>
        /// <param name="sz"></param>
        /// <returns></returns>
        private string ConvertWildcards(string sz)
        {
            return sz.Replace("%", "\\%").Replace("_", "\\_").Replace("*", "%").Replace("?", "_");
        }

        /// <summary>
        /// Adds a query term
        /// </summary>
        /// <param name="szQ">The search text</param>
        /// <param name="szParamName">Parameter name for this search term</param>
        /// <param name="szMatchField">The DB query field against which to match</param>
        /// <param name="lstTerms">The list of WHERE terms to which to add</param>
        /// <param name="lstParams">The list of MySQLParameters to which to add</param>
        private void AddQueryTerm(string szQ, string szParamName, string szMatchField, List<string> lstTerms, List<MySqlParameter> lstParams)
        {
            if (String.IsNullOrEmpty(szQ))
                return;

            lstTerms.Add(String.Format(CultureInfo.InvariantCulture, " ({0} LIKE ?{1}) ", szMatchField, szParamName));
            lstParams.Add(new MySqlParameter(szParamName, String.Format(CultureInfo.InvariantCulture, "%{0}%", ConvertWildcards(szQ))));
        }

        private static string SortOrderFromSortModeAndDirection(ModelSortMode sortmode, ModelSortDirection sortDirection)
        {
            string szOrderString;
            string szDirString = (sortDirection == ModelSortDirection.Ascending) ? "ASC" : "DESC";

            switch (sortmode)
            {
                case ModelSortMode.CatClass:
                    szOrderString = String.Format(CultureInfo.InvariantCulture, "categoryclass.CatClass {0}, manufacturer {0}, model {0}", szDirString);
                    break;
                case ModelSortMode.Manufacturer:
                    szOrderString = String.Format(CultureInfo.InvariantCulture, "manufacturer {0}, model {0}", szDirString);
                    break;
                default:
                case ModelSortMode.ModelName:
                    szOrderString = String.Format(CultureInfo.InvariantCulture, "Model {0}, modelname {0}, typename {0}", szDirString);
                    break;
            }
            return String.Format(CultureInfo.InvariantCulture, " ORDER BY Preferred DESC, {0}", szOrderString);
        }
        #endregion

        public ModelQuery()
        {
            FullText = ManufacturerName = Model = ModelName = CatClass = TypeName = string.Empty;
            Limit = -1;
            Skip = -1;
            ManufacturerID = Manufacturer.UnsavedID;
            SortMode = ModelSortMode.Manufacturer;
            SortDir = ModelSortDirection.Ascending;
            IncludeSampleImages = false;
        }
    }

    public class MakeSelectedEventArgs : EventArgs
    {
        public int SelectedModel { get; set; }

        public MakeSelectedEventArgs() : base()
        {
            SelectedModel = MakeModel.UnknownModel;
        }

        public MakeSelectedEventArgs(int idModel) : base()
        {
            SelectedModel = idModel;
        }
    }
}

