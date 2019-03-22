using System;
using System.Globalization;
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
    public enum CloudAhoyManuevers
    {
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
        #endregion

        public CloudAhoyFlight()
        {
            aircraft = new CloudAhoyAircraftDescriptor();
            airports = new CloudAhoyAirportDescriptor[0];
            crew = new CloudAhoyCrewDescriptor[0];
            maneuvers = new CloudAhoyManeuverDescriptor[0];
            cfiaScore = flightId = remarks = url = userRole = string.Empty;
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

            if (crew != null)
            {
                foreach (CloudAhoyCrewDescriptor cd in crew)
                {
                    if (cd.currentUser != 0)
                    {
                        if (cd.PIC != 0)
                            le.PIC = le.TotalFlightTime;

                        CloudAhoyRoles role = CloudAhoyRoles.None;

                        if (Enum.TryParse<CloudAhoyRoles>(cd.role, out role))
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
                                    // TODO: Add safety pilot property
                                    break;
                                case CloudAhoyRoles.Copilot:
                                    le.SIC = le.TotalFlightTime;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }

            // TODO: Add in maneuvers

            return le;
        }
    }
}