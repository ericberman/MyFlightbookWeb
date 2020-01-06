using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2017-2019 MyFlightbook LLC
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
                                    Boolean f = false;
                                    if (!Boolean.TryParse(szObj, out f))
                                    {
                                        int i = 0;
                                        if (int.TryParse(szObj, out i))
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
                catch (AmbiguousMatchException) { }
                catch (ArgumentNullException) { }
                catch (FormatException) { }
            }
        }

        /// <summary>
        /// Converts the entry to an internal format
        /// </summary>
        /// <returns></returns>
        public abstract LogbookEntry ToLogbookEntry();

        #region Utility functions

        #region String merging
        /// <summary>
        /// Joins strings with a space, excluding any empty strings and trimming as needed.  Useful for things like merging From/Via/To or multiple comments fields
        /// </summary>
        /// <param name="rgIn"></param>
        /// <returns>The merged strings</returns>
        protected string JoinStrings(IEnumerable<string> rgIn)
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
        protected DateTime FixedUTCDateFromTime(DateTime dtFlight, DateTime dtToFix, DateTime? dtRef = null)
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

    public abstract class ExternalFormatImporter
    {
        private readonly static ExternalFormatImporter[] rgFormatters = { new LogTenProImporter(), new ForeFlightImporter(), new eLogSiteImporter(), new MccPilotImporter(), new CrewTracImporter(), new RosterBusterImporter(), new CrewLog(), new TASCImporter() };

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
                throw new ArgumentNullException("rgb");

            foreach (ExternalFormatImporter efi in rgFormatters)
            {
                if (efi.CanParse(rgb))
                    return efi;
            }

            return null;
        }
    }
}