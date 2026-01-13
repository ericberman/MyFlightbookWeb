using MyFlightbook.SharedUtility.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
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
        /// Treats a datetime as nullable, converting it to a DateTime?.  If HasValue is true, has the date, else is null
        /// </summary>
        /// <param name="dt"></param>
        /// <returns>the datetime, or null</returns>
        public static DateTime? AsNulluble(this DateTime dt)
        {
            return dt.HasValue() ? dt : (DateTime?)null;
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
            return dt.HasValue() ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
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

        /// <summary>
        /// Returns the number of milliseconds since the unix reference date.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static double UnixMilliseconds(this DateTime dt)
        {
            return dt.Subtract(dtUnixReferenceDate).TotalMilliseconds;
        }

        /// <summary>
        /// Returns the number of seconds since the unix reference date.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static double UnixSeconds(this DateTime dt)
        {
            return dt.Subtract(dtUnixReferenceDate).TotalSeconds;
        }

        public static DateTime StripSeconds(this DateTime dt)
        {
            return dt == null || !dt.HasValue() ? DateTime.MinValue : new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind);
        }

        public static string FormattedNowInUtc(this DateTime dt, TimeZoneInfo tzi)
        {
            if (tzi == null)
                throw new ArgumentNullException(nameof(tzi));

            return TimeZoneInfo.ConvertTimeFromUtc(dt, tzi).UTCDateFormatString();
        }

        public static DateTime ConvertFromTimezone(this DateTime dt, TimeZoneInfo tz)
        {
            if (tz == null)
                throw new ArgumentNullException(nameof(tz));

            switch (dt.Kind)
            {
                default:
                case DateTimeKind.Unspecified:
                    return TimeZoneInfo.ConvertTimeToUtc(dt, tz);
                case DateTimeKind.Utc:
                    return dt;
                case DateTimeKind.Local:
                    return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), tz);
            }
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
                throw new ArgumentNullException(nameof(s));
            // Issue #974: Norway and some other countries no longer use ":" as the standard time separator, but pilots typically still do.
            // E.g., 72 minutes is 1.12, not 1:12
            // So we'll *emit* ".", but consume "." AND ":".  E.g., we'll display 1.12, but parse 1.12 AND 1:12.  
            string[] rgSz = s.Split(new string[] { System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.TimeSeparator, ":" }, StringSplitOptions.None);
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
            catch (Exception ex) when (ex is FormatException)
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

            if (Decimal.TryParse(sz, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal d))
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

            if (double.TryParse(sz, NumberStyles.Any, CultureInfo.CurrentCulture, out double d))
                return d;

            return defVal;
        }

        /// <summary>
        /// Parses a string into an Int32, returning defVal if any error
        /// </summary>
        /// <param name="sz">The string to parse</param>
        /// <param name="defVal">The default value</param>
        /// <returns>The parsed result or defVal</returns>
        static public int SafeParseInt(this string sz, Int32 defVal = 0)
        {
            if (String.IsNullOrEmpty(sz))
                return defVal;

            if (int.TryParse(sz, NumberStyles.Integer, CultureInfo.CurrentCulture, out int i))
                return i;
            return defVal;
        }

        static readonly LazyRegex rZuluDateLocalWithOffset = new LazyRegex("(?<sign>[-+])(?<hours>\\d{2})(?<minutes>\\d{2})$", RegexOptions.Singleline | RegexOptions.CultureInvariant);
        static readonly LazyRegex rZuluDateTime = new LazyRegex("^(?<year>\\d{4})[-/.](?<month>\\d{2})[-/.](?<day>\\d{2})T(?<hour>\\d{2})[-:.](?<minute>\\d{2})[-:.](?<seconds>\\d{2}([.,]\\d*)?)Z$", RegexOptions.Singleline | RegexOptions.CultureInvariant);

        /// <summary>
        /// Parse the specified string, assigning it UTC if it is in 8601 format
        /// </summary>
        /// <param name="sz">The string</param>
        /// <returns>A date time, in UTC if it was a UTC string</returns>
        public static DateTime ParseUTCDate(this string sz)
        {
            MatchCollection mc;
            if (String.IsNullOrEmpty(sz))
                return DateTime.MinValue;
            else if ((mc = rZuluDateTime.Matches(sz)) != null && mc.Count == 1)
            {
                GroupCollection gc = mc[0].Groups;
                double seconds = Convert.ToDouble(gc["seconds"].Value, CultureInfo.InvariantCulture);
                int wholeseconds = (int)Math.Truncate(seconds);
                int milliseconds = (int)Math.Truncate((seconds - wholeseconds) * 1000);
                return new DateTime(Convert.ToInt32(gc["year"].Value, CultureInfo.InvariantCulture), Convert.ToInt32(gc["month"].Value, CultureInfo.InvariantCulture), Convert.ToInt32(gc["day"].Value, CultureInfo.InvariantCulture),
                    Convert.ToInt32(gc["hour"].Value, CultureInfo.InvariantCulture), Convert.ToInt32(gc["minute"].Value, CultureInfo.InvariantCulture), wholeseconds, milliseconds, DateTimeKind.Utc);
            }
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
        /// <param name="timeZone">The time zone the date was expressed in, UTC is assumed if not provided.</param>
        /// <returns>The parsed date, in UTC, if possible, otherwise minvalue</returns>
        static public DateTime ParseUTCDateTime(this string sz, DateTime? dtNakedTime = null, TimeZoneInfo timeZone = null)
        {
            if (sz == null)
                throw new ArgumentNullException(nameof(sz));

            // if it appears to be only a naked time and a date for the naked time is provided, use that date.
            if (dtNakedTime != null && sz.Length <= 5 && RegexUtility.NakedTime.IsMatch(sz))
            {
                string[] rgszHM = sz.Split(timeSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (rgszHM.Length == 2)
                {
                    if (int.TryParse(rgszHM[0], out int hour) && int.TryParse(rgszHM[1], out int minute))
                        sz = String.Format(CultureInfo.InvariantCulture, "{0}T{1}:{2}Z", dtNakedTime.Value.YMDString(), hour.ToString("00", CultureInfo.InvariantCulture), minute.ToString("00", CultureInfo.InvariantCulture));
                }
            }

            if (DateTime.TryParse(sz, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out DateTime d))
            {
                if (timeZone != null && d.HasValue())
                    d = DateTime.SpecifyKind(d, DateTimeKind.Local).ConvertFromTimezone(timeZone);
                return d;
            }

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

            if (DateTime.TryParse(sz, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime dt))
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
                if (sz[0] == 'Y' || sz[0] == 'T' || sz[0] == '1' || sz.CompareCurrentCultureIgnoreCase("DH") == 0)
                    return true;
                if (Boolean.TryParse(sz, out bool f))
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
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (StringReader sr = new StringReader(xml))
            {
                using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(sr, new System.Xml.XmlReaderSettings() { XmlResolver = null }))
                {
                    return (T)xs.Deserialize(reader);
                }
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
                using (MemoryStream mso = new MemoryStream())
                {
                    using (var gs = new System.IO.Compression.GZipStream(mso, System.IO.Compression.CompressionMode.Compress, true))
                    {
                        msi.CopyTo(gs);
                    }

                    return mso.ToArray();
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
                using (MemoryStream msi = new MemoryStream(bytes))
                {
                    using (var gs = new System.IO.Compression.GZipStream(msi, System.IO.Compression.CompressionMode.Decompress, true))
                    {
                        gs.CopyTo(mso);
                    }
                }
                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        /// <summary>
        /// Convert to base 64, but using "-" and "_" in place of "+" and "/" for URL safety, especially since HttpUtility.URLEncode doesn't properly handle these (e.g., "+" is left alone, so it becomes a space when read)
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToSafeBase64(this byte[] bytes)
        {
            return Convert.ToBase64String(bytes).Replace('=', '.').Replace('+', '-').Replace('/', '_');
        }

        /// <summary>
        /// Convert from base 64, but substituting "+" and "/" in place of "-" and "/"; i.e., must be paired with ToSafeBase64
        /// </summary>
        /// <param name="sz"></param>
        /// <returns></returns>
        public static byte[] FromSafeBase64(this string sz)
        {
            return Convert.FromBase64String((sz ?? string.Empty).Replace('.', '=').Replace('-', '+').Replace('_', '/'));
        }

        /// <summary>
        /// Compresses a string and converts it to base 64 in a manner that does NOT require URL encoding so that it can be passed as a parameter in a URL
        /// </summary>
        /// <param name="sz"></param>
        /// <returns></returns>
        public static string ToSafeParameter(this string sz)
        {
            return sz?.Compress().ToSafeBase64() ?? string.Empty;
        }

        /// <summary>
        /// Reads a string that has been compressed and encoded with ToSafeParameter
        /// </summary>
        /// <param name="sz"></param>
        /// <returns></returns>
        public static string FromSafeParameter(this string sz)
        {
            return sz?.FromSafeBase64().Uncompress() ?? string.Empty;
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
        public static IEnumerable<int> ToInts(this string sz)
        {
            List<int> lst = new List<int>();
            if (String.IsNullOrEmpty(sz))
                return lst;

            string[] rgsz = sz.Split(commaSeparator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < rgsz.Length; i++)
            {
                if (int.TryParse(rgsz[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int val))
                    lst.Add(val);
            }
            return lst;
        }
        #endregion

        #region MarkDown
        private static Regex regexpWebLink;
        private static Regex regexMarkDown;

        private static string MarkupNonLinkedText(string sz)
        {
            if (String.IsNullOrEmpty(sz))
                return sz;

            if (regexMarkDown == null)
                regexMarkDown = new Regex("([*_])([^*_\r\n]*)\\1", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

            sz = regexMarkDown.Replace(sz, (m) =>
            {
                if (m.Groups.Count < 3 || m.Value.Contains("&#13;") || m.Value.Contains("&#10;") || m.Value.Contains("\r") || m.Value.Contains("\n")) // Ignore anything with a newline/carriage return.
                    return m.Value;

                string szCode = m.Groups[1].Value.CompareOrdinalIgnoreCase("*") == 0 ? "strong" : "em";

                // let two asterisks == an asterisk and/or two underscores == an underscore
                return (String.IsNullOrEmpty(m.Groups[2].Value)) ? m.Groups[1].Value : String.Format(CultureInfo.CurrentCulture, "<{0}>{1}</{0}>", szCode, m.Groups[2].Value);
            });
            return sz;
        }

        private static void AddMarkedLines(List<string> lst, string sz, bool fMarkdown)
        {
            if (String.IsNullOrEmpty(sz))
                return;
            if (lst == null)
                throw new ArgumentNullException(nameof(lst));

            if (fMarkdown)
            {
                string[] rgLines = sz.Split(newlineSeparators, StringSplitOptions.None);
                // Preserve the line breaks by doing the markup in-line
                for (int i = 0; i < rgLines.Length; i++)
                    rgLines[i] = MarkupNonLinkedText(rgLines[i]);

                lst.Add(String.Join("\r\n", rgLines));
            }
            else
                lst.Add(sz);
        }

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

            List<string> lstSegs = new List<string>();
            // Break up into segments: First by hyperlinks, then by lines.  We don't markdown the hyperlink because it can contain "_" and "*"
            MatchCollection mc = regexpWebLink.Matches(sz);
            int index = 0;
            foreach (Match m in mc)
            {
                // Catch up Non-linked text
                if (m.Index > index)
                    AddMarkedLines(lstSegs, sz.Substring(index, m.Index - index), fMarkdown);

                // what remains is the hyperlink
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
                else if (fMarkdown)
                    szReplace = MarkupNonLinkedText(szReplace);

                lstSegs.Add("<a href=\"" + szLink + "\" target=\"_blank\">" + szReplace + "</a>" + szTrailingPeriod);

                // Advance index to the end of the match
                index = m.Index + m.Value.Length; ;
            }

            // Finally, catch up any trailing text
            if (sz.Length > index)
                AddMarkedLines(lstSegs, sz.Substring(index, sz.Length - index), fMarkdown);

            return String.Join(string.Empty, lstSegs);
        }
        #endregion 

        /// <summary>
        /// Escapes MySql wildcards (% and _).
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string EscapeMySQLWildcards(this string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            return s.Replace("%", "\\%").Replace("_", "\\_");
        }

        /// <summary>
        /// Convert filesystem-style wildcards (* and ?) to MySQL wildcards (% and _).
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ConvertToMySQLWildcards(this string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            return s.Replace("*", "%").Replace("?", "_");
        }

        /// <summary>
        /// Converts a string containing "?" and "*" into a regex with . and .*, respectively, escaping everything else
        /// </summary>
        /// <param name="s"></param>
        /// <returns>A safe regex to use that includes wildcards</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ConvertToRegexWildcards(this string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            const string szSingleCharWildcard = "\b\b\b\b";
            const string szMultiCharWildcard = "\v\v\v\v";

            return Regex.Escape(s.Replace("?", szSingleCharWildcard).Replace("*", szMultiCharWildcard))
                .Replace(szSingleCharWildcard, ".").
                Replace(szMultiCharWildcard, ".*");
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
                throw new ArgumentNullException(nameof(sz));
            return (sz.Length >= maxLength) ? sz.Substring(0, maxLength - 1) : sz;
        }

        /// <summary>
        /// Fixes up the HtmlDecode function, which doesn't properly decode &apos;
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string UnescapeHTML(this string s)
        {
            return WebUtility.HtmlDecode(s).Replace("&apos;", "'");
        }

        /// <summary>
        /// Fixes up the HtmlEncode function, which aggressively encodes apostrophes
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static string EscapeHTML(this string s)
        {
            return WebUtility.HtmlEncode(s).Replace("&#39;", "'").Replace("&#9;", string.Empty);
        }

        /// <summary>
        /// Converts the string to an absolute url, NOT including scheme and host
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ToAbsolute(this string s)
        {
            return util.RequestContext.RelativeToAbsolute(s);
        }

        public static Uri ToAbsoluteURL(this string s, string scheme, string host, int port = 80)
        {
            if (scheme == null)
                throw new ArgumentNullException(nameof(scheme));
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (String.IsNullOrEmpty(s))
                return null;
            return new Uri(String.Format(CultureInfo.InvariantCulture, "{0}://{1}{2}{3}", scheme, host, (port == 80 || port == 443) ? string.Empty : String.Format(CultureInfo.InvariantCulture, ":{0}", port), util.RequestContext.RelativeToAbsolute(s)));
        }

        public static Uri ToAbsoluteBrandedUri(this string s, Brand brand = null, int port = 80)
        {
            return s.ToAbsoluteURL("https", (brand ?? Branding.CurrentBrand).HostName, port);
        }

        public static Uri ToAbsoluteURL(this string s, Uri originalUri)
        {
            if (originalUri == null)
                throw new ArgumentNullException(nameof(originalUri));
            return new Uri(String.Format(CultureInfo.InvariantCulture, "{0}://{1}{2}{3}", originalUri.Scheme, originalUri.Host, (originalUri.Port == 80 || originalUri.Port == 443) ? string.Empty : String.Format(CultureInfo.InvariantCulture, ":{0}", originalUri.Port), util.RequestContext.RelativeToAbsolute(s)));
        }

        public static string MapAbsoluteFilePath(this string s)
        {
            return util.RequestContext.RelativeToAbsoluteFilePath(s);
        }

        /// <summary>
        /// Replaces instances of "UTC" with "Custom Time Zone" if you have a non-UTC time specified.
        /// </summary>
        /// <param name="szLabel"></param>
        /// <param name="tz"></param>
        /// <returns></returns>
        public static string IndicateUTCOrCustomTimeZone(this string szLabel, TimeZoneInfo tz)
        {
            if (szLabel == null)
                throw new ArgumentNullException(nameof(szLabel));
            return (tz == null || tz.Id.CompareCurrentCultureIgnoreCase(TimeZoneInfo.Utc.Id) == 0) ? szLabel : szLabel.Replace("UTC", SharedUtilityResources.CustomTimeZone);
        }

        /// <summary>
        /// Adds the specified CSS string to a list of CSS strings, optionally replacing existing ones
        /// </summary>
        /// <param name="sz"></param>
        /// <param name="rgReplace">Classes to replace, if present (these are all removed)</param>
        /// <param name="szNew">The new CSS class</param>
        /// <param name="fAppend">If true, appends the class, otherwise pre-pends it.</param>
        /// <returns></returns>
        public static string ReplaceCSSClasses(this string sz, IEnumerable<string> rgReplace, string szNew, bool fAppend)
        {
            if (String.IsNullOrEmpty(sz))
                return szNew;

            List<string> lst = new List<string>(sz.Split(spaceSeparator, StringSplitOptions.RemoveEmptyEntries));
            if (rgReplace != null)
            {
                foreach (string szReplace in rgReplace)
                    lst.Remove(szReplace);
            }

            if (szNew != null)
            {
                if (fAppend)
                    lst.Add(szNew);
                else
                    lst.Insert(0, szNew);
            }

            return String.Join(" ", lst);
        }

        /// <summary>
        /// Returns the UTF8 byte array for the string.
        /// </summary>
        /// <param name="sz"></param>
        /// <returns></returns>
        public static byte[] UTF8Bytes(this string sz)
        {
            return Encoding.UTF8.GetBytes(sz ?? string.Empty);
        }

        public static string[] SplitSpaces(this string sz)
        {
            return (sz ?? string.Empty).Split(spaceSeparator, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string[] SplitNewlines(this string sz)
        {
            return (sz ?? string.Empty).Split(newlineSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string[] SplitCommas(this string sz)
        {
            return (sz ?? string.Empty).Split(commaSeparator, StringSplitOptions.RemoveEmptyEntries);
        }
        #endregion

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
        /// <param name="roundingUnit">Quantization factor - 60 = minute, 100 = 0.01 hours</param>
        /// <returns></returns>
        static public decimal AddMinutes(this decimal d1, decimal d2, int roundingUnit)
        {
            if (roundingUnit <= 0)
                throw new ArgumentOutOfRangeException(nameof(roundingUnit), "quantFactor must be a positive integer");
            return (Math.Round(d1 * roundingUnit) / (decimal) roundingUnit) + (Math.Round(d2 * roundingUnit) / (decimal) roundingUnit);
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

        /// <summary>
        /// Converts the decimal hours to a whole number of minutes
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        static public int ToMinutes(this decimal d)
        {
            return (int) Math.Round(d * 60);
        }

        /// <summary>
        /// Converts seconds (or milliseconds) since a reference unix date (Jan 1, 1970).  The system will assume seconds unless that's more than 5 days in the future.
        /// </summary>
        /// <param name="i">Number of seconds (milliseconds)</param>
        /// <returns></returns>
        public static DateTime DateFromUnixSeconds(this double i)
        {
            // check for whole seconds - if that yields a date more than 5 days in the future, we can assume milliseconds
            // Since this can be out of range for datetime, compare to Jan 1 2100 first; that's an easy "milliseconds" call.
            // Jan 1 2100 is 4102444800000
            return dtUnixReferenceDate.AddSeconds(i > 4102444800 || dtUnixReferenceDate.AddSeconds(i).CompareTo(DateTime.UtcNow.AddDays(5)) > 0 ? i / 1000 : i);
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
                    sb.Append(' ');
            }
            return sb.ToString();
        }

        private static readonly DateTime dtUnixReferenceDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private static readonly char[] timeSeparator = new char[] { ':' };
        private static readonly char[] commaSeparator = new char[] { ',' };
        private static readonly string[] newlineSeparators = new string[] { "\r\n", "\r", "\n"  };
        private static readonly char[] spaceSeparator = new char[] { ' ' };

        /// <summary>
        /// Converts seconds (or milliseconds) since a reference unix date (Jan 1, 1970).  The system will assume seconds unless that's more than 5 days in the future.
        /// </summary>
        /// <param name="i">Number of seconds (milliseconds)</param>
        /// <returns></returns>
        public static DateTime DateFromUnixSeconds(this Int64 i)
        {
            // check for whole seconds - if that yields a date more than 5 days in the future, we can assume milliseconds
            return dtUnixReferenceDate.AddSeconds(dtUnixReferenceDate.AddSeconds(i).CompareTo(DateTime.UtcNow.AddDays(5)) > 0 ? i / 1000 : i);
        }

        /// <summary>
        /// Converts the string to a nicely formatted string in the current culture, using thousands separators and at least one digit.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static string PrettyString(this int i)
        {
            return i.ToString("#,##0", CultureInfo.CurrentCulture);
        }
        #endregion

        #region Boolean extensions
        /// <summary>
        /// For HTML checkboxes and such - returns "checked" if true
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string ToChecked(this bool b)
        {
            return b ? "checked" : string.Empty;
        }

        /// <summary>
        /// For HTML select and such - returns "selected" if true.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string ToSelected(this bool b)
        {
            return b ? "selected" : string.Empty;
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
            catch (Exception ex) when (ex is FormatException) { }
            return sz;
        }

        public const string keyDecimalSettings = "prefDecimalDisplay";  // adaptive, single, or double digit precision

        /// <summary>
        /// Formats a decimal object into a 0.0 object or an empty string
        /// </summary>
        /// <param name="o">The object</param>
        /// <param name="fIncludeZero">True to return an "0" for zero values.</param>
        /// <param name="fUseHHMM">True for hh:mm format</param>
        /// <returns></returns>
        public static string FormatDecimal(this object o, bool fUseHHMM, bool fIncludeZero = false)
        {
            if (o == null || o == System.DBNull.Value) return string.Empty;
            Decimal d = Convert.ToDecimal(o, CultureInfo.CurrentCulture);

            // See if this user has a decimal preference
            // This is stored in the current session.  It's a bit of a hack because hhmm vs. decimal is deeply embedded throughout the system, and in many cases there's no way to pass down the context if it's an explicit property.
            DecimalFormat df = DecimalFormat.Adaptive;  // default
            df = (DecimalFormat) (util.RequestContext?.GetSessionValue(keyDecimalSettings) ?? df);

            return (d == 0.0M && !fIncludeZero) ? string.Empty : (fUseHHMM ? d.ToHHMM() : d.ToString(df == DecimalFormat.Adaptive ? "#,##0.0#" : (df == DecimalFormat.OneDecimal ? "#,##0.0" : "#,##0.00"), CultureInfo.CurrentCulture));
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
            return (i == 0) ? string.Empty : i.PrettyString();
        }

        public static string FormatMulti(this decimal[] values, bool fUseHHMM, string separator = " / ")
        {
            if (values == null)
                return string.Empty;

            bool fHasValue = false;

            List<string> lst = new List<string>();
            foreach (object value in values)
            {
                if (value == null || value == DBNull.Value)
                    continue;

                decimal d = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                lst.Add(d == 0.0M ? "0" : d.FormatDecimal(fUseHHMM));
                fHasValue = fHasValue || d != 0.0M;
            }

            return fHasValue ? String.Join(separator, lst) : string.Empty;
        }

        public static string FormatMulti(this int[] values, string separator = " / ")
        {
            if (values == null)
                return string.Empty;

            bool fHasValue = false;

            List<string> lst = new List<string>();
            foreach (object value in values)
            {
                if (value == null)
                    continue;

                int i = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                lst.Add(i == 0 ? "0" : i.FormatInt());
                fHasValue = fHasValue || i != 0;
            }

            return fHasValue ? String.Join(separator, lst) : string.Empty;
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
            DateTime dt = (o is DateTime time) ? time : DateTime.Parse(o.ToString(), CultureInfo.InvariantCulture);
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
            DateTime dt = (o is DateTime time) ? time : DateTime.Parse(o.ToString(), CultureInfo.InvariantCulture);
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
                throw new ArgumentNullException(nameof(obj));
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(obj.GetType());

            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                serializer.Serialize(writer, obj);

                return writer.ToString();
            }
        }
        #endregion
        #endregion

        #region Exception Extensions
        /// <summary>
        /// Converts an exception into a detailed string, including any optional information and any inner exceptions.
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <param name="szInfo">Any optional information to add</param>
        /// <returns>A string dump of the exception, including inner exceptions</returns>
        static public string PrettyPrint(this Exception ex, string szInfo = null)
        {
            if (ex == null)
                throw new ArgumentNullException(nameof(ex));

            StringBuilder sb = new StringBuilder(szInfo == null ? string.Empty : szInfo + "\r\n\r\n");
            while (ex != null)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "Message: {0}\r\nSource: {1}\r\n", ex.Message, ex.Source);
                if (ex.TargetSite != null)
                    sb.Append("Target site\r\n" + ex.TargetSite.ToString() + "\r\n\r\n");
                sb.AppendFormat(CultureInfo.InvariantCulture, "Stack trace:\r\n{0}OverallData:\r\n{1}\r\n\r\n", ex.StackTrace, ex.ToString());
                if (ex.Data != null && ex.Data.Keys != null)
                {
                    foreach (string key in ex.Data.Keys)
                    {
                        if (ex.Data[key] != null)
                            sb.AppendFormat(CultureInfo.CurrentCulture, "Data key {0}: {1}\r\n\r\n", key, ex.Data[key].ToString());
                    }
                }

                sb.Append(String.Format(CultureInfo.InvariantCulture, "Occured at: {0} (UTC)", DateTime.Now.ToUniversalTime().ToString("G", CultureInfo.InvariantCulture)) + "\r\n\r\n");
                sb.Append("Moving to next exception down...\r\n\r\n");

                // Assign the next InnerException
                // to drill down to the lowest level exception
                ex = ex.InnerException;
            }

            return sb.ToString();
        }
        #endregion
    }

    /// <summary>
    /// Preferences for displaying non-HHMM decimals
    /// </summary>
    public enum DecimalFormat { 
        Adaptive,       // 3.1 displays as 3.1, 3.12 as 3.12
        OneDecimal,     // 3.1 displays as 3.1, 3.12 as 3.1
        TwoDecimal      // 3.1 displays as 3.10, 3.12 as 3.12
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
                throw new ArgumentNullException(nameof(reader));

            if (reader.Value is Int32 || reader.Value is Int64)
                return DateTimeOffset.FromUnixTimeSeconds((Int64)reader.Value).UtcDateTime;

            if (long.TryParse((string)reader.Value, out long t))
                return DateTimeOffset.FromUnixTimeSeconds(t).UtcDateTime;
            else if (DateTime.TryParse((string)reader.Value, out DateTime dt))
                return dt;
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}