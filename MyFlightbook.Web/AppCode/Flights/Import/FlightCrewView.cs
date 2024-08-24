using System;
using System.Globalization;
using System.Collections.Generic;
using Dropbox.Api.TeamLog;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    [Serializable]
    public class FlightCrewFlightsResult
    {
        public IEnumerable<FlightCrewViewFlight> flights { get; set; } = Array.Empty<FlightCrewViewFlight>();
    }

    [Serializable]
    public class FlightCrewViewCrew
    {
        public string position { get; set; }
        public string name { get; set; }
        public string id { get; set; }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} ({1} - {2})", name ?? string.Empty, position ?? string.Empty, id ?? string.Empty);
        }

        public string DisplayName
        {
            get { return String.Format(CultureInfo.CurrentCulture, "{0}{1}", name ?? string.Empty, String.IsNullOrEmpty(id) ? string.Empty : String.Format(CultureInfo.CurrentCulture, " ({0})", id)); }
        }
    }


    [Serializable]
    public class FlightCrewViewFlight : ExternalFormat
    {
        #region Properties
        public FlightCrewViewCrew[] crew_list { get; set; } = Array.Empty<FlightCrewViewCrew>();
        public string fcv_flight_id { get; set; }
        public string flight_number { get; set; }
        public int is_deadhead { get; set; }
        public string dep_airport { get; set; }
        public string dep_airport_icao { get; set; }
        public string arr_airport { get; set; }
        public string arr_airport_icao { get; set; }
        public string block { get; set; }
        public string tail_info { get; set; }
        public string fcv_tail_number { get; set; }
        public string fcv_aircraft_type { get; set; }
        public DateTime? scheduled_out_local { get; set; }
        public DateTime? scheduled_out_utc { get; set; }
        public DateTime? scheduled_in_local { get; set; }
        public DateTime? scheduled_in_utc { get; set; }
        public DateTime? actual_out_local { get; set; }
        public DateTime? actual_out_utc { get; set; }
        public DateTime? actual_off_local { get; set; }
        public DateTime? actual_off_utc { get; set; }
        public DateTime? actual_on_local { get; set; }
        public DateTime? actual_on_utc { get; set; }
        public DateTime? actual_in_local { get; set; }
        public DateTime? actual_in_utc { get; set; }
        public string dep_runway { get; set; }
        public string arr_runway { get; set; }

        private decimal blockTime
        {
            get
            {
                MatchCollection mc = RegexUtility.regexHHMM.Matches(block ?? string.Empty);
                return mc.Count == 1 ? Convert.ToInt32(mc[0].Groups["hour"].Value, CultureInfo.InvariantCulture) + ((decimal)Convert.ToInt32(mc[0].Groups["minute"].Value, CultureInfo.InvariantCulture) / 60.0M) : 0.0M;
            }
        }

        private decimal elapsedTime
        {
            get { return (actual_out_utc.HasValue && actual_in_utc.HasValue && actual_in_utc.Value.CompareTo(actual_out_utc.Value) == 1) ? (decimal)actual_in_utc.Value.Subtract(actual_out_utc.Value).TotalHours : blockTime; }
        }
        #endregion

        public override LogbookEntry ToLogbookEntry()
        {
            PendingFlight le = new PendingFlight()
            {
                Route = String.Join(Resources.LocalizedText.LocalizedSpace, new string[] { dep_airport_icao ?? string.Empty, arr_airport_icao ?? string.Empty }).Trim(),
                TailNumDisplay = fcv_tail_number ?? string.Empty,
                ModelDisplay = fcv_aircraft_type,
                Date = scheduled_out_local ?? DateTime.MinValue,
                TotalFlightTime =  elapsedTime,
                FlightStart = actual_off_utc ?? DateTime.MinValue,
                FlightEnd = actual_on_utc ?? DateTime.MinValue
            };

            List<string> lstOtherCrew = new List<string>();
            List<string> lstFOs = new List<string>();
            List<string> lstROs = new List<string>();
            FlightCrewViewCrew captain = null;
            foreach (FlightCrewViewCrew c in crew_list)
            {
                if ((c.position?.CompareCurrentCultureIgnoreCase("CA") ?? 1) == 0)
                    captain = c;
                else if ((c.position?.CompareCurrentCultureIgnoreCase("FO") ?? 1) == 0)
                    lstFOs.Add(c.ToString());
                else if ((c.position?.CompareCurrentCultureIgnoreCase("RO") ?? 1) == 0)
                    lstROs.Add(c.ToString());
                else
                    lstOtherCrew.Add(c.ToString());
            }

            le.CustomProperties.SetItems(new CustomFlightProperty[] {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightNumber, flight_number),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockOut, (DateTime) (actual_out_utc ?? DateTime.MinValue), true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockIn, (DateTime) (actual_in_utc ?? DateTime.MinValue), true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropScheduledDeparture, (DateTime) (scheduled_out_utc ?? DateTime.MinValue), true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropScheduledArrival, (DateTime) (scheduled_in_utc ?? DateTime.MinValue), true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropDeadhead, is_deadhead != 0),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropCaptainName, captain?.DisplayName ?? string.Empty),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFirstOfficerName, String.Join(" ", lstFOs)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropReliefCrewNames, String.Join(" ", lstROs)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropAdditionalCrew, String.Join(" ", lstOtherCrew)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropRunwaysUsed, String.Join(" ", new string[] { dep_runway, arr_runway }))
            });

            return le;
        }
    }
}