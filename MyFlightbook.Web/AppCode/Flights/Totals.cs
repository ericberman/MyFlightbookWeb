using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightCurrency
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

    public class TotalsItem : IComparable
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

        public int CompareTo(object obj)
        {
            TotalsItem ti = (TotalsItem)obj;
            if (this.Sort == ti.Sort)
                return String.Compare(Description, ti.Description, StringComparison.CurrentCultureIgnoreCase);
            else
                return (int)this.Sort - (int)ti.Sort;
        }

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
                    return Value.FormatDecimal(IsTime && fUseHHMM);
                case TotalsItem.NumType.Currency:
                    return String.Format(CultureInfo.CurrentCulture, "{0:C}", Value);
                default:
                    return Value.ToString(CultureInfo.CurrentCulture);  // should never happen
            }
        }
    }

    public enum TotalsGrouping { CatClass, Model, Family }

    public class TotalsItemCollection : IComparable
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

        public int CompareTo(object obj)
        {
            return this.Group.CompareTo(((TotalsItemCollection)obj).Group);
        }
    }

    public class UserTotals
    {
        private readonly List<TotalsItem> alTotals = new List<TotalsItem>();

        private readonly ModelFeatureTotal[] rgModelFeatureTotals = {new ModelFeatureTotal(ModelFeatureTotal.FeatureTotalType.Complex, Resources.Totals.Complex, " fcomplex <> 0 "),
                                                            new ModelFeatureTotal(ModelFeatureTotal.FeatureTotalType.Retract, Resources.Totals.Retract, " fRetract <> 0 "),
                                                            new ModelFeatureTotal(ModelFeatureTotal.FeatureTotalType.HighPerf, Resources.Totals.HighPerf, String.Format(CultureInfo.InvariantCulture, " (fHighPerf <> 0 OR (date < '{0}' AND f200HP <> 0))", MakeModel.Date200hpHighPerformanceCutoverDate)),
                                                            new ModelFeatureTotal(ModelFeatureTotal.FeatureTotalType.Tailwheel, Resources.Totals.Tailwheel, " fTailwheel <> 0 AND CatClassOverride <> 3 AND CatClassOverride <> 4 "), /* don't count sea time as tailwheel */
                                                            new ModelFeatureTotal(ModelFeatureTotal.FeatureTotalType.Turbine, Resources.Totals.Turbine, " (fTurbine > 0 AND fTurbine < 4) ")};

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
        /// Totals for a model feature (e.g., retract, high performance, etc.)
        /// </summary>
        private class ModelFeatureTotal
        {
            public enum FeatureTotalType { Complex, Retract, HighPerf, Tailwheel, Turbine };
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

                if (((object)mft1 == null) || ((object)mft2 == null))
                    return false;

                return (mft1.PIC == mft2.PIC && mft1.SIC == mft2.SIC && mft1.TotalTime == mft2.TotalTime);
            }

            public static bool operator !=(ModelFeatureTotal mft1, ModelFeatureTotal mft2)
            {
                return !(mft1 == mft2);
            }

            public override bool Equals(object obj)
            {
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
                }
                return fq;
            }

            public void AddItems(UserTotals ut)
            {
                FlightQuery fq = ut.Restriction;

                if (PIC == TotalTime)
                {
                    ut.AddToList(new TotalsItem(Name + " - " + Resources.Totals.SIC, SIC) { Query = QueryForModelFeatureTotal(fq, FeatureSubtotal.PIC), Group = TotalsGroup.Capabilities });
                    ut.AddToList(new TotalsItem(Name + " - " + Resources.Totals.PICTotal, TotalTime) { Query = QueryForModelFeatureTotal(fq, FeatureSubtotal.Total), Group = TotalsGroup.Capabilities });
                }
                else if (SIC == TotalTime)
                {
                    ut.AddToList(new TotalsItem(Name + " - " + Resources.Totals.SICTotal, TotalTime) { Query = QueryForModelFeatureTotal(fq, FeatureSubtotal.Total), Group = TotalsGroup.Capabilities });
                    ut.AddToList(new TotalsItem(Name + " - " + Resources.Totals.PIC, PIC) { Query = QueryForModelFeatureTotal(fq, FeatureSubtotal.SIC), Group = TotalsGroup.Capabilities });
                }
                else
                {
                    ut.AddToList(new TotalsItem(Name + " - " + Resources.Totals.SIC, SIC) { Query = QueryForModelFeatureTotal(fq, FeatureSubtotal.SIC), Group = TotalsGroup.Capabilities });
                    ut.AddToList(new TotalsItem(Name + " - " + Resources.Totals.PIC, PIC) { Query = QueryForModelFeatureTotal(fq, FeatureSubtotal.PIC), Group = TotalsGroup.Capabilities });
                    ut.AddToList(new TotalsItem(Name, TotalTime) { Query = QueryForModelFeatureTotal(fq, FeatureSubtotal.Total), Group = TotalsGroup.Capabilities });
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
        private string SubDescFromLandings(int cLandings, int cDayLandings, int cNightLandings, int cApproaches)
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
        private FlightQuery AddModelToQuery(FlightQuery fq, int idModel)
        {
            List<MakeModel> lst = new List<MakeModel>(fq.MakeList);
            if (!lst.Exists(m => m.MakeModelID == idModel))
                lst.Add(MakeModel.GetModel(idModel));
            fq.MakeList = lst.ToArray();
            return fq;
        }

        private FlightQuery AddCatClassToQuery(FlightQuery fq, CategoryClass cc, string szTypeName)
        {
            List<CategoryClass> lst = new List<CategoryClass>(fq.CatClasses);
            if (!lst.Exists(c => c.IdCatClass == cc.IdCatClass))
                lst.Add(cc);
            fq.CatClasses = lst.ToArray();
            if (!String.IsNullOrEmpty(szTypeName))
                fq.TypeNames = new string[] { szTypeName };
            return fq;
        }

        private void ComputeTotalsByCatClass(MySqlCommand comm, Profile pf)
        {
            // first get the totals by catclass
            Hashtable htCct = new Hashtable();
            try
            {
                using (MySqlDataReader dr = comm.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string szModelDisplay = (string)dr["ModelDisplay"];
                        string szCatClassDisplay = (string)dr["CatClassDisplay"];
                        string szFamilyDisplay = (string)dr["FamilyDisplay"];

                        string szCatClass = dr["CatClass"].ToString();
                        int idModel = Convert.ToInt32(dr["idmodel"], CultureInfo.InvariantCulture);
                        string szTypeName = dr["typename"].ToString();
                        CategoryClass.CatClassID ccid = (CategoryClass.CatClassID)Convert.ToInt32(dr["idCatClass"], CultureInfo.InvariantCulture);
                        Decimal decTotal = (Decimal)util.ReadNullableField(dr, "Total", 0.0M);
                        Int32 cLandings = Convert.ToInt32(util.ReadNullableField(dr, "cLandings", 0), CultureInfo.InvariantCulture);
                        Int32 cApproaches = Convert.ToInt32(util.ReadNullableField(dr, "cApproaches", 0), CultureInfo.InvariantCulture);
                        Int32 cFSDayLandings = Convert.ToInt32(util.ReadNullableField(dr, "cFullStopLandings", 0), CultureInfo.InvariantCulture);
                        Int32 cFSNightLandings = Convert.ToInt32(util.ReadNullableField(dr, "cNightLandings", 0), CultureInfo.InvariantCulture);

                        string szDesc = SubDescFromLandings(cLandings, cFSDayLandings, cFSNightLandings, cApproaches);

                        // keep track of the subtotal.
                        CatClassTotal cct = (CatClassTotal)htCct[szCatClass];
                        if (cct == null)
                            htCct[szCatClass] = cct = new CatClassTotal(szCatClass, ccid);
                        cct.AddTotals(decTotal, cLandings, cFSDayLandings, cFSNightLandings, cApproaches);

                        FlightQuery fq = null;

                        // don't link to a query for type-totals, since there's no clean query for that.  But if this is a clean catclass (e.g., "AMEL") it should match and we can query it.
                        fq = new FlightQuery(Restriction);
                        string szTitle = string.Empty;
                        TotalsGroup group = TotalsGroup.None;
                        switch (pf.TotalsGroupingMode)
                        {
                            case TotalsGrouping.CatClass:
                                AddCatClassToQuery(fq, cct.CatClass, szTypeName);
                                szTitle = szCatClassDisplay;
                                group = TotalsGroup.CategoryClass;
                                break;
                            case TotalsGrouping.Model:
                                AddModelToQuery(fq, idModel);
                                szTitle = szModelDisplay;
                                group = TotalsGroup.Model;
                                break;
                            case TotalsGrouping.Family:
                                fq.ModelName = szFamilyDisplay;
                                szTitle = szFamilyDisplay;
                                group = TotalsGroup.ICAO;
                                break;
                        }

                        AddToList(new TotalsItem(szTitle, decTotal, szDesc) { Query = fq, Group = group });
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Exception in UserTotals Data Bind while reading data by catclass: {0}", comm.CommandText), ex, pf.UserName);
            }

            // Add in any catclass subtotals
            foreach (CatClassTotal cct in htCct.Values)
            {
                FlightQuery fq = AddCatClassToQuery(new FlightQuery(Restriction), cct.CatClass, string.Empty);
                if (pf.TotalsGroupingMode != TotalsGrouping.CatClass || !cct.IsRedundant)
                {
                    if (pf.TotalsGroupingMode == TotalsGrouping.CatClass) {
                        // If you have a mix of type-rated and non-type-rated aircraft, then the subtotal for non-typerated doesn't have a type specifier; it's just naked "AMEL", for example.
                        // So Find and fix up the query for that item
                        CategoryClass ccTarget = cct.CatClass;
                        foreach (TotalsItem ti in Totals)
                        {
                            FlightQuery q = ti.Query;
                            if (q != null && q.CatClasses != null && q.CatClasses.Length == 1 && q.CatClasses[0].IdCatClass == ccTarget.IdCatClass && (q.TypeNames == null || q.TypeNames.Length == 0))
                                q.TypeNames = new string[1] { string.Empty };
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

                        // don't include blacklisted properties in totals.
                        if (pf.BlacklistedProperties.Exists(i => i == cpt.PropTypeID))
                            continue;

                        List<CustomPropertyType> lstCpt = new List<CustomPropertyType>(Restriction.PropertyTypes);
                        if (!lstCpt.Exists(c => c.PropTypeID == cpt.PropTypeID))
                            lstCpt.Add(cpt);
                        FlightQuery fq = new FlightQuery(Restriction) { PropertyTypes = lstCpt.ToArray() };
                        switch (cpt.Type)
                        {
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private void ComputeTotalsForModelFeature(MySqlCommand comm, string szTempTableName, Profile pf)
        {
            foreach (ModelFeatureTotal mft in rgModelFeatureTotals)
            {
                comm.CommandText = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["TotalsByModelType"].ToString(), szTempTableName, mft.Restriction);

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
                if (mft.ModelAttribute == ModelFeatureTotal.FeatureTotalType.Retract && mft == GetModelFeatureTotalByType(ModelFeatureTotal.FeatureTotalType.Complex))
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public void DataBind()
        {
            if (Username.Length == 0 || Restriction == null)
                return;

            // get the base command with all of the relevant parameters
            using (MySqlCommand comm = new MySqlCommand())
            {
                DBHelper.InitCommandObject(comm, LogbookEntry.QueryCommand(Restriction));

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
                string szQTotalTimes = pf.DisplayTimesByDefault ? ConfigurationManager.AppSettings["TotalTimesSubQuery"].ToString() : string.Empty;
                string szQTotalsByCatClass = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["TotalsQuery"].ToString(), szQTotalTimes, szTempTableName, szGroupField);
                string szQTotals = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["TotalsQuery"].ToString(), szQTotalTimes, szTempTableName, "username"); // Note: this username is the "Group By" field, not a restriction.
                string szQCustomPropTotals = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["TotalsCustomProperties"].ToString(), szTempTableName);

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

                    // Get totals by category class
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
                finally
                {
                    if (comm.Connection != null && comm.Connection.State != ConnectionState.Closed)
                        comm.Connection.Close();
                }
            }
        }
    }
}