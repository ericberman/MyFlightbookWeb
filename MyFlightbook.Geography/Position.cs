using System;
using System.Collections.Generic;
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
    /// Position report: LatLong plus optional altitude and optional timestamp.
    /// </summary>
    [Serializable]
    public class Position : LatLong, IComparable, IEquatable<Position>
    {
        private double m_Altitude;
        private double m_Speed;
        private DateTime m_dt = DateTime.MinValue;
        private bool m_HasAltitude;
        private bool m_HasSpeed;
        private bool m_HasTimestamp;

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
        public Position(double Lat, double Lon)
            : base(Lat, Lon) { }

        public Position() : this(0, 0) { }

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
                throw new MyFlightbookException("Error reading KML coordinate: need at least a latitude and a longitude, both were not provided");

            if (double.TryParse(rgCoord[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon) &&
                double.TryParse(rgCoord[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
            {
                Latitude = lat;
                Longitude = lon;
            }
            else
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Error reading KML coordinate: {0} {1} {2}", rgCoord[0], rgCoord[1], rgCoord.Length > 2 ? rgCoord[2] : string.Empty));

            if (rgCoord.Length > 2)
            {
                Altitude = double.TryParse(rgCoord[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double alt)
                    ? alt
                    : throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Error reading altitude '{0}'", rgCoord[2]));
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
        /// Interpolates a position based on the specified timestamp (should be UTC).
        /// </summary>
        /// <param name="dt">The timestamp</param>
        /// <param name="l">An enumerable of positions.</param>
        /// <returns>A LatLong representing the best guess of position at the specified time, null if it cannot be determined</returns>
        public static LatLong Interpolate(DateTime dt, IEnumerable<Position> l)
        {
            if (l == null)
                throw new ArgumentNullException(nameof(l));

            Position pLatestBeforeTime = null;
            Position pEarliestAfterTime = null;

            foreach (Position p in l)
            {
                if (!p.HasTimeStamp)
                    continue;

                if (p.Timestamp.CompareTo(dt) <= 0 && (pLatestBeforeTime == null || p.Timestamp.CompareTo(pLatestBeforeTime.Timestamp) > 0))
                    pLatestBeforeTime = p;
                if (p.Timestamp.CompareTo(dt) >= 0 && (pEarliestAfterTime == null || p.Timestamp.CompareTo(pEarliestAfterTime.Timestamp) < 0))
                    pEarliestAfterTime = p;
            }

            if (pLatestBeforeTime == null || pEarliestAfterTime == null)
                return null;

            TimeSpan straddle = pEarliestAfterTime.Timestamp.Subtract(pLatestBeforeTime.Timestamp);
            if (straddle.TotalSeconds == 0) // exact hit, can return either one.
                return pLatestBeforeTime.ToLatLong();   // don't return a position object!!!

            double dLat = pEarliestAfterTime.Latitude - pLatestBeforeTime.Latitude;
            double dLon = pEarliestAfterTime.Longitude - pLatestBeforeTime.Longitude;

            TimeSpan tMid = dt.Subtract(pLatestBeforeTime.Timestamp);

            double frac = tMid.TotalSeconds / straddle.TotalSeconds;  // how far between the two points that straddle the time is this?  Should be between 0 and 1!!

            return new LatLong(pLatestBeforeTime.Latitude + dLat * frac, pLatestBeforeTime.Longitude + dLon * frac);
        }

        #region IComparable
        /// <summary>
        /// Orders by date, if date is present; order is otherwise undefined
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            Position samp = obj as Position;
            if (this.HasTimeStamp && samp.HasTimeStamp)
                return this.Timestamp.CompareTo(samp.Timestamp);
            else
                return 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Position);
        }

        public bool Equals(Position other)
        {
            return other != null &&
                   HasAltitude == other.HasAltitude &&
                   HasSpeed == other.HasSpeed &&
                   HasTimeStamp == other.HasTimeStamp &&
                   TypeOfSpeed == other.TypeOfSpeed &&
                   Altitude == other.Altitude &&
                   Timestamp == other.Timestamp &&
                   Speed == other.Speed &&
                   Comment == other.Comment;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = -2147241889;
                hashCode = hashCode * -1521134295 + HasAltitude.GetHashCode();
                hashCode = hashCode * -1521134295 + HasSpeed.GetHashCode();
                hashCode = hashCode * -1521134295 + HasTimeStamp.GetHashCode();
                hashCode = hashCode * -1521134295 + TypeOfSpeed.GetHashCode();
                hashCode = hashCode * -1521134295 + Altitude.GetHashCode();
                hashCode = hashCode * -1521134295 + Timestamp.GetHashCode();
                hashCode = hashCode * -1521134295 + Speed.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Comment);
                return hashCode;
            }
        }

        public static bool operator ==(Position left, Position right)
        {
            return EqualityComparer<Position>.Default.Equals(left, right);
        }

        public static bool operator !=(Position left, Position right)
        {
            return !(left == right);
        }

        public static bool operator <(Position left, Position right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(Position left, Position right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(Position left, Position right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(Position left, Position right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion

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
        /// Positions don't go down the wire to the mobile apps, so have a convenience method to force a true latlon
        /// </summary>
        /// <returns></returns>
        public LatLong ToLatLong()
        {
            return new LatLong(this.Latitude, this.Longitude);
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
                throw new ArgumentNullException(nameof(llStart));
            if (llEnd == null)
                throw new ArgumentNullException(nameof(llEnd));

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

            // low distance (< 1nm) is probably pattern work - just pick a decent speed.  If you actually go somewhere, then derive a speed.
            double speed = (distance < 1.0) ? 150 : ConversionFactors.MetersPerSecondPerKnot * (distance / ts.TotalHours);

            lst.Add(new Position(llStart, 0, dtStart, speed));

            for (long minute = 0; minute <= minutes; minute++)
            {
                if (distance < 1.0)
                    lst.Add(new Position(llStart.Latitude, llStart.Longitude, dtStart.AddMinutes(minute)) { Speed = speed });
                else
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
                    lst.Add(new Position(dlat, dlon, dtStart.AddMinutes(minute)) { Speed = speed });
                }
            }

            lst.AddRange(rgPadding);

            return lst;
        }
    }
}

