using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightCurrency
{
    /// <summary>
    /// Custom Deadlines class
    /// </summary>
    [Serializable]
    public class DeadlineCurrency : IComparable
    {
        public enum RegenUnit { None, Days, CalendarMonths, Hours };
        public enum DeadlineMode { Calendar, Hours };
        public const int idUnknownDeadline = -1;

        #region Properties
        /// <summary>
        /// The unique ID for the deadline
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The username for the deadline
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The name for the deadline
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// When does this expire?
        /// </summary>
        public DateTime Expiration { get; set; }

        /// <summary>
        /// When regenerating, how many units do we go forward from the specified date?
        /// </summary>
        public int RegenSpan { get; set; }

        /// <summary>
        /// Regeneration type
        /// </summary>
        public RegenUnit RegenType { get; set; }

        /// <summary>
        /// Most recent validation error
        /// </summary>
        public string ErrorString { get; set; }

        /// <summary>
        /// The description of how/when the deadline is renewed.
        /// </summary>
        public string RegenDescription
        {
            get
            {
                switch (RegenType)
                {
                    case RegenUnit.CalendarMonths:
                        return String.Format(CultureInfo.CurrentCulture, Resources.Currency.deadlineRegenMonths, RegenSpan);
                    case RegenUnit.Days:
                        return String.Format(CultureInfo.CurrentCulture, Resources.Currency.deadlineRegenDays, RegenSpan);
                    case RegenUnit.None:
                        return String.Format(CultureInfo.CurrentCulture, Resources.Currency.deadlineRegenManual);
                    case RegenUnit.Hours:
                        return String.Format(CultureInfo.CurrentCulture, Resources.Currency.deadlineRegenHours, RegenSpan);
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Display-friendly version of expiration rule.
        /// </summary>
        public string ExpirationDisplay
        {
            get
            {
                return UsesHours ?
                    String.Format(CultureInfo.CurrentCulture, Resources.Currency.deadlineDueTemplateHours, AircraftHours) :
                    String.Format(CultureInfo.CurrentCulture, Resources.Currency.deadlineDueTemplateDate, Expiration.ToShortDateString());
            }
        }

        /// <summary>
        /// Prompt for how the new deadline is computed
        /// </summary>
        public string RegenPrompt
        {
            get
            {
                switch (RegenType)
                {
                    case RegenUnit.CalendarMonths:
                        return String.Format(CultureInfo.CurrentCulture, Resources.Currency.deadlineRegenPromptMonths, RegenSpan);
                    case RegenUnit.Days:
                        return String.Format(CultureInfo.CurrentCulture, Resources.Currency.deadlineRegenPromptDays, RegenSpan);
                    case RegenUnit.Hours:
                        return String.Format(CultureInfo.CurrentCulture, Resources.Currency.deadlineRegenPromptHours, RegenSpan);
                    case RegenUnit.None:
                        return Resources.Currency.deadlineRegenPromptNone;
                    default:
                        return string.Empty;
                }
            }
        }

        public string DisplayName { get { return Name + (String.IsNullOrEmpty(TailNumber) ? string.Empty : Resources.LocalizedText.LocalizedSpace + String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedParenthetical, TailNumber)); } }

        /// <summary>
        /// The aircraft for which this deadline applies, if any
        /// </summary>
        public int AircraftID { get; set; }

        /// <summary>
        /// Cached tailnumber for the aircraft to which this applies
        /// </summary>
        protected string TailNumber { get; set; }

        /// <summary>
        /// The hours at which this is due (for aircraft)
        /// </summary>
        public decimal AircraftHours { get; set; }

        /// <summary>
        /// Is this deadline based on passage of time or usage?
        /// </summary>
        public DeadlineMode Mode
        {
            get { return AircraftID > 0 && AircraftHours > 0 ? DeadlineMode.Hours : DeadlineMode.Calendar; }
        }

        /// <summary>
        /// True if this deadline is based on usage
        /// </summary>
        public bool UsesHours
        {
            get { return Mode == DeadlineMode.Hours; }
        }

        /// <summary>
        /// Is this deadline shared among multiple users (i.e., tied to an aircraft rather than to a specific user?)
        /// </summary>
        public bool IsSharedAircraftDeadline
        {
            get { return Username == null && AircraftID > 0; }
        }

        /// <summary>
        /// Says whether or not the AircraftID is meaningful.
        /// </summary>
        public bool HasAssociatedAircraft
        {
            get { return AircraftID > 0; }
        }
        #endregion

        #region Initialization
        public DeadlineCurrency()
        {
            ID = idUnknownDeadline;
            AircraftID = 0;
            AircraftHours = 0.0M;
            Expiration = DateTime.MinValue;
        }

        public DeadlineCurrency(string szUser, string szName, DateTime dtExpiration, int regenspan, RegenUnit regentype, int aircraftID, decimal aircraftHours) : this()
        {
            Username = szUser;
            Name = szName;
            Expiration = dtExpiration;
            RegenSpan = regenspan;
            RegenType = regentype;
            AircraftID = aircraftID;
            AircraftHours = aircraftHours;
        }

        protected DeadlineCurrency(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            ID = Convert.ToInt32(dr["idDeadlines"], CultureInfo.InvariantCulture);
            Username = (string) util.ReadNullableField(dr, "username", null);
            Name = (string)dr["Name"];
            Expiration = Convert.ToDateTime(dr["expiration"], CultureInfo.InvariantCulture);
            RegenSpan = Convert.ToInt32(dr["regenspan"], CultureInfo.InvariantCulture);
            RegenType = (RegenUnit)Convert.ToInt32(dr["regenunit"], CultureInfo.InvariantCulture);
            AircraftID = Convert.ToInt32(dr["aircraftID"], CultureInfo.InvariantCulture);
            AircraftHours = Convert.ToDecimal(dr["aircraftHours"], CultureInfo.InvariantCulture);
            TailNumber = util.ReadNullableString(dr, "tailnumber");
        }
        #endregion

        public bool IsValid()
        {
            ErrorString = string.Empty;

            if (String.IsNullOrEmpty(Name))
                ErrorString = Resources.Currency.errDeadlineNoName;
            if (String.IsNullOrEmpty(Username) && AircraftID <= 0)
                ErrorString = "BUG: need to have EITHER a user owner OR an aircraft owner";
            if (RegenSpan <= 0 && RegenType != RegenUnit.None)
                ErrorString = Resources.Currency.errDeadlineNoSpan;
            if (!Expiration.HasValue() && AircraftHours <= 0)
                ErrorString = Resources.Currency.errDeadlineNoDate;
            if (Expiration.HasValue() && AircraftHours > 0)
                ErrorString = Resources.Currency.errDeadlineHoursOrDate;
            if (AircraftHours > 0 && AircraftID <= 0)
                ErrorString = Resources.Currency.errDeadlineHoursWithoutAircraft;
            return ErrorString.Length == 0;
        }

        public override string ToString()
        {
            return (AircraftID > 0) ?
                String.Format(CultureInfo.CurrentCulture, Resources.Currency.deadlineDisplayFormatAircraft, TailNumber, Name, ExpirationDisplay, RegenDescription) : 
                String.Format(CultureInfo.CurrentCulture, Resources.Currency.deadlineDisplayFormat, Name, ExpirationDisplay, RegenDescription);
        }

        /// <summary>
        /// Computes a new due date based on the passed-in date and the regen rules
        /// </summary>
        /// <param name="dt">The date the regen is requested</param>
        /// <returns>The new due date</returns>
        public DateTime NewDueDateBasedOnDate(DateTime dt)
        {
            if (!dt.HasValue())
                dt = Expiration;

            switch (RegenType)
            {
                default:
                case RegenUnit.None:
                    return dt;
                case RegenUnit.Days:
                    return dt.AddDays(RegenSpan);
                case RegenUnit.CalendarMonths:
                    return dt.AddCalendarMonths(RegenSpan);
                case RegenUnit.Hours:
                    throw new MyFlightbookException("Deadline is hour based, not date based!");
            }
        }

        /// <summary>
        /// Returns the regen
        /// </summary>
        /// <param name="hours"></param>
        /// <returns></returns>
        public decimal NewHoursBasedOnHours(decimal hours = 0.0M)
        {
            return (hours == 0.0M ? AircraftHours : hours) + RegenSpan;
        }

        #region Database
        /// <summary>
        /// Saves (inserts or updates) the deadline currency into the DB
        /// </summary>
        public bool FCommit()
        {
            if (!IsValid())
                return false;

            if (AircraftID <= 0)
            {
                AircraftID = 0;
                AircraftHours = 0;
            }

            string szQ = String.Format(CultureInfo.InvariantCulture, "{0} SET username=?user, Name=?displayname, Expiration=?expiration, RegenSpan=?regenspan, RegenUnit=?regentype, aircraftID=?aircraft, aircraftHours=?hours {1}",
                ID == idUnknownDeadline ? "INSERT INTO deadlines" : "UPDATE deadlines", ID == idUnknownDeadline ? string.Empty : "WHERE idDeadlines=?id");
            DBHelper dbh = new DBHelper(szQ);
            dbh.DoNonQuery(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("expiration", Expiration.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    comm.Parameters.AddWithValue("regenspan", RegenSpan);
                    comm.Parameters.AddWithValue("regentype", (int)RegenType);
                    comm.Parameters.AddWithValue("user", Username);
                    comm.Parameters.AddWithValue("displayname", Name.LimitTo(255));
                    comm.Parameters.AddWithValue("aircraft", AircraftID);
                    comm.Parameters.AddWithValue("hours", AircraftHours);
                    comm.Parameters.AddWithValue("id", ID);
                });
            ErrorString = dbh.LastError;
            return ErrorString.Length == 0;
        }

        /// <summary>
        /// Retrieves all of the deadlines owned by the specified user
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <param name="fIncludeSharedAircraftDeadlines">True to include deadlines for shared aircraft (i.e., for currency)</param>
        /// <param name="idAircraft">The aircraft to which to restrict</param>
        /// <returns>A list of matching deadlines.</returns>
        public static IEnumerable<DeadlineCurrency> DeadlinesForUser(string szUser, int idAircraft = Aircraft.idAircraftUnknown, bool fIncludeSharedDeadlines = false)
        {
            string szWhere = string.Empty;
            const string szQBase = "SELECT d.*, ac.tailnumber FROM deadlines d LEFT JOIN aircraft ac ON d.aircraftID = ac.idaircraft WHERE ";

            if (idAircraft <= 0)   // just user deadlines - no restriction on aircraft
                szWhere = "username=?user";
            else if (fIncludeSharedDeadlines)               // AircraftID matches, either yours or shared
                szWhere = "aircraftID=?id AND (username=?user OR username IS NULL)";
            else                                            // AircraftID matches, only yours
                szWhere = "aircraftID=?id AND username=?user ";

            List<DeadlineCurrency> lst = new List<DeadlineCurrency>();
            DBHelper dbh = new DBHelper(szQBase + szWhere);
            dbh.ReadRows(
                (comm) => 
                {
                    comm.Parameters.AddWithValue("user", szUser);
                    comm.Parameters.AddWithValue("id", idAircraft);
                },
                (dr) => { lst.Add(new DeadlineCurrency(dr)); });
            return lst;
        }

        /// <summary>
        /// Retrieves all deadlines that are owned by aircraft that the user maintains.  
        /// I.e., shared aircraft deadlines for which (a) the user has recorded SOME maintenance on the aircraft and (b) the aircraft is active.
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <returns>A list (enumerable) of matching deadline currencies.</returns>
        public static IEnumerable<DeadlineCurrency> SharedAircraftDeadlinesForUser(string szUser)
        {
            string szQ = @"SELECT 
    d.*, ac.tailnumber
FROM
    useraircraft ua
        INNER JOIN
    maintenancelog ml ON ua.username = ml.user
        INNER JOIN
    deadlines d ON (ml.idaircraft = d.aircraftID
        AND d.username IS NULL)
        INNER JOIN
    aircraft ac ON d.aircraftID = ac.idaircraft
WHERE
    ua.username = ?user
        AND (ua.Flags & 0x0008) = 0
GROUP BY d.iddeadlines";

            List<DeadlineCurrency> lst = new List<DeadlineCurrency>();
            DBHelper dbh = new DBHelper(szQ);
            dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("user", szUser); },
                (dr) => { lst.Add(new DeadlineCurrency(dr)); });
            return lst;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lstIn"></param>
        /// <param name="daysForWarning">How many days to use for "getting close"</param>
        /// <returns></returns>
        public static IEnumerable<CurrencyStatusItem> CurrencyForDeadlines(IEnumerable<DeadlineCurrency> lstIn, int daysForWarning = 30)
        {
            List<CurrencyStatusItem> lst = new List<CurrencyStatusItem>();

            if (lstIn == null)
                return lst;

            foreach (DeadlineCurrency dc in lstIn)
            {
                string szLabel = dc.AircraftID > 0 ? String.Format(CultureInfo.CurrentCulture, "{0} - {1}", dc.TailNumber, dc.Name) : dc.Name;

                if (dc.UsesHours)
                    lst.Add(new CurrencyStatusItem(szLabel, dc.AircraftHours.ToString("#,##0.0#", CultureInfo.CurrentCulture), CurrencyState.NoDate, string.Empty)
                    { AssociatedResourceID = dc.AircraftID, CurrencyGroup = (dc.IsSharedAircraftDeadline ? CurrencyStatusItem.CurrencyGroups.AircraftDeadline : CurrencyStatusItem.CurrencyGroups.Deadline) });
                else if (dc.Expiration.HasValue())
                {
                    TimeSpan ts = dc.Expiration.Subtract(DateTime.Now);
                    int days = (int)Math.Ceiling(ts.TotalDays);
                    CurrencyState cs = (ts.Days < 0) ? CurrencyState.NotCurrent : ((days < daysForWarning) ? CurrencyState.GettingClose : CurrencyState.OK);
                    lst.Add(new CurrencyStatusItem(szLabel, dc.Expiration.ToShortDateString(), cs, cs == CurrencyState.GettingClose ? String.Format(CultureInfo.CurrentCulture, Resources.Profile.ProfileCurrencyStatusClose, days) :
                                                                               (cs == CurrencyState.NotCurrent) ? String.Format(CultureInfo.CurrentCulture, Resources.Profile.ProfileCurrencyStatusNotCurrent, -days) : string.Empty)
                    { AssociatedResourceID = dc.AircraftID, CurrencyGroup = (dc.IsSharedAircraftDeadline ? CurrencyStatusItem.CurrencyGroups.AircraftDeadline : CurrencyStatusItem.CurrencyGroups.Deadline) });
                }

            }
            return lst;
        }

        /// <summary>
        /// Return the currency status for all of the user's current deadlines, including ones owned by aircraft that (a) are in their profile, (b) are active, and (c) they have performed maintenance on.
        /// </summary>
        /// <param name="szUser">The user</param>
        /// <returns></returns>
        public static IEnumerable<DeadlineCurrency> DeadlinesForUserCurrency(string szUser)
        {
            List<DeadlineCurrency> deadlines = new List<DeadlineCurrency>(DeadlinesForUser(szUser));
            deadlines.AddRange(SharedAircraftDeadlinesForUser(szUser));
            deadlines.Sort();

            return deadlines;
        }

        /// <summary>
        /// Deletes the specified event.
        /// </summary>
        public void FDelete()
        {
            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery(String.Format(CultureInfo.InvariantCulture, "DELETE FROM deadlines WHERE idDeadlines={0}", ID), (comm) => { });
            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException("Error deleting deadline: " + dbh.LastError);
        }
        #endregion

        #region Comparison for maintenance logging
        /// <summary>
        /// Generates a description of the change in hours or date between the specified and the new version of the deadline
        /// </summary>
        /// <param name="dcOriginal"></param>
        /// <returns></returns>
        public string DifferenceDescription(DeadlineCurrency dcOriginal)
        {
            if (dcOriginal == null)
                throw new ArgumentNullException("dcOriginal");
            if (UsesHours && !dcOriginal.AircraftHours.EqualsToPrecision(AircraftHours))
                return String.Format(CultureInfo.CurrentCulture, Resources.Currency.DeadlineChangedHours, Name, dcOriginal.AircraftHours, AircraftHours);
            if (!UsesHours && dcOriginal.Expiration.CompareTo(Expiration) != 0)
                return String.Format(CultureInfo.CurrentCulture, Resources.Currency.DeadlineChangedDate, Name, dcOriginal.Expiration, Expiration);
            return string.Empty;
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;

            DeadlineCurrency dc = (DeadlineCurrency)obj;
            if (dc.AircraftID == AircraftID)
                return DisplayName.CompareCurrentCultureIgnoreCase(dc.DisplayName);
            else
                return AircraftID.CompareTo(dc.AircraftID);
        }
        #endregion

        #region Export
        /// <summary>
        /// Provide a coalesced set of deadlines for a given user/aircraft in a single string (enables CSV export)
        /// </summary>
        /// <param name="szUser">User.  May be null for aircraft-only deadlines</param>
        /// <param name="idAircraft">Deadlines to display</param>
        /// <param name="separator">Separator to use</param>
        /// <returns>A string that can be placed in a label</returns>
        public static string CoalescedDeadlinesForAircraft(string szUser, int idAircraft, string separator = "\r\n")
        {
            if (idAircraft <= 0)
                throw new MyFlightbookValidationException("Can't show coalesced deadlines for aircraft with invalid aircraft ID");
            IEnumerable<DeadlineCurrency> rgdc = DeadlineCurrency.DeadlinesForUser(szUser, idAircraft, true);
            List<string> lst = new List<string>();
            foreach (DeadlineCurrency dc in rgdc)
                lst.Add(String.Format(CultureInfo.CurrentCulture, "{0} - {1}", dc.Name, dc.ExpirationDisplay));
            return String.Join(separator, lst);
        }
        #endregion
    }

    public class DeadlineEventArgs : EventArgs
    {
        public DeadlineCurrency OriginalDeadline { get; set; }
        public DeadlineCurrency NewDeadline { get; set; }

        public DeadlineEventArgs() : base() { }

        public DeadlineEventArgs(DeadlineCurrency dcOriginal, DeadlineCurrency dcNew) : base()
        {
            OriginalDeadline = dcOriginal;
            NewDeadline = dcNew;
        }
    }
}