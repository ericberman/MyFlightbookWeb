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
    /// <summary>
    /// Army MDS currency
    /// </summary>
    public class ArmyMDSCurrency : FlightCurrency
    {
        /// <summary>
        /// A currency object that requires 1 event in the previous 60 days
        /// </summary>
        /// <param name="szName">the name for the currency</param>
        public ArmyMDSCurrency(string szName) : base(1, 60, false, szName)
        {
        }

        public override string DiscrepancyString
        {
            get { return this.IsCurrent() ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, this.Discrepancy, (this.Discrepancy > 1) ? Resources.Currency.CustomCurrencyEventFlights : Resources.Currency.CustomCurrencyEventFlight); }
        }
    }

    /// <summary>
    /// Army MDS (95-1) night-vision currency: 1 hour of NV time within 60 days.
    /// </summary>
    public class ArmyMDSNVCurrency : ArmyMDSCurrency
    {
        public ArmyMDSNVCurrency(string szName) : base(szName)
        {
            RequiredEvents = Discrepancy = 1.0M;
        }

        public override string DiscrepancyString
        {
            get { return this.IsCurrent() ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, this.Discrepancy, Resources.Currency.Hours); }
        }
    }
}
