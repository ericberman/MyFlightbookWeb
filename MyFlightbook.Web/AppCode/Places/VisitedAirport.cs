using MyFlightbook.Geography;
using MyFlightbook.Image;
using MyFlightbook.Telemetry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2010-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/


namespace MyFlightbook.Airports
{
    /// <summary>
    /// A record of a visit to an airport.  Does not derive from airport, but includes an airport object which can be null under some circumstances.
    /// </summary>
    [Serializable]
    public class VisitedAirport : IComparable, IEquatable<VisitedAirport>
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

        public string Country
        {
            get { return Airport.CountryDisplay; }
        }

        public string Admin1
        {
            get { return Airport.Admin1Display; }
        }

        public string NameWithGeoRegion
        {
            get { return Airport.NameWithGeoRegion; }
        }

        /// <summary>
        /// If an airport has multiple codes (e.g., PHOG/OGG, or even KSFO/SFO), this has the alternatives by which it could be known (for searching)
        /// </summary>
        public string Aliases { get; set; }

        public void AddAlias(string szAlias)
        {
            Aliases = String.IsNullOrEmpty(Aliases) ? szAlias : String.Format(CultureInfo.CurrentCulture, "{0},{1}", Aliases, szAlias);
        }

        /// <summary>
        /// If an airport has multiple codes (e.g., PHOG/OGG, or even KSFO/SFO), this has all of the codes by which it could be known.
        /// </summary>
        public string AllCodes { get { return String.IsNullOrEmpty(Aliases) ? Code : String.Format(CultureInfo.CurrentCulture, "{0},{1}", Code, Aliases); } }

        public airport Airport { get; set; }

        public DateTime EarliestVisitDate { get; set; }

        public DateTime LatestVisitDate { get; set; }

        public int NumberOfVisits { get; set; }

        /// <summary>
        /// Internally cached ID of the flight where this airport was first encountered.
        /// </summary>
        [JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public int FlightIDOfFirstVisit { get; private set; }
        #endregion

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            return Airport.CompareTo(((VisitedAirport)obj).Airport);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as VisitedAirport);
        }

        public bool Equals(VisitedAirport other)
        {
            return other != null &&
                   Code == other.Code &&
                   FacilityName == other.FacilityName &&
                   Aliases == other.Aliases &&
                   AllCodes == other.AllCodes &&
                   EarliestVisitDate == other.EarliestVisitDate &&
                   LatestVisitDate == other.LatestVisitDate &&
                   NumberOfVisits == other.NumberOfVisits &&
                   FlightIDOfFirstVisit == other.FlightIDOfFirstVisit;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 1930749431;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Code);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FacilityName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Aliases);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AllCodes);
                hashCode = hashCode * -1521134295 + EarliestVisitDate.GetHashCode();
                hashCode = hashCode * -1521134295 + LatestVisitDate.GetHashCode();
                hashCode = hashCode * -1521134295 + NumberOfVisits.GetHashCode();
                hashCode = hashCode * -1521134295 + FlightIDOfFirstVisit.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(VisitedAirport left, VisitedAirport right)
        {
            return EqualityComparer<VisitedAirport>.Default.Equals(left, right);
        }

        public static bool operator !=(VisitedAirport left, VisitedAirport right)
        {
            return !(left == right);
        }

        public static bool operator <(VisitedAirport left, VisitedAirport right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(VisitedAirport left, VisitedAirport right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(VisitedAirport left, VisitedAirport right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(VisitedAirport left, VisitedAirport right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
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
                throw new ArgumentNullException(nameof(va));
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
        private static Dictionary<string, VisitedAirport> PopulateAirports(DBHelperCommandArgs commandArgs)
        {
            Dictionary<string, VisitedAirport> dictVA = new Dictionary<string, VisitedAirport>();

            DBHelper dbh = new DBHelper(commandArgs);

            dbh.ReadRows(
                (comm) => { },
                (dr) =>
                {
                    // ignore anything not in a real aircraft.
                    AircraftInstanceTypes instanceType = (AircraftInstanceTypes)Convert.ToInt32(dr["InstanceType"], CultureInfo.InvariantCulture);
                    if (instanceType != AircraftInstanceTypes.RealAircraft)
                        return;

                    DateTime dtFlight = Convert.ToDateTime(dr["date"], CultureInfo.InvariantCulture);
                    string szRoute = dr["route"].ToString();
                    int idFlight = Convert.ToInt32(dr["idflight"], CultureInfo.InvariantCulture);

                    decimal total = Convert.ToDecimal(util.ReadNullableField(dr, "totalFlightTime", 0.0M), CultureInfo.InvariantCulture);
                    decimal PIC = Convert.ToDecimal(util.ReadNullableField(dr, "PIC", 0.0M), CultureInfo.InvariantCulture);
                    decimal SIC = Convert.ToDecimal(util.ReadNullableField(dr, "SIC", 0.0M), CultureInfo.InvariantCulture);
                    decimal CFI = Convert.ToDecimal(util.ReadNullableField(dr, "CFI", 0.0M), CultureInfo.InvariantCulture);
                    decimal Dual = Convert.ToDecimal(util.ReadNullableField(dr, "dualReceived", 0.0M), CultureInfo.InvariantCulture);

                    // ignore any flight with no time logged.
                    if (total + PIC + SIC + CFI + Dual == 0)
                        return;

                    // we want to defer any db hit to the airport list until later, so we create an uninitialized airportlist
                    // We then visit each airport in the flight.
                    string[] rgszapFlight = AirportList.NormalizeAirportList(szRoute);

                    for (int iAp = 0; iAp < rgszapFlight.Length; iAp++)
                    {
                        string szap = rgszapFlight[iAp].ToUpperInvariant();

                        // If it's explicitly a navaid, ignore it
                        if (szap.StartsWith(airport.ForceNavaidPrefix, StringComparison.InvariantCultureIgnoreCase))
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

            return dictVA;
        }

        private static void MatchUpVisitedAirports(Dictionary<string, VisitedAirport> dictVA, Dictionary<string, airport> dictAirportResults)
        {
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
                        if (szCode.StartsWith(airport.USAirportPrefix, StringComparison.InvariantCultureIgnoreCase))
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
        }

        /// <summary>
        /// Returns a set of visited airports matching the specified flight query
        /// </summary>
        /// <param name="fq">The flight query</param>
        /// <returns>A set of visited airports</returns>
        public static IEnumerable<VisitedAirport> VisitedAirportsForQuery(FlightQuery fq)
        {
            if (fq == null)
                throw new ArgumentNullException(nameof(fq));
            if (String.IsNullOrEmpty(fq.UserName))
                throw new ArgumentNullException(nameof(fq));

            Dictionary<string, VisitedAirport> dictVA = PopulateAirports(LogbookEntry.QueryCommand(fq, 0, -1, true));

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
            MatchUpVisitedAirports(dictVA, dictAirportResults);

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

            // Convert everything to title case for legibility
            foreach (VisitedAirport va in lstResults)
                va.Airport.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(va.Airport.Name.ToLower(CultureInfo.CurrentCulture));

            lstResults.Sort();
            return lstResults.ToArray();
        }

        /// <summary>
        /// Returns a set of visited airports for the user, including all of their flights.
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <returns>A set of visited airports.</returns>
        public static IEnumerable<VisitedAirport> VisitedAirportsForUser(string szUser)
        {
            return VisitedAirportsForQuery(new FlightQuery(szUser));
        }
        #endregion

        public override string ToString()
        {
            return Airport == null ? base.ToString() : Airport.ToString();
        }

        #region Countries and Regions
        /// <summary>
        /// Returns a set of visited regions
        /// </summary>
        /// <param name="rgva">A list of airports</param>
        /// <returns>An enumerable of visited regions.</returns>
        public static IEnumerable<VisitedRegion> VisitedCountriesAndAdmins(IEnumerable<VisitedAirport> rgva)
        {
            Dictionary<string, VisitedRegion> dCountries = new Dictionary<string, VisitedRegion>();
            if (rgva == null)
                return dCountries.Values;

            foreach (VisitedAirport va in rgva)
            {
                string szCountry = va.Airport.CountryDisplay;
                string szAdmin = va.Airport.Admin1Display;
                if (String.IsNullOrEmpty(szCountry))
                    continue;

                if (!dCountries.ContainsKey(szCountry))
                    dCountries[szCountry] = new VisitedRegion(szCountry);

                string[] rgCodes = va.AllCodes.Split(new char[] { ',' });

                foreach (string szCode in rgCodes)
                    dCountries[szCountry].AddCodeForSubRegion(szCode, szAdmin);
            }

            List<VisitedRegion> lst = new List<VisitedRegion>(dCountries.Values);
            lst.Sort((vr1, vr2) => { return vr1.Name.CompareCurrentCultureIgnoreCase(vr2.Name); });

            return lst;
        }
        #endregion

        #region Total distance and flights
        /// <summary>
        /// Gets all routes ever flown, appends them together and INCLUDES any relevant navaids.
        /// </summary>
        /// <returns></returns>
        private static AirportList AllFlightsAndNavaids(FlightQuery fq)
        {
            DBHelper dbh = new DBHelper(LogbookEntry.QueryCommand(fq));
            StringBuilder sb = new StringBuilder();
            dbh.ReadRows((comm) => { }, (dr) => { sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", util.ReadNullableField(dr, "Route", string.Empty)); });
            return new AirportList(sb.ToString());
        }

        /// <summary>
        /// Examines all of the relevant flights for the specified query. 
        /// </summary>
        /// <param name="dbh">Query that returns the relevant flights</param>
        /// <param name="action">Action that takes flight data, route, date, and comments.  DO NOT dispose of the FlightData - it's owned by THIS.</param>
        /// <returns>Any error string, empty or null for no error</returns>
        private static string LookAtAllFlights(FlightQuery fq, LogbookEntryCore.LoadTelemetryOption lto, Action<LogbookEntry> action, bool fForceLoad = false)
        {
            if (fq == null)
                throw new ArgumentNullException(nameof(fq));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            DBHelper dbh = new DBHelper(LogbookEntry.QueryCommand(fq, lto: lto));
            dbh.ReadRows(
                (comm) => { },
                (dr) =>
                {
                    LogbookEntry le = new LogbookEntry(dr, fForceLoad ? (string)dr["username"] : fq.UserName, lto);
                    action(le);
                });
            return dbh.LastError;
        }

        /// <summary>
        /// Estimates the total distance flown by the user for the subset of flights described by the query
        /// </summary>
        /// <param name="fq">The flight query</param>
        /// <param name="fAutofillDistanceFlown">True to autofill the distance flown property if not found.</param>
        /// <param name="error">Any error</param>
        /// <returns>Distance flown, in nm</returns>
        public static double DistanceFlownByUser(FlightQuery fq, bool fAutofillDistanceFlown, out string error)
        {
            if (fq == null)
                throw new ArgumentNullException(nameof(fq));
            if (String.IsNullOrEmpty(fq.UserName))
                throw new MyFlightbookException("Don't estimate distance for an empty user!!");

            double distance = 0.0;

            // Get the master airport list
            AirportList alMaster = AllFlightsAndNavaids(fq);

            error = LookAtAllFlights(
                fq,
                LogbookEntryCore.LoadTelemetryOption.MetadataOrDB,
                (le) =>
                {
                    double distThisFlight = 0;

                    // If the trajectory had a distance, use it; otherwise, use airport-to-airport.
                    double dPath = le.Telemetry.Distance();
                    if (dPath > 0)
                        distThisFlight = dPath;
                    else if (!String.IsNullOrEmpty(le.Route))
                        distThisFlight = alMaster.CloneSubset(le.Route).DistanceForRoute();

                    distance += distThisFlight;

                    if (fAutofillDistanceFlown && distThisFlight > 0 && !le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropDistanceFlown))
                    {
                        le.CustomProperties.Add(CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropDistanceFlown, (decimal)distThisFlight));
                        le.FCommit();
                    }
                });

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
                throw new ArgumentNullException(nameof(fq));
            if (String.IsNullOrEmpty(fq.UserName) && (lstIDs == null || !lstIDs.Any()))
                throw new MyFlightbookException("Don't get all flights as KML for an empty user!!");

            if (lstIDs != null)
                fq.EnumeratedFlights = lstIDs;

            // Get the master airport list
            AirportList alMaster = AllFlightsAndNavaids(fq);

            using (KMLWriter kw = new KMLWriter(s))
            {
                kw.BeginKML();

                error = LookAtAllFlights(
                    fq,
                    LogbookEntryCore.LoadTelemetryOption.LoadAll,
                    (le) =>
                    {
                        if (le.Telemetry.HasPath)
                        {
                            using (FlightData fd = new FlightData())
                            {
                                try
                                {
                                    fd.ParseFlightData(le.Telemetry.RawData, le.Telemetry.MetaData);
                                    if (fd.HasLatLongInfo)
                                    {
                                        kw.AddPath(fd.GetTrajectory(), String.Format(CultureInfo.CurrentCulture, "{0:d} - {1}", le.Date, le.Comment), fd.SpeedFactor);
                                        return;
                                    }
                                }
                                catch (Exception ex) when (!(ex is OutOfMemoryException)) { }   // eat any error and fall through below
                            }
                        }
                        // No path was found above.
                        AirportList al = alMaster.CloneSubset(le.Route);
                        kw.AddRoute(al.GetNormalizedAirports(), String.Format(CultureInfo.CurrentCulture, "{0:d} - {1}", le.Date, le.Route));
                    },
                    lstIDs != null && lstIDs.Any());
                kw.EndKML();
            }
        }
        #endregion
    }

    public class VisitedRegion
    {
        private readonly Dictionary<string, VisitedRegion> mSubRegions = new Dictionary<string, VisitedRegion>();

        private readonly HashSet<string> mCodes = new HashSet<string>();

        #region properties
        /// <summary>
        /// Name for the region
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Level.  0 = Country, 1 = Admin1.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Subregions (e.g., states in a country
        /// </summary>
        public IEnumerable<VisitedRegion> SubRegions
        {
            get
            {
                List<VisitedRegion> lst = new List<VisitedRegion>(mSubRegions.Values);
                lst.Sort((vr1, vr2) => { return vr1.Name.CompareCurrentCultureIgnoreCase(vr2.Name); });
                return lst;
            }
        }

        public IEnumerable<string> Codes { get { return mCodes; } }

        public string JoinedCodes { get { return string.Join(",", mCodes); } }

        #endregion

        public VisitedRegion(string szName)
        {
            Name = szName;
        }

        public void AddCode(string szCode)
        {
            if (!String.IsNullOrWhiteSpace(szCode) && !mCodes.Contains(szCode.ToUpper(CultureInfo.CurrentCulture)))
                mCodes.Add(szCode.ToUpper(CultureInfo.CurrentCulture));
        }

        public void AddCodeForSubRegion(string szCode, string szRegion)
        {
            AddCode(szCode);
            if (!String.IsNullOrWhiteSpace(szRegion))
            {
                if (!mSubRegions.ContainsKey(szRegion))
                    mSubRegions[szRegion] = new VisitedRegion(szRegion);
                mSubRegions[szRegion].AddCode(szCode);
            }
        }
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
                    throw new ArgumentNullException(nameof(value));
                foreach (FlownSegment fs in value)
                    FlownSegments[fs.Segment] = fs;
            }
        }

        /// <summary>
        /// This exposes the Routes list, but since Lists do not serialize properly, we convert to/from an array.  We suppress the warning for this reason.
        /// </summary>
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

        private static string RegexpForCode(string szCode)
        {
            string szNormal = szCode.ToUpper(CultureInfo.InvariantCulture);
            if (szNormal.Length == 4 && szNormal.StartsWith("K", StringComparison.CurrentCultureIgnoreCase))
                return String.Format(CultureInfo.InvariantCulture, "K?{0}", szNormal.Substring(1, 3));
            return (szNormal.Length == 3) ? String.Format(CultureInfo.InvariantCulture, "K?{0}", szNormal) : szNormal;
        }

        private static string KeyForCityPair(string szCode1, string szCode2)
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
            get { return SearchedSegmentsCount == TotalSegmentCount; }
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
            dbh.CommandArgs.Timeout = 300;  // can be slow.
            const string szSearchTemplate = "SELECT route, idflight FROM flights WHERE route RLIKE '{0}[^a-z0-9]+{1}' OR route RLIKE '{1}[^a-z0-9]{0}' ORDER BY flights.Date ASC LIMIT 1";
            foreach (AirportList apl in SerializedRoutes)
            {
                airport[] rgap = apl.GetAirportList();
                for (int i = 0; i < rgap.Length - 1; i++)
                {
                    string szKey = KeyForCityPair(rgap[i].Code, rgap[i + 1].Code);
                    if (FlownSegments.ContainsKey(szKey))
                        continue;

                    dbh.CommandText = String.Format(CultureInfo.InvariantCulture, szSearchTemplate, RegexpForCode(rgap[i].Code), RegexpForCode(rgap[i + 1].Code));

                    int idFlight = LogbookEntryCore.idFlightNone;

                    dbh.ReadRow((comm) => { }, (dr) => { idFlight = Convert.ToInt32(dr["idflight"], CultureInfo.InvariantCulture); });

                    FlownSegment fs = new FlownSegment() { Segment = szKey, HasMatch = (idFlight != LogbookEntryCore.idFlightNone) };
                    fs.MatchingFlight = fs.HasMatch ? new LogbookEntry(idFlight, szUser:"ADMIN", lto: LogbookEntryCore.LoadTelemetryOption.None, fForceLoad:true) : null;

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

    #region TravelMap of visited locations
    /// <summary>
    /// Maps a country and admin to ISO codes.
    /// </summary>
    public class ISOMap
    {
        private readonly Dictionary<string, string> countryMap = new Dictionary<string, string>();
        private readonly Dictionary<string, string> admin1Map = new Dictionary<string, string>();

        private void InitMapping()
        {
            countryMap.Clear();
            admin1Map.Clear();

            DBHelper dbh = new DBHelper("SELECT * FROM isocodes");
            dbh.ReadRows((comm) => { },
                (dr) =>
                {
                    int level = Convert.ToInt32(dr["Level"], CultureInfo.InvariantCulture);
                    Dictionary<string, string> d = (level == 0) ? countryMap : admin1Map;
                    d[(string)dr["Region"]] = (string)dr["ISO"];
                });
        }

        public string CountryCodeForAirport(airport ap)
        {
            if (ap == null || String.IsNullOrEmpty(ap.Country))
                return string.Empty;

            return countryMap.TryGetValue(ap.Country, out string countryCode) ? countryCode : string.Empty;
        }

        public string AdminCodeForAirport(airport ap)
        {
            if (ap == null || String.IsNullOrEmpty(ap.Country))
                return string.Empty;

            return admin1Map.TryGetValue(ap.Admin1, out string adminCode) ? adminCode : ap.Admin1;
        }

        protected ISOMap()
        {
            InitMapping();
        }

        public static ISOMap CachedMap
        {
            get
            {
                const string cacheKey = "isoMapCacheKey";
                ISOMap map = (ISOMap)HttpRuntime.Cache[cacheKey];
                if (map == null)
                {
                    map = new ISOMap();
                    HttpRuntime.Cache[cacheKey] = map;
                }
                return map;
            }
        }

        public string ReverseLookupCountry(string code)
        {
            var result = code;
            foreach (string szCountry in countryMap.Keys)
                if (countryMap[szCountry].CompareCurrentCultureIgnoreCase(code) == 0)
                    result = szCountry;
            return result;
        }


        public string ReverseLookupAdmin1(string admin1)
        {
            foreach (string szAdmin1 in admin1Map.Keys)
                if (admin1Map[szAdmin1].CompareCurrentCultureIgnoreCase(admin1) == 0)
                    return szAdmin1;
            return admin1;
        }
    }

    public class VisitedCity
    {

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("description")]
        public string Description { get; set; } = "";

        [JsonProperty("coordinates")]
        public double[] Coordinates { get; private set; }

        [JsonProperty("visityear")]
        public int VisitYear { get; set; } = DateTime.Now.Year;

        public VisitedCity()
        {
            Coordinates = new double[2];
        }

        public VisitedCity(LatLong ll) : this()
        {
            if (ll == null)
                throw new ArgumentNullException(nameof(ll));

            Coordinates[0] = ll.Latitude;
            Coordinates[1] = ll.Longitude;
        }
    }

    public class VisitedAdmin1
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("cities")]
        public IList<VisitedCity> Cities { get; private set; }

        public VisitedAdmin1()
        {
            Cities = new List<VisitedCity>();
        }
    }

    public class VisitedCountry
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("cities")]
        public IList<VisitedCity> Cities { get; private set; }

        [JsonProperty("states")]
        public IList<VisitedAdmin1> Admin1s { get; private set; }

        private readonly Dictionary<string, VisitedAdmin1> d = new Dictionary<string, VisitedAdmin1>();

        public VisitedAdmin1 GetAdmin1(string idAdmin1, string name)
        {
            if (!d.TryGetValue(idAdmin1, out VisitedAdmin1 va1))
            {
                va1 = new VisitedAdmin1 { Id = idAdmin1, Name = name };
                d[idAdmin1] = va1;
                Admin1s.Add(va1);
            }
            return va1;
        }

        public VisitedCountry()
        {
            Cities = new List<VisitedCity>();
            Admin1s = new List<VisitedAdmin1>();
        }

        public VisitedCountry(string id) : this()
        {
            Id = id;
        }
    }

    public class VisitedLocations
    {

        [JsonProperty("countries")]
        public IList<VisitedCountry> Countries { get; private set; }

        public VisitedLocations()
        {
            Countries = new List<VisitedCountry>();
        }

        private readonly Dictionary<string, VisitedCountry> d = new Dictionary<string, VisitedCountry>();

        public VisitedCountry GetCountry(string idCountry)
        {
            if (!d.TryGetValue(idCountry, out VisitedCountry vc))
            {
                vc = new VisitedCountry(idCountry);
                d[idCountry] = vc;
                Countries.Add(vc);
            }

            return vc;
        }

        private readonly Dictionary<int, HashSet<string>> dByYear = new Dictionary<int, HashSet<string>>();

        public VisitedLocations(IEnumerable<VisitedAirport> airports) : this()
        {
            if (airports == null)
                return;

            ISOMap map = ISOMap.CachedMap;

            foreach (VisitedAirport va in airports)
            {
                string idCountry = map.CountryCodeForAirport(va.Airport);
                string idAdmin1 = map.AdminCodeForAirport(va.Airport);
                if (!String.IsNullOrEmpty(idCountry))   // ignore anything that doesn't map to a country code.
                {
                    VisitedCountry vc = GetCountry(idCountry);  // get (or re-use) the country.  This will add it to the countries list if necessary

                    // Treat each airport as a "city" within the admin1, in the travelmap argot, indicating the first visit.
                    // Visited airports are already coalesced (deduped), so we shouldn't need to worry about de-duping them the way we do for countries (i.e., multiple 
                    int year = va.EarliestVisitDate.Year;
                    VisitedCity vcity = new VisitedCity(va.Airport.LatLong) { Description = String.Format(CultureInfo.InvariantCulture, "{0}, {1}", va.Airport.Name, year), Name = va.Airport.Code, VisitYear = year };

                    // Add or re-use the admin1 and add the city to *that*
                    VisitedAdmin1 va1 = vc.GetAdmin1(idAdmin1, va.Airport.Admin1);
                    va1.Cities.Add(vcity);

                    if (!dByYear.TryGetValue(year, out HashSet<string> codes))
                        dByYear[year] = codes = new HashSet<string>();
                    codes.Add(idCountry);
                    if (!String.IsNullOrEmpty(idAdmin1))
                        codes.Add(idCountry + "-" + idAdmin1);
                }
            }
        }

        /// <summary>
        /// Generate locations based on visits, but without airports.
        /// </summary>
        /// <param name="szVisits">A string in the form of YEAR:code1,code2,code3;
        /// E.g.:
        /// 1998:US-NY,UK;1999:CHN,BRA, ...
        /// </param>
        public VisitedLocations(string szVisits) : this()
        {
            if (String.IsNullOrEmpty(szVisits))
                return;

            ISOMap map = ISOMap.CachedMap;

            Regex r = new Regex("(?<year>\\d{4}):(?<codes>[^;]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            MatchCollection mc = r.Matches(szVisits);
            foreach ( Match m in mc )
            {
                if (Int32.TryParse(m.Groups["year"].Value, out int year))
                {
                    string[] rgCodes = m.Groups["codes"].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    if (rgCodes.Any())
                    {
                        if (!dByYear.TryGetValue(year, out HashSet<string> codes))
                            dByYear[year] = codes = new HashSet<string>();

                        foreach (string code in rgCodes)
                        {
                            string[] rgCountryAdmin = code.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                            if (rgCountryAdmin.Length > 0)
                            {
                                string country = rgCountryAdmin[0];
                                string admin1 = rgCountryAdmin.Length > 1 ? rgCountryAdmin[1] : string.Empty;

                                VisitedCountry vc = GetCountry(rgCountryAdmin[0]);
                                VisitedCity vcity = new VisitedCity() { Description = String.Format(CultureInfo.InvariantCulture, "{0} {1}", map.ReverseLookupCountry(country), admin1, year), Name = code, VisitYear = year };
                                VisitedAdmin1 va1 = vc.GetAdmin1(admin1, map.ReverseLookupAdmin1(admin1));
                                va1.Cities.Add(vcity);
                                codes.Add(country);
                                if (!String.IsNullOrEmpty(admin1))
                                    codes.Add(code);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a timeline of visits, indexed by year.  Each year contains a cumulative list of country and country+admin1 codes
        /// Only works once VisitedLocations has been initialized from visitedairports
        /// </summary>
        /// <returns></returns>
        public IDictionary<int, IEnumerable<string>> BuildTimeline()
        {
            Dictionary<int, IEnumerable<string>> result = new Dictionary<int, IEnumerable<string>>();

            if (dByYear.Count == 0)
                return result;

            int minYear = int.MaxValue;
            int maxYear = int.MinValue;

            // Find the range of years
            foreach (int year in dByYear.Keys)
            {
                minYear = Math.Min(year, minYear);
                maxYear = Math.Max(year, maxYear);
            }

            for (int year = minYear; year <= maxYear; year++)
            {
                // Add all of the comulative visits through this year
                HashSet<string> curYearVisits = new HashSet<string>();
                result[year] = curYearVisits;
                for (int y2 = minYear; y2 <= year; y2++)
                {
                    if (dByYear.TryGetValue(y2, out HashSet<string> visits))
                    {
                        foreach (string s in visits)
                            curYearVisits.Add(s);
                    }
                }
            }


            return result;
        }
    }
    #endregion
}