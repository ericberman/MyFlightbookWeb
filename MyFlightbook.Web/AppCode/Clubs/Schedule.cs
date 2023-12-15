using MyFlightbook.Clubs;
using MyFlightbook.CSV;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/
namespace MyFlightbook.Schedule
{
    public enum ScheduleDisplayMode { Day, Week, Month };

    public static class SchedulePreferences
    {
        private const string szKeyCookieDisplayMode = "kcSchedDisplay";

        /// <summary>
        /// Persists/retrieves the current mode in a cookie
        /// </summary>
        public static ScheduleDisplayMode DefaultScheduleMode
        {
            get
            {
                ScheduleDisplayMode sdm = ScheduleDisplayMode.Day;  // default
                if (HttpContext.Current != null && HttpContext.Current.Response != null && HttpContext.Current.Response.Cookies != null)
                {
                    HttpCookie cookie = HttpContext.Current.Request.Cookies[szKeyCookieDisplayMode];
                    if (cookie != null)
                    {
                        if (int.TryParse(cookie.Value, out int i))
                            sdm = (ScheduleDisplayMode)i;
                    }
                }

                return sdm;
            }
            set
            {
                if (HttpContext.Current != null && HttpContext.Current.Response != null && HttpContext.Current.Response.Cookies != null)
                {
                    // Day is the default so clear the cookie if it's default.  Just saves a bit of space
                    if (value == ScheduleDisplayMode.Day)
                    {
                        HttpContext.Current.Response.Cookies[szKeyCookieDisplayMode].Value = string.Empty;
                        HttpContext.Current.Response.Cookies[szKeyCookieDisplayMode].Expires = DateTime.Now.AddDays(-5);
                    }
                    else
                        HttpContext.Current.Response.Cookies[szKeyCookieDisplayMode].Value = ((int)value).ToString(CultureInfo.InvariantCulture);
                }
            }
        }
    }

    [Serializable]
    public class ScheduledEvent
    {
        #region Creation
        public ScheduledEvent()
        {
            Body = ResourceID = ID = OwningUser = string.Empty;
            StartUtc = EndUtc = DateTime.MinValue;
            ClubID = Club.ClubIDNew;
            LocalTimeZone = TimeZoneInfo.Utc;
        }

        /// <summary>
        /// Creates a scheduled event
        /// </summary>
        /// <param name="dtStartUtc">The start time, IN UTC</param>
        /// <param name="dtEndUtc">The end time, IN UTC</param>
        /// <param name="szBody">The text of the event</param>
        /// <param name="szID">The ID of the event</param>
        /// <param name="szOwner">The owner (user) of the event</param>
        /// <param name="szResource">The ID of the resource being scheduled</param>
        /// <param name="tz">The timezone for the local time being passed in</param>
        public ScheduledEvent(DateTime dtStartUtc, DateTime dtEndUtc, string szBody, string szID, string szOwner, string szResource, int idclub, TimeZoneInfo tz)
        {
            LocalTimeZone = tz;
            StartUtc = dtStartUtc;
            EndUtc = dtEndUtc;
            Body = szBody;
            ID = szID;
            OwningUser = szOwner;
            ResourceID = szResource;
            ClubID = idclub;
        }

        public ScheduledEvent(MySqlDataReader dr, TimeZoneInfo tz)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            LocalTimeZone = tz;
            InitFromDataReader(dr);
        }
        #endregion

        #region Timezone
        /// <summary>
        /// Returns the first timezoneinfo object that matches the specified UTC offset; UTC if nothing found.
        /// </summary>
        /// <param name="ts">The timespan representing the UTC offset</param>
        /// <returns>The first matching timezone.</returns>
        public static TimeZoneInfo TimeZoneForOffset(TimeSpan ts)
        {
            // For 0, always return Utc.
            if (ts.CompareTo(TimeSpan.Zero) == 0)
                return TimeZoneInfo.Utc;

            foreach (TimeZoneInfo tzi in TimeZoneInfo.GetSystemTimeZones())
            {
                if (ts.CompareTo(tzi.BaseUtcOffset) == 0)
                    return tzi;
            }
            return TimeZoneInfo.Utc;
        }

        /// <summary>
        /// Returns the timezoneinfo object that matches the specified id.  If not found, it returns the best match for the offset (in minutes)
        /// </summary>
        /// <param name="szId">The id</param>
        /// <param name="offset">The offset</param>
        /// <returns>The best matching timezoneinfo</returns>
        public static TimeZoneInfo TimeZoneForIdOrOffset(string szId, int offset = 0)
        {
            TimeZoneInfo tz = null;
            try { tz = TimeZoneInfo.FindSystemTimeZoneById(szId); }
            catch (Exception ex) when (ex is TimeZoneNotFoundException) { }

            return tz ?? TimeZoneForOffset(TimeSpan.FromMinutes(offset));
        }

        /// <summary>
        /// Convert a local time to UTC from the specified timezoneinfo
        /// </summary>
        /// <param name="dt">The datetime to convert.  IF THE TIME IS AMBIGUOUS, STANDARD TIME IS ASSUMED</param>
        /// <param name="tzi">The timezone in which it is expressed</param>
        /// <returns>A new datetime object (or the original if it was already in UTC)</returns>
        public static DateTime ToUTC(DateTime dt, TimeZoneInfo tzi)
        {
            if (tzi == null)
                return dt;

            if (dt.Kind == DateTimeKind.Utc)    // nothing to do
                return dt;

            if (tzi.IsAmbiguousTime(dt))
                return DateTime.SpecifyKind(dt - TimeZoneInfo.Local.BaseUtcOffset, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeToUtc(dt, tzi);
        }

        /// <summary>
        /// Converts a UTC time to a local time in the specified timezone
        /// </summary>
        /// <param name="dt">The datetime to convert</param>
        /// <param name="tzi">The timezone in which to express it</param>
        /// <returns>A new datetime object (or the original if it is utc-to-utc</returns>
        public static DateTime FromUTC(DateTime dt, TimeZoneInfo tzi)
        {
            if (tzi == null)
                return dt;

            if (dt.Kind == DateTimeKind.Utc && tzi == TimeZoneInfo.Utc)
                return dt;

            if (dt.Kind == DateTimeKind.Local)
                return dt;

            return TimeZoneInfo.ConvertTimeFromUtc(dt, tzi);
        }

        #endregion

        #region Properties
        /// <summary>
        /// Starting date of the event IN UTC
        /// </summary>
        public DateTime StartUtc { get; set; }

        /// <summary>
        /// Ending date of the event IN UTC
        /// </summary>
        public DateTime EndUtc { get; set; }

        /// <summary>
        /// Body (text) of the event
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// For consumption only - displays the body with the owner name.
        /// </summary>
        public string OwnerName
        {
            get { return OwnerProfile == null ? string.Empty : OwnerProfile.UserFullName; }
        }

        /// <summary>
        /// Resource of the event (typically aircraftID)
        /// </summary>
        public string ResourceID { get; set; }

        /// <summary>
        /// Unique ID for the event
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Timezone offset for the event (not persisted).  Assign this for communication with a local client (see "start" and "end" properties)
        /// </summary>
        public TimeZoneInfo LocalTimeZone { get; set; }

        /// <summary>
        /// User that owns (created) the object
        /// </summary>
        public string OwningUser { get; set; }

        /// <summary>
        /// The ID of the Club to which this is associated.
        /// </summary>
        public int ClubID { get; set; }

        /// <summary>
        /// Validation error, if any.
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// Owner's user profile.  NOT CACHED, NOT PERSISTED, NULL BY DEFAULT
        /// </summary>
        public Profile OwnerProfile { get; set; }

        /// <summary>
        /// Resource aircraft, if aircraft.  NOT CACHED, NOT PERSISTED, NULL BY DEFAULT
        /// </summary>
        public Aircraft ResourceAircraft { get; set; }

        /// <summary>
        /// Display string for the resource aircraft, if resource aircraft is populated.
        /// </summary>
        public string AircraftDisplay
        {
            get { return ResourceAircraft == null ? string.Empty : ResourceAircraft.DisplayTailnumber; }
        }

        /// <summary>
        /// Resource user, if user (e.g., instructor, as opposed to the owner).  NOT CACHED, NOT PERSISTED, NULL BY DEFAULT
        /// </summary>
        public Profile ResourceUserProfile { get; set; }

        /// <summary>
        /// Can the item be edited by the specified user.  NOT CACHED, NOT PERSISTED, FALSE BY DEFAULT
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Start time in local time
        /// </summary>
        public DateTime LocalStart
        {
            get { return FromUTC(StartUtc, LocalTimeZone); }
            set { StartUtc = ToUTC(value, LocalTimeZone); }
        }

        /// <summary>
        /// End time in local time
        /// </summary>
        public DateTime LocalEnd
        {
            get { return FromUTC(EndUtc, LocalTimeZone); }
            set { EndUtc = ToUTC(value, LocalTimeZone); }
        }

        /// <summary>
        /// Returns the timespan of the event
        /// </summary>
        public TimeSpan Duration
        {
            get { return EndUtc.Subtract(StartUtc); }
        }

        public static string DurationString(TimeSpan t)
        {
            if (t.TotalHours == 1)
                return Resources.Schedule.intervalHour;
            else if (t.TotalHours < 24)
                return String.Format(CultureInfo.CurrentCulture, Resources.Schedule.intervalHours, t.TotalHours);
            else if (t.TotalHours == 24)
                return Resources.Schedule.intervalDay;
            else if (t.TotalHours < 48)
                return String.Format(CultureInfo.CurrentCulture, "{0}, {1}", Resources.Schedule.intervalDay, DurationString(new TimeSpan(t.Hours, t.Minutes, t.Seconds)));
            else if (t.TotalDays < 7)
                return String.Format(CultureInfo.CurrentCulture, "{0}, {1}", String.Format(CultureInfo.CurrentCulture, Resources.Schedule.intervalDays, t.Days), DurationString(new TimeSpan(t.Hours, t.Minutes, t.Seconds)));
            else if (t.TotalDays == 7)
                return Resources.Schedule.intervalWeek;
            else
                return String.Format(CultureInfo.CurrentCulture, "{0}, {1}", String.Format(CultureInfo.CurrentCulture, Resources.Schedule.intervalWeeks, t.Days / 7), DurationString(new TimeSpan(t.Days % 7, t.Hours, t.Minutes, t.Seconds)));
        }

        /// <summary>
        /// Returns a nice human-legible duration display
        /// </summary>
        public string DurationDisplay
        {
            get { return DurationString(Duration); }
        }

        /// <summary>
        /// Hack - derived string for Start that converts to the correct timezone, but is a string so that javascript can interpret it correctly
        /// </summary>
        public string start
        {
            get { return FromUTC(StartUtc, LocalTimeZone).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture); }
            set { StartUtc = ToUTC(Convert.ToDateTime(value, CultureInfo.InvariantCulture), LocalTimeZone); }
        }

        /// <summary>
        /// Hack - derived string for End that converts to the correct timezone, but is a string so that javascript can interpret it correctly
        /// </summary>
        public string end
        {
            get { return FromUTC(EndUtc, LocalTimeZone).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture); }
            set { EndUtc = ToUTC(Convert.ToDateTime(value, CultureInfo.InvariantCulture), LocalTimeZone); }
        }

        /// <summary>
        /// Internal use - conflicting items if double-booking - these need notifications
        /// </summary>
        private List<ScheduledEvent> ConflictingItems { get; set; }

        /// <summary>
        /// Compare to items to see if they are semantically the same.  Looks at:
        /// a) start and end times
        /// b) text of the body (ignoring case!)
        /// c) ID of the scheduled event
        /// d) Resource ID
        /// e) owner name
        /// </summary>
        /// <param name="se1">the first scheduled item</param>
        /// <param name="se2">the second scheduled item</param>
        /// <returns>True if all of the key semantic properties are identical</returns>
        public static bool operator ==(ScheduledEvent se1, ScheduledEvent se2)
        {
            // If both are null, or both are same instance, return true.
            if (object.ReferenceEquals(se1, se2))
            {
                return true;
            }

            if ((se1 is null) || (se2 is null))
                return false;

            return se1.StartUtc.CompareTo(se2.StartUtc) == 0 &&
                se1.EndUtc.CompareTo(se2.EndUtc) == 0 &&
                se1.Body.CompareCurrentCultureIgnoreCase(se2.Body) == 0 &&
                se1.ID.CompareOrdinalIgnoreCase(se2.ID) == 0 &&
                se1.ResourceID.CompareOrdinalIgnoreCase(se2.ResourceID) == 0 &&
                se1.OwnerName.CompareOrdinalIgnoreCase(se2.OwnerName) == 0;
        }

        public static bool operator !=(ScheduledEvent se1, ScheduledEvent se2)
        {
            return !(se1 == se2);
        }

        public override bool Equals(object obj)
        {
            return this == (ScheduledEvent) obj;
        }

        public override int GetHashCode()
        {
            return StartUtc.GetHashCode() * 17 + EndUtc.GetHashCode() * 23 + ID.GetHashCode() * 13 + ResourceID.GetHashCode() * 19 + Body.GetHashCode() * 29 + OwnerName.GetHashCode() * 7;
        }
        #endregion

        #region Authentication
        /// <summary>
        /// Can the specified user edit this item?
        /// </summary>
        /// <param name="szUser">Username</param>
        /// <returns>True if the user is the owner of the appointment or a member of the club</returns>
        public bool CanEdit(string szUser, Club c = null)
        {
            if (c == null)
                c = Club.ClubWithID(ClubID);

            bool fIsAdmin = c.HasAdmin(szUser);
            bool fIsOwner = String.Compare(szUser, OwningUser, StringComparison.Ordinal) == 0;

            // If restricted to admins, then, well, you gotta be an admin
            switch (c.EditingPolicy)
            {
                case Club.EditPolicy.AdminsOnly:
                    return fIsAdmin;
                case Club.EditPolicy.OwnersAndAdmins:
                    return fIsAdmin || fIsOwner;
                case Club.EditPolicy.AllMembers:
                    return !(c.GetMember(szUser)?.IsInactive ?? true);
                default:
                    throw new InvalidOperationException("Unknown editing policy: " + c.EditingPolicy.ToString());
            }
        }

        /// <summary>
        /// Is the current user able to double book an item?
        /// </summary>
        /// <param name="szUser">Username</param>
        /// <param name="c">Club in question (or the  owning club for this item, if null)</param>
        /// <returns></returns>
        public bool CanDoubleBook(string szUser, Club c = null)
        {
            // else fall through to the club
            if (c == null)
                c = Club.ClubWithID(ClubID);

            switch (c.DoubleBookRoleRestriction)
            {
                case Club.DoubleBookPolicy.Admins:
                    return c.HasAdmin(szUser);
                case Club.DoubleBookPolicy.None:
                    return false;
                case Club.DoubleBookPolicy.WholeClub:
                    return true;
            }
            return false;
        }
        #endregion

        #region Database
        private void InitFromDataReader(MySqlDataReader dr)
        {
            StartUtc = DateTime.SpecifyKind(Convert.ToDateTime(dr["start"], CultureInfo.InvariantCulture), DateTimeKind.Utc);
            EndUtc = DateTime.SpecifyKind(Convert.ToDateTime(dr["end"], CultureInfo.InvariantCulture), DateTimeKind.Utc);
            Body = (string)dr["body"];
            ID = (string)dr["idEvent"];
            ResourceID = (string)dr["resourceid"];
            OwningUser = (string)dr["username"];
            ClubID = Convert.ToInt32(dr["idclub"], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Does this appointment overlap with other appointments?
        /// </summary>
        /// <returns>True if this double-booking is disallowed.  Side effect: ConflictingItems is set to all conflicting items as a result of this!</returns>
        public bool FIsDisallowedDoubleBooked()
        {
            ConflictingItems = ScheduledEvent.AppointmentsInTimeRange(StartUtc, EndUtc, ResourceID, ClubID, LocalTimeZone);
            ConflictingItems.RemoveAll(se => se.ID == this.ID);  // don't worry about overlapping with ourselves!
            bool fIsDoubleBooked = ConflictingItems.Count > 0;

            if (fIsDoubleBooked)
            {
                ConflictingItems.Add(this);
                return !CanDoubleBook(OwningUser);
            }

            return false;
        }

        private void NotifyConflictingItems()
        {
            List<string> lst = new List<string>();
            Dictionary<string, Profile> dictAffectedUsers = new Dictionary<string, Profile>();
            ConflictingItems.ForEach((s) =>
            {
                if (!dictAffectedUsers.ContainsKey(s.OwningUser))
                    dictAffectedUsers.Add(s.OwningUser, Profile.GetUser(s.OwningUser));
                lst.Add(String.Format(CultureInfo.CurrentCulture, "{0}: {1:d} {2:t} ({3}){4}", dictAffectedUsers[s.OwningUser].UserFullName, s.LocalStart, s.LocalStart, s.DurationDisplay, String.IsNullOrEmpty(s.Body) ? string.Empty : String.Format(CultureInfo.CurrentCulture, " - {0}", s.Body)));
            });

            if (ResourceAircraft == null)
                ResourceAircraft = Club.ClubWithID(ClubID).MemberAircraft.FirstOrDefault(ca => ca.AircraftID.ToString(CultureInfo.InvariantCulture).CompareOrdinal(ResourceID) == 0);

            string szMsgBody = Branding.ReBrand(Resources.Schedule.Resourcedoublebooked.Replace("<% ScheduleDetail %>", String.Join("  \r\n  ", lst.ToArray())).Replace("<% Creator %>", dictAffectedUsers[OwningUser].UserFullName).Replace("<% Resource %>", ResourceAircraft == null ? string.Empty : ResourceAircraft.DisplayTailnumber));

            foreach (string sz in dictAffectedUsers.Keys)
            {
                Profile pf = dictAffectedUsers[sz];
                util.NotifyUser(Branding.ReBrand(Resources.Schedule.DoubleBookNotificationSubject), szMsgBody, new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);
            }
        }

        protected bool FIsValid()
        {
            bool fResult = true;
            try
            {
                if (String.IsNullOrEmpty(Body))
                    throw new MyFlightbookValidationException(Resources.Schedule.ErrNoDescription);

                if (String.IsNullOrEmpty(ResourceID))
                    throw new MyFlightbookValidationException(Resources.Schedule.ErrNoResource);

                TimeSpan ts = EndUtc.Subtract(StartUtc);
                if (ts.TotalMinutes < 0)
                    throw new MyFlightbookValidationException(Resources.Schedule.ErrInvalidDateTimes);

                if (ts.TotalMinutes < 1)
                    throw new MyFlightbookValidationException(Resources.Schedule.ErrZeroDuration);

                // Check for overlap with other appointments - no double booking!
                // We do this last because it hits the database.
                if (FIsDisallowedDoubleBooked())
                    throw new MyFlightbookValidationException(Resources.Schedule.ErrDoubleBooked);
            }
            catch (MyFlightbookValidationException ex)
            {
                LastError = ex.Message;
                fResult = false;
            }
            return fResult;
        }

        public bool FCommit()
        {
            if (FIsValid())
            {
                DBHelper dbh = new DBHelper("REPLACE INTO scheduledevents SET idEvent=?id, body=?body, username=?user, resourceid=?rid, start=?start, end=?end, idclub=?clubid");
                dbh.DoNonQuery((comm) =>
                    {
                        comm.Parameters.AddWithValue("id", ID);
                        comm.Parameters.AddWithValue("start", StartUtc);
                        comm.Parameters.AddWithValue("end", EndUtc);
                        comm.Parameters.AddWithValue("body", Body.LimitTo(255));
                        comm.Parameters.AddWithValue("user", OwningUser);
                        comm.Parameters.AddWithValue("rid", ResourceID);
                        comm.Parameters.AddWithValue("clubid", ClubID);
                    });
                bool fSuccess = String.IsNullOrEmpty(LastError = dbh.LastError);

                if (fSuccess && ConflictingItems.Count > 0) // send out notifications of conflict
                    NotifyConflictingItems();

                return fSuccess;
            }
            return false;
        }

        public bool FDelete()
        {
            DBHelper dbh = new DBHelper("DELETE FROM scheduledevents WHERE idEvent=?id");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("id", ID); });
            return String.IsNullOrEmpty(LastError = dbh.LastError);
        }

        #region Queries
        private static List<ScheduledEvent> AppointmentsForQuery(string szQ, Action<MySqlCommand> addParams, TimeZoneInfo tz)
        {
            DBHelper dbh = new DBHelper(szQ);
            List<ScheduledEvent> lst = new List<ScheduledEvent>();
            dbh.ReadRows((comm) => { addParams(comm); }, (dr) => { lst.Add(new ScheduledEvent(dr, tz)); });
            return lst;
        }

        public static List<ScheduledEvent> AppointmentsForResource(string resource, int idclub, TimeZoneInfo tz)
        {
            return AppointmentsForQuery(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM scheduledevents WHERE resourceID=?rid {0}", idclub == Club.ClubIDNew ? "AND idclub=?clubid" : string.Empty),
                (comm) => { comm.Parameters.AddWithValue("rid", resource); comm.Parameters.AddWithValue("clubid", idclub); }, tz);
        }

        public static List<ScheduledEvent> UpcomingAppointments(int idclub, TimeZoneInfo tz, int limit, string resource = null, string owner = null)
        {
            return AppointmentsForQuery(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM scheduledevents WHERE end >=?dtNow AND idclub=?clubid {0} {1} ORDER BY start ASC LIMIT {2}",
                resource == null ? string.Empty : "AND resourceID=?rid",
                owner == null ? string.Empty : "AND username=?owner",
                limit),
                (comm) =>
                {
                    comm.Parameters.AddWithValue("clubid", idclub);
                    comm.Parameters.AddWithValue("rid", resource);
                    comm.Parameters.AddWithValue("owner", owner);
                    comm.Parameters.AddWithValue("dtNow", DateTime.UtcNow);
                },
                tz);
        }

        public static List<ScheduledEvent> AppointmentsInTimeRange(DateTime dtStartUtc, DateTime dtEndUtc, string resource, int idclub, TimeZoneInfo tz)
        {
            return AppointmentsForQuery(String.Format(CultureInfo.InvariantCulture, "SELECT se.* FROM scheduledevents se INNER JOIN clubs c ON se.idclub=c.idclub WHERE resourceid=?rid AND se.idclub=?clubid AND start < ?endRange AND end > ?startRange AND start >= c.creationDate"),
                (comm) =>
                {
                    comm.Parameters.AddWithValue("startRange", dtStartUtc);
                    comm.Parameters.AddWithValue("endRange", dtEndUtc);
                    comm.Parameters.AddWithValue("rid", resource);
                    comm.Parameters.AddWithValue("clubid", idclub);
                }, tz);
        }

        public static List<ScheduledEvent> AppointmentsInTimeRange(DateTime dtStartUtc, DateTime dtEndUtc, int idclub, TimeZoneInfo tz)
        {
            return AppointmentsForQuery(String.Format(CultureInfo.InvariantCulture, "SELECT se.* FROM scheduledevents se INNER JOIN clubs c ON se.idclub=c.idclub WHERE se.idclub=?clubid AND start < ?endRange AND end > ?startRange AND start >= c.creationDate"),
                (comm) =>
                {
                    comm.Parameters.AddWithValue("startRange", dtStartUtc);
                    comm.Parameters.AddWithValue("endRange", dtEndUtc);
                    comm.Parameters.AddWithValue("clubid", idclub);
                }, tz);
        }

        public static ScheduledEvent AppointmentByID(string id, TimeZoneInfo tz)
        {
            List<ScheduledEvent> lst = AppointmentsForQuery("SELECT * FROM scheduledevents WHERE idEvent=?id", (comm) => { comm.Parameters.AddWithValue("id", id); }, tz);
            return (lst.Count > 0) ? lst[0] : null;
        }
        #endregion
        #endregion

        #region AvailabilityMap
        public static IDictionary<int, bool[]> ComputeAvailabilityMap(DateTime dtStart, int clubID, out Club club, int limitAircraft = Aircraft.idAircraftUnknown, int cDays = 1, int minuteInterval = 15)
        {
            if (cDays < 1 || cDays > 7)
                throw new InvalidOperationException("Can only do 1 to 7 days of availability");

            if (minuteInterval <= 0)
                throw new InvalidOperationException("Invalid minute interval");

            double IntervalsPerHour = 60.0 / minuteInterval;
            int IntervalsPerDay = (int) (24 * IntervalsPerHour);

            if (IntervalsPerDay != (24 * IntervalsPerHour))
                throw new InvalidOperationException("Minute interval must align on 24-hour boundaries");

            int totalIntervals = IntervalsPerDay * cDays;

            Dictionary<int, bool[]> d = new Dictionary<int, bool[]>();

            club = Club.ClubWithID(clubID);

            foreach (Aircraft ac in club.MemberAircraft)
                if (limitAircraft == Aircraft.idAircraftUnknown || ac.AircraftID == limitAircraft)
                    d[ac.AircraftID] = new bool[totalIntervals];

            DateTime dtStartLocal = new DateTime(dtStart.Year, dtStart.Month, dtStart.Day, 0, 0, 0, DateTimeKind.Unspecified);
            TimeZoneInfo tzi = club.TimeZone;
            DateTime dtStartUtc = ToUTC(dtStartLocal, tzi);

            // we need to request the appointments in UTC, even though we will display them in local time.
            List<ScheduledEvent> lst = AppointmentsInTimeRange(dtStartUtc, dtStartUtc.AddDays(cDays), clubID, tzi);

            foreach (ScheduledEvent e in lst)
            {
                if (!int.TryParse(e.ResourceID, out int idAircraft))
                    continue;

                // Block off any scheduled time that's in our window, rounding up and down as appropriate
                bool[] rg = d[idAircraft];

                int startingInterval = (int)Math.Floor(e.LocalStart.Subtract(dtStartLocal).TotalHours * IntervalsPerHour);
                int endingInterval = (int)Math.Ceiling(e.LocalEnd.Subtract(dtStartLocal).TotalHours * IntervalsPerHour);

                for (int interval = Math.Max(0, startingInterval); interval < endingInterval && interval < rg.Length; interval++)
                    rg[interval] = true;
            }

            return d;
        }
        #endregion

        #region Web Services
        protected enum PrivilegeLevel { ReadWrite, ReadOnly }

        protected static bool IsValidCaller(int idClub, PrivilegeLevel level = PrivilegeLevel.ReadWrite)
        {
            // Check that you are authenticated - prevent cross-site scripting
            if (HttpContext.Current == null || HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
                throw new MyFlightbookException("You must be authenticated to make this call");

            if (idClub == Club.ClubIDNew)
                throw new MyFlightbookException("Each scheduled item must be in the context of a club.");

            Club c = Club.ClubWithID(idClub) ?? throw new MyFlightbookException("Invalid club specification - no such club.");

            // Check that the user is a member of the specified club
            if (!c.HasMember(HttpContext.Current.User.Identity.Name))
                throw new MyFlightbookException("You must be a member of the club to make this call");

            if (level == PrivilegeLevel.ReadWrite && !c.CanWrite)
                throw new MyFlightbookException(Branding.ReBrand(c.Status == Club.ClubStatus.Expired ? Resources.Club.errClubPromoExpired : Resources.Club.errClubInactive));

            return true;
        }

        /// <summary>
        /// Create an event.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="id"></param>
        /// <param name="text"></param>
        /// <param name="resource"></param>
        /// <param name="clubID"></param>
        /// <returns>Empty string for success; otherwise, the error message</returns>
        public static string CreateEvent(DateTime start, DateTime end, string id, string text, string resource, int clubID = Club.ClubIDNew)
        {
            try
            {
                if (IsValidCaller(clubID))
                {
                    if (clubID == Club.ClubIDNew)
                        throw new MyFlightbookException("Attempt to schedule with a non-existant club");

                    Club c = Club.ClubWithID(clubID);

                    string szUser = HttpContext.Current.User.Identity.Name;

                    if ((c.EditingPolicy == Club.EditPolicy.AdminsOnly && !c.HasAdmin(szUser)) || c.GetMember(szUser).IsInactive)
                        throw new MyFlightbookException(Resources.Schedule.ErrUnauthorizedEdit);

                    TimeZoneInfo tzi = Club.ClubWithID(clubID).TimeZone;

                    // timezoneOffset is UTC time minus local time, so in Seattle it is 420 or 480 (depending on time of year)
                    ScheduledEvent se = new ScheduledEvent(ScheduledEvent.ToUTC(start, tzi), ScheduledEvent.ToUTC(end, tzi), HttpUtility.HtmlDecode(text), id, szUser, resource, clubID, tzi);
                    if (!se.FCommit())
                        throw new MyFlightbookException(se.LastError);

                    c.NotifyAdd(se, szUser);
                }
            }
            catch (MyFlightbookException ex)
            {
                return ex.Message;
            }
            return string.Empty;
        }

        public static ScheduledEvent[] ReadEvents(DateTime dtStart, DateTime dtEnd, int clubID = Club.ClubIDNew, string resourceName = null)
        {
            if (!IsValidCaller(clubID, PrivilegeLevel.ReadOnly))
                return null;

            Club c = Club.ClubWithID(clubID);
            TimeZoneInfo tzi = c.TimeZone;
            List<ScheduledEvent> lst = ScheduledEvent.AppointmentsInTimeRange(ScheduledEvent.ToUTC(dtStart, tzi), ScheduledEvent.ToUTC(dtEnd, tzi), resourceName, clubID, tzi);
            // Fix up the owner's name and html encode body
            lst.ForEach((se) =>
            {
                se.OwnerProfile = c.PrependsScheduleWithOwnerName ? c.Members.FirstOrDefault(cm => cm.UserName.CompareOrdinalIgnoreCase(se.OwningUser) == 0) : null;
                se.ReadOnly = !se.CanEdit(HttpContext.Current.User.Identity.Name, c);
                se.Body = HttpUtility.HtmlEncode(se.Body);
            });
            return lst.ToArray();
        }

        public static string UpdateEvent(DateTime start, DateTime end, string id, string text, string resource, int clubID)
        {
            try
            {
                if (IsValidCaller(clubID))
                {
                    Club c = Club.ClubWithID(clubID);
                    TimeZoneInfo tzi = c.TimeZone;
                    ScheduledEvent scheduledevent = ScheduledEvent.AppointmentByID(id, tzi);
                    ScheduledEvent scheduledeventOrig = new ScheduledEvent();

                    if (!scheduledevent.CanEdit(HttpContext.Current.User.Identity.Name))
                        throw new MyFlightbookException(Resources.Schedule.ErrUnauthorizedEdit);

                    if (scheduledevent != null)
                    {
                        util.CopyObject(scheduledevent, scheduledeventOrig);    // hold on to the original version, at least for now.

                        scheduledevent.StartUtc = ScheduledEvent.ToUTC(start, tzi);
                        scheduledevent.EndUtc = ScheduledEvent.ToUTC(end, tzi);
                        text = HttpUtility.HtmlDecode(text);
                        scheduledevent.Body = (String.IsNullOrWhiteSpace(text) && c.PrependsScheduleWithOwnerName) ? MyFlightbook.Profile.GetUser(scheduledevent.OwningUser).UserFullName : text;
                        if (!String.IsNullOrEmpty(resource))
                            scheduledevent.ResourceID = resource;
                        if (!scheduledevent.FCommit())
                            throw new MyFlightbookException(scheduledevent.LastError);

                        Club.ClubWithID(scheduledevent.ClubID).NotifyOfChange(scheduledeventOrig, scheduledevent, HttpContext.Current.User.Identity.Name);
                    }
                }
            }
            catch (MyFlightbookException ex)
            {
                return ex.Message;
            }

            return string.Empty;
        }

        public static string DeleteEvent(string id)
        {
            try
            {
                ScheduledEvent scheduledevent = ScheduledEvent.AppointmentByID(id, TimeZoneInfo.Utc);
                if (scheduledevent == null)
                    return Resources.Schedule.errItemNotFound;

                string szUser = HttpContext.Current.User.Identity.Name;

                if (!scheduledevent.CanEdit(szUser))
                    throw new MyFlightbookException(Resources.Schedule.ErrUnauthorizedEdit);

                if (scheduledevent.FDelete())
                {
                    // Send any notifications - but do it on a background thread so that we can return quickly
                    new Thread(() =>
                    {
                        Club c = Club.ClubWithID(scheduledevent.ClubID);
                        c.NotifyOfDelete(scheduledevent, szUser);
                    }).Start();
                }
                else
                    throw new MyFlightbookException(scheduledevent.LastError);
            }
            catch (MyFlightbookException ex)
            {
                return ex.Message;
            }
            return string.Empty;
        }

        public static string AvailabilityMap(DateTime dtStart, int clubID, int limitAircraft = Aircraft.idAircraftUnknown, int cDays = 1)
        {
            if (!IsValidCaller(clubID, PrivilegeLevel.ReadOnly))
                return null;

            int minutes = cDays == 1 ? 15 : cDays == 2 ? 60 : 180;
            int intervalsPerDay = (24 * 60) / minutes;
            int cellsPerHeader = (minutes < 60) ? (60 / minutes) : Math.Max(intervalsPerDay / 2, 1);
            int totalIntervals = cDays * intervalsPerDay;
            IDictionary<int, bool[]> d = ScheduledEvent.ComputeAvailabilityMap(dtStart, clubID, out Club club, limitAircraft = Aircraft.idAircraftUnknown, cDays, minutes);
            DateTime dtStartLocal = new DateTime(dtStart.Year, dtStart.Month, dtStart.Day, 0, 0, 0, DateTimeKind.Local);

            CultureInfo ciCurrent = util.SessionCulture ?? CultureInfo.CurrentCulture;

            // We have no Page, so things like Page_Load don't get called.
            // We fix this by faking a page and calling Server.Execute on it.  This sets up the form and - more importantly - causes Page_load to be called on loaded controls.
            using (Page p = new FormlessPage())
            {
                p.Controls.Add(new HtmlForm());
                using (StringWriter sw1 = new StringWriter(ciCurrent))
                    HttpContext.Current.Server.Execute(p, sw1, false);

                // Build the map, one day at a time in 15-minute increments.

                // Our map is created - iterate through it now.
                Table t = new Table() { CssClass = "tblAvailablityMap" };
                p.Form.Controls.Add(t);

                // Header row
                // Date first
                TableHeaderRow thrDate = new TableHeaderRow();
                t.Rows.Add(thrDate);
                thrDate.Cells.Add(new TableHeaderCell());  // upper left corner is blank cell
                // Add days
                for (int i = 0; i < cDays; i++)
                {
                    DateTime dt = dtStartLocal.AddDays(i);
                    thrDate.Cells.Add(new TableHeaderCell() { Text = dt.ToString("d", ciCurrent), ColumnSpan = intervalsPerDay, CssClass = "dateHeader" });
                }

                // Now times
                TableHeaderRow thrTimes = new TableHeaderRow();
                t.Rows.Add(thrTimes);
                thrTimes.Cells.Add(new TableHeaderCell());  // upper left corner is blank cell

                // We want the time minus any minutes, but keep it localized.
                string szTimeFormat = Regex.Replace(ciCurrent.DateTimeFormat.ShortTimePattern, ":m+", string.Empty);

                for (int iHeaderCol = 0; iHeaderCol < totalIntervals; iHeaderCol += cellsPerHeader)
                {
                    DateTime dt = dtStartLocal.AddMinutes(iHeaderCol * minutes);
                    thrTimes.Cells.Add(new TableHeaderCell()
                    {
                        Text = (dt.Minute == 0) ? dt.ToString(szTimeFormat, ciCurrent) : String.Empty,
                        ColumnSpan = cellsPerHeader,
                        VerticalAlign = VerticalAlign.Bottom,
                        CssClass = "timeHeader"
                    });
                }

                // Sort the aircraft by tail
                List<Aircraft> lstAc = new List<Aircraft>(club.MemberAircraft);
                lstAc.Sort((ac1, ac2) => { return String.Compare(ac1.DisplayTailnumber, ac2.DisplayTailnumber, true, ciCurrent); });
                foreach (Aircraft aircraft in lstAc)
                {
                    if (!d.ContainsKey(aircraft.AircraftID))    // sanity check - but should not happen
                        continue;

                    TableRow trAircraft = new TableRow();
                    t.Rows.Add(trAircraft);
                    trAircraft.Cells.Add(new TableCell() { Text = aircraft.DisplayTailnumber, CssClass = "avmResource" });

                    for (int i = 0; i < d[aircraft.AircraftID].Length; i++)
                    {
                        bool b = d[aircraft.AircraftID][i];
                        trAircraft.Cells.Add(new TableCell() { CssClass = b ? "avmBusy" : (i % cellsPerHeader == 0) ? "avmAvail" : "avmAvail avmSubHour" });
                    }
                }

                // Now, write it out.
                StringBuilder sb = new StringBuilder();
                using (StringWriter sw = new StringWriter(sb, ciCurrent))
                {
                    using (HtmlTextWriter htmlTW = new HtmlTextWriter(sw))
                    {
                        try
                        {
                            t.RenderControl(htmlTW);
                            return sb.ToString();
                        }
                        catch (ArgumentException ex) when (ex is ArgumentOutOfRangeException) { } // don't write bogus or incomplete HTML
                    }
                }
            }
            return String.Empty;
        }
        #endregion

        #region download
        public static byte[] DownloadScheduleTable(IEnumerable<ScheduledEvent> rgevents)
        {
            if (rgevents == null)
                throw new ArgumentNullException(nameof(rgevents));

            using (DataTable dt = new DataTable() { Locale = CultureInfo.CurrentCulture })
            {
                // add the header columns from the gridview
                dt.Columns.Add(new DataColumn("Start Date+Time (Local)", typeof(string)));
                dt.Columns.Add(new DataColumn("End Date+Time (Local)", typeof(string)));
                dt.Columns.Add(new DataColumn("Start Date+Time (Utc)", typeof(string)));
                dt.Columns.Add(new DataColumn("End Date+Time (Utc)", typeof(string)));
                dt.Columns.Add(new DataColumn("Start Date (Local)", typeof(string)));
                dt.Columns.Add(new DataColumn("End Date (Local)", typeof(string)));
                dt.Columns.Add(new DataColumn("Start Time (Local)", typeof(string)));
                dt.Columns.Add(new DataColumn("End Time (Local)", typeof(string)));
                dt.Columns.Add(new DataColumn("Duration", typeof(string)));
                dt.Columns.Add(new DataColumn("Aircraft", typeof(string)));
                dt.Columns.Add(new DataColumn("Club Member", typeof(string)));
                dt.Columns.Add(new DataColumn("Description", typeof(string)));

                foreach (ScheduledEvent e in rgevents)
                {
                    DataRow dr = dt.NewRow();

                    dr[0] = e.LocalStart.ToString("s", CultureInfo.CurrentCulture);
                    dr[1] = e.LocalEnd.ToString("s", CultureInfo.CurrentCulture);
                    dr[2] = e.StartUtc.ToString("s", CultureInfo.CurrentCulture);
                    dr[3] = e.EndUtc.ToString("s", CultureInfo.CurrentCulture);
                    dr[4] = e.LocalStart.ToString("d", CultureInfo.CurrentCulture);
                    dr[5] = e.LocalEnd.ToString("d", CultureInfo.CurrentCulture);
                    dr[6] = e.LocalStart.ToString("t", CultureInfo.CurrentCulture);
                    dr[7] = e.LocalEnd.ToString("t", CultureInfo.CurrentCulture);
                    dr[8] = e.DurationDisplay;
                    dr[9] = e.AircraftDisplay;
                    dr[10] = e.OwnerName;
                    dr[11] = e.Body;

                    dt.Rows.Add(dr);
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8, 1024))
                    {
                        CsvWriter.WriteToStream(sw, dt, true, true);
                        sw.Flush();
                        return ms.ToArray();
                    }
                }
            }
        }
        #endregion
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} - {1}: {2} {3} (tz:{4})", StartUtc.ToString("yyyy-MM-dd hh:mm", CultureInfo.InvariantCulture), EndUtc.ToString("yyyy-MM-dd hh:mm", CultureInfo.InvariantCulture), OwningUser, Body, LocalTimeZone == null ? "(none)" : LocalTimeZone.Id);
        }
    }
}