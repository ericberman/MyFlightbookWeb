using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
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
                    throw new ArgumentNullException(nameof(cfr));

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
                throw new ArgumentNullException(nameof(pfe));

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
            dt365DaysAgo = DateTime.UtcNow.AddDays(-365).Date;
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
}
