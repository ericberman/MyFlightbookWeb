using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    /// Represents a latitude/longitude pair. Pretty darned simple...
    /// </summary>
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
                throw new ArgumentNullException(nameof(ll));
            Latitude = ll.Latitude;
            Longitude = ll.Longitude;
        }
        #endregion

        private readonly static LazyRegex regPosition = new LazyRegex("([NnSs]) ?(\\d{1,2}) (\\d{0,2}(?:.\\d*)?) ([EeWw]) ?(\\d{1,3}) (\\d{0,2}(?:.\\d*)?)");
        private readonly static LazyRegex regPositionTuple = new LazyRegex("(?<lat>-?\\d{1,2}(\\.\\d*)?),(?<lon>-?\\d{1,3}(\\.\\d*)?)");
        /// <summary>
        /// Creates a LatLong from a position string in the flight data format N 46 34.345 W 122 43.34
        /// </summary>
        /// <param name="szPosition"></param>
        public LatLong(string szPosition)
        {
            MatchCollection mc = regPosition.Matches(szPosition);
            // should have 1 match with 7 groups: the whole string, N/S, deg, minutes, EW, deg, minutes
            if (mc.Count == 1 && mc[0].Groups.Count == 7)
            {
                GroupCollection g = mc[0].Groups;
                Latitude = ((String.Compare(g[1].Value, "S", StringComparison.CurrentCultureIgnoreCase) == 0) ? -1 : 1) * (Convert.ToDouble(g[2].Value, CultureInfo.CurrentCulture) + (Convert.ToDouble(g[3].Value, CultureInfo.CurrentCulture) / 60.0));
                Longitude = ((String.Compare(g[4].Value, "W", StringComparison.CurrentCultureIgnoreCase) == 0) ? -1 : 1) * (Convert.ToDouble(g[5].Value, CultureInfo.CurrentCulture) + (Convert.ToDouble(g[6].Value, CultureInfo.CurrentCulture) / 60.0));
                return;
            }
            mc = regPositionTuple.Matches(szPosition);
            if (mc.Count == 1 &&
                double.TryParse(mc[0].Groups["lat"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(mc[0].Groups["lon"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out double lon))
            {
                Latitude = lat;
                Longitude = lon;
                return;
            }

            // If we're here, then we can't parse it.
            throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Properties.Geography.errBadPosition, szPosition));
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
                    return Properties.Geography.errInvalidLatitude;
                if (!IsValidLongitude)
                    return Properties.Geography.errInvalidLongitude;
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
                throw new ArgumentNullException(nameof(ll1));
            if (ll2 == null)
                throw new ArgumentNullException(nameof(ll2));

            // Short circuit 0 distance
            if (ll1.Latitude == ll2.Latitude && ll1.Longitude == ll2.Longitude)
                return 0.0;

            // And short circuit nonsense values
            if (!ll1.IsValid || !ll2.IsValid)
                return double.NaN;

            /*
            // Below are spherical calculations.  Better to use CoordinateSharp which uses WGS84, which is more accurate (small difference, though).
            double rlat1 = Math.PI * (ll1.Latitude / 180.0);
            double rlon1 = Math.PI * (ll1.Longitude / 180.0);
            double rlat2 = Math.PI * (ll2.Latitude / 180.0);
            double rlon2 = Math.PI * (ll2.Longitude / 180.0);
            double d = Math.Acos(Math.Sin(rlat1) * Math.Sin(rlat2) + Math.Cos(rlat1) * Math.Cos(rlat2) * Math.Cos(rlon2 - rlon1)) * 3440.06479;
            return double.IsNaN(d) ? 0 : d;
            */

            CoordinateSharp.Distance d2 = new CoordinateSharp.Distance(new CoordinateSharp.Coordinate(ll1.Latitude, ll1.Longitude), new CoordinateSharp.Coordinate(ll2.Latitude, ll2.Longitude), CoordinateSharp.Shape.Ellipsoid);
            return double.IsNaN(d2.NauticalMiles) ? 0 : d2.NauticalMiles;
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
            if (szLat == null)
                throw new ArgumentNullException(nameof(szLat));
            if (szLon == null)
                throw new ArgumentNullException(nameof(szLon));

            if (szLat.GetType() == typeof(double) && szLon.GetType() == typeof(double))
                return new LatLong((double)szLat, (double)szLon);

            if (ci == null)
                ci = CultureInfo.CurrentCulture;

            if (double.TryParse(szLat.ToString(), NumberStyles.Any, ci, out double lat) && double.TryParse(szLon.ToString(), System.Globalization.NumberStyles.Any, ci, out double lon))
                return new LatLong(lat, lon);
            return null;
        }

        /// <summary>
        /// Return the antipode (opposite side of the planet) coordinate for this point.
        /// </summary>
        public LatLong Antipode()
        {
            return new LatLong(-Latitude, Longitude > 0 ? Longitude - 180.0 : Longitude + 180.0);
        }
    }

    /// <summary>
    /// Interface for anything that can be mapped.
    /// </summary>
    public interface IMapMarker
    {
        double latitude { get; }
        double longitude { get; }

        LatLong latlong { get; }
    }

    /// <summary>
    /// Concreate IMapMarker along with some helper utilities
    /// </summary>
    [Serializable]
    public class MFBGMapLatLon : IMapMarker
    {
        public double latitude { get; set; }
        public double longitude { get; set; }

        [JsonIgnore]
        public LatLong latlong
        {
            get { return new LatLong(latitude, longitude); }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                latitude = value.Latitude;
                longitude = value.Longitude;
            }
        }

        public static IEnumerable<IMapMarker> FromLatlongs(IEnumerable<LatLong> rgll)
        {
            if (rgll == null || !rgll.Any())
                return Array.Empty<IMapMarker>();

            List<IMapMarker> lst = new List<IMapMarker>();
            foreach (LatLong ll in rgll)
                lst.Add(new MFBGMapLatLon() { latlong = ll });

            return lst;
        }

        public static IEnumerable<double[]> AsArrayOfArrays(IEnumerable<IMapMarker> rg)
        {
            if (rg == null)
                return null;
            List<double[]> lst = new List<double[]>();
            foreach (IMapMarker ll in rg)
                lst.Add(new double[] { ll.latitude, ll.longitude });
            return lst;
        }
    }
}
