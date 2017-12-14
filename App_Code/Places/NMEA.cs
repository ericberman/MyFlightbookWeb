using MyFlightbook.Geography;
using System;
using System.Data;
using System.Globalization;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2010-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Telemetry
{
    /// <summary>
    /// Parser for NMEA files - format for NMEA can be found at http://www.gpsinformation.org/dale/nmea.htm#GGA
    /// </summary>
    public class NMEAParser : TelemetryParser
    {
        public NMEAParser() : base() { }

        public override bool CanParse(string szData)
        {
            return szData != null && szData.StartsWith("$GP", StringComparison.OrdinalIgnoreCase);
        }

        public override FlightData.AltitudeUnitTypes AltitudeUnits
        {
            get { return FlightData.AltitudeUnitTypes.Meters; }
        }

        public override FlightData.SpeedUnitTypes SpeedUnits
        {
            get { return FlightData.SpeedUnitTypes.Knots; }
        }

        public override bool Parse(string szData)
        {
            if (szData == null)
                throw new ArgumentNullException("szData");

            Boolean fResult = true;
            StringBuilder sbErr = new StringBuilder();

            /*
    GGA - essential fix data which provide 3D location and accuracy data.

             $GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*47

            Where:
                 GGA          Global Positioning System Fix Data
                 123519       Fix taken at 12:35:19 UTC
                 4807.038,N   Latitude 48 deg 07.038' N
                 01131.000,E  Longitude 11 deg 31.000' E
                 1            Fix quality: 0 = invalid
                                           1 = GPS fix (SPS)
                                           2 = DGPS fix
                                           3 = PPS fix
                               4 = Real Time Kinematic
                               5 = Float RTK
                                           6 = estimated (dead reckoning) (2.3 feature)
                               7 = Manual input mode
                               8 = Simulation mode
                 08           Number of satellites being tracked
                 0.9          Horizontal dilution of position
                 545.4,M      Altitude, Meters, above mean sea level
                 46.9,M       Height of geoid (mean sea level) above WGS84
                                  ellipsoid
                 (empty field) time in seconds since last DGPS update
                 (empty field) DGPS station ID number
                 *47          the checksum data, always begins with *

     
            RMC - NMEA has its own version of essential gps pvt (position, velocity, time) data. It is called RMC, The Recommended Minimum, which will look similar to:

            $GPRMC,123519,A,4807.038,N,01131.000,E,022.4,084.4,230394,003.1,W*6A

            Where:
                 RMC          Recommended Minimum sentence C
                 123519       Fix taken at 12:35:19 UTC
                 A            Status A=active or V=Void.
                 4807.038,N   Latitude 48 deg 07.038' N
                 01131.000,E  Longitude 11 deg 31.000' E
                 022.4        Speed over the ground in knots
                 084.4        Track angle in degrees True
                 230394       Date - 23rd of March 1994
                 003.1,W      Magnetic Variation
                 *6A          The checksum data, always begins with *

             * */

            char[] wordSep = { ',' };
            char[] lineSep = { '\n' };
            ParsedData.Clear();
            try
            {
                string[] rgSentences = szData.Split(lineSep, StringSplitOptions.RemoveEmptyEntries);

                double alt = 0.0;
                ParsedData.Columns.Add(new DataColumn(KnownColumnNames.DATE, typeof(DateTime)));
                ParsedData.Columns.Add(new DataColumn(KnownColumnNames.ALT, typeof(Int32)));
                ParsedData.Columns.Add(new DataColumn(KnownColumnNames.LON, typeof(double)));
                ParsedData.Columns.Add(new DataColumn(KnownColumnNames.LAT, typeof(double)));
                ParsedData.Columns.Add(new DataColumn(KnownColumnNames.SPEED, typeof(double)));

                CultureInfo ci = System.Globalization.CultureInfo.InvariantCulture;

                int iSentence = 0;
                foreach (string sentence in rgSentences)
                {
                    try
                    {
                        string[] rgWords = sentence.Split(wordSep, StringSplitOptions.None);
                        if (String.Compare(rgWords[0], "$GPGGA", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            if (String.Compare(rgWords[6], "0", StringComparison.Ordinal) != 0) // is this valid?
                                alt = Convert.ToDouble(rgWords[9], CultureInfo.InvariantCulture) * ConversionFactors.FeetPerMeter;
                        }
                        else if (String.Compare(rgWords[0], "$GPRMC", StringComparison.Ordinal) == 0) // actual sample
                        {
                            string szTime = rgWords[1];
                            string szDate = rgWords[9];

                            int year2digit = Convert.ToInt16(szDate.Substring(4), CultureInfo.InvariantCulture);
                            int curYear = DateTime.Now.Year;
                            int year20thCentury = 1900 + year2digit;
                            int year21stCentury = 2000 + year2digit;
                            int year = (Math.Abs(curYear - year20thCentury) < Math.Abs(curYear - year21stCentury)) ? year20thCentury : year21stCentury;

                            DataRow dr = ParsedData.NewRow();

                            DateTime dt = new DateTime(year,
                                Convert.ToInt16(szDate.Substring(2, 2), ci),
                                Convert.ToInt16(szDate.Substring(0, 2), ci),
                                Convert.ToInt16(szTime.Substring(0, 2), ci),
                                Convert.ToInt16(szTime.Substring(2, 2), ci),
                                Convert.ToInt16(szTime.Substring(4, 2), ci),
                                DateTimeKind.Utc);

                            dr[KnownColumnNames.DATE] = dt;
                            dr[KnownColumnNames.ALT] = alt;
                            dr[KnownColumnNames.LAT] = (Convert.ToDouble(rgWords[3].Substring(0, 2), ci) + (Convert.ToDouble(rgWords[3].Substring(2), ci) / 60.0)) * ((String.Compare(rgWords[4], "N", StringComparison.OrdinalIgnoreCase) == 0) ? 1 : -1);
                            dr[KnownColumnNames.LON] = (Convert.ToDouble(rgWords[5].Substring(0, 3), ci) + (Convert.ToDouble(rgWords[5].Substring(3), ci) / 60.0)) * ((String.Compare(rgWords[6], "E", StringComparison.OrdinalIgnoreCase) == 0) ? 1 : -1);
                            dr[KnownColumnNames.SPEED] = Convert.ToDouble(rgWords[7], ci);

                            ParsedData.Rows.Add(dr);
                        }
                    }
                    catch (System.FormatException ex)
                    {
                        sbErr.AppendFormat(CultureInfo.CurrentCulture, Resources.FlightData.errInRow, iSentence, ex.Message);
                        fResult = false;
                    }
                    catch (System.IndexOutOfRangeException ex)
                    {
                        sbErr.AppendFormat(CultureInfo.CurrentCulture, Resources.FlightData.errInRow, iSentence, ex.Message);
                        fResult = false;
                    }
                    catch (Exception ex)
                    {
                        MyFlightbookException.NotifyAdminException(ex);
                        throw;
                    }

                    iSentence++;
                }
            }
            catch (Exception ex)
            {
                MyFlightbookException.NotifyAdminException(ex);
                throw;
            }

            ErrorString = sbErr.ToString();
            return fResult;
        }
    }
}