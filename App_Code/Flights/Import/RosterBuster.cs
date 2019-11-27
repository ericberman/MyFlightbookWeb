using MyFlightbook.CSV;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    public class RosterBuster : ExternalFormat
    {
        #region Properties
        public string Route { get; set; }

        public DateTime FlightDate { get; set; }
        public DateTime BlockOut { get; set; }
        public DateTime BlockIn { get; set; }
        public string FlightNumber { get; set; }
        public string TimeZone { get; set; }
        #endregion

        public RosterBuster()
        {
            Route = FlightNumber = TimeZone = string.Empty;
            BlockIn = BlockOut = FlightDate = DateTime.MinValue;
        }

        public override LogbookEntry ToLogbookEntry()
        {
            PendingFlight pf = new PendingFlight()
            {
                Date = FlightDate,
                Route = this.Route
            };
            pf.CustomProperties.SetItems(new CustomFlightProperty[]
            {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockOut, BlockOut, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockIn, BlockIn, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightNumber, FlightNumber)
            });

            return pf;
        }
    }

    /*
     Sample Roster Buster layout - note that it's the subjects that look like routes that should be imported (others ignored), and no aircraft are provided.
        Could conceivably pick up the duty time (from "No Duty"), but for now we'll ignore that.

        Subject,Location,Start date,Start Time,End date,End Time,All day event,Show time as,ForInfoTimeZone,Categories
        No Duty,(0000Z-2359Z) NSD,10/1/2018,0:00:00,10/1/2018,0:00:00,TRUE,3,UTC,RosterBuster
        Check Out,(0710Z-0710Z) C/O,10/1/2018,7:10:00,10/1/2018,7:10:00,FALSE,3,UTC,RosterBuster
        Report,0056Z,10/2/2018,0:56:00,10/2/2018,0:56:00,FALSE,3,UTC,RosterBuster
        RNO - LAX,(0131Z-0318Z) CP6064,10/2/2018,1:31:00,10/2/2018,3:18:00,FALSE,3,UTC,RosterBuster
        LAX - PDX,(0430Z-0708Z) CP6044,10/2/2018,4:30:00,10/2/2018,7:08:00,FALSE,3,UTC,RosterBuster
        Layover,(0723Z-2352Z) HTL Hampton Inn & Suites Portland/Vancouver,10/2/2018,7:23:00,10/2/2018,23:52:00,FALSE,3,UTC,RosterBuster
        Check Out,(0723Z-0723Z) C/O,10/2/2018,7:23:00,10/2/2018,7:23:00,FALSE,3,UTC,RosterBuster
        Report,2352Z,10/2/2018,23:52:00,10/2/2018,23:52:00,FALSE,3,UTC,RosterBuster
        No Duty,(0000Z-2359Z) NSD,10/3/2018,0:00:00,10/3/2018,0:00:00,TRUE,3,UTC,RosterBuster
        PDX - LAX,(0027Z-0304Z) CP6032,10/3/2018,0:27:00,10/3/2018,3:04:00,FALSE,3,UTC,RosterBuster
        (LAX - PHX),(0415Z-0538Z) AA3258,10/3/2018,4:15:00,10/3/2018,5:38:00,FALSE,3,UTC,RosterBuster
        Check Out,(0553Z-0553Z) C/O,10/3/2018,5:53:00,10/3/2018,5:53:00,FALSE,3,UTC,RosterBuster

     */
    /// <summary>
    /// Imports from RosterBuster into pending flights.
    /// </summary>
    public class RosterBusterImporter : ExternalFormatImporter
    {

        private MatchCollection Matches = null;

        private readonly static Regex regRosterBuster = new Regex("^\\(?(?<route>\\w{3,4}[- ]+\\w{3,4})\\)?,\\(?(?<startZ>\\d{4}Z)-(?<endZ>\\d{4}Z)\\)? ?(?<FlightNum>[^,]*),(?<StartDate>[^,]+),(?<StartTime>[^,]*),(?<EndDate>[^,]+),(?<EndTime>[^,]*),([^,]*,){2}(?<Timezone>[^,]*).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public override string Name { get { return "RosterBuster"; } }

        public override bool IsPendingOnly { get { return true; } }

        public override bool CanParse(byte[] rgb)
        {
            if (rgb == null || rgb.Length == 0)
                return false;

            string sz = Encoding.UTF8.GetString(rgb);

            Matches = regRosterBuster.Matches(sz);

            return Matches.Count > 0;
        }

        public override byte[] PreProcess(byte[] rgb)
        {
            if (rgb == null || Matches == null)
                return null;

            return Encoding.UTF8.GetBytes(CSVFromDataTable(null));
        }

        public override string CSVFromDataTable(DataTable dt)
        {
            // We ignore the data table passed in - we have our data from Matches, which was initialized in CanParse.

            IEnumerable<CustomPropertyType> rgcpt = CustomPropertyType.GetCustomPropertyTypes();

            // Build up the list of RosterBuster objects first
            List<RosterBuster> lstRb = new List<RosterBuster>();
            foreach (Match ctMatch in Matches)
            {
                GroupCollection gc = ctMatch.Groups;

                try
                {
                    DateTime dtStart = Convert.ToDateTime(gc["StartDate"].Value, CultureInfo.CurrentCulture);
                    DateTime dtEnd = Convert.ToDateTime(gc["EndDate"].Value, CultureInfo.CurrentCulture);
                    int hStart = Convert.ToInt32(gc["startZ"].Value.Substring(0, 2), CultureInfo.InvariantCulture);
                    int mStart = Convert.ToInt32(gc["startZ"].Value.Substring(2, 2), CultureInfo.InvariantCulture);
                    int hEnd = Convert.ToInt32(gc["endZ"].Value.Substring(0, 2), CultureInfo.InvariantCulture);
                    int mEnd = Convert.ToInt32(gc["endZ"].Value.Substring(2, 2), CultureInfo.InvariantCulture);

                    DateTime dtStartUtc = new DateTime(dtStart.Year, dtStart.Month, dtStart.Day, hStart, mStart, 0, DateTimeKind.Utc);
                    DateTime dtEndUtc = new DateTime(dtStart.Year, dtStart.Month, dtStart.Day, hEnd, mEnd, 0, DateTimeKind.Utc);

                    if (dtEndUtc.CompareTo(dtStartUtc) < 0)
                        dtEndUtc = dtEndUtc.AddDays(1);

                    lstRb.Add(new RosterBuster()
                    {
                        FlightDate = dtStart,
                        FlightNumber = gc["FlightNum"].Value,
                        BlockOut = dtStartUtc,
                        BlockIn = dtEndUtc,
                        Route = gc["route"].Value,
                        TimeZone = gc["Timezone"].Value
                    });
                }
                catch (FormatException) { }
            }

            using (DataTable dtDst = new DataTable())
            {
                dtDst.Locale = CultureInfo.CurrentCulture;
                CSVImporter.InitializeDataTable(dtDst);
                foreach (RosterBuster rb in lstRb)
                    CSVImporter.WriteEntryToDataTable(rb.ToLogbookEntry(), dtDst);
                return CsvWriter.WriteToString(dtDst, true, true);
            }
        }

        public override IEnumerable<ExternalFormat> FromDataTable(DataTable dt)
        {
            throw new NotImplementedException();
        }
    }
}