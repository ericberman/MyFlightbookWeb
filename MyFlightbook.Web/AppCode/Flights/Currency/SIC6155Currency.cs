using System;
using System.Collections.Generic;

/******************************************************
 * 
 * Copyright (c) 2007-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    /// <summary>
    /// Determines your status w.r.t. SIC privileges for 61.55(b).
    /// As far as I can tell reading this, you need (within last 12 months):
    ///  (a) 1 takeoff/full-stop landing in an aircraft of this type (can be in an FFS)
    ///  (b) A bunch of training which I'll detect by property
    ///  (c) If you're over by 1 month, you lose that month.  E.g., if you're due in October and renew in Nov, you're good until next October.
    /// Only applies to type-rated aircraft that are NOT single-pilot certified
    /// </summary>
    public class SIC6155Currency : FlightCurrency
    {
        #region properties
        private readonly List<DateTime> lstDatesOfCurrencyChecks = new List<DateTime>();

        private readonly string typeName;

        private readonly FlightCurrency fcTakeoffs = new FlightCurrency(1, 12, true, "61.55 takeoffs");

        private readonly FlightCurrency fcFSLandings = new FlightCurrency(1, 12, true, "61.55 landings");

        private bool fHasSICTime = false;

        private DateTime? dtExpiration;

        private DateTime? dtLastCheckExpiration;
        #endregion

        public SIC6155Currency(string szName, string szType) : base(1, 12, true, szName)
        {
            DisplayName = szName;
            typeName = szType;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            if (!cfr.fIsCertifiedLanding || cfr.fIsCertifiedSinglePilot || cfr.szType.CompareCurrentCultureIgnoreCase(typeName) != 0)
                return;

            fHasSICTime = fHasSICTime || (cfr.SIC > 0);

            fcFSLandings.AddRecentFlightEvents(cfr.dtFlight, cfr.cFullStopLandings);
            int cNightTakeoffs = cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropNightTakeoff);
            int cTakeoffs = cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropTakeoffAny);
            fcTakeoffs.AddRecentFlightEvents(cfr.dtFlight, Math.Max(cNightTakeoffs, cTakeoffs));

            if (cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDProp6155SICCheck))
                lstDatesOfCurrencyChecks.Add(cfr.dtFlight);
        }

        public override bool HasBeenCurrent
        {
            get { return fHasSICTime && lstDatesOfCurrencyChecks.Count > 0 && fcFSLandings.HasBeenCurrent && fcTakeoffs.HasBeenCurrent; }
        }

        public override void Finalize(decimal totalTime, decimal picTime)
        {
            // compute the expiration date.
            if (!HasBeenCurrent)
                return;

            // see when last check would have expired
            // we have to go through each of the ones we have seen, due to 61.55(c)'s ridiculous rules
            // So, for example, suppose we see the following dates:
            //   June 8, 2012 - good until June 30, 2013
            //   June 12, 2013 - good until June 30, 2014
            //   July 9, 2014 - ONLY GOOD until June 30 (since this is in the month after it is due
            //   May 28, 2015 - GOOD UNTIL June 30 2015 (since this is in the month before it was due)
            //   Aug 8, 2016 - Good until Aug 31, 2017
            lstDatesOfCurrencyChecks.Sort();    // need to go in ascending order.
            DateTime effectiveLastCheck = lstDatesOfCurrencyChecks[0];
            foreach (DateTime dt in lstDatesOfCurrencyChecks)
            {
                DateTime dtBracketStart = effectiveLastCheck.Date.AddCalendarMonths(10).AddDays(1);     // first day of the 11th calendar month...
                DateTime dtBracketEnd = effectiveLastCheck.Date.AddCalendarMonths(13);                  // last day of the 13th month
                if (dtBracketStart.CompareTo(dt.Date) <= 0 && dtBracketEnd.CompareTo(dt.Date) >= 0)     // fell in the bracket
                    effectiveLastCheck = effectiveLastCheck.AddCalendarMonths(12);                      // so go 12 months from that prior check
                else
                    effectiveLastCheck = dt;
            }

            dtLastCheckExpiration = effectiveLastCheck.AddCalendarMonths(12);

            dtExpiration = dtLastCheckExpiration.Value.EarlierDate(fcFSLandings.ExpirationDate).EarlierDate(fcTakeoffs.ExpirationDate);
        }

        public override CurrencyState CurrentState
        {
            get
            {
                if (dtExpiration == null || !dtExpiration.HasValue || DateTime.Now.CompareTo(dtExpiration.Value) > 0)
                    return CurrencyState.NotCurrent;
                else if (DateTime.Now.AddDays(30).CompareTo(dtExpiration.Value) > 0)
                    return CurrencyState.GettingClose;
                else
                    return CurrencyState.OK;
            }
        }

        public override DateTime ExpirationDate => dtExpiration != null || dtExpiration.HasValue ? dtExpiration.Value : DateTime.MinValue;

        public override string DiscrepancyString
        {
            get
            {
                if (fcFSLandings.Discrepancy > 0)
                    return Resources.Currency.SIC6155ShortLanding;
                else if (fcTakeoffs.Discrepancy > 0)
                    return Resources.Currency.SIC6155ShortTakeoff;
                else if (dtLastCheckExpiration != null && dtLastCheckExpiration.HasValue && dtLastCheckExpiration.Value.CompareTo(DateTime.Now) < 0)
                    return Resources.Currency.SIC6155ShortProficiencyCheck;
                else
                    return string.Empty;
            }
        }
    }
}
