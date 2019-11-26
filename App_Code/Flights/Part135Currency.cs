using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightCurrency
{
    #region Part 135.29x classes
    public class Part135_293a : FlightCurrency
    {
        public Part135_293a(string szName)
            : base(1, 12, true, szName)
        {

        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDProp135293Knowledge))
                AddRecentFlightEvents(cfr.dtFlight, 1);
        }
    }

    public class Part135_293b : FlightCurrency
    {
        public Part135_293b(string szCatClass, string szName)
            : base(1, 12, true, String.Format(CultureInfo.InvariantCulture, "{0} - {1}", szCatClass, szName))
        {

        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            cfr.FlightProps.ForEachEvent((pfe) =>
            {
                if (pfe.PropTypeID == (int)CustomPropertyType.KnownProperties.IDProp135293Competency || pfe.PropTypeID == (int) CustomPropertyType.KnownProperties.IDProp135297IPC)
                    AddRecentFlightEvents(cfr.dtFlight, 1);
            });
        }
    }

    public class Part135_297a : FlightCurrency
    {
        public Part135_297a(string szCatClass, string szName)
            : base(1, 6, true, String.Format(CultureInfo.InvariantCulture, "{0} - {1}", szCatClass, szName))
        {

        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            cfr.FlightProps.ForEachEvent((pfe) =>
            {
                if (pfe.PropTypeID == (int)CustomPropertyType.KnownProperties.IDProp135297IPC)
                    AddRecentFlightEvents(cfr.dtFlight, 1);
            });
        }
    }

    public class Part135_299a : FlightCurrency
    {
        public Part135_299a(string szCatClass, string szName)
            : base(1, 12, true, String.Format(CultureInfo.InvariantCulture, "{0} - {1}", szCatClass, szName))
        {

        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            if (cfr.fIsRealAircraft && cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDProp135299FlightCheck))
                AddRecentFlightEvents(cfr.dtFlight, 1);
        }
    }
    #endregion

    #region Part 135.267 classes
    public abstract class Part135_267Base : ICurrencyExaminer
    {
        protected DateTime PeriodStart { get; set; }
        protected double Threshold { get; set; }
        protected double StatusSoFar { get; set; }
        protected string Name { get; set; }
        protected string StatusFormat { get; set; }

        protected const double YellowZoneFactor = 0.9;

        protected Part135_267Base(string szName, double threshold, string szStatusFormat)
        {
            Threshold = threshold;
            StatusSoFar = 0;
            Name = szName;
            StatusFormat = szStatusFormat;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}: {1} of {2} since {3}", Name, StatusSoFar, Threshold, PeriodStart.UTCFormattedStringOrEmpty(false));
        }

        #region IFlightExaminer Interface
        public void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            if (cfr.dtFlight.CompareTo(PeriodStart) >= 0)
            {
                if (cfr.dtFlightStart.HasValue() && cfr.dtFlightEnd.HasValue())
                {
                    DateTime dtEffectivestart = cfr.dtFlightStart.LaterDate(PeriodStart);
                    TimeSpan ts = cfr.dtFlightEnd.Subtract(cfr.dtFlightStart);
                    if (ts.TotalHours > 0)
                        StatusSoFar += ts.TotalHours;
                }
                else
                    StatusSoFar += Math.Round(((double)cfr.Total * 60)) / 60.0;
            }
        }
        #endregion

        #region ICurrencyExaminer Interface
        public CurrencyState CurrentState
        {
            get
            {
                if (StatusSoFar > Threshold)
                    return CurrencyState.NotCurrent;
                else if (StatusSoFar >= YellowZoneFactor * Threshold)
                    return CurrencyState.GettingClose;
                else
                    return CurrencyState.OK;
            }
        }

        public string DiscrepancyString
        {
            get
            {
                switch (CurrentState)
                {
                    default:
                    case CurrencyState.OK:
                        return string.Empty;
                    case CurrencyState.GettingClose:
                        return String.Format(CultureInfo.CurrentCulture, Resources.Currency.Part135267DiscrepancyGettingClose, Threshold - StatusSoFar);
                    case CurrencyState.NotCurrent:
                        return String.Format(CultureInfo.CurrentCulture, Resources.Currency.Part135267DiscrepancyOver, StatusSoFar - Threshold);
                }
            }
        }

        public string DisplayName
        {
            get { return Name; }
        }

        public abstract DateTime ExpirationDate { get; }

        public bool HasBeenCurrent
        {
            get { return StatusSoFar > 0; }
        }

        public string StatusDisplay
        {
            get { return String.Format(CultureInfo.CurrentCulture, StatusFormat, StatusSoFar); }
        }

        public virtual void Finalize(decimal totalTime, decimal picTime) { }
        #endregion
    }

    public class Part135_267a1 : Part135_267Base
    {
        public Part135_267a1() : base(Resources.Currency.Part135267a1Title, 500, Resources.Currency.Part135267FormatStatusQuarter)
        {
            PeriodStart = new DateTime(DateTime.Now.Year, (((int)((DateTime.Now.Month - 1) / 3)) * 3) + 1, 1);
        }

        public override DateTime ExpirationDate { get { return PeriodStart.AddCalendarMonths(3); } }
    }

    public class Part135_267a2 : Part135_267Base
    {
        public Part135_267a2() : base(Resources.Currency.Part135267a2Title, 800, Resources.Currency.Part135267FormatStatus2Quarters)
        {
            PeriodStart = new DateTime(DateTime.Now.Year, (((int)((DateTime.Now.Month - 1) / 3)) * 3) + 1, 1).AddCalendarMonths(-3);
        }

        public override DateTime ExpirationDate { get { return PeriodStart.AddCalendarMonths(6); } }
    }

    public class Part135_267a3 : Part135_267Base
    {
        public Part135_267a3() : base(Resources.Currency.Part135267a3Title, 1400, Resources.Currency.Part135267FormatStatusYear)
        {
            PeriodStart = new DateTime(DateTime.Now.Year, 1, 1);
        }

        public override DateTime ExpirationDate { get { return PeriodStart.AddCalendarMonths(12); } }
    }

    public class Part135_265a1 : Part135_267Base
    {
        public Part135_265a1() : base(Resources.Currency.Part135265a1Title, 1200, Resources.Currency.Part135265a1FormatStatus)
        {
            PeriodStart = new DateTime(DateTime.Now.Year, 1, 1);
        }

        public override DateTime ExpirationDate { get { return PeriodStart.AddCalendarMonths(12); } }
    }

    public class Part135_265a2 : Part135_267Base
    {
        public Part135_265a2() : base(Resources.Currency.Part135265a2Title, 120, Resources.Currency.Part135265a2FormatStatus)
        {
            PeriodStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        }

        public override DateTime ExpirationDate { get { return PeriodStart.AddMonths(1); } }
    }

    public class Part135_265a3 : Part135_267Base
    {
        public Part135_265a3() : base(Resources.Currency.Part135265a3Title, 34, Resources.Currency.Part135265a3FormatStatus)
        {
            PeriodStart = DateTime.Now.AddDays(-7);
        }

        public override DateTime ExpirationDate { get { return DateTime.Now; } }
    }
    #endregion
}