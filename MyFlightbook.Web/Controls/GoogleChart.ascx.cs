using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2012-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Charting
{
    public partial class Controls_GoogleChart : UserControl
    {
        #region properties
        private const string szVSGCData = "vsGoogleChartData";
        /// <summary>
        /// The data and its options for the chart to render.
        /// </summary>
        public GoogleChartData ChartData
        {
            get
            {
                GoogleChartData gcd = (GoogleChartData)ViewState[szVSGCData];
                if (gcd == null)
                    ViewState[szVSGCData] = gcd = new GoogleChartData();
                return gcd;
            }
            set
            {
                ViewState[szVSGCData] = value;
            }
        }

        protected string ChartDataSerialized
        {
            get {
                ChartData.ContainerID = pnlChart.ClientID;
                return JsonConvert.SerializeObject(ChartData, new JsonConverter[] { new JavaScriptDateTimeConverter() }); }
        }

        #region Helper properties for in-line values
        public GoogleChartType ChartType
        {
            get { return ChartData.ChartType; }
            set { ChartData.ChartType = value; }
        }

        public GoogleSeriesType Chart2Type
        {
            get { return ChartData.Chart2Type; }
            set { ChartData.Chart2Type = value; }
        }

        public string Title
        {
            get { return ChartData.Title; }
            set { ChartData.Title = value; }
        }

        public GoogleColumnDataType XDataType
        {
            get { return ChartData.XDataType; }
            set { ChartData.XDataType = value; }
        }
        public GoogleColumnDataType YDataType
        {
            get { return ChartData.YDataType; }
            set { ChartData.YDataType = value; }
        }

        public GoogleColumnDataType Y2DataType
        {
            get { return ChartData.Y2DataType; }
            set { ChartData.Y2DataType = value; }
        }

        public int Height
        {
            get { return (int)ChartData.Height; }
            set
            {
                ChartData.Height = (uint)value;
                pnlChart.Height = Unit.Pixel(value);
            }
        }

        public int Width
        {
            get { return (int) ChartData.Width; }
            set { ChartData.Width = (uint) value; }
        }

        public string XLabel
        {
            get { return ChartData.XLabel; }
            set { ChartData.XLabel = value; }
        }

        public string YLabel
        {
            get { return ChartData.YLabel; }
            set { ChartData.YLabel = value; }
        }

        public string Y2Label
        {
            get { return ChartData.Y2Label; }
            set { ChartData.Y2Label = value; }
        }
        public GoogleLegendType LegendType
        {
            get { return ChartData.LegendType; }
            set { ChartData.LegendType = value; }
        }

        public bool UseMonthYearDate
        {
            get { return ChartData.UseMonthYearDate; }
            set { ChartData.UseMonthYearDate = value; }
        }

        public int SlantAngle
        {
            get { return ChartData.SlantAngle; }
            set{ ChartData.SlantAngle = value; }
        }

        public int TickSpacing
        {
            get { return (int) ChartData.TickSpacing; }
            set { ChartData.TickSpacing = (uint) value; }
        }

        public string AverageFormatString
        {
            get { return ChartData.AverageFormatString; }
            set { ChartData.AverageFormatString = value; }
        }
        #endregion
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            Page.ClientScript.RegisterClientScriptInclude("GoogleJScript", "https://www.google.com/jsapi");
            Page.ClientScript.RegisterClientScriptInclude("gchart", ResolveClientUrl("~/public/Scripts/gchart.js?v=3"));
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "LoadGJScriptPackage", @"
google.load('visualization', '1.1', {packages:['corechart']});
var chartsToDraw = [];
var chartData = [];
  google.setOnLoadCallback(drawCharts);
  function drawCharts()
  {
    for (var i = 0; i < chartsToDraw.length; i++) {
        var cd = drawGChart(chartsToDraw[i]);
        chartData.push(cd);
    }
  }", true);
        }
    }
}