using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using MyFlightbook.Geography;

/******************************************************
 * 
 * Copyright (c) 2010-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Telemetry
{
    /// <summary>
    /// Parses IGC files.  See http://carrier.csi.cam.ac.uk/forsterlewis/soaring/igc_file_format/igc_format_2008.html
    /// </summary>
    public class IGCParser : TelemetryParser
    {
        public IGCParser() : base() { }

        /// <summary>
        /// Looks at the string and decides if it looks like it's an IGC file.  True if it has the requisite prefix and date lines.
        /// </summary>
        /// <param name="sz"></param>
        /// <returns></returns>
        public override bool CanParse(string szData)
        {
            if (string.IsNullOrEmpty(szData))
                return false;

            using (StringReader sr = new StringReader(szData))
            {
                string szFirstLine = sr.ReadLine();
                if (rIGCPrefix.IsMatch(szFirstLine) && rIGCDate.IsMatch(szData))
                    return true;
            }
            return false;
        }

        public override FlightData.AltitudeUnitTypes AltitudeUnits
        {
            get { return FlightData.AltitudeUnitTypes.Meters; }
        }

        public override FlightData.SpeedUnitTypes SpeedUnits
        {
            get { return FlightData.SpeedUnitTypes.KmPerHour; }
        }

        /// <summary>
        /// Parses the passed IGC data to the passed data table
        /// </summary>
        /// <param name="szData">The data to parse.</param>
        /// <returns>True for success</returns>
        public override bool Parse(string szData)
        {
            ParsedData.Clear();

            MatchCollection mcDate = null;
            try
            {
                // check for valid IGC data
                if (String.IsNullOrEmpty(szData))
                    throw new InvalidDataException("No data to parse");
                if (!CanParse(szData))
                    throw new InvalidDataException("Data to parse is not IGC format");

                // Get the base date, utc.
                mcDate = rIGCDate.Matches(szData);
                if (mcDate == null || mcDate.Count > 0 && mcDate[0].Groups.Count == 3)
                    throw new InvalidDataException("IGC Data has no date field as required");
            }
            catch (InvalidDataException ex)
            {
                ErrorString = ex.Message;
                return false;
            }

            GroupCollection gcDate = mcDate[0].Groups;
            int day = Convert.ToInt32(gcDate["day"].Value, CultureInfo.InvariantCulture);
            int month = Convert.ToInt32(gcDate["month"].Value, CultureInfo.InvariantCulture);
            int year = Convert.ToInt32(gcDate["year"].Value, CultureInfo.InvariantCulture);

            int yearNow = DateTime.Now.Year;
            int millenium = (yearNow / 1000) * 1000;
            // fix up a two digit year to be whichever is latest but less than or equal to this year
            year += (year + millenium) <= yearNow ? millenium : millenium - 100;

            DateTime dtBase = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

            int hLast = int.MinValue;
            int dayOffset = 0;

            List<Position> lst = new List<Position>();
            MatchCollection mc = rIGCPosition.Matches(szData);
            foreach (Match match in mc)
            {
                if (match.Groups.Count == 13)
                {
                    GroupCollection gc = match.Groups;
                    int h = Convert.ToInt32(gc["hrs"].Value, CultureInfo.InvariantCulture);
                    int m = Convert.ToInt32(gc["min"].Value, CultureInfo.InvariantCulture);
                    int s = Convert.ToInt32(gc["sec"].Value, CultureInfo.InvariantCulture);

                    int latd = Convert.ToInt32(gc["latd"].Value, CultureInfo.InvariantCulture);
                    int latm = Convert.ToInt32(gc["latm"].Value, CultureInfo.InvariantCulture); // = Minutes x 1000 (MMmmm)
                    int latsign = (gc["latns"].Value.CompareOrdinal("S") == 0) ? -1 : 1;

                    int lond = Convert.ToInt32(gc["lond"].Value, CultureInfo.InvariantCulture);
                    int lonm = Convert.ToInt32(gc["lonm"].Value, CultureInfo.InvariantCulture);
                    int lonsign = (gc["lonew"].Value.CompareOrdinal("W") == 0) ? -1 : 1;

                    bool fHasAlt = gc["val"].Value.CompareOrdinal("A") == 0;
                    int palt = Convert.ToInt32(gc["palt"].Value, CultureInfo.InvariantCulture);
                    int gpsalt = Convert.ToInt32(gc["gpsalt"].Value, CultureInfo.InvariantCulture);

                    // check for the flight going past midnight.
                    if (h < hLast)
                        dayOffset++;
                    hLast = h;

                    // Create the data sample
                    LatLong llSample = new LatLong(latsign * (latd + latm / 60000.0), lonsign * (lond + lonm / 60000.0));
                    DateTime dtSample = dtBase.AddSeconds(h * 3600 + m * 60 + s);
                    int altSample = fHasAlt ? palt : 0;
                    lst.Add(new Position(llSample, altSample, dtSample));
                }
            }

            Position.DeriveSpeed(lst);
            ToDataTable(lst);

            return true;
        }

        private static Regex rIGCPrefix = new Regex("^A[a-zA-Z0-9]{6}[^,;]*$", RegexOptions.Compiled);
        private static Regex rIGCPosition = new Regex("^B(?<hrs>[0-9]{2})(?<min>[0-9]{2})(?<sec>[0-9]{2})(?<latd>[0-9]{2})(?<latm>[0-9]{5})(?<latns>[NS])(?<lond>[0-9]{3})(?<lonm>[0-9]{5})(?<lonew>[EW])(?<val>[AV])(?<palt>[0-9]{5})(?<gpsalt>[0-9]{5})", RegexOptions.Compiled | RegexOptions.Multiline);
        private static Regex rIGCDate = new Regex("^HFDTE(?<day>[0-3][0-9])(?<month>[01][0-9])(?<year>[0-9]{2})", RegexOptions.Compiled | RegexOptions.Multiline);
    }
}