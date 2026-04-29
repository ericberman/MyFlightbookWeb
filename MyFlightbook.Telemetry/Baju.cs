using GeoTimeZone;
using MyFlightbook.Charting;
using MyFlightbook.CSV;
using MyFlightbook.Telemetry.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;


/******************************************************
 * 
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Telemetry
{
    /// <summary>
    /// Class to parse Baju Software telemetry file
    /// </summary>
    public class Baju : TelemetryParser
    {
        private static readonly LazyRegex rBaju = new LazyRegex("Copyright.*BajuSoftware", RegexOptions.Multiline);
        private static readonly LazyRegex rDate = new LazyRegex("Date/Time: (?<datestring>[0-9-/]+ .*[AP]M)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public Baju() : base()
        {
        }

        public override bool CanParse(string szData)
        {
            if (String.IsNullOrEmpty(szData))
                return false;

            return rBaju.IsMatch(szData);
        }

        private Dictionary<string, KnownColumn> mColumnMap = null;

        private Dictionary<string, KnownColumn> ColumnMap
        {
            get
            {
                if (mColumnMap == null)
                    mColumnMap = new Dictionary<string, KnownColumn>()
                    {
                        {"ElapsedSeconds", KnownColumn.GetKnownColumn(KnownColumnNames.DATE)},
                        {"Latitude", KnownColumn.GetKnownColumn(KnownColumnNames.LAT)},
                        {"Longitude", KnownColumn.GetKnownColumn(KnownColumnNames.LON)},
                        {"GroundSpeed (Knots)", KnownColumn.GetKnownColumn("Gnd Speed") },
                        {"Airspeed (Knots Ind)", KnownColumn.GetKnownColumn(KnownColumnNames.SPEED)},
                        {"Altitude (Feet Ind)", KnownColumn.GetKnownColumn(KnownColumnNames.ALT)},
                        {"Heading (Mag)", KnownColumn.GetKnownColumn("Heading") },
                        {"Vert Speed (Ft/Min)", KnownColumn.GetKnownColumn("VSpd") },
                        {"RPM L (P)", KnownColumn.GetKnownColumn("RPM") },
                        {"Oil Temp L (Deg F)", KnownColumn.GetKnownColumn("OilT") },
                        {"CHT L (Deg F)", KnownColumn.GetKnownColumn("C1") },
                        {"CHT R (Deg F)", KnownColumn.GetKnownColumn("C2") },
                        {"EGT L (Deg F)", KnownColumn.GetKnownColumn("E1") },
                        {"EGT R (Deg F)", KnownColumn.GetKnownColumn("E2") },
                        {"MAP L (in/Hg)", KnownColumn.GetKnownColumn("MAP") },
                        {"Fuel Flow L (Gal/Hr)", KnownColumn.GetKnownColumn("FF") },
                        {"Fuel Press L (Psi)", KnownColumn.GetKnownColumn("FP") },
                    };
                return mColumnMap;
            }
        }

        private DataRow ParseCSVRow(string[] rgszRow, Dictionary<int, KnownColumn> columnMap, DateTime baseTime, int iSecondsOffsetCol)
        {
            DataRow dr = ParsedData.NewRow();
            for (int i = 0; i < rgszRow.Length; i++)
            {
                string val = rgszRow[i].Trim();
                if (i == iSecondsOffsetCol)
                    // need to handle this column specially to convert from seconds offset to actual date/time
                    dr[KnownColumnNames.DATE] = baseTime.AddSeconds(val.SafeParseInt());
                else if (columnMap.TryGetValue(i, out KnownColumn kc))
                    dr[kc.Column] = kc.ParseToType(val);
            }

            return dr;
        }

        public override bool Parse(string szData)
        {
            StringBuilder sbErr = new StringBuilder();
            TelemetryDataTable m_dt = ParsedData;
            m_dt.Clear();
            bool fResult = true;

            DateTime dtBase = rDate.Match(szData)?.Groups["datestring"].Value.SafeParseDate(DateTime.MinValue) ?? DateTime.MinValue;
            if (!dtBase.HasValue())
                return false;

            int iSecondsOffsetCol = -1;

            int? utcOffset = null;

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(szData)))
            {
                using (CSVReader csvr = new CSVReader(ms))
                {
                    try
                    {
                        string[] rgszHeader = null;

                        // Find the first row that looks like columns.
                        try
                        {
                            do
                            {
                                rgszHeader = csvr.GetCSVLine(true);
                            } while ((rgszHeader?.Length ?? 0) < 4);
                        }
                        catch (CSVReaderInvalidCSVException ex)
                        {
                            throw new MyFlightbookException(ex.Message);
                        }

                        if (rgszHeader == null) // no valid CSV header found
                        {
                            sbErr.Append(TelemetryResources.errNoCSVHeaderRowFound);
                            return false;
                        }

                        // Set up the columns by index
                        Dictionary<int, KnownColumn> columnIndexMap = new Dictionary<int, KnownColumn>();
                        for (int i = 0; i < rgszHeader.Length; i++)
                        {
                            if (ColumnMap.TryGetValue(rgszHeader[i].Trim(), out KnownColumn kc))
                            {
                                m_dt.Columns.Add(kc.Column, kc.Type.ColumnDataType());
                                columnIndexMap[i] = kc;
                            }
                            if (rgszHeader[i].Trim().Equals("ElapsedSeconds", StringComparison.OrdinalIgnoreCase))
                                iSecondsOffsetCol = i;
                        }
                        // Add a timezone offset column, since the dtBase above is in local time.
                        m_dt.Columns.Add(KnownColumnNames.TZOFFSET, typeof(int));

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
                                DataRow row = ParseCSVRow(rgszRow, columnIndexMap, dtBase, iSecondsOffsetCol);
                                // Issue #1516: use the first sample we find to find the timezone offset; use that for this and all subsequent samples, but only initialize it once.
                                if (utcOffset == null)
                                    utcOffset = (row[KnownColumnNames.LAT] != null && row[KnownColumnNames.LON] != null) ?
                                        (int)AutoFillOptions.UtcFromZone(dtBase, TimeZoneLookup.GetTimeZone((double)row[KnownColumnNames.LAT], (double)row[KnownColumnNames.LON]).Result).Subtract(DateTime.SpecifyKind(dtBase, DateTimeKind.Utc)).TotalMinutes :
                                        0;
                                row[KnownColumnNames.TZOFFSET] = utcOffset.Value;
                                m_dt.Rows.Add(row);
                            }
                            catch (MyFlightbookException ex)
                            {
                                sbErr.Append(String.Format(CultureInfo.CurrentCulture, TelemetryResources.errInRow, iRow, ex.Message));
                                fResult = false;
                            }
                            catch (System.FormatException ex)
                            {
                                sbErr.Append(String.Format(CultureInfo.CurrentCulture, TelemetryResources.errInRow, iRow, ex.Message));
                                fResult = false;
                            }
                        }
                    }
                    catch (MyFlightbookException ex)
                    {
                        sbErr.Append(String.Format(CultureInfo.CurrentCulture, TelemetryResources.errGeneric, ex.Message));
                        fResult = false;
                    }
                    catch (DuplicateNameException ex)
                    {
                        sbErr.Append(String.Format(CultureInfo.CurrentCulture, TelemetryResources.errGeneric, ex.Message));
                        fResult = false;
                    }
                    finally
                    {
                    }
                }
            }

            ErrorString = sbErr.ToString();

            return fResult;
        }
    }
}
