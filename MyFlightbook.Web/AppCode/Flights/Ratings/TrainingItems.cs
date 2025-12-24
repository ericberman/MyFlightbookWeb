using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2023-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.RatingsProgress
{
    /// <summary>
    /// Utility class to provide training item autocomplete suggestions
    /// </summary>
    public static class TrainingItems
    {
        private readonly static char[] trainingPrefixSeparators = new char[] { '-', '[' };

        const string szCacheKey = "keyTrainingAutocomplete";

        private static List<string> AllTrainingItems
        {
            get 
            {
                List<string> lst = (List<string>)HttpRuntime.Cache?[szCacheKey];
                if (lst == null)
                {
                    lst = new List<string>();
                    DBHelper dbh = new DBHelper("SELECT * FROM trainingitems");
                    dbh.ReadRows((comm) => { }, (dr) => { lst.Add(String.Format(CultureInfo.CurrentCulture, "[{0}]", dr["task"])); });
                    HttpRuntime.Cache.Add(szCacheKey, lst, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(1, 0, 0, 0), System.Web.Caching.CacheItemPriority.BelowNormal, null);
                }

                return lst;
            }
        }

        /// <summary>
        /// Returns potential training completions for the provided prefix text.  E.g., if it is "[61.1" then it might produce all of the 61.1xxx training items.
        /// </summary>
        /// <param name="prefixText">The prefix</param>
        /// <param name="count">Number of items to return</param>
        /// <returns>An array of training items (strings)</returns>
        public static IEnumerable<string> SuggestTraining(string prefixText, int count)
        {
            // Don't do anything if the cache is null - bad things afoot!
            if (String.IsNullOrWhiteSpace(prefixText) || HttpRuntime.Cache == null)
                return Array.Empty<string>();

            // If no search terms in the prefix, return nothing.
            string[] rgszTerms = prefixText.ToUpper(CultureInfo.CurrentCulture).Split(trainingPrefixSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (rgszTerms.Length == 0)
                return rgszTerms;

            List<string> lstResult = AllTrainingItems.FindAll((sz) =>
            {
                foreach (string szTerm in rgszTerms)
                    if (!sz.ToUpper(CultureInfo.CurrentCulture).Contains(szTerm))
                        return false;
                return true;
            });

            if (lstResult.Count > count)
                lstResult.RemoveRange(count, lstResult.Count - count);

            return lstResult;
        }
    }
}