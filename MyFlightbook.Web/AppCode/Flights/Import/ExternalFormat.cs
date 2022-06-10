using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2017-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    /// <summary>
    /// Abstract base class for a flight entry from another format.
    /// </summary>
    public abstract class ExternalFormat
    {
        protected ExternalFormat() { }

        /// <summary>
        /// Create the entry from a datarow
        /// </summary>
        /// <param name="dr">A row in a data table</param>
        protected ExternalFormat(DataRow dr) : this() {
            if (dr == null)
                return;

            Type targetType = GetType();
            CultureInfo ci = dr.Table.Locale;

            // Initialize from each column
            foreach (DataColumn dc in dr.Table.Columns)
            {
                string propName = dc.ColumnName;
                object obj = dr[propName];
                if (obj == null || obj is DBNull)
                    continue;
                string szObj = obj as string;
                if (szObj != null && String.IsNullOrWhiteSpace(szObj))
                    continue;

                //	Get the matching property in the destination object
                try
                {
                    PropertyInfo targetObj = targetType.GetProperty(propName);
                    //	If there is none, skip
                    if (targetObj == null)
                        continue;

                    //	Set the value in the destination
                    if (targetObj.CanWrite)
                    {
                        object value = null;
                        switch (targetObj.PropertyType.Name.ToUpperInvariant())
                        {
                            case "INT":
                            case "INT32":
                            case "INT64":
                                value = Convert.ToInt32(obj, ci);
                                break;
                            case "BOOL":
                            case "BOOLEAN":
                                {
                                    if (!Boolean.TryParse(szObj, out bool f))
                                    {
                                        if (int.TryParse(szObj, out int i))
                                            f = i != 0;
                                    }
                                    value = f;
                                }
                                break;
                            case "STRING":
                                value = obj.ToString();
                                break;
                            case "DOUBLE":
                            case "FLOAT":
                            case "DECIMAL":
                                value = (decimal)(obj.ToString()).SafeParseDecimal();
                                break;
                            case "DATETIME":
                                value = Convert.ToDateTime(obj, ci);
                                break;
                            default:
                                continue;
                        }
                        targetObj.SetValue(this, value);
                    }
                }
                catch (Exception ex) when (ex is AmbiguousMatchException || ex is ArgumentNullException || ex is FormatException) { }
            }
        }

        /// <summary>
        /// Converts the entry to an internal format
        /// </summary>
        /// <returns></returns>
        public abstract LogbookEntry ToLogbookEntry();

        #region Utility functions

        /// <summary>
        /// Get the most likely match for a specified tail
        /// </summary>
        /// <param name="szUser">User whose aircraft are being searched</param>
        /// <param name="szTail">Tail number as key</param>
        /// <returns>ID of likely aircraft, else idAircraftUnknown if no likely match</returns>
        public static Aircraft BestGuessAircraftID(string szUser, string szTail)
        {
            if (String.IsNullOrWhiteSpace(szUser) || String.IsNullOrWhiteSpace(szTail))
                return null;
            UserAircraft ua = new UserAircraft(szUser);
            szTail = Aircraft.NormalizeTail(szTail);
            IDictionary<string, Aircraft> d = ua.DictAircraftForUser();
            return ua[szTail] ?? (d.ContainsKey(szTail) ? d[szTail] : null);
        }

        #region String merging
        /// <summary>
        /// Joins strings with a space, excluding any empty strings and trimming as needed.  Useful for things like merging From/Via/To or multiple comments fields
        /// </summary>
        /// <param name="rgIn"></param>
        /// <returns>The merged strings</returns>
        protected static string JoinStrings(IEnumerable<string> rgIn)
        {
            if (rgIn == null)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            foreach (string sz in rgIn)
            {
                if (!String.IsNullOrWhiteSpace(sz))
                {
                    sb.Append(sz.Trim());
                    sb.Append(Resources.LocalizedText.LocalizedSpace);
                }
            }
            return sb.ToString().Trim();
        }
        #endregion

        #region Times
        /// <summary>
        /// LogTen Pro and Foreflight have a number of fields that are simple hour/minutes (e.g., actual/scheduled departure/arrival, which makes it hard to convert to UTC DateTimes.
        /// This takes the date of flight and a date to fix (which is presumed to be simply hh:mm; it picked up "today" when it was read from the data table) and creates
        /// a new UTC datetime that has the date of the flight but the time of the date to fix.
        /// 
        /// It also takes an optional reference date and bumps up by a day if it is less than that datetime.  
        /// E.g., if you have a departure time of 23:14 and an arrival time of 07:35, it's pretty clear that the 7:35 arrival should be +1 day.
        /// 
        /// So best is to call this ONLY AFTER all fields are populated.
        /// 
        /// It will also ONLY make adjustments if the date of flight and specified datetime differ by a day or less (i.e., to avoid double-adjusting if it's actually fully specified)
        /// </summary>
        /// <param name="dtFlight">Date of flight</param>
        /// <param name="dtToFix">Time to fix</param>
        /// <param name="dtRef">Reference date; can be null</param>
        /// <returns>A fixed-up datetime that has the hour/minute component from the original date, but the date of flight (or date of flight +1) for the date portion.</returns>
        protected static DateTime FixedUTCDateFromTime(DateTime dtFlight, DateTime dtToFix, DateTime? dtRef = null)
        {
            if (!dtToFix.HasValue() || !dtFlight.HasValue() || Math.Abs(dtToFix.Subtract(dtFlight).TotalDays) <= 1)
                return dtToFix;

            dtToFix = new DateTime(dtFlight.Year, dtFlight.Month, dtFlight.Day, dtToFix.Hour, dtToFix.Minute, dtToFix.Second, DateTimeKind.Utc);
            if (dtRef != null && dtRef.HasValue && dtRef.Value.HasValue() && dtRef.Value.CompareTo(dtToFix) > 0)
                dtToFix = dtToFix.AddDays(1);

            return dtToFix;
        }
        #endregion
        #endregion
    }

    public class ExternalFormatConvertResults
    {
        public string ResultString { get { return InputStatus.ToString(); } }

        private CSVAnalyzer.CSVStatus InputStatus { get; set; }

        public string AuditResult { get; private set; }

        private byte[] _newRGB;

        public byte[] GetConvertedBytes() { return _newRGB; }

        public bool IsFixed { get { return InputStatus == CSVAnalyzer.CSVStatus.Fixed; } }

        public bool IsBroken { get { return InputStatus == CSVAnalyzer.CSVStatus.Broken; } }

        public bool IsOK { get { return InputStatus != CSVAnalyzer.CSVStatus.Broken; } }

        public bool IsFixedOrBroken { get { return InputStatus != CSVAnalyzer.CSVStatus.OK; } }

        public bool IsPendingOnly { get; private set; }

        public string ConvertedName { get; private set; }

        public static ExternalFormatConvertResults ConvertToCSV(byte[] rgb)
        {
            ExternalFormatConvertResults result = new ExternalFormatConvertResults
            {
                // issue #280: some files have \r\r\n as line separators!
                _newRGB = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(rgb).Replace("\r\r\n", "\r\n")),

                InputStatus = CSVAnalyzer.CSVStatus.OK
            };

            // Validate the file
            ExternalFormatImporter efi = ExternalFormatImporter.GetImporter(rgb);
            if (efi != null)
            {
                try
                {
                    rgb = efi.PreProcess(rgb);
                    result.IsPendingOnly = efi.IsPendingOnly;
                }
                catch (Exception ex) when (ex is MyFlightbookException || ex is MyFlightbookValidationException)
                {
                    result.InputStatus = CSVAnalyzer.CSVStatus.Broken;
                    result.AuditResult = ex.Message;
                }
            }

            if (result.InputStatus != CSVAnalyzer.CSVStatus.Broken)
            {
                using (DataTable dt = new DataTable() { Locale = CultureInfo.CurrentCulture })
                {
                    CSVAnalyzer csvAnalyzer;
                    using (MemoryStream ms = new MemoryStream(rgb))
                    {
                        csvAnalyzer = new CSVAnalyzer(ms, dt);
                    }
                    result.InputStatus = csvAnalyzer.Status;

                    if (result.InputStatus != CSVAnalyzer.CSVStatus.Broken)
                    {
                        string szCSV = null;
                        if (efi == null)    // was already CSV - only update it if it was fixed (vs. broken)
                        {
                            if (result.InputStatus == CSVAnalyzer.CSVStatus.Fixed)
                                szCSV = csvAnalyzer.DataAsCSV;
                        }
                        else  // But if it was converted, ALWAYS update the CSV.
                            szCSV = efi.CSVFromDataTable(csvAnalyzer.Data);

                        if (szCSV != null)
                            result._newRGB = rgb = Encoding.UTF8.GetBytes(szCSV);

                        // And show conversion, if it was converted
                        if (efi != null)
                            result.ConvertedName = efi.Name;
                    }

                    result.AuditResult = csvAnalyzer.Audit;
                }
            }

            return result;
        }
    }

    public abstract class ExternalFormatImporter
    {
        private readonly static ExternalFormatImporter[] rgFormatters = { new LogTenProImporter(), new ForeFlightImporter(), new eLogSiteImporter(), new MccPilotImporter(), new CrewTracImporter(), new RosterBusterImporter(), new CrewLog(), new TASCImporter(), new AASchedulerImporter(), new eCrew(), new FFDOImporter() };

        /// <summary>
        /// Initializes an enumerable of the external format from a datatable.
        /// </summary>
        /// <param name="dt"></param>
        public abstract IEnumerable<ExternalFormat> FromDataTable(DataTable dt);

        /// <summary>
        /// Creates a converted CSV file from the data table.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public abstract string CSVFromDataTable(DataTable dt);

        /// <summary>
        /// Looks at a datatable and decides if it is a format that it can understand.
        /// </summary>
        /// <param name="rgb">The input bytes.</param>
        /// <returns>True if the data table represents this particular external format.</returns>
        public abstract bool CanParse(byte[] rgb);

        /// <summary>
        /// Performs any pre-processing on the file, advancing the stream to the point where the import can begin in proper.
        /// </summary>
        /// <param name="rgb">The input bytes</param>
        /// <returns>A (possibly) modified byte array to parse as CSV.</returns>
        /// <remarks>MUST NOT RETURN NULL</remarks>
        public virtual byte[] PreProcess(byte[] rgb) { return rgb; }

        /// <summary>
        /// Returns the name of the file format being repsresneted.
        /// </summary>
        public abstract string Name { get;  }

        /// <summary>
        /// Indicates that errors for this format should be ignored - flights should only go into pending.  Useful if the format is for scheduled flights, or if it omits aircraft
        /// </summary>
        public virtual bool IsPendingOnly { get { return false; } }

        /// <summary>
        /// Gets the best importer for the specified data
        /// </summary>
        /// <param name="rgb">The input bytes!</param>
        /// <returns>The format importer</returns>
        public static ExternalFormatImporter GetImporter(byte[] rgb)
        {
            if (rgb == null)
                throw new ArgumentNullException(nameof(rgb));

            foreach (ExternalFormatImporter efi in rgFormatters)
            {
                if (efi.CanParse(rgb))
                    return efi;
            }

            return null;
        }
    }
}