using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2012-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Charting
{
    public partial class Controls_GoogleChart : UserControl
    {
        #region properties
        #region private vars to back initialized properties
        private int m_Width = 800;
        private int m_Height = 400;
        private int m_tickSpacing = 1;
        private List<object> m_XVals;
        private List<object> m_YVals;
        private List<object> m_Y2Vals;
        private List<string> m_toolTips;
        private GoogleColumnDataType m_XDataType = GoogleColumnDataType.@string;
        private GoogleColumnDataType m_YDataType = GoogleColumnDataType.number;
        private GoogleColumnDataType m_Y2DataType = GoogleColumnDataType.number;
        #endregion

        #region ViewState keys
        private const string szVSKeyXVals = "XValArrayVS";
        private const string szVSKeyYVals = "YValArrayVS";
        private const string szVSKeyY2Vals = "Y2ValArrayVS";
        private const string szVSKeyTooltips = "YValToolTips";
        private const string szVSTickSpacing = "tickspacingVS";
        #endregion

        public IList<object> XVals
        {
            get
            {
                if (m_XVals == null)
                    m_XVals = (List<object>)ViewState[szVSKeyXVals];
                if (m_XVals == null)
                    ViewState[szVSKeyXVals] = m_XVals = new List<object>();
                return m_XVals;
            }
        }

        public IList<object> YVals
        {
            get
            {
                if (m_YVals == null)
                    m_YVals = (List<object>)ViewState[szVSKeyYVals];
                if (m_YVals == null)
                    ViewState[szVSKeyYVals] = m_YVals = new List<object>();
                return m_YVals;
            }
        }

        public IList<object> Y2Vals
        {
            get
            {
                if (m_Y2Vals == null)
                    m_Y2Vals = (List<object>)ViewState[szVSKeyY2Vals];
                if (m_Y2Vals == null)
                    ViewState[szVSKeyY2Vals] = m_Y2Vals = new List<object>();
                return m_Y2Vals;
            }
        }

        public IList<string> Tooltips
        {
            get
            {
                if (m_toolTips == null)
                    m_toolTips = (List<string>)ViewState[szVSKeyTooltips];
                if (m_toolTips == null)
                    ViewState[szVSKeyTooltips] = m_toolTips = new List<string>();
                return m_toolTips;
            }
        }

        public bool ShowAverage { get; set; }

        public double AverageValue { get; set; }

        public int Width
        {
            get { return m_Width; }
            set { m_Width = value; }
        }

        public int Height
        {
            get { return m_Height; }
            set { m_Height = value; pnlChart.Height = Unit.Pixel(value); }
        }

        public GoogleChartType ChartType { get; set; }
        public GoogleSeriesType Chart2Type { get; set; }
        public GoogleLegendType LegendType { get; set; }
        public GoogleColumnDataType XDataType
        {
            get { return m_XDataType; }
            set { m_XDataType = value; }
        }

        public bool UseMonthYearDate { get; set; }

        public GoogleColumnDataType YDataType
        {
            get { return m_YDataType; }
            set { m_YDataType = value; }
        }

        public GoogleColumnDataType Y2DataType
        {
            get { return m_Y2DataType; }
            set { m_Y2DataType = value; }
        }

        public string XLabel { get; set; }
        public string YLabel { get; set; }
        public string Y2Label { get; set; }
        public string AverageFormatString { get; set; }

        protected string AverageLabel { get { return String.Format(CultureInfo.CurrentCulture, AverageFormatString, AverageValue); } }
        public string Title { get; set; }

        /// <summary>
        /// Javascript to execute on a click
        /// 4 variables are set for context for this
        /// a) selectedItem.row: the index of the item that was clicked
        /// b) selectedItem.column: the column of the index that was clicked (0 = x values, 1 = y values, 2 = y2 values)
        /// c) value: the y-value of the item that was clicked
        /// d) xvalue: the x-value of the item that was clicked
        /// </summary>
        public string ClickHandlerJS { get; set; }

        /// <summary>
        /// Slant angle for ticks on the horizontal axis
        /// </summary>
        public int SlantAngle { get; set; }

        public int TickSpacing
        {
            get
            {
                if (ViewState[szVSTickSpacing] != null)
                    m_tickSpacing = (int)ViewState[szVSTickSpacing];
                return m_tickSpacing;
            }
            set
            {
                if (value <= 0)
                    throw new MyFlightbookException("Invalid Tickspacing for google chart");
                ViewState[szVSTickSpacing] = m_tickSpacing = value;
            }
        }

        protected bool HasY2
        {
            get { return Y2Vals.Count > 0; }
        }

        protected string Data
        {
            get
            {
                List<string> lst = new List<string>();
                bool fCustomTooltips = Tooltips.Any();
                for (int i = 0; i < XVals.Count; i++)
                    lst.Add(String.Format(CultureInfo.InvariantCulture, "[{0}, {1}{2}{3}{4}]",
                        GoogleChart.FormatObjectForTypeJS(XVals[i], XDataType),
                        YVals[i],
                        fCustomTooltips ? String.Format(CultureInfo.InvariantCulture, ", \"{0}\"", Tooltips[i]) : String.Empty,
                        (i < Y2Vals.Count) ? String.Format(CultureInfo.InvariantCulture, ", {0}", Y2Vals[i]) : string.Empty,
                        ShowAverage ? String.Format(CultureInfo.InvariantCulture, ", {0}", AverageValue) : string.Empty));

                return String.Join(", ", lst.ToArray());
            }
        }

        protected string ChartTypeString
        {
            get { return ChartType.ToString(); }
        }

        protected string Chart2TypeString
        {
            get { return Chart2Type.ToString(); }
        }

        protected string UseSlantedTextString
        {
            get { return (SlantAngle > 0) ? "true" : "false"; }
        }

        protected string XDataTypeString
        {
            get { return XDataType.ToString(); }
        }

        protected string YDataTypeString
        {
            get { return YDataType.ToString(); }
        }

        protected string Y2DataTypeString
        {
            get { return Y2DataType.ToString(); }
        }

        protected static string AverageDataTypeString
        {
            get { return GoogleColumnDataType.number.ToString(); }
        }

        protected string XDataFormat
        {
            get { return GoogleChart.FormatStringForType(XDataType, UseMonthYearDate, XDatePattern); }
        }

        /// <summary>
        /// Format pattern for the X axis, if it is a date and UseMonthYearDate isn't set.
        /// </summary>
        public string XDatePattern { get; set; }
        #endregion

        public void Clear()
        {
            Y2Vals.Clear();
            YVals.Clear();
            XVals.Clear();
            Tooltips.Clear();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Page.ClientScript.RegisterClientScriptInclude("GoogleJScript", "https://www.google.com/jsapi");
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "LoadGJScriptPackage", @"
google.load('visualization', '1.1', {packages:['corechart']});
var chartsToDraw = [];
  google.setOnLoadCallback(drawCharts);
  function drawCharts()
  {
    for (var i = 0; i < chartsToDraw.length; i++)
        chartsToDraw[i]();
  }", true);
        }
    }
}