using MyFlightbook;
using MyFlightbook.Clubs;
using MyFlightbook.Schedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Services;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_Schedule : System.Web.UI.Page
{
    protected enum PrivilegeLevel { ReadWrite, ReadOnly }

    protected static bool IsValidCaller(int idClub, PrivilegeLevel level = PrivilegeLevel.ReadWrite)
    {
        // Check that you are authenticated - prevent cross-site scripting
        if (HttpContext.Current == null || HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
            throw new MyFlightbookException("You must be authenticated to make this call");

        if (idClub == Club.ClubIDNew)
            throw new MyFlightbookException("Each scheduled item must be in the context of a club.");

        Club c = Club.ClubWithID(idClub);
        if (c == null)
            throw new MyFlightbookException("Invalid club specification - no such club.");

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

                TimeZoneInfo tzi = Club.ClubWithID(clubID).TimeZone;

                // timezoneOffset is UTC time minus local time, so in Seattle it is 420 or 480 (depending on time of year)
                ScheduledEvent se = new ScheduledEvent(ScheduledEvent.ToUTC(start, tzi), ScheduledEvent.ToUTC(end, tzi), text, id, HttpContext.Current.User.Identity.Name, resource, clubID, tzi);
                if (!se.FCommit())
                    throw new MyFlightbookException(se.LastError);

                Club.ClubWithID(se.ClubID).NotifyAdd(se, HttpContext.Current.User.Identity.Name);
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
        // Fix up the owner's name
        lst.ForEach((se) => { 
            se.OwnerProfile = c.PrependsScheduleWithOwnerName ? c.Members.FirstOrDefault(cm => cm.UserName.CompareTo(se.OwningUser) == 0) : null;
            se.ReadOnly = !se.CanEdit(HttpContext.Current.User.Identity.Name, c);
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

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}
