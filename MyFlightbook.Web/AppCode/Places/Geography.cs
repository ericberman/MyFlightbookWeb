using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Geography
{
    public static class ConversionFactors
    {
        public const double FeetPerMeter = 3.28084;
        public const double MetersPerFoot = 0.3048;
        public const double MetersPerSecondPerKnot = 0.514444444;
        public const double MetersPerSecondPerMilesPerHour = 0.44704;
        public const double MetersPerSecondPerKmPerHour = 0.277778;
    }

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
                // Try matching on DMS ("22 03' 26.123"S)
                MatchCollection mc = Regex.Matches(szAngleString, "(\\d{1,3})\\D+([0-5]?\\d)\\D+(\\d+\\.?\\d*)\\D*([NEWS])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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
                mc = Regex.Matches(szAngleString, "(\\d{0,3}([,.]\\d+)?)\\D*([NEWS])", RegexOptions.Compiled);
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
                mc = Regex.Matches(szAngleString, "([NEWSnews])[ .]?(\\d{0,3})[ .]?(\\d{0,2})[ .]?(\\d{0,2})",  RegexOptions.Compiled);
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
                mc = Regex.Matches(szAngleString, "-?(\\d+)°(\\d+([.,]\\d+)?)", RegexOptions.Compiled);
                if (mc != null && mc.Count > 0 && mc[0].Groups.Count >= 3)
                {
                    GroupCollection gc = mc[0].Groups;
                    Sign = (szAngleString != null && szAngleString.StartsWith("-", StringComparison.CurrentCultureIgnoreCase)) ? -1 : 1;
                    Degrees = Convert.ToInt32(gc[1].Value, CultureInfo.InvariantCulture);
                    double min = Convert.ToDouble(gc[2].Value, CultureInfo.InvariantCulture);
                    Minutes = (int) Math.Truncate(min);
                    Seconds = Math.Round((min - Minutes) * 60);
                    return;
                }
            }
            catch (FormatException)
            { }

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
            Regex r = new Regex("([^a-zA-Z]+[NS]) *([^a-zA-Z]+[EW])", RegexOptions.IgnoreCase);
            MatchCollection mc = r.Matches(sz);
            if (mc != null && mc.Count > 0 && mc[0].Groups.Count >= 2)
                return new LatLong((new DMSAngle(mc[0].Groups[1].Value)).Value, (new DMSAngle(mc[0].Groups[2].Value)).Value);
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

    [Serializable]
    public class LatLong
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        #region constructors
        public LatLong(double lat, double lng)
        {
            Latitude = lat;
            Longitude = lng;
        }

        public LatLong()
        {
            Latitude = Longitude = 0.0;
        }

        public LatLong(LatLong ll)
        {
            if (ll == null)
                throw new ArgumentNullException("ll");
            Latitude = ll.Latitude;
            Longitude = ll.Longitude;
        }
        #endregion

        private readonly static Regex regPosition = new Regex("([NnSs]) ?(\\d{1,2}) (\\d{0,2}(?:.\\d*)?) ([EeWw]) ?(\\d{1,3}) (\\d{0,2}(?:.\\d*)?)", RegexOptions.Compiled);
        /// <summary>
        /// Creates a LatLong from a position string in the flight data format N 46 34.345 W 122 43.34
        /// </summary>
        /// <param name="szPosition"></param>
        public LatLong(string szPosition)
        {
            MatchCollection mc = regPosition.Matches(szPosition);
            // should have 1 match with 7 groups: the whole string, N/S, deg, minutes, EW, deg, minutes
            if (mc.Count == 0 || mc[0].Groups.Count != 7)
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.errBadPosition, szPosition));
            GroupCollection g = mc[0].Groups;
            Latitude = ((String.Compare(g[1].Value, "S", StringComparison.CurrentCultureIgnoreCase) == 0) ? -1 : 1) * (Convert.ToDouble(g[2].Value, CultureInfo.CurrentCulture) + (Convert.ToDouble(g[3].Value, CultureInfo.CurrentCulture) / 60.0));
            Longitude = ((String.Compare(g[4].Value, "W", StringComparison.CurrentCultureIgnoreCase) == 0) ? -1 : 1) * (Convert.ToDouble(g[5].Value, CultureInfo.CurrentCulture) + (Convert.ToDouble(g[6].Value, CultureInfo.CurrentCulture) / 60.0));
        }

        /// <summary>
        /// Is the latitude a valid value?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsValidLatitude
        {
            get { return (Latitude <= 90.0 && Latitude >= -90.0); }
        }

        /// <summary>
        /// Is the longitude a valid value?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsValidLongitude
        {
            get { return (Longitude >= -180.0 && Longitude <= 180.0); }
        }

        /// <summary>
        /// Validate the LatLong
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsValid
        {
            get { return IsValidLatitude && IsValidLongitude; }
        }

        /// <summary>
        /// Determines if this latlong and another one are essentially the same location, within a specified tolerance
        /// </summary>
        /// <param name="ll">the latlong to compare to</param>
        /// <param name="tolerance">The tolerance for equality, must be > 0</param>
        /// <returns>true if they are within the specified tolerance of one another</returns>
        public bool IsSameLocation(LatLong ll, double tolerance = 0.001)
        {
            return ll != null && (Math.Abs(ll.Latitude - this.Latitude) < tolerance) && (Math.Abs(ll.Longitude - this.Longitude) < tolerance);
        }

        /// <summary>
        /// Determines if two latlong's are equal, treating (0,0) as a null lat/lon.  Either can be null.
        /// </summary>
        /// <param name="ll1">The first latlon</param>
        /// <param name="ll2">The 2nd latlon</param>
        /// <returns>True if they are the same</returns>
        public static bool AreEqual(LatLong ll1, LatLong ll2)
        {
            // Treat 0,0 as a null latlong
            if (ll1 != null && ll1.Latitude == 0 && ll1.Longitude == 0)
                ll1 = null;
            if (ll2 != null && ll2.Latitude == 0 && ll2.Longitude == 0)
                ll1 = null;

            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(ll1, ll2) || (ll1 == null && ll2 == null))
                return true;

            if ((ll1 == null) ^ (ll2 == null))
                return false;

            // Tests above have already tested for 0 or 1 of them being null, so both must be non-null here.
            return ll1.IsSameLocation(ll2);
        }

        /// <summary>
        /// Error string, empty if things are valid.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string ValidationError
        {
            get
            {
                if (!IsValidLatitude)
                    return Resources.Airports.errInvalidLatitude;
                if (!IsValidLongitude)
                    return Resources.Airports.errInvalidLongitude;
                return "";
            }
        }

        /// <summary>
        /// Returns a string representing the value using invariant culture (i.e., period for decimal point)
        /// </summary>
        /// <param name="d">Double value</param>
        /// <returns>The string</returns>
        static public string InvariantString(double d)
        {
            return d.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Invariant culture string representation of latitude as a floating point number
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string LatitudeString
        {
            get { return InvariantString(Latitude); }
        }

        /// <summary>
        /// Invariant culture string representation of longitude as a floating point number
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string LongitudeString
        {
            get { return InvariantString(Longitude); }
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}, {1}", Latitude, Longitude);
        }

        /// <summary>
        /// Formats the latitude/longitude into degrees/minutes/seconds and N/S/E/W
        /// </summary>
        /// <returns>The formatted string</returns>
        public string ToDegMinSecString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}, {1}", new DMSAngle(Latitude).ToString(DMSAngle.DisplayType.Latitude), new DMSAngle(Longitude).ToString(DMSAngle.DisplayType.Longitude));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ToAdhocFixString()
        {
            return String.Format(CultureInfo.CurrentCulture, "@{0:F8}{1}{2:F8}{3}", Math.Abs(Latitude), Latitude > 0 ? "N" : "S", Math.Abs(Longitude), Longitude > 0 ? "E" : "W");
        }

        /// <summary>
        /// Distance, in NM, between two coordinates
        /// </summary>
        /// <param name="lat1"></param>
        /// <param name="lon1"></param>
        /// <param name="lat2"></param>
        /// <param name="lon2"></param>
        /// <returns>Distance, in NM</returns>
        public static double DistanceBetweenPoints(LatLong ll1, LatLong ll2)
        {
            if (ll1 == null)
                throw new ArgumentNullException("ll1");
            if (ll2 == null)
                throw new ArgumentNullException("ll2");

            // Short circuit 0 distance
            if (ll1.Latitude == ll2.Latitude && ll1.Longitude == ll2.Longitude)
                return 0.0;

            // And short circuit nonsense values
            if (!ll1.IsValid || !ll2.IsValid)
                return double.NaN;

            double rlat1 = Math.PI * (ll1.Latitude / 180.0);
            double rlon1 = Math.PI * (ll1.Longitude / 180.0);
            double rlat2 = Math.PI * (ll2.Latitude / 180.0);
            double rlon2 = Math.PI * (ll2.Longitude / 180.0);

            double d = Math.Acos(Math.Sin(rlat1) * Math.Sin(rlat2) + Math.Cos(rlat1) * Math.Cos(rlat2) * Math.Cos(rlon2 - rlon1)) * 3440.06479;
            return double.IsNaN(d) ? 0 : d;
        }

        /// <summary>
        /// Distance, in NM, between this coordinate and another one.
        /// </summary>
        /// <param name="ll">The distance</param>
        /// <returns>The distance, in nautical miles</returns>
        public double DistanceFrom(LatLong ll)
        {
            return DistanceBetweenPoints(this, ll);
        }

        /// <summary>
        /// Takes a latitude, longitude, and culture and tries to parse it.  Returns a latlong if successful, else null.
        /// </summary>
        /// <param name="szLat">latitude - can be double or string</param>
        /// <param name="szLon">longitude - can be double or string</param>
        /// <param name="ci">Culture to provide, current culture if none</param>
        /// <returns>A latlon object, or null</returns>
        public static LatLong TryParse(object szLat, object szLon, IFormatProvider ci = null)
        {
            double lat, lon;

            if (szLat == null)
                throw new ArgumentNullException("szLat");
            if (szLon == null)
                throw new ArgumentNullException("szLon");

            if (szLat.GetType() == typeof(double) && szLon.GetType() == typeof(double))
                return new LatLong((double)szLat, (double)szLon);

            if (ci == null)
                ci = CultureInfo.CurrentCulture;

            if (double.TryParse(szLat.ToString(), System.Globalization.NumberStyles.Any, ci, out lat) && double.TryParse(szLon.ToString(), System.Globalization.NumberStyles.Any, ci, out lon))
                return new LatLong(lat, lon);
            return null;
        }

        /// <summary>
        /// Return the antipode (opposite side of the planet) coordinate for this point.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [System.Web.Script.Serialization.ScriptIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public LatLong Antipode
        {
            get { return new LatLong(-Latitude, Longitude > 0 ? Longitude - 180.0 : Longitude + 180.0); }
        }
    }

    /// <summary>
    /// Position report: LatLong plus optional altitude and optional timestamp.
    /// </summary>
    [Serializable]
    public class Position : LatLong, IComparable
    {
        private double m_Altitude;
        private double m_Speed;
        private DateTime m_dt = DateTime.MinValue;
        private bool m_HasAltitude = false;
        private bool m_HasSpeed = false;
        private bool m_HasTimestamp = false;

        public enum SpeedType { Reported, Derived }

        #region properties
        /// <summary>
        /// Check to see if there is a valid altitude
        /// </summary>
        public bool HasAltitude { get { return m_HasAltitude; } }

        /// <summary>
        /// Check to see if there is a valid speed
        /// </summary>
        public bool HasSpeed { get { return m_HasSpeed; } }

        /// <summary>
        /// Check for a valid timestamp
        /// </summary>
        public bool HasTimeStamp { get { return m_HasTimestamp; } }

        /// <summary>
        /// Is the speed reported or derived?
        /// </summary>
        public SpeedType TypeOfSpeed { get; set; }

        /// <summary>
        /// The altitude for the position (units unspecified)
        /// </summary>
        public double Altitude
        {
            get { return m_Altitude; }
            set { m_Altitude = value; m_HasAltitude = true; }
        }

        /// <summary>
        /// Timestamp, if any, for this position report.
        /// </summary>
        public DateTime Timestamp
        {
            get { return m_dt; }
            set { m_dt = value; m_HasTimestamp = true; }
        }

        /// <summary>
        /// Speed for this position report
        /// </summary>
        public double Speed
        {
            get { return m_Speed; }
            set { m_Speed = value; m_HasSpeed = true; }
        }

        /// <summary>
        /// Optional comment.
        /// </summary>
        public string Comment { get; set; }
        #endregion

        #region Constructors
        public Position(double Lat = 0, double Lon = 0)
            : base(Lat, Lon)
        {
        }

        public Position(double Lat, double Lon, DateTime timeStamp)
            : base(Lat, Lon)
        {
            Timestamp = timeStamp;
        }

        public Position(LatLong ll, double alt = 0) : base(ll)
        {
            Altitude = alt;
        }

        public Position(LatLong ll, double alt, DateTime timestamp, double speed = 0) : base(ll)
        {
            Altitude = alt;
            Speed = speed;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Initializes from a KML coordinate sample, already parsed into an array
        /// </summary>
        /// <param name="rgCoord">First two strings are the longitude, then latitude (in that order).  3rd, if present, is altitude</param>
        public Position(string[] rgCoord)
        {
            if (rgCoord == null || rgCoord.Length < 2)
                throw new MyFlightbookException("Error reading KML coordinate: need at least a latitude and a longitude, both were not provided"); ;

            double lat, lon, alt;
            if (double.TryParse(rgCoord[0], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out lon) &&
                double.TryParse(rgCoord[1], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out lat))
            {
                Latitude = lat;
                Longitude = lon;
            }
            else
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Error reading KML coordinate: {0} {1} {2}", rgCoord[0], rgCoord[1], rgCoord.Length > 2 ? rgCoord[2] : string.Empty));

            if (rgCoord.Length > 2)
            {
                if (double.TryParse(rgCoord[2], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out alt))
                    Altitude = alt;
                else
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Error reading altitude '{0}'", rgCoord[2]));
            }
        }
        #endregion

        /// <summary>
        /// Goes through a list of GPSSamples and computes a derived speed.
        /// </summary>
        /// <param name="lst">A list of ordered GPSSamples</param>
        /// <param name="minRefresh">Minimum time (in seconds) between refresh of speed.  Too small can lead to spiky data</param>
        public static void DeriveSpeed(IEnumerable<Position> l, double minRefresh = 4.0)
        {
            if (l == null)
                return;

            List<Position> lst = new List<Position>(l);
            if (lst.Count == 0)
                return;

            int iSpeedRef = 0;
            double speedLast = 0.0;
            Position sampRef;

            for (int iSample = 0; iSample < lst.Count; iSample++)
            {
                Position samp = lst[iSample];

                if (!samp.HasTimeStamp) // Can't compute speed without a time stamp
                    continue;

                // Find the next reference speed to use
                for (int iRefNew = iSpeedRef + 1; iRefNew < iSample; iRefNew++)
                {
                    sampRef = lst[iRefNew];
                    if (!sampRef.HasTimeStamp)
                        continue;

                    if (samp.Timestamp.Subtract(sampRef.Timestamp).TotalSeconds > minRefresh)
                        iSpeedRef++;
                }

                sampRef = lst[iSpeedRef];
                samp.TypeOfSpeed = SpeedType.Derived;
                if (iSample <= iSpeedRef)
                    samp.Speed = speedLast;
                else
                {
                    double distInNM = sampRef.DistanceFrom(samp);
                    double timeInHours = samp.Timestamp.Subtract(sampRef.Timestamp).TotalHours;
                    speedLast = samp.Speed = (timeInHours > 0) ? distInNM / timeInHours : speedLast;
                }
            }
        }

        /// <summary>
        /// Orders by date, if date is present; order is otherwise undefined
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            Position samp = (Position)obj;
            if (this.HasTimeStamp && samp.HasTimeStamp)
                return this.Timestamp.CompareTo(samp.Timestamp);
            else
                return 0;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} - {1}{2}{3} {4}",
                this.ToDegMinSecString(),
                HasTimeStamp ? Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) : "(no time)",
                HasSpeed ? String.Format(CultureInfo.InvariantCulture, " {0:F1}kts {1}", Speed, TypeOfSpeed.ToString()) : string.Empty,
                HasAltitude ? String.Format(CultureInfo.InvariantCulture, " Alt:{0}", Altitude) : string.Empty,
                Comment);
        }

        /// <summary>
        /// Returns a synthesized path between two points, even spacing, between the two timestamps.
        /// 
        /// Can be used to estimate night flight, for example, or draw a great-circle path between two points.
        /// 
        /// From http://www.movable-type.co.uk/scripts/latlong.html
        /// Formula: 	
        ///     a = sin((1−f)⋅δ) / sin δ
        ///     b = sin(f⋅δ) / sin δ
        ///     x = a ⋅ cos φ1 ⋅ cos λ1 + b ⋅ cos φ2 ⋅ cos λ2
        ///     y = a ⋅ cos φ1 ⋅ sin λ1 + b ⋅ cos φ2 ⋅ sin λ2
        ///     z = a ⋅ sin φ1 + b ⋅ sin φ2
        ///     φi = atan2(z, √x² + y²)
        ///     λi = atan2(y, x)
        /// where f is fraction along great circle route (f=0 is point 1, f=1 is point 2), δ is the angular distance d/R between the two points.
        /// </summary>
        /// <param name="llStart">Starting position</param>
        /// <param name="dtStart">Starting date, in UTC</param>
        /// <param name="llEnd">Ending Position</param>
        /// <param name="dtEnd">Ending date, in UTC</param>
        /// <returns>Intermediate time-stamped points, with 1-minute resolution  Speed is derived.</returns>
        public static IEnumerable<Position> SynthesizePath(LatLong llStart, DateTime dtStart, LatLong llEnd, DateTime dtEnd)
        {
            if (llStart == null)
                throw new ArgumentNullException("llStart");
            if (llEnd == null)
                throw new ArgumentNullException("llEnd");

            List<Position> lst = new List<Position>();

            if (!dtStart.HasValue() || !dtEnd.HasValue())
                return lst;

            double rlat1 = Math.PI * (llStart.Latitude / 180.0);
            double rlon1 = Math.PI * (llStart.Longitude / 180.0);
            double rlat2 = Math.PI * (llEnd.Latitude / 180.0);
            double rlon2 = Math.PI * (llEnd.Longitude / 180.0);

            double dLon = rlon2 - rlon1;

            double delta = Math.Atan2(Math.Sin(dLon) * Math.Cos(rlat2), Math.Cos(rlat1) * Math.Sin(rlat2) - Math.Sin(rlat1) * Math.Cos(rlat2) * Math.Cos(dLon));
            // double delta = 2 * Math.Asin(Math.Sqrt(Math.Pow((Math.Sin((rlat1 - rlat2) / 2)), 2) + Math.Cos(rlat1) * Math.Cos(rlat2) * Math.Pow(Math.Sin((rlon1 - rlon2) / 2), 2)));
            double sin_delta = Math.Sin(delta);

            // Compute path at 1-minute intervals, subtracting off one minute since we'll add a few "full-stop" samples below.
            TimeSpan ts = dtEnd.Subtract(dtStart);
            double minutes = ts.TotalMinutes - 1;

            if (minutes > 48 * 60 || minutes <= 0)  // don't do paths more than 48 hours, or negative times.
                return lst;

            // Add a few stopped fields at the end to make it clear that there's a full-stop.  Separate them by a few seconds each.
            Position[] rgPadding = new Position[]
            {
                new Position(llEnd, 0, dtEnd.AddSeconds(3), 0),
                new Position(llEnd, 0, dtEnd.AddSeconds(6), 0),
                new Position(llEnd, 0, dtEnd.AddSeconds(9), 0)
            };

            // We need to derive an average speed.  But no need to compute - just assume constant speed.
            double distance = llStart.DistanceFrom(llEnd);
            if (distance < 1.0) // don't compute path for distance < 1nm - just do endpoints and a high speed so we register a takeoff - probably pattern work.
            {
                lst.AddRange(new Position[]
                {
                    new Position(llStart, 0, dtStart, 150),
                    new Position(llEnd, 0, dtEnd, 150)
                });
                lst.AddRange(rgPadding);
                return lst;
            }

            double speed = ConversionFactors.MetersPerSecondPerKnot * (distance / ts.TotalHours);

            lst.Add(new Position(llStart, 0, dtStart, speed));

            for (long minute = 0; minute <= minutes; minute++)
            {
                double f = ((double)minute) / minutes;
                double a = Math.Sin((1.0 - f) * delta) / sin_delta;
                double b = Math.Sin(f * delta) / sin_delta;
                double x = a * Math.Cos(rlat1) * Math.Cos(rlon1) + b * Math.Cos(rlat2) * Math.Cos(rlon2);
                double y = a * Math.Cos(rlat1) * Math.Sin(rlon1) + b * Math.Cos(rlat2) * Math.Sin(rlon2);
                double z = a * Math.Sin(rlat1) + b * Math.Sin(rlat2);

                double rlat = Math.Atan2(z, Math.Sqrt(x * x + y * y));
                double rlon = Math.Atan2(y, x);

                double dlat = 180 * (rlat / Math.PI);
                double dlon = 180 * (rlon / Math.PI);
                Position p = new Position(dlat, dlon, dtStart.AddMinutes(minute)) { Speed = speed } ;
                lst.Add(p);
            }

            lst.AddRange(rgPadding);

            return lst;
        }
    }

    [Serializable]
    public class LatLongBox
    {
        double LongEast;
        double LongWest;
        double LatNorth;
        double LatSouth;

        #region properties
        /// <summary>
        /// Create a new latlongbox, this one being just a single point
        /// </summary>
        /// <param name="l">The latitude/longitude to expand from</param>
        public LatLongBox(LatLong l)
        {
            if (l == null)
                throw new ArgumentNullException("l");
            LongEast = LongWest = l.Longitude;
            LatNorth = LatSouth = l.Latitude;
        }

        public double Width
        {
            get { return LongEast - LongWest; }
        }

        public double Height
        {
            get { return LatNorth - LatSouth; }
        }

        public double LatMax
        {
            get { return LatNorth; }
            set { LatNorth = value; }
        }

        public double LatMin
        {
            get { return LatSouth; }
            set { LatSouth = value; }
        }

        public double LongMax
        {
            get { return LongEast; }
            set { LongEast = value; }
        }

        public double LongMin
        {
            get { return LongWest; }
            set { LongWest = value; }
        }

        public bool IsEmpty
        {
            get { return Width == 0.0 && Height == 0.0; }
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "Lat: {0:F2} to {1:F2}, Lon: {2:F2} to {3:F2}", LatSouth, LatNorth, LongEast, LongWest);
        }

        /// <summary>
        /// True if this contains the specified latlong point
        /// </summary>
        /// <param name="ll">The LatLong to test</param>
        /// <returns>True if the point is within the latlongbox</returns>
        public bool ContainsPoint(LatLong ll)
        {
            if (ll == null)
                throw new ArgumentNullException("ll");

            // Reject if it doesn't fall between north/south
            if (ll.Latitude > LatNorth || ll.Latitude < LatSouth)
                return false;

            // If LongEast > LongWest, then this doesn't cross the international date line and we can simply test for w <= x <= e
            // Otherwise, we can test for x >= e OR x <= w (i.e., not contained)
            bool fContainedEastWest = ll.Longitude >= LongWest && ll.Longitude <= LongEast;

            return (LongEast >= LongWest) ? fContainedEastWest : !fContainedEastWest;
        }

        public LatLongBox Inflate(double dx)
        {
            LongEast += dx;
            LongWest -= dx;
            LatNorth += dx;
            LatSouth -= dx;
            return this;
        }

        /// <summary>
        /// Expands the bounding box to include the specified latitude longitude
        /// </summary>
        /// <param name="l">The latlong to include</param>
        public void ExpandToInclude(LatLong l)
        {
            if (l == null)
                throw new ArgumentNullException("l");
            if (LatNorth < l.Latitude)
                LatNorth = l.Latitude;
            if (LatSouth > l.Latitude)
                LatSouth = l.Latitude;
            if (LongEast < l.Longitude)
                LongEast = l.Longitude;
            if (LongWest > l.Longitude)
                LongWest = l.Longitude;
        }
    }

    /// <summary>
    /// See https://gist.github.com/shinyzhu/4617989
    /// </summary>
    [Serializable]
    public class GoogleEncodedPath
    {
        #region Properties
        /// <summary>
        /// The encoded path
        /// </summary>
        public string EncodedPath { get; set; }

        /// <summary>
        /// The bounding box for the path.  CAN BE NULL IF PARSEPATH HASN'T BEEN CALLED
        /// </summary>
        public LatLongBox Box { get; set; }

        /// <summary>
        /// The distance covered by the path, in NM
        /// </summary>
        public double Distance { get; set; }
        #endregion

        #region Object Creation
        public GoogleEncodedPath()
        {
            Box = null;
            Distance = 0.0;
            EncodedPath = string.Empty;
        }

        public GoogleEncodedPath(IEnumerable<LatLong> rgll) : this()
        {
            ParsePath(rgll);
        }
        #endregion

        #region Display
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "Distance: {0:#,###.#nm}, Length: {1:#,###}", Distance, String.IsNullOrEmpty(EncodedPath) ? 0 : EncodedPath.Length);
        }
        #endregion

        #region Encoding/Decoding
        /// <summary>
        /// Encodes a latitude or longitude.
        /// see http://code.google.com/apis/maps/documentation/polylinealgorithm.html
        /// or https://developers.google.com/maps/documentation/utilities/polylinealgorithm
        /// </summary>
        /// <param name="d">The point to encode</param>
        /// <returns>A string representing the encoded point</returns>
        protected static string EncodeLatLong(double d)
        {
            StringBuilder sb = new StringBuilder();
            // Step 1: start with the original value (no-op)
            // Step 2: multiply by 1e5 (=100000), take rounded result
            Int32 i = (Int32)Math.Round(d * 100000.0);

            // Step 2: convert to 2's complement binary representation
            // I think there is nothing to do here - should already be that way.

            // Step 4: left shift by 1 bit
            UInt32 i2 = (((UInt32)i) << 1);

            // Step 5: invert it if i < 0
            if (i < 0)
                i2 ^= 0xFFFFFFFF;

            // Step 6: break into 5-bit chunks, starting at right
            UInt32[] rgui = new uint[6];
            int j;
            for (j = 0; j < 6; j++)
                rgui[j] = (UInt32)(i2 & (0x1F << (5 * j))) >> (5 * j);

            // step 7: reverse them (no op, that's how above filled the array)

            // step 8: OR each with 0x20 if followed by others
            for (j = 5; j > 0 && rgui[j] == 0; j--) ; // skip the fully 0 bytes
            int lastchar = j;
            while (--j >= 0)  // And OR the remainders with 0x20
                rgui[j] |= 0x20;

            // Step 9: convert each value to decimal (no op)

            // Step 10-11: convert each value to ASCII by adding 63 and output
            // Don't forget to escape backslashes
            for (j = 0; j <= lastchar; j++)
            {
                sb.Append(Convert.ToChar(rgui[j] + 63, CultureInfo.InvariantCulture));
                if (Convert.ToChar(rgui[j] + 63, CultureInfo.InvariantCulture) == '\\')
                    sb.Append('\\');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Reverses EncodeLatLong
        /// </summary>
        /// <param name="sz"></param>
        /// <returns></returns>
        public static double DecodeLatLong(string sz)
        {
            double val = 0.0;

            if (!String.IsNullOrEmpty(sz))
            {
                char[] rgch = sz.ToCharArray();
                int ich = 0;
                int cch = rgch.Length;
                UInt32 chunk;
                UInt32 intVal = 0;
                int shift = 0;

                do
                {
                    // Undo steps 10-11:
                    chunk = (UInt32)(((int)rgch[ich]) - 63);    // convert from ascii to decimal by subtracting 63 (steps 9-10)
                    intVal |= (chunk & 0x1F) << (5 * shift++);  // Re-assemble from right to left by shifting 5 bits at a time (step 6-8)
                } while (++ich < cch && (chunk & 0x20) != 0);  // the 0x20 leading bit indicates that more 5-bit chunks follow

                // Undo steps 5 and 4: if it has a trailing 1, it needs to be inverted; either way, it then needs to be shifted right
                if ((intVal & 0x1) == 1)
                    intVal = ~intVal;

                // Convert it back to a signed value
                Int32 iVal = (Int32)intVal;

                // And convert it back to a decimal, dividing by 2 and by 1e5
                val = ((double)iVal) / 2e5;
            }

            return val;
        }

        /// <summary>
        /// Encodes a latlong path per the instructions at https://developers.google.com/maps/documentation/utilities/polylinealgorithm
        /// </summary>
        /// <param name="rgll">An enumerable with points.</param>
        /// <returns></returns>
        public void ParsePath(IEnumerable<LatLong> rgll)
        {
            if (rgll == null)
                return;
            StringBuilder sb = new StringBuilder();

            LatLong ll = new LatLong(0, 0);
            LatLongBox llb = null;

            double distance = 0.0;
            bool fAddDistance = false;

            foreach (LatLong llNew in rgll)
            {
                if (llb == null)
                {
                    llb = new LatLongBox(llNew);
                }
                else
                {
                    llb.ExpandToInclude(llNew);
                }

                if (!llNew.IsSameLocation(ll, .00015))   // wait until we actually move a little bit.  .00015 of latitude is approximately 20ft 
                {
                    if (fAddDistance)
                        distance += llNew.DistanceFrom(ll);
                    else
                        fAddDistance = true;

                    sb.Append(EncodeLatLong(llNew.Latitude - ll.Latitude));
                    sb.Append(EncodeLatLong(llNew.Longitude - ll.Longitude));

                    ll = llNew;
                }
            }

            EncodedPath = sb.ToString();
            Box = llb;
            Distance = distance;
        }

        /// <summary>
        /// Returns a decoded lat/lon path from the encoded string
        /// </summary>
        /// <returns>A list of LatLong values</returns>
        public IEnumerable<LatLong> DecodedPath()
        {
            List<LatLong> lst = new List<LatLong>();
            if (string.IsNullOrEmpty(EncodedPath))
                return lst;

            char[] rgch = EncodedPath.ToCharArray();

            // break it up into strings for each number
            List<string> lstVals = new List<string>();
            int ichLast = 0;
            int cch = 0;
            for (int ichCur = 0; ichCur < rgch.Length; ichCur++)
            {
                cch++;
                if (((((int)rgch[ichCur]) - 63) & 0x20) == 0)
                {
                    // need to account for backslashes
                    if (rgch[ichCur] == '\\')
                    {
                        cch++;
                        ichCur++;
                    }
                    lstVals.Add(EncodedPath.Substring(ichLast, cch));
                    cch = 0;
                    ichLast = ichCur + 1;
                }
            }

            // Verify that we have an even number of values - don't know what to do with a trailing latitude or trailing longitude!!!
            int cVals = lstVals.Count;
            if ((cVals % 2) > 0)
                cVals--;

            LatLong ll = null;
            for (int i = 0; i < cVals; i += 2)
            {
                double lat = DecodeLatLong(lstVals[i]) + ((ll == null) ? 0.0 : ll.Latitude);
                double lon = DecodeLatLong(lstVals[i + 1]) + ((ll == null) ? 0.0 : ll.Longitude);
                lst.Add(ll = new LatLong(lat, lon));
            }

            return lst;
        }
        #endregion
    }

    public interface IPolyRegion
    {
        /// <summary>
        /// Tests if the point is in the region.
        /// </summary>
        /// <param name="ll"></param>
        /// <returns></returns>
        bool ContainsLocation(LatLong ll);

        /// <summary>
        /// The name of the region
        /// </summary>
        string Name { get; }
    }

    public abstract class GeoRegionBase : IPolyRegion
    {
        #region Properties
        private List<LatLong> m_lst;

        /// <summary>
        /// The points that define the boundary of the polygon
        /// </summary>
        public IEnumerable<LatLong> BoundingPolygon
        {
            get { return m_lst; }
            set { m_lst = value == null ? null : new List<LatLong>(value); }
        }

        public virtual string Name { get { return string.Empty; } }
        #endregion

        #region Object Creation
        protected GeoRegionBase()
        {
            BoundingPolygon = null;
        }

        protected GeoRegionBase(IEnumerable<LatLong> Poly) : this()
        {
            BoundingPolygon = Poly;
        }
        #endregion

        /// <summary>
        /// Tests to see if the specified point is contained within this region
        /// Code adapted from https://stackoverflow.com/questions/924171/geo-fencing-point-inside-outside-polygon/7739297#7739297
        /// </summary>
        /// <param name="ll">The lat/lon to test</param>
        /// <returns>True if the point is contained.</returns>
        public virtual bool ContainsLocation(LatLong ll)
        {
            if (ll == null || BoundingPolygon == null || BoundingPolygon.Count() == 0)
                return false;

            int i, j;
            bool fContained = false;
            for (i = 0, j = m_lst.Count - 1; i < m_lst.Count; j = i++)
            {
                if ((((m_lst[i].Latitude <= ll.Latitude) && (ll.Latitude < m_lst[j].Latitude))
                        || ((m_lst[j].Latitude <= ll.Latitude) && (ll.Latitude < m_lst[i].Latitude)))
                        && (ll.Longitude < (m_lst[j].Longitude - m_lst[i].Longitude) * (ll.Latitude - m_lst[i].Latitude)
                            / (m_lst[j].Latitude - m_lst[i].Latitude) + m_lst[i].Longitude))

                    fContained = !fContained;
            }

            return fContained;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder((Name ?? "(unnamed)") + ": ");
            if (BoundingPolygon == null)
                sb.Append("(no polygon)");
            else
                foreach (LatLong ll in BoundingPolygon)
                    sb.AppendFormat(CultureInfo.CurrentCulture, "{0} ", ll.ToAdhocFixString());

            return sb.ToString();
        }
    }

    #region Concrete and Known GeoRegions
    public class GeoRegion : GeoRegionBase
    {
        private readonly string m_name;

        public override string Name { get { return m_name; } }

        public GeoRegion(string name, IEnumerable<LatLong> poly) : base(poly)
        {
            m_name = name;
        }
    }

    public class GeoRegionAfrica : GeoRegionBase
    {
        public override string Name { get { return Resources.Geography.regionAfrica; } }

        public GeoRegionAfrica() : base(new LatLong[] {
                new LatLong(35.9639589732242, -5.51296000479861),
                new LatLong(35.8862723352267, -6.06508590729603),
                new LatLong(27.9650246910836, -13.396328703638),
                new LatLong(20.4185511398316, -19.0220934429556),
                new LatLong(10.2067563913974, -18.1746276284209),
                new LatLong(-35.7974468653211, 18.0727332053251),
                new LatLong(-37.800423975311, 45.5756350423789),
                new LatLong(-15.5812502513301, 64.3241461405685),
                new LatLong(13.839554162838, 55.4034779375179),
                new LatLong(12.0001874490197, 43.9744547803343),
                new LatLong(12.8215166774877, 43.1793499776247),
                new LatLong(14.6748538652459, 41.9514734769258),
                new LatLong(26.4097961350399, 35.0444587248198),
                new LatLong(27.7101288125027, 33.9360619033247),
                new LatLong(28.706074147917, 32.9735482949426),
                new LatLong(29.8946920096056, 32.521020534956),
                new LatLong(30.2264448230464, 32.580571137488),
                new LatLong(30.8410092699496, 32.3510582691335),
                new LatLong(31.1126376615812, 32.6174830928286),
                new LatLong(32.759257166309, 32.3129919377845),
                new LatLong(34.8296570592962, 12.3428920775784),
                new LatLong(37.5316071447726, 11.3845983138082),
                new LatLong(37.376662630504, 2.83389562925519),
                new LatLong(35.9639589732242, -5.51296000479861) }) { }
    }

    public class GeoRegionAsia : GeoRegionBase
    {
        public override string Name { get { return Resources.Geography.regionAsia; } }

        public GeoRegionAsia() : base(new LatLong[] {
        new LatLong(41.2509219004154, 29.1327664900908),
                new LatLong(40.9734141742538, 28.9607800270263),
                new LatLong(40.7139062416896, 27.5095406385034),
                new LatLong(40.42723455786, 26.7220902256957),
                new LatLong(39.964641808291, 26.0892326658118),
                new LatLong(39.033213604809, 25.6738236896689),
                new LatLong(37.8919707829971, 25.7954007357716),
                new LatLong(35.9039485927882, 27.6153442151746),
                new LatLong(31.3042674355417, 32.3541120404423),
                new LatLong(30.5492386422801, 32.3082736215819),
                new LatLong(30.2355781570682, 32.4964367721185),
                new LatLong(29.9166389861311, 32.5755407217273),
                new LatLong(28.5938049291395, 33.0323536878531),
                new LatLong(27.6123886365306, 34.2353541632286),
                new LatLong(13.7388068586605, 42.5055435095544),
                new LatLong(12.6139998625938, 43.3696062227207),
                new LatLong(11.8309154195224, 46.7217748151204),
                new LatLong(12.1090910402245, 51.3426021907803),
                new LatLong(0.0591396777793757, 83.4779441979723),
                new LatLong(-13.0296954507373, 124.711137774139),
                new LatLong(-9.59108097354274, 133.793246439443),
                new LatLong(-9.80501498722165, 142.210469339179),
                new LatLong(-13.7299006546679, 163.709103861709),
                new LatLong(53.3520024026076, 172.161152065312),
                new LatLong(64.4717875927483, 188.593522),  // avoid wrapping issues - go more than 180 degrees east.
                new LatLong(65.7054468886283, 190.9696328),
                new LatLong(65.8174720832036, 191.0716377),
                new LatLong(75.3985684444915, 190.6717555),
                new LatLong(81.6089771036393, 84.8429070695296),
                new LatLong(70.5369188387754, 63.6472730500082),
                new LatLong(66, 69),
                new LatLong(58.5, 59.3),
                new LatLong(46.5, 50),
                new LatLong(46.6, 48.3),
                new LatLong(45.67, 46.1),
                new LatLong(45.6, 42),
                new LatLong(43.2173863336323, 42.4728025502204),
                new LatLong(43.5930962200247, 40.2995769590982),
                new LatLong(44.8732752454212, 38.1208700502164),
                new LatLong(41.2509219004154, 29.1327664900908)})
        { }
    }

    public class GeoRegionAustralia : GeoRegionBase
    {
        public override string Name { get { return Resources.Geography.regionAustralia; } }

        public GeoRegionAustralia() : base(new LatLong[] {
             new LatLong(-9.84747798837819, 142.164903744794),
                new LatLong(-10.7634822999224, 128.242927897764),
                new LatLong(-21.3883493589849, 108.9805830486),
                new LatLong(-38.7356886909232, 109.98120359961),
                new LatLong(-48.9146246440036, 154.581951859103),
                new LatLong(-24.358190937035, 154.968822697766),
                new LatLong(-9.84747798837819, 142.164903744794)})
        { }
    }

    public class GeoRegionEurope : GeoRegionBase
    {
        public override string Name { get { return Resources.Geography.regionEurope; } }

        public GeoRegionEurope() : base(new LatLong[] {
        new LatLong(80.8558924725111, -104.623029917689),
                new LatLong(74.8912746930429, -70.4480130659312),
                new LatLong(66.2374587178815, -56.7443463303166),
                new LatLong(58.3120543795464, -43.7864892029896),
                new LatLong(35.7666986983938, -9.9144334810344),
                new LatLong(35.9206725280298, -5.76605448262612),
                new LatLong(35.987725826605, -5.05260546746426),
                new LatLong(37.5945403737721, 11.4752868911118),
                new LatLong(34.1019556187922, 15.6149082883608),
                new LatLong(34.1142583862664, 23.5656801553353),
                new LatLong(35.1096880928465, 27.6335921301456),
                new LatLong(36.3328033369107, 27.1146010222214),
                new LatLong(37.8454869034392, 25.6093154686312),
                new LatLong(39.3405977850254, 25.6114546037733),
                new LatLong(40.0120089426856, 26.1663812337314),
                new LatLong(40.1603459024305, 26.41090699303),
                new LatLong(40.4489888069553, 26.8294078045092),
                new LatLong(40.9186911803264, 28.9170672550352),
                new LatLong(41.3139457691132, 29.1593413484323),
                new LatLong(43.3673702736188, 39.9994006379031),
                new LatLong(43.593075710196, 40.1067940778491),
                new LatLong(43.2537312885191, 42.5610739750239),
                new LatLong(42.7980685552358, 43.749946837025),
                new LatLong(42.704228077724, 44.4806056061958),
                new LatLong(42.3928227226911, 45.9496621358904),
                new LatLong(42.3070794920955, 45.5542477719412),
                new LatLong(41.8179734725377, 46.8006432792265),
                new LatLong(41.3396199556304, 47.2374245910874),
                new LatLong(41.2046925307547, 47.8216411217678),
                new LatLong(41.7607787135203, 48.5935325616133),
                new LatLong(46.3629939794767, 49.1797477747411),
                new LatLong(46.7265063862009, 48.5500843440706),
                new LatLong(47.8248608945324, 48.2980765207448),
                new LatLong(48.4538889039536, 46.54370124941),
                new LatLong(50.4912668686513, 47.5135479310345),
                new LatLong(49.8827419539125, 48.3332220648206),
                new LatLong(50.0741453268273, 48.8794781443255),
                new LatLong(50.5877839839358, 48.6899694551153),
                new LatLong(51.8185402882364, 50.8209198978647),
                new LatLong(68.3827888074154, 68.435652453113),
                new LatLong(69.9014945798418, 64.210230386991),
                new LatLong(77.3159309370392, 71.0856193510771),
                new LatLong(87.0110973494483, 78.3257990090781),
                new LatLong(80.8558924725111, -104.623029917689)})
        { }
    }

    public class GeoRegionNorthAmerica : GeoRegionBase
    {
        public override string Name { get { return Resources.Geography.regionNorthAmerica; } }

        public GeoRegionNorthAmerica() : base(new LatLong[] {
            new LatLong(16.338092279951, -157.646716565888),
                new LatLong(4.82956650806859, -80.282609015191),
                new LatLong(7.22181017365141, -77.880570089996),
                new LatLong(7.44776581427614, -77.786236266851),
                new LatLong(7.48638485079662, -77.703585326028),
                new LatLong(7.63529055470985, -77.703811265211),
                new LatLong(7.512716626303, -77.582810769788),
                new LatLong(7.76563668378025, -77.38333859188),
                new LatLong(7.8552333427672, -77.342562198241),
                new LatLong(7.95384883044459, -77.166922109416),
                new LatLong(8.51291616597859, -77.446398321394),
                new LatLong(8.68357371710449, -77.365255574793),
                new LatLong(11, -77.317269403893),
                new LatLong(17, -63),
                new LatLong(50.858620027233, -47),
                new LatLong(76.6833454236019, -75.722968754264),
                new LatLong(84.9536812968452, -54.132134583663),
                new LatLong(70.7433570556606, -167.95609566936),
                new LatLong(67.176093128249, -168.177707220915),
                new LatLong(65.8241496563039, -168.899825316639),
                new LatLong(63.7569457862177, -172.043426121483),
                /* new LatLong(52.9504446970992, 171.711362533652), */
                new LatLong(52.9504446970992, -188.288637466348),   // capture the aleutians, but use < -180 to avoid wrap-around issues.
                new LatLong(16.338092279951, -157.646716565888)
            })
        { }
    }

    public class GeoRegionSouthAmerica : GeoRegionBase
    {
        public override string Name { get { return Resources.Geography.regionSouthAmerica; } }

        public GeoRegionSouthAmerica() : base(new LatLong[] {
         new LatLong(8.6864747989427, -77.3578705512968),
                new LatLong(8.50861348396742, -77.4529541443921),
                new LatLong(8.26572875703845, -77.298368698503),
                new LatLong(7.92259705503796, -77.1645994759198),
                new LatLong(7.22322226621087, -77.8979236540419),
                new LatLong(1.71661207255935, -93.6043845257161),
                new LatLong(-55.352876127547, -98.716136930023),
                new LatLong(-58.6325605659904, -53.0064080186952),
                new LatLong(-4.11197623825627, -29.3523164812539),
                new LatLong(11.8941952294329, -59.764648533519),
                new LatLong(13.3535447099878, -71.3131201675003),
                new LatLong(11.5647049699963, -76.551881316024),
                new LatLong(8.6864747989427, -77.3578705512968)})
        { }
    }

    public class GeoRegionAntarctica : IPolyRegion
    {
        public string Name { get { return Resources.Geography.regionAntarctica; } }

        public bool ContainsLocation(LatLong ll) { return ll != null && ll.Latitude < -60; }
    }

    public static class KnownGeoRegions
    {
        private static IEnumerable<IPolyRegion> _AllContinents = null;

        public static IEnumerable<IPolyRegion> AllContinents
        {
            get
            {
                if (_AllContinents == null)
                    _AllContinents = new IPolyRegion[]
                        {
                            new GeoRegionAntarctica(),
                            new GeoRegionAfrica(),
                            new GeoRegionAsia(),
                            new GeoRegionAustralia(),
                            new GeoRegionEurope(),
                            new GeoRegionNorthAmerica(),
                            new GeoRegionSouthAmerica()
                        };
                return _AllContinents;
            }
        }
    }
    #endregion
}
