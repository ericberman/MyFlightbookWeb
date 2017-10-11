using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using MySql.Data.MySqlClient;

/******************************************************
 * 
 * Copyright (c) 2009-2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Country-code prefixes for a given country.
    /// </summary>
    public class CountryCodePrefix
    {
        public const string szSimPrefix = "SIM";
        public const string szAnonPrefix = "#";

        public enum RegistrationTemplateMode { NoSearch = 0, WholeTail = 1, SuffixOnly = 2, WholeWithDash = 3 }

        #region Properties
        /// <summary>
        /// The aircraft prefix for the country
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// The name of the country
        /// </summary>
        public string CountryName { get; set; }

        /// <summary>
        /// Two-letter Locale code, if any, for the country (e.g., "us" for United States, "gb" for UK, etc.)
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        /// The URL template to look up an aircraft by registration
        /// </summary>
        public string RegistrationURLTemplate { get; set; }

        /// <summary>
        /// How to use the template with a tailnumber
        /// </summary>
        public RegistrationTemplateMode RegistrationURLTemplateMode { get; set; }
        #endregion

        #region constructors
        public CountryCodePrefix()
        {
            Prefix = CountryName = RegistrationURLTemplate = String.Empty;
            RegistrationURLTemplateMode = RegistrationTemplateMode.NoSearch;
        }

        /// <summary>
        /// Create a new countrycodeprefix for a country.  Does not get persisted.
        /// </summary>
        /// <param name="szCountry">Country description</param>
        /// <param name="szPrefix">Prefix</param>
        public CountryCodePrefix(string szCountry, string szPrefix) : this()
        {
            Prefix = szPrefix;
            CountryName = szCountry;
        }

        private CountryCodePrefix(MySqlDataReader dr) : this()
        {
            CountryName = Convert.ToString(dr["CountryName"], CultureInfo.InvariantCulture);
            Prefix = Convert.ToString(dr["Prefix"], CultureInfo.InvariantCulture);
            Locale = Convert.ToString(dr["locale"], CultureInfo.InvariantCulture);
            RegistrationURLTemplate = (string)util.ReadNullableField(dr, "RegistrationURLTemplate", string.Empty);
            RegistrationURLTemplateMode = (RegistrationTemplateMode)Convert.ToInt32(dr["TemplateType"], CultureInfo.InvariantCulture);
        }
        #endregion

        /// <summary>
        /// Find the most likely country for a given tail number
        /// </summary>
        /// <param name="szTail">The tailnumber, including prefix</param>
        /// <returns>The countrycodeprefix object representing the best match</returns>
        public static CountryCodePrefix BestMatchCountryCode(string szTail)
        {
            if (szTail == null)
                throw new ArgumentNullException("szTail");

            CountryCodePrefix ccBestMatch = CountryCodePrefix.UnknownCountry;

            // check for simulator, which isn't in rgcc
            if (szTail.StartsWith(CountryCodePrefix.szSimPrefix, StringComparison.CurrentCultureIgnoreCase))
                return CountryCodePrefix.SimCountry;

            if (szTail.StartsWith(CountryCodePrefix.szAnonPrefix, StringComparison.CurrentCultureIgnoreCase))
                return CountryCodePrefix.AnonymousCountry;

            IEnumerable<CountryCodePrefix> rgcc = CountryCodePrefix.CountryCodes();

            foreach (CountryCodePrefix cc in rgcc)
                if (szTail.StartsWith(cc.Prefix, StringComparison.CurrentCultureIgnoreCase) && cc.Prefix.Length > ccBestMatch.Prefix.Length)
                    ccBestMatch = cc;

            return ccBestMatch;
        }

        /// <summary>
        /// Get the default country code for the specified locale
        /// </summary>
        /// <param name="szlocale">The two-letter locale</param>
        /// <returns>Best match.  Returns "N" (US) if no match</returns>
        public static CountryCodePrefix DefaultCountryCodeForLocale(string szlocale)
        {
            List<CountryCodePrefix> rgcc = new List<CountryCodePrefix>(CountryCodes());
            if (String.IsNullOrEmpty(szlocale))
                return rgcc[0];

            List<CountryCodePrefix> rgccMatch = rgcc.FindAll(cc => String.Compare(cc.Locale, szlocale, true) == 0);
            return rgccMatch.Count == 0 ? rgcc[0] : rgccMatch[0];
        }

        /// <summary>
        /// Returns a tailnumber the replaces the country code prefix with the supplied one
        /// </summary>
        /// <param name="ccNew">The desired country code prefix</param>
        /// <param name="szTail">The original tailnumber</param>
        /// <returns>A modified tailnumber</returns>
        public static string SetCountryCodeForTail(CountryCodePrefix ccNew, string szTail)
        {
            if (ccNew == null)
                throw new ArgumentNullException("ccNew");

            // strip the country code that was passed and pre-pend the sim prefix.
            CountryCodePrefix ccOld = CountryCodePrefix.BestMatchCountryCode(szTail);

            string szTailNew = ccNew.Prefix + szTail.Substring(ccOld.Prefix.Length);
            if (szTailNew.Length > Aircraft.maxTailLength)
                szTailNew = szTailNew.Substring(0, Aircraft.maxTailLength);

            return szTailNew.ToUpper(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns a pseudo-country-code for simulators
        /// </summary>
        public static CountryCodePrefix SimCountry
        {
            get { return new CountryCodePrefix("(Simulator)", CountryCodePrefix.szSimPrefix); }
        }

        /// <summary>
        /// Returns a pseudo-country-code for anonymous aircraft
        /// </summary>
        public static CountryCodePrefix AnonymousCountry
        {
            get { return new CountryCodePrefix("(Anonymous)", CountryCodePrefix.szAnonPrefix); }
        }

        /// <summary>
        /// Is this country code for a simulator?
        /// </summary>
        public Boolean IsSim
        {
            get { return String.Compare(Prefix, CountryCodePrefix.szSimPrefix, StringComparison.OrdinalIgnoreCase) == 0; }
        }

        public Boolean IsAnonymous
        {
            get { return String.Compare(Prefix, CountryCodePrefix.szAnonPrefix, StringComparison.OrdinalIgnoreCase) == 0; }
        }

        /// <summary>
        /// Returns a pseudo-country-code for unknown countries
        /// </summary>
        public static CountryCodePrefix UnknownCountry
        {
            get { return new CountryCodePrefix("(Unknown)", String.Empty); }
        }

        /// <summary>
        /// Is this not a known country?
        /// </summary>
        public Boolean IsUnknown
        {
            get { return String.IsNullOrEmpty(Prefix); }
        }

        /// <summary>
        /// Returns a cached array of all country codes
        /// </summary>
        /// <returns>An array of country codes</returns>
        public static IEnumerable<CountryCodePrefix> CountryCodes()
        {
            const string szCacheKey = "CountryCodesArrayKey";

            CountryCodePrefix[] rgCountryCodes = null;

            if (HttpRuntime.Cache != null)
                rgCountryCodes = (CountryCodePrefix[])HttpRuntime.Cache[szCacheKey];

            if (rgCountryCodes != null)
                return rgCountryCodes;

            List<CountryCodePrefix> ar = new List<CountryCodePrefix>();

            DBHelper dbh = new DBHelper("SELECT * FROM countrycodes");
            if (!dbh.ReadRows(
                (comm) => { },
                (dr) => { ar.Add(new CountryCodePrefix(dr)); }))
                throw new MyFlightbookException("Error getting countrycodes:\r\n" + dbh.LastError);

            rgCountryCodes = ar.ToArray();

            if (HttpRuntime.Cache != null)
                HttpRuntime.Cache[szCacheKey] = rgCountryCodes;

            return rgCountryCodes;
        }

        /// <summary>
        /// True if the specified string is identical to the sim prefix.
        /// </summary>
        /// <param name="sz"></param>
        /// <returns></returns>
        public static Boolean IsNakedSim(string sz)
        {
            return sz.CompareOrdinalIgnoreCase(szSimPrefix) == 0;
        }

        /// <summary>
        /// True if the string is identical to the anonymous prefix
        /// </summary>
        /// <param name="sz"></param>
        /// <returns></returns>
        public static Boolean IsNakedAnon(string sz)
        {
            return sz.CompareOrdinalIgnoreCase(szAnonPrefix) == 0;
        }
    }
}