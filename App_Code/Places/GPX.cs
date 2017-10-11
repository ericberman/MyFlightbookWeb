using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MyFlightbook.Geography;

/******************************************************
 * 
 * Copyright (c) 2010-2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Telemetry
{
    public class GPXParser : TelemetryParser
    {
        public GPXParser() : base() { }

        public override bool CanParse(string szData)
        {
            return szData != null && IsXML(szData) && szData.Contains("<gpx");
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
        /// Parses GPX-based flight data
        /// </summary>
        /// <param name="szData">The string of flight data</param>
        /// <returns>True for success</returns>
        public override bool Parse(string szData)
        {
            StringBuilder sbErr = new StringBuilder();
            ParsedData.Clear();
            Boolean fResult = true;

            byte[] bytes = Encoding.UTF8.GetBytes(szData);
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                XDocument xml = XDocument.Load(new StreamReader(stream));

                try
                {
                    XNamespace ns = XNamespace.Get("http://www.topografix.com/GPX/1/0");
                    XNamespace ns11 = XNamespace.Get("http://www.topografix.com/GPX/1/1");

                    IEnumerable<XElement> ele = null;

                    try
                    {
                        ele = xml.Descendants(ns + "trk").First().Descendants(ns + "trkseg");
                    }
                    catch (InvalidOperationException)
                    {
                        try
                        {
                            ele = xml.Descendants("trk").First().Descendants("trkseg");
                            if (ele != null)
                                ns = "";
                        }
                        catch (InvalidOperationException)
                        {
                            try
                            {
                                ele = xml.Descendants(ns11 + "trk");
                                ele = ele.First().Descendants(ns11 + "trkseg");
                                if (ele != null)
                                    ns = ns11;
                            }
                            catch (InvalidOperationException)
                            {
                            }
                        }
                    }

                    if (ele != null)
                    {
                        int iRow = 0;
                        bool fHasAlt = false;
                        bool fHasDate = false;
                        bool fHasLatLon = false;
                        bool fHasSpeed = false;

                        List<Position> lst = new List<Position>();

                        foreach (XElement e in ele)
                        {
                            foreach (XElement coord in e.Descendants(ns + "trkpt"))
                            {
                                XAttribute xLat = null;
                                XAttribute xLon = null;
                                XElement xAlt = null;
                                XElement xTime = null;
                                XElement xSpeed = null;

                                xLat = coord.Attribute("lat");
                                xLon = coord.Attribute("lon");
                                xAlt = coord.Descendants(ns + "ele").FirstOrDefault();
                                xTime = coord.Descendants(ns + "time").FirstOrDefault();
                                xSpeed = coord.Descendants(ns + "speed").FirstOrDefault();

                                fHasAlt = (xAlt != null);
                                fHasDate = (xTime != null);
                                fHasSpeed = (xSpeed != null);
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
                                    catch (System.FormatException)
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
                        Position.DeriveSpeed(lst);
                        ToDataTable(lst);
                    }
                    else
                        throw new MyFlightbookException(Resources.FlightData.errGPXNoPath);
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