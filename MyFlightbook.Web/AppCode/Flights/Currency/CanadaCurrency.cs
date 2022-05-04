using System;

/******************************************************
 * 
 * Copyright (c) 2007-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    public class FlightExperienceCanada : IFlightExaminer
    {
        public bool HasExperience
        {
            get { return fHasPICExperience || fHasFlightReview; }
        }

        private bool fHasPICExperience;
        private bool fHasFlightReview;

        public FlightExperienceCanada() { }

        public void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

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

        public PassengerCurrencyCanada(string szName, bool fRequireDayLandings)
            : base(szName, fRequireDayLandings)
        {
            CurrencyTimespanType = TimespanType.CalendarMonths;
            ExpirationSpan = 6;
            RequiredEvents = Discrepancy = 5;
            fcCanada = new FlightExperienceCanada();

            Query = null;
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

        public NightCurrencyCanada(string szName) : base(szName, true)  // night touch-and-go landings count.
        {
            NightTakeoffCurrency.CurrencyTimespanType = this.CurrencyTimespanType = TimespanType.CalendarMonths;
            NightTakeoffCurrency.ExpirationSpan = this.ExpirationSpan = TimeSpanCanada;
            this.RequiredEvents = this.Discrepancy = RequiredLandingsCanada;
            NightTakeoffCurrency.RequiredEvents = NightTakeoffCurrency.Discrepancy = RequiredTakeoffsCanada;
            fcCanada = new FlightExperienceCanada();

            Query = null;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            base.ExamineFlight(cfr);    // Should handle touch-and-go landings regardless.
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
    /// Instrument Currency rules for Canada - see https://laws-lois.justice.gc.ca/eng/regulations/SOR-96-433/FullText.html#s-401.05
    /// </summary>
    public class InstrumentCurrencyCanada : CompositeFlightCurrency
    {
        readonly FlightCurrency fc401_05_1a = new FlightCurrency(1.0M, 60, true, "PIC or Co-pilot in 5 years");
        readonly FlightCurrency fc401_05_1b = new FlightCurrency(1.0M, 12, true, "Flight Review within 12 months");
        readonly FlightCurrency fc401_05_3_a_c_24month = new FlightCurrency(1.0M, 24, true, "IPC or new rating within 24 months");
        readonly FlightCurrency fc401_05_3_a_c_12month = new FlightCurrency(1.0M, 12, true, "IPC or new rating within 12 months");
        readonly FlightCurrency fc401_05_3_1_aTime = new FlightCurrency(6.0M, 6, true, "6 Hours of IFR time");
        readonly FlightCurrency fc401_05_3_1_bApproaches = new FlightCurrency(6.0M, 6, true, "6 approaches");

        public InstrumentCurrencyCanada() { }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            Invalidate();

            // must be a real aircraft or certified IFR
            if (!cfr.fIsCertifiedIFR)
                return;

            // 401.05(3)(a) - IPC or equivalent
            cfr.FlightProps.ForEachEvent((pfe) =>
            {
                // add any IPC or IPC equivalents
                if (pfe.PropertyType.IsIPC)
                {
                    fc401_05_3_a_c_24month.AddRecentFlightEvents(cfr.dtFlight, 1.0M);
                    fc401_05_3_a_c_12month.AddRecentFlightEvents(cfr.dtFlight, 1.0M);
                }
                if (pfe.PropertyType.IsBFR)
                    fc401_05_1b.AddRecentFlightEvents(cfr.dtFlight, 1.0M);
            });

            if (cfr.fIsRealAircraft)
            {
                // 401.05(1) - Acted as PIC or SIC within the previous 5 years
                fc401_05_1a.AddRecentFlightEvents(cfr.dtFlight, cfr.PIC + cfr.SIC);
            }

            // 401.05(3.1)(a) Instrument time
            fc401_05_3_1_aTime.AddRecentFlightEvents(cfr.dtFlight, cfr.IMC + cfr.IMCSim);

            // 401.05(3.1)(b) - 6 approaches (can be in a sim)
            fc401_05_3_1_bApproaches.AddRecentFlightEvents(cfr.dtFlight, cfr.cApproaches);
        }

        protected override void ComputeComposite()
        {
            /*
             * MUST meet:
             *  - 401.05(1)(a) OR 401.05(1)(b) AND
             *  - 401.05(3) (24 month IPC) AND
             *  - 6 hours and 6 approaches IF the review is more than 12 months old
             *  
             *  To do the latter two, we do this as (12 month IPC) OR (24 month IPC AND 6+6)
             */
            FlightCurrency fcIFRCanada = (fc401_05_3_a_c_12month.OR(fc401_05_3_a_c_24month.AND(fc401_05_3_1_aTime).AND(fc401_05_3_1_bApproaches))).AND(fc401_05_1a.OR(fc401_05_1b));
            CompositeCurrencyState = fcIFRCanada.CurrentState;
            CompositeExpiration = fcIFRCanada.ExpirationDate;

            // If the IPC is controlling, then display that an IPC is required.
            CompositeDiscrepancy = fc401_05_3_a_c_24month.IsCurrent() ? string.Empty : Resources.Currency.IPCRequired;

        }
    }
}