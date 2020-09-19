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
    /// Determines the status w.r.t. PIC privileges for 61.58, including provisions for 61.58(i), which allows for one month on either side.
    /// </summary>
    public class PIC6158Currency : FlightCurrency
    {
        #region properties
        private readonly List<DateTime> lstDatesOfCurrencyChecks = new List<DateTime>();
        private DateTime? dtExpiration;
        #endregion

        public PIC6158Currency(decimal cThreshold, int period, bool fMonths, string szName) : base(cThreshold, period, fMonths, szName) { }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            if (String.IsNullOrEmpty(cfr.szType))
                return;

            if (cfr.FlightProps.FindEvent(pe => pe.PropertyType.IsPICProficiencyCheck6158) != null)
                lstDatesOfCurrencyChecks.Add(cfr.dtFlight);
        }

        public override bool HasBeenCurrent
        {
            get { return lstDatesOfCurrencyChecks.Count > 0; }
        }

        public override void Finalize(decimal totalTime, decimal picTime)
        {
            // compute the expiration date.
            if (!HasBeenCurrent)
                return;

            // see when last check would have expired
            // we have to go through each of the ones we have seen, due to 61.58(i)'s ridiculous rules
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
                DateTime dtBracketStart = effectiveLastCheck.Date.AddCalendarMonths(ExpirationSpan - 2).AddDays(1);     // first day of the calendar month preceding expiration
                DateTime dtBracketEnd = effectiveLastCheck.Date.AddCalendarMonths(ExpirationSpan + 1);                  // last day of the month following expiration
                if (dtBracketStart.CompareTo(dt.Date) <= 0 && dtBracketEnd.CompareTo(dt.Date) >= 0)                 // fell in the bracket
                    effectiveLastCheck = effectiveLastCheck.AddCalendarMonths(ExpirationSpan);                      // so go ExpirationSpan from that prior check
                else
                    effectiveLastCheck = dt;
            }

            dtExpiration = effectiveLastCheck.AddCalendarMonths(ExpirationSpan);
        }

        public PIC6158Currency AND(PIC6158Currency pic6158Currency)
        {
            if (pic6158Currency == null)
                throw new ArgumentNullException(nameof(pic6158Currency));

            PIC6158Currency result = new PIC6158Currency(RequiredEvents, ExpirationSpan, true, DisplayName);
            if (HasBeenCurrent && pic6158Currency.HasBeenCurrent)
            {
                result.lstDatesOfCurrencyChecks.AddRange(lstDatesOfCurrencyChecks);
                result.lstDatesOfCurrencyChecks.AddRange(pic6158Currency.lstDatesOfCurrencyChecks);
                result.dtExpiration = this.dtExpiration.Value.EarlierDate(pic6158Currency.dtExpiration.Value);
            }
            return result;
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

        public override string DiscrepancyString => string.Empty;
    }
}
