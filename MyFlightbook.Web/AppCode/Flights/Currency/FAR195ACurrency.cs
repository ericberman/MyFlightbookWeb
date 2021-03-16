using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    /// <summary>
    /// 61.195(a) - can't have more than 8 hours of instruction within 24 hours.
    /// </summary>
    public class FAR195ACurrency : FAR117Currency
    {
        private readonly DateTime dt24HoursAgo = DateTime.UtcNow.AddHours(-24);
        private double totalInstruction;
        private const double MaxInstruction = 8.0;
        private const double CloseToMaxInstruction = 5.0;

        public FAR195ACurrency(bool fUseHHMM = false)
        {
            this.DisplayName = Resources.Currency.FAR195aTitle;
            UseHHMM = fUseHHMM;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            // quick short circuit for anything more than 24 hours old, or no instruction given, or not a real aircraft
            if (cfr.CFI == 0 || !cfr.fIsRealAircraft || cfr.dtFlight.CompareTo(dt24HoursAgo.Date) < 0)
                return;

            double netCFI = (double)cfr.CFI;

            // look for explicit lesson end/lesson start times.
            DateTime lessonStart = cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropLessonStart) ? cfr.FlightProps[CustomPropertyType.KnownProperties.IDPropLessonStart].DateValue : DateTime.MinValue;
            DateTime lessonEnd = cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropLessonEnd) ? cfr.FlightProps[CustomPropertyType.KnownProperties.IDPropLessonEnd].DateValue : DateTime.MinValue;

            TimeSpan ts;

            if (lessonStart.HasValue() && lessonEnd.HasValue())
                ts = lessonEnd.Subtract(lessonStart.LaterDate(dt24HoursAgo));
            else
            {
                EffectiveDutyPeriod edp = new EffectiveDutyPeriod(cfr) { FlightDutyStart = dt24HoursAgo, FlightDutyEnd = DateTime.UtcNow };
                ts = edp.TimeSince(dt24HoursAgo, netCFI, cfr);
            }
            if (ts.TotalHours > 0)
                totalInstruction += Math.Min(netCFI, ts.TotalHours);
        }

        public override CurrencyState CurrentState
        {
            get
            {
                if (totalInstruction > MaxInstruction)
                    return CurrencyState.NotCurrent;
                else if (totalInstruction > CloseToMaxInstruction)
                    return CurrencyState.GettingClose;
                else
                    return CurrencyState.OK;
            }
        }

        public override bool HasBeenCurrent
        {
            get { return totalInstruction > 0; }
        }

        public override DateTime ExpirationDate
        {
            get { return (HasBeenCurrent) ? DateTime.UtcNow : DateTime.MinValue; }
        }

        public override string DiscrepancyString
        {
            get
            {
                double totalRemaining = MaxInstruction - totalInstruction;

                return totalRemaining > 0 ?
                    String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR195aDiscrepancy, totalRemaining.FormatDecimal(UseHHMM)) :
                    String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR195aPastLimit, (-totalRemaining).FormatDecimal(UseHHMM));
            }
        }

        public override string StatusDisplay
        {
            get { return String.Format(CultureInfo.CurrentCulture, Resources.Currency.FAR195aStatus, totalInstruction.FormatDecimal(UseHHMM)); }
        }
    }
}
