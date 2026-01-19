using GeoTimeZone;
using MyFlightbook.Airports;
using MyFlightbook.Charting;
using MyFlightbook.CSV;
using MyFlightbook.Geography;
using MyFlightbook.Geography.SolarTools;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Xml;

/******************************************************
 * 
 * Copyright (c) 2010-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Telemetry
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
        private readonly static LazyRegex regNakedTime = new LazyRegex("(\\d{1,2}):(\\d{1,2})(?::(\\d{1,2}))?",RegexOptions.CultureInvariant);
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
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.errBadLatLong, string.Empty));

            if (!double.TryParse(szValue, out double d))
                d = new DMSAngle(szValue).Value;

            if (d > 180.0 || d < -180.0 || d == 0.0000)
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.errBadLatLong, d));

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
                                throw new MyFlightbookException(Resources.FlightData.errBadAlt);
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

        public static Type ColumnDataType(KnownColumnTypes kct)
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

    /// <summary>
    /// Container for options that can affect how autofill operates
    /// </summary>
    public class AutoFillOptions
    {
        public enum AutoFillTotalOption { None, FlightTime, EngineTime, HobbsTime, BlockTime };
        public enum AutoFillHobbsOption { None, FlightTime, EngineTime, TotalTime };
        public enum NightCritera { EndOfCivilTwilight, Sunset, SunsetPlus15, SunsetPlus30, SunsetPlus60 };
        public enum NightLandingCriteria { SunsetPlus60, Night};

        public enum TimeConversionCriteria { None, Local, Preferred }

        public const int DefaultCrossCountryDistance = 50;
        public const int DefaultTakeoffSpeedKts = 70;
        public const int DefaultLandingSpeedKts = 55;
        public const double FullStopSpeed = 5.0;

        private readonly static int[] _rgSpeeds = { 20, 40, 55, 70, 85, 100 };
        private const int DefaultSpeedIndex = 3;
        private const int SpeedBreakPoint = 50;
        private const int LandingSpeedDifferentialLow = 10;
        private const int LandingSpeedDifferentialHigh = 15;

        #region Constructors
        public AutoFillOptions()
        {
        }

        /// <summary>
        /// returns a new autofill options from another one.
        /// </summary>
        /// <param name="afoSrc"></param>
        public AutoFillOptions(AutoFillOptions afoSrc) : this()
        {
            if (afoSrc != null)
                util.CopyObject(afoSrc, this);
        }

        private const string szKeyPersistedPrefAutoFill = "DefaultAutofillOptions";

        public static AutoFillOptions DefaultOptionsForUser(string szUserName)
        {
            if (String.IsNullOrEmpty(szUserName))
                return new AutoFillOptions();

            return Profile.GetUser(szUserName).GetPreferenceForKey<AutoFillOptions>(szKeyPersistedPrefAutoFill) ?? new AutoFillOptions();
        }

        public void SaveForUser(string szUserName)
        {
            if (String.IsNullOrEmpty(szUserName))
                throw new ArgumentNullException(nameof(szUserName));

            PersistedProfile pf = Profile.GetUser(szUserName);
            pf.SetPreferenceForKey(szKeyPersistedPrefAutoFill, this);
        }
        #endregion

        #region Properties
        /// <summary>
        /// timezone offset from UTC, in minutes
        /// </summary>
        public int TimeZoneOffset { get; set; } = 0;

        /// <summary>
        /// AutoTotal options
        /// </summary>
        public AutoFillTotalOption AutoFillTotal { get; set; } = AutoFillTotalOption.HobbsTime;

        /// <summary>
        /// AutoHobbs options
        /// </summary>
        public AutoFillHobbsOption AutoFillHobbs { get; set; } = AutoFillHobbsOption.EngineTime;

        /// <summary>
        /// By what criteria do we begin accumulating night flight?
        /// </summary>
        public NightCritera Night { get; set; } = NightCritera.EndOfCivilTwilight;

        /// <summary>
        /// By what criteria do we log a night landing?
        /// </summary>
        public NightLandingCriteria NightLanding { get; set; } = NightLandingCriteria.SunsetPlus60;

        /// <summary>
        /// Return the time offset (in minutes) relative to sunset to use for IsWithinNightOffset
        /// </summary>
        public int NightFlightSunsetOffset
        {
            get
            {
                switch (Night)
                {
                    case NightCritera.EndOfCivilTwilight:
                    case NightCritera.Sunset:
                    default:
                        return 0;
                    case NightCritera.SunsetPlus60:
                        return 60;
                    case NightCritera.SunsetPlus15:
                        return 15;
                    case NightCritera.SunsetPlus30:
                        return 30;
                }
            }
        }

        /// <summary>
        /// Threshold for cross-country flight
        /// </summary>
        public double CrossCountryThreshold { get; set; } = DefaultCrossCountryDistance;

        /// <summary>
        /// Speed above which the aircraft is assumed to be flying
        /// </summary>
        public double TakeOffSpeed { get; set; } = DefaultTakeoffSpeedKts;

        /// <summary>
        /// Speed below which the aircraft is assumed to be taxiing or stopped
        /// </summary>
        public double LandingSpeed { get; set; } = DefaultLandingSpeedKts;

        /// <summary>
        /// Include heliports in autodetection?
        /// </summary>
        public bool IncludeHeliports { get; set; } = false;

        /// <summary>
        /// True to plow ahead and continue even if errors are encountered.
        /// </summary>
        public bool IgnoreErrors { get; set; }

        /// <summary>
        /// Indicates if a path should be synthesized if not present (needed to estimate night time, for example)
        /// True by default.
        /// </summary>
        public bool AutoSynthesizePath { get; set; } = true;

        /// <summary>
        /// Indicates whether auto-fill times should be rounded to the nearest 10th of an hour
        /// </summary>
        public bool RoundToTenth { get; set; } = false;

        /// <summary>
        /// Indicates whether to try to convert times to UTC time based on lat/long
        /// </summary>
        [Obsolete("Use TimeConversion Instead")]
        public bool TryLocal { get; set; } = false;

        /// <summary>
        /// How should times be interpreted?  UTC?  Local time?  Or in the user's preferred time zone (using PreferredTimeZone).
        /// </summary>
        [JsonIgnore]
        public TimeConversionCriteria TimeConversion { get; set; } = TimeConversionCriteria.None;

        [JsonIgnore]
        public TimeZoneInfo PreferredTimeZone { get; set; }
        #endregion

        #region testing for night, night landings
        /// <summary>
        /// Determines if the specified date counts as night for logging night flight
        /// </summary>
        /// <param name="sst">The sunrisesunsettimes that contains the date and relevant location</param>
        /// <returns>True if the time sample should be considered night per user options</returns>
        public bool IsNightForFlight(SunriseSunsetTimes sst)
        {
            if (sst == null)
                throw new ArgumentNullException(nameof(sst));

            // short circuit
            if (!sst.IsNight)
                return false;

            switch (Night)
            {
                case AutoFillOptions.NightCritera.EndOfCivilTwilight:
                    return sst.IsFAACivilNight;
                case AutoFillOptions.NightCritera.Sunset:
                    return true;    // Actually sst.IsNight, but we've determined from the above short circuit that it must be true.
                case AutoFillOptions.NightCritera.SunsetPlus15:
                case AutoFillOptions.NightCritera.SunsetPlus30:
                case AutoFillOptions.NightCritera.SunsetPlus60:
                    return sst.IsWithinNightOffset;
            }

            return false;   // should never hit this.
        }
        #endregion

        /// <summary>
        /// Get the default take-off speed
        /// </summary>
        public static int DefaultTakeoffSpeed
        {
            get { return _rgSpeeds[DefaultSpeedIndex]; }
        }

        /// <summary>
        /// Array of default speed values.
        /// </summary>
        public static System.Collections.ObjectModel.ReadOnlyCollection<int> DefaultSpeeds
        {
            get { return new System.Collections.ObjectModel.ReadOnlyCollection<int>(_rgSpeeds); }
        }

        /// <summary>
        /// What is the best landing speed for the given take-off speed?
        /// </summary>
        /// <param name="TOSpeed"></param>
        /// <returns></returns>
        public static int BestLandingSpeedForTakeoffSpeed(int TOSpeed)
        {
            if (TOSpeed >= SpeedBreakPoint)
                return TOSpeed - LandingSpeedDifferentialHigh;
            else
                return Math.Max(TOSpeed - LandingSpeedDifferentialLow, _rgSpeeds[0] - LandingSpeedDifferentialLow);
        }
    }

    public enum DownloadFormat { Original, CSV, KML, GPX };

    public class DataSourceType
    {
        public enum FileType { None, CSV, XML, KML, GPX, Text, NMEA, IGC, Airbly };

        public FileType Type { get; set; }
        public string DefaultExtension { get; set; }
        public string Mimetype { get; set; }

        public string DisplayName
        {
            get
            {
                switch (Type)
                {
                    case FileType.CSV:
                        return Resources.FlightData.dataTypeCSV;
                    case FileType.GPX:
                        return Resources.FlightData.dataTypeGPX;
                    case FileType.KML:
                        return Resources.FlightData.dataTypeKML;
                    case FileType.NMEA:
                        return Resources.FlightData.dataTypeNMEA;
                    case FileType.IGC:
                        return Resources.FlightData.dataTypeIGC;
                    case FileType.Airbly:
                        return Resources.FlightData.dataTypeAirbly;
                    default:
                        return null;
                }
            }
        }

        public DataSourceType(FileType ft, string szExt, string szMimeType)
        {
            Type = ft;
            DefaultExtension = szExt;
            Mimetype = szMimeType;
        }

        /// <summary>
        /// Returns a new parser object suitable for this type of data, if one is available.
        /// </summary>
        public TelemetryParser Parser
        {
            get
            {
                switch (Type)
                {
                    case FileType.CSV:
                        return new CSVTelemetryParser();
                    case FileType.GPX:
                        return new GPXParser();
                    case FileType.KML:
                        return new KMLParser();
                    case FileType.NMEA:
                        return new NMEAParser();
                    case FileType.IGC:
                        return new IGCParser();
                    case FileType.Airbly:
                        return new Airbly();
                    default:
                        return null;
                }
            }
        }

        private readonly static DataSourceType[] KnownTypes = {
            new DataSourceType(FileType.CSV, "CSV", "text/csv"),
            new DataSourceType(FileType.GPX, "GPX", "application/gpx+xml"),
            new DataSourceType(FileType.KML, "KML", "application/vnd.google-earth.kml+xml"),
            new DataSourceType(FileType.Text, "TXT", "text/plain"),
            new DataSourceType(FileType.XML, "XML", "text/xml"),
            new DataSourceType(FileType.NMEA, "NMEA", "text/plain"),
            new DataSourceType(FileType.Airbly, "JSON", "application/json"),
            new DataSourceType(FileType.IGC, "IGC", "text/plain") };

        public static DataSourceType DataSourceTypeFromFileType(FileType ft)
        {
            return KnownTypes.FirstOrDefault<DataSourceType>(dst => dst.Type == ft);
        }

        public static DataSourceType BestGuessTypeFromText(string sz)
        {
            if (String.IsNullOrEmpty(sz))
                return DataSourceTypeFromFileType(FileType.CSV);

            KMLParser kp = new KMLParser();
            GPXParser gp = new GPXParser();

            if (kp.CanParse(sz))
                return DataSourceTypeFromFileType(FileType.KML);
            if (gp.CanParse(sz))
                return DataSourceTypeFromFileType(FileType.GPX);

            if (sz[0] == (char)0xFEFF)  // look for UTF-16 BOM and strip it if needed.
                sz = sz.Substring(1);

            if (TelemetryParser.IsXML(sz))
            {
                if (kp.CanParse(sz))
                    return DataSourceTypeFromFileType(FileType.KML);
                if (gp.CanParse(sz))
                    return DataSourceTypeFromFileType(FileType.GPX);
                return DataSourceTypeFromFileType(FileType.XML);
            }
            else
            {
                if (new NMEAParser().CanParse(sz))
                    return DataSourceTypeFromFileType(FileType.NMEA);

                if (new IGCParser().CanParse(sz))
                    return DataSourceTypeFromFileType(FileType.IGC);

                if (new Airbly().CanParse(sz))
                    return DataSourceTypeFromFileType(FileType.Airbly);

                // Must be CSV or plain text
                // no good way to distinguish CSV from text, at least that I know of.
                return DataSourceTypeFromFileType(FileType.CSV);
            }
        }

        /// <summary>
        /// Can you specify units for the telemetry?  E.g., CSV has ambiguous units, so you can specify them, but KML/GPX units are baked in
        /// </summary>
        public bool CanSpecifyUnitsForTelemetry
        {
            get
            {
                switch (Type)
                {
                    case DataSourceType.FileType.GPX:
                    case DataSourceType.FileType.KML:
                    case DataSourceType.FileType.NMEA:
                    case DataSourceType.FileType.IGC:
                        return false;
                    default:
                        return true;
                }
            }
        }
    }

    /// <summary>
    /// Subclass of datatable that knows what sorts of data it contains
    /// </summary>
    [Serializable]
    public class TelemetryDataTable : DataTable
    {
        #region What information is present?
        /// <summary>
        /// Name for the date column.
        /// </summary>
        public string DateColumn
        {
            get
            {
                if (Columns != null)
                {
                    // Prefer a UTC datetime if present
                    if (Columns[KnownColumnNames.UTCDateTime] != null)
                        return KnownColumnNames.UTCDateTime;
                    if (Columns[KnownColumnNames.DATE] != null)
                        return KnownColumnNames.DATE;
                    if (Columns[KnownColumnNames.TIME] != null)
                        return KnownColumnNames.TIME;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// True if the data has lat/long info (i.e., a path)
        /// </summary>
        /// <returns>True if lat/long info is present.</returns>
        public Boolean HasLatLongInfo
        {
            get { return Columns != null && (Columns[KnownColumnNames.POS] != null || (Columns[KnownColumnNames.LAT] != null && Columns[KnownColumnNames.LON] != null)); }
        }

        public Boolean HasDateTime
        {
            get { return Columns != null && !String.IsNullOrEmpty(DateColumn); }
        }

        public Boolean HasSpeed
        {
            get { return Columns != null && Columns[KnownColumnNames.SPEED] != null; }
        }

        /// <summary>
        /// Is time-zone offset present in the data?
        /// </summary>
        public Boolean HasTimezone
        {
            get { return Columns != null && (Columns[KnownColumnNames.TZOFFSET] != null || Columns[KnownColumnNames.UTCOffSet] != null); }
        }

        /// <summary>
        /// which header to use for timezone offset?  empty string if not present.
        /// </summary>
        public string TimeZoneHeader
        {
            get { return (Columns[KnownColumnNames.TZOFFSET] == null) ? (Columns[KnownColumnNames.UTCOffSet] == null ? string.Empty : KnownColumnNames.UTCOffSet) : KnownColumnNames.TZOFFSET; }
        }

        public bool HasAltitude
        {
            get { return Columns != null && Columns[KnownColumnNames.ALT] != null; }
        }

        public bool HasUTCDateTime
        {
            get { return Columns != null && Columns[KnownColumnNames.UTCDateTime] != null; }
        }
        #endregion

        public TelemetryDataTable() : base() { Locale = CultureInfo.CurrentCulture; }

        protected TelemetryDataTable(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { Locale = CultureInfo.CurrentCulture; }

        /// <summary>
        /// Merges overlapping data from another TelemetryDataTable
        /// </summary>
        /// <param name="src">The source table</param>
        public void MergeWith(DataTable src)
        {
            if (src == null)
                return;

            HashSet<string> hsColumnsThis = new HashSet<string>();
            foreach (DataColumn c in Columns)
                hsColumnsThis.Add(c.ColumnName);

            HashSet<string> hsColumnsSrc = new HashSet<string>();
            foreach (DataColumn c in src.Columns)
                hsColumnsSrc.Add(c.ColumnName);

            hsColumnsThis.IntersectWith(hsColumnsSrc);

            // check for no overlappin columns
            if (hsColumnsThis.Count == 0)
                return;

            foreach (DataRow dr in src.Rows)
            {
                DataRow drNew = this.NewRow();
                foreach (string szColumn in hsColumnsThis)
                    drNew[szColumn] = dr[szColumn];

                Rows.Add(drNew);
            }
        }

        protected LatLong LatLongForRow(DataRow dr, out string htmlDesc)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            htmlDesc = string.Empty;
            if (HasLatLongInfo)
            {
                List<string> lstDesc = new List<string>();

                double lat = 0.0, lon = 0.0;
                LatLong ll = null;

                foreach (DataColumn dc in Columns)
                {
                    bool fLat = (dc.ColumnName.CompareOrdinalIgnoreCase(KnownColumnNames.LAT) == 0);
                    bool fLon = (dc.ColumnName.CompareOrdinalIgnoreCase(KnownColumnNames.LON) == 0);
                    bool fPos = (dc.ColumnName.CompareOrdinalIgnoreCase(KnownColumnNames.POS) == 0);

                    if (fLat)
                        lat = Convert.ToDouble(dr[KnownColumnNames.LAT], CultureInfo.InvariantCulture);

                    if (fLon)
                        lon = Convert.ToDouble(dr[KnownColumnNames.LON], CultureInfo.InvariantCulture);

                    if (fPos)
                        ll = (LatLong)dr[KnownColumnNames.POS];

                    if (!(fLat || fLon || fPos))
                    {
                        object o = dr[dc.ColumnName];
                        if (o != null)
                        {
                            string sz = o.ToString();
                            if (!String.IsNullOrEmpty(sz))
                                lstDesc.Add(String.Format(CultureInfo.InvariantCulture, "{0}: {1}<br />", dc.ColumnName, sz));
                        }
                    }
                }
                if (ll == null && (lat != 0.0 || lon != 0.0))
                    ll = new LatLong(lat, lon);

                if (ll != null)
                {
                    htmlDesc = String.Join("<br />", lstDesc);
                    return ll;
                }
            }

            return null;
        }

        public string RenderHtmlTable()
        {
            bool HasLatLong = HasLatLongInfo;
            string pinRef = "~/images/pushpinsm.png".ToAbsolute();
            bool hasPosition = Columns.Contains(KnownColumnNames.POS);
            using (StringWriter sw = new StringWriter())
            {
                using (HtmlTextWriter tw = new HtmlTextWriter(sw))
                {
                    tw.AddAttribute("cellpadding", "3");
                    tw.AddAttribute("border", "1");
                    tw.AddAttribute("style", "border-collapse: collapse;");
                    tw.RenderBeginTag(HtmlTextWriterTag.Table);
                    tw.RenderBeginTag(HtmlTextWriterTag.Thead);
                    tw.RenderBeginTag(HtmlTextWriterTag.Tr);

                    tw.RenderBeginTag(HtmlTextWriterTag.Th);
                    tw.RenderEndTag();

                    if (hasPosition)
                    {
                        tw.RenderBeginTag(HtmlTextWriterTag.Th);
                        tw.WriteEncodedText(Resources.LogbookEntry.flightDetailsPositionHeader);
                        tw.RenderEndTag();
                    }
                    foreach (DataColumn dc in Columns)
                    {
                        tw.RenderBeginTag(HtmlTextWriterTag.Th);
                        tw.WriteEncodedText(dc.ColumnName);
                        tw.RenderEndTag();
                    }
                    tw.RenderEndTag();  // tr
                    tw.RenderEndTag();  // thead

                    tw.RenderBeginTag(HtmlTextWriterTag.Tbody);
                    foreach (DataRow dr in Rows)
                    {
                        string rowDesc = null;
                        LatLong ll = HasLatLong ? LatLongForRow(dr, out rowDesc) : null;
                        tw.RenderBeginTag(HtmlTextWriterTag.Tr);

                        tw.RenderBeginTag(HtmlTextWriterTag.Td);
                        if (ll != null)
                        {
                            string szZoom = String.Format(CultureInfo.InvariantCulture, "javascript:getMfbMap().gmap.setCenter(new google.maps.LatLng({0}, {1}));getMfbMap().gmap.setZoom(14);", ll.Latitude, ll.Longitude);
                            string szDrop = String.Format(CultureInfo.InvariantCulture, "javascript:dropPin(new google.maps.LatLng({0}, {1}), '{2}');", ll.Latitude, ll.Longitude, rowDesc);
                            string szTip = String.Format(CultureInfo.CurrentCulture, Resources.FlightData.GraphDropPinTip, ll.ToString());

                            tw.AddAttribute("src", pinRef);
                            tw.AddAttribute("style", "height: 20px;");
                            tw.AddAttribute("onclick", szDrop);
                            tw.AddAttribute("title", szTip);
                            tw.RenderBeginTag(HtmlTextWriterTag.Img);
                            tw.RenderEndTag();

                            tw.AddAttribute("style", "cursor: pointer;");
                            tw.AddAttribute("onclick", szZoom);
                            tw.RenderBeginTag(HtmlTextWriterTag.A);
                            tw.WriteEncodedText(Resources.FlightData.ZoomIn);
                            tw.RenderEndTag();
                        }
                        tw.RenderEndTag();

                        if (hasPosition)
                        {
                            tw.AddAttribute("style", "text-align: left");
                            tw.RenderBeginTag(HtmlTextWriterTag.Td);
                            tw.WriteEncodedText(ll.ToDegMinSecString());
                            tw.RenderEndTag();
                        }

                        foreach (DataColumn dc in Columns)
                        {
                            tw.RenderBeginTag(HtmlTextWriterTag.Td);
                            tw.WriteEncodedText(dr[dc.ColumnName].ToString());
                            tw.RenderEndTag();
                        }

                        tw.RenderEndTag();  // tr
                    }
                    tw.RenderEndTag();  // tbody
                    tw.RenderEndTag();  // table
                }
                return sw.ToString();
            }
        }

        public IEnumerable<KnownColumn> YValCandidates
        {
            get
            {
                List<KnownColumn> lstYCols = new List<KnownColumn>();
                foreach (DataColumn dc in Columns)
                {
                    KnownColumn kc = KnownColumn.GetKnownColumn(dc.Caption);
                    if (kc.Type == KnownColumnTypes.ctDec || kc.Type == KnownColumnTypes.ctFloat || kc.Type == KnownColumnTypes.ctInt)
                        lstYCols.Add(kc);
                }
                return lstYCols;
            }
        }

        public IEnumerable<KnownColumn> XValCandidates
        {
            get
            {
                List<KnownColumn> lstXCols = new List<KnownColumn>();
                foreach (DataColumn dc in Columns)
                {
                    KnownColumn kc = KnownColumn.GetKnownColumn(dc.Caption);
                    if (kc.Type.CanGraph())
                        lstXCols.Add(kc);
                }
                return lstXCols;
            }
        }

        public void PopulateGoogleChart(GoogleChartData gcData, string xAxis, string yAxis1, string yAxis2, double y1Scale, double y2Scale, out double max, out double min, out double max2, out double min2)
        {
            if (gcData == null)
                throw new ArgumentNullException(nameof(gcData));

            max = double.MinValue;
            min = double.MaxValue;
            max2 = double.MinValue;
            min2 = double.MaxValue;

            if (yAxis1 == null || yAxis2 == null || xAxis == null)
                return;

            KnownColumn kcX = KnownColumn.GetKnownColumn(xAxis);
            KnownColumn kcY1 = KnownColumn.GetKnownColumn(yAxis1);
            KnownColumn kcY2 = KnownColumn.GetKnownColumn(yAxis2);
            gcData.XLabel = kcX.FriendlyName;
            gcData.YLabel = kcY1.FriendlyName;
            gcData.Y2Label = kcY2.FriendlyName;

            gcData.Clear();

            gcData.XDataType = GoogleChartData.GoogleTypeFromKnownColumnType(kcX.Type);
            gcData.YDataType = GoogleChartData.GoogleTypeFromKnownColumnType(kcY1.Type);
            gcData.Y2DataType = GoogleChartData.GoogleTypeFromKnownColumnType(kcY2.Type);

            if (HasLatLongInfo)
                gcData.ClickHandlerJS = String.Format(CultureInfo.InvariantCulture, "function(row, column, xvalue, value) {{ dropPin(MFBMapsOnPage[0].pathArray[row], xvalue + ': ' + ((column == 1) ? '{0}' : '{1}') + ' = ' + value); }}", yAxis1, yAxis2);

            int tzOffsetColumn = Columns.IndexOf("TZOFFSET");

            foreach (DataRow dr in Rows)
            {
                if (gcData.XDataType == GoogleColumnDataType.date || gcData.XDataType == GoogleColumnDataType.datetime)
                {
                    // Convert it to UTC if it's not already in UTC AND if we have a column that tells us the UTC value
                    DateTime dt = (DateTime) dr[xAxis];
                    if (tzOffsetColumn >= 0 && dt.Kind == DateTimeKind.Unspecified)
                        dt = DateTime.SpecifyKind(dt.AddMinutes((int)dr[tzOffsetColumn]), DateTimeKind.Utc);
                    gcData.XVals.Add(dt);
                }
                else
                    gcData.XVals.Add(dr[xAxis]);

                if (!String.IsNullOrEmpty(yAxis1))
                {
                    object o = dr[yAxis1];
                    if (gcData.YDataType == GoogleColumnDataType.number)
                    {
                        double d = Convert.ToDouble(o, CultureInfo.InvariantCulture);
                        d = double.IsNaN(d) ? 0 : d * y1Scale;
                        max = Math.Max(max, d);
                        min = Math.Min(min, d);
                        o = d;
                    }
                    gcData.YVals.Add(o);
                }
                if (yAxis2.Length > 0 && yAxis2 != yAxis1)
                {
                    object o = dr[yAxis2];
                    if (gcData.Y2DataType == GoogleColumnDataType.number)
                    {
                        double d = Convert.ToDouble(o, CultureInfo.InvariantCulture);
                        d = double.IsNaN(d) ? 0 : d * y2Scale;
                        max2 = Math.Max(max2, d);
                        min2 = Math.Min(min2, d);
                        o = d;
                    }
                    gcData.Y2Vals.Add(o);
                }
            }
            gcData.TickSpacing = 1; // Math.Max(1, m_fd.Data.Rows.Count / 20);
        }
    }

    /// <summary>
    /// Base class for telemetry parsing.
    /// </summary>
    public abstract class TelemetryParser
    {
        /// <summary>
        /// The data that was parsed
        /// </summary>
        public TelemetryDataTable ParsedData { get; set; }

        /// <summary>
        /// Any aircraft tail number that was found.
        /// </summary>
        public string TailNumber { get; set; }

        /// <summary>
        /// Any summary parsing error 
        /// </summary>
        public string ErrorString { get; set; }

        /// <summary>
        /// Examines the data and determines if it can parse
        /// </summary>
        /// <param name="szData"></param>
        /// <returns></returns>
        public abstract bool CanParse(string szData);

        public virtual FlightData.SpeedUnitTypes SpeedUnits
        {
            get { return FlightData.SpeedUnitTypes.Knots; }
        }

        public virtual FlightData.AltitudeUnitTypes AltitudeUnits
        {
            get { return FlightData.AltitudeUnitTypes.Feet; }
        }

        /// <summary>
        /// Parses the specified data, populating the ParsedData and ErrorString properties.
        /// </summary>
        /// <param name="szData">The data to parse</param>
        /// <returns>True for success</returns>
        public abstract bool Parse(string szData);

        protected TelemetryParser()
        {
            ErrorString = string.Empty;
        }

        #region utility functions
        /// <summary>
        /// Creates a data table from a list of Position objects
        /// </summary>
        /// <param name="lst">The list of samples</param>
        protected void ToDataTable(IEnumerable<Position> lst)
        {
            DataTable m_dt = ParsedData;

            m_dt.Clear();
            // Add the headers, based on the 1st sample
            // We'll remove any that are all null
            bool fHasAltitude = false;
            bool fHasTimeStamp = false;
            bool fHasSpeed = false;
            bool fHasDerivedSpeed = false;
            bool fHasComment = false;

            m_dt.Columns.Add(new DataColumn(KnownColumnNames.SAMPLE, typeof(Int32)));
            m_dt.Columns.Add(new DataColumn(KnownColumnNames.LAT, typeof(double)));
            m_dt.Columns.Add(new DataColumn(KnownColumnNames.LON, typeof(double)));
            m_dt.Columns.Add(new DataColumn(KnownColumnNames.ALT, typeof(Int32)));
            m_dt.Columns.Add(new DataColumn(KnownColumnNames.TIME, typeof(DateTime)));
            m_dt.Columns.Add(new DataColumn(KnownColumnNames.TIMEKIND, typeof(int)));
            m_dt.Columns.Add(new DataColumn(KnownColumnNames.SPEED, typeof(double)));
            m_dt.Columns.Add(new DataColumn(KnownColumnNames.DERIVEDSPEED, typeof(double)));
            m_dt.Columns.Add(new DataColumn(KnownColumnNames.COMMENT, typeof(string)));

            int iRow = 0;
            if (lst != null)
            {
                foreach (Position sample in lst)
                {
                    fHasAltitude = fHasAltitude || sample.HasAltitude;
                    fHasTimeStamp = fHasTimeStamp || sample.HasTimeStamp;
                    fHasSpeed = fHasSpeed || (sample.HasSpeed && sample.TypeOfSpeed == Position.SpeedType.Reported);
                    fHasDerivedSpeed = fHasDerivedSpeed || (sample.HasSpeed && sample.TypeOfSpeed == Position.SpeedType.Derived);
                    fHasComment = fHasComment || !String.IsNullOrEmpty(sample.Comment);

                    DataRow dr = m_dt.NewRow();
                    dr[KnownColumnNames.SAMPLE] = iRow++;
                    dr[KnownColumnNames.LAT] = sample.Latitude;
                    dr[KnownColumnNames.LON] = sample.Longitude;
                    dr[KnownColumnNames.ALT] = sample.HasAltitude ? (int)sample.Altitude : 0;
                    dr[KnownColumnNames.TIME] = sample.HasTimeStamp ? sample.Timestamp : DateTime.MinValue;
                    dr[KnownColumnNames.TIMEKIND] = sample.HasTimeStamp ? (int)sample.Timestamp.Kind : (int)DateTimeKind.Unspecified;
                    dr[KnownColumnNames.DERIVEDSPEED] = (sample.HasSpeed && sample.TypeOfSpeed == Position.SpeedType.Derived) ? sample.Speed : 0.0;
                    dr[KnownColumnNames.SPEED] = (sample.HasSpeed && sample.TypeOfSpeed == Position.SpeedType.Reported) ? sample.Speed : 0.0;
                    dr[KnownColumnNames.COMMENT] = sample.Comment;

                    m_dt.Rows.Add(dr);
                }
            }

            // Remove any unused columns
            if (!fHasAltitude)
                m_dt.Columns.Remove(KnownColumnNames.ALT);
            if (!fHasTimeStamp)
                m_dt.Columns.Remove(KnownColumnNames.TIME);
            if (!fHasDerivedSpeed)
                m_dt.Columns.Remove(KnownColumnNames.DERIVEDSPEED);
            if (!fHasSpeed)
                m_dt.Columns.Remove(KnownColumnNames.SPEED);
            if (!fHasComment)
                m_dt.Columns.Remove(KnownColumnNames.COMMENT);
        }

        /// <summary>
        /// Quick check for xml
        /// </summary>
        /// <param name="sz">The string to test</param>
        /// <returns>True if it appears to be XML</returns>
        public static bool IsXML(string sz)
        {
            return sz != null && sz.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase);
        }
        #endregion
    }

    /// <summary>
    /// Summary description for FlightData
    /// </summary>
    public class FlightData : IDisposable
    {
        private TelemetryDataTable m_dt { get; set; }
        private string m_szError { get; set; }

        public enum AltitudeUnitTypes { Feet, Meters };
        public enum SpeedUnitTypes { Knots, MilesPerHour, MetersPerSecond, FeetPerSecond, KmPerHour };

        #region IDisposable Implementation
        private bool disposed; // to detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    m_dt?.Dispose();
                m_dt = null;

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~FlightData()
        {
            Dispose(false);
        }
        #endregion

        #region Properties
        /// <summary>
        /// The FlightID for this data, if any.
        /// </summary>
        public int FlightID { get; set; }

        /// <summary>
        /// Any Altitude transformation (conversion factor)
        /// </summary>
        public AltitudeUnitTypes AltitudeUnits { get; set; }

        /// <summary>
        /// Any speed transformation (conversion factor)
        /// </summary>
        public SpeedUnitTypes SpeedUnits { get; set; }

        /// <summary>
        /// A tail number, if found in the telemetry.
        /// </summary>
        public string TailNumber { get; set; }

        /// <summary>
        /// Cached distance, in nautical miles.  If it doesn't have a value, call GetPathDistance()
        /// </summary>
        public double? PathDistance { get; set; }

        public bool NeedsComputing
        {
            get { return m_dt == null || m_dt.Columns.Count == 0; }
        }

        /// <summary>
        /// Returns a string describing any error that has occured
        /// </summary>
        public string ErrorString
        {
            get { return m_szError; }
        }

        /// <summary>
        /// Return the factor to scale altitudes to meters.
        /// </summary>
        /// <returns>Result which, when multiplied by altitude, yields meters</returns>
        public double AltitudeFactor
        {
            get
            {
                switch (this.AltitudeUnits)
                {
                    case AltitudeUnitTypes.Feet:
                        return ConversionFactors.MetersPerFoot;
                    case AltitudeUnitTypes.Meters:
                    default:
                        return 1.0;
                }
            }
        }

        /// <summary>
        /// Returns the factor to scale to meters/second
        /// </summary>
        /// <returns>Value which, when multiplied by speed, yields meters/second</returns>
        public double SpeedFactor
        {
            get
            {
                switch (this.SpeedUnits)
                {
                    case SpeedUnitTypes.Knots:
                        return ConversionFactors.MetersPerSecondPerKnot;
                    case SpeedUnitTypes.FeetPerSecond:
                        return ConversionFactors.MetersPerFoot;
                    case SpeedUnitTypes.MilesPerHour:
                        return ConversionFactors.MetersPerSecondPerMilesPerHour;
                    case SpeedUnitTypes.KmPerHour:
                        return ConversionFactors.MetersPerSecondPerKmPerHour;
                    case SpeedUnitTypes.MetersPerSecond:
                    default:
                        return 1.0;
                }
            }
        }

        /// <summary>
        /// A DataTable version of the data
        /// </summary>
        public TelemetryDataTable Data
        {
            get { return m_dt; }
        }

        /// <summary>
        /// The File type of the data; set after parsing.
        /// </summary>
        public DataSourceType.FileType? DataType { get; set; }

        #region What information is present?
        /// <summary>
        /// True if the data has lat/long info (i.e., a path)
        /// </summary>
        /// <returns>True if lat/long info is present.</returns>
        public Boolean HasLatLongInfo
        {
            get { return Data != null && Data.HasLatLongInfo; }
        }

        public Boolean HasDateTime
        {
            get { return Data != null && Data.HasDateTime; }
        }

        public Boolean HasSpeed
        {
            get { return Data != null && Data.HasSpeed; }
        }

        /// <summary>
        /// Is time-zone offset present in the data?
        /// </summary>
        public Boolean HasTimezone
        {
            get { return Data != null && Data.HasTimezone; }
        }

        public bool HasAltitude
        {
            get { return Data != null && Data.HasAltitude; }
        }
        #endregion

        #endregion // properties

        #region Object Creation
        public FlightData()
        {
            m_szError = string.Empty;
            m_dt = new TelemetryDataTable();
            FlightID = 0;
            AltitudeUnits = AltitudeUnitTypes.Feet;
            SpeedUnits = SpeedUnitTypes.Knots;
        }
        #endregion

        public byte[] WriteToFormat(LogbookEntryBase le, DownloadFormat format, SpeedUnitTypes speedUnits, AltitudeUnitTypes altUnits, out DataSourceType dst)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));

            dst = DataSourceType.BestGuessTypeFromText(le.FlightData);

            // short circuit original for efficiency
            if (format == DownloadFormat.Original) {
                return Encoding.UTF8.GetBytes(le.FlightData);
            }

            if (NeedsComputing)
                ParseFlightData(le);
            FlightID = le.FlightID;
            if (dst.CanSpecifyUnitsForTelemetry)
            {
                AltitudeUnits = altUnits;
                SpeedUnits = speedUnits;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                switch (format)
                {
                    case DownloadFormat.Original:
                        break;
                    case DownloadFormat.CSV:
                        dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.CSV);
                        using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8))
                        {
                            CsvWriter.WriteToStream(sw, Data, true, true);
                        }
                        break;
                    case DownloadFormat.KML:
                        dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.KML);
                        WriteKMLData(ms);
                        break;
                    case DownloadFormat.GPX:
                        dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.GPX);
                        WriteGPXData(ms);
                        break;
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Returns the trajectory of the flight, if available, as an array of positions (LatLong + Altitude)
        /// </summary>
        /// <returns>The array, null if no trajectory.</returns>
        public Position[] GetTrajectory()
        {
            if (HasLatLongInfo && m_dt != null)
            {
                ArrayList al = new ArrayList();

                bool fLatLon = (m_dt.Columns[KnownColumnNames.LAT] != null && m_dt.Columns[KnownColumnNames.LON] != null); // Lat+lon or position?
                bool fHasAlt = HasAltitude;
                bool fHasTime = HasDateTime;
                bool fHasSpeed = HasSpeed;

                bool fIsUTC = Data.HasUTCDateTime;
                string szDateCol = fIsUTC ? KnownColumnNames.UTCDateTime : Data.DateColumn; // default to date or time, but try to get a UTC time if possible
                bool fHasUTCOffset = Data.Columns[KnownColumnNames.UTCOffSet] != null;
                bool fHasTimeKind = Data.Columns[KnownColumnNames.TIMEKIND] != null;
                
                // default values if alt or timestamp are not present.
                double alt = 0.0;
                double speed = 0.0;
                DateTime timestamp = DateTime.MinValue;

                for (int i = 0; i < m_dt.Rows.Count; i++)
                {
                    DataRow dr = m_dt.Rows[i];

                    if (fHasAlt)
                        alt = Convert.ToDouble(dr[KnownColumnNames.ALT], CultureInfo.InvariantCulture);

                    if (fIsUTC)
                    {
                        timestamp = (DateTime)dr[KnownColumnNames.UTCDateTime];
                        if (timestamp.Kind != DateTimeKind.Utc)
                            timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
                    }
                    else if (fHasTime)
                    {
                        timestamp = dr[szDateCol].ToString().SafeParseDate(DateTime.MinValue);
                        if (fHasUTCOffset)
                            timestamp = DateTime.SpecifyKind(timestamp.AddMinutes(Convert.ToInt32(dr[KnownColumnNames.UTCOffSet], CultureInfo.InvariantCulture)), DateTimeKind.Utc);
                        else if (fHasTimeKind)
                        {
                            if (!Enum.TryParse<DateTimeKind>(dr[KnownColumnNames.TIMEKIND].ToString(), out DateTimeKind kind))
                            {
                                kind = int.TryParse(dr[KnownColumnNames.TIMEKIND].ToString(), out int intKind) ? (DateTimeKind)intKind : DateTimeKind.Unspecified;
                            }
                            if (kind != DateTimeKind.Unspecified)
                                timestamp = DateTime.SpecifyKind(timestamp, kind);
                        }
                    }

                    if (fHasSpeed)
                        speed = Convert.ToDouble(dr[KnownColumnNames.SPEED], CultureInfo.InvariantCulture);

                    if (fLatLon)
                        al.Add(new Position(new LatLong(Convert.ToDouble(dr[KnownColumnNames.LAT], CultureInfo.InvariantCulture), Convert.ToDouble(dr[KnownColumnNames.LON], CultureInfo.InvariantCulture)), alt, timestamp, speed));
                    else
                        al.Add(new Position((LatLong)dr[KnownColumnNames.POS], alt, timestamp, speed));
                }

                return (Position[])al.ToArray(typeof(Position));
            }
            else
                return null;
        }

        /// <summary>
        /// Returns the flight path, if available, as an array of lat/long coordinates
        /// </summary>
        /// <returns>The array, null if no flight path.</returns>
        public LatLong[] GetPath()
        {
            Position[] rgpos = GetTrajectory();
            if (rgpos == null)
                return null;

            LatLong[] rgll = new LatLong[rgpos.Length];
            for (int i = 0; i < rgpos.Length; i++)
                rgll[i] = new LatLong(rgpos[i].Latitude, rgpos[i].Longitude);
            return rgll;
        }

        /// <summary>
        /// Returns the distance traveled along the flight path.  Value is cached in PathDistance property
        /// </summary>
        /// <returns>The distance, in nm</returns>
        public double ComputePathDistance()
        {
            if (PathDistance.HasValue)
                return PathDistance.Value;

            double d = 0;

            Position[] rgpos = GetTrajectory();

            if (rgpos != null)
            {
                for (int i = 1; i < rgpos.Length; i++)
                    d += rgpos[i].DistanceFrom(rgpos[i - 1]);
            }

            PathDistance = d;
            return d;
        }

        /// <summary>
        /// Writes the flight data in CSV format to the target stream
        /// </summary>
        /// <param name="s"></param>
        public void WriteCSVData(Stream s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            using (StreamWriter sw = new StreamWriter(s, Encoding.UTF8))
                CsvWriter.WriteToStream(sw, Data, true, true);
        }

        /// <summary>
        /// Returns the flight data in CSV format in a string
        /// </summary>
        public string WriteCSVData()
        {
            using (StringWriter writer = new StringWriter(CultureInfo.CurrentCulture))
            {
                CsvWriter.WriteToStream(writer, Data, true, true);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Returns the flight path, if available, in KML (google earth) format
        /// </summary>
        /// <returns>The KML document, null if no flight path</returns>
        public void WriteKMLData(Stream s)
        {
            Position[] rgPos = GetTrajectory();
            if (rgPos != null && rgPos.Length > 0)
            {
                using (KMLWriter kw = new KMLWriter(s))
                {
                    kw.BeginKML();
                    kw.AddPath(rgPos, Resources.FlightData.PathForFlight, AltitudeFactor);
                    kw.EndKML();
                }
            }
        }

        /// <summary>
        /// Returns a KML respresentation of all of the flights represented by the specified query
        /// </summary>
        /// <param name="fq">The flight query</param>
        /// <param name="alMaster">A pre-populated list of all navaids/airports referenced by the user (for efficiency)</param>
        /// <param name="s">The stream to which to write</param>
        /// <param name="error">Any error</param>
        /// <returns>KML string for the matching flights.</returns>
        public static void AllFlightsAsKML(FlightQuery fq, AirportList alMaster, Stream s, out string error)
        {
            if (fq == null)
                throw new ArgumentNullException(nameof(fq));
            if (String.IsNullOrEmpty(fq.UserName) && fq.EnumeratedFlights.Count == 0)
                throw new MyFlightbookException("Don't get all flights as KML for an empty user!!");
            if (alMaster == null)
                throw new ArgumentNullException(nameof(alMaster));

            // Force load if no user name in the query but we have been given explicit flights to load (i.e., enumerated flights only, as in a club scenario)
            bool fForceLoad = String.IsNullOrEmpty(fq.UserName) && fq.EnumeratedFlights.Count > 0;

            using (KMLWriter kw = new KMLWriter(s))
            {
                kw.BeginKML();

                error = LogbookEntryDisplay.IterateFlightsForQuery(
                    fq,
                    LogbookEntryCore.LoadTelemetryOption.LoadAll,
                    (le) =>
                    {
                        if (le.Telemetry.HasPath)
                        {
                            using (FlightData fd = new FlightData())
                            {
                                try
                                {
                                    fd.ParseFlightData(le.Telemetry.RawData, le.Telemetry.MetaData);
                                    if (fd.HasLatLongInfo)
                                    {
                                        kw.AddPath(fd.GetTrajectory(), String.Format(CultureInfo.CurrentCulture, "{0:d} - {1}", le.Date, le.Comment), fd.SpeedFactor);
                                        return;
                                    }
                                }
                                catch (Exception ex) when (!(ex is OutOfMemoryException)) { }   // eat any error and fall through below
                            }
                        }
                        // No path was found above.
                        AirportList al = alMaster.CloneSubset(le.Route);
                        kw.AddRoute(al.GetNormalizedAirports(), String.Format(CultureInfo.CurrentCulture, "{0:d} - {1}", le.Date, le.Route));
                    }, 
                    fForceLoad);
                kw.EndKML();
            }
        }

        public void WriteGPXData(Stream s, IEnumerable<Position> positions = null, bool hasTime = false, bool hasAlt = false, bool hasSpeed = false, IDictionary<string, string> dictMeta = null)
        {
            Position[] rgPos = positions == null ? GetTrajectory() : positions.ToArray();

            if (rgPos != null && rgPos.Length > 0)
            {
                bool fHasAlt = hasAlt || HasAltitude;
                bool fHasTime = hasTime || HasDateTime;
                bool fHasSpeed = hasSpeed || HasSpeed;

                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
                {
                    Encoding = new UTF8Encoding(false),
                    ConformanceLevel = ConformanceLevel.Document,
                    Indent = true
                };

                using (XmlWriter gpx = XmlWriter.Create(s, xmlWriterSettings))
                {
                    gpx.WriteStartDocument();

                    gpx.WriteStartElement("gpx", "http://www.topografix.com/GPX/1/1");
                    gpx.WriteAttributeString("creator", "http://myflightbook.com");
                    gpx.WriteAttributeString("version", "1.1");

                    if (dictMeta != null)
                    {
                        gpx.WriteStartElement("metadata");
                        foreach (string key in dictMeta.Keys)
                        {
                            gpx.WriteElementString(key, dictMeta[key]);
                        }
                        gpx.WriteEndElement();  // metadata
                    }

                    gpx.WriteStartElement("trk");
                    gpx.WriteStartElement("name");
                    gpx.WriteEndElement();
                    gpx.WriteStartElement("trkseg");

                    double AltXForm = AltitudeFactor;
                    double SpeedXForm = SpeedFactor;

                    foreach (Position p in rgPos)
                    {
                        gpx.WriteStartElement("trkpt");
                        gpx.WriteAttributeString("lat", p.LatitudeString);
                        gpx.WriteAttributeString("lon", p.LongitudeString);

                        // Altitude must be in meters
                        if (fHasAlt)
                            gpx.WriteElementString("ele", (p.Altitude * AltXForm).ToString("F8", System.Globalization.CultureInfo.InvariantCulture));
                        if (fHasTime)
                            gpx.WriteElementString("time", p.Timestamp.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));

                        // Speed needs to be in M/S
                        if (fHasSpeed)
                            gpx.WriteElementString("speed", (p.Speed * SpeedXForm).ToString("F8", System.Globalization.CultureInfo.InvariantCulture));
                        gpx.WriteEndElement();  // trkpt
                    }

                    gpx.WriteEndElement(); // trk
                    gpx.WriteEndElement(); // trkseg
                    gpx.WriteEndDocument(); // <gpx>

                    gpx.Flush();
                }
            }
        }

        #region Parsing
        /// <summary>
        /// Parses the flight data into a data table.  NOT CACHED - caller should cache results if necessary
        /// </summary>
        /// <param name="flightData">The data to parse</param>
        /// <param name="telemetryMetaData">Any metadata (can include data to trim)</param>
        /// <returns>True for success; if failure, see ErrorString</returns>
        public bool ParseFlightData(string flightData, TelemetryMetaData telemetryMetaData = null)
        {
            CultureInfo ciSave = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            bool fResult = false;

            DataSourceType dst = DataSourceType.BestGuessTypeFromText(flightData);
            DataType = dst.Type;
            TelemetryParser tp = dst.Parser;
            if (tp != null)
            {
                tp.ParsedData = m_dt;
                fResult = tp.Parse(flightData);
                SpeedUnits = tp.SpeedUnits;
                AltitudeUnits = tp.AltitudeUnits;
                TailNumber = tp.TailNumber;

                m_szError = tp.ErrorString;
            }

            if (telemetryMetaData != null && telemetryMetaData.HasData)
            {
                // Trim from beginning, end.
                if (telemetryMetaData.DataEnd.HasValue && telemetryMetaData.DataEnd.Value < m_dt.Rows.Count)
                {
                    for (int iRow = m_dt.Rows.Count - 1; iRow > telemetryMetaData.DataEnd.Value; iRow--)
                        m_dt.Rows[iRow].Delete();
                }
                if (telemetryMetaData.DataStart.HasValue && telemetryMetaData.DataStart.Value > 0)
                {
                    for (int iRow = telemetryMetaData.DataStart.Value - 1; iRow >= 0; iRow--)
                        m_dt.Rows[iRow].Delete();
                }
                m_dt.AcceptChanges();
            }

            System.Threading.Thread.CurrentThread.CurrentCulture = ciSave;

            return fResult;
        }

        public bool ParseFlightData(LogbookEntryBase le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            return ParseFlightData(le.FlightData ?? string.Empty, le.Telemetry.MetaData);
        }
        #endregion

        #region Autofill
        protected enum FlyingState {OnGround, OnGroundButNotYetFullyStopped, Flying};
        private class AutoFillContext
        {
            #region properties
            #region internal properties
            protected AutoFillOptions Options { get; set; }
            protected LogbookEntry ActiveFlight { get; set; }
            protected FlyingState CurrentState { get; set; }
            protected bool HasStartedFlight { get; set; }
            protected bool LastPositionWasNight { get; set; }
            protected Position LastPositionReport { get; set; }
            protected StringBuilder RouteSoFar { get; set; }
            protected bool HasSpeed { get; set; }
            protected double TotalNight { get; set; }
            #endregion

            /// <summary>
            /// The column to use for the date value
            /// </summary>
            public string DateColumn { get; set; }

            private string _szSpeedCol;

            /// <summary>
            /// The column to use for the speed
            /// </summary>
            public string SpeedColumn 
            {
                get {return _szSpeedCol;}
                set {_szSpeedCol = value; HasSpeed = !String.IsNullOrEmpty(value);}
            }

            /// <summary>
            /// True if this specifies UTC vs. Local
            /// </summary>
            public bool HasDateTimeKind { get; set; }

            /// <summary>
            /// True if a timezone offset is provided
            /// </summary>
            public bool HasTimezone { get; set; }

            /// <summary>
            /// The name of the column to use for zime zone offset
            /// </summary>
            public string TimeZoneColumnName { get; set; }

            /// <summary>
            /// True to use Position column, false to use Lat/Lon columns
            /// </summary>
            public bool HasPosition { get; set; }
            #endregion

            public AutoFillContext(AutoFillOptions opt, LogbookEntry le)
            {
                Options = opt;
                ActiveFlight = le;
                CurrentState = FlyingState.OnGround;
                HasStartedFlight = LastPositionWasNight = false;
                HasSpeed = HasDateTimeKind = HasTimezone = false;
                LastPositionReport = null;
                TotalNight = 0.0;
                HasPosition = false;
                RouteSoFar = new StringBuilder();
                DateColumn = SpeedColumn = TimeZoneColumnName = string.Empty;
            }

            private static void AppendToRoute(StringBuilder sb, string szCode)
            {
                string szNewAirport = szCode.ToUpper(CultureInfo.CurrentCulture);
                if (!sb.ToString().Trim().EndsWith(szNewAirport, StringComparison.CurrentCultureIgnoreCase))
                    sb.AppendFormat(CultureInfo.CurrentCulture, " {0}", szNewAirport);
            }

            private static void AppendNearest(StringBuilder sb, Position po, bool fIncludeHeliports)
            {
                airport[] rgAp = airport.AirportsNearPosition(po.Latitude, po.Longitude, 1, fIncludeHeliports).ToArray();
                if (rgAp != null && rgAp.Length > 0)
                    AppendToRoute(sb, rgAp[0].Code);
            }

            /// <summary>
            /// Updates the flying state
            /// </summary>
            /// <param name="s">The speed</param>
            /// <param name="dtSample">The timestamp of the position report</param>
            /// <param name="po">The position report (may not have a timestamp)</param>
            /// <param name="fIsNight">True if it is night</param>
            private void UpdateFlyingState(double s, DateTime dtSample, Position po, bool fIsNight)
            {
                switch (CurrentState)
                {
                    case FlyingState.Flying:
                        if (s < Options.LandingSpeed)
                        {
                            CurrentState = FlyingState.OnGroundButNotYetFullyStopped;
                            ActiveFlight.Landings++;

                            AppendNearest(RouteSoFar, po, Options.IncludeHeliports);

                            ActiveFlight.FlightEnd = dtSample;
                        }
                        break;
                    case FlyingState.OnGround:
                    case FlyingState.OnGroundButNotYetFullyStopped:
                        if (s > Options.TakeOffSpeed)
                        {
                            CurrentState = FlyingState.Flying;

                            if (!HasStartedFlight)
                            {
                                HasStartedFlight = true;
                                // Issue #1385 - since date-of-flight is in local time by default, only update it if it's more than 48 hours off.
                                DateTime dtFlight = new DateTime(dtSample.Ticks).AddMinutes(Options.TimeZoneOffset);
                                if (Math.Abs(DateTime.SpecifyKind(ActiveFlight.Date.Date, DateTimeKind.Utc).Subtract(dtFlight).TotalHours) > 48)
                                    ActiveFlight.Date = DateTime.SpecifyKind(dtFlight.Date, DateTimeKind.Unspecified);
                            }

                            AppendNearest(RouteSoFar, po, Options.IncludeHeliports);

                            if (ActiveFlight.FlightStart.CompareTo(DateTime.MinValue) == 0)
                                ActiveFlight.FlightStart = dtSample;

                            // Add a night take-off if it's night
                            if (fIsNight)
                            {
                                // see if the night take-off property is already present
                                CustomFlightProperty cfpNightTO = ActiveFlight.CustomProperties.GetEventWithTypeIDOrNew(CustomPropertyType.KnownProperties.IDPropNightTakeoff);
                                cfpNightTO.IntValue++;
                                ActiveFlight.CustomProperties.Add(cfpNightTO);
                            }
                        }
                        break;
                }

                if (CurrentState == FlyingState.OnGroundButNotYetFullyStopped && s < AutoFillOptions.FullStopSpeed)
                {
                    CurrentState = FlyingState.OnGround;

                    if (fIsNight)
                        ActiveFlight.NightLandings++;
                    else
                        ActiveFlight.FullStopLandings++;
                }
            }

            /// <summary>
            /// Reads a data row and updates the progress of the flight accordingly.
            /// </summary>
            /// <param name="dr">The datarow</param>
            public void ProcessSample(DataRow dr)
            {
                if (dr == null)
                    throw new ArgumentNullException(nameof(dr));

                object o = dr[DateColumn];

                DateTime dtSample = (o is DateTime time) ? time : o.ToString().SafeParseDate(DateTime.MinValue);
                if (HasDateTimeKind)
                    dtSample = DateTime.SpecifyKind(dtSample, (DateTimeKind)dr[KnownColumnNames.TIMEKIND]);

                double s = 0.0;
                if (HasSpeed)
                    s = Convert.ToDouble(dr[SpeedColumn], CultureInfo.CurrentCulture);

                if (!dtSample.HasValue())
                    return;

                if (dtSample.Kind != DateTimeKind.Utc)
                    dtSample = new DateTime(dtSample.AddMinutes(HasTimezone ? Convert.ToInt32(dr[TimeZoneColumnName], CultureInfo.InvariantCulture) : -Options.TimeZoneOffset).Ticks, DateTimeKind.Utc);

                if (!ActiveFlight.EngineStart.HasValue())
                    ActiveFlight.EngineStart = dtSample;
                if (!ActiveFlight.EngineEnd.HasValue() || dtSample.CompareTo(ActiveFlight.EngineEnd) > 0)
                    ActiveFlight.EngineEnd = dtSample;

                Position po = null;
                bool fIsNightForLandings = false;
                bool fIsNightForFlight = false;

                LatLong ll = HasPosition ? (LatLong) dr[KnownColumnNames.POS] : LatLong.TryParse(dr[KnownColumnNames.LAT], dr[KnownColumnNames.LON], CultureInfo.CurrentCulture);
                if (ll != null)
                {
                    po = new Position(ll.Latitude, ll.Longitude, dtSample);
                    SunriseSunsetTimes sst = new SunriseSunsetTimes(po.Timestamp, po.Latitude, po.Longitude, Options.NightFlightSunsetOffset);
                    fIsNightForFlight = Options.IsNightForFlight(sst);
                    fIsNightForLandings =  (Options.NightLanding == AutoFillOptions.NightLandingCriteria.Night) ? fIsNightForFlight : sst.IsFAANight;
                }

                if (po != null && LastPositionReport != null)
                {
                    if (fIsNightForFlight && LastPositionWasNight)
                    {
                        double night = po.Timestamp.Subtract(LastPositionReport.Timestamp).TotalHours;
                        if (night < 0.5)    // don't add the night time if samples are spaced more than 30 minutes apart
                            TotalNight += night;
                    }
                }
                LastPositionReport = po;
                LastPositionWasNight = fIsNightForFlight;

                UpdateFlyingState(s, dtSample, po, fIsNightForLandings);
            }

            /// <summary>
            /// Fills in the remaining fields that you can't fill in until you've seen all samples (or at least for which it is redundant to do so!)
            /// </summary>
            public void FinalizeFlight(AutoFillOptions opt)
            {
                string szNewRoute = RouteSoFar.ToString().Trim();
                if (!String.IsNullOrEmpty(szNewRoute))
                    ActiveFlight.Route = szNewRoute;
                ActiveFlight.Nighttime = Convert.ToDecimal(Math.Round(TotalNight, opt.RoundToTenth ? 1 : 2));
            }
        }

        /// <summary>
        /// Form https://tkit.dev/2018/04/05/converting-to-and-from-local-time-in-c-net-with-noda-time/
        /// Converts a local-time DateTime to UTC DateTime based on the specified
        /// timezone. The returned object will be of UTC DateTimeKind. To be used
        /// when we want to know what's the UTC representation of the time somewhere
        /// in the world.
        /// </summary>
        /// <param name="dateTime">Local DateTime as UTC or Unspecified DateTimeKind.</param>
        /// <param name="timezone">Timezone name (in TZDB format).</param>
        /// <returns>UTC DateTime as UTC DateTimeKind.</returns>
        private static DateTime UtcFromZone(DateTime dateTime, string timezone)
        {
            if (dateTime.Kind == DateTimeKind.Local)
                throw new ArgumentException("Expected non-local kind of DateTime");

            var zone = DateTimeZoneProviders.Tzdb[timezone];
            LocalDateTime asLocal = dateTime.ToLocalDateTime();
            ZonedDateTime asZoned = asLocal.InZoneLeniently(zone);
            Instant instant = asZoned.ToInstant();
            ZonedDateTime asZonedInUtc = instant.InUtc();
            DateTime utc = asZonedInUtc.ToDateTimeUtc();

            return utc;
        }

        private string GenerateSyntheticPath(LogbookEntry le, AutoFillOptions opt)
        {
            string result = string.Empty;
            if (le != null && String.IsNullOrEmpty(le.FlightData))
            {
                DateTime dtStart = le.BestStartDateTime();
                DateTime dtEnd = le.BestEndDateTime();

                if (dtStart.HasValue() && dtEnd.HasValue())
                {
                    AirportList apl = new AirportList(le.Route);
                    airport[] rgap = apl.GetNormalizedAirports();
                    if (rgap.Length == 2)
                    {
                        LatLong ll1 = rgap[0].LatLong;
                        LatLong ll2 = rgap[1].LatLong;

                        if (opt.TimeConversion == AutoFillOptions.TimeConversionCriteria.Local)
                        {
                            // Issue #1172
                            // We are assuming here that dtStart/dtEnd are in UTC (which they MUST be for any math to work.
                            // But if fTryConvertLocal is true, we assume they are in local time.  In which case, we need to:
                            //  * find the timezone based on the latitude/longitude
                            //  * convert it (for purposes of this path) to UTC using system timezone functions
                            // If this fails for EITHER time, then just treat them both as UTC
                            // Note that the TryLocal option is NOT displayed to the user EXCEPT on import.  Why?
                            // Because it mucks up display of the engine/block times, which are entered in either UTC or in the user's preferred timezone
                            // but do NOT mix local times.  So it just makes a mess of things.  We are simplifying here by only allowing
                            // local time conversion when importing from a file.
                            try
                            {
                                TimeZoneResult szIANATimeZone1 = TimeZoneLookup.GetTimeZone(ll1.Latitude, ll1.Longitude);
                                TimeZoneResult szIANATimeZone2 = TimeZoneLookup.GetTimeZone(ll2.Latitude, ll2.Longitude);

                                // OK, now we have the timezones, create pseudo-local times:
                                DateTime dtStartLocal = DateTime.SpecifyKind(dtStart, DateTimeKind.Unspecified);
                                DateTime dtEndLocal = DateTime.SpecifyKind(dtEnd, DateTimeKind.Unspecified);

                                // Update engine start/end regardless since we're going to recompute those.
                                // Even if we started from block, we'll leave block unchanged.
                                le.EngineStart = dtStart = UtcFromZone(dtStartLocal, szIANATimeZone1.Result);
                                le.EngineEnd = dtEnd = UtcFromZone(dtEndLocal, szIANATimeZone2.Result);
                            }
                            catch (Exception ex) when (!(ex is OutOfMemoryException)) { }
                        } else if (opt.TimeConversion == AutoFillOptions.TimeConversionCriteria.Preferred)
                        {
                            if (opt.PreferredTimeZone == null)
                                throw new InvalidOperationException("Autofill with user's preferred timezone is requested, but no time zone was provided");
                            // OK, now we have the timezones, create pseudo-local times:
                            DateTime dtStartLocal = DateTime.SpecifyKind(dtStart, DateTimeKind.Unspecified);
                            DateTime dtEndLocal = DateTime.SpecifyKind(dtEnd, DateTimeKind.Unspecified);

                            // Update engine start/end regardless since we're going to recompute those.
                            // Even if we started from block, we'll leave block unchanged.
                            le.EngineStart = dtStart = DateTime.SpecifyKind(dtStartLocal, DateTimeKind.Local).ConvertFromTimezone(opt.PreferredTimeZone);
                            le.EngineEnd = dtEnd = DateTime.SpecifyKind(dtEndLocal, DateTimeKind.Local).ConvertFromTimezone(opt.PreferredTimeZone);
                        }

                        le.FixAmbiguousTimes(); // ensure that ambiguous times are fixed.

                        // Verify that the fix applied to our dtStart/dtEnd
                        if (dtStart.CompareTo(dtEnd) > 0)
                        {
                            dtStart = le.BestStartDateTime();
                            dtEnd = le.BestEndDateTime();
                        }

                        IEnumerable<Position> rgPos = Position.SynthesizePath(ll1, dtStart, ll2, dtEnd);
                        SpeedUnits = SpeedUnitTypes.MetersPerSecond;    // SynthesizePath uses meters/s.

                        using (MemoryStream ms = new MemoryStream())
                        {
                            using (StreamReader sr = new StreamReader(ms))
                            {
                                MemoryStream ms2 = sr.BaseStream as MemoryStream;
                                WriteGPXData(ms2, rgPos, true, false, true);
                                ms2.Seek(0, SeekOrigin.Begin);
                                result = sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Updates the specified entry with as much as can be gleaned from the telemetry and/or times.
        /// Ensures as well that FixAmbiguousTimes is called for the flight. (Issue #1441)
        /// </summary>
        /// <param name="le"></param>
        /// <returns></returns>
        public void AutoFill(LogbookEntry le, AutoFillOptions opt)
        {
            if (le == null || opt == null)
                return;

            bool fSyntheticPath = false;
            // see if we can synthesize a path.  If so, save it as GPX
            if (String.IsNullOrEmpty(le.FlightData) && opt.AutoSynthesizePath)
            {
                le.FlightData = GenerateSyntheticPath(le, opt);
                le.Telemetry.MetaData.Clear();  // ensure no cropping.
                fSyntheticPath = !String.IsNullOrEmpty(le.FlightData);
            }

            // Issue #1441: if start/end times were naked (no date), then we need to fix them to account for overnight flights; GenerateSyntheticPath will have done this already, after potentially handling local times.
            le.FixAmbiguousTimes();

            // first, parse the telemetry.
            if (!String.IsNullOrEmpty(le.FlightData) && (ParseFlightData(le) || opt.IgnoreErrors) && HasLatLongInfo && HasDateTime)
            {
                // clear out the stuff we may fill out
                le.Landings = le.NightLandings = le.FullStopLandings = 0;
                le.CrossCountry = 0;
                le.TotalFlightTime = 0;
                le.Nighttime = 0;
                if (!fSyntheticPath)
                    le.FlightStart = le.FlightEnd = DateTime.MinValue;
                le.CustomProperties.RemoveItem(CustomPropertyType.KnownProperties.IDPropNightTakeoff);

                AutoFillContext afc = new AutoFillContext(opt, le) {
                    DateColumn = Data.DateColumn, 
                    SpeedColumn = HasSpeed ? KnownColumnNames.SPEED : ((m_dt.Columns[KnownColumnNames.DERIVEDSPEED] != null) ? KnownColumnNames.DERIVEDSPEED : string.Empty),
                    HasDateTimeKind = (m_dt.Columns[KnownColumnNames.TIMEKIND] != null),
                    HasTimezone = HasTimezone,
                    HasPosition = (m_dt.Columns[KnownColumnNames.POS] != null),
                    TimeZoneColumnName = Data.TimeZoneHeader
                };

                m_dt.DefaultView.Sort = afc.DateColumn + " ASC";

                decimal factorToKts = (decimal) (SpeedFactor / Geography.ConversionFactors.MetersPerSecondPerKnot);
                bool fConvertNativeSpeed = HasSpeed && afc.SpeedColumn.CompareOrdinal(KnownColumnNames.SPEED) == 0;

                bool fCheckHobbs = le.HobbsStart == 0 && m_dt.Columns.Contains(KnownColumnNames.HOBBS);

                foreach (DataRow dr in m_dt.DefaultView.Table.Rows)
                {
                    if (fCheckHobbs)
                    {
                        decimal newHobbs = Convert.ToDecimal(dr[KnownColumnNames.HOBBS], CultureInfo.CurrentCulture);
                        le.HobbsStart = (le.HobbsStart == 0) ? newHobbs : Math.Min(newHobbs, le.HobbsStart);
                        le.HobbsEnd = Math.Max(le.HobbsEnd, newHobbs);
                    }
                    decimal dSaved = 0.0M;
                    // convert to kts for processing
                    if (fConvertNativeSpeed)
                    {
                        dSaved = Convert.ToDecimal(dr[KnownColumnNames.SPEED], CultureInfo.CurrentCulture);
                        dr[KnownColumnNames.SPEED] = dSaved * factorToKts;
                    }
                        
                    afc.ProcessSample(dr);

                    if (fConvertNativeSpeed)
                        dr[KnownColumnNames.SPEED] = dSaved;
                }

                afc.FinalizeFlight(opt);
            }

            // Auto hobbs and auto totals.
            le.AutoTotals(opt);
            le.AutoHobbs(opt);

            le.TotalFlightTime = Math.Round(le.TotalFlightTime, opt.RoundToTenth ? 1 : 2);

            le.AutoFillFinish(opt);

            if (fSyntheticPath) // no point in saving a bogus flight path that simply mimics the point-to-point.
                le.FlightData = string.Empty;
        }
        #endregion
    
        /// <summary>
        /// Reads the provided stream and returns either it's string representation or, if a zip, the first file in the zip
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string ReadFromStream(Stream s)
        {
            if (s == null)
                return string.Empty;

            try
            {
                using (ZipArchive zipArchive = new ZipArchive(s, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in zipArchive.Entries)
                    {
                        using (StreamReader sr = new StreamReader(entry.Open()))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is InvalidDataException)
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    return sr.ReadToEnd();
                }
            }
            return string.Empty;
        }
    }

    [Serializable]
    public class TelemetryMetaData
    {
        #region Properties
        /// <summary>
        /// Index of the first sample to display
        /// </summary>
        public int? DataStart { get; set; }

        /// <summary>
        /// Index of the last sample to display
        /// </summary>
        public int? DataEnd { get; set; }

        [JsonIgnore]
        public bool HasData
        {
            get { return DataStart.HasValue || DataEnd.HasValue; }
        }
        #endregion

        public TelemetryMetaData() { }

        public void Clear()
        {
            DataStart = DataEnd = null;
        }
    }

    /// <summary>
    /// Summarizes telemetry for a flight
    /// </summary>
    [Serializable]
    public class TelemetryReference
    {
        private const string TelemetryExtension = ".telemetry"; // use a constant extension so that if the data type changes, we still overwrite the SAME file each time, avoiding orphans

        #region Object Creation
        public TelemetryReference()
        {
            FlightID = null;
            CachedDistance = null;
            RawData = null;
            MetaData = new TelemetryMetaData();
            Compressed = 0;
            Error = string.Empty;
            TelemetryType = DataSourceType.FileType.Text;
            GoogleData = null;
        }

        public TelemetryReference(MySqlDataReader dr)
            : this()
        {
            InitFromDataReader(dr);
        }

        public TelemetryReference(string szTelemetry, int idFlight)
            : this()
        {
            FlightID = idFlight;
            RawData = szTelemetry;
            if (!String.IsNullOrEmpty(szTelemetry))
                InitFromTelemetry();
        }

        /// <summary>
        /// Loads the telemetry reference for the specified flight.  CAN RETURN NULL
        /// </summary>
        /// <param name="idFlight"></param>
        /// <returns></returns>
        public static TelemetryReference LoadForFlight(int idFlight) 
        {
            TelemetryReference result = null;
            DBHelper dbh = new DBHelper("SELECT * FROM flighttelemetry WHERE idflight=?idf");
            dbh.ReadRow(
                (comm) => { comm.Parameters.AddWithValue("idf", idFlight); }, 
                (dr) => { result = new TelemetryReference(dr); });
            return result;
        }
        #endregion

        #region Object Initialization
        /// <summary>
        /// Initializes the google data/distance from the specified telemetry, or from the RawData property (if no telemetry is specified)
        /// </summary>
        /// <param name="szTelemetry">The telemetry string</param>
        private void InitFromTelemetry(string szTelemetry = null)
        {
            if (string.IsNullOrEmpty(szTelemetry) && string.IsNullOrEmpty(RawData))
                throw new ArgumentNullException(nameof(szTelemetry));
            
            if (szTelemetry == null)
                szTelemetry = RawData;

            using (FlightData fd = new FlightData())
            {
                MetaData.Clear();   // ensure no cropping, etc.
                if (!fd.ParseFlightData(szTelemetry, MetaData))
                    Error = fd.ErrorString;
                // cache the data as best as possible regardless of errors
                TelemetryType = fd.DataType.Value;  // should always have a value.
                RecalcGoogleData(fd);
            }
        }

        public void RecalcGoogleData(FlightData fd)
        {
            if (fd == null)
                throw new ArgumentNullException(nameof(fd));
            GoogleData = new GoogleEncodedPath(fd.GetTrajectory());
            CachedDistance = GoogleData.Distance;
        }

        private void InitFromDataReader(IDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));

            object o = dr["idflight"];
            if (o is DBNull)   // don't initialize if values are null (flights with no telemetry are like this)
                return;

            FlightID = Convert.ToInt32(o, CultureInfo.InvariantCulture);

            o = dr["telemetrytype"];
            if (o is DBNull)
                return;
            TelemetryType = (DataSourceType.FileType)Convert.ToInt32(dr["telemetrytype"], CultureInfo.InvariantCulture);
            
            o = dr["distance"];
            CachedDistance = (o is DBNull) ? (double?) null : Convert.ToDouble(o, CultureInfo.InvariantCulture);
            string szPath = dr["flightpath"].ToString();
            if (!string.IsNullOrEmpty(szPath))
                GoogleData = new GoogleEncodedPath() { EncodedPath = szPath, Distance = CachedDistance ?? 0 };

            o = dr["metadata"];
            string szJSONMeta = (o is DBNull) ? string.Empty : (string) o;
            if (!String.IsNullOrEmpty(szJSONMeta))
                MetaData = JsonConvert.DeserializeObject<TelemetryMetaData>(szJSONMeta);
        }
        #endregion

        #region Object persistance
        public void Commit()
        {
            if (GoogleData == null && !String.IsNullOrEmpty(RawData))
                InitFromTelemetry();

            string szQ = @"REPLACE INTO flighttelemetry SET idflight=?idf, distance=?d, flightpath=?path, telemetrytype=?tt, metadata=?md";
            DBHelper dbh = new DBHelper(szQ);
            if (!dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("idf", FlightID);
                comm.Parameters.AddWithValue("d", CachedDistance.HasValue && !double.IsNaN(CachedDistance.Value) ? CachedDistance : null);
                comm.Parameters.AddWithValue("path", GoogleData?.EncodedPath);
                comm.Parameters.AddWithValue("tt", (int)TelemetryType);
                comm.Parameters.AddWithValue("md", MetaData.HasData ? JsonConvert.SerializeObject(MetaData, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore }) : null);
            }))
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Exception committing TelemetryReference: idFlight={0}, distance={1}, path={2}, type={3}, error={4}", FlightID, CachedDistance.HasValue ? CachedDistance.Value.ToString(CultureInfo.InvariantCulture) : "(none)", GoogleData == null ? "(none)" : "(path)", TelemetryType.ToString(), dbh.LastError));

            if (!String.IsNullOrEmpty(RawData))
                SaveData();
        }

        /// <summary>
        /// Deletes this summary from the database and any associated file.  
        /// </summary>
        public void Delete()
        {
            DeleteFile();
            DBHelper dbh = new DBHelper("DELETE FROM flighttelemetry WHERE idflight=?idf");
            if (!(dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idf", FlightID); })))
                throw new MyFlightbookException(dbh.LastError);
        }

        /// <summary>
        /// Deletes the file associated with this, if it exists.
        /// </summary>
        public void DeleteFile()
        {
            string szPath = FilePath;
            if (File.Exists(szPath))
                File.Delete(szPath);
        }

        /// <summary>
        /// Deletes the telemetry file associated with the specified flight but does NOT hit the database.
        /// </summary>
        /// <param name="idFlight">The flight ID for which we want to delete data</param>
        public static void DeleteFile(int idFlight)
        {
            TelemetryReference ts = new TelemetryReference() { FlightID = idFlight };
            ts.DeleteFile();
        }

        public void SaveData()
        {
            if (String.IsNullOrEmpty(RawData))
                throw new MyFlightbookException("SaveData called but no raw data round to write!");
            File.WriteAllText(FilePath, RawData);
        }

        public string LoadData()
        {
            string path = FilePath;
            if (File.Exists(path))
                return RawData = File.ReadAllText(path);
            throw new FileNotFoundException("Telemetry file not found", path);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Any parsing error
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// The ID of the flight for which this data is associated
        /// </summary>
        public int? FlightID { get; set; }

        /// <summary>
        /// The raw telemetry
        /// </summary>
        public string RawData { get; set; }

        /// <summary>
        /// Size of the uncompressed telemetry - NOT PERSISTED
        /// </summary>
        public int Uncompressed 
        {
            get { return RawData == null ? 0 : RawData.Length; }
        }

        /// <summary>
        /// Size of the compressed telemetry - NOT PERSISTED
        /// </summary>
        public int Compressed { get; set; }

        /// <summary>
        /// Length of the encoded path - NOT PERSISTED
        /// </summary>
        public int GoogleDataSize
        {
            get { return GoogleData == null || GoogleData.EncodedPath == null ? 0 : GoogleData.EncodedPath.Length; }
        }

        /// <summary>
        /// The distance represented by the encoded path.
        /// </summary>
        public double? CachedDistance { get; set; }

        /// <summary>
        /// Directory where the telemetry files live.
        /// </summary>
        private static string FileDir
        {
            get { return ConfigurationManager.AppSettings["TelemetryDir"].ToString(CultureInfo.InvariantCulture) + "/"; }
        }

        /// <summary>
        /// The file name of the telemetry file on disk (not including path)
        /// </summary>
        private string FileName
        {
            get
            {
                if (!FlightID.HasValue)
                    throw new ArgumentException("Attempt to generate a FilePath for a TelemetrySummary object without a flightID");

                if (LogbookEntry.IsNewFlightID(FlightID.Value))
                    throw new InvalidConstraintException("Attempt to generate a FilePath for a TelemetrySummary object without a new-flight FlightID");

                return FlightID.Value.ToString(CultureInfo.InvariantCulture) + TelemetryExtension;                
            }
        }

        /// <summary>
        /// Full path of the original data on disk.
        /// </summary>
        private string FilePath 
        {
            get
            {
                return (FileDir + FileName).MapAbsoluteFilePath();
            }
        }

        /// <summary>
        /// The type of data (GPX, CSV, KML, etc.)
        /// </summary>
        public DataSourceType.FileType TelemetryType { get; set; }

        public GoogleEncodedPath GoogleData { get; set; }

        /// <summary>
        /// Does this have a compressed (cached) path?
        /// </summary>
        public bool HasCompressedPath
        {
            get { return GoogleData != null && !String.IsNullOrEmpty(GoogleData.EncodedPath); }
        }

        /// <summary>
        /// Does this have the raw data cached.
        /// </summary>
        public bool HasRawPath
        {
            get { return !String.IsNullOrEmpty(RawData);  }
        }

        /// <summary>
        /// Does this have a path that can be computed (either compressed or from raw data)?
        /// </summary>
        public bool HasPath
        {
            get { return HasRawPath || HasCompressedPath; }
        }

        public TelemetryMetaData MetaData { get; private set; }
        #endregion

        #region Cacheable data calls
        /// <summary>
        /// Returns the distance, using a cached value if available, and triggering a parsing of the data if not
        /// </summary>
        /// <returns>Distance for the telemetry, or 0 if there is any error or no telemetry</returns>
        public double Distance()
        {
            if (!CachedDistance.HasValue && HasPath)
            {
                if (String.IsNullOrEmpty(RawData))  // Issue #837 - rare, but can happen for example if you have invalid lat/lon values (sample in 837 have latitude values of 250 degrees.)
                    return 0;
                InitFromTelemetry();
            }

            return CachedDistance ?? 0;
        }

        /// <summary>
        /// Returns the path, using the encoded path if available, triggering a parsing of data if not.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<LatLong> Path()
        {
            if (!HasCompressedPath)
                InitFromTelemetry();
            return GoogleData.DecodedPath();
        }
        #endregion

        #region Merging
        /// <summary>
        /// Returns a merged telemetry reference from a list of references
        /// </summary>
        /// <param name="lstIn">The input references</param>
        /// <param name="idFlight">The flight with which the target will be associated</param>
        /// <returns>A merged telemetry reference.</returns>
        public static TelemetryReference MergedTelemetry(IEnumerable<TelemetryReference> lstIn, int idFlight)
        {
            if (lstIn == null || !lstIn.Any())
                return null;
            if (lstIn.Count() == 1)
                return lstIn.ElementAt(0);

            List<TelemetryReference> lst = new List<TelemetryReference>(lstIn);

            using (FlightData fdTarget = new FlightData())
            {
                fdTarget.ParseFlightData(String.IsNullOrEmpty(lst[0].RawData) ? lst[0].LoadData() : lst[0].RawData);
                lst.RemoveAt(0);

                foreach (TelemetryReference tr in lst)
                {
                    using (FlightData fdSrc = new FlightData())
                    {
                        fdSrc.ParseFlightData(String.IsNullOrEmpty(tr.RawData) ? tr.LoadData() : tr.RawData);
                        fdTarget.Data.MergeWith(fdSrc.Data);
                    }
                }

                return new TelemetryReference(fdTarget.WriteCSVData(), idFlight);
            }
        }
        #endregion

        #region admin functions
        /// <summary>
        /// Find all flights for there is both a telemetryreference AND data in the flights table
        /// </summary>
        /// <returns>An enumeration of flightIDs</returns>
        public static IEnumerable<int> FindDuplicateTelemetry()
        {
            List<int> lst = new List<int>();
            DBHelper dbh = new DBHelper("SELECT f.idFlight FROM flights f INNER JOIN flighttelemetry ft ON f.idflight=ft.idflight WHERE f.telemetry IS NOT NULL");
            dbh.ReadRows((comm) => { comm.CommandTimeout = 300; }, (dr) => { lst.Add(Convert.ToInt32(dr["idflight"], CultureInfo.InvariantCulture)); });
            return lst;
        }

        /// <summary>
        /// Find all flights for there is a telemetryreference but no corresponding file on the disk
        /// </summary>
        /// <returns>An enumeration of flightIDs</returns>
        public static IEnumerable<int> FindOrphanedRefs()
        {
            List<int> lst = new  List<int>();
            HashSet<string> files = new HashSet<string>(Directory.EnumerateFiles(FileDir.MapAbsoluteFilePath(), "*" + TelemetryExtension, SearchOption.TopDirectoryOnly));
            DBHelper dbh = new DBHelper("SELECT * FROM FlightTelemetry");
            dbh.ReadRows((comm) => { comm.CommandTimeout = 300; }, (dr) =>
            {
                TelemetryReference tr = new TelemetryReference(dr);
                if (!files.Contains(tr.FilePath))
                    lst.Add(tr.FlightID.Value);
            });
            return lst;
        }

        /// <summary>
        /// Find all files for which there is a file on the disk but no telemetry reference
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> FindOrphanedFiles()
        {
            HashSet<string> files = new HashSet<string>(Directory.EnumerateFiles(FileDir.MapAbsoluteFilePath(), "*" + TelemetryExtension, SearchOption.TopDirectoryOnly));
            DBHelper dbh = new DBHelper("SELECT * FROM FlightTelemetry");
            HashSet<string> References = new HashSet<string>();
            dbh.ReadRows(
                (comm) => { comm.CommandTimeout = 300; },
                (dr) => { References.Add(new TelemetryReference(dr).FilePath); });
            return files.Except(References);
        }

        /// <summary>
        /// Migrates Telemetry FROM the database TO files
        /// </summary>
        /// <param name="szUser">The user for whom to move files</param>
        /// <param name="limit">The max number of files to move; 50 if not provided.</param>
        /// <returns>Status of the result</returns>
        /// <exception cref="MyFlightbookException"></exception>
        public static string MigrateFROMDBToFiles(string szUser = null, int limit = 50)
        {
            DateTime dtStart = DateTime.Now;

            // for memory, just get the ID's up front, then we'll do flights one at a time.
            List<int> lst = new List<int>();
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT idFlight FROM flights WHERE {0} telemetry IS NOT NULL LIMIT {1}", String.IsNullOrEmpty(szUser) ? string.Empty : "username=?szuser AND ", limit));
            dbh.ReadRows((comm) =>
            {
                comm.Parameters.AddWithValue("szuser", szUser);
                comm.CommandTimeout = 300;  // can be slow.
            },
            (dr) => { lst.Add(Convert.ToInt32(dr["idFlight"], CultureInfo.InvariantCulture)); });

            // HACK: Force load the flight, so provide a non-empty username. 
            if (String.IsNullOrEmpty(szUser))
                szUser = "ADMIN";

            int cFlightsMigrated = 0;
            lst.ForEach((idFlight) =>
            {
                try
                {
                    new LogbookEntry(idFlight, szUser, LogbookEntryCore.LoadTelemetryOption.LoadAll, true).MoveTelemetryFromFlightEntry();
                    cFlightsMigrated++;
                    if ((cFlightsMigrated % 20) == 0)
                        System.Threading.Thread.Sleep(0);
                }
                catch (Exception ex)
                {
                    throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Exception migrating flight {0}: {1}", idFlight, ex.Message), ex);
                }
            });

            DateTime dtEnd = DateTime.Now;

            return String.Format(CultureInfo.InvariantCulture, "{0} flights migrated, elapsed: {1} seconds", cFlightsMigrated, dtEnd.Subtract(dtStart).TotalSeconds);
        }

        /// <summary>
        /// Migrates Telemetry FROM files TO the database
        /// </summary>
        /// <param name="szUser">The user for whom to move files</param>
        /// <param name="limit">The max number of files to move; 50 if not provided.</param>
        /// <returns>Status of the result</returns>
        /// <exception cref="MyFlightbookException"></exception>
        public static string MigrateFROMFilesToDB(string szUser = null, int limit = 50)
        {
            DateTime dtStart = DateTime.Now;

            // for memory, just get the ID's up front, then we'll do flights one at a time.
            List<TelemetryReference> lst = new List<TelemetryReference>();
            DBHelper dbh = new DBHelper((String.IsNullOrEmpty(szUser) ? "SELECT * from FlightTelemetry" : "SELECT ft.* FROM FlightTelemetry ft INNER JOIN flights f ON ft.idflight=f.idflight WHERE f.username=?szuser") + " LIMIT " + limit.ToString(CultureInfo.InvariantCulture));
            dbh.ReadRows((comm) =>
            {
                comm.Parameters.AddWithValue("szuser", szUser);
                comm.CommandTimeout = 300;
            }, (dr) => { lst.Add(new TelemetryReference(dr)); });

            string szStatus = string.Empty;

            // HACK: Force load the flight, so provide a non-empty username. 
            if (String.IsNullOrEmpty(szUser))
                szUser = "ADMIN";

            int cFlightsMigrated = 0;
            lst.ForEach((tr) =>
            {
                if (!tr.FlightID.HasValue)
                {
                    szStatus += "Flight ID without a value!!! ";
                }
                else
                {
                    new LogbookEntry(tr.FlightID.Value, szUser, LogbookEntryCore.LoadTelemetryOption.LoadAll, true).MoveTelemetryToFlightEntry();
                    cFlightsMigrated++;
                }
                if ((cFlightsMigrated % 20) == 0)
                    System.Threading.Thread.Sleep(0);
            });

            DateTime dtEnd = DateTime.Now;

            szStatus += String.Format(CultureInfo.InvariantCulture, "{0} flights migrated, elapsed: {1} seconds", cFlightsMigrated, dtEnd.Subtract(dtStart).TotalSeconds);
            return szStatus;
        }

        /// <summary>
        /// Get all telemetry for the specified user
        /// </summary>
        /// <param name="szUser">User to view</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<TelemetryReference> ADMINTelemetryForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            List<TelemetryReference> lst = new List<TelemetryReference>();

            DBHelper dbh = new DBHelper(@"SELECT 
	f.idflight, 
    Length(telemetry) AS compressed, 
    length(uncompress(telemetry)) AS uncompressed, 
    CAST(UNCOMPRESS(telemetry) AS CHAR) AS telemetry,
    ft.*,
    IF(ft.idflight IS NULL, 0 , 1) AS HasFT
FROM flights f 
	LEFT JOIN FlightTelemetry ft ON f.idflight=ft.idflight
WHERE username=?szUser AND Coalesce(f.telemetry, ft.idflight) IS NOT NULL 
ORDER BY f.idFlight DESC;");
            
            lst.Clear();
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("szUser", szUser); },
                (dr) =>
                {
                    TelemetryReference tr = (Convert.ToInt32(dr["HasFT"], CultureInfo.InvariantCulture) == 0) ?
                        new TelemetryReference(dr["telemetry"].ToString(), Convert.ToInt32(dr["idflight"], CultureInfo.InvariantCulture)) { Compressed = Convert.ToInt32(dr["compressed"], CultureInfo.InvariantCulture) } :
                        new TelemetryReference(dr) { Compressed = 0, RawData = dr["telemetry"].ToString() };
                    lst.Add(tr);
                });

            return lst;
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "Uncompressed {0} Compressed {1} {2}", Uncompressed, Compressed, GoogleData.ToString());
        }
    }

    /// <summary>
    /// A match result when importing telemetry
    /// </summary>
    public class TelemetryMatchResult
    {
        #region properties
        public string TelemetryFileName { get; set; }
        public int FlightID { get; set; }
        public bool Success { get; set; }
        public string Status { get; set; }
        public string MatchedFlightDescription { get; set; }
        public DateTime? Date { get; set; }
        public double TimeZoneOffset { get; set; }
        public DateTime AdjustedDate { get { return Date.HasValue ? Date.Value.AddHours(TimeZoneOffset) : DateTime.MinValue; } }

        #region Display properties
        public string CssClass { get { return Success ? "success" : "error"; } }
        public string DateDisplay { get { return Date.HasValue ? Date.Value.ToShortDateString() : string.Empty; } }
        public string AdjustedDateDisplay { get { return Date.HasValue ? AdjustedDate.ToShortDateString() : string.Empty; } }
        public string MatchHREF { get { return "~/mvc/pub/ViewFlight/" + FlightID.ToString(CultureInfo.CurrentCulture); } }
        #endregion
        #endregion;

        public TelemetryMatchResult()
        {
            FlightID = LogbookEntry.idFlightNone;
            Success = false;
            MatchedFlightDescription = Status = TelemetryFileName = string.Empty;
        }

        /// <summary>
        /// Matches a set of telemetry files to a set of flights for a user for bulk import.
        /// </summary>
        /// <param name="userName">The user name</param>
        /// <param name="tz">the flights to use</param>
        /// <param name="d">A dictionary mapping a filename to the data for that flight</param>
        /// <returns>An enumerable of TelemetryMatchResults</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<TelemetryMatchResult> MatchToFlights(string userName, string importTimeZone, IDictionary<string, string> d)
        {
            if (String.IsNullOrEmpty(userName))
                throw new ArgumentNullException(nameof(userName));
            if (d == null)
                throw new ArgumentNullException(nameof(d));

            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(importTimeZone);
            List<TelemetryMatchResult> lst = new List<TelemetryMatchResult>();
            foreach (string fname in d.Keys)
            {
                TelemetryMatchResult tmr = new TelemetryMatchResult() { TelemetryFileName = fname };
                if (tz != null)
                    tmr.TimeZoneOffset = tz.BaseUtcOffset.TotalHours;

                DateTime? dtFallback = null;
                MatchCollection mc = RegexUtility.RegexYMDDate.Matches(fname);
                if (mc != null && mc.Count > 0 && mc[0].Groups.Count > 0)
                {
                    if (DateTime.TryParse(mc[0].Groups[0].Value, out DateTime dt))
                    {
                        dtFallback = dt;
                        tmr.TimeZoneOffset = 0; // do it all in local time.
                    }
                }

                tmr.MatchToFlight(d[fname], userName, dtFallback);
                lst.Add(tmr);
            }
            return lst;
        }

        private static DateTime? ExtractDate(string szData, out LatLong ll)
        {
            ll = null;
            using (FlightData fd = new FlightData())
            {
                if (fd.ParseFlightData(szData))
                {
                    if (fd.HasLatLongInfo)
                    {
                        LatLong[] rgll = fd.GetPath();
                        if (rgll != null && rgll.Length > 0)
                            ll = rgll[0];
                    }
                    if (fd.HasDateTime && fd.Data.Rows.Count > 0)
                        return (DateTime)fd.Data.Rows[0][fd.Data.DateColumn];
                }
                else
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.ImportErrCantParse, fd.ErrorString));
            }
            return null;
        }

        public void MatchToFlight(string szData, string szUser, DateTime? dtFallback)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new UnauthorizedAccessException();

            try
            {
                Date = ExtractDate(szData, out LatLong ll);

                if (!Date.HasValue)
                    Date = dtFallback.HasValue ? (DateTime?)dtFallback.Value : throw new MyFlightbookException(Resources.FlightData.ImportErrNoDate);

                FlightQuery fq = new FlightQuery(szUser) { DateRange = FlightQuery.DateRanges.Custom, DateMin = AdjustedDate, DateMax = AdjustedDate };
                List<LogbookEntry> lstLe = new List<LogbookEntry>();

                DBHelper dbh = new DBHelper(LogbookEntry.QueryCommand(fq));
                dbh.ReadRows((comm) => { }, (dr) => { lstLe.Add(new LogbookEntry(dr, fq.UserName)); });

                LogbookEntry leMatch = null;

                if (lstLe.Count == 0)
                    throw new MyFlightbookException(Resources.FlightData.ImportErrNoMatch);

                else if (lstLe.Count == 1)
                    leMatch = lstLe[0];
                else if (lstLe.Count > 1)
                {
                    if (ll == null)
                        throw new MyFlightbookException(Resources.FlightData.ImportErrMultiMatchNoLocation);

                    List<LogbookEntry> lstPossibleMatches = new List<LogbookEntry>();
                    foreach (LogbookEntry le in lstLe)
                    {
                        AirportList apl = new AirportList(le.Route);
                        airport[] rgRoute = apl.GetNormalizedAirports();
                        if (rgRoute.Length > 0)
                        {
                            if (ll.DistanceFrom(rgRoute[0].LatLong) < 5)  // take anything within 5nm of the departure airport
                                lstPossibleMatches.Add(le);
                        }
                    }
                    if (lstPossibleMatches.Count == 0)
                        throw new MyFlightbookException(Resources.FlightData.ImportErrMultiMatchNoneClose);
                    else if (lstPossibleMatches.Count == 1)
                        leMatch = lstPossibleMatches[0];
                    else if (lstPossibleMatches.Count > 1)
                    {
                        const int maxTimeDiscrepancy = 20;
                        // find best starting time.
                        List<LogbookEntry> lstMatchesByTime = Date.HasValue ? lstPossibleMatches.FindAll(le =>
                            (le.EngineStart.HasValue() && Math.Abs(le.EngineStart.Subtract(Date.Value).TotalMinutes) < maxTimeDiscrepancy) ||
                            (le.FlightStart.HasValue() && Math.Abs(le.FlightStart.Subtract(Date.Value).TotalMinutes) < maxTimeDiscrepancy)) :
                            new List<LogbookEntry>();

                        leMatch = lstMatchesByTime.Count == 1
                            ? lstMatchesByTime[0]
                            : throw new MyFlightbookException(Resources.FlightData.ImportErrMultiMatchMultiClose);
                    }
                }

                if (leMatch != null)
                {
                    leMatch.FlightData = szData;
                    leMatch.FCommit(true);
                    Success = true;
                    Status = Resources.FlightData.ImportStatusMatch;
                    MatchedFlightDescription = leMatch.Date.ToShortDateString() + Resources.LocalizedText.LocalizedSpace + leMatch.Route + Resources.LocalizedText.LocalizedSpace + leMatch.Comment;
                    FlightID = leMatch.FlightID;
                }
            }
            catch (MyFlightbookException ex)
            {
                Success = false;
                Status = ex.Message;
            }
        }
    }

}