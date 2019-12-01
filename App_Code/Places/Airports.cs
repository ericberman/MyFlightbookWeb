using MyFlightbook.Geography;
using MyFlightbook.Image;
using MyFlightbook.Telemetry;
using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2010-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/


namespace MyFlightbook.Airports
{
    /// <summary>
    /// A set of known navigational aid types such as VORs, NDBs, etc.
    /// </summary>
    public class NavAidTypes
    {
        private static NavAidTypes[] _rgKnownTypes = null;

        #region Properties
        /// <summary>
        /// Abbreviation for the type of navaid
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Friendly name for the type of navaid
        /// </summary>
        public string Name { get; set; }
        #endregion

        public NavAidTypes(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public static NavAidTypes[] GetKnownTypes()
        {
            if (_rgKnownTypes != null)
                return _rgKnownTypes;

            List<NavAidTypes> lst = new List<NavAidTypes>();
            DBHelper dbh = new DBHelper("SELECT * FROM NavaidTypes");

            if (!dbh.ReadRows(
                (comm) => { },
                (dr) => { lst.Add(new NavAidTypes(dr["Code"].ToString(), dr["FriendlyName"].ToString())); }))
                throw new MyFlightbookException("Error getting known navaid types: " + dbh.LastError);

            return (_rgKnownTypes = lst.ToArray());
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} - {1}", Code, Name);
        }
    }

    /// <summary>
    /// A record of a visit to an airport.  Does not derive from airport, but includes an airport object which can be null under some circumstances.
    /// </summary>
    [Serializable]
    public class VisitedAirport : IComparable
    {
        #region Properties
        public string Code
        {
            get { return Airport.Code; }
            set { Airport.Code = value; }
        }

        public string FacilityName
        {
            get { return Airport.FullName; }
        }

        /// <summary>
        /// If an airport has multiple codes (e.g., PHOG/OGG, or even KSFO/SFO), this has the alternatives by which it could be known (for searching)
        /// </summary>
        public string Aliases { get; set; }

        public void AddAlias(string szAlias)
        {
            if (String.IsNullOrEmpty(Aliases))
                Aliases = szAlias;
            else
                Aliases = String.Format(CultureInfo.CurrentCulture, "{0},{1}", Aliases, szAlias);
        }

        /// <summary>
        /// If an airport has multiple codes (e.g., PHOG/OGG, or even KSFO/SFO), this has all of the codes by which it could be known.
        /// </summary>
        public string AllCodes { get { return String.IsNullOrEmpty(Aliases) ? Code : String.Format(CultureInfo.CurrentCulture, "{0},{1}", Code, Aliases); } }

        public airport Airport {get; set;}

        public DateTime EarliestVisitDate { get; set;}

        public DateTime LatestVisitDate { get; set;}

        public int NumberOfVisits {get; set;}

        /// <summary>
        /// Internally cached ID of the flight where this airport was first encountered.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public int FlightIDOfFirstVisit { get; private set; }
        #endregion

        #region IComparable
        public int CompareTo(object obj)
        {
            return Airport.CompareTo(((VisitedAirport)obj).Airport);
        }
        #endregion

        #region Object creation
        public VisitedAirport()
        {
            Aliases = string.Empty;
            EarliestVisitDate = LatestVisitDate = DateTime.MinValue;
            NumberOfVisits = 0;
            FlightIDOfFirstVisit = LogbookEntry.idFlightNone;
        }

        public VisitedAirport(DateTime dtVisited) : this()
        {
            EarliestVisitDate = LatestVisitDate = dtVisited;
            NumberOfVisits = 1;
        }
        #endregion

        /// <summary>
        /// Record a visit to the airport on the specified date
        /// </summary>
        /// <param name="dt">The date of the visit</param>
        private void VisitAirport(DateTime dt, int idFlight)
        {
            if (EarliestVisitDate.CompareTo(dt) > 0)
            {
                EarliestVisitDate = dt;
                FlightIDOfFirstVisit = idFlight;
            }
            if (LatestVisitDate.CompareTo(dt) < 0)
                LatestVisitDate = dt;
            NumberOfVisits++;
        }

        /// <summary>
        /// Merge with another visited airport.  E.g., Maui can be OGG (IATA) or PHOG (ICAO).  If you've visited both, then this creates a single record with an alias.        /// 
        /// </summary>
        /// <param name="va">The visited airport with which to merge</param>
        private void MergeWith(VisitedAirport va)
        {
            if (va == null)
                throw new ArgumentNullException("va");
            if (EarliestVisitDate.CompareTo(va.EarliestVisitDate) > 0)
            {
                EarliestVisitDate = va.EarliestVisitDate;
                FlightIDOfFirstVisit = va.FlightIDOfFirstVisit;
            }
            if (LatestVisitDate.CompareTo(va.LatestVisitDate) < 0)
                LatestVisitDate = va.LatestVisitDate;
            NumberOfVisits += va.NumberOfVisits;

            if (va.Airport != null)
                AddAlias(va.Code);
        }

        #region Getting visited airports
        /// <summary>
        /// Returns a set of visited airports matching the specified flight query
        /// </summary>
        /// <param name="fq">The flight query</param>
        /// <returns>A set of visited airports</returns>
        public static VisitedAirport[] VisitedAirportsForQuery(FlightQuery fq)
        {
            if (String.IsNullOrEmpty(fq.UserName))
                throw new ArgumentNullException("fq");

            AirportList al = new AirportList();
            Dictionary<string, VisitedAirport> dictVA = new Dictionary<string, VisitedAirport>();

            DBHelper dbh = new DBHelper(LogbookEntry.QueryCommand(fq, 0, -1, true));

            dbh.ReadRows(
                (comm) => { },
                (dr) =>
                {
                    DateTime dtFlight = Convert.ToDateTime(dr["date"], CultureInfo.InvariantCulture);
                    string szRoute = dr["route"].ToString();
                    int idFlight = Convert.ToInt32(dr["idflight"], CultureInfo.InvariantCulture);

                    // we want to defer any db hit to the airport list until later, so we create an uninitialized airportlist
                    // We then visit each airport in the flight.
                    string[] rgszapFlight = AirportList.NormalizeAirportList(szRoute);

                    for (int iAp = 0; iAp < rgszapFlight.Length; iAp++)
                    {
                        string szap = rgszapFlight[iAp].ToUpper();

                        // If it's explicitly a navaid, ignore it
                        if (szap.StartsWith(airport.ForceNavaidPrefix))
                            continue;

                        VisitedAirport va = dictVA.ContainsKey(szap) ? dictVA[szap] : null;

                        // Heuristic: if the flight only has a single airport, we visit that airport
                        // BUT if the flight has multiple airport codes, ignore the first airport in the list 
                        // UNLESS we've never seen that airport before.  (E.g., fly commercial to Stockton to pick up 40FG).
                        if (iAp == 0 && va != null && rgszapFlight.Length > 1)
                            continue;

                        // for now, the key holds the airport code, since the airport itself within the visited airport is still null
                        if (va == null)
                            dictVA[szap] = va = new VisitedAirport(dtFlight) { FlightIDOfFirstVisit = idFlight };
                        else
                            va.VisitAirport(dtFlight, idFlight);
                    }
                });

            // We now have a hashtable of visited airports.  We need to initialize the airport element in each of these.
            string[] rgCodes = new string[dictVA.Keys.Count];
            dictVA.Keys.CopyTo(rgCodes, 0);
            string szAll = String.Join(" ", rgCodes);

            // do a single db hit to get all of the airport information
            // Rebuild the airport list based on ONLY those airports that returned a result

            // Build a dictionary of found airports from which we can build our visited airports
            Dictionary<string, airport> dictAirportResults = new Dictionary<string, airport>();
            Array.ForEach<airport>(new AirportList(szAll).GetAirportList(), (ap) => 
                {
                    if (ap.IsPort)
                        dictAirportResults[ap.Code] = ap;
                });

            // We now have a list of all visited airports as the user typed them (in dictVA), and of all airports (in dictAirportResults)
            // We need to match the two up.
            // Initialize any airports we found
            foreach (string szCode in dictVA.Keys)
            {
                VisitedAirport va = dictVA[szCode];

                string szKey = string.Empty;

                if (dictAirportResults.ContainsKey(szCode)) // exact match
                    szKey = szCode;
                else
                {
                    // check for the K variant (e.g., user typed XXX and KXXX got returned)
                    string szKHack = airport.USAirportPrefix + szCode;
                    if (airport.IsUSAirport(szKHack) && dictAirportResults.ContainsKey(szKHack))
                        szKey = szKHack;
                    else
                    {
                        // Check for the un-K variant (e.g., user typed Kxxx but xxx got returned
                        if (szCode.StartsWith(airport.USAirportPrefix))
                        {
                            string szKUnhack = airport.USPrefixConvenienceAlias(szCode);
                            if (dictAirportResults.ContainsKey(szKUnhack))
                                szKey = szKUnhack;
                        }
                    }
                }

                if (!String.IsNullOrEmpty(szKey))
                    va.Airport = dictAirportResults[szKey];
            }

            /*
             * Need to de-dupe by location:
             *  - AST example: AST is in the DB but KAST is not.  So if KAST has a null airport but the AST version exists, merge KAST->AST
             *  - KNZC: KNZC is in the DB, but NZC is not.  Merge in the other direction (towards KNZC)
             *  - KSFO/SFO are both in the DB.  Merge to the naked (SFO) version, if only because when we search for flights SFO will hit airports more liberally.
             *  - OGG/PHOG or CHC/NZCH - go to the longer one; we'll do this AFTER the K-hack ones because the K-hack may be dealing with non-existent airports, but non-K-hack ALWAYS has real airports.
             * */
            foreach (string key in dictVA.Keys)
            {
                if (airport.IsUSAirport(key)) // if this is the Kxxx version...
                {
                    string szKUnhack = airport.USPrefixConvenienceAlias(key);
                    VisitedAirport vaKPrefix = dictVA.ContainsKey(key) ? dictVA[key] : null;   // should never be null by definition - it's in "keys" collection.
                    VisitedAirport vaNoPrefix = dictVA.ContainsKey(szKUnhack) ? dictVA[szKUnhack] : null;

                    if (vaNoPrefix != null)
                    {
                        // KNZC example: this is KNZC, but "NZC" doesn't have an airport: Merge NZC into KNZC and keep KNZC; NZC will be discarded below.
                        if (vaNoPrefix.Airport == null)
                            vaKPrefix.MergeWith(vaKPrefix);

                        // AST/KAST example: this is KAST and has no airport, but the convenience alias did: Merge KAST into AST; KAST will be discarded below.
                        if (vaKPrefix.Airport == null && vaNoPrefix.Airport != null)
                            vaNoPrefix.MergeWith(vaKPrefix);

                        // SFO/KSFO exmaple: this is KSFO and has an airport, but so does SFO.
                        // But only if the airports are essentially the same location
                        // Merge KSFO into SFO and nullify KSFO; it will be discarded below.
                        if (vaKPrefix.Airport != null && vaNoPrefix.Airport != null &&
                            vaKPrefix.Airport.LatLong.IsSameLocation(vaNoPrefix.Airport.LatLong))
                        {
                            vaKPrefix.MergeWith(vaNoPrefix);
                            vaNoPrefix.Airport = null; // we've merged; don't return the K version.
                        }
                    }
                }
            }

            // Now need to merge any remaining airports with multiple codes but the same location.  E.g., OGG and PHOG or CHC and NZCH
            Dictionary<string, VisitedAirport> dDedupe = new Dictionary<string, VisitedAirport>();
            foreach (VisitedAirport va in dictVA.Values)
            {
                if (va.Airport == null)
                    continue;
                string szLocKey = va.Airport.GeoHashKey;

                // If the code is already present AND it has a longer code than this one, then merge them
                if (dDedupe.ContainsKey(szLocKey))
                {
                    VisitedAirport vaDupe = dDedupe[szLocKey];
                    if (vaDupe.Code.Length > va.Code.Length)
                    {
                        vaDupe.MergeWith(va);
                        va.Airport = null;  // we've merged, don't return this one.
                        continue;
                    }
                    else
                    {
                        va.MergeWith(vaDupe);   // we'll replace vadupe with va below.
                        vaDupe.Airport = null;
                    }
                }

                // Otherwise, store this one.
                dDedupe[szLocKey] = va;
            }

            // copy the ones with matching airports into an array list
            List<VisitedAirport> lstResults = new List<VisitedAirport>(dDedupe.Values);

            lstResults.Sort();
            return lstResults.ToArray();
        }

        /// <summary>
        /// Returns a set of visited airports for the user, including all of their flights.
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <returns>A set of visited airports.</returns>
        public static VisitedAirport[] VisitedAirportsForUser(string szUser)
        {
            return VisitedAirportsForQuery(new FlightQuery(szUser));
        }
        #endregion

        public override string ToString()
        {
            return Airport == null ? base.ToString() : Airport.ToString();
        }

        #region Total distance and flights
        /// <summary>
        /// Gets all routes ever flown, appends them together and INCLUDES any relevant navaids.
        /// </summary>
        /// <returns></returns>
        private static AirportList AllFlightsAndNavaids(FlightQuery fq)
        {
            DBHelper dbh = new DBHelper(LogbookEntry.QueryCommand(fq));
            StringBuilder sb = new StringBuilder();
            dbh.ReadRows((comm) => { }, (dr) => { sb.AppendFormat("{0} ", util.ReadNullableField(dr, "Route", string.Empty)); });
            return new AirportList(sb.ToString());
        }

        /// <summary>
        /// Examines all of the relevant flights for the specified query. 
        /// </summary>
        /// <param name="dbh">Query that returns the relevant flights</param>
        /// <param name="action">Action that takes flight data, route, date, and comments.  DO NOT dispose of the FlightData - it's owned by THIS.</param>
        private static void LookAtAllFlights(DBHelper dbh, Action<TelemetryReference, string, DateTime, string> action)
        {
            dbh.ReadRows(
                (comm) => { },
                (dr) =>
                {
                    object o = dr["Route"];
                    string szRoute = (string)(o == System.DBNull.Value ? "" : o);
                    o = dr["FlightData"];
                    string szData = (string)(o == System.DBNull.Value ? "" : o);
                    TelemetryReference tr = new TelemetryReference(dr) { RawData = szData };

                    if (tr.HasPath && String.IsNullOrEmpty(szData))
                        szData = tr.LoadData();

                    action(tr, szRoute, Convert.ToDateTime(dr["Date"], CultureInfo.InvariantCulture), dr["Comments"].ToString());
                });
        }

        /// <summary>
        /// Estimates the total distance flown by the user for the subset of flights described by the query
        /// </summary>
        /// <param name="fq">The flight query</param>
        /// <param name="error">Any error</param>
        /// <returns>Distance flown, in nm</returns>
        public static double DistanceFlownByUser(FlightQuery fq, out string error)
        {
            if (fq == null)
                throw new ArgumentNullException("fq");
            if (String.IsNullOrEmpty(fq.UserName))
                throw new MyFlightbookException("Don't estimate distance for an empty user!!");

            double distance = 0.0;

            // Get the master airport list
            AirportList alMaster = AllFlightsAndNavaids(fq);

            DBHelper dbh = new DBHelper(LogbookEntry.QueryCommand(fq, lto:LogbookEntry.LoadTelemetryOption.MetadataOrDB));
            LookAtAllFlights(
                dbh,
                (TelemetryReference tr, string szRoute, DateTime dt, string szComment) =>
                {
                    double dPath = tr.Distance();

                    // If the trajectory had a distance, use it; otherwise, use airport-to-airport.
                    if (dPath > 0)
                        distance += dPath;
                    else if (!String.IsNullOrEmpty(szRoute))
                        distance += alMaster.CloneSubset(szRoute).DistanceForRoute();
                });

            error = dbh.LastError;
            return distance;
        }

        /// <summary>
        /// Returns a KML respresentation of all of the flights represented by the specified query
        /// </summary>
        /// <param name="fq">The flight query</param>
        /// <param name="s">The stream to which to write</param>
        /// <param name="error">Any error</param>
        /// <param name="lstIDs">The list of specific flight IDs to request</param>
        /// <returns>KML string for the matching flights.</returns>
        public static void AllFlightsAsKML(FlightQuery fq, Stream s, out string error, IEnumerable<int> lstIDs = null)
        {
            if (fq == null)
                throw new ArgumentNullException("fq");
            if (String.IsNullOrEmpty(fq.UserName) && (lstIDs == null || lstIDs.Count() == 0))
                throw new MyFlightbookException("Don't get all flights as KML for an empty user!!");

            if (lstIDs != null)
                fq.CustomRestriction = String.Format(CultureInfo.InvariantCulture, " flights.idFlight IN ({0}) ", String.Join(", ", lstIDs));

            string szKML = string.Empty;

            // Get the master airport list
            AirportList alMaster = AllFlightsAndNavaids(fq);

            DBHelper dbh = new DBHelper(LogbookEntry.QueryCommand(fq, lto:LogbookEntry.LoadTelemetryOption.LoadAll));
            using (KMLWriter kw = new KMLWriter(s))
            {
                kw.BeginKML();

                LookAtAllFlights(
                    dbh,
                    (TelemetryReference tr, string szRoute, DateTime dt, string szComment) =>
                    {
                        if (tr.HasPath)
                        {
                            using (FlightData fd = new FlightData())
                            {
                                try
                                {
                                    fd.ParseFlightData(tr.RawData);
                                    if (fd.HasLatLongInfo)
                                    {
                                        kw.AddPath(fd.GetTrajectory(), String.Format(CultureInfo.CurrentCulture, "{0} - {1}", dt.ToString(), szComment), fd.SpeedFactor);
                                        return;
                                    }
                                }
                                catch { }   // eat any error and fall through below
                            }
                        }
                        // No path was found above.
                        AirportList al = alMaster.CloneSubset(szRoute);
                        kw.AddRoute(al.GetNormalizedAirports(), String.Format(CultureInfo.CurrentCulture, "{0} - {1}", dt.ToShortDateString(), szRoute));
                    });
                kw.EndKML();
            }

            error = dbh.LastError;
        }
        #endregion
    }

    /// <summary>
    /// Represents a named point in space.
    /// </summary>
    public interface IFix
    {
        string Code { get; }
        LatLong LatLong { get; }

        double DistanceFromFix(IFix f);
    }

    /// <summary>
    /// Represents an airport as a latitude/longitude, airport code, and name
    /// </summary>
    [Serializable]
    public class airport : IComparable, IFix
    {
        #region properties
        /// <summary>
        /// Distance from a specified position, 0.0 if unknown (and if lat/long are different from current position).
        /// </summary>
        public double DistanceFromPosition { get; set; }

        /// <summary>
        /// User that created the airport; empty for built-in airports
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// FAA Data code representing the type of the facility (1-2 chars)
        /// </summary>
        public string FacilityTypeCode { get; set; }

        /// <summary>
        /// Friendly name for the type of facility (e.g., "VOR")
        /// </summary>
        public string FacilityType { get; set; }

        /// <summary>
        /// The IATA or ICAO code for the airport
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The friendly name for the airport
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Latitude/longitude of the airport
        /// </summary>
        public LatLong LatLong { get; set; }

        /// <summary>
        /// DEPRECATED The airport's latitude (string representation of a decimal number)
        /// </summary>
        public string Latitude
        {
            get { return this.LatLong.LatitudeString; }
            set { this.LatLong.Latitude = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture); }
        }

        /// <summary>
        /// DEPRECATED The airport's longitude (string representation of a decimal number)
        /// </summary>
        public string Longitude
        {
            get { return this.LatLong.LongitudeString; }
            set { this.LatLong.Longitude = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture); }
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
            get { return (UserName.Length > 0); }
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
        [Newtonsoft.Json.JsonIgnore]
        public string GeoHashKey
        {
            get { return String.Format(CultureInfo.InvariantCulture, "{0}La{1:F2}Lo{2:F2}", FacilityTypeCode, LatLong.Latitude, LatLong.Longitude); }
        }
        #endregion  // properties

        private Boolean isNew = false;

        public const int minNavaidCodeLength = 2;
        public const int minAirportCodeLength = 3;
        public const int maxCodeLength = 6; // because of navaids, now allow up to 5 letters.
        public const string ForceNavaidPrefix = "@";
        public const string USAirportPrefix = "K";
        private const string szRegAdHocFix = ForceNavaidPrefix + "\\b\\d{1,2}(?:[\\.,]\\d*)?[NS]\\d{1,3}(?:[\\.,]\\d*)?[EW]\\b";  // Must have a digit on the left side of the decimal
        private readonly static string szRegexAirports = String.Format(CultureInfo.InvariantCulture, "((?:{0})|(?:@?\\b[A-Z0-9]{{{1},{2}}}\\b))", szRegAdHocFix, Math.Min(airport.minNavaidCodeLength, airport.minAirportCodeLength), airport.maxCodeLength);
        private readonly static Regex regAdHocFix = new Regex(szRegAdHocFix, RegexOptions.Compiled);
        private readonly static Regex regAirport = new Regex(szRegexAirports, RegexOptions.Compiled);

        override public string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}({1}) - {2}", Code, FacilityTypeCode, Name);
        }

        #region IComparable
        public int CompareTo(object obj)
        {
            return String.Compare(Code, ((airport)obj).Code, StringComparison.CurrentCulture);
        }
        #endregion

        #region Object Creation
        public airport()
        {
            Code = Name = FacilityType = FacilityTypeCode = UserName = string.Empty;
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
                throw new ArgumentNullException("dr");

            Code = dr["airportID"].ToString();
            Name = dr["FacilityName"].ToString();
            this.LatLong = new LatLong(Convert.ToDouble(dr["Latitude"], CultureInfo.InvariantCulture), Convert.ToDouble(dr["Longitude"], CultureInfo.InvariantCulture));
            FacilityTypeCode = dr["Type"].ToString();
            FacilityType = dr["FriendlyName"].ToString();
            UserName = dr["SourceUserName"].ToString();
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
                throw new ArgumentNullException("f");
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
                throw new ArgumentNullException("szcode");
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
                throw new ArgumentNullException("szAirports");
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
        public static IEnumerable<airport> AirportsWithinBounds(double latSouth, double lonWest, double latNorth, double lonEast)
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

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "{0} WHERE Type IN ('A', 'S') AND Latitude BETWEEN ?lat1 AND ?lat2 AND Longitude BETWEEN ?lon1 AND ?lon2 LIMIT 200", airport.DefaultSelectStatement("0.0")));
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

            if (String.IsNullOrEmpty(szSearchText.Trim()))
                return lstAp;

            string[] rgSearchTerms = Regex.Split(szSearchText, "\\s");

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
                            string szParam = "AirportName" + comm.Parameters.Count.ToString();
                            sb.AppendFormat("(CONCAT(FacilityName, ' ', AirportID) LIKE ?{0})", szParam);
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
                    if (szairport.StartsWith(ForceNavaidPrefix))
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
            {
                double temp = minLong;
                minLong = maxLong;
                maxLong = temp;
            }

            string szQ = String.Format(CultureInfo.InvariantCulture, szTemplate, airport.DefaultSelectStatement(szDistanceComp), minLat, maxLat, minLong, maxLong, (fIncludeHeliports ? " OR airports.type='H'" : ""), limit);

            ArrayList rgAirports = new ArrayList();
            DBHelper dbh = new DBHelper(szQ);
            dbh.ReadRows(
                (comm) => { },
                (dr) => { rgAirports.Add(new airport(dr)); });

            return (airport[])rgAirports.ToArray(typeof(airport));
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
                    ErrorText = String.Format(CultureInfo.CurrentCulture, Resources.Airports.errConflict, Code);
                    return false;
                }

                if (!IsPort && fMatchedNavaids)
                {
                    ErrorText = String.Format(CultureInfo.CurrentCulture, Resources.Airports.errConflictNavaid, Code);
                    return false;
                }

                isNew = true;
                return true;
            }

            // We have an exact match.
            // Can't edit if username doesn't match the user
            if (apMatch.UserName.Length == 0)
            {
                ErrorText = String.Format(CultureInfo.CurrentCulture, Resources.Airports.errBuiltInAirport, Code);
                return false;
            }

            // if current user is admin and this is user-created, we can do anything.  Preserve the username, though.
            if (Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
            {
                if (!isNew && apMatch != null)
                    this.UserName = apMatch.UserName;
                return true;
            }
            
            // see if the returned object is owned by this user.
            // We've already checked above that this.username is not empty string, so
            // this can never return true for a built-in airport.
            if (String.Compare(apMatch.UserName, this.UserName, StringComparison.CurrentCultureIgnoreCase) != 0)
            {
                ErrorText = String.Format(CultureInfo.CurrentCulture, Resources.Airports.errNotYourAirport, Code);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Is this a valid airport or navaid?
        /// </summary>
        /// <returns>True if this is in a state to be saved</returns>
        private Boolean FValidate()
        {
            ErrorText = string.Empty;

            try
            {
                if (Code.Length < (IsPort ? minAirportCodeLength : minNavaidCodeLength))
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Airports.errCodeTooShort, Code));

                if (Code.Length > maxCodeLength)
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Airports.errCodeTooLong, Code));

                string[] airports = AirportList.NormalizeAirportList(Code);

                if (airports.Length != 1)
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Airports.errIllegalCharacters, Code));

                if (Name.Length == 0)
                    throw new MyFlightbookException(Resources.Airports.errEmptyName);

                if (!LatLong.IsValid)
                    throw new MyFlightbookException(LatLong.ValidationError);

                NavAidTypes[] rgNavAidTypes = NavAidTypes.GetKnownTypes();
                Boolean fIsKnownType = false;
                foreach (NavAidTypes navaidtype in rgNavAidTypes)
                    if (String.Compare(navaidtype.Code, this.FacilityTypeCode, StringComparison.CurrentCultureIgnoreCase) == 0)
                        fIsKnownType = true;
                if (!fIsKnownType)
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Airports.errNotKnownType, this.Code));

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
                        comm.Parameters.AddWithValue("Code", this.Code.ToUpper());
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

        /// <summary>
        /// Deletes the airport.  Must be admin or owner.
        /// </summary>
        /// <returns></returns>
        public Boolean FDelete(bool fAdminForce = false)
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
    }

    /// <summary>
    /// Admin Functionality for airports
    /// </summary>
    [Serializable]
    public class AdminAirport : airport
    {
        #region Properties
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
        public void MergeFrom(AdminAirport apSource)
        {
            if (apSource == null)
                throw new ArgumentNullException("apSource");

            LatLong = apSource.LatLong;
            if (!FCommit(fAdmin:true))
                throw new MyFlightbookException("Error merging airport: " + ErrorText);
        }
    }

    /// <summary>
    /// Represents a row of data with a code, name, and lat/lon which can be imported into the airports data table.
    /// </summary>
    [Serializable]
    public class airportImportCandidate : airport
    {
        public enum MatchStatus { Unknown, InDB, InDBWrongLocation, NotInDB, WrongType, NotApplicable };

        public const double LocationTolerance = 0.01;

        #region properties
        private string m_MatchedFAAName, m_MatchedIATAName, m_MatchedICAOName;
        private airport m_FAAMatch, m_IATAMatch, m_ICAOMatch;

        public string IATA { get; set; }
        public string ICAO { get; set; }
        public string FAA { get; set; }
        public MatchStatus MatchStatusFAA { get; set; }
        public MatchStatus MatchStatusIATA { get; set; }
        public MatchStatus MatchStatusICAO { get; set; }

        public string MatchNotes { get; set; }

        public airport IATAMatch
        {
            get { return m_IATAMatch; }
            set { m_IATAMatch = value; }
        }

        public airport FAAMatch
        {
            get { return m_FAAMatch; }
            set { m_FAAMatch = value; }
        }

        public airport ICAOMatch
        {
            get { return m_ICAOMatch; }
            set { m_ICAOMatch = value; }
        }

        public string MatchedICAOName
        {
            get { return m_MatchedICAOName; }
            set { m_MatchedICAOName = value; }
        }

        public string MatchedIATAName
        {
            get { return m_MatchedIATAName; }
            set { m_MatchedIATAName = value; }
        }

        public string MatchedFAAName
        {
            get { return m_MatchedFAAName; }
            set { m_MatchedFAAName = value; }
        }

        public string MatchStatusDescription
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (!String.IsNullOrEmpty(FAA))
                    sb.AppendFormat(CultureInfo.CurrentCulture, "FAA: {0}\r\n", MatchStatusFAA.ToString());
                if (!String.IsNullOrEmpty(ICAO))
                    sb.AppendFormat(CultureInfo.CurrentCulture, "ICAO: {0}\r\n", MatchStatusICAO.ToString());
                if (!String.IsNullOrEmpty(IATA))
                    sb.AppendFormat(CultureInfo.CurrentCulture, "IATA: {0}\r\n", MatchStatusIATA.ToString());
                return sb.ToString();
            }
        }

        public bool IsOK
        {
            get { return StatusIsOK(MatchStatusFAA) && StatusIsOK(MatchStatusIATA) && StatusIsOK(MatchStatusICAO); }
        }

        public bool IsKHack
        {
            get
            {
                return MatchStatusFAA == MatchStatus.InDB && MatchStatusIATA == MatchStatus.NotApplicable && MatchStatusICAO == MatchStatus.NotInDB &&
                String.Compare(ICAO, String.Format(CultureInfo.InvariantCulture, "K{0}", FAA), StringComparison.CurrentCultureIgnoreCase) == 0 && m_FAAMatch.LatLong.IsSameLocation(this.LatLong, LocationTolerance);
            }
        }

        #endregion

        public airportImportCandidate() : base()
        {
            IATA = ICAO = FAA = string.Empty;
            FacilityTypeCode = "A";
            MatchStatusFAA = MatchStatusIATA = MatchStatusICAO = MatchStatus.Unknown;
        }

        public void CheckStatus(AirportList al = null)
        {
            if (al == null)
                al = new AirportList(String.Format(CultureInfo.CurrentCulture, "{0} {1} {2}", IATA, ICAO, FAA));

            airport[] rgap = al.GetAirportList();

            MatchStatusFAA = GetStatus(FAA, rgap, out m_MatchedFAAName, out m_FAAMatch);
            MatchStatusIATA = GetStatus(IATA, rgap, out m_MatchedIATAName, out m_IATAMatch);
            MatchStatusICAO = GetStatus(ICAO, rgap, out m_MatchedICAOName, out m_ICAOMatch);
        }

        public static bool StatusIsOK(MatchStatus ms)
        {
            return ms == MatchStatus.InDB || ms == MatchStatus.NotApplicable;
        }

        private MatchStatus GetStatus(string szcode, airport[] rgap, out string matchedName, out airport matchedAirport)
        {
            matchedName = string.Empty;
            matchedAirport = null;
            if (String.IsNullOrEmpty(szcode))
                return MatchStatus.NotApplicable;
            matchedAirport = Array.Find<airport>(rgap, ap2 => String.Compare(szcode, ap2.Code, true) == 0);
            if (matchedAirport == null)
                return MatchStatus.NotInDB;

            matchedName = matchedAirport.Name;
            if (String.Compare(matchedAirport.FacilityTypeCode, FacilityTypeCode, true) != 0)
                return MatchStatus.WrongType;
            if (this.LatLong == null || !matchedAirport.LatLong.IsSameLocation(this.LatLong, LocationTolerance))
            {
                MatchNotes = String.Format(CultureInfo.CurrentCulture, "{0}\r\nLoc In DB: {1}\r\nLoc in import: {2}\r\n", MatchNotes, matchedAirport.LatLong.ToString(), this.LatLong.ToString());
                return MatchStatus.InDBWrongLocation;
            }
            return MatchStatus.InDB;
        }
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

    /// <summary>
    /// Result from parsing a list of routes
    /// </summary>
    [Serializable]
    public class ListsFromRoutesResults
    {
        /// <summary>
        /// The resulting list of AirportLists
        /// </summary>
        public List<AirportList> Result { get; set; }

        /// <summary>
        /// The AirportList containing the set of all airports found.
        /// </summary>
        public AirportList MasterList { get; set; }

        public ListsFromRoutesResults(IEnumerable<AirportList> result, AirportList master)
        {
            Result = new List<AirportList>(result);
            MasterList = master;
        }
    }

    /// <summary>
    /// Provides utility functions for airports, including name resolution, lat/long bounding rectangles, and mapping
    /// </summary>
    [Serializable]
    public class AirportList
    {
        public const string RouteSeparator = "=>";

        private string[] m_rgszAirportsNormal;
        private Dictionary<string, airport> m_htAirportsByCode = new Dictionary<string, airport>();
        private List<airport> m_rgAirports;

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} airports: {1}", m_rgAirports.Count, String.Join(", ", m_rgszAirportsNormal));
        }

        #region properties for serialization only
        /// <summary>
        /// The normalized airports - used only for serialization.
        /// Note that arrays serialize/de-serialize just fine, but List does not.  Also dictionaries/hashtables do NOT serialize."  
        /// So we are suppressing the error message and using this as a way to ensure that serialization works
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] NormalizedAirportsForSerialization
        {
            get { return m_rgszAirportsNormal; }
            set { m_rgszAirportsNormal = value; }
        }

        /// <summary>
        /// The airport array.  Generic Lists do NOT serialize/deserialize properly, so we expose this as an array.
        /// On deserialization, we also initialize the hashtable, since hashtables/dictionaries also do NOT serialize
        /// So we are suppressing the error message and using this as a way to ensure that serialization works
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public airport[] AirportArrayForSerialization
        {
            get { return m_rgAirports.ToArray(); }
            set 
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                m_rgAirports = new List<airport>(value);
                foreach (airport ap in value)
                    AddAirportToHashtable(ap);
            }
        }
        #endregion

        #region Object creation
        public AirportList()
        {
        }

        public AirportList(IEnumerable<VisitedAirport> rgVA) : this()
        {
            if (rgVA == null)
                throw new ArgumentNullException("rgVA");
            m_rgAirports = new List<airport>();
            List<string> alAirports = new List<string>();
            foreach (VisitedAirport va in rgVA)
            {
                m_rgAirports.Add(va.Airport);
                m_htAirportsByCode[va.Code] = va.Airport;
                alAirports.Add(va.Code);
            }

            m_rgszAirportsNormal = alAirports.ToArray();
        }

        public AirportList(string szAirports) : this()
        {
            m_rgszAirportsNormal = NormalizeAirportList(szAirports);
            m_rgAirports = new List<airport>(airport.AirportsMatchingCodes(m_rgszAirportsNormal).ToArray());

            foreach (airport ap in m_rgAirports)
                AddAirportToHashtable(ap);
        }
        #endregion

        private void AddAirportToHashtable(airport ap)
        {
            // segregate airports from navaids by using the navaid prefix as the key if necessary
            // Use priority to store the highest-priority navaid, if multiple found
            if (ap.IsPort)
                m_htAirportsByCode[ap.Code] = ap;
            else
            {
                string szKey = airport.ForceNavaidPrefix + ap.Code;
                // see if a navaid is already there; store the new one only if it is higher priority (lower value) than what's already there.
                airport ap2;
                m_htAirportsByCode.TryGetValue(szKey, out ap2);
                if (ap2 == null || (ap.Priority < ap2.Priority))
                    m_htAirportsByCode[szKey] = ap;
            }
        }

        #region Cloning subsets of airports - efficiency (avoids extra data calls)
        /// <summary>
        /// Returns a new AirportList that is a subset of the current ones, using airports out of the array of codes (from NormalizeAirportList)
        /// </summary>
        /// <param name="rgApCodeNormal">The array of airport codes (call NormalizeAirportList on a route to get this)</param>
        /// <param name="fPortsOnly">Indicates whether we should ONLY include ports (vs navaids)</param>
        /// <returns>A new airportList object that is the intersection of the source object and the specified codes</returns>
        private AirportList CloneSubset(string[] rgApCodeNormal, bool fPortsOnly = false)
        {
            AirportList al = new AirportList()
            {
                m_rgAirports = new List<airport>(),
                m_rgszAirportsNormal = rgApCodeNormal
            };

            foreach (string sz in al.m_rgszAirportsNormal)
            {
                airport ap = GetAirportByCode(sz);
                if (ap != null && (ap.IsPort || !fPortsOnly))
                {
                    al.m_htAirportsByCode[sz] = ap;
                    al.m_rgAirports.Add(ap);
                }
            }
            return al;
        }

        /// <summary>
        /// Returns a new AirportList that is a subset of the current ones, using airports out of the specified route
        /// </summary>
        /// <param name="szRoute">A string with the route</param>
        /// <param name="fPortsOnly">Indicates whether we should ONLY include ports</param>
        /// <returns>A new airportList object that is the intersection of the source object and the specified codes</returns>
        public AirportList CloneSubset(string szRoute, bool fPortsOnly = false)
        {
            return CloneSubset(NormalizeAirportList(szRoute), fPortsOnly);
        }
        #endregion

        #region Routes helpers
        /// <summary>
        /// Returns a list of AirportLists given a route.  "=>" is the separator between routes.  This hits the database exactly once, for efficiency, and excludes duplicates.
        /// </summary>
        /// <param name="rgRoutes">The array of routes.</param>
        /// <param name="aplMaster">The "master" airportlist (contains all of the airports)</param>
        /// <returns>A list of airportlists; each contains at least one airport</returns>
        public static ListsFromRoutesResults ListsFromRoutes(IEnumerable<string> rgRoutes)
        {
            List<AirportList> lst = new List<AirportList>();

            if (rgRoutes == null || rgRoutes.Count() == 0)
                return new ListsFromRoutesResults(lst, new AirportList(string.Empty));

            // Create a single "worker" airportlist from which we will create the others.
            AirportList aplMaster = new AirportList(String.Join(" ", rgRoutes.ToArray()));

            Dictionary<string, AirportList> dictRoutes = new Dictionary<string, AirportList>();

            // Now create the resulting airport lists.  Exclude duplicate routes
            foreach (string  szRoute in rgRoutes)
            {
                string[] rgApCodeNormal = AirportList.NormalizeAirportList(szRoute);
                string szRouteKey = String.Join("", rgApCodeNormal);

                // skip duplicate routes
                if (dictRoutes.ContainsKey(szRouteKey))
                    continue;

                AirportList al = aplMaster.CloneSubset(rgApCodeNormal);
                if (al.m_rgAirports.Count > 0)
                {
                    lst.Add(al);
                    dictRoutes.Add(szRouteKey, al);
                }
            }
            return new ListsFromRoutesResults(lst, aplMaster);
        }

        /// <summary>
        /// Returns a list of AirportLists given a route.  "=>" is the separator between routes.
        /// </summary>
        /// <param name="szAirports">The route string.</param>
        /// <returns>A list of Airportlists, in order.  If more than one, then the 1st element contains the "full route" airport list for efficiency.</returns>
        public static ListsFromRoutesResults ListsFromRoutes(string szAirports)
        {
            if (szAirports == null)
                throw new ArgumentNullException("szAirports");
            return ListsFromRoutes(new List<string>(szAirports.Split(new string[] { RouteSeparator }, StringSplitOptions.RemoveEmptyEntries)));
        }
        #endregion

        #region Extracting airports
        /// <summary>
        /// Find an airport in the hashtable by the code.  Implements the navaid forcing and the K-hack for US airports
        /// </summary>
        /// <param name="sz">The code of the airport</param>
        /// <returns>The matching airport, null if not found.</returns>
        private airport GetAirportByCode(string sz)
        {
            airport ap;
            m_htAirportsByCode.TryGetValue(sz, out ap);

            // if this wasn't a forced navaid and the airport wasn't found, look to see if the navaid matched.
            if (ap == null && !sz.StartsWith(airport.ForceNavaidPrefix, StringComparison.CurrentCultureIgnoreCase))
                m_htAirportsByCode.TryGetValue(airport.ForceNavaidPrefix + sz, out ap);

            // else try the USPrefix hack
            if (ap == null && airport.IsUSAirport(sz))
                m_htAirportsByCode.TryGetValue(airport.USPrefixConvenienceAlias(sz), out ap);

            return ap;
        }

        /// <summary>
        /// Returns an array of airports - in order and including duplicates - from the normalized list of airports.
        /// </summary>
        /// <returns></returns>
        public airport[] GetNormalizedAirports()
        {
            ArrayList al = new ArrayList();
            foreach (string sz in m_rgszAirportsNormal)
            {
                airport ap = GetAirportByCode(sz);
                if (ap != null)
                    al.Add(ap);
            }
            return (airport[])al.ToArray(typeof(airport));
        }

        /// <summary>
        /// Return the list of airports associated with this airport list
        /// </summary>
        /// <returns>An array of airports</returns>
        public airport[] GetAirportList()
        {
            return m_rgAirports.ToArray();
        }

        public IEnumerable<airport> UniqueAirports
        {
            get { return (m_htAirportsByCode == null) ? new airport[0] : (IEnumerable<airport>)m_htAirportsByCode.Values; }
        }
        #endregion

        /// <summary>
        /// Breaks up a string of delimited airport names into an array of constituant 3- and 4- letter airport codes
        /// TODO: This has moved to airports; can remove these references.
        /// </summary>
        /// <param name="szAirports">String containing a list of airports.  Any non-alpha character (other than ForceNavaidPrefix) serves as a delimiter</param>
        /// <returns>An array of strings corresponding to the normalized airport codes</returns>
        public static string[] NormalizeAirportList(string szAirports)
        {
            if (szAirports == null)
                throw new ArgumentNullException("szAirports");
            return airport.SplitCodes(szAirports).ToArray();
        }

        /// <summary>
        /// Strips any navaids from a list of airports
        /// </summary>
        /// <param name="rgap">The array of airports</param>
        /// <returns>A new array with the navaids removed</returns>
        public static airport[] RemoveNavaids(IEnumerable<airport> rgap)
        {
            if (rgap == null)
                throw new ArgumentNullException("rgap");
            List<airport> al = new List<airport>();
            foreach (airport ap in rgap)
                if (ap.IsPort)
                    al.Add(ap);
            return al.ToArray();
        }

        /// <summary>
        /// Pass in an array of airports and get back the latitude/longitude bounding box of all of the airports
        /// </summary>
        /// <returns>A LatLongBox.</returns>
        public LatLongBox LatLongBox(bool fAirportsOnly = false)
        {
            airport[] rgap = fAirportsOnly ? AirportList.RemoveNavaids(m_rgAirports) : m_rgAirports.ToArray();
            if (rgap.Length == 0)
                return new LatLongBox(new LatLong(0.0, 0.0));

            LatLongBox llb = new LatLongBox(((airport)rgap[0]).LatLong);

            foreach (airport ap in rgap)
                llb.ExpandToInclude(ap.LatLong);

            return llb;
        }

        public void InitFromSearch(string szQuery)
        {
            m_rgAirports = new List<airport>();
            m_rgszAirportsNormal = new string[0];
            m_htAirportsByCode = new Dictionary<string, airport>();
            List<string> lstCodes = new List<string>();

            IEnumerable<airport> rgap = airport.AirportsMatchingText(szQuery);
            m_rgAirports = new List<airport>(rgap);
            foreach (airport ap in rgap)
            {
                lstCodes.Add(ap.Code);
                m_htAirportsByCode[ap.Code] = ap;
            }

            m_rgszAirportsNormal = lstCodes.ToArray();
        }

        #region Distance computations
        /// <summary>
        /// Examines a route and determines the maximum distance flown between ANY two airports (even if that pair doesn't represent a flown segment)
        /// </summary>
        /// <returns>0 or the maximum distance</returns>
        public double MaxDistanceForRoute()
        {
            double d = 0.0;
            airport[] rgAp = GetNormalizedAirports();
            int cAp = rgAp.Length;
            if (cAp < 2)
                return d;

            for (int i = 0; i < cAp; i++)
            {
                for (int j = i + 1; j < cAp; j++)
                    if (rgAp[i].IsPort && rgAp[j].IsPort)
                        d = Math.Max(d, rgAp[i].DistanceFromAirport(rgAp[j]));
            }

            return d;
        }

        /// <summary>
        /// Examines a route and returns the total distance flown.
        /// </summary>
        /// <returns></returns>
        public double DistanceForRoute()
        {
            double d = 0.0;
            airport[] rgAp = GetNormalizedAirports();
            for (int i = 1; i < rgAp.Length; i++)
                d += rgAp[i].DistanceFromAirport(rgAp[i - 1]);
            return d;
        }

        /// <summary>
        /// Finds the longest segment for the route
        /// </summary>
        /// <returns>The longest segment</returns>
        public double MaxSegmentForRoute()
        {
            double d = 0.0;
            airport[] rgAp = GetNormalizedAirports();
            for (int i = 1; i < rgAp.Length; i++)
                d = Math.Max(rgAp[i].DistanceFromAirport(rgAp[i - 1]), d);
            return d;
        }

        /// <summary>
        /// Find the furthest distance away from the STARTING airport that was flown.
        /// </summary>
        /// <returns>Distance in nm.</returns>
        public double MaxDistanceFromStartingAirport()
        {
            double d = 0.0;
            airport[] rgAp = GetNormalizedAirports();
            for (int i = 1; i < rgAp.Length; i++)
                d = Math.Max(d, rgAp[i].DistanceFromAirport(rgAp[0]));
            return d;
        }
        #endregion
    }

    /// <summary>
    /// A segment (one airport to another) that has been visited by a user
    /// </summary>
    [Serializable]
    public class FlownSegment
    {
        #region Properties
        /// <summary>
        /// The airport pair (e.g., two airport codes)
        /// </summary>
        public string Segment { get; set; }

        /// <summary>
        /// Does this have a match?  Helps distinguish NULL logbook entry from Not Found
        /// </summary>
        public bool HasMatch { get; set; }

        /// <summary>
        /// If a match is found, which flight is the match?
        /// </summary>
        public LogbookEntry MatchingFlight { get; set; }
        #endregion

        public FlownSegment()
        {
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} - {1}", Segment, HasMatch ? (MatchingFlight == null ? "Not yet flown" : MatchingFlight.ToString()) : "Not yet matched");
        }
    }

    [Serializable]
    public class VisitedRoute
    {
        #region Properties
        /// <summary>
        /// The routes
        /// </summary>
        private List<AirportList> Routes { get; set; }

        #region public properties for serialization
        /// <summary>
        /// A master list containing all of the airports (for efficiency), no dupes.
        /// </summary>
        public AirportList MasterAirportList { get; set; }

        /// <summary>
        /// Virtual property, for serialization only.
        /// NOTE: The segments are stored in a dictionary, but dictionaries (and hashtables) do NOT serialize
        /// So, when we get this we enumerate all of the values inthe dictionary and put them into a list, which we then convert to an array and return.
        /// When setting, we store the values into the dictionary, since it is keyed off of the Segment property anyhow.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public FlownSegment[] SerializedSegments
        {
            get
            {
                List<FlownSegment> lst = new List<FlownSegment>();
                foreach (string key in FlownSegments.Keys)
                    lst.Add(FlownSegments[key]);
                return lst.ToArray();
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                foreach (FlownSegment fs in value)
                    FlownSegments[fs.Segment] = fs;
            }
        }

        /// <summary>
        /// This exposes the Routes list, but since Lists do not serialize properly, we convert to/from an array.  We suppress the warning for this reason.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public AirportList[] SerializedRoutes
        {
            get { return Routes.ToArray(); }
            set { Routes = new List<AirportList>(value); }
        }
        #endregion
        /// <summary>
        /// A dictionary of the flown segments, keyed by city pair.
        /// </summary>
        private Dictionary<string, FlownSegment> FlownSegments { get; set; }
        #endregion

        #region Object Instantiation
        public VisitedRoute()
        {
            Routes = new List<AirportList>();
            MasterAirportList = new AirportList();
            FlownSegments = new Dictionary<string, FlownSegment>();
        }

        public VisitedRoute(string szRoute) : this()
        {
            ListsFromRoutesResults result = AirportList.ListsFromRoutes(szRoute);
            Routes = result.Result;
            MasterAirportList = result.MasterList;
        }
        #endregion

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string key in FlownSegments.Keys)
                sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n", FlownSegments[key].ToString());
            return sb.ToString();
        }

        public IEnumerable<AirportList> ComputeFlownSegments()
        {
            List<AirportList> lst = new List<AirportList>();
            foreach (FlownSegment fs in FlownSegments.Values)
                if (fs.HasMatch)
                    lst.Add(MasterAirportList.CloneSubset(fs.Segment));
            return lst;
        }

        public MFBImageInfo[] GetImagesOnFlownSegments()
        {
            List<MFBImageInfo> lst = new List<MFBImageInfo>();
            ImageList il = new ImageList() { Class = MFBImageInfo.ImageClass.Flight };
            foreach (FlownSegment fs in FlownSegments.Values)
            {
                if (fs.HasMatch && fs.MatchingFlight.fIsPublic)
                {
                    il.Key = fs.MatchingFlight.FlightID.ToString(CultureInfo.InvariantCulture);
                    il.Refresh();
                    lst.AddRange(il.ImageArray);
                }
            }

            return lst.ToArray();
        }

        private string RegexpForCode(string szCode)
        {
            string szNormal = szCode.ToUpper(CultureInfo.InvariantCulture);
            if (szNormal.Length == 4 && szNormal.StartsWith("K", StringComparison.CurrentCultureIgnoreCase))
                return String.Format(CultureInfo.InvariantCulture, "K?{0}", szNormal.Substring(1, 3));
            return (szNormal.Length == 3) ? String.Format(CultureInfo.InvariantCulture, "K?{0}", szNormal) : szNormal;
        }

        private string KeyForCityPair(string szCode1, string szCode2)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}-{1}", szCode1, szCode2);
        }

        public int SearchedSegmentsCount
        {
            get { return FlownSegments.Keys.Count; }
        }

        public int TotalSegmentCount
        {
            get
            {
                int i = 0;
                foreach (AirportList apl in Routes)
                {
                    airport[] rgap = apl.GetAirportList();
                    if (rgap.Length > 1)
                        i += rgap.Length - 1;
                }
                return i;
            }
        }

        public bool IsComplete
        {
            get { return SearchedSegmentsCount == TotalSegmentCount;}
        }

        /// <summary>
        /// Fills in up to 
        /// </summary>
        /// <param name="maxSegments"></param>
        /// <returns></returns>
        public int Refresh(int maxSegments)
        {
            int segmentsSearched = 0;
            DBHelper dbh = new DBHelper();
            const string szSearchTemplate = "SELECT route, idflight FROM flights WHERE route RLIKE '{0}[^a-z0-9]+{1}' OR route RLIKE '{1}[^a-z0-9]{0}' ORDER BY flights.Date ASC LIMIT 1";
            foreach (AirportList apl in Routes)
            {
                airport[] rgap = apl.GetAirportList();
                for (int i = 0; i < rgap.Length - 1; i++)
                {
                    string szKey = KeyForCityPair(rgap[i].Code, rgap[i+1].Code);
                    if (FlownSegments.ContainsKey(szKey))
                        continue;

                    dbh.CommandText = String.Format(CultureInfo.InvariantCulture, szSearchTemplate, RegexpForCode(rgap[i].Code), RegexpForCode(rgap[i+1].Code));

                    int idFlight = LogbookEntry.idFlightNone;

                    dbh.ReadRow((comm) => { }, (dr) => { idFlight = Convert.ToInt32(dr["idflight"], CultureInfo.InvariantCulture); });

                    FlownSegment fs = new FlownSegment() { Segment = szKey, HasMatch = (idFlight != LogbookEntry.idFlightNone) };
                    if (fs.HasMatch)
                        fs.MatchingFlight = new LogbookEntry(idFlight, lto: LogbookEntry.LoadTelemetryOption.None);
                    else
                        fs.MatchingFlight = null;

                    FlownSegments.Add(szKey, fs);

                    if (++segmentsSearched == maxSegments)
                        break;
                }

                if (segmentsSearched == maxSegments)
                    break;
            }

            return segmentsSearched;
        }
    }

    /// <summary>
    /// A single question for an airport identification quiz.  Includes a set of answers and the index of the correct one
    /// </summary>
    [Serializable]
    public class AirportQuizQuestion
    {
        private int m_CorrectAnswer = -1;
        private readonly airport[] m_Answers = null;

        /// <summary>
        /// The index of the answer to the most recently asked question
        /// </summary>
        public int CorrectAnswerIndex
        {
            get { return m_CorrectAnswer; }
            set { m_CorrectAnswer = value; }
        }

        /// <summary>
        /// The airports that comprise the choices for the user; the correct one is in index CorrectAnswerIndex
        /// </summary>
        public ReadOnlyCollection<airport> Answers
        {
            get { return new ReadOnlyCollection<airport>(m_Answers); }
        }

        public AirportQuizQuestion(airport[] answers, int correctAnswerIndex)
        {
            m_Answers = answers;
            CorrectAnswerIndex = correctAnswerIndex;
        }
    }

    [Serializable]
    public class AirportQuiz
    {
        public const string szBusyUSAirports = "KABQ;KALB;KATL;KAUS;KBDL;KBHM;KBNA;KBOI;KBOS;KBTV;KBUF;KBUR;KBWI;KCAK;KCHS;KCLE;KCLT;KCMH;KCOS;KCVG;KDAL;KDAY;KDCA;KDEN;KDFW;KDSM;KDTW;KELP;KEWR;KFLL;KGEG;KGRR;KGSO;PHNL;KHOU;KHPN;KIAD;KIAH;KICT;KIND;KISP;KJAN;KJAX;KJFK;PHKO;KLAS;KLAX;KLGA;KLGB;PHLI;KLIT;KMCI;KMCO;KMDW;KMEM;KMHT;KMIA;KMKE;KMSN;KMSP;KMSY;KMYR;KOAK;PHOG;KOKC;KOMA;KONT;KORD;KORF;KPBI;KPDX;KPHL;KPHX;KPIT;KPNS;KPVD;KPWM;KRDU;KRIC;KRNO;KROC;KRSW;KSAN;KSAT;KSAV;KSDF;KSEA;KSFO;KSJC;TJSJ;KSLC;KSMF;KSNA;KSTL;KSYR;KTPA;KTUL;KTUS;KTYS";

        private int m_CurrentIndex = -1;
        private int m_CorrectAnswerCount = -1;
        private int[] m_rgShuffle;
        private airport[] m_rgAirport;
        private int m_cBluffs = 3;

        #region Properties
        /// <summary>
        /// The index of the current question
        /// </summary>
        public int CurrentQuestionIndex
        {
            get { return m_CurrentIndex; }
            set { m_CurrentIndex = value; }
        }

        /// <summary>
        /// The # of correct answers given so far
        /// </summary>
        public int CorrectAnswerCount
        {
            get { return m_CorrectAnswerCount; }
            set { m_CorrectAnswerCount = value; }
        }

        /// <summary>
        /// Returns the random object in use, so that it doesn't have to be recreated.
        /// </summary>
        public Random Random {get; set;}

        /// <summary>
        /// Gets/sets the # of bluff answers for each question
        /// </summary>
        public int BluffCount
        {
            get { return m_cBluffs; }
            set
            {
                if (value > 0)
                    m_cBluffs = value;
                else
                    throw new MyFlightbookException("Bluffs must be > 0");
            }
        }

        /// <summary>
        /// Returns the current question
        /// </summary>
        public AirportQuizQuestion CurrentQuestion {get; set;}
        #endregion

        /// <summary>
        /// Creates a new AirportQuiz object, which holds the state for an airport quiz
        /// </summary>
        /// <param name="szDefaultAirportList">The delimited list of airport codes to use for the quiz.  Defaults to szBusyUSAirports</param>
        public AirportQuiz()
        {
            this.Random = new Random((int) (DateTime.Now.Ticks % Int32.MaxValue));
            CorrectAnswerCount = 0;
        }

        /// <summary>
        /// Returns the list of airports used to initialize this quiz
        /// </summary>
        public airport[] DefaultAirportList { get; set;}

        /// <summary>
        /// Initializes a quiz, shuffling the airports and resetting the current quiz index and correct answer count.  You must then do GenerateQuestion to actually create a question.
        /// </summary>
        /// <param name="szDefaultAirportList"></param>
        public void Init(airport[] rgAirports)
        {
            if (rgAirports == null)
                throw new ArgumentNullException("rgAirports");

            DefaultAirportList = rgAirports;

            m_rgAirport = rgAirports;
            m_rgShuffle = ShuffleIndex(m_rgAirport.Length);
            CurrentQuestionIndex = 0;
            CurrentQuestion = null;
            m_CorrectAnswerCount = 0;
        }

        /// <summary>
        /// Generate a new question, increment the current question index
        /// </summary>
        /// <returns></returns>
        public void GenerateQuestion()
        {
            airport[] rgAirportAnswers = new airport[BluffCount + 1];
            CurrentQuestion = new AirportQuizQuestion(rgAirportAnswers, this.Random.Next(BluffCount + 1));

            // set the index of the correct answer
            rgAirportAnswers[CurrentQuestion.CorrectAnswerIndex] = m_rgAirport[m_rgShuffle[this.CurrentQuestionIndex]];

            // fill in the ringers...
            int[] rgBluffs = ShuffleIndex(m_rgAirport.Length);
            for (int i = 0, iBluff = 0; i < BluffCount; i++)
            {
                // see if the airport index of the bluff is the same as the index of the correct answer
                if (rgBluffs[iBluff] == m_rgShuffle[CurrentQuestionIndex])
                    iBluff = (iBluff + 1) % rgBluffs.Length;

                int iResponse = (CurrentQuestion.CorrectAnswerIndex + i + 1) % (BluffCount + 1);

                rgAirportAnswers[iResponse] = m_rgAirport[rgBluffs[iBluff]];

                iBluff = (iBluff + 1) % rgBluffs.Length;
            }

            m_CurrentIndex++;
        }

        /// <summary>
        /// Returns a shuffled array of indices from 0 to count-1
        /// </summary>
        /// <param name="count">Highest allowed value in the array</param>
        /// <param name="r">(optional) the random number generator to use</param>
        /// <returns>A shuffled array of integers</returns>
        private int[] ShuffleIndex(int count)
        {
            int[] rgShuffle = new int[count];

            for (int i = 0; i < count; i++)
            {
                rgShuffle[i] = i;
            }

            for (int i = 0; i < count; i++)
            {
                int k = this.Random.Next(i, count);
                int j = rgShuffle[i];
                rgShuffle[i] = rgShuffle[k];
                rgShuffle[k] = j;
            }

            return rgShuffle;
        }
    }

    /// <summary>
    /// Solution to TSP for a collection of airports, code adapted from https://stackoverflow.com/questions/2927469/traveling-salesman-problem-2-opt-algorithm-c-sharp-implementation
    /// </summary>
    public static class TravelingSalesman
    {
        private class Stop
        {
            #region Constructors
            public Stop(IFix f)
            {
                Fix = f;
            }
            #endregion

            #region Properties
            public Stop Next { get; set; }

            public IFix Fix { get; set; }
            #endregion

            public Stop Clone()
            {
                return new Stop(Fix);
            }
            
            public static double Distance(Stop first, Stop other)
            {
                return first.Fix.DistanceFromFix(other.Fix);
            }
            
            //list of nodes, including this one, that we can get to
            public IEnumerable<Stop> CanGetTo()
            {
                var current = this;
                while (true)
                {
                    yield return current;
                    current = current.Next;
                    if (current == this) break;
                }
            }
            
            public override bool Equals(object obj)
            {
                return Fix.Code.CompareCurrentCultureIgnoreCase(((Stop)obj).Fix.Code) == 0;
            }
            
            public override int GetHashCode()
            {
                return Fix.GetHashCode();
            }
            
            public override string ToString()
            {
                return Fix.ToString();
            }
        }
        
        private class Tour
        {
            public Tour(IEnumerable<Stop> stops)
            {
                Anchor = stops.First();
            }

            //the set of tours we can make with 2-opt out of this one
            public IEnumerable<Tour> GenerateMutations()
            {
                for (Stop stop = Anchor; stop.Next != Anchor; stop = stop.Next)
                {
                    //skip the next one, since you can't swap with that
                    Stop current = stop.Next.Next;
                    while (current != Anchor)
                    {
                        yield return CloneWithSwap(stop.Fix, current.Fix);
                        current = current.Next;
                    }
                }
            }
            
            public Stop Anchor { get; set; }
            
            public Tour CloneWithSwap(IFix firstFix, IFix secondFix)
            {
                Stop firstFrom = null, secondFrom = null;
                var stops = UnconnectedClones();
                stops.Connect(true);

                foreach (Stop stop in stops)
                {
                    if (stop.Fix == firstFix) firstFrom = stop;

                    if (stop.Fix == secondFix) secondFrom = stop;
                }

                //the swap part
                var firstTo = firstFrom.Next;
                var secondTo = secondFrom.Next;

                //reverse all of the links between the swaps
                firstTo.CanGetTo()
                       .TakeWhile(stop => stop != secondTo)
                       .Reverse()
                       .Connect(false);

                firstTo.Next = secondTo;
                firstFrom.Next = secondFrom;

                var tour = new Tour(stops);
                return tour;
            }


            public IList<Stop> UnconnectedClones()
            {
                return Cycle().Select(stop => stop.Clone()).ToList();
            }


            public double Cost()
            {
                return Cycle().Aggregate(
                    0.0,
                    (sum, stop) =>
                    sum + Stop.Distance(stop, stop.Next));
            }


            private IEnumerable<Stop> Cycle()
            {
                return Anchor.CanGetTo();
            }


            public override string ToString()
            {
                string path = String.Join(
                    "->",
                    Cycle().Select(stop => stop.ToString()).ToArray());
                return String.Format(CultureInfo.CurrentCulture, "Cost: {0}, Path:{1}", Cost(), path);
            }

            public IEnumerable<IFix> Path()
            {
                return Cycle().Select(stop => stop.Fix);
            }
        }

        public static IEnumerable<IFix> ShortestPath(IEnumerable<IFix> rgFixes)
        {
            if (rgFixes == null)
                return new airport[0];
            if (rgFixes.Count() <= 2)
                return rgFixes;

            var lstStops = new List<Stop>();
            foreach (airport ap in rgFixes)
                lstStops.Add(new Stop(ap));

            lstStops.NearestNeighbors().Connect(true);

            Tour startingTour = new Tour(lstStops);

            //the actual algorithm
            while (true)
            {
                var newTour = startingTour.GenerateMutations()
                                          .MinBy(tour => tour.Cost());
                if (newTour.Cost() < startingTour.Cost()) startingTour = newTour;
                else break;
            }

            // Success?
            return startingTour.Path();
        }

        //take an ordered list of nodes and set their next properties
        private static void Connect(this IEnumerable<Stop> stops, bool loop)
        {
            Stop prev = null, first = null;
            foreach (var stop in stops)
            {
                if (first == null) first = stop;
                if (prev != null) prev.Next = stop;
                prev = stop;
            }

            if (loop)
            {
                prev.Next = first;
            }
        }


        //T with the smallest func(T)
        private static T MinBy<T, TComparable>(
            this IEnumerable<T> xs,
            Func<T, TComparable> func)
            where TComparable : IComparable<TComparable>
        {
            return xs.DefaultIfEmpty().Aggregate(
                (maxSoFar, elem) =>
                func(elem).CompareTo(func(maxSoFar)) > 0 ? maxSoFar : elem);
        }


        //return an ordered nearest neighbor set
        private static IEnumerable<Stop> NearestNeighbors(this IEnumerable<Stop> stops)
        {
            var stopsLeft = stops.ToList();
            for (var stop = stopsLeft.First();
                 stop != null;
                 stop = stopsLeft.MinBy(s => Stop.Distance(stop, s)))
            {
                stopsLeft.Remove(stop);
                yield return stop;
            }
        }
    }
}
