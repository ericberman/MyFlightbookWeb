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
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    /// <summary>
    /// Summary description for CrewLog
    /// </summary>
    public class CrewLogFlight : ExternalFormat
    {
        #region Properties
        public string AC { get; set; }
        public string ACTYPE { get; set; }
        public string DATEGMT { get; set; }
        public string REG { get; set; }
        public string DEPTTIME { get; set; }
        public string DEPT { get; set; }
        public string ARRV { get; set; }
        public string ARRVTIME { get; set; }
        public decimal ACTFLT { get; set; }
        public decimal NM { get; set; }
        public decimal PIC { get; set; }
        public decimal PF { get; set; }
        public decimal PNF { get; set; }
        public decimal TBT { get; set; }
        public decimal ACM { get; set; }
        public int PA { get; set; }
        public int NPA { get; set; }
        public int PMA { get; set; }
        public int CAT1WHUD { get; set; }
        public int CAT1WOHUD { get; set; }
        public int CAT2WHUD { get; set; }
        public int CAT2WOHUD { get; set; }
        public int CAT3WHUD { get; set; }
        public int CAT3WOHUD { get; set; }
        public decimal AIT { get; set; }
        public decimal SIT { get; set; }
        public decimal NITE { get; set; }
        public int DT { get; set; }
        public int NT { get; set; }
        public int DL { get; set; }
        public int NL { get; set; }
        public int HOLD { get; set; }
        public int TRCK { get; set; }
        #endregion

        public CrewLogFlight(DataRow dr) : base(dr)
        {
        }

        public override LogbookEntry ToLogbookEntry()
        {
            DateTime dt;
            DateTime blockOut = DateTime.MinValue;
            DateTime blockIn = DateTime.MinValue;
            Regex rTime = new Regex("(?<hour>\\d{2}):(?<min>\\d{2})", RegexOptions.Compiled);

            if (DateTime.TryParse(DATEGMT, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal, out dt))
            {
                MatchCollection mc = rTime.Matches(DEPTTIME);
                if (mc.Count == 1)
                    blockOut = dt.AddMinutes(Convert.ToInt32(mc[0].Groups["hour"].Value, CultureInfo.InvariantCulture) * 60 + Convert.ToInt32(mc[0].Groups["min"].Value, CultureInfo.InvariantCulture));

                mc = rTime.Matches(ARRVTIME);
                if (mc.Count == 1)
                    blockIn = dt.AddMinutes(Convert.ToInt32(mc[0].Groups["hour"].Value, CultureInfo.InvariantCulture) * 60 + Convert.ToInt32(mc[0].Groups["min"].Value, CultureInfo.InvariantCulture));
            }
            else
                dt = DateTime.MinValue;

            LogbookEntry le = new LogbookEntry()
            {
                Date = dt,
                TailNumDisplay = AC,
                ModelDisplay = ACTYPE,
                Route = String.Join(" ", new string[] { DEPT, ARRV }),
                EngineStart = blockOut,
                EngineEnd = blockIn,
                PIC = PIC,
                TotalFlightTime = TBT,
                Approaches = PA + NPA,
                IMC = AIT,
                SimulatedIFR = SIT,
                Nighttime = NITE,
                FullStopLandings = DL,
                NightLandings = NL,
                fHoldingProcedures = (HOLD != 0)
            };

            if (ACTFLT > 0 && blockOut.HasValue())
            {
                le.FlightStart = blockOut;
                le.FlightEnd = blockOut.AddHours((double) ACTFLT);
            }

            List<CustomFlightProperty> lst = new List<CustomFlightProperty>();
            switch (REG)
            {
                case "91":
                    lst.Add(CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPart91, true));
                    break;
                case "121":
                    lst.Add(CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPart121, true));
                    break;
                case "135":
                    lst.Add(CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPart135, true));
                    break;
                default:
                    break;
            }

            lst.AddRange(new CustomFlightProperty[]
            {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPilotFlyingTime, PF),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPilotMonitoringTime, PNF),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNightTakeoff, NT),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropTakeoffAny, DT)
            });

            le.CustomProperties.SetItems(lst);

            return le;
        }
    }

    public class CrewLog: ExternalFormatImporter
    {
        public override string Name { get { return "CrewLog"; } }

        public override bool CanParse(byte[] rgb)
        {
            if (rgb == null || rgb.Length == 0)
                return false;

            string sz = Encoding.UTF8.GetString(rgb);

            return sz.Contains("PIC,PF,PNF") && sz.Contains("AIT,SIT,NITE,DT,NT,DL") && sz.Contains("(CREWLOG");
        }

        private bool IsEmptyCell(string sz)
        {
            return String.IsNullOrWhiteSpace(sz) && sz.CompareCurrentCultureIgnoreCase("-") != 0;
        }

        private bool IsEmptyCSVRow(IEnumerable<string> rgsz)
        {
            foreach (string sz in rgsz)
            {
                if (!IsEmptyCell(sz))
                    return false;
            }
            return true;
        }

        public override byte[] PreProcess(byte[] rgb)
        {
            MemoryStream ms = null;

            try
            {
                ms = new MemoryStream(rgb);
                using (CSVReader reader = new CSVReader(ms))
                {
                    ms = null;  // for CA2202

                    using (DataTable dt = new DataTable())
                    {
                        dt.Locale = CultureInfo.CurrentCulture;
                        string[] rgRow;
                        Dictionary<int, string> dictHeaders = new Dictionary<int, string>();
                        bool fHeaderFound = false;

                        // Find the header row
                        while ((rgRow = reader.GetCSVLine()) != null)
                        {
                            if (IsEmptyCSVRow(rgRow) || rgRow.Length < 10)
                                continue;

                            if (fHeaderFound)
                            {
                                // data row or else we're done
                                if (rgRow[0].StartsWith("SUBTOTAL", StringComparison.CurrentCultureIgnoreCase) || rgRow[0].Contains(":"))
                                    break;

                                DataRow dr = dt.NewRow();
                                for (int iCol = 0; iCol < rgRow.Length; iCol++)
                                {
                                    string szHeader = dictHeaders[iCol];
                                    if (szHeader != null)
                                        dr[szHeader] = rgRow[iCol];
                                }
                                dt.Rows.Add(dr);
                            }
                            else if (rgRow[0].CompareCurrentCultureIgnoreCase("AC") == 0)   // we found our header row
                            {
                                fHeaderFound = true;
                                for (int iCol = 0; iCol < rgRow.Length; iCol++)
                                {
                                    string szHeader = Regex.Replace(rgRow[iCol], "\\W", string.Empty, RegexOptions.Multiline | RegexOptions.Compiled).ToUpper(CultureInfo.CurrentCulture);
                                    dictHeaders[iCol] = IsEmptyCell(szHeader) ? null : szHeader;
                                }

                                foreach (string szHeader in dictHeaders.Values)
                                    if (szHeader != null)
                                        dt.Columns.Add(new DataColumn(szHeader, typeof(string)));
                            }
                        }

                        return Encoding.UTF8.GetBytes(CsvWriter.WriteToString(dt, true, true));
                    }
                }
            }
            finally
            {
                if (ms != null)
                    ms.Dispose();
            }
        }

        public override string CSVFromDataTable(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException("dt");
            using (DataTable dtDst = new DataTable())
            {
                dtDst.Locale = dt.Locale;
                CSVImporter.InitializeDataTable(dtDst);
                foreach (DataRow dr in dt.Rows)
                {
                    CrewLogFlight clf = new CrewLogFlight(dr);
                    CSVImporter.WriteEntryToDataTable(clf.ToLogbookEntry(), dtDst);
                }
                return CsvWriter.WriteToString(dtDst, true, true);
            }
        }

        public override IEnumerable<ExternalFormat> FromDataTable(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException("dt");
            List<CrewLogFlight> lst = new List<CrewLogFlight>();
            foreach (DataRow dr in dt.Rows)
                lst.Add(new CrewLogFlight(dr));
            return lst;
        }
    }
}