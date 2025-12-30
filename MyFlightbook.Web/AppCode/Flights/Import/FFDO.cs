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
 * Copyright (c) 2022-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    // Class to import from FFDO format.  See https://rpa.flica.net/webhelp/webhelprpacm/Schedules/FFDOFormatTextScheduleCSH.htm
    public class FFDO : ExternalFormat
    {
        #region Properties
        public DateTime DATE { get; set; }
        public string FLTNO { get; set; }
        public string DPS { get; set; }
        public string ARS { get; set; }
        public string DEPL { get; set; }
        public string ARRL { get; set; }
        #endregion

        public FFDO(DataRow dr) : base(dr) { }

        static private readonly LazyRegex rTime = new LazyRegex("^(?<hour>\\d{1,2}):?(?<min>\\d{2})$", RegexOptions.CultureInvariant);

        private static DateTime FixDateForTime(DateTime dtRef, string szTime)
        {
            DateTime dt = DateTime.MinValue;

            if (szTime == null)
                return dt;

            MatchCollection m = rTime.Matches(szTime);

            if (m.Count == 1)
                return FixedUTCDateFromTime(dtRef, new DateTime(dtRef.Year, dtRef.Month, dtRef.Day, Convert.ToInt32(m[0].Groups["hour"].Value, CultureInfo.InvariantCulture), Convert.ToInt32(m[0].Groups["min"].Value, CultureInfo.InvariantCulture), 0));

            return dt;
        }

        public override LogbookEntry ToLogbookEntry()
        {
            
            PendingFlight pf = new PendingFlight()
            {
                Date = DATE,
                Route = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, DPS, ARS).Trim()
            };

            pf.CustomProperties.SetItems(new CustomFlightProperty[] {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightNumber, FLTNO),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockOut, FixDateForTime(DATE, DEPL), true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockIn, FixDateForTime(DATE, ARRL), true)
            });
            return pf;
        }
    }

    public class FFDOImporter : ExternalFormatImporter
    {
        public override string Name => "FFDO (Federal Flight Deck Officer)";

        public override bool IsPendingOnly => true;

        public override bool CanParse(byte[] rgb)
        {
            if (rgb == null)
                return false;

            using (MemoryStream ms = new MemoryStream(rgb))
            {
                using (CSVReader r = new CSVReader(ms))
                {
                    string[] rgHeader = r.GetCSVLine();
                    if (rgHeader == null || rgHeader.Length == 0)
                        return false;
                    HashSet<string> hs = new HashSet<string>();
                    foreach (string sz in rgHeader)
                        hs.Add(sz.ToUpperInvariant());

                    return hs.Contains("DATE") && hs.Contains("FLTNO") && hs.Contains("DPS") && hs.Contains("DEPL") && hs.Contains("ARS") && hs.Contains("ARRL");
                }
            }
        }

        /// <summary>
        /// Converts the FFDO to regular CSV.
        /// </summary>
        /// <param name="rgb"></param>
        /// <returns></returns>
        public override byte[] PreProcess(byte[] rgb)
        {
            if (rgb == null || rgb.Length == 0)
                return Array.Empty<byte>();

            // Capitalize header row for import
            using (MemoryStream ms = new MemoryStream(rgb))
            {
                using (CSVReader r = new CSVReader(ms))
                {
                    string[] rgHeader = r.GetCSVLine();

                    if (rgHeader == null || rgHeader.Length == 0)
                        return Array.Empty<byte>();

                    for (int i = 0; i < rgHeader.Length; i++)
                        rgHeader[i] = rgHeader[i].ToUpperInvariant();

                    using (DataTable dtDst = new DataTable() {  Locale = CultureInfo.CurrentCulture })
                    {
                        for (int i = 0; i < rgHeader.Length; i++)
                            dtDst.Columns.Add(new DataColumn(rgHeader[i], typeof(string)));

                        // Add one more column for tail number, just to avoid that error
                        dtDst.Columns.Add(new DataColumn("Tail Number", typeof(string)));

                        // Now write out the remaining rows.
                        string[] rowData;
                        while ((rowData = r.GetCSVLine()) != null)
                        {
                            DataRow row = dtDst.NewRow();
                            for (int i = 0; i < rowData.Length; i++)
                                row[i] = rowData[i];
                            dtDst.Rows.Add(row);
                        }

                        return Encoding.UTF8.GetBytes(CsvWriter.WriteToString(dtDst, true, true));
                    }
                }
            }
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
                    FFDO ffdo= new FFDO(dr);
                    CSVImporter.WriteEntryToDataTable(ffdo.ToLogbookEntry(), dtDst);
                }
                return CsvWriter.WriteToString(dtDst, true, true);
            }
        }
    }
}
