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
    /// Currency object for basic passenger carrying
    /// </summary>
    public class PassengerCurrency : FlightCurrency
    {
        protected bool RequireDayLandings { get; set; }

        /// <summary>
        /// A currency object that requires 3 landings in the previous 90 days
        /// </summary>
        /// <param name="szName">the name for the currency</param>
        /// <param name="fRequireDayLandings">True to require day landings for day currency</param>
        public PassengerCurrency(string szName, bool fRequireDayLandings) : base(3, 90, false, szName)
        {
            RequireDayLandings = fRequireDayLandings;
            Query = new FlightQuery()
            {
                DateRange = FlightQuery.DateRanges.Trailing90,
                HasLandings = true,
                PropertiesConjunction = GroupConjunction.None
            };
            Query.PropertyTypes.Add(CustomPropertyType.GetCustomPropertyType((int)CustomPropertyType.KnownProperties.IDPropPilotMonitoring));
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            base.ExamineFlight(cfr);

            // we need to subtract out monitored landings, or ignore all if you were monitoring for the whole flight
            int cMonitoredLandings = cfr.FlightProps.TotalCountForPredicate(p => p.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropMonitoredDayLandings || p.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropMonitoredNightLandings);

            // Also need to subtract out night landings if day landings are required
            int cNightLandings = RequireDayLandings ? cfr.cFullStopNightLandings + cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropNightTouchAndGo) : 0;

            if (!cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropPilotMonitoring))
                AddRecentFlightEvents(cfr.dtFlight, Math.Max(cfr.cLandingsThisFlight - cMonitoredLandings - cNightLandings, 0));
        }
    }
}
