using MyFlightbook.Airports;
using MyFlightbook.Geography;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

/******************************************************
 * 
 * Copyright (c) 2010-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Telemetry
{
    public class KMLWriter : IDisposable
    {
        Stream ms;
        XmlWriter kml;

        #region IDisposable Implementation
        private bool disposed = false; // to detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (ms != null)
                        ms.Dispose();
                    if (kml != null)
                        kml.Dispose();

                    ms = null;
                    kml = null;
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~KMLWriter()
        {
            Dispose(false);
        }
        #endregion

        public KMLWriter(Stream outputStream)
        {
            ms = outputStream;
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
            {
                Encoding = new UTF8Encoding(false),
                ConformanceLevel = ConformanceLevel.Document,
                Indent = true
            };
            kml = XmlWriter.Create(ms, xmlWriterSettings);
        }

        public void BeginKML()
        {
            kml.WriteStartDocument();

            kml.WriteStartElement("kml", "http://www.opengis.net/kml/2.2");
            kml.WriteAttributeString("xmlns", "gx", null, "http://www.google.com/kml/ext/2.2");

            kml.WriteStartElement("Document");

            kml.WriteStartElement("Style");
            kml.WriteAttributeString("id", "redPoly");
            kml.WriteStartElement("LineStyle");
            kml.WriteElementString("color", "7f0000ff");
            kml.WriteElementString("width", "4");
            kml.WriteEndElement(); // line style
            kml.WriteStartElement("PolyStyle");
            kml.WriteElementString("color", "7f0000ff");
            kml.WriteEndElement(); // PolyStyle
            kml.WriteEndElement(); // Style

            kml.WriteElementString("open", "1");
            kml.WriteElementString("visibility", "1");
        }

        public void AddPath(Position[] rgPos, string szName, double AltXForm)
        {
            if (rgPos == null)
                return;

            kml.WriteStartElement("Placemark");
            try
            {
                // We've had instances where szName has invalid cahracters, so ignore any errors that arise
                kml.WriteElementString("name", szName);
            }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }

            kml.WriteElementString("styleUrl", "#redPoly");
            kml.WriteStartElement("gx", "Track", null);

            kml.WriteElementString("extrude", "0"); // don't connect to the ground
            kml.WriteElementString("altitudeMode", rgPos.Length > 0 && rgPos[0].HasAltitude ? "absolute" : "clampToGround");

            StringBuilder sbCoords = new StringBuilder();

            // Altitude, in meters
            foreach (Position p in rgPos)
            {
                if (p.HasTimeStamp)
                    kml.WriteElementString("when", p.Timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
                kml.WriteElementString("gx", "coord", null, String.Format(CultureInfo.InvariantCulture, "{0} {1}{2}", p.LongitudeString, p.LatitudeString, p.HasAltitude ? String.Format(CultureInfo.InvariantCulture, " {0}", p.Altitude * AltXForm) : string.Empty));
            }

            kml.WriteEndElement(); // gx:track
            kml.WriteEndElement(); // <Placemark>
        }

        public void AddRoute(airport[] rgap, string szName)
        {
            if (rgap == null)
                return;

            kml.WriteStartElement("Placemark");
            try
            {
                // We've had instances where szName has invalid cahracters, so ignore any errors that arise
                kml.WriteElementString("name", szName);
            }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            kml.WriteElementString("styleUrl", "#redPoly");
            kml.WriteStartElement("LineString");

            kml.WriteElementString("extrude", "0"); // don't connect to the ground
            kml.WriteElementString("altitudeMode", "clampToGround");

            StringBuilder sbCoords = new StringBuilder();

            // Altitude, in meters
            foreach (airport ap in rgap)
                sbCoords.AppendFormat(CultureInfo.InvariantCulture, "{0},{1}\r\n", LatLong.InvariantString(ap.LatLong.Longitude), LatLong.InvariantString(ap.LatLong.Latitude));

            kml.WriteElementString("coordinates", sbCoords.ToString());

            kml.WriteEndElement(); // LineString
            kml.WriteEndElement(); // <Placemark>
        }

        public void EndKML()
        {
            kml.WriteEndElement(); // Document
            kml.WriteEndDocument(); // <kml>
            kml.Flush();
            kml.Close();
        }
    }

    public class KMLParser : TelemetryParser
    {
        public KMLParser() : base() { }

        #region KMLParsingUtility functions
        /// <summary>
        /// Parses KML and tries to figure out which namespace to use.
        /// </summary>
        protected class KMLElements
        {
            public XElement ele { get; set; }
            public XElement ele22 { get; set; }
            public IEnumerable<XElement> eleCoords { get; set; }
            public IEnumerable eleArray { get; set; }
            public XNamespace ns { get; set; }
            public XNamespace ns22 { get; set; }
            public XNamespace nsAlt { get; set; }
            public XNamespace nsAlt22 { get; set; }
            public XNamespace nsAlt21 { get; set; }
            public StringBuilder sbErr { get; set; }
            public XDocument xmlDoc { get; set; }

            public KMLElements(XDocument xml)
            {
                if (xml == null)
                    throw new ArgumentNullException("xml");

                xmlDoc = xml;
                ele = null;
                ele22 = null;
                eleCoords = null;
                eleArray = null;
                ns = XNamespace.Get("http://www.opengis.net/kml/2.2");
                ns22 = XNamespace.Get("http://www.google.com/kml/ext/2.2");
                nsAlt = XNamespace.Get("http://earth.google.com/kml/2.0");
                nsAlt22 = XNamespace.Get("http://earth.google.com/kml/2.2");
                nsAlt21 = XNamespace.Get("http://earth.google.com/kml/2.1");
                sbErr = new StringBuilder();

                try
                {
                    ele = xml.Descendants(ns + "LineString").First();
                    eleCoords = xml.Descendants(ns + "Placemark").Descendants(ns + "LineString").Descendants(ns + "coordinates");
                    ele = eleCoords.First();
                }
                catch (InvalidOperationException) { }

                if (ele == null)
                {
                    try
                    {
                        ele = xml.Descendants(nsAlt + "LineString").First();
                        eleArray = xml.Descendants(nsAlt + "LineString");
                    }
                    catch (InvalidOperationException) { }
                }

                try
                {
                    ele22 = xml.Descendants(ns + "Placemark").Descendants(ns22 + "Track").First();
                }
                catch (InvalidOperationException)
                {
                    try
                    {
                        ele = xml.Descendants(nsAlt22 + "LineString").First();
                        eleArray = xml.Descendants(nsAlt22 + "LineString");
                    }
                    catch (InvalidOperationException)
                    {
                        try
                        {
                            ele = xml.Descendants(nsAlt21 + "LineString").First();
                            eleArray = xml.Descendants(nsAlt21 + "LineString");
                        }
                        catch (InvalidOperationException) { }
                    }
                }
            }
        }

        private bool ParseKMLv1(KMLElements k)
        {
            bool fResult = true;
            Regex r = new Regex("(-?[0-9.]+) *, *(-?[0-9.]+)(?: *, *(-?[0-9]+))? *", RegexOptions.Compiled);

            string coords;
            if (k.eleArray != null)
            {
                StringBuilder sb = new StringBuilder();
                foreach (XElement e in k.eleArray)
                    sb.Append(e.Value.Replace("\r", " ").Replace("\n", " "));
                coords = sb.ToString();
            }
            else if (k.eleCoords != null)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < k.eleCoords.Count(); i++)
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", (k.eleCoords.ElementAt(i).Value.Replace("\r", " ").Replace("\n", " ")));
                coords = sb.ToString();
            }
            else
                coords = k.ele.Value.Replace("\r", " ").Replace("\n", " ");

            MatchCollection mc = r.Matches(coords);

            if (mc.Count > 0)
            {
                List<Position> lstSamples = new List<Position>();
                ArrayList al = new ArrayList();
                for (int i = 1; i < mc[0].Groups.Count; i++)
                    if (mc[0].Groups[i].Value.Trim().Length > 0)
                        al.Add(mc[0].Groups[i].Value);

                for (int iRow = 0; iRow < mc.Count; iRow++)
                {
                    al.Clear();
                    GroupCollection gc = mc[iRow].Groups;
                    for (int i = 1; i < gc.Count; i++)
                        al.Add(gc[i].Value);
                    try
                    {
                        lstSamples.Add(new Position((string[])al.ToArray(typeof(string))));
                    }
                    catch (MyFlightbookException ex)
                    {
                        fResult = false;
                        k.sbErr.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n", ex.Message);
                    }
                }
                ToDataTable(lstSamples);
            }

            return fResult;
        }

        private bool ParseKMLv2(KMLElements k)
        {
            var timeStamps = k.ele22.Descendants(k.ns + "when");
            var coords = k.ele22.Descendants(k.ns22 + "coord");

            int cTimeStamps = timeStamps.Count();
            int cCoords = coords.Count();

            bool fHasTime = cTimeStamps > 0 && cTimeStamps == cCoords;

            List<Position> lstSamples = new List<Position>();
            for (int iRow = 0; iRow < cCoords; iRow++)
            {
                var coord = coords.ElementAt(iRow);
                string[] rgRow = coord.Value.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                Position sample = new Position(rgRow);

                if (iRow < timeStamps.Count())
                {
                    sample.TypeOfSpeed = Position.SpeedType.Derived;
                    string szTimeStamp = timeStamps.ElementAt(iRow).Value;
                    if (!String.IsNullOrEmpty(szTimeStamp))
                        sample.Timestamp = szTimeStamp.ParseUTCDate();
                }
                lstSamples.Add(sample);
            }

            // Derive speed or see if it is available in extended data.
            bool fHasSpeed = false;

            var extendedData = k.xmlDoc.Descendants(k.ns22 + "SimpleArrayData");
            if (extendedData != null)
            {
                // Go through the items to find speed
                foreach (XElement e in extendedData)
                {
                    var attr = e.Attribute("name");
                    if (attr != null && (String.Compare(attr.Value, "speedKts", StringComparison.OrdinalIgnoreCase) == 0 || String.Compare(attr.Value, "speed_kts", StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        int i = 0;
                        int maxSample = lstSamples.Count;
                        foreach (XElement speedval in e.Descendants())
                        {
                            string szVal = speedval.Value;
                            double sp;
                            if (i < maxSample && double.TryParse(szVal, out sp))
                            {
                                Position p = lstSamples[i++];
                                p.Speed = sp;
                                p.TypeOfSpeed = Position.SpeedType.Reported;
                                fHasSpeed = true;
                            }
                            else
                                break;
                        }
                        break;  // no need to go through any other elements in extended data
                    }
                }
            }

            // derive speed if we didn't find it above
            if (!fHasSpeed)
                Position.DeriveSpeed(lstSamples);

            // And put it into a data table
            ToDataTable(lstSamples);

            return true;
        }
        #endregion

        public override bool CanParse(string szData)
        {
            return szData != null && IsXML(szData) && szData.Contains("<kml");
        }

        public override FlightData.AltitudeUnitTypes AltitudeUnits
        {
            get { return FlightData.AltitudeUnitTypes.Meters; }
        }

        public override FlightData.SpeedUnitTypes SpeedUnits
        {
            get { return FlightData.SpeedUnitTypes.MetersPerSecond; }
        }

        /// <summary>
        /// Parses KML-based flight data
        /// </summary>
        /// <param name="flightData">The string of flight data</param>
        /// <returns>True for success</returns>
        public override bool Parse(string szData)
        {
            DataTable m_dt = ParsedData;
            m_dt.Clear();
            Boolean fResult = true;

            byte[] bytes = Encoding.UTF8.GetBytes(szData);
            KMLElements k = null;
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                try
                {
                    k = new KMLElements(XDocument.Load(new StreamReader(stream)));

                    if (k.ele22 != null)
                        fResult = ParseKMLv2(k);
                    else if (k.ele != null || k.eleArray != null)
                        fResult = ParseKMLv1(k);
                    else
                        throw new MyFlightbookException(Resources.FlightData.errNoKMLTrack);
                }
                catch (System.Xml.XmlException ex)
                {
                    ErrorString = String.Format(CultureInfo.CurrentCulture, Resources.FlightData.errGeneric, ex.Message);
                    fResult = false;
                }
                catch (MyFlightbookException ex)
                {
                    k.sbErr.Append(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.errGeneric, ex.Message));
                    fResult = false;
                }
            }

            ErrorString = (k == null) ? string.Empty : k.sbErr.ToString();

            return fResult;
        }
    }
}