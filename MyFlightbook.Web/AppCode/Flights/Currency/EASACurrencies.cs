using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    #region EASA PPL
    /// <summary>
    /// Passenger currency (FCL.060) in EASA is basically the same as US, but for Balloons.  So this is just a static class to get the right passenger currency (if any).
    /// </summary>
    public static class EASAPPLPassengerCurrency
    {
        public static FlightCurrency CurrencyForCatClass(CategoryClass.CatClassID ccid, string szName, bool fRequireDayLandings)
        {
            switch (ccid)
            {
                case CategoryClass.CatClassID.GasBalloon:
                case CategoryClass.CatClassID.HotAirBalloon:
                // TODO: implement EASA balloon FCL.060
                default:
                    return new PassengerCurrency(szName, fRequireDayLandings);
                case CategoryClass.CatClassID.Glider:
                    return new EASASPLCurrency(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, szName, Resources.Currency.EASASailplane).Trim());
                case CategoryClass.CatClassID.Airship:
                case CategoryClass.CatClassID.AMEL:
                case CategoryClass.CatClassID.AMES:
                case CategoryClass.CatClassID.ASEL:
                case CategoryClass.CatClassID.ASES:
                case CategoryClass.CatClassID.Helicopter:
                case CategoryClass.CatClassID.PoweredLift:
                    return new PassengerCurrency(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, szName, Resources.Currency.PassengersEASA).Trim(), fRequireDayLandings);
            }
        }

        public static FlightCurrency InstructorCurrencyForCatClass(CategoryClass.CatClassID ccid)
        {
            return (ccid == CategoryClass.CatClassID.Glider) ? new EASASPLInstructorCurrency(Resources.Currency.EASASailplaneFI) : null;
        }
    }

    /// <summary>
    /// FCL.060 for night flight currency
    /// Night currency is different from the US: only one landing, landings don't have to be to a full-stop, and if you are instrument current, you're good to go.
    /// </summary>
    public class EASAPPLNightPassengerCurrency : FlightCurrency
    {
        protected FlightCurrency fcNightLanding { get; set; } = new FlightCurrency(1, 90, false, string.Empty);
        protected FlightCurrency fcNightTakeoff { get; set; } = new FlightCurrency(1, 90, false, string.Empty);

        public EASAPPLNightPassengerCurrency(string szName) : base()
        {
            DisplayName = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, szName, Resources.Currency.PassengersEASANight);
        }

        /// <summary>
        /// FCL.060(b)(2) says that you can be current either from experience, or simply if you "hold" an IR; it doesn't say your IR has to be current.
        /// Net currency - either current from experience, or if you have an instrument rating then we pretenc you've never been current (i.e., don't show anything), since currency is unnecessary.
        /// </summary>
        protected FlightCurrency NetCurrency
        {
            get { return fcNightLanding.AND(fcNightTakeoff); }
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            // Must be real aircraft or FFS
            if (!cfr.fIsCertifiedLanding)
                return;

            int cNightTakeoffs = cfr.FlightProps.TotalCountForPredicate(cfp => cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropNightTakeoff);
            int cNightLandings = cfr.cFullStopNightLandings + cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropNightTouchAndGo);

            if (cNightTakeoffs > 0)
                fcNightTakeoff.AddRecentFlightEvents(cfr.dtFlight, cNightTakeoffs);
            if (cNightLandings > 0)
                fcNightLanding.AddRecentFlightEvents(cfr.dtFlight, cNightLandings);
        }

        public override string DiscrepancyString
        {
            get
            {
                if (!fcNightLanding.IsCurrent())
                    return String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, fcNightLanding.Discrepancy, (fcNightLanding.Discrepancy > 1) ? Resources.Totals.Landings : Resources.Totals.Landing);
                else if (!fcNightTakeoff.IsCurrent())
                    return String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplateNight, fcNightTakeoff.Discrepancy, (fcNightTakeoff.Discrepancy > 1) ? Resources.Currency.Takeoffs : Resources.Currency.Takeoff);
                return string.Empty;
            }
        }

        public override string StatusDisplay
        {
            get { return NetCurrency.StatusDisplay; }
        }

        public override DateTime ExpirationDate
        {
            get { return NetCurrency.ExpirationDate; }
        }

        public override CurrencyState CurrentState
        {
            get { return NetCurrency.CurrentState; }
        }

        public override bool HasBeenCurrent
        {
            get { return NetCurrency.HasBeenCurrent; }
        }
    }
    #endregion  // EASA PPL Currencies

    #region LAPL Currency
    /// <summary>
    /// Abstract base class for LAPL currencies.  See https://www.caa.co.uk/General-aviation/Pilot-licences/EASA-requirements/LAPL/LAPL-(A)-requirements/ for details 
    /// </summary>
    public abstract class LAPLBase : CurrencyExaminer
    {
        protected FlightCurrency fcLandings { get; set; }
        protected FlightCurrency fcPIC { get; set; }
        protected FlightCurrency fcDual { get; set; }
        protected FlightCurrency fcProficiencyCheck { get; set; }
        protected FlightCurrency fcAlternatePIC { get; set; }
        protected FlightCurrency fcAlternateLandings { get; set; }
        protected FlightCurrency fcResult { get; set; }
        protected string DiscrepancyOverride { get; set; }

        protected LAPLBase(int cLandings, decimal picTime, int period, string szName) : base()
        {
            fcLandings = new FlightCurrency(cLandings, period, true, "Landings (takeoffs implied)");
            fcAlternateLandings = new FlightCurrency(cLandings, period, true, "Alternate Landings (takeoffs implied)");
            fcPIC = new FlightCurrency(picTime, period, true, "PIC time");
            fcAlternatePIC = new FlightCurrency(picTime, period, true, "Alternate PIC time");
            fcDual = new FlightCurrency(1.0M, period, true, "Dual");
            fcProficiencyCheck = new FlightCurrency(1, period, true, "Proficiency check");
            DisplayName = szName;
            DiscrepancyOverride = null;
        }

        /// <summary>
        /// Returns the LAPL license that corresponds to the category/class in the row; can be used as a key in computing currency
        /// </summary>
        /// <param name="cfr">The flight row</param>
        /// <returns>A string suitable for use as a dictionary key, else empty</returns>
        public static string KeyForLAPL(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            if (cfr.fIsRealAircraft)
            {
                if (cfr.fMotorGlider || CategoryClass.IsAirplane(cfr.idCatClassOverride))
                    return Resources.Currency.LAPLA;
                if (cfr.idCatClassOverride == CategoryClass.CatClassID.Helicopter)
                    return Resources.Currency.LAPLH;
            }
            return string.Empty;
        }

        /// <summary>
        /// Instantiates an LAPL Currency appropriate for the flight row.  Can be null if we don't support LAPL currency for this aircraft.
        /// Useful to call KeyForLAPL first since it is a lightweight way (no object instantiation) to check this.
        /// </summary>
        /// <param name="cfr">The flight row</param>
        /// <returns>Null if there is no appropriate currency object.</returns>
        public static LAPLBase LAPLACurrencyForCategoryClass(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            if (!cfr.fIsRealAircraft)
                return null;
            if (cfr.fMotorGlider || CategoryClass.IsAirplane(cfr.idCatClassOverride))
                return new LAPLACurrency();
            else if (cfr.idCatClassOverride == CategoryClass.CatClassID.Helicopter)
                return new LAPLHCurrency();
            else
                return null;
        }

        /// <summary>
        /// Checks to see if this flight qualifies to be examined 
        /// </summary>
        /// <param name="cfr">The flight</param>
        /// <returns>True if it qualifies (e.g., matching category/class or allowed simulator</returns>
        protected abstract bool IsQualifyingFlight(ExaminerFlightRow cfr);

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            // Normal currency
            fcPIC.AddRecentFlightEvents(cfr.dtFlight, cfr.PIC);
            if (cfr.PIC > 0)
                fcLandings.AddRecentFlightEvents(cfr.dtFlight, cfr.cLandingsThisFlight);

            fcDual.AddRecentFlightEvents(cfr.dtFlight, cfr.Dual);

            // If we aren't current above, we still want to count any landings or time spent as dual, solo, or DPIC with an Instructor on board.
            bool fHasProficiencyCheck = false;
            decimal dPIC = 0.0M;
            bool fInstructorOnBoard = false;
            cfr.FlightProps.ForEachEvent((cfp) =>
            {
                if (cfp.PropertyType.IsPICProficiencyCheck6158)
                    fHasProficiencyCheck = true;
                if (cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropInstructorOnBoard && !cfp.IsDefaultValue)
                    fInstructorOnBoard = true;    // instructor-on-board time only counts if you are acting as PIC
                if (cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropDutiesOfPIC && !cfp.IsDefaultValue)
                    dPIC += cfp.DecValue;
            });

            if (fHasProficiencyCheck)
                fcProficiencyCheck.AddRecentFlightEvents(cfr.dtFlight, 1);

            if (!fInstructorOnBoard)
                dPIC = 0.0M;        // only counts if instructor on board.

            if (cfr.Dual + dPIC > 0)
            {
                fcAlternateLandings.AddRecentFlightEvents(cfr.dtFlight, cfr.cLandingsThisFlight);
                fcAlternatePIC.AddRecentFlightEvents(cfr.dtFlight, Math.Min(cfr.Total, cfr.Dual + dPIC));
            }
        }

        public override void Finalize(decimal totalTime, decimal picTime)
        {
            FlightCurrency fcNormal = fcLandings.AND(fcPIC).AND(fcDual);
            FlightCurrency fcAlternate = fcAlternateLandings.AND(fcAlternatePIC);
            FlightCurrency fcIPCAlternate = fcAlternate.OR(fcProficiencyCheck);

            fcResult = fcNormal.OR(fcIPCAlternate);

            // If you're NOT current, then you MUST follow the alternate OR a proficiency check.  So don't give a "short by", just say that you need that.
            if (!fcResult.IsCurrent())
                DiscrepancyOverride = Resources.Currency.LAPLProficiencyCheckRequired;
        }

        public override CurrencyState CurrentState { get { return fcResult.CurrentState; } }

        public override string DiscrepancyString { get { return DiscrepancyOverride ?? fcResult.DiscrepancyString; } }

        public override DateTime ExpirationDate { get { return fcResult.ExpirationDate; } }

        public override bool HasBeenCurrent { get { return fcResult.HasBeenCurrent; } }

        public override string StatusDisplay { get { return fcResult.StatusDisplay; } }
    }

    /// <summary>
    /// LAPL Airplane (see https://www.caa.co.uk/General-aviation/Pilot-licences/EASA-requirements/LAPL/LAPL-(A)-requirements/) - 12 landings and 12 hours PIC in 24 months and an hour of instruction
    /// </summary>
    public class LAPLACurrency : LAPLBase
    {
        public LAPLACurrency() : base(12, 12, 24, Resources.Currency.LAPLA) {  }

        protected override bool IsQualifyingFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            return (cfr.fIsRealAircraft && (cfr.fMotorGlider || CategoryClass.IsAirplane(cfr.idCatClassOverride)));
        }
    }

    /// <summary>
    /// LAPL Hellicopter (see https://www.caa.co.uk/General-aviation/Pilot-licences/EASA-requirements/LAPL/LAPL-(H)-requirements/) - 6 landings and 6 hours PIC in 12 months and an hour of instruction
    /// </summary>
    public class LAPLHCurrency : LAPLBase
    {
        public LAPLHCurrency() : base(6, 6, 12, Resources.Currency.LAPLH) { }

        protected override bool IsQualifyingFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            return (cfr.fIsRealAircraft && cfr.idCatClassOverride == CategoryClass.CatClassID.Helicopter);
        }
    }
    #endregion


    #region Sailplane and TMG currencies
    /// <summary>
    /// Sailplane (SPL) currency - see https://www.easa.europa.eu/sites/default/files/dfu/Sailplane%20Rule%20Book.pdf, SFCL.160(a)
    /// </summary>
    public class EASASPLCurrency : CompositeFlightCurrency
    {
        protected FlightCurrency fcHours { get; set; } = new FlightCurrency(5, 2 * 365, false, string.Empty);
        protected FlightCurrency fcLaunches { get; set; } = new FlightCurrency(15, 2 * 365, false, string.Empty);
        protected FlightCurrency fcTrainingFlights { get; set; } = new FlightCurrency(2, 2 * 365, false, string.Empty);

        protected FlightCurrency fcProficiency { get; set; } = new FlightCurrency(1, 2 * 365, false, string.Empty);

        /// <summary>
        /// TMG currency as well, as a subset of glider (sailplane) currency
        /// </summary>
        public EASATMGCurrency TMGCurrency { get; set; } = new EASATMGCurrency(Resources.Currency.EASASailplaneTMG);

        public EASASPLCurrency(string szName) : base(szName)
        {
            Query = new FlightQuery() { DateRange = FlightQuery.DateRanges.Custom, DateMin = DateTime.Now.AddYears(-2) };
            Query.AddCatClass(CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.Glider));
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            TMGCurrency.ExamineFlight(cfr);

            // remainder of This currency ONLY looks at glider flights, and excludes TMG
            if (cfr.idCatClassOverride != CategoryClass.CatClassID.Glider || cfr.fMotorGlider || !cfr.fIsRealAircraft)
                return;

            fcHours.AddRecentFlightEvents(cfr.dtFlight, Math.Max(cfr.PIC, cfr.Dual));

            // Each flight is at least one launch, if none are otherwise specified
            int cLaunches = Math.Max(1, cfr.FlightProps.TotalCountForPredicate(cfp => cfp.PropertyType.IsGliderGroundLaunch));
            fcLaunches.AddRecentFlightEvents(cfr.dtFlight, cLaunches);

            if (cfr.Dual > 0)
                fcTrainingFlights.AddRecentFlightEvents(cfr.dtFlight, 1);

            bool fIsBFR = false;
            cfr.FlightProps.ForEachEvent((cfp) => { fIsBFR = fIsBFR || cfp.PropertyType.IsBFR; });
            if (fIsBFR)
                fcProficiency.AddRecentFlightEvents(cfr.dtFlight, 1);
        }

        /// <summary>
        /// Computes the overall currency
        /// </summary>
        protected override void ComputeComposite()
        {
            // Only bother doing any real computation if we've ever been fully current on things.
            if (fcHours.HasBeenCurrent && fcLaunches.HasBeenCurrent && fcProficiency.HasBeenCurrent && fcTrainingFlights.HasBeenCurrent)
            {
                FlightCurrency fcAggregate = fcHours.AND(fcLaunches).AND(fcProficiency).AND(fcTrainingFlights);
                CompositeCurrencyState = fcAggregate.CurrentState;
                CompositeExpiration = fcAggregate.ExpirationDate;

                // Set a discrepancy string in order that we find one
                CompositeDiscrepancy = !fcHours.IsCurrent()
                    ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, fcHours.Discrepancy, Resources.Currency.Hours)
                    : !fcLaunches.IsCurrent()
                    ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, fcLaunches.Discrepancy, Resources.Currency.Launches)
                    : !fcTrainingFlights.IsCurrent()
                    ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, fcTrainingFlights.Discrepancy, Resources.Currency.TrainingFlights)
                    : !fcProficiency.IsCurrent() 
                    ? Resources.Currency.ProficiencyCheckRequired
                    : string.Empty;
            }
        }
    }

    /// <summary>
    /// TMG (SPL/TMG) currency - see https://www.easa.europa.eu/sites/default/files/dfu/Sailplane%20Rule%20Book.pdf, SFCL.160(b)
    /// </summary>
    public class EASATMGCurrency : CompositeFlightCurrency
    {
        protected FlightCurrency fcHoursSPL { get; set; } = new FlightCurrency(12, 2 * 365, false, string.Empty);

        protected FlightCurrency fcHoursTMG { get; set; } = new FlightCurrency(6, 2 * 365, false, string.Empty);
        protected FlightCurrency fcLandings { get; set; } = new FlightCurrency(12, 2 * 365, false, string.Empty);
        protected FlightCurrency fcTrainingFlights { get; set; } = new FlightCurrency(1, 2 * 365, false, string.Empty);

        protected FlightCurrency fcProficiency { get; set; } = new FlightCurrency(1, 2 * 365, false, string.Empty);

        public EASATMGCurrency(string szName) : base(szName)
        {
            Query = new FlightQuery() { DateRange = FlightQuery.DateRanges.Custom, DateMin = DateTime.Now.AddYears(-2), IsMotorglider = true };
            Query.AddCatClass(CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.Glider));
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            // This currency ONLY looks at glider flights, and excludes TMG
            if (cfr.idCatClassOverride != CategoryClass.CatClassID.Glider || !cfr.fIsRealAircraft)
                return;

            fcHoursSPL.AddRecentFlightEvents(cfr.dtFlight, Math.Max(cfr.PIC, cfr.Dual));

            if (!cfr.fMotorGlider)
                return;

            fcHoursTMG.AddRecentFlightEvents(cfr.dtFlight, Math.Max(cfr.PIC, cfr.Dual));

            // Count landings - assume one takeoff per landing
            fcLandings.AddRecentFlightEvents(cfr.dtFlight, cfr.cLandingsThisFlight);

            if (cfr.Dual > 0 && cfr.Total >= 1)
                fcTrainingFlights.AddRecentFlightEvents(cfr.dtFlight, 1);

            bool fIsBFR = false;
            cfr.FlightProps.ForEachEvent((cfp) => { fIsBFR = fIsBFR || cfp.PropertyType.IsBFR; });
            if (fIsBFR)
                fcProficiency.AddRecentFlightEvents(cfr.dtFlight, 1);
        }

        /// <summary>
        /// Computes the overall currency
        /// </summary>
        protected override void ComputeComposite()
        {
            // Only bother doing any real computation if we've ever been fully current on things.
            if (fcHoursSPL.HasBeenCurrent && fcHoursTMG.HasBeenCurrent && fcLandings.HasBeenCurrent && fcProficiency.HasBeenCurrent && fcTrainingFlights.HasBeenCurrent)
            {
                FlightCurrency fcAggregate = fcHoursSPL.AND(fcHoursTMG).AND(fcLandings).AND(fcProficiency).AND(fcTrainingFlights);
                CompositeCurrencyState = fcAggregate.CurrentState;
                CompositeExpiration = fcAggregate.ExpirationDate;

                CompositeDiscrepancy = string.Empty;

                // Set a discrepancy string in order that we find one
                if (CompositeCurrencyState == CurrencyState.NotCurrent)
                {
                    CompositeDiscrepancy = !fcHoursSPL.IsCurrent()
                        ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, fcHoursSPL.Discrepancy, Resources.Currency.HoursSailPlane)
                        : !fcHoursTMG.IsCurrent()
                        ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, fcHoursTMG.Discrepancy, Resources.Currency.HoursTMG)
                        : !fcLandings.IsCurrent()
                        ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, fcLandings.Discrepancy, Resources.Currency.Landings)
                        : !fcTrainingFlights.IsCurrent()
                        ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, fcTrainingFlights.Discrepancy, Resources.Currency.TrainingFlights)
                        : Resources.Currency.ProficiencyCheckRequired;
                }
            }
        }
    }

    public class EASASPLInstructorCurrency : CompositeFlightCurrency
    {
        protected FlightCurrency fcHours = new FlightCurrency(30, 365 * 3, false, string.Empty);
        protected FlightCurrency fcLandings = new FlightCurrency(60, 365 * 3, false, string.Empty);

        public EASASPLInstructorCurrency(string szName) : base(szName)
        {
            Query = new FlightQuery() { DateRange = FlightQuery.DateRanges.Custom, DateMin = DateTime.Now.Date.AddYears(-3), HasCFI = true, HasLandings = true, FlightCharacteristicsConjunction = GroupConjunction.Any };
            Query.AddCatClass(CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.Glider));
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            if (cfr.idCatClassOverride != CategoryClass.CatClassID.Glider || !cfr.fIsRealAircraft || cfr.CFI == 0.0M)
                return;

            fcHours.AddRecentFlightEvents(cfr.dtFlight, cfr.CFI);

            fcLandings.AddRecentFlightEvents(cfr.dtFlight, Math.Max(cfr.cLandingsThisFlight, cfr.FlightProps.TotalCountForPredicate(cfp => cfp.PropertyType.IsGliderGroundLaunch)));
        }

        /// <summary>
        /// Computes the overall currency
        /// </summary>
        protected override void ComputeComposite()
        {
            if (fcLandings.HasBeenCurrent || fcHours.HasBeenCurrent)
            {
                FlightCurrency fcAggregate = fcHours.OR(fcLandings);
                CompositeCurrencyState = fcAggregate.CurrentState;
                CompositeExpiration = fcAggregate.ExpirationDate;

                // By definition, if we are not current then both are not current.  Arbitrarily pick hours as instructor as discrepancy
                CompositeDiscrepancy = (CompositeCurrencyState == CurrencyState.NotCurrent) ? String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, fcHours.Discrepancy, Resources.Currency.HoursAsInstructor) : string.Empty;
            }
        }
    }
    #endregion
}