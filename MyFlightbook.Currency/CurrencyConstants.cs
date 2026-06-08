using System;

/******************************************************
 * 
 * Copyright (c) 2007-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    /// <summary>
    /// Bitflags for various currency options which can be selected by the user.
    /// </summary>
    [Flags]
    public enum CurrencyOptionFlags
    {
        flagPerModelCurrency = 0x0001,
        flagArmyMDSCurrency = 0x0002,
        flagFAR117DutyTimeCurrency = 0x0004,
        flagFAR135DutyTimeCurrency = 0x0008,
        flagCurrencyExpirationBit1 = 0x0010,
        flagCurrencyExpirationBit2 = 0x0020,
        flagCurrencyExpirationBit3 = 0x0040,
        flagCurrencyExpirationMask = flagCurrencyExpirationBit1 | flagCurrencyExpirationBit2 | flagCurrencyExpirationBit3,
        flagUseLooseIFRCurrency = 0x0080, // DEPRECATED, do not use
        flagShowTotalsPerModel = 0x0100,
        flagUseCanadianCurrencyRules = 0x0200,
        flagUseFAR135_29xStatus = 0x0400,
        flagUseFAR135_26xStatus = 0x0800,
        flagUseLAPLCurrency = 0x1000,
        flagFAR117IncludeAllFlights = 0x2000,
        flagUseFAR61217 = 0x4000,
        flagUseEASAMedical = 0x8000,
        flagsShowTotalsPerFamily = 0x00010000,
        flagSuppressModelFeatureTotals = 0x00020000,
        flagAllowNightTouchAndGo = 0x00040000,
        flagRequireDayLandingsDayCurrency = 0x00080000,
        flagShow2DigitTotals = 0x00100000,	// DEPRECATED, do not use
        flagUseFAR125_2xxStatus = 0x00200000,
        flagUseAustralianCurrency = 0x00400000
    }

    /// <summary>
    /// Status for currency
    /// </summary>
    public enum CurrencyState { NotCurrent = 0, GettingClose = 1, OK = 2, NoDate = 3 };

    public enum CurrencyJurisdiction { FAA, Canada, EASA, Australia }

    /// <summary>
    /// Utility class for pruning expired currency items per use preference.
    /// </summary>
    public static class CurrencyExpiration
    {
        public enum Expiration { None, TenYear, FiveYear, ThreeYear, TwoYear, OneYear }

        public static DateTime CutoffDate(Expiration ce)
        {
            switch (ce)
            {
                default:
                case Expiration.None:
                    return DateTime.MinValue;
                case Expiration.OneYear:
                    return DateTime.Now.AddYears(-1);
                case Expiration.TwoYear:
                    return DateTime.Now.AddYears(-2);
                case Expiration.ThreeYear:
                    return DateTime.Now.AddYears(-3);
                case Expiration.FiveYear:
                    return DateTime.Now.AddYears(-5);
                case Expiration.TenYear:
                    return DateTime.Now.AddYears(-10);
            }
        }

        public static string ExpirationLabel(Expiration ce)
        {
            switch (ce)
            {
                default:
                case Expiration.None:
                    return Properties.Currency.currencyExpirationNone;
                case Expiration.OneYear:
                    return Properties.Currency.currencyExpiration1Year;
                case Expiration.TwoYear:
                    return Properties.Currency.currencyExpiration2Years;
                case Expiration.ThreeYear:
                    return Properties.Currency.currencyExpiration3Years;
                case Expiration.FiveYear:
                    return Properties.Currency.currencyExpiration5Years;
                case Expiration.TenYear:
                    return Properties.Currency.currencyExpiration10Years;
            }
        }
    }
}
