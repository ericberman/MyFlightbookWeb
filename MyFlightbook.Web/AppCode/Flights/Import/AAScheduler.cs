using MyFlightbook.Airports;
using MyFlightbook.CSV;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    /* 
      Below is a sample American Airlines export.  Note that the "Activity" distinguishes flights from other things.  And there's no flight time.
        Activity,Start Date, Start Time, End Date, End Time, SEQ#, Leg#, Flight#, DEP STA, ARV STA, Block Time, Duty, GND Time, ODL, Hotel, Time Stamp
        FLT,4/14/2020,14:49,4/14/2020,16:45,31283,1,(D) 2700,MIA,CLT,1:56,2:48,,13:15,,4/15/2020 3:53:19 PM Central Time 
        DFP,04/03/2020,00:00,04/03/2020,23:59,,,,,,,,,,,4/15/2020 3:53:19 PM Central Time 
    */

    /// <summary>
    /// Encapsulates AA schedules
    /// </summary>
    public class AAScheduler : ExternalFormat
    {
        #region Properties
        public DateTime ScheduledDeparture { get; set; } = DateTime.MinValue;

        public DateTime ScheduledArrival { get; set; } = DateTime.MinValue;

        public string Sequence { get; set; } = string.Empty;

        public int Leg { get; set; }

        public string FlightNumber { get; set; } = string.Empty;

        public string From { get; set; } = string.Empty;

        public string To { get; set; } = string.Empty;

        public decimal BlockTime { get; set; }

        public decimal CrossCountry { get; set; }

        public bool Deadhead { get; set; }
        #endregion

        public AAScheduler() { }

        public override LogbookEntry ToLogbookEntry()
        {
            LogbookEntry le = new LogbookEntry()
            {
                Date = this.ScheduledDeparture.Date,
                Route = JoinStrings(new string[] { From, To }),
                TotalFlightTime = Deadhead ? 0 : BlockTime,
                CrossCountry = Deadhead ? 0 : BlockTime,
                Landings = 1,
                FullStopLandings = 1,
            };

            le.CustomProperties.SetItems(new CustomFlightProperty[]
            {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropScheduledDeparture, ScheduledDeparture, false),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropScheduledArrival, ScheduledArrival, false),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightNumber, FlightNumber),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropDeadhead, Deadhead),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropSequence, Sequence)
            });

            return le;
        }

    }

    public class AASchedulerImporter : ExternalFormatImporter
    {
        private MatchCollection Matches;

        public override string Name { get { return "AA Scheduler"; } }

        private readonly static Regex regAA = new Regex("^FLT,(?<startdate>[0-9/-]{8,10}),(?<starttime>\\d{1,2}:\\d{2}),(?<enddate>[0-9/-]{8,10}),(?<endtime>\\d{1,2}:\\d{2}),(?<seq>[^,]*),(?<leg>[^,]*),(?<deadhead>\\(D\\) )?(?<flightnum>\\d+),(?<from>[a-zA-Z0-9]+),(?<to>[a-zA-Z0-9]+),(?<blocktime>[0-9:]+),.*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public override bool CanParse(byte[] rgb)
        {
            if (rgb == null || rgb.Length == 0)
                return false;

            string sz = Encoding.UTF8.GetString(rgb);

            string szFirstLine = null;
            using (System.IO.StringReader sr = new System.IO.StringReader(sz))
            {
                szFirstLine = sr.ReadLine();
            }

            if (szFirstLine == null || !szFirstLine.Replace(" ", string.Empty).StartsWith("Activity,StartDate,StartTime,EndDate,EndTime", StringComparison.CurrentCultureIgnoreCase))
                return false;

            Matches = regAA.Matches(sz);

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

            // Build up the list of CrewTrac objects first; we'll then fill in cross-country time.
            List<AAScheduler> lstAA = new List<AAScheduler>();
            foreach (Match aaMatch in Matches)
            {
                GroupCollection gc = aaMatch.Groups;

                try
                {
                    DateTime dtDep = Convert.ToDateTime(String.Format(CultureInfo.CurrentCulture, "{0} {1}", gc["startdate"].Value, gc["starttime"].Value), CultureInfo.CurrentCulture);
                    DateTime dtArr = Convert.ToDateTime(String.Format(CultureInfo.CurrentCulture, "{0} {1}", gc["enddate"].Value, gc["endtime"].Value), CultureInfo.CurrentCulture);
                    AAScheduler aas = new AAScheduler()
                    {
                        ScheduledDeparture = dtDep,
                        ScheduledArrival = dtArr,
                        BlockTime = gc["blocktime"].Value.DecimalFromHHMM(),
                        FlightNumber = gc["flightnum"].Value,
                        Deadhead = !String.IsNullOrEmpty(gc["deadhead"].Value),
                        From = gc["from"].Value,
                        To = gc["to"].Value,
                        Sequence = gc["seq"].Value,
                        Leg = String.IsNullOrEmpty(gc["leg"].Value) ? 0 : Convert.ToInt32(gc["leg"].Value, CultureInfo.CurrentCulture)
                    };
                    lstAA.Add(aas);
                }
                catch (Exception ex) when (ex is FormatException) { }

                // Build up a list of airports so that we can determine cross-country time.
                StringBuilder sbRoutes = new StringBuilder();
                foreach (AAScheduler aas in lstAA)
                    sbRoutes.AppendFormat(CultureInfo.CurrentCulture, " {0} {1}", aas.From, aas.To);
                AirportList alRoutes = new AirportList(sbRoutes.ToString());

                foreach (AAScheduler aas in lstAA)
                {
                    AirportList alFlight = alRoutes.CloneSubset(aas.From + " " + aas.To);
                    if (alFlight.MaxDistanceForRoute() > 50.0)
                        aas.CrossCountry = aas.BlockTime;
                }
            }

            using (DataTable dtDst = new DataTable())
            {
                dtDst.Locale = CultureInfo.CurrentCulture;
                CSVImporter.InitializeDataTable(dtDst);
                foreach (AAScheduler aa in lstAA)
                    CSVImporter.WriteEntryToDataTable(aa.ToLogbookEntry(), dtDst);
                return CsvWriter.WriteToString(dtDst, true, true);
            }
        }

        public override bool IsPendingOnly => true;

        public override IEnumerable<ExternalFormat> FromDataTable(DataTable dt)
        {
            throw new NotImplementedException();
        }
    }
    }