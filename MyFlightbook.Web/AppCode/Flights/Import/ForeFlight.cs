using JouniHeikniemi.Tools.Text;
using MyFlightbook.CSV;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2017-2024 MyFlightbook LLC
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
        #region Properties
        public DateTime Date { get; set; }
        public string AircraftID { get; set; }
        public string TailNum { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Route { get; set; }
        public DateTime TimeOut { get; set; }
        public DateTime TimeOff { get; set; }
        public DateTime TimeOn { get; set; }
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
        protected List<string> PassengerNames { get; private set; }
        protected List<string> StudentNames { get; private set; }
        protected List<string> InstructorNames { get; private set; }
        protected List<string> ExaminerNames { get; private set; }
        #endregion

        public ForeFlight(DataRow dr, IDictionary<string, ForeFlightAircraftDescriptor> dict) : base(dr)
        {
            if (dict != null && !String.IsNullOrWhiteSpace(AircraftID) && dict.ContainsKey(AircraftID))
            {
                Model = dict[AircraftID].Model;
                TypeCode = dict[AircraftID].TypeCode;
            }
        }

        protected string AddToRole(string szPerson)
        {
            if (szPerson == null)
                return string.Empty;

            List<string> lstFields = new List<string>(szPerson.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            if (lstFields.Count >= 2)
            {
                string szRole = lstFields[1].ToLower(CultureInfo.CurrentCulture);
                List<string> lstResult = null;
                switch (szRole)
                {
                    case "student":
                        lstResult = StudentNames;
                        break;
                    case "instructor":
                        lstResult = InstructorNames;
                        break;
                    case "passenger":
                        lstResult = PassengerNames;
                        break;
                    case "examiner":
                        lstResult = ExaminerNames;
                        break;
                }
                if (lstResult != null)
                {
                    lstFields.RemoveAt(1);
                    lstResult.Add(JoinStrings(lstFields));
                    return string.Empty;
                }
            }

            return szPerson;
        }

        private readonly static LazyRegex rApproachFixed = new LazyRegex(" ?\\(GPS\\)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private readonly static LazyRegex rModelFixed = new LazyRegex(" *[([]?SIM[)\\]]?$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        public override LogbookEntry ToLogbookEntry()
        {
            OnDuty = FixedUTCDateFromTime(Date, OnDuty);
            OffDuty = FixedUTCDateFromTime(Date, OffDuty, OnDuty);

            TimeOut = FixedUTCDateFromTime(Date, TimeOut);
            TimeIn = FixedUTCDateFromTime(Date, TimeIn, TimeOut);
            TimeOff = FixedUTCDateFromTime(Date, TimeOff);
            TimeOn = FixedUTCDateFromTime(Date, TimeOn);

            List<string> lstApproachStrings = new List<string>() { Approach1, Approach2, Approach3, Approach4, Approach5, Approach6 };
            StringBuilder sbApproaches = new StringBuilder();
            int cApproaches = 0;
            foreach (string szApproach in lstApproachStrings)
            {
                if (!String.IsNullOrWhiteSpace(szApproach))
                {
                    string szFixedApproach = rApproachFixed.Expr.Replace(szApproach, "/GPS");
                    try
                    {
                        MatchCollection mc = RegexUtility.ApproachDescriptionForeflight.Matches(szFixedApproach);
                        if (mc.Count == 0)
                        {
                            // handle the case of "2;;;;" (i.e., 2 approaches, no additional details
                            mc = RegexUtility.ApproachCountForeFlight.Matches(szFixedApproach);
                            if (mc.Count == 1)
                                cApproaches += Convert.ToInt32(mc[0].Groups["count"].Value, CultureInfo.CurrentCulture);
                        }
                        else
                        {
                            foreach (Match m in mc)
                            {
                                ApproachDescription ad = new ApproachDescription(m);
                                sbApproaches.Append(ad.ToCanonicalString() + Resources.LocalizedText.LocalizedSpace + m.Groups["remark"].Value.Replace(";", " ").Trim() + Resources.LocalizedText.LocalizedSpace);
                                cApproaches += ad.Count;
                            }
                        }
                    }
                    catch (Exception ex) when (ex is FormatException) { }
                }
            }

            StudentNames = new List<string>();
            InstructorNames = new List<string>();
            ExaminerNames = new List<string>();
            PassengerNames = new List<string>();

            // try to parse people's role on the flight
            Person1 = AddToRole(Person1);
            Person2 = AddToRole(Person2);
            Person3 = AddToRole(Person3);
            Person4 = AddToRole(Person4);
            Person5 = AddToRole(Person5);
            Person6 = AddToRole(Person6);
            if (!String.IsNullOrEmpty(InstructorName))
                InstructorNames.Add(InstructorName);
            InstructorName = JoinStrings(InstructorNames);
            
            if (AircraftID == null)
                AircraftID = TailNum ?? string.Empty;

            if (AircraftID.StartsWith(CountryCodePrefix.szSimPrefix, StringComparison.CurrentCultureIgnoreCase))
                AircraftID = CountryCodePrefix.szSimPrefix;

            string szModel = rModelFixed.Expr.Replace(((String.IsNullOrEmpty(TypeCode) ? Model : TypeCode) ?? string.Empty), string.Empty);

            LogbookEntry le = new LogbookEntry()
            {
                Date = this.Date,
                ModelDisplay = szModel,
                TailNumDisplay = String.IsNullOrWhiteSpace(AircraftID) ? (SimulatedFlight > 0 ? CountryCodePrefix.SimCountry.Prefix : CountryCodePrefix.AnonymousCountry.Prefix) : AircraftID,
                Route = RegexUtility.DMSDegreesSlashed.Replace(JoinStrings(new string[] { From, Route, To }), (m) => { return "@" + m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value + m.Groups[4].Value; }),
                FlightStart = TimeOff,
                FlightEnd = TimeOn,
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
            };

            le.CustomProperties.SetItems(new CustomFlightProperty[]
            {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightDutyTimeStart, OnDuty, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightDutyTimeEnd, OffDuty, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockOut, TimeOut, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockIn, TimeIn, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropSolo, Solo),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNightTakeoff, NightTakeoffs),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropTachStart, TachStart),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropTachEnd, TachEnd),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropInstructorName, InstructorName),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPassengerNames, JoinStrings(PassengerNames)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropStudentName, JoinStrings(StudentNames)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNameOfExaminer, JoinStrings(ExaminerNames)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropGroundInstructionGiven, DualGiven > 0 ? GroundTraining : 0),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropGroundInstructionReceived, DualReceived > 0 ? GroundTraining : 0),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightReview, FlightReview),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropIPC, IPC),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropCheckRide, Checkride)
            });

            return le;
        }
    }

    public class ForeFlightImporter : ExternalFormatImporter
    {
        private readonly static LazyRegex rDataTypes = new LazyRegex("^(Text|hhmm|Decimal|Boolean)$", true);
        private readonly Dictionary<string, ForeFlightAircraftDescriptor> dictAircraft = new Dictionary<string, ForeFlightAircraftDescriptor>();

        public override string Name { get { return "ForeFlight"; } }

        public override bool CanParse(byte[] rgb)
        {
            using (MemoryStream ms = new MemoryStream(rgb))
            {
                using (StreamReader sr = new StreamReader(ms))
                {
                    string sz = sr.ReadLine();
                    return sz != null && sz.StartsWith("ForeFlight", StringComparison.CurrentCultureIgnoreCase);
                }
            }
        }

        private void ReadAircraftTable(CSVReader reader)
        {
            int iColAircraft = -1;
            int iColTypeCode = -1;
            int iColModel = -1;

            // Find the start of the aircraft table
            string[] rgHeaders;
            while ((rgHeaders = reader.GetCSVLine()) != null)
            {
                if (rgHeaders != null && rgHeaders.Length > 0 && rgHeaders[0].CompareCurrentCultureIgnoreCase("Aircraft Table") == 0)
                    break;
            }

            // Now find the next line that isn't generic data types
            // Look for "Text", "hhmm", "Decimal", "Boolean" - if any of these, skip ahead.
            while ((rgHeaders = reader.GetCSVLine()) != null)
            {
                if (Array.Find(rgHeaders, sz => rDataTypes.IsMatch(sz)) == null)
                    break;
            }

            if (rgHeaders != null)
            {
                for (int i = 0; i < rgHeaders.Length; i++)
                {
                    if (rgHeaders[i].CompareCurrentCultureIgnoreCase("AircraftID") == 0)
                        iColAircraft = i;
                    else if (rgHeaders[i].CompareCurrentCultureIgnoreCase("TypeCode") == 0)
                        iColTypeCode = i;
                    else if (rgHeaders[i].CompareCurrentCultureIgnoreCase("Model") == 0)
                        iColModel = i;
                }
            }

            string[] rgRow;

            while ((rgRow = reader.GetCSVLine()) != null)
            {
                if (rgRow.Length == 0)
                    break;

                if (iColAircraft < 0 || iColAircraft >= rgRow.Length)
                    break;

                string szAircraft = rgRow[iColAircraft];
                if (String.IsNullOrWhiteSpace(szAircraft))
                    break;

                string szModel = (iColModel >= 0) ? rgRow[iColModel] : string.Empty;
                string szTypeCode = (iColTypeCode >= 0) ? rgRow[iColTypeCode] : string.Empty;
                ForeFlightAircraftDescriptor ad = new ForeFlightAircraftDescriptor() { AircraftID = szAircraft, Model = szModel, TypeCode = szTypeCode };
                dictAircraft[ad.AircraftID] = ad;
            }
        }

        public override byte[] PreProcess(byte[] rgb)
        {
            if (rgb == null)
                throw new ArgumentNullException(nameof(rgb));
            base.PreProcess(rgb);
            using (MemoryStream ms = new MemoryStream(rgb))
            {
                dictAircraft.Clear();
                using (CSVReader reader = new CSVReader(ms))
                {
                    ReadAircraftTable(reader);

                    string[] rgRow = null;

                    string[] rgHeaders;
                    // Now find the start of the flight table and stop - the regular CSVAnalyzer can pick up from here, and our aircraft table is now set up.
                    while ((rgHeaders = reader.GetCSVLine()) != null)
                    {
                        if (rgHeaders != null && rgHeaders.Length > 0 && rgHeaders[0].CompareCurrentCultureIgnoreCase("Flights Table") == 0)
                            break;
                    }


                    // Now find the next line that isn't generic data types
                    // Look for "Text", "hhmm", "Decimal", "Boolean" - if any of these, skip ahead.
                    while ((rgHeaders = reader.GetCSVLine()) != null)
                    {
                        if (Array.Find(rgHeaders, sz => rDataTypes.IsMatch(sz)) == null)
                            break;
                    }

                    if (rgHeaders != null)
                    {
                        // Build a *new* byte array from here
                        using (DataTable dt = new DataTable())
                        {
                            dt.Locale = CultureInfo.CurrentCulture;
                            foreach (string header in rgHeaders)
                            {
                                try
                                {
                                    dt.Columns.Add(new DataColumn(header.Trim(), typeof(string)));
                                }
                                catch (DuplicateNameException)
                                {
                                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportDuplicateColumn, header));
                                }
                            }

                            while ((rgRow = reader.GetCSVLine()) != null)
                            {
                                bool fHasData = false;
                                Array.ForEach<string>(rgRow, (sz) => { fHasData = fHasData || !String.IsNullOrWhiteSpace(sz); });
                                if (!fHasData)
                                    continue;

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
            return rgb;
        }

        public override string CSVFromDataTable(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException(nameof(dt));
            using (DataTable dtDst = new DataTable())
            {
                dtDst.Locale = dt.Locale;
                CSVImporter.InitializeDataTable(dtDst);
                foreach (DataRow dr in dt.Rows)
                {
                    ForeFlight ltp = new ForeFlight(dr, dictAircraft);
                    CSVImporter.WriteEntryToDataTable(ltp.ToLogbookEntry(), dtDst);
                }
                return CsvWriter.WriteToString(dtDst, true, true);
            }
        }
    }
}