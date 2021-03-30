using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

/******************************************************
 * 
 * Copyright (c) 2019-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// A flight that needs to be reviewed before it is saved; it has either not yet occured, or it has been imported but not saved
    /// As such, it may fail various validation checks, and is stored in a separate table in JSON format.
    /// </summary>
    [Serializable]
    public class PendingFlight : LogbookEntry
    {
        #region Properties
        /// <summary>
        /// The ID of the flight in the PENDING table.  Assigned on create.
        /// </summary>
        public string PendingID { get; set; }

        // Null this out to avoid pointless JSON bloat
        public override string SendFlightLink
        {
            get { return null; }
            set { }
        }

        // Null this out to avoid pointless JSON bloat
        public override string SocialMediaLink
        {
            get { return null; }
            set { }
        }
        #endregion

        #region Constructors
        public PendingFlight() : base()
        {
            PendingID = Guid.NewGuid().ToString();
        }

        public PendingFlight(string ID) : this()
        {
            PendingID = ID;
        }

        private PendingFlight(MySqlDataReader dr) : this()
        {
            User = dr["username"].ToString();
            PendingID = dr["id"].ToString();
            string jsonflight = dr["jsonflight"].ToString();
            if (jsonflight != null)
                JsonConvert.PopulateObject(jsonflight, this);
            // Populating an object doesn't fully flesh out a custom property with its type
            foreach (CustomFlightProperty cfp in this.CustomProperties)
                cfp.InitPropertyType(new CustomPropertyType[] { CustomPropertyType.GetCustomPropertyType(cfp.PropTypeID) });
        }

        public PendingFlight(LogbookEntry le) : this()
        {
            if (le == null)
                return;
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(le, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore }), this);
        }
        #endregion

        #region Database
        /// <summary>
        /// Get any pending flights for the specified user
        /// </summary>
        /// <param name="szUser">username for whom to retrieve flights</param>
        /// <returns>An enumerable of flights</returns>
        static public IEnumerable<PendingFlight> PendingFlightsForUser(string szUser)
        {
            List<PendingFlight> lst = new List<PendingFlight>();
            DBHelper dbh = new DBHelper("SELECT * FROM pendingflights WHERE username=?user");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("user", szUser); },
                (dr) => { lst.Add(new PendingFlight(dr)); });

            // Sort by date, desc
            lst.Sort((l1, l2) => { return LogbookEntryCore.CompareFlights(l1, l2, "Date", System.Web.UI.WebControls.SortDirection.Descending); });
            return lst;
        }

        /// <summary>
        /// See who has a lot of pending flights and thus might need a nudge to deal with them.
        /// </summary>
        /// <param name="threshold">Threshold for inclusion</param>
        /// <returns>A dictionary with a set of usernames and the count of their pending flights</returns>
        static public IDictionary<string, int> UsersWithLotsOfPendingFlights(int threshold)
        {
            Dictionary<string, int> d = new Dictionary<string, int>();
            DBHelper dbh = new DBHelper("SELECT username, COUNT(id) AS num FROM pendingflights GROUP BY username HAVING num > ?thresh ORDER BY num DESC");
            dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("thresh", threshold); },
                (dr) => { d[(string)dr["username"]] = Convert.ToInt32(dr["num"], System.Globalization.CultureInfo.InvariantCulture); });
            return d;
        }

        /// <summary>
        /// Deletes the pending flight from the pending flights table
        /// </summary>
        /// <param name="id">The id of the flight to delete</param>
        static public void DeletePendingFlight(string id)
        {
            DBHelper dbh = new DBHelper("DELETE FROM pendingflights WHERE id=?idflight");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idflight", id); });
        }

        static public void DeletePendingFlightsForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));
            DBHelper dbh = new DBHelper("DELETE FROM pendingflights WHERE username=?uname");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("uname", szUser); });
        }

        /// <summary>
        /// Deletes this pending flight from the pending flights table
        /// </summary>
        public void Delete()
        {
            DeletePendingFlight(this.PendingID);
        }

        /// <summary>
        /// Saves a pending flight for later
        /// </summary>
        public void Commit()
        {
            if (String.IsNullOrWhiteSpace(User))
                throw new InvalidOperationException("No username specified for pending flight");
            if (String.IsNullOrWhiteSpace(PendingID))
                throw new InvalidOperationException("No unique ID specified for pending flight");

            FlightID = 0;   // don't set a flight ID

            // Make sure that the date-of-flight is indeed just a date
            Date = DateTime.SpecifyKind(Date.Date, DateTimeKind.Unspecified);
            // Since we're setting the pendingID in its own column, no reason to put it in the JSON as well.
            string szPending = PendingID;
            PendingID = null;
            string szJSON = JsonConvert.SerializeObject(this, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling=NullValueHandling.Ignore });
            PendingID = szPending;

            DBHelper dbh = new DBHelper("REPLACE INTO pendingflights SET username=?user, id=?idflight, jsonflight=?json");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("user", User);
                comm.Parameters.AddWithValue("json", szJSON);
                comm.Parameters.AddWithValue("idflight", PendingID);
            });
        }

        /// <summary>
        /// Commits the flight and deletes it from pending.
        /// </summary>
        /// <param name="fUpdateFlightData"></param>
        /// <param name="fUpdateSignature"></param>
        /// <returns></returns>
        public override bool FCommit(bool fUpdateFlightData = false, bool fUpdateSignature = false)
        {
            if (FlightID >= 0)
                FlightID = LogbookEntry.idFlightNew;    // might have been 0 to go up/down the wire.
            bool fSuccess = base.FCommit(fUpdateFlightData, fUpdateSignature);
            if (fSuccess)
                Delete();
            return fSuccess;
        }
        #endregion
    }
}