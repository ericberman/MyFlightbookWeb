using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using MyFlightbook.Airports;
using MyFlightbook.Clubs;
using MyFlightbook.Image;
using MyFlightbook.Geography;

/******************************************************
 * 
 * Copyright (c) 2015-2018 MyFlightbook LLC
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
        #region properties
        /// <summary>
        /// Type of map - satellite, hybrid, etc.
        /// </summary>
        public GMap_MapType MapType { get; set; }

        /// <summary>
        /// Dynamic or static map?
        /// </summary>
        public GMap_Mode MapMode { get; set; }

        /// <summary>
        /// True to show markers
        /// </summary>
        public Boolean ShowMarkers { get; set; }

        /// <summary>
        /// True to show the route (airport to airport) - i.e., connect-the-dots
        /// </summary>
        public Boolean ShowRoute { get; set; }

        /// <summary>
        /// Is the map visible?
        /// </summary>
        public Boolean MapVisible { get; set; }

        /// <summary>
        /// Path to display
        /// </summary>
        public IEnumerable<LatLong> Path { get; set; }

        /// <summary>
        /// Zoom factor
        /// </summary>
        public GMap_ZoomLevels ZoomFactor { get; set; }

        /// <summary>
        /// Center of the map
        /// </summary>
        public LatLong MapCenter { get; set; }

        /// <summary>
        /// Javascript to call on click
        /// </summary>
        public string ClickHandler { get; set; }

        /// <summary>
        /// An array of MFBImageInfo objects that are thumbnails to display on the map
        /// </summary>
        public IEnumerable<MFBImageInfo> Images { get; set; }

        /// <summary>
        /// A list of clubs to display
        /// </summary>
        public IEnumerable<Club> Clubs { get; set; }

        /// <summary>
        /// Name of a function to call when a club gets clicked.  The ID of the club is the parameter
        /// </summary>
        public string ClubClickHandler { get; set; }

        private List<AirportList> m_Airports = null;
        /// <summary>
        /// A list of AirportLists containing the airports to display.  If markers are on, the airports within an airportlist will be connected
        /// </summary>
        public IEnumerable<AirportList> Airports
        {
            get
            {
                if (m_Airports == null)
                    m_Airports = new List<AirportList>();
                return m_Airports;
            }
            set
            {
                m_Airports = new List<AirportList>(value);
            }
        }

        /// <summary>
        /// Helper to set a single airportlist rather than an enumerable of them
        /// </summary>
        /// <param name="al"></param>
        public void SetAirportList(AirportList al)
        {
            m_Airports = new List<AirportList>() { al };
        }

        /// <summary>
        /// Auto-fill airports on pan/zoom?
        /// </summary>
        public Boolean AutofillOnPanZoom { get; set; }

        /// <summary>
        /// Normally, the airportlist is normalized; set this to "true" to use the raw (including duplicate) list.
        /// </summary>
        public Boolean AllowDupeMarkers { get; set; }

        /// <summary>
        /// Name for the variable that holds the flight path array (array of lat/longs)
        /// </summary>
        public string PathVarName { get; set; }

        private LatLongBox BoundingBox { get; set; }
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

            bool fHasRoute = Airports != null && Airports.Count() > 0;
            bool fHasPath = Path != null && Path.Count() > 0 && ShowRoute;

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

                    if (ShowMarkers)
                        sb.Append("&markers=color:red|" + String.Join("|", lstMarkers));
                }
                if (ShowRoute)
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
                        sb.Append("&path=color:0x0000FF88|weight:5|geodesic:true|" + String.Join("|", segment));

                    // and add the path, if possible
                    if (sb.Length + szPath.Length < 8100)
                        sb.Append("&path=color:0xFF000088|weight:5|" + szPath);
                }
            }
            else
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "&center={0:F8},{1:F8}&zoom={2}", MapCenter.Latitude, MapCenter.Longitude, (int)ZoomFactor);
            }

            return sb.ToString();
        }
        #endregion

        #region Constructors
        public GoogleMap()
        {
            MapType = GMap_MapType.G_HYBRID_MAP;
            MapMode = GMap_Mode.Dynamic;
            ShowMarkers = true;
            ShowRoute = true;
            MapVisible = true;
            Path = new LatLong[0];
            ZoomFactor = GMap_ZoomLevels.US;
            MapCenter = new LatLong(35.224762, -86.156354); // approximate center of US
            ClickHandler = string.Empty;
            PathVarName = "rgPath";
        }
        #endregion

        private string MapTypeToString(GMap_MapType mp)
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

        #region Helper routines for Javascript generation
        private string ProcessAirports()
        {
            List<string> lstAllRoutes = new List<string>();
            if (Airports != null)
            {
                int cItems = 0;

                foreach (AirportList airportlist in Airports)
                {
                    airport[] rgap = AllowDupeMarkers ? airportlist.GetAirportList() : airportlist.GetNormalizedAirports();

                    List<string> lstRoute = new List<string>();

                    for (int i = 0; i < rgap.Length; i++)
                    {
                        // Google maps barfs on anything in the high/low latitudes.
                        if (Math.Abs(rgap[i].LatLong.Latitude) > 88)
                            continue;

                        lstRoute.Add(String.Format(CultureInfo.InvariantCulture, "new MFBAirportMarker({0}, {1}, '{2}', '{3}', '{4}', {5})", rgap[i].LatLong.Latitude, rgap[i].LatLong.Longitude, rgap[i].Name.JavascriptEncode(), rgap[i].Code, rgap[i].FacilityType, ShowMarkers ? 1 : 0));

                        if (BoundingBox == null)
                            BoundingBox = new LatLongBox(rgap[i].LatLong);
                        else
                            BoundingBox.ExpandToInclude(rgap[i].LatLong);

                        cItems++;
                    }

                    lstAllRoutes.Add(String.Format(CultureInfo.InvariantCulture, "[{0}]", String.Join(",\r\n", lstRoute.ToArray())));
                }

                if (cItems > 0 && (BoundingBox == null || BoundingBox.IsEmpty))
                    ZoomFactor = GMap_ZoomLevels.Airport;
            }

            return String.Format(CultureInfo.InvariantCulture, "[{0}]", String.Join(",\r\n", lstAllRoutes.ToArray()));
        }

        private string ProcessImages()
        {
            StringBuilder szImages = new StringBuilder("[");
            if (Images != null)
            {
                LatLong ll;
                List<string> lstPhotoMarkers = new List<string>();
                foreach (MFBImageInfo ii in Images)
                {
                    double ratio = MFBImageInfo.ResizeRatio(MFBImageInfo.ThumbnailHeight / 2, MFBImageInfo.ThumbnailWidth / 2, ii.HeightThumbnail, ii.WidthThumbnail);
                    if ((ll = ii.Location) != null)
                        lstPhotoMarkers.Add(String.Format(CultureInfo.InvariantCulture, "new MFBPhotoMarker('{0}', '{1}', {2}, {3}, '{4}', {5}, {6})", ii.URLThumbnail, ii.URLFullImage, ll.Latitude, ll.Longitude, HttpUtility.HtmlEncode(ii.Comment).JavascriptEncode(), (int)(ii.WidthThumbnail * ratio), (int)(ii.HeightThumbnail * ratio)));
                }
                szImages.Append(String.Join(", ", lstPhotoMarkers.ToArray()));
            }
            szImages.Append("]");

            return szImages.ToString();
        }

        private string ProcessClubs()
        {
            StringBuilder szClubs = new StringBuilder("[");
            if (Clubs != null)
            {
                List<string> lstClubs = new List<string>();
                foreach (Club c in Clubs)
                {
                    if (c.HomeAirport != null && !String.IsNullOrEmpty(c.HomeAirport.Code))
                    {
                        lstClubs.Add(String.Format(CultureInfo.InvariantCulture, "new MFBClubMarker({0}, {1}, '{2}', {3}, '{4}')", c.HomeAirport.LatLong.Latitude, c.HomeAirport.LatLong.Longitude, c.Name.JavascriptEncode(), c.ID, ClubClickHandler));
                        if (BoundingBox == null)
                            BoundingBox = new LatLongBox(c.HomeAirport.LatLong);
                        else
                            BoundingBox.ExpandToInclude(c.HomeAirport.LatLong);
                    }
                }
                szClubs.AppendFormat(CultureInfo.InvariantCulture, "{0}", String.Join(",\r\n", lstClubs.ToArray()));
            }
            szClubs.Append("]");
            return szClubs.ToString();
        }

        private string ProcessPath()
        {
            StringBuilder sbPath = new StringBuilder();
            if (Path != null && Path.Count() > 0)
            {
                if (BoundingBox == null)
                    BoundingBox = new LatLongBox(Path.ElementAt(0));

                foreach (LatLong ll in Path)
                {
                    BoundingBox.ExpandToInclude(ll);
                    sbPath.AppendFormat(CultureInfo.InvariantCulture, "{0} nll({1}, {2})", (sbPath.Length > 0) ? ",\r\n" : "", ll.Latitude, ll.Longitude);
                }
            }
            return sbPath.ToString();
        }
        #endregion

        /// <summary>
        /// Registers the script block that includes the data for the airports and the preferences for the map.
        /// <param name="mapID">The ID of the element on the page that holds the map</param>
        /// <param name="containerID">The client ID of the map's container</param>
        /// </summary>
        public string MapJScript(string mapID, string containerID)
        {
            Boolean fAutoZoom = false;

            // Initialize any airports
            BoundingBox = null;
            StringBuilder sbJSAirports = new StringBuilder("function InitAirports() {\r\n", 2000);

            string szAirportMarkers = ProcessAirports();

            string szImages = ProcessImages();

            string szClubs = ProcessClubs();
            fAutoZoom = (BoundingBox != null && !BoundingBox.IsEmpty);

            string szPath = ProcessPath();
            if (!String.IsNullOrEmpty(szPath))
                fAutoZoom = true;

            StringBuilder sbInitMap = new StringBuilder();

            // If there is a path, define a variable to hold it now.
            if (Path != null && Path.Count() > 0)
                sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "var {0} = [{1}];\r\n", PathVarName, szPath);

            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "var {0} = AddMap(MFBNewMapOptions({{", mapID);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "id: '{0}', ", mapID);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "rgAirports: {0}, ", szAirportMarkers);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "rgImages: {0}, ", szImages);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "rgClubs: {0}, ", szClubs);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "divContainer: '{0}', ", containerID);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "fZoom: {0}, ", 1);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "fAutofillPanZoom: {0}, ", AutofillOnPanZoom ? 1 : 0);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "fShowMap: {0}, ", MapVisible ? 1 : 0);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "fShowMarkers: {0}, ", ShowMarkers ? 1 : 0);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "fShowRoute: {0}, ", ShowRoute ? 1 : 0);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "fDisableUserManip: {0}, ", 0);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "defaultZoom: {0}, ", Convert.ToInt32(ZoomFactor, CultureInfo.InvariantCulture));
            sbInitMap.Append(String.Format(CultureInfo.InvariantCulture, "defaultLat: {0}, defaultLong: {1}, ", MapCenter.Latitude, MapCenter.Longitude));
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "MapType: {0}, ", MapTypeToString(MapType));
            if (BoundingBox == null)
                BoundingBox = new LatLongBox(new LatLong(MapCenter.Latitude, MapCenter.Longitude));

            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "minLat: {0}, ", BoundingBox.LatMin);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "maxLat: {0}, ", BoundingBox.LatMax);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "minLong: {0}, ", BoundingBox.LongMin);
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "maxLong: {0}, ", BoundingBox.LongMax);

            // pass in the variable that points to the path array (defined above).
            if (Path != null && Path.Count() > 0)
                sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "pathArray: {0}, ", PathVarName);

            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "fAutoZoom: {0}", fAutoZoom ? 1 : 0);
            sbInitMap.Append("}));");
            sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "MFBMapsOnPage.push({0});", mapID);

            if (ClickHandler.Length > 0)
                sbInitMap.AppendFormat(CultureInfo.InvariantCulture, "\r\n{0}.addListenerFunction({1});\r\n", mapID, ClickHandler);

            return sbInitMap.ToString();
        }
    }
    #endregion
}