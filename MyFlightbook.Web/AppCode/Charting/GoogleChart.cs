using MyFlightbook.Telemetry;
using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Charting
{
    public enum GoogleChartType { LineChart, ColumnChart, ComboChart };
    public enum GoogleSeriesType { line, bars };
    public enum GoogleLegendType { none, top, left, right, bottom };
    public enum GoogleColumnDataType { @string, number, boolean, date, datetime, timeofday };

    public static class GoogleChart
    {
        public static GoogleColumnDataType GoogleTypeFromKnownColumnType(KnownColumnTypes kct)
        {
            switch (kct)
            {
                case KnownColumnTypes.ctDateTime:
                    return GoogleColumnDataType.datetime;
                case KnownColumnTypes.ctDec:
                case KnownColumnTypes.ctFloat:
                case KnownColumnTypes.ctInt:
                case KnownColumnTypes.ctLatLong:
                    return GoogleColumnDataType.number;
                case KnownColumnTypes.ctPosition:
                case KnownColumnTypes.ctString:
                default:
                    return GoogleColumnDataType.@string;
            }
        }

        /// <summary>
        /// Writes out an object in Javascript literal notation based on its type.
        /// </summary>
        /// <param name="o">The object</param>
        /// <param name="gcdt">The column data type</param>
        /// <returns>The javascript literal notation for the object</returns>
        public static string FormatObjectForTypeJS(object o, GoogleColumnDataType gcdt)
        {
            if (o == null)
                throw new ArgumentNullException(nameof(o));

            switch (gcdt)
            {
                case GoogleColumnDataType.date:
                case GoogleColumnDataType.datetime:
                case GoogleColumnDataType.timeofday:
                    return String.Format(CultureInfo.InvariantCulture, "new Date({0})", ((DateTime)o).ToString("yyyy, M - 1, d, H, m, s", System.Globalization.CultureInfo.InvariantCulture));
                case GoogleColumnDataType.boolean:
                    return ((bool)o) ? "true" : "false";
                case GoogleColumnDataType.number:
                    return o.ToString();
                default:
                case GoogleColumnDataType.@string:
                    return String.Format(CultureInfo.InvariantCulture, "'{0}'", o.ToString());
            }
        }

        /// <summary>
        /// Provides the format string for an axis
        /// </summary>
        /// <param name="gcdt">The column data type</param>
        /// <param name="datePattern">Date pattern, if it's a date</param>
        /// <param name="fUseMonthYearDate">True to use a month/year (no day) notation</param>
        /// <returns>The Format string</returns>
        public static string FormatStringForType(GoogleColumnDataType gcdt, bool fUseMonthYearDate = false, string datePattern = null)
        {
            switch (gcdt)
            {
                case GoogleColumnDataType.date:
                    return fUseMonthYearDate ? "MMM yyyy" : (String.IsNullOrWhiteSpace(datePattern) ? CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern : datePattern);
                case GoogleColumnDataType.datetime:
                    return CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + "HH:mm";
                case GoogleColumnDataType.timeofday:
                    return CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
                default:
                case GoogleColumnDataType.number:
                case GoogleColumnDataType.boolean:
                case GoogleColumnDataType.@string:
                    return String.Empty;
            }
        }

    }
}