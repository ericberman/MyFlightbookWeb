using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DDay.iCal;
using DDay.iCal.Serialization;
using DDay.iCal.Serialization.iCalendar;
using MyFlightbook;
using MyFlightbook.Schedule;
using MyFlightbook.Clubs;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
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

            using (iCalendar ic = new iCalendar())
            {
                ic.AddTimeZone(c.TimeZone);

                string szTitle = string.Empty;

                foreach (string sid in rgSIDs)
                {
                    ScheduledEvent se = ScheduledEvent.AppointmentByID(sid, c.TimeZone);

                    if (se == null)
                        throw new MyFlightbookException(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Invalid scheduled event ID: {0}", sid));

                    if (!fIsAdmin && Page.User.Identity.Name.CompareOrdinal(se.OwningUser) != 0)
                        throw new MyFlightbookException("Attempt to download appointment that you don't own!");

                    ClubAircraft ca = c.MemberAircraft.FirstOrDefault(ca2 => ca2.AircraftID.ToString(System.Globalization.CultureInfo.InvariantCulture).CompareOrdinal(se.ResourceID) == 0);

                    Event ev = ic.Create<Event>();
                    ev.UID = se.ID;
                    ev.IsAllDay = false;
                    ev.Start = new iCalDateTime(se.StartUtc, TimeZoneInfo.Utc.Id);
                    ev.End = new iCalDateTime(se.EndUtc, TimeZoneInfo.Utc.Id);
                    ev.Start.HasTime = ev.End.HasTime = true;   // has time is false if the ultimate time is midnight.
                    szTitle = ev.Description = ev.Summary = String.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}{1}", ca == null ? string.Empty : String.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} - ", ca.DisplayTailnumber), se.Body);
                    ev.Location = c.HomeAirport == null ? c.HomeAirportCode : String.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} - {1}", c.HomeAirportCode, c.HomeAirport.Name);

                    Alarm a = new Alarm();
                    a.Action = AlarmAction.Display;
                    a.Description = ev.Summary;
                    a.Trigger = new Trigger();
                    a.Trigger.DateTime = ev.Start.AddMinutes(-30);
                    ev.Alarms.Add(a);

                    ic.Method = "PUBLISH";
                }

                iCalendarSerializer s = new iCalendarSerializer();

                string output = s.SerializeToString(ic);
                Page.Response.Clear();
                Page.Response.ContentType = "text/calendar";
                Response.AddHeader("Content-Disposition", String.Format(System.Globalization.CultureInfo.InvariantCulture, "inline;filename={0}", Branding.ReBrand(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}appt.ics", szTitle)).Replace(" ", "-")));
                Response.Write(output);
                Response.Flush();
                Response.End();
            }
        }
    }
}