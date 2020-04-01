using System;

/******************************************************
 * 
 * Copyright (c) 2007-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    /// <summary>
    /// Currency object for basic tailwheel
    /// </summary>
    public class TailwheelCurrency : FlightCurrency
    {
        /// <summary>
        /// A currency object that requires 3 full-stop landings in the previous 90 days
        /// </summary>
        /// <param name="szName">the name for the currency</param>
        public TailwheelCurrency(string szName) : base(3, 90, false, szName)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            base.ExamineFlight(cfr);

            // we need to subtract out monitored landings, or ignore all if you were monitoring for the whole flight
            int cMonitoredLandings = cfr.FlightProps.TotalCountForPredicate(p => p.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropMonitoredDayLandings || p.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropMonitoredNightLandings);
            if (!cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropPilotMonitoring))
                AddRecentFlightEvents(cfr.dtFlight, Math.Max(cfr.cFullStopLandings + cfr.cFullStopNightLandings - cMonitoredLandings, 0));
        }
    }
}
