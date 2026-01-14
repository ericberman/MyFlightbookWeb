using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2015-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Geography
{
    /// <summary>
    /// Represents a 2-dimensional geographic area.
    /// </summary>
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
                throw new ArgumentNullException(nameof(l));
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
                throw new ArgumentNullException(nameof(ll));

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
                throw new ArgumentNullException(nameof(l));
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
}
