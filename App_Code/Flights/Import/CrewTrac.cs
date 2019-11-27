using MyFlightbook.Airports;
using MyFlightbook.CSV;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2018-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{

    /* 
     * Below is a sample CrewTrac cut/paste.  it's text, not CSV.  And the headers span multiple lines.
     
        Date: 03/16/18                                                    Virgin America                                                   Sabre CrewTrac 
        Time: 09:33                                                     Flight Log Report                                                         Page 1
                                                                              MAR18
            13846   FO   [Pilot name]               

                                 BLOCK  A/C                  EMP                                EMP                               EMP
        FLT     DATE    ORG DES  HOURS  NUM EQPT LNDG  POS   NO   EMP NAME               POS    NO  EMP NAME               POS    NO  EMP NAME

        1027  03/06/18  JFK SFO   6:22  842  320   1   CA     1   [Name]                                                                      
        1964  03/06/18  SFO SAN   1:30  528  319       CA     15  [Name]                                                                      
        ...
        1865  03/14/18  DEN SFO   2:51  628  320   1   CA   2430  [Name]
                         37:19             9                                                           

        The trouble with this is in the EQPT and A/C NUM fields: specifically, that the EQPT field doesn't really map to an ICAO model ("320" is way too ambiguous to map to an A-320), 
        and the "A/C NUM" is a shorthand for a fleet tail number (in this case, NxxxVA - e.g., N842VA, N528VA, etc.)

        So we're going to do a hack: look at the private notes for #ALTxxx#.  E.g., #ALT842# in the private notes for an alternate tail number of "842"
    */

    public class CrewTracEmployee
    {
        public enum CrewTracRole { CA, FO };
        #region Properties
        public string Name { get; set; }
        public CrewTracRole Role { get; set; }
        public int Number { get; set; }
        #endregion

        public CrewTracEmployee()
        {
            Name = string.Empty;
        }
    }

    /// <summary>
    /// Encapsulates CrewTrac scheduling
    /// </summary>
    public class CrewTrac : ExternalFormat
    {
        #region Properties
        public int FlightNumber { get; set; }
        public DateTime Date { get; set; }
        public string AircraftNickName { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public decimal BlockHours { get; set; }
        public decimal CrossCountry { get; set; }
        public string Tail { get; set; }
        public string Model { get; set; }
        public int Landings { get; set; }
        public IEnumerable<CrewTracEmployee> Employees { get; set; }
        #endregion

        public CrewTrac()
        {
            AircraftNickName = From = To = Tail = Model = string.Empty;
            Employees = new CrewTracEmployee[0];
        }

        private CrewTracEmployee FirstOfficer
        {
            get
            {
                return (Employees == null) ? null : Employees.FirstOrDefault(cte => cte.Role == CrewTracEmployee.CrewTracRole.FO);
            }
        }

        private CrewTracEmployee Captain
        {
            get
            {
                return (Employees == null) ? null : Employees.FirstOrDefault(cte => cte.Role == CrewTracEmployee.CrewTracRole.CA);
            }
        }

        public override LogbookEntry ToLogbookEntry()
        {
            LogbookEntry le = new LogbookEntry()
            {
                Date = this.Date,
                Route = JoinStrings(new string[] { From, To }),
                TotalFlightTime = BlockHours,
                TailNumDisplay = Tail,
                ModelDisplay = Model,
                Landings = this.Landings,
                CrossCountry = this.CrossCountry
            };

            CrewTracEmployee PIC = Captain;
            CrewTracEmployee SIC = FirstOfficer;

            le.CustomProperties.SetItems(new CustomFlightProperty[] {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightNumber, FlightNumber),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNameOfPIC, PIC == null ? string.Empty : PIC.Name),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNameOfSIC, SIC == null ? string.Empty : SIC.Name)
                });

            return le;
        }
    }

    public class CrewTracImporter : ExternalFormatImporter
    {
        private MatchCollection Matches = null;

        public override string Name { get { return "CrewTrac"; } }

        private readonly static Regex regCrewTrac = new Regex("(?<flightnum>\\d{1,4})\\s+(?<date>[0-9/-]{8,10})\\s+(?<from>[a-zA-Z0-9]{3,4})\\s+(?<to>[a-zA-Z0-9]{3,4})\\s+(?<block>\\d{1,2}:\\d{2})\\s+(?<tail>[0-9a-zA-Z]+)\\s+(?<model>[a-zA-Z0-9]+)\\s+(?<landings>\\d*)\\s+(?<employees>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private readonly static Regex regCrewMember = new Regex("((?<pos>(?:CA|FO))\\s+\\d*\\s+(?<empname>[^\\t]+))+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override bool CanParse(byte[] rgb)
        {
            if (rgb == null || rgb.Length == 0)
                return false;

            string sz = System.Text.Encoding.UTF8.GetString(rgb);

            Matches = regCrewTrac.Matches(sz);

            return Matches.Count > 0;
        }

        public override byte[] PreProcess(byte[] rgb)
        {
            if (rgb == null || Matches == null)
                return null;

            return System.Text.Encoding.UTF8.GetBytes(CSVFromDataTable(null));
        }

        public override string CSVFromDataTable(DataTable dt)
        {
            // We ignore the data table passed in - we have our data from Matches, which was initialized in CanParse.

            IEnumerable<CustomPropertyType> rgcpt = CustomPropertyType.GetCustomPropertyTypes();

            // Build up the list of CrewTrac objects first; we'll then fill in cross-country time.
            List<CrewTrac> lstCt = new List<CrewTrac>();
            foreach (Match ctMatch in Matches)
            {
                GroupCollection gc = ctMatch.Groups;

                string szEmployees = gc["employees"].Value;
                // replace runs of 2 or more spaces with a tab for better finding boundaries (e.g., multiple employees).
                while (szEmployees.Contains("  "))
                    szEmployees = szEmployees.Replace("  ", "\t");

                MatchCollection mcEmployees = regCrewMember.Matches(szEmployees);

                List<CrewTracEmployee> lst = new List<CrewTracEmployee>();
                foreach (Match empMatch in mcEmployees)
                {
                    GroupCollection gcEmployee = empMatch.Groups;
                    CrewTracEmployee.CrewTracRole role = CrewTracEmployee.CrewTracRole.CA;
                    if (!Enum.TryParse<CrewTracEmployee.CrewTracRole>(gcEmployee["pos"].Value, out role))
                        role = CrewTracEmployee.CrewTracRole.CA;
                    lst.Add(new CrewTracEmployee() { Role = role, Name = gcEmployee["empname"].Value });
                }

                try
                {
                    CrewTrac ct = new CrewTrac()
                    {
                        FlightNumber = String.IsNullOrEmpty(gc["flightnum"].Value) ? 0 : Convert.ToInt32(gc["flightnum"].Value, CultureInfo.CurrentCulture),
                        Date = Convert.ToDateTime(gc["date"].Value, CultureInfo.CurrentCulture),
                        From = gc["from"].Value,
                        To = gc["to"].Value,
                        BlockHours = gc["block"].Value.DecimalFromHHMM(),
                        Tail = gc["tail"].Value,
                        Model = gc["model"].Value,
                        Landings = String.IsNullOrEmpty(gc["landings"].Value) ? 0 : Convert.ToInt32(gc["landings"].Value, CultureInfo.CurrentCulture),
                        Employees = lst
                    };
                    lstCt.Add(ct);

                }
                catch (FormatException) { }
            }

            // Build up a list of airports so that we can determine cross-country time.
            StringBuilder sbRoutes = new StringBuilder();
            foreach (CrewTrac ct in lstCt)
                sbRoutes.AppendFormat(CultureInfo.CurrentCulture, " {0} {1}", ct.From, ct.To);
            AirportList alRoutes = new AirportList(sbRoutes.ToString());

            foreach (CrewTrac ct in lstCt)
            {
                AirportList alFlight = alRoutes.CloneSubset(ct.From + " " + ct.To);
                if (alFlight.MaxDistanceForRoute() > 50.0)
                    ct.CrossCountry = ct.BlockHours;
            }

            using (DataTable dtDst = new DataTable())
            {
                dtDst.Locale = CultureInfo.CurrentCulture;
                CSVImporter.InitializeDataTable(dtDst);
                foreach (CrewTrac ct in lstCt)
                    CSVImporter.WriteEntryToDataTable(ct.ToLogbookEntry(), dtDst);
                return CsvWriter.WriteToString(dtDst, true, true);
            }
        }

        public override IEnumerable<ExternalFormat> FromDataTable(DataTable dt)
        {
            throw new NotImplementedException();
        }
    }
}
 