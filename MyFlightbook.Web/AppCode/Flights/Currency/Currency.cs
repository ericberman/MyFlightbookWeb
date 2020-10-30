using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Globalization;
using System.Runtime.Serialization;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2007-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    /// <summary>
    /// Status for currency
    /// </summary>
    public enum CurrencyState { NotCurrent = 0, GettingClose = 1, OK = 2, NoDate = 3 };

    /// <summary>
    /// Bitflags for various currency options which can be selected by the user.
    /// </summary>
    [FlagsAttribute]
    public enum CurrencyOptionFlags
    {
        flagPerModelCurrency = 0x0001,
        flagArmyMDSCurrency = 0x0002,
        flagFAR117DutyTimeCurrency = 0x0004,
        flagFAR135DutyTimeCurrency = 0x0008,
        flagCurrencyExpirationBit1 = 0x0010,
        flagCurrencyExpirationBit2 = 0x0020,
        flagCurrencyExpirationBit3 = 0x0040,
        flagCurrencyExpirationMask = flagCurrencyExpirationBit1 | flagCurrencyExpirationBit2 | flagCurrencyExpirationBit3,
        flagUseLooseIFRCurrency = 0x0080,
        flagShowTotalsPerModel = 0x0100,
        flagUseCanadianCurrencyRules = 0x0200,
        flagUseFAR135_29xStatus = 0x0400,
        flagUseFAR135_26xStatus = 0x0800,
        flagUseLAPLCurrency = 0x1000,
        flagFAR117IncludeAllFlights = 0x2000,
        flagUseFAR61217 = 0x4000,
        flagUseEASAMedical = 0x8000,
        flagsShowTotalsPerFamily = 0x00010000,
        flagSuppressModelFeatureTotals = 0x00020000,
        flagAllowNightTouchAndGo = 0x00040000,
        flagRequireDayLandingsDayCurrency = 0x00080000,
        flagShow2DigitTotals = 0x00100000,
        flagUseFAR125_2xxStatus = 0x00200000
    }

    /// <summary>
    /// Utility class for pruning expired currency items per use preference.
    /// </summary>
    public static class CurrencyExpiration
    {
        public enum Expiration { None, TenYear, FiveYear, ThreeYear, TwoYear, OneYear }

        public static DateTime CutoffDate(Expiration ce)
        {
            switch (ce)
            {
                default:
                case Expiration.None:
                    return DateTime.MinValue;
                case Expiration.OneYear:
                    return DateTime.Now.AddYears(-1);
                case Expiration.TwoYear:
                    return DateTime.Now.AddYears(-2);
                case Expiration.ThreeYear:
                    return DateTime.Now.AddYears(-3);
                case Expiration.FiveYear:
                    return DateTime.Now.AddYears(-5);
                case Expiration.TenYear:
                    return DateTime.Now.AddYears(-10);
            }
        }

        public static string ExpirationLabel(Expiration ce)
        {
            switch (ce)
            {
                default:
                case Expiration.None:
                    return Resources.Currency.currencyExpirationNone;
                case Expiration.OneYear:
                    return Resources.Currency.currencyExpiration1Year;
                case Expiration.TwoYear:
                    return Resources.Currency.currencyExpiration2Years;
                case Expiration.ThreeYear:
                    return Resources.Currency.currencyExpiration3Years;
                case Expiration.FiveYear:
                    return Resources.Currency.currencyExpiration5Years;
                case Expiration.TenYear:
                    return Resources.Currency.currencyExpiration10Years;
            }
        }
    }

    /// <summary>
    /// Represents a given state of currency for a given attribute; fairly generic
    /// </summary>
    [Serializable]
    [DataContract]
    public class CurrencyStatusItem : IComparable, IEquatable<CurrencyStatusItem>
    {
        public enum CurrencyGroups { None, FlightExperience, FlightReview, Aircraft, AircraftDeadline, Certificates, Medical, Deadline, CustomCurrency }

        // Keys for profile associated data
        public const string AssociatedDateKeyExpiringCurrencies = "ExpiringCurrencies";
        public const string AssociatedDataKeyCachedCurrencies = "MostRecentCurrency";

        #region properties
        /// <summary>
        /// The specific currency attribute (e.g., "Instrument flight", "BFR Due," or "VOR Check"
        /// </summary>
        [DataMember]
        public string Attribute { get; set; }

        /// <summary>
        /// The value or description of the state
        /// </summary>
        [DataMember]
        public string Value { get; set; }

        /// <summary>
        /// Everything OK?  Expired?  Close to expiration?
        /// </summary>
        [DataMember]
        public CurrencyState Status { get; set; }

        /// <summary>
        /// What is the gap between current state and some bad state (e.g., how long ago did it expire?  How soon will an inspection be due?
        /// </summary>
        [DataMember]
        public string Discrepancy { get; set; }

        /// <summary>
        /// URL (Link) to the underlying resource or options page
        /// </summary>
        public string AssociatedResourceLink
        {
            get
            {
                string szResult;
                switch (CurrencyGroup)
                {
                    default:
                    case CurrencyGroups.None:
                    case CurrencyGroups.FlightExperience:
                        return Query == null ? null : String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx?fq={0}", HttpUtility.UrlEncode(Query.ToBase64CompressedJSONString()));
                    case CurrencyGroups.FlightReview:
                        szResult = VirtualPathUtility.ToAbsolute("~/Member/EditProfile.aspx/pftPilotInfo?pane=flightreview");
                        break;
                    case CurrencyGroups.Aircraft:
                    case CurrencyGroups.AircraftDeadline:
                        szResult = VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}", AssociatedResourceID));
                        break;
                    case CurrencyGroups.Certificates:
                        szResult = VirtualPathUtility.ToAbsolute("~/Member/EditProfile.aspx/pftPilotInfo?pane=certificates");
                        break;
                    case CurrencyGroups.Medical:
                        szResult = VirtualPathUtility.ToAbsolute("~/Member/EditProfile.aspx/pftPilotInfo?pane=medical");
                        break;
                    case CurrencyGroups.Deadline:
                        szResult = VirtualPathUtility.ToAbsolute("~/Member/EditProfile.aspx/pftPrefs?pane=deadlines");
                        break;
                    case CurrencyGroups.CustomCurrency:
                        szResult = VirtualPathUtility.ToAbsolute(Query == null ? "~/Member/EditProfile.aspx/pftPrefs?pane=custcurrency" : String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx?ft=Totals&fq={0}", HttpUtility.UrlEncode(Query.ToBase64CompressedJSONString())));
                        break;
                }

                return String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Branding.CurrentBrand.HostName, szResult);
            }
        }

        /// <summary>
        /// The ID of the resource to which this is linked (typically aircraft)
        /// </summary>
        [DataMember]
        public int AssociatedResourceID { get; set; }

        /// <summary>
        /// The kind of resource to which this 
        /// </summary>
        [DataMember]
        public CurrencyGroups CurrencyGroup { get; set; }

        /// <summary>
        /// The query that might return matching flights.
        /// </summary>
        [DataMember]
        public FlightQuery Query { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a currency status item in-place
        /// </summary>
        public CurrencyStatusItem()
        {
            Attribute = Value = Discrepancy = string.Empty;
            Status = CurrencyState.OK;
            Query = null;
            AssociatedResourceID = 0;
            CurrencyGroup = CurrencyGroups.None;
        }

        /// <param name="szAttribute">The specific currency attribute (e.g., "Instrument flight," "BFR Due," etc.</param>
        /// <param name="szValue">The value or description of the state</param>
        /// <param name="cs">Everything OK?  Expired?  Close to expiration?</param>
        /// <param name="szDiscrepancy">What is the gap between the current state and some bad state?</param>
        public CurrencyStatusItem(string szAttribute, string szValue, CurrencyState cs, string szDiscrepancy = null) : this()
        {
            Attribute = szAttribute;
            Value = szValue;
            Status = cs;
            Discrepancy = szDiscrepancy;
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} - {1} ({2}, {3})", Attribute, Value, Status.ToString(), Discrepancy);
        }

        /// <summary>
        /// Get the full set of known currencies for the specified user
        /// </summary>
        /// <param name="szUser">Username</param>
        /// <returns>A set of currencystatusitem objects.</returns>
        static public IEnumerable<CurrencyStatusItem> GetCurrencyItemsForUser(string szUser)
        {
            List<CurrencyStatusItem> lst = new List<CurrencyStatusItem>(CurrencyExaminer.ComputeCurrency(szUser));

            List<DeadlineCurrency> deadlines = new List<DeadlineCurrency>(DeadlineCurrency.DeadlinesForUserCurrency(szUser));

            List<DeadlineCurrency> lstNoAircraft = deadlines.FindAll(dc => !dc.HasAssociatedAircraft);
            List<DeadlineCurrency> lstWithAircraft = deadlines.FindAll(dc => dc.HasAssociatedAircraft);
            lst.AddRange(MaintenanceLog.AircraftInspectionWarningsForUser(szUser, lstWithAircraft));
            lst.AddRange(DeadlineCurrency.CurrencyForDeadlines(lstNoAircraft));
            lst.AddRange(Profile.GetUser(szUser).WarningsForUser());
            return lst;
        }

        /// <summary>
        /// Checks to see if the user has any expiring currencies relative to the last time a check was made.
        /// Caches the expiring items and current items (if any expiring items) in the profile cache for quick retrieval
        /// </summary>
        /// <param name="state">Any state returned from the last check</param>
        /// <param name="szUser">The user for whom to check</param>
        /// <param name="newState">Current state</param>
        /// <returns>An enumerable of expiring (newly expired) currencies.  If zero length, then nothing has changed.</returns>
        static public IEnumerable<CurrencyStatusItem> CheckForExpiringCurrencies(string state, string szUser, out string newState)
        {
            if (string.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            Profile pf = Profile.GetUser(szUser);
            if (string.IsNullOrEmpty(pf.UserName))
                throw new MyFlightbookValidationException("No such user: " + szUser);

            IEnumerable<CurrencyStatusItem> priorItems = (state == null) ? Array.Empty<CurrencyStatusItem>() : Newtonsoft.Json.JsonConvert.DeserializeObject<CurrencyStatusItem[]>(state);
            IEnumerable<CurrencyStatusItem> newItems = GetCurrencyItemsForUser(szUser);
            newState = Newtonsoft.Json.JsonConvert.SerializeObject(newItems);

            IEnumerable<CurrencyStatusItem> expiringItems = NeedsNotification(priorItems, newItems);

            if (expiringItems.Any())
            {
                pf.AssociatedData[AssociatedDataKeyCachedCurrencies] = newItems;
                pf.AssociatedData[AssociatedDateKeyExpiringCurrencies] = expiringItems;
            }

            return expiringItems;
        }

        /// <summary>
        /// Compares two sets of currency status items and returns the set of status items that change from OK to nearing expiration, or from either of those states to Expired.
        /// </summary>
        /// <param name="rgcsi1">Set of previous status items</param>
        /// <param name="rgcsi2">Set of current status items</param>
        /// <returns></returns>
        static protected IEnumerable<CurrencyStatusItem> NeedsNotification(IEnumerable<CurrencyStatusItem> rgcsi1, IEnumerable<CurrencyStatusItem> rgcsi2)
        {
            if (rgcsi2 == null)
                throw new ArgumentNullException(nameof(rgcsi2));

            if (!rgcsi2.Any())
                return Array.Empty<CurrencyStatusItem>();

            List<CurrencyStatusItem> lstResult = new List<CurrencyStatusItem>(rgcsi2).FindAll(csi => csi.Status != CurrencyState.OK && csi.Status != CurrencyState.NoDate);

            // quick short circuit - if there's nothing in the previous items, just return the current ones that aren't OK.
            if (rgcsi1 == null || !rgcsi1.Any())
                return lstResult;

            Dictionary<string, CurrencyStatusItem> dict2 = new Dictionary<string, CurrencyStatusItem>();

            foreach (CurrencyStatusItem csi in lstResult)
                dict2[csi.Attribute] = csi;

            // Remove everything from dict2 that has NOT changed state since last time.
            foreach (CurrencyStatusItem csi in rgcsi1)
            {
                if (dict2.TryGetValue(csi.Attribute, out CurrencyStatusItem csi2) && csi2.Status == csi.Status)
                    dict2.Remove(csi.Attribute);
            }
            return dict2.Values;
        }

        #region IComparable

        /// <summary>
        /// When sorting, we can group custom currencies together and we can group aircraft deadlines with aircraft maintenance
        /// </summary>
        /// <param name="cg"></param>
        /// <returns></returns>
        protected static int GroupSortBucket(CurrencyGroups cg)
        {
            switch (cg)
            {
                case CurrencyGroups.None:
                    return 0;
                case CurrencyGroups.FlightExperience:
                case CurrencyGroups.CustomCurrency:
                    return 1;
                case CurrencyGroups.Aircraft:
                case CurrencyGroups.AircraftDeadline:
                    return 2;
                case CurrencyGroups.FlightReview:
                    return 3;
                case CurrencyGroups.Certificates:
                    return 4;
                case CurrencyGroups.Medical:
                    return 5;
                case CurrencyGroups.Deadline:
                    return 6;
                default:
                    return (int)cg;
            }
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (!(obj is CurrencyStatusItem csi))
                throw new InvalidCastException("obj is not currencystatusitem, it is " + obj.GetType().ToString());

            int gspThis = GroupSortBucket(CurrencyGroup);
            int gspThat = GroupSortBucket(csi.CurrencyGroup);
            return gspThis.CompareTo(gspThat);  // don't subsort on name because the ordering of that is fine as is.
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CurrencyStatusItem);
        }

        public bool Equals(CurrencyStatusItem other)
        {
            return other != null &&
                   Attribute == other.Attribute &&
                   Value == other.Value &&
                   Status == other.Status &&
                   Discrepancy == other.Discrepancy &&
                   CurrencyGroup == other.CurrencyGroup;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = -1537879147;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Attribute);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Value);
                hashCode = hashCode * -1521134295 + Status.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Discrepancy);
                hashCode = hashCode * -1521134295 + CurrencyGroup.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(CurrencyStatusItem left, CurrencyStatusItem right)
        {
            return EqualityComparer<CurrencyStatusItem>.Default.Equals(left, right);
        }

        public static bool operator !=(CurrencyStatusItem left, CurrencyStatusItem right)
        {
            return !(left == right);
        }

        public static bool operator <(CurrencyStatusItem left, CurrencyStatusItem right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(CurrencyStatusItem left, CurrencyStatusItem right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(CurrencyStatusItem left, CurrencyStatusItem right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(CurrencyStatusItem left, CurrencyStatusItem right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion
    }

    /// <summary>
    /// Represents a row from a currency query.  Primarily serves to encapsulate data about a flight that is used in currency computations
    /// </summary>
    public class ExaminerFlightRow
    {
        #region Properties
        public DateTime dtFlight { get; set; }
        public int flightID { get; set; }
        public int cLandingsThisFlight { get; set; }
        public int cFullStopLandings { get; set; }
        public int cFullStopNightLandings { get; set; }
        public int cApproaches { get; set; }
        public int cHolds { get; set; }
        public int idModel { get; set; }
        public int idAircraft { get; set; }
        public string szType { get; set; }
        public string szCategory { get; set; }
        public CategoryClass.CatClassID idCatClassOverride { get; set; }
        public string szCatClassType { get; set; }
        public string szCatClassBase { get; set; }
        public string szArmyMDS { get; set; }
        public string szFamily { get; set; }
        public string szModel { get; set; }
        public string Comments { get; set; }
        public string Route { get; set; }
        public Boolean fTailwheel { get; set; }
        public Boolean fNight { get; set; }
        public Boolean fIsCertifiedIFR { get; set; }
        public Boolean fIsCertifiedLanding { get; set; }
        public Boolean fIsFullMotion { get; set; }
        public Boolean fIsATD { get; set; }
        public Boolean fIsFTD { get; set; }
        public Boolean fIsComplex { get; set; }
        public Boolean fIsTAA { get; set; }
        public MakeModel.TurbineLevel turbineLevel { get; set; }
        public Boolean fIsCertifiedSinglePilot { get; set; }
        public Decimal IMC { get; set; }
        public Decimal IMCSim { get; set; }
        public Decimal PIC { get; set; }
        public Decimal SIC { get; set; }
        public Decimal CFI { get; set; }
        public Decimal Total { get; set; }
        public Decimal Dual { get; set; }
        public Decimal GroundSim { get; set; }
        public Decimal Night { get; set; }
        public Decimal XC { get; set; }
        public Boolean fIsRealAircraft { get; set; }
        public Boolean fIsGlider { get; set; }
        public Boolean fMotorGlider { get; set; }
        public Boolean fIsSingleEngine { get; set; }
        public Boolean fIsR44 { get; set; }
        public Boolean fIsR22 { get; set; }
        public DateTime dtEngineStart { get; set; }
        public DateTime dtEngineEnd { get; set; }
        public DateTime dtFlightStart { get; set; }
        public DateTime dtFlightEnd { get; set; }

        private readonly static DateTime s_Aug2018Cutover = new DateTime(2018, 8, 27);
        private readonly static DateTime s_Nov2018Cutover = new DateTime(2018, 11, 26);

        public static DateTime Aug2018Cutover { get { return s_Aug2018Cutover; } }
        public static DateTime Nov2018Cutover { get { return s_Nov2018Cutover; } }

        private readonly CustomPropertyCollection m_flightprops;    // backing list for FlightEvents

        /// <summary>
        /// Set of flightproperties (could include profile events, though these are rare now) for the flight.  Will not be null.
        /// </summary>
        public CustomPropertyCollection FlightProps { get { return m_flightprops; } }
        #endregion

        public ExaminerFlightRow(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));

            dtFlight = (DateTime)dr["date"];
            flightID = Convert.ToInt32(dr["FlightID"], CultureInfo.InvariantCulture);
            cLandingsThisFlight = Convert.ToInt32(dr["cLandings"], CultureInfo.InvariantCulture);
            cFullStopLandings = Convert.ToInt32(dr["cFullStopLandings"], CultureInfo.InvariantCulture);
            cFullStopNightLandings = Convert.ToInt32(dr["cNightLandings"], CultureInfo.InvariantCulture);
            cApproaches = Convert.ToInt32(dr["cInstrumentApproaches"], CultureInfo.InvariantCulture);
            cHolds = Convert.ToInt32(dr["fHold"], CultureInfo.InvariantCulture);
            idModel = Convert.ToInt32(dr["idModel"], CultureInfo.InvariantCulture);
            idAircraft = Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture);
            szType = dr["TypeName"].ToString();
            szCategory = dr["Category"].ToString();
            szCatClassType = dr["CatClassWithType"].ToString();
            szCatClassBase = dr["BaseCatClass"].ToString();
            idCatClassOverride = (CategoryClass.CatClassID)Convert.ToInt32(dr["CatClassOverride"], CultureInfo.InvariantCulture);
            fIsGlider = idCatClassOverride == CategoryClass.CatClassID.Glider;
            szArmyMDS = dr["ArmyMDS"].ToString();
            szModel = dr["model"].ToString();
            szFamily = dr["Family"].ToString();
            if (String.IsNullOrEmpty(szFamily))
                szFamily = szModel;
            Route = dr["Route"].ToString();
            Comments = dr["Comments"].ToString();
            fTailwheel = Convert.ToBoolean(dr["fTailwheel"], CultureInfo.InvariantCulture);
            Night = (Convert.ToDecimal(dr["night"], CultureInfo.InvariantCulture));
            fNight = (Night > 0.0M);
            fIsCertifiedIFR = Convert.ToBoolean(dr["IsCertifiedIFR"], CultureInfo.InvariantCulture);
            fIsCertifiedLanding = Convert.ToBoolean(dr["IsCertifiedLanding"], CultureInfo.InvariantCulture);
            fIsFullMotion = Convert.ToBoolean(dr["IsFullMotion"], CultureInfo.InvariantCulture);
            fIsATD = Convert.ToBoolean(dr["IsATD"], CultureInfo.InvariantCulture);
            fIsFTD = Convert.ToBoolean(dr["IsFTD"], CultureInfo.InvariantCulture);
            fIsComplex = Convert.ToBoolean(dr["fComplex"], CultureInfo.InvariantCulture);
            fIsTAA = Convert.ToBoolean(dr["IsTAA"], CultureInfo.InvariantCulture);
            turbineLevel = (MakeModel.TurbineLevel)Convert.ToInt32(dr["fTurbine"], CultureInfo.InvariantCulture);
            fMotorGlider = Convert.ToBoolean(dr["fMotorGlider"], CultureInfo.InvariantCulture);
            fIsCertifiedSinglePilot = Convert.ToBoolean(dr["fCertifiedSinglePilot"], CultureInfo.InvariantCulture);
            IMC = Convert.ToDecimal(util.ReadNullableField(dr, "IMC", 0.0M), CultureInfo.InvariantCulture);
            IMCSim = Convert.ToDecimal(util.ReadNullableField(dr, "simulatedInstrument", 0.0M), CultureInfo.InvariantCulture);
            PIC = Convert.ToDecimal(dr["PIC"], CultureInfo.InvariantCulture);
            SIC = (Decimal)util.ReadNullableField(dr, "SIC", 0.0M);
            CFI = (Decimal)util.ReadNullableField(dr, "CFI", 0.0M);
            Total = Convert.ToDecimal(dr["totalFlightTime"], CultureInfo.InvariantCulture);
            Dual = Convert.ToDecimal(dr["dualReceived"], CultureInfo.InvariantCulture);
            XC = Convert.ToDecimal(dr["crosscountry"], CultureInfo.InvariantCulture);
            GroundSim = Convert.ToDecimal(util.ReadNullableField(dr, "groundSim", 0.0M), CultureInfo.InvariantCulture);
            fIsRealAircraft = ((AircraftInstanceTypes)Convert.ToInt32(dr["InstanceTypeID"], CultureInfo.InvariantCulture)) == AircraftInstanceTypes.RealAircraft;
            fIsSingleEngine = (String.Compare(szCatClassBase, "ASEL", StringComparison.OrdinalIgnoreCase) == 0 || String.Compare(szCatClassBase, "ASES", StringComparison.OrdinalIgnoreCase) == 0) && fIsCertifiedIFR && fIsCertifiedLanding;
            fIsR22 = Convert.ToBoolean(dr["IsR22"], CultureInfo.InvariantCulture);
            fIsR44 = Convert.ToBoolean(dr["IsR44"], CultureInfo.InvariantCulture);
            dtEngineStart = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "dtEngineStart", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);
            dtEngineEnd = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "dtEngineEnd", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);
            dtFlightStart = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "dtFlightStart", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);
            dtFlightEnd = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "dtFlightEnd", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);

            m_flightprops = new CustomPropertyCollection(CustomFlightProperty.PropertiesFromJSONTuples((string)util.ReadNullableField(dr, "CustomPropsJSON", string.Empty), flightID));
        }
    }

    /// <summary>
    /// Interface for any class that wishes to examine all of a user's flights sequentially
    /// </summary>
    public interface IFlightExaminer
    {
        /// <summary>
        /// Examine a flight for whatever information it has.  Called on each flight sequentially
        /// </summary>
        /// <param name="cfr">The flight to examine</param>
        void ExamineFlight(ExaminerFlightRow cfr);
    }

    /// <summary>
    /// Interface to be implemented by currency objects.  Implements IFlightExaminer, plus the ability to retrieve results.
    /// </summary>
    public interface ICurrencyExaminer : IFlightExaminer
    {
        CurrencyState CurrentState { get; }

        DateTime ExpirationDate { get; }

        bool HasBeenCurrent { get; }

        string DiscrepancyString { get; }

        string StatusDisplay { get; }

        string DisplayName { get; }

        void Finalize(decimal totalTime, decimal picTime);

        FlightQuery Query { get; set; }
    }

    /// <summary>
    /// Represents a period of time.  We use it here to represent a period of time when the user was current.
    /// </summary>
    public class CurrencyPeriod
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public CurrencyPeriod()
        {
            StartDate = EndDate = DateTime.MinValue;
        }

        public CurrencyPeriod(DateTime dtStart, DateTime dtEnd)
        {
            StartDate = dtStart;
            EndDate = dtEnd;
        }

        /// <summary>
        /// Extends the start or end date for this as necessary to cover the union of this and the provided currency period, but ONLY IF THEY OVERLAP
        /// </summary>
        /// <param name="cp">The currency period with which to merge</param>
        /// <returns>True if they were merged, false if they didn't overlap</returns>
        public bool MergeWith(CurrencyPeriod cp)
        {
            if (cp == null)
                throw new ArgumentNullException(nameof(cp));
            if (cp.EndDate.CompareTo(StartDate) < 0 || cp.StartDate.CompareTo(EndDate) > 0)
                return false;
            StartDate = cp.StartDate.EarlierDate(this.StartDate);
            EndDate = cp.EndDate.LaterDate(this.EndDate);
            return true;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "Current from {0} to {1}", StartDate.ToShortDateString(), EndDate.ToShortDateString());
        }
    }

    internal class ComputeCurrencyContext
    {
        public Profile pf { get; set; }

        // Each of the following dictionaries enables segregation of currency by category
        public Dictionary<string, ICurrencyExaminer> dictFlightCurrency { get; set; } = new Dictionary<string, ICurrencyExaminer>();   // Flight currency per 61.57(a), striped by category/class/type.  Also does night and tailwheel
        public Dictionary<string, ArmyMDSCurrency> dictArmyCurrency { get; set; } = new Dictionary<string, ArmyMDSCurrency>();     // Flight currency per AR 95-1
        public Dictionary<string, CurrencyExaminer> dictIFRCurrency { get; set; } = new Dictionary<string, CurrencyExaminer>();      // IFR currency per 61.57(c)(1) or 61.57(c)(2) (Real airplane or certified flight simulator
        public Dictionary<string, PIC6158Currency> dictPICProficiencyChecks { get; set; } = new Dictionary<string, PIC6158Currency>();     // PIC Proficiency checks
        public Dictionary<string, SIC6155Currency> dictSICProficiencyChecks { get; set; } = new Dictionary<string, SIC6155Currency>();   // 61.55 SIC Proficiency checks
        public bool fHasIR { get; set; }    // for EASA currency, don't bother reporting night currency if user holds an instrument rating (defined as having seen an IPC/instrument checkride.

        public GliderIFRCurrency gliderIFR { get; set; } = new GliderIFRCurrency(); // IFR currency in a glider.

        public NVCurrency nvCurrencyHeli { get; set; } = new NVCurrency(CategoryClass.CatClassID.Helicopter);
        public NVCurrency nvCurrencyNonHeli { get; set; } = new NVCurrency(CategoryClass.CatClassID.ASEL);

        // PIC Proficiency check in ANY aircraft
        public PIC6158Currency fcPICPCInAny { get; set; } = new PIC6158Currency(1, 12, true, "61.58(a) - PIC Check in ANY type-rated aircraft");

        public bool fIncludeFAR117 { get; set; }

        public bool fUses61217 { get; set; }
        public FAR61217Currency fc61217 { get; set; } = new FAR61217Currency();

        // Get any customcurrency objects for the user
        public IEnumerable<CustomCurrency> rgCustomCurrency { get; set; }

        public FAR117Currency fcFAR117 { get; set; }
        public FAR195ACurrency fcFAR195 { get; set; }
        public UASCurrency fcUAS { get; set; } = new UASCurrency();

        public IEnumerable<SFAR73Currency> sFAR73Currencies { get; set; } = SFAR73Currency.SFAR73Currencies;

        public decimal totalTime { get; set; } = 0.0M;
        public decimal picTime { get; set; } = 0.0M;

        public ComputeCurrencyContext(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));

            pf = Profile.GetUser(szUser);

            fIncludeFAR117 = pf.UsesFAR117DutyTime;
            fUses61217 = pf.UsesFAR61217Currency;

            // Get any customcurrency objects for the user
            rgCustomCurrency = CustomCurrency.CustomCurrenciesForUser(szUser);

            fcFAR117 = new FAR117Currency(pf.UsesFAR117DutyTimeAllFlights, pf.UsesHHMM);
            fcFAR195 = new FAR195ACurrency(pf.UsesHHMM);
        }

        public ICurrencyExaminer SICProficiencyCurrencyExaminer(CatClassContext catclasscontext, ExaminerFlightRow cfr)
        {
            if (!dictSICProficiencyChecks.ContainsKey(catclasscontext.Name))
            {
                SIC6155Currency curr = new SIC6155Currency(String.Format(CultureInfo.CurrentCulture, Resources.Currency.SIC6155CurrencyName, catclasscontext.Name), cfr.szType);
                dictSICProficiencyChecks.Add(catclasscontext.Name, curr);
                catclasscontext.AddContextToQuery(curr.Query, pf.UserName);
            }
            return dictSICProficiencyChecks[catclasscontext.Name];
        }

        public ICurrencyExaminer PassengerCurrencyExaminer(CatClassContext catclasscontext, ExaminerFlightRow cfr)
        {
            if (!dictFlightCurrency.ContainsKey(catclasscontext.Name))
            {
                string szName = String.Format(CultureInfo.InvariantCulture, "{0} - {1}", catclasscontext.Name, Resources.Currency.Passengers);
                ICurrencyExaminer curr = pf.UseCanadianCurrencyRules ? new PassengerCurrencyCanada(szName, pf.OnlyDayLandingsForDayCurrency) : (pf.UsesLAPLCurrency ? EASAPPLPassengerCurrency.CurrencyForCatClass(cfr.idCatClassOverride, szName, pf.OnlyDayLandingsForDayCurrency) : new PassengerCurrency(szName, pf.OnlyDayLandingsForDayCurrency));
                catclasscontext.AddContextToQuery(curr.Query, pf.UserName);
                dictFlightCurrency.Add(catclasscontext.Name, curr);
            }
            return dictFlightCurrency[catclasscontext.Name];
        }

        public ICurrencyExaminer TailwheelCurrencyExaminer(CatClassContext catclasscontext)
        {
            string szKey = catclasscontext.Name + "TAILWHEEL";
            if (!dictFlightCurrency.ContainsKey(szKey))
            {
                TailwheelCurrency fcTailwheel = new TailwheelCurrency(catclasscontext.Name + " - " + Resources.Currency.Tailwheel);
                catclasscontext.AddContextToQuery(fcTailwheel.Query, pf.UserName);
                dictFlightCurrency.Add(szKey, fcTailwheel);
            }
            return dictFlightCurrency[szKey];
        }

        public ICurrencyExaminer NightCurrencyExaminer(CatClassContext catclasscontext, ExaminerFlightRow cfr, bool fIsTypeRatedCategory)
        {
            string szNightKey = catclasscontext.Name + "NIGHT";
            if (!dictFlightCurrency.ContainsKey(szNightKey))
            {
                string szName = String.Format(CultureInfo.InvariantCulture, "{0} - {1}", catclasscontext.Name, Resources.Currency.Night);
                ICurrencyExaminer curr = pf.UseCanadianCurrencyRules ? new NightCurrencyCanada(szName) : (pf.UsesLAPLCurrency ? (ICurrencyExaminer) new EASAPPLNightPassengerCurrency(szName) : new NightCurrency(szName, fIsTypeRatedCategory ? cfr.szType : string.Empty, pf.AllowNightTouchAndGoes));
                catclasscontext.AddContextToQuery(curr.Query, pf.UserName);
                dictFlightCurrency.Add(szNightKey, curr);
            }
            return dictFlightCurrency[szNightKey];
        }

        public ICurrencyExaminer LAPLCurrencyExaminer(CatClassContext catClassContext, ExaminerFlightRow cfr)
        {
            string szKey = LAPLBase.KeyForLAPL(cfr);
            if (!string.IsNullOrEmpty(szKey))
            {
                if (!dictFlightCurrency.ContainsKey(szKey))
                {
                    LAPLBase curr = LAPLBase.LAPLACurrencyForCategoryClass(cfr);
                    catClassContext.AddContextToQuery(curr.Query, pf.UserName);
                    dictFlightCurrency.Add(szKey, curr);
                }
                return dictFlightCurrency[szKey];
            }
            return null;
        }
    }

    /// <summary>
    /// Used to stripe currency by category/class/(type), or by ICAO
    /// </summary>
    internal class CatClassContext
    {
        #region properties
        public string Name { get; set; }
        public string ICAO { get; set; }
        public CategoryClass CatClass { get; set; }
        public string TypeName { get; set; }

        public string Category { get; set; }
        #endregion

        public CatClassContext(string szName, CategoryClass cc = null, string szType = null, string szcategory = null, string szICAO = null)
        {
            Name = szName;
            CatClass = cc;
            TypeName = szType;
            Category = szcategory;
            ICAO = szICAO;
        }

        public void AddContextToQuery(FlightQuery fq, string szUser)
        {
            if (fq == null)
                return;

            fq.UserName = szUser;

            if (!String.IsNullOrWhiteSpace(ICAO))
                fq.ModelName = String.Format(CultureInfo.InvariantCulture, "ICAO:{0}", ICAO);
            if (CatClass != null)
            {
                fq.CatClasses.Clear();
                fq.CatClasses.Add(CatClass);
            }

            if (!String.IsNullOrWhiteSpace(Category))
            {
                foreach (CategoryClass cc in CategoryClass.CategoryClasses())
                    if (cc.Category.CompareCurrentCultureIgnoreCase(Category) == 0)
                        fq.CatClasses.Add(cc);
            }

            if (!String.IsNullOrWhiteSpace(TypeName))
            {
                fq.TypeNames.Clear();
                fq.TypeNames.Add(TypeName);
            }
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} - {1} {2} {3} {4}", Name, CatClass, TypeName, ICAO, Category);
        }
    }

    /// <summary>
    /// Abstract base class for the FlightExaminer objects that comprise currency computations.
    /// This has one useful static method, though - ComputeCurrency, which computes all flying-based currency 
    /// (and knows about all of the other currency classes).
    /// </summary>
    public abstract class CurrencyExaminer : ICurrencyExaminer
    {
        #region FlightExaminer Interface - must be implemented by derived class
        /// <summary>
        /// Examine a flight for whatever information it has.  Called on each flight sequentially
        /// </summary>
        /// <param name="cfr">The flight to examine</param>
        public abstract void ExamineFlight(ExaminerFlightRow cfr);
        #endregion

        #region ICurrencyExaminer interface - (almost) all implemented by derived classes
        /// <summary>
        /// Current state of this currency
        /// </summary>
        /// <returns>The current state</returns>
        public abstract CurrencyState CurrentState { get; }

        /// <summary>
        /// Expiration date of this currency (could be in the past)
        /// </summary>
        /// <returns>The best guess of when this currency did (or will) expire</returns>
        public abstract DateTime ExpirationDate { get; }

        /// <summary>
        /// Has this currency ever been valid?
        /// </summary>
        /// <returns>True if there was ever a period when this currency was valid.</returns>
        public abstract bool HasBeenCurrent { get; }

        /// <summary>
        /// What needs to be done to get current (if not already current)
        /// </summary>
        public abstract string DiscrepancyString { get; }

        /// <summary>
        /// Display string for the currency
        /// </summary>
        /// <returns></returns>
        public abstract string StatusDisplay { get; }

        /// <summary>
        /// Display name for the examiner; optional to override.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Called to provide total time, other totals, if needed, to a currency item.
        /// </summary>
        /// <param name="totalTime">Total time for the pilot</param>
        /// <param name="picTime">Total PIC time</param>
        public virtual void Finalize(decimal totalTime, decimal picTime) { }
        #endregion

        #region Static utility functions for CurrencyExaminers
        /// <summary>
        /// Display string for the specified expiration date (e.g., "Expired: 3/14/2015" or "Current until: 5/28/2013")
        /// </summary>
        /// <param name="dtExpiration">The expiration date</param>
        /// <returns>A human-readable string for the currency status</returns>
        public static string StatusDisplayForDate(DateTime dtExpiration)
        {
            return String.Format(CultureInfo.CurrentCulture, (DateTime.Compare(dtExpiration.Date, DateTime.Now.Date) < 0) ? Resources.Currency.FormatExpired : Resources.Currency.FormatCurrent, dtExpiration.ToShortDateString());
        }

        /// <summary>
        /// Determines if the expiration date is in the "Getting close" category
        /// </summary>
        /// <param name="dtExp">The expiration date</param>
        /// <returns>True if we are current but almost not</returns>
        public static Boolean IsAlmostCurrent(DateTime dtExp)
        {
            return (DateTime.Compare(DateTime.Now.Date, dtExp.Date) <= 0 && DateTime.Compare(DateTime.Now.AddDays(31).Date, dtExp.Date) >= 0);
        }

        /// <summary>
        /// Determines if the expiration date is in the future
        /// </summary>
        /// <param name="dtExp">The expiration date</param>
        /// <returns>True if we are current</returns>
        public static Boolean IsCurrent(DateTime dtExp)
        {
            return (DateTime.Compare(DateTime.Now.Date, dtExp.Date) <= 0);
        }

        public enum CurrencyQueryDirection { Ascending, Descending };

        public static string CurrencyQuery(CurrencyQueryDirection dir)
        {
            return String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["FlightsCurrencyQuery"], dir == CurrencyQueryDirection.Ascending ? "ASC" : "DESC");
        }
        #endregion

        /// <summary>
        /// Query for the currency examiner - can be null!
        /// </summary>
        public virtual FlightQuery Query { get; set; }

        #region Computing flight currency for a specified user - the big kahuna
        #region Examine a flight for currency
        private static void ExamineFlightInContext(ExaminerFlightRow cfr, ComputeCurrencyContext ccc)
        {
            // keep running totals of PIC and Total time.
            ccc.totalTime += cfr.Total;
            ccc.picTime += cfr.PIC;

            if (cfr.FlightProps.FindEvent(cfp => cfp.PropertyType.IsIPC) != null)
                ccc.fHasIR = true;

            // do any custom currencies first because we may short-circuit everything else if this is unmanned
            foreach (CustomCurrency cc in ccc.rgCustomCurrency)
                cc.ExamineFlight(cfr);

            // And UAS events
            ccc.fcUAS.ExamineFlight(cfr);

            // If this is not a manned aircraft, then nothing below applies (nothing in 61.57)
            if (!CategoryClass.IsManned(cfr.idCatClassOverride))
                return;

            ExamineFlightInContextCategoryClass(cfr, ccc);

            ExamineFlightInContext13526x(cfr, ccc);

            ExamineFlightInContextArmy951(cfr, ccc);

            // SFAR 73 currencies
            foreach (FlightCurrency fc in ccc.sFAR73Currencies)
                fc.ExamineFlight(cfr);

            // get glider IFR currency events.
            ccc.gliderIFR.ExamineFlight(cfr);

            ExamineFlightInContextIFR(cfr, ccc);

            ExamineFlightInContextRemaining(cfr, ccc);
        }

        private static void ExamineFlightInContextCategoryClass(ExaminerFlightRow cfr, ComputeCurrencyContext ccc)
        {
            // If the user is using FAA-style currency:
            // currency in a type-rated aircraft should apply to a non-type-rated aircraft.  So, if the catclasstype differs from the catclass
            // (i.e., requires a type rating), then we need to do two passes - one for the type-rated catclass, one for the generic catclass.
            // If the user is using per-model currency: we should name the "type" to be the catclasstype
            List<CatClassContext> lstCatClasses = new List<CatClassContext>();
            Boolean fFlightInTypeRatedAircraft = cfr.szType.Length > 0;
            if (ccc.pf.UsesPerModelCurrency)
                lstCatClasses.Add(new CatClassContext(String.Format(CultureInfo.InvariantCulture, "{0} ({1})", cfr.szFamily, cfr.szCatClassType), CategoryClass.CategoryClassFromID(cfr.idCatClassOverride), cfr.szType, szICAO: cfr.szFamily));
            else
            {
                lstCatClasses.Add(new CatClassContext(cfr.szCatClassType, CategoryClass.CategoryClassFromID(cfr.idCatClassOverride), cfr.szType));
                if (fFlightInTypeRatedAircraft)
                    lstCatClasses.Add(new CatClassContext(cfr.szCatClassBase, CategoryClass.CategoryClassFromID(cfr.idCatClassOverride)));
            }

            foreach (CatClassContext catclasscontext in lstCatClasses)
            {
                // determine if this pass is for type currency.
                bool fIsTypeRatedCategory = !String.IsNullOrWhiteSpace(catclasscontext.TypeName);

                // SIC proficiency
                if (fIsTypeRatedCategory)
                    ccc.SICProficiencyCurrencyExaminer(catclasscontext, cfr).ExamineFlight(cfr);

                // Night, tailwheel, and basic passenger carrying
                if ((cfr.cLandingsThisFlight > 0 || cfr.FlightProps.TotalCountForPredicate(cfp => cfp.PropertyType.IsNightTakeOff) > 0 || ccc.pf.UseCanadianCurrencyRules || fIsTypeRatedCategory) && (cfr.fIsCertifiedLanding || cfr.fIsFullMotion))
                {
                    ccc.PassengerCurrencyExaminer(catclasscontext, cfr).ExamineFlight(cfr);

                    if (CategoryClass.IsAirplane(cfr.idCatClassOverride) && cfr.fTailwheel && (cfr.cFullStopLandings + cfr.cFullStopNightLandings > 0))
                        ccc.TailwheelCurrencyExaminer(catclasscontext).ExamineFlight(cfr);

                    // for 61.57(e), we need to look at all flights, regardless of whether they have night flight, in computing currency
                    ccc.NightCurrencyExaminer(catclasscontext, cfr, fIsTypeRatedCategory).ExamineFlight(cfr);
                }

                ExamineLAPLCurrency(cfr, ccc, catclasscontext);

                ExamineFAR13529x(cfr, ccc, catclasscontext);
                ExamineFAR1252xx(cfr, ccc, catclasscontext);
            } // foreach catclass
        }

        private static void ExamineLAPLCurrency(ExaminerFlightRow cfr, ComputeCurrencyContext ccc, CatClassContext catClassContext)
        {
            if (ccc.pf.UsesLAPLCurrency)
                ccc.LAPLCurrencyExaminer(catClassContext, cfr)?.ExamineFlight(cfr);
        }

        private static void ExamineFAR13529x(ExaminerFlightRow cfr, ComputeCurrencyContext ccc, CatClassContext catClassContext)
        {
            if (ccc.pf.UsesFAR13529xCurrency)
            {
                string szCatClass = catClassContext.Name;
                const string szKey293a = "135.293(a)";    // not striped by cat/class!!
                string szKey293b = szCatClass + "135.293(b)";
                string szKey297a = szCatClass + "135.297(a)";
                string szKey299 = szCatClass + "135.299(a)";

                if (!ccc.dictFlightCurrency.ContainsKey(szKey293a))
                    ccc.dictFlightCurrency.Add(szKey293a, new Part135_293a(Resources.Currency.Part135293aTitle));
                ccc.dictFlightCurrency[szKey293a].ExamineFlight(cfr);

                if (!ccc.dictFlightCurrency.ContainsKey(szKey293b))
                    ccc.dictFlightCurrency.Add(szKey293b, new Part135_293b(szCatClass, Resources.Currency.Part135293bTitle));
                ccc.dictFlightCurrency[szKey293b].ExamineFlight(cfr);

                if (!ccc.dictFlightCurrency.ContainsKey(szKey297a))
                    ccc.dictFlightCurrency.Add(szKey297a, new Part135_297a(szCatClass, Resources.Currency.Part135297aTitle));
                ccc.dictFlightCurrency[szKey297a].ExamineFlight(cfr);

                if (!ccc.dictFlightCurrency.ContainsKey(szKey299))
                    ccc.dictFlightCurrency.Add(szKey299, new Part135_299a(szCatClass, Resources.Currency.Part135299aTitle));
                ccc.dictFlightCurrency[szKey299].ExamineFlight(cfr);
            }
        }

        private static void ExamineFAR1252xx(ExaminerFlightRow cfr, ComputeCurrencyContext ccc, CatClassContext catClassContext)
        {
            if (ccc.pf.UsesFAR1252xxCurrency)
            {
                string szCatClass = catClassContext.Name;
                const string szKey287a = "125.287(a)";    // not striped by cat/class!!
                string szKey287b = szCatClass + "125.287(b)";
                string szKey291a = szCatClass + "125.291(a)";

                if (!ccc.dictFlightCurrency.ContainsKey(szKey287a))
                    ccc.dictFlightCurrency.Add(szKey287a, new Part125_287a(Resources.Currency.Part125287aTitle));
                ccc.dictFlightCurrency[szKey287a].ExamineFlight(cfr);

                if (!ccc.dictFlightCurrency.ContainsKey(szKey287b))
                    ccc.dictFlightCurrency.Add(szKey287b, new Part125_287b(szCatClass, Resources.Currency.Part125287bTitle));
                ccc.dictFlightCurrency[szKey287b].ExamineFlight(cfr);

                if (!ccc.dictFlightCurrency.ContainsKey(szKey291a))
                    ccc.dictFlightCurrency.Add(szKey291a, new Part125_291a(szCatClass, Resources.Currency.Part135297aTitle));
                ccc.dictFlightCurrency[szKey291a].ExamineFlight(cfr);
            }
        }

        private static void ExamineFlightInContext13526x(ExaminerFlightRow cfr, ComputeCurrencyContext ccc)
        {
            if (ccc.pf.UsesFAR13526xCurrency)
            {
                const string szKey267a1 = "135.267(a)(1)";
                const string szKey267a2 = "135.267(a)(2)";
                const string szKey267a3 = "135.267(a)(3)";
                const string szKey265a1 = "135.265(a)(1)";
                const string szKey265a2 = "135.265(a)(2)";
                const string szKey265a3 = "135.265(a)(3)";
                const string szKey267b1 = "135.267(b)(1)";
                const string szKey267b2 = "135.267(b)(2)";

                if (!ccc.dictFlightCurrency.ContainsKey(szKey267a1))
                    ccc.dictFlightCurrency.Add(szKey267a1, new Part135_267a1());
                ccc.dictFlightCurrency[szKey267a1].ExamineFlight(cfr);

                if (!ccc.dictFlightCurrency.ContainsKey(szKey267a2))
                    ccc.dictFlightCurrency.Add(szKey267a2, new Part135_267a2());
                ccc.dictFlightCurrency[szKey267a2].ExamineFlight(cfr);

                if (!ccc.dictFlightCurrency.ContainsKey(szKey267a3))
                    ccc.dictFlightCurrency.Add(szKey267a3, new Part135_267a3());
                ccc.dictFlightCurrency[szKey267a3].ExamineFlight(cfr);

                if (!ccc.dictFlightCurrency.ContainsKey(szKey265a1))
                    ccc.dictFlightCurrency.Add(szKey265a1, new Part135_265a1());
                ccc.dictFlightCurrency[szKey265a1].ExamineFlight(cfr);

                if (!ccc.dictFlightCurrency.ContainsKey(szKey265a2))
                    ccc.dictFlightCurrency.Add(szKey265a2, new Part135_265a2());
                ccc.dictFlightCurrency[szKey265a2].ExamineFlight(cfr);

                if (!ccc.dictFlightCurrency.ContainsKey(szKey265a3))
                    ccc.dictFlightCurrency.Add(szKey265a3, new Part135_265a3());
                ccc.dictFlightCurrency[szKey265a3].ExamineFlight(cfr);

                if (!ccc.dictFlightCurrency.ContainsKey(szKey267b1))
                    ccc.dictFlightCurrency.Add(szKey267b1, new Part135_267B1Currency(ccc.pf.UsesHHMM));
                ccc.dictFlightCurrency[szKey267b1].ExamineFlight(cfr);

                if (!ccc.dictFlightCurrency.ContainsKey(szKey267b2))
                    ccc.dictFlightCurrency.Add(szKey267b2, new Part135_267B2Currency(ccc.pf.UsesHHMM));
                ccc.dictFlightCurrency[szKey267b2].ExamineFlight(cfr);
            }
        }

        private static void ExamineFlightInContextArmy951(ExaminerFlightRow cfr, ComputeCurrencyContext ccc)
        {
            // Army 95-1 currency
            if (cfr.szArmyMDS.Length > 0 && cfr.Total > 0)
            {
                const string szKeyPrefix = "95-1: ";
                // basic 
                if (!ccc.dictArmyCurrency.ContainsKey(cfr.szArmyMDS))
                    ccc.dictArmyCurrency.Add(cfr.szArmyMDS, new ArmyMDSCurrency(szKeyPrefix + cfr.szArmyMDS));

                if (cfr.fIsRealAircraft)
                    ccc.dictArmyCurrency[cfr.szArmyMDS].AddRecentFlightEvents(cfr.dtFlight, 1);

                // NV - is striped by MDS, but "similar" aircraft are allowed, so we use family to group all of the *H-60* blackhawks. 
                // E.g., UH-60M and HH-60A/L are different for plain currency but the same for NV currency
                string szKeyNV = (String.IsNullOrEmpty(cfr.szFamily) ? cfr.szArmyMDS : cfr.szFamily) + " - " + Resources.Currency.NightVision;

                decimal nvTime = 0.0M;
                bool fIsNVProficiencyCheck = false;
                cfr.FlightProps.ForEachEvent((pfe) =>
                {
                    if (pfe.PropertyType.IsNightVisionAnything && pfe.PropertyType.Type == CFPPropertyType.cfpDecimal)
                        nvTime += pfe.DecValue;
                    if (pfe.PropertyType.IsNightVisionProficiencyCheck)
                        fIsNVProficiencyCheck = true;
                });

                if (nvTime > 0.0M || fIsNVProficiencyCheck)
                {
                    if (!ccc.dictArmyCurrency.ContainsKey(szKeyNV))
                        ccc.dictArmyCurrency.Add(szKeyNV, new ArmyMDSNVCurrency(szKeyPrefix + szKeyNV));
                    ArmyMDSCurrency c = ccc.dictArmyCurrency[szKeyNV];
                    c.AddRecentFlightEvents(cfr.dtFlight, nvTime);
                    if (fIsNVProficiencyCheck)
                        c.AddRecentFlightEvents(cfr.dtFlight, c.RequiredEvents);
                }
            }
        }

        private static void ExamineFlightInContextIFR(ExaminerFlightRow cfr, ComputeCurrencyContext ccc)
        {
            // IFR currency is more complex.
            if (!cfr.fIsGlider)
            {
                // SFAR 73: currency in an R22 or R44 requires that all parts of 61.57 be done in an R22 or R44.
                // see http://rgl.faa.gov/Regulatory_and_Guidance_Library/rgFAR.nsf/0/C039C8820E83B2F4862578940053960F?OpenDocument&Highlight=sfar%2073
                // we handle this for regular currency by treating these as type-rated aircraft.
                // But for instrument we need to do the same thing as above with type-rated: IFR events in an R22/R44 contribute to BOTH R22/R44 currency AND Helicopter currency,
                // but IFR events in a helicopter do NOT contribute to R22/R44 IFR currency.
                List<string> lstIFRCategories = new List<string>() { cfr.szCategory };

                /*
                 * Per discussion with Kersten Brändle <kersten.braendle@hotmail.com>, it appears that SFAR restriction doesn't apply to instrument, for two reasons:
                 * a) the SFAR restriction (d) says 
                 *      "No person may act as pilot in command of a Robinson model R-22 or R-44 helicopter carrying passengers unless the pilot in command
                 *      has met the recency of flight experience requirements of Sec. 61.57",
                 *    so it's about passenger carrying.  That will be the limiting factor, not instrument certification, and
                 * b) apparantly R22/R44 are never IFR certified anyhow.
                 *    
                 * 
                if (cfr.fIsR22)
                    lstIFRCategories.Add(String.Format("{0} (R22)", cfr.szCategory));
                if (cfr.fIsR44)
                    lstIFRCategories.Add(String.Format("{0} (R44)", cfr.szCategory));
                 * */

                foreach (string szIFRCat in lstIFRCategories)
                {
                    if (!ccc.dictIFRCurrency.ContainsKey(szIFRCat))
                    {
                        CurrencyExaminer curr = ccc.pf.UseCanadianCurrencyRules ? (CurrencyExaminer)new InstrumentCurrencyCanada() : (CurrencyExaminer)new InstrumentCurrency();
                        ccc.dictIFRCurrency[szIFRCat] = curr;
                        new CatClassContext(szIFRCat, szcategory: szIFRCat).AddContextToQuery(curr.Query, ccc.pf.UserName);
                    }

                    ccc.dictIFRCurrency[szIFRCat].ExamineFlight(cfr);
                }
            } // if is not a glider.
        }

        private static void ExamineFlightInContextRemaining(ExaminerFlightRow cfr, ComputeCurrencyContext ccc)
        {
            // 61.58 - Pilot proficiency checks
            if (!String.IsNullOrEmpty(cfr.szType))
            {
                ccc.fcPICPCInAny.ExamineFlight(cfr);
                if (!ccc.dictPICProficiencyChecks.ContainsKey(cfr.szType))
                {
                    PIC6158Currency curr = new PIC6158Currency(1, 24, true, String.Format(CultureInfo.CurrentCulture, "61.58(b) - PIC Check in this {0}", cfr.szType));
                    new CatClassContext(cfr.szType, CategoryClass.CategoryClassFromID(cfr.idCatClassOverride), cfr.szType).AddContextToQuery(curr.Query, ccc.pf.UserName);
                    ccc.dictPICProficiencyChecks[cfr.szType] = curr;
                }
                ccc.dictPICProficiencyChecks[cfr.szType].ExamineFlight(cfr);
            }

            // FAR 117 - Pilot rest/duty periods
            if (ccc.fIncludeFAR117)
                ccc.fcFAR117.ExamineFlight(cfr);

            // Always do FAR 195 and 61.217
            ccc.fcFAR195.ExamineFlight(cfr);
            if (ccc.fUses61217)
                ccc.fc61217.ExamineFlight(cfr);

            // Night vision
            ccc.nvCurrencyNonHeli.ExamineFlight(cfr);
            ccc.nvCurrencyHeli.ExamineFlight(cfr);
        }
        #endregion

        /// <summary>
        /// Computes flying-based currency for a specified user.
        /// </summary>
        /// <param name="szUser">Username</param>
        /// <returns>A set of CurrencyStatusItem objects, pruned if never current</returns>
        public static IEnumerable<CurrencyStatusItem> ComputeCurrency(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                return Array.Empty<CurrencyStatusItem>();

            ComputeCurrencyContext ccc = new ComputeCurrencyContext(szUser);

            DBHelper dbh = new DBHelper(CurrencyQuery(CurrencyQueryDirection.Descending));
            dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("UserName", szUser);
                    comm.Parameters.AddWithValue("langID", System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);
                },
                (dr) => { ExamineFlightInContext(new ExaminerFlightRow(dr), ccc); });

            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException("Exception computing currency: " + dbh.LastError);

            if (ccc.pf.UsesLAPLCurrency && ccc.fHasIR)  // remove night currency reporting if you have an instrument rating
            {
                List<string> keys = new List<string>(ccc.dictFlightCurrency.Keys);
                foreach (string szKey in keys)
                    if (ccc.dictFlightCurrency[szKey] is EASAPPLNightPassengerCurrency)
                        ccc.dictFlightCurrency.Remove(szKey);
            }

            // Now build the currencystatusitem list.
            // First regular FAA currency, in sorted order

            List<CurrencyStatusItem> arcs = new List<CurrencyStatusItem>();

            // Get the latest date for expired 
            DateTime dtCutoff = CurrencyExpiration.CutoffDate(ccc.pf.CurrencyExpiration);

            AddCatClassChecks(arcs, ccc, dtCutoff);
            AddIFRChecks(arcs, ccc, dtCutoff);
            AddGliderChecks(arcs, ccc, dtCutoff);

            // Now NV:
            AddNVChecks(arcs, ccc, dtCutoff);

            // Army currency
            AddArmy951Checks(arcs, ccc, dtCutoff);

            AddSICChecks(arcs, ccc, dtCutoff);
            AddPICProficiencyChecks(arcs, ccc, dtCutoff);

            AddFAR117Currencies(arcs, ccc);

            // FAR 195(a)
            AddIfCurrent(arcs, ccc.fcFAR195, DateTime.MinValue);
            AddIfCurrent(arcs, ccc.fc61217, DateTime.MinValue, new CurrencyStatusItem(ccc.fc61217.DisplayName, ccc.fc61217.StatusDisplay, ccc.fc61217.CurrentState, string.Empty));

            // UAS's
            ccc.fcUAS.Finalize(ccc.totalTime, ccc.picTime);
            AddIfCurrent(arcs, ccc.fcUAS, dtCutoff);

            // Finally add in any custom currencies
            AddCustomCurrencies(arcs, ccc, dtCutoff);

            // And any flight reviews from SFAR 73 (i.e., not picked up by profile events):
            AddSFAR73Currencies(arcs, ccc, dtCutoff);

            return arcs;
        }

        #region Building the list of resulting currencies
        private static void AddCatClassChecks(IList<CurrencyStatusItem> arcs, ComputeCurrencyContext ccc, DateTime dtCutoff)
        {
            foreach (string sz in SortedKeys(ccc.dictFlightCurrency.Keys))
            {
                ICurrencyExaminer fc = ccc.dictFlightCurrency[sz];
                fc.Finalize(ccc.totalTime, ccc.picTime);    // in case the currency needs totals.  Currently only true of night currency

                // don't bother with ones where you've never been current, add the rest to our list of currency objects
                AddIfCurrent(arcs, fc, dtCutoff, defaultGroup:CurrencyStatusItem.CurrencyGroups.FlightExperience);
            }
        }

        private static void AddIFRChecks(IList<CurrencyStatusItem> arcs, ComputeCurrencyContext ccc, DateTime dtCutoff)
        {
            // Then IFR:
            // IFR is composite and only by category.  No need to display instrument currency if you've never been current.
            foreach (string key in SortedKeys(ccc.dictIFRCurrency.Keys))
            {
                CurrencyExaminer fcIFR = ccc.dictIFRCurrency[key];
                AddIfCurrent(arcs, fcIFR, dtCutoff, new CurrencyStatusItem(Resources.Currency.IFR + " - " + key.ToString(CultureInfo.CurrentCulture), fcIFR.StatusDisplay, fcIFR.CurrentState, fcIFR.DiscrepancyString), defaultGroup: CurrencyStatusItem.CurrencyGroups.FlightExperience);
            }
        }

        private static void AddGliderChecks(IList<CurrencyStatusItem> arcs, ComputeCurrencyContext ccc, DateTime dtCutoff)
        {
            const string szCategoryGlider = "Glider";

            // Glider IFR currency is it's own thing too.  Because ASEL can enable glider IFR currency, we need to
            // only show IFR currency if you have ever also been current for landings in a glider.
            CurrencyExaminer fcGlider = ccc.dictFlightCurrency.ContainsKey(szCategoryGlider) ? (CurrencyExaminer)ccc.dictFlightCurrency[szCategoryGlider] : null;
            if (fcGlider != null && fcGlider.HasBeenCurrent && ccc.gliderIFR.HasBeenCurrent && fcGlider.ExpirationDate.CompareTo(dtCutoff) > 0 && ccc.gliderIFR.ExpirationDate.CompareTo(dtCutoff) > 0)
            {
                string szPrivilege = ccc.gliderIFR.Privilege;
                arcs.Add(new CurrencyStatusItem(Resources.Currency.IFRGlider + (szPrivilege.Length > 0 ? " - " + szPrivilege : string.Empty), ccc.gliderIFR.StatusDisplay, ccc.gliderIFR.CurrentState, ccc.gliderIFR.DiscrepancyString) { CurrencyGroup = CurrencyStatusItem.CurrencyGroups.FlightExperience });
            }
        }

        private static void AddNVChecks(IList<CurrencyStatusItem> arcs, ComputeCurrencyContext ccc, DateTime dtCutoff)
        {
            List<FlightCurrency> lstNV = new List<FlightCurrency>(ccc.nvCurrencyNonHeli.CurrencyEvents);
            lstNV.AddRange(ccc.nvCurrencyHeli.CurrencyEvents);
            lstNV.ForEach((ce) => { AddIfCurrent(arcs, ce, dtCutoff, defaultGroup: CurrencyStatusItem.CurrencyGroups.FlightExperience); });
        }

        private static void AddArmy951Checks(IList<CurrencyStatusItem> arcs, ComputeCurrencyContext ccc, DateTime dtCutoff)
        {
            if (ccc.pf.UsesArmyCurrency)
            {
                foreach (string sz in SortedKeys(ccc.dictArmyCurrency.Keys))
                    AddIfCurrent(arcs, ccc.dictArmyCurrency[sz], dtCutoff, defaultGroup: CurrencyStatusItem.CurrencyGroups.FlightExperience);
            }
        }

        private static void AddSICChecks(IList<CurrencyStatusItem> arcs, ComputeCurrencyContext ccc, DateTime dtCutoff)
        {
            foreach (string sz in SortedKeys(ccc.dictSICProficiencyChecks.Keys))
            {
                SIC6155Currency sicCurr = ccc.dictSICProficiencyChecks[sz];
                sicCurr.Finalize(ccc.totalTime, ccc.picTime);
                AddIfCurrent(arcs, sicCurr, dtCutoff, defaultGroup: CurrencyStatusItem.CurrencyGroups.FlightReview);
            }
        }

        private static void AddPICProficiencyChecks(IList<CurrencyStatusItem> arcs, ComputeCurrencyContext ccc, DateTime dtCutoff)
        {
            // PIC Proficiency checks
            ccc.fcPICPCInAny.Finalize(ccc.totalTime, ccc.picTime);
            foreach (string szKey in SortedKeys(ccc.dictPICProficiencyChecks.Keys))
            {
                PIC6158Currency fcInType = ccc.dictPICProficiencyChecks[szKey];
                fcInType.Finalize(ccc.totalTime, ccc.picTime);
                PIC6158Currency fcComputed = fcInType.AND(ccc.fcPICPCInAny);

                fcComputed.Query = new FlightQuery(ccc.pf.UserName) { DateRange = FlightQuery.DateRanges.Custom, DateMin = DateTime.Now.Date.AddCalendarMonths(-24), PropertiesConjunction = GroupConjunction.Any };
                foreach (CustomPropertyType cpt in CustomPropertyType.GetCustomPropertyTypes())
                    if (cpt.IsPICProficiencyCheck6158)
                        fcComputed.Query.PropertyTypes.Add(cpt);

                AddIfCurrent(arcs, fcComputed, dtCutoff, new CurrencyStatusItem(String.Format(CultureInfo.CurrentCulture, Resources.Currency.NextPICProficiencyCheck, szKey), fcComputed.StatusDisplay, fcComputed.CurrentState, string.Empty) { CurrencyGroup = CurrencyStatusItem.CurrencyGroups.FlightReview });
            }
        }

        private static void AddFAR117Currencies(IList<CurrencyStatusItem> arcs, ComputeCurrencyContext ccc)
        {
            // FAR 117 status
            ccc.fcFAR117.Finalize(ccc.totalTime, ccc.picTime);
            foreach (CurrencyStatusItem csi in ccc.fcFAR117.Status)
                arcs.Add(csi);
        }

        private static void AddCustomCurrencies(IList<CurrencyStatusItem> arcs, ComputeCurrencyContext ccc, DateTime dtCutoff)
        {
            foreach (CustomCurrency cc in ccc.rgCustomCurrency)
            {
                cc.Finalize(ccc.totalTime, ccc.picTime);
                if (cc.HasBeenCurrent && (!cc.ExpirationDate.HasValue() || cc.ExpirationDate.CompareTo(dtCutoff) > 0))
                    arcs.Add(new CurrencyStatusItem(cc.DisplayName, cc.StatusDisplay, cc.CurrentState, cc.DiscrepancyString) { Query = cc.Query, CurrencyGroup = CurrencyStatusItem.CurrencyGroups.CustomCurrency });
            }
        }

        private static void AddSFAR73Currencies(IList<CurrencyStatusItem> arcs, ComputeCurrencyContext ccc, DateTime dtCutoff)
        {
            foreach (FlightCurrency fc in ccc.sFAR73Currencies)
            {
                if (fc.HasBeenCurrent & (fc.ExpirationDate.HasValue() || fc.ExpirationDate.CompareTo(dtCutoff) > 0))
                    arcs.Add(new CurrencyStatusItem(fc.DisplayName, fc.StatusDisplay, fc.CurrentState, fc.DiscrepancyString) { CurrencyGroup = CurrencyStatusItem.CurrencyGroups.FlightReview });
            }
        }

        private static void AddIfCurrent(IList<CurrencyStatusItem> lst, ICurrencyExaminer fc, DateTime dtCutoff, CurrencyStatusItem csi = null, CurrencyStatusItem.CurrencyGroups defaultGroup = CurrencyStatusItem.CurrencyGroups.None)
        {
            if (fc.HasBeenCurrent && fc.ExpirationDate.CompareTo(dtCutoff) > 0)
            {
                if (csi == null)
                {
                    csi = new CurrencyStatusItem(fc.DisplayName, fc.StatusDisplay, fc.CurrentState, fc.DiscrepancyString) { CurrencyGroup = defaultGroup };
                    if (fc.Query != null)
                        csi.Query = fc.Query;
                }
                else
                    csi.Query = csi.Query ?? fc.Query;
                if (csi.Query != null && csi.CurrencyGroup == CurrencyStatusItem.CurrencyGroups.None)
                    csi.CurrencyGroup = CurrencyStatusItem.CurrencyGroups.FlightExperience;
                lst.Add(csi);
            }
        }
        #endregion

        /// <summary>
        /// Sort keys from a dictionary helper routine
        /// </summary>
        /// <param name="lstIn"></param>
        /// <returns></returns>
        private static IEnumerable<string> SortedKeys(IEnumerable<string> lstIn)
        {
            List<string> lst = new List<string>(lstIn);
            lst.Sort();
            return lst;
        }
        #endregion

        /// <summary>
        /// An event that had a certain number of things (landings, hours) on a particular date
        /// </summary>
        protected class CurrencyEvent
        {
            /// <summary>
            /// # of events (landings/hours)
            /// </summary>
            public Decimal Count { get; set; }

            /// <summary>
            /// Date of the event
            /// </summary>
            public DateTime Date { get; set; }

            public CurrencyEvent(Decimal count, DateTime date)
            {
                Count = count;
                Date = date;
            }
        }
    }

    #region Timespan Types
    /// <summary>
    /// Different forms of time-spans for currency; only the first two (Days/CalendarMonths) are used by regulatory currencies, the rest are used by customcurrencies.
    /// DO NOT RE-ORDER THESE as they are persisted in the database for custom currencies; that's why (for example), sliding months is at the end.
    /// </summary>
    public enum TimespanType
    {
        Days, CalendarMonths,
        TwelveMonthJan, TwelveMonthFeb, TwelveMonthMar, TwelveMonthApr, TwelveMonthMay, TwelveMonthJun, TwelveMonthJul, TwelveMonthAug, TwelveMonthSep, TwelveMonthOct, TwelveMonthNov, TwelveMonthDec,
        SixMonthJan, SixMonthFeb, SixMonthMar, SixMonthApr, SixMonthMay, SixMonthJun,
        FourMonthJan, FourMonthFeb, FourMonthMar, FourMonthApr,
        ThreeMonthJan, ThreeMonthFeb, ThreeMonthMar, SlidingMonths
    }
    #endregion

    #region Concrete flavors of CurrencyExaminers and other currencies
    /// <summary>
    /// Represents a basic flight currency where you need a certain number of events in a certain timeframe  E.g., nighttime currency
    /// FAR 61.57 has the various requirements for currency
    /// </summary>
    public class FlightCurrency : CurrencyExaminer
    {
        readonly Queue<CurrencyEvent> m_eventQueue; // queue of events within the time period window.
        List<CurrencyPeriod> m_lstValidCurrencies;  // expiration dates that were computed as we went along.
        DateTime m_dtEarliest;                      // Datetime of the earliest date that events can matter for whether or not you are current NOW
        Decimal m_Discrepancy;                      // # of events (or hours) short of currency we are to become current NOW
        private int m_ExpirationSpan;             // # of days or months to expiration from the oldest event

        #region Properties
        /// <summary>
        /// The youngest (latest) event date - useful for part 61.57(c)(4)
        /// </summary>
        public DateTime MostRecentEventDate { get; set; }

        /// <summary>
        /// # of events received towards currency
        /// </summary>
        public Decimal NumEvents { get; set; }

        /// <summary>
        /// # of events that are required to be current
        /// </summary>
        public Decimal RequiredEvents { get; set; }

        public TimespanType CurrencyTimespanType { get; set; } = TimespanType.Days;

        /// <summary>
        /// Timespan in which the required events must occur to apply to currency as of NOW.  Earlier events can be used to 
        /// compute an expiration date in the past, but can't contribute to you being current right now.
        /// </summary>
        public int ExpirationSpan
        {
            get { return m_ExpirationSpan; }
            set
            {
                m_ExpirationSpan = value;
                switch (CurrencyTimespanType)
                {
                    default:
                    case TimespanType.Days:
                        m_dtEarliest = DateTime.Now.AddDays(-value);
                        break;
                    case TimespanType.CalendarMonths:
                        m_dtEarliest = DateTime.Now.AddCalendarMonths(-value);
                        break;
                    case TimespanType.SlidingMonths:
                        m_dtEarliest = DateTime.Now.AddMonths(-value);
                        break;
                }
            }
        }

        protected DateTime EarliestDate
        {
            get { return m_dtEarliest; }
        }
        #endregion

        #region Constructors
        public FlightCurrency()
        {
            MostRecentEventDate = DateTime.MinValue;
            NumEvents = 0;
            ExpirationSpan = 0;
            m_Discrepancy = RequiredEvents = 0;
            DisplayName = string.Empty;
            m_eventQueue = new Queue<CurrencyEvent>();
            m_lstValidCurrencies = new List<CurrencyPeriod>();
        }

        /// <summary>
        /// Creates a new FlightCurrency object with the specified expiration span.
        /// </summary>
        /// <param name="cThreshold"># of flight events required to achieve currency</param>
        /// <param name="Period"># of days or months before this aspect of flight currency expires</param>
        /// <param name="fMonths">True if ExpirationSpan is in calendar months; false if in days.</param>
        /// <param name="szName">Display name for this currency object</param>
        public FlightCurrency(Decimal cThreshold, int Period, Boolean fMonths, string szName) : this()
        {
            CurrencyTimespanType = fMonths ? TimespanType.CalendarMonths : TimespanType.Days;
            ExpirationSpan = Period;
            RequiredEvents = Discrepancy = cThreshold;
            DisplayName = szName;
        }
        #endregion

        #region Combining Flight Currencies
        /// <summary>
        /// Enables index-style access to dates in a list of currencyperiods, as if it were an array of descending dates.
        /// 0 is the EndDate of the first element in the list, 1 is the StartDate of that element, 2 is EndDate of 2nd element, etc.
        /// ASSUMES THAT THE LISTS ARE SORTED IN DESCENDING ORDER AND DO NOT OVERLAP
        /// </summary>
        /// <param name="l">The list</param>
        /// <param name="i">The virtual index.  This goes from 0 to ((2 * l.count) - 1)</param>
        /// <returns>The date at that index</returns>
        private static DateTime GetDateAtVirtualIndex(IList<CurrencyPeriod> l, int i)
        {
            return ((i % 2) == 0) ? l[i / 2].EndDate : l[i / 2].StartDate;
        }

        private enum MergeOption { AND, OR };

        /// <summary>
        /// Merges two lists of currency periods, returning the resulting lists.  The lists MUST BE SORTED IN DESCENDING ORDER WITH NO OVERLAP for this to work.
        /// </summary>
        /// <param name="l1">The 1st list</param>
        /// <param name="l2">The 2nd list</param>
        /// <param name="option">AND = output list only contains periods when currencyperiods from BOTH lists were active; OR = output list only contains periods when EITHER currencyperiod was active.</param>
        /// <returns>The merged list</returns>
        private static List<CurrencyPeriod> MergeLists(IList<CurrencyPeriod> l1, IList<CurrencyPeriod> l2, MergeOption option)
        {
            if (l1 == null)
                throw new ArgumentNullException(nameof(l1));
            if (l2 == null)
                throw new ArgumentNullException(nameof(l2));
            // How to merge listA and listB of currency periods:
            //  Each list is effectively a set of descending dates.  EndDate, StartDate, EndDate, StartDate, ...
            //  We have a routine "GetDateAtVirtualIndex" that will get the n'th date in this list, which is in the (n/2)'th currency period.  So...
            //  a) Treat each list as if it were an array of descending dates.
            //  b) Walk through these in order, pulling from whichever virtual array has the latest date.
            //  c) If the index in this virtual array is ODD (after incementing), we are between an EndDate and a StartDate - we are ACTIVE for that currency period.
            //     If the index is EVEN (after incrementing, we are between a StartDate and the next (descending) currencyperiod's EndDate - we are INACTIVE
            //  d) "AND": If we go from not having both active to both being active, we have an enddate for the resulting (merged) currency period. (remember - descending!)
            //            If we go from having both active to either being inactive, we have a start date.
            //     "OR":  If we go from having none active to having at least one active, we have an enddate for the resulting (merged) currency period
            //            If we go from having at least one active to having none active, we have a start date.
            //  e) We can use an "in progress" currencyperiod (cp) to track the state.  If it is null and we discover an enddate, we start a cp.
            //     If it is non-null (i.e., has an enddate) and we discover a startdate, fill in the start date and add it to the list.
            List<CurrencyPeriod> lOut = new List<CurrencyPeriod>();

            int c1 = l1.Count * 2; // two dates per currency period
            int c2 = l2.Count * 2;

            int i1 = 0;
            int i2 = 0;

            bool f1Active;
            bool f2Active;

            CurrencyPeriod cp = null;

            while (i1 < c1 || i2 < c2)
            {
                DateTime dt1 = (i1 < c1) ? GetDateAtVirtualIndex(l1, i1) : DateTime.MinValue;
                DateTime dt2 = (i2 < c2) ? GetDateAtVirtualIndex(l2, i2) : DateTime.MinValue;

                DateTime dt;
                if (dt1.CompareTo(dt2) > 0)
                {
                    i1++;
                    dt = dt1;
                }
                else
                {
                    i2++;
                    dt = dt2;
                }

                f1Active = (i1 % 2) != 0;
                f2Active = (i2 % 2) != 0;

                // see if we need to open or close the currency period
                // if our in-progress currencyperiod (cp) is null and we've met either AND or OR condition, start a new in-progress currencyperiod.
                if (cp == null &&
                    ((option == MergeOption.AND && f1Active && f2Active) ||
                     (option == MergeOption.OR && (f1Active || f2Active))))
                {
                    cp = new CurrencyPeriod() { EndDate = dt };
                }
                // Otherwise, if we have a currencyperiod in progress and have stopped meeting the condition, close off the currencyperiod
                // add it to the list and reset cp.
                else if (cp != null &&
                    ((option == MergeOption.AND && !(f1Active && f2Active)) ||
                     (option == MergeOption.OR && !(f1Active || f2Active))))
                {
                    cp.StartDate = dt;
                    lOut.Add(cp);
                    cp = null;
                }
            }

            return lOut;
        }

        /// <summary>
        /// If this AND anther flight currency must both be true, a new flightcurrency which has a list of merged currencyperiods.
        /// </summary>
        /// <param name="fc">The flight currency with which this is being ANDed</param>
        /// <returns>A new flightcurrency object with valid currencyperiods.  This object does NOT have other values set (such as discrepancy) and inherits the host object's duration.  It is basically useful only for expiration and currency.</returns>
        public FlightCurrency AND(FlightCurrency fc)
        {
            if (fc == null)
                throw new ArgumentNullException(nameof(fc));
            FlightCurrency fcNew = new FlightCurrency(NumEvents, ExpirationSpan, CurrencyTimespanType == TimespanType.CalendarMonths, DisplayName) { m_lstValidCurrencies = MergeLists(m_lstValidCurrencies, fc.m_lstValidCurrencies, MergeOption.AND) };
            return fcNew;
        }

        /// <summary>
        /// If this OR anther flight currency may be true, returns the later expiration date
        /// </summary>
        /// <param name="fc">The flight currency with which this is being ORed</param>
        /// <returns>The resulting expiration date, MinValue if neither has been current</returns>
        public FlightCurrency OR(FlightCurrency fc)
        {
            if (fc == null)
                throw new ArgumentNullException(nameof(fc));
            FlightCurrency fcNew = new FlightCurrency(NumEvents, ExpirationSpan, CurrencyTimespanType == TimespanType.CalendarMonths, DisplayName) { m_lstValidCurrencies = MergeLists(m_lstValidCurrencies, fc.m_lstValidCurrencies, MergeOption.OR) };
            return fcNew;
        }
        #endregion

        #region Computing Currency for single flightcurrency
        /// <summary>
        /// Records a flight event - e.g., an approach, a hold, a landing.
        /// Because FlightCurrency only stores one date and one count, you should ALWAYS ADD THESE in reverse chronological order.
        /// If the added events do not contribute to currency (e.g., too old), or if it is not older than the oldest known events, they are ignored.
        /// </summary>
        /// <param name="dtEvent">DateTime that the flight events occured</param>
        /// <param name="cEvents">Number of flight events that occured that day</param>
        /// <returns>Returns the # of events recorded so far.</returns>
        public Decimal AddRecentFlightEvents(DateTime dtEvent, Decimal cEvents)
        {
            // update our running counts.  
            // NumEvents goes up by the number of events
            // BUT discrpancy only goes down until we get to the earliest date that can matter; after that, we don't care about discrepancy.
            NumEvents += cEvents;
            if (DateTime.Compare(dtEvent.Date, m_dtEarliest.Date) >= 0)
            {
                m_Discrepancy -= cEvents;
            }

            MostRecentEventDate = MostRecentEventDate.LaterDate(dtEvent); // track the youngest event (for 61.57(c)(4))

            // Add this to the queue, then trim the queue.
            m_eventQueue.Enqueue(new CurrencyEvent(cEvents, dtEvent));

            // We can trim off everything that is EITHER 
            //  (a) expirationSpan later than this event, OR
            //  (b) unnecessary to contribute to currency
            DateTime dtEndOfWindow = ExpirationFromDate(dtEvent);
            CurrencyEvent ce;
            while ((ce = m_eventQueue.Peek()) != null && (ce.Date.CompareTo(dtEndOfWindow) > 0 || (NumEvents - ce.Count) >= RequiredEvents))
            {
                NumEvents -= ce.Count;
                m_eventQueue.Dequeue();
            }

            // At this point, if we are above the required event count, we are current from the point of the LATEST
            // ITEM IN THE QUEUE (the one about to be Peek'd) - which could be this one - through dtEndOfWindow.
            // Merge that into the current currency window map OR append a new one.
            if (NumEvents >= RequiredEvents)
            {
                if (m_eventQueue.Count == 0)
                    throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "No events in the queue but NumEvents > Required events - how is that possible? Num={0}, Required={1}", NumEvents, RequiredEvents));

                // The currency period implied, now that we're trimmed, goes from the YOUNGEST contributing
                // event in the queue (from Peek()) to dtEndOfWindow, which is computed off of the CURRENT date (dtEvent).
                // I.e., if d1 is the date of the first contributing event, d2 is the date of the last contributing event,
                // and ts is the timespan of currency (e.g., 90 days or 6 calendar months), then the currency period is
                // d2 through (d1 + ts), which is d2 through dtEndOfWindow.
                CurrencyPeriod cp = new CurrencyPeriod(m_eventQueue.Peek().Date, dtEndOfWindow);

                // add it to the list or try to merge it with the last item in the list.
                if (m_lstValidCurrencies.Count == 0 || !m_lstValidCurrencies[m_lstValidCurrencies.Count - 1].MergeWith(cp))
                    m_lstValidCurrencies.Add(cp);
            }

            return NumEvents;
        }

        /// <summary>
        /// Returns the expiration date from a given date, as if all conditions had been met as of that date.  Simple Date computation
        /// </summary>
        /// <param name="dt">Date in</param>
        /// <returns>The resulting date out (e.g., 90 days later or 6 calendar months later)</returns>
        private DateTime ExpirationFromDate(DateTime dt)
        {
            switch (CurrencyTimespanType)
            {
                default:
                case TimespanType.Days:
                    return dt.AddDays(m_ExpirationSpan);
                case TimespanType.CalendarMonths:
                    return dt.AddCalendarMonths(m_ExpirationSpan);
                case TimespanType.SlidingMonths:
                    return dt.AddMonths(m_ExpirationSpan);
            }
        }

        /// <summary>
        /// Computes the expiration date based on the current value of m_dtOldest, regardless of whether the threshold has been met.
        /// </summary>
        /// <returns>DateTime of the expiration, as if the threshold were met (whether or not it actually has been met)</returns>
        private DateTime GetBestGuessExpirationDate()
        {
            if (m_lstValidCurrencies.Count > 0)
                return m_lstValidCurrencies[0].EndDate;
            else
                return DateTime.MinValue;
        }

        /// <summary>
        /// Determines if the criteria for this flight currency object has been met
        /// </summary>
        /// <returns>True if the flight currency criteria has been met, else false.</returns>
        public Boolean IsCurrent()
        {
            return IsCurrent(GetBestGuessExpirationDate());
        }

        /// <summary>
        /// Determines if the criteria for this flight currency object has been met but is close to expiration
        /// </summary>
        /// <returns>True if "almost current"</returns>
        public Boolean IsAlmostCurrent()
        {
            return (IsAlmostCurrent(GetBestGuessExpirationDate()));
        }

        /// <summary>
        /// Quick check of status - current, almost expired, not current
        /// </summary>
        /// <returns>The appropriate state, default of notcurrent</returns>
        public override CurrencyState CurrentState
        {
            get { return IsAlmostCurrent() ? CurrencyState.GettingClose : (IsCurrent() ? CurrencyState.OK : CurrencyState.NotCurrent); }
        }

        /// <summary>
        /// Computes the number of events required to make up the gap if not current
        /// </summary>
        /// <returns>Integer # of events required to get current again</returns>
        public Decimal Discrepancy
        {
            get { return m_Discrepancy > 0 ? m_Discrepancy : 0; }
            set { m_Discrepancy = value; }
        }

        /// <summary>
        /// Tells whether this flight currency has ever been current, regardless of whether or not it is current right now.
        /// If so, then GetExpirationDate will be non-null and can be used to fetch either the date that currency expired, or
        /// the date that the currency will expire.  If false, then expiration date has no meaning.
        /// </summary>
        /// <returns>True if it has been current, false otherwise</returns>
        public override Boolean HasBeenCurrent
        {
            get { return m_lstValidCurrencies.Count > 0; }
        }

        /// <summary>
        /// Returns the expiration date for this flight currency object, regardless of whether or not the requisite number of events have been met.
        /// Use IsCurrent() to determine if the minimum number of events has been met.
        /// </summary>
        /// <returns>The expiration date.</returns>
        public override DateTime ExpirationDate
        {
            get { return GetBestGuessExpirationDate(); }
        }

        /// <summary>
        /// Returns a display string for the currency state
        /// </summary>
        /// <returns>The string, formatted using the format strings that are specified</returns>
        public override string StatusDisplay
        {
            get { return (IsCurrent() || HasBeenCurrent) ? StatusDisplayForDate(ExpirationDate) : Resources.Currency.FormatNeverCurrent; }
        }

        /// <summary>
        /// Display string for any discrepancy between current status and what is required to be current
        /// </summary>
        public override string DiscrepancyString
        {
            get
            {
                return this.IsCurrent() ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, this.Discrepancy, (this.Discrepancy > 1) ? Resources.Totals.Landings : Resources.Totals.Landing);
            }
        }

        /// <summary>
        /// Determines if there is a gap in the currencyperiods which occurs after a particular date.
        /// Typical usage would be finding a 6-month gap in currencyperiods that is after the pilot's most recent IPC
        /// </summary>
        /// <param name="dtLast">The latest date for a gap; any gaps prior to this date are ignored.</param>
        /// <param name="cGapMonths">The size of the gap, in calendar months</param>
        /// <returns>The latest currency period that ends AFTER the IPC but 6 calendar months BEFORE the next currency period; null if no such gap is found</returns>
        public CurrencyPeriod[] FindCurrencyGap(DateTime dtLast, int cGapMonths)
        {
            for (int i = 0; i <= m_lstValidCurrencies.Count - 2; i++)
            {
                CurrencyPeriod cpCurrent = m_lstValidCurrencies[i];
                CurrencyPeriod cpPrior = m_lstValidCurrencies[i + 1];

                if (cpPrior.EndDate.CompareTo(dtLast) <= 0)
                    return null;

                if (cpPrior.EndDate.AddCalendarMonths(cGapMonths).CompareTo(cpCurrent.StartDate) < 0)
                    return new CurrencyPeriod[] { cpPrior, cpCurrent };
            }
            return null;
        }

        /// <summary>
        /// Should be overridden
        /// </summary>
        /// <param name="cfr">Examines a currency flight row for relevant events.</param>
        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
        }
        #endregion

        #region Misc
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0}: {1}", DisplayName, StatusDisplay);
        }
        #endregion
    }
    #endregion
}
