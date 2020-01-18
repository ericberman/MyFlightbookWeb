using System;

/******************************************************
 * 
 * Copyright (c) 2007-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightCurrency
{
    public class FlightExperienceCanada : IFlightExaminer
    {
        public bool HasExperience
        {
            get { return fHasPICExperience || fHasFlightReview; }
        }

        private bool fHasPICExperience = false;
        private bool fHasFlightReview = false;

        public FlightExperienceCanada() { }

        public void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr.dtFlight.CompareTo(DateTime.Now.AddYears(-5)) < 0)
                return;

            fHasPICExperience = fHasPICExperience || (cfr.PIC + cfr.SIC > 0);

            // Alternative to having PIC experience is having a flight review in the prior year
            if (cfr.dtFlight.CompareTo(DateTime.Now.AddYears(-1)) > 0)
            {
                cfr.FlightProps.ForEachEvent((pe) =>
                {
                    if (pe.PropertyType.IsBFR)
                        fHasFlightReview = true;
                });
            }
        }
    }
    /// <summary>
    /// Rules for Canada are a bit different.  See https://www.tc.gc.ca/eng/civilaviation/publications/tp185-1-10-takefive-559.htm and http://laws-lois.justice.gc.ca/eng/regulations/SOR-96-433/FullText.html#s-401.05
    /// </summary>
    public class PassengerCurrencyCanada : PassengerCurrency
    {
        private readonly FlightExperienceCanada fcCanada;

        public PassengerCurrencyCanada(string szName)
            : base(szName)
        {
            IsCalendarMonth = true;
            ExpirationSpan = 6;
            RequiredEvents = Discrepancy = 5;
            fcCanada = new FlightExperienceCanada();
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            base.ExamineFlight(cfr);
            fcCanada.ExamineFlight(cfr);
        }

        public override CurrencyState CurrentState
        {
            get { return fcCanada.HasExperience ? base.CurrentState : CurrencyState.NotCurrent; }
        }

        public override string DiscrepancyString
        {
            get { return (fcCanada.HasExperience) ? base.DiscrepancyString : Resources.Currency.CanadaNoPICTime; }
        }
    }

    /// <summary>
    /// Canadian rules are slightly different.  See https://www.tc.gc.ca/eng/civilaviation/publications/tp185-1-10-takefive-559.htm
    /// </summary>
    public class NightCurrencyCanada : NightCurrency
    {
        const int RequiredLandingsCanada = 5;
        const int RequiredTakeoffsCanada = 5;
        const int TimeSpanCanada = 6;
        private readonly FlightExperienceCanada fcCanada;

        public NightCurrencyCanada(string szName)
            : base(szName)
        {
            NightTakeoffCurrency.IsCalendarMonth = this.IsCalendarMonth = true;
            NightTakeoffCurrency.ExpirationSpan = this.ExpirationSpan = TimeSpanCanada;
            this.RequiredEvents = this.Discrepancy = RequiredLandingsCanada;
            NightTakeoffCurrency.RequiredEvents = NightTakeoffCurrency.Discrepancy = RequiredTakeoffsCanada;
            fcCanada = new FlightExperienceCanada();
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            base.ExamineFlight(cfr);
            // Add in night touch-and-go landings too, since they also count.
            AddRecentFlightEvents(cfr.dtFlight, cfr.FlightProps.TotalCountForPredicate(cfp => cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropNightTouchAndGo));
            fcCanada.ExamineFlight(cfr);
        }

        public override CurrencyState CurrentState
        {
            get { return fcCanada.HasExperience ? base.CurrentState : CurrencyState.NotCurrent; }
        }

        public override string DiscrepancyString
        {
            get { return (fcCanada.HasExperience) ? base.DiscrepancyString : Resources.Currency.CanadaNoPICTime; }
        }
    }

    /// <summary>
    /// Instrument Currency rules for Canada - see http://laws-lois.justice.gc.ca/eng/regulations/SOR-96-433/FullText.html#s-401.05
    /// </summary>
    public class InstrumentCurrencyCanada : CurrencyExaminer
    {
        readonly FlightCurrency fc401_05_3_a = new FlightCurrency(1.0M, 12, true, "IPC or new rating");
        readonly FlightCurrency fc401_05_3_bTime = new FlightCurrency(6.0M, 6, true, "6 Hours of IFR time");
        readonly FlightCurrency fc401_05_3_bApproaches = new FlightCurrency(6.0M, 6, true, "6 approaches");
        readonly FlightCurrency fc401_05_3_cTime = new FlightCurrency(6.0M, 6, true, "6 Hours of IFR time in a real aircraft");
        readonly FlightCurrency fc401_05_3_cApproaches = new FlightCurrency(6.0M, 6, true, "6 approaches in a real aircraft");

        private Boolean m_fCacheValid = false;
        private CurrencyState m_csCurrent = CurrencyState.NotCurrent;
        private DateTime m_dtExpiration = DateTime.MinValue;
        private string m_szDiscrepancy = string.Empty;

        public InstrumentCurrencyCanada() { }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            m_fCacheValid = false;

            // 401.05(3)(a) - IPC or equivalent
            cfr.FlightProps.ForEachEvent((pfe) =>
            {
                // add any IPC or IPC equivalents
                if (pfe.PropertyType.IsIPC)
                    fc401_05_3_a.AddRecentFlightEvents(cfr.dtFlight, 1.0M);
            });

            decimal IFRTime = cfr.IMC + cfr.IMCSim;
            decimal IFRCFITime = Math.Min(IFRTime, cfr.CFI);

            // 401.05(3)(b) - flight in a real aircraft or sim
            fc401_05_3_bTime.AddRecentFlightEvents(cfr.dtFlight, Math.Max(0, IFRTime - IFRCFITime));
            fc401_05_3_bApproaches.AddRecentFlightEvents(cfr.dtFlight, cfr.cApproaches);

            // 401.05(3)(c) - IFR instruction in a real aircraft
            if (cfr.fIsRealAircraft)
            {
                fc401_05_3_cTime.AddRecentFlightEvents(cfr.dtFlight, IFRCFITime);
                fc401_05_3_cApproaches.AddRecentFlightEvents(cfr.dtFlight, cfr.CFI > 0 ? cfr.cApproaches : 0);
            }
        }

        private void RefreshCurrency()
        {
            if (m_fCacheValid)
                return;

            // 401.05(3)(a)-(c) say you have an IPC OR 6approaches+6hours OR 6approaches+6hours instructing.
            FlightCurrency fcIFRCanada = fc401_05_3_a.OR(fc401_05_3_bTime.AND(fc401_05_3_bApproaches)).OR(fc401_05_3_cTime.AND(fc401_05_3_cApproaches));
            m_csCurrent = fcIFRCanada.CurrentState;
            m_dtExpiration = fcIFRCanada.ExpirationDate;
            if (m_csCurrent == CurrencyState.NotCurrent)
                m_szDiscrepancy = Resources.Currency.IPCRequired;

            m_fCacheValid = true;
        }

        public override bool HasBeenCurrent
        {
            get
            {
                RefreshCurrency();
                return m_dtExpiration.HasValue();
            }
        }

        public override DateTime ExpirationDate
        {
            get { return m_dtExpiration; }
        }

        public override string StatusDisplay
        {
            get
            {
                RefreshCurrency();
                return CurrencyExaminer.StatusDisplayForDate(m_dtExpiration);
            }
        }

        public override CurrencyState CurrentState
        {
            get { return m_csCurrent; }
        }

        public override string DiscrepancyString
        {
            get
            {
                RefreshCurrency();
                return m_szDiscrepancy;
            }
        }
    }
}