using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2009-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
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

        public enum RegistrationTemplateMode
        {
            /// <summary>
            /// We don't know how to search for this 
            /// </summary>
            NoSearch = 0,

            /// <summary>
            /// Pass the entire tail number when searching
            /// </summary>
            WholeTail = 1,

            /// <summary>
            /// Pass only the tailnumber that FOLLOWS the country code when searching
            /// </summary>
            SuffixOnly = 2,

            /// <summary>
            /// Pass the whole tailnumber, with a dash
            /// </summary>
            WholeWithDash = 3
        }

        /// <summary>
        /// Preference for a given country code.  E.g., US (N) doesn't use a hyphen (N12345, not N-12345), but many countries, like Canada (C-FABC) do.
        /// </summary>
        public enum HyphenPreference {
            /// <summary>
            /// No preference - anything goes
            /// </summary>
            None,

            /// <summary>
            /// Use a hyphen (e.g., C-FABC for Canada)
            /// </summary>
            Hyphenate,

            /// <summary>
            /// Do NOT use a hyphen (e.g., US or countries that already have a hyphen, like F-OG
            /// </summary>
            NoHyphen
        }

        #region Properties
        /// <summary>
        /// The aircraft prefix for the country
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// The prefix, including any required (preferred) hyphen
        /// </summary>
        public string HyphenatedPrefix
        {
            get { return HyphenPref == HyphenPreference.Hyphenate ? Prefix + "-" : Prefix; }
        }

        /// <summary>
        /// The prefix with NO hyphens at all
        /// </summary>
        public string NormalizedPrefix { get; set; }

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

        public HyphenPreference HyphenPref { get; set; }

        #region Test for special countries
        /// <summary>
        /// Is this country code for a simulator?
        /// </summary>
        public Boolean IsSim { get { return String.Compare(Prefix, CountryCodePrefix.szSimPrefix, StringComparison.OrdinalIgnoreCase) == 0; } }

        public Boolean IsAnonymous { get { return String.Compare(Prefix, CountryCodePrefix.szAnonPrefix, StringComparison.OrdinalIgnoreCase) == 0; } }

        /// <summary>
        /// Is this not a known country?
        /// </summary>
        public Boolean IsUnknown { get { return String.IsNullOrEmpty(Prefix); } }
        #endregion

        /// <summary>
        /// Get the set of country codes that are "children" of this - e.g., "F" has children "FA", "F-O", "F-OD", "F-OG", and "F-OH"; "F-O" has childresn "F-OD", "F-OG", and "F-OH".
        /// </summary>
        public IEnumerable<CountryCodePrefix> Children
        {
            get { return new List<CountryCodePrefix>(CountryCodePrefix.CountryCodes()).FindAll(ccp => ccp.Prefix.CompareCurrentCultureIgnoreCase(this.Prefix) != 0 && ccp.NormalizedPrefix.StartsWith(this.NormalizedPrefix, StringComparison.CurrentCultureIgnoreCase)); }
        }
        #endregion

        #region constructors
        public CountryCodePrefix()
        {
            Prefix = NormalizedPrefix = CountryName = RegistrationURLTemplate = String.Empty;
            RegistrationURLTemplateMode = RegistrationTemplateMode.NoSearch;
            HyphenPref = HyphenPreference.None;
        }

        /// <summary>
        /// Create a new countrycodeprefix for a country.  Does not get persisted.
        /// </summary>
        /// <param name="szCountry">Country description</param>
        /// <param name="szPrefix">Prefix</param>
        public CountryCodePrefix(string szCountry, string szPrefix) : this()
        {
            Prefix = szPrefix ?? string.Empty;
            NormalizedPrefix = Prefix.Replace("-", string.Empty);
            CountryName = szCountry;
        }

        private CountryCodePrefix(MySqlDataReader dr) : this()
        {
            CountryName = Convert.ToString(dr["CountryName"], CultureInfo.InvariantCulture);
            Prefix = Convert.ToString(dr["Prefix"], CultureInfo.InvariantCulture);
            NormalizedPrefix = Prefix.Replace("-", string.Empty);
            Locale = Convert.ToString(dr["locale"], CultureInfo.InvariantCulture);
            RegistrationURLTemplate = (string)util.ReadNullableField(dr, "RegistrationURLTemplate", string.Empty);
            RegistrationURLTemplateMode = (RegistrationTemplateMode)Convert.ToInt32(dr["TemplateType"], CultureInfo.InvariantCulture);
            HyphenPref = (HyphenPreference)Convert.ToInt32(dr["HyphenPref"], CultureInfo.InvariantCulture);
        }
        #endregion

        #region Finding the best (longest) prefix match
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
            string szCompare = szTail.Replace("-", string.Empty);

            foreach (CountryCodePrefix cc in rgcc)
                if (szCompare.StartsWith(cc.NormalizedPrefix, StringComparison.CurrentCultureIgnoreCase) && cc.NormalizedPrefix.Length > ccBestMatch.NormalizedPrefix.Length)
                    ccBestMatch = cc;

            return ccBestMatch;
        }
        #endregion

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

        #region Tail numbers
        /// <summary>
        /// Returns a tailnumber that replaces the country code prefix with the supplied one
        /// </summary>
        /// <param name="ccNew">The desired country code prefix</param>
        /// <param name="szTail">The original tailnumber</param>
        /// <returns>A modified tailnumber</returns>
        public static string SetCountryCodeForTail(CountryCodePrefix ccNew, string szTail)
        {
            if (ccNew == null)
                throw new ArgumentNullException("ccNew");

            if (!String.IsNullOrEmpty(szTail) && (szTail.StartsWith(szAnonPrefix, StringComparison.OrdinalIgnoreCase) || szTail.StartsWith(szSimPrefix, StringComparison.OrdinalIgnoreCase)))
                return szTail;

            // strip the country code that was passed and pre-pend the sim prefix.
            CountryCodePrefix ccOld = CountryCodePrefix.BestMatchCountryCode(szTail);

            string szTailNew = szTail;
            if (ccOld.Prefix.CompareCurrentCulture(ccNew.Prefix) != 0)
            {
                szTailNew = ccNew.HyphenatedPrefix + Aircraft.NormalizeTail(szTail).Substring(ccOld.NormalizedPrefix.Length);
                if (szTailNew.Length > Aircraft.maxTailLength)
                    szTailNew = szTailNew.Substring(0, Aircraft.maxTailLength);
            }

            return szTailNew.ToUpper(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Sets hyphenation for all aircraft to match this country code's hyphenation conventions.  No-op for "None".  Does NOT affect children.
        /// </summary>
        /// <returns>Number of aircraft that are affected</returns>
        public int ADMINNormalizeMatchingAircraft()
        {
            // Nothing to do.
            if (HyphenPref == HyphenPreference.None)
                return -1;
            
            // HyphenPreference Hyphenate or nohyphen are handled by the "hyphenatedprefix" parameter.
            StringBuilder szQ = new StringBuilder(String.Format(CultureInfo.InvariantCulture, @"UPDATE aircraft 
                SET 
                    tailnumber = CONCAT(?hyphenatedPrefix,
                        SUBSTRING(REPLACE(tailnumber, '-', ''),
                        LENGTH(?NormalizedPrefix) + 1))
                WHERE
                    REPLACE(tailnumber, '-', '') LIKE ?NormalizedPrefixWildcard 
                        AND (length(tailnumber) < {0}) ", Aircraft.maxTailLength - ((HyphenPref == HyphenPreference.Hyphenate) ? 1 : 0)));

            List<MySqlParameter> lstParams = new List<MySqlParameter>() {
                new MySqlParameter("hyphenatedPrefix", HyphenatedPrefix),
                new MySqlParameter("NormalizedPrefix", NormalizedPrefix),
                new MySqlParameter("NormalizedPrefixWildcard", NormalizedPrefix + "%")
            };

            int i = 0;
            foreach (CountryCodePrefix ccp in Children)
            {
                string szParamName = String.Format(CultureInfo.InvariantCulture, "child{0}", i++);
                string szParamVal = ccp.NormalizedPrefix + "%";
                szQ.AppendFormat(CultureInfo.InvariantCulture, " AND REPLACE(tailnumber, '-', '') NOT LIKE ?{0} ", szParamName);
                lstParams.Add(new MySqlParameter(szParamName, szParamVal));
            }

            DBHelper dbh = new DBHelper(szQ.ToString());
            dbh.DoNonQuery((comm) => { comm.Parameters.AddRange(lstParams.ToArray()); });
            return dbh.AffectedRowCount;
        }
        #endregion

        #region Special pseudo-countries
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
        /// Returns a pseudo-country-code for unknown countries
        /// </summary>
        public static CountryCodePrefix UnknownCountry
        {
            get { return new CountryCodePrefix("(Unknown)", String.Empty); }
        }
        #endregion

        static private List<CountryCodePrefix> _cachedCountryCodes = null;

        /// <summary>
        /// Returns a cached array of all country codes
        /// </summary>
        /// <returns>An array of country codes</returns>
        public static IEnumerable<CountryCodePrefix> CountryCodes()
        {
            if (_cachedCountryCodes != null)
                return _cachedCountryCodes;

            _cachedCountryCodes = new List<CountryCodePrefix>();

            DBHelper dbh = new DBHelper("SELECT * FROM countrycodes");
            if (!dbh.ReadRows(
                (comm) => { },
                (dr) => { _cachedCountryCodes.Add(new CountryCodePrefix(dr)); }))
                throw new MyFlightbookException("Error getting countrycodes:\r\n" + dbh.LastError);

            return _cachedCountryCodes;
        }

        public static void FlushCache()
        {
            _cachedCountryCodes = null;
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

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} - {1}{2}", Prefix, CountryName, String.IsNullOrEmpty(Locale) ? string.Empty : String.Format(CultureInfo.CurrentCulture, " ({0})", Locale));
        }
    }
}