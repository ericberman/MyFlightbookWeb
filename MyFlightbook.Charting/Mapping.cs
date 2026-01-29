using MyFlightbook.Airports;
using MyFlightbook.Geography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Mapping
{
    #region Google Maps
    public enum GMap_MapType { G_NORMAL_MAP, G_SATELLITE_MAP, G_HYBRID_MAP, G_TERRAIN };

    public enum GMap_Mode { Dynamic, Static }

    public enum GMap_ZoomLevels { Invalid = 0, World = 1, US = 4, AirportAndVicinity = 10, Airport = 14 };

    /// <summary>
    /// Encapsulates settings and data for a google map on the page.
    /// </summary>
    public class GoogleMap
    {
        #region Constructors
        public GoogleMap() { }

        public GoogleMap(string containerName, GMap_Mode mapMode = GMap_Mode.Static) : base()
        {
            MapMode = mapMode;

            Options.divContainer = containerName ?? throw new ArgumentNullException(nameof(containerName));
        }
        #endregion

        #region properties
        public MFBGoogleMapOptions Options { get; private set; } = new MFBGoogleMapOptions();

        /// <summary>
        /// Dynamic or static map?
        /// </summary>
        public GMap_Mode MapMode { get; set; } = GMap_Mode.Dynamic;

        /// <summary>
        /// Path to display
        /// </summary>
        public IEnumerable<LatLong> Path { get; set; } = Array.Empty<LatLong>();

        /// <summary>
        /// Javascript to call on click
        /// </summary>
        public string ClickHandler { get; set; } = string.Empty;

        /// <summary>
        /// An array of MFBImageInfo objects that are thumbnails to display on the map
        /// </summary>
        public IEnumerable<IMapMarker> Images { get; set; } = Array.Empty<IMapMarker>();

        /// <summary>
        /// A list of clubs to display
        /// </summary>
        public IEnumerable<IMapMarker> Clubs { get; set; } = Array.Empty<IMapMarker>();

        /// <summary>
        /// A list of AirportLists containing the airports to display.  If markers are on, the airports within an airportlist will be connected
        /// </summary>
        public IEnumerable<AirportList> Airports { get; set; } = new List<AirportList>();

        /// <summary>
        /// Helper to set a single airportlist rather than an enumerable of them
        /// </summary>
        /// <param name="al"></param>
        public void SetAirportList(AirportList al)
        {
            Airports = new List<AirportList>() { al };
        }

        /// <summary>
        /// Normally, the airportlist is normalized; set this to "true" to use the raw (including duplicate) list.
        /// </summary>
        public Boolean AllowDupeMarkers { get; set; }
        #endregion

        #region Static Map
        /// <summary>
        /// Additional querystring arguments for a static map.  E.g., style=feature:all|element:labels|visibility:off
        /// </summary>
        public string StaticMapAdditionalParams { get; set; }

        /// <summary>
        /// Returns the HREF string suitable for use in an IMG tag (i.e., for a static map vs. a dynamic map)
        /// <param name="szMapKey">The key for using the google maps API</param>
        /// <param name="width">The width for the static map</param>
        /// <param name="height">The height for the static map</param>
        /// </summary>
        public string StaticMapHRef(string szMapKey, int height, int width)
        {
            StringBuilder sb = new StringBuilder(String.Format(CultureInfo.InvariantCulture, "https://maps.googleapis.com/maps/api/staticmap?maptype=hybrid&key={0}&size={1}x{2}", HttpUtility.UrlEncode(szMapKey), width, height));

            bool fHasRoute = Airports != null && Airports.Any();
            bool fHasPath = Path != null && Path.Any() && Options.fShowRoute;

            if (!String.IsNullOrEmpty(StaticMapAdditionalParams))
                sb.AppendFormat(CultureInfo.InvariantCulture, "&{0}", StaticMapAdditionalParams);

            if (fHasPath || fHasRoute)
            {
                List<List<string>> lstSegments = new List<List<string>>();
                if (fHasRoute)
                {
                    List<string> lstMarkers = new List<string>();
                    foreach (AirportList al in Airports)
                    {
                        List<string> lstSegment = new List<string>();
                        lstSegments.Add(lstSegment);
                        foreach (airport ap in al.GetAirportList())
                        {
                            string szSegment = String.Format(CultureInfo.InvariantCulture, "{0:F6},{1:F6}", ap.LatLong.Latitude, ap.LatLong.Longitude);
                            lstMarkers.Add(szSegment);
                            lstSegment.Add(szSegment);
                        }
                    }

                    if (Options.fShowMarkers)
                        sb.Append("&markers=color:red|" + String.Join("|", lstMarkers));
                }
                if (Options.fShowRoute)
                {
                    string szPath = string.Empty;
                    if (fHasPath)
                    {
                        List<string> lstPath = new List<string>();
                        foreach (LatLong ll in Path)
                            lstPath.Add(String.Format(CultureInfo.InvariantCulture, "{0:F6},{1:F6}", ll.Latitude, ll.Longitude));
                        szPath = String.Join("|", lstPath);
                    }

                    // always connect the segments...
                    foreach (List<string> segment in lstSegments)
                        sb.Append("&path=color:0x" + Options.RouteColor.Substring(1) + "88|weight:5|geodesic:true|" + String.Join("|", segment));

                    // and add the path, if possible
                    if (sb.Length + szPath.Length < 8100)
                        sb.Append("&path=color:0x" + Options.PathColor.Substring(1) + "88|weight:5|" + szPath);
                }

                if (lstSegments.Count == 1 && lstSegments[0].Count == 1)
                    sb.AppendFormat(CultureInfo.InvariantCulture, "&zoom={0}", (int)GMap_ZoomLevels.Airport);
            }
            else
                sb.AppendFormat(CultureInfo.InvariantCulture, "&center={0:F8},{1:F8}&zoom={2}", Options.defaultLat, Options.defaultLong, Options.defaultZoom);

            return sb.ToString();
        }
        #endregion

        internal static string MapTypeToString(GMap_MapType mp)
        {
            switch (mp)
            {
                case GMap_MapType.G_HYBRID_MAP:
                    return "google.maps.MapTypeId.HYBRID";
                case GMap_MapType.G_NORMAL_MAP:
                    return "google.maps.MapTypeId.ROADMAP";
                case GMap_MapType.G_SATELLITE_MAP:
                    return "google.maps.MapTypeId.SATELLITE";
                case GMap_MapType.G_TERRAIN:
                    return "google.maps.MapTypeId.TERRAIN";
                default:
                    return mp.ToString();
            }
        }

        /// <summary>
        /// Registers the script block that includes the data for the airports and the preferences for the map.
        /// <param name="mapID">The ID of the element on the page that holds the map</param>
        /// <param name="containerID">The client ID of the map's container</param>
        /// </summary>
        public string MapJScript(string mapID, string containerID)
        {
            Options.id = mapID;
            Options.divContainer = containerID;

            // figure out any bounding box, adding airports, images, paths, and clubs.
            Options.BoundingBox = null;
            Options.SetAirports(Airports, AllowDupeMarkers);
            Options.SetPath(Path);
            Options.SetClubs(Clubs);
            Options.SetImages(Images);
            Options.fAutoZoom = Options.BoundingBox != null && !Options.BoundingBox.IsEmpty;
            if (Options.BoundingBox == null)
                Options.BoundingBox = new LatLongBox(new LatLong(Options.defaultLat, Options.defaultLong));

            StringBuilder sbInitMap = new StringBuilder("function () {\r\n");

            // If there is a path, define a variable to hold it now.
            // We do this as an array of 2-element latitude/longitude pairs; setPath already converted the path to MFBGMapLatLong objects
            if (Path != null && Path.Any())
                sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "var {0} = {1};\r\n", Options.PathVarName, JsonConvert.SerializeObject(MFBGMapLatLon.AsArrayOfArrays(Options.pathArray)));
            else
                sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "var {0} = null;\r\n", Options.PathVarName);

            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, @"var {0} = AddMap(new MFBNewMapOptions({1}, {2})); MFBMapsOnPage.push({0});", mapID, JsonConvert.SerializeObject(Options), Options.PathVarName);

            if (ClickHandler.Length > 0)
                sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "\r\n{0}.addListenerFunction({1});\r\n", mapID, ClickHandler);

            sbInitMap.Append("\r\n}");

            return sbInitMap.ToString();
        }
    }

    /// <summary>
    /// Helper class to encapsulate the data for a google map object.
    /// </summary>
    public class MFBGoogleMapOptions
    {
        #region Properties
        /// <summary>
        /// ID for the map
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// Google Map ID
        /// </summary>
        public string MapID { get; set; }

        /// <summary>
        /// Name of the element containing the map
        /// </summary>
        public string divContainer { get; set; }

        /// <summary>
        /// Auto-fill airports on pan/zoom?
        /// </summary>
        public bool fAutofillPanZoom { get; set; }

        public bool fAutoZoom { get; set; }

        /// <summary>
        /// Autofill should include heliports or not?
        /// </summary>
        public bool fAutofillHeliports { get; set; }

        /// <summary>
        /// Is the map visible?
        /// </summary>
        public bool fShowMap { get; set; } = true;

        /// <summary>
        /// True to show the route (airport to airport) - i.e., connect-the-dots.
        /// </summary>
        public bool fShowRoute { get; set; } = true;

        /// <summary>
        /// True to show markers
        /// </summary>
        public bool fShowMarkers { get; set; } = true;

        /// <summary>
        /// Preferred zoom factor, as an integer
        /// </summary>
        public int defaultZoom
        {
            get { return (int)ZoomFactor; }
            set { ZoomFactor = (GMap_ZoomLevels)value; }
        }

        /// <summary>
        /// Zoom factor
        /// </summary>
        [JsonIgnore]
        public GMap_ZoomLevels ZoomFactor { get ;set; } = GMap_ZoomLevels.US;

        /// <summary>
        /// Type of map - satellite, hybrid, etc.
        /// </summary>
        [JsonIgnore]
        public GMap_MapType MapType
        {
            get { return (GMap_MapType)Enum.Parse(typeof(GMap_MapType), mapType); }
            set { mapType = value.ToString(); }
        }

        public string mapType { get; set; } = GoogleMap.MapTypeToString(GMap_MapType.G_HYBRID_MAP);

        public IEnumerable<IEnumerable<MFBAirportMarker>> rgAirports { get; private set; }

        /// <summary>
        /// Path to display
        /// </summary>
        [JsonIgnore]
        public IEnumerable<IMapMarker> pathArray { get; private set; }

        /// <summary>
        /// Name for the variable that holds the flight path array (array of lat/longs)
        /// </summary>
        public string PathVarName { get; set; } = "rgPath";

        public IEnumerable<IMapMarker> rgImages { get; private set; }

        public IEnumerable<IMapMarker> rgClubs { get; private set; }

        [JsonIgnore]
        public LatLongBox BoundingBox { get; set; }

        public double minLat
        {
            get { return BoundingBox.LatMin; }
            set { BoundingBox.LatMin = value; }
        }

        public double maxLat
        {
            get { return BoundingBox.LatMax; }
            set { BoundingBox.LatMax = value; }
        }

        public double minLong
        {
            get { return BoundingBox.LongMin; }
            set { BoundingBox.LongMin = value; }
        }


        public double maxLong
        {
            get { return BoundingBox.LongMax; }
            set { BoundingBox.LongMax = value; }
        }

        /// <summary>
        /// Center latitude of map to use, as needed
        /// </summary>
        public double defaultLat { get; set; } = 35.224762; // approximate center of US

        /// <summary>
        /// Center latitude of map to use, as needed
        /// </summary>
        public double defaultLong { get; set; } = -86.156354; // approximate center of US

        [JsonIgnore]
        public LatLong MapCenter
        {
            get { return new LatLong(defaultLat, defaultLong); }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                defaultLat = value.Latitude;
                defaultLong = value.Longitude;
            }
        }

        public const string DefaultRouteColor = "#0000FF";

        public const string DefaultPathColor = "#FF0000";

        public string RouteColor { get; set; } = DefaultRouteColor;

        public string PathColor { get; set; } = DefaultPathColor;
        #endregion

        private void UpdateBoundingBox(IMapMarker gll)
        {
            if (BoundingBox == null)
                BoundingBox = new LatLongBox(gll.latlong);
            else
                BoundingBox.ExpandToInclude(gll.latlong);
        }

        private void UpdateBoundingBox(IEnumerable<IMapMarker> rgll)
        {
            if (rgll == null)
                return;
            foreach (IMapMarker ll in rgll)
                UpdateBoundingBox(ll);
        }

        public void SetAirports(IEnumerable<AirportList> rgal, bool AllowDupeMarkers)
        {
            if (rgal == null || !rgal.Any())
                return;

            int cItems = 0;
            List<IEnumerable<MFBAirportMarker>> routes = new List<IEnumerable<MFBAirportMarker>>();
            foreach (AirportList airportlist in rgal)
            {
                IEnumerable<MFBAirportMarker> segment = MFBAirportMarker.FromAirports(AllowDupeMarkers ? airportlist.GetAirportList() : airportlist.GetNormalizedAirports(), fShowMarkers);
                UpdateBoundingBox(segment);
                routes.Add(segment);
                cItems += segment.Count();
            }

            if (cItems > 0 && (BoundingBox == null || BoundingBox.IsEmpty))
                 ZoomFactor = GMap_ZoomLevels.Airport;

            rgAirports = routes;
        }

        public void SetImages(IEnumerable<IMapMarker> rgmfbii)
        {
            if (rgmfbii == null)
                return;

            rgImages = rgmfbii;
        }

        public void SetClubs(IEnumerable<IMapMarker> clubs)
        {
            if (clubs == null)
                return;

            foreach (IMapMarker c in clubs)
                UpdateBoundingBox(c);

            rgClubs = clubs;
        }

        public void SetPath(IEnumerable<LatLong> rgll)
        {
            if (rgll == null)
                return;

            List<IMapMarker> lst = new List<IMapMarker>();
            lst.AddRange(MFBGMapLatLon.FromLatlongs(rgll));
            foreach (IMapMarker gll in lst)
                UpdateBoundingBox(gll);
            pathArray = lst;
        }
    }

    [Serializable]
    public class MFBAirportMarker : MFBGMapLatLon
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string Type { get; set; }
        public bool fShowMarker { get; set; }

        public static IEnumerable<MFBAirportMarker> FromAirports(IEnumerable<airport> rgap, bool fShowMarkers)
        {
            if (rgap == null || !rgap.Any())
                return Array.Empty<MFBAirportMarker>();

            List<MFBAirportMarker> lst = new List<MFBAirportMarker>();
            foreach (airport ap in rgap)
            {
                // Google maps barfs on anything in the high/low latitudes.
                if (Math.Abs(ap.LatLong.Latitude) > 88)
                    continue;
                lst.Add(new MFBAirportMarker() { latitude = ap.LatLong.Latitude, longitude = ap.LatLong.Longitude, Name = ap.NameWithGeoRegion, Code = ap.Code, Type = ap.FacilityType, fShowMarker = fShowMarkers });
            }
            return lst;
        }
    }
    #endregion
}