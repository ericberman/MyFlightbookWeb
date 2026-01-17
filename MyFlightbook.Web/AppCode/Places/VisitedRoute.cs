using MyFlightbook.Image;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2010-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Airports
{
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
                    fs.MatchingFlight = fs.HasMatch ? new LogbookEntry(idFlight, szUser: "ADMIN", lto: LogbookEntryCore.LoadTelemetryOption.None, fForceLoad: true) : null;

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
}