using System;
using System.Globalization;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2015-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Geography
{
    /// <summary>
    /// Represents an angle in degrees/minutes/seconds
    /// </summary>
    public class DMSAngle
    {
        public int Degrees { get; set; }
        public int Minutes { get; set; }
        public double Seconds { get; set; }
        public int Sign { get; set; }
        public enum DisplayType { Decimal, Latitude, Longitude }

        private void InitFromAngle(double angle)
        {
            Sign = (angle < 0) ? -1 : 1;
            angle = Math.Abs(angle);
            Degrees = (int)Math.Floor(angle);
            double dmin = (angle - Degrees) * 60.0;
            Minutes = (int)Math.Floor(dmin);
            Seconds = (dmin - Minutes) * 60.0;
        }

        public DMSAngle(double angle)
        {
            InitFromAngle(angle);
        }

        public DMSAngle(string szAngleString)
        {
            try
            {
                // Try matching on DMS like ("22 03' 26.123"S)
                MatchCollection mc = RegexUtility.DMSNumeric.Matches(szAngleString);
                if (mc != null && mc.Count > 0 && mc[0].Groups.Count >= 5)
                {
                    GroupCollection gc = mc[0].Groups;
                    Degrees = Convert.ToInt32(gc[1].Value, CultureInfo.InvariantCulture);
                    Minutes = Convert.ToInt32(gc[2].Value, CultureInfo.InvariantCulture);
                    Seconds = Convert.ToDouble(gc[3].Value, CultureInfo.InvariantCulture);
                    Sign = (gc[4].Value.StartsWith("N", StringComparison.CurrentCultureIgnoreCase) || gc[4].Value.StartsWith("E", StringComparison.CurrentCultureIgnoreCase)) ? 1 : -1;
                    return;
                }
                // Else, try matching on decimal ("22.5483 S 27.863E")
                mc = RegexUtility.DMSDecimal.Matches(szAngleString);
                if (mc != null && mc.Count > 0 && mc[0].Groups.Count >= 3)
                {
                    GroupCollection gc = mc[0].Groups;
                    if (!String.IsNullOrEmpty(gc[3].Value) && !String.IsNullOrEmpty(gc[1].Value))
                    {
                        Sign = (gc[3].Value.StartsWith("N", StringComparison.CurrentCultureIgnoreCase) || gc[3].Value.StartsWith("E", StringComparison.CurrentCultureIgnoreCase)) ? 1 : -1;
                        double angle = Sign * Math.Abs(Convert.ToDouble(gc[1].Value, CultureInfo.CurrentCulture));
                        InitFromAngle(angle);
                        return;
                    }
                }
                // Else, try matching on decimal ("W122.23.15")
                mc = RegexUtility.DMSDotted.Matches(szAngleString);
                if (mc != null && mc.Count > 0 && mc[0].Groups.Count >= 4)
                {
                    GroupCollection gc = mc[0].Groups;
                    Sign = (gc[1].Value.StartsWith("N", StringComparison.CurrentCultureIgnoreCase) || gc[0].Value.StartsWith("E", StringComparison.CurrentCultureIgnoreCase)) ? 1 : -1;
                    Degrees = Convert.ToInt32(gc[2].Value, CultureInfo.InvariantCulture);
                    Minutes = Convert.ToInt32(gc[3].Value, CultureInfo.InvariantCulture);
                    Seconds = Convert.ToDouble(gc[4].Value, CultureInfo.InvariantCulture);
                    return;
                }
                // Finally, try matching on degrees and minutes ("48°01.3358")
                mc = RegexUtility.DMSDegrees.Matches(szAngleString);
                if (mc != null && mc.Count > 0 && mc[0].Groups.Count >= 3)
                {
                    GroupCollection gc = mc[0].Groups;
                    Sign = (szAngleString != null && szAngleString.StartsWith("-", StringComparison.CurrentCultureIgnoreCase)) ? -1 : 1;
                    Degrees = Convert.ToInt32(gc[1].Value, CultureInfo.InvariantCulture);
                    double min = Convert.ToDouble(gc[2].Value, CultureInfo.InvariantCulture);
                    Minutes = (int)Math.Truncate(min);
                    Seconds = Math.Round((min - Minutes) * 60);
                    return;
                }
            }
            catch (Exception ex) when (ex is FormatException) { }

            // Default value
            Degrees = Minutes = 0;
            Seconds = 0;
            Sign = 1;
        }

        /// <summary>
        /// Tries to return a latlon from a DMS string (e.g., 42° 37' 14" N 32° 14' 16" E)
        /// </summary>
        /// <param name="sz"></param>
        /// <returns>Null if there is a problem.</returns>
        static public LatLong LatLonFromDMSString(string sz)
        {
            MatchCollection mc = RegexUtility.DMSLatLong.Matches(sz);
            if (mc != null && mc.Count > 0 && mc[0].Groups.Count >= 2)
                return new LatLong((new DMSAngle(mc[0].Groups[1].Value)).Value, (new DMSAngle(mc[0].Groups[2].Value)).Value);
            else if (CoordinateSharp.MilitaryGridReferenceSystem.TryParse(sz, out CoordinateSharp.MilitaryGridReferenceSystem mgrs))
            {
                CoordinateSharp.Coordinate c = CoordinateSharp.MilitaryGridReferenceSystem.MGRStoLatLong(mgrs);
                return new LatLong(c.Latitude.DecimalDegree, c.Longitude.DecimalDegree);
            }

            return null;
        }

        public double Value
        {
            get { return Sign * (Degrees + (Minutes / 60.0) + (Seconds / 3600.0)); }
            set { InitFromAngle(value); }
        }

        public string ToString(DisplayType dt)
        {
            return String.Format(CultureInfo.CurrentCulture, "{3}{0}° {1}' {2:F3}\"{4}",
                Degrees,
                Minutes,
                Seconds,
                (Sign < 0 && dt == DisplayType.Decimal) ? "-" : string.Empty,
                (Sign > 0 ? (dt == DisplayType.Latitude ? " N" : " E") : (dt == DisplayType.Latitude ? " S" : " W")));
        }

        public override string ToString()
        {
            return ToString(DisplayType.Decimal);
        }
    }
}
