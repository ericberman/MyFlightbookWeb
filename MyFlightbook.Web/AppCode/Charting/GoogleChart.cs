using MyFlightbook.Telemetry;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2020-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Charting
{
    public enum GoogleChartType { LineChart, ColumnChart, ComboChart };
    public enum GoogleSeriesType { line, bars };
    public enum GoogleLegendType { none, top, left, right, bottom };
    public enum GoogleColumnDataType { @string, number, boolean, date, datetime, timeofday };

    /// <summary>
    /// Class to encapsulate all of the data to display in a chart.
    /// </summary>
    [Serializable]
    public class GoogleChartData
    {
        #region Properties
        /// <summary>
        /// Values for the X axis
        /// </summary>
        [JsonIgnore]
        public IList<object> XVals { get; private set; } = new List<object>();

        /// <summary>
        /// Values for the Y axis
        /// </summary>
        [JsonIgnore]
        public IList<object> YVals { get; private set; } = new List<object>();

        /// <summary>
        /// Values for the 2nd Y axis
        /// </summary>
        [JsonIgnore]
        public IList<object> Y2Vals { get; private set; } = new List<object>();

        /// <summary>
        /// Does this chart contain secondary data?
        /// </summary>
        public bool HasY2
        {
            get { return Y2Vals.Count > 0; }
        }

        public bool ShowTrendline { get; set; }

        public string TrendlineLabel
        {
            get { return Resources.FlightData.TrendLineLabel; }
        }

        /// <summary>
        /// Tooltips for the values.  This is a parallel array to the X points
        /// </summary>
        [JsonIgnore]
        public IList<string> Tooltips { get; private set; } = new List<string>();

        /// <summary>
        /// Does this chart contain tooltips? 
        /// </summary>
        public bool HasTooltips { get { return Tooltips.Count > 0; } }

        /// <summary>
        /// Width for the chart
        /// </summary>
        public uint Width { get; set; } = 800;

        /// <summary>
        /// Height for the chart.  
        /// </summary>
        public uint Height { get; set; } = 400;

        /// <summary>
        /// The type of the chart for primary (Y1) data
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public GoogleChartType ChartType { get; set; }

        /// <summary>
        /// The type of the chart for secondary (Y2) data
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public GoogleSeriesType Chart2Type { get; set; }

        /// <summary>
        /// The legend position for the chart
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public GoogleLegendType LegendType { get; set; }

        /// <summary>
        /// True to format dates as Month/year/date
        /// </summary>
        public bool UseMonthYearDate { get; set; }

        /// <summary>
        /// Format pattern for the X axis, if it is a date and UseMonthYearDate isn't set.
        /// </summary>
        public string XDatePattern { get; set; }

        /// <summary>
        /// The data type for the x-data
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public GoogleColumnDataType XDataType { get; set; } = GoogleColumnDataType.@string;

        /// <summary>
        /// Data type for the primary data
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public GoogleColumnDataType YDataType { get; set; } = GoogleColumnDataType.number;

        /// <summary>
        /// Data type for the 2ndary data
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public GoogleColumnDataType Y2DataType { get; set; } = GoogleColumnDataType.number;

        /// <summary>
        /// Label for the X-axis
        /// </summary>
        public string XLabel { get; set; }

        /// <summary>
        /// Label for the primary Y-axis
        /// </summary>
        public string YLabel { get; set; }

        /// <summary>
        /// Label for the secondary Y-axis
        /// </summary>
        public string Y2Label { get; set; }

        /// <summary>
        /// Indicates whether the average should be shown.
        /// </summary>
        public bool ShowAverage { get; set; }

        /// <summary>
        /// Average value
        /// </summary>
        public double AverageValue { get; set; }

        /// <summary>
        /// Format string for the average
        /// </summary>
        public string AverageFormatString { get; set; }

        /// <summary>
        /// Label for the average, formatted using the AverageFormatString
        /// </summary>
        public string AverageLabel { get { return String.IsNullOrEmpty(AverageFormatString) ? AverageValue.ToString(CultureInfo.CurrentCulture) :String.Format(CultureInfo.CurrentCulture, AverageFormatString, AverageValue); } }

        /// <summary>
        /// Title for the chart
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Javascript to execute on a click.
        /// Function follows the form:
        /// f(row, column, xval, yval), where:
        ///  - row = the selected row
        ///  - column = the selected column
        ///  - xval = the x-value of the item that was clicked
        ///  - yval = the y-value of the item that was clicked.
        /// </summary>
        /// </summary>
        [JsonIgnore]
        public string ClickHandlerJS { get; set; }

        /// <summary>
        /// Javascript to execute on a click.
        /// Function follows the form:
        /// f(row, column, xval, yval), where:
        ///  - row = the selected row
        ///  - column = the selected column
        ///  - xval = the x-value of the item that was clicked
        ///  - yval = the y-value of the item that was clicked.
        /// </summary>
        public JRaw ClickHandlerFunction 
        { 
            get { return String.IsNullOrEmpty(ClickHandlerJS) ? null : new JRaw(ClickHandlerJS); }
        }

        /// <summary>
        /// Slant angle for ticks on the horizontal axis; default is 0
        /// </summary>
        public int SlantAngle { get; set; }

        /// <summary>
        /// Spacing (of samples) for ticks.
        /// </summary>
        public uint TickSpacing { get; set; } = 1;

        /// <summary>
        /// HTML ID of the container div
        /// </summary>
        public string ContainerID { get; set; }
        #endregion

        public GoogleChartData() { }

        public void Clear()
        {
            Y2Vals.Clear();
            YVals.Clear();
            XVals.Clear();
            Tooltips.Clear();
        }

        /// <summary>
        /// Provides the format string for an axis
        /// </summary>
        /// <param name="gcdt">The column data type</param>
        /// <returns>The Format string</returns>
        public string XFormatString
        {
            get
            {
                switch (XDataType)
                {
                    case GoogleColumnDataType.date:
                        return UseMonthYearDate ? "MMM yyyy" : (String.IsNullOrWhiteSpace(XDatePattern) ? CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern : XDatePattern);
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

        private static object ObjectForJS(object o, GoogleColumnDataType gcdt)
        {
            if (o == null)
                throw new ArgumentNullException(nameof(o));

            switch (gcdt)
            {
                case GoogleColumnDataType.date:
                case GoogleColumnDataType.datetime:
                case GoogleColumnDataType.timeofday:
                    return ((DateTime)o);
                case GoogleColumnDataType.boolean:
                    return ((bool)o);
                case GoogleColumnDataType.number:
                    return Convert.ToDouble(o, CultureInfo.InvariantCulture);
                default:
                case GoogleColumnDataType.@string:
                    return o.ToString();
            }
        }

        /// <summary>
        /// Returns the data in a manner that can be JSON serialized.
        /// </summary>
        public IEnumerable<IEnumerable<object>> Data
        {
            get
            {
                List<List<object>> lst = new List<List<object>>();
                bool fCustomTooltips = Tooltips.Count > 0;
                for (int i = 0; i < XVals.Count; i++)
                {
                    List<object> row = new List<object>() { ObjectForJS(XVals[i], XDataType), YVals[i] };
                    if (fCustomTooltips)
                        row.Add(Tooltips[i]);
                    if (i < Y2Vals.Count)
                        row.Add(Y2Vals[i]);
                    if (ShowAverage)
                        row.Add(AverageValue);
                    lst.Add(row);
                }
                return lst;
            }
        }

        /// <summary>
        /// Extension method to convert from a KnownColumnType in FlightData to a GoogleColumnDataType
        /// </summary>
        /// <param name="kct"></param>
        /// <returns>The corresponding GoogleColumnDataType</returns>
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
    }
}