using AircraftSupport.Properties;
using MyFlightbook.Image;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2009-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
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

    public static class MakeModelExtension
    {
        /// <summary>
        /// Determines if the specified turbinelevel is turbine (vs. piston or electric)
        /// </summary>
        /// <param name="tl"></param>
        /// <returns></returns>
        public static bool IsTurbine(this MakeModel.TurbineLevel tl) { return tl == MakeModel.TurbineLevel.Jet || tl == MakeModel.TurbineLevel.UnspecifiedTurbine || tl == MakeModel.TurbineLevel.TurboProp; }
    }

    /// <summary>
    /// Makes/models of airplanes
    /// </summary>
    [Serializable]
    public class MakeModel
    {
        public const int UnknownModel = -1;
        private string szSampleAircraftIDs = string.Empty;
        static readonly LazyRegex rModelNormalize = new LazyRegex("[^a-zA-Z0-9 ]*");
        static readonly LazyRegex rModelNormalizeNoSpace = new LazyRegex("[^a-zA-Z0-9]*");

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
                    return $"{ManufacturerDisplay} {ModelDisplayName}".Trim();
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
                        TypeName.Length > 0 ? String.Format(CultureInfo.CurrentCulture, AircraftResources.DisplayTemplateWithType, Model, TypeName) : Model,
                        ModelName.Length > 0 ? String.Format(CultureInfo.CurrentCulture, " \"{0}\"", ModelName) : string.Empty).Trim();
                else
                    return string.Empty;
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public string ICAODisplay
        {
            get { return String.IsNullOrEmpty(FamilyName) ? string.Empty : String.Format(CultureInfo.CurrentCulture, AircraftResources.ICAOTemplate, FamilyName); }
        }

        public string EASAClassificationForCatClass(CategoryClass.CatClassID categoryClass)
        {
            switch (categoryClass)
            {
                case CategoryClass.CatClassID.ASEL:
                    return EngineType.IsTurbine() ? AircraftResources.EASACategoryClassASELT : AircraftResources.EASACategoryClassASELP;
                case CategoryClass.CatClassID.ASES:
                    return EngineType.IsTurbine() ? AircraftResources.EASACategoryClassASEST : AircraftResources.EASACategoryClassASESP;
                case CategoryClass.CatClassID.AMEL:
                    return EngineType.IsTurbine() ? AircraftResources.EASACategoryClassAMELT : AircraftResources.EASACategoryClassAMELP;
                case CategoryClass.CatClassID.AMES:
                    return EngineType.IsTurbine() ? AircraftResources.EASACategoryClassAMEST : AircraftResources.EASACategoryClassAMESP;
                case CategoryClass.CatClassID.Glider:
                    return IsMotorGlider ? AircraftResources.EASACategoryClassMotorGlider : AircraftResources.EASACategoryClassGlider;
                case CategoryClass.CatClassID.PoweredLift:
                    return AircraftResources.EASACategoryClassPoweredLift;
                default:
                    return null;
            }
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

        /// <summary>
        /// Directly returns sample images from the database.  More efficient than calling SampleAircraft because it will work across multiple models at once.
        /// The dictionary is keyed on model ID and returns an enumerable (up to limit) of MFBImageInfo's
        /// </summary>
        /// <param name="models"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public static IDictionary<int, IList<MFBImageInfo>> SampleImagesForModels(IEnumerable<MakeModel> models, int limit = 10)
        {
            Dictionary<int, IList<MFBImageInfo>> dict = new Dictionary<int, IList<MFBImageInfo>>();
            if (!(models?.Any() ?? false))
                return dict;

            List<int> modelIDs = new List<int>();
            foreach (MakeModel m in models)
                modelIDs.Add(m.MakeModelID);

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"SELECT 
    m.idmodel, i.*
FROM
    images i
        INNER JOIN
    aircraft ac ON i.ImageKey = ac.idaircraft
        INNER JOIN
    models m ON ac.idmodel = m.idmodel
WHERE
    i.VirtPathID = 1
        AND m.idmodel IN ({0})
ORDER BY model ASC;", string.Join(",", modelIDs)));

            dbh.ReadRows(
                (comm) => { },
                (dr) =>
                {
                    int modelID = Convert.ToInt32(dr["idmodel"], CultureInfo.InvariantCulture);
                    if (dict.TryGetValue(modelID, out IList<MFBImageInfo> samples))
                    {
                        if (samples.Count < limit)
                            samples.Add(MFBImageInfo.ImageFromDBRow(dr));
                    }
                    else
                        dict[modelID] = new List<MFBImageInfo> { MFBImageInfo.ImageFromDBRow(dr) };
                });
            return dict;
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
        public string ManufacturerDisplay { get; set; } = string.Empty;

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
        [Required]
        public string Model { get; set; }

        /// <summary>
        /// Friendly name for the model (e.g., "Skyhawk")
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// Type name, if any (e.g., "777-200ER")
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// Family name - e.g., "PA28" or "C-172"
        /// </summary>
        public string FamilyName { get; set; } = string.Empty;

        public string FamilyDisplay
        {
            get { return String.IsNullOrEmpty(FamilyName) ? Model : FamilyName; }
        }

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
            MakeModelID = UnknownModel;
            CategoryClassID = CategoryClass.CatClassID.ASEL;
            ArmyMDS = FamilyName = ModelName = FamilyName = TypeName = string.Empty;
            Is200HP = IsAllGlass = IsAllTAA = IsCertifiedSinglePilot = IsComplex = IsComplex = IsConstantProp = IsHighPerf = IsMotorGlider = IsMultiEngineHelicopter = IsRetract = IsTailWheel = false;
        }

        protected const string szSQLSelectTemplate = @"SELECT models.*, categoryclass.CatClass as 'Category/Class', manufacturers.manufacturer, '' AS AircraftIDs
FROM models 
INNER JOIN categoryclass ON categoryclass.idCatClass = models.idcategoryclass 
INNER JOIN manufacturers ON models.idManufacturer=manufacturers.idManufacturer 
{0}
WHERE {1}";

        // Create a new make/model object, initialize by ID
        public MakeModel(int id) : this()
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szSQLSelectTemplate, string.Empty, String.Format(CultureInfo.InvariantCulture, "models.idmodel={0} LIMIT 1", id)));
            dbh.ReadRow((comm) => { }, (dr) => { InitFromDataReader(dr); });
            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Error loading make {0}: {1}", id, dbh.LastError));
        }

        public MakeModel(MySqlDataReader dr) : this()
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
                if (ManufacturerID <= 0)
                    throw new MyFlightbookValidationException(AircraftResources.errManufacturerRequired);

                if (String.IsNullOrEmpty(Model))
                    throw new MyFlightbookValidationException(AircraftResources.editMakeValModelNameRequired);

                // Category class and manufacturer will throw exceptions if there is an issue.
                if (CategoryClass.CategoryClassFromID(CategoryClassID) == null || new Manufacturer(ManufacturerID) == null)
                    return false;

                if (ArmyMDS.Length > 40)
                    throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, AircraftResources.errMDSTooLong, ArmyMDS));

                if (IsCertifiedSinglePilot && (!EngineType.IsTurbine() || String.IsNullOrEmpty(TypeName)))
                    throw new MyFlightbookValidationException(AircraftResources.errSinglePilotButNotTypeRated);

                FamilyName = FamilyName.ToUpperInvariant();
                if (!RegexUtility.ICAO.IsMatch(FamilyName))
                    throw new MyFlightbookValidationException(AircraftResources.errInvalidICAO);

                if (TypeName.CompareCurrentCultureIgnoreCase("yes") == 0)
                    throw new MyFlightbookValidationException(AircraftResources.errYesNotValidType);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                ErrorString = ex.Message;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Commits the the model to the database.
        /// </summary>
        /// <param name="szUser">The user who is making the edit</param>
        /// <param name="fAsAdmin">True if we should perform this as an admin</param>
        /// <param name="onCommit">Lambda to be called after the edit</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void CommitForUser(string szUser, bool fAsAdmin, Action<MakeModel> onCommit)
        {
            if (string.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            MakeModel mmExisting = null;

            // If this is a new make, inherit the manufacturer's sim-only status.
            if (IsNew) // creation event
                AllowedTypes = (new Manufacturer(ManufacturerID)).AllowedTypes;
            else
            {
                mmExisting = new MakeModel(MakeModelID);
                // otherwise, if the user isn't an admin, restore the existing allowed types
                if (!fAsAdmin)
                    AllowedTypes = mmExisting.AllowedTypes;
            }

            if (!FIsValid())
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, AircraftResources.errSaveMakeFailed, ErrorString));

            // Inherit the allowed aircraft types from the manufacturer if this is new or if it otherwise is unrestricted.
            if (IsNew || AllowedTypes == AllowedAircraftTypes.Any)
                AllowedTypes = new Manufacturer(ManufacturerID).AllowedTypes;

            string szQ = String.Format(CultureInfo.InvariantCulture, "{0} models SET model = ?Model, modelname = ?modelName, typename = ?typeName, family=?familyname, idcategoryclass = ?idCatClass, idmanufacturer = ?idManufacturer, " +
                        "fcomplex = ?IsComplex, fHighPerf = ?IsHighPerf, f200HP=?Is200hp, fTailwheel = ?IsTailWheel, fTurbine=?engineType, fGlassOnly=?IsGlassOnly, fTAA=?IsTaa, fRetract=?IsRetract, fCowlFlaps=?HasFlaps, " +
                        "fConstantProp=?IsConstantProp, ArmyMissionDesignSeries=?armyMDS, fSimOnly=?simOnly, fMotorGlider=?motorglider, fMultiHelicopter=?multiHeli, fCertifiedSinglePilot=?singlePilot {1}",
                        IsNew ? "INSERT INTO" : "UPDATE",
                        IsNew ? string.Empty : String.Format(CultureInfo.InvariantCulture, "WHERE idModel = {0}", MakeModelID)
                        );

            DBHelper dbh = new DBHelper(szQ);
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("Model", Model.LimitTo(44));
                comm.Parameters.AddWithValue("modelName", ModelName.LimitTo(44));
                comm.Parameters.AddWithValue("typeName", TypeName.LimitTo(44));
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
                comm.Parameters.AddWithValue("armyMDS", ArmyMDS.LimitTo(44));
                comm.Parameters.AddWithValue("simOnly", (int)AllowedTypes);
                comm.Parameters.AddWithValue("motorglider", IsMotorGlider);
                comm.Parameters.AddWithValue("multiHeli", IsMultiEngineHelicopter);
                comm.Parameters.AddWithValue("singlePilot", IsCertifiedSinglePilot);
            });
            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, AircraftResources.errSaveMakeFailed, szQ + "\r\n" + dbh.LastError));

            if (MakeModelID == -1)
                MakeModelID = dbh.LastInsertedRowId;

            // Fix up display, if needed (e.g., for newly added or edited model)
            if (String.IsNullOrEmpty(ManufacturerDisplay))
                ManufacturerDisplay = Manufacturer.CachedManufacturers().First(man => man.ManufacturerID == ManufacturerID).ManufacturerName;
            if (String.IsNullOrEmpty(CategoryClassDisplay))
                CategoryClassDisplay = CategoryClass.CategoryClassFromID(CategoryClassID).CatClass;

            if (MakeModelID != -1)
                util.GlobalCache.Set(CacheKey(MakeModelID), this, DateTimeOffset.UtcNow.AddHours(1));

            onCommit?.Invoke(this);

        }

        private void InitFromDataReader(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
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

            util.GlobalCache.Set(CacheKey(this.MakeModelID), this, DateTimeOffset.UtcNow.AddHours(2));
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

        /// <summary>
        /// Returns the number of models in the system.
        /// </summary>
        /// <returns></returns>
        public static int ModelCount()
        {
            const string szCacheKeyModelCount = "modelCountKey";
            object o = util.GlobalCache.Get(szCacheKeyModelCount);
            int cModels = (o == null) ? 0 : (int) o;
            if (cModels == 0)
            {
                DBHelper dbh = new DBHelper("SELECT count(*) AS numModels FROM models");
                dbh.ReadRow((comm) => { }, (dr) => { cModels = Convert.ToInt32(dr["numModels"], CultureInfo.InvariantCulture); });
                util.GlobalCache.Set(szCacheKeyModelCount, cModels, DateTimeOffset.UtcNow.AddHours(2));
            }
            return cModels;
        }

        private const string szCacheKeyModels = "keyAllModelsByManufacturer";

        public static IDictionary<int, List<MakeModel>> ModelsByManufacturer(bool fIncludeGeneric = false)
        {
            Dictionary<int, List<MakeModel>> d = (Dictionary<int, List<MakeModel>>)util.GlobalCache.Get(szCacheKeyModels);
            if (d == null)
            {
                d = new Dictionary<int, List<MakeModel>>();
                Collection<MakeModel> allModels = MatchingMakes();

                foreach (MakeModel m in allModels)
                {
                    // skip any sim/generic-only types
                    if (!fIncludeGeneric && m.AllowedTypes != AllowedAircraftTypes.Any)
                        continue;

                    if (!d.ContainsKey(m.ManufacturerID))
                        d[m.ManufacturerID] = new List<MakeModel>();
                    d[m.ManufacturerID].Add(m);
                }

                util.GlobalCache.Set(szCacheKeyModels, d, DateTimeOffset.UtcNow.AddMinutes(30));
            }
            return d;
        }

        private string m_szSearchNormal;
        private string NormalSearchString
        {
            get
            {
                m_szSearchNormal = m_szSearchNormal ?? String.Format(CultureInfo.CurrentCulture, "{0} {1} {2} {3} {4}",
                        rModelNormalizeNoSpace.Replace(Model, string.Empty),
                        FamilyName,
                        rModelNormalizeNoSpace.Replace(ManufacturerDisplay, string.Empty),
                        rModelNormalizeNoSpace.Replace(ModelName, string.Empty),
                        rModelNormalizeNoSpace.Replace(TypeName, string.Empty)).ToUpperInvariant();
                return m_szSearchNormal;
            }
        }

        private string m_szModelNormal;
        private string NormalModel
        {
            get
            {
                m_szModelNormal = m_szModelNormal ?? rModelNormalizeNoSpace.Replace(Model, string.Empty).ToUpperInvariant();
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
                throw new ArgumentNullException(nameof(rgMakes));
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
            ModelQuery mq = new ModelQuery() { ManufacturerID = idManufacturer, SortMode = ModelQuery.ModelSortMode.ModelName, SortDir = SortDirection.Ascending };
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
                throw new ArgumentNullException(nameof(mm));
            string szCompare = rModelNormalize.Replace(mm.DisplayName, "").ToUpper(CultureInfo.CurrentCulture);

            // for now, let's see how this works if you just see if the manufacturer name is present AND the (modelname OR model) is present
            string szManufacturerNormal = rModelNormalize.Replace(ManufacturerDisplay, string.Empty).ToUpper(CultureInfo.CurrentCulture);
            string szModelNameNormal = rModelNormalize.Replace(ModelName, string.Empty).ToUpper(CultureInfo.CurrentCulture);
            string szModelNormal = rModelNormalize.Replace(Model, string.Empty).ToUpper(CultureInfo.CurrentCulture);

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
            return (MakeModel)util.GlobalCache.Get(CacheKey(id));
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
        /// <param name="rgmodelid">An enumerable set of make/model IDs</param>
        /// <returns>A sorted (by model) enumerable list of matching models</returns>
        public static IEnumerable<MakeModel> ModelsForAircraftIds(IEnumerable<int> rgmodelid)
        {
            if (rgmodelid == null)
                throw new ArgumentNullException(nameof(rgmodelid));
            Dictionary<int, MakeModel> dModels = new Dictionary<int, MakeModel>();
            List<int> lstIdsToGet = new List<int>();
            foreach (int mmid in rgmodelid)
                if (!dModels.ContainsKey(mmid))
                {
                    MakeModel m = CachedModel(mmid);
                    dModels[mmid] = m ?? new MakeModel(); // if null, put in a placeholder so that we know we've seen it; we'll fill it in below.
                    if (m == null)
                        lstIdsToGet.Add(mmid);
                }

            // Now get each of the models that didn't hit the cache
            if (lstIdsToGet.Count > 0)
            {
                DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szSQLSelectTemplate, string.Empty, String.Format(CultureInfo.InvariantCulture, "models.idmodel IN ({0})", String.Join(",", lstIdsToGet))));
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
            lst.Sort((mm1, mm2) => { return mm1.DisplayName.CompareCurrentCulture(mm2.DisplayName); });
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
                    lstAttributes.Add(AircraftResources.IsJet);
                    break;
                case MakeModel.TurbineLevel.TurboProp:
                    lstAttributes.Add(AircraftResources.IsTurboprop);
                    break;
                case MakeModel.TurbineLevel.UnspecifiedTurbine:
                    lstAttributes.Add(AircraftResources.IsTurbine);
                    break;
                case MakeModel.TurbineLevel.Electric:
                    lstAttributes.Add(AircraftResources.IsElectric);
                    break;
                default:
                    break;
            }
            if (IsTailWheel) lstAttributes.Add(AircraftResources.IsTailwheel);
            if (IsHighPerf) lstAttributes.Add(AircraftResources.IsHighPerf);
            if (Is200HP) lstAttributes.Add(AircraftResources.IsLegacyHighPerf);
            if (IsMotorGlider) lstAttributes.Add(AircraftResources.IsTMG);
            if (IsMultiEngineHelicopter) lstAttributes.Add(AircraftResources.IsMultiHelicopter);

            AvionicsTechnologyType avionicsTechnologyType = (AvionicsTechnologyType)Math.Max((int)AvionicsTechnology, (int)upgradeType);
            switch (avionicsTechnologyType)
            {
                case AvionicsTechnologyType.None:
                    break;
                case AvionicsTechnologyType.Glass:
                    lstAttributes.Add(upgradeDate.HasValue ? String.Format(CultureInfo.CurrentCulture, "{0} ({1})", AircraftResources.IsGlass, upgradeDate.Value.ToShortDateString()) : AircraftResources.IsGlass);
                    break;
                case AvionicsTechnologyType.TAA:
                    lstAttributes.Add(upgradeDate.HasValue ? String.Format(CultureInfo.CurrentCulture, "{0} ({1})", AircraftResources.IsTAA, upgradeDate.Value.ToShortDateString()) : AircraftResources.IsTAA);
                    break;
            }
            if (IsCertifiedSinglePilot) lstAttributes.Add(AircraftResources.IsCertifiedSinglePilot);
            if (IsComplex)
                lstAttributes.Add(AircraftResources.IsComplex);
            else
            {
                if (IsRetract)
                    lstAttributes.Add(AircraftResources.IsRetract);
                if (IsConstantProp)
                    lstAttributes.Add(AircraftResources.IsConstantProp);
                if (HasFlaps)
                    lstAttributes.Add(AircraftResources.HasFlaps);
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
                return !col.Any() ? string.Empty : String.Format(CultureInfo.CurrentCulture, "({0})", String.Join(", ", col));
            }
        }
    }
}