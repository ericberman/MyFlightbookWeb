using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using MyFlightbook;
using MyFlightbook.Clubs;
using MyFlightbook.Schedule;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_IcalAppt : System.Web.UI.Page
{
    private const string szFormatYahoo = "yyyyMMddTHHmm00";

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

            string szFormat = util.GetStringParam(Request, "fmt");

            switch (szFormat.ToUpperInvariant())
            {
                case "ICAL":
                default:
                    WriteICal(ic);
                    break;
                case "G":
                    WriteGoogle(ic, c.TimeZone);
                    break;
                case "Y":
                    WriteYahoo(ic, c.TimeZone);
                    break;
            }
        }
    }

    protected void WriteYahoo(Calendar ic, TimeZoneInfo tz)
    {
        if (ic == null)
            throw new ArgumentNullException(nameof(ic));
        CalendarEvent ev = ic.Events.First();
        UriBuilder uriBuilder = new UriBuilder("https://calendar.yahoo.com/");
        // Use ParseQueryString because that is how you get an HttpValueCollection, on which ToString() works.
        NameValueCollection nvc = HttpUtility.ParseQueryString(string.Empty);

        // For yahoo, convert to local time zone.
        DateTime dtStart = TimeZoneInfo.ConvertTimeFromUtc(ev.Start.AsUtc, tz);
        DateTime dtEnd = TimeZoneInfo.ConvertTimeFromUtc(ev.End.AsUtc, tz);
        nvc.Add(new NameValueCollection()
        {
            { "v","60" },
            { "TITLE", ev.Summary },
            {"VIEW", "d" },
            { "in_loc", ev.Location },
            {"st", dtStart.ToString(szFormatYahoo, System.Globalization.CultureInfo.InvariantCulture) },
            {"et", dtEnd.ToString(szFormatYahoo, System.Globalization.CultureInfo.InvariantCulture) },
            {"URL", String.Format(System.Globalization.CultureInfo.InvariantCulture, "website:{0}", Request.Url.Host) },
            {"DESC", ev.Summary },
            {"rem1", "30M" }
        });
        uriBuilder.Query = nvc.ToString();

        Response.Redirect(uriBuilder.Uri.ToString());
    }

    protected void WriteGoogle(Calendar ic, TimeZoneInfo tz)
    {
        if (ic == null)
            throw new ArgumentNullException(nameof(ic));
        CalendarEvent ev = ic.Events.First();
        UriBuilder uriBuilder = new UriBuilder("https://www.google.com/calendar/event");
        // Use ParseQueryString because that is how you get an HttpValueCollection, on which ToString() works.
        NameValueCollection nvc = HttpUtility.ParseQueryString(string.Empty);

        // For google, convert to local time zone.
        DateTime dtStart = TimeZoneInfo.ConvertTimeFromUtc(ev.Start.AsUtc, tz);
        DateTime dtEnd = TimeZoneInfo.ConvertTimeFromUtc(ev.End.AsUtc, tz);
        nvc.Add(new NameValueCollection()
        {
            { "action","TEMPLATE" },
            { "text", ev.Summary },
            {"dates", String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}/{1}", dtStart.ToString("yyyyMMddTHHmm00", System.Globalization.CultureInfo.InvariantCulture), dtEnd.ToString("yyyyMMddTHHmm00", System.Globalization.CultureInfo.InvariantCulture)) },
            {"location", ev.Location },
            {"trp", "true" },
            {"sprop", String.Format(System.Globalization.CultureInfo.InvariantCulture, "website:{0}", Request.Url.Host) }
        });
        uriBuilder.Query = nvc.ToString();

        Response.Redirect(uriBuilder.Uri.ToString());
    }

    protected void WriteICal(Calendar ic)
    {
        if (ic == null)
            throw new ArgumentNullException(nameof(ic));
        CalendarEvent ev = ic.Events.First();
        CalendarSerializer s = new CalendarSerializer();

        string output = s.SerializeToString(ic);
        Page.Response.Clear();
        Page.Response.ContentType = "text/calendar";
        Response.AddHeader("Content-Disposition", String.Format(System.Globalization.CultureInfo.InvariantCulture, "inline;filename={0}", Branding.ReBrand(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}-appt.ics", ev.Summary)).Replace(" ", "-")));
        Response.Write(output);
        Response.Flush();
        Response.End();
    }
}