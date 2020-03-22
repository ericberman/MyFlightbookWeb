using MyFlightbook.Airports;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Lint
{
    /// <summary>
    /// Class to specify the areas in which checking should occur.
    /// </summary>
    [Flags]
    public enum LintOptions
    {
        None = 0x0000,
        SimIssues = 0x0001,
        IFRIssues = 0x0002,
        AirportIssues = 0x0004,
        XCIssues = 0x0008,
        PICSICDualMath = 0x0010,
        TimeIssues = 0x0020,
        DateTimeIssues = 0x0040,
        MiscIssues = 0x8000
    }

    public class FlightIssue
    {
        #region Properties
        public LintOptions Area { get; set; }

        public string IssueDescription { get; set; }
        #endregion

        public FlightIssue()
        {
            Area = LintOptions.None;
            IssueDescription = string.Empty;
        }

        public FlightIssue(LintOptions area, string description)
        {
            Area = area;
            IssueDescription = description;
        }
    }

    public class FlightWithIssues
    {
        #region Properties
        public LogbookEntryBase Flight { get; private set; }

        public IEnumerable<FlightIssue> Issues { get; private set; }
        #endregion

        public FlightWithIssues(LogbookEntryBase le, IEnumerable<FlightIssue> lst)
        {
            Flight = le;
            Issues = lst;
        }
    }

    /// <summary>
    /// Examines flights (LogbookEntry objects) for common issues that do NOT rise to the level of "error".
    /// </summary>
    public class FlightLint
    {
        #region properties
        protected UserAircraft userAircraft { get; set; }

        protected Aircraft currentAircraft { get; set; }

        protected MakeModel currentModel { get; set; }

        protected AirportList alMaster { get; set; }

        protected AirportList alSubset { get; set; }

        protected CategoryClass.CatClassID currentCatClassID { get; set; }

        protected HashSet<int> seenCheckrides { get; private set; }

        protected List<FlightIssue> currentIssues { get; private set; }

        protected LogbookEntryBase previousFlight { get; set; }

        protected DateTime? dutyStart { get; set; }

        protected DateTime? dutyEnd { get; set; }

        protected DateTime? flightDutyStart { get; set; }

        protected DateTime? flightDutyEnd { get; set; }
        #endregion

        public static UInt32 DefaultOptionsForLocale
        {
            get
            {
                UInt32 defOptions = 0xFFFFFFFF; // everything by default.

                if (CultureInfo.CurrentCulture.Name.ToUpperInvariant().Contains("-US"))
                    defOptions &= ~((UInt32)LintOptions.PICSICDualMath);

                return defOptions;
            }
        }

        public FlightLint() { }

        private void AddConditionalIssue(bool f, LintOptions option, string szIssue)
        {
            if (f)
                currentIssues.Add(new FlightIssue(option, szIssue));
        }

        private void AddIssue(LintOptions option, string szIssue)
        {
            currentIssues.Add(new FlightIssue(option, szIssue));
        }

        /// <summary>
        /// Checks flights for various potential issues
        /// </summary>
        /// <param name="rgle">An enumerable of flights.  MUST BE IN ASCENDING CHRONOLOGICAL ORDER</param>
        /// <param name="options">A bitmask of LintOptions flags</param>
        /// <param name="szUser">The name of the user (necessary to get their aircraft, other preferences</param>
        /// <param name="dtMinDate">Ignore any issues with flights on or before this date</param>
        /// <returns></returns>
        public IEnumerable<FlightWithIssues> CheckFlights(IEnumerable<LogbookEntryBase> rgle, string szUser, UInt32 options, DateTime? dtMinDate = null)
        {
            if (rgle == null)
                throw new ArgumentNullException(nameof(rgle));

            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));

            if (options == 0)
                return Array.Empty<FlightWithIssues>();

            List<FlightWithIssues> lstFlights = new List<FlightWithIssues>();
            userAircraft = new UserAircraft(szUser);

            seenCheckrides = new HashSet<int>();

            StringBuilder sb = new StringBuilder();
            foreach (LogbookEntryBase le in rgle)
                sb.AppendFormat(CultureInfo.CurrentCulture, "{0} ", le.Route);
            alMaster = new AirportList(sb.ToString());

            // context for sequential flight issues
            previousFlight = null;
            dutyStart = dutyEnd = null;
            flightDutyStart = flightDutyEnd = null;


            foreach (LogbookEntryBase le in rgle)
            {
                currentIssues = new List<FlightIssue>();

                // If the flight has any actual errors, add those first
                if (!String.IsNullOrEmpty(le.ErrorString))
                    currentIssues.Add(new FlightIssue() { IssueDescription = le.ErrorString });

                currentAircraft = userAircraft.GetUserAircraftByID(le.AircraftID);

                if (currentAircraft == null)
                {
                    currentIssues.Add(new FlightIssue(LintOptions.MiscIssues, Resources.FlightLint.warningSIMAircraftNotFound));
                    continue;
                }

                currentModel = MakeModel.GetModel(currentAircraft.ModelID);

                currentCatClassID = (le.CatClassOverride == 0 ? currentModel.CategoryClassID : (CategoryClass.CatClassID)le.CatClassOverride);

                if (alMaster != null)
                    alSubset = alMaster.CloneSubset(le.Route);

                if ((options & (UInt32)LintOptions.SimIssues) != 0)
                    CheckSimIssues(le);

                if ((options & (UInt32)LintOptions.IFRIssues) != 0)
                    CheckIFRIssues(le);

                if ((options & (UInt32)LintOptions.AirportIssues) != 0)
                    CheckAirportIssues(le);

                if ((options & (UInt32)LintOptions.XCIssues) != 0)
                    CheckXCIssues(le);

                if ((options & (UInt32)LintOptions.PICSICDualMath) != 0)
                    CheckPICSICDualIssues(le);

                if ((options & (UInt32)LintOptions.TimeIssues) != 0)
                    CheckTimeIssues(le);

                if ((options & (UInt32)LintOptions.DateTimeIssues) != 0)
                    CheckDateTimeIssues(le);

                if ((options & (UInt32)LintOptions.MiscIssues) != 0)
                    CheckMiscIssues(le);

                if (currentIssues.Count > 0 && (dtMinDate == null || (dtMinDate.HasValue && le.Date.CompareTo(dtMinDate.Value) > 0)))
                    lstFlights.Add(new FlightWithIssues(le, currentIssues));

                previousFlight = le;
            }

            return lstFlights;
        }

        private void CheckSimIssues(LogbookEntryBase le)
        {
            bool hasSimRegistration = le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropSimRegistration);
            if (currentAircraft.InstanceType == AircraftInstanceTypes.RealAircraft)
            {
                AddConditionalIssue(le.GroundSim > 0, LintOptions.SimIssues, Resources.FlightLint.warningSIMGroundSimInRealAircraft);
                AddConditionalIssue(hasSimRegistration, LintOptions.SimIssues, Resources.FlightLint.warningSIMDeviceIdentifierOnRealAircraft);
            }
            else
            {
                AddConditionalIssue(le.PIC > 0, LintOptions.SimIssues, Resources.FlightLint.warningSIMPICInSim);
                AddConditionalIssue(le.SIC > 0, LintOptions.SimIssues, Resources.FlightLint.warningSIMSICInSim);
                AddConditionalIssue(le.TotalFlightTime > 0, LintOptions.SimIssues, Resources.FlightLint.warningSIMTotalInSim);
                AddConditionalIssue(le.IMC > 0, LintOptions.SimIssues, Resources.FlightLint.warningSIMActualIMC);
                AddConditionalIssue(le.CrossCountry > 0, LintOptions.SimIssues, Resources.FlightLint.warningSIMCrossCountryInSim);
                AddConditionalIssue(!hasSimRegistration, LintOptions.SimIssues, Resources.FlightLint.warningSIMNoDeviceIdentifier);
            }
        }

        private void CheckIFRIssues(LogbookEntryBase le)
        {
            AddConditionalIssue(le.Approaches > 0 && !le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropApproachName) && ApproachDescription.ExtractApproaches(le.Comment).Count() == 0, LintOptions.IFRIssues, Resources.FlightLint.warningIFRNoApproachDescription);

            AddConditionalIssue(le.SimulatedIFR > 0 && le.Dual == 0 && currentAircraft.InstanceType == AircraftInstanceTypes.RealAircraft && 
                !le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropSafetyPilotName) &&
                !le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropNameOfExaminer), LintOptions.IFRIssues, Resources.FlightLint.warningIFRNoSafetyPilot);
            AddConditionalIssue((le.Approaches > 0 || le.fHoldingProcedures) && le.SimulatedIFR + le.IMC == 0, LintOptions.IFRIssues, Resources.FlightLint.warningIFRApproachesButNoIFR);
        }

        private void CheckAirportIssues(LogbookEntryBase le)
        {
            IEnumerable<string> rgCodes = airport.SplitCodes(le.Route);

            foreach (string szCode in rgCodes)
            {
                if (szCode.StartsWith(airport.ForceNavaidPrefix, StringComparison.CurrentCultureIgnoreCase))
                    continue;

                if (szCode.CompareCurrentCultureIgnoreCase("LOCAL") == 0 || szCode.CompareCurrentCultureIgnoreCase("LCL") == 0)
                {
                    AddIssue(LintOptions.AirportIssues, Resources.FlightLint.warningAirportLocal);
                    continue;
                }

                airport ap = alSubset.GetAirportByCode(szCode);

                if (ap == null)
                    AddIssue(LintOptions.AirportIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningAirportNotFound, szCode));
                else
                {
                    AddConditionalIssue((currentCatClassID == CategoryClass.CatClassID.AMEL || currentCatClassID == CategoryClass.CatClassID.ASEL) && ap.IsSeaport, LintOptions.AirportIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningAirportLandPlaneAtSeaport, szCode));
                    AddConditionalIssue(CategoryClass.IsAirplane(currentCatClassID) && ap.FacilityTypeCode == "H", LintOptions.AirportIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningAirportAirplaneAtHeliport, szCode));
                }
            }

            // Sanity check for speed
            if (le.TotalFlightTime > 0)
            {
                double dist = alSubset.DistanceForRoute();
                double speed = dist / (double) le.TotalFlightTime;

                AddConditionalIssue(speed > (currentModel.EngineType == MakeModel.TurbineLevel.Piston ? 500 : 1000), LintOptions.AirportIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningAirportUnlikelyImpliedSpeed, speed));
            }

            // Look for missing night takeoffs or landings
            if (rgCodes.Count() > 0)
            {
                airport apDep = alSubset.GetAirportByCode(rgCodes.ElementAt(0));
                airport apArr = alSubset.GetAirportByCode(rgCodes.ElementAt(rgCodes.Count() - 1));
                if (apDep != null)
                    AddConditionalIssue(le.FlightStart.HasValue() && !le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropNightTakeoff) &&
                        new Solar.SunriseSunsetTimes(le.FlightStart, apDep.LatLong.Latitude, apDep.LatLong.Longitude).IsFAANight, LintOptions.AirportIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningAirportMissingNightTakeoff, apDep.Code, le.FlightStart.UTCFormattedStringOrEmpty(false)));
                if (apArr != null)
                    AddConditionalIssue(le.FlightEnd.HasValue() && le.NightLandings == 0 &&
                        new Solar.SunriseSunsetTimes(le.FlightEnd, apArr.LatLong.Latitude, apArr.LatLong.Longitude).IsFAANight, LintOptions.AirportIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningAirportMissingNightLanding, apArr.Code, le.FlightEnd.UTCFormattedStringOrEmpty(false)));
            }
        }

        private void CheckXCIssues(LogbookEntryBase le)
        {
            if (currentAircraft.InstanceType != AircraftInstanceTypes.RealAircraft)
                return;

            AddConditionalIssue(le.CrossCountry > 0 && le.CrossCountry.ToMinutes() < le.TotalFlightTime.ToMinutes(), LintOptions.XCIssues, Resources.FlightLint.warningXCNotWholeFlightXC);

            double distance = alSubset.MaxDistanceFromStartingAirport();

            if (le.CrossCountry == 0)
            {
                int minDistanceXC = (currentCatClassID == CategoryClass.CatClassID.Helicopter ? 25 : 50);
                AddConditionalIssue(distance > minDistanceXC, LintOptions.XCIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningXCMissingXC, minDistanceXC));
            }

            if (le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropXCLessThan25nm) && distance > 25)
                AddIssue(LintOptions.XCIssues, Resources.FlightLint.warningXCDistanceLessThan25ButFlewMore);
            else if (le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropXCLessThan50nm) && distance > 50)
                AddIssue(LintOptions.XCIssues, Resources.FlightLint.warningXCDistanceLessThan50ButFlewMore);
            else if (le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropXCMoreThan50nm) && distance < 50)
                AddIssue(LintOptions.XCIssues, Resources.FlightLint.warningXCDistanceMoreThan50ButFlewLess);
            else if (le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropXCMoreThan100nm) && distance < 100)
                AddIssue(LintOptions.XCIssues, Resources.FlightLint.warningXCDistanceMoreThan100ButFlewLess);
        }

        private void CheckPICSICDualIssues(LogbookEntryBase le)
        {
            AddConditionalIssue(le.PIC.ToMinutes() + le.SIC.ToMinutes() + le.Dual.ToMinutes() - le.TotalFlightTime.ToMinutes() != 0, LintOptions.PICSICDualMath, Resources.FlightLint.warningPICSICDualBroken);
        }

        private void CheckTimeIssues(LogbookEntryBase le)
        {
            if (currentAircraft.InstanceType != AircraftInstanceTypes.RealAircraft)
                return;

            int totalMinutes = le.TotalFlightTime.ToMinutes();

            AddConditionalIssue(le.CrossCountry.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesXCGreaterThanTotal);
            AddConditionalIssue(le.Nighttime.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesNightGreaterThanTotal);
            AddConditionalIssue(le.SimulatedIFR.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesSimIFRGreaterThanTotal);
            AddConditionalIssue(le.IMC.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesIMCGreaterThanTotal);
            AddConditionalIssue(le.IMC.ToMinutes() + le.SimulatedIFR.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesSimPlusIMCGreaterThanTotal);
            AddConditionalIssue(le.Dual.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesDualGreaterThanTotal);
            AddConditionalIssue(le.CFI.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesCFIGreaterThanTotal);
            AddConditionalIssue(le.SIC.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesSICGreaterThanTotal);
            AddConditionalIssue(le.PIC.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesPICGreaterThanTotal);

            foreach (CustomFlightProperty cfp in le.CustomProperties)
            {
                AddConditionalIssue(cfp.PropertyType.Type == CFPPropertyType.cfpDecimal && !cfp.PropertyType.IsBasicDecimal &&
                    cfp.PropTypeID != (int)CustomPropertyType.KnownProperties.IDPropGroundInstructionGiven &&
                    cfp.PropTypeID != (int)CustomPropertyType.KnownProperties.IDPropGroundInstructionReceived &&
                    cfp.DecValue.ToMinutes() > totalMinutes, 
                    LintOptions.TimeIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningPropertyGreaterThanTotal, cfp.PropertyType.Title));
            }
        }

        private void CheckDateTimeIssues(LogbookEntryBase le)
        {
            CustomFlightProperty cfpBlockOut = le.CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockOut);
            CustomFlightProperty cfpBlockIn = le.CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockIn);

            // Block out after block in
            AddConditionalIssue(cfpBlockIn != null && cfpBlockOut != null && cfpBlockOut.DateValue.CompareTo(cfpBlockIn.DateValue) > 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningDateTimeInvalidBlock);

            if (le.EngineStart.HasValue())
            {
                // Engine start must be before flight.  Can be after block out, but not before block in
                AddConditionalIssue(le.FlightStart.HasValue() && le.FlightStart.CompareTo(le.EngineStart) < 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningDateEngineAfterFlight);
                AddConditionalIssue(le.FlightEnd.HasValue() && le.FlightEnd.CompareTo(le.EngineStart) < 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightEndBeforeEngineStart);

                AddConditionalIssue(cfpBlockIn != null && cfpBlockIn.DateValue.CompareTo(le.EngineStart) <= 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningEngineStartAfterBlockIn);
            }

            // Flight start must be after engine start (checked above) and after block out
            AddConditionalIssue(le.FlightStart.HasValue() && cfpBlockOut != null && le.FlightStart.CompareTo(cfpBlockOut.DateValue) < 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningDateBlockAfterFlight);

            // Flight end must be after engine/flight start (checked in regular validation), after block out, and before block in
            if (le.FlightEnd.HasValue())
            {
                AddConditionalIssue(cfpBlockOut != null && le.FlightEnd.CompareTo(cfpBlockOut.DateValue) < 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightEndBeforeBlockOut);
                AddConditionalIssue(cfpBlockIn != null && le.FlightEnd.CompareTo(cfpBlockIn.DateValue) > 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightEndAfterBlockIn);
                AddConditionalIssue(le.EngineEnd.HasValue() && le.FlightEnd.CompareTo(le.EngineEnd) > 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightEndAfterEngineEnd);
            }

            if (le.EngineEnd.HasValue())
            {
                AddConditionalIssue(cfpBlockIn != null && le.EngineEnd.CompareTo(cfpBlockIn.DateValue) < 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningEngineEndBeforeBlockIn);
            }

            const int MaxHoursDifference = 48;
            // Check that engine, flight, and block times are all roughly equal to date-of-flight
            AddConditionalIssue((le.EngineStart.HasValue() && Math.Abs(le.EngineStart.Subtract(le.Date).TotalHours) > MaxHoursDifference) ||
                (le.EngineEnd.HasValue() && Math.Abs(le.EngineEnd.Subtract(le.Date).TotalHours) > MaxHoursDifference),
                LintOptions.DateTimeIssues, Resources.FlightLint.warningEngineTimeDiffersDate);

            AddConditionalIssue((le.FlightStart.HasValue() && Math.Abs(le.FlightStart.Subtract(le.Date).TotalHours) > MaxHoursDifference) ||
                (le.FlightEnd.HasValue() && Math.Abs(le.FlightEnd.Subtract(le.Date).TotalHours) > MaxHoursDifference),
                LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightTimeDiffersDate);

            AddConditionalIssue((cfpBlockOut != null && Math.Abs(cfpBlockOut.DateValue.Subtract(le.Date).TotalHours) > MaxHoursDifference) ||
                (cfpBlockIn != null && Math.Abs(cfpBlockIn.DateValue.Subtract(le.Date).TotalHours) > MaxHoursDifference),
                LintOptions.DateTimeIssues, Resources.FlightLint.warningBlockTimeDiffersDate);

            // Look for issues with sequential flights
            CheckSequentialFlightIssues(le);
        }

        private void CheckSequentialFlightIssues(LogbookEntryBase le)
        {
            if (previousFlight == null)
                return;

            AddConditionalIssue(previousFlight.EngineEnd.HasValue() && le.EngineStart.HasValue() && previousFlight.EngineEnd.CompareTo(le.EngineStart) > 0,
                LintOptions.DateTimeIssues, Resources.FlightLint.warningPreviousEngineEndsAfterStart);

            AddConditionalIssue(previousFlight.FlightEnd.HasValue() && le.FlightStart.HasValue() && previousFlight.FlightEnd.CompareTo(le.FlightStart) > 0,
                LintOptions.DateTimeIssues, Resources.FlightLint.warningPreviousFlightEndsAfterStart);

            CustomFlightProperty cfpBlockOut = le.CustomProperties[CustomPropertyType.KnownProperties.IDBlockOut];
            AddConditionalIssue(cfpBlockOut != null && previousFlight.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDBlockIn) && cfpBlockOut.DateValue.CompareTo(previousFlight.CustomProperties[CustomPropertyType.KnownProperties.IDBlockIn].DateValue) < 0,
                LintOptions.DateTimeIssues, Resources.FlightLint.warningPreviousFlightBlockEndAfterStart);

            // Look for a new duty start when a prior period is still open or a duty end when a prior period is NOT open
            CustomFlightProperty cfpDutyStart = le.CustomProperties[CustomPropertyType.KnownProperties.IDPropDutyStart];
            CustomFlightProperty cfpFlightDutyStart = le.CustomProperties[CustomPropertyType.KnownProperties.IDPropFlightDutyTimeStart];
            CustomFlightProperty cfpDutyEnd = le.CustomProperties[CustomPropertyType.KnownProperties.IDPropDutyEnd];
            CustomFlightProperty cfpFlightDutyEnd = le.CustomProperties[CustomPropertyType.KnownProperties.IDPropFlightDutyTimeEnd];

            AddConditionalIssue(dutyStart != null && dutyStart.HasValue && cfpDutyStart != null, LintOptions.DateTimeIssues, Resources.FlightLint.warningNewDutyStart);
            AddConditionalIssue(flightDutyStart != null && flightDutyStart.HasValue && cfpFlightDutyStart != null, LintOptions.DateTimeIssues, Resources.FlightLint.warningNewFlightDutyStart);

            AddConditionalIssue(dutyEnd != null && dutyEnd.HasValue && cfpDutyStart != null && cfpDutyStart.DateValue.CompareTo(dutyEnd.Value) < 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningDutyStartPriorToPreviousDutyEnd);
            AddConditionalIssue(flightDutyEnd != null && flightDutyEnd.HasValue && cfpFlightDutyStart != null && cfpFlightDutyStart.DateValue.CompareTo(flightDutyEnd.Value) < 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightDutyStartPriorToPreviousFlightDutyEnd);

            AddConditionalIssue(dutyEnd != null && cfpDutyStart == null && cfpDutyEnd != null, LintOptions.DateTimeIssues, Resources.FlightLint.warningNewDutyEndNoStart);
            AddConditionalIssue(flightDutyEnd != null && cfpFlightDutyStart == null && cfpFlightDutyEnd != null, LintOptions.DateTimeIssues, Resources.FlightLint.warningNewFlightDutyEndNoStart);

            // Close off a duty period if we have a duty end; if we're starting (or restarting) a duty period (and not closing it in the same flight), reset the duty period
            if (cfpDutyEnd != null)
            {
                dutyStart = null;   // don't have an open duty period
                dutyEnd = cfpDutyEnd.DateValue;
            }
            else if (cfpDutyStart != null)
                dutyStart = cfpDutyStart.DateValue;

            if (cfpFlightDutyEnd != null)
            {
                flightDutyStart = null;  // don't have an open flight duty period
                flightDutyEnd = cfpFlightDutyEnd.DateValue;
            }
            else if (cfpFlightDutyStart != null)
                flightDutyStart = cfpFlightDutyStart.DateValue;
        }

        private void CheckMiscIssues(LogbookEntryBase le)
        {
            foreach (CustomFlightProperty cfp in le.CustomProperties)
            {
                if (cfp.PropertyType.IsExcludedFromMRU)
                {
                    AddConditionalIssue(seenCheckrides.Contains(cfp.PropTypeID), LintOptions.MiscIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningMiscMultipleRedundantCheckrides, cfp.PropertyType.Title));
                    seenCheckrides.Add(cfp.PropTypeID);
                }
            }

            AddConditionalIssue(le.Dual > 0 && !le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropInstructorName) && le.CFISignatureState == LogbookEntryBase.SignatureState.None, LintOptions.MiscIssues, Resources.FlightLint.warningDualLoggedButNoCFIName);

            LogbookEntryBase leDefault = new LogbookEntry() { Date = le.Date, AircraftID = le.AircraftID, User = le.User };
            AddConditionalIssue(le.IsEqualTo(leDefault), LintOptions.MiscIssues, Resources.FlightLint.warningFlightHasNoData);
        }
    }
}