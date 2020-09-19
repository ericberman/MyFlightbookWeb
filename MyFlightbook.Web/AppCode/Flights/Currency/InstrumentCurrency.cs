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

        readonly private Boolean m_fUseLoose6157c4;
        private Boolean m_fCacheValid;
        private CurrencyState m_csCurrent = CurrencyState.NotCurrent;
        private DateTime m_dtExpiration = DateTime.MinValue;
        private string m_szDiscrepancy = string.Empty;
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

        public InstrumentCurrency()
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

            if (fcIPCOrCheckride.IsCurrent() && fcIPCOrCheckride.ExpirationDate.CompareTo(m_dtExpiration) >= 0)
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
                throw new ArgumentNullException(nameof(cfr));

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
                {
                    AddIPC(cfr.dtFlight);
                    fSeenCheckride = true;
                }
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
            /*
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
            */
        }
        #endregion
    }
}
