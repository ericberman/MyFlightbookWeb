using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2009-2019 MyFlightbook LLC
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
        public CategoryClass[] CatClasses { get; set; }

        /// <summary>
        /// The requested property types
        /// </summary>
        public CustomPropertyType[] PropertyTypes { get; set; }

        /// <summary>
        /// Properties to search - AND?  OR? NOT?  Default is OR ("ANY")
        /// </summary>
        public GroupConjunction PropertiesConjunction { get; set; }

        /// <summary>
        /// The user for whom flights are being found
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Find public flights?
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Any additional custom restriciton, suitable for inclusion in a "WHERE" clause
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [JsonIgnoreAttribute]
        public string CustomRestriction { get; set; }

        /// <summary>
        /// Flight Characteristics - AND?  OR?  NOT?  Default is AND ("ALL")
        /// </summary>
        public GroupConjunction FlightCharacteristicsConjunction { get; set; }

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
        public Aircraft[] AircraftList { get; set; }

        /// <summary>
        /// Much smaller list of aircraft ID's, rather than full aircraft (significantly reduces JSON size)
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public IEnumerable<int> AircraftIDList
        {
            get
            {
                List<int> lst = new List<int>();
                if (AircraftList == null || AircraftList.Length == 0)
                {
                    AircraftList = new Aircraft[0];
                    return null;
                }
                    
                foreach (Aircraft ac in AircraftList)
                    lst.Add(ac.AircraftID);
                return lst.ToArray();
            }
            set
            {
                if (value == null)
                {
                    AircraftList = new Aircraft[0];
                    return;
                }
                List<Aircraft> lst = new List<Aircraft>();
                if (String.IsNullOrEmpty(UserName)) // no user - hit the database
                    AircraftList = Aircraft.AircraftFromIDs(value).ToArray();
                else
                {
                    UserAircraft ua = new UserAircraft(UserName);
                    if (value != null)
                        foreach (int id in value)
                            lst.Add(ua.GetUserAircraftByID(id));
                    lst.RemoveAll(ac => ac == null);
                    AircraftList = lst.ToArray();
                }
            }
        }

        /// <summary>
        /// List of matching airports for the matching flights
        /// </summary>
        public string[] AirportList { get; set; }

        /// <summary>
        /// List of matching makes/models for the matching flights
        /// </summary>
        [JsonIgnore]
        public MakeModel[] MakeList { get; set; }

        /// <summary>
        /// Much smaller list of model ID's, rather than full models (significantly reduces JSON size)
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public IEnumerable<int> MakeIDList
        {
            get
            {
                if (MakeList == null || MakeList.Length == 0)
                    return null;

                List<int> lst = new List<int>();
                foreach (MakeModel m in MakeList)
                    lst.Add(m.MakeModelID);
                return lst.ToArray();
            }
            set
            {
                List<MakeModel> lst = new List<MakeModel>();
                if (value != null)
                    foreach (int i in value)
                        lst.Add(MakeModel.GetModel(i));
                MakeList = lst.ToArray();
            }
        }

        /// <summary>
        /// Text contained in model name
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Text of type names - not contains, must match.
        /// </summary>
        public string[] TypeNames { get; set; }

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
        protected bool HasAircraftFeatures
        {
            get
            {
                return (AircraftInstanceTypes != AircraftInstanceRestriction.AllAircraft) || (EngineType != EngineTypeRestriction.AllEngines) ||
                    IsComplex || HasFlaps || IsHighPerformance || IsConstantSpeedProp || IsRetract || IsTailwheel || IsGlass || IsTechnicallyAdvanced || IsMotorglider || IsMultiEngineHeli;
            }
            set
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
        protected bool HasFlightFeatures
        {
            get
            {
                return HasNightLandings || HasFullStopLandings || HasLandings || HasApproaches || HasHolds || HasXC || HasSimIMCTime || HasGroundSim || HasIMC || HasAnyInstrument || HasNight || HasDual ||
                    HasCFI || HasSIC || HasPIC || HasTotalTime || IsPublic || IsSigned || HasTelemetry || HasImages;
            }
            set
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
        public bool IsDefault
        {
            get { return JsonConvert.SerializeObject(this).CompareCurrentCultureIgnoreCase(JsonConvert.SerializeObject(new FlightQuery(this.UserName))) == 0; }
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

        /// <summary>
        /// Returns the flight query in JSON format
        /// </summary>
        public string ToJSONString()
        {
            // Nullify any empty strings or empty arrays or unused values.  Can't rely on "ShouldSerializeXXXX" methods because that disables the XML serialization as well.
            FlightQuery fqNew = new FlightQuery(this);
            if (String.IsNullOrEmpty(fqNew.GeneralText))
                fqNew.GeneralText = null;
            if (String.IsNullOrEmpty(fqNew.ModelName))
                fqNew.ModelName = null;
            if (fqNew.CatClasses == null || fqNew.CatClasses.Length == 0)
                fqNew.CatClasses = null;
            if (fqNew.AircraftIDList == null || fqNew.AircraftIDList.Count() == 0)
                fqNew.AircraftIDList = null;
            if (fqNew.AirportList == null || fqNew.AirportList.Length == 0)
                fqNew.AirportList = null;
            if (fqNew.MakeIDList == null || fqNew.MakeIDList.Count() == 0)
                fqNew.MakeIDList = null;
            if (fqNew.TypeNames == null || fqNew.TypeNames.Length == 0)
                fqNew.TypeNames = null;

            if (fqNew.PropertyTypes == null || fqNew.PropertyTypes.Length == 0)
                fqNew.PropertyTypes = null;
            else
            {
                fqNew.PropertyTypes = new CustomPropertyType[PropertyTypes.Length];
                for (int i = 0; i < fqNew.PropertyTypes.Length; i++)
                {
                    CustomPropertyType cpt = new CustomPropertyType();
                    util.CopyObject(PropertyTypes[i], cpt);
                    cpt.Description = cpt.FormatString = null;
                    cpt.PreviousValues = null;
                    fqNew.PropertyTypes[i] = cpt;
                }
            }
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
        /// Returns the base64 encoded compressed JSON representation of the query
        /// </summary>
        public string ToBase64CompressedJSONString()
        {
            return Convert.ToBase64String(ToCompressedJSONString());
        }

        protected static FlightQuery FromJSON(string szJSON)
        {
            return JsonConvert.DeserializeObject<FlightQuery>(szJSON, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore });
        }

        protected static FlightQuery FromCompressedJSON(byte[] rgJSON)
        {
            return FromJSON(rgJSON.Uncompress());
        }

        public static FlightQuery FromBase64CompressedJSON(string sz)
        {
            return FromCompressedJSON(Convert.FromBase64String(sz));
        }
        #endregion

        [NonSerialized]
        private string szRestrict = string.Empty;
        [NonSerialized]
        private string szHaving = string.Empty;
        [NonSerialized]
        private List<MySqlParameter> m_rgParams = null;

        private DateTime dtStartOfYear { get { return new DateTime(DateTime.Now.Year, 1, 1); } }

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
        protected List<QueryFilterItem> Filters { get; set; }

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
            AircraftList = new Aircraft[0];
            AirportList = new string[0];
            MakeList = new MakeModel[0];
            TypeNames = new string[0];

            DateRange = DateRanges.AllTime;
            m_rgParams = new List<MySqlParameter>();
            CatClasses = new CategoryClass[0];
            PropertyTypes = new CustomPropertyType[0];

            FlightCharacteristicsConjunction = GroupConjunction.All;
            PropertiesConjunction = GroupConjunction.Any;

            EngineType = EngineTypeRestriction.AllEngines;
            AircraftInstanceTypes = AircraftInstanceRestriction.AllAircraft;
        }

        /// <summary>
        /// Creates a new flight query using an existing one as an initializer
        /// </summary>
        /// <param name="q">The query from which to initialize</param>
        public FlightQuery(FlightQuery q) : this()
        {
            util.CopyObject(q, this);
        }

        /// <summary>
        /// Conditionally adds a restriction clause to the passed stringbuilder object.  Includes the "AND" clause if a restriction already has been started.
        /// </summary>
        /// <param name="sb">The stringbuilder to add to</param>
        /// <param name="szClause">The clause to add</param>
        /// <param name="f">True to add it, false to do nothing</param>
        /// <param name="conjunction">Conjunction to use (default to "ALL" (AND))</param>
        private void AddClause(StringBuilder sb, string szClause, Boolean f = true, GroupConjunction conjunction = GroupConjunction.All)
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

        private void AppendIfChecked(StringBuilder sb, Boolean f, string sz)
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
            if (GeneralText.Length > 0)
            {
                Regex rg = new Regex(" ");
                string[] words = rg.Split(GeneralText);

                if (words.Length > 0)
                {
                    int iWord = 0;
                    foreach (string word in words)
                    {
                        if (word.Trim().Length > 0)
                        {
                            string szParam = "SearchWord" + (iWord++).ToString(CultureInfo.InvariantCulture);

                            AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, "CONCAT_WS(' ', ModelDisplay, TailNumber, Route, Comments, CustomProperties, CFIComment, CFIName, AircraftPrivateNotes) LIKE ?{0} ", szParam));
                            AddParameter(szParam, String.Format(CultureInfo.InvariantCulture, "%{0}%", word.Trim()));
                        }
                    }

                    Filters.Add(new QueryFilterItem(Resources.FlightQuery.ContainsText, GeneralText, "GeneralText"));
                }
            }
        }

        private void UpdateAircraft(StringBuilder sbQuery)
        {
            if (AircraftList.Length > 0)
            {
                StringBuilder sbAircraftID = new StringBuilder("flights.idaircraft IN (");
                StringBuilder sbAircraftDesc = new StringBuilder();

                for (int i = 0; i < AircraftList.Length; i++)
                {
                    if (i > 0)
                    {
                        sbAircraftID.Append(", ");
                        sbAircraftDesc.Append(", ");
                    }

                    sbAircraftID.Append(AircraftList[i].AircraftID.ToString(CultureInfo.InvariantCulture));
                    sbAircraftDesc.Append(AircraftList[i].DisplayTailnumber);
                }
                sbAircraftID.Append(") ");

                AddClause(sbQuery, sbAircraftID.ToString());
                Filters.Add(new QueryFilterItem(Resources.FlightQuery.ContainsAircraft, sbAircraftDesc.ToString(), "AircraftList"));
            }
        }
        
        private void UpdateAirports(StringBuilder sbQuery)
        {
            switch (Distance)
            {
                case FlightDistance.AllFlights:
                    break;
                case FlightDistance.LocalOnly:
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " ((LENGTH(Route) <= {0}) OR (Route IS NULL) OR (Route RLIKE CONCAT('^', LEFT(Route, 4), '[^a-zA-Z0-9]*', LEFT(Route, 4), '$') OR Route RLIKE CONCAT('^', LEFT(Route, 3), '[^a-zA-Z0-9]*', LEFT(Route, 3), '$'))) ", MyFlightbook.Airports.airport.maxCodeLength));
                    Filters.Add(new QueryFilterItem(Resources.FlightQuery.FilterFlightRange, Resources.FlightQuery.FlightRangeLocal, "Distance"));
                    break;
                case FlightDistance.NonLocalOnly:
                    // Query here is for route length greater than the length of a single airport, but we also ad a hack to look for "ABC ABC" or "ABCD-ABCD" 
                    // (i.e., 3- or 4- characters
                    AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, " LENGTH(Route) > {0}  AND NOT (Route RLIKE CONCAT('^', LEFT(Route, 4), '[^a-zA-Z0-9]*', LEFT(Route, 4), '$') OR Route RLIKE CONCAT('^', LEFT(Route, 3), '[^a-zA-Z0-9]*', LEFT(Route, 3), '$'))", Airports.airport.maxCodeLength));
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

                    sbAirports.Append(String.Format(CultureInfo.InvariantCulture, "(flights.Route LIKE ?{0})", szParam));
                    string szCode = lstAirports[i];
                    bool fIsDepart = szCode.StartsWith(SearchFullStopAnchor, StringComparison.CurrentCultureIgnoreCase);
                    bool fIsArrival = szCode.EndsWith(SearchFullStopAnchor, StringComparison.CurrentCultureIgnoreCase);
                    string szNormalAirport = Airports.airport.USPrefixConvenienceAlias(szCode.Replace(SearchFullStopAnchor, string.Empty));
                    string szParamValue = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", fIsDepart ? string.Empty : "%", szNormalAirport, fIsArrival ? string.Empty : "%");
                    AddParameter(szParam, szParamValue);

                    sbAirportDesc.Append(lstAirports[i] + Resources.LocalizedText.LocalizedSpace);
                }

                sbAirports.Append(")");
                AddClause(sbQuery, sbAirports.ToString());

                Filters.Add(new QueryFilterItem(Resources.FlightQuery.ContainsAirports, sbAirportDesc.ToString().Trim(), "AirportList"));
            }
        }

        private void UpdateModels(StringBuilder sbQuery)
        {
            StringBuilder sbModelsQuery = new StringBuilder();
            string szModelsIDQuery = string.Empty;
            string szModelsTextQuery = string.Empty;
            string szModelsTypeQuery = string.Empty;

            if (MakeList.Length > 0)
            {
                StringBuilder sbModelID = new StringBuilder("models.idmodel IN (");
                StringBuilder sbDescMakes = new StringBuilder();

                for (int i = 0; i < MakeList.Length; i++)
                {
                    if (i > 0)
                    {
                        sbModelID.Append(", ");
                        sbDescMakes.Append(", ");
                    }

                    sbModelID.Append(MakeList[i].MakeModelID.ToString(CultureInfo.InvariantCulture));

                    sbDescMakes.Append(MakeList[i].DisplayName.Trim());
                }
                sbModelID.Append(") ");
                szModelsIDQuery = sbModelID.ToString();

                Filters.Add(new QueryFilterItem(Resources.FlightQuery.ContainsMakeModel, sbDescMakes.ToString(), "MakeList"));
            }

            if (!String.IsNullOrEmpty(ModelName.Trim()))
            {
                string[] rgModelFragment = Regex.Split(ModelName, "[^a-zA-Z0-9]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                int i = 0;
                StringBuilder sbModelName = new StringBuilder();
                foreach (string sz in rgModelFragment)
                {
                    if (sz.Trim().Length > 0)
                    {
                        string szParamName = String.Format(CultureInfo.InvariantCulture, "?modelQuery{0}", i++);
                        AddParameter(szParamName, String.Format(CultureInfo.InvariantCulture, "%{0}%", sz));
                        sbModelName.Append(sbModelName.Length == 0 ? "(" : " AND ");
                        sbModelName.AppendFormat(CultureInfo.InvariantCulture, "(ModelDisplay LIKE {0} OR FamilyDisplay LIKE {0}) ", szParamName);
                    }
                }
                if (sbModelName.Length > 0)
                {
                    sbModelName.Append(")");
                    szModelsTextQuery = sbModelName.ToString();
                    Filters.Add(new QueryFilterItem(String.IsNullOrEmpty(szModelsIDQuery) ? Resources.FlightQuery.ContainsMakeModelText : Resources.FlightQuery.ContainsMakeModelTextOR, ModelName, "ModelName"));
                }
            }

            if (TypeNames != null && TypeNames.Length > 0)
            {
                if (TypeNames.Length == 1 && String.IsNullOrEmpty(TypeNames[0]))
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
                        sbTypes.Append(")");
                        szModelsTypeQuery = sbTypes.ToString();
                        Filters.Add(new QueryFilterItem(TypeNames.Length == 1 ? Resources.FlightQuery.ContainsType : Resources.FlightQuery.ContainsTypeMultiple, String.Join(",", TypeNames), "TypeNames"));
                    }
                }

                // Splice the type query into the models query as an OR - i.e., matches EITHER model OR typenames.
                if (String.IsNullOrEmpty(szModelsTextQuery))
                    szModelsTextQuery = szModelsTypeQuery;
                else
                    szModelsTextQuery = String.Format(CultureInfo.InvariantCulture, "({0}) OR {1}", szModelsTextQuery, szModelsTypeQuery);

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
            AddClause(sbQuery, "(flights.idflight IN (SELECT f.idflight FROM flights f INNER JOIN images img ON (img.virtpathid=0 AND img.Imagekey=f.idflight) WHERE f.username=?uname AND img.imagekey IS NOT NULL))", HasImages, FlightCharacteristicsConjunction);

            if (sbQuery.Length > 0)
                AddClause(sbQ, String.Format(CultureInfo.InvariantCulture, "({0}) ", sbQuery.ToString()));

            StringBuilder sbFlight = new StringBuilder();

            AppendIfChecked(sbFlight, HasNightLandings, Resources.FlightQuery.FlightFeatureFSNightLanding);
            AppendIfChecked(sbFlight, HasFullStopLandings, Resources.FlightQuery.FlightFeatureFSLanding);
            AppendIfChecked(sbFlight, HasLandings, Resources.FlightQuery.FlightFeatureAnyLandings);
            AppendIfChecked(sbFlight, HasApproaches, Resources.FlightQuery.FlightFeatureApproaches);
            AppendIfChecked(sbFlight, HasHolds, Resources.FlightQuery.FlightFeatureaHolds);
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
            if (PropertyTypes != null && PropertyTypes.Length > 0)
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
            if (CatClasses.Length > 0)
            {
                StringBuilder sbCatClass = new StringBuilder("(");
                StringBuilder sbDescCatClass = new StringBuilder();

                for (int i = 0; i < CatClasses.Length; i++)
                {
                    if (i > 0)
                    {
                        sbCatClass.Append(", ");
                        sbDescCatClass.Append(", ");
                    }

                    sbCatClass.Append((int)CatClasses[i].IdCatClass);

                    sbDescCatClass.Append(CatClasses[i].CatClass);
                }
                sbCatClass.Append(") ");

                AddClause(sbQuery, String.Format(CultureInfo.InvariantCulture, "((flights.idCatClassOverride = 0 AND models.idcategoryclass IN {0}) OR flights.idCatClassOverride IN {0})", sbCatClass.ToString()));

                Filters.Add(new QueryFilterItem(Resources.FlightQuery.CategoryClass, sbDescCatClass.ToString().Trim(), "CatClasses"));
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

            if (!String.IsNullOrEmpty(CustomRestriction))
                AddClause(sbQuery, CustomRestriction);

            szHaving = sbHaving.ToString();

            szRestrict = sbQuery.ToString();
        }

        /// <summary>
        /// Resets a specified property to its default value (useful for clicking on a filter to delete it.)
        /// Uses reflection
        /// </summary>
        /// <param name="qfi">The QueryFilterItem object (must have a specified valid property name)</param>
        public FlightQuery ClearRestriction(QueryFilterItem qfi)
        {
            if (qfi == null)
                throw new ArgumentNullException("qfi");
            FlightQuery fqBlank = new FlightQuery();

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

            return this;
        }
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

        public CannedQuery(FlightQuery fq, string szName) : base()
        {
            util.CopyObject(fq, this);
            QueryName = szName;
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
                throw new ArgumentNullException("szUser");

            if (String.IsNullOrWhiteSpace(szUser))
                throw new ArgumentOutOfRangeException("szUser");

            Profile pf = Profile.GetUser(szUser);
            List<CannedQuery> lst = (List<CannedQuery>) pf.CachedObject(szUserQueriesKey);
            if (lst != null)
                return lst;
            else
                lst = new List<CannedQuery>();

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
        public void Commit()
        {
            if (String.IsNullOrWhiteSpace(QueryName) || IsDefault)
                return;

            if (String.IsNullOrEmpty(UserName))
                throw new MyFlightbookValidationException("No username provided for canned query!");

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
        }
        #endregion

        #region IComparable
        public int CompareTo(object obj)
        {
            return QueryName.CompareCurrentCultureIgnoreCase(((CannedQuery)obj).QueryName);
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

    public class FilterItemClicked : EventArgs
    {
        public QueryFilterItem FilterItem { get; set; }

        public FilterItemClicked(QueryFilterItem qfi = null)
            : base()
        {
            FilterItem = qfi ?? new QueryFilterItem();
        }
    }

    public class FlightQueryEventArgs : EventArgs
    {
        public FlightQuery Query { get; set; }

        public FlightQueryEventArgs(FlightQuery fq = null) : base()
        {
            Query = fq ?? new FlightQuery();
        }
    }
}