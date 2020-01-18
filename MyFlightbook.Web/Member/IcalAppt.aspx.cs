using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using MyFlightbook;
using MyFlightbook.Clubs;
using MyFlightbook.Schedule;
using System;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2015-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_IcalAppt : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            if (!Page.User.Identity.IsAuthenticated)
                throw new MyFlightbookException("Unauthorized!");

            int idClub = util.GetIntParam(Request, "c", 0);
            if (idClub == 0)
                throw new MyFlightbookException("Invalid club");

            Club c = Club.ClubWithID(idClub);

            if (c == null)
                throw new MyFlightbookException(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Invalid club: {0}", idClub));

            string szIDs = util.GetStringParam(Request, "sid");
            string[] rgSIDs = szIDs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (rgSIDs.Length == 0)
                throw new MyFlightbookException("No scheduled events to download specified");

            bool fIsAdmin = c.HasAdmin(Page.User.Identity.Name);

            Calendar ic = new Calendar();
            ic.Calendar.AddTimeZone(c.TimeZone);

            string szTitle = string.Empty;

            foreach (string sid in rgSIDs)
            {
                ScheduledEvent se = ScheduledEvent.AppointmentByID(sid, c.TimeZone);

                if (se == null)
                    throw new MyFlightbookException(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Invalid scheduled event ID: {0}", sid));

                if (!fIsAdmin && Page.User.Identity.Name.CompareOrdinal(se.OwningUser) != 0)
                    throw new MyFlightbookException("Attempt to download appointment that you don't own!");

                ClubAircraft ca = c.MemberAircraft.FirstOrDefault(ca2 => ca2.AircraftID.ToString(System.Globalization.CultureInfo.InvariantCulture).CompareOrdinal(se.ResourceID) == 0);

                szTitle = String.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}{1}", ca == null ? string.Empty : String.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} - ", ca.DisplayTailnumber), se.Body);
                CalendarEvent ev = new CalendarEvent()
                {
                    Uid = se.ID,
                    IsAllDay = false,
                    Start = new CalDateTime(se.StartUtc, TimeZoneInfo.Utc.Id),
                    End = new CalDateTime(se.EndUtc, TimeZoneInfo.Utc.Id),
                    Description = szTitle,
                    Summary = szTitle,
                    Location = c.HomeAirport == null ? c.HomeAirportCode : String.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} - {1}", c.HomeAirportCode, c.HomeAirport.Name)
                };
                ev.Start.HasTime = ev.End.HasTime = true;  // has time is false if the ultimate time is midnight.

                Alarm a = new Alarm()
                {
                    Action = AlarmAction.Display,
                    Description = ev.Summary,
                    Trigger = new Trigger()
                    {
                        DateTime = ev.Start.AddMinutes(-30),
                        AssociatedObject = ic
                    }
                };
                ev.Alarms.Add(a);
                ic.Calendar.Events.Add(ev);
                ic.Calendar.Method = "PUBLISH";
            }

            CalendarSerializer s = new CalendarSerializer();

            string output = s.SerializeToString(ic);
            Page.Response.Clear();
            Page.Response.ContentType = "text/calendar";
            Response.AddHeader("Content-Disposition", String.Format(System.Globalization.CultureInfo.InvariantCulture, "inline;filename={0}", Branding.ReBrand(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}-appt.ics", szTitle)).Replace(" ", "-")));
            Response.Write(output);
            Response.Flush();
            Response.End();
        }
    }
}