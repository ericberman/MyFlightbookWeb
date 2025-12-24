using MyFlightbook.Airports;
using MyFlightbook.Clubs;
using MyFlightbook.Currency;
using MyFlightbook.Printing;
using MyFlightbook.Schedule;
using MyFlightbook.Subscriptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using System.ServiceModel;
using System.Web;
using System.Web.Services;

/******************************************************
 * 
 * Copyright (c) 2022-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Ajax
{
    /// <summary>
    /// Provides AUTHENTICATED AJAX support for the Website.  NOT FOR EXTERNAL CONSUMPTION!!!  These APIs may change at any point.
    /// </summary>
    [WebService(Namespace = "http://myflightbook.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ServiceContract]
    [System.Web.Script.Services.ScriptService]
    [System.ComponentModel.ToolboxItem(false)]
    public class MyFlightbookAjax : WebService
    {
        private static void CheckAuth()
        {
            if (HttpContext.Current == null || HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
                throw new UnauthorizedAccessException();
        }

        #region Property Autocomplete 
        private static readonly char[] previouslyUsedValsSeparator = new char[] { ';' };
        private static readonly char[] metarAirportSeparator = new char[] { ' ' };

        [WebMethod(EnableSession = true)]
        [System.Web.Script.Services.ScriptMethod]
        public string[] PreviouslyUsedTextProperties(string prefixText, int count, string contextKey)
        {
            string[] rgResultDefault = Array.Empty<string>();

            if (String.IsNullOrEmpty(contextKey) || String.IsNullOrWhiteSpace(prefixText))
                return rgResultDefault;

            string[] rgsz = contextKey.Split(previouslyUsedValsSeparator, StringSplitOptions.RemoveEmptyEntries);
            if (rgsz.Length != 2 || String.IsNullOrEmpty(rgsz[0]) || string.IsNullOrEmpty(rgsz[1]))
                return rgResultDefault;

            if (!Int32.TryParse(rgsz[1], out int idPropType))
                return rgResultDefault;

            // Handle METAR autofill a bit different from other properties: fetch raw metar.
            if (idPropType == (int)CustomPropertyType.KnownProperties.IDPropMetar)
            {
                string[] rgAirports = prefixText.Split(metarAirportSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (rgAirports == null || rgAirports.Length == 0)
                    return rgResultDefault;

                string szLastWord = rgAirports[rgAirports.Length - 1];
                if (szLastWord.Length != 4)
                    return rgResultDefault;

                // trim the last word out of the prefix
                prefixText = prefixText.Trim();
                prefixText = prefixText.Substring(0, prefixText.Length - szLastWord.Length);

                List<string> lst = new List<string>();
                foreach (Weather.ADDS.METAR m in Weather.ADDS.ADDSService.LatestMETARSForAirports(szLastWord, false))
                    lst.Add(String.Format(CultureInfo.CurrentCulture, "{0} {1}", prefixText, m.raw_text).Trim());

                return lst.ToArray();
            }
            else
            {
                Dictionary<int, string[]> d = CustomFlightProperty.PreviouslyUsedTextValuesForUser(rgsz[0]);

                string[] results = (d.TryGetValue(idPropType, out string[] value)) ? value : null;
                if (results == null)
                    return Array.Empty<string>();

                List<string> lst = new List<string>(results);

                string szSearch = prefixText.ToUpperInvariant();

                lst = lst.FindAll(sz => sz.ToUpperInvariant().StartsWith(szSearch, StringComparison.InvariantCulture));
                if (lst.Count > count && count >= 1)
                    lst.RemoveRange(count - 1, lst.Count - count);

                return lst.ToArray();
            }
        }
        #endregion

        #region Other Autocomplete
        private static string[] DoSuggestion(string szQ, string prefixText, int count)
        {
            if (String.IsNullOrEmpty(prefixText) || string.IsNullOrEmpty(szQ) || prefixText.Length <= 2)
                return Array.Empty<string>();

            return util.GetKeysFromDB(String.Format(CultureInfo.InvariantCulture, szQ, util.keyColumn, count), prefixText);
        }

        [WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        public string[] SuggestModels(string prefixText, int count)
        {
            return DoSuggestion("SELECT DISTINCT model AS {0} FROM models WHERE REPLACE(model, '-', '') LIKE CONCAT(?prefix, '%') ORDER BY model ASC LIMIT {1}", Aircraft.NormalizeTail(prefixText, null), count);
        }

        [WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        public string[] SuggestModelTypes(string prefixText, int count)
        {
            return DoSuggestion("SELECT DISTINCT typename AS {0} FROM models WHERE REPLACE(typename, '-', '') LIKE CONCAT(?prefix, '%') ORDER BY model ASC LIMIT {1}", Aircraft.NormalizeTail(prefixText, null), count);
        }
        #endregion
    }
}
