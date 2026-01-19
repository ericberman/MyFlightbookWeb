using MyFlightbook.Web.Sharing;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

/******************************************************
 * 
 * Copyright (c) 2009-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    public enum GroupConjunction { Any, All, None }

    public static class FlightQueryConjunction
    {
        public static string ToSQLConjunction(this GroupConjunction conjunction)
        {
            switch (conjunction)
            {
                case GroupConjunction.All:
                    return " AND ";
                case GroupConjunction.Any:
                    return " OR ";
                case GroupConjunction.None:
                    return " AND NOT ";
            }
            throw new NotImplementedException("Unhandled conjunction: " + conjunction.ToString());
        }

        public static string ToDisplayString(this GroupConjunction conjunction)
        {
            switch (conjunction)
            {
                case GroupConjunction.All:
                    return Resources.FlightQuery.ConjunctionAll;
                case GroupConjunction.Any:
                    return Resources.FlightQuery.ConjunctionAny;
                case GroupConjunction.None:
                    return Resources.FlightQuery.ConjunctionNone;
            }
            throw new NotImplementedException("Unhandled conjunction: " + conjunction.ToString());
        }
    }

    /// <summary>
    /// Manages complex structured queries for matching flights.
    /// </summary>
    [Serializable]
    public class FlightQuery
    {
        public const string SearchFullStopAnchor = "!";

        public enum DateRanges { AllTime, YTD, Tailing6Months, Trailing12Months, ThisMonth, PrevMonth, PrevYear, Trailing30, Trailing90, Custom };
        public enum FlightDistance { AllFlights, LocalOnly, NonLocalOnly };
        public enum AircraftInstanceRestriction { AllAircraft, RealOnly, TrainingOnly };
        public enum EngineTypeRestriction { AllEngines, Piston, Jet, Turboprop, AnyTurbine, Electric };

        #region properties
        /// <summary>
        /// built-in date range
        /// </summary>
        public DateRanges DateRange { get; set; }

        /// <summary>
        /// Local flights, non-local flights, or all flights
        /// </summary>
        public FlightDistance Distance { get; set; }

        /// <summary>
        /// The requested category/classes
        /// </summary>
        public HashSet<CategoryClass> CatClasses { get; private set; }

        /// <summary>
        /// The requested property types
        /// </summary>
        public HashSet<CustomPropertyType> PropertyTypes { get; private set; }

        /// <summary>
        /// Properties to search - AND?  OR? NOT?  Default is OR ("ANY")
        /// </summary>
        [System.ComponentModel.DefaultValue(GroupConjunction.Any)]
        public GroupConjunction PropertiesConjunction { get; set; } = GroupConjunction.Any;

        /// <summary>
        /// The user for whom flights are being found
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Find public flights?
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Any additional enumeration of specific flight IDs.  NOT sent over the wire.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public HashSet<int> EnumeratedFlights { get; set; } = new HashSet<int>();

        /// <summary>
        /// Flight Characteristics - AND?  OR?  NOT?  Default is AND ("ALL")
        /// </summary>
        [System.ComponentModel.DefaultValue(GroupConjunction.All)]
        public GroupConjunction FlightCharacteristicsConjunction { get; set; } = GroupConjunction.All;

        /// <summary>
        /// Matching flights have nighttime landings
        /// </summary>
        public Boolean HasNightLandings { get; set; }

        /// <summary>
        /// Matching flights have full-stop landings
        /// </summary>
        public Boolean HasFullStopLandings { get; set; }

        /// <summary>
        /// Matching flights have any landings.
        /// </summary>
        public Boolean HasLandings { get; set; }

        /// <summary>
        /// Matching flights have IFR approach procedures
        /// </summary>
        public Boolean HasApproaches { get; set; }

        /// <summary>
        /// Matching flights have holding procedures
        /// </summary>
        public Boolean HasHolds { get; set; }

        /// <summary>
        /// Matching flights have logged cross-country time
        /// </summary>
        public Boolean HasXC { get; set; }

        /// <summary>
        /// Matching flights have simulated IMC time
        /// </summary>
        public Boolean HasSimIMCTime { get; set; }

        /// <summary>
        /// Matching flights have ground simulator time
        /// </summary>
        public Boolean HasGroundSim { get; set; }

        /// <summary>
        /// Matching flights have actual IMC time
        /// </summary>
        public Boolean HasIMC { get; set; }

        /// <summary>
        /// Matching flights have actual OR simulated IMC time
        /// </summary>
        public Boolean HasAnyInstrument { get; set; }

        /// <summary>
        /// Matching flights have nighttime flight logged
        /// </summary>
        public Boolean HasNight { get; set; }

        /// <summary>
        /// Matching flights have dual time logged
        /// </summary>
        public Boolean HasDual { get; set; }

        /// <summary>
        /// Matching flights have CFI time logged
        /// </summary>
        public Boolean HasCFI { get; set; }

        /// <summary>
        /// Matching flights have second-in-command time logged
        /// </summary>
        public Boolean HasSIC { get; set; }

        /// <summary>
        /// Matching flights have pilot-in-command time logged
        /// </summary>
        public Boolean HasPIC { get; set; }

        /// <summary>
        /// Matching flights have total time logged
        /// </summary>
        public Boolean HasTotalTime { get; set; }

        /// <summary>
        /// Matching flights have been signed by a CFI
        /// </summary>
        public Boolean IsSigned { get; set; }

        /// <summary>
        /// Earliest date for matching flights
        /// </summary>
        public DateTime DateMin { get; set; }

        /// <summary>
        /// Latest date for matching flights.
        /// </summary>
        public DateTime DateMax { get; set; }

        [XmlIgnore]
        /// <summary>
        /// Convenience property for possibly problematic Min dates from javascript; fails silently.  E.g., 15/15/24 becomes MinValue
        /// DO NOT USE IN CODE - this is strictly to allow JSON serialization to work even with garbage dates
        /// </summary>
        public string DateMinStr
        {
            get { return DateMin.HasValue() ? DateMin.ToShortDateString() : string.Empty; }
            set { DateMin = DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : DateTime.MinValue; }
        }

        [XmlIgnore]
        /// <summary>
        /// Convenience property for possibly problematic Max dates from javascript; fails silently.  E.g., 15/15/24 becomes MinValue
        /// DO NOT USE IN CODE - this is strictly to allow JSON serialization to work even with garbage dates
        /// </summary>
        public string DateMaxStr
        {
            get { return DateMax.HasValue() ? DateMax.ToShortDateString() : string.Empty; }
            set { DateMax = DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : DateTime.MinValue; }
        }

        /// <summary>
        /// General (unspecified) text
        /// </summary>
        public string GeneralText { get; set; }

        /// <summary>
        /// Any additional search columns that need to be added to the query.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string SearchColumns
        {
            get { return String.IsNullOrEmpty(GeneralText) ? string.Empty : "userAircraft.PrivateNotes AS AircraftPrivateNotes, "; }
        }

        /// <summary>
        /// True if we need to join with user aircraft
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool NeedsUserAircraft
        {
            get { return !String.IsNullOrEmpty(GeneralText); }
        }

        /// <summary>
        /// List of aircraft for the matching flights
        /// </summary>
        [JsonIgnore]
        public HashSet<Aircraft> AircraftList { get; private set; }

        /// <summary>
        /// Much smaller list of aircraft ID's, rather than full aircraft (significantly reduces JSON size)
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public IEnumerable<int> AircraftIDList
        {
            get
            {
                List<int> lst = new List<int>();
                if (AircraftList == null || AircraftList.Count == 0)
                    return null;
                    
                foreach (Aircraft ac in AircraftList)
                    lst.Add(ac.AircraftID);
                return lst.ToArray();
            }
            set
            {
                AircraftList.Clear();
                if (value == null || !value.Any())
                    return;
                if (String.IsNullOrEmpty(UserName)) // no user - hit the database
                    AddAircraft(Aircraft.AircraftFromIDs(value));
                else
                {
                    UserAircraft ua = new UserAircraft(UserName);
                    foreach (int id in value)
                    {
                        Aircraft ac = ua.GetUserAircraftByID(id);
                        if (ac != null)
                            AircraftList.Add(ac);
                    }
                }
            }
        }

        /// <summary>
        /// List of matching airports for the matching flights
        /// </summary>
        public HashSet<string> AirportList { get; private set; }

        /// <summary>
        /// List of matching makes/models for the matching flights
        /// </summary>
        [JsonIgnore]
        public HashSet<MakeModel> MakeList { get; private set; }

        /// <summary>
        /// Much smaller list of model ID's, rather than full models (significantly reduces JSON size)
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public IEnumerable<int> MakeIDList
        {
            get
            {
                if (MakeList == null || MakeList.Count == 0)
                    return null;

                List<int> lst = new List<int>();
                foreach (MakeModel m in MakeList)
                    lst.Add(m.MakeModelID);
                return lst.ToArray();
            }
            set
            {
                MakeList.Clear();
                if (value != null)
                    AddModels(value);
            }
        }

        /// <summary>
        /// Text contained in model name
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Text of type names - not contains, must match.
        /// </summary>
        public HashSet<string> TypeNames { get; private set; }

        /// <summary>
        /// All matching flights must be in complex aircraft
        /// </summary>
        public Boolean IsComplex { get; set; }

        /// <summary>
        /// All matching flights must be in aircraft with flaps
        /// </summary>
        public Boolean HasFlaps { get; set; }

        /// <summary>
        /// All matching flights must be in high-performance aircraft
        /// </summary>
        public Boolean IsHighPerformance { get; set; }

        /// <summary>
        /// Matching flights are in aircraft with constant speed props
        /// </summary>
        public Boolean IsConstantSpeedProp { get; set; }

        /// <summary>
        /// Matching flights are in retract aircraft
        /// </summary>
        public Boolean IsRetract { get; set; }

        /// <summary>
        /// Like IsGlass, but deeper: requires GPS moving map and autopilot.
        /// </summary>
        public Boolean IsTechnicallyAdvanced { get; set; }

        /// <summary>
        /// Is the flight in a glass cockpit?
        /// </summary>
        public Boolean IsGlass { get; set; }

        /// <summary>
        /// Matching flights are in tailwheel aircraft
        /// </summary>
        public Boolean IsTailwheel { get; set; }

        /// <summary>
        /// Types of engines to include
        /// </summary>
        public EngineTypeRestriction EngineType { get; set; }

        /// <summary>
        /// Matching flights are in multi-engine helicopters
        /// </summary>
        public Boolean IsMultiEngineHeli { get; set; }

        /// <summary>
        /// DEPRECATED - Matching flights are in turbine aircraft
        /// </summary>
        public Boolean IsTurbine
        {
            get { return EngineType == EngineTypeRestriction.AnyTurbine; }
            set
            {
                if (value && EngineType == EngineTypeRestriction.AllEngines)
                    EngineType = EngineTypeRestriction.AnyTurbine;
            }
        }

        /// <summary>
        /// Does this flight have telemetry?
        /// </summary>
        public Boolean HasTelemetry { get; set; }

        /// <summary>
        /// Does this flight have images or videos?
        /// </summary>
        public Boolean HasImages { get; set; }

        /// <summary>
        /// Is this a motorglider?
        /// </summary>
        public Boolean IsMotorglider { get; set; }

        /// <summary>
        /// What kinds of instances to include?
        /// </summary>
        public AircraftInstanceRestriction AircraftInstanceTypes { get; set; }

        /// <summary>
        /// Hack property to enable quick reset of all featureprops
        /// Returns true if ANY of the aircraft properties are set.  Setting is ignored unless false
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [JsonIgnore]
        public bool HasAircraftFeatures
        {
            get
            {
                return (AircraftInstanceTypes != AircraftInstanceRestriction.AllAircraft) || (EngineType != EngineTypeRestriction.AllEngines) ||
                    IsComplex || HasFlaps || IsHighPerformance || IsConstantSpeedProp || IsRetract || IsTailwheel || IsGlass || IsTechnicallyAdvanced || IsMotorglider || IsMultiEngineHeli;
            }
            protected set
            {
                if (value)  // ONLY reset
                    return;
                IsComplex = HasFlaps = IsHighPerformance = IsConstantSpeedProp = IsRetract = IsTailwheel = IsGlass = IsTechnicallyAdvanced = IsMotorglider = IsMultiEngineHeli = false;
                AircraftInstanceTypes = AircraftInstanceRestriction.AllAircraft;
                EngineType = EngineTypeRestriction.AllEngines;
            }
        }

        /// <summary>
        /// Hack property to enable quick reset of all featureprops
        /// Returns true if ANY of the aircraft properties are set.  Setting is ignored unless false
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [JsonIgnore]
        public bool HasFlightFeatures
        {
            get
            {
                return HasNightLandings || HasFullStopLandings || HasLandings || HasApproaches || HasHolds || HasXC || HasSimIMCTime || HasGroundSim || HasIMC || HasAnyInstrument || HasNight || HasDual ||
                    HasCFI || HasSIC || HasPIC || HasTotalTime || IsPublic || IsSigned || HasTelemetry || HasImages;
            }
            protected set
            {
                if (value)  // ONLY reset
                    return;
                HasNightLandings = HasFullStopLandings = HasLandings = HasApproaches = HasHolds = HasXC = HasSimIMCTime = HasGroundSim = HasIMC = HasAnyInstrument = HasNight = HasDual =
                    HasCFI = HasSIC = HasPIC = HasTotalTime = IsPublic = IsSigned = HasTelemetry = HasImages = false;
            }
        }

        /// <summary>
        /// Determines if this object is in its default (empty) state.  A hack, but I do this by comparing JSON serializations - should be robust against new/modified properties.
        /// </summary>
        [JsonIgnoreAttribute]
        public virtual bool IsDefault
        {
            get { return JsonConvert.SerializeObject(this, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore }).CompareCurrentCultureIgnoreCase(JsonConvert.SerializeObject(new FlightQuery(this.UserName), new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore })) == 0; }
        }
        #endregion

        #region Collection Helpers
        /// <summary>
        /// Adds the model associated with the given ID, avoiding duplicates
        /// </summary>
        /// <param name="idModel">The model ID</param>
        public void AddModelById(int idModel)
        {
            MakeModel mm = MakeModel.GetModel(idModel);
            if (mm != null)
                MakeList.Add(mm);
        }

        /// <summary>
        /// Adds the set of specified models - by ID - to the query.
        /// </summary>
        /// <param name="rgModelIds"></param>
        public void AddModels(IEnumerable<int> rgModelIds)
        {
            if (rgModelIds == null)
                return;

            foreach (int i in rgModelIds)
                AddModelById(i);
        }

        /// <summary>
        /// Adds the category class associated and type with the given ID, avoiding duplicates
        /// </summary>
        /// <param name="cc">The category class</param>
        /// <param name="szTypeName">The type name</param>
        public void AddCatClass(CategoryClass cc, string szTypeName = null)
        {
            if (cc == null)
                throw new ArgumentNullException(nameof(cc));
            CatClasses.Add(cc);

            if (!String.IsNullOrEmpty(szTypeName))
            {
                TypeNames.Clear();
                TypeNames.Add(szTypeName);
            }
        }

        public void AddAircraft(IEnumerable<Aircraft> rgac)
        {
            if (rgac == null)
                return;

            foreach (Aircraft ac in rgac)
                AircraftList.Add(ac);
        }

        /// <summary>
        /// Adds multiple airports at once
        /// </summary>
        /// <param name="rgsz"></param>
        public void AddAirports(IEnumerable<string> rgsz)
        {
            if (rgsz == null)
                throw new ArgumentNullException(nameof(rgsz));
            foreach (string sz in rgsz)
                AirportList.Add(sz);
        }

        public void AddPropertyTypes(IEnumerable<CustomPropertyType> rgcpt)
        {
            if (rgcpt == null)
                return;
            foreach (CustomPropertyType cpt in rgcpt)
                PropertyTypes.Add(cpt);
        }
        #endregion

        /// <summary>
        /// Sees if two queries are semantically the same.
        /// Compares JSON serializations.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if they are the same.</returns>
        public bool IsSameAs(FlightQuery fq)
        {
            if (fq == null)
                return false;

            return String.Compare(ToJSONString(), fq.ToJSONString(), StringComparison.Ordinal) == 0;
        }

        #region JSON Management
        #region Serialization of empty arrays
        public bool ShouldSerializeSearchColumns() { return false; }
        #endregion

        protected virtual FlightQuery Clone()
        {
            return new FlightQuery(this);
        }

        /// <summary>
        /// Returns the flight query in JSON format
        /// </summary>
        public string ToJSONString()
        {
            // Nullify any empty strings or empty arrays or unused values.  Can't rely on "ShouldSerializeXXXX" methods because that disables the XML serialization as well.
            FlightQuery fqNew = Clone();
            if (String.IsNullOrWhiteSpace(fqNew.GeneralText))
                fqNew.GeneralText = null;
            if (String.IsNullOrWhiteSpace(fqNew.ModelName))
                fqNew.ModelName = null;
            if (fqNew.CatClasses == null || fqNew.CatClasses.Count == 0)
                fqNew.CatClasses = null;
            if (fqNew.AircraftIDList == null || !fqNew.AircraftIDList.Any())
                fqNew.AircraftIDList = null;
            if (fqNew.AirportList == null || fqNew.AirportList.Count == 0)
                fqNew.AirportList = null;
            if (fqNew.MakeIDList == null || !fqNew.MakeIDList.Any())
                fqNew.MakeIDList = null;
            if (fqNew.TypeNames == null || fqNew.TypeNames.Count == 0)
                fqNew.TypeNames = null;
            if (fqNew.EnumeratedFlights == null || fqNew.EnumeratedFlights.Count == 0)
                fqNew.EnumeratedFlights = null;

            if (fqNew.PropertyTypes == null || fqNew.PropertyTypes.Count == 0)
                fqNew.PropertyTypes = null;
            else
            {
                fqNew.PropertyTypes = new HashSet<CustomPropertyType>();
                foreach (CustomPropertyType cptSrc in PropertyTypes)
                {
                    CustomPropertyType cpt = new CustomPropertyType();
                    util.CopyObject(cptSrc, cpt);
                    cpt.StripUnnededFields();
                    fqNew.PropertyTypes.Add(cpt);
                }
            }
            fqNew.DateMax = fqNew.DateMax.Date; // remove any time portion - issue #1135
            fqNew.DateMin = fqNew.DateMin.Date;
            return JsonConvert.SerializeObject(fqNew, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore });
        }

        /// <summary>
        /// Returns the compressed JSON representation of the query
        /// </summary>
        public byte[] ToCompressedJSONString()
        {
            return ToJSONString().Compress();
        }

        /// <summary>
        /// Returns the base64 encoded compressed JSON representation of the query.  This SHOULD be URL safe.
        /// </summary>
        public string ToBase64CompressedJSONString()
        {
            // Issue #1181 - base64 encode introduces "+" and "/" which are not URL safe, and urlencode doesn't encode "+" to %2B! 
            return ToJSONString().ToSafeParameter();
        }

        public static FlightQuery FromJSON(string szJSON)
        {
            return JsonConvert.DeserializeObject<FlightQuery>(szJSON, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore });
        }

        protected static FlightQuery FromCompressedJSON(byte[] rgJSON)
        {
            return FromJSON(rgJSON.Uncompress());
        }

        public static FlightQuery FromBase64CompressedJSON(string sz)
        {
            if (sz == null)
                throw new ArgumentNullException(nameof(sz));
            // Issue #1181 - base64 encode introduces "+" and "/" which are not URL safe, and urlencode doesn't encode "+" to %2B! 
            return FromJSON(sz.FromSafeParameter());
        }
        #endregion

        [NonSerialized]
        private string szRestrict = string.Empty;
        [NonSerialized]
        private string szHaving = string.Empty;
        [NonSerialized]
        private List<MySqlParameter> m_rgParams = null;

        protected static DateTime dtStartOfYear { get { return new DateTime(DateTime.Now.Year, 1, 1); } }

        private const string szBreak = "\r\n";

        public void Refresh()
        {
            UpdateRestriction();
        }

        /// <summary>
        /// The SQL "WHERE" clause
        /// </summary>
        [JsonIgnoreAttribute]
        public string RestrictClause
        {
            get { return szRestrict ?? string.Empty; }
        }

        /// <summary>
        /// The SQL "HAVING" clause
        /// </summary>
        [JsonIgnoreAttribute]
        public string HavingClause
        {
            get { return szHaving ?? string.Empty; }
        }

        /// <summary>
        /// A bag of MySqlParameters which go with the restriction string.  NOTE: WE MUST DO THIS FOR REFLECTION
        /// </summary>
        public IEnumerable<MySqlParameter> QueryParameters()
        {
            return m_rgParams;
        }

        /// <summary>
        /// The human-readable description of the query
        /// </summary>
        [JsonIgnoreAttribute]
        public string Description
        {
            get
            {
                if (Filters == null || Filters.Count == 0)
                    return Resources.FlightQuery.FilterNone;

                StringBuilder sb = new StringBuilder();
                foreach (QueryFilterItem qfi in Filters)
                    sb.AppendFormat(CultureInfo.CurrentCulture, "{0}: {1}{2}", qfi.FilterName, qfi.FilterValue, szBreak);
                return sb.ToString();
            }
        }

        [JsonIgnoreAttribute]
        protected List<QueryFilterItem> Filters { get; private set; }

        [JsonIgnoreAttribute]
        public IEnumerable<QueryFilterItem> QueryFilterItems
        {
            get { return Filters; }
        }

        /// <summary>
        /// Initializes a generic flightquery object
        /// <param name="szUsername">The user for whom this applies</param>
        /// </summary>
        public FlightQuery(string szUsername) : this()
        {
            UserName = szUsername;
        }

        /// <summary>
        /// Initializes a generic flightquery object without a user - this is NOT VALID
        /// </summary>
        public FlightQuery()
        {
            UserName = string.Empty;
            DateMin = DateMax = DateTime.MinValue;
            Distance = FlightDistance.AllFlights;
            GeneralText = ModelName = string.Empty;
            AircraftList = new HashSet<Aircraft>();
            AirportList = new HashSet<string>();
            MakeList = new HashSet<MakeModel>();
            TypeNames = new HashSet<string>();

            DateRange = DateRanges.AllTime;
            m_rgParams = new List<MySqlParameter>();
            CatClasses = new HashSet<CategoryClass>();
            PropertyTypes = new HashSet<CustomPropertyType>();

            EngineType = EngineTypeRestriction.AllEngines;
            AircraftInstanceTypes = AircraftInstanceRestriction.AllAircraft;
        }

        /// <summary>
        /// Creates a new flight query using an existing one as an initializer
        /// </summary>
        /// <param name="q">The query from which to initialize</param>
        public FlightQuery(FlightQuery q) : this()
        {
            if (q == null)
                throw new ArgumentNullException(nameof(q));
            // Collections are copied by reference (unlike arrays), so util.copyobject doesn't quite work.
            // So we start with copyObject, but then explicitly make copies of the other collections.
            util.CopyObject(q, this);
            // Go through the collections and copy them explicitly
            CatClasses = new HashSet<CategoryClass>(q.CatClasses);
            PropertyTypes = new HashSet<CustomPropertyType>(q.PropertyTypes);
            AircraftList = new HashSet<Aircraft>(q.AircraftList);
            AirportList = new HashSet<string>(q.AirportList);
            MakeList = new HashSet<MakeModel>(q.MakeList);
            TypeNames = new HashSet<string>(q.TypeNames);
        }

        /// <summary>
        /// Conditionally adds a restriction clause to the passed stringbuilder object.  Includes the "AND" clause if a restriction already has been started.
        /// </summary>
        /// <param name="sb">The stringbuilder to add to</param>
        /// <param name="szClause">The clause to add</param>
        /// <param name="f">True to add it, false to do nothing</param>
        /// <param name="conjunction">Conjunction to use (default to "ALL" (AND))</param>
        private static void AddClause(StringBuilder sb, string szClause, Boolean f = true, GroupConjunction conjunction = GroupConjunction.All)
        {
            if (f && !String.IsNullOrEmpty(szClause))
            {
                if (sb.Length > 0)
                    sb.Append(conjunction.ToSQLConjunction());
                else if (conjunction == GroupConjunction.None)  // if "None" and first element, need to add the initial "not".
                    sb.Append("NOT ");

                sb.Append(szClause);
            }
        }

        private static void AppendIfChecked(StringBuilder sb, Boolean f, string sz)
        {
            if (f)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(sz);
            }
        }

        protected void AddParameter(string szParam, object value)
        {
            m_rgParams.Add(new MySqlParameter(szParam, value));
        }

        #region Update utilities
        private void UpdateDateParameters(StringBuilder sbQuery)
        {
            switch (DateRange)
            {
                case DateRanges.AllTime:
                    // Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterDate, Resources.FlightQuery.DatesAll, "DateRange"));
                    break;
                case DateRanges.Tailing6Months:
                    Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterDate, Resources.FlightQuery.DatesPrev6Month, "DateRange"));
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (flights.date >= '{0}') ", DateTime.Now.AddMonths(-6).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    break;
                case DateRanges.Trailing12Months:
                    Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterDate,  Resources.FlightQuery.DatesPrev12Month, "DateRange"));
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (flights.date >= '{0}') ", DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    break;
                case DateRanges.Trailing30:
                    Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterDate,  Resources.FlightQuery.DatesPrev30Days, "DateRange"));
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (flights.date >= '{0}') ", DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    break;
                case DateRanges.Trailing90:
                    Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterDate,  Resources.FlightQuery.DatesPrev90Days, "DateRange"));
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (flights.date >= '{0}') ", DateTime.Now.AddDays(-90).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    break;
                case DateRanges.YTD:
                    Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterDate,  Resources.FlightQuery.DatesYearToDate, "DateRange"));
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (flights.date >= '{0}') ", dtStartOfYear.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    break;
                case DateRanges.ThisMonth:
                    Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterDate,  Resources.FlightQuery.DatesThisMonth, "DateRange"));
                    DateTime dtMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (flights.date >= '{0}') ", dtMonthStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    break;
                case DateRanges.PrevMonth:
                    Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterDate,  Resources.FlightQuery.DatesPrevMonth, "DateRange"));
                    DateTime dtPrevMonthEnd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);
                    DateTime dtPrevMonthStart = new DateTime(dtPrevMonthEnd.Year, dtPrevMonthEnd.Month, 1);
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (flights.date >= '{0}' && flights.date <= '{1}') ", dtPrevMonthStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), dtPrevMonthEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    break;
                case DateRanges.PrevYear:
                    Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterDate,  Resources.FlightQuery.DatesPrevYear, "DateRange"));
                    DateTime dtPrevYearStart = new DateTime(DateTime.Now.Year - 1, 1, 1);
                    DateTime dtPrevYearEnd = new DateTime(DateTime.Now.Year - 1, 12, 31);
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (flights.date >= '{0}' && flights.date <= '{1}') ", dtPrevYearStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), dtPrevYearEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    break;
                case DateRanges.Custom:
                    bool fHasStartDate = (DateMin.CompareTo(DateTime.MinValue) != 0);
                    bool fHasEndDate = (DateMax.CompareTo(DateTime.MinValue) != 0);
                    if (!fHasStartDate && fHasEndDate)
                    {
                        Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterDate,  String.Format(CultureInfo.CurrentCulture, Resources.FlightQuery.DatesBefore, DateMax.ToShortDateString()), "DateRange"));
                        AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (flights.date <= '{0}') ", DateMax.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    }
                    else if (fHasStartDate && !fHasEndDate)
                    {
                        Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterDate,  String.Format(CultureInfo.CurrentCulture, Resources.FlightQuery.DatesAfter, DateMin.ToShortDateString()), "DateRange"));
                        AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (flights.date >= '{0}') ", DateMin.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    }
                    else if (fHasStartDate && fHasEndDate)
                    {
                        Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterDate,  String.Format(CultureInfo.CurrentCulture, Resources.FlightQuery.DatesBetween, DateMin.Date.ToShortDateString(), DateMax.Date.ToShortDateString()), "DateRange"));
                        AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (flights.date >= '{0}') ", DateMin.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                        AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (flights.date <= '{0}') ", DateMax.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    }
                    // else neither date specified - add no date specifier!!
                    break;
                default:
                    throw new MyFlightbookValidationException("Unknown date range!");
            }
        }

        private void UpdateGeneralText(StringBuilder sbQuery)
        {
            if (String.IsNullOrWhiteSpace(GeneralText))
                return;

            /* following Google model (sorta) here:
                * dog cat = must contain "dog" and must contain "cat" (but not necessarily in that order, separated by anything)
                * "dog cat" = must contains "dog cat" (inclusive of spaces)
                * dog OR cat = contains dog OR contains cat
                * -dog = must NOT contain dog
                * -"dog cat" = must NOT contain "dog cat"
                * -"dog -cat" must NOT contain "dog -cat" (i.e., the hyphen inside the quoted text is preserved, not negation)
                * -dog -cat = contains neither dog nor cat ("NOT dog AND NOT cat") - so there is no negation of an "OR", since this can express that.
                * -dog OR cat = doesn't contain dog OR does contain cat.

                This is nice because we can largely just look at prefixes

                Searches are ALWAYS case insensitive, ALWAYS partial word search

                Finally - issue #662: add support for Trailing:##{D|CM|M} for trailing ## days, calendar months, or months.  Will adjust the date parameters.
            */

            // First, look for trailing ##.  If we find this, we will then strip it out.
            MatchCollection mcTrailing = RegexUtility.QueryTrailingDate.Matches(GeneralText);
            if (mcTrailing.Count > 0)
            {
                string szType = mcTrailing[0].Groups["rangetype"].Value;
                if (int.TryParse(mcTrailing[0].Groups["quantity"].Value, out int count))
                {
                    DateRange = DateRanges.Custom;
                    DateMax = DateTime.MinValue;
                    switch (szType.ToUpperInvariant())
                    {
                        case "D":
                            DateMin = DateTime.Now.Date.AddDays(-count);
                            break;
                        case "CM":
                            DateMin = DateTime.Now.Date.AddCalendarMonths(-count);
                            break;
                        case "M":
                            DateMin = DateTime.Now.Date.AddMonths(-count);
                            break;
                        case "W":
                            DateMin = DateTime.Now.Date.AddDays(-count * 7);
                            break;
                    }

                    // Now remove this from GeneralText because we're not actually searching for the text.
                    GeneralText = RegexUtility.QueryTrailingDate.Replace(GeneralText, string.Empty);
                }
            }

            const string szFreeText = "CONCAT_WS(' ', ModelDisplay, TailNumber, Route, Comments, CustomProperties, CFIComment, CFIName, AircraftPrivateNotes)";

            // Split phrases at OR, everything within them is an AND.  E.g., "cat dog or horse buggy" should be treated as "cat and dog" OR "horse and buggy".
            // So each PHRASE is an "AND", and the phrases themselves will be OR'd together.
            string[] phrases = RegexUtility.QueryORPhrases.Split(GeneralText.Trim()).Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();

            int iWord = 0;
            List<string> lstORClauses = new List<string>();
            foreach (string phrase in phrases)
            {
                List<string> lstANDClauses = new List<string>();
                MatchCollection mTerms = RegexUtility.QuerySearchTerms.Matches(phrase);
                foreach (Match mTerm in mTerms)
                {
                    string szTerm = mTerm.Groups["term"].Value.EscapeMySQLWildcards().Trim();
                    bool fNegate = !String.IsNullOrEmpty(mTerm.Groups["negate"].Value);
                    if (string.IsNullOrWhiteSpace(szTerm))
                        continue;

                    string szParam = "SearchWord" + (iWord++).ToString(CultureInfo.InvariantCulture);

                    // Issue #802 - Quick hack to direct a specific query to comment or route
                    MatchCollection mcSpecific = RegexUtility.QuerySpecificField.Matches(szTerm);
                    if (mcSpecific.Count > 0)    // user is requesting a specific match on a specific field
                    {
                        Match m = mcSpecific[0];
                        string szField = string.Empty;
                        switch (m.Groups["field"].Value.ToUpperInvariant())
                        {
                            case RegexUtility.szPrefixLimitComments:
                                szField = "Comments";
                                break;
                            case RegexUtility.szPrefixLimitRoute:
                                szField = "Route";
                                break;
                            default:
                                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Unknown prefix '{0}' matched RegexUtility.QuerySpecificField", m.Groups["field"].Value));
                        }

                        // CMT= or RTE= means empty
                        string szValue = m.Groups["value"].Value.Trim();
                        if (String.IsNullOrEmpty(szValue))
                            lstANDClauses.Add(String.Format(CultureInfo.InvariantCulture, "{0} {1} '' ", szField, fNegate ? "<>" : "="));
                        else
                        {
                            lstANDClauses.Add(String.Format(CultureInfo.InvariantCulture, "{0} {1} LIKE ?{2}", szField, fNegate ? "NOT" : string.Empty, szParam));
                            AddParameter(szParam, szValue.EscapeMySQLWildcards().ConvertToMySQLWildcards()); // must be a match on the full field, but can have * and ? as wildcards.
                        }
                    }
                    else
                    {
                        lstANDClauses.Add(String.Format(CultureInfo.InvariantCulture, "{0} {1} LIKE ?{2}", szFreeText, fNegate ? "NOT " : string.Empty, szParam));
                        AddParameter(szParam, String.Format(CultureInfo.InvariantCulture, "%{0}%", szTerm.ConvertToMySQLWildcards()));
                    }
                }

                lstORClauses.Add(String.Format(CultureInfo.InvariantCulture, "({0})", String.Join(" AND ", lstANDClauses)));
            }
            AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, "({0}) ", String.Join(" OR ", lstORClauses)));

            Filters.Add(new QueryFilterItem(Resources.FlightQuery.ContainsText, GeneralText, "GeneralText"));
        }

        private void UpdateAircraft(StringBuilder sbQuery)
        {
            if (AircraftList.Count != 0)
            {
                List<string> lstIds = new List<string>();
                List<string> lstDescriptors = new List<string>();

                foreach (Aircraft ac in AircraftList)
                {
                    lstIds.Add(ac.AircraftID.ToString(CultureInfo.InvariantCulture));
                    lstDescriptors.Add(ac.DisplayTailnumber);
                }

                // issue #908 - not sure how this happens, but it has happened.
                if (lstIds.Count == 0)
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Aircraft list in FlightQuery has {0} item(s), id list has {1}.", AircraftList.Count, lstIds.Count));

                AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, "flights.idaircraft IN ({0}) ", String.Join(", ", lstIds)));
                Filters.Add(new QueryFilterItem(Resources.FlightQuery.ContainsAircraft, String.Join(", ", lstDescriptors), "AircraftList"));
            }
        }
        
        private void UpdateAirports(StringBuilder sbQuery)
        {
            const string regexLocal = "^([a-zA-Z0-9]{3,5})[^a-zA-Z0-9]+\\\\1$";
            switch (Distance)
            {
                case FlightDistance.AllFlights:
                    break;
                case FlightDistance.LocalOnly:
                    // MySQL 8 doesn't support the CONCAT expression in the RLIKE, but MySql 5.7 doesn't support the backreference.  Backreference is better, of course...
                    if (DBHelper.DBVersion.StartsWith("5"))
                        AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " ((LENGTH(Route) <= {0}) OR (Route IS NULL) OR (Route RLIKE CONCAT('^', LEFT(Route, 4), '[^a-zA-Z0-9]*', LEFT(Route, 4), '$') OR Route RLIKE CONCAT('^', LEFT(Route, 3), '[^a-zA-Z0-9]*', LEFT(Route, 3), '$'))) ", MyFlightbook.Airports.airport.maxCodeLength));
                    else
                        AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " ((LENGTH(Route) <= {0}) OR (Route IS NULL) OR (Route RLIKE '{1}')) ", Airports.airport.maxCodeLength, regexLocal));
                    Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterFlightRange, Resources.FlightQuery.FlightRangeLocal, "Distance"));
                    break;
                case FlightDistance.NonLocalOnly:
                    // MySQL 8 doesn't support the CONCAT expression in the RLIKE, but MySql 5.7 doesn't support the backreference.  Backreference is better, of course...
                    if (DBHelper.DBVersion.StartsWith("5"))
                        AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " LENGTH(Route) > {0}  AND NOT (Route RLIKE CONCAT('^', LEFT(Route, 4), '[^a-zA-Z0-9]*', LEFT(Route, 4), '$') OR Route RLIKE CONCAT('^', LEFT(Route, 3), '[^a-zA-Z0-9]*', LEFT(Route, 3), '$'))", Airports.airport.maxCodeLength));
                    else
                        AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (LENGTH(Route) > {0}  AND NOT (Route RLIKE '{1}')) ", Airports.airport.maxCodeLength, regexLocal));
                    Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterFlightRange, Resources.FlightQuery.FlightRangeNonLocal, "Distance"));
                    break;
                default:
                    break;
            }

            // Airports:
            List<string> lstAirports = new List<string>(AirportList);
            lstAirports.RemoveAll(s => String.IsNullOrWhiteSpace(s));
            if (lstAirports.Count > 0)
            {
                StringBuilder sbAirports = new StringBuilder("( ");
                StringBuilder sbAirportDesc = new StringBuilder();

                for (int i = 0; i < lstAirports.Count; i++)
                {
                    if (i > 0)
                        sbAirports.Append(" OR ");

                    string szParam = "SearchAirport" + i.ToString(CultureInfo.InvariantCulture);

                    sbAirports.Append(String.Format(CultureInfo.InvariantCulture, "(flights.Route RLIKE ?{0})", szParam));
                    string szCode = lstAirports[i];
                    bool fIsDepart = szCode.StartsWith(SearchFullStopAnchor, StringComparison.CurrentCultureIgnoreCase);
                    bool fIsArrival = szCode.EndsWith(SearchFullStopAnchor, StringComparison.CurrentCultureIgnoreCase);
                    string szNormalAirport = szCode.Replace(SearchFullStopAnchor, string.Empty);    // remove any anchor
                    // Make the leading "K" optional
                    if (szNormalAirport.StartsWith(Airports.airport.USAirportPrefix, StringComparison.CurrentCultureIgnoreCase) && szNormalAirport.Length == 4) // k-hack only applies for 4 characters or longer
                        szNormalAirport = String.Format(CultureInfo.InvariantCulture, "({0})?{1}", Airports.airport.USAirportPrefix, szNormalAirport.Substring(Airports.airport.USAirportPrefix.Length));
                    string szParamValue = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", fIsDepart ? "^" : string.Empty, szNormalAirport, fIsArrival ? "$" : string.Empty);
                    AddParameter(szParam, szParamValue);

                    sbAirportDesc.Append(lstAirports[i] + Resources.LocalizedText.LocalizedSpace);
                }

                sbAirports.Append(')');
                AddClause(sbQuery, sbAirports.ToString());

                Filters.Add(new QueryFilterItem(Resources.FlightQuery.ContainsAirports, sbAirportDesc.ToString().Trim(), "AirportList"));
            }
        }

        protected const string szICAOPrefix = "ICAO:";

        private void UpdateModels(StringBuilder sbQuery)
        {
            string szModelsIDQuery = string.Empty;
            string szModelsTextQuery = string.Empty;
            string szModelsTypeQuery = string.Empty;

            if (MakeList.Count != 0)
            {
                List<string> lstIds = new List<string>();
                List<string> lstDesc = new List<string>();

                foreach (MakeModel mm in MakeList)
                {
                    lstIds.Add(mm.MakeModelID.ToString(CultureInfo.InvariantCulture));
                    lstDesc.Add(mm.DisplayName.Trim());
                }
                szModelsIDQuery = String.Format(CultureInfo.InvariantCulture, "models.idmodel IN ({0}) ", String.Join(", ", lstIds));

                Filters.Add(new QueryFilterItem(Resources.FlightQuery.ContainsMakeModel, String.Join(", ", lstDesc), "MakeList"));
            }

            if (!String.IsNullOrWhiteSpace(ModelName))
            {
                string[] rgModelFragment = RegexUtility.ModelFragmentBoundary.Split(ModelName);
                int i = 0;
                StringBuilder sbModelName = new StringBuilder();
                foreach (string sz in rgModelFragment)
                {
                    string szSearch = sz.Trim();
                    bool fICAO = szSearch.ToUpperInvariant().StartsWith(szICAOPrefix, StringComparison.CurrentCultureIgnoreCase);
                    if (fICAO)
                        szSearch = szSearch.Substring(szICAOPrefix.Length).Trim();

                    if (szSearch.Length > 0)
                    {
                        string szParamName = String.Format(CultureInfo.InvariantCulture, "?modelQuery{0}", i++);
                        AddParameter(szParamName, fICAO ? szSearch : String.Format(CultureInfo.InvariantCulture, "%{0}%", szSearch));
                        sbModelName.Append(sbModelName.Length == 0 ? "(" : " AND ");
                        sbModelName.AppendFormat(CultureInfo.InvariantCulture, fICAO ? "(models.family={0}) " : "(ModelDisplay LIKE {0} OR FamilyDisplay LIKE {0}) ", szParamName);
                    }
                }
                if (sbModelName.Length > 0)
                {
                    sbModelName.Append(')');
                    szModelsTextQuery = sbModelName.ToString();
                    Filters.Add(new QueryFilterItem(String.IsNullOrEmpty(szModelsIDQuery) ? Resources.FlightQuery.ContainsMakeModelText : Resources.FlightQuery.ContainsMakeModelTextOR, ModelName, "ModelName"));
                }
            }

            if (TypeNames != null && TypeNames.Count > 0)
            {
                if (TypeNames.Count == 1 && String.IsNullOrEmpty(TypeNames.ElementAt(0)))
                {
                    // special case: single empty typename = "No type."
                    szModelsTypeQuery = " (typename='') ";
                    Filters.Add(new QueryFilterItem(Resources.FlightQuery.ContainsType, Resources.FlightQuery.ContainsTypeNone, "TypeNames"));
                }
                else
                {
                    int i = 0;
                    StringBuilder sbTypes = new StringBuilder();
                    foreach (string szType in TypeNames)
                    {
                        if (!string.IsNullOrWhiteSpace(szType))
                        {
                            string szParamName = String.Format(CultureInfo.InvariantCulture, "?typequery{0}", i++);
                            AddParameter(szParamName, szType.Trim());
                            sbTypes.Append(sbTypes.Length == 0 ? "(" : " OR ");
                            sbTypes.AppendFormat(CultureInfo.InvariantCulture, "typename={0}", szParamName);
                        }
                    }
                    if (sbTypes.Length > 0)
                    {
                        sbTypes.Append(')');
                        szModelsTypeQuery = sbTypes.ToString();
                        Filters.Add(new QueryFilterItem(TypeNames.Count == 1 ? Resources.FlightQuery.ContainsType : Resources.FlightQuery.ContainsTypeMultiple, String.Join(",", TypeNames), "TypeNames"));
                    }
                }

                // Splice the type query into the models query as an OR - i.e., matches EITHER model OR typenames.
                szModelsTextQuery = String.IsNullOrEmpty(szModelsTextQuery)
                    ? szModelsTypeQuery
                    : String.Format(CultureInfo.InvariantCulture, "({0}) OR {1}", szModelsTextQuery, szModelsTypeQuery);

            }

            // If both ID's and free text are specified, OR them.  Otherwise, just do whatever is specified
            if (!String.IsNullOrEmpty(szModelsIDQuery) && !String.IsNullOrEmpty(szModelsTextQuery)) 
            {
                AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, "(({0}) OR ({1})) ", szModelsIDQuery, szModelsTextQuery));
            }
            else
            {
                AddClause(sbQuery, szModelsIDQuery);
                AddClause(sbQuery, szModelsTextQuery);
            }
        }

        private void UpdateAircraftCharacteristics(StringBuilder sbQuery)
        {
            AddClause(sbQuery, "(models.fTailwheel <> 0)", IsTailwheel);
            AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (models.fHighPerf <> 0 OR (date < '{0}' AND models.f200HP <> 0))", MakeModel.Date200hpHighPerformanceCutoverDate), IsHighPerformance);
            AddClause(sbQuery, "(models.fGlassOnly <> 0 OR (Aircraft.HasGlassUpgrade <> 0 AND (Aircraft.GlassUpgradeDate IS NULL OR date >= Aircraft.GlassUpgradeDate)))", IsGlass);
            AddClause(sbQuery, "(models.fTAA <> 0 OR (Aircraft.HasTAAUpgrade <> 0 AND (Aircraft.GlassUpgradeDate IS NULL OR date >= Aircraft.GlassUpgradeDate)))", IsTechnicallyAdvanced);

            if (IsComplex)
                AddClause(sbQuery, "(models.fComplex <> 0 OR (models.fCowlFlaps <> 0 AND models.fConstantProp <> 0 AND (models.fRetract <> 0 OR (flights.idCatClassOverride IN (3,4) OR (flights.idCatClassOverride = 0 AND models.idcategoryclass IN (3,4))))))");
            else
            {
                AddClause(sbQuery, "(models.fCowlFlaps <> 0)", HasFlaps);
                AddClause(sbQuery, "(models.fConstantProp <> 0)", IsConstantSpeedProp);
                AddClause(sbQuery, "(models.fRetract <> 0)", IsRetract);
            }
            AddClause(sbQuery, "(models.fMotorGlider <> 0)", IsMotorglider);
            AddClause(sbQuery, "(models.fMultiHelicopter <> 0)", IsMultiEngineHeli);

            StringBuilder sbType = new StringBuilder();
            AppendIfChecked(sbType, IsComplex, Resources.FlightQuery.AircraftFeatureComplex);
            AppendIfChecked(sbType, HasFlaps, Resources.FlightQuery.AircraftFeatureFlaps);
            AppendIfChecked(sbType, IsHighPerformance, Resources.FlightQuery.AircraftFeatureHighPerformance);
            AppendIfChecked(sbType, IsConstantSpeedProp, Resources.FlightQuery.AircraftFeatureConstantSpeedProp);
            AppendIfChecked(sbType, IsRetract, Resources.FlightQuery.AircraftFeatureRetractableGear);
            AppendIfChecked(sbType, IsTailwheel, Resources.FlightQuery.AircraftFeatureTailwheel);
            AppendIfChecked(sbType, IsGlass, Resources.FlightQuery.AircraftFeatureGlass);
            AppendIfChecked(sbType, IsTechnicallyAdvanced, Resources.FlightQuery.AircraftFeatureTAA);
            AppendIfChecked(sbType, IsMotorglider, Resources.FlightQuery.AircraftFeatureMotorGlider);
            AppendIfChecked(sbType, IsMultiEngineHeli, Resources.FlightQuery.AircraftFeatureMultiEngineHelicopter);

            const string szPoweredAircraftTemplate = "((IF (flights.idCatClassOverride = 0, models.idcategoryclass, flights.idCatClassOverride) in (1, 2, 3, 4, 7, 8, 9, 13, 14, 17, 18) OR models.fMotorglider <> 0) AND {0})";
            switch (EngineType)
            {
                default:
                case EngineTypeRestriction.AllEngines:
                    break;
                case EngineTypeRestriction.AnyTurbine:
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, szPoweredAircraftTemplate, "(models.fTurbine in (1, 2, 3))"), true);
                    AppendIfChecked(sbType, true, Resources.FlightQuery.AircraftFeatureTurbine);
                    break;
                case EngineTypeRestriction.Piston:
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, szPoweredAircraftTemplate, "(models.fTurbine = 0)"), true);
                    AppendIfChecked(sbType, true, Resources.FlightQuery.AircraftFeaturePiston);
                    break;
                case EngineTypeRestriction.Turboprop:
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, szPoweredAircraftTemplate, "(models.fTurbine = 1)"), true);
                    AppendIfChecked(sbType, true, Resources.FlightQuery.AircraftFeatureTurboprop);
                    break;
                case EngineTypeRestriction.Jet:
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, szPoweredAircraftTemplate, "(models.fTurbine = 2)"), true);
                    AppendIfChecked(sbType, true, Resources.FlightQuery.AircraftFeatureJet);
                    break;
                case EngineTypeRestriction.Electric:
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, szPoweredAircraftTemplate, "(models.fTurbine = 4)"), true);
                    AppendIfChecked(sbType, true, Resources.FlightQuery.AircraftFeatureElectric);
                    break;
            }

            // Aircraft instance types
            switch (AircraftInstanceTypes)
            {
                default:
                case AircraftInstanceRestriction.AllAircraft:
                    break;
                case AircraftInstanceRestriction.RealOnly:
                    AddClause(sbQuery, "(aircraft.instancetype = 1)", true);
                    AppendIfChecked(sbType, true, Resources.FlightQuery.AircraftFeatureReal);
                    break;
                case AircraftInstanceRestriction.TrainingOnly:
                    AddClause(sbQuery, "(aircraft.instancetype <> 1)", true);
                    AppendIfChecked(sbType, true, Resources.FlightQuery.AircraftFeatureTrainingDevice);
                    break;
            }

            if (sbType.Length > 0)
                Filters.Add(new QueryFilterItem(Resources.FlightQuery.AircraftFeatureType, sbType.ToString(), "HasAircraftFeatures"));
        }

        private void UpdateFlightCharacteristics(StringBuilder sbQ)
        {
            StringBuilder sbQuery = new StringBuilder();

            AddClause(sbQuery, "(flights.cNightLandings > 0)", HasNightLandings, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(flights.cFullStopLandings > 0)", HasFullStopLandings, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(flights.cLandings > 0)", HasLandings, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(flights.cInstrumentApproaches > 0)", HasApproaches, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(flights.fHold <> 0)", HasHolds, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(flights.crosscountry > 0.0)", HasXC, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(flights.simulatedInstrument > 0.0)", HasSimIMCTime, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(COALESCE(flights.groundSim, 0) > 0.0)", HasGroundSim, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(flights.IMC > 0.0)", HasIMC, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "((flights.IMC + flights.simulatedInstrument) > 0.0)", HasAnyInstrument, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(flights.night > 0.0)", HasNight, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(flights.dualReceived > 0.0)", HasDual, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(COALESCE(flights.cfi, 0) > 0.0)", HasCFI, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(COALESCE(flights.SIC, 0) > 0.0)", HasSIC, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(flights.PIC > 0.0)", HasPIC, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(flights.totalFlightTime > 0.0)", HasTotalTime, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(flights.fPublic <> 0)", IsPublic, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(COALESCE(flights.signaturestate, 0) <> 0)", IsSigned, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(Coalesce(flights.Telemetry, ft.idflight) IS NOT NULL)", HasTelemetry, FlightCharacteristicsConjunction);
            AddClause(sbQuery, "(flights.idflight IN (SELECT f.idflight FROM flights f INNER JOIN images img ON (img.virtpathid=0 AND CAST(f.idflight AS CHAR(15) CHARACTER SET utf8mb4) COLLATE utf8mb4_unicode_ci=img.Imagekey) WHERE f.username=?uname AND img.imagekey IS NOT NULL))", HasImages, FlightCharacteristicsConjunction);

            if (sbQuery.Length > 0)
                AddClause(sbQ, String.Format(CultureInfo.InvariantCulture, "({0}) ", sbQuery.ToString()));

            StringBuilder sbFlight = new StringBuilder();

            AppendIfChecked(sbFlight, HasNightLandings, Resources.FlightQuery.FlightFeatureFSNightLanding);
            AppendIfChecked(sbFlight, HasFullStopLandings, Resources.FlightQuery.FlightFeatureFSLanding);
            AppendIfChecked(sbFlight, HasLandings, Resources.FlightQuery.FlightFeatureAnyLandings);
            AppendIfChecked(sbFlight, HasApproaches, Resources.FlightQuery.FlightFeatureApproaches);
            AppendIfChecked(sbFlight, HasHolds, Resources.FlightQuery.FlightFeatureHolds);
            AppendIfChecked(sbFlight, HasXC, Resources.FlightQuery.FlightFeatureXC);
            AppendIfChecked(sbFlight, HasSimIMCTime, Resources.FlightQuery.FlightFeatureSimIMC);
            AppendIfChecked(sbFlight, HasGroundSim, Resources.FlightQuery.FlightFeatureGroundsim);
            AppendIfChecked(sbFlight, HasIMC, Resources.FlightQuery.FlightFeatureIMC);
            AppendIfChecked(sbFlight, HasAnyInstrument, Resources.FlightQuery.FlightFeatureAnyInstrument);
            AppendIfChecked(sbFlight, HasNight, Resources.FlightQuery.FlightFeatureNight);
            AppendIfChecked(sbFlight, HasDual, Resources.FlightQuery.FlightFeatureDual);
            AppendIfChecked(sbFlight, HasCFI, Resources.FlightQuery.FlightFeatureCFI);
            AppendIfChecked(sbFlight, HasSIC, Resources.FlightQuery.FlightFeatureSIC);
            AppendIfChecked(sbFlight, HasPIC, Resources.FlightQuery.FlightFeaturePIC);
            AppendIfChecked(sbFlight, HasTotalTime, Resources.FlightQuery.FlightFeatureTotalTime);
            AppendIfChecked(sbFlight, IsPublic, Resources.FlightQuery.FlightFeaturePublic);
            AppendIfChecked(sbFlight, IsSigned, Resources.FlightQuery.FlightFeatureIsSigned);
            AppendIfChecked(sbFlight, HasTelemetry, Resources.FlightQuery.FlightFeatureHasTelemetry);
            AppendIfChecked(sbFlight, HasImages, Resources.FlightQuery.FlightFeatureHasImages);

            if (sbFlight.Length > 0)
                Filters.Add(new QueryFilterItem(String.Format(CultureInfo.CurrentCulture, Resources.FlightQuery.FlightCharacteristics, FlightCharacteristicsConjunction.ToDisplayString()), sbFlight.ToString(), "HasFlightFeatures"));
        }

        private void UpdateFlightProperties(StringBuilder sbQuery)
        {
            if (PropertyTypes != null && PropertyTypes.Count > 0)
            {
                List<string> lstCPT = new List<string>();
                List<string> lstCPTDesc = new List<string>();

                foreach (CustomPropertyType cpt in PropertyTypes)
                {
                    lstCPT.Add(cpt.PropTypeID.ToString(CultureInfo.InvariantCulture));
                    lstCPTDesc.Add(cpt.Title);
                }

                switch (PropertiesConjunction)
                {
                    case GroupConjunction.All:
                        foreach (CustomPropertyType cpt in PropertyTypes)
                            AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " flights.idflight IN (SELECT fp.idflight FROM flightproperties fp INNER JOIN flights f ON fp.idflight=f.idflight WHERE f.username=?uName AND fp.idproptype={0})", cpt.PropTypeID));
                        break;
                    case GroupConjunction.Any:
                        AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " flights.idflight IN (SELECT fp.idflight FROM flightproperties fp INNER JOIN flights f ON fp.idflight=f.idflight WHERE f.username=?uName AND fp.idproptype IN ({0}))", String.Join(", ", lstCPT)));
                        break;
                    case GroupConjunction.None:
                        AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " flights.idflight NOT IN (SELECT fp.idflight FROM flightproperties fp INNER JOIN flights f ON fp.idflight=f.idflight WHERE f.username=?uName AND fp.idproptype IN ({0}))", String.Join(", ", lstCPT)));
                        break;
                    default:
                        throw new NotImplementedException("Unkown properties conjunction: " + PropertiesConjunction.ToString());
                }

                Filters.Add(new QueryFilterItem(String.Format(CultureInfo.CurrentCulture, Resources.FlightQuery.FlightHasProperties, PropertiesConjunction.ToDisplayString()), String.Join(", ", lstCPTDesc), "PropertyTypes"));
            }
        }

        private void UpdateCatClass(StringBuilder sbQuery)
        {
            if (CatClasses.Count > 0)
            {
                List<string> lstIds = new List<string>();
                List<string> lstDesc = new List<string>();

                foreach (CategoryClass cc in CatClasses)
                {
                    lstIds.Add(((int)cc.IdCatClass).ToString(CultureInfo.InvariantCulture));
                    lstDesc.Add(cc.CatClass);
                }
                AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, "((flights.idCatClassOverride = 0 AND models.idcategoryclass IN ({0})) OR flights.idCatClassOverride IN ({0}))", String.Join(", ", lstIds)));

                Filters.Add(new QueryFilterItem(Resources.FlightQuery.CategoryClass, String.Join(", ", lstDesc), "CatClasses"));
            }
        }

        private void UpdateEnumeratedFlights(StringBuilder sbQuery)
        {
            if (EnumeratedFlights != null && EnumeratedFlights.Count != 0)
            {
                AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " (flights.idFlight IN ({0})) ", String.Join(", ", EnumeratedFlights)));
                Filters.Add(new QueryFilterItem(Resources.FlightQuery.EnumeratedFlights, string.Empty, "EnumeratedFlights"));
            }
        }
        #endregion

        protected void UpdateRestriction()
        {
            StringBuilder sbQuery = new StringBuilder();
            StringBuilder sbHaving = new StringBuilder();
            szHaving = szRestrict = string.Empty;
            m_rgParams = new List<MySqlParameter>(); // recreate this - it isn't serialized, could have become null.

            if (!String.IsNullOrEmpty(UserName))
            {
                AddClause(sbQuery, " flights.UserName=?uName ");
                AddParameter("uName", UserName);
            }

            Filters = new List<QueryFilterItem>();
            UpdateGeneralText(sbHaving);    // Text contains - this goes in the HAVING clause
            UpdateDateParameters(sbQuery);
            UpdateAircraft(sbQuery);
            UpdateAirports(sbQuery);
            UpdateModels(sbHaving);         // For some reason, this is slow in the WHERE clause, but fast in HAVING clause
            UpdateAircraftCharacteristics(sbQuery);
            UpdateFlightCharacteristics(sbQuery);
            UpdateFlightProperties(sbQuery);
            UpdateCatClass(sbQuery);
            UpdateEnumeratedFlights(sbQuery);

            szHaving = sbHaving.ToString();

            szRestrict = sbQuery.ToString();
        }

        public FlightQuery ClearRestriction(string prop)
        {
            if (Filters == null)
                UpdateRestriction();

            return ClearRestriction(new QueryFilterItem(string.Empty, string.Empty, prop ?? string.Empty));
        }

        /// <summary>
        /// Resets a specified property to its default value (useful for clicking on a filter to delete it.)
        /// Uses reflection
        /// </summary>
        /// <param name="qfi">The QueryFilterItem object (must have a specified valid property name)</param>
        public FlightQuery ClearRestriction(QueryFilterItem qfi)
        {
            if (qfi == null)
                throw new ArgumentNullException(nameof(qfi));
            FlightQuery fqBlank = new FlightQuery(UserName);

            // Blank property means clear all
            if (String.IsNullOrEmpty(qfi.PropertyName))
            {
                foreach (QueryFilterItem q in this.QueryFilterItems)
                    if (!String.IsNullOrEmpty(q.PropertyName))
                        ClearRestriction(q);
                Filters.Clear();
                return this;
            }

            Type t = typeof(FlightQuery);

            //	Get the matching property in the destination object
            PropertyInfo pi = t.GetProperty(qfi.PropertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            //	If there is none, skip
            if (pi != null && pi.CanWrite)
                pi.SetValue(this, pi.GetValue(fqBlank, null), null);

            // DateRange needs, as a side effect, to clear any specified dates.  Otherwise "IsDefault" won't work.
            if (qfi.PropertyName.CompareOrdinal("DateRange") == 0)
                DateMin = DateMax = DateTime.MinValue;

            // Likewise, clearing flight characteristics or properties needs to reset those conjunctions or else IsDefault won't work
            if (qfi.PropertyName.CompareOrdinal("PropertyTypes") == 0)
                PropertiesConjunction = fqBlank.PropertiesConjunction;
            if (qfi.PropertyName.CompareOrdinal("HasFlightFeatures") == 0)
                FlightCharacteristicsConjunction = fqBlank.FlightCharacteristicsConjunction;

            UpdateRestriction();    // this gets called when we have a flightresult cache miss, but if the cache hits, then we need to pre-emptively update the restriction.

            return this;
        }

        #region Initialization from url query parameters
        private void InitPassedQueryText(string s)
        {
            if (!String.IsNullOrWhiteSpace(s))
                GeneralText = s;
        }

        private void InitPassedAirport(string ap)
        {
            if (!String.IsNullOrWhiteSpace(ap))
            {
                AirportList.Clear();
                AddAirports(Airports.AirportList.NormalizeAirportList(ap));
            }
        }

        private void InitPassedDates(int y, int m, int w, int d)
        {
            if (y > 1900)
            {
                if (m >= 0 && m < 12)
                {
                    DateTime dtStart = new DateTime(y, m + 1, d > 0 ? d : 1);
                    DateTime dtEnd = (d > 0) ? (w > 0 ? dtStart.AddDays(6) : dtStart) : dtStart.AddMonths(1).AddDays(-1);
                    DateRange = DateRanges.Custom;
                    DateMin = dtStart;
                    DateMax = dtEnd;
                }
                else
                {
                    DateRange = DateRanges.Custom;
                    DateMin = new DateTime(y, 1, 1);
                    DateMax = new DateTime(y, 12, 31);
                }
            }
        }

        private void InitPassedAircraft(string tn, string mn, string icao)
        {
            // Add the tail number, model, or ICAO as needed.
            if (!String.IsNullOrEmpty(tn) || !String.IsNullOrEmpty(mn) || !String.IsNullOrEmpty(icao))
            {
                UserAircraft ua = new UserAircraft(UserName);
                List<Aircraft> lstac = new List<Aircraft>();
                HashSet<int> lstmm = new HashSet<int>();
                tn = tn ?? string.Empty;
                mn = mn ?? string.Empty;
                icao = icao ?? string.Empty;

                foreach (Aircraft ac in ua.GetAircraftForUser())
                {
                    if (ac.DisplayTailnumber.CompareCurrentCultureIgnoreCase(tn) == 0)
                        lstac.Add(ac);

                    MakeModel mm = MakeModel.GetModel(ac.ModelID);
                    if (!lstmm.Contains(mm.MakeModelID) &&
                        ((!String.IsNullOrEmpty(mn) && mm.Model.CompareCurrentCultureIgnoreCase(mn) == 0) ||
                        (!String.IsNullOrEmpty(icao) && mm.FamilyName.CompareCurrentCultureIgnoreCase(icao) == 0)))
                        lstmm.Add(mm.MakeModelID);
                }
                if (lstac.Count > 0)
                {
                    AirportList.Clear();
                    AddAircraft(lstac);
                }
                if (lstmm.Count > 0)
                {
                    MakeList.Clear();
                    AddModels(lstmm);
                }
            }
        }

        private void InitPassedCatClass(string szCc)
        {
            if (!String.IsNullOrEmpty(szCc))
            {
                foreach (CategoryClass catclass in CategoryClass.CategoryClasses())
                    if (catclass.CatClass.CompareCurrentCultureIgnoreCase(szCc) == 0)
                        AddCatClass(catclass);
            }
        }

        /// <summary>
        /// Initializes/updates the query based on passed parameters
        /// </summary>
        /// <param name="s">General search term</param>
        /// <param name="ap">Airport query (set of airports)</param>
        /// <param name="y">year, -1 for none</param>
        /// <param name="m">month, -1 for none</param>
        /// <param name="w">week, -1 for none</param>
        /// <param name="d">day, -1 for none</param>
        /// <param name="tn">Tail number</param>
        /// <param name="mn">Model name</param>
        /// <param name="icao">ICAO name</param>
        /// <param name="cc">Category class</param>
        public void InitPassedQueryItems(string s, string ap, int y, int m, int w, int d, string tn, string mn, string icao, string cc)
        {
            InitPassedQueryText(s);
            InitPassedAirport(ap);
            InitPassedDates(y, m, w, d);
            InitPassedAircraft(tn, mn, icao);
            InitPassedCatClass(cc);
            Refresh();
        }
        #endregion
    }

    /// <summary>
    /// Represents a stored query.
    /// </summary>
    [Serializable]
    public class CannedQuery : FlightQuery, IComparable
    {
        const string szUserQueriesKey = "queriesForUser";

        #region Properties
        /// <summary>
        /// Name of the query.  Not serialized in the JSON itself because there's not need to do so (and screws up IsDefault hack anyhow)
        /// </summary>
        [JsonIgnore]
        public string QueryName { get; set; }

        /// <summary>
        /// Optional string representation for flightcoloring
        /// </summary>
        public string ColorString { get; set; }
        #endregion

        #region Constructors
        public CannedQuery() : base()
        {
            QueryName = string.Empty;
        }

        public CannedQuery(string szName) : base(szName)
        {
            QueryName = string.Empty;
        }

        public CannedQuery(FlightQuery fq, string szName) : base(fq)
        {
            QueryName = szName;
        }
        #endregion

        #region Overrides
        protected override FlightQuery Clone()
        {
            return new CannedQuery(this, QueryName);
        }

        public override bool IsDefault
        {
            get
            {
                // Use base IsDefault, but colorstring shouldn't factor if otherwise default.
                string szColor = ColorString;
                ColorString = null;
                bool fResult = base.IsDefault;
                ColorString = szColor;
                return fResult;
            }
        }
        #endregion

        #region database
        /// <summary>
        /// Retrieve the canned queries for a given user
        /// </summary>
        /// <param name="szUser">Username for whom to retrieve</param>
        /// <returns>A set of canned queries.</returns>
        public static IEnumerable<CannedQuery> QueriesForUser(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));

            if (String.IsNullOrWhiteSpace(szUser))
                return Array.Empty<CannedQuery>();

            Profile pf = Profile.GetUser(szUser);
            List<CannedQuery> lst = (List<CannedQuery>) pf.CachedObject(szUserQueriesKey);
            if (lst != null)
                return lst;
            else
                lst = new List<CannedQuery>();

            // Important - preserve "ORDER BY name ASC" so that multiple canned queries can work for flight coloring.
            DBHelperCommandArgs dba = new DBHelperCommandArgs("SELECT * FROM storedQueries WHERE username=?uname ORDER BY name ASC");
            dba.AddWithValue("uname", szUser);
            DBHelper dbh = new DBHelper(dba);
            dbh.ReadRows((comm) => { }, (dr) =>
            {
                CannedQuery cq = JsonConvert.DeserializeObject<CannedQuery>((string)dr["queryjson"], new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore });
                cq.QueryName = (string)dr["name"];
                lst.Add(cq);
            });
            pf.AssociatedData[szUserQueriesKey] = lst;    // cache 'em!
            return lst;
        }

        private void FlushCachedQueries()
        {
            Profile.GetUser(UserName).AssociatedData.Remove(szUserQueriesKey);
        }

        /// <summary>
        /// Saves this cannedquery to the database
        /// </summary>
        public void Commit(bool fPreserveColor = true)
        {
            if (String.IsNullOrWhiteSpace(QueryName) || IsDefault)
                return;

            if (String.IsNullOrEmpty(UserName))
                throw new MyFlightbookValidationException("No username provided for canned query!");

            if (fPreserveColor && String.IsNullOrWhiteSpace(ColorString))
            {
                List<CannedQuery> lst = new List<CannedQuery>(CannedQuery.QueriesForUser(UserName));
                CannedQuery cq = lst.FirstOrDefault(cq2 => cq2.QueryName.CompareCurrentCultureIgnoreCase(QueryName) == 0);
                if (cq != null)
                    ColorString = cq.ColorString;
            }

            DBHelperCommandArgs dba = new DBHelperCommandArgs("REPLACE INTO storedQueries SET username=?uname, name=?qname, queryjson=?fqjson");
            dba.AddWithValue("uname", UserName);
            dba.AddWithValue("qname", QueryName);
            dba.AddWithValue("fqjson", ToJSONString());
            new DBHelper(dba).DoNonQuery();

            FlushCachedQueries();
        }

        public void Delete()
        {
            DBHelperCommandArgs dba = new DBHelperCommandArgs("DELETE FROM storedQueries WHERE username=?uname AND name=?qname");
            dba.AddWithValue("uname", UserName);
            dba.AddWithValue("qname", QueryName);
            new DBHelper(dba).DoNonQuery();
            FlushCachedQueries();

            // Delete any sharekeys that reference this too
            ShareKey.DeleteForQueryName(UserName, QueryName);
        }

        /// <summary>
        /// Returns the specified named query for the specified named user, or null if not found.  Case sensitive.
        /// </summary>
        /// <param name="szUser"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static CannedQuery QueryForUser(string szUser, string name)
        {
            if (String.IsNullOrEmpty(name))
                return null;
            return new List<CannedQuery>(QueriesForUser(szUser)).FirstOrDefault(cq => cq.QueryName.CompareCurrentCulture(name) == 0);
        }
        #endregion

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            CannedQuery cq = obj as CannedQuery;
            return (UserName + QueryName).CompareCurrentCultureIgnoreCase(cq.UserName + cq.QueryName);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 1285204636;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(UserName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(QueryName);
                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is null || !(obj is CannedQuery))
                return false;

            return CompareTo(obj) == 0;
        }

        public static bool operator ==(CannedQuery left, CannedQuery right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(CannedQuery left, CannedQuery right)
        {
            return !(left == right);
        }

        public static bool operator <(CannedQuery left, CannedQuery right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(CannedQuery left, CannedQuery right)
        {
            return 
                left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(CannedQuery left, CannedQuery right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(CannedQuery left, CannedQuery right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion

        #region Matching LogbookEntry Objects
        #region Handling conjuctives
        private static void AddResultIfCondition(List<bool> lst, bool condition, bool result)
        {
            if (condition)
                lst.Add(result);
        }

        private static bool TestConditionsForConjunction(GroupConjunction groupConjunction, List<bool> lst)
        {
            // empty list is always a match
            if (lst == null || lst.Count == 0)
                return true;

            // count the number of true elements
            int cTrue = 0;
            lst.ForEach((b) => { if (b) cTrue++; });

            switch (groupConjunction)
            {
                case GroupConjunction.All:
                    return cTrue == lst.Count;
                case GroupConjunction.Any:
                    return cTrue > 0;
                case GroupConjunction.None:
                    return cTrue == 0;
                default:
                    throw new InvalidOperationException("Unknown conjunction: " + groupConjunction.ToString());
            }
        }
        #endregion

        private bool IsDateMatch(LogbookEntryCore le)
        {
            switch (DateRange)
            {
                case DateRanges.AllTime:
                    return true;
                case DateRanges.Tailing6Months:
                    return le.Date.CompareTo(DateTime.Now.AddMonths(-6)) >= 0;
                case DateRanges.Trailing12Months:
                    return le.Date.CompareTo(DateTime.Now.AddYears(-1)) >= 0;
                case DateRanges.Trailing30:
                    return le.Date.CompareTo(DateTime.Now.AddDays(-30)) >= 0;
                case DateRanges.Trailing90:
                    return le.Date.CompareTo(DateTime.Now.AddDays(-90)) >= 0;
                case DateRanges.YTD:
                    return le.Date.CompareTo(dtStartOfYear) >= 0;
                case DateRanges.ThisMonth:
                    return le.Date.CompareTo(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)) >= 0;
                case DateRanges.PrevMonth:
                    DateTime dtPrevMonthEnd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);
                    DateTime dtPrevMonthStart = new DateTime(dtPrevMonthEnd.Year, dtPrevMonthEnd.Month, 1);
                    return le.Date.CompareTo(dtPrevMonthStart.Date) >= 0 && le.Date.Date.CompareTo(dtPrevMonthEnd.Date) <= 0;
                case DateRanges.PrevYear:
                    DateTime dtPrevYearStart = new DateTime(DateTime.Now.Year - 1, 1, 1);
                    DateTime dtPrevYearEnd = new DateTime(DateTime.Now.Year - 1, 12, 31);
                    return le.Date.CompareTo(dtPrevYearStart.Date) >= 0 && le.Date.Date.CompareTo(dtPrevYearEnd.Date) <= 0;
                case DateRanges.Custom:
                    bool fHasStartDate = DateMin.HasValue();
                    bool fHasEndDate = DateMax.HasValue();
                    if (!fHasStartDate && fHasEndDate)
                        return le.Date.Date.CompareTo(DateMax.Date) <= 0;
                    else if (fHasStartDate & !fHasEndDate)
                        return le.Date.Date.CompareTo(DateMin.Date) >= 0;
                    else
                        return le.Date.Date.CompareTo(DateMin.Date) >= 0 && le.Date.Date.CompareTo(DateMax.Date) <= 0;
                default:
                    throw new MyFlightbookValidationException("Unknown date range!");
            }
        }

        private bool IsAircraftMatch(LogbookEntryCore le)
        {
            return AircraftList.Count == 0 || (AircraftList.FirstOrDefault(ac => ac.AircraftID == le.AircraftID) != null);
        }

        public bool IsAirportMatch(LogbookEntryCore le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            if (String.IsNullOrWhiteSpace(le.Route) && (Distance != FlightDistance.AllFlights || AirportList.Count != 0))
                return false;

            string szRoute = le.Route.ToUpper(CultureInfo.CurrentCulture).Trim();

            switch (Distance)
            {
                case FlightDistance.AllFlights:
                    break;
                case FlightDistance.LocalOnly:
                    if (!RegexUtility.LocalFlight.IsMatch(szRoute))
                        return false;
                    break;
                case FlightDistance.NonLocalOnly:
                    if (RegexUtility.LocalFlight.IsMatch(szRoute))
                        return false;
                    break;
                default:
                    break;
            }

            foreach (string szAirport in AirportList)
            {
                if (String.IsNullOrWhiteSpace(szAirport))
                    continue;

                string szTerm = szAirport.ToUpper(CultureInfo.CurrentCulture).Replace(SearchFullStopAnchor, string.Empty);
                string szAltTerm = Airports.airport.USPrefixConvenienceAlias(szTerm);

                if (szAirport.StartsWith(SearchFullStopAnchor, StringComparison.CurrentCultureIgnoreCase) && !szRoute.StartsWith(szTerm) && !szRoute.StartsWith(szAltTerm))
                    return false;
                else if (szAirport.EndsWith(SearchFullStopAnchor, StringComparison.CurrentCultureIgnoreCase) && !szRoute.EndsWith(szTerm) && !szRoute.EndsWith(szAltTerm))
                    return false;
                else if (!szRoute.Contains(szTerm) && !szRoute.Contains(szAltTerm))
                    return false;
            }

            return true;
        }

        private bool IsModelMatch(LogbookEntry le)
        {
            if (MakeList.Count != 0 && MakeList.FirstOrDefault(mm => le.ModelID == mm.MakeModelID) == null)
                return false;

            string[] rgModelFragment = RegexUtility.ModelFragmentBoundary.Split(ModelName);
            foreach (string sz in rgModelFragment)
            {
                string szSearch = sz.Trim().ToUpper(CultureInfo.CurrentCulture);
                bool fICAO = szSearch.StartsWith(szICAOPrefix, StringComparison.CurrentCultureIgnoreCase);
                if (fICAO)
                    szSearch = szSearch.Substring(szICAOPrefix.Length).Trim();
                if (szSearch.Length > 0)
                {
                    // ICAO has to match the icao, exactly
                    if (fICAO && le.FamilyName.CompareCurrentCultureIgnoreCase(szSearch) != 0)
                        return false;
                    // Otherwise, must contain the words
                    if (!le.FamilyName.Contains(szSearch) && !le.ModelDisplay.ToUpper(CultureInfo.CurrentCulture).Contains(szSearch))
                        return false;
                }
            }

            if (TypeNames.Count != 0)
            {
                // Need to get the actual model to do this.
                MakeModel mm = MakeModel.GetModel(le.ModelID);
                if (String.IsNullOrEmpty(TypeNames.ElementAt(0)) && !String.IsNullOrWhiteSpace(mm.TypeName))
                    return false;

                foreach (string szType in TypeNames)
                    if (!String.IsNullOrWhiteSpace(szType) && !mm.TypeName.ToUpperInvariant().Contains(szType.ToUpperInvariant()))
                        return false;
            }

            return true;
        }

        private bool IsInstanceTypeMatch(Aircraft ac)
        {
            switch (AircraftInstanceTypes)
            {
                default:
                case AircraftInstanceRestriction.AllAircraft:
                    break;
                case AircraftInstanceRestriction.RealOnly:
                    if (ac.InstanceType != MyFlightbook.AircraftInstanceTypes.RealAircraft)
                        return false;
                    break;
                case AircraftInstanceRestriction.TrainingOnly:
                    if (ac.InstanceType == MyFlightbook.AircraftInstanceTypes.RealAircraft)
                        return false;
                    break;
            }
            return true;
        }

        private bool IsEngineTypeMatch(MakeModel mm, LogbookEntry le)
        {
            if (CategoryClass.IsPowered(le.IsOverridden ? (CategoryClass.CatClassID)le.CatClassOverride : mm.CategoryClassID) || mm.IsMotorGlider)
            {
                switch (EngineType)
                {
                    default:
                    case EngineTypeRestriction.AllEngines:
                        return true;
                    case EngineTypeRestriction.AnyTurbine:
                        return mm.EngineType == MakeModel.TurbineLevel.Jet || mm.EngineType == MakeModel.TurbineLevel.UnspecifiedTurbine || mm.EngineType == MakeModel.TurbineLevel.TurboProp;
                    case EngineTypeRestriction.Piston:
                        return mm.EngineType == MakeModel.TurbineLevel.Piston;
                    case EngineTypeRestriction.Turboprop:
                        return mm.EngineType == MakeModel.TurbineLevel.TurboProp;
                    case EngineTypeRestriction.Jet:
                        return mm.EngineType == MakeModel.TurbineLevel.Jet;
                    case EngineTypeRestriction.Electric:
                        return mm.EngineType == MakeModel.TurbineLevel.Electric;
                }
            }
            else
                return EngineType == EngineTypeRestriction.AllEngines;  // if unpowered, then it's not a match unless "allengines" is specified; issue #1305
        }

        private bool IsAircraftCharacteristicsMatch(LogbookEntry le)
        {
            UserAircraft ua = new UserAircraft(le.User);
            Aircraft ac = ua.GetUserAircraftByID(le.AircraftID);

            if (!IsInstanceTypeMatch(ac))
                return false;

            MakeModel mm = MakeModel.GetModel(ac.ModelID);

            List<bool> lst = new List<bool>();
            AddResultIfCondition(lst, IsTailwheel, mm.IsTailWheel); 
            AddResultIfCondition(lst, IsHighPerformance, mm.IsHighPerf || (mm.Is200HP && le.Date.CompareTo(Convert.ToDateTime(MakeModel.Date200hpHighPerformanceCutoverDate, CultureInfo.InvariantCulture)) < 0));
            AddResultIfCondition(lst, IsGlass, mm.AvionicsTechnology != MakeModel.AvionicsTechnologyType.None || (ac.AvionicsTechnologyUpgrade != MakeModel.AvionicsTechnologyType.None && le.Date.CompareTo(ac.GlassUpgradeDate ?? DateTime.MinValue) >= 0));
            AddResultIfCondition(lst, IsTechnicallyAdvanced, mm.AvionicsTechnology == MakeModel.AvionicsTechnologyType.TAA || (ac.AvionicsTechnologyUpgrade == MakeModel.AvionicsTechnologyType.TAA && le.Date.CompareTo(ac.GlassUpgradeDate ?? DateTime.MinValue) >= 0));
            AddResultIfCondition(lst, IsComplex, mm.IsComplex);
            AddResultIfCondition(lst,IsMotorglider, mm.IsMotorGlider);
            AddResultIfCondition(lst, IsMultiEngineHeli, mm.IsMultiEngineHelicopter);
            AddResultIfCondition(lst, HasFlaps, mm.HasFlaps);
            AddResultIfCondition(lst, IsConstantSpeedProp, mm.IsConstantProp);
            AddResultIfCondition(lst, IsRetract, mm.IsRetract);

            if (!TestConditionsForConjunction(GroupConjunction.All, lst))
                return false;

            if (!IsEngineTypeMatch(mm, le))
                return false;

            // Do category class here, since we have aircraft already loaded
            if (CatClasses != null && CatClasses.Count != 0 && !CatClasses.Contains(CategoryClass.CategoryClassFromID(le.IsOverridden ? (CategoryClass.CatClassID)le.CatClassOverride : mm.CategoryClassID)))
                return false;

            return true;
        }

        private bool IsFlightCharacteristicsMatch(LogbookEntryBase le)
        {
            List<bool> lst = new List<bool>();
            AddResultIfCondition(lst, HasNightLandings, le.NightLandings != 0);
            AddResultIfCondition(lst, HasFullStopLandings, le.FullStopLandings != 0);
            AddResultIfCondition(lst, HasLandings, le.Landings != 0);
            AddResultIfCondition(lst, HasApproaches, le.Approaches != 0);
            AddResultIfCondition(lst, HasHolds, le.fHoldingProcedures);
            AddResultIfCondition(lst, HasXC, le.CrossCountry != 0);
            AddResultIfCondition(lst, HasSimIMCTime, le.SimulatedIFR != 0);
            AddResultIfCondition(lst, HasGroundSim, le.GroundSim != 0);
            AddResultIfCondition(lst, HasIMC, le.IMC != 0);
            AddResultIfCondition(lst, HasAnyInstrument, le.IMC + le.SimulatedIFR > 0);
            AddResultIfCondition(lst, HasNight, le.Nighttime != 0);
            AddResultIfCondition(lst, HasDual, le.Dual != 0);
            AddResultIfCondition(lst, HasCFI, le.CFI != 0);
            AddResultIfCondition(lst, HasSIC, le.SIC != 0);
            AddResultIfCondition(lst, HasPIC, le.PIC != 0);
            AddResultIfCondition(lst, HasTotalTime, le.TotalFlightTime != 0);
            AddResultIfCondition(lst, IsPublic, le.fIsPublic);
            AddResultIfCondition(lst, IsSigned, le.CFISignatureState != LogbookEntryCore.SignatureState.None);
            AddResultIfCondition(lst, HasTelemetry, le.HasFlightData);
            AddResultIfCondition(lst, HasImages, le.FlightImages.Count != 0);

            return TestConditionsForConjunction(FlightCharacteristicsConjunction, lst);
        }

        private bool IsPropertiesMatch(LogbookEntryCore le)
        {
            if (PropertyTypes == null || PropertyTypes.Count == 0)
                return true;

            List<bool> lst = new List<bool>();
            foreach (CustomPropertyType cpt in PropertyTypes)
                AddResultIfCondition(lst, true, le.CustomProperties.GetEventWithTypeID(cpt.PropTypeID) != null);

            return TestConditionsForConjunction(PropertiesConjunction, lst);
        }

        private bool IsEnumerated(LogbookEntryCore le)
        {
            return (EnumeratedFlights == null || EnumeratedFlights.Count == 0 || EnumeratedFlights.Contains(le.FlightID));
        }

        private bool IsGeneralTextMatch(LogbookEntry le)
        {
            // This is the most complicated to match what the database does.
            if (String.IsNullOrWhiteSpace(GeneralText))
                return true;

            string szGeneral = GeneralText; // so that we can modify this without modifying the underlying query.

            // First, look for trailing ##.
            MatchCollection mcTrailing = RegexUtility.QueryTrailingDate.Matches(szGeneral);
            if (mcTrailing.Count > 0)
            {
                string szType = mcTrailing[0].Groups["rangetype"].Value;
                if (int.TryParse(mcTrailing[0].Groups["quantity"].Value, out int count))
                {
                    switch (szType.ToUpperInvariant())
                    {
                        case "D":
                            if (le.Date.Date.CompareTo(DateTime.Now.Date.AddDays(-count)) < 0)
                                return false;
                            break;
                        case "CM":
                            if (le.Date.Date.CompareTo(DateTime.Now.Date.AddCalendarMonths(-count)) < 0)
                                return false;
                            break;
                        case "M":
                            if (le.Date.Date.CompareTo(DateTime.Now.Date.AddMonths(-count)) < 0)
                                return false;
                            break;
                        case "W":
                            if (le.Date.Date.CompareTo(DateTime.Now.Date.AddDays(-count * 7)) < 0)
                                return false;
                            break;
                    }

                    // Now remove this from szGeneral because we're not actually searching for the text.
                    szGeneral = RegexUtility.QueryTrailingDate.Replace(szGeneral, string.Empty);
                }
            }

            UserAircraft ua = new UserAircraft(le.User);
            Aircraft ac = ua.GetUserAircraftByID(le.AircraftID);

            // OK, now we're on to general search.  First, generate - once - the string to search
            string szMatch = le.SearchStringForFlight(ac.PrivateNotes);

            // Split phrases at OR, everything within them is an AND.  E.g., "cat dog or horse buggy" should be treated as "cat and dog" OR "horse and buggy".
            // So each PHRASE is an "AND", and the phrases themselves will be OR'd together.
            string[] phrases = RegexUtility.QueryORPhrases.Split(GeneralText.Trim()).Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();

            // No phrases = no condition = call it a match
            if (phrases.Length == 0)
                return true;

            foreach (string phrase in phrases)
            {
                MatchCollection mTerms = RegexUtility.QuerySearchTerms.Matches(phrase);
                bool matchesWholePhrase = true;
                foreach (Match mTerm in mTerms)
                {
                    string szTerm = mTerm.Groups["term"].Value.Trim();
                    bool fNegate = !String.IsNullOrEmpty(mTerm.Groups["negate"].Value);

                    if (string.IsNullOrWhiteSpace(szTerm))
                        continue;

                    if (!(fNegate ^ IsPhraseMatch(szTerm, szMatch, le)))
                    {
                        matchesWholePhrase = false;
                        break;
                    }
                }

                if (matchesWholePhrase)
                    return true;
            }

            return false;
        }

        private static bool IsPhraseMatch(string szPhrase, string szMatch, LogbookEntryBase le)
        {
            if (String.IsNullOrEmpty(szMatch))
                return false;

            // Issue #802 - Quick hack to direct a specific query to comment or route
            MatchCollection mcSpecific = RegexUtility.QuerySpecificField.Matches(szPhrase);
            if (mcSpecific.Count > 0)    // user is requesting a specific match on a specific field
            {
                GroupCollection gc = mcSpecific[0].Groups;
                string szValue = gc["value"].Value.Trim();
                // Convert the wildcards to something unique, escape, the put the wildcards back
                string rMatch = String.IsNullOrEmpty(szValue) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "^{0}$", szValue.ConvertToRegexWildcards());
                string szTest;
                switch (gc["field"].Value.ToUpperInvariant())
                {
                    case RegexUtility.szPrefixLimitComments:
                        szTest = le.Comment;
                        break;
                    case RegexUtility.szPrefixLimitRoute:
                        szTest = le.Route;
                        break;
                    default:
                        throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Unknown prefix '{0}' matched RegexUtility.QuerySpecificField", gc["field"].Value));
                }
                return Regex.IsMatch(szTest.Trim(), rMatch, RegexOptions.IgnoreCase);
            }
            else
                return Regex.IsMatch(szMatch.Trim(), szPhrase.ConvertToRegexWildcards(), RegexOptions.IgnoreCase);
        }

        public bool MatchesFlight(LogbookEntry le)
        {
            if (le == null)
                return false;

            return IsEnumerated(le) &&
                IsAircraftMatch(le) && 
                IsPropertiesMatch(le) &&
                IsDateMatch(le) &&
                IsAirportMatch(le) &&
                IsGeneralTextMatch(le) && 
                IsFlightCharacteristicsMatch(le) &&
                IsAircraftCharacteristicsMatch(le) &&
                IsModelMatch(le);
        }
        #endregion
    }

    /// <summary>
    /// Represents an element of the flight query object which contains a description and a value which can be removed
    /// </summary>
    [Serializable]
    public class QueryFilterItem
    {
        #region Properties
        /// <summary>
        /// The human-readable description shown to the user
        /// </summary>
        public string FilterName { get; set; }

        /// <summary>
        /// The value of the 
        /// </summary>
        public string FilterValue { get; set; }

        public string PropertyName { get; set; }
        #endregion

        public QueryFilterItem()
        {
            FilterName = FilterValue = PropertyName = string.Empty;
        }

        public QueryFilterItem(string szFName, string szFValue, string szPropName)
        {
            FilterName = szFName;
            FilterValue = szFValue;
            PropertyName = szPropName;
        }
    }

    public class FilterItemClickedEventArgs
        : EventArgs
    {
        public QueryFilterItem FilterItem { get; set; }

        public FilterItemClickedEventArgs(QueryFilterItem qfi = null)
            : base()
        {
            FilterItem = qfi ?? new QueryFilterItem();
        }
    }
}