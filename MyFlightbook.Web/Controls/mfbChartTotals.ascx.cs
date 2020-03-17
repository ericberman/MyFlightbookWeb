using MyFlightbook;
using MyFlightbook.Histogram;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbChartTotals : System.Web.UI.UserControl
{
    public HistogramManager HistogramManager { get; set; }

    protected BucketManager BucketManager
    {
        get { return HistogramManager.SupportedBucketManagers.FirstOrDefault(bm => bm.DisplayName.CompareOrdinal(cmbGrouping.SelectedValue) == 0); }
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            cmbGrouping.SelectedValue = value.DisplayName;
        }
    }

    protected HistogramableValue SelectedFieldToGraph
    {
        get { return HistogramManager.Values.FirstOrDefault(hv => hv.DataField.CompareOrdinal(cmbFieldToView.SelectedValue) == 0); }
    }

    public bool CanDownload
    {
        get { return lnkDownloadCSV.Visible; }
        set { lnkDownloadCSV.Visible = value; }
    }

    protected void SetUpSelectors()
    {
        if (HistogramManager != null && (cmbFieldToView.Items.Count == 0 || cmbGrouping.Items.Count == 0))
        {
            cmbFieldToView.DataSource = HistogramManager.Values;
            cmbFieldToView.DataBind();
            cmbFieldToView.SelectedIndex = 0;

            cmbGrouping.DataSource = HistogramManager.SupportedBucketManagers;
            cmbGrouping.DataBind();
            cmbGrouping.SelectedIndex = 0;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // verify that we have a valid user (should never be a problem)
            if (!Page.User.Identity.IsAuthenticated)
                return;

            SetUpSelectors();
        }
        
        if (Visible)
            Refresh();
    }

    /// <summary>
    /// Updates the chart based on the computed data (stored in RawData)
    /// </summary>
    protected void RefreshChartAndTable(IEnumerable<Bucket> buckets)
    {
        if (buckets == null)
            throw new ArgumentNullException(nameof(buckets));

        HistogramableValue hv = SelectedFieldToGraph;

        gcTrends.Clear();
        foreach (Bucket b in buckets)
        {
            gcTrends.XVals.Add(b.OrdinalValue);
            gcTrends.YVals.Add(b.Values[hv.DataField]);

            if (b.HasRunningTotals)
                gcTrends.Y2Vals.Add(b.RunningTotals[hv.DataField]);
        }

        string szLabel = "{0}";
        {
            switch (hv.DataType)
            {
                case HistogramValueTypes.Integer:
                    szLabel = Resources.LocalizedText.ChartTotalsNumOfX;
                    break;
                case HistogramValueTypes.Time:
                    szLabel = Resources.LocalizedText.ChartTotalsHoursOfX;
                    break;
                case HistogramValueTypes.Decimal:
                case HistogramValueTypes.Currency:
                    szLabel = Resources.LocalizedText.ChartTotalsAmountOfX;
                    break;
            }
        }
        gcTrends.YLabel = String.Format(CultureInfo.CurrentCulture, szLabel, hv.DataName);
        gcTrends.Y2Label = Resources.LocalizedText.ChartRunningTotal;

        gcTrends.ClickHandlerJS = BucketManager.ChartJScript;

        pnlChart.Visible = true;
    }

    protected void cmbFieldToview_SelectedIndexChanged(object sender, EventArgs e)
    {
        Refresh();
    }

    /// <summary>
    /// Recomputes the data from the datasource and refreshes it
    /// </summary>
    public void Refresh()
    {
        // In case Page_Load has not been called, make sure combo boxes are populated.
        SetUpSelectors();
        if (String.IsNullOrEmpty(cmbGrouping.SelectedValue))
            cmbGrouping.SelectedIndex = 0;

        if (HistogramManager == null)
            throw new InvalidOperationException("Null HistogramManager");

        BucketManager bm = BucketManager;
        
        bm.ScanData(HistogramManager);

        // check for daily with less than a year
        if (bm is DailyBucketManager dbm && dbm.MaxDate.CompareTo(dbm.MinDate) > 0 && dbm.MaxDate.Subtract(dbm.MinDate).TotalDays > 365)
        {
            BucketManager = bm = new WeeklyBucketManager();
            bm.ScanData(HistogramManager);
        }

        if (bm is DateBucketManager datebm)
        {
            gcTrends.XDatePattern = datebm.DateFormat;
            gcTrends.XDataType = Controls_GoogleChart.GoogleColumnDataType.date;
        }
        else
        {
            gcTrends.XDatePattern = "{0}";
            gcTrends.XDataType = Controls_GoogleChart.GoogleColumnDataType.@string;
        }

        using (DataTable dt = bm.ToDataTable(HistogramManager))
        {
            gvRawData.Columns.Clear();
            if (String.IsNullOrEmpty(bm.BaseHRef))
                gvRawData.Columns.Add(new BoundField() { DataField = BucketManager.ColumnNameDisplayName, HeaderText = bm.DisplayName });
            else
                gvRawData.Columns.Add(new HyperLinkField() { DataTextField = BucketManager.ColumnNameDisplayName, DataNavigateUrlFormatString = "{0}", DataNavigateUrlFields = new string[] { BucketManager.ColumnNameHRef }, HeaderText = bm.DisplayName, Target = "_blank" });

            foreach (DataColumn dc in dt.Columns)
            {
                if (dc.ColumnName.CompareCurrentCultureIgnoreCase(BucketManager.ColumnNameHRef) == 0 || dc.ColumnName.CompareOrdinal(BucketManager.ColumnNameDisplayName) == 0)
                    continue;
                gvRawData.Columns.Add(new BoundField() { HeaderText = dc.ColumnName, DataField = dc.ColumnName });
            }
            gvRawData.DataSource = dt;
            gvRawData.DataBind();
        }

        RefreshChartAndTable(bm.Buckets);
    }

    protected void cmbGrouping_SelectedIndexChanged(object sender, EventArgs e)
    {
        Refresh();
    }

    protected void lnkDownloadCSV_Click(object sender, EventArgs e)
    {
        Response.Clear();
        Response.ContentType = "text/csv";
        // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
        string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}-{3}", Branding.CurrentBrand.AppName, Resources.LocalizedText.DownloadFlyingStatsFilename, Profile.GetUser(Page.User.Identity.Name).UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
        string szDisposition = String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}.csv", System.Text.RegularExpressions.Regex.Replace(szFilename, "[^0-9a-zA-Z-]", ""));
        Response.AddHeader("Content-Disposition", szDisposition);
        Response.Write('\uFEFF');   // UTF-8 BOM.
        Response.Write(gvRawData.CSVFromData());
        Response.End();
    }
}
