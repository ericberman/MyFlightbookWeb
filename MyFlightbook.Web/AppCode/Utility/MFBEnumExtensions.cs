using MyFlightbook.Currency;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    public static class MFBEnumExtensions
    {
        #region Enum Extensions
        /// <summary>
        /// Determines if the specified turbinelevel is turbine (vs. piston or electric)
        /// </summary>
        /// <param name="tl"></param>
        /// <returns></returns>
        public static bool IsTurbine(this MakeModel.TurbineLevel tl) { return tl == MakeModel.TurbineLevel.Jet || tl == MakeModel.TurbineLevel.UnspecifiedTurbine || tl == MakeModel.TurbineLevel.TurboProp; }

        /// <summary>
        /// Determines if the currency state is any of the current states.
        /// </summary>
        /// <param name="cs"></param>
        /// <returns></returns>
        public static bool IsCurrent(this CurrencyState cs) { return cs != CurrencyState.NotCurrent; }

        public static string ToMySQLSort(this SortDirection sd) { return sd == SortDirection.Ascending ? "ASC" : "DESC"; }

        /// <summary>
        /// Format a signature state
        /// </summary>
        /// <param name="i">The signature state object (must be an integer that casts to a signature state</param>
        /// <returns>Empty string, valid, or invalid.</returns>
        public static string FormatSignatureState(this LogbookEntryCore.SignatureState ss)
        {
            return ss == LogbookEntryCore.SignatureState.None ? string.Empty : ss.ToString();
        }
        #endregion
    }
}