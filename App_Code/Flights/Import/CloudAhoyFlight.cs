using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights.CloudAhoy
{
    /// <summary>
    /// A flight as represented in CloudAhoy
    /// </summary>
    [Serializable]
    public enum CloudAhoyManeuvers
    {
        unknown,
        chandelle,
        land,
        lineupAndWait,
        missedApproach,
        oscarPattern,
        slowFlight,
        spiral,
        stall,
        stopAndGo,
        sTurns,
        takeoff,
        touchAndGo,
        trafficPattern,
        turbulence,
        turn360,
        glide,
        tow,
        thermalling,
        autoRotate,
        hover
    }

    public enum CloudAhoyRoles
    {
        None, Pilot, Copilot, Student, Instructor, Passenger, SafetyPilot, Examiner
    }

    [Serializable]
    public class CloudAhoyAircraftDescriptor
    {
        public string make { get; set; }
        public string model { get; set; }
        public string registration { get; set; }

        public CloudAhoyAircraftDescriptor()
        {
            make = model = registration = string.Empty;
        }
    }

    [Serializable]
    public class CloudAhoyAirportDescriptor
    {
        public string icao { get; set; }
        public string name { get; set; }

        public CloudAhoyAirportDescriptor()
        {
            icao = name = string.Empty;
        }
    }

    [Serializable]
    public class CloudAhoyCrewDescriptor
    {
        public int PIC { get; set; }
        public int checkride { get; set; }
        public int solo { get; set; }
        public int currentUser { get; set; }
        public string role { get; set; }
        public string name { get; set; }

        public CloudAhoyCrewDescriptor()
        {
            role = name = string.Empty;
        }
    }

    [Serializable]
    public class CloudAhoyManeuverDescriptor
    {
        public string maneuverId { get; set; }
        public string url { get; set; }
        public string code { get; set; }
        public string label { get; set; }

        public CloudAhoyManeuverDescriptor()
        {
            maneuverId = url = code = label = string.Empty;
        }
    }

    public class CloudAhoyFlight : ExternalFormat
    {
        #region Properties
        public CloudAhoyAircraftDescriptor aircraft { get; set; }
        public CloudAhoyAirportDescriptor[] airports { get; set; }
        public string cfiaScore { get; set; }
        public CloudAhoyCrewDescriptor[] crew { get; set; }
        public long duration { get; set; }
        public string flightId { get; set; }
        public CloudAhoyManeuverDescriptor[] maneuvers { get; set; }
        public string remarks { get; set; }
        public long time { get; set; }
        public string url { get; set; }
        public string userRole { get; set; }

        private Dictionary<CustomPropertyType.KnownProperties, CustomFlightProperty> DictProps { get; set; }
        #endregion

        public CloudAhoyFlight()
        {
            aircraft = new CloudAhoyAircraftDescriptor();
            airports = new CloudAhoyAirportDescriptor[0];
            crew = new CloudAhoyCrewDescriptor[0];
            maneuvers = new CloudAhoyManeuverDescriptor[0];
            cfiaScore = flightId = remarks = url = userRole = string.Empty;
            DictProps = new Dictionary<CustomPropertyType.KnownProperties, CustomFlightProperty>();
        }

        private void PopulateCrewInfo(LogbookEntry le)
        {
            if (crew == null)
                return;
            foreach (CloudAhoyCrewDescriptor cd in crew)
            {
                if (cd.currentUser != 0)
                {
                    if (cd.PIC != 0)
                        le.PIC = le.TotalFlightTime;

                    CloudAhoyRoles role = CloudAhoyRoles.None;

                    if (Enum.TryParse<CloudAhoyRoles>(cd.role.Replace(" ", string.Empty), out role))
                    {
                        switch (role)
                        {
                            case CloudAhoyRoles.Instructor:
                                le.CFI = le.TotalFlightTime;
                                break;
                            case CloudAhoyRoles.Student:
                                le.Dual = le.TotalFlightTime;
                                break;
                            case CloudAhoyRoles.SafetyPilot:
                                DictProps[CustomPropertyType.KnownProperties.IDPropSafetyPilotTime] = PropertyWithValue(CustomPropertyType.KnownProperties.IDPropSafetyPilotTime, le.TotalFlightTime);
                                break;
                            case CloudAhoyRoles.Copilot:
                                le.SIC = le.TotalFlightTime;
                                break;
                            default:
                                break;
                        }
                    }

                    if (cd.solo != 0)
                        DictProps[CustomPropertyType.KnownProperties.IDPropSolo] = PropertyWithValue(CustomPropertyType.KnownProperties.IDPropSolo, le.TotalFlightTime);
                }
            }
        }

        private void PopulateManeuvers(LogbookEntry le)
        {
            if (maneuvers == null)
                return;

            foreach (CloudAhoyManeuverDescriptor md in maneuvers)
            {
                CloudAhoyManeuvers maneuver = CloudAhoyManeuvers.unknown;
                if (Enum.TryParse<CloudAhoyManeuvers>(md.code, out maneuver))
                {
                    switch (maneuver)
                    {
                        case CloudAhoyManeuvers.land:
                            le.Landings++;
                            break;
                        case CloudAhoyManeuvers.stopAndGo:
                            le.Landings++;
                            le.FullStopLandings++;
                            break;
                        case CloudAhoyManeuvers.touchAndGo:
                            le.Landings++;
                            break;
                        case CloudAhoyManeuvers.missedApproach:
                            le.Approaches++;
                            break;
                        case CloudAhoyManeuvers.slowFlight:
                            DictProps[CustomPropertyType.KnownProperties.IDPropManeuverSlowFlight] = PropertyWithValue(CustomPropertyType.KnownProperties.IDPropManeuverSlowFlight, true);
                            break;
                        case CloudAhoyManeuvers.chandelle:
                            DictProps[CustomPropertyType.KnownProperties.IDPropManeuverChandelle] = PropertyWithValue(CustomPropertyType.KnownProperties.IDPropManeuverChandelle, true);
                            break;
                        case CloudAhoyManeuvers.sTurns:
                            DictProps[CustomPropertyType.KnownProperties.IDPropManeuverSTurns] = PropertyWithValue(CustomPropertyType.KnownProperties.IDPropManeuverSTurns, true);
                            break;
                        case CloudAhoyManeuvers.stall:
                            DictProps[CustomPropertyType.KnownProperties.IDPropPowerOffStall] = PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPowerOffStall, true);
                            break;
                        case CloudAhoyManeuvers.autoRotate:
                            DictProps[CustomPropertyType.KnownProperties.IDPropAutoRotate] = PropertyWithValue(CustomPropertyType.KnownProperties.IDPropAutoRotate, true);
                            break;
                        case CloudAhoyManeuvers.hover:
                            DictProps[CustomPropertyType.KnownProperties.IDPropHover] = PropertyWithValue(CustomPropertyType.KnownProperties.IDPropHover, true);
                            break;
                        case CloudAhoyManeuvers.tow:
                            if (!DictProps.ContainsKey(CustomPropertyType.KnownProperties.IDPropGliderTow))
                                DictProps[CustomPropertyType.KnownProperties.IDPropGliderTow] = PropertyWithValue(CustomPropertyType.KnownProperties.IDPropGliderTow, 0);
                            DictProps[CustomPropertyType.KnownProperties.IDPropGliderTow].IntValue++;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public override LogbookEntry ToLogbookEntry()
        {
            StringBuilder sb = new StringBuilder();
            if (airports != null)
                foreach (CloudAhoyAirportDescriptor ap in airports)
                    sb.AppendFormat(CultureInfo.CurrentCulture, "{0} ", ap.icao);
            if (aircraft == null)
                aircraft = new CloudAhoyAircraftDescriptor();

            DateTime dtStart = DateTimeOffset.FromUnixTimeSeconds(time).DateTime;

            DictProps.Clear();

            LogbookEntry le = new LogbookEntry()
            {
                FlightID = LogbookEntry.idFlightNew,
                TailNumDisplay = aircraft.registration,
                ModelDisplay = aircraft.model,
                Route = sb.ToString().Trim(),
                Comment = String.IsNullOrEmpty(url) ? remarks : String.Format(CultureInfo.CurrentCulture, "{0} ({1})", remarks, url),
                TotalFlightTime = duration / 3600.0M,
                EngineStart = dtStart,
                EngineEnd = dtStart.AddSeconds(duration),
                Date = dtStart.Date
            };

            PopulateCrewInfo(le);
            PopulateManeuvers(le);

            le.CustomProperties = PropertiesWithoutNullOrDefault(DictProps.Values).ToArray();

            return le;
        }
    }
}