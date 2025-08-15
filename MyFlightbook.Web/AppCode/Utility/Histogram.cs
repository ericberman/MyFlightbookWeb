﻿using MyFlightbook.Charting;
using MyFlightbook.CSV;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2008-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Histogram
{
    public enum HistogramValueTypes { Integer, Decimal, Time, Currency }

    /// <summary>
    /// Describes the things that can be summed in a histogram.  Specifically its type and display string
    /// </summary>
    public class HistogramableValue
    {
        #region properties
        /// <summary>
        /// The internal data field or property or ... to which this data corresponds.  I.e., not localized
        /// </summary>
        public string DataField { get; set; }

        /// <summary>
        /// The display name for this data
        /// </summary>
        public string DataName { get; set; }

        public string RunningTotalDataName
        {
            get { return DataName == null ? string.Empty : String.Format(CultureInfo.CurrentCulture, "{0} {1}", DataName, Resources.LocalizedText.ChartDataRunningTotal); }
        }

        /// <summary>
        /// Is this an integer, a decimal, a time (i.e., can be HHMM), or currency?
        /// </summary>
        public HistogramValueTypes DataType { get; set; } = HistogramValueTypes.Integer;
        #endregion

        #region Constructors
        public HistogramableValue()
        {
            DataField = DataName = string.Empty;
        }

        public HistogramableValue(string fieldName, string displayName, HistogramValueTypes dataType)
        {
            DataField = fieldName;
            DataName = displayName;
            DataType = dataType;
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} ({1}) - {2}", DataName, DataField, DataType);
        }
    }

    /// <summary>
    /// Describes the capabilities of something that can be put into a histogram, including:
    ///  a) what columns it can report, and
    ///  b) what buckets it can support
    /// 
    /// Also can generate the datatable for the histogramable data
    /// </summary>
    public class HistogramManager
    {
        #region properties
        /// <summary>
        /// The set of columns from which you can choose in the data
        /// I.e., this is what determines the columns
        /// </summary>
        public IEnumerable<HistogramableValue> Values { get; set; } = Array.Empty<HistogramableValue>();

        /// <summary>
        /// The set of bucket managers supported for this data
        /// </summary>
        public IEnumerable<BucketManager> SupportedBucketManagers { get; set; }

        /// <summary>
        /// The source data that is historgramable
        /// </summary>
        public IEnumerable<IHistogramable> SourceData { get; set; }

        public IDictionary<string, object> Context { get; private set; } = new Dictionary<string, object>();
        #endregion

        #region Charting
        /// <summary>
        /// Populates google chart data with the data from this histogram manager.
        /// </summary>
        /// <param name="gcd">The google chart data</param>
        /// <param name="selectedBucket">Bucket to use for grouping</param>
        /// <param name="fieldToGraph">The field to graph</param>
        /// <param name="fUseHHMM">Whether or not to format in hhmm</param>
        /// <param name="fIncludeAverage">Whether or not to include the average</param>
        /// <returns>The bucket manager that populated the data</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public BucketManager PopulateChart(GoogleChartData gcd, string selectedBucket, string fieldToGraph, bool fUseHHMM, bool fIncludeAverage = false)
        {
            if (gcd == null)
                throw new ArgumentNullException(nameof(gcd));
            BucketManager bm = SupportedBucketManagers.FirstOrDefault(b => b.DisplayName.CompareOrdinal(selectedBucket) == 0);
            HistogramableValue hv = Values.FirstOrDefault(h => h.DataField.CompareOrdinal(fieldToGraph) == 0);

            if (bm == null)
                throw new InvalidOperationException("Unknown bucket for grouping: " + selectedBucket);
            if (hv == null)
                throw new InvalidOperationException("unknown data field to graph: " + fieldToGraph);

            gcd.XLabel = fieldToGraph;

            bm.ScanData(this);

            // check for daily with less than a year
            if (bm is DailyBucketManager dbm && dbm.MaxDate.CompareTo(dbm.MinDate) > 0 && dbm.MaxDate.Subtract(dbm.MinDate).TotalDays > 365)
            {
                bm = new WeeklyBucketManager();
                bm.ScanData(this);
            }

            if (bm is DateBucketManager datebm)
            {
                gcd.XDatePattern = datebm.DateFormat;
                gcd.XDataType = GoogleColumnDataType.date;
            }
            else
            {
                gcd.XDatePattern = "{0}";
                gcd.XDataType = GoogleColumnDataType.@string;
            }
            gcd.ShowTrendline = bm.SupportsTrendline;   

            int count = 0;
            double average = 0;

            foreach (Bucket b in bm.Buckets)
            {
                gcd.XVals.Add(gcd.XDataType == GoogleColumnDataType.@string ? b.DisplayName : b.OrdinalValue);
                gcd.YVals.Add(b.Values[hv.DataField]);
                if (!b.ExcludeFromAverage)
                {
                    average += b.Values[hv.DataField];
                    count++;
                }

                if (b.HasRunningTotals)
                    gcd.Y2Vals.Add(b.RunningTotals[hv.DataField]);

                string RankAndPercent = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ChartTotalsRankAndPercentOfTotals, b.Ranks[hv.DataField], bm.Buckets.Count(), b.PercentOfTotal[hv.DataField]);
                // Add a tooltip for the item.
                gcd.Tooltips.Add(String.Format(CultureInfo.CurrentCulture, "<div class='ttip'><div class='dataVal'>{0}</div><div>{1}: <span class='dataVal'>{2}</span></div><div>{3}</div></div>",
                    HttpUtility.HtmlEncode(b.DisplayName),
                    HttpUtility.HtmlEncode(hv.DataName),
                    HttpUtility.HtmlEncode(BucketManager.FormatForType(b.Values[hv.DataField], hv.DataType, fUseHHMM)),
                    HttpUtility.HtmlEncode(RankAndPercent)));
            }

            if (gcd.ShowAverage = (fIncludeAverage && count > 0))
                gcd.AverageValue = average / count;

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
            gcd.YLabel = String.Format(CultureInfo.CurrentCulture, szLabel, hv.DataName);
            gcd.Y2Label = Resources.LocalizedText.ChartRunningTotal;

            gcd.ClickHandlerJS = bm.ChartJScript;

            return bm;
        }
        #endregion
    }

    /// <summary>
    /// Represents the x-axis on a histogram
    /// </summary>
    public class Bucket : IComparable
    {
        #region Properties
        /// <summary>
        /// The name of the bucket
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The link string for the bucket (i.e., the URL for if you click to view the constituent data)
        /// </summary>
        public string HRef { get; set; }

        /// <summary>
        /// The ordinal bucket value.  Must be sortable.
        /// </summary>
        public IComparable OrdinalValue { get; set; }

        /// <summary>
        /// The values (Y-axes) for this bucket.
        /// </summary>
        public IDictionary<string, double> Values { get; private set; }

        /// <summary>
        /// The ordinal ranks (1 = most) for this bucket
        /// </summary>
        public IDictionary<string, int> Ranks { get; private set; }

        /// <summary>
        /// The percentage of total for this bucket
        /// </summary>
        public IDictionary<string, double> PercentOfTotal { get; private set; }

        /// <summary>
        /// Does this bucket have running totals?
        /// </summary>
        public bool HasRunningTotals { get; set; }

        /// <summary>
        /// The running totals for the Y-axis for this bucket, if appropriate
        /// </summary>
        public IDictionary<string, double> RunningTotals { get; private set; }

        /// <summary>
        /// True to exclude this bucket from average (e.g., if it is a month bucket that only exists to align to January)
        /// </summary>
        public bool ExcludeFromAverage { get; set; }
        #endregion

        #region Object creation
        public void InitColumns(IEnumerable<HistogramableValue> columns)
        {
            if (columns == null)
                throw new ArgumentNullException(nameof(columns));
            foreach (HistogramableValue column in columns)
                RunningTotals[column.DataField] = Values[column.DataField] = 0.0;
        }

        public Bucket(IComparable ordinalValue, string szName, IEnumerable<HistogramableValue> columns)
        {
            DisplayName = szName;
            OrdinalValue = ordinalValue;
            Values = new Dictionary<string, double>();
            PercentOfTotal = new Dictionary<string, double>();
            Ranks = new Dictionary<string, int>();
            RunningTotals = new Dictionary<string, double>();
            if (columns != null)
                InitColumns(columns);
            HRef = string.Empty;
        }
        #endregion

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            Bucket b = obj as Bucket;
            if (b == null)
                throw new InvalidCastException(nameof(b));
            return OrdinalValue.CompareTo(b.OrdinalValue);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is null)
            {
                return false;
            }

            if (!(obj is Bucket))
                return false;

            return CompareTo(obj) == 0;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = -1524998725;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DisplayName);
                hashCode = hashCode * -1521134295 + EqualityComparer<IComparable>.Default.GetHashCode(OrdinalValue);
                hashCode = hashCode * -1521134295 + Values.GetHashCode();
                hashCode = hashCode * -1521134295 + PercentOfTotal.GetHashCode();
                hashCode = hashCode * -1521134295 + Ranks.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Bucket left, Bucket right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(Bucket left, Bucket right)
        {
            return !(left == right);
        }

        public static bool operator <(Bucket left, Bucket right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(Bucket left, Bucket right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(Bucket left, Bucket right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(Bucket left, Bucket right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion
    }

    #region Bucket Management
    /// <summary>
    /// Abstract base class for a bucket manager, which manages buckets for a data type.  Two concrete subclasses exist: a date manager and a range manager.
    /// The purpose of this class is to provide the buckets for the x-axis of the histogram.
    /// </summary>
    [Serializable]
    public abstract class BucketManager
    {
        #region Properties
        public string DisplayName { get; set; }

        /// <summary>
        /// The selector used by this bucket.  E.g., "Date" for date buckets, or the name of the requested datafield for string buckets.
        /// </summary>
        public string BucketSelectorName { get; set; }

        /// <summary>
        /// Does this bucketing model support running totals?  E.g., dates do, groupings by model of aircraft doesn't
        /// </summary>
        public virtual bool SupportsRunningTotals { get { return true; } }

        /// <summary>
        /// Does this bucketing model support trendlines?  E.g., a trendline makes sense for dates, but not for models of aircraft.
        /// Default value is that if you support running totals, then you support a trendline.
        /// </summary>
        public virtual bool SupportsTrendline { get { return SupportsRunningTotals; } }

        /// <summary>
        /// The buckets
        /// </summary>
        public IEnumerable<Bucket> Buckets { get; set; }

        /// <summary>
        /// The base for any URL - the bucketmanager will add appropriate parameters to it.
        /// </summary>
        public string BaseHRef { get; set; }
        #endregion

        #region constructors
        protected BucketManager(string baseHref = null)
        {
            BaseHRef = baseHref;
        }

        /// <summary>
        /// Returns the javascript template for the chart for when you click on a link  Relies on underlying BaseHRef being set
        /// </summary>
        /// <returns></returns>
        public virtual string ChartJScript { get { return string.Empty; } }

        /// <summary>
        /// Returns the parameters to attach to the BaseHRef for each bucket.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public virtual IDictionary<string, string> ParametersForBucket(Bucket b)
        {
            return new Dictionary<string, string>();
        }
        #endregion

        /// <summary>
        /// Scans the set of items and returns a complete set of buckets.  The buckets should include at least all possible items, and optionally include some padding on either side.
        /// </summary>
        /// <param name="items">An enumerable of the items to be scanned</param>
        /// <returns>A keyed collection of buckets, using the Histogrammable's BucketKey as the key</returns>
        protected abstract IDictionary<IComparable, Bucket> BucketsForData(IEnumerable<IHistogramable> items, IEnumerable<HistogramableValue> columns);

        /// <summary>
        /// Returns the bucket key for the specified comparable.  
        /// E.g., for date-based, would return April 1, 2016 for a passed date of April 5, 2016.  Or for integers, might return "1" for passed value of 75 if bucket size is 100, or 2 if bucketize is 50.
        /// </summary>
        /// <param name="o">The comparable</param>
        /// <returns></returns>
        protected abstract IComparable KeyForValue(IComparable o);

        private string FullHRef(IDictionary<string, string> dictParams)
        {
            if (String.IsNullOrEmpty(BaseHRef))
                return string.Empty;

            List<string> lst = new List<string>();
            foreach (string key in dictParams.Keys)
                lst.Add(String.Format(CultureInfo.InvariantCulture, "{0}={1}", key, HttpUtility.UrlEncode(dictParams[key])));

            return String.Format(CultureInfo.InvariantCulture, "{0}?{1}", BaseHRef, string.Join("&", lst));
        }

        public static string FormatForType(object o, HistogramValueTypes valueType, bool fUseHHMM = false, bool fIncludeZero = true)
        {
            if (o == null)
                return string.Empty;

            switch (valueType)
            {
                case HistogramValueTypes.Currency:
                    return String.Format(CultureInfo.CurrentCulture, "{0:C}", o);
                case HistogramValueTypes.Decimal:
                case HistogramValueTypes.Time:
                    return o.FormatDecimal(fUseHHMM, fIncludeZero);
                case HistogramValueTypes.Integer:
                    return String.Format(CultureInfo.CurrentCulture, "{0:#,##0}", o);
                default:
                    return o.ToString();
            }
        }

        public const string ColumnNameHRef = "HREF";
        public const string ColumnNameDisplayName = "DisplayName";

        /// <summary>
        /// Returns the buckets as a datatable, formatted (all values are strings).  MUST BE DISPOSED BY CALLER!
        /// </summary>
        /// <param name="hm">A valid HistogramManager (contains values and buckets)</param>
        /// <returns></returns>
        public DataTable ToDataTable(HistogramManager hm)
        {
            if (hm == null)
                throw new ArgumentNullException(nameof(hm));

            DataTable dt = new DataTable() { Locale = CultureInfo.CurrentCulture };

            // Add two known columns: bucket name and bucket HREf.
            dt.Columns.Add(new DataColumn(ColumnNameDisplayName, typeof(string)));
            dt.Columns.Add(new DataColumn(ColumnNameHRef, typeof(string)));

            // Now for each of the value columns, add the value column and - optionally - the running totals column
            foreach (HistogramableValue hv in hm.Values)
            {
                dt.Columns.Add(new DataColumn(hv.DataName, typeof(string)));
                if (SupportsRunningTotals)
                    dt.Columns.Add(new DataColumn(hv.RunningTotalDataName, typeof(string)));
            }

            foreach (Bucket b in Buckets)
            {
                DataRow dr = dt.NewRow();
                dt.Rows.Add(dr);
                dr[ColumnNameDisplayName] = b.DisplayName;
                string szHRef = FullHRef(ParametersForBucket(b));
                dr[ColumnNameHRef] = szHRef;
                if (String.IsNullOrEmpty(b.HRef))
                    b.HRef = szHRef;

                foreach (HistogramableValue hv in hm.Values)
                {
                    dr[hv.DataName] = FormatForType(b.Values[hv.DataField], hv.DataType);
                    if (SupportsRunningTotals)
                        dr[hv.RunningTotalDataName] = FormatForType(b.RunningTotals[hv.DataField], hv.DataType);
                }
            }

            // Remove any rows that have no data
            List<int> emptyColumns = new List<int>();
            for (int iCol = 0; iCol < dt.Columns.Count; iCol++)
            {
                bool fHasValue = false;
                foreach (DataRow dr in dt.Rows)
                {
                    if (!decimal.TryParse(dr[iCol].ToString(), out decimal result) || result != 0)
                    {
                        fHasValue = true;
                        break;
                    }
                }
                if (!fHasValue)
                    emptyColumns.Add(iCol);
            }
            emptyColumns.Sort();
            for (int iCol = emptyColumns.Count - 1; iCol >= 0; iCol--)
                dt.Columns.RemoveAt(emptyColumns[iCol]);

            return dt;
        }

        /// <summary>
        /// Writes out the data to CSV
        /// </summary>
        /// <param name="hm"></param>
        /// <returns></returns>
        public string RenderCSV(HistogramManager hm)
        {
            using (DataTable dt = ToDataTable(hm))
            {
                // Remove the HREF column, rename the DisplayName column
                dt.Columns.Remove(ColumnNameHRef);
                dt.Columns[ColumnNameDisplayName].ColumnName = DisplayName;
                return CsvWriter.WriteToString(dt, true, true);
            }
        }

        /// <summary>
        /// Renders the table as HTML (higher performance than using cshtml or even aspx).
        /// </summary>
        /// <param name="hm"></param>
        /// <param name="fLink"></param>
        /// <returns></returns>
        public string RenderHTML(HistogramManager hm, bool fLink)
        {
            using (DataTable dt = ToDataTable(hm))
            {
                using (StringWriter sw = new StringWriter())
                {
                    using (HtmlTextWriter tw = new HtmlTextWriter(sw))
                    {
                        tw.AddAttribute("callpadding", "3");
                        tw.AddAttribute("style", "font-size: 8pt; border-collapse: collapse;");
                        tw.RenderBeginTag(HtmlTextWriterTag.Table);

                        tw.RenderBeginTag(HtmlTextWriterTag.Thead);
                        tw.RenderBeginTag(HtmlTextWriterTag.Tr);

                        tw.AddAttribute("class", "PaddedCell");
                        tw.RenderBeginTag(HtmlTextWriterTag.Th);
                        tw.WriteEncodedText(DisplayName);
                        tw.RenderEndTag();  // th

                        foreach (DataColumn dc in dt.Columns)
                        {
                            if (dc.ColumnName.CompareCurrentCultureIgnoreCase(BucketManager.ColumnNameHRef) != 0 && dc.ColumnName.CompareCurrentCultureIgnoreCase(BucketManager.ColumnNameDisplayName) != 0)
                            {
                                tw.AddAttribute("class", "PaddedCell");
                                tw.RenderBeginTag(HtmlTextWriterTag.Th);
                                tw.WriteEncodedText(dc.ColumnName);
                                tw.RenderEndTag();
                            }
                        }

                        tw.RenderEndTag();  // tr
                        tw.RenderEndTag();  // thead

                        tw.RenderBeginTag(HtmlTextWriterTag.Tbody);

                        foreach (DataRow dr in dt.Rows)
                        {
                            tw.RenderBeginTag(HtmlTextWriterTag.Tr);

                            tw.AddAttribute("class", "PaddedCell");
                            tw.RenderBeginTag(HtmlTextWriterTag.Td);
                            if(fLink && !String.IsNullOrEmpty(BaseHRef))
                            {
                                tw.AddAttribute("target", "_blank");
                                tw.AddAttribute("href", dr[ColumnNameHRef].ToString().ToAbsolute());
                                tw.RenderBeginTag(HtmlTextWriterTag.A);
                                tw.WriteEncodedText(dr[ColumnNameDisplayName].ToString());
                                tw.RenderEndTag();
                            }
                            else
                                tw.WriteEncodedText(dr[ColumnNameDisplayName].ToString());
                            tw.RenderEndTag();  // td

                            foreach (DataColumn dc in dt.Columns)
                            {
                                if (dc.ColumnName.CompareCurrentCultureIgnoreCase(ColumnNameHRef) != 0 && dc.ColumnName.CompareCurrentCultureIgnoreCase(ColumnNameDisplayName) != 0)
                                {
                                    tw.AddAttribute("class", "PaddedCell");
                                    tw.RenderBeginTag(HtmlTextWriterTag.Td);
                                    tw.WriteEncodedText(dr[dc.ColumnName].ToString());
                                    tw.RenderEndTag();
                                }
                            }

                            tw.RenderEndTag();  // tr
                        }

                        tw.RenderEndTag();  // tbody
                        tw.RenderEndTag();  // table
                    }
                    return sw.ToString();
                }
            }
        }

        internal class RankedValue 
        {
            public int index { get; set; }
            public double value { get; set; }
        }

        /// <summary>
        /// Scans the source data, building a set of buckets.  Makes multiple: once to determine the buckets, once to fill them, once to determine rank and percent of total, and optionally once to set running totals
        /// </summary>
        /// <param name="hm">The histogram manager controlling this.</param>
        /// <returns>The resulting histogram buckets, which are also persisted in the "Buckets" property</returns>
        public void ScanData(HistogramManager hm)
        {
            if (hm == null)
                throw new ArgumentNullException(nameof(hm));

            if (String.IsNullOrEmpty(BucketSelectorName))
                throw new InvalidOperationException("BucketSelectorName is null or empty");

            IDictionary<IComparable, Bucket> dict = this.BucketsForData(hm.SourceData, hm.Values);
            List<Bucket> lst = new List<Bucket>(dict.Values);
            lst.Sort();
            Buckets = lst;

            hm.Context.Clear(); // start fresh, in case multiple scan-data passes.

            object rf = HttpContext.Current?.Session?[MFBConstants.keyMathRoundingUnits];
            double roundingFactor = rf == null ? 60.0 : Convert.ToDouble((int)rf);

            foreach (IHistogramable h in hm.SourceData)
            {
                foreach (HistogramableValue hv in hm.Values)
                    dict[KeyForValue(h.BucketSelector(BucketSelectorName))].Values[hv.DataField] += (hv.DataType == HistogramValueTypes.Time) ? Math.Round(h.HistogramValue(hv.DataField, hm.Context) * roundingFactor) / roundingFactor : h.HistogramValue(hv.DataField, hm.Context);
            }

            // compute percent of total and rank for each datafield
            foreach (HistogramableValue hv in hm.Values)
            {
                double total = 0;
                List<RankedValue> ranks = new List<RankedValue>();

                // Get the total for percent of total and store these in order for rank
                for (int i = 0; i < lst.Count; i++) 
                {
                    double val = lst[i].Values[hv.DataField];
                    total += val;
                    ranks.Add(new RankedValue() { index = i, value = val });
                }

                // Now assign a percent of total
                foreach (Bucket b in lst)
                    b.PercentOfTotal[hv.DataField] = (total > 0) ? (100 * b.Values[hv.DataField]) / total : 0;

                // Finally, figure out rank.
                ranks.Sort((a, b) => { return b.value.CompareTo(a.value); });
                for (int i = 0; i < ranks.Count; i++)
                    lst[ranks[i].index].Ranks[hv.DataField] = i + 1;
            }

            if (SupportsRunningTotals)
            {
                foreach (HistogramableValue hv in hm.Values)
                {
                    double total = 0.0;
                    foreach (Bucket b in Buckets)
                    {
                        b.HasRunningTotals = true;
                        b.RunningTotals[hv.DataField] = (total += (hv.DataType == HistogramValueTypes.Time) ? (Math.Round(b.Values[hv.DataField] * roundingFactor) / roundingFactor) : b.Values[hv.DataField]);
                    }
                }
            }
        }

        public override string ToString() { return DisplayName; }
    }

    public abstract class DateBucketManager : BucketManager
    {
        #region properties
        /// <summary>
        /// Earliest date bucket
        /// </summary>
        public DateTime MinDate { get; protected set; }

        /// <summary>
        /// Latest date bucket
        /// </summary>
        public DateTime MaxDate { get; protected set; }

        /// <summary>
        /// Format string to use.  Default is "MMM yyyy"
        /// </summary>
        public string DateFormat { get; set; }
        #endregion

        protected void ComputeDateRange(IEnumerable<IHistogramable> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            foreach (IHistogramable h in items)
            {
                DateTime dt = (DateTime)h.BucketSelector(BucketSelectorName);
                if (MinDate.CompareTo(dt) > 0)
                    MinDate = dt;
                if (MaxDate.CompareTo(dt) < 0)
                    MaxDate = dt;
            }
        }

        protected DateBucketManager(string szBaseHRef = null) : base(szBaseHRef)
        {
            BucketSelectorName = "Date";
            MinDate = DateTime.MaxValue;
            MaxDate = DateTime.MinValue;
            DateFormat = "MMM yyyy";
        }
    }

    /// <summary>
    /// BucketManager for days (e.g., "May 5 2016") data
    /// </summary>
    public class DailyBucketManager : DateBucketManager
    {
        public DailyBucketManager(string szBaseHRef = null) : base(szBaseHRef)
        {
            DateFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            DisplayName = Resources.LocalizedText.ChartTotalsGroupDay;
        }

        /// <summary>
        /// Generates the buckets for the range of Histogrammable.  WILL THROW AN EXCEPTION IF THE ORDINAL IS NOT A DATETIME!
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        protected override IDictionary<IComparable, Bucket> BucketsForData(IEnumerable<IHistogramable> items, IEnumerable<HistogramableValue> columns)
        {
            Dictionary<IComparable, Bucket> dict = new Dictionary<IComparable, Bucket>();
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (!items.Any())
                return dict;

            ComputeDateRange(items);

            // Now create the buckets
            DateTime dt = MinDate;
            do
            {
                dict[dt] = new Bucket((DateTime)KeyForValue(dt), dt.ToShortDateString(), columns);
                dt = dt.AddDays(1);
            } while (dt.CompareTo(MaxDate) <= 0);

            return dict;
        }

        public override string ChartJScript
        {
            get { return String.IsNullOrEmpty(BaseHRef) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "function(row, column, xvalue, value) {{ window.open('{0}?y=' + xvalue.getFullYear()  + '&m=' + xvalue.getMonth() + '&d=' + xvalue.getDate(), '_blank').focus(); }}", VirtualPathUtility.ToAbsolute(BaseHRef)); }
        }

        public override IDictionary<string, string> ParametersForBucket(Bucket b)
        {
            if (b == null)
                throw new ArgumentNullException(nameof(b));
            DateTime d = (DateTime) b.OrdinalValue;
            return new Dictionary<string, string>()
            {
                {"y",  d.Year.ToString(CultureInfo.InvariantCulture) },
                {"m", (d.Month - 1).ToString(CultureInfo.InvariantCulture) },
                {"d", d.Day.ToString(CultureInfo.InvariantCulture) }
            };
        }

        protected override IComparable KeyForValue(IComparable o)
        {
            return ((DateTime) o).Date;
        }
    }

    public class DayOfWeekBucketManager : BucketManager
    {
        public DayOfWeekBucketManager() : base()
        {
            DisplayName = Resources.LocalizedText.ChartTotalsGroupDayOfWeek;
            BucketSelectorName = "Date";
        }

        public override bool SupportsRunningTotals { get { return false; } }

        protected override IDictionary<IComparable, Bucket> BucketsForData(IEnumerable<IHistogramable> items, IEnumerable<HistogramableValue> columns)
        {
            Dictionary<IComparable, Bucket> d = new Dictionary<IComparable, Bucket>();
            for (int day = 0; day < 7; day++)
                d[day] = new Bucket(day, DateTimeFormatInfo.CurrentInfo.GetDayName((DayOfWeek)day), columns);
            return d;
        }

        protected override IComparable KeyForValue(IComparable o)
        {
            if (o is int day)
                return day;
            else if (o is DateTime dt)
                return (int) dt.DayOfWeek;
            return null;
        }
    }

    public class DayOfYearBucketManager : BucketManager
    {
        public DayOfYearBucketManager() : base()
        {
            DisplayName = Resources.LocalizedText.ChartTotalsGroupDayOfYear;
            BucketSelectorName = "Date";
        }

        public override bool SupportsRunningTotals { get { return false; } }

        protected override IDictionary<IComparable, Bucket> BucketsForData(IEnumerable<IHistogramable> items, IEnumerable<HistogramableValue> columns)
        {
            Dictionary<IComparable, Bucket> d = new Dictionary<IComparable, Bucket>();
            // use a leap year to ensure buckets for 366 days
            DateTime dt = new DateTime(2004, 1, 1); // start on Jan 1 2004, a leap year
            for (int day = 0; day < 366; day++)
            {
                string sz = dt.AddDays(day).ToString("M", CultureInfo.CurrentCulture);
                d[sz] = new Bucket(day, sz, columns);
            }
            return d;
        }

        protected override IComparable KeyForValue(IComparable o)
        {
            if (o is DateTime dt)
                return dt.ToString("M", CultureInfo.CurrentCulture);
            return null;
        }
    }

    public class MonthOfYearBucketManager : BucketManager
    {
        public MonthOfYearBucketManager() : base()
        {
            DisplayName = Resources.LocalizedText.ChartTotalsGroupMonthOfYear;
            BucketSelectorName = "Date";
        }

        public override bool SupportsRunningTotals { get { return false; } }

        protected override IDictionary<IComparable, Bucket> BucketsForData(IEnumerable<IHistogramable> items, IEnumerable<HistogramableValue> columns)
        {
            Dictionary<IComparable, Bucket> d = new Dictionary<IComparable, Bucket>();
            int maxMonth = String.IsNullOrEmpty(DateTimeFormatInfo.CurrentInfo.GetMonthName(13)) ? 12 : 13;
            for (int month = 1; month <= maxMonth; month++)
                d[month] = new Bucket(month, DateTimeFormatInfo.CurrentInfo.GetMonthName(month), columns);
            return d;
        }

        protected override IComparable KeyForValue(IComparable o)
        {
            if (o is int day)
                return day;
            else if (o is DateTime dt)
                return (int)dt.Month;
            return null;
        }
    }

    /// <summary>
    /// BucketManager for days (e.g., "May 5 2016") data
    /// </summary>
    public class WeeklyBucketManager : DateBucketManager
    {
        /// <summary>
        /// Day to use as the start of week.
        /// </summary>
        public DayOfWeek WeekStart { get; set; }

        public WeeklyBucketManager(string szBaseHRef = null) : base(szBaseHRef)
        {
            WeekStart = DayOfWeek.Sunday;
            DateFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            DisplayName = Resources.LocalizedText.ChartTotalsGroupWeek;
        }

        /// <summary>
        /// Generates the buckets for the range of Histogrammable.  WILL THROW AN EXCEPTION IF THE ORDINAL IS NOT A DATETIME!
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        protected override IDictionary<IComparable, Bucket> BucketsForData(IEnumerable<IHistogramable> items, IEnumerable<HistogramableValue> columns)
        {
            Dictionary<IComparable, Bucket> dict = new Dictionary<IComparable, Bucket>();
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (!items.Any())
                return dict;

            ComputeDateRange(items);

            // Now create the buckets
            DateTime dt = (DateTime) KeyForValue(MinDate);
            do
            {
                dict[dt] = new Bucket((DateTime)KeyForValue(dt), String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.WeeklyBucketTitleTemplate, dt.ToShortDateString()), columns);
                dt = dt.AddDays(7);
            } while (dt.CompareTo(MaxDate) <= 0);

            return dict;
        }

        protected override IComparable KeyForValue(IComparable o)
        {
            DateTime dt = (DateTime)o;
            int cDays = (int)dt.DayOfWeek - (int)WeekStart; // # of days between now and the start of the week.  E.g., if weekstart is Monday and dt is Thursday, we need to subtract 3.
            if (cDays < 0)
                cDays += 7;
            return dt.AddDays(-cDays);  // align with start of the week.
        }

        public override string ChartJScript
        {
            get { return String.IsNullOrEmpty(BaseHRef) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "function(row, column, xvalue, value) {{ window.open('{0}?y=' + xvalue.getFullYear()  + '&m=' + xvalue.getMonth() + '&d=' + xvalue.getDate() + '&w=1', '_blank').focus(); }}", VirtualPathUtility.ToAbsolute(BaseHRef)); }
        }

        public override IDictionary<string, string> ParametersForBucket(Bucket b)
        {
            if (b == null)
                throw new ArgumentNullException(nameof(b));
            DateTime d = (DateTime)b.OrdinalValue;
            return new Dictionary<string, string>()
            {
                {"y",  d.Year.ToString(CultureInfo.InvariantCulture) },
                {"m", (d.Month - 1).ToString(CultureInfo.InvariantCulture) },
                {"d", d.Day.ToString(CultureInfo.InvariantCulture) },
                {"w", "1" }
            };
        }
    }

    /// <summary>
    /// Encapsulates a year of historgramable results.
    /// </summary>
    public class MonthsOfYearData
    {
        #region properties
        public int Year { get; set; }

        public IDictionary<int, Bucket> ValuesByMonth { get; }
        #endregion

        public Bucket ValueForMonth(int i)
        {
            if (ValuesByMonth.TryGetValue(i, out Bucket b))
                return b;
            return null;
        }

        public MonthsOfYearData()
        {
            ValuesByMonth = new Dictionary<int, Bucket>();
        }
    }

    /// <summary>
    /// BucketManager for year-month (e.g., "2016-May") data
    /// </summary>
    public class YearMonthBucketManager : DateBucketManager
    {
        #region properties
        /// <summary>
        /// True (default) to align the buckets to the January of the earliest year
        /// </summary>
        public bool AlignStartToJanuary { get; set; }

        /// <summary>
        /// True to align the buckets to December of the latest year.  False by default.
        /// </summary>
        public bool AlignEndToDecember { get; set; }
        #endregion

        public YearMonthBucketManager(string szBaseHRef = null) : base(szBaseHRef)
        {
            AlignStartToJanuary = true;
            AlignEndToDecember = false;
            DateFormat = "MMM yyyy";
            DisplayName = Resources.LocalizedText.ChartTotalsGroupMonth;
        }

        /// <summary>
        /// Generates the buckets for the range of Histogrammable.  WILL THROW AN EXCEPTION IF THE ORDINAL IS NOT A DATETIME!
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        protected override IDictionary<IComparable, Bucket> BucketsForData(IEnumerable<IHistogramable> items, IEnumerable<HistogramableValue> columns)
        {
            Dictionary<IComparable, Bucket> dict = new Dictionary<IComparable, Bucket>();
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (!items.Any())
                return dict;

            ComputeDateRange(items);

            // Align to 1st of the month, and optionally to January/December.
            DateTime _dtMin = new DateTime(MinDate.Year, AlignStartToJanuary ? 1 : MinDate.Month, 1);
            DateTime _dtMax = new DateTime(MaxDate.Year, AlignEndToDecember ? 12 : MaxDate.Month, 1);

            DateTime dtEarliestMonthWithData = new DateTime(MinDate.Year, MinDate.Month, 1);
            DateTime dtLatestMonthWithData = new DateTime(MaxDate.Year, MaxDate.Month, 1).AddMonths(1).AddDays(-1);

            // Now create the buckets
            DateTime dt = _dtMin;
            do {
                dict[dt] = new Bucket((DateTime)KeyForValue(dt), dt.ToString(DateFormat, CultureInfo.CurrentCulture), columns)
                {
                    ExcludeFromAverage = (dt.CompareTo(dtEarliestMonthWithData) < 0 || dt.CompareTo(dtLatestMonthWithData) > 0)
                };

                dt = dt.AddMonths(1);
            } while (dt.CompareTo(_dtMax) <= 0);

            return dict;
        }

        protected override IComparable KeyForValue(IComparable o)
        {
            DateTime dt = (DateTime)o;
            return new DateTime(dt.Year, dt.Month, 1);
        }

        public override string ChartJScript
        {
            get { return String.IsNullOrEmpty(BaseHRef) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "function(row, column, xvalue, value) {{ window.open('{0}?y=' + xvalue.getFullYear()  + '&m=' + xvalue.getMonth(), '_blank').focus(); }}", VirtualPathUtility.ToAbsolute(BaseHRef)); }
        }

        public override IDictionary<string, string> ParametersForBucket(Bucket b)
        {
            if (b == null)
                throw new ArgumentNullException(nameof(b));
            DateTime d = (DateTime)b.OrdinalValue;
            return new Dictionary<string, string>()
            {
                {"y",  d.Year.ToString(CultureInfo.InvariantCulture) },
                {"m", (d.Month - 1).ToString(CultureInfo.InvariantCulture) }
            };
        }

        /// <summary>
        /// Provides a month-by-month grouped-by-year summary of the buckets.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MonthsOfYearData> ToYearlySummary()
        {
            Dictionary<int, MonthsOfYearData> d = new Dictionary<int, MonthsOfYearData>();

            foreach (Bucket b in Buckets)
            {
                if (!(b.OrdinalValue is DateTime dt))
                    throw new InvalidOperationException("Bucket must have a datetime ordinal");

                if (!d.TryGetValue(dt.Year, out MonthsOfYearData moy))
                    d[dt.Year] = moy = new MonthsOfYearData() { Year = dt.Year };

                moy.ValuesByMonth[dt.Month] = b;
            }

            List<MonthsOfYearData> lst = new List<MonthsOfYearData>();
            foreach (MonthsOfYearData moy in d.Values)
                lst.Add(moy);
            lst.Sort((moy1, moy2) => { return moy1.Year.CompareTo(moy2.Year); });
            return lst;
        }
    }

    /// <summary>
    /// BucketManager for days (e.g., "May 5 2016") data
    /// </summary>
    public class YearlyBucketManager : DateBucketManager
    {
        public YearlyBucketManager(string szBaseHRef = null) : base(szBaseHRef)
        { 
            DateFormat = "yyyy";
            DisplayName = Resources.LocalizedText.ChartTotalsGroupYear;
        }

        /// <summary>
        /// Generates the buckets for the range of Histogrammable.  WILL THROW AN EXCEPTION IF THE ORDINAL IS NOT A DATETIME!
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        protected override IDictionary<IComparable, Bucket> BucketsForData(IEnumerable<IHistogramable> items, IEnumerable<HistogramableValue> columns)
        {
            Dictionary<IComparable, Bucket> dict = new Dictionary<IComparable, Bucket>();
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (!items.Any())
                return dict;

            ComputeDateRange(items);

            // Now create the buckets
            DateTime dt = (DateTime) KeyForValue(MinDate);
            do
            {
                dict[dt] = new Bucket((DateTime)KeyForValue(dt), dt.Year.ToString(CultureInfo.CurrentCulture), columns);
                dt = dt.AddYears(1);
            } while (dt.CompareTo(MaxDate) <= 0);

            return dict;
        }

        protected override IComparable KeyForValue(IComparable o)
        {
            DateTime dt = (DateTime)o;
            return new DateTime(dt.Year, 1, 1);
        }

        public override string ChartJScript
        {
            get { return String.IsNullOrEmpty(BaseHRef) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "function(row, column, xvalue, value) {{ window.open('{0}?y=' + xvalue.getFullYear(), '_blank').focus(); }}", VirtualPathUtility.ToAbsolute(BaseHRef)); }
        }

        public override IDictionary<string, string> ParametersForBucket(Bucket b)
        {
            if (b == null)
                throw new ArgumentNullException(nameof(b));
            DateTime d = (DateTime)b.OrdinalValue;
            return new Dictionary<string, string>()
            {
                {"y",  d.Year.ToString(CultureInfo.InvariantCulture) }
            };
        }
    }

    /// <summary>
    /// BucketManager for numeric ranges (e.g., "0, 1-200, 201-300, etc.)
    /// </summary>
    public class NumericBucketmanager : BucketManager
    {
        #region properties
        /// <summary>
        /// Width of each bucket (e.g., 200 items, 100 items.  Default is 100
        /// </summary>
        public int BucketWidth { get; set; }

        /// <summary>
        /// True if 0 should get its own bucket (in which case next bucket begins with 1).  Default is false.
        /// </summary>
        public bool BucketForZero { get; set; }
        #endregion

        public NumericBucketmanager()
            : base()
        {
            BucketWidth = 100;
            BucketForZero = false;
        }

        /// <summary>
        /// Generates the buckets for the range of Histogrammable.  WILL THROW AN EXCEPTION IF THE ORDINAL IS NOT AN INTEGER!
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        protected override IDictionary<IComparable, Bucket> BucketsForData(IEnumerable<IHistogramable> items, IEnumerable<HistogramableValue> columns)
        {
            Dictionary<IComparable, Bucket> dict = new Dictionary<IComparable, Bucket>();
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (!items.Any())
                return dict;

            if (BucketWidth == 0)
                throw new InvalidOperationException("Buckets cannot have 0 width!");

            int min = Int32.MaxValue, max = Int32.MinValue;

            // Find the range.
            foreach (IHistogramable h in items)
            {
                int i = (int) h.BucketSelector(BucketSelectorName);
                if (i < min)
                    min = i;
                if (i > max)
                    max = i;
            }

            if (min > 0 && BucketForZero)
                min = 0;

            // Align to bucket width
            min = (int) KeyForValue(min);

            // Now create the buckets
            do
            {
                if (min == 0 && BucketForZero)
                {
                    dict[KeyForValue(min)] = new Bucket((int)KeyForValue(min), min.ToString(CultureInfo.CurrentCulture), columns);      // zero bucket
                    ++min;
                    dict[KeyForValue(min)] = new Bucket((int)KeyForValue(min), String.Format(CultureInfo.CurrentCulture, "{0}-{1}", min, BucketWidth - 1), columns);   // 1-(Bucketwidth-1) bucket
                    min = BucketWidth + 1;  // go to the "101" bucket.
                }
                else
                {
                    dict[KeyForValue(min)] = new Bucket((int)KeyForValue(min), String.Format(CultureInfo.CurrentCulture, "{0}-{1}", min, min + BucketWidth - 1), columns);
                    min += BucketWidth;
                }
            } while (min <= max);

            return dict;
        }

        /// <summary>
        /// Returns the key for the specified integer, based on bucketwidth.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        protected override IComparable KeyForValue(IComparable o)
        {
            int i = (int)o;

            // We want:
            //   1-100 to be bucket 1
            // 101-200 to be bucket 2
            // 201-300 to be bucket 3
            // ...
            // BUT if BucketForZero is true, we want 0 to go into bucket 0 and bucket 1 to be 1-100.
            if (i == 0)
                return BucketForZero ? 0 : KeyForValue(1);

            return (i < 0 ? -1 : 1) * ((Math.Abs(i) + BucketWidth - 1) / BucketWidth);
        }
    }

    public class StringBucketManager : BucketManager
    {
        public override bool SupportsRunningTotals { get { return false; } }

        /// <summary>
        /// The parameter to be used in constructing the link URL for this.
        /// </summary>
        public string SearchParam { get; set; }

        public override string ChartJScript
        {
            get { return String.IsNullOrEmpty(BaseHRef) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "function(row, column, xvalue, value) {{ window.open('{0}?{1}=' + xvalue, '_blank').focus(); }}", VirtualPathUtility.ToAbsolute(BaseHRef), SearchParam); }
        }

        public override IDictionary<string, string> ParametersForBucket(Bucket b)
        {
            if (b == null)
                throw new ArgumentNullException(nameof(b));
            return new Dictionary<string, string>() { {SearchParam ?? string.Empty,  (string) b.OrdinalValue } };
        }

        public StringBucketManager(string dataField, string displayName, string baseHRef) : base(baseHRef) 
        {
            BucketSelectorName = dataField;
            DisplayName = displayName;
        }

        protected override IDictionary<IComparable, Bucket> BucketsForData(IEnumerable<IHistogramable> items, IEnumerable<HistogramableValue> columns)
        {
            Dictionary<IComparable, Bucket> dict = new Dictionary<IComparable, Bucket>();
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (!items.Any())
                return dict;

            // Find the range.
            foreach (IHistogramable h in items)
            {
                string s = h.BucketSelector(BucketSelectorName) as string;
                if (!dict.ContainsKey(s))
                    dict[s] = new Bucket(s, s, columns);
            }

            return dict;
        }

        protected override IComparable KeyForValue(IComparable o)
        {
            if (o == null)
                throw new ArgumentNullException(nameof(o));
            return ((string)o).ToUpper(CultureInfo.CurrentCulture);
        }
    }
    #endregion

    /// <summary>
    /// Interface implemented by objects which can be summed up in a histogram
    /// </summary>
    public interface IHistogramable
    {
        /// <summary>
        /// Returns the IComparable that determines the bucket to which this gets assigned.
        /// </summary>
        IComparable BucketSelector(string bucketSelectorName);

        /// <summary>
        /// Examines the object and returns the value that should be added to that bucket's total
        /// </summary>
        /// <param name="context">An optional parameter for context.  E.g., if there are multiple fields that can be summed, this could specify the field to use</param>
        /// <returns>The value to add to the bucket for this item</returns>
        double HistogramValue(string value, IDictionary<string, object> context = null);
    }

    public class SimpleDateHistogrammable : IHistogramable
    {
        public DateTime Date { get; set; }

        public int Count { get; set; }

        public SimpleDateHistogrammable() { }

        public IComparable BucketSelector(string bucketSelectorName)
        {
            // Ignore the bucket selector name
            return Date;
        }

        public double HistogramValue(string value, IDictionary<string, object> context = null)
        {
            // Ignore the requested value
            return Count;
        }
    }

    public class SimpleCountHistogrammable : IHistogramable
    {
        /// <summary>
        /// the top of the range (for bucketing).
        /// </summary>
        public int Range { get; set; }

        public int Count { get; set; }

        public SimpleCountHistogrammable() { }

        public IComparable BucketSelector(string bucketSelectorName)
        {
            // Ignore the bucket selector name
            return Range;
        }

        public double HistogramValue(string value, IDictionary<string, object> context = null)
        {
            // Ignore the requested value
            return Count;
        }
    }
}
