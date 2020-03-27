using MyFlightbook.Airports;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2017-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Weather.ADDS
{
    /// <summary>
    /// Extensions to the METAR class for sorting, etc.
    /// </summary>
    public partial class METAR : IComparable, IEquatable<METAR>
    {
        public enum FlightCategory { None, VFR, MVFR, IFR, LIFR }

        #region Additional Properties
        public DateTime? TimeStamp
        {
            get
            {
                if (String.IsNullOrEmpty(observation_time))
                    return null;
                if (DateTime.TryParse(observation_time, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dt))
                    return dt.ToUniversalTime();
                return null;
            }
        }

        /// <summary>
        /// Returns the flight category (rules)
        /// </summary>
        public FlightCategory Category
        {
            get
            {
                if (String.IsNullOrEmpty(flight_category))
                    return FlightCategory.None;

                if (flight_category.CompareOrdinalIgnoreCase("VFR") == 0)
                    return FlightCategory.VFR;

                if (flight_category.CompareOrdinalIgnoreCase("MVFR") == 0)
                    return FlightCategory.MVFR;

                if (flight_category.CompareOrdinalIgnoreCase("IFR") == 0)
                    return FlightCategory.IFR;

                if (flight_category.CompareOrdinalIgnoreCase("LIFR") == 0)
                    return FlightCategory.LIFR;

                return FlightCategory.None;
            }
        }

        #region display helpers
        /// <summary>
        /// Returns a human-readable version of the metar_type property
        /// </summary>
        public string METARTypeDisplay
        {
            get
            {
                switch (metar_type.ToUpperInvariant())
                {
                    case "METAR":
                        return string.Empty;
                    case "SPECI":
                        return Resources.Weather.metarTypeSpecial;
                    default:
                        return metar_type;
                }
            }
        }

        /// <summary>
        /// Displays the date+time in local format
        /// </summary>
        public string TimeDisplay
        {
            get
            {
                return TimeStamp.HasValue ? TimeStamp.Value.UTCDateFormatString() + " (Zulu)" : string.Empty;
            }
        }

        /// <summary>
        /// Altitude, in "hg, no label
        /// </summary>
        public string AltitudeHgDisplay
        {
            get { return altim_in_hgFieldSpecified ? String.Format(CultureInfo.CurrentCulture, Resources.Weather.pressureHgFormat, altim_in_hg) : string.Empty; }
        }

        /// <summary>
        /// Visibility in sm, no label
        /// </summary>
        public string VisibilityDisplay
        {
            get { return visibility_statute_miSpecified ? String.Format(CultureInfo.CurrentCulture, Resources.Weather.visibilitySmFormat, visibility_statute_mi) : string.Empty; }
        }

        /// <summary>
        /// Display version of any quality fields.
        /// </summary>
        public string QualityDisplay
        {
            get
            {
                if (quality_control_flags == null)
                    return string.Empty;
                StringBuilder sb = new StringBuilder();
                if (quality_control_flags.auto_station.CompareOrdinalIgnoreCase("TRUE") == 0)
                    sb.Append(Resources.Weather.auto_stationField);
                if (quality_control_flags.corrected.CompareOrdinalIgnoreCase("TRUE") == 0)
                    sb.Append(Resources.Weather.correctedField);
                if (quality_control_flags.freezing_rain_sensor_off.CompareOrdinalIgnoreCase("TRUE") == 0)
                    sb.Append(Resources.Weather.freezing_rain_sensor_offField);
                if (quality_control_flags.lightning_sensor_off.CompareOrdinalIgnoreCase("TRUE") == 0)
                    sb.Append(Resources.Weather.lightning_sensor_offField);
                if (quality_control_flags.maintenance_indicator_on.CompareOrdinalIgnoreCase("TRUE") == 0)
                    sb.Append(Resources.Weather.maintenance_indicator_onField);
                if (quality_control_flags.present_weather_sensor_off.CompareOrdinalIgnoreCase("TRUE") == 0)
                    sb.Append(Resources.Weather.present_weather_sensor_offField);
                return sb.ToString();
            }
        }

        /// <summary>
        /// Display version of temp + duepoint.  Includes label
        /// </summary>
        public string TempDewpointDisplay
        {
            get
            {
                if (temp_cSpecified && dewpoint_cSpecified)
                    return String.Format(CultureInfo.CurrentCulture, Resources.Weather.TempAndDewpoint, temp_c, dewpoint_c);
                else if (temp_cSpecified)
                    return String.Format(CultureInfo.CurrentCulture, Resources.Weather.temp_cField, temp_c);
                else if (dewpoint_cSpecified)
                    return String.Format(CultureInfo.CurrentCulture, Resources.Weather.dewpoint_cField, dewpoint_c);
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Display version of temp.  Includes units but not label
        /// </summary>
        public string TempDisplay
        {
            get { return temp_cFieldSpecified ? String.Format(CultureInfo.CurrentCulture, Resources.Weather.tempFormat, temp_c) : string.Empty; }
        }

        /// <summary>
        /// Display version of dewpoint. Includes units but not label
        /// </summary>
        public string DewpointDisplay
        {
            get { return dewpoint_cFieldSpecified ? String.Format(CultureInfo.CurrentCulture, Resources.Weather.tempFormat, dewpoint_c) : string.Empty; }
        }

        /// <summary>
        /// Display version of temp + dewpoint
        /// </summary>
        public string TempAndDewpointDisplay
        {
            get { return dewpoint_cFieldSpecified ? String.Format(CultureInfo.CurrentCulture, "{0}/{1}", TempDisplay, DewpointDisplay) : TempDisplay; }
        }

        /// <summary>
        /// Display version of wind direction.  Includes units but not label
        /// </summary>
        public string WindDirDisplay
        {
            get { return wind_dir_degreesFieldSpecified ? String.Format(CultureInfo.CurrentCulture, Resources.Weather.wind_dir_degreesField, wind_dir_degrees) : string.Empty; }
        }


        #endregion
        #endregion

        #region IComparable
        /// <summary>
        /// Sorts METARS in ascending order by station ID and *DESCENDING* order by timestamp
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            METAR m = obj as METAR;
            
            int i = String.Compare(station_id, m.station_id, StringComparison.CurrentCultureIgnoreCase);
            if (i != 0)
                return i;

            if (!TimeStamp.HasValue && !m.TimeStamp.HasValue)
                return 0;

            if (TimeStamp.HasValue && !m.TimeStamp.HasValue)
                return 1;

            if (!TimeStamp.HasValue && m.TimeStamp.HasValue)
                return -1;

            return m.TimeStamp.Value.CompareTo(TimeStamp.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is null)
            {
                return false;
            }

            return Equals(obj as METAR);
        }

        public bool Equals(METAR other)
        {
            return other != null &&
                   TimeStamp == other.TimeStamp &&
                   Category == other.Category &&
                   METARTypeDisplay == other.METARTypeDisplay &&
                   TimeDisplay == other.TimeDisplay &&
                   AltitudeHgDisplay == other.AltitudeHgDisplay &&
                   VisibilityDisplay == other.VisibilityDisplay &&
                   QualityDisplay == other.QualityDisplay &&
                   TempDewpointDisplay == other.TempDewpointDisplay &&
                   TempDisplay == other.TempDisplay &&
                   DewpointDisplay == other.DewpointDisplay &&
                   TempAndDewpointDisplay == other.TempAndDewpointDisplay &&
                   WindDirDisplay == other.WindDirDisplay;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = -106303338;
                hashCode = hashCode * -1521134295 + TimeStamp.GetHashCode();
                hashCode = hashCode * -1521134295 + Category.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(METARTypeDisplay);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TimeDisplay);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AltitudeHgDisplay);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(VisibilityDisplay);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(QualityDisplay);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TempDewpointDisplay);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TempDisplay);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DewpointDisplay);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TempAndDewpointDisplay);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(WindDirDisplay);
                return hashCode;
            }
        }

        public static bool operator ==(METAR left, METAR right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(METAR left, METAR right)
        {
            return !(left == right);
        }

        public static bool operator <(METAR left, METAR right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(METAR left, METAR right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(METAR left, METAR right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(METAR left, METAR right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion
    }

    public partial class sky_condition
    {
        public string SkyCoverDisplay
        {
            get
            {
                switch (sky_cover.ToUpperInvariant())
                {
                    default:
                        return sky_cover;
                    case "CLR":
                        return Resources.Weather.Clear;
                    case "FEW":
                        return Resources.Weather.Few;
                    case "SCT":
                        return Resources.Weather.Scattered;
                    case "BKN":
                        return Resources.Weather.Broken;
                    case "OVC":
                        return Resources.Weather.Overcast;
                }
            }
        }
    }

    /// <summary>
    /// Summary description for ADDS
    /// </summary>
    public static class ADDSService
    {
        private const string szRecentTemplate = @"http://aviationweather.gov/adds/dataserver_current/httpparam?dataSource=metars&format=xml&requestType=retrieve&hoursBeforeNow=3&mostRecent=false&stationString={0}";

        #region Getting requests
        private static response GetRequest(string szU)
        {
            WebRequest req = (HttpWebRequest)WebRequest.Create(new Uri(szU));
            //Set values for the request back
            req.Method = "GET";
            req.ContentType = "text/xml";
            req.ContentLength = 0;
            string strResponse = string.Empty;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // allows for validation of SSL conversations

            try
            {
                using (StreamReader streamIn = new StreamReader(req.GetResponse().GetResponseStream()))
                    strResponse = streamIn.ReadToEnd();
                return strResponse.DeserializeFromXML<response>();
            }
            catch (Exception ex) when (ex is WebException)
            {
                return null;
            }
        }
        #endregion

        public static response METARsForAirports(string airports)
        {
            if (string.IsNullOrEmpty(airports))
                return null;

            string fixedCodes = String.Join(",", airport.SplitCodes(airports));
            return GetRequest(String.Format(CultureInfo.InvariantCulture, szRecentTemplate, fixedCodes));
        }

        public static IEnumerable<METAR> LatestMETARSForAirports(string airports)
        {
            response r = METARsForAirports(airports);

            List<METAR> lst = new List<METAR>();

            if (r != null && r.data != null && r.data.METAR != null)
            {
                // Just sort them and then take the first instance of each station.
                List<METAR> lstSorted = new List<METAR>(r.data.METAR);
                lstSorted.Sort();

                string szStation = string.Empty;

                foreach (METAR m in lstSorted)
                {
                    if (m.station_id.CompareCurrentCultureIgnoreCase(szStation) != 0)
                    {
                        lst.Add(m);
                        szStation = m.station_id;
                    }
                }
            }

            return lst;
        }
    }
}