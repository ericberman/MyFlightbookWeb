using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2010-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Airports
{
    /// <summary>
    /// A set of known navigational aid types such as VORs, NDBs, etc.
    /// </summary>
    public class NavAidTypes
    {
        private static NavAidTypes[] _rgKnownTypes;

        #region Properties
        /// <summary>
        /// Abbreviation for the type of navaid
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Friendly name for the type of navaid
        /// </summary>
        public string Name { get; set; }
        #endregion

        public NavAidTypes(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public static NavAidTypes[] GetKnownTypes()
        {
            if (_rgKnownTypes != null)
                return _rgKnownTypes;

            List<NavAidTypes> lst = new List<NavAidTypes>();
            DBHelper dbh = new DBHelper("SELECT * FROM NavaidTypes");

            if (!dbh.ReadRows(
                (comm) => { },
                (dr) => { lst.Add(new NavAidTypes(dr["Code"].ToString(), dr["FriendlyName"].ToString())); }))
                throw new MyFlightbookException("Error getting known navaid types: " + dbh.LastError);

            return (_rgKnownTypes = lst.ToArray());
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} - {1}", Code, Name);
        }
    }
}