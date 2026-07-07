using MyFlightbook.Geography;
using MyFlightbook.Telemetry.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

/******************************************************
 * 
 * Copyright (c) 2010-2026 MyFlightbook LLC
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

        public override AltitudeUnitTypes AltitudeUnits
        {
            get { return AltitudeUnitTypes.Meters; }
        }

        public override SpeedUnitTypes SpeedUnits
        {
            get { return SpeedUnitTypes.MetersPerSecond; }
        }

        /// <summary>
        /// Finds descendants matching a local name, ignoring namespace entirely.
        /// GPX files in the wild are frequently inconsistent about namespacing
        /// (e.g., some elements prefixed, some not, prefixes bound to different
        /// GPX schema versions within the same document), so matching purely on
        /// local name is far more robust than trying to track "the" namespace.
        /// </summary>
        private static IEnumerable<XElement> DescendantsByLocalName(XContainer container, string localName)
        {
            return container.Descendants().Where(e => e.Name.LocalName == localName);
        }

        private static IEnumerable<XElement> FindRoot(XDocument xml)
        {
            if (xml == null)
                return null;

            XElement trk = DescendantsByLocalName(xml, "trk").FirstOrDefault();

            return trk == null ? null : DescendantsByLocalName(trk, "trkseg");
        }

        private static XElement SpeedElement(XElement coord)
        {
            XElement xSpeed = DescendantsByLocalName(coord, "speed").FirstOrDefault();
            if (xSpeed != null)
                return xSpeed;

            XElement xExtensions = DescendantsByLocalName(coord, "extensions").FirstOrDefault();

            return xExtensions == null ? null : DescendantsByLocalName(xExtensions, "speed").FirstOrDefault();
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

                        IEnumerable<XElement> trkSegs = FindRoot(xml);

                        if (trkSegs == null)
                            throw new MyFlightbookException(TelemetryResources.errGPXNoPath);

                        {
                            int iRow = 0;
                            bool fHasAlt = false;
                            bool fHasDate = false;
                            bool fHasLatLon = false;
                            bool fHasSpeed = false;

                            List<Position> lst = new List<Position>();

                            foreach (XElement e in trkSegs)
                            {
                                foreach (XElement coord in DescendantsByLocalName(e, "trkpt"))
                                {
                                    XAttribute xLat = null;
                                    XAttribute xLon = null;
                                    XElement xAlt = null;
                                    XElement xTime = null;
                                    XElement xSpeed = null;

                                    xLat = coord.Attribute("lat");
                                    xLon = coord.Attribute("lon");
                                    xAlt = DescendantsByLocalName(coord, "ele").FirstOrDefault();
                                    xTime = DescendantsByLocalName(coord, "time").FirstOrDefault();
                                    xSpeed = SpeedElement(coord);

                                    fHasAlt = (xAlt != null);
                                    fHasDate = (xTime != null);
                                    fHasSpeed = (xSpeed != null);
                                    fHasLatLon = (xLat != null && xLon != null);

                                    if (!fHasAlt && !fHasDate && !fHasSpeed && !fHasLatLon)
                                        throw new MyFlightbookException(TelemetryResources.errGPXNoPath);

                                    if (fHasLatLon)
                                    {
                                        try
                                        {
                                            Position samp = new Position(Convert.ToDouble(xLat.Value, CultureInfo.InvariantCulture), Convert.ToDouble(xLon.Value, CultureInfo.InvariantCulture));
                                            if (fHasAlt)
                                                samp.Altitude = (Int32)Convert.ToDouble(xAlt.Value, CultureInfo.InvariantCulture);
                                            if (fHasDate)
                                                samp.Timestamp = xTime.Value.ParseUTCDate();
                                            if (fHasSpeed)
                                                samp.Speed = Convert.ToDouble(xSpeed.Value, CultureInfo.InvariantCulture);
                                            lst.Add(samp);
                                        }
                                        catch (Exception ex) when (ex is FormatException)
                                        {
                                            fResult = false;
                                            sbErr.AppendFormat(CultureInfo.CurrentCulture, TelemetryResources.errGPXBadRow, iRow);
                                            sbErr.Append("\r\n");
                                        }
                                        catch (Exception ex)
                                        {
                                            util.NotifyAdminException("Unknown error in ParseFlightdataGPX", ex);
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
                    }
                }
                catch (System.Xml.XmlException ex)
                {
                    sbErr.Append(TelemetryResources.errGeneric + ex.Message);
                    fResult = false;
                }
                catch (MyFlightbookException ex)
                {
                    sbErr.Append(TelemetryResources.errGeneric + ex.Message);
                    fResult = false;
                }
            }

            ErrorString = sbErr.ToString();

            return fResult;
        }
    }
}