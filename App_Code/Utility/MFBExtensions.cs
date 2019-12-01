using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2008-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Extension methods to other classes for those classes
    /// </summary>
    public static class MFBExtensions
    {
        #region DateTime Extensions
        /// <summary>
        /// Is the date/time valid (i.e., does it have a value other than minvalue)
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static bool HasValue(this DateTime dt)
        {
            return dt.CompareTo(DateTime.MinValue) != 0 && dt.Year > 1;
        }

        /// <summary>
        /// Returns the format string for UTC date in the current locale's format
        /// </summary>
        /// <param name="dt">The date</param>
        /// <param name="fTimeOnly">True if the date is implicit and we only need a time</param>
        /// <returns>The format string</returns>
        public static string UTCDateFormatString(this DateTime dt, bool fTimeOnly = false)
        {
            string szFormat = (fTimeOnly ? string.Empty : System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortDatePattern + " ") + "HH:mm";
            return dt.ToString(szFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Return a locally formatted datetime string for the given value.
        /// </summary>
        /// <param name="dt">The date</param>
        /// <param name="fTimeOnly">True if the date is implicit and we only need a time</param>
        /// <returns>A locale-formatted UTC string or emptyr string if no value</returns>
        public static string UTCFormattedStringOrEmpty(this DateTime dt, bool fTimeOnly)
        {
            return dt.HasValue() ? dt.UTCDateFormatString(fTimeOnly) : string.Empty;
        }

        /// <summary>
        /// Returns the date in yyyy-MM-dd format
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string YMDString(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Looks ahead the specified number of calendar months, returns the last day of the valid month.
        /// E.g., if today is Jan 14, then AddCalendarMonths of 6 months should return July 31
        /// If cMonths is negative, returns the 1st day of the respective number of months back
        /// If cMonths is 0 it goes to the end of the current month.
        /// </summary>
        /// <param name="dt">The base date</param>
        /// <param name="cMonths">The # of months </param>
        /// <returns>The date representing the outer (start/finish, depending on sign of cMonths) date of the added month</returns>
        static public DateTime AddCalendarMonths(this DateTime dt, int cMonths)
        {
            if (!dt.HasValue())
                return DateTime.MinValue;

            if (cMonths >= 0)
                return new DateTime(dt.Year, dt.Month, 1).AddMonths(cMonths + 1).AddDays(-1);
            else
                return new DateTime(dt.Year, dt.Month, 1).AddMonths(cMonths);
        }

        /// <summary>
        /// Returns the earlier of two dates
        /// </summary>
        /// <param name="dt1">The 1st date</param>
        /// <param name="dt2">The 2nd date</param>
        /// <returns>The earlier date</returns>
        static public DateTime EarlierDate(this DateTime dt1, DateTime dt2)
        {
            return (DateTime.Compare(dt1, dt2) < 0) ? dt1 : dt2;
        }

        /// <summary>
        /// Returns the later of two dates
        /// </summary>
        /// <param name="dt1">The 1st date</param>
        /// <param name="dt2">The 2nd date</param>
        /// <returns>The earlier date</returns>
        static public DateTime LaterDate(this DateTime dt1, DateTime dt2)
        {
            return (DateTime.Compare(dt1, dt2) > 0) ? dt1 : dt2;
        }

        /// <summary>
        /// Since different systems have different approximations for distant past, this pegs the date to distant past if its year is less than 100
        /// </summary>
        /// <param name="dt">The date</param>
        /// <returns>DateTime.MinValue or the original date</returns>
        static public DateTime NormalizeDate(this DateTime dt)
        {
            if (dt.Year < 100)
                return DateTime.MinValue;
            return dt;
        }
        #endregion

        #region String Extensions
        #region Conversions/parsing/deserialization
        /// <summary>
        /// Converts an HH:MM formatted string into a decimal value
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="defVal">The default value in case of error</param>
        /// <returns>A decimal value (could be defval)</returns>
        static public decimal DecimalFromHHMM(this string s, decimal defVal = 0.0M)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            string[] rgSz = s.Split(new string[] { System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.TimeSeparator }, StringSplitOptions.None);
            if (rgSz.Length == 0 || rgSz.Length > 2)
                return defVal;

            try
            {
                int hours = String.IsNullOrEmpty(rgSz[0]) ? 0 : Convert.ToInt32(rgSz[0], CultureInfo.InvariantCulture);
                int minutes = (rgSz.Length == 2 && !String.IsNullOrEmpty(rgSz[1])) ? Convert.ToInt32(rgSz[1], CultureInfo.InvariantCulture) : 0;
                if (minutes >= 60)
                    return defVal;
                return Math.Round((hours + (minutes / 60.0M)) * 100.0M) / 100.0M;
            }
            catch (FormatException)
            {
                return defVal;
            }
        }

        /// <summary>
        /// Parses a string into an Decimal, returning defVal if any error
        /// </summary>
        /// <param name="sz">The string to parse</param>
        /// <param name="defVal">The default value</param>
        /// <returns>The parsed result or defVal</returns>
        static public Decimal SafeParseDecimal(this string sz, Decimal defVal = 0.0M)
        {
            if (String.IsNullOrEmpty(sz))
                return defVal;

            Decimal d;
            if (Decimal.TryParse(sz, NumberStyles.Any, CultureInfo.CurrentCulture, out d))
                return d;
            else
                return sz.DecimalFromHHMM(defVal);
        }

        /// <summary>
        /// Parses a string into an Decimal, returning defVal if any error
        /// </summary>
        /// <param name="sz">The string to parse</param>
        /// <param name="defVal">The default value</param>
        /// <returns>The parsed result or defVal</returns>
        static public double SafeParseDouble(this string sz, double defVal = 0.0)
        {
            if (String.IsNullOrEmpty(sz))
                return defVal;

            double d;
            if (double.TryParse(sz, NumberStyles.Any, CultureInfo.CurrentCulture, out d))
                return d;

            return defVal;
        }

        /// <summary>
        /// Parses a string into an Int32, returning defVal if any error
        /// </summary>
        /// <param name="sz">The string to parse</param>
        /// <param name="defVal">The default value</param>
        /// <returns>The parsed result or defVal</returns>
        static public Int32 SafeParseInt(this string sz, Int32 defVal = 0)
        {
            if (String.IsNullOrEmpty(sz))
                return defVal;

            int i;
            if (Int32.TryParse(sz, NumberStyles.Integer, CultureInfo.CurrentCulture, out i))
                return i;
            return defVal;
        }

        static readonly Regex rZuluDateLocalWithOffset = new Regex("(?<sign>[-+])(?<hours>\\d{2})(?<minutes>\\d{2})$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        /// <summary>
        /// Parse the specified string, assigning it UTC if it is in 8601 format
        /// </summary>
        /// <param name="sz">The string</param>
        /// <returns>A date time, in UTC if it was a UTC string</returns>
        public static DateTime ParseUTCDate(this string sz)
        {
            if (String.IsNullOrEmpty(sz))
                return DateTime.MinValue;
            else if (sz.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
                return DateTime.Parse(sz, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            else if (rZuluDateLocalWithOffset.IsMatch(sz))
                return DateTime.Parse(sz, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal);
            else
                return DateTime.Parse(sz, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Parse the specified string, assigning it UTC, and using the specified date if it appears to be only time (i.e., hh:mm).
        /// </summary>
        /// <param name="sz">The string</param>
        /// <param name="dtNakedTime">A date time, or just a time.</param>
        /// <returns>The parsed date, if possible, otherwise minvalue</returns>
        static public DateTime ParseUTCDateTime(this string sz, DateTime? dtNakedTime = null)
        {
            if (sz == null)
                throw new ArgumentNullException("sz");

            // if it appears to be only a naked time and a date for the naked time is provided, use that date.
            if (dtNakedTime != null && sz.Length <= 5 && System.Text.RegularExpressions.Regex.IsMatch(sz, "^\\d{0,2}:\\d{2}$", System.Text.RegularExpressions.RegexOptions.Compiled))
            {
                string[] rgszHM = sz.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (rgszHM.Length == 2)
                {
                    int hour;
                    int minute;
                    if (int.TryParse(rgszHM[0], out hour) && int.TryParse(rgszHM[1], out minute))
                        sz = String.Format(CultureInfo.InvariantCulture, "{0}T{1}:{2}Z", dtNakedTime.Value.YMDString(), hour.ToString("00", CultureInfo.InvariantCulture), minute.ToString("00", CultureInfo.InvariantCulture));
                }
            }

            DateTime d;
            if (DateTime.TryParse(sz, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out d))
                return d;

            return DateTime.MinValue;
        }

        /// <summary>
        /// Parses a string into a DateTime, returning defVal if any error
        /// </summary>
        /// <param name="sz">The string to parse</param>
        /// <param name="defVal">The default value</param>
        /// <returns>The parsed result or defVal</returns>
        static public DateTime SafeParseDate(this string sz, DateTime defVal)
        {
            if (String.IsNullOrEmpty(sz))
                return defVal;

            DateTime dt;
            if (DateTime.TryParse(sz, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out dt))
                return dt;

            return defVal;
        }

        /// <summary>
        /// Parses a string into a boolean
        /// </summary>
        /// <param name="szIn">The string to parse</param>
        /// <returns>the parsed value.</returns>
        static public Boolean SafeParseBoolean(this string szIn)
        {
            if (String.IsNullOrEmpty(szIn))
                return false;

            string sz = szIn.ToUpperInvariant().Trim();
            if (sz.Length == 0)
                return false;
            else
            {
                if (sz[0] == 'Y' || sz[0] == 'T' || sz[0] == '1')
                    return true;
                Boolean f;
                if (Boolean.TryParse(sz, out f))
                    return f;
                return false;
            }
        }

        /// <summary>
        /// De-serialize a serialized object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static T DeserializeFromXML<T>(this string xml)
        {
            using (StringReader sr = new StringReader(xml))
            {
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(T));
                return (T)xs.Deserialize(sr);
            }
        }

        /// <summary>
        /// Deserialize a JSON string to an object of type T
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="json">The string in JSON format</param>
        /// <returns>The deserialized object</returns>
        public static T DeserialiseFromJSON<T>(this string json)
        {
            var obj = Activator.CreateInstance<T>();
            using (var memoryStream = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(obj.GetType());
                obj = (T)serializer.ReadObject(memoryStream);
                return obj;
            }
        }

        /// <summary>
        /// Compress a string, returning a byte array
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] Compress(this string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            {
                MemoryStream mso = null;
                MemoryStream result = null;
                try
                {
                    result = mso = new MemoryStream();
                    using (var gs = new System.IO.Compression.GZipStream(mso, System.IO.Compression.CompressionMode.Compress, true))
                    {
                        mso = null; // CA2202
                        msi.CopyTo(gs);
                    }

                    return result.ToArray();
                }
                finally
                {
                    if (mso != null)
                        mso.Dispose();
                }
            }
        }

        /// <summary>
        /// Uncompress a string to a UTF8-8 string
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string Uncompress(this byte[] bytes)
        {
            using (var mso = new MemoryStream())
            {
                MemoryStream msi = null;
                try
                {
                    msi = new MemoryStream(bytes);
                    using (var gs = new System.IO.Compression.GZipStream(msi, System.IO.Compression.CompressionMode.Decompress, true))
                    {
                        msi = null;
                        gs.CopyTo(mso);
                    }
                }
                finally
                {
                    if (msi != null)
                        msi.Dispose();
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        public static int CompareCurrentCultureIgnoreCase(this string sz, string sz2)
        {
            return String.Compare(sz, sz2, StringComparison.CurrentCultureIgnoreCase);
        }

        public static int CompareCurrentCulture(this string sz, string sz2)
        {
            return String.Compare(sz, sz2, StringComparison.CurrentCulture);
        }

        public static int CompareOrdinalIgnoreCase(this string sz, string sz2)
        {
            return String.Compare(sz, sz2, StringComparison.OrdinalIgnoreCase);
        }

        public static int CompareOrdinal(this string sz, string sz2)
        {
            return String.Compare(sz, sz2, StringComparison.Ordinal);
        }

        /// <summary>
        /// Splits a comma separated string of integers into an array of integers.
        /// </summary>
        /// <param name="sz"></param>
        /// <returns></returns>
        public static System.Collections.Generic.IEnumerable<int> ToInts(this string sz)
        {
            System.Collections.Generic.List<int> lst = new System.Collections.Generic.List<int>();
            if (String.IsNullOrEmpty(sz))
                return lst;

            string[] rgsz = sz.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < rgsz.Length; i++)
                lst.Add(Convert.ToInt32(rgsz[i], CultureInfo.InvariantCulture));
            return lst;
        }
        #endregion

        private static Regex regexpWebLink = null;
        private static Regex regexMarkDown = null;

        /// <summary>
        /// Turn links in the string into anchor tags.  Does html escaping first.
        /// Optionally also does _xxx_ => emphasis (italic), *xxx* => strong (bold)
        /// </summary>
        /// <param name="sz"></param>
        /// <param name="fMarkdown">Indicates if you want *xxx*/_xxx_ to become strong/em (respectively)</param>
        /// <returns></returns>
        public static string Linkify(this string sz, bool fMarkdown = true)
        {
            if (regexpWebLink == null)
                regexpWebLink = new Regex("(\\[(?<linkText>[^\\]\\r\\n]*)\\])?\\(?(?<linkRef>https?://([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?)\\)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // avoid injection by escaping explicit HTML elements before linkifying, since linkify itself can't be used in an escaped (i.e., <%: %>) context or it will be escaped.
            sz = sz.EscapeHTML().Replace("&#13;", "\r").Replace("&#10;", "\n");

            sz = regexpWebLink.Replace(sz, (m) => {
                string szReplace = m.Groups["linkText"].Value;
                string szLink = m.Groups["linkRef"].Value;
                string szTrailingPeriod = string.Empty;

                // trailing period is allowed in the URL, but I don't want to linkify it.
                if (szLink.EndsWith(".", StringComparison.Ordinal))
                {
                    szLink = szLink.Substring(0, szLink.Length - 1);
                    szTrailingPeriod = ".";
                }

                if (String.IsNullOrWhiteSpace(szReplace))
                    szReplace = szLink;

                return "<a href=\"" + szLink + "\" target=\"_blank\">" + szReplace + "</a>" + szTrailingPeriod;
            });

            if (fMarkdown)
            {
                if (regexMarkDown == null)
                    regexMarkDown = new Regex("([*_])([^*_\r\n]*)\\1", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

                sz = regexMarkDown.Replace(sz, (m) =>
                {
                    if (m.Groups.Count < 3 || m.Value.Contains("&#13;") || m.Value.Contains("&#10;") || m.Value.Contains("\r") || m.Value.Contains("\n")) // Ignore anything with a newline/carriage return.
                        return m.Value;

                    string szCode = m.Groups[1].Value.CompareOrdinalIgnoreCase("*") == 0 ? "strong" : "em";

                    return String.Format(CultureInfo.CurrentCulture, "<{0}>{1}</{0}>", szCode, m.Groups[2].Value);
                });
            }

            return sz;
        }

        /// <summary>
        /// Escape quotes in a string to be placed in javascript
        /// </summary>
        /// <param name="s">The input string</param>
        /// <returns>The escaped string</returns>
        public static string JavascriptEncode(this string s)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            return s.Replace("'", "\\'").Replace("\"", "\\\"");
        }

        /// <summary>
        /// Returns a string limited to a specific length
        /// </summary>
        /// <param name="sz">The string to limit</param>
        /// <param name="maxLength">The maximum length</param>
        /// <returns>The truncated string or the original string</returns>
        public static string LimitTo(this string sz, int maxLength)
        {
            if (sz == null)
                throw new ArgumentNullException("sz");
            return (sz.Length >= maxLength) ? sz.Substring(0, maxLength - 1) : sz;
        }

        /// <summary>
        /// Fixes up the HtmlDecode function, which doesn't properly decode &apos;
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string UnescapeHTML(this string s)
        {
            return HttpUtility.HtmlDecode(s).Replace("&apos;", "'");
        }

        /// <summary>
        /// Fixes up the HtmlEncode function, which aggressively encodes apostrophes
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string EscapeHTML(this string s)
        {
            return HttpUtility.HtmlEncode(s).Replace("&#39;", "'").Replace("&#9;", string.Empty);
        }

        /// <summary>
        /// Returns the string as a fully resolved absolute url, including scheme and host
        /// </summary>
        /// <param name="s">The relative URL</param>
        /// <param name="Request">The request</param>
        /// <returns>A fully resolved absolute URL</returns>
        public static Uri ToAbsoluteURL(this string s, HttpRequest Request)
        {
            if (Request == null)
                throw new ArgumentNullException("Request");
            return s.ToAbsoluteURL(Request.Url.Scheme, Request.Url.Host);
        }

        public static Uri ToAbsoluteURL(this string s, string scheme, string host)
        {
            if (scheme == null)
                throw new ArgumentNullException("scheme");
            if (host == null)
                throw new ArgumentNullException("host");
            if (String.IsNullOrEmpty(s))
                return null;
            return new Uri(String.Format(CultureInfo.InvariantCulture, "{0}://{1}{2}", scheme, host, VirtualPathUtility.ToAbsolute(s)));
        }
        #endregion

        /// <summary>
        /// Replaces instances of "UTC" with "Custom Time Zone" if you have a non-UTC time specified.
        /// </summary>
        /// <param name="szLabel"></param>
        /// <param name="tz"></param>
        /// <returns></returns>
        public static string IndicateUTCOrCustomTimeZone(this string szLabel, TimeZoneInfo tz)
        {
            return (tz == null || tz.Id.CompareCurrentCultureIgnoreCase(TimeZoneInfo.Utc.Id) == 0) ? szLabel : szLabel.Replace("UTC", Resources.LocalizedText.CustomTimeZone);
        }

        #region Decimal Extensions
        static public string ToHHMM(this decimal d)
        {
            d = Math.Round(d * 60.0M) / 60.0M;
            return String.Format(CultureInfo.CurrentCulture, "{0:#,#0}{2}{1:00}", Math.Truncate(d), (d - Math.Truncate(d)) * 60, System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.TimeSeparator);
        }

        /// <summary>
        /// Returns a new value that is the sum of these two, each rounded to minutes (i.e., ROUND(60*x) / 60).
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        static public decimal AddMinutes(this decimal d1, decimal d2)
        {
            return (Math.Round(d1 * 60) / 60M) + (Math.Round(d2 * 60) / 60M);
        }

        /// <summary>
        /// Determines equality out to a specified # of decimal places.
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <param name="precision"># of decimal places; 2 by default</param>
        /// <returns></returns>
        static public bool EqualsToPrecision(this decimal d1, decimal d2, int precision = 2)
        {
            return Math.Round(d1, precision) == Math.Round(d2, precision);
        }
        #endregion

        #region Integer Extensions
        /// <summary>
        /// Display an unsigned integer in binary bytes
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static string ToBinaryString(this UInt32 i)
        {
            StringBuilder sb = new StringBuilder();
            for (int j = 31; j >= 0; j--)
            {
                sb.Append(((i & (1 << j)) != 0) ? "1" : "0");
                if ((j % 8) == 0)
                    sb.Append(" ");
            }
            return sb.ToString();
        }
        #endregion

        #region object Extensions
        #region Format objects from database to strings
        /// <summary>
        /// Formats a boolean object into a checkmark or an empty string
        /// </summary>
        /// <param name="o">the object (typically a db result)</param>
        /// <returns></returns>
        public static string FormatBoolean(this object o)
        {
            return (o == null || o == System.DBNull.Value || !Convert.ToBoolean(o, CultureInfo.CurrentCulture)) ? string.Empty : "&#x2713;";
        }

        /// <summary>
        /// Formats a boolean object into Yes or an empty string
        /// </summary>
        /// <param name="i">the OBJECT</param>
        /// <returns></returns>
        public static string FormatBooleanInt(this object i)
        {
            string sz = string.Empty;
            try
            {
                if (Convert.ToInt32(i, CultureInfo.InvariantCulture) != 0)
                    sz = "Yes";
            }
            catch (FormatException) { }
            return sz;
        }

        /// <summary>
        /// Format a signature state
        /// </summary>
        /// <param name="i">The signature state object (must be an integer that casts to a signature state</param>
        /// <returns>Empty string, valid, or invalid.</returns>
        public static string FormatSignatureState(this object i)
        {
            try
            {
                LogbookEntry.SignatureState ss = (LogbookEntry.SignatureState)i;
                if (ss != LogbookEntry.SignatureState.None)
                    return ss.ToString();
            }
            catch (InvalidCastException) { }
            return string.Empty;
        }

        /// <summary>
        /// Formats a decimal object into a 0.0 object or an empty string
        /// </summary>
        /// <param name="o">The object</param>
        /// <param name="fIncludeZero">True to return an "0" for zero values.</param>
        /// <returns></returns>
        public static string FormatDecimal(this object o, bool fUseHHMM, bool fIncludeZero = false)
        {
            if (o == null || o == System.DBNull.Value) return string.Empty;
            Decimal d = Convert.ToDecimal(o, CultureInfo.CurrentCulture);
            return (d == 0.0M && !fIncludeZero) ? string.Empty : (fUseHHMM ? d.ToHHMM() : d.ToString("#,#0.0#", CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Formats an integer object into a string or, if 0, an empty string
        /// </summary>
        /// <param name="o">The object (typically a db result)</param>
        /// <returns>The resulting string</returns>
        public static string FormatInt(this object o)
        {
            if (o == null || o == System.DBNull.Value)
                return string.Empty;
            int i = Convert.ToInt32(o, CultureInfo.InvariantCulture);
            return (i == 0) ? "" : i.ToString("#,##0", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Format the object as a short date string
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string FormatDate(this object o)
        {
            if (o == null || o == System.DBNull.Value)
                return string.Empty;
            DateTime dt = (o is DateTime) ? (DateTime)o : DateTime.Parse(o.ToString(), CultureInfo.InvariantCulture);
            return dt.ToShortDateString();
        }

        /// <summary>
        /// Formats a datetime in zulu (UTC) notation, if it has a value.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string FormatDateZulu(this object o)
        {
            if (!(o is DateTime))
                return string.Empty;

            DateTime dt = (DateTime)o;

            return (dt.HasValue()) ? dt.ToString("u", CultureInfo.CurrentCulture) : string.Empty;
        }

        /// <summary>
        /// Format the object as a date string in yyyy-MM-dd format, or else an empty string if not a valid date
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string FormatOptionalInvariantDate(this object o)
        {
            if (o == null || o == System.DBNull.Value)
                return string.Empty;
            DateTime dt = (o is DateTime) ? (DateTime)o : DateTime.Parse(o.ToString(), CultureInfo.InvariantCulture);
            return dt.HasValue() ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
        }

        #endregion

        #region Serialization/deserialization
        /// <summary>
        /// Get a string representation of a serialized object, for debugging
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string SerializeXML(this object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(obj.GetType());

            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                serializer.Serialize(writer, obj);

                return writer.ToString();
            }
        }
        #endregion
        #endregion

        #region HttpRequest and HttpClient Extensions
        /// <summary>
        /// Determines if this is a known mobile device.  Tablets are NOT considered mobile; use IsMobileDeviceOrTablet
        /// </summary>
        /// <param name="r">The HTTPRequest object</param>
        /// <returns>True if it's known</returns>
        public static Boolean IsMobileDevice(this HttpRequest r)
        {
            if (r == null || r.UserAgent == null)
                return false;

            string s = r.UserAgent.ToUpperInvariant();

            return !s.Contains("IPAD") && // iPad is NOT mobile, as far as I'm concerned.
            (r.Browser.IsMobileDevice || s.Contains("IPHONE") ||
              s.Contains("PPC") ||
              s.Contains("WINDOWS CE") ||
              s.Contains("BLACKBERRY") ||
              s.Contains("OPERA MINI") ||
              s.Contains("MOBILE") ||
              s.Contains("PARLM") ||
              s.Contains("PORTABLE"));
        }

        /// <summary>
        /// IsMobileDevice OR iPad OR Android
        /// </summary>
        /// <param name="r">The HTTPRequest object</param>
        /// <returns>True if it's a mobile device or a tablet</returns>
        public static Boolean IsMobileDeviceOrTablet(this HttpRequest r)
        {
            if (r == null || String.IsNullOrEmpty(r.UserAgent))
                return false;

            return (IsMobileDevice(r) || r.UserAgent.ToUpperInvariant().Contains("IPAD") || r.UserAgent.ToUpperInvariant().Contains("ANDROID"));
        }

        /// <summary>
        /// Enables an async PATCH method.
        /// Adapted from http://stackoverflow.com/questions/26218764/patch-async-requests-with-windows-web-http-httpclient-class
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestUri"></param>
        /// <param name="iContent"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> PatchAsync(this HttpClient client, Uri requestUri, HttpContent iContent)
        {
            var method = new HttpMethod("PATCH");
            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = iContent
            };

            return await client.SendAsync(request);
        }
        #endregion

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
        public static bool IsCurrent(this FlightCurrency.CurrencyState cs) { return cs != FlightCurrency.CurrencyState.NotCurrent; }

        /// <summary>
        /// Indicates whether a particular column type can be graphed.
        /// </summary>
        /// <param name="kct"></param>
        /// <returns></returns>
        public static bool CanGraph(this Telemetry.KnownColumnTypes kct) { return kct != Telemetry.KnownColumnTypes.ctLatLong && kct != Telemetry.KnownColumnTypes.ctString; }
        #endregion
    }

    /// <summary>
    /// Enable DateTime to be set via either DateTime or unix epoch
    /// </summary>
    public class MFBDateTimeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            long t;
            DateTime dt;

            if (reader.Value is Int32 || reader.Value is Int64)
                return DateTimeOffset.FromUnixTimeSeconds((Int64)reader.Value).UtcDateTime;

            if (long.TryParse((string)reader.Value, out t))
                return DateTimeOffset.FromUnixTimeSeconds(t).UtcDateTime;
            else if (DateTime.TryParse((string)reader.Value, out dt))
                return dt;
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}