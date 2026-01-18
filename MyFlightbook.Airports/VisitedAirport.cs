using MyFlightbook.Airports.Properties;
using MyFlightbook.CSV;
using MyFlightbook.Geography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2010-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/


namespace MyFlightbook.Airports
{
    /// <summary>
    /// Interface for an object that can visit one or more airports
    /// </summary>
    public interface IAirportVisitor
    {
        /// <summary>
        /// The path that was flown, in airport codes
        /// </summary>
        string Route { get; }
        /// <summary>
        /// The ID of the flight that visited the airports
        /// </summary>
        int FlightID { get; }

        /// <summary>
        /// The date of the flight.
        /// </summary>
        DateTime Date { get; }
    }

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
            FlightIDOfFirstVisit = -1;
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
        private static Dictionary<string, VisitedAirport> PopulateAirports(IEnumerable<IAirportVisitor> rgav)
        {
            Dictionary<string, VisitedAirport> dictVA = new Dictionary<string, VisitedAirport>();

            foreach (IAirportVisitor av in rgav)
            {
                string[] rgszapFlight = AirportList.NormalizeAirportList(av.Route);

                for (int iAp = 0; iAp < rgszapFlight.Length; iAp++)
                {
                    // we want to defer any db hit to the airport list until later, so we create an uninitialized airportlist
                    // We then visit each airport in the flight.

                    string szap = rgszapFlight[iAp].ToUpperInvariant();

                    // If it's explicitly a navaid, ignore it
                    if (szap.StartsWith(airport.ForceNavaidPrefix, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    VisitedAirport va = dictVA.TryGetValue(szap, out VisitedAirport value) ? value : null;

                    // Heuristic: if the flight only has a single airport, we visit that airport
                    // BUT if the flight has multiple airport codes, ignore the first airport in the list 
                    // UNLESS we've never seen that airport before.  (E.g., fly commercial to Stockton to pick up 40FG).
                    if (iAp == 0 && va != null && rgszapFlight.Length > 1)
                        continue;

                    // for now, the key holds the airport code, since the airport itself within the visited airport is still null
                    if (va == null)
                        dictVA[szap] = va = new VisitedAirport(av.Date) { FlightIDOfFirstVisit = av.FlightID };
                    else
                        va.VisitAirport(av.Date, av.FlightID);
                }
            }

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
        public static IEnumerable<VisitedAirport> VisitedAirportsFromVisitors(IEnumerable<IAirportVisitor> rgav)
        {
            if (rgav == null)
                throw new ArgumentNullException(nameof(rgav));

            Dictionary<string, VisitedAirport> dictVA = PopulateAirports(rgav);

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

        #region Download
        /// <summary>
        /// Gets a byte array to return a downloadable file of visited airports.
        /// </summary>
        /// <param name="rgva"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static byte[] DownloadVisitedTable(IEnumerable<VisitedAirport> rgva)
        {
            if (rgva == null)
                throw new ArgumentNullException(nameof(rgva));

            using (DataTable dt = new DataTable())
            {
                dt.Locale = CultureInfo.CurrentCulture;

                // add the header columns from the gridview
                dt.Columns.Add(new DataColumn(AirportResources.airportCode, typeof(string)));
                dt.Columns.Add(new DataColumn(AirportResources.airportName, typeof(string)));
                dt.Columns.Add(new DataColumn(AirportResources.airportCountry + "*", typeof(string)));
                dt.Columns.Add(new DataColumn(AirportResources.airportRegion, typeof(string)));
                dt.Columns.Add(new DataColumn(AirportResources.airportVisits, typeof(int)));
                dt.Columns.Add(new DataColumn(AirportResources.airportEarliestVisit, typeof(string)));
                dt.Columns.Add(new DataColumn(AirportResources.airportLatestVisit, typeof(string)));

                foreach (VisitedAirport va in rgva)
                {
                    DataRow dr = dt.NewRow();
                    dr[0] = va.Code;
                    dr[1] = va.Airport.Name;
                    dr[2] = va.Country;
                    dr[3] = va.Admin1;
                    dr[4] = va.NumberOfVisits;
                    dr[5] = va.EarliestVisitDate.ToString("d", CultureInfo.CurrentCulture);
                    dr[6] = va.LatestVisitDate.ToString("d", CultureInfo.CurrentCulture);

                    dt.Rows.Add(dr);
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8, 1024))
                    {
                        CsvWriter.WriteToStream(sw, dt, true, true);
                        sw.Write(String.Format(CultureInfo.CurrentCulture, "\r\n\r\n\" *{0}\"", Branding.ReBrand(AirportResources.airportCountryDisclaimer)));
                        sw.Flush();
                        return ms.ToArray();
                    }
                }
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
                ISOMap map = (ISOMap)util.GlobalCache.Get(cacheKey);
                if (map == null)
                {
                    map = new ISOMap();
                    util.GlobalCache.Set(cacheKey, map, DateTimeOffset.UtcNow.AddHours(1));
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