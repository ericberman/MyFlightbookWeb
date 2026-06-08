using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    /// <summary>
    /// Represents a period of time.  We use it here to represent a period of time when the user was current.
    /// </summary>
    public class CurrencyPeriod
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public CurrencyPeriod()
        {
            StartDate = EndDate = DateTime.MinValue;
        }

        public CurrencyPeriod(DateTime dtStart, DateTime dtEnd)
        {
            StartDate = dtStart;
            EndDate = dtEnd;
        }

        /// <summary>
        /// Extends the start or end date for this as necessary to cover the union of this and the provided currency period, but ONLY IF THEY OVERLAP
        /// </summary>
        /// <param name="cp">The currency period with which to merge</param>
        /// <returns>True if they were merged, false if they didn't overlap</returns>
        public bool MergeWith(CurrencyPeriod cp)
        {
            if (cp == null)
                throw new ArgumentNullException(nameof(cp));
            if (cp.EndDate.CompareTo(StartDate) < 0 || cp.StartDate.CompareTo(EndDate) > 0)
                return false;
            StartDate = cp.StartDate.EarlierDate(this.StartDate);
            EndDate = cp.EndDate.LaterDate(this.EndDate);
            return true;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "Current from {0} to {1}", StartDate.ToShortDateString(), EndDate.ToShortDateString());
        }
    }
}
