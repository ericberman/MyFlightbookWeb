using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2007-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    public enum TotalsGroup { None, CategoryClass, ICAO, Model, Capabilities, CoreFields, Properties, Total }

    public static class TotalsGroupUtility
    {
        public static string NameForGroup(this TotalsGroup group)
        {
            switch (group)
            {
                default:
                case TotalsGroup.None:
                    return string.Empty;
                case TotalsGroup.CategoryClass:
                    return Resources.Totals.TotalGroupCategoryClass;
                case TotalsGroup.Capabilities:
                    return Resources.Totals.TotalGroupCapabilities;
                case TotalsGroup.CoreFields:
                    return Resources.Totals.TotalGroupCoreFields;
                case TotalsGroup.ICAO:
                    return Resources.Totals.TotalGroupICAO;
                case TotalsGroup.Model:
                    return Resources.Totals.TotalGroupModel;
                case TotalsGroup.Properties:
                    return Resources.Totals.TotalGroupProperties;
                case TotalsGroup.Total:
                    return Resources.Totals.TotalGroupTotal;
            }
        }
    }

    public class TotalsItem : IComparable, IEquatable<TotalsItem>
    {
        /// <summary>
        /// Type of number
        /// This is a total hack in order to maintain backwards compatibility
        /// When I first created properties, there were just integer/decimal, the former doing counts, the latter doing times.
        /// I.e., I didn't distinguish between data types (integer vs. decimal) and semantics (a decimal can be a number, a time, or a monetary amount.  The real difference is formatting).
        /// I eventually added currency for monetary amounts, and then I added the mobile apps.  But some sums are not times
        /// E.g., "Distance travelled" is a vanilla decimal.  But I can't add that as another custom property type data type
        /// because that would confuse the mobile apps.
        /// So this is a backwards compatibility kludge that blurs this in a way that won't fuck over the mobile apps.
        /// </summary>
        public enum NumType { Integer, Decimal, Time, Currency };

        public enum SortMode { Model, CatClass };

        #region Creation/Initialization
        public TotalsItem()
        {
            SubDescription = Description = String.Empty;
            Group = TotalsGroup.None;
            Value = 0.0M;
            NumericType = NumType.Time; // decimals are time by default
        }

        public TotalsItem(string description, Decimal value) : this()
        {
            Description = description;
            Value = value;
        }

        /// <summary>
        /// Creates a totals item, specifying how to interpret (format) the value;
        /// </summary>
        /// <param name="description">Description</param>
        /// <param name="value">Value</param>
        /// <param name="isTime">True if this is a time</param>
        /// <param name="isInt"></param>
        public TotalsItem(string description, Decimal value, NumType nt) : this()
        {
            Description = description;
            Value = value;
            NumericType = nt;
        }

        public TotalsItem(string description, int value) : this()
        {
            Description = description;
            Value = value;
            NumericType = NumType.Integer;
        }

        public TotalsItem(string description, Decimal value, string subDescription, SortMode mode = SortMode.CatClass) : this()
        {
            Description = description;
            Value = value;
            SubDescription = subDescription;
            this.Sort = mode;
        }
        #endregion

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            TotalsItem ti = (TotalsItem)obj;
            if (this.Sort == ti.Sort)
                return String.Compare(Description, ti.Description, StringComparison.CurrentCultureIgnoreCase);
            else
                return (int)this.Sort - (int)ti.Sort;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TotalsItem);
        }

        public bool Equals(TotalsItem other)
        {
            return other != null &&
                   Value == other.Value &&
                   Description == other.Description &&
                   SubDescription == other.SubDescription &&
                   NumericType == other.NumericType &&
                   Group == other.Group;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 1244116911;
                hashCode = hashCode * -1521134295 + Value.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Description);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SubDescription);
                hashCode = hashCode * -1521134295 + NumericType.GetHashCode();
                hashCode = hashCode * -1521134295 + Group.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(TotalsItem left, TotalsItem right)
        {
            return EqualityComparer<TotalsItem>.Default.Equals(left, right);
        }

        public static bool operator !=(TotalsItem left, TotalsItem right)
        {
            return !(left == right);
        }

        public static bool operator <(TotalsItem left, TotalsItem right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(TotalsItem left, TotalsItem right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(TotalsItem left, TotalsItem right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(TotalsItem left, TotalsItem right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion

        #region Properties
        /// <summary>
        /// The amount
        /// </summary>
        public Decimal Value { get; set; }

        /// <summary>
        /// The description of the total
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Additional sub-information about the descriptions
        /// </summary>
        public string SubDescription { get; set; }

        /// <summary>
        /// How should the value be interpreted?
        /// </summary>
        public NumType NumericType { get; set; }

        /// <summary>
        /// Does this get sorted in with category class or put at the end?
        /// </summary>
        private SortMode Sort { get; set; }

        /// <summary>
        /// Is this an integer value or decimal?
        /// Setting this is a no-op if false; setter is only provided for backwards compatibility with webservices
        /// </summary>
        public bool IsInt
        {
            get { return NumericType == NumType.Integer; }
            set { NumericType = value ? NumType.Integer : NumericType; }
        }

        /// <summary>
        /// Does this represent a time (that can be formatted as HH:MM) or plain decimal?
        /// Setting this is a no-op if false; setter is only provided for backwards compatibility with webservices
        /// </summary>
        public bool IsTime
        {
            get { return NumericType == NumType.Time; }
            set { NumericType = value ? NumType.Time : NumericType; }
        }

        /// <summary>
        /// Does this represent a monetary amount?
        /// Setting this is a no-op if false; setter is only provided for backwards compatibility with webservices
        /// </summary>
        public bool IsCurrency
        {
            get { return NumericType == NumType.Currency; }
            set { NumericType = value ? NumType.Currency : NumericType; }
        }

        /// <summary>
        /// The JSON serialized string that represents the query for this totals item.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string JSONSerializedQuery
        {
            get { return Query == null ? Newtonsoft.Json.JsonConvert.SerializeObject(null) : Query.ToJSONString(); }
        }

        /// <summary>
        /// The query that should find the contributing flights for this.
        /// </summary>
        public FlightQuery Query { get; set; }

        public TotalsGroup Group { get; set; }

        public string GroupName
        {
            get { return Group.NameForGroup(); }
            set { throw new NotImplementedException(); /* this is only present to enable serialization */ } 
        }
        #endregion

        /// <summary>
        /// Returns a formatted value string
        /// </summary>
        /// <param name="fUseHHMM">Whether or not to use hhmm format, if it's a time</param>
        /// <returns>The formatted string in the current culture</returns>
        public string ValueString(bool fUseHHMM)
        {
            switch (NumericType)
            {
                case TotalsItem.NumType.Integer:
                    return String.Format(CultureInfo.CurrentCulture, "{0:#,##0}", (int) Value);
                case TotalsItem.NumType.Decimal:
                    return Value.FormatDecimal(false);
                case TotalsItem.NumType.Time:
                    return Value.FormatDecimal(IsTime && fUseHHMM, false);
                case TotalsItem.NumType.Currency:
                    return String.Format(CultureInfo.CurrentCulture, "{0:C}", Value);
                default:
                    return Value.ToString(CultureInfo.CurrentCulture);  // should never happen
            }
        }
    }

    public enum TotalsGrouping { CatClass, Model, Family }

    public class TotalsItemCollection : IComparable, IEquatable<TotalsItemCollection>
    {
        #region Properties
        public IEnumerable<TotalsItem> Items { get; set; }

        public TotalsGroup Group { get; set; }

        public string GroupName
        {
            get { return Group.NameForGroup(); }
        }
        #endregion

        public TotalsItemCollection(TotalsGroup group)
        {
            Group = group;
            Items = new List<TotalsItem>();
        }

        public void AddTotalsItem(TotalsItem ti)
        {
            ((List<TotalsItem>)Items).Add(ti);
        }

        public static IEnumerable<TotalsItemCollection> AsGroups(IEnumerable<TotalsItem> lst)
        {
            if (lst == null)
                throw new ArgumentNullException(nameof(lst));
            Dictionary<TotalsGroup, TotalsItemCollection> d = new Dictionary<TotalsGroup, TotalsItemCollection>();
            foreach (TotalsItem ti in lst)
            {
                if (!d.ContainsKey(ti.Group))
                    d[ti.Group] = new TotalsItemCollection(ti.Group);
                d[ti.Group].AddTotalsItem(ti);
            }

            List<TotalsItemCollection> lstOut = new List<TotalsItemCollection>(d.Values);
            lstOut.Sort();
            return lstOut;
        }

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            return this.Group.CompareTo(((TotalsItemCollection)obj).Group);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TotalsItemCollection);
        }

        public bool Equals(TotalsItemCollection other)
        {
            return other != null &&
                   EqualityComparer<IEnumerable<TotalsItem>>.Default.Equals(Items, other.Items) &&
                   Group == other.Group;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = -513398093;
                hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<TotalsItem>>.Default.GetHashCode(Items);
                hashCode = hashCode * -1521134295 + Group.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(TotalsItemCollection left, TotalsItemCollection right)
        {
            return EqualityComparer<TotalsItemCollection>.Default.Equals(left, right);
        }

        public static bool operator !=(TotalsItemCollection left, TotalsItemCollection right)
        {
            return !(left == right);
        }

        public static bool operator <(TotalsItemCollection left, TotalsItemCollection right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(TotalsItemCollection left, TotalsItemCollection right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(TotalsItemCollection left, TotalsItemCollection right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(TotalsItemCollection left, TotalsItemCollection right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion
    }

    public class UserTotals
    {
        private readonly List<TotalsItem> alTotals = new List<TotalsItem>();

        private readonly ModelFeatureTotal[] rgModelFeatureTotals = {new ModelFeatureTotal(ModelFeatureTotal.FeatureTotalType.Complex, Resources.Totals.Complex, " fcomplex <> 0 "),
                                                            new ModelFeatureTotal(ModelFeatureTotal.FeatureTotalType.Retract, Resources.Totals.Retract, " fRetract <> 0 "),
                                                            new ModelFeatureTotal(ModelFeatureTotal.FeatureTotalType.HighPerf, Resources.Totals.HighPerf, String.Format(CultureInfo.InvariantCulture, " (fHighPerf <> 0 OR (date < '{0}' AND f200HP <> 0))", MakeModel.Date200hpHighPerformanceCutoverDate)),
                                                            new ModelFeatureTotal(ModelFeatureTotal.FeatureTotalType.Tailwheel, Resources.Totals.Tailwheel, " fTailwheel <> 0 AND CatClassOverride <> 3 AND CatClassOverride <> 4 "), /* don't count sea time as tailwheel */
                                                            new ModelFeatureTotal(ModelFeatureTotal.FeatureTotalType.TAA, Resources.Totals.TAA, "IsTAA <> 0 AND CatClassOverride in (1, 2, 3, 4) "),    /* TAA is exclusive to AIRPLANES */
                                                            new ModelFeatureTotal(ModelFeatureTotal.FeatureTotalType.Turbine, Resources.Totals.Turbine, " (fTurbine > 0 AND fTurbine < 4) "),
                                                            new ModelFeatureTotal(ModelFeatureTotal.FeatureTotalType.MEHelicopter, Resources.Totals.MEHelicopter, " (fMultiHelicopter <> 0) ") };

        /// <summary>
        /// Keeps track of subtotals for a given catclass.  (As opposed to a cat/class/type)
        /// e.g., if you have AMEL, AMEL (B747-300), and AMEL (B737), these are all AMEL and could be summed up in a catclasstotal
        /// </summary>
        private class CatClassTotal
        {
            public Decimal Total { get; set; }
            public int TotalLandings { get; set; }
            public int TotalFSDayLandings { get; set; }
            public int TotalFSNightLandings { get; set; }
            public int TotalApproaches { get; set; }
            public string CatClassDisplay { get; set; }
            public CategoryClass CatClass { get; set; }
            public CategoryClass.CatClassID CCId { get; set; }

            /// <summary>
            /// How many totals have been added with this category/class?
            /// </summary>
            private int Count { get; set; }

            public CatClassTotal(string catClass, CategoryClass.CatClassID ccid)
            {
                Total = 0.0M;
                CatClassDisplay = catClass;
                CCId = ccid;
                CatClass = CategoryClass.CategoryClassFromID(ccid);
                Count = 0;
                TotalLandings = TotalFSDayLandings = TotalFSNightLandings = TotalApproaches = 0;
            }

            public void AddTotals(Decimal total, int cLandings, int cFSDayLandings, int cFSNightLandings, int cApproaches)
            {
                if (total > 0)
                {
                    Total += total;
                    Count++;
                    TotalLandings += cLandings;
                    TotalFSDayLandings += cFSDayLandings;
                    TotalFSNightLandings += cFSNightLandings;
                    TotalApproaches += cApproaches;
                }
            }

            /// <summary>
            /// True if there is only one entry, in which case we shouldn't show it.
            /// </summary>
            public bool IsRedundant
            {
                get { return Count <= 1; }
            }

            /// <summary>
            /// Returns the display name for this subtotal.
            /// </summary>
            public string DisplayName
            {
                get { return String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, CatClassDisplay, Resources.Totals.TotalForAllCatClass); }
            }
        }

        /// <summary>
        /// Represents a row from the cat/class feature query 
        /// </summary>
        private class CatClassFeatureRow
        {
            #region properties
            public string ModelDisplay { get; private set; }
            public string CatClassDisplay { get; private set; }
            public string FamilyDisplay { get; private set; }

            public string RawFamily { get; private set; }

            public string CatClass { get; private set; }
            public string TypeName { get; private set; }

            public CategoryClass.CatClassID CCID { get; private set; }

            public int ModelID { get; private set; }

            public decimal Total { get; private set; }

            public int Landings { get; private set; }
            public int Approaches { get; private set; }
            public int FSDayLandings { get; private set; }
            public int FSNightLandings { get; private set; }
            #endregion

            private CatClassFeatureRow(MySqlDataReader dr)
            {
                ModelDisplay = (string)dr["ModelDisplay"];
                CatClassDisplay = (string)dr["CatClassDisplay"];
                FamilyDisplay = (string)dr["FamilyDisplay"];
                RawFamily = util.ReadNullableString(dr, "Family");

                CatClass = dr["CatClass"].ToString();
                ModelID = Convert.ToInt32(dr["idmodel"], CultureInfo.InvariantCulture);
                TypeName = dr["typename"].ToString();
                CCID = (CategoryClass.CatClassID)Convert.ToInt32(dr["idCatClass"], CultureInfo.InvariantCulture);
                Total = (Decimal)util.ReadNullableField(dr, "Total", 0.0M);
                Landings = Convert.ToInt32(util.ReadNullableField(dr, "cLandings", 0), CultureInfo.InvariantCulture);
                Approaches = Convert.ToInt32(util.ReadNullableField(dr, "cApproaches", 0), CultureInfo.InvariantCulture);
                FSDayLandings = Convert.ToInt32(util.ReadNullableField(dr, "cFullStopLandings", 0), CultureInfo.InvariantCulture);
                FSNightLandings = Convert.ToInt32(util.ReadNullableField(dr, "cNightLandings", 0), CultureInfo.InvariantCulture);
            }

            public static IEnumerable<CatClassFeatureRow> ReadRows(MySqlCommand comm)
            {
                if (comm == null)
                    throw new ArgumentNullException(nameof(comm));

                List<CatClassFeatureRow> lst = new List<CatClassFeatureRow>();

                using (MySqlDataReader dr = comm.ExecuteReader())
                {
                    while (dr.Read())
                        lst.Add(new CatClassFeatureRow(dr));
                }

                return lst;
            }
        }

        /// <summary>
        /// Totals for a model feature (e.g., retract, high performance, etc.)
        /// </summary>
        private class ModelFeatureTotal
        {
            public enum FeatureTotalType { Complex, Retract, HighPerf, Tailwheel, Turbine, TAA, MEHelicopter };
            private enum FeatureSubtotal { PIC, SIC, Total };
            public string Restriction { get; set; }
            public string Name { get; set; }
            public FeatureTotalType ModelAttribute { get; set; }
            public Decimal PIC { get; set; }
            public Decimal SIC { get; set; }
            public Decimal TotalTime { get; set; }

            public ModelFeatureTotal(FeatureTotalType ftt, string szName, string szRestrict)
            {
                Name = szName;
                Restriction = szRestrict;
                ModelAttribute = ftt;
            }

            public void InitFromDataReader(MySqlDataReader dr)
            {
                PIC = Convert.ToDecimal(util.ReadNullableField(dr, "PIC", 0.0M), CultureInfo.InvariantCulture);
                SIC = Convert.ToDecimal(util.ReadNullableField(dr, "SIC", 0.0M), CultureInfo.InvariantCulture);
                TotalTime = Convert.ToDecimal(util.ReadNullableField(dr, "TotalTime", 0.0M), CultureInfo.InvariantCulture);
            }

            public static bool operator ==(ModelFeatureTotal mft1, ModelFeatureTotal mft2)
            {
                // If both are null, or both are same instance, return true.
                if (System.Object.ReferenceEquals(mft1, mft2))
                {
                    return true;
                }

                if ((mft1 is null) || (mft2 is null))
                    return false;

                return (mft1.PIC == mft2.PIC && mft1.SIC == mft2.SIC && mft1.TotalTime == mft2.TotalTime);
            }

            public static bool operator !=(ModelFeatureTotal mft1, ModelFeatureTotal mft2)
            {
                return !(mft1 == mft2);
            }

            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is ModelFeatureTotal))
                    return false;

                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            private FlightQuery QueryForModelFeatureTotal(FlightQuery fqBase, FeatureSubtotal fs)
            {
                FlightQuery fq = new FlightQuery(fqBase);
                switch (fs)
                {
                    case FeatureSubtotal.PIC:
                        fq.HasPIC = true;
                        break;
                    case FeatureSubtotal.SIC:
                        fq.HasSIC = true;
                        break;
                    case FeatureSubtotal.Total:
                    default:
                        break;
                }
                switch (ModelAttribute)
                {
                    case FeatureTotalType.Complex:
                        fq.IsComplex = true;
                        break;
                    case FeatureTotalType.HighPerf:
                        fq.IsHighPerformance = true;
                        break;
                    case FeatureTotalType.Retract:
                        fq.IsRetract = true;
                        break;
                    case FeatureTotalType.Tailwheel:
                        fq.IsTailwheel = true;
                        break;
                    case FeatureTotalType.Turbine:
                        fq.IsTurbine = true;
                        break;
                    case FeatureTotalType.TAA:
                        fq.IsTechnicallyAdvanced = true;
                        break;
                    case FeatureTotalType.MEHelicopter:
                        fq.IsMultiEngineHeli = true;
                        break;
                }
                return fq;
            }

            public void AddItems(UserTotals ut)
            {
                FlightQuery fq = ut.Restriction;

                if (PIC == TotalTime && ut.FilterEmptyTotals)
                {
                    ut.AddToList(new TotalsItem(Name + " - " + Resources.Totals.PICTotal, TotalTime) { Query = QueryForModelFeatureTotal(fq, FeatureSubtotal.Total), Group = TotalsGroup.Capabilities });
                    ut.AddToList(new TotalsItem(Name + " - " + Resources.Totals.SIC, SIC) { Query = QueryForModelFeatureTotal(fq, FeatureSubtotal.SIC), Group = TotalsGroup.Capabilities });
                }
                else if (SIC == TotalTime && ut.FilterEmptyTotals)
                {
                    ut.AddToList(new TotalsItem(Name + " - " + Resources.Totals.SICTotal, TotalTime) { Query = QueryForModelFeatureTotal(fq, FeatureSubtotal.Total), Group = TotalsGroup.Capabilities });
                    ut.AddToList(new TotalsItem(Name + " - " + Resources.Totals.PIC, PIC) { Query = QueryForModelFeatureTotal(fq, FeatureSubtotal.PIC), Group = TotalsGroup.Capabilities });
                }
                else
                {
                    ut.AddToList(new TotalsItem(Name, TotalTime) { Query = QueryForModelFeatureTotal(fq, FeatureSubtotal.Total), Group = TotalsGroup.Capabilities });
                    ut.AddToList(new TotalsItem(Name + " - " + Resources.Totals.SIC, SIC) { Query = QueryForModelFeatureTotal(fq, FeatureSubtotal.SIC), Group = TotalsGroup.Capabilities });
                    ut.AddToList(new TotalsItem(Name + " - " + Resources.Totals.PIC, PIC) { Query = QueryForModelFeatureTotal(fq, FeatureSubtotal.PIC), Group = TotalsGroup.Capabilities });
                }
            }
        }

        private ModelFeatureTotal GetModelFeatureTotalByType(ModelFeatureTotal.FeatureTotalType ftt)
        {
            foreach (ModelFeatureTotal mft in rgModelFeatureTotals)
                if (mft.ModelAttribute == ftt)
                    return mft;

            return null;
        }

        #region Properties
        /// <summary>
        /// Name of the user for whom we are getting totals
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// An additional restriction clause as a fully formed "WHERE" clause minus the "WHERE"
        /// </summary>
        public FlightQuery Restriction { get; set; }

        /// <summary>
        /// Whether or not to include totals that are 0.
        /// </summary>
        public Boolean FilterEmptyTotals { get; set; }

        /// <summary>
        /// The resulting totals.
        /// </summary>
        public IEnumerable<TotalsItem> Totals
        {
            get { return alTotals; }
        }
        #endregion

        #region Constructors
        public UserTotals()
        {
            Username = "";
            Restriction = null;
            FilterEmptyTotals = false;
        }

        /// <summary>
        /// New User Totals object
        /// </summary>
        /// <param name="szUser">User for whom we are getting totals</param>
        /// <param name="fq">Any additional restrictions</param>
        /// <param name="fFilterEmpty">True to ignore any totals that are 0</param>
        public UserTotals(string szUser, FlightQuery fq, Boolean fFilterEmpty)
        {
            Username = szUser;
            Restriction = fq;
            FilterEmptyTotals = fFilterEmpty;
        }
        #endregion

        private void AddToList(TotalsItem ti)
        {
            if (ti.Value != 0.0M || !FilterEmptyTotals)
                alTotals.Add(ti);
        }

        /// <summary>
        /// Creates the sub-description line based on the number of landings and approaches
        /// </summary>
        /// <param name="cLandings">total landings</param>
        /// <param name="cDayLandings">FS Day landings</param>
        /// <param name="cNightLandings">FS Night Landings</param>
        /// <param name="cApproaches">Approaches</param>
        /// <returns>Human readable description</returns>
        private static string SubDescFromLandings(int cLandings, int cDayLandings, int cNightLandings, int cApproaches)
        {
            string szLandings = (cLandings == 0) ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, cLandings.ToString("#,##0", CultureInfo.CurrentCulture), (cLandings == 1) ? Resources.Totals.Landing : Resources.Totals.Landings);
            string szFSDayLandings = (cDayLandings == 0) ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, cDayLandings.ToString("#,##0", CultureInfo.CurrentCulture), Resources.Totals.DayLanding);
            string szFSNight = (cNightLandings == 0) ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, cNightLandings.ToString("#,##0", CultureInfo.CurrentCulture), Resources.Totals.NightLanding);
            string szApproaches = (cApproaches == 0) ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, cApproaches.ToString("#,##0", CultureInfo.CurrentCulture), (cApproaches == 1) ? Resources.Totals.Approach : Resources.Totals.Approaches);

            List<string> lst = new List<string> { szFSDayLandings, szFSNight };
            lst.RemoveAll(sz => String.IsNullOrEmpty(sz));
            szLandings += (lst.Count == 0) ? string.Empty : String.Format(CultureInfo.InvariantCulture, " ({0})", String.Join(", ", lst));

            return String.Format(CultureInfo.CurrentCulture, "{0}{1}{2}", szLandings, String.IsNullOrEmpty(szLandings) || String.IsNullOrEmpty(szApproaches) ? string.Empty : ", ", szApproaches);
        }

        #region Totals Helper methods
        private void ComputeTotalsByCatClass(MySqlCommand comm, Profile pf)
        {
            // first get the totals by catclass
            Dictionary<string, CatClassTotal> htCct = new Dictionary<string, CatClassTotal>();

            try
            {
                IEnumerable<CatClassFeatureRow> lstRawRows = CatClassFeatureRow.ReadRows(comm);

                // Issue #811 - count models, looking for duplicates, to see if we need to distinguish them.
                Dictionary<string, int> dModelCount = new Dictionary<string, int>();
                foreach (CatClassFeatureRow ccfr in lstRawRows)
                {
                    string szTitle = pf.TotalsGroupingMode == TotalsGrouping.CatClass ? ccfr.CatClassDisplay : (pf.TotalsGroupingMode == TotalsGrouping.Model ? ccfr.ModelDisplay : ccfr.FamilyDisplay);
                    dModelCount[szTitle] = dModelCount.ContainsKey(szTitle) ? dModelCount[szTitle] + 1 : 0;
                }

                // Now do a second pass, actually accumulating the totals
                foreach (CatClassFeatureRow ccfr in lstRawRows)
                {
                    string szDesc = SubDescFromLandings(ccfr.Landings, ccfr.FSDayLandings, ccfr.FSNightLandings, ccfr.Approaches);

                    // keep track of the subtotal.
                    CatClassTotal cct = htCct.ContainsKey(ccfr.CatClass) ? htCct[ccfr.CatClass] : null;
                    if (cct == null)
                        htCct[ccfr.CatClass] = cct = new CatClassTotal(ccfr.CatClass, ccfr.CCID);
                    cct.AddTotals(ccfr.Total, ccfr.Landings, ccfr.FSDayLandings, ccfr.FSNightLandings, ccfr.Approaches);

                    FlightQuery fq = new FlightQuery(Restriction);
                    string szTitle = string.Empty;
                    TotalsGroup group = TotalsGroup.None;
                    switch (pf.TotalsGroupingMode)
                    {
                        case TotalsGrouping.CatClass:
                            fq.AddCatClass(cct.CatClass, ccfr.TypeName);
                            szTitle = ccfr.CatClassDisplay;
                            group = TotalsGroup.CategoryClass;
                            break;
                        case TotalsGrouping.Model:
                            fq.AddModelById(ccfr.ModelID);
                            szTitle = ccfr.ModelDisplay;
                            group = TotalsGroup.Model;
                            break;
                        case TotalsGrouping.Family:
                            fq.ModelName = String.IsNullOrWhiteSpace(ccfr.RawFamily) ? ccfr.ModelDisplay : ModelQuery.ICAOPrefix + ccfr.FamilyDisplay;
                            fq.CatClasses.Add(CategoryClass.CategoryClassFromID(ccfr.CCID));    // redundant, but just to be safe...
                            szTitle = ccfr.FamilyDisplay;
                            group = TotalsGroup.ICAO;
                            break;
                    }

                    if (dModelCount.TryGetValue(szTitle, out int count) && count > 0)
                        szTitle = String.Format(CultureInfo.CurrentCulture, "{0} - {1}", szTitle, ccfr.CatClassDisplay);

                    AddToList(new TotalsItem(szTitle, ccfr.Total, szDesc) { Query = fq, Group = group });
                }
            }
            catch (MySqlException ex)
            {
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Exception in UserTotals Data Bind while reading data by catclass: {0}", comm.CommandText), ex, pf.UserName);
            }

            AddSubtotals(pf, htCct.Values);
        }

        private void AddSubtotals(Profile pf, IEnumerable<CatClassTotal> rgcct)
        {
            // Add in any catclass subtotals
            foreach (CatClassTotal cct in rgcct)
            {
                FlightQuery fq = new FlightQuery(Restriction);
                fq.AddCatClass(cct.CatClass, string.Empty);
                if (pf.TotalsGroupingMode != TotalsGrouping.CatClass || !cct.IsRedundant)
                {
                    if (pf.TotalsGroupingMode == TotalsGrouping.CatClass)
                    {
                        // If you have a mix of type-rated and non-type-rated aircraft, then the subtotal for non-typerated doesn't have a type specifier; it's just naked "AMEL", for example.
                        // So Find and fix up the query for that item
                        CategoryClass ccTarget = cct.CatClass;
                        foreach (TotalsItem ti in Totals)
                        {
                            FlightQuery q = ti.Query;
                            if (q != null && q.CatClasses != null && q.CatClasses.Count == 1 && q.CatClasses.ElementAt(0).IdCatClass == ccTarget.IdCatClass && (q.TypeNames == null || q.TypeNames.Count == 0))
                            {
                                q.TypeNames.Clear();
                                q.TypeNames.Add(string.Empty);
                            }
                        }
                    }

                    AddToList(new TotalsItem(cct.DisplayName, cct.Total, SubDescFromLandings(cct.TotalLandings, cct.TotalFSDayLandings, cct.TotalFSNightLandings, cct.TotalApproaches), pf.TotalsGroupingMode == TotalsGrouping.CatClass ? TotalsItem.SortMode.CatClass : TotalsItem.SortMode.Model) { Query = fq, Group = TotalsGroup.CategoryClass });
                }
            }
        }

        private void ComputeTotalsForCustomProperties(MySqlCommand comm, Profile pf)
        {
            try
            {
                using (MySqlDataReader dr = comm.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        CustomPropertyType cpt = new CustomPropertyType(dr);

                        // don't include blocklisted properties in totals.
                        if (pf.BlocklistedProperties.Exists(i => i == cpt.PropTypeID))
                            continue;

                        bool fPropIsInQuery = Restriction.PropertyTypes.Contains(cpt);

                        // Only do totals for Booleans if
                        // (a) not in exclusion list.  Blocklisted is already handled above.
                        // (b) ARE in query.
                        // (c) are NOT excluded from MRU (e.g., showing # of checkrides is silly.)
                        if (cpt.Type == CFPPropertyType.cfpBoolean && (cpt.IsExcludedFromMRU || !fPropIsInQuery))
                            continue;

                        FlightQuery fq = new FlightQuery(Restriction) { PropertiesConjunction = GroupConjunction.All };

                        // If we're here, then we need to add the property to the query, if it's not already there.
                        if (!fPropIsInQuery)
                            fq.PropertyTypes.Add(cpt);

                        switch (cpt.Type)
                        {
                            case CFPPropertyType.cfpBoolean:
                                // "total" here is the number of flights.
                                AddToList(new TotalsItem(String.Format(CultureInfo.CurrentCulture, Resources.Totals.FlightsWithBooleanProp, cpt.Title), Convert.ToInt32(dr["intTotal"], CultureInfo.InvariantCulture)) { Query = fq, Group = TotalsGroup.Properties });
                                break;
                            case CFPPropertyType.cfpDecimal:
                                if (cpt.IsBasicDecimal)
                                    AddToList(new TotalsItem(cpt.Title, Convert.ToDecimal(dr["decTotal"], CultureInfo.InvariantCulture), TotalsItem.NumType.Decimal) { Query = fq, Group = TotalsGroup.Properties });
                                else
                                    AddToList(new TotalsItem(cpt.Title, Convert.ToDecimal(dr["timeTotal"], CultureInfo.InvariantCulture), TotalsItem.NumType.Time) { Query = fq, Group = TotalsGroup.Properties });
                                break;
                            case CFPPropertyType.cfpCurrency:
                                AddToList(new TotalsItem(cpt.Title, Convert.ToDecimal(dr["decTotal"], CultureInfo.InvariantCulture), TotalsItem.NumType.Currency) { Query = fq, Group = TotalsGroup.Properties });
                                break;
                            default:
                            case CFPPropertyType.cfpInteger:
                                AddToList(new TotalsItem(cpt.Title, Convert.ToInt32(dr["intTotal"], CultureInfo.InvariantCulture)) { Query = fq, Group = TotalsGroup.Properties });
                                break;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Exception in UserTotals Data Bind while reading custom property totals: {0}", comm.CommandText), ex, pf.UserName);
            }
        }

        private void ComputeTotalsForModelFeature(MySqlCommand comm, string szTempTableName, Profile pf)
        {
            foreach (ModelFeatureTotal mft in rgModelFeatureTotals)
            {
                comm.CommandText = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["TotalsByModelType"], szTempTableName, mft.Restriction);

                try
                {
                    using (MySqlDataReader dr = comm.ExecuteReader())
                    {
                        if (dr.Read())
                            mft.InitFromDataReader(dr);
                    }
                }
                catch (MySqlException ex)
                {
                    throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Exception in UserTotals while reading model-based query: {0}\r\n{1}", comm.CommandText, ex.Message), ex, pf.UserName);
                }
            }

            // now add them
            foreach (ModelFeatureTotal mft in rgModelFeatureTotals)
            {
                // skip over retract if it is the same as complex.
                if (FilterEmptyTotals && mft.ModelAttribute == ModelFeatureTotal.FeatureTotalType.Retract && mft == GetModelFeatureTotalByType(ModelFeatureTotal.FeatureTotalType.Complex))
                    continue;
                mft.AddItems(this);
            }
        }

        private void ComputeTotalsOverall(MySqlCommand comm, Profile pf, bool fIncludeSubtotals)
        {
            try
            {
                using (MySqlDataReader dr = comm.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        if (pf.DisplayTimesByDefault)
                        {
                            AddToList(new TotalsItem(Resources.Totals.FlightTime, Convert.ToDecimal(util.ReadNullableField(dr, "TotalFlightTime", 0.0M), CultureInfo.InvariantCulture)) { Group = TotalsGroup.CoreFields });
                            AddToList(new TotalsItem(Resources.Totals.EngineTime, Convert.ToDecimal(util.ReadNullableField(dr, "TotalEngineTime", 0.0M), CultureInfo.InvariantCulture)) { Group = TotalsGroup.CoreFields });
                            AddToList(new TotalsItem(Resources.Totals.Hobbs, Convert.ToDecimal(util.ReadNullableField(dr, "TotalHobbs", 0.0M), CultureInfo.InvariantCulture)) { Group = TotalsGroup.CoreFields });
                        }

                        AddToList(new TotalsItem(Resources.Totals.CrossCountry, Convert.ToDecimal(util.ReadNullableField(dr, "XCountry", 0.0M), CultureInfo.InvariantCulture)) { Query = new FlightQuery(Restriction) { HasXC = true }, Group = TotalsGroup.CoreFields });
                        AddToList(new TotalsItem(Resources.Totals.Night, Convert.ToDecimal(util.ReadNullableField(dr, "Night", 0.0M), CultureInfo.InvariantCulture)) { Query = new FlightQuery(Restriction) { HasNight = true }, Group = TotalsGroup.CoreFields });
                        AddToList(new TotalsItem(Resources.Totals.IMC, Convert.ToDecimal(util.ReadNullableField(dr, "IMC", 0.0M), CultureInfo.InvariantCulture)) { Query = new FlightQuery(Restriction) { HasIMC = true }, Group = TotalsGroup.CoreFields });
                        AddToList(new TotalsItem(Resources.Totals.SimIMC, Convert.ToDecimal(util.ReadNullableField(dr, "SimulatedInstrument", 0.0M), CultureInfo.InvariantCulture)) { Query = new FlightQuery(Restriction) { HasSimIMCTime = true }, Group = TotalsGroup.CoreFields });
                        AddToList(new TotalsItem(Resources.Totals.Ground, Convert.ToDecimal(util.ReadNullableField(dr, "GroundSim", 0.0M), CultureInfo.InvariantCulture)) { Query = new FlightQuery(Restriction) { HasGroundSim = true }, Group = TotalsGroup.CoreFields });
                        AddToList(new TotalsItem(Resources.Totals.Dual, Convert.ToDecimal(util.ReadNullableField(dr, "Dualtime", 0.0M), CultureInfo.InvariantCulture)) { Query = new FlightQuery(Restriction) { HasDual = true }, Group = TotalsGroup.CoreFields });
                        AddToList(new TotalsItem(Resources.Totals.CFI, Convert.ToDecimal(util.ReadNullableField(dr, "cfi", 0.0M), CultureInfo.InvariantCulture)) { Query = new FlightQuery(Restriction) { HasCFI = true }, Group = TotalsGroup.CoreFields });
                        AddToList(new TotalsItem(Resources.Totals.SIC, Convert.ToDecimal(util.ReadNullableField(dr, "SIC", 0.0M), CultureInfo.InvariantCulture)) { Query = new FlightQuery(Restriction) { HasSIC = true }, Group = TotalsGroup.CoreFields });
                        AddToList(new TotalsItem(Resources.Totals.PIC, Convert.ToDecimal(util.ReadNullableField(dr, "PIC", 0.0M), CultureInfo.InvariantCulture)) { Query = new FlightQuery(Restriction) { HasPIC = true }, Group = TotalsGroup.CoreFields });

                        Int32 cLandings = Convert.ToInt32(util.ReadNullableField(dr, "cLandings", 0), CultureInfo.InvariantCulture);
                        Int32 cApproaches = Convert.ToInt32(util.ReadNullableField(dr, "cApproaches", 0), CultureInfo.InvariantCulture);
                        Int32 cFSDayLandings = Convert.ToInt32(util.ReadNullableField(dr, "cFullStopLandings", 0), CultureInfo.InvariantCulture);
                        Int32 cFSNightLandings = Convert.ToInt32(util.ReadNullableField(dr, "cNightLandings", 0), CultureInfo.InvariantCulture);

                        string szDesc = fIncludeSubtotals ? SubDescFromLandings(cLandings, cFSDayLandings, cFSNightLandings, cApproaches) : string.Empty;

                        AddToList(new TotalsItem(Resources.Totals.Flights, Convert.ToInt32(util.ReadNullableField(dr, "numFlights", 0), CultureInfo.InvariantCulture), TotalsItem.NumType.Integer) { Group = TotalsGroup.Total });
                        AddToList(new TotalsItem(Resources.Totals.TotalTime, Convert.ToDecimal(util.ReadNullableField(dr, "Total", 0.0M), CultureInfo.InvariantCulture), szDesc) { Query = Restriction.IsDefault ? null : new FlightQuery(Restriction), Group = TotalsGroup.Total });
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Exception in UserTotals DataBind while reading total data for user: {0}", comm.CommandText), ex, pf.UserName);
            }
        }
        #endregion

        /// <summary>
        /// Binds to the user's totals so that the Totals property is valid.
        /// </summary>
        public void DataBind()
        {
            if (Username.Length == 0 || Restriction == null)
                return;

            // get the base command with all of the relevant parameters
            using (MySqlCommand comm = new MySqlCommand())
            {
                using (comm.Connection = new MySqlConnection(DBHelper.ConnectionString))
                {
                    DBHelper.InitCommandObject(comm, LogbookEntryBase.QueryCommand(Restriction));

                    string szInnerQuery = comm.CommandText; // this got set above; cache it away for a moment or two

                    string szTempTableName = "flightsForUserWithQuery";

                    Profile pf = Profile.GetUser(Username);

                    string szGroupField = string.Empty;
                    switch (pf.TotalsGroupingMode)
                    {
                        case TotalsGrouping.CatClass:
                            szGroupField = "CatClassDisplay";
                            break;
                        case TotalsGrouping.Model:
                            szGroupField = "CatClassDisplay, ModelDisplay";
                            break;
                        case TotalsGrouping.Family:
                            szGroupField = "CatClassDisplay, FamilyDisplay";
                            break;
                    }

                    // All three queries below use the innerquery above to find the matching flights; they then find totals from that subset of flights.
                    string szQTotalTimes = pf.DisplayTimesByDefault ? ConfigurationManager.AppSettings["TotalTimesSubQuery"] : string.Empty;
                    string szQTotalsByCatClass = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["TotalsQuery"], szQTotalTimes, szTempTableName, szGroupField);
                    string szQTotals = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["TotalsQuery"], szQTotalTimes, szTempTableName, "username"); // Note: this username is the "Group By" field, not a restriction.
                    string szQCustomPropTotals = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["TotalsCustomProperties"], szTempTableName);

                    alTotals.Clear();

                    try
                    {
                        // Set up temporary table:
                        comm.Connection.Open();
                        comm.Parameters.AddWithValue("lang", System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);

                        // Create the temp table
                        comm.CommandText = String.Format(CultureInfo.InvariantCulture, "DROP TEMPORARY TABLE IF EXISTS {0}", szTempTableName);
                        comm.ExecuteScalar();

                        try
                        {
                            comm.CommandText = String.Format(CultureInfo.InvariantCulture, "CREATE TEMPORARY TABLE {0} AS {1}", szTempTableName, szInnerQuery);
                            comm.ExecuteScalar();
                        }
                        catch (MySqlException ex)
                        {
                            throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Exception creating temporary table:\r\n{0}\r\n{1}\r\n{2}", ex.Message, szTempTableName, szInnerQuery), ex, pf.UserName);
                        }

                        int cTotalsSoFar = alTotals.Count;  // should always be 0, but safer this way
                        comm.CommandText = szQTotalsByCatClass;
                        ComputeTotalsByCatClass(comm, pf);
                        bool fShowOverallSubtotals = ((alTotals.Count - cTotalsSoFar) > 1);  // if we had multiple totals from above, then we want to show overall subtotals of approaches/landings.

                        // Sort what we have so far.
                        alTotals.Sort();

                        // now get the custom property totals
                        comm.CommandText = szQCustomPropTotals;
                        ComputeTotalsForCustomProperties(comm, pf);

                        // Get model-based time (retract, complex, etc.)
                        if (!pf.SuppressModelFeatureTotals)
                            ComputeTotalsForModelFeature(comm, szTempTableName, pf);

                        // Now get full totals
                        comm.CommandText = szQTotals;
                        ComputeTotalsOverall(comm, pf, fShowOverallSubtotals);

                        // Drop the temp table
                        comm.CommandText = String.Format(CultureInfo.InvariantCulture, "DROP TEMPORARY TABLE {0};", szTempTableName);
                        comm.ExecuteScalar();
                    }
                    catch (MySqlException ex)
                    {
                        throw new MyFlightbookException("Exception in UserTotals DataBind - setup", ex, pf.UserName);
                    }
                }
            }
        }
    }

    public class Form8710Row
    {
        protected Form8710Row(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));

            Category = util.ReadNullableString(dr, "Category");
            TotalTime = Convert.ToDecimal(util.ReadNullableField(dr, "TotalTime", 0.0M), CultureInfo.InvariantCulture);
            InstructionReceived = Convert.ToDecimal(util.ReadNullableField(dr, "InstructionReceived", 0.0M), CultureInfo.InvariantCulture);
            SoloTime = Convert.ToDecimal(util.ReadNullableField(dr, "SoloTime", 0.0M), CultureInfo.InvariantCulture);
            PIC = Convert.ToDecimal(util.ReadNullableField(dr, "PIC", 0.0M), CultureInfo.InvariantCulture);
            SIC = Convert.ToDecimal(util.ReadNullableField(dr, "SIC", 0.0M), CultureInfo.InvariantCulture);
            CrossCountryDual = Convert.ToDecimal(util.ReadNullableField(dr, "CrossCountryDual", 0.0M), CultureInfo.InvariantCulture);
            CrossCountrySolo = Convert.ToDecimal(util.ReadNullableField(dr, "CrossCountrySolo", 0.0M), CultureInfo.InvariantCulture);
            CrossCountryPIC = Convert.ToDecimal(util.ReadNullableField(dr, "CrossCountryPIC", 0.0M), CultureInfo.InvariantCulture);
            CrosscountrySIC = Convert.ToDecimal(util.ReadNullableField(dr, "CrosscountrySIC", 0.0M), CultureInfo.InvariantCulture);
            InstrumentTime = Convert.ToDecimal(util.ReadNullableField(dr, "InstrumentTime", 0.0M), CultureInfo.InvariantCulture);
            NightDual = Convert.ToDecimal(util.ReadNullableField(dr, "NightDual", 0.0M), CultureInfo.InvariantCulture);
            NightTakeoffs = Convert.ToInt32(util.ReadNullableField(dr, "NightTakeoffs", 0.0M), CultureInfo.InvariantCulture);
            NightLandings = Convert.ToInt32(util.ReadNullableField(dr, "NightLandings", 0.0M), CultureInfo.InvariantCulture);
            NightPIC = Convert.ToDecimal(util.ReadNullableField(dr, "NightPIC", 0.0M), CultureInfo.InvariantCulture);
            NightSIC = Convert.ToDecimal(util.ReadNullableField(dr, "NightSIC", 0.0M), CultureInfo.InvariantCulture);
            NightPICTakeoffs = Convert.ToInt32(util.ReadNullableField(dr, "NightPICTakeoffs", 0.0M), CultureInfo.InvariantCulture);
            NightPICLandings = Convert.ToInt32(util.ReadNullableField(dr, "NightPICLandings", 0.0M), CultureInfo.InvariantCulture);
            NightSICTakeoffs = Convert.ToInt32(util.ReadNullableField(dr, "NightSICTakeoffs", 0.0M), CultureInfo.InvariantCulture);
            NightSICLandings = Convert.ToInt32(util.ReadNullableField(dr, "NightSICLandings", 0.0M), CultureInfo.InvariantCulture);
            NumberOfFlights = Convert.ToInt32(util.ReadNullableField(dr, "NumberOfFlights", 0.0M), CultureInfo.InvariantCulture);
            AeroTows = Convert.ToInt32(util.ReadNullableField(dr, "AeroTows", 0.0M), CultureInfo.InvariantCulture);
            WinchedLaunches = Convert.ToInt32(util.ReadNullableField(dr, "WinchedLaunches", 0.0M), CultureInfo.InvariantCulture);
            SelfLaunches = Convert.ToInt32(util.ReadNullableField(dr, "SelfLaunches", 0.0M), CultureInfo.InvariantCulture);
        }

        #region properties
        public string Category { get; private set; }
        public decimal TotalTime { get; private set; }
        public decimal InstructionReceived { get; private set; }

        public decimal SoloTime { get; private set; }

        public decimal PIC { get; private set; }

        public decimal SIC { get; private set; }

        public decimal CrossCountryDual { get; private set; }

        public decimal CrossCountrySolo { get; private set; }

        public decimal CrossCountryPIC { get; private set; }

        public decimal CrosscountrySIC { get; private set; }

        public decimal InstrumentTime { get; private set; }

        public decimal NightDual { get; private set; }

        public int NightTakeoffs { get; private set; }

        public int NightLandings { get; private set; }

        public decimal NightPIC { get; private set; }

        public decimal NightSIC { get; private set; }

        public int NightPICTakeoffs { get; private set; }

        public int NightPICLandings { get; private set; }

        public int NightSICTakeoffs { get; private set; }

        public int NightSICLandings { get; private set; }

        public int NumberOfFlights { get; private set; }

        public int AeroTows { get; private set; }

        public int WinchedLaunches { get; private set; }

        public int SelfLaunches { get; private set; }
        #endregion

        /// <summary>
        /// Returns the rows of an 8710 form
        /// </summary>
        /// <param name="fq">FlightQuery</param>
        /// <param name="args">Optional arguments, if the flightquery has already been refreshed.  Otherwise, this will call refresh</param>
        /// <returns>An enumerable of 8710 rows</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<Form8710Row> Form8710ForQuery(FlightQuery fq, DBHelperCommandArgs args)
        {
            List<Form8710Row> lst = new List<Form8710Row>();

            if (fq == null)
                throw new ArgumentNullException(nameof(fq));

            if (args == null)
            {
                fq.Refresh();
                args = new DBHelperCommandArgs() { Timeout = 120 };
                args.AddFrom(fq.QueryParameters());
            }

            DBHelper.ExecuteWithArgs(args, 
                String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["8710ForUserQuery"], fq.RestrictClause, String.IsNullOrEmpty(fq.HavingClause) ? string.Empty : "HAVING " + fq.HavingClause, "f.category"), 
                (dr) =>
                {
                    if (dr.HasRows)
                    {
                        while (dr.Read())
                            lst.Add(new Form8710Row(dr));
                    }
                });

            return lst;            
        }
    }

    public class Form8710ClassTotal
    {
        #region Properties
        public string ClassName { get; private set; }
        public decimal Total { get; private set; }
        public decimal PIC { get; private set; }
        public decimal SIC { get; private set; }
        #endregion

        /// <summary>
        /// Computes class totals for a query (required for the "Class Totals" section of the 8710 form)
        /// </summary>
        /// <param name="fq">FlightQuery</param>
        /// <param name="args">Optional arguments, if the flightquery has already been refreshed.  Otherwise, this will call refresh</param>
        /// <returns>A dictionary of the class totals within a given query, keyed off of the relevant class</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IDictionary<string, IList<Form8710ClassTotal>> ClassTotalsForQuery(FlightQuery fq, DBHelperCommandArgs args)
        {
            Dictionary<string, IList<Form8710ClassTotal>> d = new Dictionary<string, IList<Form8710ClassTotal>>();

            if (fq == null)
                throw new ArgumentNullException(nameof(fq));

            if (args == null)
            {
                fq.Refresh();
                args = new DBHelperCommandArgs() { Timeout = 120 };
                args.AddFrom(fq.QueryParameters());
            }

            DBHelper.ExecuteWithArgs(args,
                String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["8710ForUserQuery"], fq.RestrictClause, String.IsNullOrEmpty(fq.HavingClause) ? string.Empty : "HAVING " + fq.HavingClause, "f.InstanceTypeID, f.CatClassID"),
                (dr) =>
                {
                    if (dr.HasRows)
                    {
                        while (dr.Read())
                        {
                            string szCategory = (string)dr["Category"];
                            string szClass = (string)dr["Class"];
                            string szCatClass = (string)dr["CatClass"];
                            if (!String.IsNullOrEmpty(szCategory) && !String.IsNullOrEmpty(szClass) && !String.IsNullOrEmpty(szCatClass))
                            {
                                if (!d.ContainsKey(szCategory))
                                    d[szCategory] = new List<Form8710ClassTotal>();
                                IList<Form8710ClassTotal> lst = d[szCategory];
                                Form8710ClassTotal ct = new Form8710ClassTotal()
                                {
                                    ClassName = szCatClass,
                                    Total = Convert.ToDecimal(dr["TotalTime"], CultureInfo.InvariantCulture),
                                    PIC = Convert.ToDecimal(dr["PIC"], CultureInfo.InvariantCulture),
                                    SIC = Convert.ToDecimal(dr["SIC"], CultureInfo.InvariantCulture)
                                };
                                lst.Add(ct);
                            }
                        }
                    }
                });

            return d;
        }
    }

    public class ModelRollupRow
    {
        protected ModelRollupRow(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));

            if (dr["FamilyDisplay"] == DBNull.Value)
                ModelDisplay = Resources.LogbookEntry.FieldTotal;
            else
            {
                string szFamily = (string)dr["FamilyDisplay"];
                ModelDisplay = szFamily.StartsWith("(", StringComparison.CurrentCultureIgnoreCase) ? szFamily : util.ReadNullableString(dr, "ModelDisplay");
            }

            DualReceived = Convert.ToDecimal(util.ReadNullableField(dr, "DualReceived", 0.0M), CultureInfo.InvariantCulture);
            flightengineer = Convert.ToDecimal(util.ReadNullableField(dr, "FlightEngineer", 0.0M), CultureInfo.InvariantCulture);
            MilTime = Convert.ToDecimal(util.ReadNullableField(dr, "MilTime", 0.0M), CultureInfo.InvariantCulture);
            CFI = Convert.ToDecimal(util.ReadNullableField(dr, "CFI", 0.0M), CultureInfo.InvariantCulture);
            Night = Convert.ToDecimal(util.ReadNullableField(dr, "Night", 0.0M), CultureInfo.InvariantCulture);
            IMC = Convert.ToDecimal(util.ReadNullableField(dr, "IMC", 0.0M), CultureInfo.InvariantCulture);
            SimIMC = Convert.ToDecimal(util.ReadNullableField(dr, "SimIMC", 0.0M), CultureInfo.InvariantCulture);
            XC = Convert.ToDecimal(util.ReadNullableField(dr, "XC", 0.0M), CultureInfo.InvariantCulture);
            Landings = Convert.ToInt32(util.ReadNullableField(dr, "Landings", 0.0M), CultureInfo.InvariantCulture);
            approaches = Convert.ToInt32(util.ReadNullableField(dr, "approaches", 0.0M), CultureInfo.InvariantCulture);
            _6MonthApproaches = Convert.ToInt32(util.ReadNullableField(dr, "_6MonthApproaches", 0.0M), CultureInfo.InvariantCulture);
            _12MonthApproaches = Convert.ToInt32(util.ReadNullableField(dr, "_12MonthApproaches", 0.0M), CultureInfo.InvariantCulture);
            PIC = Convert.ToDecimal(util.ReadNullableField(dr, "PIC", 0.0M), CultureInfo.InvariantCulture);
            SIC = Convert.ToDecimal(util.ReadNullableField(dr, "SIC", 0.0M), CultureInfo.InvariantCulture);
            TurboPropPIC = Convert.ToDecimal(util.ReadNullableField(dr, "TurboPropPIC", 0.0M), CultureInfo.InvariantCulture);
            TurboPropSIC = Convert.ToDecimal(util.ReadNullableField(dr, "TurboPropSIC", 0.0M), CultureInfo.InvariantCulture);
            JetPIC = Convert.ToDecimal(util.ReadNullableField(dr, "JetPIC", 0.0M), CultureInfo.InvariantCulture);
            JetSIC = Convert.ToDecimal(util.ReadNullableField(dr, "JetSIC", 0.0M), CultureInfo.InvariantCulture);
            MultiPIC = Convert.ToDecimal(util.ReadNullableField(dr, "MultiPIC", 0.0M), CultureInfo.InvariantCulture);
            MultiSIC = Convert.ToDecimal(util.ReadNullableField(dr, "MultiSIC", 0.0M), CultureInfo.InvariantCulture);
            Total = Convert.ToDecimal(util.ReadNullableField(dr, "Total", 0.0M), CultureInfo.InvariantCulture);
            _12MonthTotal = Convert.ToDecimal(util.ReadNullableField(dr, "_12MonthTotal", 0.0M), CultureInfo.InvariantCulture);
            _24MonthTotal = Convert.ToDecimal(util.ReadNullableField(dr, "_24MonthTotal", 0.0M), CultureInfo.InvariantCulture);
            LastFlight = Convert.ToDateTime(util.ReadNullableField(dr, "LastFlight", DateTime.MinValue), CultureInfo.InvariantCulture);
        }

        #region properties
        public string ModelDisplay { get; private set; }
        public decimal DualReceived { get; private set; }
        public decimal flightengineer { get; private set; }
        public decimal MilTime { get; private set; }
        public decimal CFI { get; private set; }
        public decimal Night { get; private set; }
        public decimal IMC { get; private set; }
        public decimal SimIMC { get; private set; }
        public decimal XC { get; private set; }
        public int Landings { get; private set; }
        public int approaches { get; private set; }
        public int _6MonthApproaches { get; private set; }
        public int _12MonthApproaches { get; private set; }
        public decimal PIC { get; private set; }
        public decimal SIC { get; private set; }
        public decimal TurboPropPIC { get; private set; }
        public decimal TurboPropSIC { get; private set; }
        public decimal JetPIC { get; private set; }
        public decimal JetSIC { get; private set; }
        public decimal MultiPIC { get; private set; }
        public decimal MultiSIC { get; private set; }
        public decimal Total { get; private set; }
        public decimal _12MonthTotal { get; private set; }
        public decimal _24MonthTotal { get; private set; }
        public DateTime LastFlight { get; private set; }
        #endregion

        /// <summary>
        /// Computes the RollupByModel report.
        /// </summary>
        /// <param name="fq">FlightQuery</param>
        /// <param name="args">Optional arguments, if the flightquery has already been refreshed.  Otherwise, this will call refresh</param>
        /// <returns>An enumerable of rows for the Rollup By Model report</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<ModelRollupRow> ModelRollupForQuery(FlightQuery fq, DBHelperCommandArgs args)
        {
            if (fq == null)
                throw new ArgumentNullException(nameof(fq));

            if (args == null)
            {
                fq.Refresh();
                args = new DBHelperCommandArgs() { Timeout = 120 };
                args.AddFrom(fq.QueryParameters());
            }

            List<ModelRollupRow> lst = new List<ModelRollupRow>();

            DBHelper.ExecuteWithArgs(args,
                String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["RollupGridQuery"], fq.RestrictClause, String.IsNullOrEmpty(fq.HavingClause) ? string.Empty : "HAVING " + fq.HavingClause),
                (dr) =>
                {
                    if (dr.HasRows)
                    {
                        while (dr.Read())
                            lst.Add(new ModelRollupRow(dr));
                    }
                });

            return lst;
        }
    }
}