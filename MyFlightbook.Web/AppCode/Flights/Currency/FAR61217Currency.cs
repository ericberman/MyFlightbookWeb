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
    /// Currency per 61.217 - need to have done some instruction within past 12 months to be current.
    /// </summary>
    public class FAR61217Currency : FlightCurrency
    {
        DateTime? LastInstruction;

        public FAR61217Currency() : base(0.01M, 12, true, Resources.Currency.Part61217Title)
        {
            LastInstruction = new DateTime?();
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (LastInstruction.HasValue)
                return;

            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            if (cfr.CFI > 0 || cfr.FlightProps.TimeForProperty(CustomPropertyType.KnownProperties.IDPropGroundInstructionGiven) > 0)
                LastInstruction = cfr.dtFlight;
        }

        public override bool HasBeenCurrent
        {
            get { return LastInstruction.HasValue; }
        }

        public override CurrencyState CurrentState
        {
            get
            {
                if (!LastInstruction.HasValue)
                    return CurrencyState.OK;

                DateTime dtCutoff = DateTime.Now.AddCalendarMonths(-12);

                if (dtCutoff.CompareTo(LastInstruction.Value) > 0)
                    return CurrencyState.NotCurrent;
                else if (dtCutoff.CompareTo(LastInstruction.Value.AddMonths(-1)) > 0)
                    return CurrencyState.GettingClose;

                return CurrencyState.OK;
            }
        }

        public override DateTime ExpirationDate
        {
            get { return (LastInstruction.HasValue) ? LastInstruction.Value.AddCalendarMonths(12) : DateTime.MinValue; }
        }
    }
}
