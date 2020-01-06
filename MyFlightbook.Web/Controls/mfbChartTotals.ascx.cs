using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;
using MyFlightbook.Histogram;

/******************************************************
 * 
 * Copyright (c) 2009-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbChartTotals : System.Web.UI.UserControl
{
    /// <summary>
    /// Not persisted - source data, to potentially avoid hitting the database.
    /// </summary>
    public IEnumerable<IHistogramable> SourceData { get; set; }

    private const string szVSRawData = "keyViewStateRawData";

    public DateBucketManager.GroupingMode CurrentGrouping
    {
        get { return (DateBucketManager.GroupingMode)Enum.Parse(typeof(DateBucketManager.GroupingMode), cmbGrouping.SelectedValue); }
        set { cmbGrouping.SelectedValue = value.ToString(); }
    }

    private Bucket<DateTime>[] RawData
    {
        get { return (Bucket<DateTime>[])ViewState[szVSRawData]; }
        set { ViewState[szVSRawData] = value; }
    }

    protected LogbookEntryDisplay.HistogramSelector SelectedFieldToGraph
    {
        get { return (LogbookEntryDisplay.HistogramSelector)Enum.Parse(typeof(LogbookEntryDisplay.HistogramSelector), cmbFieldToview.SelectedValue); }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // verify that we have a valid user (should never be a problem)
            if (!Page.User.Identity.IsAuthenticated)
                return;

            MyFlightbook.Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);

            cmbFieldToview.Items.FindByValue("CFI").Enabled = pf.IsInstructor;
            cmbFieldToview.Items.FindByValue("SIC").Enabled = pf.TracksSecondInCommandTime;
        }
        else if (Visible)
            RefreshChartAndTable();
    }

    /// <summary>
    /// Updates the chart based on the computed data (stored in RawData)
    /// </summary>
    protected void RefreshChartAndTable()
    {
        gvRawData.DataSource = RawData;
        gvRawData.DataBind();

        LogbookEntryDisplay.HistogramSelector sel = SelectedFieldToGraph;
        Boolean fIsInt = (sel == LogbookEntryDisplay.HistogramSelector.Landings || sel == LogbookEntryDisplay.HistogramSelector.Approaches || sel == LogbookEntryDisplay.HistogramSelector.Flights || sel == LogbookEntryDisplay.HistogramSelector.FlightDays);

        gcTrends.Clear();
        foreach (Bucket<DateTime> b in RawData)
        {
            gcTrends.XVals.Add((DateTime) b.Ordinal);
            gcTrends.YVals.Add(b.Value);
            gcTrends.Y2Vals.Add(b.RunningTotal);
        }
        gcTrends.YLabel = String.Format(CultureInfo.CurrentCulture, fIsInt ? Resources.LocalizedText.ChartTotalsNumOfX : Resources.LocalizedText.ChartTotalsHoursOfX, cmbFieldToview.SelectedItem.Text);
        gcTrends.Y2Label = Resources.LocalizedText.ChartRunningTotal;
        gcTrends.ClickHandlerJS = String.Format(CultureInfo.InvariantCulture, "window.open('{0}?y=' + xvalue.getFullYear() {1} {2} {3}, '_blank').focus()",
            VirtualPathUtility.ToAbsolute("~/Member/LogbookNew.aspx"),
            CurrentGrouping == DateBucketManager.GroupingMode.Year ? string.Empty : " + '&m=' + xvalue.getMonth() ",
            CurrentGrouping == DateBucketManager.GroupingMode.Month ? string.Empty : " + '&d=' + xvalue.getDate()",
            CurrentGrouping == DateBucketManager.GroupingMode.Week ? " + '&w=1'" : string.Empty);

        pnlChart.Visible = true;
    }

    protected void cmbFieldToview_SelectedIndexChanged(object sender, EventArgs e)
    {
        Refresh(SourceData);
    }

    /// <summary>
    /// Recomputes the data from the datasource and refreshes it
    /// </summary>
    /// <param name="datasource">Histogrammable data that uses datetime for the buckets.</param>
    public void Refresh(IEnumerable<IHistogramable> datasource)
    {
        if (datasource == null)
            throw new ArgumentNullException("datasource");
        DateBucketManager bm = DateBucketManager.BucketManagerForGroupingMode(CurrentGrouping);
        Dictionary<string, object> context = new Dictionary<string, object>() { { LogbookEntryDisplay.HistogramContextSelectorKey, SelectedFieldToGraph } };
        bm.ScanData(datasource, context, true);
        // check for daily with less than a year
        if (CurrentGrouping == DateBucketManager.GroupingMode.Day && bm.MaxDate.CompareTo(bm.MinDate) > 0 && bm.MaxDate.Subtract(bm.MinDate).TotalDays > 365)
        {
            CurrentGrouping = DateBucketManager.GroupingMode.Week;
            bm = new WeeklyBucketManager();
            bm.ScanData(datasource, context, true);
        }
        gcTrends.XDatePattern = bm.DateFormat;

        RawData = bm.Buckets.ToArray<Bucket<DateTime>>();
        RefreshChartAndTable();
    }

    protected void gvRawData_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            Bucket<DateTime> b = (Bucket<DateTime>)e.Row.DataItem;
            HyperLink h = (HyperLink)e.Row.FindControl("lnkValue");
            h.Text = b.Value.ToString(CultureInfo.InvariantCulture);
            DateTime dt = (DateTime)b.Ordinal;
            System.Text.StringBuilder sb = new System.Text.StringBuilder("~/Member/LogbookNew.aspx?y=");
            sb.Append(dt.Year.ToString(CultureInfo.InvariantCulture));
            if (CurrentGrouping != DateBucketManager.GroupingMode.Year)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "&m={0}", dt.Month - 1);
                if (CurrentGrouping != DateBucketManager.GroupingMode.Month)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "&d={0}", dt.Day);
                    if (CurrentGrouping == DateBucketManager.GroupingMode.Week)
                        sb.AppendFormat(CultureInfo.InvariantCulture, "&w=1");
                }
            }
            h.NavigateUrl = sb.ToString();
        }
    }

    protected void cmbGrouping_SelectedIndexChanged(object sender, EventArgs e)
    {
        Refresh(SourceData);
    }
}
