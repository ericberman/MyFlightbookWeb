﻿using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    public class GliderIFRCurrency : CompositeFlightCurrency
    {
        readonly FlightCurrency fcGliderIFRTime = new FlightCurrency(1.0M, 6, true, "Instrument time in Glider or single-engine airplane with view limiting device");
        readonly FlightCurrency fcGliderIFRTimePassengers = new FlightCurrency(2.0M, 6, true, "Instrument time in a glider.");
        readonly FlightCurrency fcGliderInstrumentManeuvers = new FlightCurrency(2.0M, 6, true, "Instrument Maneuvers per 61.57(c)(6)(i) => (c)(3)(i)");
        readonly FlightCurrency fcGliderInstrumentPassengers = new FlightCurrency(1, 6, true, "Instrument Maneuvers required for passengers, per 61.57(c)(6)(ii) => (c)(3)(ii)");
        readonly FlightCurrency fcGliderIPC = new FlightCurrency(1, 6, true, "Glider IPC");

        Boolean m_fCurrentSolo;
        Boolean m_fCurrentPassengers;

        #region Adding Events
        /// <summary>
        /// Add an IPC or equivalent (e.g., instrument check ride)
        /// </summary>
        /// <param name="dt">Date of the IPC</param>
        public void AddIPC(DateTime dt)
        {
            fcGliderIPC.AddRecentFlightEvents(dt, 1);
            Invalidate();
        }

        /// <summary>
        /// Add IFR time for part 61.57(c)(6)(i)(A) => (c)(3)(i)(A).  Can be in a glider OR ASEL under the hood
        /// </summary>
        /// <param name="dt">Date of the time</param>
        /// <param name="time">Amount of time to add</param>
        public void AddIFRTime(DateTime dt, Decimal time)
        {
            fcGliderIFRTime.AddRecentFlightEvents(dt, time);
            Invalidate();
        }

        /// <summary>
        /// Add IFR time for part 61.57(c)(6)(ii)(A) => (c)(3)(ii)(A).  MUST BE IN A GLIDER
        /// </summary>
        /// <param name="dt">Date of the time</param>
        /// <param name="time">Amount of time to add</param>
        public void AddIFRTimePassengers(DateTime dt, Decimal time)
        {
            fcGliderIFRTimePassengers.AddRecentFlightEvents(dt, time);
            Invalidate();
        }

        /// <summary>
        /// Add maneuvering time for part 61.57(c)(6)(i)(B) => (c)(3)(i)(B).  Can be in a glider or single-engine airplane simulated IMC.
        /// </summary>
        /// <param name="dt">Date of the time</param>
        /// <param name="time">Amount of time to add</param>
        public void AddManeuverTime(DateTime dt, Decimal time)
        {
            fcGliderInstrumentManeuvers.AddRecentFlightEvents(dt, time);
            Invalidate();
        }

        /// <summary>
        /// Add performance maneuvers for part 61.57(c)(6)(ii)(B) => (c)(3)(ii)(B).  Does NOT appear to need to have been in a glider.
        /// </summary>
        /// <param name="dt"></param>
        public void AddPerformanceManeuvers(DateTime dt)
        {
            fcGliderInstrumentPassengers.AddRecentFlightEvents(dt, 1);
            Invalidate();
        }
        #endregion

        public GliderIFRCurrency() : base()
        {
        }

        /// <summary>
        /// Computes the overall currency
        /// </summary>
        protected override void ComputeComposite()
        {
            // Compute currency according to 61.57(c)(3) (i) and (ii) including each one's expiration.  

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
                    CompositeCurrencyState = fc6157c6ii.CurrentState;
                    CompositeExpiration = fc6157c6ii.ExpirationDate;
                    CompositeDiscrepancy = fc6157c6ii.DiscrepancyString;
                }
                else // must be current solo)
                {
                    CompositeCurrencyState = fc6157c6i.CurrentState;
                    CompositeExpiration = fc6157c6i.ExpirationDate;
                    CompositeDiscrepancy = fc6157c6i.DiscrepancyString;
                }

                // check to see if there is an embedded gap that needs an IPC
                CurrencyPeriod[] rgcpMissingIPC = fc6157c6i.FindCurrencyGap(fcGliderIPC.MostRecentEventDate, 6);
                if (rgcpMissingIPC != null && CompositeDiscrepancy.Length == 0)
                    CompositeDiscrepancy = String.Format(CultureInfo.CurrentCulture, Resources.Currency.IPCMayBeRequired, rgcpMissingIPC[0].EndDate.ToShortDateString(), rgcpMissingIPC[1].StartDate.ToShortDateString());
            }
            else
            {
                // And expiration date is the later of the passenger/no-passenger date
                CompositeExpiration = fc6157c6i.ExpirationDate.LaterDate(fc6157c6ii.ExpirationDate);

                // see if we need an IPC
                // if more than 6 calendar months have passed since expiration, an IPC is required.
                // otherwise, just name the one that is short
                CompositeDiscrepancy = DateTime.Compare(CompositeExpiration.AddCalendarMonths(6).Date, DateTime.Now.Date) > 0
                    ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplateGliderIFRPassengers, fcGliderIFRTime.Discrepancy, fcGliderInstrumentManeuvers.Discrepancy)
                    : Resources.Currency.IPCRequired;
            }
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

        #region FlightExaminer Interface
        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
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
}
