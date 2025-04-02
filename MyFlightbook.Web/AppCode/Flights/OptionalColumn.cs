using System;
using System.Collections.Generic;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2008-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Printing
{

    /// <summary>
    /// Specifies the kind of additional columns that can be displayed for printing.
    /// </summary>
    public enum OptionalColumnType { None, Complex, Retract, Tailwheel, HighPerf, TAA, Turbine, Jet, TurboProp, ATD, FTD, FFS, ASEL, ASES, AMEL, AMES, Helicopter, Glider, CustomProp, CrossCountry, Gyroplane, HotAirBalloon, GasBalloon, UAS, TurbinePIC, TurbineSIC, XCInstruction, XCSolo, XCPIC, XCSIC, NightInstruction, NightSolo, NightPIC, NightSIC }

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

        private OptionalColumnValueType ValueTypeForColumnType(OptionalColumnType oct)
        {
            return (oct == OptionalColumnType.None || oct == OptionalColumnType.CustomProp) ? OptionalColumnValueType.Decimal : OptionalColumnValueType.Time;
        }

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

            ValueType = ValueTypeForColumnType(type);
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
            Title = cpt.ShortTitle;
        }
        #endregion

        private static readonly Dictionary<OptionalColumnType, string> dTitles = new Dictionary<OptionalColumnType, string>()
        {
            { OptionalColumnType.None, string.Empty },
            { OptionalColumnType.CustomProp, string.Empty },
            { OptionalColumnType.Complex, Resources.Makes.IsComplex },
            { OptionalColumnType.Retract, Resources.Makes.IsRetract },
            { OptionalColumnType.Tailwheel, Resources.Makes.IsTailwheel },
            { OptionalColumnType.HighPerf, Resources.Makes.IsHighPerf },
            { OptionalColumnType.TAA, Resources.Makes.IsTAA },
            { OptionalColumnType.Turbine, Resources.Makes.IsTurbine },
            { OptionalColumnType.TurbinePIC, Resources.LogbookEntry.PrintHeaderTurbinePIC },
            { OptionalColumnType.TurbineSIC, Resources.LogbookEntry.PrintHeaderTurbineSIC },
            { OptionalColumnType.Jet, Resources.Makes.IsJet },
            { OptionalColumnType.TurboProp, Resources.Makes.IsTurboprop },
            { OptionalColumnType.ASEL, CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.ASEL).CatClass },
            { OptionalColumnType.AMEL, CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.AMEL).CatClass },
            { OptionalColumnType.ASES, CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.ASES).CatClass },
            { OptionalColumnType.AMES, CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.AMES).CatClass },
            { OptionalColumnType.Glider, CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.Glider).CatClass },
            { OptionalColumnType.Helicopter, CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.Helicopter).CatClass },
            { OptionalColumnType.Gyroplane, CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.Gyroplane).CatClass },
            { OptionalColumnType.HotAirBalloon, CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.HotAirBalloon).CatClass },
            { OptionalColumnType.GasBalloon, CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.GasBalloon).CatClass },
            { OptionalColumnType.UAS, CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.UnmannedAerialSystem).CatClass },
            { OptionalColumnType.ATD, OptionalColumnType.ATD.ToString() },
            { OptionalColumnType.FTD, OptionalColumnType.FTD.ToString() },
            { OptionalColumnType.FFS, OptionalColumnType.FFS.ToString() },
            { OptionalColumnType.CrossCountry, Resources.LogbookEntry.PrintHeaderCrossCountry },
            { OptionalColumnType.XCInstruction, Resources.LogbookEntry.ComboXCInstruction },
            { OptionalColumnType.XCSolo, Resources.LogbookEntry.ComboXCSolo },
            { OptionalColumnType.XCPIC, Resources.LogbookEntry.ComboXCPIC},
            { OptionalColumnType.XCSIC, Resources.LogbookEntry.ComboXCSIC },
            { OptionalColumnType.NightInstruction, Resources.LogbookEntry.ComboNightInstruction },
            { OptionalColumnType.NightSolo, Resources.LogbookEntry.ComboNightSolo },
            { OptionalColumnType.NightPIC, Resources.LogbookEntry.ComboNightPIC},
            { OptionalColumnType.NightSIC, Resources.LogbookEntry.ComboNightSIC },
        };

        public static string TitleForType(OptionalColumnType type)
        {
            if (dTitles.TryGetValue(type, out var title)) 
                return title;

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
                    case OptionalColumnType.Gyroplane:
                    case OptionalColumnType.HotAirBalloon:
                    case OptionalColumnType.GasBalloon:
                    case OptionalColumnType.UAS:
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
                    case CategoryClass.CatClassID.Gyroplane:
                        if (oc.ColumnType == OptionalColumnType.Gyroplane)
                            return false;
                        break;
                    case CategoryClass.CatClassID.Glider:
                        if (oc.ColumnType == OptionalColumnType.Glider)
                            return false;
                        break;
                    case CategoryClass.CatClassID.HotAirBalloon:
                        if (oc.ColumnType == OptionalColumnType.HotAirBalloon)
                            return false;
                        break;
                    case CategoryClass.CatClassID.GasBalloon:
                        if (oc.ColumnType == OptionalColumnType.GasBalloon)
                            return false;
                        break;
                    case CategoryClass.CatClassID.UnmannedAerialSystem:
                        if (oc.ColumnType == OptionalColumnType.UAS)
                            return false;
                        break;
                }
            }

            return true;
        }

        [Newtonsoft.Json.JsonIgnore]
        public CategoryClass.CatClassID AssociatedCategoryClass
        {
            get
            {
                switch (ColumnType)
                {
                    case OptionalColumnType.AMEL:
                        return CategoryClass.CatClassID.AMEL;
                    case OptionalColumnType.AMES:
                        return CategoryClass.CatClassID.AMES;
                    case OptionalColumnType.ASEL:
                        return CategoryClass.CatClassID.ASEL;
                    case OptionalColumnType.ASES:
                        return CategoryClass.CatClassID.ASES;
                    case OptionalColumnType.Glider:
                        return CategoryClass.CatClassID.Glider;
                    case OptionalColumnType.Helicopter:
                        return CategoryClass.CatClassID.Helicopter;
                    case OptionalColumnType.Gyroplane:
                        return CategoryClass.CatClassID.Gyroplane;
                    case OptionalColumnType.HotAirBalloon:
                        return CategoryClass.CatClassID.HotAirBalloon;
                    case OptionalColumnType.GasBalloon:
                        return CategoryClass.CatClassID.GasBalloon;
                    case OptionalColumnType.UAS:
                        return CategoryClass.CatClassID.UnmannedAerialSystem;
                    default:
                        return CategoryClass.CatClassID.ASEL;
                }
            }
        }
    }
}

