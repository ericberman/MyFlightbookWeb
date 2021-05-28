using HtmlAgilityPack;
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
 * Copyright (c) 2019-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    public class eCrewFlight : ExternalFormat
    {
        #region Properties
        public DateTime Date { get; set; }
        public string FlightNum { get; set; }
        public string TailNum { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string BlockOut { get; set; }
        public string BlockIn { get; set; }
        public string Model { get; set; }
        public string PICName { get; set; }
        public int DayTO { get; set; }
        public int NightTO { get; set; }
        public int DayLandings { get; set; }
        public int NightLandings { get; set; }
        public decimal PIC { get; set; }
        public decimal SIC { get; set; }
        public decimal IMC { get; set; }
        public decimal TotalTime { get; set; }
        public decimal GroundSim { get; set; }
        public string SimID { get; set; }
        public bool Part91 { get; set; }
        #endregion

        public eCrewFlight(DataRow dr)
        {
            if (dr == null)
                return;

            Date = Convert.ToDateTime(dr["Date"], CultureInfo.CurrentCulture);
            TailNum = (string) dr["Reg."] ?? string.Empty;
            Model = (string)dr["Model"] ?? string.Empty;
            From = (string)dr["From"] ?? string.Empty;
            To = (string)dr["To"] ?? string.Empty;
            TotalTime = ((string)dr["Total Flight Time"]).SafeParseDecimal();
            PIC = ((string)dr["PIC"]).SafeParseDecimal();
            SIC = ((string)dr["Co-Plt"]).SafeParseDecimal();
            IMC = ((string)dr["Instr."]).SafeParseDecimal();
            GroundSim = ((string)dr["Ground Simulator"]).SafeParseDecimal();
            FlightNum = dr.Table.Columns.Contains("Flight Number") ? ((string)dr["Flight Number"] ?? string.Empty) : string.Empty;
            BlockOut = (string)dr["Block Out Time"] ?? string.Empty;
            BlockIn = (string)dr["Block In Time"] ?? string.Empty;
            SimID = (string)dr["Simulator/Training Device Identifier"] ?? string.Empty;
            DayTO = ((string)dr["Takeoffs (any)"]).SafeParseInt();
            NightTO= ((string)dr["Takeoffs - Night"]).SafeParseInt();
            DayLandings = ((string)dr["FS Day Landings"]).SafeParseInt();
            NightLandings = ((string)dr["FS Night Landings"]).SafeParseInt();
            Part91 = dr.Table.Columns.Contains("PT91") && !String.IsNullOrEmpty((string)dr["PT91"]);
            PICName = ((string)dr["Name PIC"]) ?? string.Empty;
        }

        private static readonly Regex rTime = new Regex("(?<hour>\\d{1,2}):(?<min>\\d{2})", RegexOptions.Compiled);

        public override LogbookEntry ToLogbookEntry()
        {
            LogbookEntry le = new LogbookEntry()
            {
                Date = this.Date,
                TailNumDisplay = TailNum,
                ModelDisplay = Model,
                Route = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, From, To),
                TotalFlightTime = TotalTime,
                PIC = this.PIC,
                SIC = this.SIC,
                IMC = this.IMC,
                GroundSim = this.GroundSim,
                FullStopLandings = DayLandings,
                NightLandings = NightLandings
            };

            // Compute block out/block in.
            MatchCollection mcBlockOut = rTime.Matches(BlockOut);
            MatchCollection mcBlockIn = rTime.Matches(BlockIn);

            DateTime? blockOut = null;
            DateTime? blockIn = null;

            if (mcBlockOut.Count > 0)
                blockOut = DateTime.SpecifyKind(Convert.ToDateTime(String.Format(CultureInfo.InvariantCulture, "{0} {1}:{2}", Date.YMDString(), mcBlockOut[0].Groups["hour"].Value, mcBlockOut[0].Groups["min"].Value), CultureInfo.InvariantCulture), DateTimeKind.Utc);

            if (mcBlockIn.Count > 0)
                blockIn = DateTime.SpecifyKind(Convert.ToDateTime(String.Format(CultureInfo.InvariantCulture, "{0} {1}:{2}", Date.YMDString(), mcBlockIn[0].Groups["hour"].Value, mcBlockIn[0].Groups["min"].Value), CultureInfo.InvariantCulture), DateTimeKind.Utc);

            if (blockIn != null && blockOut != null && blockIn.Value.CompareTo(blockOut.Value) < 0)
                blockIn = blockIn.Value.AddDays(1);

            List<CustomFlightProperty> lst = new List<CustomFlightProperty>()
            {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropSimRegistration, SimID),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightNumber, FlightNum),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPart91, Part91),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNameOfPIC, PICName),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNightTakeoff, NightTO),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropTakeoffAny, DayTO)
            };

            if (blockIn != null)
                lst.Add(CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockIn, blockIn.Value, true));

            if (blockOut != null)
                lst.Add(CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockOut, blockOut.Value, true));

            le.CustomProperties.SetItems(lst);
            return le;
        }
    }

    internal class PositionedDiv : IComparable
    {
        public int x { get; set; }
        public int y { get; set; }
        public string value { get; set; }

        public int CompareTo(Object obj)
        {
            PositionedDiv pd = obj as PositionedDiv;
            return (pd.y == y) ? x.CompareTo(pd.x) : y.CompareTo(pd.y);
        }

        public override string ToString()
        {
            return value;
        }
    }

    public class eCrew : ExternalFormatImporter
    {
        public override string Name => "eCrew";

        public override bool CanParse(byte[] rgb)
        {
            if (rgb == null || rgb.Length == 0)
                return false;

            string sz = Encoding.UTF8.GetString(rgb);
            return sz.StartsWith("<html>", StringComparison.InvariantCultureIgnoreCase) && sz.Contains("Pilot&nbsp;LogBook&nbsp;Report");
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
                    eCrewFlight ecf= new eCrewFlight(dr);
                    CSVImporter.WriteEntryToDataTable(ecf.ToLogbookEntry(), dtDst);
                }
                return CsvWriter.WriteToString(dtDst, true, true);
            }
        }

        private static readonly Regex rTop = new Regex("top: *(?<yval>\\d+)px", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rLeft = new Regex("left: *(?<xval>\\d+)px", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rZIndex = new Regex("z-index:", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static IList<PositionedDiv> ParseDivs(byte[] rgb)
        {
            HtmlDocument doc = new HtmlDocument();
            List<PositionedDiv> lstAllValues = new List<PositionedDiv>();
            using (MemoryStream ms = new MemoryStream(rgb))
            {
                doc.Load(ms);
                foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//div[@style]"))
                {
                    string szVal = node.InnerText.Replace("&nbsp;", " ").Trim();
                    string szStyle = node.Attributes["style"].Value;
                    MatchCollection mcTop = rTop.Matches(szStyle);
                    MatchCollection mcLeft = rLeft.Matches(szStyle);
                    if (rZIndex.IsMatch(szStyle) && mcTop.Count > 0 && mcLeft.Count > 0 && Int32.TryParse(mcTop[0].Groups["yval"].Value, out int yval) && Int32.TryParse(mcLeft[0].Groups["xval"].Value, out int xval))
                        lstAllValues.Add(new PositionedDiv() { value = szVal, x = xval, y = yval });
                }
            }
            lstAllValues.Sort();
            return lstAllValues;
        }

        private static Dictionary<int, IList<PositionedDiv>> GroupPositionedDivs(IEnumerable<PositionedDiv> positionedDivs)
        {
            Dictionary<int, IList<PositionedDiv>> d = new Dictionary<int, IList<PositionedDiv>>();
            foreach (PositionedDiv pd in positionedDivs)
            {
                if (!d.ContainsKey(pd.y))
                    d[pd.y] = new List<PositionedDiv>();

                d[pd.y].Add(pd);
            }
            return d;
        }

        private static int ModeForDictionary(Dictionary<int, IList<PositionedDiv>> d)
        {
            Dictionary<int, int> dMode = new Dictionary<int, int>();
            foreach (int key in d.Keys)
            {
                int count = d[key].Count;
                if (!dMode.ContainsKey(count))
                    dMode[count] = 0;
                dMode[count]++;
            }
            int maxVal = 0;
            int N = 0;
            foreach (int key in dMode.Keys)
                if (dMode[key] > maxVal)
                {
                    maxVal = dMode[key];
                    N = key;
                }
            return N;
        }

        /// <summary>
        /// The header row only has the bottom bits of the column header, so it has redundancies or incomplete names.
        /// </summary>
        /// <param name="headerRow"></param>
        private static void MapHeaderColumns(IList<PositionedDiv> headerRow)
        {
            int placeCount = 0;
            int timeCount = 0;
            int typeCount = 0;
            int dayCount = 0;
            int nightCount = 0;

            HashSet<string> hs = new HashSet<string>();

            for (int i = 0; i < headerRow.Count; i++)
            {
                PositionedDiv pd = headerRow[i];
                switch (pd.value.ToUpperInvariant())
                {
                    case "FLIGHT":
                        pd.value = "Flight Number";
                        break;
                    case "PLACE":
                        pd.value = (placeCount == 0) ? "From" : "To";
                        placeCount++;
                        break;
                    case "TIME":
                        pd.value = (timeCount == 0) ? "Block Out Time" : ((timeCount == 1) ? "Block In Time" : "Ground Simulator");
                        timeCount++;
                        break;
                    case "TYPE":
                        pd.value = (typeCount == 0) ? "Model" : "Simulator/Training Device Identifier";
                        typeCount++;
                        break;
                    case "OF FLIGHT":
                        pd.value = "Total Flight Time";
                        break;
                    case "DAY":
                        pd.value = (dayCount == 0) ? "Takeoffs (any)" : "FS Day Landings";
                        dayCount++;
                        break;
                    case "NIGHT":
                        pd.value = (nightCount == 0) ? "Takeoffs - Night" : "FS Night Landings";
                        nightCount++;
                        break;
                    default:
                        {
                            // avoid other duplicates.  E.g., "#" is on some ecrew.  Also catch any empty values.
                            string szValue = pd.value.Trim();
                            int ext = 0;
                            while (String.IsNullOrEmpty(szValue) || hs.Contains(szValue))
                                szValue = pd.value + ext++.ToString(CultureInfo.InvariantCulture);
                            pd.value = szValue;
                            hs.Add(szValue);
                        }
                        break;
                }
            }
        }

        private static byte[] WriteRowsToCSV(IEnumerable<IList<PositionedDiv>> rows)
        {
            using (DataTable dt = new DataTable())
            {
                dt.Locale = CultureInfo.CurrentCulture;

                bool fFirstRow = true;

                foreach (IList<PositionedDiv> row in rows)
                {
                    if (fFirstRow)
                    {
                        fFirstRow = false;
                        foreach (PositionedDiv pd in row)
                            dt.Columns.Add(new DataColumn(pd.value, typeof(string)));
                    }
                    else
                    {
                        DataRow dr = dt.NewRow();
                        for (int iCol = 0; iCol < row.Count; iCol++)
                            dr[iCol] = row[iCol].value;
                        dt.Rows.Add(dr);
                    }
                }
                return Encoding.UTF8.GetBytes(CsvWriter.WriteToString(dt, true, true));
            }
        }

        public override byte[] PreProcess(byte[] rgb)
        {
            /*
                Maybe here's a simpler hack: add a feature to the playpen for this. Takes the HTML, parses it into CSV, and you can adjust the column headers as appropriate.
                Here's what it would do:

                Look for all absolutely positioned divs
                Sort by y position
                Group by y-position, sorting by x position. E.g., it's an array of Lists, each list contains div values sorted by X
                Compute N, the mode of counts. E.g., what is the most frequent value for the # of columns at a given Y value. That's probably your rows of data, and column headers as well.
                Remove any rows that are redundant with the first (lowest "Y") row. (I.e., redundant column labels) or which have other than N data values.
                Spit those rows out as CSV
                Let the user adjust the column headings, since the column headers from this method will likely be ambiguous (e.g., redundant "Place" and "Time") headers. Could optionally try to be a bit smart and look for 1st and 2nd place/time, assuming first is departure and 2nd is arrival (ditto takeoffs and landings).
            */

            // Find all of the divs, sorted.
            IList<PositionedDiv> positionedDivs = ParseDivs(rgb);

            // Group by y position
            Dictionary<int, IList<PositionedDiv>> d = GroupPositionedDivs(positionedDivs);

            // Find the mode
            int N = ModeForDictionary(d);

            // if nothing found, short circuit.
            if (N == 0)
                return Array.Empty<byte>();

            // N should now have the most frequently found row count.  That's (presumably our data).  Remove any rows with fewer than this number, and which are redundant with the first such row.
            // First row (headings) SHOULD start with "Date".  If not, bail.  Remove any subsequent row that starts with "Date"
            List<IList<PositionedDiv>> lstRows = new List<IList<PositionedDiv>>();
            bool fSeenHeader = false;
            List<int> lstKeys = new List<int>(d.Keys);
            lstKeys.Sort();   // just to be sure we enumerate in ascending order
            foreach (int key in lstKeys)
            {
                if (d[key].Count != N)
                    continue;

                bool fIsHeaderRow = d[key][0].value.CompareCurrentCultureIgnoreCase("Date") == 0;
                if (fSeenHeader && fIsHeaderRow)
                    continue;

                // Ignore the totals rows too.
                if (d[key][0].value.StartsWith("Totals", StringComparison.CurrentCultureIgnoreCase))
                    continue;

                if (fIsHeaderRow && !fSeenHeader)
                {
                    fSeenHeader = true;

                    // This had better be before any data rows, otherwise bail.
                    if (lstRows.Count > 0)
                        return Array.Empty<byte>();
                }

                lstRows.Add(d[key]);
            }

            // Adjust column headers as best we can.  lstRows *should* have at least one row if we're here.
            MapHeaderColumns(lstRows[0]);

            // We should now have something approximating a CSV.  Write it out
            return WriteRowsToCSV(lstRows);
        }

        public override IEnumerable<ExternalFormat> FromDataTable(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException(nameof(dt));
            List<eCrewFlight> lst = new List<eCrewFlight>();
            foreach (DataRow dr in dt.Rows)
                lst.Add(new eCrewFlight(dr));
            return lst;
        }
    }
}