using MyFlightbook.Airports;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2020-2025 MyFlightbook LLC
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
        MiscIssues = 0x8000,
        IncludeIgnored = 0x00010000
    }

    [Serializable]
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

    [Serializable]
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
        public const string IgnoreMarker = "\u2006";    // six-per-em space; a very thin space.  See https://en.wikipedia.org/wiki/Whitespace_character

        public static void SetIgnoreFlagForFlight(LogbookEntryCore le, bool fIgnore)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));

            le.Route = String.Format(CultureInfo.CurrentCulture, "{0}{1}", le.Route.Trim(), fIgnore ? IgnoreMarker : string.Empty);
            FlightResultManager.InvalidateForUser(le.User);
        }

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
                UInt32 defOptions = 0xFFFFFFFF & ~((UInt32)LintOptions.IncludeIgnored); // ALMOST everything by default.

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

        private void CheckIfOption(bool b, LogbookEntryBase le, Action<LogbookEntryBase> action)
        {
            if (b)
                action(le);
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

                try
                {
                    // If the flight has any actual errors, add those first
                    if (!String.IsNullOrEmpty(le.ErrorString))
                        currentIssues.Add(new FlightIssue() { IssueDescription = le.ErrorString });

                    // ignore deadhead flights or flights that have been explicitly ignored (indicated by IgnoreMarker at end of string)
                    if (le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropDeadhead) || ((options & (UInt32)LintOptions.IncludeIgnored) == 0 && le.Route.EndsWith(IgnoreMarker, StringComparison.CurrentCultureIgnoreCase)))
                        continue;

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

                    CheckIfOption((options & (UInt32)LintOptions.SimIssues) != 0, le, CheckSimIssues);
                    CheckIfOption((options & (UInt32)LintOptions.IFRIssues) != 0, le, CheckIFRIssues);
                    CheckIfOption((options & (UInt32)LintOptions.AirportIssues) != 0, le, CheckAirportIssues);
                    CheckIfOption((options & (UInt32)LintOptions.XCIssues) != 0, le, CheckXCIssues);
                    CheckIfOption((options & (UInt32)LintOptions.PICSICDualMath) != 0, le, CheckPICSICDualIssues);
                    CheckIfOption(((options & (UInt32)LintOptions.TimeIssues) != 0 && currentAircraft.InstanceType == AircraftInstanceTypes.RealAircraft), le, CheckTimeIssues);
                    CheckIfOption((options & (UInt32)LintOptions.DateTimeIssues) != 0, le, CheckDateTimeIssues);
                    CheckIfOption((options & (UInt32)LintOptions.MiscIssues) != 0, le,CheckMiscIssues);
                }
                catch (Exception ex) when (!(ex is OutOfMemoryException))
                {
                    // issue # 1332 - check flights can throw an exception sometimes; catch it as an issue
                    currentIssues.Add(new FlightIssue() { IssueDescription = ex.Message });
                }

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
            AddConditionalIssue(le.Approaches > 0 && !le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropApproachName) && !ApproachDescription.ExtractApproaches(le.Comment).Any(), LintOptions.IFRIssues, Resources.FlightLint.warningIFRNoApproachDescription);

            AddConditionalIssue(le.SimulatedIFR > 0 && le.Dual == 0 && currentAircraft.InstanceType == AircraftInstanceTypes.RealAircraft && 
                !le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropSafetyPilotName) &&
                !le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropNameOfExaminer), LintOptions.IFRIssues, Resources.FlightLint.warningIFRNoSafetyPilot);
            AddConditionalIssue((le.Approaches > 0 || le.fHoldingProcedures) && le.SimulatedIFR + le.IMC == 0, LintOptions.IFRIssues, Resources.FlightLint.warningIFRApproachesButNoIFR);

            AddConditionalIssue(le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropNightIMC) > Math.Min(le.IMC, le.Nighttime), LintOptions.IFRIssues, Resources.FlightLint.warningIFRInvalidNightIMC);
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
            if (le.TotalFlightTime > 0 && currentAircraft.InstanceType == AircraftInstanceTypes.RealAircraft)
            {
                double dist = alSubset.DistanceForRoute();
                double speed = dist / (double) le.TotalFlightTime;

                AddConditionalIssue(speed > (currentModel.EngineType == MakeModel.TurbineLevel.Piston ? 500 : 1000), LintOptions.AirportIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningAirportUnlikelyImpliedSpeed, speed));
            }

            // Look for missing night takeoffs or landings
            // Issue #1129: suppress this check if pilot monitoring whole flight.
            if (rgCodes.Any() && !le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropPilotMonitoring))
            {
                airport apDep = alSubset.GetAirportByCode(rgCodes.ElementAt(0));
                airport apArr = alSubset.GetAirportByCode(rgCodes.ElementAt(rgCodes.Count() - 1));
                if (apDep != null)
                    AddConditionalIssue(le.FlightStart.HasValue() && !le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropNightTakeoff) &&
                        new SolarTools.SunriseSunsetTimes(le.FlightStart, apDep.LatLong.Latitude, apDep.LatLong.Longitude).IsFAANight, LintOptions.AirportIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningAirportMissingNightTakeoff, apDep.Code, le.FlightStart.UTCFormattedStringOrEmpty(false)));
                if (apArr != null)
                    AddConditionalIssue(le.FlightEnd.HasValue() && le.NightLandings == 0 &&
                        new SolarTools.SunriseSunsetTimes(le.FlightEnd, apArr.LatLong.Latitude, apArr.LatLong.Longitude).IsFAANight, LintOptions.AirportIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningAirportMissingNightLanding, apArr.Code, le.FlightEnd.UTCFormattedStringOrEmpty(false)));
            }
        }

        private void CheckXCIssues(LogbookEntryBase le)
        {
            if (currentAircraft.InstanceType != AircraftInstanceTypes.RealAircraft)
                return;

            decimal safetyPilotTime = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropSafetyPilotTime);
            AddConditionalIssue(le.CrossCountry > 0 && le.CrossCountry.ToMinutes() < le.TotalFlightTime.ToMinutes(), LintOptions.XCIssues, Resources.FlightLint.warningXCNotWholeFlightXC);
            AddConditionalIssue(le.CrossCountry > 0 && (le.CFI + le.Dual + le.SIC + le.PIC).ToMinutes() == 0, LintOptions.XCIssues, Resources.FlightLint.warningXCTimeFoundButNoRole);
            AddConditionalIssue(le.CrossCountry > 0 && safetyPilotTime > 0, LintOptions.XCIssues, Resources.FlightLint.warningXCTimeFoundForSafetyPilot);

            double distance = alSubset.MaxDistanceFromStartingAirport();

            if (le.CrossCountry == 0 && safetyPilotTime == 0)
            {
                int minDistanceXC = (currentCatClassID == CategoryClass.CatClassID.Helicopter ? 25 : 50);
                AddConditionalIssue(distance > minDistanceXC, LintOptions.XCIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningXCMissingXC, minDistanceXC));
            }

            decimal xcATP = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropXCNoLanding);
            AddConditionalIssue(xcATP > 0 && le.CrossCountry > 0, LintOptions.XCIssues, Resources.FlightLint.warningATPXCAndXCFound);
            AddConditionalIssue(xcATP > 0 && xcATP.ToMinutes() != le.TotalFlightTime.ToMinutes(), LintOptions.XCIssues, Resources.FlightLint.warningATPXCNotEqualTotal);

            bool fxcLessThan25 = le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropXCLessThan25nm);
            bool fxcLessThan50 = le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropXCLessThan50nm);
            bool fxcMoreThan50 = le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropXCMoreThan50nm);
            bool fxcMoreThan100 = le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropXCMoreThan100nm);
            bool fxcMoreThan400 = le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropXCMoreThan400nm);

            AddConditionalIssue(fxcLessThan25 && distance > 25, LintOptions.XCIssues, Resources.FlightLint.warningXCDistanceLessThan25ButFlewMore);
            AddConditionalIssue(fxcLessThan50 && distance > 50, LintOptions.XCIssues, Resources.FlightLint.warningXCDistanceLessThan50ButFlewMore);
            AddConditionalIssue(fxcMoreThan50 && distance < 50, LintOptions.XCIssues, Resources.FlightLint.warningXCDistanceMoreThan50ButFlewLess);
            AddConditionalIssue(fxcMoreThan100 && distance < 100, LintOptions.XCIssues, Resources.FlightLint.warningXCDistanceMoreThan100ButFlewLess);
            AddConditionalIssue(fxcMoreThan400 && distance < 400, LintOptions.XCIssues, Resources.FlightLint.warningXCDistanceMoreThan400ButFlewLess);
            AddConditionalIssue((fxcLessThan25 ? 1 : 0) + (fxcLessThan50 ? 1 : 0) + (fxcMoreThan50 ? 1 : 0) + (fxcMoreThan100 ? 1 : 0) + (fxcMoreThan400 ? 1 : 0) > 1, LintOptions.XCIssues, Resources.FlightLint.warningXCInconsistentDistances);

            decimal xcLessThan25 = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropXCLessThan25nm);
            decimal xcLessThan50 = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropXCLessThan50nm);
            decimal xcMoreThan50 = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropXCMoreThan50nm);
            decimal xcMoreThan100 = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropXCMoreThan100nm);
            decimal xcMoreThan400 = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropXCMoreThan400nm);

            AddConditionalIssue(xcLessThan25 > 0 && xcLessThan25.ToMinutes() != le.CrossCountry.ToMinutes(), LintOptions.XCIssues, Resources.FlightLint.warningXCTimeDistanceNotEqualXC);
            AddConditionalIssue(xcLessThan50 > 0 && xcLessThan50.ToMinutes() != le.CrossCountry.ToMinutes(), LintOptions.XCIssues, Resources.FlightLint.warningXCTimeDistanceNotEqualXC);
            AddConditionalIssue(xcMoreThan50 > 0 && xcMoreThan50.ToMinutes() != le.CrossCountry.ToMinutes(), LintOptions.XCIssues, Resources.FlightLint.warningXCTimeDistanceNotEqualXC);
            AddConditionalIssue(xcMoreThan100 > 0 && xcMoreThan100.ToMinutes() != le.CrossCountry.ToMinutes(), LintOptions.XCIssues, Resources.FlightLint.warningXCTimeDistanceNotEqualXC);
            AddConditionalIssue(xcMoreThan400 > 0 && xcMoreThan400.ToMinutes() != le.CrossCountry.ToMinutes(), LintOptions.XCIssues, Resources.FlightLint.warningXCTimeDistanceNotEqualXC);
        }

        private void CheckPICSICDualIssues(LogbookEntryBase le)
        {
            int basetime = le.PIC.ToMinutes() + le.SIC.ToMinutes() + le.Dual.ToMinutes();
            int picus = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropPICUS).ToMinutes();
            int totalMinutes = le.TotalFlightTime.ToMinutes();
            AddConditionalIssue(currentAircraft.InstanceType == AircraftInstanceTypes.RealAircraft && basetime != totalMinutes && basetime + picus != totalMinutes, LintOptions.PICSICDualMath, Resources.FlightLint.warningPICSICDualBroken);
        }

        private readonly static HashSet<int> hsExcludedTimeProps = new HashSet<int>() {
            (int) CustomPropertyType.KnownProperties.IDPropGroundInstructionGiven,
            (int) CustomPropertyType.KnownProperties.IDPropGroundInstructionReceived,
            (int) CustomPropertyType.KnownProperties.IDPropPilotMonitoringTime,
            (int) CustomPropertyType.KnownProperties.IDPropPlannedBlock
        };

        private void CheckCoreTimeIssues(LogbookEntryBase le, int totalMinutes)
        {
            AddConditionalIssue(le.CrossCountry.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesXCGreaterThanTotal);
            AddConditionalIssue(le.Nighttime.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesNightGreaterThanTotal);
            AddConditionalIssue(le.SimulatedIFR.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesSimIFRGreaterThanTotal);
            AddConditionalIssue(le.IMC.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesIMCGreaterThanTotal);
            AddConditionalIssue(le.IMC.ToMinutes() + le.SimulatedIFR.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesSimPlusIMCGreaterThanTotal);
            AddConditionalIssue(le.Dual.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesDualGreaterThanTotal);
            AddConditionalIssue(le.CFI.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesCFIGreaterThanTotal);
            AddConditionalIssue(le.SIC.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesSICGreaterThanTotal);
            AddConditionalIssue(le.PIC.ToMinutes() > totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningTimesPICGreaterThanTotal);
            int picus = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropPICUS).ToMinutes();
            AddConditionalIssue(totalMinutes > 0 && le.PIC.ToMinutes() + le.SIC.ToMinutes() + le.CFI.ToMinutes() + le.Dual.ToMinutes() + picus == 0, LintOptions.TimeIssues, Resources.FlightLint.warningTotalTimeButNoOtherTime);
            AddConditionalIssue(le.NightLandings > 0 && le.Nighttime == 0.0M, LintOptions.TimeIssues, Resources.LogbookEntry.errNoNightFlight);
        }

        private void CheckTimeIssues(LogbookEntryBase le)
        {
            int totalMinutes = le.TotalFlightTime.ToMinutes();
            CheckCoreTimeIssues(le, totalMinutes);

            CustomFlightProperty cfpSolo = le.CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropSolo);
            int soloMinutes = cfpSolo?.DecValue.ToMinutes() ?? 0;
            if (soloMinutes > 0)
            {
                AddConditionalIssue(soloMinutes > le.PIC.ToMinutes(), LintOptions.TimeIssues, Resources.FlightLint.warningSoloTimeExceedsPICTime);
                AddConditionalIssue(soloMinutes > totalMinutes - le.SIC.ToMinutes() - le.CFI.ToMinutes() - le.Dual.ToMinutes(), LintOptions.TimeIssues, Resources.FlightLint.warningSoloTimeWithNonSoloTime);
                AddConditionalIssue(soloMinutes == totalMinutes &&
                    (le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropInstructorOnBoard) ||
                     le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropPassengerNames) ||
                     le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropInstructorName) ||
                     le.CustomProperties.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropPassengerCount) > 0), LintOptions.TimeIssues, Resources.FlightLint.warningSoloTimeWithNonSoloTime2);
            }

            foreach (CustomFlightProperty cfp in le.CustomProperties)
            {
                AddConditionalIssue(cfp.PropertyType.Type == CFPPropertyType.cfpDecimal && !cfp.PropertyType.IsBasicDecimal && !cfp.PropertyType.IsNoSum && !hsExcludedTimeProps.Contains(cfp.PropTypeID) &&
                    cfp.DecValue.ToMinutes() > totalMinutes,
                    LintOptions.TimeIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningPropertyGreaterThanTotal, cfp.PropertyType.Title));
            }

            decimal dayFlight = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropDayFlight);
            AddConditionalIssue(dayFlight > 0 && dayFlight.ToMinutes() != le.TotalFlightTime.ToMinutes() - le.Nighttime.ToMinutes(), LintOptions.TimeIssues, Resources.FlightLint.warningDayFlightInconsistent);

            AddConditionalIssue(le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropSinglePilot) && le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropMultiPilotTime),
                LintOptions.TimeIssues, Resources.FlightLint.warningBothSingleAndMultiPilotFound);

            CustomFlightProperty cfpTachStart = le.CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropTachStart);
            CustomFlightProperty cfpTachEnd = le.CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropTachEnd);
            AddConditionalIssue(cfpTachStart != null && cfpTachEnd != null && cfpTachEnd.DecValue < cfpTachStart.DecValue, LintOptions.TimeIssues, Resources.FlightLint.warningTachEndBeforeTachStart);

            int militaryTimeMinutes = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropMilitaryPrimaryTime).ToMinutes() + le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropMilitarySecondaryTime).ToMinutes();
            AddConditionalIssue(militaryTimeMinutes > 0 && militaryTimeMinutes != totalMinutes, LintOptions.TimeIssues, Resources.FlightLint.warningMilitaryTimeUnaccounted);

            if (le.TotalFlightTime > 0)
            {
                // Look for block time or engine time that varies significantly from total time UNLESS hobbs is present; if so, compare hobbs to that.  Ditto tach, if tach is of by more than 30%.
                const decimal maxHobbsVariation = 0.2M;
                const decimal maxBlockVariation = 0.1M;

                CustomFlightProperty cfpBlockOut = le.CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockOut);
                CustomFlightProperty cfpBlockIn = le.CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockIn);

                bool fHasHobbs = le.HobbsEnd > 0 && le.HobbsStart > 0 && le.HobbsEnd > le.HobbsStart;
                decimal hobbsVariation = Math.Abs(le.HobbsEnd - le.HobbsStart - le.TotalFlightTime);
                decimal blockTimeVariation = (cfpBlockIn != null && cfpBlockOut != null) ? Math.Abs((decimal)cfpBlockIn.DateValue.Subtract(cfpBlockOut.DateValue).TotalHours - le.TotalFlightTime) : 0;
                AddConditionalIssue(fHasHobbs && hobbsVariation > maxHobbsVariation, LintOptions.TimeIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningHobbsAndTotalsDiffer, hobbsVariation.ToHHMM()));
                AddConditionalIssue(!fHasHobbs && blockTimeVariation > maxBlockVariation, LintOptions.TimeIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningBlockAndTotalsDiffer, blockTimeVariation.ToHHMM()));
            }
        }

        private void CheckFlightLengthIssues(LogbookEntryBase le)
        {
            CustomFlightProperty cfpBlockOut = le.CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockOut);
            CustomFlightProperty cfpBlockIn = le.CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockIn);

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
        }

        private void CheckDateTimeIssues(LogbookEntryBase le)
        {
            CustomFlightProperty cfpBlockOut = le.CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockOut);
            CustomFlightProperty cfpBlockIn = le.CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockIn);

            // Block out after block in
            AddConditionalIssue(cfpBlockIn != null && cfpBlockOut != null && cfpBlockOut.DateValue.CompareTo(cfpBlockIn.DateValue) > 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningDateTimeInvalidBlock);

            bool fHasEngineStart = le.EngineStart.HasValue();
            bool fHasEngineEnd = le.EngineEnd.HasValue();
            bool fHasFlightStart = le.FlightStart.HasValue();
            bool fHasFlightEnd = le.FlightEnd.HasValue();

            // Engine start must be before flight.  Can be after block out, but not before block in
            AddConditionalIssue(fHasEngineStart && fHasFlightStart && le.FlightStart.CompareTo(le.EngineStart) < 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningDateEngineAfterFlight);
            AddConditionalIssue(fHasEngineStart && fHasFlightEnd && le.FlightEnd.CompareTo(le.EngineStart) < 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightEndBeforeEngineStart);

            AddConditionalIssue(fHasEngineStart && cfpBlockIn != null && cfpBlockIn.DateValue.CompareTo(le.EngineStart) <= 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningEngineStartAfterBlockIn);

            // Flight start must be after engine start (checked above) and after block out
            AddConditionalIssue(fHasFlightStart && cfpBlockOut != null && le.FlightStart.CompareTo(cfpBlockOut.DateValue) < 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningDateBlockAfterFlight);

            // Flight end must be after engine/flight start (checked in regular validation), after block out, and before block in
            AddConditionalIssue(fHasFlightEnd && cfpBlockOut != null && le.FlightEnd.CompareTo(cfpBlockOut.DateValue) < 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightEndBeforeBlockOut);
            AddConditionalIssue(fHasFlightEnd && cfpBlockIn != null && le.FlightEnd.CompareTo(cfpBlockIn.DateValue) > 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightEndAfterBlockIn);
            AddConditionalIssue(fHasFlightEnd && fHasEngineEnd && le.FlightEnd.CompareTo(le.EngineEnd) > 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightEndAfterEngineEnd);

            AddConditionalIssue(fHasEngineEnd && cfpBlockIn != null && le.EngineEnd.CompareTo(cfpBlockIn.DateValue) < 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningEngineEndBeforeBlockIn);

            CheckFlightLengthIssues(le);

            // Look for issues with sequential flights
            CheckSequentialFlightIssues(le);
        }

        private void UpdateDutyPeriods(CustomFlightProperty cfpDutyStart, CustomFlightProperty cfpFlightDutyStart, CustomFlightProperty cfpDutyEnd, CustomFlightProperty cfpFlightDutyEnd)
        {
            // Close off a duty period if we have a duty end; if we're starting (or restarting) a duty period (and not closing it in the same flight), reset the duty period
            if (cfpDutyEnd != null)
            {
                dutyStart = null;   // don't have an open duty period
                dutyEnd = cfpDutyEnd.DateValue;
            }
            else if (cfpDutyStart != null)
            {
                dutyStart = cfpDutyStart.DateValue;
                if (cfpDutyEnd == null)
                    dutyEnd = null; // we have a start of the duty period that isn't closed in the same flight - leave the duty end unspecified.
            }

            if (cfpFlightDutyEnd != null)
            {
                flightDutyStart = null;  // don't have an open flight duty period
                flightDutyEnd = cfpFlightDutyEnd.DateValue;
            }
            else if (cfpFlightDutyStart != null)
            {
                flightDutyStart = cfpFlightDutyStart.DateValue;
                if (cfpFlightDutyEnd == null)
                    flightDutyEnd = null;// we have a start of the duty period that isn't closed in the same flight - leave the duty end unspecified.
            }
        }

        private void CheckBlockIssues(CustomFlightProperty cfpBlockOut)
        {
            AddConditionalIssue(cfpBlockOut != null && previousFlight.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDBlockIn) && cfpBlockOut.DateValue.CompareTo(previousFlight.CustomProperties[CustomPropertyType.KnownProperties.IDBlockIn].DateValue) < 0,
                LintOptions.DateTimeIssues, Resources.FlightLint.warningPreviousFlightBlockEndAfterStart);
        }

        private void CheckDutyIssues(LogbookEntryBase le, CustomFlightProperty cfpBlockIn, CustomFlightProperty cfpBlockOut)
        {
            // Look for a new duty start when a prior period is still open or a duty end when a prior period is NOT open
            CustomFlightProperty cfpDutyStart = le.CustomProperties[CustomPropertyType.KnownProperties.IDPropDutyStart];
            CustomFlightProperty cfpFlightDutyStart = le.CustomProperties[CustomPropertyType.KnownProperties.IDPropFlightDutyTimeStart];
            CustomFlightProperty cfpDutyEnd = le.CustomProperties[CustomPropertyType.KnownProperties.IDPropDutyEnd];
            CustomFlightProperty cfpFlightDutyEnd = le.CustomProperties[CustomPropertyType.KnownProperties.IDPropFlightDutyTimeEnd];

            // Starting a new duty period or flight duty period while a prior duty period is open
            AddConditionalIssue(dutyStart != null && dutyStart.HasValue && cfpDutyStart != null, LintOptions.DateTimeIssues, Resources.FlightLint.warningNewDutyStart);
            AddConditionalIssue(flightDutyStart != null && flightDutyStart.HasValue && cfpFlightDutyStart != null, LintOptions.DateTimeIssues, Resources.FlightLint.warningNewFlightDutyStart);

            // Starting a new duty period or flight duty period prior to the close of the prior one.
            AddConditionalIssue(dutyEnd != null && dutyEnd.HasValue && cfpDutyStart != null && cfpDutyStart.DateValue.CompareTo(dutyEnd.Value) < 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningDutyStartPriorToPreviousDutyEnd);
            AddConditionalIssue(flightDutyEnd != null && flightDutyEnd.HasValue && cfpFlightDutyStart != null && cfpFlightDutyStart.DateValue.CompareTo(flightDutyEnd.Value) < 0, LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightDutyStartPriorToPreviousFlightDutyEnd);

            // Ending a duty period or flight duty period that has no corresponding start.
            AddConditionalIssue(dutyStart == null && cfpDutyStart == null && cfpDutyEnd != null, LintOptions.DateTimeIssues, Resources.FlightLint.warningNewDutyEndNoStart);
            AddConditionalIssue(flightDutyStart == null && cfpFlightDutyStart == null && cfpFlightDutyEnd != null, LintOptions.DateTimeIssues, Resources.FlightLint.warningNewFlightDutyEndNoStart);

            DateTime bogusDate = le.FlightStart.LaterDate(le.FlightEnd)
                .LaterDate(le.EngineStart).LaterDate(le.EngineEnd)
                .LaterDate(cfpBlockIn == null ? DateTime.MinValue : cfpBlockIn.DateValue).LaterDate(cfpBlockOut == null ? DateTime.MinValue : cfpBlockOut.DateValue)
                .LaterDate(cfpDutyStart == null ? DateTime.MinValue : cfpDutyStart.DateValue).LaterDate(cfpDutyEnd == null ? DateTime.MinValue : cfpDutyEnd.DateValue)
                .LaterDate(cfpFlightDutyStart == null ? DateTime.MinValue : cfpFlightDutyStart.DateValue).LaterDate(cfpFlightDutyEnd == null ? DateTime.MinValue : cfpFlightDutyEnd.DateValue);
            AddConditionalIssue(bogusDate.CompareTo(DateTime.UtcNow.AddDays(5)) > 0, LintOptions.DateTimeIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningTimesSuspectTime, bogusDate.UTCDateFormatString()));

            UpdateDutyPeriods(cfpDutyStart, cfpFlightDutyStart, cfpDutyEnd, cfpFlightDutyEnd);
        }

        private void CheckSequentialFlightIssues(LogbookEntryBase le)
        {
            if (previousFlight == null)
                return;

            AddConditionalIssue(previousFlight.EngineEnd.HasValue() && le.EngineStart.HasValue() && previousFlight.EngineEnd.CompareTo(le.EngineStart) > 0,
                LintOptions.DateTimeIssues, Resources.FlightLint.warningPreviousEngineEndsAfterStart);

            AddConditionalIssue(previousFlight.FlightEnd.HasValue() && le.FlightStart.HasValue() && previousFlight.FlightEnd.CompareTo(le.FlightStart) > 0,
                LintOptions.DateTimeIssues, Resources.FlightLint.warningPreviousFlightEndsAfterStart);

            CustomFlightProperty cfpBlockIn = le.CustomProperties[CustomPropertyType.KnownProperties.IDBlockIn];
            CustomFlightProperty cfpBlockOut = le.CustomProperties[CustomPropertyType.KnownProperties.IDBlockOut];
            CheckBlockIssues(cfpBlockOut);

            CheckDutyIssues(le, cfpBlockIn, cfpBlockOut);
        }

        // Issue #1444 - check for water operations in a land aircraft
        // Here's a quick way to check for water ops: Water landings, Water Takeoffs, Water Taxi, Water Step Taxi, Water Docking, Water Docking (CrossWind)
        private static readonly HashSet<int> _waterOpsProps = new HashSet<int>() { (int) CustomPropertyType.KnownProperties.IDPropWaterLandings, (int)CustomPropertyType.KnownProperties.IDPropWaterTakeoffs, 423, 424, 425, 426 };

        private void CheckMiscIssues(LogbookEntryBase le)
        {
            bool fHasWaterOps = false;

            foreach (CustomFlightProperty cfp in le.CustomProperties)
            {
                if (cfp.PropertyType.IsExcludedFromMRU)
                {
                    AddConditionalIssue(seenCheckrides.Contains(cfp.PropTypeID), LintOptions.MiscIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningMiscMultipleRedundantCheckrides, cfp.PropertyType.Title));
                    seenCheckrides.Add(cfp.PropTypeID);
                }
                fHasWaterOps = fHasWaterOps || _waterOpsProps.Contains(cfp.PropTypeID);
            }

            // Issue #1444: check for water operations in a land aircraft.
            AddConditionalIssue(fHasWaterOps && !CategoryClass.IsSeaClass(currentCatClassID) && CategoryClass.IsAirplane(currentCatClassID), LintOptions.MiscIssues, Resources.FlightLint.warningWaterOperationsInLandPlane);

            AddConditionalIssue(le.Landings + le.Approaches > 0 && le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropPilotMonitoring), LintOptions.MiscIssues, Resources.FlightLint.warningOperationsLoggedWhileMonitoring);
            // Issue #1227 - check for too many described landings
            AddConditionalIssue(le.FullStopLandings + le.NightLandings + le.CustomProperties.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropNightTouchAndGo) > le.Landings, LintOptions.MiscIssues, Resources.FlightLint.warningTooManyDescribedLandings);

            int maxDescribedLandings = 0;
            le.CustomProperties.ForEachEvent((cfp) => { if (cfp.PropertyType.IsLanding) maxDescribedLandings = Math.Max(maxDescribedLandings, cfp.IntValue); });
            AddConditionalIssue(maxDescribedLandings > le.Landings, LintOptions.MiscIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningMoreDescribedLandingsThanTotal, le.Landings));

            AddConditionalIssue(le.Dual > 0 && !le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropInstructorName) && le.CFISignatureState == LogbookEntryBase.SignatureState.None, LintOptions.MiscIssues, Resources.FlightLint.warningDualLoggedButNoCFIName);

            LogbookEntryBase leDefault = new LogbookEntry() { Date = le.Date, AircraftID = le.AircraftID, User = le.User };
            AddConditionalIssue(le.IsEqualTo(leDefault), LintOptions.MiscIssues, Resources.FlightLint.warningFlightHasNoData);

            AddConditionalIssue(previousFlight != null && le.IsEqualTo(previousFlight), LintOptions.MiscIssues, Resources.FlightLint.warningMiscDuplicateFlight);
        }
    }
}