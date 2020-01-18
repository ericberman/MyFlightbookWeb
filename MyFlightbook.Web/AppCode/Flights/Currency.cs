using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Runtime.Serialization;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2007-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightCurrency
{
    /// <summary>
    /// Status for currency
    /// </summary>
    public enum CurrencyState { NotCurrent = 0, GettingClose = 1, OK = 2, NoDate = 3 };

    /// <summary>
    /// Bitflags for various currency options which can be selected by the user.
    /// </summary>
    [FlagsAttribute]
    public enum CurrencyOptionFlag
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
        flagSuppressModelFeatureTotals = 0x00020000
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
    public class CurrencyStatusItem
    {
        public enum CurrencyGroups { None, FlightExperience, FlightReview, Aircraft, AircraftDeadline, Certificates, Medical, Deadline, CustomCurrency }

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
                        return null;
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
                        szResult = VirtualPathUtility.ToAbsolute(Query == null ? "~/Member/EditProfile.aspx/pftPrefs?pane=custcurrency" : String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx?ft=Totals&fq={0}", HttpUtility.UrlEncode(Convert.ToBase64String(Query.ToJSONString().Compress()))));
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

        /// <summary>
        /// Creates a currency status item in-place
        /// </summary>
        /// <param name="szAttribute">The specific currency attribute (e.g., "Instrument flight," "BFR Due," etc.</param>
        /// <param name="szValue">The value or description of the state</param>
        /// <param name="cs">Everything OK?  Expired?  Close to expiration?</param>
        /// <param name="szDiscrepancy">What is the gap between the current state and some bad state?</param>
        public CurrencyStatusItem()
        {
            Attribute = Value = Discrepancy = string.Empty;
            Status = CurrencyState.OK;
            Query = null;
            AssociatedResourceID = 0;
            CurrencyGroup = CurrencyGroups.None;
        }

        public CurrencyStatusItem(string szAttribute, string szValue, CurrencyState cs, string szDiscrepancy = null) : this()
        {
            Attribute = szAttribute;
            Value = szValue;
            Status = cs;
            Discrepancy = szDiscrepancy;
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
                throw new ArgumentNullException("dr");

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
    }

    /// <summary>
    /// Represents a period of time.  We use it here to representa  period of time when the user was current.
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
                throw new ArgumentNullException("cp");
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
        protected static string StatusDisplayForDate(DateTime dtExpiration)
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
            return String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["FlightsCurrencyQuery"].ToString(), dir == CurrencyQueryDirection.Ascending ? "ASC" : "DESC");
        }
        #endregion

        #region Computing flight currency for a specified user - the big kahuna
        /// <summary>
        /// Computes flying-based currency for a specified user.
        /// </summary>
        /// <param name="szUser">Username</param>
        /// <returns>A set of CurrencyStatusItem objects, pruned if never current</returns>
        public static IEnumerable<CurrencyStatusItem> ComputeCurrency(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                return new CurrencyStatusItem[0];

            Profile pf = Profile.GetUser(szUser);

            // Each of the following dictionaries enables segregation of currency by category
            Dictionary<string, ICurrencyExaminer> dictFlightCurrency = new Dictionary<string, ICurrencyExaminer>();   // Flight currency per 61.57(a), striped by category/class/type.  Also does night and tailwheel
            Dictionary<string, ArmyMDSCurrency> dictArmyCurrency = new Dictionary<string, ArmyMDSCurrency>();     // Flight currency per AR 95-1
            Dictionary<string, CurrencyExaminer> dictIFRCurrency = new Dictionary<string, CurrencyExaminer>();      // IFR currency per 61.57(c)(1) or 61.57(c)(2) (Real airplane or certified flight simulator
            Dictionary<string, FlightCurrency> dictPICProficiencyChecks = new Dictionary<string, FlightCurrency>();     // PIC Proficiency checks
            bool fHasIR = false;    // for EASA currency, don't bother reporting night currency if user holds an instrument rating (defined as having seen an IPC/instrument checkride.

            GliderIFRCurrency gliderIFR = new GliderIFRCurrency(); // IFR currency in a glider.

            NVCurrency nvCurrencyHeli = new NVCurrency(CategoryClass.CatClassID.Helicopter);
            NVCurrency nvCurrencyNonHeli = new NVCurrency(CategoryClass.CatClassID.ASEL);

            // PIC Proficiency check in ANY aircraft
            FlightCurrency fcPICPCInAny = new FlightCurrency(1, 12, true, "61.58(a) - PIC Check in ANY type-rated aircraft");

            bool fIncludeFAR117 = pf.UsesFAR117DutyTime;

            bool fUses61217 = pf.UsesFAR61217Currency;
            FAR61217Currency fc61217 = new FAR61217Currency();

            // Get any customcurrency objects for the user
            IEnumerable<CustomCurrency> rgCustomCurrency = CustomCurrency.CustomCurrenciesForUser(szUser);

            FAR117Currency fcFAR117 = new FAR117Currency(pf.UsesFAR117DutyTimeAllFlights, pf.UsesHHMM);
            FAR195ACurrency fcFAR195 = new FAR195ACurrency(pf.UsesHHMM);
            UASCurrency fcUAS = new UASCurrency();

            decimal totalTime = 0.0M;
            decimal picTime = 0.0M;

            DBHelper dbh = new DBHelper(CurrencyQuery(CurrencyQueryDirection.Descending));
            dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("UserName", szUser);
                    comm.Parameters.AddWithValue("langID", System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);
                },
                (dr) =>
                {
                    ExaminerFlightRow cfr = new ExaminerFlightRow(dr);

                    // keep running totals of PIC and Total time.
                    totalTime += cfr.Total;
                    picTime += cfr.PIC;

                    if (cfr.FlightProps.FindEvent(cfp => cfp.PropertyType.IsIPC) != null)
                        fHasIR = true;

                    // do any custom currencies first because we may short-circuit everything else if this is unmanned
                    foreach (CustomCurrency cc in rgCustomCurrency)
                        cc.ExamineFlight(cfr);

                    // And UAS events
                    fcUAS.ExamineFlight(cfr);

                    // If this is not a manned aircraft, then nothing below applies (nothing in 61.57)
                    if (!CategoryClass.IsManned(cfr.idCatClassOverride))
                        return;

                    // If the user is using FAA-style currency:
                    // currency in a type-rated aircraft should apply to a non-type-rated aircraft.  So, if the catclasstype differs from the catclass
                    // (i.e., requires a type rating), then we need to do two passes - one for the type-rated catclass, one for the generic catclass.
                    // If the user is using per-model currency: we should name the "type" to be the catclasstype
                    List<string> lstCatClasses = new List<string>();
                    Boolean fFlightInTypeRatedAircraft = cfr.szType.Length > 0;
                    if (pf.UsesPerModelCurrency)
                        lstCatClasses.Add(String.Format("{0} ({1})", cfr.szFamily, cfr.szCatClassType));
                    else
                    {
                        lstCatClasses.Add(cfr.szCatClassType);
                        if (fFlightInTypeRatedAircraft)
                            lstCatClasses.Add(cfr.szCatClassBase);
                    }

                    foreach (string szCatClass in lstCatClasses)
                    {
                        // determine if this pass is for type currency.
                        bool fIsTypeRatedCategory = szCatClass.CompareOrdinal(cfr.szCatClassBase) != 0 && szCatClass.CompareOrdinal(cfr.szCatClassType) == 0;

                        // Night, tailwheel, and basic passenger carrying
                        if ((cfr.cLandingsThisFlight > 0 || pf.UseCanadianCurrencyRules || fIsTypeRatedCategory) && (cfr.fIsCertifiedLanding || cfr.fIsFullMotion))
                        {
                            if (!dictFlightCurrency.ContainsKey(szCatClass))
                            {
                                string szName = String.Format("{0} - {1}", szCatClass, Resources.Currency.Passengers);
                                dictFlightCurrency.Add(szCatClass, pf.UseCanadianCurrencyRules ? (ICurrencyExaminer)new PassengerCurrencyCanada(szName) : (pf.UsesLAPLCurrency ? (ICurrencyExaminer)EASAPPLPassengerCurrency.CurrencyForCatClass(cfr.idCatClassOverride, szName) : (ICurrencyExaminer)new PassengerCurrency(szName)));
                            }

                            dictFlightCurrency[szCatClass].ExamineFlight(cfr);

                            if (CategoryClass.IsAirplane(cfr.idCatClassOverride) && cfr.fTailwheel && (cfr.cFullStopLandings + cfr.cFullStopNightLandings > 0))
                            {
                                string szKey = szCatClass + "TAILWHEEL";
                                if (!dictFlightCurrency.ContainsKey(szKey))
                                {
                                    TailwheelCurrency fcTailwheel = new TailwheelCurrency(szCatClass + " - " + Resources.Currency.Tailwheel);
                                    dictFlightCurrency.Add(szKey, fcTailwheel);
                                }
                                dictFlightCurrency[szKey].ExamineFlight(cfr);
                            }

                            // for 61.57(e), we need to look at all flights, regardless of whether they have night flight, in computing currency
                            string szNightKey = szCatClass + "NIGHT";
                            if (!dictFlightCurrency.ContainsKey(szNightKey))
                            {
                                string szName = String.Format("{0} - {1}", szCatClass, Resources.Currency.Night);
                                dictFlightCurrency.Add(szNightKey, pf.UseCanadianCurrencyRules ? (ICurrencyExaminer)new NightCurrencyCanada(szName) : (pf.UsesLAPLCurrency ? (ICurrencyExaminer)new EASAPPLNightPassengerCurrency(szName) : (ICurrencyExaminer)new NightCurrency(szName, fIsTypeRatedCategory ? cfr.szType : string.Empty)));
                            }

                            dictFlightCurrency[szNightKey].ExamineFlight(cfr);
                        }

                        if (pf.UsesLAPLCurrency)
                        {
                            string szKey = LAPLBase.KeyForLAPL(cfr);
                            if (!string.IsNullOrEmpty(szKey))
                            {
                                if (!dictFlightCurrency.ContainsKey(szKey))
                                    dictFlightCurrency.Add(szKey, LAPLBase.LAPLACurrencyForCategoryClass(cfr));
                                dictFlightCurrency[szKey].ExamineFlight(cfr);
                            }
                        }

                        if (pf.UsesFAR13529xCurrency)
                        {
                            string szKey293a = "135.293(a)";    // not striped by cat/class!!
                            string szKey293b = szCatClass + "135.293(b)";
                            string szKey297a = szCatClass + "135.297(a)";
                            string szKey299 = szCatClass + "135.299(a)";

                            if (!dictFlightCurrency.ContainsKey(szKey293a))
                                dictFlightCurrency.Add(szKey293a, new Part135_293a(Resources.Currency.Part135293aTitle));
                            dictFlightCurrency[szKey293a].ExamineFlight(cfr);

                            if (!dictFlightCurrency.ContainsKey(szKey293b))
                                dictFlightCurrency.Add(szKey293b, new Part135_293b(szCatClass, Resources.Currency.Part135293bTitle));
                            dictFlightCurrency[szKey293b].ExamineFlight(cfr);

                            if (!dictFlightCurrency.ContainsKey(szKey297a))
                                dictFlightCurrency.Add(szKey297a, new Part135_297a(szCatClass, Resources.Currency.Part135297aTitle));
                            dictFlightCurrency[szKey297a].ExamineFlight(cfr);

                            if (!dictFlightCurrency.ContainsKey(szKey299))
                                dictFlightCurrency.Add(szKey299, new Part135_299a(szCatClass, Resources.Currency.Part135299aTitle));
                            dictFlightCurrency[szKey299].ExamineFlight(cfr);
                        }
                    } // foreach catclass

                    if (pf.UsesFAR13526xCurrency)
                    {
                        const string szKey267a1 = "135.267(a)(1)";
                        const string szKey267a2 = "135.267(a)(2)";
                        const string szKey267a3 = "135.267(a)(3)";
                        const string szKey265a1 = "135.265(a)(1)";
                        const string szKey265a2 = "135.265(a)(2)";
                        const string szKey265a3 = "135.265(a)(3)";

                        if (!dictFlightCurrency.ContainsKey(szKey267a1))
                            dictFlightCurrency.Add(szKey267a1, new Part135_267a1());
                        dictFlightCurrency[szKey267a1].ExamineFlight(cfr);

                        if (!dictFlightCurrency.ContainsKey(szKey267a2))
                            dictFlightCurrency.Add(szKey267a2, new Part135_267a2());
                        dictFlightCurrency[szKey267a2].ExamineFlight(cfr);

                        if (!dictFlightCurrency.ContainsKey(szKey267a3))
                            dictFlightCurrency.Add(szKey267a3, new Part135_267a3());
                        dictFlightCurrency[szKey267a3].ExamineFlight(cfr);

                        if (!dictFlightCurrency.ContainsKey(szKey265a1))
                            dictFlightCurrency.Add(szKey265a1, new Part135_265a1());
                        dictFlightCurrency[szKey265a1].ExamineFlight(cfr);

                        if (!dictFlightCurrency.ContainsKey(szKey265a2))
                            dictFlightCurrency.Add(szKey265a2, new Part135_265a2());
                        dictFlightCurrency[szKey265a2].ExamineFlight(cfr);

                        if (!dictFlightCurrency.ContainsKey(szKey265a3))
                            dictFlightCurrency.Add(szKey265a3, new Part135_265a3());
                        dictFlightCurrency[szKey265a3].ExamineFlight(cfr);
                    }

                    // Army 95-1 currency
                    if (cfr.szArmyMDS.Length > 0 && cfr.Total > 0)
                    {
                        const string szKeyPrefix = "95-1: ";
                        // basic 
                        if (!dictArmyCurrency.ContainsKey(cfr.szArmyMDS))
                            dictArmyCurrency.Add(cfr.szArmyMDS, new ArmyMDSCurrency(szKeyPrefix + cfr.szArmyMDS));

                        if (cfr.fIsRealAircraft)
                            dictArmyCurrency[cfr.szArmyMDS].AddRecentFlightEvents(cfr.dtFlight, 1);

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
                            if (!dictArmyCurrency.ContainsKey(szKeyNV))
                                dictArmyCurrency.Add(szKeyNV, new ArmyMDSNVCurrency(szKeyPrefix + szKeyNV));
                            ArmyMDSCurrency c = dictArmyCurrency[szKeyNV];
                            c.AddRecentFlightEvents(cfr.dtFlight, nvTime);
                            if (fIsNVProficiencyCheck)
                                c.AddRecentFlightEvents(cfr.dtFlight, c.RequiredEvents);
                        }
                    }

                    // get glider IFR currency events.
                    gliderIFR.ExamineFlight(cfr);

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
                            if (!dictIFRCurrency.ContainsKey(szIFRCat))
                                dictIFRCurrency[szIFRCat] = pf.UseCanadianCurrencyRules ? (CurrencyExaminer)new InstrumentCurrencyCanada() : (CurrencyExaminer)new InstrumentCurrency(pf.UsesLooseIFRCurrency);

                            dictIFRCurrency[szIFRCat].ExamineFlight(cfr);
                        }
                    } // if is not a glider.

                    // 61.58 - Pilot proficiency checks
                    if (!String.IsNullOrEmpty(cfr.szType))
                    {
                        bool fHasPICCheck = false;
                        cfr.FlightProps.ForEachEvent(pe => { if (pe.PropertyType.IsPICProficiencyCheck6158) fHasPICCheck = true; });
                        if (fHasPICCheck)
                        {
                            if (!dictPICProficiencyChecks.ContainsKey(cfr.szType))
                                dictPICProficiencyChecks[cfr.szType] = new FlightCurrency(1, 24, true, String.Format("61.58(b) - PIC Check in this {0}", cfr.szType));
                            dictPICProficiencyChecks[cfr.szType].AddRecentFlightEvents(cfr.dtFlight, 1);
                            fcPICPCInAny.AddRecentFlightEvents(cfr.dtFlight, 1);
                        }
                    }

                    // FAR 117 - Pilot rest/duty periods
                    if (fIncludeFAR117)
                        fcFAR117.ExamineFlight(cfr);

                    // Always do FAR 195 and 61.217
                    fcFAR195.ExamineFlight(cfr);
                    if (fUses61217)
                        fc61217.ExamineFlight(cfr);

                    // Night vision
                    nvCurrencyNonHeli.ExamineFlight(cfr);
                    nvCurrencyHeli.ExamineFlight(cfr);
                });

            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException("Exception computing currency: " + dbh.LastError);

            if (pf.UsesLAPLCurrency && fHasIR)  // remove night currency reporting if you have an instrument rating
            {
                List<string> keys = new List<string>(dictFlightCurrency.Keys);
                foreach (string szKey in keys)
                    if (dictFlightCurrency[szKey] is EASAPPLNightPassengerCurrency)
                        dictFlightCurrency.Remove(szKey);
            }

            // Now build the currencystatusitem list.
            // First regular FAA currency, in sorted order

            List<CurrencyStatusItem> arcs = new List<CurrencyStatusItem>();

            // Get the latest date for expired 
            DateTime dtCutoff = CurrencyExpiration.CutoffDate(pf.CurrencyExpiration);

            foreach (string sz in SortedKeys(dictFlightCurrency.Keys))
            {
                ICurrencyExaminer fc = dictFlightCurrency[sz];
                fc.Finalize(totalTime, picTime);    // in case the currency needs totals.  Currently only true of night currency

                // don't bother with ones where you've never been current, add the rest to our list of currency objects
                if (fc.HasBeenCurrent && fc.ExpirationDate.CompareTo(dtCutoff) > 0)
                    arcs.Add(new CurrencyStatusItem(fc.DisplayName, fc.StatusDisplay, fc.CurrentState, fc.DiscrepancyString));
            }

            // Then IFR:
            // IFR is composite and only by category.  No need to display instrument currency if you've never been current.
            foreach (string key in SortedKeys(dictIFRCurrency.Keys))
            {
                CurrencyExaminer fcIFR = dictIFRCurrency[key];

                if (fcIFR.HasBeenCurrent && fcIFR.ExpirationDate.CompareTo(dtCutoff) > 0)
                    arcs.Add(new CurrencyStatusItem(Resources.Currency.IFR + " - " + key.ToString(), fcIFR.StatusDisplay, fcIFR.CurrentState, fcIFR.DiscrepancyString));
            }

            const string szCategoryGlider = "Glider";

            // Glider IFR currency is it's own thing too.  Because ASEL can enable glider IFR currency, we need to
            // only show IFR currency if you have ever also been current for landings in a glider.
            CurrencyExaminer fcGlider = dictFlightCurrency.ContainsKey(szCategoryGlider) ? (CurrencyExaminer)dictFlightCurrency[szCategoryGlider] : null;
            if (fcGlider != null && fcGlider.HasBeenCurrent && gliderIFR.HasBeenCurrent && fcGlider.ExpirationDate.CompareTo(dtCutoff) > 0 && gliderIFR.ExpirationDate.CompareTo(dtCutoff) > 0)
            {
                string szPrivilege = gliderIFR.Privilege;
                arcs.Add(new CurrencyStatusItem(Resources.Currency.IFRGlider + (szPrivilege.Length > 0 ? " - " + szPrivilege : string.Empty), gliderIFR.StatusDisplay, gliderIFR.CurrentState, gliderIFR.DiscrepancyString));
            }

            // Now NV:
            List<FlightCurrency> lstNV = new List<FlightCurrency>(nvCurrencyNonHeli.CurrencyEvents);
            lstNV.AddRange(nvCurrencyHeli.CurrencyEvents);
            lstNV.ForEach((ce) =>
            {
                if (ce.HasBeenCurrent && ce.ExpirationDate.CompareTo(dtCutoff) > 0)
                    arcs.Add(new CurrencyStatusItem(ce.DisplayName, ce.StatusDisplay, ce.CurrentState, ce.DiscrepancyString));
            });

            // Army currency
            if (pf.UsesArmyCurrency)
            {
                foreach (string sz in SortedKeys(dictArmyCurrency.Keys))
                {
                    FlightCurrency fc = dictArmyCurrency[sz];
                    if (fc.HasBeenCurrent && fc.ExpirationDate.CompareTo(dtCutoff) > 0)
                        arcs.Add(new CurrencyStatusItem(fc.DisplayName, fc.StatusDisplay, fc.CurrentState, fc.DiscrepancyString));
                }
            }

            // PIC Proficiency checks
            foreach (string szKey in SortedKeys(dictPICProficiencyChecks.Keys))
            {
                FlightCurrency fcInType = dictPICProficiencyChecks[szKey];
                FlightCurrency fcComputed = fcInType.AND(fcPICPCInAny);

                if (fcComputed.HasBeenCurrent && fcComputed.ExpirationDate.CompareTo(dtCutoff) > 0)
                    arcs.Add(new CurrencyStatusItem(String.Format(Resources.Currency.NextPICProficiencyCheck, szKey), fcComputed.StatusDisplay, fcComputed.CurrentState, string.Empty));
            }

            // FAR 117 status
            foreach (CurrencyStatusItem csi in fcFAR117.Status)
                arcs.Add(csi);

            // FAR 195(a)
            if (fcFAR195.HasBeenCurrent)
                arcs.Add(new CurrencyStatusItem(fcFAR195.DisplayName, fcFAR195.StatusDisplay, fcFAR195.CurrentState, fcFAR195.DiscrepancyString));
            if (fUses61217 && fc61217.HasBeenCurrent)
                arcs.Add(new CurrencyStatusItem(fc61217.DisplayName, fc61217.StatusDisplay, fc61217.CurrentState, string.Empty));

            // UAS's
            fcUAS.Finalize(totalTime, picTime);
            if (fcUAS.HasBeenCurrent && fcUAS.ExpirationDate.CompareTo(dtCutoff) > 0)
                arcs.Add(new CurrencyStatusItem(fcUAS.DisplayName, fcUAS.StatusDisplay, fcUAS.CurrentState, fcUAS.DiscrepancyString));

            // Finally add in any custom currencies
            foreach (CustomCurrency cc in rgCustomCurrency)
            {
                if (cc.HasBeenCurrent && (!cc.ExpirationDate.HasValue() || cc.ExpirationDate.CompareTo(dtCutoff) > 0))
                    arcs.Add(new CurrencyStatusItem(cc.DisplayName, cc.StatusDisplay, cc.CurrentState, cc.DiscrepancyString) { Query = cc.Query, CurrencyGroup = CurrencyStatusItem.CurrencyGroups.CustomCurrency });
            }

            return arcs;
        }

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

        /// <summary>
        /// Is the timespan expressed in days (false) or calendar months (true)?
        /// </summary>
        public Boolean IsCalendarMonth { get; set; }

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
                m_dtEarliest = IsCalendarMonth ? DateTime.Now.AddCalendarMonths(-value) : DateTime.Now.AddDays(-value);
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
            IsCalendarMonth = false;
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
        /// <param name="fMonths">True if ExpirationSpan is in months; false if in days.</param>
        /// <param name="szName">Display name for this currency object</param>
        public FlightCurrency(Decimal cThreshold, int Period, Boolean fMonths, string szName) : this()
        {
            IsCalendarMonth = fMonths;
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
                throw new ArgumentNullException("l1");
            if (l2 == null)
                throw new ArgumentNullException("l2");
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
                throw new ArgumentNullException("fc");
            FlightCurrency fcNew = new FlightCurrency(NumEvents, ExpirationSpan, IsCalendarMonth, DisplayName) { m_lstValidCurrencies = MergeLists(m_lstValidCurrencies, fc.m_lstValidCurrencies, MergeOption.AND) };
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
                throw new ArgumentNullException("fc");
            FlightCurrency fcNew = new FlightCurrency(NumEvents, ExpirationSpan, IsCalendarMonth, DisplayName) { m_lstValidCurrencies = MergeLists(m_lstValidCurrencies, fc.m_lstValidCurrencies, MergeOption.OR) };
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
            return (IsCalendarMonth) ? dt.AddCalendarMonths(m_ExpirationSpan) : dt.AddDays(m_ExpirationSpan);
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

    /// <summary>
    /// Currency object for basic passenger carrying
    /// </summary>
    public class PassengerCurrency : FlightCurrency
    {
        /// <summary>
        /// A currency object that requires 3 landings in the previous 90 days
        /// </summary>
        /// <param name="szName">the name for the currency</param>
        public PassengerCurrency(string szName) : base(3, 90, false, szName)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            base.ExamineFlight(cfr);

            // we need to subtract out monitored landings, or ignore all if you were monitoring for the whole flight
            int cMonitoredLandings = cfr.FlightProps.TotalCountForPredicate(p => p.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropMonitoredDayLandings || p.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropMonitoredNightLandings);
            if (!cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropPilotMonitoring))
                AddRecentFlightEvents(cfr.dtFlight, Math.Max(cfr.cLandingsThisFlight - cMonitoredLandings, 0));
        }
    }

    /// <summary>
    /// Currency object for basic tailwheel
    /// </summary>
    public class TailwheelCurrency : FlightCurrency
    {
        /// <summary>
        /// A currency object that requires 3 full-stop landings in the previous 90 days
        /// </summary>
        /// <param name="szName">the name for the currency</param>
        public TailwheelCurrency(string szName) : base(3, 90, false, szName)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            base.ExamineFlight(cfr);

            // we need to subtract out monitored landings, or ignore all if you were monitoring for the whole flight
            int cMonitoredLandings = cfr.FlightProps.TotalCountForPredicate(p => p.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropMonitoredDayLandings || p.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropMonitoredNightLandings);
            if (!cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropPilotMonitoring))
                AddRecentFlightEvents(cfr.dtFlight, Math.Max(cfr.cFullStopLandings + cfr.cFullStopNightLandings - cMonitoredLandings, 0));
        }
    }

    /// <summary>
    /// Army MDS currency
    /// </summary>
    public class ArmyMDSCurrency : FlightCurrency
    {
        /// <summary>
        /// A currency object that requires 1 event in the previous 60 days
        /// </summary>
        /// <param name="szName">the name for the currency</param>
        public ArmyMDSCurrency(string szName) : base(1, 60, false, szName)
        {
        }

        public override string DiscrepancyString
        {
            get { return this.IsCurrent() ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, this.Discrepancy, (this.Discrepancy > 1) ? Resources.Currency.CustomCurrencyEventFlights : Resources.Currency.CustomCurrencyEventFlight); }
        }
    }

    /// <summary>
    /// Army MDS (95-1) night-vision currency: 1 hour of NV time within 60 days.
    /// </summary>
    public class ArmyMDSNVCurrency : ArmyMDSCurrency
    {
        public ArmyMDSNVCurrency(string szName) : base(szName)
        {
            RequiredEvents = Discrepancy = 1.0M;
        }

        public override string DiscrepancyString
        {
            get { return this.IsCurrent() ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, this.Discrepancy, Resources.Currency.Hours); }
        }
    }

    /// <summary>
    /// Computes FAR 117 Currency (duty period/rest time for part 121 commercial operations)
    /// </summary>
    public class FAR117Currency : FlightCurrency
    {
        protected bool UseHHMM { get; set; }

        protected bool IsMostRecentFlight { get; set; }

        /// <summary>
        /// Which duty time attributes were specified on a flight?
        /// </summary>
        protected enum DutySpecification { None, Both, Start, End };

        /// <summary>
        /// Represents an effective duty period, which can be explicit (specified by duty periods) or inferred (just the 24-hour period)
        /// </summary>
        protected class EffectiveDutyPeriod
        {
            #region properties
            /// <summary>
            /// Start of the flight duty period
            /// </summary>
            public DateTime FlightDutyStart { get; set; }

            /// <summary>
            /// End of the flight duty period
            /// </summary>
            public DateTime FlightDutyEnd { get; set; }

            public DateTime? AdditionalDutyStart { get; set; }

            public DateTime? AdditionalDutyEnd { get; set; }

            /// <summary>
            /// If present, holds the flight property for duty start that was found
            /// </summary>
            public CustomFlightProperty FPDutyStart { get; set; }

            /// <summary>
            /// If present, holds the flight property for duty end that was found.
            /// </summary>
            public CustomFlightProperty FPDutyEnd { get; set; }

            public bool HasAdditionalDuty
            {
                get { return AdditionalDutyEnd.HasValue && AdditionalDutyStart.HasValue; }
            }

            /// <summary>
            /// Was this explicit or inferred?
            /// </summary>
            public DutySpecification Specification { get; set; }
            #endregion

            #region constructors
            /// <summary>
            /// Constructor for a new EffectivedutyPeriod
            /// </summary>
            /// <param name="dutyStart">UTC Datetime of duty period start</param>
            /// <param name="dutyEnd">UTC Datetime of duty period end</param>
            /// <param name="specification">What exactly was specified?</param>
            public EffectiveDutyPeriod()
            {
                FlightDutyStart = FlightDutyEnd = DateTime.MinValue;
                AdditionalDutyEnd = AdditionalDutyStart = null;
                Specification = DutySpecification.None;
                FPDutyEnd = FPDutyStart = null;
            }

            /// <summary>
            /// Computes an effective duty period for the flight.
            /// If Duty Start/End properties are present and logical, those are used.
            /// If not, Duty is computed as the date of flight (in UTC!) from 00:00 to 23:59.
            /// </summary>
            /// <param name="cfr">The flight to examine</param>
            /// <param name="fInferDutyEnd">True if we should infer the duty end to be "Now".  E.g., if this is the most recent flight in the logbook and it only has a start time</param>
            public EffectiveDutyPeriod(ExaminerFlightRow cfr, bool fInferDutyEnd = false) : this()
            {
                if (cfr == null)
                    throw new ArgumentNullException("cfr");

                CustomFlightProperty pfeFlightDutyStart = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropFlightDutyTimeStart);
                CustomFlightProperty pfeFlightDutyEnd = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropFlightDutyTimeEnd);
                CustomFlightProperty pfeAdditionalDutyStart = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropDutyStart);
                CustomFlightProperty pfeAdditionalDutyEnd = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropDutyEnd);

                if (pfeFlightDutyStart != null)
                    FPDutyStart = pfeFlightDutyStart;
                if (pfeFlightDutyEnd != null)
                    FPDutyEnd = pfeFlightDutyEnd;
                if (pfeAdditionalDutyStart != null)
                    AdditionalDutyStart = pfeAdditionalDutyStart.DateValue;
                if (pfeAdditionalDutyEnd != null)
                    AdditionalDutyEnd = pfeAdditionalDutyEnd.DateValue;

                // if we have duty start/end values, then this is a true duty range.
                if (FPDutyStart != null && FPDutyEnd != null && FPDutyEnd.DateValue.CompareTo(FPDutyStart.DateValue) >= 0)
                {
                    FlightDutyStart = FPDutyStart.DateValue;
                    FlightDutyEnd = FPDutyEnd.DateValue.EarlierDate(DateTime.UtcNow);
                    Specification = DutySpecification.Both;
                    return;
                }
                else if (FPDutyEnd != null && FPDutyStart == null)
                    Specification = DutySpecification.End;
                else if (FPDutyEnd == null && FPDutyStart != null)
                {
                    Specification = DutySpecification.Start;

                    // Issue #406: if we have an open duty start on the user's most recent flight, then infer a duty end of right now.
                    // Otherwise, we can fall through (below) and block off the whole day.
                    if (fInferDutyEnd)
                    {
                        FlightDutyStart = FPDutyStart.DateValue;
                        FlightDutyEnd = DateTime.UtcNow.AddSeconds(-20);  // Subtract a few seconds to close off the duty period but without looking like we're currently in a rest period.
                        return;
                    }
                }

                // OK, we didn't have both duty properties, or they weren't consistent (e.g., start later than end)
                // Just block off the whole day for the duty, since we can't be certain when it began/end
                DateTime dt = cfr.dtFlight;
                FlightDutyEnd = (FlightDutyStart = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc)).AddDays(1.0);
            }
            #endregion

            /// <summary>
            /// Returns the timespan since the specified date that overlaps the nettime.  E.g., if you have 2.5 hours of CFI time and need to know how much of that fell in the window since
            /// the start of the period (dtStart), this returns the amount of the 2.5 hours that falls within the window
            /// To figure out how much of the time (e.g., CFI time) could contribute to the 8-hour limit within 24 hours, we need to figure out the most conservative 
            /// start to the 24 hour period, as follows
            /// a) Call EffectiveDuty.This sets dtDutyStart, dtDutyEnd to either their specified values OR to the 24 hour midnight-to-midnight range.  This provides a starting point.  (Done in the constructor)
            /// b) If a flight start or an engine start is specified, use the later of those(or the duty time), since it's only flight time that is limited.  
            /// Since engine should precede flight, doing a "LaterDate" call should prefer flightstart over enginestart, which is what we want.If dutytime is later still, then this is a no-op.
            /// c) If not flight/engine start is specified and a flight end is specified, subtract off the CFI time from that and use that as the start time, if it's later than what emerged from (b).
            /// Don't do this, though, if we had start times.
            /// d) Otherwise, use an engine time(if specified).  But don't do this if we had flight-end.
            /// Regardless, pull the effective END of duty time in by the end-of-flight time, if specified, or the engine end, if specified.This limits the end time.
            /// We've now got as late a start time as possible and as early an end-time as possible.  
            /// Caller will Contribute MIN(netTime, duty-time window) towards the limit.
            /// </summary>
            /// <param name="dtStart">Start of the period</param>
            /// <param name="netTime">Max time that could be within the period</param>
            /// <param name="cfr">The flight row that specifies a potential duty period</param>
            /// <returns></returns>
            public TimeSpan TimeSince(DateTime dtStart, double netTime, ExaminerFlightRow cfr)
            {
                if (cfr == null)
                    throw new ArgumentNullException("cfr");

                DateTime dtDutyStart = FlightDutyStart;
                DateTime dtDutyEnd = FlightDutyEnd;

                if (cfr.dtFlightStart.HasValue() || cfr.dtEngineStart.HasValue())
                    dtDutyStart = dtDutyStart.LaterDate(cfr.dtFlightStart).LaterDate(cfr.dtEngineStart);
                else if (cfr.dtFlightEnd.HasValue())
                    dtDutyStart = dtDutyStart.LaterDate(cfr.dtFlightEnd.AddHours(-netTime));
                else if (cfr.dtEngineEnd.HasValue())    // don't do engine if we have a flight end
                    dtDutyStart = dtDutyStart.LaterDate(cfr.dtEngineEnd.AddHours(-netTime));

                if (cfr.dtFlightEnd.HasValue())
                    dtDutyEnd = dtDutyEnd.EarlierDate(cfr.dtFlightEnd);
                else if (cfr.dtEngineEnd.HasValue())
                    dtDutyEnd = dtDutyEnd.EarlierDate(cfr.dtEngineEnd);

                return dtDutyEnd.Subtract(dtDutyStart.LaterDate(dtStart));
            }
        }

        #region local variables
        private DateTime currentRestPeriodStart;
        private EffectiveDutyPeriod m_edpCurrent;
        private TimeSpan tsLongestRest11725b;
        private decimal hoursFlightTime11723b1;
        private decimal hoursFlightTime11723b2;
        private DateTime dtLastDutyStart;
        private bool HasSeenProperDutyPeriod;
        private readonly bool fIncludeAllFlights;
        private readonly List<CurrencyPeriod> lstDutyPeriods = new List<CurrencyPeriod>();

        // Useful dates we don't want to continuously recompute
        private readonly DateTime dt168HoursAgo;
        private readonly DateTime dt672HoursAgo;
        private readonly DateTime dt365DaysAgo;
        #endregion

        public static bool IsFAR117Property(CustomFlightProperty pfe)
        {
            if (pfe == null)
                throw new ArgumentNullException("pfe");

            int typeID = pfe.PropTypeID;
            return typeID == (int)CustomPropertyType.KnownProperties.IDPropFlightDutyTimeEnd ||
                   typeID == (int)CustomPropertyType.KnownProperties.IDPropFlightDutyTimeStart ||
                   typeID == (int)CustomPropertyType.KnownProperties.IDPropDutyEnd ||
                   typeID == (int)CustomPropertyType.KnownProperties.IDPropDutyStart;
        }

        public FAR117Currency(bool includeAllFlights = true, bool fUseHHMM = false)
        {
            dtLastDutyStart = currentRestPeriodStart = DateTime.MinValue;
            hoursFlightTime11723b1 = hoursFlightTime11723b2 = 0;
            tsLongestRest11725b = TimeSpan.Zero;
            fIncludeAllFlights = includeAllFlights;
            IsMostRecentFlight = true;  // we haven't examined any flights yet.
            m_edpCurrent = null;
            UseHHMM = fUseHHMM;

            dt168HoursAgo = DateTime.UtcNow.AddHours(-168);
            dt672HoursAgo = DateTime.UtcNow.AddHours(-672);
            dt365DaysAgo = DateTime.UtcNow.AddDays(-365);
        }

        private void UpdateRest(DateTime dtDutyStart, DateTime dtDutyEnd)
        {
            if (dtDutyStart.CompareTo(dtDutyEnd) > 0)
                return; // invalid duty period

            // Are we in a rest period?
            bool fInRest = DateTime.UtcNow.CompareTo(dtDutyEnd) > 0;

            // Compute the start of the current rest period from the end of the latest duty period
            currentRestPeriodStart = (fInRest) ? dtDutyEnd.LaterDate(currentRestPeriodStart) : DateTime.UtcNow;

            // 117.25(b) - when was our last 30-hour rest period?
            if ((tsLongestRest11725b.TotalSeconds < 1) && fInRest)  // don't compare to timespan.zero because there could be a few fractions of a second.
                tsLongestRest11725b = DateTime.UtcNow.Subtract(currentRestPeriodStart);
            else if (dtLastDutyStart.CompareTo(dtDutyEnd) > 0 && dtLastDutyStart.CompareTo(dt168HoursAgo) > 0)
            {
                TimeSpan tsThisRestPeriod = dtLastDutyStart.Subtract(dt168HoursAgo.LaterDate(dtDutyEnd));
                tsLongestRest11725b = tsLongestRest11725b.CompareTo(tsThisRestPeriod) > 0 ? tsLongestRest11725b : tsThisRestPeriod;
            }
            dtLastDutyStart = dtDutyStart;
        }

        private void Add11723BTime(ExaminerFlightRow cfr, DateTime dtDutyEnd)
        {
            DateTime dtFlight = DateTime.SpecifyKind(cfr.dtFlight, DateTimeKind.Utc);

            DateTime flightStart, flightEnd;

            decimal totalMinutesTime = Math.Round(cfr.Total * 60.0M) / 60.0M;

            if (cfr.dtEngineStart.HasValue() && cfr.dtEngineEnd.HasValue())
            {
                flightStart = cfr.dtEngineStart;
                flightEnd = cfr.dtEngineEnd;
            }
            else if (cfr.dtFlightStart.HasValue() && cfr.dtFlightEnd.HasValue())
            {
                flightStart = cfr.dtFlightStart;
                flightEnd = cfr.dtFlightEnd;
            }
            else
            {
                // use 11:59pm as the flight end time and compute flight start based off of that to pull as much of it as possible 
                // into the 672 hour or 365 day window.  (I.e., most conservative)
                flightEnd = new DateTime(cfr.dtFlight.Year, cfr.dtFlight.Month, cfr.dtFlight.Day, 23, 59, 0, DateTimeKind.Utc);
                if (dtDutyEnd.HasValue())
                    flightEnd = flightEnd.EarlierDate(dtDutyEnd);
                flightStart = flightEnd.AddHours((double) -totalMinutesTime);
            }

            // 117.23(b)(1) - 100 hours of flight time in 672 consecutive
            if (flightEnd.CompareTo(dt672HoursAgo) > 0)
                hoursFlightTime11723b1 += Math.Max((totalMinutesTime - Math.Max((decimal) dt672HoursAgo.Subtract(flightStart).TotalHours, 0.0M)), 0.0M);
            // 117.23(b)(2) - 1000 hours in 365 consecutive days.  This is NOT hour-for-hour, so can simply compare dates.
            if (flightEnd.CompareTo(dt365DaysAgo) > 0)
                hoursFlightTime11723b2 += totalMinutesTime;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            EffectiveDutyPeriod edp = new EffectiveDutyPeriod(cfr, IsMostRecentFlight);
            IsMostRecentFlight = false; // no more inferring duty end!
            HasSeenProperDutyPeriod = HasSeenProperDutyPeriod || edp.Specification != DutySpecification.None;

            DateTime dtDutyStart = edp.FlightDutyStart;
            DateTime dtDutyEnd = edp.FlightDutyEnd;

            // Add in flight times if 
            // (a) we include all flights OR
            // (b) we don't include all flights but we have a currently active duty period, OR
            // (c) this flight has some duty period specified
            if (fIncludeAllFlights || m_edpCurrent != null || edp.Specification != DutySpecification.None)
                Add11723BTime(cfr, dtDutyEnd);

            // If we haven't yet seen the start of an open duty period, don't compute the rest yet (need to know the close off point), 
            // so store the end of the duty period in m_edpCurrent and leave the computation to a subsequent flight.
            // OR, if we have an open duty period (m_edpCurrent isn't null) and this one can close it off, then we can do so.
            // OR, if we have an open duty period and this one doesn't close it off, continue to a subsequent flight.
            // Of course, if we're including all flights, then we ALWAYS process this flight using effective (computed) times.
            if (!fIncludeAllFlights)
            {
                // if neither start nor end duty times were specified, return; no more processing to do on this flight other than rest period computation.
                if (edp.Specification == DutySpecification.None)
                {
                    // Handle any additional duty processing
                    if (edp.HasAdditionalDuty)
                        UpdateRest(edp.AdditionalDutyStart.Value, edp.AdditionalDutyEnd.Value);
                    return;
                }
                else if (m_edpCurrent == null && edp.Specification == DutySpecification.End) // we've found the end of a duty period - open up an active duty period
                    m_edpCurrent = edp;
                else if (m_edpCurrent != null && edp.FPDutyStart != null)
                {
                    edp.AdditionalDutyEnd = m_edpCurrent.AdditionalDutyEnd; // be sure to capture any additional duty time from the END of FDP, recorded in a flight we saw previously.
                    dtDutyEnd = m_edpCurrent.FPDutyEnd.DateValue;
                    dtDutyStart = edp.FPDutyStart.DateValue;
                    m_edpCurrent = null;
                }
            }

            if (!fIncludeAllFlights && m_edpCurrent != null)
                return;

            // merge this period with other currency periods.
            CurrencyPeriod cp = new CurrencyPeriod(dtDutyStart, dtDutyEnd);
            if (lstDutyPeriods.Count == 0 || !lstDutyPeriods[lstDutyPeriods.Count - 1].MergeWith(cp))
                lstDutyPeriods.Add(cp);

            // Do a rest computation, using the earlier of dutystart/FDP start and later of duty end/FDP end
            UpdateRest(edp.AdditionalDutyStart.HasValue ? dtDutyStart.EarlierDate(edp.AdditionalDutyStart.Value) : dtDutyStart,
                       edp.AdditionalDutyEnd.HasValue ? dtDutyEnd.LaterDate(edp.AdditionalDutyEnd.Value) : dtDutyEnd);
        }

        public IEnumerable<CurrencyStatusItem> Status
        {
            get
            {
                List<CurrencyStatusItem> lst = new List<CurrencyStatusItem>();

                if (HasSeenProperDutyPeriod)
                {
                    // merge up all of the time spans
                    TimeSpan tsDutyTime11723c1 = TimeSpan.Zero, tsDutyTime11723c2 = TimeSpan.Zero;

                    foreach (CurrencyPeriod cp in lstDutyPeriods)
                    {
                        // 117.23(c)(1) - 60 hours of flight duty time in 168 consecutive hours
                        if (cp.EndDate.CompareTo(dt168HoursAgo) > 0)
                            tsDutyTime11723c1 = tsDutyTime11723c1.Add(cp.EndDate.Subtract(dt168HoursAgo.LaterDate(cp.StartDate)));
                        // 117.23(c)(2) - 190 hours in the prior 672 hours
                        if (cp.EndDate.CompareTo(dt672HoursAgo) > 0)
                            tsDutyTime11723c2 = tsDutyTime11723c2.Add(cp.EndDate.Subtract(dt672HoursAgo.LaterDate(cp.StartDate)));
                    }

                    lst.Add(new CurrencyStatusItem(Resources.Currency.FAR11723b1,
                        String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursTemplate, hoursFlightTime11723b1.FormatDecimal(UseHHMM, true)),
                        (hoursFlightTime11723b1 > 100) ? CurrencyState.NotCurrent : ((hoursFlightTime11723b1 > 80) ? CurrencyState.GettingClose : CurrencyState.OK),
                        (hoursFlightTime11723b1 > 100) ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursOver, (hoursFlightTime11723b1 - 100).FormatDecimal(UseHHMM, true)) : String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursAvailable, (100 - hoursFlightTime11723b1).FormatDecimal(UseHHMM, true))));

                    lst.Add(new CurrencyStatusItem(Resources.Currency.FAR11723b2,
                        String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursTemplate, hoursFlightTime11723b2.FormatDecimal(UseHHMM, true)),
                        (hoursFlightTime11723b2 > 1000) ? CurrencyState.NotCurrent : ((hoursFlightTime11723b2 > 900) ? CurrencyState.GettingClose : CurrencyState.OK),
                        (hoursFlightTime11723b2 > 1000) ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursOver, (hoursFlightTime11723b2 - 1000).FormatDecimal(UseHHMM, true)) : String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursAvailable, (1000 - hoursFlightTime11723b2).FormatDecimal(UseHHMM, true))));

                    lst.Add(new CurrencyStatusItem(Resources.Currency.FAR11723c1,
                        String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursTemplate, tsDutyTime11723c1.TotalHours.FormatDecimal(UseHHMM, true)),
                        (tsDutyTime11723c1.TotalHours > 60) ? CurrencyState.NotCurrent : ((tsDutyTime11723c1.TotalHours > 50) ? CurrencyState.GettingClose : CurrencyState.OK),
                        (tsDutyTime11723c1.TotalHours > 60) ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursOver, (tsDutyTime11723c1.TotalHours - 60).FormatDecimal(UseHHMM)) : String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursAvailable, (60 - tsDutyTime11723c1.TotalHours).FormatDecimal(UseHHMM))));

                    lst.Add(new CurrencyStatusItem(Resources.Currency.FAR11723c2,
                        String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursTemplate, tsDutyTime11723c2.TotalHours.FormatDecimal(UseHHMM, true)),
                        (tsDutyTime11723c2.TotalHours > 190) ? CurrencyState.NotCurrent : ((tsDutyTime11723c2.TotalHours > 150) ? CurrencyState.GettingClose : CurrencyState.OK),
                        (tsDutyTime11723c2.TotalHours > 190) ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursOver, (tsDutyTime11723c2.TotalHours - 190).FormatDecimal(UseHHMM, true)) : String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursAvailable, (190 - tsDutyTime11723c2.TotalHours).FormatDecimal(UseHHMM, true))));

                    // 25b - need a 30-hour rest period within the prior 168 hours
                    double hoursLongestRest = Math.Min(tsLongestRest11725b.TotalHours, 168.0);
                    lst.Add(new CurrencyStatusItem(Resources.Currency.FAR11725b,
                        String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursTemplate, hoursLongestRest.FormatDecimal(UseHHMM, true)),
                        (hoursLongestRest > 56) ? CurrencyState.OK : ((hoursLongestRest > 30) ? CurrencyState.GettingClose : CurrencyState.NotCurrent),
                        string.Empty));

                    // Finally, report on current rest period
                    lst.Add(new CurrencyStatusItem(Resources.Currency.FAR117CurrentRest,
                        String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursTemplate, DateTime.UtcNow.Subtract(currentRestPeriodStart).TotalHours.FormatDecimal(UseHHMM, true)),
                        CurrencyState.OK, string.Empty));
                }
                return lst;
            }
        }
    }

    /// <summary>
    /// 61.195(a) - can't have more than 8 hours of instruction within 24 hours.
    /// </summary>
    public class FAR195ACurrency : FAR117Currency
    {
        private readonly DateTime dt24HoursAgo = DateTime.UtcNow.AddHours(-24);
        private double totalInstruction = 0.0;
        private const double MaxInstruction = 8.0;
        private const double CloseToMaxInstruction = 5.0;

        public FAR195ACurrency(bool fUseHHMM = false)
        {
            this.DisplayName = Resources.Currency.FAR195aTitle;
            UseHHMM = fUseHHMM;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            // quick short circuit for anything more than 24 hours old, or no instruction given, or not a real aircraft
            if (cfr.CFI == 0 || !cfr.fIsRealAircraft || cfr.dtFlight.CompareTo(dt24HoursAgo.Date) < 0)
                return;

            double netCFI = (double)cfr.CFI;

            EffectiveDutyPeriod edp = new EffectiveDutyPeriod(cfr) { FlightDutyStart = DateTime.UtcNow.AddDays(-1), FlightDutyEnd = DateTime.UtcNow };

            TimeSpan ts = edp.TimeSince(dt24HoursAgo, netCFI, cfr);
            if (ts.TotalHours > 0)
                totalInstruction += Math.Min(netCFI, ts.TotalHours);
        }

        public override CurrencyState CurrentState
        {
            get
            {
                if (totalInstruction > MaxInstruction)
                    return CurrencyState.NotCurrent;
                else if (totalInstruction > CloseToMaxInstruction)
                    return CurrencyState.GettingClose;
                else
                    return CurrencyState.OK;
            }
        }

        public override bool HasBeenCurrent
        {
            get { return totalInstruction > 0; }
        }

        public override string DiscrepancyString
        {
            get
            {
                double totalRemaining = MaxInstruction - totalInstruction;

                return totalRemaining > 0 ?
                    String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR195aDiscrepancy, totalRemaining.FormatDecimal(UseHHMM)) :
                    String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR195aPastLimit, (-totalRemaining).FormatDecimal(UseHHMM));
            }
        }

        public override string StatusDisplay
        {
            get { return String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR195aStatus, totalInstruction.FormatDecimal(UseHHMM)); }
        }
    }

    /// <summary>
    /// Currency per 61.217 - need to have done some instruction within past 12 months to be current.
    /// </summary>
    public class FAR61217Currency : FlightCurrency
    {
        DateTime? LastInstruction;

        public FAR61217Currency() : base(0.01M, 12, true, Resources.Currency.Part61217Title)
        {
            LastInstruction = new DateTime?();
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (LastInstruction.HasValue)
                return;

            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (cfr.CFI > 0 || cfr.FlightProps.TimeForProperty(CustomPropertyType.KnownProperties.IDPropGroundInstructionGiven) > 0)
                LastInstruction = cfr.dtFlight;
        }

        public override bool HasBeenCurrent
        {
            get { return LastInstruction.HasValue; }
        }

        public override CurrencyState CurrentState
        {
            get
            {
                if (!LastInstruction.HasValue)
                    return CurrencyState.OK;

                DateTime dtCutoff = DateTime.Now.AddCalendarMonths(-12);

                if (dtCutoff.CompareTo(LastInstruction.Value) > 0)
                    return CurrencyState.NotCurrent;
                else if (dtCutoff.CompareTo(LastInstruction.Value.AddMonths(-1)) > 0)
                    return CurrencyState.GettingClose;

                return CurrencyState.OK;
            }
        }

        public override DateTime ExpirationDate
        {
            get { return (LastInstruction.HasValue) ? LastInstruction.Value.AddCalendarMonths(12) : DateTime.MinValue; }
        }
    }

    /// <summary>
    /// Compute Flight review requirements for SFAR 73 (regular R22/R44 currency is computed by piggybacking on type-rated currency)
    /// </summary>
    public class SFAR73Currency
    {
        private readonly int R22Duration;
        private readonly int R44Duration;

        public DateTime NextR22FlightReview(DateTime lastR22Review)
        {
            return lastR22Review.AddCalendarMonths(R22Duration);
        }

        public DateTime NextR44FlightReview(DateTime lastR44Review)
        {
            return lastR44Review.AddCalendarMonths(R44Duration);
        }

        public SFAR73Currency(string szUser)
        {
            if (string.IsNullOrEmpty(szUser))
                return;

            // Get Helicopter time and R22/R44 time to comply with SFAR 73
            DBHelper dbh = new DBHelper(@"SELECT 
                                                    SUM(f.totalflighttime) AS totalTime, m.typename
                                                FROM
                                                    flights f
                                                        INNER JOIN
                                                    aircraft ac ON f.idaircraft = ac.idaircraft
                                                        INNER JOIN
                                                    models m ON ac.idmodel = m.idmodel
                                                WHERE
                                                    f.username = ?user
                                                        AND m.idcategoryclass = 7
                                                GROUP BY m.typename");

            double heliTime = 0.0;
            double R22Time = 0.0;
            double R44Time = 0.0;

            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("user", szUser); },
                (dr) =>
                {
                    double time = Convert.ToDouble(dr["totalTime"], CultureInfo.InvariantCulture);
                    heliTime += time;
                    if (((string)dr["typename"]).CompareCurrentCultureIgnoreCase("R22") == 0)
                        R22Time += time;
                    if (((string)dr["typename"]).CompareCurrentCultureIgnoreCase("R44") == 0)
                        R44Time += time;
                });
            R44Time += Math.Min(R22Time, 25.0); // up to 25 hours of R22 time can credit towards R44 time

            R22Duration = (heliTime >= 200.0) && R22Time >= 50.0 ? 24 : 12;
            R44Duration = (heliTime >= 200.0) && R44Time >= 50.0 ? 24 : 12;
        }
    }

    /// <summary>
    /// NightCurrency class per 61.57
    /// </summary>
    public class NightCurrency : FlightCurrency
    {
        // 61.57(b) parameters and sub-currencies
        const int RequiredLandings = 3;
        const int RequiredTakeoffs = 3;
        const int TimeSpan = 90;

        public FlightCurrency NightTakeoffCurrency { get; set; }

        // 61.57(e) options
        const int MinTime6157e = 1500;                     // 61.57(e)(4)(i)(A) and (ii)(A)
        const int MinRecentTimeInType = 15;                // 61.57(e)(4)(i)(C) and (ii)(C) - 15 hours in type in the preceding 90 days
        const int MinRecencyInType = 90;
        const int MinLandings6157eAirplane = 3;
        const int Duration6157eAirplane = 6;
        const int MinLandings6157eSim = 6;
        const int MinTakeoffs6157eSim = 6;
        const int Duration6157eSim = 12;

        // 61.57(e) sub-currencies
        private readonly FlightCurrency m_fc6157ei = new FlightCurrency(MinLandings6157eAirplane, Duration6157eAirplane, true, "Landings per 61.57(e)(4)(i)");
        private readonly FlightCurrency m_fc6157eiTakeoffs = new FlightCurrency(MinLandings6157eAirplane, Duration6157eAirplane, true, "Takeoffs per 61.57(e)(4)(i)");
        private readonly FlightCurrency m_fc6157eii = new FlightCurrency(MinLandings6157eSim, Duration6157eSim, true, "Landings per 61.57(e)(4)(ii)");
        private readonly FlightCurrency m_fc6157eiiTakeoffs = new FlightCurrency(MinTakeoffs6157eSim, 12, true, "Takeoffs per 61.57(e)(4)(ii)");
        private readonly FlightCurrency m_fc6157eTotalTime = new FlightCurrency(MinTime6157e, 12, true, "Total time");
        private readonly FlightCurrency m_fc6157TimeInType = new FlightCurrency(MinRecentTimeInType, MinRecencyInType, false, "Recent time in type");
        private readonly PassengerCurrency m_fc6157Passenger = new PassengerCurrency("61.57(e)(4)(i)(B)"); // 61.57(e)(4)(i)(B) and (ii)(B) - regular passenger currency must have been met too

        public string TypeDesignator { get; set; }

        // Computing composite currency
        private Boolean m_fCacheValid = false;
        private CurrencyState m_csCurrent = CurrencyState.NotCurrent;
        private DateTime m_dtExpiration = DateTime.MinValue;
        private string m_szDiscrepancy = string.Empty;

        public NightCurrency(string szName) : base(RequiredLandings, TimeSpan, false, szName)
        {
            NightTakeoffCurrency = new FlightCurrency(RequiredTakeoffs, TimeSpan, false, szName);
        }

        public NightCurrency(string szName, string szType) : this(szName)
        {
            TypeDesignator = szType;
        }

        private void RefreshCurrency()
        {
            if (m_fCacheValid)
                return;

            // Compute both loose (ignores takeoffs) and strict (requires takeoffs) night currencies.
            // Discrepancy can't be counted on after AND/OR so we set that to the one we wish to expose
            FlightCurrency fc6157b = this;  // just for clarity that the "this" object does the basic 61.57(b) implementation.
            FlightCurrency fc6157e4i = m_fc6157eTotalTime.AND(m_fc6157Passenger).AND(m_fc6157TimeInType).AND(m_fc6157ei);
            FlightCurrency fc6157e4ii = m_fc6157eTotalTime.AND(m_fc6157Passenger).AND(m_fc6157TimeInType).AND(m_fc6157eii);
            FlightCurrency fcLoose = fc6157b.OR(fc6157e4i.OR(fc6157e4ii));
            fcLoose.Discrepancy = fc6157b.Discrepancy;

            FlightCurrency fc6157bStrict = fc6157b.AND(NightTakeoffCurrency);
            FlightCurrency fc6157e4iStrict = fc6157e4i.AND(m_fc6157eiTakeoffs);
            FlightCurrency fc6157e4iiStrict = fc6157e4ii.AND(m_fc6157eiiTakeoffs);
            FlightCurrency fcStrict = fc6157bStrict.OR(fc6157e4iStrict).OR(fc6157e4iiStrict);

            // Loose rules for purposes of determining state
            m_csCurrent = fcLoose.CurrentState;
            m_dtExpiration = fcLoose.ExpirationDate;
            m_szDiscrepancy = fcLoose.DiscrepancyString;

            // determine the correct discrepancy string to show
            // if we've met the strict definition, we're fine - no discrepancy
            if (fcStrict.CurrentState.IsCurrent())
            {
                m_szDiscrepancy = string.Empty;
                m_dtExpiration = fcStrict.ExpirationDate;
            }
            // else if we met the loose definition but not strict - meaning takeoffs were not found; Give a reminder about required takeoffs
            else if (m_csCurrent.IsCurrent())
            {
                if (NightTakeoffCurrency.Discrepancy >= NightTakeoffCurrency.RequiredEvents)
                    m_szDiscrepancy = Resources.Currency.NightTakeoffReminder;
                else
                    m_szDiscrepancy = String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplateNight, NightTakeoffCurrency.Discrepancy, (NightTakeoffCurrency.Discrepancy > 1) ? Resources.Currency.Takeoffs : Resources.Currency.Takeoff);
            }
            // else we aren't current at all - use the full discrepancy template using 61.57(b).  DON'T CALL DISCREPANCY STRING because that causes an infinite recursion.
            else
                m_szDiscrepancy = String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, this.Discrepancy, (this.Discrepancy > 1) ? Resources.Totals.Landings : Resources.Totals.Landing);

            m_fCacheValid = true;
        }

        #region CurrencyExaminer Overrides
        public override DateTime ExpirationDate { get { return m_dtExpiration; } }

        /// <summary>
        /// Get the discrepancy string; appends a warning if we're current but haven't found the required take-off events.
        /// </summary>
        /// <returns>The discrepancy string</returns>
        public override string DiscrepancyString
        {
            get
            {
                RefreshCurrency();
                return m_szDiscrepancy;
            }
        }

        public override string StatusDisplay
        {
            get
            {
                RefreshCurrency();
                return FlightCurrency.StatusDisplayForDate(m_dtExpiration);
            }
        }

        /// <summary>
        /// Retrieves the current IFR currency state.
        /// </summary>
        /// <returns></returns>
        public override CurrencyState CurrentState
        {
            get
            {
                RefreshCurrency();
                return m_csCurrent;
            }
        }

        /// <summary>
        /// Has this ever registered currency?
        /// </summary>
        /// <returns></returns>
        public override Boolean HasBeenCurrent
        {
            get
            {
                RefreshCurrency();
                return m_dtExpiration.HasValue();
            }
        }
        #endregion

        public override void Finalize(decimal totalTime, decimal picTime)
        {
            base.Finalize(totalTime, picTime);
            m_fc6157eTotalTime.AddRecentFlightEvents(DateTime.Now, totalTime);
        }

        private enum NightCurrencyOptions { FAR6157bOnly, FAR6157eAirplane, FAR6157eSim }

        /// <summary>
        /// Adds night-time takeoff(s) to the currency.  This is mostly informative - we don't currently require these to be logged
        /// </summary>
        /// <param name="dt">The date of the takeoff(s)</param>
        /// <param name="cEvents">The number of takeoffs</param>
        /// <param name="nco">Indicates whether 61.57(e) applies, and if so, whether it is sim or real aircraft</param>
        private void AddNighttimeTakeOffEvent(DateTime dt, decimal cEvents, NightCurrencyOptions nco)
        {
            NightTakeoffCurrency.AddRecentFlightEvents(dt, cEvents);
            if (nco == NightCurrencyOptions.FAR6157eAirplane)
                m_fc6157eiTakeoffs.AddRecentFlightEvents(dt, cEvents);
            else if (nco == NightCurrencyOptions.FAR6157eSim)
                m_fc6157eiiTakeoffs.AddRecentFlightEvents(dt, cEvents);
        }

        /// <summary>
        /// Adds night-time landing(s) to the currency.
        /// </summary>
        /// <param name="dt">The date of the landing(s)</param>
        /// <param name="cEvents">The number of landings</param>
        /// <param name="nco">Indicates whether 61.57(e) applies, and if so, whether it is sim or real aircraft</param>
        private void AddNighttimeLandingEvent(DateTime dt, decimal cEvents, NightCurrencyOptions nco)
        {
            AddRecentFlightEvents(dt, cEvents);
            if (nco == NightCurrencyOptions.FAR6157eAirplane)
                m_fc6157ei.AddRecentFlightEvents(dt, cEvents);
            else if (nco == NightCurrencyOptions.FAR6157eSim)
                m_fc6157eii.AddRecentFlightEvents(dt, cEvents);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            base.ExamineFlight(cfr);

            if (!cfr.fIsCertifiedLanding)
                return;

            // 61.57(e) only applies if turbine and type rated.  Everything else must be in certified landing, or turbine airplane, or not type rated, or not in the type for this aircraft
            bool fIsMatchingType = !String.IsNullOrEmpty(TypeDesignator) && cfr.szType.CompareCurrentCultureIgnoreCase(cfr.szType) == 0 && CategoryClass.IsAirplane(cfr.idCatClassOverride) && cfr.turbineLevel.IsTurbine() && !cfr.fIsCertifiedSinglePilot;
            NightCurrencyOptions nco = fIsMatchingType ? (cfr.fIsRealAircraft ? NightCurrencyOptions.FAR6157eAirplane : NightCurrencyOptions.FAR6157eSim) : NightCurrencyOptions.FAR6157bOnly;

            // 61.57(e)(4)(i/ii)(A) - 1500 hrs - comes into play after finalize

            // 61.57(e)(4)(i/ii)(C) - 15 hours in this type in the last 90 days.  Only if in an actual aircraft, since it doesn't seem to allow sim time.
            // Do this first because we'll exclude others if you were pilot monitoring
            if (nco == NightCurrencyOptions.FAR6157eAirplane)
                m_fc6157TimeInType.AddRecentFlightEvents(cfr.dtFlight, cfr.Total);

            if (!cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropPilotMonitoring))
            {
                // we need to subtract out monitored landings, or ignore all if you were monitoring for the whole flight
                int cMonitoredLandings = cfr.FlightProps.TotalCountForPredicate(p => p.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropMonitoredNightLandings);
                int cMonitoredTakeoffs = cfr.FlightProps.TotalCountForPredicate(p => p.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropMonitoredNightTakeoffs);

                // 61.57(e)(4)(i/ii)(B) - passenger currency in this type
                m_fc6157Passenger.ExamineFlight(cfr);

                // 61.57(b), 61.57(e)(4)(i/ii)(D) - Night takeoffs/landings
                if (cfr.cFullStopNightLandings > 0)
                    AddNighttimeLandingEvent(cfr.dtFlight, Math.Max(cfr.cFullStopNightLandings - cMonitoredLandings, 0), nco);

                // Night-time take-offs are also technically required for night currency
                cfr.FlightProps.ForEachEvent((pfe) =>
                {
                    if (pfe.PropertyType.IsNightTakeOff)
                        AddNighttimeTakeOffEvent(cfr.dtFlight, Math.Max(pfe.IntValue - cMonitoredTakeoffs, 0), nco);
                });
            }
        }
    }

    public class InstrumentCurrency : CurrencyExaminer
    {
        // 61.57(c)(1)
        readonly FlightCurrency fcIFRHold = new FlightCurrency(1, 6, true, "IFR - Holds");
        readonly FlightCurrency fcIFRApproach = new FlightCurrency(6, 6, true, "IFR - Approaches");

        // 61.57(c)(2) (OLD - expires Nov 26, 2018)
        readonly FlightCurrency fcFTDHold = new FlightCurrency(1, 6, true, "IFR - Holds (FTD)");
        readonly FlightCurrency fcFTDApproach = new FlightCurrency(6, 6, true, "IFR - Approaches (FTD)");

        // 61.57(c)(3) (OLD - expires Nov 26, 2018)
        readonly FlightCurrency fcATDHold = new FlightCurrency(1, 2, true, "IFR - Holds (ATD)");
        readonly FlightCurrency fcATDApproach = new FlightCurrency(6, 2, true, "IFR - Approaches (ATD)");
        readonly FlightCurrency fcUnusualAttitudesAsc = new FlightCurrency(2, 2, true, "Unusual Attitudes Recoveries, Ascending");
        readonly FlightCurrency fcUnusualAttitudesDesc = new FlightCurrency(2, 2, true, "Unusual Attitude Recoveries (Descending, Vne)");
        readonly FlightCurrency fcInstrumentHours = new FlightCurrency(3, 2, true, "Hours of instrument time");

        /*
         * 61.57 says (as of 4/8/13):
         * (4) Combination of completing instrument experience in an aircraft and a flight simulator, flight training device, and aviation training device. A person who elects to complete the 
         * instrument experience with a combination of an aircraft, flight simulator or flight training device,
         * and aviation training device must have performed and logged the following within the 6 calendar months preceding the month of the flight--
         *  (i) Instrument experience in an airplane, powered-lift, helicopter, or airship, as appropriate, for the instrument rating privileges to be maintained, performed in actual weather conditions,
         *  or under simulated weather conditions while using a view-limiting device, on the following instrument currency tasks:
         *   (A) Instrument approaches.
         *   (B) Holding procedures and tasks.
         *   (C) Interception and tracking courses through the use of navigational electronic systems.
         * (ii) Instrument experience in a flight simulator or flight training device that represents the category of aircraft for the instrument 
         * rating privileges to be maintained and involves performing at least the following tasks--
         *   (A) Instrument approaches.
         *   (B) Holding procedures and tasks.
         *   (C) Interception and tracking courses through the use of navigational electronic systems.
         * (iii) Instrument experience in an aviation training device that represents the category of aircraft for the instrument rating privileges to be maintained and involves performing at least the following tasks-- 
         *   (A) Six instrument approaches.
         *   (B) Holding procedures and tasks.
         *   (C) Interception and tracking courses through the use of navigational electronic systems.
         *   
         * I read this as requiring no fewer than 8 approaches and 3 holds: at least 1 in a real aircraft, 1 in an FS/FTD, and 6 in an ATD.  That is
         * what is coded below in STRICT.
         * 
         * On 4/8/13, I spoke with Michael Haynes at the Seattle FSDO who said, after consulting with other inspectors as well, that the correct way
         * to interpret this is "Any combination of Real Aircraft + (FS/FTD OR ATD) that totals 66-HIT".  (He also confirmed that the "IT" part of "HIT"
         * is generally met by the 6/H part.)
         * 
         * I'll implement this interpretation here as LOOSE, and require an approach and hold in a real airplane but the balance in anything.
         * 
         * Update: 6/1/2013: I'm lossening this a bit more, so the hold can be in a real airplane OR ATD OR FS/FTD.
         * 
         * Update: 7/6/2018 - pending changes to regs make all of this moot.
        */

        // 61.57(c)(4) - STRICT all data above is captured except we need a 6-month ATDHold/Approach as well
        readonly FlightCurrency fcATDHold6Month = new FlightCurrency(1, 6, true, "IFR - Holds (ATD, 6-month)");
        readonly FlightCurrency fcATDAppch6Month = new FlightCurrency(6, 6, true, "IFR - Approaches (ATD, 6-month)");
        readonly FlightCurrency fcFTDApproach6Month = new FlightCurrency(1, 6, true, "IFR - FS/FTD Approach");
        readonly FlightCurrency fcAirplaneApproach6Month = new FlightCurrency(1, 6, true, "IFR - at least one approach in 6 months in airplane");

        // 61.57(c)(4) - LOOSE Interpretation  OBSOLETE AS OF Nov 26, 2018
        readonly FlightCurrency fcComboApproach6Month = new FlightCurrency(6, 6, true, "IFR - 6 Approaches (Real AND (FS/FTD OR ATD))");

        // 61.57(c)(5) - seems redundant with (c)(2).  OBSOLETE

        // 61.57(d) - IPC (Instrument checkride counts here too)
        readonly FlightCurrency fcIPCOrCheckride = new FlightCurrency(1, 6, true, "IPC or Instrument Checkride");

        readonly private Boolean m_fUseLoose6157c4 = false;
        private Boolean m_fCacheValid = false;
        private CurrencyState m_csCurrent = CurrencyState.NotCurrent;
        private DateTime m_dtExpiration = DateTime.MinValue;
        private string m_szDiscrepancy = string.Empty;
        private Boolean fSeenCheckride = false;

        #region Adding Events
        /// <summary>
        /// Add approaches in a real plane
        /// </summary>
        /// <param name="dt">Date of the approaches</param>
        /// <param name="cApproaches"># of approaches</param>
        public void AddApproaches(DateTime dt, int cApproaches)
        {
            fcIFRApproach.AddRecentFlightEvents(dt, cApproaches);
            fcAirplaneApproach6Month.AddRecentFlightEvents(dt, cApproaches);
            m_fCacheValid = false;
        }

        /// <summary>
        /// Add holds in a real plane or certified flight simulator
        /// </summary>
        /// <param name="dt">Date of the hold</param>
        /// <param name="cHolds"># of holds</param>
        public void AddHolds(DateTime dt, int cHolds)
        {
            fcIFRHold.AddRecentFlightEvents(dt, cHolds);
            m_fCacheValid = false;
        }

        /// <summary>
        /// Add an IPC or equivalent (e.g., instrument check ride)
        /// </summary>
        /// <param name="dt">Date of the IPC</param>
        public void AddIPC(DateTime dt)
        {
            fcIPCOrCheckride.AddRecentFlightEvents(dt, 1);
            m_fCacheValid = false;
        }

        /// <summary>
        /// Add approaches in an ATD
        /// </summary>
        /// <param name="dt">Date of the approaches</param>
        /// <param name="cApproaches"># of approaches</param>
        public void AddATDApproaches(DateTime dt, int cApproaches)
        {
            fcATDApproach.AddRecentFlightEvents(dt, cApproaches);
            fcATDAppch6Month.AddRecentFlightEvents(dt, cApproaches);
            m_fCacheValid = false;
        }

        /// <summary>
        /// Add holds in an ATD
        /// </summary>
        /// <param name="dt">Date of the hold</param>
        /// <param name="cHolds"># of holds</param>
        public void AddATDHold(DateTime dt, int cHolds)
        {
            fcATDHold.AddRecentFlightEvents(dt, cHolds);
            fcATDHold6Month.AddRecentFlightEvents(dt, cHolds);
            m_fCacheValid = false;
        }

        /// <summary>
        /// Add approaches in an FTD
        /// </summary>
        /// <param name="dt">Date of the approaches</param>
        /// <param name="cApproaches"># of approaches</param>
        public void AddFTDApproaches(DateTime dt, int cApproaches)
        {
            fcFTDApproach.AddRecentFlightEvents(dt, cApproaches);
            fcFTDApproach6Month.AddRecentFlightEvents(dt, cApproaches);
            m_fCacheValid = false;
        }

        /// <summary>
        /// Add holds in an FTD
        /// </summary>
        /// <param name="dt">Date of the hold</param>
        /// <param name="cHolds"># of holds</param>
        public void AddFTDHold(DateTime dt, int cHolds)
        {
            fcFTDHold.AddRecentFlightEvents(dt, cHolds);
            m_fCacheValid = false;
        }
        /// <summary>
        /// Add instrument time in an ATD
        /// </summary>
        /// <param name="dt">Date of the instrument time</param>
        /// <param name="time">Amount of time</param>
        public void AddATDInstrumentTime(DateTime dt, Decimal time)
        {
            fcInstrumentHours.AddRecentFlightEvents(dt, time);
            m_fCacheValid = false;
        }

        /// <summary>
        /// Add unusual attitude recoveres in an ATD - Ascending near-stall condition
        /// </summary>
        /// <param name="dt">Date of the recoveries</param>
        /// <param name="cRecoveries"># of recoveries</param>
        public void AddUARecoveryAsc(DateTime dt, int cRecoveries)
        {
            fcUnusualAttitudesAsc.AddRecentFlightEvents(dt, cRecoveries);
            m_fCacheValid = false;
        }

        /// <summary>
        /// Add unusual attitude recoveres in an ATD - Descending, Vne condition
        /// </summary>
        /// <param name="dt">Date of the recoveries</param>
        /// <param name="cRecoveries"># of recoveries</param>
        public void AddUARecoveryDesc(DateTime dt, int cRecoveries)
        {
            fcUnusualAttitudesDesc.AddRecentFlightEvents(dt, cRecoveries);
            m_fCacheValid = false;
        }
        #endregion

        public InstrumentCurrency(bool fUseLoose6157c4)
        {
            m_fUseLoose6157c4 = fUseLoose6157c4;
        }

        /// <summary>
        /// Computes the overall currency
        /// </summary>
        protected void RefreshCurrency()
        {
            // Compute currency according to 61.57(c)(1)-(5) and 61.57(e)
            // including each one's expiration.  The one that expires latest is the expiration date if you are current, the one that
            // expired most recently is the expiration date since when you were not current.

            if (m_fCacheValid)
                return;

            // This is ((61.57(c)(1) OR (2) OR (3) OR (4) OR (5) OR 61.57(e)), each of which is the AND of several currencies.

            // 61.57(c)(1) (66-HIT in airplane)
            FlightCurrency fc6157c1 = fcIFRApproach.AND(fcIFRHold);

            // 61.57(c)(2) (66-HIT in an FTD or flight simulator)
            FlightCurrency fc6157c2 = fcFTDApproach.AND(fcFTDHold);

            // 61.57(c)(3) - ATD
            FlightCurrency fc6157c3 = fcATDHold.AND(fcATDApproach).AND(fcUnusualAttitudesAsc).AND(fcUnusualAttitudesDesc).AND(fcInstrumentHours);

            // 61.57(c)(4) - Combo STRICT - 6 approaches/hold in an ATD PLUS an approach/hold in an airplane PLUS an approach/hold in an FTD
            FlightCurrency fc6157c4 = fcATDAppch6Month.AND(fcATDHold6Month).AND(fcIFRHold).AND(fcAirplaneApproach6Month).AND(fcFTDApproach6Month).AND(fcFTDHold);

            // 61.57(c)(4) - Combo LOOSE - any combination that yields 66-HIT, but require at least one aircraft approach and hold.
            FlightCurrency fc6157c4Loose = fcComboApproach6Month.AND(fcAirplaneApproach6Month).AND(fcIFRHold.OR(fcATDHold6Month).OR(fcFTDHold));

            // 61.57(c)(5) - combo meal, but seems redundant with (c)(2)/(3).  I.e., if you've met this, you've met (2) or (3).

            // 61.57 (e) - IPC; no need to AND anything together for this one.

            FlightCurrency fcIFR = fc6157c1.OR(fc6157c2).OR(fc6157c3).OR(fc6157c4).OR(fcIPCOrCheckride);
            if (m_fUseLoose6157c4)
                fcIFR = fcIFR.OR(fc6157c4Loose);

            m_csCurrent = fcIFR.CurrentState;
            m_dtExpiration = fcIFR.ExpirationDate;

            if (m_csCurrent == CurrencyState.NotCurrent)
            {
                // if more than 6 calendar months have passed since expiration, an IPC is required.
                // otherwise, just assume 66-HIT.
                if (DateTime.Compare(m_dtExpiration.AddCalendarMonths(6).Date, DateTime.Now.Date) > 0)
                    m_szDiscrepancy = String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplateIFR,
                                fcIFRHold.Discrepancy,
                                (fcIFRHold.Discrepancy == 1) ? Resources.Currency.Hold : Resources.Currency.Holds,
                                fcIFRApproach.Discrepancy,
                                (fcIFRApproach.Discrepancy == 1) ? Resources.Totals.Approach : Resources.Totals.Approaches);
                else
                    m_szDiscrepancy = Resources.Currency.IPCRequired;
            }
            else
            {
                // Check to see if IPC is required by looking for > 6 month gap in IFR currency.
                // For now, we won't make you un-current, but we'll warn.
                // (IPC above, though, is un-current).  
                CurrencyPeriod[] rgcpMissingIPC = fcIFR.FindCurrencyGap(fcIPCOrCheckride.MostRecentEventDate, 6);

                if (rgcpMissingIPC != null)
                    m_szDiscrepancy = String.Format(CultureInfo.CurrentCulture, Resources.Currency.IPCMayBeRequired, rgcpMissingIPC[0].EndDate.ToShortDateString(), rgcpMissingIPC[1].StartDate.ToShortDateString());
                else
                    m_szDiscrepancy = string.Empty;
            }

            m_fCacheValid = true;
        }

        #region CurrencyExaminer Overrides
        public override DateTime ExpirationDate { get { return m_dtExpiration; } }

        /// <summary>
        /// Returns the requirements to get current again.  If you are not current, it always assumes you will do 66-HIT 
        /// (6 in the last 6 months of holds, intercepting, and tracking and approaches), even though alternative methods may
        /// be valid.
        /// </summary>
        /// <returns>The display string for what is needed to restore currency</returns>
        public override string DiscrepancyString
        {
            get
            {
                RefreshCurrency();
                return m_szDiscrepancy;
            }
        }

        public override string StatusDisplay
        {
            get
            {
                RefreshCurrency();
                return FlightCurrency.StatusDisplayForDate(m_dtExpiration);
            }
        }

        /// <summary>
        /// Retrieves the current IFR currency state.
        /// </summary>
        /// <returns></returns>
        public override CurrencyState CurrentState
        {
            get
            {
                RefreshCurrency();
                return m_csCurrent;
            }
        }

        /// <summary>
        /// Has this ever registered currency?
        /// </summary>
        /// <returns></returns>
        public override Boolean HasBeenCurrent
        {
            get
            {
                RefreshCurrency();
                return m_dtExpiration.HasValue();
            }
        }
        #endregion

        #region FlightExaminer Implementation
        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            if (!cfr.fIsCertifiedIFR)
                return;

            // Once we see a checkride (vs. a vanilla IPC), any prior approaches/holds/etc. are clearly for training and
            // thus we shouldn't count them for currency.  Otherwise, we might warn that an IPC is required due to a gap in training when
            // one isn't actually.
            if (fSeenCheckride)
                return;

            cfr.FlightProps.ForEachEvent((pfe) =>
            {
                // add any IPC or IPC equivalents
                if (pfe.PropertyType.IsIPC)
                    AddIPC(cfr.dtFlight);
                if (pfe.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropCheckrideIFR)
                    fSeenCheckride = true;
            });

            // add any potentially relevant IFR currency events.
            // After Nov 26, 2018, the rules change: any combination of real aircraft, FTD, FFS, or ATD that adds up to 6 approaches + hold works.
            if (cfr.fIsRealAircraft || DateTime.Now.CompareTo(ExaminerFlightRow.Nov2018Cutover) >= 0)
            {
                if (cfr.cApproaches > 0)
                    AddApproaches(cfr.dtFlight, cfr.cApproaches);
                if (cfr.cHolds > 0)
                    AddHolds(cfr.dtFlight, cfr.cHolds);
            }
            // Other IFR currency events for ATD's
            else if (cfr.fIsATD)
            {
                // Add any IFR-relevant flight events for 61.57(c)(3)
                cfr.FlightProps.ForEachEvent((pfe) =>
                {
                    // Add unusal attitudes performed in an ATD
                    if (pfe.PropertyType.IsUnusualAttitudeAscending)
                        AddUARecoveryAsc(cfr.dtFlight, pfe.IntValue);
                    if (pfe.PropertyType.IsUnusualAttitudeDescending)
                        AddUARecoveryDesc(cfr.dtFlight, pfe.IntValue);
                });

                if (cfr.cApproaches > 0)
                    AddATDApproaches(cfr.dtFlight, cfr.cApproaches);
                if (cfr.cHolds > 0)
                    AddATDHold(cfr.dtFlight, cfr.cHolds);

                AddATDInstrumentTime(cfr.dtFlight, cfr.IMC + cfr.IMCSim);
            }
            else if (cfr.fIsFTD)
            {
                if (cfr.cApproaches > 0)
                    AddFTDApproaches(cfr.dtFlight, cfr.cApproaches);
                if (cfr.cHolds > 0)
                    AddFTDHold(cfr.dtFlight, cfr.cHolds);
            }

            fcComboApproach6Month.AddRecentFlightEvents(cfr.dtFlight, cfr.cApproaches);
        }
        #endregion
    }

    public class NVCurrencyItem : FlightCurrency
    {
        /// <summary>
        /// When this expires, how many months are allowed before a proficiency check is required?
        /// </summary>
        public int MonthsBeforeProficiencyRequired { get; set; }

        /// <summary>
        /// Regardless of found events, does this need a proficiency check?
        /// </summary>
        public bool NeedsProficiencyCheck { get; set; }

        public NVCurrencyItem()
            : base()
        {
        }

        public NVCurrencyItem(decimal cOps, int cMonths, string szName, int monthsGapAllowed)
            : base(cOps, cMonths, true, szName)
        {
            MonthsBeforeProficiencyRequired = monthsGapAllowed;
        }

        public override CurrencyState CurrentState
        {
            get { return NeedsProficiencyCheck ? CurrencyState.NotCurrent : base.CurrentState; }
        }

        public override string DiscrepancyString
        {
            get
            {
                if (NeedsProficiencyCheck || DateTime.Compare(ExpirationDate.AddCalendarMonths(MonthsBeforeProficiencyRequired).Date, DateTime.Now.Date) < 0)
                    return Resources.Currency.NVProficiencyCheckRequired;
                else if (IsCurrent())
                    return string.Empty;
                else
                    return String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, Discrepancy, Discrepancy == 1 ? Resources.Currency.NVGoggleOperation : Resources.Currency.NVGoggleOperations);
            }
        }
    }

    /// <summary>
    /// Compute 61.57(f) currency
    /// Basic rule: 3 NV Goggle Operations within 2 calendar months to be PIC with passengers
    /// If Helicopter or Powered Lift, requirement is 6 ops
    /// If out of currency, can be PIC (no passengers) for 4 calendar months
    /// </summary>
    public class NVCurrency : IFlightExaminer
    {
        readonly NVCurrencyItem fcNVPassengers;
        readonly NVCurrencyItem fcNVNoPassengers;
        readonly NVCurrencyItem fcNVIPC;
        List<FlightCurrency> m_lstCurrencies;
        readonly CategoryClass.CatClassID m_ccid;
        readonly string szNVGeneric;

        public static bool IsHelicopterOrPoweredLift(CategoryClass.CatClassID ccid)
        {
            return ccid == CategoryClass.CatClassID.Helicopter || ccid == CategoryClass.CatClassID.PoweredLift;
        }

        public NVCurrency(CategoryClass.CatClassID ccid)
        {
            m_ccid = ccid;
            bool fIsHeliOrPowered = IsHelicopterOrPoweredLift(ccid);
            int requiredOps = fIsHeliOrPowered ? 6 : 3;
            fcNVPassengers = new NVCurrencyItem(requiredOps, 2, String.Format(CultureInfo.CurrentCulture, Resources.Currency.NVPIC, fIsHeliOrPowered ? Resources.Currency.NVHeliOrPoweredLift : Resources.Currency.NVAirplane, Resources.Currency.NVPassengers), 4);
            fcNVNoPassengers = new NVCurrencyItem(requiredOps, 4, String.Format(CultureInfo.CurrentCulture, Resources.Currency.NVPIC, fIsHeliOrPowered ? Resources.Currency.NVHeliOrPoweredLift : Resources.Currency.NVAirplane, Resources.Currency.NVNoPassengers), 0);
            fcNVIPC = new NVCurrencyItem(1, 4, "NV Proficiency Check", 0);
            szNVGeneric = String.Format(CultureInfo.CurrentCulture, Resources.Currency.NVPIC, fIsHeliOrPowered ? Resources.Currency.NVHeliOrPoweredLift : string.Empty, string.Empty);
            m_lstCurrencies = new List<FlightCurrency>();
        }

        public void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr.FlightProps == null)
                return;

            cfr.FlightProps.ForEachEvent((pe) =>
            {
                if (pe.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropNVGoggleOperations)
                {
                    // Since being current for a helicopter/powered lift requires doing hovering tasks (61.57(f)(1)(ii)),
                    // we add the events IF (a) they were performed in a heli/pl OR (b) this NVCurrency is not a heli/pl
                    if (IsHelicopterOrPoweredLift(cfr.idCatClassOverride) || !IsHelicopterOrPoweredLift(m_ccid))
                    {
                        fcNVPassengers.AddRecentFlightEvents(cfr.dtFlight, pe.IntValue);
                        fcNVNoPassengers.AddRecentFlightEvents(cfr.dtFlight, pe.IntValue);
                    }
                }
                else if (pe.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropNVGoggleProficiencyCheck)
                {
                    fcNVNoPassengers.AddRecentFlightEvents(cfr.dtFlight, fcNVNoPassengers.RequiredEvents);
                    fcNVPassengers.AddRecentFlightEvents(cfr.dtFlight, fcNVPassengers.RequiredEvents);
                    fcNVIPC.AddRecentFlightEvents(cfr.dtFlight, 1);
                }
            });
        }

        private void AddProficiencyRequired(DateTime dtExpired)
        {
            NVCurrencyItem nvci = new NVCurrencyItem(1, 0, szNVGeneric, 0) { NeedsProficiencyCheck = true };
            nvci.AddRecentFlightEvents(dtExpired, 1);
            m_lstCurrencies.Add(nvci);
        }

        /// <summary>
        /// Get all of the possible NV Flight currencies
        /// </summary>
        /// <returns></returns>
        public IEnumerable<FlightCurrency> CurrencyEvents
        {
            get
            {
                m_lstCurrencies = new List<FlightCurrency>();
                FlightCurrency fcNV = fcNVPassengers.OR(fcNVNoPassengers).OR(fcNVIPC);  // composite value

                if (fcNV.HasBeenCurrent)
                {
                    if (fcNV.CurrentState == CurrencyState.NotCurrent)
                        AddProficiencyRequired(fcNVNoPassengers.ExpirationDate);
                    else
                    {
                        // check for a gap that could indicate that a proficiency check is needed
                        CurrencyPeriod[] rgcpMissingProfCheck = fcNV.FindCurrencyGap(fcNVIPC.MostRecentEventDate, 0);
                        if (rgcpMissingProfCheck == null)   // no gap - we have potentially two currencies to return
                        {
                            m_lstCurrencies.Add(fcNVPassengers);
                            if (!fcNVPassengers.IsCurrent())
                                m_lstCurrencies.Add(fcNVNoPassengers);
                        }
                        else
                        {
                            // gap was found - we aren't actually current.
                            AddProficiencyRequired(rgcpMissingProfCheck[0].EndDate);
                        }
                    }
                }
                return m_lstCurrencies;
            }
        }
    }

    public class GliderIFRCurrency : CurrencyExaminer
    {
        readonly FlightCurrency fcGliderIFRTime = new FlightCurrency(1.0M, 6, true, "Instrument time in Glider or single-engine airplane with view limiting device");
        readonly FlightCurrency fcGliderIFRTimePassengers = new FlightCurrency(2.0M, 6, true, "Instrument time in a glider.");
        readonly FlightCurrency fcGliderInstrumentManeuvers = new FlightCurrency(2.0M, 6, true, "Instrument Maneuvers per 61.57(c)(6)(i) => (c)(3)(i)");
        readonly FlightCurrency fcGliderInstrumentPassengers = new FlightCurrency(1, 6, true, "Instrument Maneuvers required for passengers, per 61.57(c)(6)(ii) => (c)(3)(ii)");
        readonly FlightCurrency fcGliderIPC = new FlightCurrency(1, 6, true, "Glider IPC");

        private Boolean m_fCacheValid = false;

        Boolean m_fCurrentSolo = false;
        Boolean m_fCurrentPassengers = false;

        private string m_szDiscrepancy = "";
        private CurrencyState m_csCurrent = CurrencyState.NotCurrent;
        private DateTime m_dtExpiration = DateTime.MinValue;

        #region Adding Events
        /// <summary>
        /// Add an IPC or equivalent (e.g., instrument check ride)
        /// </summary>
        /// <param name="dt">Date of the IPC</param>
        public void AddIPC(DateTime dt)
        {
            fcGliderIPC.AddRecentFlightEvents(dt, 1);
            m_fCacheValid = false;
        }

        /// <summary>
        /// Add IFR time for part 61.57(c)(6)(i)(A) => (c)(3)(i)(A).  Can be in a glider OR ASEL under the hood
        /// </summary>
        /// <param name="dt">Date of the time</param>
        /// <param name="time">Amount of time to add</param>
        public void AddIFRTime(DateTime dt, Decimal time)
        {
            fcGliderIFRTime.AddRecentFlightEvents(dt, time);
            m_fCacheValid = false;
        }

        /// <summary>
        /// Add IFR time for part 61.57(c)(6)(ii)(A) => (c)(3)(ii)(A).  MUST BE IN A GLIDER
        /// </summary>
        /// <param name="dt">Date of the time</param>
        /// <param name="time">Amount of time to add</param>
        public void AddIFRTimePassengers(DateTime dt, Decimal time)
        {
            fcGliderIFRTimePassengers.AddRecentFlightEvents(dt, time);
            m_fCacheValid = false;
        }

        /// <summary>
        /// Add maneuvering time for part 61.57(c)(6)(i)(B) => (c)(3)(i)(B).  Can be in a glider or single-engine airplane simulated IMC.
        /// </summary>
        /// <param name="dt">Date of the time</param>
        /// <param name="time">Amount of time to add</param>
        public void AddManeuverTime(DateTime dt, Decimal time)
        {
            fcGliderInstrumentManeuvers.AddRecentFlightEvents(dt, time);
            m_fCacheValid = false;
        }

        /// <summary>
        /// Add performance maneuvers for part 61.57(c)(6)(ii)(B) => (c)(3)(ii)(B).  Does NOT appear to need to have been in a glider.
        /// </summary>
        /// <param name="dt"></param>
        public void AddPerformanceManeuvers(DateTime dt)
        {
            fcGliderInstrumentPassengers.AddRecentFlightEvents(dt, 1);
            m_fCacheValid = false;
        }
        #endregion

        public GliderIFRCurrency()
        {
        }

        /// <summary>
        /// Computes the overall currency - DEPRECATED
        /// </summary>
        public void RefreshCurrencyOLD()
        {
            // Compute currency according to 61.57(c)(6) (i) and (ii)  => (c)(3) including each one's expiration.  

            if (m_fCacheValid)
                return;

            m_szDiscrepancy = string.Empty;
            m_csCurrent = CurrencyState.NotCurrent;
            m_dtExpiration = DateTime.MinValue;

            DateTime dtExpirationSolo;
            DateTime dtExpirationPassengers;

            CurrencyState cs6157c6iA = fcGliderIFRTime.CurrentState;
            CurrencyState cs6157c6iB = fcGliderInstrumentManeuvers.CurrentState;
            CurrencyState cs6157c6iiA = fcGliderIFRTimePassengers.CurrentState;
            CurrencyState cs6157c6iiB = fcGliderInstrumentPassengers.CurrentState;
            // 61.57(c)(6)(i)  => (c)(3)(i) - basic instrument currency
            if (fcGliderIFRTime.HasBeenCurrent && fcGliderInstrumentManeuvers.HasBeenCurrent)
            {
                // find the earliest expiration
                DateTime dtExpIFRTime = fcGliderIFRTime.ExpirationDate;
                DateTime dtExpInstMan = fcGliderInstrumentManeuvers.ExpirationDate;

                dtExpirationSolo = dtExpIFRTime;
                dtExpirationSolo = dtExpInstMan.EarlierDate(dtExpirationSolo);
                m_dtExpiration = dtExpirationSolo;

                m_csCurrent = FlightCurrency.IsAlmostCurrent(m_dtExpiration) ? CurrencyState.GettingClose :
                    (FlightCurrency.IsCurrent(m_dtExpiration) ? CurrencyState.OK : CurrencyState.NotCurrent);

                m_fCurrentSolo = (m_csCurrent != CurrencyState.NotCurrent);

                // at this point we've determined if you're current for solo (61.57(c)(6)(i))  => (c)(3)(i); now see if we can carry passengers
                if (m_fCurrentSolo && fcGliderIFRTimePassengers.HasBeenCurrent && fcGliderInstrumentPassengers.HasBeenCurrent)
                {
                    DateTime dtExpIFRTimePassengers = fcGliderIFRTimePassengers.ExpirationDate;
                    DateTime dtExpIFRPassengers = fcGliderInstrumentPassengers.ExpirationDate;

                    dtExpirationPassengers = dtExpIFRTimePassengers.EarlierDate(dtExpIFRPassengers);

                    CurrencyState csPassengers = FlightCurrency.IsAlmostCurrent(dtExpirationPassengers) ? CurrencyState.GettingClose :
                        (FlightCurrency.IsCurrent(dtExpirationPassengers) ? CurrencyState.OK : CurrencyState.NotCurrent);

                    // if current for passengers, then we are the more restrictive of overall close to losing currency or fully current.
                    if (m_fCurrentPassengers = (csPassengers != CurrencyState.NotCurrent))
                    {
                        m_csCurrent = (m_csCurrent == CurrencyState.OK && csPassengers == CurrencyState.OK) ? CurrencyState.OK : CurrencyState.GettingClose;
                        m_dtExpiration = dtExpirationPassengers.EarlierDate(dtExpirationSolo);
                    }
                }
            }

            // IPC can also set currency.
            if (fcGliderIPC.HasBeenCurrent)
            {
                // set currency to the most permissive of either current state or IPC state
                m_csCurrent = (fcGliderIPC.CurrentState > m_csCurrent) ? fcGliderIPC.CurrentState : m_csCurrent;

                // Expiration date is the LATER of the current expiration date OR the IPC expiration date, with one exception below.
                m_dtExpiration = fcGliderIPC.ExpirationDate.LaterDate(m_dtExpiration);

                // if you have a valid IPC, you are currently valid for both passengers and solo.
                // however, IPC could expire before the solo requirements expire, so if it is the 
                // IPC that is driving passengers, use the IPC date as the expiration date.  Otherwise, use the later one.
                // That way, when the IPC expires, your passenger privs expire too, so you'll see a new "no passengers" priv remaining
                // with a new date.
                if (fcGliderIPC.IsCurrent())
                {
                    if (m_fCurrentSolo && !m_fCurrentPassengers)
                        m_dtExpiration = fcGliderIPC.ExpirationDate;
                    m_fCurrentPassengers = m_fCurrentSolo = true;
                }
            }

            m_fCacheValid = true;
        }

        /// <summary>
        /// Computes the overall currency
        /// </summary>
        public void RefreshCurrency()
        {
            // Compute currency according to 61.57(c)(6) ( => (c)(3)) (i) and (ii) including each one's expiration.  

            if (m_fCacheValid)
                return;

            // 61.57(c)(6)(i) => (c)(3)(i) -  no passengers.  IPC covers this.
            FlightCurrency fc6157c6i = fcGliderIFRTime.AND(fcGliderInstrumentManeuvers).OR(fcGliderIPC);

            // 61.57(c)(6)(ii) => (c)(3)(ii) -  passengers.  Above + two more.  Again, IPC covers this too.
            FlightCurrency fc6157c6ii = fc6157c6i.AND(fcGliderIFRTimePassengers).AND(fcGliderInstrumentPassengers).OR(fcGliderIPC);

            m_fCurrentSolo = fc6157c6i.IsCurrent();
            m_fCurrentPassengers = fc6157c6ii.IsCurrent();

            if (m_fCurrentSolo || m_fCurrentPassengers)
            {
                if (m_fCurrentPassengers)
                {
                    m_csCurrent = fc6157c6ii.CurrentState;
                    m_dtExpiration = fc6157c6ii.ExpirationDate;
                    m_szDiscrepancy = fc6157c6ii.DiscrepancyString;
                }
                else // must be current solo)
                {
                    m_csCurrent = fc6157c6i.CurrentState;
                    m_dtExpiration = fc6157c6i.ExpirationDate;
                    m_szDiscrepancy = fc6157c6i.DiscrepancyString;
                }

                // check to see if there is an embedded gap that needs an IPC
                CurrencyPeriod[] rgcpMissingIPC = fc6157c6i.FindCurrencyGap(fcGliderIPC.MostRecentEventDate, 6);
                if (rgcpMissingIPC != null && m_szDiscrepancy.Length == 0)
                    m_szDiscrepancy = String.Format(CultureInfo.CurrentCulture, Resources.Currency.IPCMayBeRequired, rgcpMissingIPC[0].EndDate.ToShortDateString(), rgcpMissingIPC[1].StartDate.ToShortDateString());
            }
            else
            {
                // And expiration date is the later of the passenger/no-passenger date
                m_dtExpiration = fc6157c6i.ExpirationDate.LaterDate(fc6157c6ii.ExpirationDate);

                // see if we need an IPC
                // if more than 6 calendar months have passed since expiration, an IPC is required.
                // otherwise, just name the one that is short
                if (DateTime.Compare(m_dtExpiration.AddCalendarMonths(6).Date, DateTime.Now.Date) > 0)
                    m_szDiscrepancy = String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplateGliderIFRPassengers, fcGliderIFRTime.Discrepancy, fcGliderInstrumentManeuvers.Discrepancy);
                else
                    m_szDiscrepancy = Resources.Currency.IPCRequired;
            }

            m_fCacheValid = true;
        }

        /// <summary>
        /// The highest privilege for which you are current.
        /// </summary>
        public string Privilege
        {
            get
            {
                RefreshCurrency();
                if (m_fCurrentPassengers)
                    return Resources.Currency.IFRGliderPassengers;
                else if (m_fCurrentSolo)
                    return Resources.Currency.IFRGliderNoPassengers;
                else
                    return "";
            }
        }

        #region CurrencyExaminer overrides
        /// <summary>
        /// Returns the requirements to get current again.  If you are not current, it always assumes you will do 66-HIT 
        /// (6 in the last 6 months of holds, intercepting, and tracking and approaches), even though alternative methods may
        /// be valid.
        /// </summary>
        /// <returns>The display string for what is needed to restore currency</returns>
        public override string DiscrepancyString
        {
            get
            {
                RefreshCurrency();
                return m_szDiscrepancy;
            }
        }

        public override string StatusDisplay
        {
            get
            {
                RefreshCurrency();
                return String.Format(CultureInfo.CurrentCulture, FlightCurrency.StatusDisplayForDate(m_dtExpiration));
            }
        }

        /// <summary>
        /// Retrieves the current IFR currency state.
        /// </summary>
        /// <returns></returns>
        public override CurrencyState CurrentState
        {
            get
            {
                RefreshCurrency();
                return m_csCurrent;
            }
        }

        /// <summary>
        /// Has this ever registered currency?
        /// </summary>
        /// <returns></returns>
        public override Boolean HasBeenCurrent
        {
            get
            {
                RefreshCurrency();
                return m_dtExpiration.HasValue();
            }
        }

        public override DateTime ExpirationDate { get { return m_dtExpiration; } }
        #endregion

        #region FlightExaminer Interface
        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (cfr.fIsSingleEngine || cfr.fIsGlider)
            {
                if (cfr.IMC + cfr.IMCSim > 0)
                {
                    // 61.57(c)(6)(i)(A) => (c)(3)(i)(A)
                    AddIFRTime(cfr.dtFlight, cfr.IMC + cfr.IMCSim);

                    // 61.57(c)(6)(ii)(A) => (c)(3)(ii)(A)
                    if (cfr.fIsGlider)
                        AddIFRTimePassengers(cfr.dtFlight, cfr.IMC + cfr.IMCSim);
                }

                cfr.FlightProps.ForEachEvent((pfe) =>
                {
                    // 61.57(c)(6)(i)(B) => (c)(3)(i)(B)
                    if (pfe.PropertyType.IsGliderInstrumentManeuvers)
                        AddManeuverTime(cfr.dtFlight, pfe.DecValue);

                    if (cfr.fIsGlider)
                    {
                        // 61.57(c)(ii)(B) - does not seem to require being in a glider, but we'll require it to be safe.
                        if (pfe.PropertyType.IsGliderInstrumentManeuversPassengers)
                            AddPerformanceManeuvers(cfr.dtFlight);
                    }

                    // IPC can be in EITHER ASEL OR Glider but MUST be a real aircraft
                    if (pfe.PropertyType.IsIPC && cfr.fIsRealAircraft)
                        AddIPC(cfr.dtFlight);
                });
            }
        }
        #endregion
    }
    #endregion
}
