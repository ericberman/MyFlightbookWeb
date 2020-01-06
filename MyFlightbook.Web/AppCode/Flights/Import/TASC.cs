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
    /// Imports crazy TASC (Sabre/AA) layout, which isn't at all well structured.
    /// </summary>
    public class TASCFlight : ExternalFormat
    {
        #region Properties
        public DateTime blockIn;
        public DateTime blockOut;
        public string model;
        public string tail;
        public string dep;
        public string arr;
        public string position;
        public string flightNumber;
        #endregion

        public TASCFlight()
        {
            model = tail = dep = arr = position = flightNumber = string.Empty;
            blockIn = blockOut = DateTime.MinValue;
        }

        public override LogbookEntry ToLogbookEntry()
        {
            PendingFlight pf = new PendingFlight()
            {
                Date = blockOut.Date,
                ModelDisplay = model,
                TailNumDisplay = tail,
                Route = String.Format(CultureInfo.InvariantCulture, Resources.LocalizedText.LocalizedJoinWithSpace, dep, arr),
                TotalFlightTime = Math.Max((decimal) blockIn.Subtract(blockOut).TotalHours, 0)
            };

            if (position.CompareCurrentCultureIgnoreCase("CA") == 0)
                pf.PIC = pf.TotalFlightTime;

            if (position.CompareCurrentCultureIgnoreCase("FO") == 0)
                pf.SIC = pf.TotalFlightTime;

            pf.CustomProperties.SetItems(new CustomFlightProperty[]
            {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightNumber, flightNumber),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockOut, blockOut, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockIn, blockIn, true)
            });

            return pf;
        }
    }

    public class TASCImporter : ExternalFormatImporter
    {
        private readonly List<TASCFlight> lstMatches = new List<TASCFlight>();

        private readonly static Regex regTASC = new Regex("^(?<position>\\w{2}),,(?<model>\\w+),,(?<tail>\\w+),,(?<uselessmodel>\\w+),,(?<eq>\\w+),(?<flight>\\w+),,(?<dep>\\w+),,(?<depmonth>\\d{1,2})\\/(?<depday>\\d{1,2}) (?<dephour>\\d{1,2}):(?<depmin>\\d{2})[^,]*,,,[^,]*,,,(?<arr>\\w+),,,(?<arrmonth>\\d{1,2})\\/(?<arrday>\\d{1,2}) (?<arrhour>\\d{1,2}):(?<arrmin>\\d{2})[^,]*,.*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private readonly static Regex regDateAnchor = new Regex("^(?<date>\\d{1,4}\\/\\d{1,4}\\/\\d{1,4}) ");

        public override string Name {get {return "TASC";} }

        public override bool CanParse(byte[] rgb)
        {
            if (rgb == null || rgb.Length == 0)
                return false;

            string sz = Encoding.UTF8.GetString(rgb);
            return sz.Contains("TASC LOG DATA") && sz.Contains("GTR");
        }

        public override byte[] PreProcess(byte[] rgb)
        {
            if (rgb == null)
                return null;

            String sz = Encoding.UTF8.GetString(rgb);
            DateTime dt = DateTime.Now;
            string szLine;
            using (StreamReader sr = new StreamReader(new MemoryStream(rgb)))
            {
                while ((szLine = sr.ReadLine()) != null)
                {
                    MatchCollection mc = regDateAnchor.Matches(szLine);
                    if (mc != null && mc.Count > 0)
                    {
                        try
                        {
                            dt = Convert.ToDateTime(mc[0].Groups["date"].Value, CultureInfo.CurrentCulture);
                        }
                        catch (FormatException) { }
                        continue;
                    }

                    mc = regTASC.Matches(szLine);
                    if (mc != null && mc.Count > 0)
                    {
                        GroupCollection gc = mc[0].Groups;
                        TASCFlight t = new TASCFlight()
                        {
                            position = gc["position"].Value,
                            model = gc["model"].Value,
                            tail = gc["tail"].Value,
                            flightNumber = gc["flight"].Value,
                            dep = gc["dep"].Value,
                            arr = gc["arr"].Value,
                            blockOut = new DateTime(dt.Year, Convert.ToInt32(gc["depmonth"].Value, CultureInfo.InvariantCulture), Convert.ToInt32(gc["depday"].Value, CultureInfo.InvariantCulture), Convert.ToInt32(gc["dephour"].Value, CultureInfo.InvariantCulture), Convert.ToInt32(gc["depmin"].Value, CultureInfo.InvariantCulture), 0, DateTimeKind.Utc),
                            blockIn = new DateTime(dt.Year, Convert.ToInt32(gc["arrmonth"].Value, CultureInfo.InvariantCulture), Convert.ToInt32(gc["arrday"].Value, CultureInfo.InvariantCulture), Convert.ToInt32(gc["arrhour"].Value, CultureInfo.InvariantCulture), Convert.ToInt32(gc["arrmin"].Value, CultureInfo.InvariantCulture), 0, DateTimeKind.Utc)
                        };

                        // handle rollover to new year
                        if (t.blockOut.Month < dt.Month)
                            t.blockOut = t.blockOut.AddYears(1);
                        if (t.blockIn.Month < dt.Month)
                            t.blockIn = t.blockIn.AddYears(1);

                        if (t.position.CompareCurrentCultureIgnoreCase("PO") == 0)
                            continue;

                        lstMatches.Add(t);
                    }
                }
            }

            return Encoding.UTF8.GetBytes(CSVFromDataTable(null));
        }

        public override string CSVFromDataTable(DataTable dt)
        {
            // We ignore the data table passed in - we have our data from Matches, which was initialized in PreProcess.

            using (DataTable dtDst = new DataTable())
            {
                dtDst.Locale = CultureInfo.CurrentCulture;
                CSVImporter.InitializeDataTable(dtDst);
                foreach (TASCFlight rb in lstMatches)
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