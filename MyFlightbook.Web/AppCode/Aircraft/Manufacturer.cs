using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2009-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Can a given aircraft model or manufacturer be a real aircraft, or must it be a sim, or can it be a sim or anonymous?
    /// E.g., a Frasca can only be a Sim, but "Generic" can be a sim or anonymous (but not real).
    /// </summary>
    public enum AllowedAircraftTypes { Any = 0, SimulatorOnly = 1, SimOrAnonymous = 2 };

    public class Manufacturer
    {
        public const int UnsavedID = -1;

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
        [Required]
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

        const string szGeneric = "Generic";

        /// <summary>
        /// Create a new manufacturer object, initialize by name
        /// </summary>
        /// <param name="szName">The name of the manufacturer</param>
        public Manufacturer(string szName) : this()
        {
            // Issue #295
            // check for variations on generic manufacturers; if so, map it to Generic.
            if (RegexUtility.FakeManufacturer.IsMatch(szName))
                szName = szGeneric;

            ManufacturerName = szName;

            try
            {
                FLoad("WHERE manufacturer=?manName", new MySqlParameter("manName", szName));
            }
            catch (Exception ex) when (ex is MyFlightbookException)
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
                util.NotifyAdminEvent("New manufacturer added", String.Format(CultureInfo.CurrentCulture, "New manufacturer '{0}' added by user {1}.", ManufacturerName, Profile.GetUser(util.RequestContext.CurrentUserName).DetailedName), ProfileRoles.maskCanManageData);
            }

            return true;
        }

        protected void InitFromDataReader(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
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

        private const string szCacheKey = "allManufacturers";

        /// <summary>
        /// Gets all manufacturers, cached.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Manufacturer> CachedManufacturers()
        {
            IEnumerable<Manufacturer> rg = (IEnumerable<Manufacturer>) util.GlobalCache.Get(szCacheKey);
            if (rg != null)
                return rg;

            DBHelper dbh = new DBHelper("SELECT * FROM Manufacturers ORDER BY manufacturer ASC");
            List<Manufacturer> lst = new List<Manufacturer>();
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new Manufacturer(dr)); });
            util.GlobalCache.Set(szCacheKey, lst, DateTimeOffset.UtcNow.AddHours(1));
            return lst;
        }

        public static void FlushCache()
        {
            util.GlobalCache.Remove(szCacheKey);
        }

        public Boolean FIsValid()
        {
            return ManufacturerName.Trim().Length > 0;
        }
    }

    public class AdminManufacturer : Manufacturer
    {
        public int ModelCount { get; set; }

        public static IEnumerable<AdminManufacturer> AllManufacturers()
        {
            DBHelper dbh = new DBHelper(@"SELECT man.*, COUNT(m.idmodel) AS modelCount
            FROM manufacturers man
            LEFT JOIN models m ON m.idmanufacturer=man.idmanufacturer
            GROUP BY man.idmanufacturer
            ORDER BY manufacturer ASC");
            List<AdminManufacturer> lst = new List<AdminManufacturer>();
            dbh.ReadRows((comm) => { },
                (dr) => { lst.Add(new AdminManufacturer(dr));});
            return lst;
        }

        public static IEnumerable<Manufacturer> DupeManufacturers()
        {
            DBHelper dbh = new DBHelper(@"SELECT *, 0 as modelCount
                FROM manufacturers
                WHERE manufacturer IN
                  (SELECT manufacturer FROM manufacturers GROUP BY manufacturer HAVING count(manufacturer) > 1) ORDER BY idmanufacturer");

            List<Manufacturer> lst = new List<Manufacturer>();
            dbh.ReadRows((comm) => { },
                (dr) => { lst.Add(new AdminManufacturer(dr)); });
            return lst;
        }

        private AdminManufacturer(MySqlDataReader dr) : base(dr)
        {
            ModelCount = Convert.ToInt32(dr["modelCount"], CultureInfo.InvariantCulture);
        }

        public AdminManufacturer(int id)
        {
            DBHelper dbh = new DBHelper(@"SELECT man.*, COUNT(m.idmodel) AS modelCount
                FROM manufacturers man
                LEFT JOIN models m ON m.idmanufacturer=man.idmanufacturer
                WHERE man.idmanufacturer=?id
                GROUP BY man.idmanufacturer
                ORDER BY manufacturer ASC");
            dbh.ReadRow((comm) => { comm.Parameters.AddWithValue("id", id); }, 
                (dr) => { 
                    InitFromDataReader(dr);
                    ModelCount = Convert.ToInt32(dr["modelCount"], CultureInfo.InvariantCulture);
                });
        }

        public static void Delete(int id)
        {
            DBHelper dbh = new DBHelper("DELETE FROM manufacturers WHERE idManufacturer=?idManufacturer");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idManufacturer", id); });
        }

        /// <summary>
        /// Merges a duplicate manufacturer into this one
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="MyFlightbookException"></exception>
        public void MergeFrom(int idToKill)
        {
            if (idToKill == UnsavedID)
                throw new InvalidOperationException("Can't merge an unsaved manufacturer");
            if (idToKill == ManufacturerID)
                throw new InvalidOperationException("Can't merge to self");

            AdminManufacturer am = new AdminManufacturer(idToKill);
            if (am.ManufacturerName.CompareCurrentCultureIgnoreCase(ManufacturerName) != 0)
                throw new InvalidOperationException("Names aren't same - is this properly a dupe?");
            if (am.AllowedTypes != AllowedTypes)
                throw new InvalidOperationException("Allowed aircraft types are not the same - ambiguous which should win");

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.CurrentCulture, "UPDATE models SET idManufacturer={0} WHERE idmanufacturer={1}", ManufacturerID, idToKill));
            if (!dbh.DoNonQuery())
                throw new MyFlightbookException("Error remapping model: " + dbh.CommandText + "\r\n" + dbh.LastError);

            // Then delete the old manufacturer
            Delete(idToKill);
        }
    }
}
