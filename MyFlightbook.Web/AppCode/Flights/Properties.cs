using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Describes cross-fill for a specific property with a tip and a script reference
    /// </summary>
    public class CrossFillDescriptor
    {
        /// <summary>
        /// The top for the cross-fill arrow
        /// </summary>
        public string Tip { get; set; }

        /// <summary>
        /// Client-resolved reference to the script to execute on cross-fill
        /// </summary>
        public string ScriptRef { get; set; }

        public CrossFillDescriptor()
        {
            Tip = ScriptRef = string.Empty;
        }

        public CrossFillDescriptor(string tip, string scriptref)
        {
            Tip = tip;
            ScriptRef = scriptref;
        }
    }

    /// <summary>
    /// A flight property coupled with date and some flight properties.  
    /// Called a "ProfileEvent" because it (used to) include mostly events that were not necessarily coupled with flights 
    /// (the most recent flight review for a pilot used to be in the profile).  But these are used to:
    ///    a) Show warnings / expirations that are not strictly flight-experience related (e.g., flight reviews)
    ///    b) Display a list of BFRs and IPCs to the user
    /// </summary>
    [Serializable]
    public class ProfileEvent : CustomFlightProperty, IComparable<ProfileEvent>, IEquatable<ProfileEvent>
    {
        #region properties
        /// <summary>
        /// Date of the event
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// The model of aircraft in which the event occured
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// The type of aircraft in which the event occured.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The category of aircraft in which the event occured
        /// </summary>
        public string Category { get; set; }
        #endregion

        #region Instantiation/Initialization
        public ProfileEvent()
            : base()
        {
            Date = DateTime.MinValue;
            Category = Type = Model = string.Empty;
        }

        protected ProfileEvent(MySqlDataReader dr)
            : base(dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            Date = Convert.ToDateTime(dr["DateOfFlight"], CultureInfo.InvariantCulture);
            Category = Convert.ToString(dr["Category"], CultureInfo.InvariantCulture);
            Model = dr["model"].ToString();
            Type = dr["typename"].ToString();
        }
        #endregion

        #region Getting ProfileEvents
        private static List<ProfileEvent> GetFlightEvents(string szUser, UInt32 pg)
        {
            List<ProfileEvent> ar = new List<ProfileEvent>();

            string szQueryBase = @"
SELECT f.date AS DateOfFlight, 
    f.idFlight AS FlightID, 
    cc.Category AS Category, 
    fp.*, 
    cp.*, 
    mo.*,
    cp.Title AS LocTitle,
    cp.FormatString AS LocFormatString,
    0 AS IsFavorite,
    '' AS LocDescription, '' AS prevValues
FROM flights f 
INNER JOIN flightproperties fp ON f.idFlight=fp.idFlight 
INNER JOIN custompropertytypes cp ON fp.idPropType=cp.idPropType 
INNER JOIN aircraft ac ON ac.idaircraft = f.idaircraft 
INNER JOIN models mo ON ac.idmodel = mo.idmodel 
INNER JOIN manufacturers man ON mo.idManufacturer=man.idManufacturer
INNER JOIN categoryclass cc ON mo.idcategoryclass = cc.idCatClass 
WHERE f.username=?User AND (cp.Flags & {0}) <> 0 AND ((fp.IntValue <> 0) OR (fp.DecValue <> 0.0) OR (cp.Type=4)) 
ORDER BY f.Date Desc";

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szQueryBase, (ulong) pg));
            if (!dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("User", szUser);
                },
                (dr) =>
                {
                    ar.Add(new ProfileEvent(dr));
                }))
                throw new MyFlightbookException(dbh.LastError);

            return ar;
        }

        /// <summary>
        /// Returns all events that are IPCs
        /// </summary>
        /// <param name="szUser">Username that owns the events</param>
        /// <returns>An array of matching ProfileEvent objects</returns>
        public static ProfileEvent[] GetIPCEvents(string szUser)
        {
            return GetFlightEvents(szUser, CustomPropertyType.CFPPropertyFlag.cfpFlagIPC).ToArray();
        }

        public static ProfileEvent[] GetBFREvents(string szUser, ProfileEvent pfeInsert = null)
        {
            List<ProfileEvent> ar = GetFlightEvents(szUser, CustomPropertyType.CFPPropertyFlag.cfpFlagBFR);

            if (pfeInsert != null)
                ar.Add(pfeInsert);

            ar.Sort();      // will sort ascending
            ar.Reverse();   // Issue #963: make it descending to match IPC, put most recent on top.

            return ar.ToArray();
        }
        #endregion

        #region IComparable implementation
        public int CompareTo(ProfileEvent other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            return Date.CompareTo(((ProfileEvent)other).Date);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ProfileEvent);
        }

        public bool Equals(ProfileEvent other)
        {
            return other != null &&
                   Date == other.Date &&
                   Model == other.Model &&
                   Type == other.Type &&
                   Category == other.Category;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 1522972355;
                hashCode = hashCode * -1521134295 + Date.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Model);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Category);
                return hashCode;
            }
        }

        public static bool operator ==(ProfileEvent left, ProfileEvent right)
        {
            return EqualityComparer<ProfileEvent>.Default.Equals(left, right);
        }

        public static bool operator !=(ProfileEvent left, ProfileEvent right)
        {
            return !(left == right);
        }
        public static bool operator <(ProfileEvent left, ProfileEvent right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(ProfileEvent left, ProfileEvent right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(ProfileEvent left, ProfileEvent right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(ProfileEvent left, ProfileEvent right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion

        public override string ToString()
        {
            return DisplayString;
        }

        public IDictionary<string, object> AsKeyValuePairs()
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            if (Date.HasValue())
                d["Date"] = Date.YMDString();
            d["Category"] = Category;
            d["Model"] = Model;
            d["Type"] = Type;
            d["Flight ID"] = FlightID.ToString(CultureInfo.InvariantCulture);
            d["Property Type"] = PropertyType == null ? PropTypeID.ToString(CultureInfo.InvariantCulture) : PropertyType.Title;
            return d;
        }

        public static IEnumerable<IDictionary<string, object>> AsPublicList(IEnumerable<ProfileEvent> lstIn)
        {
            List<IDictionary<string, object>> lst = new List<IDictionary<string, object>>();
            if (lstIn != null)
            {
                foreach (ProfileEvent pfe in lstIn)
                    lst.Add(pfe.AsKeyValuePairs());
            }
            return lst;
        }
    }
}