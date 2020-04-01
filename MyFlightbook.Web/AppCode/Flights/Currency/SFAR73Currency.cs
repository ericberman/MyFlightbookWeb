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
    /// Compute Flight review requirements for SFAR 73 (regular R22/R44 currency is computed by piggybacking on type-rated currency)
    /// </summary>
    public class SFAR73Currency : FlightCurrency
    {
        protected const string R22ICAO = "R22";
        protected const string R44ICAO = "R44";
        protected const decimal MinRxxHours = 50;
        protected const decimal MinHelicopterHours = 200;
        protected const decimal MaxR22HoursTowardsR44 = 25;

        #region properties
        protected string Model { get; set; }

        protected bool IsR44 { get; set; }

        protected decimal HelicopterHours { get; set; }

        protected decimal HoursInType { get; set; }

        protected decimal R22SubstituteHours { get; set; }

        protected DateTime? LastFlightReviewInType { get; set; }
        #endregion

        public static IEnumerable<SFAR73Currency> SFAR73Currencies
        {
            get { return new SFAR73Currency[] { new SFAR73Currency(R22ICAO, Resources.Currency.NextFlightReviewR22), new SFAR73Currency(R44ICAO, Resources.Currency.NextFlightReviewR44) }; }
        }

        public SFAR73Currency(string icao, string szName) : base(1, 24, true, szName)
        {
            Model = icao ?? throw new ArgumentNullException(nameof(icao));
            if (icao.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(icao), "Must specify an ICAO value");

            IsR44 = icao.CompareCurrentCultureIgnoreCase(R44ICAO) == 0;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            if (!cfr.fIsRealAircraft)
                return;

            if (cfr.idCatClassOverride != CategoryClass.CatClassID.Helicopter)
                return;

            HelicopterHours += cfr.Total;

            // SFAR 73 (b)(2)(i) Can count up to 25 hours of R22 time towards R44 hours
            if (IsR44 && R22SubstituteHours < MaxR22HoursTowardsR44 && cfr.szFamily.CompareCurrentCultureIgnoreCase(R22ICAO) == 0)
                R22SubstituteHours = Math.Min(MaxR22HoursTowardsR44, R22SubstituteHours + cfr.Total);

            if (cfr.szFamily.CompareCurrentCultureIgnoreCase(Model) == 0)
            {
                HoursInType += cfr.Total;
                // Look for a BFR in this model of Robinson
                if (!LastFlightReviewInType.HasValue)
                {
                    cfr.FlightProps.ForEachEvent(cfp =>
                    {
                        if (cfp.PropertyType.IsBFR)
                            LastFlightReviewInType = cfr.dtFlight;
                    });
                }
            }
        }

        // Per SFAR73(b)(1/2)(i), if you HAVE 200 hrs in helicopters + 50 hrs in R22 ((b)(1)(i)) or R44 ((b)(2)(i), allowing up to 25 hrs of R22 time) , you're good to go 
        // but then SFAR73(c)(1/2) kicks in, so you need your most recent flight review (per 61.56) to have been in an R22/R44.  Since that's a regular flight review, it's
        // good for 2 years.
        //
        // If you *DON'T* meet SFAR73(b)(1/2)(i), then you instead must meet SFAR73(b)(1/2)(ii), which requires 10 hours of dual (not counted here since that's not really a currency thing)
        // and have a flight review in an R22/R44 within the preceding *12* calendar months.
        //
        // I.e., if you have the 200+50 time, you're on a 24 month flight review, otherwise you're on a 12 month.
        protected int FlightReviewDuration
        {
            get { return (HelicopterHours >= MinHelicopterHours && HoursInType + R22SubstituteHours >= MinRxxHours) ? 24 : 12; }
        }

        public override DateTime ExpirationDate
        {
            get { return LastFlightReviewInType.HasValue ? LastFlightReviewInType.Value.AddCalendarMonths(FlightReviewDuration) : DateTime.MinValue; }
        }

        public override CurrencyState CurrentState
        {
            get
            {
                if (!LastFlightReviewInType.HasValue)
                    return CurrencyState.NotCurrent;

                DateTime dtExpiration = ExpirationDate;

                return dtExpiration.CompareTo(DateTime.Now) < 0 ? CurrencyState.NotCurrent : (dtExpiration.CompareTo(DateTime.Now.AddDays(30)) < 0 ? CurrencyState.GettingClose : CurrencyState.OK);
            }
        }

        public override bool HasBeenCurrent
        {
            get { return LastFlightReviewInType.HasValue; }
        }

        public override string DiscrepancyString {
            get
            {
                TimeSpan ts = ExpirationDate.Subtract(DateTime.Now);
                int days = (int)Math.Ceiling(ts.TotalDays);
                CurrencyState cs = CurrentState;
                return (cs == CurrencyState.GettingClose) ? String.Format(CultureInfo.CurrentCulture, Resources.Profile.ProfileCurrencyStatusClose, days) :
                                                                                   (cs == CurrencyState.NotCurrent) ? String.Format(CultureInfo.CurrentCulture, Resources.Profile.ProfileCurrencyStatusNotCurrent, -days) : string.Empty;
            }
        }
    }
}
