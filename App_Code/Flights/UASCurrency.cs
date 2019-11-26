using System;

/******************************************************
 * 
 * Copyright (c) 2016-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightCurrency
{
    /// <summary>
    /// Support for 107.65 currency (UAS)
    /// </summary>
    public class UASCurrency : ICurrencyExaminer
    {
        protected FlightCurrency TrainingCourse { get; set; }
        protected FlightCurrency KnowledgeTest { get; set; }
        protected FlightCurrency BFR { get; set; }
        protected FlightCurrency m_fcResult { get; set; }

        public UASCurrency()
        {
            TrainingCourse = new FlightCurrency(1, 24, true, "107.65(a)");
            KnowledgeTest = new FlightCurrency(1, 24, true, "107.65(b)");
            BFR = new FlightCurrency(1, 24, true, "107.65(c)");
        }

        public CurrencyState CurrentState { get { return m_fcResult.CurrentState; } }

        public string DiscrepancyString { get { return (m_fcResult.CurrentState == CurrencyState.NotCurrent) ? Resources.Currency.UASDiscrepancy : string.Empty; } }

        public string DisplayName { get { return Resources.Currency.UASCurrencyTitle; } }

        public DateTime ExpirationDate { get { return m_fcResult.ExpirationDate; } }

        public bool HasBeenCurrent { get { return m_fcResult.HasBeenCurrent; } }

        public string StatusDisplay { get { return m_fcResult.StatusDisplay; } }

        public void ExamineFlight(ExaminerFlightRow cfr)
        {
            cfr.FlightProps.ForEachEvent((cfp) =>
            {
                if (cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropUASKnowledgeTest10773)
                    KnowledgeTest.AddRecentFlightEvents(cfr.dtFlight, 1);
                if (cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropUASTrainingCourse10774)
                    TrainingCourse.AddRecentFlightEvents(cfr.dtFlight, 1);
                if (cfp.PropertyType.IsBFR && cfr.fIsRealAircraft)
                    BFR.AddRecentFlightEvents(cfr.dtFlight, 1);
            });
        }

        public void Finalize(decimal totalTime, decimal picTime)
        {
            m_fcResult = KnowledgeTest.OR(TrainingCourse.AND(BFR));
        }
    }
}
