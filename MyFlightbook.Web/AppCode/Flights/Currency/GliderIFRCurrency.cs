using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
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
