using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2007-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    /// <summary>
    /// Which duty time attributes were specified on a flight?
    /// </summary>
    public enum DutySpecification { None, Both, Start, End };

    /// <summary>
    /// Represents an effective duty period, which can be explicit (specified by duty periods) or inferred (just the 24-hour period)
    /// </summary>
    public class EffectiveDutyPeriod
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

        /// <summary>
        /// What is the effective start of non-rest for this duty period (earlier of flight duty start and duty start)
        /// </summary>
        public DateTime EffectiveDutyStart { get { return FlightDutyStart.HasValue() ? (AdditionalDutyStart.HasValue ? FlightDutyStart.EarlierDate(AdditionalDutyStart.Value) : FlightDutyStart) : (AdditionalDutyStart ?? DateTime.MinValue); } }

        /// <summary>
        /// What is the effective end of non-rest for this duty period (earlier of flight duty end and duty end)
        /// </summary>
        public DateTime EffectiveDutyEnd { get { return FlightDutyEnd.HasValue() ? (AdditionalDutyEnd.HasValue ? FlightDutyEnd.LaterDate(AdditionalDutyEnd.Value) : FlightDutyEnd) : (AdditionalDutyEnd ?? DateTime.MinValue); } }

        /// <summary>
        /// How many hours of non-rest time does this duty period represent (= EffectiveDutyStart to EffectiveDutyEnd).
        /// </summary>
        public double NonRestTime { get { return EffectiveDutyEnd.Subtract(EffectiveDutyStart).TotalHours; } }

        /// <summary>
        /// How many hours of flight duty time does this represent (= flightdutyend minus flightdutystart)
        /// </summary>
        public double ElapsedFlightDuty { get { return FlightDutyEnd.Subtract(FlightDutyStart).TotalHours; } }

        /// <summary>
        /// Field that can optionally hold reset time since an adjacent effective duty period
        /// </summary>
        public double RestSince { get; set; }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}-{1} ({2})", EffectiveDutyStart.UTCFormattedStringOrEmpty(false), EffectiveDutyEnd.UTCFormattedStringOrEmpty(false), Specification.ToString());
        }

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
                throw new ArgumentNullException(nameof(cfr));

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
            {
                FlightDutyEnd = FPDutyEnd.DateValue;
                Specification = DutySpecification.End;
            }
            else if (FPDutyStart == null && FPDutyEnd == null && AdditionalDutyEnd != null && AdditionalDutyStart != null)
            {
                // e.g., deadhead - duty time (so non-rest), but no flight-duty time. 
                Specification = DutySpecification.Both;
                return;
            }
            else if ((FPDutyEnd == null && FPDutyStart != null) || AdditionalDutyStart != null)
            {
                Specification = DutySpecification.Start;

                // Issue #406: if we have an open duty start on the user's most recent flight, then infer a duty end of right now.
                // Otherwise, we can fall through (below) and block off the whole day.
                if (fInferDutyEnd)
                {
                    if (AdditionalDutyEnd == null)
                        AdditionalDutyEnd = DateTime.UtcNow.AddSeconds(10);

                    if (FPDutyStart != null)
                    {
                        FlightDutyStart = FPDutyStart.DateValue;
                        FlightDutyEnd = DateTime.UtcNow.AddSeconds(10);  // Add a few seconds from now, so it doesn't look like we're in a rest period.
                    }
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
        /// b) The flight time range goes from EARLIEST(Flight start, Engine start, or Block Out) to LATEST(Flight End, Engine End, Block End).  Call this TR.start to TR.end
        /// c) If we can compute one of TR.start or TR.end, we can figure out the other end by adding (subtracting) net time.  i.e., if we know TR.start, then TR.end is TR.start + net time
        /// d) The contributed "time since" is TR.end minus LATEST(FlightDutyStart, TR.start).  i.e., if TR starts after FlightDutyStart, then all of the net time counts.  
        ///    Otherwise, only the amount that is after FlightDutyStart counts.
        /// </summary>
        /// <param name="dtStart">Start of the period</param>
        /// <param name="netTime">Max time that could be within the period</param>
        /// <param name="cfr">The flight row that specifies a potential duty period</param>
        /// <returns></returns>
        public TimeSpan TimeSince(DateTime dtStart, double netTime, ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            DateTime dtStartFlight = DateTime.MaxValue;
            DateTime dtEndFlight = DateTime.MinValue;

            DateTime blockOut = cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDBlockOut) ? cfr.FlightProps[CustomPropertyType.KnownProperties.IDBlockOut].DateValue : DateTime.MaxValue;
            DateTime blockIn = cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDBlockIn) ? cfr.FlightProps[CustomPropertyType.KnownProperties.IDBlockIn].DateValue : DateTime.MinValue;

            if (cfr.dtFlightStart.HasValue())
                dtStartFlight = dtStartFlight.EarlierDate(cfr.dtFlightStart);
            if (cfr.dtEngineStart.HasValue())
                dtStartFlight = dtStartFlight.EarlierDate(cfr.dtEngineStart);
            dtStartFlight = dtStartFlight.EarlierDate(blockOut);

            if (dtStartFlight.CompareTo(DateTime.MaxValue) == 0) // none were found
                dtStartFlight = DateTime.MinValue;

            if (cfr.dtFlightEnd.HasValue())
                dtEndFlight = dtEndFlight.LaterDate(cfr.dtFlightEnd);
            if (cfr.dtEngineEnd.HasValue())
                dtEndFlight = dtEndFlight.LaterDate(cfr.dtEngineEnd);
            dtEndFlight = dtEndFlight.LaterDate(blockIn);

            if (!dtEndFlight.HasValue() && !dtStartFlight.HasValue())
            {
                // Neither end found - conservatively set from (FlightDutyEnd - netTime) to FlightDutyEnd
                dtEndFlight = FlightDutyEnd;
                dtStartFlight = FlightDutyEnd.AddHours(-netTime);
            }
            if (dtEndFlight.HasValue())
                // found an ending time but not a starting time - compute starting based on end
                dtStartFlight = dtEndFlight.AddHours(-netTime);
            else if (dtStartFlight.HasValue())
                // found a start time, but no ending time - compute an end based on the start
                dtEndFlight = dtStartFlight.AddHours(netTime);

            return dtEndFlight.Subtract(dtStartFlight.LaterDate(dtStart));
        }

        /// <summary>
        /// Returns the net amount of time for this effective duty period that occurs since the specified UTC DateTime and UTCNow.
        /// </summary>
        /// <param name="dtStart">The specified earliest date</param>
        /// <returns></returns>
        public TimeSpan DutyTimeSince(DateTime dtStart)
        {
            return DateTime.UtcNow.EarlierDate(EffectiveDutyEnd).Subtract(EffectiveDutyStart.LaterDate(dtStart));
        }

        /// <summary>
        /// Returns the net amount of flight duty time for this effective duty period that occurs since the specified UTC DateTime and UTCNow.
        /// </summary>
        /// <param name="dtStart">The specified earliest date</param>
        /// <returns></returns>
        public TimeSpan FlightDutyTimeSince(DateTime dtStart)
        {
            return DateTime.UtcNow.EarlierDate(FlightDutyEnd).Subtract(FlightDutyStart.LaterDate(dtStart));
        }

        #region Day-oriented rest and duty time computations
        /// <summary>
        /// Computes the duty time for duty periods in the specified timespan looking back from UtcNow
        /// </summary>
        /// <param name="ts">Timespan to look back</param>
        /// <param name="lst">Set of effective duty periods to consider</param>
        /// <returns>The total duty time.</returns>
        public static decimal DutyTimeSince(TimeSpan ts, IEnumerable<EffectiveDutyPeriod> lst)
        {
            if (lst == null)
                throw new ArgumentNullException(nameof(lst));

            DateTime dtMin = DateTime.UtcNow.Subtract(ts);

            double totalDuty = 0;
            foreach (EffectiveDutyPeriod edp in lst)
                totalDuty += Math.Max(0, edp.DutyTimeSince(dtMin).TotalHours);

            return (decimal)totalDuty;
        }

        public static decimal FlightDutyTimeSince(TimeSpan ts, IEnumerable<EffectiveDutyPeriod> lst)
        {
            if (lst == null)
                throw new ArgumentNullException(nameof(lst));

            DateTime dtMin = DateTime.UtcNow.Subtract(ts);

            double totalDuty = 0;
            foreach (EffectiveDutyPeriod edp in lst)
                totalDuty += Math.Max(0, edp.FlightDutyTimeSince(dtMin).TotalHours);

            return (decimal)totalDuty;
        }

        public static double LongestRestSince(TimeSpan ts, IEnumerable<EffectiveDutyPeriod> lst)
        {
            if (lst == null)
                throw new ArgumentNullException(nameof(lst));

            DateTime dtMin = DateTime.UtcNow.Subtract(ts);

            if (!lst.Any()) // no duty periods found - entire period is rest
                return ts.TotalHours;

            double maxRest = 0.0;

            foreach (EffectiveDutyPeriod edp in lst)
            {
                if (edp.EffectiveDutyEnd.CompareTo(dtMin) >= 0)
                    maxRest = Math.Max(maxRest, edp.RestSince);
                else
                {
                    maxRest = Math.Max(maxRest, edp.RestSince - (dtMin.Subtract(edp.EffectiveDutyEnd)).TotalHours);
                    break;
                }
            }
            return maxRest;
        }
        #endregion
    }

    public class DutyPeriodExaminer : FlightCurrency
    {
        private readonly List<EffectiveDutyPeriod> m_effectiveDutyPeriods = new List<EffectiveDutyPeriod>();
        private EffectiveDutyPeriod m_edpCurrent;

        #region properties
        /// <summary>
        /// Are all flights included, or only those between valid duty start/end periods?
        /// </summary>
        protected bool IncludeAllFlights { get; }

        /// <summary>
        /// If we've never seen a duty period, don't report any 117 currencies.
        /// </summary>
        protected bool HasSeenProperDutyPeriod { get; set; }

        /// <summary>
        /// The EDP produced for the most recently examined flight.
        /// </summary>
        protected EffectiveDutyPeriod CurrentEDP { get; set; }

        /// <summary>
        /// True if there is an in-progress EDP (i.e., that hasn't had one end closed yet)
        /// </summary>
        protected bool HasIndeterminateEDP { get { return m_edpCurrent != null; } }

        public IEnumerable<EffectiveDutyPeriod> EffectiveDutyPeriods { get { return m_effectiveDutyPeriods; } }
        #endregion

        /// <summary>
        /// Adds a duty period to the list of dutyperiods, checking for duplicates (but not overlap!)  Handles multiple flights when IncludeAllFlights is checked.
        /// </summary>
        /// <param name="edp"></param>
        protected void AddDutyPeriodToList(EffectiveDutyPeriod edp)
        {
            if (edp == null)
                throw new ArgumentNullException(nameof(edp));

            if (m_effectiveDutyPeriods.Any())
            {
                EffectiveDutyPeriod edpLatest = m_effectiveDutyPeriods[m_effectiveDutyPeriods.Count - 1];
                if (edp.EffectiveDutyEnd.CompareTo(edpLatest.EffectiveDutyEnd) == 0 && edp.EffectiveDutyStart.CompareTo(edpLatest.EffectiveDutyStart) == 0)
                    return;
            }
            m_effectiveDutyPeriods.Add(edp);
        }

        public override void Finalize(decimal totalTime, decimal picTime)
        {
            // Fill in Rest Since
            DateTime dtLastStart = DateTime.UtcNow;
            foreach (EffectiveDutyPeriod edp in EffectiveDutyPeriods)
            {
                edp.RestSince = Math.Max(0, dtLastStart.Subtract(edp.EffectiveDutyEnd).TotalHours);
                dtLastStart = edp.EffectiveDutyStart;
            }
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            EffectiveDutyPeriod edp = CurrentEDP = new EffectiveDutyPeriod(cfr, !HasSeenProperDutyPeriod);

            // If we haven't yet seen the start of an open duty period, don't compute the rest yet (need to know the close off point), 
            // so store the end of the duty period in m_edpCurrent and leave the computation to a subsequent flight.
            // OR, if we have an open duty period (m_edpCurrent isn't null) and this one can close it off, then we can do so.
            // OR, if we have an open duty period and this one doesn't close it off, continue to a subsequent flight.
            // Of course, if we're including all flights, then we ALWAYS process this flight using effective (computed) times.
            if (!IncludeAllFlights)
            {
                // if neither start nor end duty times were specified, return; no more processing to do on this flight
                if (edp.Specification == DutySpecification.None)
                    return;
                // If it's fully specified, there's nothing to do but to record the duty period
                // OR if we haven't seen a proper duty period yet AND we don't have a current open edp (we shouldn't) AND this indicates a start duty/flightduty, add it to the duty period.
                else if (edp.Specification == DutySpecification.Both || (!HasSeenProperDutyPeriod && m_edpCurrent == null && (edp.FPDutyStart != null || edp.AdditionalDutyStart.HasValue)))
                    AddDutyPeriodToList(edp);
                // Otherwise, if we have just the end of a duty period, open up the active duty period.
                else if (m_edpCurrent == null && edp.Specification == DutySpecification.End) // we've found the end of a duty period - open up an active duty period
                    m_edpCurrent = edp;
                // Otherwise, we've found a start of a period - we can close it off.
                else if (m_edpCurrent != null && edp.FPDutyStart != null)
                {
                    edp.AdditionalDutyEnd = m_edpCurrent.AdditionalDutyEnd; // be sure to capture any additional duty time from the END of FDP, recorded in a flight we saw previously.
                    m_edpCurrent.FlightDutyEnd = m_edpCurrent.FPDutyEnd.DateValue;
                    m_edpCurrent.FlightDutyStart = edp.FPDutyStart.DateValue;
                    AddDutyPeriodToList(m_edpCurrent);
                    m_edpCurrent.Specification = DutySpecification.Both;
                    CurrentEDP = m_edpCurrent;  // we'll return this whole EDP.
                    m_edpCurrent = null;
                }
            }
            else
                AddDutyPeriodToList(edp);

            HasSeenProperDutyPeriod = HasSeenProperDutyPeriod || edp.Specification != DutySpecification.None;
        }

        public DutyPeriodExaminer(bool includeAllFlights) : base()
        {
            IncludeAllFlights = includeAllFlights;
            m_edpCurrent = null;
        }
    }

    /// <summary>
    /// Computes FAR 117 Currency (duty period/rest time for part 121 commercial operations)
    /// </summary>
    public class FAR117Currency : DutyPeriodExaminer
    {
        protected bool UseHHMM { get; set; }

        #region local variables
        private decimal hoursFlightTime11723b1 = 0.0M;
        private decimal hoursFlightTime11723b2 = 0.0M;

        // Useful dates we don't want to continuously recompute
        private readonly DateTime dt672HoursAgo = DateTime.UtcNow.AddHours(-672);
        private readonly DateTime dt365DaysAgo = DateTime.UtcNow.AddDays(-365).Date;
        #endregion

        public FAR117Currency(bool includeAllFlights = true, bool fUseHHMM = false) : base(includeAllFlights)
        {
            UseHHMM = fUseHHMM;
        }

        private void Add11723BTime(ExaminerFlightRow cfr, DateTime dtDutyEnd)
        {
            DateTime flightStart, flightEnd;

            decimal totalHoursTime = Math.Round(cfr.Total * 60.0M) / 60.0M;

            // Issue #503 - favor block time (which matches the regulation's "flight time" definition), using engine as a secondary proxy
            CustomFlightProperty cfpBlockOut = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockOut);
            CustomFlightProperty cfpBlockIn = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockIn);
            if (cfpBlockOut != null && cfpBlockIn != null && cfpBlockIn.DateValue.CompareTo(cfpBlockOut.DateValue) > 0)
            {
                flightStart = cfpBlockOut.DateValue;
                flightEnd = cfpBlockIn.DateValue;
                totalHoursTime = Math.Round((decimal) flightEnd.Subtract(flightStart).TotalHours * 60.0M) / 60.0M;
            } else if (cfr.dtEngineStart.HasValue() && cfr.dtEngineEnd.HasValue())
            {
                flightStart = cfr.dtEngineStart;
                flightEnd = cfr.dtEngineEnd;
                totalHoursTime = Math.Round((decimal)flightEnd.Subtract(flightStart).TotalHours * 60.0M) / 60.0M;
            }
            else if (cfr.dtFlightStart.HasValue() && cfr.dtFlightEnd.HasValue())
            {
                flightStart = cfr.dtFlightStart;
                flightEnd = cfr.dtFlightEnd;
                // Don't adjust total minnutes here because flight time is likely and undercount.
            }
            else
            {
                // use 11:59pm as the flight end time and compute flight start based off of that to pull as much of it as possible 
                // into the 672 hour or 365 day window.  (I.e., most conservative)
                flightEnd = new DateTime(cfr.dtFlight.Year, cfr.dtFlight.Month, cfr.dtFlight.Day, 23, 59, 0, DateTimeKind.Utc);
                if (dtDutyEnd.HasValue())
                    flightEnd = flightEnd.EarlierDate(dtDutyEnd);
                flightStart = flightEnd.AddHours((double) -totalHoursTime);
            }

            // 117.23(b)(1) - 100 hours of flight time in 672 consecutive
            if (flightEnd.CompareTo(dt672HoursAgo) > 0)
                hoursFlightTime11723b1 += Math.Max((totalHoursTime - Math.Max((decimal) dt672HoursAgo.Subtract(flightStart).TotalHours, 0.0M)), 0.0M);
            // 117.23(b)(2) - 1000 hours in 365 consecutive days.  This is NOT hour-for-hour, so can simply compare dates.
            if (flightEnd.Date.CompareTo(dt365DaysAgo) > 0)
                hoursFlightTime11723b2 += totalHoursTime;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            base.ExamineFlight(cfr);    // will set CurrentEDP

            EffectiveDutyPeriod edp = CurrentEDP;

            // Add in flight times if 
            // (a) we include all flights OR
            // (b) we don't include all flights but we have a currently active duty period, OR
            // (c) this flight has some duty period specified
            if (IncludeAllFlights || HasIndeterminateEDP || edp.Specification != DutySpecification.None)
                Add11723BTime(cfr, edp.FlightDutyEnd);
        }

        public IEnumerable<CurrencyStatusItem> Status
        {
            get
            {
                List<CurrencyStatusItem> lst = new List<CurrencyStatusItem>();

                if (HasSeenProperDutyPeriod)
                {
                    // merge up all of the time spans

                    lst.Add(new CurrencyStatusItem(Resources.Currency.FAR11723b1,
                        String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursTemplate, hoursFlightTime11723b1.FormatDecimal(UseHHMM, true)),
                        (hoursFlightTime11723b1 > 100) ? CurrencyState.NotCurrent : ((hoursFlightTime11723b1 > 80) ? CurrencyState.GettingClose : CurrencyState.OK),
                        (hoursFlightTime11723b1 > 100) ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursOver, (hoursFlightTime11723b1 - 100).FormatDecimal(UseHHMM, true)) : String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursAvailable, (100 - hoursFlightTime11723b1).FormatDecimal(UseHHMM, true))));

                    lst.Add(new CurrencyStatusItem(Resources.Currency.FAR11723b2,
                        String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursTemplate, hoursFlightTime11723b2.FormatDecimal(UseHHMM, true)),
                        (hoursFlightTime11723b2 > 1000) ? CurrencyState.NotCurrent : ((hoursFlightTime11723b2 > 900) ? CurrencyState.GettingClose : CurrencyState.OK),
                        (hoursFlightTime11723b2 > 1000) ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursOver, (hoursFlightTime11723b2 - 1000).FormatDecimal(UseHHMM, true)) : String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursAvailable, (1000 - hoursFlightTime11723b2).FormatDecimal(UseHHMM, true))));

                    decimal dutyTime11723c1 = EffectiveDutyPeriod.FlightDutyTimeSince(new TimeSpan(168, 0, 0), EffectiveDutyPeriods);
                    lst.Add(new CurrencyStatusItem(Resources.Currency.FAR11723c1,
                        String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursTemplate, dutyTime11723c1.FormatDecimal(UseHHMM, true)),
                        (dutyTime11723c1 > 60) ? CurrencyState.NotCurrent : ((dutyTime11723c1 > 50) ? CurrencyState.GettingClose : CurrencyState.OK),
                        (dutyTime11723c1 > 60) ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursOver, (dutyTime11723c1 - 60).FormatDecimal(UseHHMM)) : String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursAvailable, (60 - dutyTime11723c1).FormatDecimal(UseHHMM))));

                    decimal dutyTime11723c2 = EffectiveDutyPeriod.FlightDutyTimeSince(new TimeSpan(672, 0, 0), EffectiveDutyPeriods);
                    lst.Add(new CurrencyStatusItem(Resources.Currency.FAR11723c2,
                        String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursTemplate, dutyTime11723c2.FormatDecimal(UseHHMM, true)),
                        (dutyTime11723c2 > 190) ? CurrencyState.NotCurrent : ((dutyTime11723c2 > 150) ? CurrencyState.GettingClose : CurrencyState.OK),
                        (dutyTime11723c2 > 190) ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursOver, (dutyTime11723c2 - 190).FormatDecimal(UseHHMM, true)) : String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursAvailable, (190 - dutyTime11723c2).FormatDecimal(UseHHMM, true))));

                    // 25b - need a 30-hour rest period within the prior 168 hours
                    double hoursLongestRest = Math.Min(EffectiveDutyPeriod.LongestRestSince(new TimeSpan(168, 0, 0), EffectiveDutyPeriods), 168.0);
                    lst.Add(new CurrencyStatusItem(Resources.Currency.FAR11725b,
                        String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursTemplate, hoursLongestRest.FormatDecimal(UseHHMM, true)),
                        (hoursLongestRest > 56) ? CurrencyState.OK : ((hoursLongestRest > 30) ? CurrencyState.GettingClose : CurrencyState.NotCurrent),
                        string.Empty));

                    /*
                     * Issue #577: If not currently in a rest period, show how long you've been in your duty period
                     * You are in a duty period if:
                     * a) The most recent duty start or flight duty start is greater than the most recent duty end/flight duty end OR
                     * b) The most recent duty end/flight duty end is later than UtcNow
                    */
                    EffectiveDutyPeriod edpMostRecent = EffectiveDutyPeriods.ElementAt(0);
                    bool fInDuty = edpMostRecent.EffectiveDutyEnd.CompareTo(DateTime.UtcNow.AddSeconds(-20)) > 0;

                    if (fInDuty)
                        lst.Add(new CurrencyStatusItem(Resources.Currency.FAR117CurrentDutyPeriod,
                            String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursTemplate, DateTime.UtcNow.Subtract(edpMostRecent.EffectiveDutyStart).TotalHours.FormatDecimal(UseHHMM, true)),
                            CurrencyState.OK, string.Empty));

                    // Finally, report on current rest period
                    lst.Add(new CurrencyStatusItem(Resources.Currency.FAR117CurrentRest,
                        String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR117HoursTemplate, Math.Max(0, DateTime.UtcNow.Subtract(edpMostRecent.EffectiveDutyEnd).TotalHours).FormatDecimal(UseHHMM, true)),
                        CurrencyState.OK, string.Empty));
                }

                foreach (CurrencyStatusItem csi in lst)
                    csi.CurrencyGroup = CurrencyStatusItem.CurrencyGroups.FlightExperience;

                return lst;
            }
        }
    }
}
