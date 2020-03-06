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

        protected HashSet<int> seenCheckrides { get; set; }
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

        /// <summary>
        /// Checks flights for various potential issues
        /// </summary>
        /// <param name="rgle">An enumerable of flights</param>
        /// <param name="options">A bitmask of LintOptions flags</param>
        /// <returns></returns>
        public IEnumerable<FlightWithIssues> CheckFlights(IEnumerable<LogbookEntryBase> rgle, string szUser, UInt32 options)
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

            foreach (LogbookEntryBase le in rgle)
            {
                List<FlightIssue> lstIssues = new List<FlightIssue>();

                currentAircraft = userAircraft.GetUserAircraftByID(le.AircraftID);

                if (currentAircraft == null)
                {
                    lstIssues.Add(new FlightIssue(LintOptions.MiscIssues, Resources.FlightLint.warningSIMAircraftNotFound));
                    continue;
                }

                currentModel = MakeModel.GetModel(currentAircraft.ModelID);

                currentCatClassID = (le.CatClassOverride == 0 ? currentModel.CategoryClassID : (CategoryClass.CatClassID)le.CatClassOverride);

                if (alMaster != null)
                    alSubset = alMaster.CloneSubset(le.Route);

                if ((options & (UInt32)LintOptions.SimIssues) != 0)
                    CheckSimIssues(le, lstIssues);

                if ((options & (UInt32)LintOptions.IFRIssues) != 0)
                    CheckIFRIssues(le, lstIssues);

                if ((options & (UInt32)LintOptions.AirportIssues) != 0)
                    CheckAirportIssues(le, lstIssues);

                if ((options & (UInt32)LintOptions.XCIssues) != 0)
                    CheckXCIssues(le, lstIssues);

                if ((options & (UInt32)LintOptions.PICSICDualMath) != 0)
                    CheckPICSICDualIssues(le, lstIssues);

                if ((options & (UInt32)LintOptions.TimeIssues) != 0)
                    CheckTimeIssues(le, lstIssues);

                if ((options & (UInt32)LintOptions.DateTimeIssues) != 0)
                    CheckDateTimeIssues(le, lstIssues);

                if ((options & (UInt32)LintOptions.MiscIssues) != 0)
                    CheckMiscIssues(le, lstIssues);

                if (lstIssues.Count > 0)
                    lstFlights.Add(new FlightWithIssues(le, lstIssues));
            }

            return lstFlights;
        }

        private void CheckSimIssues(LogbookEntryBase le, List<FlightIssue> lst)
        {
            if (currentAircraft.InstanceType == AircraftInstanceTypes.RealAircraft)
            {
                if (le.GroundSim > 0)
                    lst.Add(new FlightIssue(LintOptions.SimIssues, Resources.FlightLint.warningSIMGroundSimInRealAircraft));
            }
            else
            {
                if (le.PIC > 0)
                    lst.Add(new FlightIssue(LintOptions.SimIssues, Resources.FlightLint.warningSIMPICInSim));
                if (le.SIC > 0)
                    lst.Add(new FlightIssue(LintOptions.SimIssues, Resources.FlightLint.warningSIMSICInSim));
                if (le.TotalFlightTime > 0)
                    lst.Add(new FlightIssue(LintOptions.SimIssues, Resources.FlightLint.warningSIMTotalInSim));
                if (le.IMC > 0)
                    lst.Add(new FlightIssue(LintOptions.SimIssues, Resources.FlightLint.warningSIMActualIMC));
                if (le.CrossCountry > 0)
                    lst.Add(new FlightIssue(LintOptions.SimIssues, Resources.FlightLint.warningSIMCrossCountryInSim));
                if (!le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropSimRegistration))
                    lst.Add(new FlightIssue(LintOptions.SimIssues, Resources.FlightLint.warningSIMNoDeviceIdentifier));
            }
        }

        private void CheckIFRIssues(LogbookEntryBase le, List<FlightIssue> lst)
        {
            if (le.Approaches > 0 && !le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropApproachName) && ApproachDescription.ExtractApproaches(le.Comment).Count() == 0)
                lst.Add(new FlightIssue(LintOptions.IFRIssues, Resources.FlightLint.warningIFRNoApproachDescription));
            if (le.SimulatedIFR > 0 && le.Dual == 0 && currentAircraft.InstanceType == AircraftInstanceTypes.RealAircraft && !le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropSafetyPilotName))
                lst.Add(new FlightIssue(LintOptions.IFRIssues, Resources.FlightLint.warningIFRNoSafetyPilot));
            if ((le.Approaches > 0 || le.fHoldingProcedures) && le.SimulatedIFR + le.IMC == 0)
                lst.Add(new FlightIssue(LintOptions.IFRIssues, Resources.FlightLint.warningIFRApproachesButNoIFR));
        }
        private void CheckAirportIssues(LogbookEntryBase le, List<FlightIssue> lst)
        {
            IEnumerable<string> rgCodes = airport.SplitCodes(le.Route);

            foreach (string szCode in rgCodes)
            {
                if (szCode.StartsWith(airport.ForceNavaidPrefix, StringComparison.CurrentCultureIgnoreCase))
                    continue;

                if (szCode.CompareCurrentCultureIgnoreCase("LOCAL") == 0 || szCode.CompareCurrentCultureIgnoreCase("LCL") == 0)
                {
                    lst.Add(new FlightIssue(LintOptions.AirportIssues, Resources.FlightLint.warningAirportLocal));
                    continue;
                }

                airport ap = alSubset.GetAirportByCode(szCode);

                if (ap == null)
                    lst.Add(new FlightIssue(LintOptions.AirportIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningAirportNotFound, szCode)));
                else
                {
                    if ((currentCatClassID == CategoryClass.CatClassID.AMEL || currentCatClassID == CategoryClass.CatClassID.ASEL) && ap.IsSeaport)
                        lst.Add(new FlightIssue(LintOptions.AirportIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningAirportLandPlaneAtSeaport, szCode)));
                    if (CategoryClass.IsAirplane(currentCatClassID) && ap.FacilityTypeCode == "H")
                        lst.Add(new FlightIssue(LintOptions.AirportIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningAirportAirplaneAtHeliport, szCode)));
                }
            }

            // Sanity check for speed
            if (le.TotalFlightTime > 0)
            {
                double dist = alSubset.DistanceForRoute();
                double speed = dist / (double) le.TotalFlightTime;

                if (speed > (currentModel.EngineType == MakeModel.TurbineLevel.Piston ? 500 : 1000))
                    lst.Add(new FlightIssue(LintOptions.AirportIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningAirportUnlikelyImpliedSpeed, speed)));
            }
        }

        private void CheckXCIssues(LogbookEntryBase le, List<FlightIssue> lst)
        {
            if (currentAircraft.InstanceType != AircraftInstanceTypes.RealAircraft)
                return;

            if (le.CrossCountry > 0 && le.CrossCountry < le.TotalFlightTime)
                lst.Add(new FlightIssue(LintOptions.XCIssues, Resources.FlightLint.warningXCNotWholeFlightXC));

            double distance = alSubset.MaxDistanceFromStartingAirport();

            if (le.CrossCountry == 0)
            {
                int minDistanceXC = (currentCatClassID == CategoryClass.CatClassID.Helicopter ? 25 : 50);
                if (distance > minDistanceXC)
                    lst.Add(new FlightIssue(LintOptions.XCIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningXCMissingXC, minDistanceXC)));
            }

            if (le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropXCLessThan25nm) && distance > 25)
                lst.Add(new FlightIssue(LintOptions.XCIssues, Resources.FlightLint.warningXCDistanceLessThan25ButFlewMore));
            else if (le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropXCLessThan50nm) && distance > 50)
                lst.Add(new FlightIssue(LintOptions.XCIssues, Resources.FlightLint.warningXCDistanceLessThan50ButFlewMore));
            else if (le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropXCMoreThan50nm) && distance < 50)
                lst.Add(new FlightIssue(LintOptions.XCIssues, Resources.FlightLint.warningXCDistanceMoreThan50ButFlewLess));
        }

        private void CheckPICSICDualIssues(LogbookEntryBase le, List<FlightIssue> lst)
        {
            if (le.PIC + le.SIC + le.Dual - le.TotalFlightTime != 0)
                lst.Add(new FlightIssue(LintOptions.PICSICDualMath, Resources.FlightLint.warningPICSICDualBroken));
        }

        private void CheckTimeIssues(LogbookEntryBase le, List<FlightIssue> lst)
        {
            if (currentAircraft.InstanceType != AircraftInstanceTypes.RealAircraft)
                return;

            if (le.CrossCountry > le.TotalFlightTime)
                lst.Add(new FlightIssue(LintOptions.TimeIssues, Resources.FlightLint.warningTimesXCGreaterThanTotal));
            if (le.Nighttime > le.TotalFlightTime)
                lst.Add(new FlightIssue(LintOptions.TimeIssues, Resources.FlightLint.warningTimesNightGreaterThanTotal));
            if (le.SimulatedIFR > le.TotalFlightTime)
                lst.Add(new FlightIssue(LintOptions.TimeIssues, Resources.FlightLint.warningTimesSimIFRGreaterThanTotal));
            if (le.IMC > le.TotalFlightTime)
                lst.Add(new FlightIssue(LintOptions.TimeIssues, Resources.FlightLint.warningTimesIMCGreaterThanTotal));
            if (le.IMC + le.SimulatedIFR > le.TotalFlightTime)
                lst.Add(new FlightIssue(LintOptions.TimeIssues, Resources.FlightLint.warningTimesSimPlusIMCGreaterThanTotal));
            if (le.Dual > le.TotalFlightTime)
                lst.Add(new FlightIssue(LintOptions.TimeIssues, Resources.FlightLint.warningTimesDualGreaterThanTotal));
            if (le.CFI > le.TotalFlightTime)
                lst.Add(new FlightIssue(LintOptions.TimeIssues, Resources.FlightLint.warningTimesCFIGreaterThanTotal));
            if (le.SIC > le.TotalFlightTime)
                lst.Add(new FlightIssue(LintOptions.TimeIssues, Resources.FlightLint.warningTimesSICGreaterThanTotal));
            if (le.PIC > le.TotalFlightTime)
                lst.Add(new FlightIssue(LintOptions.TimeIssues, Resources.FlightLint.warningTimesPICGreaterThanTotal));

            foreach (CustomFlightProperty cfp in le.CustomProperties)
            {
                if (cfp.PropertyType.Type == CFPPropertyType.cfpDecimal && !cfp.PropertyType.IsBasicDecimal &&
                    cfp.PropTypeID != (int)CustomPropertyType.KnownProperties.IDPropGroundInstructionGiven &&
                    cfp.PropTypeID != (int)CustomPropertyType.KnownProperties.IDPropGroundInstructionReceived &&
                    cfp.DecValue > le.TotalFlightTime)
                    lst.Add(new FlightIssue(LintOptions.TimeIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningPropertyGreaterThanTotal, cfp.PropertyType.Title)));
            }
        }

        private void CheckDateTimeIssues(LogbookEntryBase le, List<FlightIssue> lst)
        {
            CustomFlightProperty cfpBlockOut = le.CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockOut);
            CustomFlightProperty cfpBlockIn = le.CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockIn);

            // Block out after block in
            if (cfpBlockIn != null && cfpBlockOut != null && cfpBlockOut.DateValue.CompareTo(cfpBlockIn.DateValue) > 0)
                lst.Add(new FlightIssue(LintOptions.DateTimeIssues, Resources.FlightLint.warningDateTimeInvalidBlock));

            if (le.EngineStart.HasValue())
            {
                // Engine start must be before flight.  Can be after block out, but not before block in
                if (le.FlightStart.HasValue() && le.FlightStart.CompareTo(le.EngineStart) < 0)
                    lst.Add(new FlightIssue(LintOptions.DateTimeIssues, Resources.FlightLint.warningDateEngineAfterFlight));
                if (le.FlightEnd.HasValue() && le.FlightEnd.CompareTo(le.EngineStart) < 0)
                    lst.Add(new FlightIssue(LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightEndBeforeEngineStart));

                if (cfpBlockIn != null && cfpBlockIn.DateValue.CompareTo(le.EngineStart) <= 0)
                    lst.Add(new FlightIssue(LintOptions.DateTimeIssues, Resources.FlightLint.warningEngineStartAfterBlockIn));
            }

            // Flight start must be after engine start (checked above) and after block out
            if (le.FlightStart.HasValue() && cfpBlockOut != null && le.FlightStart.CompareTo(cfpBlockOut.DateValue) < 0)
                lst.Add(new FlightIssue(LintOptions.DateTimeIssues, Resources.FlightLint.warningDateBlockAfterFlight));

            // Flight end must be after engine/flight start (checked in regular validation), after block out, and before block in
            if (le.FlightEnd.HasValue())
            {
                if (cfpBlockOut != null && le.FlightEnd.CompareTo(cfpBlockOut) < 0)
                    lst.Add(new FlightIssue(LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightEndBeforeBlockOut));
                if (cfpBlockIn != null && le.FlightEnd.CompareTo(cfpBlockIn.DateValue) > 0)
                    lst.Add(new FlightIssue(LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightEndAfterBlockIn));
                if (le.EngineEnd.HasValue() && le.FlightEnd.CompareTo(le.EngineEnd) > 0)
                    lst.Add(new FlightIssue(LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightEndAfterEngineEnd));
            }

            if (le.EngineEnd.HasValue())
            {
                if (cfpBlockIn != null && le.EngineEnd.CompareTo(cfpBlockIn.DateValue) < 0)
                    lst.Add(new FlightIssue(LintOptions.DateTimeIssues, Resources.FlightLint.warningEngineEndBeforeBlockIn));
            }

            const int MaxHoursDifference = 48;
            // Check that engine, flight, and block times are all roughly equal to date-of-flight
            if ((le.EngineStart.HasValue() && Math.Abs(le.EngineStart.Subtract(le.Date).TotalHours) > MaxHoursDifference) ||
                (le.EngineEnd.HasValue() && Math.Abs(le.EngineEnd.Subtract(le.Date).TotalHours) > MaxHoursDifference))
                lst.Add(new FlightIssue(LintOptions.DateTimeIssues, Resources.FlightLint.warningEngineTimeDiffersDate));

            if ((le.FlightStart.HasValue() && Math.Abs(le.FlightStart.Subtract(le.Date).TotalHours) > MaxHoursDifference) ||
                (le.FlightEnd.HasValue() && Math.Abs(le.FlightEnd.Subtract(le.Date).TotalHours) > MaxHoursDifference))
                lst.Add(new FlightIssue(LintOptions.DateTimeIssues, Resources.FlightLint.warningFlightTimeDiffersDate));

            if ((cfpBlockOut != null && Math.Abs(cfpBlockOut.DateValue.Subtract(le.Date).TotalHours) > MaxHoursDifference) ||
                (cfpBlockIn != null && Math.Abs(cfpBlockIn.DateValue.Subtract(le.Date).TotalHours) > MaxHoursDifference))
                lst.Add(new FlightIssue(LintOptions.DateTimeIssues, Resources.FlightLint.warningBlockTimeDiffersDate));
        }

        private void CheckMiscIssues(LogbookEntryBase le, List<FlightIssue> lst)
        {
            foreach (CustomFlightProperty cfp in le.CustomProperties)
            {
                if (cfp.PropertyType.IsExcludedFromMRU)
                {
                    if (seenCheckrides.Contains(cfp.PropTypeID))
                        lst.Add(new FlightIssue(LintOptions.MiscIssues, String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.warningMiscMultipleRedundantCheckrides, cfp.PropertyType.Title)));
                    seenCheckrides.Add(cfp.PropTypeID);
                }
            }
        }
    }
}