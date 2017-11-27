using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

/******************************************************
 * 
 * Copyright (c) 2007-2017 MyFlightbook LLC
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
            FMSApproaches = 24
        };

        public enum CurrencyRefType { Aircraft = 0, Models = 1 };

        /// <summary>
        /// Is this a required minimum number of events, or a "do not exceed?"
        /// </summary>
        public enum LimitType { Minimum, Maximum };

        private const int ID_UNKNOWN = -1;

        string m_szTailNumbers, m_szModelNames, m_szCatClass;

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
            Discrepancy = RequiredEvents = Convert.ToInt32(dr["minEvents"], CultureInfo.InvariantCulture);
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

            ModelsRestriction = new List<int>(dr["ModelsRestriction"].ToString().ToInts());
            AircraftRestriction = new List<int>(dr["AircraftRestriction"].ToString().ToInts());

            // convenience properties - not persisted
            m_szTailNumbers = dr["AircraftDisplay"].ToString();
            m_szModelNames = dr["ModelsDisplay"].ToString();
            m_szCatClass = dr["CatClassDisplay"].ToString();
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

        override public string DiscrepancyString
        {
            get
            {
                if (CurrencyLimitType == LimitType.Maximum)
                    return string.Empty;    // no discrepancy to show
                if (TimespanType.IsAligned())
                    return CurrentState == CurrencyState.OK ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyAlignedDueDate, AlignedDueDate.Value.ToShortDateString());
                else
                    return IsCurrent() ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, this.Discrepancy, EventTypeLabel(this.Discrepancy, EventType));
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
                CurrencyState cs = CurrencyState.OK;

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

            return String.IsNullOrEmpty(ErrorString);
        }

        public Boolean FCommit()
        {
            bool fIsNew = IsNew;

            if (!FIsValid())
                return false;

            string szSet = @"SET 
username=?uname, name=?name, minEvents=?minEvents, limitType=?limit, timespan=?timespan, timespantype=?timespanType, eventType=?eventType, 
categoryRestriction=?categoryRestriction, catClassRestriction=?catClassRestriction";

            string szQ = String.Format(CultureInfo.InvariantCulture, "REPLACE INTO customcurrency {0}{1}", szSet, fIsNew ? string.Empty : ", idCurrency=?id");

            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery(szQ,
                (comm) =>
                {
                    comm.Parameters.AddWithValue("id", ID);
                    comm.Parameters.AddWithValue("uname", UserName);
                    comm.Parameters.AddWithValue("name", DisplayName);
                    comm.Parameters.AddWithValue("minEvents", RequiredEvents);
                    comm.Parameters.AddWithValue("limit", (int)CurrencyLimitType);
                    comm.Parameters.AddWithValue("timespan", ExpirationSpan);
                    comm.Parameters.AddWithValue("timespantype", (int)TimespanType);
                    comm.Parameters.AddWithValue("eventType", (int)EventType);
                    comm.Parameters.AddWithValue("categoryRestriction", String.IsNullOrEmpty(CategoryRestriction) ? "" : CategoryRestriction);
                    comm.Parameters.AddWithValue("catClassrestriction", (int)CatClassRestriction);
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

            return (szErr.Length == 0);
        }

        public void FDelete()
        {
            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery(String.Format(CultureInfo.InvariantCulture, "DELETE FROM customcurrency WHERE idCurrency={0}", ID), (comm) => { });
            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException("Error deleting customcurrency: " + dbh.LastError);
        }

        private static string EventTypeLabelSingle(CustomCurrencyEventType ccet)
        {
            switch (ccet)
            {
                case CustomCurrencyEventType.Flights:
                    return Resources.Currency.CustomCurrencyEventFlight;
                case CustomCurrencyEventType.Hours:
                    return Resources.Currency.CustomCurrencyEventHour;
                case CustomCurrencyEventType.Landings:
                    return Resources.Currency.CustomCurrencyEventLanding;
                case CustomCurrencyEventType.IFRApproaches:
                    return Resources.Currency.CustomCurrencyEventApproach;
                case CustomCurrencyEventType.IFRHours:
                    return Resources.Currency.CustomCurrencyEventInstrumentHour;
                case CustomCurrencyEventType.BaseCheck:
                    return Resources.Currency.CustomCurrencyEventBaseCheck;
                case CustomCurrencyEventType.UASLaunch:
                    return Resources.Currency.CustomCurrencyEventLaunch;
                case CustomCurrencyEventType.UASRecovery:
                    return Resources.Currency.CustomCurrencyEventRecovery;
                case CustomCurrencyEventType.NightLandings:
                    return Resources.Currency.CustomCurrencyEventNightLanding;
                case CustomCurrencyEventType.NightTakeoffs:
                    return Resources.Currency.CustomCurrencyEventNightTakeoff;
                case CustomCurrencyEventType.PICLandings:
                    return Resources.Currency.CustomCurrencyEventLandingPIC;
                case CustomCurrencyEventType.PICNightLandings:
                    return Resources.Currency.CustomCurrencyEventNightLandingPIC;
                case CustomCurrencyEventType.BackseatHours:
                    return Resources.Currency.CustomCurrencyEventBackSeatHour;
                case CustomCurrencyEventType.FrontSeatHours:
                    return Resources.Currency.CustomCurrencyEventFrontSeatHour;
                case CustomCurrencyEventType.TotalHours:
                    return Resources.Currency.CustomCurrencyEventTotalHour;
                case CustomCurrencyEventType.GroundSimHours:
                    return Resources.Currency.CustomCurrencyEventSimulatorHour;
                case CustomCurrencyEventType.HoistOperations:
                    return Resources.Currency.CustomCurrencyHoistOperation;
                case CustomCurrencyEventType.NVHours:
                    return Resources.Currency.CustomCurrencyEventNVHour;
                case CustomCurrencyEventType.NVGoggles:
                    return Resources.Currency.CustomCurrencyEventNVGHour;
                case CustomCurrencyEventType.NVFLIR:
                    return Resources.Currency.CustomCurrencyEventNVSHour;
                case CustomCurrencyEventType.LandingsHighAltitude:
                    return Resources.Currency.CustomCurrencyEventHighAltitudeLanding;
                case CustomCurrencyEventType.NightFlight:
                    return Resources.Currency.CustomCurrencyEventNightHour;
                case CustomCurrencyEventType.CAP5Checkride:
                    return Resources.Currency.CustomCurrencyEventCap5Checkride;
                case CustomCurrencyEventType.CAP91Checkride:
                    return Resources.Currency.CustomCurrencyEventCap91Checkride;
                case CustomCurrencyEventType.FMSApproaches:
                    return Resources.Currency.CustomCurrencyEventFMSApproach;
                default:
                    return ccet.ToString();
            }
        }

        private static string EventTypeLabelPlural(CustomCurrencyEventType ccet)
        {
            switch (ccet)
            {
                case CustomCurrencyEventType.Flights:
                    return Resources.Currency.CustomCurrencyEventFlights;
                case CustomCurrencyEventType.Hours:
                    return Resources.Currency.CustomCurrencyEventHours;
                case CustomCurrencyEventType.Landings:
                    return Resources.Currency.CustomCurrencyEventLandings;
                case CustomCurrencyEventType.IFRApproaches:
                    return Resources.Currency.CustomCurrencyEventApproaches;
                case CustomCurrencyEventType.IFRHours:
                    return Resources.Currency.CustomCurrencyEventInstrumentHours;
                case CustomCurrencyEventType.BaseCheck:
                    return Resources.Currency.CustomCurrencyEventBaseChecks;
                case CustomCurrencyEventType.UASLaunch:
                    return Resources.Currency.CustomCurrencyEventLaunches;
                case CustomCurrencyEventType.UASRecovery:
                    return Resources.Currency.CustomCurrencyEventRecoveries;
                case CustomCurrencyEventType.NightLandings:
                    return Resources.Currency.CustomCurrencyEventNightLandings;
                case CustomCurrencyEventType.NightTakeoffs:
                    return Resources.Currency.CustomCurrencyEventNightTakeoffs;
                case CustomCurrencyEventType.PICLandings:
                    return Resources.Currency.CustomCurrencyEventLandingsPIC;
                case CustomCurrencyEventType.PICNightLandings:
                    return Resources.Currency.CustomCurrencyEventNightLandingsPIC;
                case CustomCurrencyEventType.BackseatHours:
                    return Resources.Currency.CustomCurrencyEventBackSeatHours;
                case CustomCurrencyEventType.FrontSeatHours:
                    return Resources.Currency.CustomCurrencyEventFrontSeatHours;
                case CustomCurrencyEventType.TotalHours:
                    return Resources.Currency.CustomCurrencyEventTotalHours;
                case CustomCurrencyEventType.GroundSimHours:
                    return Resources.Currency.CustomCurrencyEventSimulatorHours;
                case CustomCurrencyEventType.HoistOperations:
                    return Resources.Currency.CustomCurrencyHoistOperations;
                case CustomCurrencyEventType.NVHours:
                    return Resources.Currency.CustomCurrencyEventNVHours;
                case CustomCurrencyEventType.NVGoggles:
                    return Resources.Currency.CustomCurrencyEventNVGHours;
                case CustomCurrencyEventType.NVFLIR:
                    return Resources.Currency.CustomCurrencyEventNVSHours;
                case CustomCurrencyEventType.LandingsHighAltitude:
                    return Resources.Currency.CustomCurrencyEventHighAltitudeLandings;
                case CustomCurrencyEventType.NightFlight:
                    return Resources.Currency.CustomCurrencyEventNightHours;
                case CustomCurrencyEventType.CAP5Checkride:
                    return Resources.Currency.CustomCurrencyEventCap5Checkrides;
                case CustomCurrencyEventType.CAP91Checkride:
                    return Resources.Currency.CustomCurrencyEventCap91Checkrides;
                case CustomCurrencyEventType.FMSApproaches:
                    return Resources.Currency.CustomCurrencyEventFMSApproaches;
                default:
                    return ccet.ToString();
            }
        }

        public static string EventTypeLabel(Decimal count, CustomCurrencyEventType ccet)
        {
            return (count == 1) ? EventTypeLabelSingle(ccet) : EventTypeLabelPlural(ccet);
        }

        override public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string szLimit = (CurrencyLimitType == LimitType.Minimum) ? Resources.Currency.CustomCurrencyMinEventsPrompt : Resources.Currency.CustomCurrencyMaxEventsPrompt;
            string szBase = TimespanType.IsAligned() ?
                String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyDescriptionAligned, szLimit, RequiredEvents, EventTypeLabel(RequiredEvents, EventType), TimespanType.DisplayString()) :
                String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyDescription, szLimit, RequiredEvents, EventTypeLabel(RequiredEvents, EventType), ExpirationSpan, TimespanType.DisplayString());

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
        /// <returns>The newly generated flightquery.  Don't forget to refresh it!</returns>
        public string FlightQueryJSON
        {
            get
            {
                FlightQuery fq = new FlightQuery(this.UserName);

                fq.DateRange = FlightQuery.DateRanges.Custom;
                fq.DateMax = DateTime.Now.AddDays(1);
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

                CustomPropertyType.KnownProperties prop = CustomPropertyType.KnownProperties.IDPropInvalid;
                List<CustomPropertyType> lstprops = new List<CustomPropertyType>(CustomPropertyType.GetCustomPropertyTypes());
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
                    default:
                        throw new MyFlightbookException("Unknown event type: " + EventType.ToString() + " in ToQuery()");
                }
                if (prop != CustomPropertyType.KnownProperties.IDPropInvalid)
                    fq.PropertyTypes = lstprops.FindAll(cpt => cpt.PropTypeID == (int)prop).ToArray();

                return System.Web.HttpUtility.UrlEncode(fq.ToBase64CompressedJSONString());
            }
        }

        /// <summary>
        /// Looks at the events in the flight for custom currency computations.
        /// </summary>
        /// <param name="cfr">The flight row to examine</param>
        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            // quickly see if we can ignore this.
            int idmodel = cfr.idModel;
            if (ModelsRestriction != null && ModelsRestriction.Count() > 0 && !ModelsRestriction.Any(model => model == idmodel))
                return;
            if (!String.IsNullOrEmpty(CategoryRestriction) && cfr.szCategory.ToString().CompareTo(CategoryRestriction) != 0)
                return;
            if (CatClassRestriction > 0 && cfr.idCatClassOverride != CatClassRestriction)
                return;
            int idAircraft = Convert.ToInt32(cfr.idAircraft);
            if (AircraftRestriction != null && AircraftRestriction.Count() > 0 && !AircraftRestriction.Any(idac => idac == idAircraft))
                return;
            if (AlignedStartDate.HasValue && AlignedStartDate.Value.CompareTo(cfr.dtFlight) > 0)
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
                    if ((cfp = cfr.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropFrontSeatTime)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.DecValue);
                    break;
                case CustomCurrencyEventType.BackseatHours:
                    if ((cfp = cfr.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropBackSeatTime)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.DecValue);
                    break;
                case CustomCurrencyEventType.HoistOperations:
                    if ((cfp = cfr.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropHoistOperations)) != null)
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
                case CustomCurrencyEventType.BaseCheck:
                    cfr.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsBaseCheck)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.BoolValue ? 1 : 0);
                    });
                    break;
                case CustomCurrencyEventType.UASLaunch:
                    cfr.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsUASLaunch)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.IntValue);
                    });
                    break;
                case CustomCurrencyEventType.UASRecovery:
                    cfr.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsUASRecovery)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.IntValue);
                    });
                    break;
                case CustomCurrencyEventType.NightLandings:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.cFullStopNightLandings);
                    break;
                case CustomCurrencyEventType.NightTakeoffs:
                    cfr.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsNightTakeOff)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.IntValue);
                    });
                    break;
                case CustomCurrencyEventType.NightFlight:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.Night);
                    break;
                case CustomCurrencyEventType.NVHours:
                    cfr.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsNightVisionTime)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.DecValue);
                    });
                    break;
                case CustomCurrencyEventType.NVGoggles:
                    if ((cfp = cfr.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropNVGoggleTime)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.DecValue);
                    break;
                case CustomCurrencyEventType.NVFLIR:
                    if ((cfp = cfr.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropNVFLIRTime)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.DecValue);
                    break;
                case CustomCurrencyEventType.LandingsHighAltitude:
                    if ((cfp = cfr.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropHighAltitudeLandings)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.IntValue);
                    break;
                case CustomCurrencyEventType.CAP5Checkride:
                    if ((cfp = cfr.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropCAP5Checkride)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.BoolValue ? 1 : 0);
                    break;
                case CustomCurrencyEventType.CAP91Checkride:
                    if ((cfp = cfr.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropCAP91Checkride)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.BoolValue ? 1 : 0);
                    break;
                case CustomCurrencyEventType.FMSApproaches:
                    if ((cfp = cfr.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropFMSApproaches)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.IntValue);
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