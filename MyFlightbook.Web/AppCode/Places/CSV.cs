using JouniHeikniemi.Tools.Text;
using MyFlightbook.Geography;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2010-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Telemetry
{
    public class CSVTelemetryParser : TelemetryParser
    {
        public CSVTelemetryParser() : base() { }

        public override bool CanParse(string szData)
        {
            return szData != null;  // we can read anything.
        }

        #region CSV Reading Utilities
        /// <summary>
        /// We have some telemetry in the system from non-US iPhone/Androids that use commas a separators AND local locale with a comma as the decimal separator.
        /// In this case, we can look at the first line to determine it and fix it up.
        /// </summary>
        /// <param name="flightData"></param>
        /// <returns></returns>
        private string FixedFlightData(string flightData)
        {
            // Android:   LAT,LON,PALT,SPEED,HERROR,DATE,COMMENT\r\n
            // MFBIphone: LAT,LON,PALT,SPEED,HERROR,DATE,COMMENT\r\n
            Regex r = new Regex("^LAT,LON,PALT,SPEED,HERROR,DATE,COMMENT[\r\n]+[0-9]{1,2},[0-9]+,[0-9]{1,3},[0-9]+,[0-9]+,[0-9]+,[0-9],[0-9]+,[0-9],.*,.*[\r\n]", RegexOptions.Compiled);
            if (!r.IsMatch(flightData))
                return flightData;

            StringBuilder sbNew = new StringBuilder();
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream(Encoding.UTF8.GetBytes(flightData));
                using (CSVReader csvr = new CSVReader(ms))
                {
                    ms = null;  // for CA2202
                    string[] rgszHeader = csvr.GetCSVLine();
                    sbNew.AppendFormat(CultureInfo.InvariantCulture, "{0}\r\n", String.Join(",", rgszHeader));
                    string[] rgszRow = null;
                    while ((rgszRow = csvr.GetCSVLine()) != null)
                    {
                        if (rgszRow.Length != 11)   // give up if not 10 elements - otherwise we could fuck things up or have an exception below
                            return flightData;
                        sbNew.AppendFormat(CultureInfo.InvariantCulture, "{0}.{1},{2}.{3},{4},{5}.{6},{7}.{8},\"{9}\",\"{10}\"\r\n",
                            rgszRow[0], // Lat - integer
                            rgszRow[1], // Lat - fraction
                            rgszRow[2], // Lon, integer
                            rgszRow[3], // Lon, fraction
                            rgszRow[4], // PALT - is an integer
                            rgszRow[5], // Speed - integer
                            rgszRow[6], // Speed - fraction
                            rgszRow[7], // HError - integer
                            rgszRow[8], // Herror - Fraction
                            rgszRow[9], // Date
                            rgszRow[10] // Comment, if any
                            );
                    }
                }
            }
            finally
            {
                if (ms != null)
                    ms.Dispose();
            }

            return sbNew.ToString();
        }

        private string[] FixRowHack(string[] rgszRow, string[] rgszHeader)
        {
            /*
             *  Handle a bug where we have some older versions of mobile apps that record telemetry
             *  using commas as separator AND as decimal.  Yikes.  SO...if it has the header from a 
             *  mobile app AND has exactly the right number of too-many columns, fix it up.
             */
            if (rgszHeader.Length == 7 && rgszRow.Length == 11 &&                               // quick check for this could be a suspect row, just based on lengths
                String.Compare(rgszHeader[0], KnownColumnNames.LAT, StringComparison.CurrentCultureIgnoreCase) == 0 && String.Compare(rgszHeader[6], KnownColumnNames.COMMENT, StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                // now merge the relevant columns
                string[] rgszRowNew = new string[7];
                rgszRowNew[0] = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", rgszRow[0], rgszRow[1]);   // LAT
                rgszRowNew[1] = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", rgszRow[2], rgszRow[3]);   // LON
                rgszRowNew[2] = rgszRow[4];                                         // PALT
                rgszRowNew[3] = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", rgszRow[5], rgszRow[6]);   // Speed
                rgszRowNew[4] = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", rgszRow[7], rgszRow[8]);   // HERROR
                rgszRowNew[5] = rgszRow[9];                                         // Date
                rgszRowNew[6] = rgszRow[10];                                        // Comment
                return rgszRowNew;                                               // now swap this for the row to parse.
            }
            return rgszRow;
        }

        /// <summary>
        /// provides state and context for parsing CSV files
        /// </summary>
        private class CSVParsingContext
        {
            #region properties
            public bool fHasNakedTime { get; set; }
            public bool fHasNakedDate { get; set; }
            public bool fHasDateTime { get; set; }
            public bool fDeriveDateTime { get; set; }
            public bool fHasTimezone { get; set; }
            public bool fDeriveSpeed { get; set; }
            public bool fDeriveUTCDateTime { get; set; }
            public string szDateCol { get; set; }

            public int derivedColumnCount { get; set; }

            private List<KnownColumn> _columnList = null;
            public List<KnownColumn> ColumnList
            {
                get { return _columnList ?? (_columnList = new List<KnownColumn>()); }
            }

            private List<Position> _samplesList = null;
            public List<Position> SamplesList
            {
                get { return _samplesList ?? (_samplesList = new List<Position>()); }
            }
            #endregion

            public CSVParsingContext()
            {
                fHasNakedDate = fHasNakedTime = fHasDateTime = fDeriveDateTime = fDeriveUTCDateTime = fHasTimezone = fDeriveSpeed = false;
                szDateCol = string.Empty;
            }
        }

        private void SetUpContextFromHeaderRow(CSVParsingContext pc, string[] rgszHeader)
        {
            // This is a hack for JPI date, which has a "DATE" and a "TIME" column, which are actually both naked.  So effectively re-name them to indicate naked
            int iDateCol = -1;
            int iTimeCol = -1;
            for (int i = 0; i < rgszHeader.Length; i++)
            {
                if (String.Compare(rgszHeader[i], KnownColumnNames.DATE, StringComparison.CurrentCultureIgnoreCase) == 0)
                    iDateCol = i;
                if (String.Compare(rgszHeader[i], KnownColumnNames.TIME, StringComparison.CurrentCultureIgnoreCase) == 0)
                    iTimeCol = i;
            }
            if (iDateCol >= 0 && iTimeCol >= 0)
            {
                rgszHeader[iDateCol] = "LOCAL DATE";
                rgszHeader[iTimeCol] = "LOCAL TIME";
            }

            for (int i = 0; i < rgszHeader.Length; i++)
            {
                KnownColumn kc = KnownColumn.GetKnownColumn(rgszHeader[i].Trim());
                pc.ColumnList.Add(kc);
                if (kc.Type == KnownColumnTypes.ctNakedDate)
                    pc.fHasNakedDate = true;
                if (kc.Type == KnownColumnTypes.ctNakedTime)
                    pc.fHasNakedTime = true;
                ParsedData.Columns.Add(new DataColumn(kc.ColumnHeaderName, KnownColumn.ColumnDataType(kc.Type)));
            }

            pc.fDeriveDateTime = pc.fHasNakedTime && pc.fHasNakedDate && !ParsedData.HasDateTime;
            if (pc.fDeriveDateTime)
            {
                KnownColumn kc = KnownColumn.GetKnownColumn(KnownColumnNames.UTCDateTime);
                pc.ColumnList.Add(kc);
                DataColumn dc = new DataColumn(kc.Column, KnownColumn.ColumnDataType(kc.Type)) { DateTimeMode = DataSetDateTime.Utc };
                ParsedData.Columns.Add(dc);
                dc.DateTimeMode = DataSetDateTime.Utc;
                pc.derivedColumnCount++;
            }

            pc.fHasDateTime = ParsedData.HasDateTime;
            pc.fHasTimezone = ParsedData.HasTimezone;

            pc.szDateCol = ParsedData.DateColumn;   // Set this before deriving a UTCDateTime so that when we derive UTCDateTime, we don't try to read from it.

            if (pc.fHasDateTime && pc.fHasTimezone && !ParsedData.HasUTCDateTime)    // add a UTC time as well
            {
                pc.fDeriveUTCDateTime = true;
                KnownColumn kc = KnownColumn.GetKnownColumn(KnownColumnNames.UTCDateTime);
                pc.ColumnList.Add(kc);
                ParsedData.Columns.Add(new DataColumn(kc.Column, KnownColumn.ColumnDataType(kc.Type)) { DateTimeMode = DataSetDateTime.Utc });
                pc.derivedColumnCount++;
            }

            pc.fDeriveSpeed = (ParsedData.HasLatLongInfo && pc.fHasDateTime && !ParsedData.HasSpeed);
            if (pc.fDeriveSpeed)
            {
                KnownColumn kc = KnownColumn.GetKnownColumn(KnownColumnNames.DERIVEDSPEED);
                pc.ColumnList.Add(kc);
                ParsedData.Columns.Add(new DataColumn(kc.Column, KnownColumn.ColumnDataType(kc.Type)));
                pc.derivedColumnCount++;
            }
        }

        private DataRow ParseCSVRow(CSVParsingContext pc, string[] rgszRow, int iRow)
        {
            int cColExpected = rgszRow.Length + pc.derivedColumnCount;
            if (cColExpected < ParsedData.Columns.Count)
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.errNotEnoughColumns, iRow));
            if (cColExpected > ParsedData.Columns.Count)
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.errTooManyColumns, iRow));

            DataRow dr = ParsedData.NewRow();
            for (int j = 0; j < rgszRow.Length; j++)
                dr[j] = pc.ColumnList[j].ParseToType(rgszRow[j].Trim());

            // derive time, if necessary
            if (pc.fDeriveDateTime)
            {
                DateTime dtNakedDate = (DateTime)dr[KnownColumnNames.NakedDate];
                DateTime dtNakedTime = (DateTime)dr[KnownColumnNames.NakedTime];
                int tzOffset = (pc.fHasTimezone) ? (int)dr[ParsedData.TimeZoneHeader] : 0;
                dr[KnownColumnNames.UTCDateTime] = DateTime.SpecifyKind(new DateTime(dtNakedDate.Year, dtNakedDate.Month, dtNakedDate.Day, dtNakedTime.Hour, dtNakedTime.Minute, dtNakedTime.Second, pc.fHasTimezone ? DateTimeKind.Unspecified : DateTimeKind.Utc).AddMinutes(tzOffset), DateTimeKind.Utc);
            }

            if (pc.fDeriveUTCDateTime)
            {
                DateTime dt = (DateTime)dr[pc.szDateCol];
                dr[KnownColumnNames.UTCDateTime] = DateTime.SpecifyKind(dt.AddMinutes((int)dr[ParsedData.TimeZoneHeader]), DateTimeKind.Utc);
            }

            // derive speed, if necessary
            if (pc.fDeriveSpeed)
            {
                Position samp = new Position(Convert.ToDouble(dr[KnownColumnNames.LAT], CultureInfo.CurrentCulture), Convert.ToDouble(dr[KnownColumnNames.LON], CultureInfo.CurrentCulture), Convert.ToDateTime(dr[pc.szDateCol], CultureInfo.CurrentCulture));
                pc.SamplesList.Add(samp);
            }
            return dr;
        }
        #endregion
        /// <summary>
        /// Parses CSV-based flight data.  Note that we do this in Invariant locale - so we use COMMAS as list separators and we use periods as decimal separators.
        /// </summary>
        /// <param name="flightData">The string of flight data</param>
        /// <returns>True for success</returns>
        public override bool Parse(string szData)
        {
            StringBuilder sbErr = new StringBuilder();
            TelemetryDataTable m_dt = ParsedData;
            m_dt.Clear();
            Boolean fResult = true;

            string flightData = FixedFlightData(szData);

            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream(Encoding.UTF8.GetBytes(flightData));
                using (CSVReader csvr = new CSVReader(ms))
                {
                    ms = null;
                    try
                    {
                        string[] rgszHeader = null;

                        // Find the first row that looks like columns.
                        try
                        {
                            do
                            {
                                rgszHeader = csvr.GetCSVLine(true);
                            } while (rgszHeader != null && (rgszHeader.Length < 2 || rgszHeader[0].StartsWith("#", StringComparison.OrdinalIgnoreCase) || rgszHeader[0].Contains("Date (yyyy-mm-dd)")));
                        }
                        catch (CSVReaderInvalidCSVException ex)
                        {
                            throw new MyFlightbookException(ex.Message);
                        }

                        if (rgszHeader == null) // no valid CSV header found
                        {
                            sbErr.Append(Resources.FlightData.errNoCSVHeaderRowFound);
                            return false;
                        }

                        CSVParsingContext pc = new CSVParsingContext();
                        SetUpContextFromHeaderRow(pc, rgszHeader);

                        string[] rgszRow = null;
                        int iRow = 0;

                        while ((rgszRow = csvr.GetCSVLine()) != null)
                        {
                            // Test for empty row
                            if (String.Join(string.Empty, rgszRow).Trim().Length == 0)
                                continue;

                            iRow++;

                            try
                            {
                                m_dt.Rows.Add(ParseCSVRow(pc, FixRowHack(rgszRow, rgszHeader), iRow));
                            }
                            catch (MyFlightbookException ex)
                            {
                                sbErr.Append(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.errInRow, iRow, ex.Message));
                                fResult = false;
                            }
                            catch (System.FormatException ex)
                            {
                                sbErr.Append(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.errInRow, iRow, ex.Message));
                                fResult = false;
                            }
                        }

                        // Go back and put the derived speeds in.
                        if (pc.fDeriveSpeed)
                        {
                            Position.DeriveSpeed(pc.SamplesList);
                            for (int i = 0; i < m_dt.Rows.Count && i < pc.SamplesList.Count; i++)
                                if (pc.SamplesList[i].HasSpeed)
                                    m_dt.Rows[i][KnownColumnNames.DERIVEDSPEED] = pc.SamplesList[i].Speed;
                        }
                    }
                    catch (MyFlightbookException ex)
                    {
                        sbErr.Append(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.errGeneric, ex.Message));
                        fResult = false;
                    }
                    catch (System.Data.DuplicateNameException ex)
                    {
                        sbErr.Append(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.errGeneric, ex.Message));
                        fResult = false;
                    }
                    finally
                    {
                    }
                }
            }
            finally
            {
                if (ms != null)
                    ms.Dispose();
            }

            ErrorString = sbErr.ToString();

            return fResult;
        }
    }
}