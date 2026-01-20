using MyFlightbook.Charting.Properties;
using MyFlightbook.Geography;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2010-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Charting
{
    public enum KnownColumnTypes { ctInt = 0, ctDec, ctFloat, ctString, ctLatLong, ctDateTime, ctPosition, ctUnixTimeStamp, ctTimeZoneOffset, ctNakedDate, ctNakedTime, ctNakedUTCDate, ctNakedUTCTime };

    public static class KnownColumnNames
    {
        public const string LAT = "LAT";
        public const string LON = "LON";
        public const string POS = "POSITION";
        public const string ALT = "ALT";
        public const string SPEED = "SPEED";
        public const string TZOFFSET = "TZOFFSET";
        public const string SAMPLE = "SAMPLE";
        public const string DATE = "DATE";
        public const string TIME = "TIME";
        public const string TIMEKIND = "TIMEKIND";
        public const string DERIVEDSPEED = "ComputedSpeed";
        public const string COMMENT = "Comment";
        public const string NakedDate = "NakedDate";
        public const string NakedTime = "NakedTime";
        public const string UTCOffSet = "UTC Offset";
        public const string UTCDateTime = "UTC DateTime";
        public const string PITCH = "Pitch";
        public const string ROLL = "Roll";
        public const string HOBBS = "Hobbs";

        /// <summary>
        /// Indicates whether a particular column type can be graphed.
        /// </summary>
        /// <param name="kct"></param>
        /// <returns></returns>
        public static bool CanGraph(this KnownColumnTypes kct) { return kct != KnownColumnTypes.ctLatLong && kct != KnownColumnTypes.ctString; }

        /// <summary>
        /// Gets the c# type that corresponds to the KnownColumnType
        /// </summary>
        /// <param name="kct"></param>
        /// <returns></returns>
        public static Type ColumnDataType(this KnownColumnTypes kct)
        {
            switch (kct)
            {
                case KnownColumnTypes.ctDateTime:
                case KnownColumnTypes.ctUnixTimeStamp:
                case KnownColumnTypes.ctNakedDate:
                case KnownColumnTypes.ctNakedTime:
                case KnownColumnTypes.ctNakedUTCDate:
                case KnownColumnTypes.ctNakedUTCTime:
                    return typeof(DateTime);
                case KnownColumnTypes.ctDec:
                    return typeof(Decimal);
                case KnownColumnTypes.ctLatLong:
                case KnownColumnTypes.ctFloat:
                    return typeof(double);
                case KnownColumnTypes.ctInt:
                case KnownColumnTypes.ctTimeZoneOffset:
                    return typeof(Int32);
                case KnownColumnTypes.ctPosition:
                    return typeof(LatLong);
                case KnownColumnTypes.ctString:
                default:
                    return typeof(string);
            }
        }
    }

    public class KnownColumn
    {
        #region Properties
        public string Column { get; set; }

        public string FriendlyName { get; set; }

        public KnownColumnTypes Type { get; set; }

        public string ColumnAlias { get; set; }

        public string ColumnDescription { get; set; }

        public string ColumnNotes { get; set; }

        /// <summary>
        /// The data table column name to use - enables aliasing
        /// </summary>
        public string ColumnHeaderName
        {
            get { return String.IsNullOrEmpty(ColumnAlias) ? Column : ColumnAlias; }
        }
        #endregion

        #region Object creation
        public KnownColumn(string szColumn, string szFriendlyName, KnownColumnTypes kctType)
        {
            Column = szColumn;
            FriendlyName = szFriendlyName;
            Type = kctType;
            ColumnAlias = ColumnDescription = ColumnNotes = string.Empty;
        }

        public KnownColumn(MySqlDataReader dr)
        {
            if (dr != null)
            {
                Column = dr["RawName"].ToString().ToUpper(CultureInfo.CurrentCulture);
                FriendlyName = dr["FriendlyName"].ToString();
                Type = (KnownColumnTypes)Convert.ToInt32(dr["TypeID"], CultureInfo.InvariantCulture);
                ColumnAlias = util.ReadNullableString(dr, "ColumnName");
                ColumnDescription = util.ReadNullableString(dr, "ColumnDescription");
                ColumnNotes = util.ReadNullableString(dr, "ColumnNotes");
            }
        }
        #endregion

        private readonly static LazyRegex regStripUnits = new LazyRegex("-?\\d*(\\.\\d*)?", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        private readonly static LazyRegex regNakedTime = new LazyRegex("(\\d{1,2}):(\\d{1,2})(?::(\\d{1,2}))?", RegexOptions.CultureInvariant);
        private readonly static LazyRegex regUTCOffset = new LazyRegex("(-)?(\\d{1,2}):(\\d{1,2})", RegexOptions.CultureInvariant);

        private static int ParseToInt(string szNumber)
        {
            if (!Int32.TryParse(szNumber, out int i))
            {
                i = Double.TryParse(szNumber, NumberStyles.Float, CultureInfo.CurrentCulture, out double d) ? (int)d : 0;
            }

            return i;
        }

        private static DateTime ParseToNakedTime(string szValue, bool fUTC = false)
        {
            GroupCollection g = regNakedTime.Match(szValue).Groups;
            if (g.Count > 3)
                return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Convert.ToInt32(g[1].Value, CultureInfo.InvariantCulture), Convert.ToInt32(g[2].Value, CultureInfo.InvariantCulture), String.IsNullOrEmpty(g[3].Value) ? 0 : Convert.ToInt32(g[3].Value, CultureInfo.InvariantCulture), fUTC ? DateTimeKind.Utc : DateTimeKind.Unspecified);
            return DateTime.MinValue;
        }

        private static object ParseUnixTimeStamp(string szValue)
        {
            // UnixTimeStamp, at least in ForeFlight, is # of ms since Jan 1 1970.
            if (double.TryParse(szValue, out double i))
                return i.DateFromUnixSeconds();
            else
                return szValue.ParseUTCDate();
        }

        private static object ParseLatLong(string szValue)
        {
            if (String.IsNullOrEmpty(szValue))
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, ChartingResources.errBadLatLong, string.Empty));

            if (!double.TryParse(szValue, out double d))
                d = new DMSAngle(szValue).Value;

            if (d > 180.0 || d < -180.0 || d == 0.0000)
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, ChartingResources.errBadLatLong, d));

            return d;
        }

        public object ParseToType(string szValue)
        {
            object o;
            try
            {
                switch (Type)
                {
                    case KnownColumnTypes.ctNakedUTCTime:
                    case KnownColumnTypes.ctNakedTime:
                        o = ParseToNakedTime(szValue, Type == KnownColumnTypes.ctNakedUTCTime);
                        break;
                    case KnownColumnTypes.ctDateTime:
                    case KnownColumnTypes.ctNakedDate:
                        o = szValue.ParseUTCDate();
                        break;
                    case KnownColumnTypes.ctNakedUTCDate:
                        o = DateTime.SpecifyKind(Convert.ToDateTime(szValue, CultureInfo.CurrentCulture), DateTimeKind.Utc);
                        break;
                    case KnownColumnTypes.ctUnixTimeStamp:
                        o = ParseUnixTimeStamp(szValue);
                        break;
                    case KnownColumnTypes.ctDec:
                        o = regStripUnits.Match(szValue).Captures[0].Value.SafeParseDecimal();
                        break;
                    case KnownColumnTypes.ctLatLong:
                        o = ParseLatLong(szValue);
                        break;
                    case KnownColumnTypes.ctFloat:
                        o = regStripUnits.Match(szValue).Captures[0].Value.SafeParseDouble();
                        break;
                    case KnownColumnTypes.ctTimeZoneOffset:
                    case KnownColumnTypes.ctInt:
                        if (Type == KnownColumnTypes.ctTimeZoneOffset && String.Compare(ColumnHeaderName, KnownColumnNames.UTCOffSet, StringComparison.OrdinalIgnoreCase) == 0)  // UTC offset is opposite TZOffset and is in hh:mm format
                        {
                            GroupCollection g = regUTCOffset.Match(szValue).Groups;
                            o = (g.Count > 3) ? (String.IsNullOrEmpty(g[1].Value) ? -1 : 1) * (60 * Convert.ToInt32(g[2].Value, CultureInfo.InvariantCulture) + Convert.ToInt32(g[3].Value, CultureInfo.InvariantCulture)) : 0; // note that if there is NO leading minus sign, we need to add it, since we want minutes to add to adjust.
                        }
                        else
                        {
                            int i = ParseToInt(regStripUnits.Match(szValue).Captures[0].Value);
                            if (String.Compare(ColumnAlias, KnownColumnNames.ALT, StringComparison.OrdinalIgnoreCase) == 0 // altitude, make sure it's reasonable
                                && i < -1600)
                                throw new MyFlightbookException(ChartingResources.errBadAlt);
                            o = i;
                        }
                        break;
                    case KnownColumnTypes.ctPosition:
                        o = new LatLong(szValue);
                        break;
                    case KnownColumnTypes.ctString:
                    default:
                        o = szValue;
                        break;
                }
            }
            catch (MyFlightbookException ex)
            {
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Error parsing {0} ({1}) from value {2} - {3}", Column, FriendlyName, szValue, ex.Message), ex);
            }

            return o;
        }

        #region Static utilities
        public static IEnumerable<KnownColumn> GetKnownColumns()
        {
            List<KnownColumn> lst = new List<KnownColumn>();
            DBHelper dbh = new DBHelper("SELECT * FROM FlightDataColumns ORDER BY RawName ASC");
            if (!dbh.ReadRows(
                (comm) => { },
                (dr) => { lst.Add(new KnownColumn(dr)); }))
                throw new MyFlightbookException("Error initializing known column types: " + dbh.LastError);
            return lst;
        }

        #region KnownColumn Management
        /// <summary>
        /// Given a key, returns a KnownColumn for that key.  If unknown, it creates a bogus column with a default type of string
        /// </summary>
        /// <param name="sz">The key for the column</param>
        /// <returns>The KnownColumn for the key</returns>
        public static KnownColumn GetKnownColumn(string sz)
        {
            const string szKnownColumnsCacheKey = "KnownColumnsCache";
            Dictionary<string, KnownColumn> dict = (Dictionary<string, KnownColumn>)util.GlobalCache.Get(szKnownColumnsCacheKey);
            if (dict == null)
            {
                dict = new Dictionary<string, KnownColumn>();
                IEnumerable<KnownColumn> lst = KnownColumn.GetKnownColumns();
                foreach (KnownColumn kc in lst)
                    dict[kc.Column] = kc;
                util.GlobalCache.Set(szKnownColumnsCacheKey, dict, DateTimeOffset.UtcNow.AddHours(1));
            }

            string szKey = sz == null ? string.Empty : sz.ToUpper(CultureInfo.CurrentCulture);
            if (String.IsNullOrEmpty(sz) || !dict.TryGetValue(szKey, out KnownColumn value))
                return new KnownColumn(sz, sz, KnownColumnTypes.ctString);
            return value;
        }
        #endregion

        #endregion
    }
}