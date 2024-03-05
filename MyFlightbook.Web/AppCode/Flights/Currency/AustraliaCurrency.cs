using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2021-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    /// <summary>
    /// Australian currency: see http://www5.austlii.edu.au/au/legis/cth/consol_reg/casr1998333/s61.395.html.  Striped by category, not catclass
    /// </summary>
    public class AustraliaPassengerCurrency : PassengerCurrency
    {
        public AustraliaPassengerCurrency(string szName, bool fRequireDayLandings) : base(szName, fRequireDayLandings) { }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            if (cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropPilotMonitoring) || !cfr.fIsCertifiedLanding)
                return;
            base.ExamineFlight(cfr);
            bool fMeetsFlightReview = false;
            cfr.FlightProps.ForEachEvent((cfp) =>
            {
                if ((cfp.PropertyType.IsIPC || cfp.PropertyType.IsBFR) && cfr.cLandingsThisFlight > 0) 
                    fMeetsFlightReview = true;
            });
            if (fMeetsFlightReview)
                AddRecentFlightEvents(cfr.dtFlight, 3);
        }
    }

    /// <summary>
    /// Australian currency: see http://www5.austlii.edu.au/au/legis/cth/consol_reg/casr1998333/s61.395.html; striped by category, not catclass
    /// </summary>
    public class AustraliaNightPassengerCurrency : NightCurrency
    {
        private List<int> relevantFlights { get; set; } = new List<int>();

        public AustraliaNightPassengerCurrency(string szName) : base(szName, true)
        {
            Query.HasNightLandings = false; // we support touch-and-to too so look for explicit flights
        }
        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            if (cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropPilotMonitoring) || !cfr.fIsCertifiedLanding)
                return;

            int cNightTouchAndGo = cfr.FlightProps.TotalCountForPredicate(fp => fp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropNightTouchAndGo);
            int cNightTakeoffs = cfr.FlightProps.TotalCountForPredicate(fp => fp.PropTypeID == (int) CustomPropertyType.KnownProperties.IDPropNightTakeoff) + cNightTouchAndGo;
            int cNightLandings = cfr.cFullStopNightLandings + cNightTouchAndGo;
            bool fMeetsFlightReview = false;
            cfr.FlightProps.ForEachEvent((cfp) =>
            {
                if ((cfp.PropertyType.IsIPC || cfp.PropertyType.IsBFR) && cNightLandings > 0 && cNightTakeoffs > 0)
                    fMeetsFlightReview = true;
            });

            if (fMeetsFlightReview)
            {
                AddRecentFlightEvents(cfr.dtFlight, 3);
                NightTakeoffCurrency.AddRecentFlightEvents(cfr.dtFlight, 3);
            } 
            else
            {
                AddRecentFlightEvents(cfr.dtFlight, cNightLandings);
                NightTakeoffCurrency.AddRecentFlightEvents(cfr.dtFlight, cNightTakeoffs);
            }

            if (fMeetsFlightReview || cNightLandings + cNightTakeoffs + cNightTouchAndGo > 0)
                relevantFlights.Add(cfr.flightID);
        }

        protected override void ComputeComposite()
        {
            FlightCurrency fcComposite = this.AND(NightTakeoffCurrency);
            CompositeCurrencyState = fcComposite.CurrentState;
            CompositeDiscrepancy = fcComposite.DiscrepancyString;
            CompositeExpiration = fcComposite.ExpirationDate;
            Query.EnumeratedFlights = new HashSet<int>(relevantFlights);
        }
    }

    /// <summary>
    /// Australian Currency: see http://classic.austlii.edu.au/au/legis/cth/consol_reg/casr1998333/s61.870.html
    /// </summary>
    public class AustraliaInstrumentIFRGeneral : FlightCurrency
    {
        public AustraliaInstrumentIFRGeneral(string szUser) : base(3, 90, false, Resources.Currency.CurrencyAustraliaIFRGeneral)
        {
            Query = new FlightQuery(szUser) { DateRange = FlightQuery.DateRanges.Trailing90, HasApproaches = true };
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            if (!cfr.fIsCertifiedIFR)
                return;

            AddRecentFlightEvents(cfr.dtFlight, cfr.cApproaches);
        }
    }

    public class AustraliaInstrumentIFRCategory : FlightCurrency
    {
        public AustraliaInstrumentIFRCategory(string szUser) : base(1, 90, false, Resources.Currency.CurrencyAustraliaIFRCategory)
        {
            Query = new FlightQuery(szUser) { DateRange = FlightQuery.DateRanges.Trailing90, HasApproaches = true };
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            if (!cfr.fIsCertifiedIFR)
                return;

            AddRecentFlightEvents(cfr.dtFlight, cfr.cApproaches);
        }
    }

    public class AustraliaInstrumentIFR3D : FlightCurrency
    {
        public AustraliaInstrumentIFR3D(string szUser) : base(1, 90, false, Resources.Currency.CurrencyAustraliaIFR3D) 
        {
            Query = new FlightQuery(szUser) { DateRange = FlightQuery.DateRanges.Trailing90, HasApproaches = true, PropertiesConjunction = GroupConjunction.Any };
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            if (!cfr.fIsCertifiedIFR)
                return;

            cfr.FlightProps.ForEachEvent((cfp) =>
            {
                if (cfp.PropertyType.IsPrecisionApproach)
                {
                    AddRecentFlightEvents(cfr.dtFlight, cfp.IntValue);
                    Query.PropertyTypes.Add(cfp.PropertyType);
                }
            });
        }

        public override string DiscrepancyString
        {
            get
            {
                return this.IsCurrent() ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, this.Discrepancy, (this.Discrepancy > 1) ? Resources.Totals.Approaches : Resources.Totals.Approach);
            }
        }
    }

    public class AustraliaInstrumentIFR2D : FlightCurrency
    {
        public AustraliaInstrumentIFR2D(string szUser) : base(1, 90, false, Resources.Currency.CurrencyAustraliaIFR2D)
        {
            Query = new FlightQuery(szUser) { DateRange = FlightQuery.DateRanges.Trailing90, HasApproaches = true, PropertiesConjunction = GroupConjunction.Any };
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            if (!cfr.fIsCertifiedIFR)
                return;

            cfr.FlightProps.ForEachEvent((cfp) =>
            {
                if (cfp.PropertyType.IsApproach && !cfp.PropertyType.IsPrecisionApproach)
                    Query.PropertyTypes.Add(cfp.PropertyType);
            });

            AddRecentFlightEvents(cfr.dtFlight, Math.Max(0, cfr.cApproaches - cfr.FlightProps.TotalCountForPredicate(fp => fp.PropertyType.IsPrecisionApproach)));
        }

        public override string DiscrepancyString
        {
            get
            {
                return this.IsCurrent() ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, this.Discrepancy, (this.Discrepancy > 1) ? Resources.Totals.Approaches : Resources.Totals.Approach);
            }
        }
    }
}