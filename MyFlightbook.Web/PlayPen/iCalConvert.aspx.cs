using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using JouniHeikniemi.Tools.Text;
using MyFlightbook;
using System;

/******************************************************
 * 
 * Copyright (c) 2015-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_iCalConvert : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }
    protected void btnUpload_Click(object sender, EventArgs e)
    {
        try
        {
            if (!FileUpload1.HasFile)
                throw new MyFlightbookException("Please provide a file.");

            Calendar ic = new Calendar();
            using (CSVReader cr = new CSVReader(FileUpload1.FileContent))
            {
                string[] rgCols = cr.GetCSVLine(true);

                int iColSubject = -1;
                int iColStartDate = -1;
                int iColStartTime = -1;
                int iColEndDate = -1;
                int iColEndTime = -1;
                int iColLocation = -1;

                for (int i = 0; i < rgCols.Length; i++)
                {
                    switch (rgCols[i])
                    {
                        case "Subject":
                            iColSubject = i;
                            break;
                        case "Start Date":
                            iColStartDate = i;
                            break;
                        case "Start Time":
                            iColStartTime = i;
                            break;
                        case "End Date":
                            iColEndDate = i;
                            break;
                        case "End Time":
                            iColEndTime = i;
                            break;
                        case "Location":
                            iColLocation = i;
                            break;
                    }
                }

                if (iColSubject < 0)
                    throw new MyFlightbookException("No subject column found");
                if (iColStartDate < 0)
                    throw new MyFlightbookException("No start date column found");
                if (iColStartTime < 0)
                    throw new MyFlightbookException("No start time column found");
                if (iColEndDate < 0)
                    throw new MyFlightbookException("No end date column found");
                if (iColEndTime < 0)
                    throw new MyFlightbookException("No end time column found");
                if (iColLocation < 0)
                    throw new MyFlightbookException("No location column found");

                int id = 0;
                while ((rgCols = cr.GetCSVLine()) != null)
                {
                    if (String.IsNullOrEmpty(rgCols[iColSubject]))
                        continue;

                    CalendarEvent ev = new CalendarEvent()
                    {
                        Uid = id++.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        IsAllDay = false,
                        Description = rgCols[iColSubject],
                        Summary = rgCols[iColSubject],
                        Location = rgCols[iColLocation]
                    };

                    DateTime dtStart = Convert.ToDateTime(String.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} {1}", rgCols[iColStartDate], rgCols[iColStartTime]), System.Globalization.CultureInfo.CurrentCulture);
                    DateTime dtEnd = Convert.ToDateTime(String.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} {1}", rgCols[iColEndDate], rgCols[iColEndTime]), System.Globalization.CultureInfo.CurrentCulture);
                    ev.Start = new CalDateTime(dtStart, "Pacific Standard Time");
                    ev.End = new CalDateTime(dtEnd, "Pacific Standard Time");

                    Alarm a = new Alarm()
                    {
                        Action = AlarmAction.Display,
                        Description = ev.Summary,
                        Trigger = new Trigger() { DateTime = ev.Start.AddMinutes(-30) }
                    };
                    ev.Alarms.Add(a);
                    ic.Events.Add(ev);

                    ic.Calendar.Method = "PUBLISH";
                }

                CalendarSerializer s = new CalendarSerializer();

                string output = s.SerializeToString(ic);
                Page.Response.Clear();
                Page.Response.ContentType = "text/calendar";
                Response.AddHeader("Content-Disposition", String.Format(System.Globalization.CultureInfo.InvariantCulture, "inline;filename={0}.ics", txtTitle.Text));
                Response.Write(output);
                Response.Flush();
                Response.End();
            }
        }
        catch (CSVReaderInvalidCSVException ex)
        {
            lblErr.Text = ex.Message;
        }
        catch (MyFlightbookException ex)
        {
            lblErr.Text = ex.Message;
        }
    }
}