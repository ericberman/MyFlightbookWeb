using JouniHeikniemi.Tools.Text;
using MyFlightbook.CSV;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2018-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    /// <summary>
    /// Encapsulates eLogSite import.  One weird thing about eLogSite is that they export in minutes; doing this under the covers makes sense, but why they export it that way is beyond me.
    /// </summary>
    public class eLogSite : ExternalFormat
    {
        #region Properties
        public DateTime date { get; set; }
        public string ident { get; set; }
        public string type { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public string route { get; set; }
        public DateTime scheduleddeptime { get; set; }
        public DateTime arrivaltime { get; set; }
        public decimal total { get; set; }
        public string flt { get; set; }
        public decimal asel { get; set; }
        public decimal ases { get; set; }
        public decimal asea { get; set; }
        public decimal amel { get; set; }
        public decimal ames { get; set; }
        public decimal amea { get; set; }
        public decimal rotorcraft { get; set; }
        public decimal gyro { get; set; }
        public decimal glider { get; set; }
        public decimal airship { get; set; }
        public decimal balloon { get; set; }
        public decimal PL { get; set; }
        public decimal PPL { get; set; }
        public decimal PPS { get; set; }
        public decimal WSCL { get; set; }
        public decimal WSCS { get; set; }
        public decimal cstm1 { get; set; }
        public decimal cstm2 { get; set; }
        public decimal cstm3 { get; set; }
        public int daylandings { get; set; }
        public int nightlandings { get; set; }
        public int daywaterlandings { get; set; }
        public int nightwaterlandings { get; set; }
        public int cstm1landings { get; set; }
        public int cstm2landings { get; set; }
        public int cstm3landings { get; set; }
        public decimal night { get; set; }
        public decimal ioegiven { get; set; }
        public decimal actualinstrument { get; set; }
        public int numberofapproaches { get; set; }
        public decimal sim { get; set; }
        public decimal pic { get; set; }
        public decimal sic { get; set; }
        public decimal turbine { get; set; }
        public decimal jet { get; set; }
        public decimal turboprop { get; set; }
        public decimal tailwheel { get; set; }
        public decimal dualgiven { get; set; }
        public decimal xcountry { get; set; }
        public decimal solo { get; set; }
        public decimal dualreceived { get; set; }
        public string remarks { get; set; }
        public decimal greatcircledist { get; set; }
        public decimal hobbsout { get; set; }
        public decimal hobbsin { get; set; }
        public string companydayAA { get; set; }
        public string companynightAA { get; set; }
        #endregion

        public eLogSite() : base() { }

        public eLogSite(DataRow dr) : base(dr) { }

        public override LogbookEntry ToLogbookEntry()
        {
            scheduleddeptime = FixedUTCDateFromTime(date, scheduleddeptime);
            arrivaltime = FixedUTCDateFromTime(date, arrivaltime, scheduleddeptime);

            LogbookEntry le = new LogbookEntry()
            {
                Date = date,
                TailNumDisplay = ident,
                ModelDisplay = type,
                Route = JoinStrings(new string[] { from, route, to }),
                TotalFlightTime = Math.Round(total / 60.0M, 2),
                FullStopLandings = daylandings,
                NightLandings = nightlandings,
                Nighttime = Math.Round(night / 60.0M, 2),
                CFI = Math.Round(dualgiven / 60.0M, 2),
                Dual= Math.Round(dualreceived / 60.0M, 2),
                IMC = Math.Round(actualinstrument / 60.0M, 2),
                Approaches = numberofapproaches,
                GroundSim = Math.Round(sim / 60.0M, 2),
                PIC = Math.Round(pic / 60.0M, 2),
                SIC = Math.Round(sic / 60.0M, 2),
                CrossCountry= Math.Round(xcountry / 60.0M, 2),
                Comment = remarks,
                HobbsStart = hobbsout,
                HobbsEnd = hobbsin
            };

            le.CustomProperties.SetItems(new CustomFlightProperty[]
            {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropScheduledDeparture, scheduleddeptime, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropScheduledArrival, arrivaltime, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightNumber, flt),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropSolo, Math.Round(solo / 60.0M, 2))
            });

            return le;
        }
    }

    public class eLogSiteImporter : ExternalFormatImporter
    {
        public override string Name { get { return "eLogSite"; } }

        public override bool CanParse(byte[] rgb)
        {
            if (rgb == null)
                return false;

            MemoryStream ms = new MemoryStream(rgb);

            // Read the first non-empty row and see if it has ELog headers
            try
            {
                using (CSVReader reader = new CSVReader(ms))
                {
                    ms = null;
                    try
                    {
                        string[] rgszHeader = reader.GetCSVLine(true);
                        if (rgszHeader == null || rgszHeader.Length == 0)
                            return false;
                        HashSet<string> hs = new HashSet<string>();

                        foreach (string sz in rgszHeader)
                            hs.Add(sz.Trim().ToLower(CultureInfo.CurrentCulture));

                        return hs.Contains("day water landings") && hs.Contains("great circle dist") && hs.Contains("company day (aa)");
                    }
                    catch (CSVReaderInvalidCSVException) { }
                }
            }
            finally
            {
                if (ms != null)
                    ms.Dispose();
            }
            return false;
        }

        protected void NormalizeColumnNames(DataTable dt)
        {
            if (dt == null)
                return;

            // Fix up the column names in the data table
            foreach (DataColumn dc in dt.Columns)
                dc.ColumnName = Regex.Replace(dc.ColumnName, "[^a-zA-Z0-9-]", string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public override string CSVFromDataTable(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException("dt");
            NormalizeColumnNames(dt);
            using (DataTable dtDst = new DataTable())
            {
                dtDst.Locale = dt.Locale;
                CSVImporter.InitializeDataTable(dtDst);
                foreach (DataRow dr in dt.Rows)
                {
                    eLogSite els = new eLogSite(dr);
                    CSVImporter.WriteEntryToDataTable(els.ToLogbookEntry(), dtDst);
                }
                return CsvWriter.WriteToString(dtDst, true, true);
            }
        }

        public override IEnumerable<ExternalFormat> FromDataTable(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException("dt");

            NormalizeColumnNames(dt);
            List<eLogSite> lst = new List<eLogSite>();
            foreach (DataRow dr in dt.Rows)
                lst.Add(new eLogSite(dr));
            return lst;
        }
    }
}