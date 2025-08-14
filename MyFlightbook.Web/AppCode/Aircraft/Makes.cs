﻿using MyFlightbook.Image;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Caching;

/******************************************************
 * 
 * Copyright (c) 2009-2025 MyFlightbook LLC
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
            List<SimpleMakeModel> al = new List<SimpleMakeModel>();
            DBHelper dbh = new DBHelper(ConfigurationManager.AppSettings["MakesAndModels"]);
            if (!dbh.ReadRows(
                (comm) => { },
                (dr) =>
                {
                    SimpleMakeModel smm = new SimpleMakeModel() { Description = dr["MakeName"].ToString(), ModelID = Convert.ToInt32(dr["idmodel"], CultureInfo.InvariantCulture) };
                    al.Add(smm);
                }))
                throw new MyFlightbookException("Error in GetAllMakeModels: " + dbh.LastError);

            return al.ToArray();
        }


        /// <summary>
        /// Returns a very simple list of models that are similar to the specified model.  ONLY WORKS FOR SIM-ONLY MODELS
        /// </summary>
        /// <param name="idModel"></param>
        /// <returns></returns>
        public static IEnumerable<SimpleMakeModel> ADMINModelsSimilarToSIM(int idModel)
        {
            List<SimpleMakeModel> lst = new List<SimpleMakeModel>();
            DBHelper dbh = new DBHelper(@"SELECT 
    m1.idmodel,
    m1.model,
    m1.typename,
    m1.family,
    man.manufacturer
FROM
    models m1
        INNER JOIN
    manufacturers man on m1.idmanufacturer = man.idmanufacturer
        LEFT JOIN
    models m2 on m2.idmodel = ?targetID
WHERE
    m1.idmodel <> m2.idmodel
		AND m1.family=m2.family
        AND m1.fSimOnly = 1
        AND m1.idcategoryclass = m2.idcategoryclass
        AND m1.fComplex = m2.fComplex
        AND m1.fhighperf = m2.fHighPerf
        AND m1.f200HP = m2.f200HP
        AND m1.fTailwheel = m2.fTailwheel
        AND m1.fConstantProp = m2.fConstantProp
        AND m1.fturbine = m2.fturbine
        AND m1.fretract = m2.fretract
        AND m1.fcertifiedsinglepilot = m2.fcertifiedsinglepilot
        AND m1.fcowlflaps = m2.fCowlFlaps
        AND m1.armymissiondesignseries = m2.armymissiondesignseries
        AND m1.fTAA = m2.fTAA 
        AND m1.fMotorGlider = m2.fMotorGlider
        AND m1.fMultiHelicopter = m2.fMultiHelicopter
ORDER BY man.manufacturer ASC, m1.model ASC");

            dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("targetID", idModel); },
                (dr) => {
                    lst.Add(new SimpleMakeModel() { Description = $"{dr["manufacturer"]} - {dr["model"]}/{dr["family"]} Type: {dr["typename"]}", ModelID = Convert.ToInt32(dr["idmodel"], CultureInfo.InvariantCulture) });
            });
            return lst;
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

        public string EASAClassificationForCatClass(CategoryClass.CatClassID categoryClass)
        {
            switch (categoryClass)
            {
                case CategoryClass.CatClassID.ASEL:
                    return EngineType.IsTurbine() ? Resources.Aircraft.EASACategoryClassASELT : Resources.Aircraft.EASACategoryClassASELP;
                case CategoryClass.CatClassID.ASES:
                    return EngineType.IsTurbine() ? Resources.Aircraft.EASACategoryClassASEST : Resources.Aircraft.EASACategoryClassASESP;
                case CategoryClass.CatClassID.AMEL:
                    return EngineType.IsTurbine() ? Resources.Aircraft.EASACategoryClassAMELT : Resources.Aircraft.EASACategoryClassAMELP;
                case CategoryClass.CatClassID.AMES:
                    return EngineType.IsTurbine() ? Resources.Aircraft.EASACategoryClassAMEST : Resources.Aircraft.EASACategoryClassAMESP;
                case CategoryClass.CatClassID.Glider:
                    return IsMotorGlider ? Resources.Aircraft.EASACategoryClassMotorGlider : Resources.Aircraft.EASACategoryClassGlider;
                case CategoryClass.CatClassID.PoweredLift:
                    return Resources.Aircraft.EASACategoryClassPoweredLift;
                default:
                    return null;
            }
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
                return Array.Empty<int>();

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
                    throw new MyFlightbookValidationException(Resources.Makes.errManufacturerRequired);

                if (String.IsNullOrEmpty(Model))
                    throw new MyFlightbookValidationException(Resources.Makes.editMakeValModelNameRequired);

                // Category class and manufacturer will throw exceptions if there is an issue.
                if (CategoryClass.CategoryClassFromID(CategoryClassID) == null || new Manufacturer(ManufacturerID) == null)
                    return false;

                if (ArmyMDS.Length > 40)
                    throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.Makes.errMDSTooLong, ArmyMDS));

                if (IsCertifiedSinglePilot && (!EngineType.IsTurbine() || String.IsNullOrEmpty(TypeName)))
                    throw new MyFlightbookValidationException(Resources.Makes.errSinglePilotButNotTypeRated);

                FamilyName = FamilyName.ToUpperInvariant();
                if (!RegexUtility.ICAO.IsMatch(FamilyName))
                    throw new MyFlightbookValidationException(Resources.Makes.errInvalidICAO);

                if (TypeName.CompareCurrentCultureIgnoreCase("yes") == 0)
                    throw new MyFlightbookValidationException(Resources.Makes.errYesNotValidType);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                ErrorString = ex.Message;
                return false;
            }

            return true;
        }

        public void CommitForUser(string szUser)
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
                if (!Profile.GetUser(szUser).CanManageData)
                    AllowedTypes = mmExisting.AllowedTypes;
            }

            Commit(szUser, IsNew ? string.Empty : mmExisting.ToString());

        }

        /// <summary>
        /// Saves the make/model to the DB, creating it if necessary
        /// <paramref name="szChangingUser">User making the change</paramref>
        /// <paramref name="szOriginalDesc">Description of the original version; empty for new</paramref>
        /// </summary>
        public void Commit(string szChangingUser, string szOriginalDesc)
        {
            if (!FIsValid())
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Makes.errSaveMakeFailed, ErrorString));

            if (szChangingUser == null)
                throw new UnauthorizedAccessException("MUST be authenticated to change a model");
            if (szOriginalDesc == null && !IsNew)
                throw new InvalidOperationException("Model updates MUST provide an original description");

            bool fWasNew = IsNew;   // need this for below, since IsNew will change state after commit

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
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, Resources.Makes.errSaveMakeFailed, szQ + "\r\n" + dbh.LastError));

            if (MakeModelID == -1)
                MakeModelID = dbh.LastInsertedRowId;

            // Fix up display, if needed (e.g., for newly added or edited model)
            if (String.IsNullOrEmpty(ManufacturerDisplay))
                ManufacturerDisplay = Manufacturer.CachedManufacturers().First(man => man.ManufacturerID == ManufacturerID).ManufacturerName;
            if (String.IsNullOrEmpty(CategoryClassDisplay))
                CategoryClassDisplay = CategoryClass.CategoryClassFromID(CategoryClassID).CatClass;

            if (MakeModelID != -1)
                HttpRuntime.Cache.Insert(CacheKey(MakeModelID), this, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(60), CacheItemPriority.Normal, null);

            // use fIsNew because Model.IsNew may have been true and not now.
            string szLinkEditModel = String.Format(CultureInfo.InvariantCulture, "{0}/{1}", "~/mvc/Aircraft/ViewModel".ToAbsoluteURL(HttpContext.Current.Request), MakeModelID);
            string szNewDesc = this.ToString();
            if (fWasNew)
                util.NotifyAdminEvent("New Model created", String.Format(CultureInfo.InvariantCulture, "User: {0}\r\n\r\n{1}\r\n{2}", Profile.GetUser(szChangingUser).DetailedName, szNewDesc, szLinkEditModel), ProfileRoles.maskCanManageData);
            else
            {
                if (String.Compare(szNewDesc, szOriginalDesc, StringComparison.Ordinal) != 0)
                    util.NotifyAdminEvent("Model updated", String.Format(CultureInfo.InvariantCulture, "User: {0}\r\n\r\nWas:\r\n{1}\r\n\r\nIs Now: \r\n{2}\r\n \r\nID: {3}, {4}",
                        Profile.GetUser(szChangingUser).DetailedName,
                        szOriginalDesc,
                        szNewDesc,
                        MakeModelID, szLinkEditModel), ProfileRoles.maskCanManageData);
            }
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

            HttpRuntime.Cache.Insert(CacheKey(this.MakeModelID), this, null, DateTime.Now.AddHours(2), Cache.NoSlidingExpiration, CacheItemPriority.Low, null);
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
            object o = HttpRuntime.Cache[szCacheKeyModelCount];
            int cModels = (o == null) ? 0 : (int) o;
            if (cModels == 0)
            {
                DBHelper dbh = new DBHelper("SELECT count(*) AS numModels FROM models");
                dbh.ReadRow((comm) => { }, (dr) => { cModels = Convert.ToInt32(dr["numModels"], CultureInfo.InvariantCulture); });
                HttpRuntime.Cache[szCacheKeyModelCount] = cModels;
            }
            return cModels;
        }

        private const string szCacheKeyModels = "keyAllModelsByManufacturer";

        public static IDictionary<int, List<MakeModel>> ModelsByManufacturer(bool fIncludeGeneric = false)
        {
            Dictionary<int, List<MakeModel>> d = (Dictionary<int, List<MakeModel>>)HttpRuntime.Cache[szCacheKeyModels];
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

                HttpRuntime.Cache.Add(szCacheKeyModels, d, null, Cache.NoAbsoluteExpiration, new TimeSpan(0, 30, 0), CacheItemPriority.BelowNormal, null);
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
                throw new ArgumentNullException(nameof(rgac));
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
                return !col.Any() ? string.Empty : String.Format(CultureInfo.CurrentCulture, "({0})", String.Join(", ", col));
            }
        }

        public LinkedString UserFlightsTotal(string szUser, MakeModelStats userStats)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));
            if (userStats == null)
                throw new ArgumentNullException(nameof(userStats));

            FlightQuery fq = new FlightQuery(szUser);
            fq.MakeList.Add(this);
            string szStatsLabel = String.Format(CultureInfo.CurrentCulture, Resources.Makes.MakeStatsFlightsCount, userStats.NumFlights, userStats.EarliestFlight.HasValue && userStats.LatestFlight.HasValue ?
            String.Format(CultureInfo.CurrentCulture, Resources.Makes.MakeStatsFlightsDateRange, userStats.EarliestFlight.Value, userStats.LatestFlight.Value) : string.Empty);
            return new LinkedString(szStatsLabel, String.Format(CultureInfo.InvariantCulture, "~/mvc/flights?ft=Totals&fq={0}", fq.ToBase64CompressedJSONString()));
        }

        public IEnumerable<LinkedString> AttributeListForUser(IEnumerable<Aircraft> rgac, string szUser, MakeModelStats userStats, AircraftStats acStats, AvionicsTechnologyType upgradeType = AvionicsTechnologyType.None, DateTime? upgradeDate = null)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            List<LinkedString> lstAttribs = new List<LinkedString>();
            if (acStats != null)
                lstAttribs.Add(acStats.UserStatsDisplay);

            if (!String.IsNullOrEmpty(FamilyName))
                lstAttribs.Add(new LinkedString(ModelQuery.ICAOPrefix + FamilyName));
            foreach (string sz in AttributeList(upgradeType, upgradeDate))
                lstAttribs.Add(new LinkedString(sz));
            if (rgac?.Any() ?? false)
            {
                lstAttribs.Add(new LinkedString(String.Format(CultureInfo.CurrentCulture, Resources.Makes.MakeStatsAircraftCount, rgac.Count())));
                MakeModelStats stats = userStats ?? StatsForUser(szUser);
                lstAttribs.Add(UserFlightsTotal(szUser, stats));
            }
            return lstAttribs;
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

        static readonly LazyRegex rNormalizeModel = new LazyRegex("[- ]+");

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

        /// <summary>
        /// Is this an empty query?
        /// </summary>
        public bool IsEmpty
        {
            get { return (FullText?.Length ?? 0) + (ManufacturerName?.Length ?? 0) + (ModelName?.Length ?? 0) + (CatClass?.Length ?? 0) + (TypeName?.Length ?? 0) == 0; }
        }

        /// <summary>
        /// Is this an advanced query?
        /// </summary>
        public bool IsAdvanced
        {
            get { return (FullText?.Length ?? 0) == 0 && !IsEmpty; }
        }

        private static readonly char[] searchSeparator = new char[] { ' ' };
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
            string[] rgTerms = FullText.Replace("-", string.Empty).Split(searchSeparator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < rgTerms.Length; i++)
                AddQueryTerm(rgTerms[i], String.Format(CultureInfo.InvariantCulture, "FullText{0}", i), "REPLACE(Concat(model, ' ', manufacturers.manufacturer, ' ', typename, ' ', family, ' ', modelname, ' ', categoryclass.CatClass), '-', '')", lstWhereTerms, lstParams);

            string szPreferred = "0";
            if (PreferModelNameMatch)
            {
                string szModelMatch = String.Format(CultureInfo.InvariantCulture, "%{0}%", FullText.EscapeMySQLWildcards().ConvertToMySQLWildcards());
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
        /// Adds a query term
        /// </summary>
        /// <param name="szQ">The search text</param>
        /// <param name="szParamName">Parameter name for this search term</param>
        /// <param name="szMatchField">The DB query field against which to match</param>
        /// <param name="lstTerms">The list of WHERE terms to which to add</param>
        /// <param name="lstParams">The list of MySQLParameters to which to add</param>
        private static void AddQueryTerm(string szQ, string szParamName, string szMatchField, List<string> lstTerms, List<MySqlParameter> lstParams)
        {
            if (String.IsNullOrEmpty(szQ))
                return;

            lstTerms.Add(String.Format(CultureInfo.InvariantCulture, " ({0} LIKE ?{1}) ", szMatchField, szParamName));
            lstParams.Add(new MySqlParameter(szParamName, String.Format(CultureInfo.InvariantCulture, "%{0}%", szQ.EscapeMySQLWildcards().ConvertToMySQLWildcards())));
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

    /// <summary>
    /// Admin Utility functions for models.  All static functions.
    /// </summary>
    public class AdminMakeModel : MakeModel
    {
        /// <summary>
        /// Admin function to merge two duplicate models
        /// </summary>
        /// <param name="idModelToDelete">The id of the model that is redundant</param>
        /// <param name="idModelToMergeInto">The id of the model into which the redundant model should be merged</param>
        /// <returns>Audit of the operations that occor</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="MyFlightbookException"></exception>
        public static IEnumerable<string> AdminMergeDuplicateModels(int idModelToDelete, int idModelToMergeInto)
        {
            if (idModelToDelete < 0)
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Invalid model to delete: {0}", idModelToDelete));
            if (idModelToDelete < 0)
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Invalid model to merge into: {0}", idModelToMergeInto));

            List<string> lst = new List<string>() { "Audit of changes made" };

            // Before we migrate old aircraft, see if there are generics.
            Aircraft acGenericSource = new Aircraft(Aircraft.AnonymousTailnumberForModel(idModelToDelete));
            Aircraft acGenericTarget = new Aircraft(Aircraft.AnonymousTailnumberForModel(idModelToMergeInto));

            if (acGenericSource.AircraftID != Aircraft.idAircraftUnknown)
            {
                // if the generic for the target doesn't exist, then no problem - just rename it and remap it!
                if (acGenericTarget.AircraftID == Aircraft.idAircraftUnknown)
                {
                    acGenericSource.ModelID = idModelToMergeInto;
                    acGenericSource.TailNumber = Aircraft.AnonymousTailnumberForModel(idModelToMergeInto);
                    acGenericSource.Commit();
                }
                else
                {
                    // if the generic for the target also exists, need to merge the aircraft (creating a tombstone).
                    AircraftUtility.AdminMergeDupeAircraft(acGenericTarget, acGenericSource);
                }
            }

            IEnumerable<Aircraft> rgac = new UserAircraft(null).GetAircraftForUser(UserAircraft.AircraftRestriction.AllMakeModel, idModelToDelete);

            foreach (Aircraft ac in rgac)
            {
                // Issue #1068: if Aircraft already exists with idModelToMergeInto, do a merge
                List<Aircraft> lstSimilarTails = Aircraft.AircraftMatchingTail(ac.TailNumber);
                Aircraft acDupe = lstSimilarTails.FirstOrDefault(a => a.ModelID == idModelToMergeInto);

                if (acDupe == null) // no collision
                {
                    ac.ModelID = idModelToMergeInto;
                    ac.Commit();
                    lst.Add(String.Format(CultureInfo.CurrentCulture, "Updated aircraft {0} to model {1}", ac.AircraftID, idModelToMergeInto));
                }
                else
                {
                    // collision - do a merge instead!
                    AircraftUtility.AdminMergeDupeAircraft(acDupe, ac);
                    lst.Add(string.Format(CultureInfo.CurrentCulture, "Aircraft {0} exists with both models!  Merged", ac.TailNumber));
                }
            }

            // Update any custom currency references to the old model
            DBHelper dbhCCR = new DBHelper("UPDATE CustCurrencyRef SET value=?newID WHERE value=?oldID AND type=?modelsType");
            dbhCCR.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("newID", idModelToMergeInto);
                comm.Parameters.AddWithValue("oldID", idModelToDelete);
                comm.Parameters.AddWithValue("modelsType", (int)Currency.CustomCurrency.CurrencyRefType.Models);
            });

            // Then delete the old model
            string szQ = "DELETE FROM models WHERE idmodel=?idOldModel";
            DBHelper dbhDelete = new DBHelper(szQ);
            if (!dbhDelete.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idOldModel", idModelToDelete); }))
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Error deleting model {0}: {1}", idModelToDelete, dbhDelete.LastError));
            lst.Add(String.Format(CultureInfo.CurrentCulture, "Deleted model {0}", idModelToDelete));
            util.FlushCache();
            return lst;
        }

        public static IEnumerable<MakeModel> ModelsThatShoulBeSims()
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szSQLSelectTemplate, string.Empty, "manufacturers.defaultSim <> 0 AND models.fSimOnly = 0"));
            List<MakeModel> lst = new List<MakeModel>();
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new MakeModel(dr)); });
            return lst;
        }

        public static IEnumerable<MakeModel> OrphanedModels()
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szSQLSelectTemplate, "LEFT JOIN aircraft ac ON models.idmodel=ac.idmodel", "ac.idaircraft IS NULL"));
            List<MakeModel> lst = new List<MakeModel>();
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new MakeModel(dr)); });
            return lst;
        }

        public static IEnumerable<MakeModel> TypeRatedModels()
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szSQLSelectTemplate, string.Empty, "typename <> '' ORDER BY categoryclass.idcatclass ASC, manufacturers.manufacturer ASC, models.model ASC, models.typename ASC"));
            List<MakeModel> lst = new List<MakeModel>();
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new MakeModel(dr)); });
            return lst;
        }

        public static IEnumerable<MakeModel> PotentialDupes(bool fIncludeSims)
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szSQLSelectTemplate, string.Empty,
                String.Format(CultureInfo.InvariantCulture, @"UPPER(REPLACE(REPLACE(CONCAT(models.model, models.idcategoryclass,models.typename), '-', ''), ' ', '')) IN
                    (SELECT modelandtype FROM (SELECT model, COUNT(model) AS cModel, UPPER(REPLACE(REPLACE(CONCAT(m2.model,m2.idcategoryclass,m2.typename), '-', ''), ' ', '')) AS modelandtype FROM models m2 GROUP BY modelandtype HAVING cModel > 1) AS dupes)
                    {0}
                ORDER BY models.model", fIncludeSims ? string.Empty : "HAVING models.fSimOnly = 0")));
            List<MakeModel> lst = new List<MakeModel>();
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new MakeModel(dr)); });
            return lst;
        }

        public static void DeleteModel(int id)
        {
            DBHelper dbh = new DBHelper("DELETE FROM models WHERE idmodel=?idmodel");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idmodel", id); });
            util.FlushCache();
        }
    }
}

