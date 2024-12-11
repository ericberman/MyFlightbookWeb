using MyFlightbook.Geography;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2010-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Airports
{
    /// <summary>
    /// Result from parsing a list of routes
    /// </summary>
    [Serializable]
    public class ListsFromRoutesResults
    {
        /// <summary>
        /// The resulting list of AirportLists
        /// </summary>
        public List<AirportList> Result { get; private set; }

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
    /// Represents a segment of a route, including distance and two endpoints.
    /// </summary>
    public class RouteSegment : IComparable<RouteSegment>
    {
        #region properties
        public IFix From { get; set; } = null;
        public IFix To { get; set; } = null;

        /// <summary>
        /// Distance, in nm
        /// </summary>
        public double Distance { get; set; } = 0.0;
        #endregion

        public RouteSegment() { }

        public RouteSegment(IFix from, IFix to, double distance) : this()
        {
            From = from;
            To = to;
            Distance = distance;
        }

        public RouteSegment(IFix from, IFix to) : this(from, to, 0)
        {
            if (from != null && to != null)
                Distance = from.DistanceFromFix(to);
        }

        public int CompareTo(RouteSegment other)
        {
            return Distance.CompareTo(other?.Distance);
        }

        public override string ToString()
        {
            return Distance == 0.0 ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.RouteSegmentTemplate, Distance, From.Code, To.Code);
        }

        public static RouteSegment LongerRouteSegment(RouteSegment rs1, RouteSegment rs2)
        {
            if (rs1 == null && rs2 == null)
                return null;
            if (rs1 == null)
                return rs2;
            if (rs2 == null)
                return rs1;

            return (rs1.CompareTo(rs2) > 0) ? rs1 : rs2;
        }

        public static RouteSegment Empty
        {
            get { return new RouteSegment(); }
        }

        public static implicit operator double(RouteSegment rs)
        {
            return rs == null ? 0 : rs.Distance;
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
        public airport[] AirportArrayForSerialization
        {
            get { return m_rgAirports?.ToArray() ?? Array.Empty<airport>(); }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
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
                throw new ArgumentNullException(nameof(rgVA));
            m_rgAirports = new List<airport>();
            List<string> alAirports = new List<string>();
            foreach (VisitedAirport va in rgVA)
            {
                m_rgAirports.Add(va.Airport);
                m_htAirportsByCode[va.Code] = va.Airport;

                if (!String.IsNullOrEmpty(va.Aliases))
                {
                    foreach (string alias in NormalizeAirportList(va.Aliases))
                    {
                        if (!m_htAirportsByCode.ContainsKey(alias))
                            m_htAirportsByCode[alias] = va.Airport;
                    }
                }

                alAirports.Add(va.Code);
            }

            m_rgszAirportsNormal = alAirports.ToArray();
        }

        public AirportList(IEnumerable<airport> rgap) : this()
        {
            if (rgap == null)
                throw new ArgumentNullException(nameof(rgap));
            m_rgAirports = new List<airport>(rgap);
            List<string> rgszap = new List<string>();
            foreach (airport ap in rgap)
            {
                m_rgAirports.Add(ap);
                m_htAirportsByCode[ap.Code] = ap;
                rgszap.Add(ap.Code);
            }
            m_rgszAirportsNormal = rgszap.ToArray();
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
                m_htAirportsByCode.TryGetValue(szKey, out airport ap2);
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

            if (rgRoutes == null || !rgRoutes.Any())
                return new ListsFromRoutesResults(lst, new AirportList(string.Empty));

            // Create a single "worker" airportlist from which we will create the others.
            AirportList aplMaster = new AirportList(String.Join(" ", rgRoutes.ToArray()));

            Dictionary<string, AirportList> dictRoutes = new Dictionary<string, AirportList>();

            // Now create the resulting airport lists.  Exclude duplicate routes
            foreach (string szRoute in rgRoutes)
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
                throw new ArgumentNullException(nameof(szAirports));
            return ListsFromRoutes(new List<string>(szAirports.Split(new string[] { RouteSeparator }, StringSplitOptions.RemoveEmptyEntries)));
        }

        /// <summary>
        /// Returns a list of airportlists for the specified query, breaking into paths if the route is meant to be shown.  Just returns alMatches if not showing routes
        /// </summary>
        /// <param name="fq">The flight query</param>
        /// <param name="alMatches">An airport list of all known matches</param>
        /// <param name="fShowRoute">True to break it into separate routes</param>
        /// <returns></returns>
        public static IEnumerable<AirportList> PathsForQuery(FlightQuery fq, AirportList alMatches, bool fShowRoute)
        {
            if (alMatches == null)
                throw new ArgumentNullException(nameof(alMatches));
            if (fq == null)
                throw new ArgumentNullException(nameof(fq));

            if (!fShowRoute)
                return new AirportList[] { alMatches };

            List<AirportList> lst = new List<AirportList>();

            DBHelper dbh = new DBHelper(LogbookEntryBase.QueryCommand(fq, lto: LogbookEntryCore.LoadTelemetryOption.None));
            dbh.ReadRows((comm) => { }, (dr) =>
            {
                object o = dr["Route"];
                string szRoute = (string)(o == DBNull.Value ? string.Empty : o);

                if (!String.IsNullOrEmpty(szRoute))
                    lst.Add(alMatches.CloneSubset(szRoute));
            });
            return lst;
        }
        #endregion

        #region Extracting airports
        /// <summary>
        /// Find an airport in the hashtable by the code.  Implements the navaid forcing and the K-hack for US airports
        /// </summary>
        /// <param name="sz">The code of the airport</param>
        /// <returns>The matching airport, null if not found.</returns>
        public airport GetAirportByCode(string sz)
        {
            if (sz == null)
                return null;

            // Direct hit - return it
            if (m_htAirportsByCode.TryGetValue(sz, out airport ap))
                return ap;
            // If this *doesn't* have the forcenavaidprefix, see if the navaid is in the hash table and return that.
            else if (!sz.StartsWith(airport.ForceNavaidPrefix, StringComparison.CurrentCultureIgnoreCase) && m_htAirportsByCode.TryGetValue(airport.ForceNavaidPrefix + sz, out ap))
                return ap;
            // issue #764: try to allow @(airport) in addition to forcing a navaid, but treat it as an airspace fix, not an airport.
            else if (sz.StartsWith(airport.ForceNavaidPrefix, StringComparison.CurrentCultureIgnoreCase) && m_htAirportsByCode.TryGetValue(sz.Substring(airport.ForceNavaidPrefix.Length), out ap))
                return new AdHocFix(ap.LatLong.ToAdhocFixString());
            // else try the USPrefix hack - look for the code with the "K" prefix removed.
            else if (airport.IsUSAirport(sz))
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

        [JsonIgnore]
        public IEnumerable<airport> UniqueAirports
        {
            get { return (m_htAirportsByCode == null) ? Array.Empty<airport>() : (IEnumerable<airport>)m_htAirportsByCode.Values; }
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
                throw new ArgumentNullException(nameof(szAirports));
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
                throw new ArgumentNullException(nameof(rgap));
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
            m_rgszAirportsNormal = Array.Empty<string>();
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
        /// <returns>0 or the maximum distance.  If > 0, ap1/ap2 are filled</returns>
        public RouteSegment MaxDistanceForRoute()
        {
            RouteSegment rs = RouteSegment.Empty;
            airport[] rgAp = GetNormalizedAirports();
            int cAp = rgAp.Length;
            if (cAp >= 2)
            {
                for (int i = 0; i < cAp; i++)
                {
                    for (int j = i + 1; j < cAp; j++)
                        if (rgAp[i].IsPort && rgAp[j].IsPort)
                            rs = RouteSegment.LongerRouteSegment(rs, new RouteSegment(rgAp[i], rgAp[j]));
                }
            }

            return rs;
        }

        /// <summary>
        /// Examines a route and returns the total distance flown.
        /// </summary>
        /// <returns>The distance for the route, in NM</returns>
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
        public RouteSegment MaxSegmentForRoute()
        {
            RouteSegment rs = RouteSegment.Empty;
            airport[] rgAp = GetNormalizedAirports();
            for (int i = 1; i < rgAp.Length; i++)
                rs = RouteSegment.LongerRouteSegment(rs, new RouteSegment(rgAp[i - 1], rgAp[i]));
            return rs;
        }

        /// <summary>
        /// Find the furthest distance away from the STARTING airport that was flown.
        /// </summary>
        /// <returns>Distance in nm.</returns>
        public RouteSegment MaxDistanceFromStartingAirport()
        {
            RouteSegment rs = RouteSegment.Empty;
            airport[] rgAp = GetNormalizedAirports();
            for (int i = 1; i < rgAp.Length; i++)
                rs = RouteSegment.LongerRouteSegment(rs, new RouteSegment(rgAp[0], rgAp[i]));
            return rs;
        }
        #endregion
    }
}