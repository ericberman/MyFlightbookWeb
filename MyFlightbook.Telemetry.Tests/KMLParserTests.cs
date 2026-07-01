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

        [TestMethod]
        public void Parse_GxTrackAcrossMultiplePlacemarks_ParsesAllTracks()
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
                  <Placemark>
                    <gx:Track>
                      <when>2025-03-14T19:11:26Z</when>
                      <when>2025-03-14T19:11:42Z</when>
                      <gx:coord>-122.3510 47.6215 420</gx:coord>
                      <gx:coord>-122.3520 47.6220 430</gx:coord>
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
            Assert.AreEqual(4, parser.ParsedData.Rows.Count);
            Assert.AreEqual(DateTime.Parse("2025-03-14T19:11:42Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                (DateTime)parser.ParsedData.Rows[3][KnownColumnNames.TIME]);
            Assert.AreEqual(47.6220, Convert.ToDouble(parser.ParsedData.Rows[3][KnownColumnNames.LAT], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(-122.3520, Convert.ToDouble(parser.ParsedData.Rows[3][KnownColumnNames.LON], CultureInfo.InvariantCulture), 0.0001);
        }

        [TestMethod]
        public void Parse_GxTracksOutOfChronologicalOrder_SortsByStartTime()
        {
            const string kml = """
                <?xml version="1.0" encoding="utf-8"?>
                <kml xmlns="http://www.opengis.net/kml/2.2" xmlns:gx="http://www.google.com/kml/ext/2.2">
                  <Placemark>
                    <gx:Track>
                      <when>2025-03-14T19:20:00Z</when>
                      <when>2025-03-14T19:20:16Z</when>
                      <gx:coord>-122.1000 47.1000 100</gx:coord>
                      <gx:coord>-122.1100 47.1100 110</gx:coord>
                    </gx:Track>
                  </Placemark>
                  <Placemark>
                    <gx:Track>
                      <when>2025-03-14T19:00:00Z</when>
                      <when>2025-03-14T19:00:16Z</when>
                      <gx:coord>-122.3000 47.3000 300</gx:coord>
                      <gx:coord>-122.3100 47.3100 310</gx:coord>
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
            Assert.AreEqual(4, parser.ParsedData.Rows.Count);
            Assert.AreEqual(DateTime.Parse("2025-03-14T19:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                (DateTime)parser.ParsedData.Rows[0][KnownColumnNames.TIME]);
            Assert.AreEqual(47.3000, Convert.ToDouble(parser.ParsedData.Rows[0][KnownColumnNames.LAT], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(DateTime.Parse("2025-03-14T19:20:16Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                (DateTime)parser.ParsedData.Rows[3][KnownColumnNames.TIME]);
            Assert.AreEqual(47.1100, Convert.ToDouble(parser.ParsedData.Rows[3][KnownColumnNames.LAT], CultureInfo.InvariantCulture), 0.0001);
            Assert.IsNotNull(parser.ParsedData.Columns[KnownColumnNames.DERIVEDSPEED]);
        }

        [TestMethod]
        public void Parse_GxTrackTimestampedThenNaked_DropsNakedTrack()
        {
            const string kml = """
                <?xml version="1.0" encoding="utf-8"?>
                <kml xmlns="http://www.opengis.net/kml/2.2" xmlns:gx="http://www.google.com/kml/ext/2.2">
                  <Placemark>
                    <gx:Track>
                      <when>2025-03-14T19:00:00Z</when>
                      <when>2025-03-14T19:00:16Z</when>
                      <gx:coord>-122.1000 47.1000 100</gx:coord>
                      <gx:coord>-122.1100 47.1100 110</gx:coord>
                    </gx:Track>
                  </Placemark>
                  <Placemark>
                    <gx:Track>
                      <gx:coord>-122.9000 47.9000 900</gx:coord>
                      <gx:coord>-122.9100 47.9100 910</gx:coord>
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
            Assert.IsNotNull(parser.ParsedData.Columns[KnownColumnNames.TIME]);
            Assert.AreEqual(47.1000, Convert.ToDouble(parser.ParsedData.Rows[0][KnownColumnNames.LAT], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(47.1100, Convert.ToDouble(parser.ParsedData.Rows[1][KnownColumnNames.LAT], CultureInfo.InvariantCulture), 0.0001);
        }

        [TestMethod]
        public void Parse_GxTracksAllNaked_MergesInDocumentOrder()
        {
            const string kml = """
                <?xml version="1.0" encoding="utf-8"?>
                <kml xmlns="http://www.opengis.net/kml/2.2" xmlns:gx="http://www.google.com/kml/ext/2.2">
                  <Placemark>
                    <gx:Track>
                      <gx:coord>-122.1000 47.1000 100</gx:coord>
                      <gx:coord>-122.1100 47.1100 110</gx:coord>
                    </gx:Track>
                  </Placemark>
                  <Placemark>
                    <gx:Track>
                      <gx:coord>-122.2000 47.2000 200</gx:coord>
                      <gx:coord>-122.2100 47.2100 210</gx:coord>
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
            Assert.AreEqual(4, parser.ParsedData.Rows.Count);
            Assert.IsNull(parser.ParsedData.Columns[KnownColumnNames.TIME]);
            Assert.AreEqual(47.1000, Convert.ToDouble(parser.ParsedData.Rows[0][KnownColumnNames.LAT], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(47.2100, Convert.ToDouble(parser.ParsedData.Rows[3][KnownColumnNames.LAT], CultureInfo.InvariantCulture), 0.0001);
        }

        [TestMethod]
        public void Parse_GxTrackSinglePointPlacemark_IsIgnored()
        {
            const string kml = """
                <?xml version="1.0" encoding="utf-8"?>
                <kml xmlns="http://www.opengis.net/kml/2.2" xmlns:gx="http://www.google.com/kml/ext/2.2">
                  <Placemark>
                    <gx:Track>
                      <when>2025-03-14T19:00:00Z</when>
                      <when>2025-03-14T19:00:16Z</when>
                      <gx:coord>-122.1000 47.1000 100</gx:coord>
                      <gx:coord>-122.1100 47.1100 110</gx:coord>
                    </gx:Track>
                  </Placemark>
                  <Placemark>
                    <gx:Track>
                      <when>2025-03-14T19:05:00Z</when>
                      <gx:coord>-122.5000 47.5000 500</gx:coord>
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
            Assert.AreEqual(47.1000, Convert.ToDouble(parser.ParsedData.Rows[0][KnownColumnNames.LAT], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(47.1100, Convert.ToDouble(parser.ParsedData.Rows[1][KnownColumnNames.LAT], CultureInfo.InvariantCulture), 0.0001);
        }

        [TestMethod]
        public void Parse_GxTracksWithReportedSpeed_StayAlignedAfterSort()
        {
            const string kml = """
                <?xml version="1.0" encoding="utf-8"?>
                <kml xmlns="http://www.opengis.net/kml/2.2" xmlns:gx="http://www.google.com/kml/ext/2.2">
                  <Placemark>
                    <gx:Track>
                      <when>2025-03-14T19:20:00Z</when>
                      <when>2025-03-14T19:20:16Z</when>
                      <gx:coord>-122.1000 47.1000 100</gx:coord>
                      <gx:coord>-122.1100 47.1100 110</gx:coord>
                      <ExtendedData>
                        <SchemaData>
                          <gx:SimpleArrayData name="speed_kts">
                            <gx:value>100</gx:value>
                            <gx:value>110</gx:value>
                          </gx:SimpleArrayData>
                        </SchemaData>
                      </ExtendedData>
                    </gx:Track>
                  </Placemark>
                  <Placemark>
                    <gx:Track>
                      <when>2025-03-14T19:00:00Z</when>
                      <when>2025-03-14T19:00:16Z</when>
                      <gx:coord>-122.3000 47.3000 300</gx:coord>
                      <gx:coord>-122.3100 47.3100 310</gx:coord>
                      <ExtendedData>
                        <SchemaData>
                          <gx:SimpleArrayData name="speed_kts">
                            <gx:value>50</gx:value>
                            <gx:value>60</gx:value>
                          </gx:SimpleArrayData>
                        </SchemaData>
                      </ExtendedData>
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
            Assert.AreEqual(4, parser.ParsedData.Rows.Count);
            Assert.IsNotNull(parser.ParsedData.Columns[KnownColumnNames.SPEED]);
            Assert.AreEqual(50, Convert.ToDouble(parser.ParsedData.Rows[0][KnownColumnNames.SPEED], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(60, Convert.ToDouble(parser.ParsedData.Rows[1][KnownColumnNames.SPEED], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(100, Convert.ToDouble(parser.ParsedData.Rows[2][KnownColumnNames.SPEED], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(110, Convert.ToDouble(parser.ParsedData.Rows[3][KnownColumnNames.SPEED], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(DateTime.Parse("2025-03-14T19:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                (DateTime)parser.ParsedData.Rows[0][KnownColumnNames.TIME]);
            Assert.AreEqual(DateTime.Parse("2025-03-14T19:20:16Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                (DateTime)parser.ParsedData.Rows[3][KnownColumnNames.TIME]);
        }

        [TestMethod]
        public void Parse_GxMultiTrack_ParsesAllSegments()
        {
            const string kml = """
                <?xml version="1.0" encoding="utf-8"?>
                <kml xmlns="http://www.opengis.net/kml/2.2" xmlns:gx="http://www.google.com/kml/ext/2.2">
                  <Placemark>
                    <gx:MultiTrack>
                      <gx:Track>
                        <when>2025-03-14T19:00:00Z</when>
                        <when>2025-03-14T19:00:16Z</when>
                        <gx:coord>-122.1000 47.1000 100</gx:coord>
                        <gx:coord>-122.1100 47.1100 110</gx:coord>
                      </gx:Track>
                      <gx:Track>
                        <when>2025-03-14T19:10:00Z</when>
                        <when>2025-03-14T19:10:16Z</when>
                        <gx:coord>-122.2000 47.2000 200</gx:coord>
                        <gx:coord>-122.2100 47.2100 210</gx:coord>
                      </gx:Track>
                    </gx:MultiTrack>
                  </Placemark>
                </kml>
                """;

            KMLParser parser = new KMLParser
            {
                ParsedData = new TelemetryDataTable()
            };

            bool parsed = parser.Parse(kml);

            Assert.IsTrue(parsed, parser.ErrorString);
            Assert.AreEqual(4, parser.ParsedData.Rows.Count);
            Assert.AreEqual(DateTime.Parse("2025-03-14T19:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                (DateTime)parser.ParsedData.Rows[0][KnownColumnNames.TIME]);
            Assert.AreEqual(DateTime.Parse("2025-03-14T19:10:16Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                (DateTime)parser.ParsedData.Rows[3][KnownColumnNames.TIME]);
        }

        [TestMethod]
        public void Parse_GxTracksMixedReportedSpeed_DropsSpeedlessTrack()
        {
            const string kml = """
                <?xml version="1.0" encoding="utf-8"?>
                <kml xmlns="http://www.opengis.net/kml/2.2" xmlns:gx="http://www.google.com/kml/ext/2.2">
                  <Placemark>
                    <gx:Track>
                      <when>2025-03-14T19:00:00Z</when>
                      <when>2025-03-14T19:00:16Z</when>
                      <gx:coord>-122.1000 47.1000 100</gx:coord>
                      <gx:coord>-122.1100 47.1100 110</gx:coord>
                      <ExtendedData>
                        <SchemaData>
                          <gx:SimpleArrayData name="speed_kts">
                            <gx:value>50</gx:value>
                            <gx:value>60</gx:value>
                          </gx:SimpleArrayData>
                        </SchemaData>
                      </ExtendedData>
                    </gx:Track>
                  </Placemark>
                  <Placemark>
                    <gx:Track>
                      <when>2025-03-14T19:10:00Z</when>
                      <when>2025-03-14T19:10:16Z</when>
                      <gx:coord>-122.2000 47.2000 200</gx:coord>
                      <gx:coord>-122.2100 47.2100 210</gx:coord>
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
            Assert.IsNotNull(parser.ParsedData.Columns[KnownColumnNames.SPEED]);
            Assert.AreEqual(47.1000, Convert.ToDouble(parser.ParsedData.Rows[0][KnownColumnNames.LAT], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(50, Convert.ToDouble(parser.ParsedData.Rows[0][KnownColumnNames.SPEED], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(60, Convert.ToDouble(parser.ParsedData.Rows[1][KnownColumnNames.SPEED], CultureInfo.InvariantCulture), 0.0001);
        }

        [TestMethod]
        public void Parse_GxTrackWithFewerTimestampsThanCoordinates_IsDropped()
        {
            const string kml = """
                <?xml version="1.0" encoding="utf-8"?>
                <kml xmlns="http://www.opengis.net/kml/2.2" xmlns:gx="http://www.google.com/kml/ext/2.2">
                  <Placemark>
                    <gx:Track>
                      <when>2025-03-14T19:00:00Z</when>
                      <when>2025-03-14T19:00:16Z</when>
                      <gx:coord>-122.1000 47.1000 100</gx:coord>
                      <gx:coord>-122.1100 47.1100 110</gx:coord>
                    </gx:Track>
                  </Placemark>
                  <Placemark>
                    <gx:Track>
                      <when>2025-03-14T19:05:00Z</when>
                      <when>2025-03-14T19:05:16Z</when>
                      <gx:coord>-122.5000 47.5000 500</gx:coord>
                      <gx:coord>-122.5100 47.5100 510</gx:coord>
                      <gx:coord>-122.5200 47.5200 520</gx:coord>
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
            Assert.AreEqual(47.1000, Convert.ToDouble(parser.ParsedData.Rows[0][KnownColumnNames.LAT], CultureInfo.InvariantCulture), 0.0001);
            Assert.AreEqual(47.1100, Convert.ToDouble(parser.ParsedData.Rows[1][KnownColumnNames.LAT], CultureInfo.InvariantCulture), 0.0001);
        }
    }
}
