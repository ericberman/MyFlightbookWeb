using System;

/******************************************************
 * 
 * Copyright (c) 2007-2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightCurrency
{
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
                throw new ArgumentNullException("cfr");
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
                throw new ArgumentNullException("cfr");
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
                throw new ArgumentNullException("cfr");
            // Normal currency
            fcPIC.AddRecentFlightEvents(cfr.dtFlight, cfr.PIC);
            if (cfr.PIC > 0)
                fcLandings.AddRecentFlightEvents(cfr.dtFlight, cfr.cLandingsThisFlight);

            fcDual.AddRecentFlightEvents(cfr.dtFlight, cfr.Dual);

            // If we aren't current above, we still want to count any landings or time spent as dual, solo, or DPIC with an Instructor on board.
            bool fHasProficiencyCheck = false;
            decimal dPIC = 0.0M;
            bool fInstructorOnBoard = false;
            cfr.ForEachEvent((cfp) =>
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
                throw new ArgumentNullException("cfr");
            return (cfr.fIsRealAircraft && (cfr.fMotorGlider || CategoryClass.IsAirplane(cfr.idCatClassOverride)));
        }
    }

    /// <summary>
    /// LAPL Airplane (see https://www.caa.co.uk/General-aviation/Pilot-licences/EASA-requirements/LAPL/LAPL-(H)-requirements/) - 6 landings and 6 hours PIC in 12 months and an hour of instruction
    /// </summary>
    public class LAPLHCurrency : LAPLBase
    {
        public LAPLHCurrency() : base(6, 6, 12, Resources.Currency.LAPLH) { }

        protected override bool IsQualifyingFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            return (cfr.fIsRealAircraft && cfr.idCatClassOverride == CategoryClass.CatClassID.Helicopter);
        }
    }
}