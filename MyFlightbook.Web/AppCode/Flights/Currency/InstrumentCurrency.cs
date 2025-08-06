using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    public class InstrumentCurrency : CompositeFlightCurrency
    {
        // 61.57(c)(1)
        readonly FlightCurrency fcIFRHold = new FlightCurrency(1, 6, true, "IFR - Holds");
        readonly FlightCurrency fcIFRApproach = new FlightCurrency(6, 6, true, "IFR - Approaches");

        // 61.57(d) - IPC (Instrument checkride counts here too)
        readonly FlightCurrency fcIPCOrCheckride = new FlightCurrency(1, 6, true, "IPC or Instrument Checkride");

        private Boolean fSeenCheckride;

        #region Adding Events
        /// <summary>
        /// Add approaches in a real plane
        /// </summary>
        /// <param name="dt">Date of the approaches</param>
        /// <param name="cApproaches"># of approaches</param>
        public void AddApproaches(DateTime dt, int cApproaches)
        {
            fcIFRApproach.AddRecentFlightEvents(dt, cApproaches);
            Invalidate();
        }

        /// <summary>
        /// Add holds in a real plane or certified flight simulator
        /// </summary>
        /// <param name="dt">Date of the hold</param>
        /// <param name="cHolds"># of holds</param>
        public void AddHolds(DateTime dt, int cHolds)
        {
            fcIFRHold.AddRecentFlightEvents(dt, cHolds);
            Invalidate();
        }

        /// <summary>
        /// Add an IPC or equivalent (e.g., instrument check ride)
        /// </summary>
        /// <param name="dt">Date of the IPC</param>
        public void AddIPC(DateTime dt)
        {
            fcIPCOrCheckride.AddRecentFlightEvents(dt, 1);
            Invalidate();
        }
        #endregion

        public InstrumentCurrency() : base()
        {
            Query = new FlightQuery()
            {
                FlightCharacteristicsConjunction = GroupConjunction.Any,
                HasApproaches = true,
                HasHolds = true,
                DateRange = FlightQuery.DateRanges.Custom,
                DateMin = DateTime.Now.Date.AddCalendarMonths(-6),
                DateMax = DateTime.Now.Date.AddCalendarMonths(0)
            };
        }

        /// <summary>
        /// Computes the overall currency
        /// </summary>
        protected override void ComputeComposite()
        {
            // Compute currency according to 61.57(c)(1)-(5) and 61.57(e)
            // including each one's expiration.  The one that expires latest is the expiration date if you are current, the one that
            // expired most recently is the expiration date since when you were not current.

            // 61.57(c)(1) (66-HIT in airplane, flight simulator, ATD, or FTD) OR an IPC.
            FlightCurrency fcIFR = fcIPCOrCheckride.OR(fcIFRApproach.AND(fcIFRHold));

            CompositeCurrencyState = fcIFR.CurrentState;
            CompositeExpiration = fcIFR.ExpirationDate;

            if (fcIPCOrCheckride.IsCurrent() && fcIPCOrCheckride.ExpirationDate.CompareTo(CompositeExpiration) >= 0)
            {
                Query.HasHolds = Query.HasApproaches = false;
                Query.PropertiesConjunction = GroupConjunction.Any;
                Query.PropertyTypes.Clear();
                foreach (CustomPropertyType cpt in CustomPropertyType.GetCustomPropertyTypes())
                {
                    if (cpt.IsIPC)
                        Query.PropertyTypes.Add(cpt);
                }
            }

            if (CompositeCurrencyState == CurrencyState.NotCurrent)
            {
                // if more than 6 calendar months have passed since expiration, an IPC is required.
                // otherwise, just assume 66-HIT.
                if (DateTime.Compare(CompositeExpiration.AddCalendarMonths(6).Date, DateTime.Now.Date) > 0)
                    CompositeDiscrepancy = String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplateIFR,
                                fcIFRHold.Discrepancy,
                                (fcIFRHold.Discrepancy == 1) ? Resources.Currency.Hold : Resources.Currency.Holds,
                                fcIFRApproach.Discrepancy,
                                (fcIFRApproach.Discrepancy == 1) ? Resources.Totals.Approach : Resources.Totals.Approaches);
                else
                    CompositeDiscrepancy = Resources.Currency.IPCRequired;
            }
            else
            {
                // Check to see if IPC is required by looking for > 6 month gap in IFR currency.
                // For now, we won't make you un-current, but we'll warn.
                // (IPC above, though, is un-current).  
                CurrencyPeriod[] rgcpMissingIPC = fcIFR.FindCurrencyGap(fcIPCOrCheckride.MostRecentEventDate, 6);

                CompositeDiscrepancy = rgcpMissingIPC != null
                    ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.IPCMayBeRequired, rgcpMissingIPC[0].EndDate.ToShortDateString(), rgcpMissingIPC[1].StartDate.ToShortDateString())
                    : string.Empty;
            }
        }

        #region FlightExaminer Implementation
        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            // Issue #1456 - Ignore any flight where you were monitoring the whole flight or in something that is not IFR certified.
            if (!cfr.fIsCertifiedIFR || cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropPilotMonitoring))
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
                {
                    AddIPC(cfr.dtFlight);
                    fSeenCheckride = true;
                }
            });

            // add any potentially relevant IFR currency events.
            // After Nov 26, 2018, the rules changed: any combination of real aircraft, FTD, FFS, or ATD that adds up to 6 approaches + hold works.
            if (cfr.cApproaches > 0)
                AddApproaches(cfr.dtFlight, cfr.cApproaches);
            if (cfr.cHolds > 0)
                AddHolds(cfr.dtFlight, cfr.cHolds);
        }
        #endregion
    }
}
