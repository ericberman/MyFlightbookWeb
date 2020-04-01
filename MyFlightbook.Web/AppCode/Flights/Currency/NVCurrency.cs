using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    public class NVCurrencyItem : FlightCurrency
    {
        /// <summary>
        /// When this expires, how many months are allowed before a proficiency check is required?
        /// </summary>
        public int MonthsBeforeProficiencyRequired { get; set; }

        /// <summary>
        /// Regardless of found events, does this need a proficiency check?
        /// </summary>
        public bool NeedsProficiencyCheck { get; set; }

        public NVCurrencyItem()
            : base()
        {
        }

        public NVCurrencyItem(decimal cOps, int cMonths, string szName, int monthsGapAllowed)
            : base(cOps, cMonths, true, szName)
        {
            MonthsBeforeProficiencyRequired = monthsGapAllowed;
        }

        public override CurrencyState CurrentState
        {
            get { return NeedsProficiencyCheck ? CurrencyState.NotCurrent : base.CurrentState; }
        }

        public override string DiscrepancyString
        {
            get
            {
                if (NeedsProficiencyCheck || DateTime.Compare(ExpirationDate.AddCalendarMonths(MonthsBeforeProficiencyRequired).Date, DateTime.Now.Date) < 0)
                    return Resources.Currency.NVProficiencyCheckRequired;
                else if (IsCurrent())
                    return string.Empty;
                else
                    return String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, Discrepancy, Discrepancy == 1 ? Resources.Currency.NVGoggleOperation : Resources.Currency.NVGoggleOperations);
            }
        }
    }
    
    /// <summary>
         /// Compute 61.57(f) currency
         /// Basic rule: 3 NV Goggle Operations within 2 calendar months to be PIC with passengers
         /// If Helicopter or Powered Lift, requirement is 6 ops
         /// If out of currency, can be PIC (no passengers) for 4 calendar months
         /// </summary>
    public class NVCurrency : IFlightExaminer
    {
        readonly NVCurrencyItem fcNVPassengers;
        readonly NVCurrencyItem fcNVNoPassengers;
        readonly NVCurrencyItem fcNVIPC;
        List<FlightCurrency> m_lstCurrencies;
        readonly CategoryClass.CatClassID m_ccid;
        readonly string szNVGeneric;

        public static bool IsHelicopterOrPoweredLift(CategoryClass.CatClassID ccid)
        {
            return ccid == CategoryClass.CatClassID.Helicopter || ccid == CategoryClass.CatClassID.PoweredLift;
        }

        public NVCurrency(CategoryClass.CatClassID ccid)
        {
            m_ccid = ccid;
            bool fIsHeliOrPowered = IsHelicopterOrPoweredLift(ccid);
            int requiredOps = fIsHeliOrPowered ? 6 : 3;
            fcNVPassengers = new NVCurrencyItem(requiredOps, 2, String.Format(CultureInfo.CurrentCulture, Resources.Currency.NVPIC, fIsHeliOrPowered ? Resources.Currency.NVHeliOrPoweredLift : Resources.Currency.NVAirplane, Resources.Currency.NVPassengers), 4);
            fcNVNoPassengers = new NVCurrencyItem(requiredOps, 4, String.Format(CultureInfo.CurrentCulture, Resources.Currency.NVPIC, fIsHeliOrPowered ? Resources.Currency.NVHeliOrPoweredLift : Resources.Currency.NVAirplane, Resources.Currency.NVNoPassengers), 0);
            fcNVIPC = new NVCurrencyItem(1, 4, "NV Proficiency Check", 0);
            szNVGeneric = String.Format(CultureInfo.CurrentCulture, Resources.Currency.NVPIC, fIsHeliOrPowered ? Resources.Currency.NVHeliOrPoweredLift : string.Empty, string.Empty);
            m_lstCurrencies = new List<FlightCurrency>();
        }

        public void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            if (cfr.FlightProps == null)
                return;

            cfr.FlightProps.ForEachEvent((pe) =>
            {
                if (pe.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropNVGoggleOperations)
                {
                    // Since being current for a helicopter/powered lift requires doing hovering tasks (61.57(f)(1)(ii)),
                    // we add the events IF (a) they were performed in a heli/pl OR (b) this NVCurrency is not a heli/pl
                    if (IsHelicopterOrPoweredLift(cfr.idCatClassOverride) || !IsHelicopterOrPoweredLift(m_ccid))
                    {
                        fcNVPassengers.AddRecentFlightEvents(cfr.dtFlight, pe.IntValue);
                        fcNVNoPassengers.AddRecentFlightEvents(cfr.dtFlight, pe.IntValue);
                    }
                }
                else if (pe.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropNVGoggleProficiencyCheck)
                {
                    fcNVNoPassengers.AddRecentFlightEvents(cfr.dtFlight, fcNVNoPassengers.RequiredEvents);
                    fcNVPassengers.AddRecentFlightEvents(cfr.dtFlight, fcNVPassengers.RequiredEvents);
                    fcNVIPC.AddRecentFlightEvents(cfr.dtFlight, 1);
                }
            });
        }

        private void AddProficiencyRequired(DateTime dtExpired)
        {
            NVCurrencyItem nvci = new NVCurrencyItem(1, 0, szNVGeneric, 0) { NeedsProficiencyCheck = true };
            nvci.AddRecentFlightEvents(dtExpired, 1);
            m_lstCurrencies.Add(nvci);
        }

        /// <summary>
        /// Get all of the possible NV Flight currencies
        /// </summary>
        /// <returns></returns>
        public IEnumerable<FlightCurrency> CurrencyEvents
        {
            get
            {
                m_lstCurrencies = new List<FlightCurrency>();
                FlightCurrency fcNV = fcNVPassengers.OR(fcNVNoPassengers).OR(fcNVIPC);  // composite value

                if (fcNV.HasBeenCurrent)
                {
                    if (fcNV.CurrentState == CurrencyState.NotCurrent)
                        AddProficiencyRequired(fcNVNoPassengers.ExpirationDate);
                    else
                    {
                        // check for a gap that could indicate that a proficiency check is needed
                        CurrencyPeriod[] rgcpMissingProfCheck = fcNV.FindCurrencyGap(fcNVIPC.MostRecentEventDate, 0);
                        if (rgcpMissingProfCheck == null)   // no gap - we have potentially two currencies to return
                        {
                            m_lstCurrencies.Add(fcNVPassengers);
                            if (!fcNVPassengers.IsCurrent())
                                m_lstCurrencies.Add(fcNVNoPassengers);
                        }
                        else
                        {
                            // gap was found - we aren't actually current.
                            AddProficiencyRequired(rgcpMissingProfCheck[0].EndDate);
                        }
                    }
                }
                return m_lstCurrencies;
            }
        }
    }
}
