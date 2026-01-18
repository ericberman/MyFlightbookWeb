using MyFlightbook.Airports.Properties;
using MyFlightbook.CSV;
using MyFlightbook.Geography;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

/******************************************************
 * 
 * Copyright (c) 2010-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Airports
{
    /// <summary>
    /// Represents an airport as a latitude/longitude, airport code, and name
    /// </summary>
    [Serializable]
    public class airport : IComparable, IFix, IEquatable<airport>
    {
        #region properties
        /// <summary>
        /// Distance from a specified position, 0.0 if unknown (and if lat/long are different from current position).
        /// </summary>
        public double DistanceFromPosition { get; set; }

        /// <summary>
        /// User that created the airport; empty for built-in airports
        /// </summary>
        [JsonIgnore]
        public string UserName { get; set; }

        /// <summary>
        /// FAA Data code representing the type of the facility (1-2 chars)
        /// </summary>
        [Required]
        public string FacilityTypeCode { get; set; }

        /// <summary>
        /// Friendly name for the type of facility (e.g., "VOR")
        /// </summary>
        public string FacilityType { get; set; }

        /// <summary>
        /// The IATA or ICAO code for the airport
        /// </summary>
        [Required]
        [RegularExpression("[a-zA-Z0-9]{2,6}")]
        public string Code { get; set; }

        /// <summary>
        /// The friendly name for the airport
        /// </summary>
        [Required]
        public string Name { get; set; }

        [JsonIgnore]
        public string NameWithGeoRegion
        {
            get
            {
                List<string> lst = new List<string>() { CountryDisplay, Admin1Display };
                lst = lst.FindAll(sz => !String.IsNullOrWhiteSpace(sz));
                return String.Format(CultureInfo.CurrentCulture, "{0}{1}", Name, lst.Count == 0 ? string.Empty : String.Format(CultureInfo.CurrentCulture, " ({0})", String.Join(", ", lst)));
            }
        }

        public const string szDisputedRegion = "--";

        /// <summary>
        /// The name of the top-level administrative region (i.e., country)
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// The name of the 1st-level administrative region under country (e.g., states in the US, provinces in Canada)
        /// </summary>
        public string Admin1 { get; set; }

        [JsonIgnore]
        public string CountryDisplay { get { return (Country ?? string.Empty).StartsWith(szDisputedRegion, StringComparison.CurrentCultureIgnoreCase) ? string.Empty : Country ?? string.Empty; } }

        [JsonIgnore]
        public string Admin1Display { get { return (Admin1 ?? string.Empty).StartsWith(szDisputedRegion, StringComparison.CurrentCultureIgnoreCase) ? string.Empty : Admin1 ?? string.Empty; } }

        /// <summary>
        /// Latitude/longitude of the airport
        /// </summary>
        public LatLong LatLong { get; set; }

        /// <summary>
        /// DEPRECATED The airport's latitude (string representation of a decimal number)
        /// Used only for validation
        /// </summary>
        [JsonIgnore]
        [Required]
        [RegularExpression("-?\\d{0,2}(.\\d*)?")]
        public string Latitude
        {
            get { return this.LatLong.LatitudeString; }
            set { /* This is only here for backwards compatibility in the WSDL;  DO NOT SET */ }
        }

        /// <summary>
        /// DEPRECATED The airport's longitude (string representation of a decimal number)
        /// Used only for validation
        /// </summary>
        [JsonIgnore]
        [Required]
        [RegularExpression("-?\\d{0,3}(.\\d*)?")]
        public string Longitude
        {
            get { return this.LatLong.LongitudeString; }
            set { /* This is only here for backwards compatibility in the WSDL; DO NOT SET */ }
        }

        /// <summary>
        /// The error from the last event
        /// </summary>
        public string ErrorText { get; set; }

        #region Attributes of this airport
        /// <summary>
        /// True if this airport is user-generated.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsUserGenerated
        {
            get { return ((UserName?.Length ?? 0) > 0); }
        }

        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsAirport
        {
            get { return (FacilityTypeCode == "A" || FacilityType == "Airport"); }
        }

        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsSeaport
        {
            get { return (FacilityTypeCode == "S"); }
        }

        /// <summary>
        /// Is this an airport, seaport, or heliport?  (I.e., someplace to land)?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean IsPort
        {
            get { return (IsAirport || FacilityTypeCode == "S" || FacilityTypeCode == "H"); }
        }

        /// <summary>
        /// Priority for purposes of disambiguation
        /// </summary>
        /// <returns>An integer indicating priority, lowest (0) value is highest priority</returns>
        [Newtonsoft.Json.JsonIgnore]
        public int Priority
        {
            get
            {
                // airports ALWAYS have priority
                if (IsPort)
                    return 0;

                // Otherwise, give priority to VOR/VORTAC/etc., else NDB, else GPS fix, else everything else
                switch (FacilityTypeCode)
                {
                    // VOR types
                    case "V":
                    case "C":
                    case "D":
                    case "T":
                        return 1;
                    // NDB Types
                    case "R":
                    case "RD":
                    case "M":
                    case "MD":
                    case "U":
                        return 2;
                    // Generic fix
                    case "FX":
                        return 3;
                    default:
                        return 4;
                }
            }
        }

        /// <summary>
        /// Returns the full name of the airport (friendly name + code)
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string FullName
        {
            get { return String.Format(CultureInfo.CurrentCulture, "{0} ({1})", Name.Trim(), Code); }
        }

        /// <summary>
        /// Is this airport in a (generous) latitude/longitude box that surrounds Hawaii?  (Used for cross-country ratings)
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsHawaiian
        {
            get { return LatLong != null && LatLong.Latitude <= 26 && LatLong.Latitude >= 18 && LatLong.Longitude >= -173 && LatLong.Longitude <= -154; }
        }
        #endregion  // Attributes

        /// <summary>
        /// Hash code that rounds the location so that multiple airports of the same type at approximately the same location will merge.
        /// </summary>
        [JsonIgnore]
        public string GeoHashKey
        {
            get { return String.Format(CultureInfo.InvariantCulture, "{0}La{1:F2}Lo{2:F2}", FacilityTypeCode, LatLong.Latitude, LatLong.Longitude); }
        }
        #endregion  // properties

        private Boolean isNew;

        public const int minNavaidCodeLength = 2;
        public const int minAirportCodeLength = 3;
        public const int maxCodeLength = 6; // because of navaids, now allow up to 5 letters.
        public const string ForceNavaidPrefix = "@";
        public const string USAirportPrefix = "K";
        // adhoc fixes can either be @LatLon (e.g., @47.348N103.23W) or @MGRS coordinate (e.g., @13UEP715168)
        private const string szRegAdHocFix = ForceNavaidPrefix + "\\d{1,2}(?:[\\.,]\\d*)?[NS]\\d{1,3}(?:[\\.,]\\d*)?[EW]\\b";  // Must have a digit on the left side of the decimal
        private const string szRegMGRS = ForceNavaidPrefix + "\\d{1,2}[^ABIOYZabioyz][A-Za-z]{2}([0-9][0-9])+\\b";
        private readonly static string szRegexAirports = String.Format(CultureInfo.InvariantCulture, "((?:{0})|(?:{1})|(?:@[A-Z0-9]{{{2},{3}}}\\b)|(?:\\b[A-Z0-9]{{{2},{3}}}\\b))", szRegAdHocFix, szRegMGRS, Math.Min(airport.minNavaidCodeLength, airport.minAirportCodeLength), airport.maxCodeLength);
        private readonly static LazyRegex regAdHocFix = new LazyRegex(String.Format(CultureInfo.InvariantCulture, "((?:{0})|(?:{1}))", szRegAdHocFix, szRegMGRS));
        private readonly static LazyRegex regAirport = new LazyRegex(szRegexAirports);

        override public string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}({1}) - {2}", Code, FacilityTypeCode, Name);
        }

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            return String.Compare(Code, ((airport)obj).Code, StringComparison.CurrentCulture);
        }


        public override bool Equals(object obj)
        {
            return Equals(obj as airport);
        }

        public bool Equals(airport other)
        {
            return other != null &&
                   UserName == other.UserName &&
                   FacilityTypeCode == other.FacilityTypeCode &&
                   Code == other.Code &&
                   Name == other.Name &&
                   LatLong.Latitude == other.LatLong.Latitude &&
                   LatLong.Longitude == other.LatLong.Longitude &&
                   Priority == other.Priority;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = -1236571660;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(UserName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FacilityTypeCode);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Code);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<LatLong>.Default.GetHashCode(LatLong);
                hashCode = hashCode * -1521134295 + Priority.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(airport left, airport right)
        {
            return EqualityComparer<airport>.Default.Equals(left, right);
        }

        public static bool operator !=(airport left, airport right)
        {
            return !(left == right);
        }


        public static bool operator <(airport left, airport right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(airport left, airport right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(airport left, airport right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(airport left, airport right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion

        #region Object Creation
        public airport()
        {
            Code = Name = FacilityType = FacilityTypeCode = UserName = Country = Admin1 = string.Empty;
            DistanceFromPosition = 0.0;
            LatLong = new LatLong(0, 0);
        }

        /// <summary>
        /// Create an airport
        /// </summary>
        /// <param name="code">The ICAO or TLA code for the airport</param>
        /// <param name="name">The friendly name for the airport</param>
        /// <param name="latitude">The airport's latitude</param>
        /// <param name="longitude">The airport's longitude</param>
        /// <param name="facilitytypeCode">Code representing the facility type</param>
        /// <param name="facilitytype">The type of facility (airport, VOR, etc.)</param>
        /// <param name="dist">Distance from some specified reference point</param>
        /// <param name="szUserName">Name of the user that created this airport</param>
        public airport(string code, string name, double latitude, double longitude, string facilitytypeCode, string facilitytype, double dist, string szUser) : this()
        {
            Code = code;
            Name = name;
            this.LatLong = new LatLong(latitude, longitude);
            FacilityTypeCode = facilitytypeCode;
            FacilityType = facilitytype;
            UserName = szUser;
            DistanceFromPosition = dist;
        }

        /// <summary>
        /// Creates a new airport object from a datareader
        /// </summary>
        /// <param name="dr">The data reader.  If dist is not present, it will be set to 0</param>
        public airport(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));

            Code = dr["airportID"].ToString();
            Name = dr["FacilityName"].ToString();
            this.LatLong = new LatLong(Convert.ToDouble(dr["Latitude"], CultureInfo.InvariantCulture), Convert.ToDouble(dr["Longitude"], CultureInfo.InvariantCulture));
            FacilityTypeCode = dr["Type"].ToString();
            FacilityType = dr["FriendlyName"].ToString();
            UserName = dr["SourceUserName"].ToString();
            Country = (string) util.ReadNullableField(dr, "country", string.Empty);
            Admin1 = (string) util.ReadNullableField(dr, "admin1", string.Empty);
            object o = dr["dist"];
            DistanceFromPosition = o == DBNull.Value ? 0.0 : Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }
        #endregion

        /// <summary>
        /// Returns the distance between this airport and another, in NM
        /// </summary>
        /// <param name="ap">The airport to which it is being compared</param>
        /// <returns>Distance, in nautical miles</returns>
        public double DistanceFromAirport(IFix ap)
        {
            return DistanceFromFix(ap);
        }

        public double DistanceFromFix(IFix f)
        {
            if (f == null)
                throw new ArgumentNullException(nameof(f));
            return this.LatLong.DistanceFrom(f.LatLong);
        }

        #region Route Parsing Utilities
        /// <summary>
        /// Does this look like a US airport?
        /// </summary>
        /// <param name="szcode">The code</param>
        /// <returns>True if it looks like a US airport</returns>
        public static bool IsUSAirport(string szcode)
        {
            return szcode != null && szcode.Length == 4 && szcode.StartsWith(USAirportPrefix, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// To support the hack of typing "K" before an airport code in the US, we will see if Kxxx is hits on simply xxx
        /// </summary>
        /// <param name="szcode">The airport code</param>
        /// <returns>The code with the leading "K" stripped</returns>
        public static string USPrefixConvenienceAlias(string szcode)
        {
            if (szcode == null)
                throw new ArgumentNullException(nameof(szcode));
            return IsUSAirport(szcode) ? szcode.Substring(1) : szcode;
        }

        /// <summary>
        /// Given a string airport codes, splits them into an enumerable of codes 
        /// </summary>
        /// <param name="szAirports">The codes (e.g., "KSFO @LAX KPAE")</param>
        /// <returns>An enumerable of airport codes</returns>
        public static IEnumerable<string> SplitCodes(string szAirports)
        {
            if (szAirports == null)
                throw new ArgumentNullException(nameof(szAirports));
            List<string> lst = new List<string>();
            MatchCollection mc = regAirport.Matches(szAirports.ToUpper(CultureInfo.CurrentCulture));

            foreach (Match m in mc)
                lst.Add(m.Captures[0].Value);

            return lst;
        }
        #endregion

        #region Finding/querying airports
        protected static string DefaultSelectStatement(string szDistComp)
        {
            return String.Format(CultureInfo.InvariantCulture, "SELECT airports.*, navaidtypes.FriendlyName as FriendlyName, {0} AS dist FROM airports INNER JOIN navaidtypes ON (airports.Type = navaidtypes.code)", szDistComp);
        }

        /// <summary>
        /// Return a set of airports within the specified bounds
        /// Empty if more than 5 degrees width/height specified
        /// </summary>
        /// <param name="latSouth">Southern point of the bounds</param>
        /// <param name="lonWest">Western point of the bounds</param>
        /// <param name="latNorth">Northern point of the bounds</param>
        /// <param name="lonEast">Eastern point of the bounds</param>
        /// <returns>Matching airports</returns>
        public static IEnumerable<airport> AirportsWithinBounds(double latSouth, double lonWest, double latNorth, double lonEast, bool fIncludeHeliports)
        {
            List<airport> lst = new List<airport>();

            LatLong llSW = new LatLong(latSouth, lonWest);
            LatLong llNE = new LatLong(latNorth, lonEast);

            if (!llSW.IsValid || !llNE.IsValid)
                return lst;

            LatLongBox llb = new LatLongBox(new LatLong(latSouth, lonWest));
            llb.ExpandToInclude(new LatLong(latNorth, lonEast));

            if (llb.Width > 5.0 || llb.Height > 5.0)
                return lst;

            string szTypes = fIncludeHeliports ? "('A', 'S', 'H')" : "('A', 'S')";

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "{0} WHERE Type IN {1} AND Latitude BETWEEN ?lat1 AND ?lat2 AND Longitude BETWEEN ?lon1 AND ?lon2 LIMIT 200", airport.DefaultSelectStatement("0.0"), szTypes));
            dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("lat1", latSouth);
                    comm.Parameters.AddWithValue("lat2", latNorth);
                    comm.Parameters.AddWithValue("lon1", lonWest);
                    comm.Parameters.AddWithValue("lon2", lonEast);
                },
                (dr) => { lst.Add(new airport(dr)); }
            );
            return lst;
        }

        /// <summary>
        /// Returns a set of airports having the specified search words in the facility name.  All words must be contained, but can be in any order
        /// </summary>
        /// <param name="szSearchText">The words to find</param>
        /// <returns>A set of matching airports</returns>
        public static IEnumerable<airport> AirportsMatchingText(string szSearchText)
        {
            List<airport> lstAp = new List<airport>();

            if (szSearchText == null || String.IsNullOrEmpty(szSearchText.Trim()))
                return lstAp;

            string[] rgSearchTerms = RegexUtility.WhiteSpace.Split(szSearchText);

            DBHelper dbh = new DBHelper();
            dbh.ReadRows(
                (comm) =>
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string szTerm in rgSearchTerms)
                    {
                        string sz = szTerm.Trim();
                        if (!String.IsNullOrEmpty(sz))
                        {
                            if (sb.Length > 0)
                                sb.Append(" AND ");
                            string szParam = "AirportName" + comm.Parameters.Count.ToString(CultureInfo.InvariantCulture);
                            sb.AppendFormat(CultureInfo.InvariantCulture, "(CONCAT(FacilityName, ' ', AirportID) LIKE ?{0})", szParam);
                            comm.Parameters.AddWithValue(szParam, String.Format(CultureInfo.InvariantCulture, "%{0}%", sz));
                        }
                    }

                    comm.CommandText = String.Format(CultureInfo.InvariantCulture, "{0} WHERE {1} LIMIT 100",
                        airport.DefaultSelectStatement("0.0"),
                        sb.Length == 0 ? "AirportID IS NULL" : sb.ToString());
                },
                (dr) => { lstAp.Add(new airport(dr)); });

            return lstAp;
        }

        /// <summary>
        /// Adds a match candidate to the query
        /// </summary>
        /// <param name="sb">Stringbuilder holding the match clause</param>
        /// <param name="comm">The comm object (holds relevant parameters)</param>
        /// <param name="szCode">The airport code to match</param>
        private static void AddToQuery(StringBuilder sb, List<MySqlParameter> lst, string szCode)
        {
            if (sb.Length > 0)
                sb.Append(", ");

            string szParam = "AirportID" + lst.Count.ToString(CultureInfo.InvariantCulture);

            sb.Append(String.Format(CultureInfo.InvariantCulture, "?{0}", szParam));
            lst.Add(new MySqlParameter(szParam, szCode));
        }

        /// <summary>
        /// Returns a set of airports that match the specified codes
        /// </summary>
        /// <param name="codes">An enumerable of airport codes, possibly including ad-hoc fixes</param>
        /// <returns>The set of airports (possibly containing dupes such as KSFO and SFO) that match the codes</returns>
        public static IEnumerable<airport> AirportsMatchingCodes(IEnumerable<string> codes)
        {
            List<airport> al = new List<airport>();
            if (codes == null)
                return al;

            StringBuilder sb = new StringBuilder();

            List<MySqlParameter> lstParams = new List<MySqlParameter>();

            foreach (string szairport in codes)
            {
                if (szairport.Length > 0)
                {
                    // If it has the navaid prefix, then we want to find the name without the prefix (will hit the airport too - that's OK; when we resolve it, we'll match it back up and force the navaid)
                    // NOTE: We used to look for "K" prefix and add/remove it.  No more - both KSFO and SFO are in the DB now (and 400 more matches), so both will hit.
                    // To verify that this is still the case; here is the query to do so:
                    /*
                     select a1.airportid, a1.facilityname, a2.airportid, a2.facilityname 
                        from airports a1 
                        left join airports a2 ON a1.airportid=concat('K', a2.airportid)
                        where a1.airportid like 'K%' and length(a1.airportid) = 4 and a1.type='A' AND a2.airportid is null;

                     * */
                    if (szairport.StartsWith(ForceNavaidPrefix, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (regAdHocFix.IsMatch(szairport)) // adhoc fix - just make up the airport, don't add to query
                            al.Add(new AdHocFix(szairport.Replace(ForceNavaidPrefix, string.Empty)));
                        else
                            AddToQuery(sb, lstParams, szairport.Substring(ForceNavaidPrefix.Length));
                    }
                    else
                    {
                        AddToQuery(sb, lstParams, szairport);
                        if (IsUSAirport(szairport))
                            AddToQuery(sb, lstParams, USPrefixConvenienceAlias(szairport));
                    }
                }
            }

            if (sb.Length > 0)
            {
                DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "{0} WHERE airportID IN ({1})", airport.DefaultSelectStatement("0.0"), sb.ToString()));
                dbh.ReadRows(
                    (comm) => { comm.Parameters.AddRange(lstParams.ToArray()); },
                    (dr) => { al.Add(new airport(dr)); });
            }

            return al;
        }

        /// <summary>
        /// Returns an array of airports that are exact match hits for the identified airport codes
        /// </summary>
        /// <param name="szCodes">delimited string (any non-alpha char is a delimeter</param>
        /// <returns>The matches</returns>
        public static IEnumerable<airport> AirportsWithExactMatch(string szCodes)
        {
            string[] rgCodes = AirportList.NormalizeAirportList(szCodes);

            string szQ = String.Format(CultureInfo.InvariantCulture, "{0} WHERE airportID IN ('{1}') ORDER BY airportID ASC", DefaultSelectStatement("0.0"), String.Join("', '", rgCodes));
            List<airport> lst = new List<airport>();
            DBHelper dbh = new DBHelper(szQ);
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new airport(dr)); });
            return lst;
        }

        /// <summary>
        /// Returns a list of airports created by the given user
        /// </summary>
        /// <param name="szUser">The user in question</param>
        /// <param name="fAdmin">If true, show ALL user-defined airports</param>
        /// <returns>The airports/etc. created by that user</returns>
        public static IEnumerable<airport> AirportsForUser(string szUser, Boolean fAdmin)
        {
            ArrayList rgAirports = new ArrayList();
            string szQ = String.Format(CultureInfo.InvariantCulture, "{0} {1} ORDER BY AirportID", airport.DefaultSelectStatement("0.0"), fAdmin ? "WHERE SourceUsername <> '' " : "WHERE SourceUsername=?User ");
            DBHelper dbh = new DBHelper(szQ);
            dbh.ReadRows(
                (comm) => {
                    if (!fAdmin)
                        comm.Parameters.AddWithValue("User", szUser);
                },
                (dr) => { rgAirports.Add(new airport(dr)); });

            return (airport[])rgAirports.ToArray(typeof(airport));
        }

        /// <summary>
        /// Returns an array of airports closest to the specified position, in order of distance.  Only searches +/- 1.5 degree of lat/long.
        /// </summary>
        /// <param name="lat">Latitude of current position</param>
        /// <param name="lon">Longitude of current position</param>
        /// <param name="limit">Maximum number of results</param>
        /// <param name="fIncludeHeliports">Whether heliports are included</param>
        /// <returns>Array of airports</returns>
        static public IEnumerable<airport> AirportsNearPosition(double lat, double lon, int limit, Boolean fIncludeHeliports)
        {
            string szDistanceComp = String.Format(CultureInfo.InvariantCulture, "acos(sin(Radians(airports.latitude))*sin(Radians({0}))+cos(Radians(airports.latitude))*cos(Radians({0}))*cos(Radians({1}-airports.longitude)))*3440.06479", lat, lon);
            string szTemplate = "{0} WHERE (airports.latitude BETWEEN {1} AND {2})AND (airports.longitude BETWEEN {3} and {4}) AND (airports.type='A' OR airports.type='S' {5}) ORDER BY ROUND(dist, 2) ASC, LENGTH(airports.airportID) DESC, Preferred DESC LIMIT {6}";
            double minLat = Math.Max(lat - 1.5, -90.0);
            double maxLat = Math.Min(lat + 1.5, +90.0);
            double minLong = lon - 1.5;
            double maxLong = lon + 1.5;
            if (minLong < -180.0)
                minLong += 360;
            if (maxLong > 180.0)
                maxLong -= 180.0;
            if (minLong > maxLong)
                (maxLong, minLong) = (minLong, maxLong);    // using tuples to swap 

            string szQ = String.Format(CultureInfo.InvariantCulture, szTemplate, airport.DefaultSelectStatement(szDistanceComp), minLat, maxLat, minLong, maxLong, (fIncludeHeliports ? " OR airports.type='H'" : ""), limit);

            List<airport> rgAirports = new List<airport>();
            DBHelper dbh = new DBHelper(szQ);
            dbh.ReadRows(
                (comm) => { },
                (dr) => { rgAirports.Add(new airport(dr)); });

            return rgAirports.ToArray();
        }
        #endregion

        /// <summary>
        /// Determines if the owner of this airport object is allowed to modify it or create it.  
        /// Side effect: sets fIsNew, updates the username if we are admin and the airport exists.
        /// </summary>
        /// <returns>True if it is new and non-colliding, if it exists and you are the owner, or if you are the admin </returns>
        private Boolean FIsOwned()
        {
            isNew = false;

            // No editing anonymously.
            if (this.UserName.Length == 0)
            {
                ErrorText = "No username provided";
                return false;
            }

            // see if this is colliding
            airport[] rgAp = (new AirportList(this.Code)).GetAirportList();

            if (rgAp.Length == 0) // new, non-colliding airport - it's available for anyone to edit
            {
                isNew = true;
                return true;
            }

            airport apMatch = null;
            bool fMatchedPorts = false;
            bool fMatchedNavaids = false;
            foreach (airport ap in rgAp)
            {
                if (String.Compare(ap.Code, this.Code, StringComparison.CurrentCultureIgnoreCase) == 0 &&
                    String.Compare(ap.FacilityTypeCode, this.FacilityTypeCode, StringComparison.CurrentCultureIgnoreCase) == 0)
                    apMatch = ap;
                fMatchedPorts = fMatchedPorts || ap.IsPort;
                fMatchedNavaids = fMatchedNavaids || !ap.IsPort;
            }

            if (apMatch == null) // no true match (code + facilitytypecode) - we can add it if it doesn't cause two airports to collide
            {
                // We can't disambiguate two airports from each other, nor can we disambiguate two navaids.  Only navaids from airports
                if (IsPort && fMatchedPorts)
                {
                    ErrorText = String.Format(CultureInfo.CurrentCulture, AirportResources.errConflict, Code);
                    return false;
                }

                if (!IsPort && fMatchedNavaids)
                {
                    ErrorText = String.Format(CultureInfo.CurrentCulture, AirportResources.errConflictNavaid, Code);
                    return false;
                }

                isNew = true;
                return true;
            }

            // We have an exact match.
            // Can't edit if username doesn't match the user
            if (apMatch.UserName.Length == 0)
            {
                ErrorText = String.Format(CultureInfo.CurrentCulture, AirportResources.errBuiltInAirport, Code);
                return false;
            }

            // see if the returned object is owned by this user.
            // We've already checked above that this.username is not empty string, so
            // this can never return true for a built-in airport.
            if (String.Compare(apMatch.UserName, this.UserName, StringComparison.CurrentCultureIgnoreCase) != 0)
            {
                ErrorText = String.Format(CultureInfo.CurrentCulture, AirportResources.errNotYourAirport, Code);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Is this a valid airport or navaid?
        /// </summary>
        /// <returns>True if this is in a state to be saved</returns>
        public bool FValidate()
        {
            ErrorText = string.Empty;

            try
            {
                if (Code.Length < (IsPort ? minAirportCodeLength : minNavaidCodeLength))
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, AirportResources.errCodeTooShort, Code));

                if (Code.Length > maxCodeLength)
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, AirportResources.errCodeTooLong, Code));

                string[] airports = AirportList.NormalizeAirportList(Code);

                if (airports.Length != 1)
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, AirportResources.errIllegalCharacters, Code));

                if (Name.Length == 0)
                    throw new MyFlightbookException(AirportResources.errEmptyName);

                if (LatLong.Latitude == 0 && LatLong.Longitude == 0)
                    throw new MyFlightbookException(AirportResources.errEmptyLatLong);

                if (!LatLong.IsValid)
                    throw new MyFlightbookException(LatLong.ValidationError);

                if (Code.CompareCurrentCultureIgnoreCase("ZZZZ") == 0 || Code.CompareCurrentCultureIgnoreCase("AFIL") == 0)
                    throw new MyFlightbookException(AirportResources.errReservedCode);

                NavAidTypes[] rgNavAidTypes = NavAidTypes.GetKnownTypes();
                Boolean fIsKnownType = false;
                foreach (NavAidTypes navaidtype in rgNavAidTypes)
                    if (String.Compare(navaidtype.Code, this.FacilityTypeCode, StringComparison.CurrentCultureIgnoreCase) == 0)
                        fIsKnownType = true;
                if (!fIsKnownType)
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, AirportResources.errNotKnownType, this.Code));

                return true;
            }
            catch (MyFlightbookException ex)
            {
                ErrorText = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Add/update the airport for the current user - must be admin or owner, and non-conflicting
        /// </summary>
        /// <param name="fAdmin">True for admin override - USE WITH CARE!!!</param>
        /// <param name="fIsNew">True to overrid Is New - must also be admin</param>
        /// <returns>True for success</returns>
        public virtual Boolean FCommit(bool fAdmin = false, bool fForceNew = false)
        {
            if (FValidate() && (fAdmin || FIsOwned()))
            {
                string szSet = "SET airportID=?Code, FacilityName=?Name, airports.Latitude=?lat, airports.Longitude=?lon, Type=?Type, SourceUsername=?UserName";
                DBHelper dbh = new DBHelper();
                dbh.DoNonQuery(
                    ((isNew || (fAdmin && fForceNew)) ? "REPLACE INTO airports " + szSet : String.Format(CultureInfo.InvariantCulture,"UPDATE airports {0} WHERE airportID=?Code2 && Type=?Type2", szSet)),
                    (comm) =>
                    {
                        comm.Parameters.AddWithValue("Code2", this.Code);
                        comm.Parameters.AddWithValue("Type2", this.FacilityTypeCode);
                        comm.Parameters.AddWithValue("Code", this.Code.ToUpperInvariant());
                        comm.Parameters.AddWithValue("Name", this.Name);
                        comm.Parameters.AddWithValue("lat", this.LatLong.Latitude);
                        comm.Parameters.AddWithValue("lon", this.LatLong.Longitude);
                        comm.Parameters.AddWithValue("Type", this.FacilityTypeCode);
                        comm.Parameters.AddWithValue("UserName", this.UserName);
                    }
                );
                ErrorText = dbh.LastError;
            }
            return (ErrorText.Length == 0);
        }

        public void SetLocale(string szCountry, string szAdmin)
        {
            // Can't have Admin1 without country
            if (String.IsNullOrWhiteSpace(szCountry) && !String.IsNullOrWhiteSpace(szAdmin))
                throw new MyFlightbookValidationException("Must specify a country!");

            // Handle null, whitespace, or "NULL"
            szCountry = (String.IsNullOrWhiteSpace(szCountry) || szCountry.CompareCurrentCultureIgnoreCase("NULL") == 0) ? null : szCountry;
            szAdmin = (String.IsNullOrWhiteSpace(szAdmin) || szAdmin.CompareCurrentCultureIgnoreCase("NULL") == 0) ? null : szAdmin;

            Country = szCountry;
            Admin1 = szAdmin;
            DBHelper dbh = new DBHelper("UPDATE airports SET country = ?country, admin1 = ?admin1 WHERE airportID=?Code && Type=?Type");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("country", szCountry);
                comm.Parameters.AddWithValue("admin1", szAdmin);
                comm.Parameters.AddWithValue("Code", this.Code);
                comm.Parameters.AddWithValue("Type", this.FacilityTypeCode);
            });
            if (!String.IsNullOrEmpty(dbh.LastError))
                throw new MyFlightbookException("Error setting locale: " + dbh.LastError);
        }

        /// <summary>
        /// Deletes the airport.  Must be admin or owner.
        /// </summary>
        /// <returns></returns>
        public Boolean FDelete(bool fAdminForce)
        {
            if (FValidate() && (fAdminForce || FIsOwned()))
            {
                DBHelper dbh = new DBHelper();
                dbh.DoNonQuery("DELETE FROM airports WHERE airportID=?Code AND Type=?type",
                    (comm) =>
                    {
                        comm.Parameters.AddWithValue("Code", this.Code);
                        comm.Parameters.AddWithValue("type", this.FacilityTypeCode);
                    });
                ErrorText += dbh.LastError;
            }

            return (ErrorText.Length == 0);
        }

        /// <summary>
        /// Deletes an airport by code/type for a given user, if it exists for them
        /// </summary>
        /// <param name="Username"></param>
        /// <param name="Code"></param>
        /// <param name="TypeCode"></param>
        /// <param name="fAdmin">If true - and if the user is an admin - deletes an airport that may belong to someone else</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void DeleteAirportForUser(string Username, string Code, string TypeCode, bool fAdmin)
        {
            if (Username == null)
                throw new ArgumentNullException(nameof(Username));
            List<airport> lst = new List<airport>(airport.AirportsForUser(Username, fAdmin));
            airport ap = lst.FirstOrDefault(a => a.Code.CompareCurrentCultureIgnoreCase(Code) == 0 && a.FacilityTypeCode.CompareCurrentCultureIgnoreCase(TypeCode) == 0);
            ap?.FDelete(fAdmin);
        }

        public void ADMINReviewPotentialDupe(IEnumerable<airport> rgap)
        {
            if (rgap == null)
                throw new ArgumentNullException(nameof(rgap));

            // needs review
            StringBuilder sb = new StringBuilder();
            StringBuilder sbViewLink = new StringBuilder();
            sbViewLink.AppendFormat(CultureInfo.InvariantCulture, "https://{0}{1}?Airports={2}", Branding.CurrentBrand.HostName, "~/mvc/Airport/MapRoute".ToAbsolute(), Code);
            sb.AppendFormat(CultureInfo.CurrentCulture, "User: {0}, Airport: {1} ({2}) {3} {4}\r\n\r\nCould match:\r\n", UserName, Code, FacilityTypeCode, Name, LatLong.ToDegMinSecString());
            foreach (airport a in rgap)
            {
                sb.AppendFormat(CultureInfo.CurrentCulture, "{0} - ({1}) {2} {3}\r\n\r\n", a.Code, a.FacilityTypeCode, a.Name, a.LatLong.ToDegMinSecString());
                sbViewLink.AppendFormat(CultureInfo.InvariantCulture, "+{0}", a.Code);
            }
            sb.AppendFormat(CultureInfo.InvariantCulture, "\r\n\r\n{0}\r\n", sbViewLink.ToString());
            util.NotifyAdminEvent("New airport created - needs review", sb.ToString(), ProfileRoles.maskCanManageData);
        }
    }

    /// <summary>
    /// Admin Functionality for airports
    /// </summary>
    [Serializable]
    public class AdminAirport : airport
    {
        #region Properties
        public bool IsPreferred { get; set; }   // generally don't need to actually load this except for admin functions.

        public string SourceUserName { get; set; }  // we don't expose UserName in javascript for privacy, but need it for admin dupe detection

        public airport NearestTagged { get; set; } = null;
        #endregion

        #region constructors
        public AdminAirport() : base() { }

        public AdminAirport(MySqlDataReader dr) : base(dr) { }
        #endregion

        public static AdminAirport AirportWithCodeAndType(string szCode, string szType)
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "{0} WHERE airportID=?code AND type=?type", DefaultSelectStatement("0.0")));
            AdminAirport result = null;
            dbh.ReadRows((comm) =>
            {
                comm.Parameters.AddWithValue("code", szCode);
                comm.Parameters.AddWithValue("type", szType);
            },
            (dr) => { result = new AdminAirport(dr); });
            return result;
        }

        /// <summary>
        /// Finds airports in the specified country and/or admin region.  Pass null to search for one or the other being missing, empty string for "don't care"
        /// </summary>
        /// <param name="szCountry"></param>
        /// <param name="szAdmin"></param>
        /// <param name="start"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public static IEnumerable<AdminAirport> AirportsMatchingGeoReference(string szCountry, string szAdmin, int start, int limit)
        {
            List<AdminAirport> lst = new List<AdminAirport>();

            List<string> lstWhere = new List<string>() { "type IN ('A', 'H', 'S')" };
            if (szCountry == null)
                lstWhere.Add("(country IS NULL OR country='')");
            else if (!String.IsNullOrWhiteSpace(szCountry))
                lstWhere.Add("country=?country");
            if (szAdmin == null)
                lstWhere.Add("(admin1 IS NULL OR admin1='')");
            else if (!String.IsNullOrWhiteSpace(szAdmin))
                lstWhere.Add("admin1=?admin1");

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "{0} WHERE {1} LIMIT ?start, ?lim", DefaultSelectStatement("0.0"), String.Join(" AND ", lstWhere)));

            dbh.ReadRows((comm) =>
            {
                comm.Parameters.AddWithValue("country", szCountry);
                comm.Parameters.AddWithValue("admin1", szAdmin);
                comm.Parameters.AddWithValue("start", start);
                comm.Parameters.AddWithValue("lim", limit);
            },
              (dr) => { lst.Add(new AdminAirport(dr)); });

            return lst;
        }

        /// <summary>
        /// Finds clusters of potentially duplicate airports.
        /// Airports are duplicate if they are very close (within 0.02 latitude or longitude) to one another (but not exactly identical) and no preferred airport is indicated.
        /// If a seed airport ("dupeseed") is provided, then the constraints above are relaxed and instead ANY airport that is close to that airport is returned in a cluster
        /// </summary>
        /// <param name="start">Index of starting cluster</param>
        /// <param name="count">Number of clusters to return</param>
        /// <param name="dupeSeed">(optional) seed airport</param>
        /// <returns>A list of clusters of potential duplicate airports.</returns>
        public static IEnumerable<AdminAirport[]> GetDupeAirports(int start, int count, string dupeSeed)
        {
            string szLimit = string.Empty;
            if (!String.IsNullOrWhiteSpace(dupeSeed))
            {
                AirportList al = new AirportList(dupeSeed);
                airport[] rgap = al.GetNormalizedAirports();
                if (rgap.Length > 0)
                {
                    airport ap = rgap[0];
                    szLimit = String.Format(CultureInfo.InvariantCulture, " AND latitude between {0:F8} and {1:F8} and longitude between {2:F8} and {3:F8} ", ap.LatLong.Latitude - 0.02, ap.LatLong.Latitude + 0.02, ap.LatLong.Longitude - 0.02, ap.LatLong.Longitude + 0.02);
                }
            }

            // Issue #1117: if a seed is provided, don't restrict to num > 1 or differing lat/lon; just find all of the potential dupes.
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"select JSON_ARRAYAGG(JSON_OBJECT(""Code"", airportid, ""Name"", facilityname, ""FacilityTypeCode"", type, ""IsPreferred"", preferred, ""SourceUserName"", sourceusername,  ""LatLong"", JSON_OBJECT(""Latitude"", latitude, ""Longitude"", longitude), ""Country"", country, ""Admin1"", admin1)) as ports, type, count(*) as num, max(latitude)-min(latitude) as dlat, max(longitude)-min(Longitude) as dlon, max(Preferred) as hasPref
from airports 
where type in ('A', 'H', 'S') {0}
group by type, CONCAT(Round(latitude, 2), '/', round(longitude, 2))
{1}
order by type asc, num desc
limit ?start, ?count", szLimit, String.IsNullOrWhiteSpace(dupeSeed) ? "having num > 1 and ((dlat > 0 or dlon > 0)  OR hasPref=0)" : string.Empty));

            List<AdminAirport[]> lst = new List<AdminAirport[]>();

            // First pass: get all of the airport circles:
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("start", start); comm.Parameters.AddWithValue("count", count); },
            (dr) => { lst.Add(JsonConvert.DeserializeObject<AdminAirport[]>((string)dr["ports"])); });
            return lst;
        }

        public static IEnumerable<AdminAirport> UntaggedAirportsInBox(LatLongBox llb)
        {
            if (llb == null)
                throw new ArgumentNullException(nameof(llb));

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "{0} WHERE type IN ('A', 'H', 'S') AND country IS NULL AND latitude BETWEEN ?latmin AND ?latmax AND longitude BETWEEN ?lonmin AND ?lonMax",
                DefaultSelectStatement("0.0")));
            List<AdminAirport> lst = new List<AdminAirport>();
            dbh.ReadRows((comm) =>
            {
                comm.Parameters.AddWithValue("latmin", llb.LatMin);
                comm.Parameters.AddWithValue("latmax", llb.LatMax);
                comm.Parameters.AddWithValue("lonmin", llb.LongMin);
                comm.Parameters.AddWithValue("lonmax", llb.LongMax);
            }, (dr) => { lst.Add(new AdminAirport(dr)); });
            return lst;
        }

        /// <summary>
        /// Sets/unsets the preferred flag for this airport
        /// </summary>
        /// <param name="fPreferred"></param>
        public void SetPreferred(bool fPreferred)
        {
            DBHelper dbh = new DBHelper("UPDATE airports SET Preferred = ?pref WHERE airportID=?Code && Type=?Type");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("pref", fPreferred ? 1 : 0);
                comm.Parameters.AddWithValue("Code", this.Code);
                comm.Parameters.AddWithValue("Type", this.FacilityTypeCode);
            });
            if (!String.IsNullOrEmpty(dbh.LastError))
                throw new MyFlightbookException("Error Making preferred: " + dbh.LastError);
        }

        public void MakeNative()
        {
            if (String.IsNullOrEmpty(UserName))
                throw new MyFlightbookException("Airport is already native");

            DBHelper dbh = new DBHelper("UPDATE airports SET SourceUserName = '' WHERE airportID=?Code && Type=?Type");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("Code", this.Code);
                comm.Parameters.AddWithValue("Type", this.FacilityTypeCode);
            });
            if (!String.IsNullOrEmpty(dbh.LastError))
                throw new MyFlightbookException("Error Making Native: " + dbh.LastError);
        }

        /// <summary>
        /// Merges the latitude/longitude from the specified airport
        /// </summary>
        /// <param name="apSource"></param>
        /// <param name="MakeNative"></param>
        public void MergeFrom(airport apSource)
        {
            if (apSource == null)
                throw new ArgumentNullException(nameof(apSource));

            LatLong = apSource.LatLong;
            if (!FCommit(fAdmin:true))
                throw new MyFlightbookException("Error merging airport: " + ErrorText);
            if ((!String.IsNullOrWhiteSpace(apSource.Country) && String.IsNullOrWhiteSpace(Country)) || (!String.IsNullOrWhiteSpace(apSource.Admin1) && String.IsNullOrWhiteSpace(Admin1)))
                SetLocale(apSource.Country, apSource.Admin1);
        }

        /// <summary>
        /// Deletes the specified user airport, updating the routes in their flights as needed.
        /// </summary>
        /// <param name="szCode">The airport code to replace</param>
        /// <param name="szReplacement">The replacement code to which it should be mapped</param>
        /// <param name="szUser">The username - they MUST own the airport</param>
        /// <param name="szType">Airport type</param>
        public static void DeleteUserAirport(string szCode, string szReplacement, string szUser, string szType)
        {
            if (String.IsNullOrWhiteSpace(szCode))
                throw new ArgumentOutOfRangeException(nameof(szCode));
            if (String.IsNullOrWhiteSpace(szReplacement))
                throw new ArgumentOutOfRangeException(nameof(szReplacement));
            if (String.IsNullOrWhiteSpace(szUser))
                throw new ArgumentOutOfRangeException(nameof(szUser));
            if (String.IsNullOrWhiteSpace(szType))
                throw new ArgumentOutOfRangeException(nameof(szType));

            airport apToDelete = new airport(szCode, "(None)", 0, 0, szType, string.Empty, 0, szUser);

            List<airport> lst = new List<airport>(airport.AirportsForUser(szUser, false));
            if (lst.FirstOrDefault(ap => ap.Code.CompareCurrentCultureIgnoreCase(szCode) == 0 && ap.FacilityTypeCode.CompareCurrentCultureIgnoreCase(szType) == 0) == null)
                throw new UnauthorizedAccessException(String.Format(CultureInfo.CurrentCulture, "Airport {0} (type {1}) not found for user", szCode, szType));

            if (apToDelete.FDelete(true))
            {
                if (!String.IsNullOrEmpty(szUser))
                {
                    DBHelper dbh = new DBHelper("UPDATE flights SET route=REPLACE(route, ?idDelete, ?idMap) WHERE username=?user AND route LIKE CONCAT('%', ?idDelete, '%')");
                    if (!dbh.DoNonQuery((comm) =>
                    {
                        comm.Parameters.AddWithValue("idDelete", szCode);
                        comm.Parameters.AddWithValue("idMap", szReplacement);
                        comm.Parameters.AddWithValue("user", szUser);
                    }))
                        throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Error mapping from {0} to {1} in flights for user {2}: {3}", szCode, szReplacement, szUser, dbh.LastError));
                }
            }
            else
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Error deleting airport {0}: {1}", apToDelete.Code, apToDelete.ErrorText));
        }

        private static int BackfillOldCodes(int iColOldCode, int iColCurrentCode, CSVReader csvr)
        {
            int cAirports = 0;
            string szSelect = String.Format(CultureInfo.InvariantCulture,
                    "{0} {1}",
                    airport.DefaultSelectStatement("0.0"),
                    @"INNER JOIN (SELECT * FROM airports WHERE airportid=?idTarget AND type in ('A', 'S', 'H')) aptarget 
                        ON airports.type=aptarget.type AND abs(airports.latitude - aptarget.latitude) < .02 and abs(airports.longitude - aptarget.longitude) < 0.02 
                        ORDER BY airports.preferred DESC, LENGTH(airports.AirportID) DESC");
            DBHelper dbh = new DBHelper(szSelect);
            StringBuilder sb = new StringBuilder();

            string[] rgCols;
            while ((rgCols = csvr.GetCSVLine()) != null)
            {
                string szOld = rgCols[iColOldCode];
                string szCurrent = rgCols[iColCurrentCode];

                if (String.IsNullOrEmpty(szOld) || String.IsNullOrEmpty(szCurrent))
                {
                    sb.AppendFormat(CultureInfo.CurrentCulture, "Skipping row: {0} {1} (one or both is empty)", szOld, szCurrent);
                    continue;
                }

                bool fHasPreferred = false;

                // Get everything close to the "current airport"
                List<AdminAirport> lstApExisting = new List<AdminAirport>();
                dbh.ReadRows((comm) => { comm.Parameters.Clear(); comm.Parameters.AddWithValue("idTarget", szCurrent); },
                    (dr) => { 
                        lstApExisting.Add(new AdminAirport(dr));
                        fHasPreferred = fHasPreferred || Convert.ToInt32(dr["preferred"], CultureInfo.InvariantCulture) != 0;
                    });

                // if the "current" airport isn't in the system, then we don't have enough information to even do anything
                // And if the "old" airport is in the system, there's nothing to do
                if (lstApExisting.Count == 0 || lstApExisting.Exists(ap => ap.Code.CompareCurrentCultureIgnoreCase(szOld) == 0))
                    continue;

                // verify that the code we want to add doesn't exist in any form - e.g., may not have been found above due to it being in some other location
                if (airport.AirportsMatchingCodes(new string[] { szOld }).Any())
                    continue;

                // Make sure at least one of the "current" airports is marked as preferred.
                if (!fHasPreferred)
                    lstApExisting[0].SetPreferred(true);

                // Now copy that airport 
                sb.AppendFormat(CultureInfo.CurrentCulture, "Adding '{0}' as an alias for '{1}' ({2})", szOld, lstApExisting[0].Code, lstApExisting[0].Name);
                airport apOld = lstApExisting[0];
                apOld.Code = szOld;
                apOld.Name += " (Obsolete)";
                apOld.FCommit(true, true);
                cAirports++;
            }

            return cAirports;
        }

        const int iColID = 0;
        const int iColName = 1;
        const int iColType = 2;
        const int iColSourceUserName = 3;
        const int iColLat = 4;
        const int iColLon = 5;
        const int iColPreferred = 6;
        const int iColOldCode = 7;
        const int iColCurrentCode = 8;
        const int iColCountry = 9;
        const int iColAdmin1 = 10;

        private static void MapColumnHeader(string[] rgheaders, Dictionary<int, int> columnMap)
        {
            for (int i = 0; i < rgheaders.Length; i++)
            {
                switch (rgheaders[i].ToUpperInvariant())
                {
                    case "ICAO":
                    case "IATA":
                    case "FAA":
                    case "CODE":
                    case "AIRPORTID":
                        columnMap[iColID] = i;
                        break;
                    case "NAME":
                    case "FACILITYNAME":
                        columnMap[iColName] = i;
                        break;
                    case "TYPE":
                        columnMap[iColType] = i;
                        break;
                    case "SOURCEUSERNAME":
                        columnMap[iColSourceUserName] = i;
                        break;
                    case "LATITUDE":
                        columnMap[iColLat] = i;
                        break;
                    case "LONGITUDE":
                        columnMap[iColLon] = i;
                        break;
                    case "PREFERRED":
                        columnMap[iColPreferred] = i;
                        break;
                    case "OLDCODE":
                        columnMap[iColOldCode] = i;
                        break;
                    case "CURRENTCODE":
                        columnMap[iColCurrentCode] = i;
                        break;
                    case "COUNTRY":
                        columnMap[iColCountry] = i;
                        break;
                    case "ADMIN1":
                        columnMap[iColAdmin1] = i;
                        break;
                }
            }
        }

        public static int BulkImportAirports(Stream s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            int cAirportsAdded = 0;

            Dictionary<int, int> columnMap = new Dictionary<int, int>();

            using (CSVReader csvReader = new CSVReader(s))
            {
                try
                {
                    string[] rgheaders = csvReader.GetCSVLine(true);
                    MapColumnHeader(rgheaders, columnMap);

                    // Look for just backfilling of old codes: oldcode column and currentcode column
                    if (columnMap.TryGetValue(iColOldCode, out int valueOld) && columnMap.TryGetValue(iColCurrentCode, out int valueNue))
                        return BackfillOldCodes(valueOld, valueNue, csvReader);

                    if (!columnMap.ContainsKey(iColID) || !columnMap.ContainsKey(iColName) || !columnMap.ContainsKey(iColType) || !columnMap.ContainsKey(iColLat) || !columnMap.ContainsKey(iColLon) || !columnMap.ContainsKey(iColCountry) || !columnMap.ContainsKey(iColAdmin1))
                        throw new MyFlightbookValidationException("File doesn't have all required columns.");

                    bool fHasPreferred = columnMap.ContainsKey(iColPreferred);
                    bool fHasUser = columnMap.ContainsKey(iColSourceUserName);

                    string[] rgCols;
                    while ((rgCols = csvReader.GetCSVLine()) != null)
                    {
                        AdminAirport ap = new AdminAirport()
                        {
                            Code = rgCols[columnMap[iColID]],
                            Name = rgCols[columnMap[iColName]],
                            LatLong = new LatLong(Convert.ToDouble(rgCols[columnMap[iColLat]], CultureInfo.CurrentCulture), Convert.ToDouble(rgCols[columnMap[iColLon]], CultureInfo.CurrentCulture)),
                            FacilityTypeCode = rgCols[columnMap[iColType]],
                            UserName = fHasUser ? rgCols[columnMap[iColSourceUserName]] : string.Empty,
                            Country = rgCols[columnMap[iColCountry]],
                            Admin1 = rgCols[columnMap[iColAdmin1]]
                        };

                        if (!ap.FCommit(true, true))
                            throw new MyFlightbookException(ap.ErrorText);

                        if (!String.IsNullOrEmpty(ap.Country))
                            ap.SetLocale(ap.Country, ap.Admin1);

                        ++cAirportsAdded;

                        if (fHasPreferred && !String.IsNullOrEmpty(rgCols[columnMap[iColPreferred]]) && Int32.TryParse(rgCols[columnMap[iColPreferred]], NumberStyles.Integer, CultureInfo.CurrentCulture, out int preferred) && preferred != 0)
                            ap.SetPreferred(true);
                    }

                }
                catch (Exception ex) when (ex is CSVReaderInvalidCSVException || ex is MyFlightbookException || ex is MyFlightbookValidationException)
                {
                    throw new MyFlightbookException(ex.Message, ex);
                }

                return cAirportsAdded;
            }
        }

        #region Geolocating from GPX
        protected static void GeoReferenceForPoly(List<LatLong> rgll, string szCountry, string szAdmin, StringBuilder UpdateString, StringBuilder AuditString)
        {
            if (rgll == null)
                throw new ArgumentNullException(nameof(rgll));
            if (szCountry == null)
                throw new ArgumentNullException(nameof(szCountry));
            if (UpdateString == null)
                throw new ArgumentNullException(nameof(UpdateString));
            if (AuditString == null)
                throw new ArgumentNullException(nameof(AuditString));

            if (rgll.Count == 0)
                throw new InvalidOperationException("No latitude/longitudes in rgll!");


            // Get the bounding box.
            LatLongBox llb = new LatLongBox(rgll.First());
            foreach (LatLong ll in rgll)
                llb.ExpandToInclude(ll);

            IEnumerable<airport> rgap = AdminAirport.UntaggedAirportsInBox(llb);
            GeoRegion geo = new GeoRegion(string.Empty, rgll);

            int cAirports = 0;

            foreach (airport ap in rgap)
                if (geo.ContainsLocation(ap.LatLong))
                {
                    UpdateString.AppendLine(String.Format(CultureInfo.InvariantCulture, "UPDATE airports SET Country='{0}' {1} WHERE type='{2}' AND airportID='{3}';", szCountry, String.IsNullOrWhiteSpace(szAdmin) ? string.Empty : String.Format(CultureInfo.InvariantCulture, ", admin1 = '{0}' ", szAdmin), ap.FacilityTypeCode, ap.Code));
                    ap.SetLocale(szCountry, szAdmin);
                    cAirports++;
                }

            if (cAirports > 0)
                AuditString.AppendLine(String.Format(CultureInfo.CurrentCulture, "Updated {0} airports for country {1}, region {2}", cAirports, szCountry, szAdmin));
        }

        public static void GeoLocateFromGPX(Stream content, out string szAudit, out string szCommands)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            szAudit = szCommands = string.Empty;
            StringBuilder AuditString = new StringBuilder();
            StringBuilder UpdateString = new StringBuilder();
            string szActiveCountry = string.Empty;
            string szActiveAdmin1 = string.Empty;
            string szName = string.Empty;
            string szVal = string.Empty;
            bool fCountry = false;
            LatLong llActive = null;
            List<LatLong> lstPoly = new List<LatLong>();

            using (XmlReader reader = XmlReader.Create(content))
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.Name.ToUpperInvariant())
                            {
                                default:
                                    break;
                                case "TRKPT":
                                    llActive = new LatLong(Convert.ToDouble(reader.GetAttribute("lat"), CultureInfo.InvariantCulture), Convert.ToDouble(reader.GetAttribute("lon"), CultureInfo.InvariantCulture));
                                    break;
                                case "TRKSEG":
                                    // We should have all of the metadata
                                    if (fCountry)
                                    {
                                        szActiveCountry = szName;
                                        szActiveAdmin1 = null;
                                    }
                                    else
                                    {
                                        szActiveAdmin1 = szName;
                                        // szActiveCountry should already be set.
                                    }
                                    lstPoly.Clear();
                                    break;
                            }
                            break;
                        case XmlNodeType.Text:
                            szVal = reader.Value;
                            break;
                        case XmlNodeType.EndElement:
                            switch (reader.Name.ToUpperInvariant())
                            {
                                default:
                                    break;
                                case "NAME":
                                    szName = szVal; // Could be a country or an admin region.
                                    break;
                                case "OGR:ADMIN":
                                    szActiveCountry = szVal;    // always the country, in our case
                                    break;
                                case "TRKPT":
                                    lstPoly.Add(llActive);
                                    llActive = null;
                                    break;
                                case "OGR:FEATURECLA":
                                    fCountry = szVal.StartsWith("Admin-0", StringComparison.CurrentCultureIgnoreCase);
                                    break;
                                case "TRKSEG":
                                    // We've completed one polygon for this region - georeference it
                                    GeoReferenceForPoly(lstPoly, szActiveCountry, szActiveAdmin1, UpdateString, AuditString);
                                    break;
                                case "TRK":
                                    // We're done with this region
                                    szName = szActiveAdmin1 = szActiveAdmin1 = string.Empty;
                                    fCountry = false;
                                    lstPoly.Clear();
                                    break;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            szAudit = AuditString.ToString();
            szCommands = UpdateString.ToString();
        }

        /// <summary>
        /// Given a prefix, suggests country names up to the specified limit
        /// </summary>
        /// <param name="prefixText">Prefix</param>
        /// <param name="count">Max # to return</param>
        /// <returns>Country name</returns>
        public static IEnumerable<string> SuggestCountries(string prefixText, int count)
        {
            if (String.IsNullOrEmpty(prefixText))
                return Array.Empty<string>();
            List<string> lst = new List<string>();
            DBHelper dbh = new DBHelper("SELECT distinct Country from Airports where Country like ?prefix ORDER BY Country ASC LIMIT ?count");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("prefix", "%" + prefixText + "%"); comm.Parameters.AddWithValue("count", count); },
                (dr) => { lst.Add((string)dr["Country"]); });
            return lst;
        }

        /// <summary>
        /// Given a prefix, suggests administrative regions ("admin1" - a state or province level) up to the specified limit
        /// </summary>
        /// <param name="prefixText">Prefix</param>
        /// <param name="count">Max # to return</param>
        /// <returns>Admin1  name</returns>
        public static IEnumerable<string> SuggestAdmin1(string prefixText, int count)
        {
            if (String.IsNullOrEmpty(prefixText))
                return Array.Empty<string>();
            List<string> lst = new List<string>();
            DBHelper dbh = new DBHelper("SELECT distinct Admin1 from Airports where Admin1 like ?prefix ORDER BY Admin1 ASC LIMIT ?count");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("prefix", "%" + prefixText + "%"); comm.Parameters.AddWithValue("count", count); },
                (dr) => { lst.Add((string)dr["Admin1"]); });
            return lst;
        }
        #endregion
    }

    /// <summary>
    /// Ad-hoc fix - a fix in space that is not named.
    /// </summary>
    [Serializable]
    public class AdHocFix : airport
    {
        public AdHocFix() : base()
        { }

        public AdHocFix(string szDMS) : this()
        {
            this.LatLong = DMSAngle.LatLonFromDMSString(szDMS);
            NavAidTypes nat = Array.Find<NavAidTypes>(NavAidTypes.GetKnownTypes(), n => String.Compare(n.Code, "FX", StringComparison.CurrentCultureIgnoreCase) == 0);
            this.FacilityTypeCode = nat.Code;
            this.FacilityType = nat.Name;
            this.Code = szDMS;
            this.Name = this.LatLong.ToDegMinSecString();
            this.DistanceFromPosition = 0;
            this.UserName = string.Empty;
        }

        public override bool FCommit(bool fAdmin = false, bool fForceNew = false)
        {
            throw new InvalidOperationException("Cannot commit an adhoc fix!");
        }
    }
}
