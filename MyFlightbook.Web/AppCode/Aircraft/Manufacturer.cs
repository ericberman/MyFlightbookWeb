using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2009-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    public class Manufacturer
    {
        public const int UnsavedID = -1;

        private const string szCacheKey = "allManufacturers";

        #region Properties
        /// <summary>
        /// Do makes based on this manufacturer default to being a sim?
        /// </summary>
        public AllowedAircraftTypes AllowedTypes { get; set; }

        /// <summary>
        /// The manufacturer's ID
        /// </summary>
        public int ManufacturerID { get; set; }

        /// <summary>
        /// The name of the manufacturer
        /// </summary>
        public string ManufacturerName { get; set; }

        /// <summary>
        /// New manufacturer?
        /// </summary>
        /// <returns>True if this is not an existing manufacturer</returns>
        public Boolean IsNew
        {
            get { return (ManufacturerID == UnsavedID); }
        }
        #endregion

        #region Instantiation
        /// <summary>
        /// Create a new manufacturer
        /// </summary>
        public Manufacturer()
        {
            ManufacturerID = UnsavedID;
            ManufacturerName = string.Empty;
        }

        /// <summary>
        /// Create a new manufacturer object, initialize by ID 
        /// </summary>
        /// <param name="id">The ID of the manufacturer</param>
        public Manufacturer(int id) : this()
        {
            FLoad("WHERE idmanufacturer=?idMan", new MySqlParameter("idMan", id));
        }

        static Regex rGeneric = new Regex("VARIOUS|UNKNOWN|MISC|MISCELLANEOUS", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        const string szGeneric = "Generic";

        /// <summary>
        /// Create a new manufacturer object, initialize by name
        /// </summary>
        /// <param name="szName">The name of the manufacturer</param>
        public Manufacturer(string szName) : this()
        {
            // Issue #295
            // check for variations on generic manufacturers; if so, map it to Generic.
            if (rGeneric.IsMatch(szName))
                szName = szGeneric;

            ManufacturerName = szName;

            try
            {
                FLoad("WHERE manufacturer=?manName", new MySqlParameter("manName", szName));
            }
            catch (MyFlightbookException)
            {
                ManufacturerID = UnsavedID;
            }
        }

        protected Manufacturer(MySqlDataReader dr) : this()
        {
            InitFromDataReader(dr);
        }
        #endregion

        #region Database
        /// <summary>
        /// Saves the make/model to the DB, creating it if necessary
        /// </summary>
        public Boolean FCommit()
        {
            Boolean fResult = false;

            if (!FIsValid())
                return fResult;

            Boolean fIsNew = IsNew;
            string szQ = String.Format(CultureInfo.InvariantCulture, "{0} manufacturers SET manufacturer = ?Manufacturer, DefaultSim = ?defSim {1}",
                        fIsNew ? "INSERT INTO" : "UPDATE",
                        fIsNew ? string.Empty : String.Format(CultureInfo.InvariantCulture, "WHERE idManufacturer = {0}", ManufacturerID)
                        );

            DBHelper dbh = new DBHelper(szQ);
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("Manufacturer", ManufacturerName);
                comm.Parameters.AddWithValue("defSim", (int)AllowedTypes);
            });

            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, Resources.Makes.errSaveManufacturerFailed, szQ, dbh.LastError));

            FlushCache();

            if (fIsNew)
            {
                ManufacturerID = dbh.LastInsertedRowId;
                util.NotifyAdminEvent("New manufacturer added", String.Format(CultureInfo.CurrentCulture, "New manufacturer '{0}' added by user {1}.", ManufacturerName, Profile.GetUser(HttpContext.Current.User.Identity.Name).DetailedName), ProfileRoles.maskCanManageData);
            }

            return true;
        }

        protected void InitFromDataReader(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            ManufacturerID = Convert.ToInt32(dr["idManufacturer"], CultureInfo.InvariantCulture);
            ManufacturerName = (string)(dr["Manufacturer"]);
            AllowedTypes = (AllowedAircraftTypes)Convert.ToInt32(dr["DefaultSim"], CultureInfo.InvariantCulture);
        }

        private void FLoad(string szWhere, MySqlParameter p)
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM manufacturers {0}", szWhere));
            if (!dbh.ReadRow(
                (comm) => { comm.Parameters.Add(p); },
                (dr) => { InitFromDataReader(dr); }))
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Unable to load manufacturer {0}: {1}", ManufacturerID, dbh.LastError));
        }
        #endregion

        /// <summary>
        /// Gets a list of all manufacturers from the database.  NOT CACHED
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Manufacturer> AllManufacturers()
        {
            DBHelper dbh = new DBHelper("SELECT * FROM Manufacturers ORDER BY manufacturer ASC");
            List<Manufacturer> lst = new List<Manufacturer>();
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new Manufacturer(dr)); });
            if (HttpRuntime.Cache != null)
                HttpRuntime.Cache.Add(szCacheKey, lst, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(1, 0, 0), System.Web.Caching.CacheItemPriority.Low, null);
            HttpRuntime.Cache[szCacheKey] = lst;
            return lst;
        }

        /// <summary>
        /// Gets all manufacturers, cached.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Manufacturer> CachedManufacturers()
        {
            object o = null;
            if (HttpRuntime.Cache != null && (o = HttpRuntime.Cache[szCacheKey]) != null)
                return (IEnumerable<Manufacturer>) o;
            IEnumerable<Manufacturer> lst = AllManufacturers();
            return lst;
        }

        public static void FlushCache()
        {
            if (HttpRuntime.Cache != null)
                HttpRuntime.Cache.Remove(szCacheKey);
        }

        public Boolean FIsValid()
        {
            return ManufacturerName.Trim().Length > 0;
        }
    }
}
