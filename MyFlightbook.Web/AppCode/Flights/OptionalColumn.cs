using System;
using System.Collections.Generic;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2008-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{

    /// <summary>
    /// Specifies the kind of additional columns that can be displayed for printing.
    /// </summary>
    public enum OptionalColumnType { None, Complex, Retract, Tailwheel, HighPerf, TAA, Turbine, Jet, TurboProp, ATD, FTD, FFS, ASEL, ASES, AMEL, AMES, Helicopter, Glider, CustomProp, CrossCountry }

    public enum OptionalColumnValueType { Decimal, Integer, Time }

    /// <summary>
    /// An additional print column that can be displayed.
    /// </summary>
    [Serializable]
    public class OptionalColumn
    {
        #region Properties
        /// <summary>
        /// What kind of property is this?
        /// </summary>
        public OptionalColumnType ColumnType { get; set; }

        /// <summary>
        /// If this is a property, what is the ID of the property?
        /// </summary>
        public int IDPropType { get; set; }

        /// <summary>
        /// The title for the column
        /// </summary>
        public string Title { get; set; }

        public OptionalColumnValueType ValueType { get; set; }
        #endregion

        #region Constructors
        public OptionalColumn()
        {
            ColumnType = OptionalColumnType.None;
            IDPropType = (int)CustomPropertyType.KnownProperties.IDPropInvalid;
            Title = string.Empty;
            ValueType = OptionalColumnValueType.Decimal;
        }

        public OptionalColumn(OptionalColumnType type) : this()
        {
            ColumnType = type;
            if (type == OptionalColumnType.CustomProp || type == OptionalColumnType.None)
                throw new ArgumentOutOfRangeException(nameof(type));

            ValueType = OptionalColumnValueType.Decimal;
            Title = TitleForType(type);
        }

        public OptionalColumn(int idPropType) : this()
        {
            ColumnType = OptionalColumnType.CustomProp;
            IDPropType = idPropType;
            CustomPropertyType cpt = CustomPropertyType.GetCustomPropertyType(idPropType);
            switch (cpt.Type)
            {
                case CFPPropertyType.cfpDecimal:
                    ValueType = (cpt.IsBasicDecimal) ? OptionalColumnValueType.Decimal : OptionalColumnValueType.Time;
                    break;
                case CFPPropertyType.cfpInteger:
                    ValueType = OptionalColumnValueType.Integer;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(idPropType));
            }
            Title = cpt.Title;
        }
        #endregion

        public static string TitleForType(OptionalColumnType type)
        {
            switch (type)
            {
                case OptionalColumnType.CustomProp:
                case OptionalColumnType.None:
                    return string.Empty;
                case OptionalColumnType.Complex:
                    return Resources.Makes.IsComplex;
                case OptionalColumnType.Retract:
                    return Resources.Makes.IsRetract;
                case OptionalColumnType.Tailwheel:
                    return Resources.Makes.IsTailwheel;
                case OptionalColumnType.HighPerf:
                    return Resources.Makes.IsHighPerf;
                case OptionalColumnType.TAA:
                    return Resources.Makes.IsTAA;
                case OptionalColumnType.Turbine:
                    return Resources.Makes.IsTurbine;
                case OptionalColumnType.Jet:
                    return Resources.Makes.IsJet;
                case OptionalColumnType.TurboProp:
                    return Resources.Makes.IsTurboprop;
                case OptionalColumnType.ASEL:
                    return CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.ASEL).CatClass;
                case OptionalColumnType.AMEL:
                    return CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.AMEL).CatClass;
                case OptionalColumnType.ASES:
                    return CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.ASES).CatClass;
                case OptionalColumnType.AMES:
                    return CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.AMES).CatClass;
                case OptionalColumnType.Glider:
                    return CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.Glider).CatClass;
                case OptionalColumnType.Helicopter:
                    return CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.Helicopter).CatClass;
                case OptionalColumnType.ATD:
                case OptionalColumnType.FTD:
                case OptionalColumnType.FFS:
                    return type.ToString();
                case OptionalColumnType.CrossCountry:
                    return Resources.LogbookEntry.PrintHeaderCrossCountry;
            }
            throw new ArgumentOutOfRangeException(nameof(type), "Unknown OptionalColumnType: " + type.ToString());
        }

        public bool IsCatClass
        {
            get
            {
                switch (ColumnType)
                {
                    default:
                        return false;
                    case OptionalColumnType.AMEL:
                    case OptionalColumnType.AMES:
                    case OptionalColumnType.ASEL:
                    case OptionalColumnType.ASES:
                    case OptionalColumnType.Glider:
                    case OptionalColumnType.Helicopter:
                        return true;
                }
            }
        }

        public static int CatClassColumnCount(IEnumerable<OptionalColumn> optionalColumns)
        {
            return (optionalColumns == null) ? 0 : optionalColumns.Count(oc => oc.IsCatClass);
        }

        public override string ToString() { return Title; }

        public enum OptionalColumnRestriction { None, CatClassOnly, NotCatClass };

        /// <summary>
        /// Indicates whether or not to show a column at the specified index.  Can restrict to category/class columns if the layout would group those separate from other optional columns
        /// </summary>
        /// <param name="OptionalColumns"></param>
        /// <param name="index"></param>
        /// <param name="restriction"></param>
        /// <returns></returns>
        public static bool ShowOptionalColumn(IEnumerable<OptionalColumn> OptionalColumns, int index, OptionalColumnRestriction restriction)
        {
            if (OptionalColumns == null || index < 0 || index >= OptionalColumns.Count())
                return false;

            switch (restriction)
            {
                default:
                case OptionalColumnRestriction.None:
                    return true;
                case OptionalColumnRestriction.CatClassOnly:
                    return OptionalColumns.ElementAt(index).IsCatClass;
                case OptionalColumnRestriction.NotCatClass:
                    return !OptionalColumns.ElementAt(index).IsCatClass;
            }
        }

        /// <summary>
        /// Title for a given column to display
        /// </summary>
        /// <param name="OptionalColumns"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string OptionalColumnName(IEnumerable<OptionalColumn> OptionalColumns, int index)
        {
            return ShowOptionalColumn(OptionalColumns, index, OptionalColumnRestriction.None) ? OptionalColumns.ElementAt(index).Title : string.Empty;
        }

        /// <summary>
        /// For layouts that have a column for all times not otherwise in columns, answers whether or not the given catclass has its own columnID.  If true, should show this in the "other" bucket, if false, should suppress
        /// </summary>
        /// <param name="OptionalColumns"></param>
        /// <param name="catClassID"></param>
        /// <returns></returns>
        public static bool ShowOtherCatClass(IEnumerable<OptionalColumn> OptionalColumns, CategoryClass.CatClassID catClassID)
        {
            if (OptionalColumns == null)
                return true;

            foreach (OptionalColumn oc in OptionalColumns)
            {
                switch (catClassID)
                {
                    default:
                        break;
                    case CategoryClass.CatClassID.AMEL:
                        if (oc.ColumnType == OptionalColumnType.AMEL)
                            return false;
                        break;
                    case CategoryClass.CatClassID.ASEL:
                        if (oc.ColumnType == OptionalColumnType.ASEL)
                            return false;
                        break;
                    case CategoryClass.CatClassID.ASES:
                        if (oc.ColumnType == OptionalColumnType.ASES)
                            return false;
                        break;
                    case CategoryClass.CatClassID.AMES:
                        if (oc.ColumnType == OptionalColumnType.AMES)
                            return false;
                        break;
                    case CategoryClass.CatClassID.Helicopter:
                        if (oc.ColumnType == OptionalColumnType.Helicopter)
                            return false;
                        break;
                    case CategoryClass.CatClassID.Glider:
                        if (oc.ColumnType == OptionalColumnType.Glider)
                            return false;
                        break;
                }
            }

            return true;
        }
    }
}

