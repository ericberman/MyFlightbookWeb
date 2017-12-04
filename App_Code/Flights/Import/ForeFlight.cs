    using System;
using MyFlightbook.CSV;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
using JouniHeikniemi.Tools.Text;

/******************************************************
 * 
 * Copyright (c) 2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    public class ForeFlightAircraftDescriptor
    {
        public string AircraftID { get; set; }
        public string TypeCode { get; set; }
        public string Model { get; set; }
    }
    
    /// <summary>
    /// Implements external format for ForeFlight
    /// </summary>
    public class ForeFlight : ExternalFormat
    {
        private static Regex rLatLon = new Regex("(\\d{0,2}[.,]\\d*)\\D{0,2}°?([NS])/(\\d{0,3}[.,]\\d*)\\D{0,2}°?([EW])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex regApproach = new Regex("\\b(?<count>\\d{1,2});(?<desc>[-a-zA-Z/]{3,}?(?: \\(GPS\\))?(?:-[abcxyzABCXYZ])?);(?:RWY)?(?<rwy>[0-3]?\\d[LRC]?);(?<airport>[a-zA-Z0-9]{3,4})[ ;]?(?<remark>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        #region Properties
        public DateTime Date { get; set; }
        public string AircraftID { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Route { get; set; }
        public DateTime TimeOut { get; set; }
        public DateTime TimeIn { get; set; }
        public DateTime OnDuty { get; set; }
        public DateTime OffDuty { get; set; }
        public decimal TotalTime { get; set; }
        public decimal PIC { get; set; }
        public decimal SIC { get; set; }
        public decimal Night { get; set; }
        public decimal Solo { get; set; }
        public decimal CrossCountry { get; set; }
        public decimal Distance { get; set; }
        public int DayTakeoffs { get; set; }
        public int DayLandingsFullStop { get; set; }
        public int NightTakeoffs { get; set; }
        public int NightLandingsFullStop { get; set; }
        public int AllLandings { get; set; }
        public decimal ActualInstrument { get; set; }
        public decimal SimulatedInstrument { get; set; }
        public decimal HobbsStart { get; set; }
        public decimal HobbsEnd { get; set; }
        public decimal TachStart { get; set; }
        public decimal TachEnd { get; set; }
        public int Holds { get; set; }
        public string Approach1 { get; set; }
        public string Approach2 { get; set; }
        public string Approach3 { get; set; }
        public string Approach4 { get; set; }
        public string Approach5 { get; set; }
        public string Approach6 { get; set; }
        public decimal DualGiven { get; set; }
        public decimal DualReceived { get; set; }
        public decimal SimulatedFlight { get; set; }
        public decimal GroundTraining { get; set; }
        public string InstructorName { get; set; }
        public string InstructorComments { get; set; }
        public string Person1 { get; set; }
        public string Person2 { get; set; }
        public string Person3 { get; set; }
        public string Person4 { get; set; }
        public string Person5 { get; set; }
        public string Person6 { get; set; }
        public bool FlightReview { get; set; }
        public bool Checkride { get; set; }
        public bool IPC { get; set; }
        public string PilotComments { get; set; }
        public string Text { get; set; }
        public string TypeCode { get; set; }
        public string Model { get; set; }
        #endregion

        public ForeFlight(DataRow dr, IDictionary<string, ForeFlightAircraftDescriptor> dict, IEnumerable<CustomPropertyType> lstProps = null) : base(dr, lstProps)
        {
            if (dict != null && !String.IsNullOrWhiteSpace(AircraftID) && dict.ContainsKey(AircraftID))
            {
                Model = dict[AircraftID].Model;
                TypeCode = dict[AircraftID].TypeCode;
            }
        }
        
        public override LogbookEntry ToLogbookEntry()
        {
            OnDuty = FixedUTCDateFromTime(Date, OnDuty);
            OffDuty = FixedUTCDateFromTime(Date, OffDuty, OnDuty);

            TimeOut = FixedUTCDateFromTime(Date, TimeOut);
            TimeIn = FixedUTCDateFromTime(Date, TimeIn, TimeOut);

            List<string> lstApproachStrings = new List<string>() { Approach1, Approach2, Approach3, Approach4, Approach5, Approach6 };
            StringBuilder sbApproaches = new StringBuilder();
            int cApproaches = 0;
            foreach (string szApproach in lstApproachStrings)
            {
                if (!String.IsNullOrWhiteSpace(szApproach))
                {
                    string szFixedApproach = Regex.Replace(szApproach, " ?\\(GPS\\)", "/GPS", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
                    try
                    {
                        MatchCollection mc = regApproach.Matches(szFixedApproach);
                        foreach (Match m in mc)
                        {
                            ApproachDescription ad = new ApproachDescription(m);
                            sbApproaches.Append(ad.ToCanonicalString() + Resources.LocalizedText.LocalizedSpace + m.Groups["remark"].Value + Resources.LocalizedText.LocalizedSpace);
                            cApproaches += ad.Count;
                        }
                        
                    }
                    catch (FormatException) { }
                }
            }

            if (AircraftID == null)
                AircraftID = string.Empty;

            if (AircraftID.StartsWith(CountryCodePrefix.szSimPrefix, StringComparison.CurrentCultureIgnoreCase))
                AircraftID = CountryCodePrefix.szSimPrefix;

            string szModel = Regex.Replace(((String.IsNullOrEmpty(Model) ? TypeCode : Model) ?? string.Empty), " *[([]?SIM[)\\]]?$", String.Empty, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

            LogbookEntry le = new LogbookEntry()
            {
                Date = this.Date,
                ModelDisplay = szModel,
                TailNumDisplay = String.IsNullOrWhiteSpace(AircraftID) ? (SimulatedFlight > 0 ? CountryCodePrefix.SimCountry.Prefix : CountryCodePrefix.AnonymousCountry.Prefix) : AircraftID,
                Route = rLatLon.Replace(JoinStrings(new string[] { From, Route, To }), (m) => { return "@" + m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value + m.Groups[4].Value; }),
                EngineStart = TimeOut,
                EngineEnd = TimeIn,
                TotalFlightTime = TotalTime,
                PIC = this.PIC,
                SIC = this.SIC,
                Nighttime = Night,
                CrossCountry = CrossCountry,
                IMC = ActualInstrument,
                SimulatedIFR = SimulatedInstrument,
                CFI = DualGiven,
                Dual = DualReceived,
                GroundSim = SimulatedFlight,

                FullStopLandings = DayLandingsFullStop,
                NightLandings = NightLandingsFullStop,
                Landings = AllLandings,
                HobbsStart = HobbsStart,
                HobbsEnd = HobbsEnd,
                Approaches = cApproaches,
                fHoldingProcedures = (Holds > 0),
                CFIName = InstructorName,
                Comment = JoinStrings(new string[] { PilotComments, Text, sbApproaches.ToString().Trim(), Person1, Person2, Person3, Person4, Person5, Person6 }),

                CustomProperties = PropertiesWithoutNullOrDefault(new CustomFlightProperty[]
                    {
                        PropertyWithValue(CustomPropertyType.KnownProperties.IDPropDutyTimeStart, OnDuty, true),
                        PropertyWithValue(CustomPropertyType.KnownProperties.IDPropDutyTimeEnd, OffDuty, true),
                        PropertyWithValue(CustomPropertyType.KnownProperties.IDPropSolo, Solo),
                        PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNightTakeoff, NightTakeoffs),
                        PropertyWithValue(CustomPropertyType.KnownProperties.IDPropTachStart, TachStart),
                        PropertyWithValue(CustomPropertyType.KnownProperties.IDPropTachEnd, TachEnd),
                        PropertyWithValue(CustomPropertyType.KnownProperties.IDPropInstructorName, InstructorName),
                        PropertyWithValue(CustomPropertyType.KnownProperties.IDPropGroundInstructionReceived, GroundTraining),
                        PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightReview, FlightReview),
                        PropertyWithValue(CustomPropertyType.KnownProperties.IDPropIPC, IPC),
                        PropertyWithValue(CustomPropertyType.KnownProperties.IDPropCheckRide, Checkride)
                    }).ToArray()
            };
            return le;
        }
    }

    public class ForeFlightImporter : ExternalFormatImporter
    {
        private Dictionary<string, ForeFlightAircraftDescriptor> dictAircraft = new Dictionary<string, ForeFlightAircraftDescriptor>();

        public override string Name { get { return "ForeFlight"; } }

        public override bool CanParse(byte[] rgb)
        {
            MemoryStream ms = new MemoryStream(rgb);
            try
            {
                using (StreamReader sr = new StreamReader(ms))
                {
                    ms = null;
                    string sz = sr.ReadLine();
                    return sz.StartsWith("ForeFlight", StringComparison.CurrentCultureIgnoreCase);
                }
            }
            finally
            {
                if (ms != null)
                    ms.Dispose();
            }
        }

        public override byte[] PreProcess(byte[] rgb)
        {
            if (rgb == null)
                throw new ArgumentNullException("rgb");
            base.PreProcess(rgb);
            MemoryStream ms = new MemoryStream(rgb);
            try
            {
                int iColAircraft = -1;
                int iColTypeCode = -1;
                int iColModel = -1;
                dictAircraft.Clear();
                using (CSVReader reader = new CSVReader(ms))
                {
                    ms = null;  // for CA2202

                    // Find the start of the aircraft table
                    string[] rgRow = null;
                    while ((rgRow = reader.GetCSVLine()) != null)
                    {
                        if (rgRow != null && rgRow.Length > 0 && rgRow[0].CompareCurrentCultureIgnoreCase("Aircraft Table") == 0)
                            break;
                    }

                    string[] rgHeaders = reader.GetCSVLine();
                    int cColumnHeader = int.MaxValue;
                    if (rgHeaders != null)
                    {
                        cColumnHeader = rgHeaders.Length;
                        for (int i = 0; i < rgHeaders.Length; i++)
                        {
                            if (rgHeaders[i].CompareCurrentCultureIgnoreCase("AircraftID") == 0)
                                iColAircraft = i;
                            else if (rgHeaders[i].CompareCurrentCultureIgnoreCase("TypeCode") == 0)
                                iColTypeCode= i;
                            else if (rgHeaders[i].CompareCurrentCultureIgnoreCase("Model") == 0)
                                iColModel = i;
                        }
                    }

                    while ((rgRow = reader.GetCSVLine()) != null)
                    {
                        if (rgRow.Length == 0)
                            break;

                        string szAircraft = rgRow[iColAircraft];
                        if (String.IsNullOrWhiteSpace(szAircraft))
                            break;
                        ForeFlightAircraftDescriptor ad = new ForeFlightAircraftDescriptor() { AircraftID = szAircraft, Model = rgRow[iColModel], TypeCode = rgRow[iColTypeCode] };
                        dictAircraft[ad.AircraftID] = ad;
                    }

                    // Now find the start of the flight table and stop - the regular CSVAnalyzer can pick up from here, and our aircraft table is now set up.
                    while ((rgRow = reader.GetCSVLine()) != null)
                    {
                        if (rgRow != null && rgRow[0].CompareCurrentCultureIgnoreCase("Flights Table") == 0)
                            break;
                    }

                    rgHeaders = reader.GetCSVLine();

                    if (rgHeaders != null)
                    {
                        // Build a *new* byte array from here
                        using (DataTable dt = new DataTable())
                        {
                            dt.Locale = CultureInfo.CurrentCulture;
                            foreach (string header in rgHeaders)
                                dt.Columns.Add(new DataColumn(header.Trim(), typeof(string)));

                            while ((rgRow = reader.GetCSVLine()) != null)
                            {
                                DataRow dr = dt.NewRow();
                                for (int i = 0; i < rgRow.Length && i < rgHeaders.Length; i++)
                                    dr[i] = rgRow[i];
                                dt.Rows.Add(dr);
                            }

                            return Encoding.UTF8.GetBytes(CsvWriter.WriteToString(dt, true, true));
                        }
                    }
                }
            }
            finally
            {
                if (ms != null)
                    ms.Dispose();
            }
            return rgb;
        }

        public override string CSVFromDataTable(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException("dt");
            IEnumerable<CustomPropertyType> rgcpt = CustomPropertyType.GetCustomPropertyTypes();
            using (DataTable dtDst = new DataTable())
            {
                dtDst.Locale = dt.Locale;
                CSVImporter.InitializeDataTable(dtDst);
                foreach (DataRow dr in dt.Rows)
                {
                    ForeFlight ltp = new ForeFlight(dr, dictAircraft, rgcpt);
                    CSVImporter.WriteEntryToDataTable(ltp.ToLogbookEntry(), dtDst);
                }
                return CsvWriter.WriteToString(dtDst, true, true);
            }
        }

        public override IEnumerable<ExternalFormat> FromDataTable(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException("dt");
            List<ForeFlight> lst = new List<ForeFlight>();
            foreach (DataRow dr in dt.Rows)
                lst.Add(new ForeFlight(dr, dictAircraft));
            return lst;
        }
    }
}