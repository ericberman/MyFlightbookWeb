using MyFlightbook.Schedule;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Clubs
{
    public partial class ScheduleWebServices : Page
    {
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
        [WebMethod(EnableSession = true)]
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

        [WebMethod(EnableSession = true)]
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

        [WebMethod(EnableSession = true)]
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

        [WebMethod(EnableSession = true)]
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

        [WebMethod(EnableSession = true)]
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

        protected void Page_Load(object sender, EventArgs e)
        {

        }
    }
}