using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2008-2020 MyFlightbook LLC
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
        public IDictionary<string, double> Values { get; set; }

        /// <summary>
        /// Does this bucket have running totals?
        /// </summary>
        public bool HasRunningTotals { get; set; } = false;

        /// <summary>
        /// The running totals for the Y-axis for this bucket, if appropriate
        /// </summary>
        public IDictionary<string, double> RunningTotals { get; set; }
        #endregion

        #region Object creation
        public void InitColumns(IEnumerable<HistogramableValue> columns)
        {
            foreach (HistogramableValue column in columns)
                RunningTotals[column.DataField] = Values[column.DataField] = 0.0;
        }

        public Bucket(IComparable ordinalValue, string szName, IEnumerable<HistogramableValue> columns)
        {
            DisplayName = szName;
            OrdinalValue = ordinalValue;
            Values = new Dictionary<string, double>();
            RunningTotals = new Dictionary<string, double>();
            if (columns != null)
                InitColumns(columns);
            HRef = string.Empty;
        }
        #endregion

        #region IComparable
        public int CompareTo(object obj)
        {
            return OrdinalValue.CompareTo(((Bucket) obj).OrdinalValue);
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
            return left is object && left.CompareTo(right) > 0;
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

        protected static string FormatForType(object o, HistogramValueTypes valueType)
        {
            if (o == null)
                return string.Empty;

            switch (valueType)
            {
                case HistogramValueTypes.Currency:
                    return String.Format(CultureInfo.CurrentCulture, "{0:C}", o);
                case HistogramValueTypes.Decimal:
                case HistogramValueTypes.Time:
                    return String.Format(CultureInfo.CurrentCulture, "{0:#,##0.0#}", o);
                case HistogramValueTypes.Integer:
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
                dr[ColumnNameHRef] = FullHRef(ParametersForBucket(b));

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
        /// Scans the source data, building a set of buckets.  Makes two or three passes: once to determine the buckets, once to fill them, and optionally once to set running totals
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

            foreach (IHistogramable h in hm.SourceData)
            {
                foreach (HistogramableValue hv in hm.Values)
                    dict[KeyForValue(h.BucketSelector(BucketSelectorName))].Values[hv.DataField] += (hv.DataType == HistogramValueTypes.Time) ? Math.Round(h.HistogramValue(hv.DataField, hm.Context) * 60.0) / 60.0 : h.HistogramValue(hv.DataField, hm.Context);
            }

            if (SupportsRunningTotals)
            {
                foreach (HistogramableValue hv in hm.Values)
                {
                    double total = 0.0;
                    foreach (Bucket b in Buckets)
                    {
                        b.HasRunningTotals = true;
                        b.RunningTotals[hv.DataField] = (total += (hv.DataType == HistogramValueTypes.Time) ? (Math.Round(b.Values[hv.DataField] * 60.0) / 60.0) : b.Values[hv.DataField]);
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

            if (items.Count() == 0)
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
            get { return String.IsNullOrEmpty(BaseHRef) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "window.open('{0}?y=' + xvalue.getFullYear()  + '&m=' + xvalue.getMonth() + '&d=' + xvalue.getDate(), '_blank').focus()", VirtualPathUtility.ToAbsolute(BaseHRef)); }
        }

        public override IDictionary<string, string> ParametersForBucket(Bucket b)
        {
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

            if (items.Count() == 0)
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
            get { return String.IsNullOrEmpty(BaseHRef) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "window.open('{0}?y=' + xvalue.getFullYear()  + '&m=' + xvalue.getMonth() + '&d=' + xvalue.getDate() + '&w=1', '_blank').focus()", VirtualPathUtility.ToAbsolute(BaseHRef)); }
        }

        public override IDictionary<string, string> ParametersForBucket(Bucket b)
        {
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

            if (items.Count() == 0)
                return dict;

            ComputeDateRange(items);

            // Align to 1st of the month, and optionallyi to January/December.
            DateTime _dtMin = new DateTime(MinDate.Year, AlignStartToJanuary ? 1 : MinDate.Month, 1);
            DateTime _dtMax = new DateTime(MaxDate.Year, AlignEndToDecember ? 12 : MaxDate.Month, 1);

            // Now create the buckets
            DateTime dt = _dtMin;
            do {
                dict[dt] = new Bucket((DateTime) KeyForValue(dt), dt.ToString(DateFormat, CultureInfo.CurrentCulture), columns);
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
            get { return String.IsNullOrEmpty(BaseHRef) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "window.open('{0}?y=' + xvalue.getFullYear()  + '&m=' + xvalue.getMonth(), '_blank').focus()", VirtualPathUtility.ToAbsolute(BaseHRef)); }
        }

        public override IDictionary<string, string> ParametersForBucket(Bucket b)
        {
            DateTime d = (DateTime)b.OrdinalValue;
            return new Dictionary<string, string>()
            {
                {"y",  d.Year.ToString(CultureInfo.InvariantCulture) },
                {"m", (d.Month - 1).ToString(CultureInfo.InvariantCulture) }
            };
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

            if (items.Count() == 0)
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
            get { return String.IsNullOrEmpty(BaseHRef) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "window.open('{0}?y=' + xvalue.getFullYear(), '_blank').focus()", VirtualPathUtility.ToAbsolute(BaseHRef)); }
        }

        public override IDictionary<string, string> ParametersForBucket(Bucket b)
        {
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

            if (items.Count() == 0)
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
                    min = BucketWidth;
                }
                else
                {
                    dict[KeyForValue(min)] = new Bucket((int)KeyForValue(min), String.Format(CultureInfo.CurrentCulture, "{0}-{1}", min, min + BucketWidth - 1), columns);
                    min += BucketWidth;
                }
            } while (min < max);

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

            if (i == 0)
                return BucketForZero ? 0 : 1;

            return (int) ((i < 0 ? -1 : 1) * Math.Ceiling(Math.Abs(i) / (double)BucketWidth));
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
            get { return String.IsNullOrEmpty(BaseHRef) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "window.open('{0}?{1}=' + xvalue, '_blank').focus()", VirtualPathUtility.ToAbsolute(BaseHRef), SearchParam); }
        }

        public override IDictionary<string, string> ParametersForBucket(Bucket b)
        {
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

            if (items.Count() == 0)
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
}
