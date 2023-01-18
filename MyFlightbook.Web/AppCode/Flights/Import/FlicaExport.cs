using JouniHeikniemi.Tools.Text;
using MyFlightbook.CSV;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    /// <summary>
    /// Imports what appears to be a schedule export from Flica.
    /// </summary>
    public class FlicaExport : ExternalFormat
    {
        #region Properties
        public DateTime DATE { get; set; }
        public string DH { get; set; }
        public string TAIL { get; set; }
        public string EQP { get; set; }
        public string FLIGHT { get; set; }
        public string DEP { get; set; }

        public string ARR { get; set; }

        /// <summary>
        /// Departure time - useless because it's local
        /// </summary>
        public string DEPTIME { get; set; }

        /// <summary>
        /// Arrival time - useless because it's local
        /// </summary>
        public string ARRTIME { get; set; }

        public string BLOCK { get; set; }

        public string CREW { get; set; }
        #endregion

        public FlicaExport(DataRow dr) : base(dr) { }

        public override LogbookEntry ToLogbookEntry()
        {
            // "BLOCK" contains the time as a solid integer.  E.g., 77 minutes is 107.  Pathetic.  What's wrong with using a colon?
            int totalTime = String.IsNullOrEmpty(BLOCK) ? 0 : Convert.ToInt32(BLOCK, CultureInfo.InvariantCulture);
            int totalMinutes = totalTime % 100;
            int totalHours = totalTime / 100;
            PendingFlight pf = new PendingFlight()
            {
                Date = DATE,
                Route = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, DEP, ARR).Trim(),
                TotalFlightTime = totalHours + totalMinutes / 60.0M,
                TailNumDisplay = TAIL
            };

            pf.CustomProperties.SetItems(new CustomFlightProperty[] {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightNumber, FLIGHT),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropCrew1, CREW),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropDeadhead, String.Compare(DH, "DH", true, CultureInfo.InvariantCulture) == 0)
            });
            return pf;
        }
    }

    public class FlicaExportImporter : ExternalFormatImporter
    {
        public override string Name => "Flica Export";

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

                    return hs.Contains("DATE") && hs.Contains("DH") && hs.Contains("EQP") && hs.Contains("FLIGHT") && hs.Contains("DEPTIME") && hs.Contains("ARRTIME") && hs.Contains("CREW");
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

                    using (DataTable dtDst = new DataTable() { Locale = CultureInfo.CurrentCulture })
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
                    FlicaExport fe = new FlicaExport(dr);
                    CSVImporter.WriteEntryToDataTable(fe.ToLogbookEntry(), dtDst);
                }
                return CsvWriter.WriteToString(dtDst, true, true);
            }
        }
    }
}