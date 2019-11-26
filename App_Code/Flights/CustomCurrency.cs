using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

/******************************************************
 * 
 * Copyright (c) 2007-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightCurrency
{
    public enum CustomCurrencyTimespanType
    {
        Days, CalendarMonths,
        TwelveMonthJan, TwelveMonthFeb, TwelveMonthMar, TwelveMonthApr, TwelveMonthMay, TwelveMonthJun, TwelveMonthJul, TwelveMonthAug, TwelveMonthSep, TwelveMonthOct, TwelveMonthNov, TwelveMonthDec,
        SixMonthJan, SixMonthFeb, SixMonthMar, SixMonthApr, SixMonthMay, SixMonthJun,
        FourMonthJan, FourMonthFeb, FourMonthMar, FourMonthApr,
        ThreeMonthJan, ThreeMonthFeb, ThreeMonthMar
    }

    public static class CustomCurrencyTimespanExtensions
    {
        #region extension methods for CustomCurrencyTimespanTypeTimespanType
        /// <summary>
        /// For fixed-range timespan ranges, returns the month (1 = Jan) for alignment
        /// </summary>
        /// <param name="tst"></param>
        /// <returns></returns>
        public static int AlignmentMonth(this CustomCurrencyTimespanType tst)
        {
            switch (tst)
            {
                default:
                    throw new ArgumentException("Unknown duration for TimeSpanType: {0}", tst.ToString());
                case CustomCurrencyTimespanType.CalendarMonths:
                case CustomCurrencyTimespanType.Days:
                    throw new ArgumentException("CalendarMonths or days do not have an alignment date");
                case CustomCurrencyTimespanType.ThreeMonthJan:
                case CustomCurrencyTimespanType.FourMonthJan:
                case CustomCurrencyTimespanType.SixMonthJan:
                case CustomCurrencyTimespanType.TwelveMonthJan:
                    return 1;
                case CustomCurrencyTimespanType.ThreeMonthFeb:
                case CustomCurrencyTimespanType.FourMonthFeb:
                case CustomCurrencyTimespanType.SixMonthFeb:
                case CustomCurrencyTimespanType.TwelveMonthFeb:
                    return 2;
                case CustomCurrencyTimespanType.ThreeMonthMar:
                case CustomCurrencyTimespanType.FourMonthMar:
                case CustomCurrencyTimespanType.SixMonthMar:
                case CustomCurrencyTimespanType.TwelveMonthMar:
                    return 3;
                case CustomCurrencyTimespanType.FourMonthApr:
                case CustomCurrencyTimespanType.SixMonthApr:
                case CustomCurrencyTimespanType.TwelveMonthApr:
                    return 4;
                case CustomCurrencyTimespanType.SixMonthMay:
                case CustomCurrencyTimespanType.TwelveMonthMay:
                    return 5;
                case CustomCurrencyTimespanType.SixMonthJun:
                case CustomCurrencyTimespanType.TwelveMonthJun:
                    return 6;
                case CustomCurrencyTimespanType.TwelveMonthJul:
                    return 7;
                case CustomCurrencyTimespanType.TwelveMonthAug:
                    return 8;
                case CustomCurrencyTimespanType.TwelveMonthSep:
                    return 9;
                case CustomCurrencyTimespanType.TwelveMonthOct:
                    return 10;
                case CustomCurrencyTimespanType.TwelveMonthNov:
                    return 11;
                case CustomCurrencyTimespanType.TwelveMonthDec:
                    return 12;
            }
        }

        /// <summary>
        /// Returns the number of months duration for the specified timespantype
        /// </summary>
        /// <param name="tst"></param>
        /// <returns></returns>
        public static int Duration(this CustomCurrencyTimespanType tst)
        {
            switch (tst)
            {
                default:
                    throw new ArgumentException("Unknown duration for TimeSpanType: {0}", tst.ToString());
                case CustomCurrencyTimespanType.Days:
                    throw new ArgumentException("Days do not have a duration of months");
                case CustomCurrencyTimespanType.CalendarMonths:
                    return 1;
                case CustomCurrencyTimespanType.ThreeMonthJan:
                case CustomCurrencyTimespanType.ThreeMonthFeb:
                case CustomCurrencyTimespanType.ThreeMonthMar:
                    return 3;
                case CustomCurrencyTimespanType.FourMonthJan:
                case CustomCurrencyTimespanType.FourMonthFeb:
                case CustomCurrencyTimespanType.FourMonthMar:
                case CustomCurrencyTimespanType.FourMonthApr:
                    return 4;
                case CustomCurrencyTimespanType.SixMonthJan:
                case CustomCurrencyTimespanType.SixMonthFeb:
                case CustomCurrencyTimespanType.SixMonthMar:
                case CustomCurrencyTimespanType.SixMonthApr:
                case CustomCurrencyTimespanType.SixMonthMay:
                case CustomCurrencyTimespanType.SixMonthJun:
                    return 6;
                case CustomCurrencyTimespanType.TwelveMonthJan:
                case CustomCurrencyTimespanType.TwelveMonthFeb:
                case CustomCurrencyTimespanType.TwelveMonthMar:
                case CustomCurrencyTimespanType.TwelveMonthApr:
                case CustomCurrencyTimespanType.TwelveMonthMay:
                case CustomCurrencyTimespanType.TwelveMonthJun:
                case CustomCurrencyTimespanType.TwelveMonthJul:
                case CustomCurrencyTimespanType.TwelveMonthAug:
                case CustomCurrencyTimespanType.TwelveMonthSep:
                case CustomCurrencyTimespanType.TwelveMonthOct:
                case CustomCurrencyTimespanType.TwelveMonthNov:
                case CustomCurrencyTimespanType.TwelveMonthDec:
                    return 12;
            }
        }

        /// <summary>
        /// Human readable string for the timespan type
        /// </summary>
        /// <param name="tst"></param>
        /// <returns></returns>
        public static string DisplayString(this CustomCurrencyTimespanType tst)
        {
            bool isAligned = tst.IsAligned();
            int alignMonth = isAligned ? tst.AlignmentMonth() : 1;
            int duration = isAligned ? tst.Duration() : 1;
            DateTime dt1 = new DateTime(DateTime.Now.Year, alignMonth, 1);
            DateTime dt2 = dt1.AddMonths(duration);
            DateTime dt3 = dt2.AddMonths(duration);
            DateTime dt4 = dt3.AddMonths(duration);
            switch (tst)
            {
                default:
                    throw new ArgumentException("Unknown duration for TimeSpanType: {0}", tst.ToString());
                case CustomCurrencyTimespanType.CalendarMonths:
                    return Resources.Currency.CustomCurrencyMonths;
                case CustomCurrencyTimespanType.Days:
                    return Resources.Currency.CustomCurrencyDays;
                case CustomCurrencyTimespanType.ThreeMonthJan:
                case CustomCurrencyTimespanType.ThreeMonthFeb:
                case CustomCurrencyTimespanType.ThreeMonthMar:
                    return String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrency3Month, dt1.ToString("MMM", CultureInfo.CurrentCulture), dt2.ToString("MMM", CultureInfo.CurrentCulture), dt3.ToString("MMM", CultureInfo.CurrentCulture), dt4.ToString("MMM", CultureInfo.CurrentCulture));
                case CustomCurrencyTimespanType.FourMonthJan:
                case CustomCurrencyTimespanType.FourMonthFeb:
                case CustomCurrencyTimespanType.FourMonthMar:
                case CustomCurrencyTimespanType.FourMonthApr:
                    return String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrency4Month, dt1.ToString("MMM", CultureInfo.CurrentCulture), dt2.ToString("MMM", CultureInfo.CurrentCulture), dt3.ToString("MMM", CultureInfo.CurrentCulture));
                case CustomCurrencyTimespanType.SixMonthJan:
                case CustomCurrencyTimespanType.SixMonthFeb:
                case CustomCurrencyTimespanType.SixMonthMar:
                case CustomCurrencyTimespanType.SixMonthApr:
                case CustomCurrencyTimespanType.SixMonthMay:
                case CustomCurrencyTimespanType.SixMonthJun:
                    return String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrency6Month, dt1.ToString("MMM", CultureInfo.CurrentCulture), dt2.ToString("MMM", CultureInfo.CurrentCulture));
                case CustomCurrencyTimespanType.TwelveMonthJan:
                case CustomCurrencyTimespanType.TwelveMonthFeb:
                case CustomCurrencyTimespanType.TwelveMonthMar:
                case CustomCurrencyTimespanType.TwelveMonthApr:
                case CustomCurrencyTimespanType.TwelveMonthMay:
                case CustomCurrencyTimespanType.TwelveMonthJun:
                case CustomCurrencyTimespanType.TwelveMonthJul:
                case CustomCurrencyTimespanType.TwelveMonthAug:
                case CustomCurrencyTimespanType.TwelveMonthSep:
                case CustomCurrencyTimespanType.TwelveMonthOct:
                case CustomCurrencyTimespanType.TwelveMonthNov:
                case CustomCurrencyTimespanType.TwelveMonthDec:
                    return String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrency12Month, dt1.ToString("MMM", CultureInfo.CurrentCulture));
            }
        }

        /// <summary>
        /// True if this is an aligned bucket (vs. normal currency computations which look back from today)
        /// </summary>
        /// <param name="tst"></param>
        /// <returns></returns>
        public static bool IsAligned(this CustomCurrencyTimespanType tst)
        {
            return tst != CustomCurrencyTimespanType.Days && tst != CustomCurrencyTimespanType.CalendarMonths;
        }
        #endregion
    }

    /// <summary>
    /// Local class to pair a singular/plural name with an event type.
    /// </summary>
    public static class CustomCurrencyEventTypeExtensions
    {
        #region extension methods for CustomCurrencyEvenType
        private static Dictionary<CustomCurrency.CustomCurrencyEventType, string[]> s_eventTypeLabels = null;

        private static string[] LabelsForEventType(CustomCurrency.CustomCurrencyEventType ccet)
        {
            if (s_eventTypeLabels == null)
            {
                s_eventTypeLabels = new Dictionary<CustomCurrency.CustomCurrencyEventType, string[]>()
                    {
                        { CustomCurrency.CustomCurrencyEventType.Flights, new string[] { Resources.Currency.CustomCurrencyEventFlight, Resources.Currency.CustomCurrencyEventFlights } },
                        { CustomCurrency.CustomCurrencyEventType.Landings, new string[] { Resources.Currency.CustomCurrencyEventLanding, Resources.Currency.CustomCurrencyEventLandings } },
                        { CustomCurrency.CustomCurrencyEventType.Hours, new string[] { Resources.Currency.CustomCurrencyEventHour, Resources.Currency.CustomCurrencyEventHours } },
                        { CustomCurrency.CustomCurrencyEventType.IFRHours, new string[] { Resources.Currency.CustomCurrencyEventInstrumentHour, Resources.Currency.CustomCurrencyEventInstrumentHours } },
                        { CustomCurrency.CustomCurrencyEventType.IFRApproaches, new string[] { Resources.Currency.CustomCurrencyEventApproach, Resources.Currency.CustomCurrencyEventApproaches } },
                        { CustomCurrency.CustomCurrencyEventType.BaseCheck, new string[] { Resources.Currency.CustomCurrencyEventBaseCheck, Resources.Currency.CustomCurrencyEventBaseChecks } },
                        { CustomCurrency.CustomCurrencyEventType.UASLaunch, new string[] { Resources.Currency.CustomCurrencyEventLaunch, Resources.Currency.CustomCurrencyEventLaunches } },
                        { CustomCurrency.CustomCurrencyEventType.UASRecovery, new string[] { Resources.Currency.CustomCurrencyEventRecovery, Resources.Currency.CustomCurrencyEventRecoveries } },
                        { CustomCurrency.CustomCurrencyEventType.NightLandings, new string[] { Resources.Currency.CustomCurrencyEventNightLanding, Resources.Currency.CustomCurrencyEventNightLandings } },
                        { CustomCurrency.CustomCurrencyEventType.NightTakeoffs, new string[] { Resources.Currency.CustomCurrencyEventNightTakeoff, Resources.Currency.CustomCurrencyEventNightTakeoffs } },
                        { CustomCurrency.CustomCurrencyEventType.PICLandings, new string[] { Resources.Currency.CustomCurrencyEventLandingPIC, Resources.Currency.CustomCurrencyEventLandingsPIC } },
                        { CustomCurrency.CustomCurrencyEventType.PICNightLandings, new string[] { Resources.Currency.CustomCurrencyEventNightLandingPIC, Resources.Currency.CustomCurrencyEventNightLandingsPIC } },
                        { CustomCurrency.CustomCurrencyEventType.TotalHours, new string[] { Resources.Currency.CustomCurrencyEventTotalHour, Resources.Currency.CustomCurrencyEventTotalHours } },
                        { CustomCurrency.CustomCurrencyEventType.GroundSimHours, new string[] { Resources.Currency.CustomCurrencyEventSimulatorHour, Resources.Currency.CustomCurrencyEventSimulatorHours } },
                        { CustomCurrency.CustomCurrencyEventType.BackseatHours, new string[] { Resources.Currency.CustomCurrencyEventBackSeatHour, Resources.Currency.CustomCurrencyEventBackSeatHours } },
                        { CustomCurrency.CustomCurrencyEventType.FrontSeatHours, new string[] { Resources.Currency.CustomCurrencyEventFrontSeatHour, Resources.Currency.CustomCurrencyEventFrontSeatHours } },
                        { CustomCurrency.CustomCurrencyEventType.HoistOperations, new string[] { Resources.Currency.CustomCurrencyHoistOperation, Resources.Currency.CustomCurrencyHoistOperations } },
                        { CustomCurrency.CustomCurrencyEventType.NVHours, new string[] { Resources.Currency.CustomCurrencyEventNVHour, Resources.Currency.CustomCurrencyEventNVHours } },
                        { CustomCurrency.CustomCurrencyEventType.NVGoggles, new string[] { Resources.Currency.CustomCurrencyEventNVGHour, Resources.Currency.CustomCurrencyEventNVGHours } },
                        { CustomCurrency.CustomCurrencyEventType.NVFLIR, new string[] { Resources.Currency.CustomCurrencyEventNVSHour, Resources.Currency.CustomCurrencyEventNVSHours } },
                        { CustomCurrency.CustomCurrencyEventType.LandingsHighAltitude, new string[] { Resources.Currency.CustomCurrencyEventHighAltitudeLanding, Resources.Currency.CustomCurrencyEventHighAltitudeLandings } },
                        { CustomCurrency.CustomCurrencyEventType.HoursDual, new string[] { Resources.Currency.CustomCurrencyEventDualHour, Resources.Currency.CustomCurrencyEventDualHours } },
                        { CustomCurrency.CustomCurrencyEventType.NightFlight, new string[] { Resources.Currency.CustomCurrencyEventNightHour, Resources.Currency.CustomCurrencyEventNightHours } },
                        { CustomCurrency.CustomCurrencyEventType.CAP5Checkride, new string[] { Resources.Currency.CustomCurrencyEventCap5Checkride, Resources.Currency.CustomCurrencyEventCap5Checkrides } },
                        { CustomCurrency.CustomCurrencyEventType.CAP91Checkride, new string[] { Resources.Currency.CustomCurrencyEventCap91Checkride, Resources.Currency.CustomCurrencyEventCap91Checkrides } },
                        { CustomCurrency.CustomCurrencyEventType.FMSApproaches, new string[] { Resources.Currency.CustomCurrencyEventFMSApproach, Resources.Currency.CustomCurrencyEventFMSApproaches } },
                        { CustomCurrency.CustomCurrencyEventType.NightTouchAndGo, new string[] {Resources.Currency.CustomCurrencyEventNightTouchAndGo, Resources.Currency.CustomCurrencyEventNightTouchAndGos} },
                        { CustomCurrency.CustomCurrencyEventType.GliderTow, new string[] {Resources.Currency.CustomCurrencyEventGliderTow, Resources.Currency.CustomCurrencyEventGliderTows } },
                        { CustomCurrency.CustomCurrencyEventType.IPC, new string[] {Resources.Currency.CustomCurrencyEventIPC, Resources.Currency.CustomCurrencyEventIPC } },
                        { CustomCurrency.CustomCurrencyEventType.FlightReview, new string[] {Resources.Currency.CustomCurrencyEventFlightReview, Resources.Currency.CustomCurrencyEventFlightReview } },
                        { CustomCurrency.CustomCurrencyEventType.NightLandingAny, new string[] {Resources.Currency.CustomCurrencyEventNightLandingAny, Resources.Currency.CustomCurrencyEventNightLandingsAny } }
                    };
            }

            return s_eventTypeLabels.ContainsKey(ccet) ? s_eventTypeLabels[ccet] : null;
        }

        /// <summary>
        /// Description for the event type when there is one event.  E.g., "1 flight"
        /// </summary>
        /// <param name="ccet"></param>
        /// <returns></returns>
        public static string SingularName(this CustomCurrency.CustomCurrencyEventType ccet)
        {
            string[] rgsz = LabelsForEventType(ccet);
            return (rgsz == null || rgsz.Length < 1) ? ccet.ToString() : rgsz[0];
        }

        /// <summary>
        /// Description for the event type when there are multiple events.  E.g., "2 flights"
        /// </summary>
        /// <param name="ccet"></param>
        /// <returns></returns>
        public static string PluralName(this CustomCurrency.CustomCurrencyEventType ccet)
        {
            string[] rgsz = LabelsForEventType(ccet);
            return (rgsz == null || rgsz.Length < 2) ? ccet.ToString() : rgsz[1];
        }

        public static bool IsIntegerOnly(this CustomCurrency.CustomCurrencyEventType ccet)
        {
            switch (ccet)
            {
                case CustomCurrency.CustomCurrencyEventType.Flights:
                case CustomCurrency.CustomCurrencyEventType.Landings:
                case CustomCurrency.CustomCurrencyEventType.IFRApproaches:
                case CustomCurrency.CustomCurrencyEventType.BaseCheck:
                case CustomCurrency.CustomCurrencyEventType.UASLaunch:
                case CustomCurrency.CustomCurrencyEventType.UASRecovery:
                case CustomCurrency.CustomCurrencyEventType.NightLandings:
                case CustomCurrency.CustomCurrencyEventType.NightTouchAndGo:
                case CustomCurrency.CustomCurrencyEventType.NightLandingAny:
                case CustomCurrency.CustomCurrencyEventType.NightTakeoffs:
                case CustomCurrency.CustomCurrencyEventType.PICLandings:
                case CustomCurrency.CustomCurrencyEventType.PICNightLandings:
                case CustomCurrency.CustomCurrencyEventType.HoistOperations:
                case CustomCurrency.CustomCurrencyEventType.LandingsHighAltitude:
                case CustomCurrency.CustomCurrencyEventType.CAP5Checkride:
                case CustomCurrency.CustomCurrencyEventType.CAP91Checkride:
                case CustomCurrency.CustomCurrencyEventType.FMSApproaches:
                case CustomCurrency.CustomCurrencyEventType.GliderTow:
                case CustomCurrency.CustomCurrencyEventType.IPC:
                case CustomCurrency.CustomCurrencyEventType.FlightReview:
                    return true;
                case CustomCurrency.CustomCurrencyEventType.TotalHours:
                case CustomCurrency.CustomCurrencyEventType.Hours:
                case CustomCurrency.CustomCurrencyEventType.IFRHours:
                case CustomCurrency.CustomCurrencyEventType.GroundSimHours:
                case CustomCurrency.CustomCurrencyEventType.BackseatHours:
                case CustomCurrency.CustomCurrencyEventType.FrontSeatHours:
                case CustomCurrency.CustomCurrencyEventType.NVHours:
                case CustomCurrency.CustomCurrencyEventType.NVGoggles:
                case CustomCurrency.CustomCurrencyEventType.NVFLIR:
                case CustomCurrency.CustomCurrencyEventType.NightFlight:
                case CustomCurrency.CustomCurrencyEventType.HoursDual:
                default:
                    return false;
            }
        }
        #endregion
    }

    /// <summary>
    /// Currencies that are defined by the user.
    /// </summary>
    public class CustomCurrency : FlightCurrency
    {
        /// <summary>
        /// Indicates the type of event we are looking for
        /// </summary>
        public enum CustomCurrencyEventType
        {
            // NOTE: DO NOT REORDER THESE, THEY ARE PERSISTED IN THE DATABASE
            Flights = 0,
            Landings = 1,
            Hours = 2,
            IFRHours = 3,
            IFRApproaches = 4,
            BaseCheck = 5,
            UASLaunch = 6,
            UASRecovery = 7,
            NightLandings = 8,
            NightTakeoffs = 9,
            PICLandings = 10,
            PICNightLandings = 11,
            TotalHours = 12,
            GroundSimHours = 13,
            BackseatHours = 14,
            FrontSeatHours = 15,
            HoistOperations = 16,
            NVHours = 17,
            NVGoggles = 18,
            NVFLIR = 19,
            LandingsHighAltitude = 20,
            NightFlight = 21,
            CAP5Checkride = 22,
            CAP91Checkride = 23,
            FMSApproaches = 24,
            HoursDual = 25,
            NightTouchAndGo = 26,
            GliderTow = 27,
            IPC = 28,
            FlightReview = 29,
            NightLandingAny = 30
        };

        public enum CurrencyRefType { Aircraft = 0, Models = 1, Properties = 2 };

        /// <summary>
        /// Is this a required minimum number of events, or a "do not exceed?"
        /// </summary>
        public enum LimitType { Minimum, Maximum };

        private const int ID_UNKNOWN = -1;

        readonly string m_szTailNumbers, m_szModelNames;
        string m_szCatClass;
        readonly string m_szPropertyNames;

        #region Initialization
        public CustomCurrency()
        {
            ModelsRestriction = new List<int>();
            AircraftRestriction = new List<int>();
            ID = ID_UNKNOWN;
            m_szTailNumbers = m_szModelNames = m_szCatClass = ErrorString = UserName = CategoryRestriction = string.Empty;
            TimespanType = CustomCurrencyTimespanType.Days;
            AlignedStartDate = null;
            CurrencyLimitType = LimitType.Minimum;
        }

        private CustomCurrency(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException("dr");

            ID = Convert.ToInt32(dr["idCurrency"], CultureInfo.InvariantCulture);
            UserName = dr["username"].ToString();
            DisplayName = dr["name"].ToString();
            Discrepancy = RequiredEvents = Convert.ToDecimal(dr["minEvents"], CultureInfo.InvariantCulture);
            CurrencyLimitType = (LimitType)Convert.ToInt32(dr["limitType"], CultureInfo.InvariantCulture);

            // MUST do calendar month THEN set Expiration span property, NOT the member variable.  This sets dtEarliest.
            TimespanType = (CustomCurrencyTimespanType)Convert.ToInt32(dr["timespanType"], CultureInfo.InvariantCulture);
            IsCalendarMonth = (TimespanType == CustomCurrencyTimespanType.CalendarMonths);    // not entirely true if aligned

            if (TimespanType.IsAligned())
            {
                int mNow = DateTime.Now.Month;
                int mAlign = TimespanType.AlignmentMonth();
                int d = TimespanType.Duration();
                int y = DateTime.Now.Year;

                if (mNow < mAlign)
                {
                    mNow += 12;
                    y--;
                }
                AlignedStartDate = new DateTime(y, mNow - ((mNow - mAlign) % d), 1);

                // Set the expiration span in days
                ExpirationSpan = AlignedStartDate.Value.AddMonths(d).Subtract(AlignedStartDate.Value).Days;
            }
            else
                ExpirationSpan = Convert.ToInt32(dr["timespan"], CultureInfo.InvariantCulture);

            EventType = (CustomCurrencyEventType)Convert.ToInt32(dr["eventType"], CultureInfo.InvariantCulture);
            CategoryRestriction = dr["categoryRestriction"].ToString();
            CatClassRestriction = (CategoryClass.CatClassID)Convert.ToInt32(dr["catClassRestriction"], CultureInfo.InvariantCulture);
            AirportRestriction = dr["airportRestriction"].ToString();
            TextRestriction = dr["textRestriction"].ToString();

            ModelsRestriction = new List<int>(dr["ModelsRestriction"].ToString().ToInts());
            AircraftRestriction = new List<int>(dr["AircraftRestriction"].ToString().ToInts());
            PropertyRestriction = new List<int>(dr["PropertyRestriction"].ToString().ToInts());

            // convenience properties - not persisted
            m_szTailNumbers = dr["AircraftDisplay"].ToString();
            m_szModelNames = dr["ModelsDisplay"].ToString();
            m_szCatClass = dr["CatClassDisplay"].ToString();
            m_szPropertyNames = dr["PropertyDisplay"].ToString();
        }
        #endregion
        
        #region Properties
        /// <summary>
        /// Last error
        /// </summary>
        public string ErrorString { get; set; }

        /// <summary>
        /// ID for the currency
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Username of the owner
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Type of event which contributes currency
        /// </summary>
        public CustomCurrencyEventType EventType { get; set; }

        /// <summary>
        /// Name of the category to which this applies, null or empty for any
        /// </summary>
        public string CategoryRestriction { get; set; }

        /// <summary>
        /// ID of the catclass to which this applies, 0 for any
        /// </summary>
        public CategoryClass.CatClassID CatClassRestriction { get; set; }

        /// <summary>
        /// Airport which must be visited
        /// </summary>
        public string AirportRestriction { get; set; }

        /// <summary>
        /// Text contained in the comments field or any text properties
        /// </summary>
        public string TextRestriction { get; set; }

        /// <summary>
        /// ID's of properties that must be present
        /// </summary>
        public IEnumerable<int> PropertyRestriction { get; set; }

        override public string DiscrepancyString
        {
            get
            {
                if (CurrencyLimitType == LimitType.Maximum)
                    return string.Empty;    // no discrepancy to show
                if (TimespanType.IsAligned())
                    return CurrentState == CurrencyState.OK ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyAlignedDueDate, AlignedDueDate.Value.ToShortDateString());
                else
                    return IsCurrent() ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, this.Discrepancy.ToString(EventType.IsIntegerOnly() ? "0" : "#,#.0#", CultureInfo.CurrentCulture), EventTypeLabel(this.Discrepancy, EventType));
            }
        }

        public CustomCurrencyTimespanType TimespanType { get; set; }

        /// <summary>
        /// If this is an aligned property (e.g., 6 months April aligned), this is the start date based on today's date
        /// </summary>
        public DateTime? AlignedStartDate { get; set; }

        public DateTime? AlignedDueDate
        {
            get
            {
                return (AlignedStartDate.HasValue) ? AlignedStartDate.Value.AddMonths(TimespanType.Duration()).AddDays(-1) : (DateTime?)null;
            }
        }

        /// <summary>
        /// Specify models that contribute to currency
        /// </summary>
        public IEnumerable<int> ModelsRestriction { get; set; }

        /// <summary>
        /// Specify aircraft (plural) that contribute to currency
        /// </summary>
        public IEnumerable<int> AircraftRestriction { get; set; }

        /// <summary>
        /// By default, we are looking for a MINIMUM number of qualifying events within a particular time frame
        /// If we set the Limit to Maximum, then we are looking for a MAXIMUM number of events.  E.g., no more than 10 flights in a 15 day period.
        /// This turns the currency on its head.  It basically inverts: you start off current and then lose currency, and you
        /// have a date when you've filled up and THEN you have an expiration date
        /// </summary>
        public LimitType CurrencyLimitType { get; set; }
        #endregion

        static public IEnumerable<CustomCurrency> CustomCurrenciesForUser(string szUser)
        {
            List<CustomCurrency> lst = new List<CustomCurrency>();
            DBHelper dbh = new DBHelper(ConfigurationManager.AppSettings["CustomCurrencyForUserQuery"].ToString());
            if (!dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("uname", szUser); },
                (dr) => { lst.Add(new CustomCurrency(dr)); }))
                throw new MyFlightbookException("Exception in CustomCurrenciesForUser - setup: " + dbh.LastError);

            return lst;
        }

        #region Overrides to support aligned currencies
        public override CurrencyState CurrentState
        {
            get
            {
                CurrencyState cs;

                if (TimespanType.IsAligned())
                {
                    if (NumEvents >= RequiredEvents)
                        cs = CurrencyState.OK;
                    else
                        cs = CurrencyState.NotCurrent;
                }
                else
                    cs = base.CurrentState;

                // If the limit is set as a maximum number of events, then invert it.
                if (CurrencyLimitType == LimitType.Maximum)
                {
                    switch (cs)
                    {
                        case CurrencyState.GettingClose:
                        case CurrencyState.OK:
                            cs = CurrencyState.NotCurrent;
                            break;
                        case CurrencyState.NotCurrent:
                            cs = (Discrepancy < 3) ? CurrencyState.GettingClose : CurrencyState.OK;
                            break;
                        default:
                            break;
                    }
                }
                return cs;
            }
        }

        public override string StatusDisplay
        {
            get
            {
                switch (CurrencyLimitType)
                {
                    case LimitType.Minimum:
                        if (TimespanType.IsAligned())
                            return CurrentState == CurrencyState.OK ?
                                String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyAlignedStatusCompleted, RequiredEvents, EventTypeLabel(RequiredEvents, EventType), AlignedDueDate.Value.ToShortDateString()) :
                                String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyAlignedStatusIncomplete, NumEvents, RequiredEvents, EventTypeLabel(RequiredEvents, EventType));
                        else
                            return base.StatusDisplay;
                    case LimitType.Maximum:
                        return CurrentState == CurrencyState.NotCurrent ?
                            String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyStatusLimitExceeded, (TimespanType.IsAligned() ? AlignedDueDate.Value : ExpirationDate).ToShortDateString()) :
                            String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyStatusLimitOK, Discrepancy, RequiredEvents, EventTypeLabel(RequiredEvents, EventType));
                    default:
                        return Resources.Currency.FormatNeverCurrent;
                }
            }
        }

        public override bool HasBeenCurrent
        {
            get
            {
                return TimespanType.IsAligned() || CurrencyLimitType == LimitType.Maximum || base.HasBeenCurrent;
            }
        }

        public bool IsNew { get { return ID == ID_UNKNOWN; } }
        #endregion

        public Boolean FIsValid()
        {
            ErrorString = string.Empty;
            if (String.IsNullOrEmpty(DisplayName))
                ErrorString = Resources.Currency.errCustomCurrencyNoName;
            if (ExpirationSpan <= 0 && !TimespanType.IsAligned())
                ErrorString = Resources.Currency.errCusotmCurrencyBadTimeSpan;
            if (RequiredEvents <= 0)
                ErrorString = Resources.Currency.errCustomCurrencyBadRequiredEvents;
            if (RequiredEvents > 2000)
                ErrorString = Resources.Currency.errCustomCurrencyInvalidEventCount;
            if (DisplayName.Length > 45)
                ErrorString = Resources.Currency.errCustomCurrencyNameTooLong;

            return String.IsNullOrEmpty(ErrorString);
        }

        public Boolean FCommit()
        {
            bool fIsNew = IsNew;

            if (!FIsValid())
                return false;

            string szSet = @"SET 
username=?uname, name=?name, minEvents=?minEvents, limitType=?limit, timespan=?timespan, timespantype=?timespanType, eventType=?eventType, 
categoryRestriction=?categoryRestriction, catClassRestriction=?catClassRestriction, airportRestriction=?airportRestriction, textRestriction=?textRestriction";

            string szQ = String.Format(CultureInfo.InvariantCulture, "REPLACE INTO customcurrency {0}{1}", szSet, fIsNew ? string.Empty : ", idCurrency=?id");

            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery(szQ,
                (comm) =>
                {
                    comm.Parameters.AddWithValue("id", ID);
                    comm.Parameters.AddWithValue("uname", UserName);
                    comm.Parameters.AddWithValue("name", DisplayName.LimitTo(44));
                    comm.Parameters.AddWithValue("minEvents", RequiredEvents);
                    comm.Parameters.AddWithValue("limit", (int)CurrencyLimitType);
                    comm.Parameters.AddWithValue("timespan", ExpirationSpan);
                    comm.Parameters.AddWithValue("timespantype", (int)TimespanType);
                    comm.Parameters.AddWithValue("eventType", (int)EventType);
                    comm.Parameters.AddWithValue("categoryRestriction", String.IsNullOrEmpty(CategoryRestriction) ? string.Empty : CategoryRestriction);
                    comm.Parameters.AddWithValue("catClassrestriction", (int)CatClassRestriction);
                    comm.Parameters.AddWithValue("airportRestriction", AirportRestriction.LimitTo(45));
                    comm.Parameters.AddWithValue("textRestriction", TextRestriction.LimitTo(254));
                });

            string szErr = (dbh.LastError.Length > 0) ? szErr = "Error saving customcurrency: " + szQ + "\r\n" + dbh.LastError : "";

            if (szErr.Length > 0)
                throw new MyFlightbookException(szErr);

            if (fIsNew)
                ID = dbh.LastInsertedRowId;

            // Always re-write the models/aircraft restrictions.
            string szDeleteExisting = String.Format(CultureInfo.InvariantCulture, "DELETE FROM custcurrencyref WHERE idCurrency={0}", ID);
            dbh.DoNonQuery(szDeleteExisting,
                (comm) => { });

            string szInsert = "INSERT INTO custcurrencyref SET idCurrency={0}, value={1}, type={2}";
            // Write out the models
            foreach (int idModel in ModelsRestriction)
            {
                dbh.CommandText = String.Format(CultureInfo.InvariantCulture, szInsert, ID, idModel, (int)CurrencyRefType.Models);
                dbh.DoNonQuery();
            }

            // Write out the aircraft
            foreach (int idAircraft in AircraftRestriction)
            {
                dbh.CommandText = String.Format(CultureInfo.InvariantCulture, szInsert, ID, idAircraft, (int)CurrencyRefType.Aircraft);
                dbh.DoNonQuery();
            }

            foreach (int idProp in PropertyRestriction)
            {
                dbh.CommandText = String.Format(CultureInfo.InvariantCulture, szInsert, ID, idProp, (int)CurrencyRefType.Properties);
                dbh.DoNonQuery();
            }

            return (szErr.Length == 0);
        }

        public void FDelete()
        {
            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery(String.Format(CultureInfo.InvariantCulture, "DELETE FROM customcurrency WHERE idCurrency={0}", ID), (comm) => { });
            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException("Error deleting customcurrency: " + dbh.LastError);
        }

        public static string EventTypeLabel(Decimal count, CustomCurrencyEventType ccet)
        {
            return (count == 1) ? ccet.SingularName() : ccet.PluralName();
        }

        override public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string szLimit = (CurrencyLimitType == LimitType.Minimum) ? Resources.Currency.CustomCurrencyMinEventsPrompt : Resources.Currency.CustomCurrencyMaxEventsPrompt;
            string szMinEvents = String.Format(CultureInfo.CurrentCulture, EventType.IsIntegerOnly() ? "{0:0}" : "{0:#,#.0#}", RequiredEvents);
            string szBase = TimespanType.IsAligned() ?
                String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyDescriptionAligned, szLimit, szMinEvents, EventTypeLabel(RequiredEvents, EventType), TimespanType.DisplayString()) :
                String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyDescription, szLimit, szMinEvents, EventTypeLabel(RequiredEvents, EventType), ExpirationSpan, TimespanType.DisplayString());

            if (ModelsRestriction.Count() > 0)
                sb.AppendFormat(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyModel, m_szModelNames);

            if (!String.IsNullOrEmpty(CategoryRestriction))
                sb.AppendFormat(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyCategory, CategoryRestriction);

            if (CatClassRestriction > 0)
            {
                if (String.IsNullOrEmpty(m_szCatClass))
                    m_szCatClass = CategoryClass.CategoryClassFromID(CatClassRestriction).CatClass;
                sb.AppendFormat(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyCatClass, m_szCatClass);
            }
            if (AircraftRestriction.Count() > 0)
                sb.AppendFormat(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyAircraft, m_szTailNumbers);

            if (!String.IsNullOrWhiteSpace(AirportRestriction))
                sb.AppendFormat(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyAirports, AirportRestriction);

            if (!String.IsNullOrWhiteSpace(TextRestriction))
                sb.AppendFormat(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyText, TextRestriction);

            if (PropertyRestriction != null && PropertyRestriction.Count() > 0)
                sb.AppendFormat(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyProperties, m_szPropertyNames);

            string szRestrict = sb.ToString();
            if (szRestrict.EndsWith(";", StringComparison.Ordinal))
                szRestrict = szRestrict.Substring(0, szRestrict.Length - 1);
            szRestrict = szRestrict.Replace(";", "\r\n");

            return String.Format(CultureInfo.InvariantCulture, "{0}\r\n{1}", szBase, szRestrict);
        }

        public string DisplayString
        {
            get { return ToString(); }
        }

        /// <summary>
        /// Generates a new flightquery object representing flights that match this custom currency
        /// </summary>
        public FlightQuery Query
        {
            get
            {
                FlightQuery fq = new FlightQuery(this.UserName)
                {
                    DateRange = FlightQuery.DateRanges.Custom,
                    DateMax = DateTime.Now.AddDays(1)
                };
                switch (TimespanType)
                {
                    case CustomCurrencyTimespanType.CalendarMonths:
                        fq.DateMin = DateTime.Now.AddCalendarMonths(-ExpirationSpan);
                        break;
                    case CustomCurrencyTimespanType.Days:
                        fq.DateMin = DateTime.Now.AddDays(-ExpirationSpan);
                        break;
                    default:
                        fq.DateMin = AlignedStartDate.Value;
                        break;
                }

                List<CategoryClass> lstCc = new List<CategoryClass>();
                if (CatClassRestriction > 0)
                    lstCc.Add(CategoryClass.CategoryClassFromID(CatClassRestriction));
                if (!String.IsNullOrEmpty(CategoryRestriction))
                {
                    foreach (CategoryClass cc in CategoryClass.CategoryClasses())
                        if (cc.Category.CompareOrdinalIgnoreCase(CategoryRestriction) == 0)
                            lstCc.Add(cc);
                }
                fq.CatClasses = lstCc.ToArray();

                List<MakeModel> lstModels = new List<MakeModel>();
                foreach (int idmodel in ModelsRestriction)
                    lstModels.Add(MakeModel.GetModel(idmodel));
                fq.MakeList = lstModels.ToArray();

                UserAircraft ua = new UserAircraft(UserName);
                List<Aircraft> lstAircraft = new List<Aircraft>();
                Array.ForEach<Aircraft>(ua.GetAircraftForUser(), (ac) => { if (AircraftRestriction.Contains(ac.AircraftID)) lstAircraft.Add(ac); });
                fq.AircraftList = lstAircraft.ToArray();

                if (!String.IsNullOrEmpty(AirportRestriction))
                    fq.AirportList = Airports.AirportList.NormalizeAirportList(AirportRestriction);

                if (!String.IsNullOrEmpty(TextRestriction))
                    fq.GeneralText = TextRestriction;

                CustomPropertyType.KnownProperties prop = CustomPropertyType.KnownProperties.IDPropInvalid;
                List<CustomPropertyType> lstprops = new List<CustomPropertyType>(CustomPropertyType.GetCustomPropertyTypes());
                fq.PropertiesConjunction = GroupConjunction.All;
                switch (EventType)
                {
                    case CustomCurrencyEventType.BackseatHours:
                        prop = CustomPropertyType.KnownProperties.IDPropBackSeatTime;
                        break;
                    case CustomCurrencyEventType.BaseCheck:
                        fq.PropertyTypes = lstprops.FindAll(cpt => cpt.IsBaseCheck).ToArray();
                        break;
                    case CustomCurrencyEventType.Flights:
                        break;
                    case CustomCurrencyEventType.FrontSeatHours:
                        prop = CustomPropertyType.KnownProperties.IDPropFrontSeatTime;
                        break;
                    case CustomCurrencyEventType.GroundSimHours:
                        fq.HasGroundSim = true;
                        break;
                    case CustomCurrencyEventType.HoistOperations:
                        prop = CustomPropertyType.KnownProperties.IDPropHoistOperations;
                        break;
                    case CustomCurrencyEventType.Hours:
                        fq.HasPIC = true;
                        break;
                    case CustomCurrencyEventType.IFRApproaches:
                        fq.HasApproaches = true;
                        break;
                    case CustomCurrencyEventType.IFRHours:
                        fq.HasAnyInstrument = true;
                        break;
                    case CustomCurrencyEventType.Landings:
                    case CustomCurrencyEventType.PICLandings:
                        fq.HasLandings = true;
                        if (EventType == CustomCurrencyEventType.PICLandings)
                            fq.HasPIC = true;
                        break;
                    case CustomCurrencyEventType.LandingsHighAltitude:
                        prop = CustomPropertyType.KnownProperties.IDPropHighAltitudeLandings;
                        break;
                    case CustomCurrencyEventType.NightFlight:
                        fq.HasNight = true;
                        break;
                    case CustomCurrencyEventType.NightLandings:
                        fq.HasNightLandings = true;
                        break;
                    case CustomCurrencyEventType.NightTouchAndGo:
                        prop = CustomPropertyType.KnownProperties.IDPropNightTouchAndGo;
                        break;
                    case CustomCurrencyEventType.NightTakeoffs:
                        fq.PropertyTypes = lstprops.FindAll(cpt => cpt.IsNightTakeOff).ToArray();
                        break;
                    case CustomCurrencyEventType.NVFLIR:
                        prop = CustomPropertyType.KnownProperties.IDPropNVFLIRTime;
                        break;
                    case CustomCurrencyEventType.NVGoggles:
                        prop = CustomPropertyType.KnownProperties.IDPropNVGoggleTime;
                        break;
                    case CustomCurrencyEventType.NVHours:
                        fq.PropertyTypes = lstprops.FindAll(cpt => cpt.IsNightVisionTime).ToArray();
                        break;
                    case CustomCurrencyEventType.PICNightLandings:
                        fq.HasNightLandings = true;
                        fq.HasPIC = true;
                        break;
                    case CustomCurrencyEventType.TotalHours:
                        fq.HasTotalTime = true;
                        break;
                    case CustomCurrencyEventType.UASLaunch:
                        fq.PropertyTypes = lstprops.FindAll(cpt => cpt.IsUASLaunch).ToArray();
                        break;
                    case CustomCurrencyEventType.UASRecovery:
                        fq.PropertyTypes = lstprops.FindAll(cpt => cpt.IsUASRecovery).ToArray();
                        break;
                    case CustomCurrencyEventType.CAP5Checkride:
                        prop = CustomPropertyType.KnownProperties.IDPropCAP5Checkride;
                        break;
                    case CustomCurrencyEventType.CAP91Checkride:
                        prop = CustomPropertyType.KnownProperties.IDPropCAP91Checkride;
                        break;
                    case CustomCurrencyEventType.FMSApproaches:
                        prop = CustomPropertyType.KnownProperties.IDPropFMSApproaches;
                        break;
                    case CustomCurrencyEventType.GliderTow:
                        prop = CustomPropertyType.KnownProperties.IDPropGliderTow;
                        break;
                    case CustomCurrencyEventType.HoursDual:
                        fq.HasDual = true;
                        break;
                    case CustomCurrencyEventType.FlightReview:
                        fq.PropertyTypes = lstprops.FindAll(cpt => cpt.IsBFR).ToArray();
                        break;
                    case CustomCurrencyEventType.IPC:
                        fq.PropertyTypes = lstprops.FindAll(cpt => cpt.IsIPC).ToArray();
                        break;
                    case CustomCurrencyEventType.NightLandingAny:
                        fq.HasNight = true;
                        fq.HasLandings = true;
                        break;
                    default:
                        throw new MyFlightbookException("Unknown event type: " + EventType.ToString() + " in ToQuery()");
                }
                if (prop != CustomPropertyType.KnownProperties.IDPropInvalid)
                    fq.PropertyTypes = lstprops.FindAll(cpt => cpt.PropTypeID == (int)prop).ToArray();

                if (PropertyRestriction != null && PropertyRestriction.Count() > 0)
                {
                    // Merge additional properties with any "intrinsic" properties from the type of custom currency above.
                    HashSet<CustomPropertyType> hs = new HashSet<CustomPropertyType>(fq.PropertyTypes ?? new CustomPropertyType[0]);

                    foreach (int i in PropertyRestriction)
                        hs.Add(CustomPropertyType.GetCustomPropertyType(i));

                    fq.PropertyTypes = hs.ToArray();
                }

                return fq;
            }
        }

        /// <summary>
        /// Returns the query representing flights that match this custom currency, compressed and URL encoded.
        /// </summary>
        /// <returns>The newly generated flightquery.  Don't forget to refresh it!</returns>
        public string FlightQueryJSON
        {
            get { return System.Web.HttpUtility.UrlEncode(Query.ToBase64CompressedJSONString()); }
        }

        private bool CheckMatchesCriteria(ExaminerFlightRow cfr)
        {
            // quickly see if we can ignore this.
            int idmodel = cfr.idModel;
            if (ModelsRestriction != null && ModelsRestriction.Count() > 0 && !ModelsRestriction.Any(model => model == idmodel))
                return false;
            if (!String.IsNullOrEmpty(CategoryRestriction) && cfr.szCategory.ToString().CompareTo(CategoryRestriction) != 0)
                return false;
            if (CatClassRestriction > 0 && cfr.idCatClassOverride != CatClassRestriction)
                return false;
            int idAircraft = Convert.ToInt32(cfr.idAircraft);
            if (AircraftRestriction != null && AircraftRestriction.Count() > 0 && !AircraftRestriction.Any(idac => idac == idAircraft))
                return false;
            if (AlignedStartDate.HasValue && AlignedStartDate.Value.CompareTo(cfr.dtFlight) > 0)
                return false;
            if (PropertyRestriction != null && PropertyRestriction.Count() > 0)
            {
                if (cfr.FlightProps.Count() == 0)  // short circuit, plus empty set can be superset of other sets.
                    return false;

                HashSet<int> hsProps = new HashSet<int>(PropertyRestriction);
                HashSet<int> hsFlight = new HashSet<int>();
                foreach (CustomFlightProperty cfp in cfr.FlightProps)
                    hsFlight.Add(cfp.PropTypeID);

                if (!hsFlight.IsSupersetOf(hsProps))
                    return false;
            }
            if (!String.IsNullOrWhiteSpace(TextRestriction))
            {
                string szUpper = TextRestriction.ToUpper();
                bool fFound = false;
                if (cfr.Comments.ToUpper().Contains(szUpper))
                    fFound = true;
                if (!fFound)
                {
                    foreach (CustomFlightProperty cfp in cfr.FlightProps)
                        if (cfp.PropertyType.Type == CFPPropertyType.cfpString && cfp.TextValue.ToUpper().Contains(szUpper))
                        {
                            fFound = true;
                            break;
                        }
                }
                if (!fFound)
                    return false;
            }
            if (!String.IsNullOrWhiteSpace(AirportRestriction))
            {
                string[] szAirports = Airports.AirportList.NormalizeAirportList(AirportRestriction.ToUpper());
                string szRouteUpper = cfr.Route.ToUpper();
                foreach (string szAirport in szAirports)
                    if (!szRouteUpper.Contains(szAirport))
                        return false;
            }
            return true;
        }

        /// <summary>
        /// Looks at the events in the flight for custom currency computations.
        /// </summary>
        /// <param name="cfr">The flight row to examine</param>
        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (!CheckMatchesCriteria(cfr))
                return;
            // don't look at anything older than necessary if we're a *maximum* currency
            // since we start out current and lose it over time, so "has been current" is always true.
            if (CurrencyLimitType == LimitType.Maximum && cfr.dtFlight.CompareTo(EarliestDate) < 0)
                return;

            CustomFlightProperty cfp = null;    //useful for checking properties

            // OK if we're here then the currency applies.
            switch (EventType)
            {
                case CustomCurrencyEventType.Flights:
                    AddRecentFlightEvents(cfr.dtFlight, 1);
                    break;
                case CustomCurrencyEventType.Hours:
                    AddRecentFlightEvents(cfr.dtFlight, Convert.ToDecimal(cfr.PIC));
                    break;
                case CustomCurrencyEventType.TotalHours:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.Total);
                    break;
                case CustomCurrencyEventType.GroundSimHours:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.GroundSim);
                    break;
                case CustomCurrencyEventType.FrontSeatHours:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropFrontSeatTime)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.DecValue);
                    break;
                case CustomCurrencyEventType.BackseatHours:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropBackSeatTime)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.DecValue);
                    break;
                case CustomCurrencyEventType.HoistOperations:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropHoistOperations)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.IntValue);
                    break;
                case CustomCurrencyEventType.Landings:
                    AddRecentFlightEvents(cfr.dtFlight, Convert.ToInt32(cfr.cLandingsThisFlight));
                    break;
                case CustomCurrencyEventType.PICLandings:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.PIC > 0 ? cfr.cLandingsThisFlight : 0);
                    break;
                case CustomCurrencyEventType.PICNightLandings:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.PIC > 0 ? cfr.cFullStopNightLandings : 0);
                    break;
                case CustomCurrencyEventType.IFRHours:
                    AddRecentFlightEvents(cfr.dtFlight, Convert.ToDecimal(cfr.IMCSim));
                    AddRecentFlightEvents(cfr.dtFlight, Convert.ToDecimal(cfr.IMC));
                    break;
                case CustomCurrencyEventType.IFRApproaches:
                    AddRecentFlightEvents(cfr.dtFlight, Convert.ToInt32(cfr.cApproaches));
                    break;
                case CustomCurrencyEventType.HoursDual:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.Dual);
                    break;
                case CustomCurrencyEventType.BaseCheck:
                    cfr.FlightProps.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsBaseCheck)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.BoolValue ? 1 : 0);
                    });
                    break;
                case CustomCurrencyEventType.UASLaunch:
                    cfr.FlightProps.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsUASLaunch)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.IntValue);
                    });
                    break;
                case CustomCurrencyEventType.UASRecovery:
                    cfr.FlightProps.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsUASRecovery)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.IntValue);
                    });
                    break;
                case CustomCurrencyEventType.NightLandings:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.cFullStopNightLandings);
                    break;
                case CustomCurrencyEventType.NightLandingAny:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.cFullStopNightLandings + cfr.FlightProps.TotalCountForPredicate(fp => fp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropNightTouchAndGo));
                    break;
                case CustomCurrencyEventType.NightTakeoffs:
                    cfr.FlightProps.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsNightTakeOff)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.IntValue);
                    });
                    break;
                case CustomCurrencyEventType.NightTouchAndGo:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropNightTouchAndGo)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.IntValue);
                    break;
                case CustomCurrencyEventType.NightFlight:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.Night);
                    break;
                case CustomCurrencyEventType.NVHours:
                    cfr.FlightProps.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsNightVisionTime)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.DecValue);
                    });
                    break;
                case CustomCurrencyEventType.NVGoggles:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropNVGoggleTime)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.DecValue);
                    break;
                case CustomCurrencyEventType.NVFLIR:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropNVFLIRTime)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.DecValue);
                    break;
                case CustomCurrencyEventType.LandingsHighAltitude:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropHighAltitudeLandings)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.IntValue);
                    break;
                case CustomCurrencyEventType.CAP5Checkride:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropCAP5Checkride)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.BoolValue ? 1 : 0);
                    break;
                case CustomCurrencyEventType.CAP91Checkride:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropCAP91Checkride)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.BoolValue ? 1 : 0);
                    break;
                case CustomCurrencyEventType.FMSApproaches:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropFMSApproaches)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.IntValue);
                    break;
                case CustomCurrencyEventType.GliderTow:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropGliderTow)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.IntValue);
                    break;
                case CustomCurrencyEventType.FlightReview:
                    if ((cfp = cfr.FlightProps.FindEvent(fp => fp.PropertyType.IsBFR)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, 1);
                    break;
                case CustomCurrencyEventType.IPC:
                    if ((cfp = cfr.FlightProps.FindEvent(fp => fp.PropertyType.IsIPC)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, 1);
                    break;
            }
        }
    }

    public class CustomCurrencyEventArgs : EventArgs
    {
        public CustomCurrency Currency { get; set; }

        public CustomCurrencyEventArgs() : base()
        {
            Currency = null;
        }

        public CustomCurrencyEventArgs(CustomCurrency cc) : base()
        {
            Currency = cc;
        }
    }
}