using MyFlightbook.Charting;
using MyFlightbook.Geography;
using MyFlightbook.Telemetry.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2010-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Telemetry
{
    public enum DownloadFormat { Original, CSV, KML, GPX };

    public enum AltitudeUnitTypes { Feet, Meters };
    public enum SpeedUnitTypes { Knots, MilesPerHour, MetersPerSecond, FeetPerSecond, KmPerHour };

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
                        return TelemetryResources.dataTypeCSV;
                    case FileType.GPX:
                        return TelemetryResources.dataTypeGPX;
                    case FileType.KML:
                        return TelemetryResources.dataTypeKML;
                    case FileType.NMEA:
                        return TelemetryResources.dataTypeNMEA;
                    case FileType.IGC:
                        return TelemetryResources.dataTypeIGC;
                    case FileType.Airbly:
                        return TelemetryResources.dataTypeAirbly;
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

        public LatLong LatLongForRow(DataRow dr, out string htmlDesc)
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
                    DateTime dt = (DateTime)dr[xAxis];
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

        public virtual SpeedUnitTypes SpeedUnits
        {
            get { return SpeedUnitTypes.Knots; }
        }

        public virtual AltitudeUnitTypes AltitudeUnits
        {
            get { return AltitudeUnitTypes.Feet; }
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
}
