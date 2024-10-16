using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MyFlightbook.Geography;

/******************************************************
 * 
 * Copyright (c) 2010-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Telemetry
{
    public class GPXParser : TelemetryParser
    {
        private static readonly LazyRegex rGPX = new LazyRegex("^<gpx [^>]*xmlns[:=]", RegexOptions.Multiline);

        public GPXParser() : base() { }

        public override bool CanParse(string szData)
        {
            if (szData == null)
                return false;
            return rGPX.IsMatch(szData) || (IsXML(szData) && szData.Contains("<gpx"));
        }

        public override FlightData.AltitudeUnitTypes AltitudeUnits
        {
            get { return FlightData.AltitudeUnitTypes.Meters; }
        }

        public override FlightData.SpeedUnitTypes SpeedUnits
        {
            get { return FlightData.SpeedUnitTypes.MetersPerSecond; }
        }

        private class GPXPathRoot
        {
            public XNamespace xnamespace { get; set;}
            public IEnumerable<XElement> elements { get; set; }
        }

        private static GPXPathRoot FindRoot(XDocument xml)
        {
            if (xml == null)
                return null;

            XNamespace ns = XNamespace.Get("http://www.topografix.com/GPX/1/0");

            IEnumerable<XElement> ele = null;

            try
            {
                ele = xml.Descendants(ns + "trk").First().Descendants(ns + "trkseg");
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                try
                {
                    ele = xml.Descendants("trk").First().Descendants("trkseg");
                    if (ele != null)
                        ns = "";
                }
                catch (Exception ex2) when (ex2 is InvalidOperationException)
                {
                    try
                    {
                        // XNamespace ns11 = XNamespace.Get("http://www.topografix.com/GPX/1/1");
                        XNamespace ns11 = xml.Document.Root.Name.Namespace.NamespaceName;
                        ele = xml.Descendants(ns11 + "trk");
                        ele = ele.First().Descendants(ns11 + "trkseg");
                        if (ele != null)
                            ns = ns11;
                    }
                    catch (Exception ex3) when (!(ex3 is OutOfMemoryException))
                    {
                    }
                }
            }

            return new GPXPathRoot { xnamespace = ns, elements = ele };
        }

        private XNamespace _badElfExtensionNamespace;
        private XNamespace BadElfExtensionNamespace
        {
            get
            {
                if (_badElfExtensionNamespace == null)
                    _badElfExtensionNamespace = "http://bad-elf.com/xmlschemas/GpxExtensionsV1";
                return _badElfExtensionNamespace;
            }
        }

        private XElement SpeedElement(XElement coord, GPXPathRoot root)
        {
            XElement xSpeed = coord.Descendants(root.xnamespace + "speed").FirstOrDefault();
            if (xSpeed != null)
                return xSpeed;

            XElement xSpeedAlternative = coord.Descendants(root.xnamespace + "extensions").FirstOrDefault();

            if (xSpeedAlternative == null)
                return null;

            return xSpeedAlternative.Descendants(BadElfExtensionNamespace + "speed").FirstOrDefault();
        }

        /// <summary>
        /// Parses GPX-based flight data
        /// </summary>
        /// <param name="szData">The string of flight data</param>
        /// <returns>True for success</returns>
        public override bool Parse(string szData)
        {
            if (String.IsNullOrEmpty(szData))
                return false;

            StringBuilder sbErr = new StringBuilder();
            ParsedData.Clear();
            Boolean fResult = true;

            byte[] bytes = Encoding.UTF8.GetBytes(szData);
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        XDocument xml = XDocument.Load(sr);

                        GPXPathRoot root = FindRoot(xml);

                        if (root == null)
                            return false;

                        if (root.elements != null)
                        {
                            int iRow = 0;
                            bool fHasAlt = false;
                            bool fHasDate = false;
                            bool fHasLatLon = false;
                            bool fHasSpeed = false;

                            List<Position> lst = new List<Position>();

                            foreach (XElement e in root.elements)
                            {
                                foreach (XElement coord in e.Descendants(root.xnamespace + "trkpt"))
                                {
                                    XAttribute xLat = null;
                                    XAttribute xLon = null;
                                    XElement xAlt = null;
                                    XElement xTime = null;
                                    XElement xSpeed = null;
                                    XElement xBadElfSpeed = null;

                                    xLat = coord.Attribute("lat");
                                    xLon = coord.Attribute("lon");
                                    xAlt = coord.Descendants(root.xnamespace + "ele").FirstOrDefault();
                                    xTime = coord.Descendants(root.xnamespace + "time").FirstOrDefault();
                                    xSpeed = SpeedElement(coord, root);

                                    fHasAlt = (xAlt != null);
                                    fHasDate = (xTime != null);
                                    fHasSpeed = (xSpeed != null || xBadElfSpeed != null);
                                    fHasLatLon = (xLat != null && xLon != null);

                                    if (!fHasAlt && !fHasDate && !fHasSpeed && !fHasLatLon)
                                        throw new MyFlightbookException(Resources.FlightData.errGPXNoPath);

                                    if (fHasLatLon)
                                    {
                                        try
                                        {
                                            Position samp = new Position(Convert.ToDouble(xLat.Value, System.Globalization.CultureInfo.InvariantCulture), Convert.ToDouble(xLon.Value, System.Globalization.CultureInfo.InvariantCulture));
                                            if (fHasAlt)
                                                samp.Altitude = (Int32)Convert.ToDouble(xAlt.Value, System.Globalization.CultureInfo.InvariantCulture);
                                            if (fHasDate)
                                                samp.Timestamp = xTime.Value.ParseUTCDate();
                                            if (fHasSpeed)
                                                samp.Speed = Convert.ToDouble(xSpeed.Value, System.Globalization.CultureInfo.InvariantCulture);
                                            lst.Add(samp);
                                        }
                                        catch (Exception ex) when (ex is FormatException)
                                        {
                                            fResult = false;
                                            sbErr.AppendFormat(CultureInfo.CurrentCulture, Resources.FlightData.errGPXBadRow, iRow);
                                            sbErr.Append("\r\n");
                                        }
                                        catch (Exception ex)
                                        {
                                            MyFlightbookException.NotifyAdminException(new MyFlightbookException("Unknown error in ParseFlightDataGPX", ex));
                                            throw;
                                        }
                                    }
                                    iRow++;
                                }
                            }

                            // Derive speed and put it into a data table
                            if (!fHasSpeed)
                                Position.DeriveSpeed(lst);
                            ToDataTable(lst);
                        }
                        else
                            throw new MyFlightbookException(Resources.FlightData.errGPXNoPath);
                    }
                }
                catch (System.Xml.XmlException ex)
                {
                    sbErr.Append(Resources.FlightData.errGeneric + ex.Message);
                    fResult = false;
                }
                catch (MyFlightbookException ex)
                {
                    sbErr.Append(Resources.FlightData.errGeneric + ex.Message);
                    fResult = false;
                }
            }

            ErrorString = sbErr.ToString();

            return fResult;
        }
    }
}