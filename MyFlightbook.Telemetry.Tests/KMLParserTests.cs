using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyFlightbook.Charting;
using MyFlightbook.Telemetry;

namespace MyFlightbook.Telemetry.Tests
{
    [TestClass]
    public class KMLParserTests
    {
        [TestMethod]
        public void Parse_LineStringCoordinatesAcrossMultiplePlacemarks_ParsesAllSamples()
        {
            const string kml = """
                <?xml version="1.0" encoding="utf-8"?>
                <kml xmlns="http://www.opengis.net/kml/2.2">
                  <Placemark>
                    <LineString>
                      <coordinates>-122.1000,47.1000,100 -122.2000,47.2000,200</coordinates>
                    </LineString>
                  </Placemark>
                  <Placemark>
                    <LineString>
                      <coordinates>-122.3000,47.3000,300</coordinates>
                    </LineString>
                  </Placemark>
                </kml>
                """;

            KMLParser parser = new KMLParser
            {
                ParsedData = new TelemetryDataTable()
            };

            bool parsed = parser.Parse(kml);

            Assert.IsTrue(parsed, parser.ErrorString);
            Assert.AreEqual(3, parser.ParsedData.Rows.Count);
            Assert.AreEqual(47.1, Convert.ToDouble(parser.ParsedData.Rows[0][KnownColumnNames.LAT], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(-122.3, Convert.ToDouble(parser.ParsedData.Rows[2][KnownColumnNames.LON], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(300, Convert.ToInt32(parser.ParsedData.Rows[2][KnownColumnNames.ALT], CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public void Parse_GxTrack_ParsesCoordinatesAndTimestampsWithoutRepeatedEnumeration()
        {
            const string kml = """
                <?xml version="1.0" encoding="utf-8"?>
                <kml xmlns="http://www.opengis.net/kml/2.2" xmlns:gx="http://www.google.com/kml/ext/2.2">
                  <Placemark>
                    <gx:Track>
                      <when>2025-03-14T19:09:26Z</when>
                      <when>2025-03-14T19:10:26Z</when>
                      <gx:coord>-122.3493 47.6205 376.2756</gx:coord>
                      <gx:coord>-122.3501 47.6210 400</gx:coord>
                    </gx:Track>
                  </Placemark>
                </kml>
                """;

            KMLParser parser = new KMLParser
            {
                ParsedData = new TelemetryDataTable()
            };

            bool parsed = parser.Parse(kml);

            Assert.IsTrue(parsed, parser.ErrorString);
            Assert.AreEqual(2, parser.ParsedData.Rows.Count);
            Assert.AreEqual(DateTime.Parse("2025-03-14T19:09:26Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                (DateTime)parser.ParsedData.Rows[0][KnownColumnNames.TIME]);
            Assert.AreEqual(DateTime.Parse("2025-03-14T19:10:26Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                (DateTime)parser.ParsedData.Rows[1][KnownColumnNames.TIME]);
            Assert.AreEqual(47.621, Convert.ToDouble(parser.ParsedData.Rows[1][KnownColumnNames.LAT], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(-122.3501, Convert.ToDouble(parser.ParsedData.Rows[1][KnownColumnNames.LON], CultureInfo.InvariantCulture), 0.0001);
        }
    }
}
