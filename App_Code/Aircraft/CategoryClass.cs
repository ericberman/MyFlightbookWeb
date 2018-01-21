using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web;
using MySql.Data.MySqlClient;

/******************************************************
 * 
 * Copyright (c) 2009-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    [Serializable]
    public class CategoryClass
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
        public enum CatClassID
        {
            ASEL = 1,
            AMEL,
            ASES,
            AMES,
            Glider,
            // 6 is unused, but don't pass this down or it will screw up iOS app; just leave the gap!  
            Helicopter = 7,
            Gyroplane,
            PoweredLift,
            Airship,
            HotAirBalloon,
            GasBalloon,
            PoweredParachuteLand,
            PoweredParachuteSea,
            WeightShiftControlLand,
            WeightShiftControlSea,
            UnmannedAerialSystem,
            PoweredParaglider
        };

        #region Properties
        /// <summary>
        /// Category/Class string (e.g., "ASEL")
        /// </summary>
        public string CatClass { get; set; }

        /// <summary>
        /// Category (e.g., "Airplane")
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Class (e.g., "Single engine, Land")
        /// </summary>
        public string Class { get; set; }

        /// <summary>
        /// The ID of the likely alternate catclass for this.  E.g., if an aircraft can easily switch between land and see.
        /// </summary>
        public int AltCatClass { get; set; }

        /// <summary>
        /// The ID for this catclass
        /// </summary>
        public CatClassID IdCatClass { get; set; }

        /// <summary>
        /// Handy utility to return catclass as an int for checkboxes, lists
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public int IDCatClassAsInt { get { return (int)IdCatClass; } }

        /// <summary>
        /// Tells whether or not there is a possible alternate class for this.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean HasPossibleAlternate
        {
            get { return AltCatClass != 0; }
        }
        #endregion

        #region static utility functions
        /// <summary>
        /// Is this a sea or a land vessel?
        /// </summary>
        /// <param name="cc">The CatClasses enumerator</param>
        /// <returns>True for sea</returns>
        public static Boolean IsSeaClass(CatClassID cc)
        {
            switch (cc)
            {
                case CatClassID.AMES:
                case CatClassID.ASES:
                case CatClassID.PoweredParachuteSea:
                case CatClassID.WeightShiftControlSea:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Is this an airplane?
        /// </summary>
        /// <param name="ccid">ID of the category/class</param>
        /// <returns>True if it is ASEL/ASES/AMEL/AMES</returns>
        public static bool IsAirplane(CatClassID ccid)
        {
            switch (ccid)
            {
                case CategoryClass.CatClassID.ASEL:
                case CategoryClass.CatClassID.ASES:
                case CategoryClass.CatClassID.AMEL:
                case CategoryClass.CatClassID.AMES:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Is this a powered aircraft?
        /// </summary>
        /// <param name="ccid">ID of the category/class</param>
        /// <returns>True if it is powered</returns>
        public static bool IsPowered(CatClassID ccid)
        {
            switch (ccid)
            {
                case CatClassID.AMEL:
                case CatClassID.AMES:
                case CatClassID.ASEL:
                case CatClassID.ASES:
                case CatClassID.Gyroplane:
                case CatClassID.Helicopter:
                case CatClassID.PoweredLift:
                case CatClassID.PoweredParachuteLand:
                case CatClassID.PoweredParachuteSea:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Is this a lighter-than-air
        /// </summary>
        /// <param name="ccid">The ID</param>
        /// <returns>True for lighter-than-air</returns>
        public static bool IsLighterThanAir(CatClassID ccid)
        {
            switch (ccid)
            {
                default: return false;
                case CatClassID.Airship:
                case CatClassID.GasBalloon:
                case CatClassID.HotAirBalloon:
                    return true;
            }
        }

        /// <summary>
        /// Is this heavier-than-air?
        /// </summary>
        /// <param name="ccid">The ID</param>
        /// <returns>True for not-lighter-than-air</returns>
        public static bool IsHeavierThanAir(CatClassID ccid)
        {
            return !IsLighterThanAir(ccid);
        }

        /// <summary>
        /// Is this a balloon?
        /// </summary>
        /// <param name="ccid">ID of the category/class</param>
        /// <returns>True if it is a gas balloon or a hot air balloon</returns>
        public static bool IsBalloon(CategoryClass.CatClassID ccid)
        {
            return (ccid == CategoryClass.CatClassID.GasBalloon || ccid == CategoryClass.CatClassID.HotAirBalloon);
        }

        /// <summary>
        /// Does this have an engine?
        /// </summary>
        /// <param name="cc"></param>
        /// <returns></returns>
        public static Boolean HasEngine(CatClassID cc)
        {
            switch (cc)
            {
                case CatClassID.Glider:
                case CatClassID.GasBalloon:
                case CatClassID.HotAirBalloon:
                case CatClassID.Airship:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Is this a manned aircraft (i.e., is the pilot on board?) - 61.57 really only applies to manned categories.
        /// </summary>
        /// <param name="cc">ID of the category/class</param>
        /// <returns>True if it is manned</returns>
        public static Boolean IsManned(CatClassID cc)
        {
            return (cc != CatClassID.UnmannedAerialSystem);
        }
        #endregion

        #region creation/initialization
        public CategoryClass()
        {
            Category = Class = CatClass = string.Empty;
            IdCatClass = CatClassID.ASEL;
            AltCatClass = 0;
        }

        private CategoryClass(MySqlDataReader dr) : this()
        {
            IdCatClass = (CatClassID)Convert.ToInt32(dr["idCatClass"], CultureInfo.InvariantCulture);
            CatClass = Convert.ToString(dr["CatClass"], CultureInfo.InvariantCulture);
            Category = Convert.ToString(dr["Category"], CultureInfo.InvariantCulture);
            Class = Convert.ToString(dr["Class"], CultureInfo.InvariantCulture);
            AltCatClass = Convert.ToInt32(dr["AltCatClass"], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns a cached array of all category classes
        /// </summary>
        /// <returns>An array of category/classes</returns>
        public static IEnumerable<CategoryClass> CategoryClasses()
        {
            const string szCacheKey = "CatClassArrayKey";

            CategoryClass[] rgCatClass = null;

            if (HttpRuntime.Cache != null)
                rgCatClass = (CategoryClass[])HttpRuntime.Cache[szCacheKey];

            if (rgCatClass != null)
                return rgCatClass;

            List<CategoryClass> ar = new List<CategoryClass>();

            DBHelper dbh = new DBHelper("SELECT * FROM categoryclass");
            if (!dbh.ReadRows(
                (comm) => { },
                (dr) => { ar.Add(new CategoryClass(dr)); }))
                throw new MyFlightbookException("Error getting catclasses:\r\n" + dbh.LastError);

            rgCatClass = ar.ToArray();

            if (HttpRuntime.Cache != null)
                HttpRuntime.Cache[szCacheKey] = rgCatClass;
            return rgCatClass;
        }

        /// <summary>
        /// Returns the category/class for a given ID
        /// </summary>
        /// <param name="id">The requested ID</param>
        /// <returns>The specified category/class; throws an exception if not found</returns>
        public static CategoryClass CategoryClassFromID(CatClassID id)
        {
            IEnumerable<CategoryClass> rgCatClass = CategoryClasses();
            foreach (CategoryClass cc in rgCatClass)
                if (cc.IdCatClass == id)
                    return cc;
            throw new InvalidDataException("CategoryClassFromID: category/class with ID " + id.ToString() + " does not exist.");
        }
        #endregion
    }
}