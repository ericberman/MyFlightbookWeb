using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;
using MyFlightbook.Telemetry;

/******************************************************
 * 
 * Copyright (c) 2012-2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_GoogleChart : System.Web.UI.UserControl
{
    public enum GoogleChartType { LineChart, ColumnChart, ComboChart };
    public enum GoogleSeriesType { line, bars };
    public enum GoogleLegendType { none, top, left, right, bottom };
    public enum GoogleColumnDataType {@string, number, boolean, date, datetime, timeofday};

    #region properties
    #region private vars to back initialized properties
    private int m_Width = 800;
    private int m_Height = 400;
    private int m_tickSpacing = 1;
    private ArrayList m_XVals = null;
    private ArrayList m_YVals = null;
    private ArrayList m_Y2Vals = null;
    private GoogleColumnDataType m_XDataType = GoogleColumnDataType.@string;
    private GoogleColumnDataType m_YDataType = GoogleColumnDataType.number;
    private GoogleColumnDataType m_Y2DataType = GoogleColumnDataType.number;
    #endregion

    #region ViewState keys
    private string szVSKeyXVals = "XValArrayVS";
    private string szVSKeyYVals = "YValArrayVS";
    private string szVSKeyY2Vals = "Y2ValArrayVS";
    private string szVSTickSpacing = "tickspacingVS";
    #endregion

    public ArrayList XVals
    {
        get 
        {
            if (m_XVals == null)
                m_XVals = (ArrayList)ViewState[szVSKeyXVals];
            if (m_XVals == null)
                ViewState[szVSKeyXVals] = m_XVals = new ArrayList();
            return m_XVals; 
        }
    }

    public ArrayList YVals
    {
        get
        {
            if (m_YVals == null)
                m_YVals = (ArrayList)ViewState[szVSKeyYVals];
            if (m_YVals == null)
                ViewState[szVSKeyYVals] = m_YVals = new ArrayList();
            return m_YVals;
        }
    }

    public ArrayList Y2Vals
    {
        get
        {
            if (m_Y2Vals == null)
                m_Y2Vals = (ArrayList)ViewState[szVSKeyY2Vals];
            if (m_Y2Vals == null)
                ViewState[szVSKeyY2Vals] = m_Y2Vals = new ArrayList();
            return m_Y2Vals;
        }
    }

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
        get {
            if (ViewState[szVSTickSpacing] != null)
                m_tickSpacing = (int)ViewState[szVSTickSpacing];
            return m_tickSpacing; 
        }
        set {
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
            for (int i = 0; i < XVals.Count; i++)
                lst.Add(String.Format(System.Globalization.CultureInfo.InvariantCulture, "[{0}, {1}{2}]", 
                    FormatObjectForTypeJS(XVals[i], XDataType),
                    YVals[i],
                    (i < Y2Vals.Count) ? String.Format(System.Globalization.CultureInfo.InvariantCulture, ", {0}", Y2Vals[i]) : ""));

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

    protected string XDataFormat
    {
        get { return FormatStringForType(XDataType); }
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
    }

    /// <summary>
    /// Writes out an object in Javascript literal notation based on its type.
    /// </summary>
    /// <param name="o">The object</param>
    /// <param name="gcdt">The column data type</param>
    /// <returns>The javascript literal notation for the object</returns>
    private string FormatObjectForTypeJS(object o, GoogleColumnDataType gcdt)
    {
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
    /// <returns>The Format string</returns>
    protected string FormatStringForType(GoogleColumnDataType gcdt)
    {
        switch (gcdt)
        {
            case GoogleColumnDataType.date:
                return UseMonthYearDate ? "MMM yyyy" : (String.IsNullOrWhiteSpace(XDatePattern) ? System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortDatePattern : XDatePattern);
            case GoogleColumnDataType.datetime:
                return System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + "HH:mm";
            case GoogleColumnDataType.timeofday:
                return System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortTimePattern;
            default:
            case GoogleColumnDataType.number:
            case GoogleColumnDataType.boolean:
            case GoogleColumnDataType.@string:
                return String.Empty;
        }
    }
    public GoogleColumnDataType GoogleTypeFromKnownColumnType(KnownColumnTypes kct)
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