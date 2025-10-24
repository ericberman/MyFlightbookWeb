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
 * Copyright (c) 2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    public class PilotProAircraftDescriptor
    {
        public string TailNumber { get; set; }
        public string MakeModel { get; set; }
        public bool IsActive { get; set; }
    }

    public class PilotPro : ExternalFormat
    {
        #region Properties
        public DateTime Date { get; set; }
        public string TailNumber {  set; get; } = string.Empty;
        public string Departure {  set; get; } = string.Empty;
        public string Destination { set; get; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public int DayLandings { get; set; }
        public int NightLandings { get; set; }

        /// <summary>
        /// Format is 4 digit hhmm
        /// </summary>
        public int StartHHMM { get; set; }
        /// <summary>
        /// Format is 4 digit hhmm
        /// </summary>
        public int EndHHMM { get; set; }

        /// <summary>
        /// Format appears to be Quantity-Type-Airport-Runway-Holding, sepaated by semicolons.  E.g. 1-GPS/GNSS-KPAE-16R-no;2-BC-KPAE-16R-yes;
        /// </summary>
        public string ApproachNames { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public decimal Duration { get; set; }
        public decimal PIC { get; set; }
        public decimal SIC { get; set; }
        public decimal Solo { get; set; }
        public decimal Night { get; set; }
        public decimal Instrument { get; set; }
        public decimal SimulatedInstrument { get; set; }
        public decimal DualGiven {  get; set; }
        public decimal DualReceived { get; set; }
        public decimal XC { get; set; }
        PilotProAircraftDescriptor _ppad { get; set; } = null;
        public bool Holding { get; set; }
        #endregion

        private readonly static LazyRegex rApproaches = new LazyRegex("(?<quantity>\\d+)-.*?-(?<holding>yes|no)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public PilotPro(DataRow dr, IDictionary<string, PilotProAircraftDescriptor> dict) : base(dr)
        {
            if (!String.IsNullOrWhiteSpace(TailNumber) && (dict?.TryGetValue(TailNumber, out PilotProAircraftDescriptor ppad) ?? false))
                _ppad = ppad;
        }

        public override LogbookEntry ToLogbookEntry()
        {
            MatchCollection mcApproaches = rApproaches.Matches(ApproachNames);
            int cApproaches = 0;
            bool fHold = false;
            foreach (Match m in mcApproaches)
            {
                cApproaches += Convert.ToInt32(m.Groups["quantity"].Value, CultureInfo.InvariantCulture);
                fHold = fHold || m.Groups["holding"].Value.CompareCurrentCultureIgnoreCase("yes") == 0;
            }

            LogbookEntry entry = new LogbookEntry()
            {
                Date = Date,
                Comment = Remarks,
                Landings = DayLandings + NightLandings,
                NightLandings = NightLandings,
                ModelDisplay = _ppad?.MakeModel ?? string.Empty,
                TailNumDisplay = _ppad?.TailNumber ?? string.Empty,
                Route = JoinStrings(new string[] { Departure, Route, Destination }),
                Approaches = cApproaches,
                fHoldingProcedures = fHold,
                FlightStart = (StartHHMM > 0 && StartHHMM < 2400) ? new DateTime(Date.Year, Date.Month, Date.Day, StartHHMM / 100, StartHHMM % 100, 0, 0, DateTimeKind.Utc) : DateTime.MinValue,
                FlightEnd = (EndHHMM > 0 && EndHHMM < 2400) ? new DateTime(Date.Year, Date.Month, Date.Day, EndHHMM / 100, EndHHMM % 100, 0, 0, DateTimeKind.Utc) : DateTime.MinValue,
                IMC = Instrument,
                SimulatedIFR = SimulatedInstrument,
                Dual = DualReceived,
                Nighttime = Night,
                CrossCountry = XC,
                CFI = DualGiven,
                SIC = SIC,
                PIC = PIC,
                TotalFlightTime = Duration,
            };

            entry.CustomProperties.SetItems(new CustomFlightProperty[]
            {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropSolo, Solo),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropApproachName, ApproachNames),
            });

            return entry;
        }
    }

    public class PilotProImporter : ExternalFormatImporter
    {
        public override string Name => "Pilot Pro";
        private readonly Dictionary<string, PilotProAircraftDescriptor> dictAircraft = new Dictionary<string, PilotProAircraftDescriptor>();
        private static Dictionary<string, string> _dictPropMap = null;

        private static Dictionary<string, string> HeaderToPropMap
        {
            get
            {
                if (_dictPropMap == null)
                    _dictPropMap = new Dictionary<string, string>()
                    {
                        { "Flight Date (MM/DD/YYYY)", "Date" },
                        { "Aircraft Registration", "TailNumber" },
                        { "Day Landings", "DayLandings" },
                        { "Night Landings", "NightLandings" },
                        { "Flight Start Time", "StartHHMM" },
                        { "Flight Stop Time", "EndHHMM" },
                        { "Approaches (Quantity-Type-Airport-Runway-Holding)", "ApproachNames" },
                        { "Simulated Instrument", "SimulatedInstrument" },
                        { "Dual Given", "DualGiven" },
                        { "Dual Received", "DualReceived" },
                        { "Cross Country", "XC" }
                    };
                return _dictPropMap;
            }
        }

        private static string MapHeader(string szHeader)
        {
            szHeader = szHeader?.Trim() ?? string.Empty;
            return HeaderToPropMap.TryGetValue(szHeader.Trim(), out string mappedHeader) ? mappedHeader : szHeader;
        }

        public override bool CanParse(byte[] rgb)
        {
            using (MemoryStream ms = new MemoryStream(rgb))
            {
                using (StreamReader sr = new StreamReader(ms))
                {
                    string sz = sr.ReadLine();
                    return sz?.StartsWith("Pilot Pro", StringComparison.CurrentCultureIgnoreCase) ?? false;
                }
            }
        }

        private void ReadAircraftTable(CSVReader reader)
        {
            int iColAircraft = -1;
            int iColModel = -1;

            // Find the start of the aircraft table
            string[] rgHeaders;
            while ((rgHeaders = reader.GetCSVLine()) != null)
            {
                if (rgHeaders != null && rgHeaders.Length > 0 && rgHeaders[0].StartsWith("|Aircraft|", StringComparison.CurrentCultureIgnoreCase))
                    break;
            }

            // Now find the next headers for the aircraft themselves
            while ((rgHeaders = reader.GetCSVLine()) != null)
            {
                if (rgHeaders.Length > 0 && rgHeaders[0].CompareCurrentCultureIgnoreCase("Aircraft Registration") == 0)
                    break;
            }

            if (rgHeaders != null)
            {
                for (int i = 0; i < rgHeaders.Length; i++)
                {
                    if (rgHeaders[i].CompareCurrentCultureIgnoreCase("Aircraft Registration") == 0)
                        iColAircraft = i;
                    else if (rgHeaders[i].CompareCurrentCultureIgnoreCase("Make/Model") == 0)
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
                PilotProAircraftDescriptor ad = new PilotProAircraftDescriptor() { TailNumber = szAircraft, MakeModel = szModel };
                dictAircraft[ad.TailNumber] = ad;
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
                        if (rgHeaders.Length > 0 && rgHeaders[0].CompareCurrentCultureIgnoreCase("|Logbook|") == 0)
                        {
                            // pull one more row
                            rgHeaders = reader.GetCSVLine();    // "Do not reorder or delete these columns. The date, aircraft, departure, destination, and duration are required."
                            rgHeaders = reader.GetCSVLine();    // this should be the header line
                            break;
                        }
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
                                    // Map the headers so that they'll align with property names.
                                    dt.Columns.Add(new DataColumn(MapHeader(header), typeof(string)));
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
                    PilotPro pp = new PilotPro(dr, dictAircraft);
                    CSVImporter.WriteEntryToDataTable(pp.ToLogbookEntry(), dtDst);
                }
                return CsvWriter.WriteToString(dtDst, true, true);
            }
        }
    }
}