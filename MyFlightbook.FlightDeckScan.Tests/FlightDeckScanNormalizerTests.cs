using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyFlightbook.FlightDeckScan;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

// NOTE: this file is NOT wired into a .csproj yet - it's a straight C# port of the
// Node prototype's normalize.test.js (11 tests, all passing there), validated against
// the same 5 sample screens from https://github.com/ericberman/MyFlightbookWeb/issues/1577.
// It follows the MSTest conventions already used in MyFlightbook.Telemetry.Tests
// (KMLParserTests.cs). Drop it into whichever test project ends up referencing
// MyFlightbook.Web's AppCode classes (or into a new project, if FlightDeckScan* gets
// pulled out into its own class library like MyFlightbook.Telemetry) and it should run
// as-is. This has NOT been compiled - no C# toolchain was available to verify it.
namespace MyFlightbook.FlightDeckScan.Tests
{
    [TestClass]
    public class FlightDeckScanNormalizerTests
    {
        private static readonly DateTime ReferenceDateUtc = new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc);

        [TestMethod]
        public void ParseTime_HandlesHhmmColonSecondsAndBlanks()
        {
            Assert.AreEqual("13:31", FlightDeckScanNormalizer.ParseTime("1331Z").Hhmm);
            Assert.AreEqual("13:31", FlightDeckScanNormalizer.ParseTime("1331").Hhmm);
            Assert.AreEqual("13:31", FlightDeckScanNormalizer.ParseTime("13:31").Hhmm);
            Assert.AreEqual("05:02", FlightDeckScanNormalizer.ParseTime("05:02:40").Hhmm);
            Assert.AreEqual("05:02", FlightDeckScanNormalizer.ParseTime("050240").Hhmm); // bare HHMMSS, no separators
            Assert.IsNull(FlightDeckScanNormalizer.ParseTime(null));
            Assert.IsNull(FlightDeckScanNormalizer.ParseTime("----"));
            Assert.IsNull(FlightDeckScanNormalizer.ParseTime("-----------"));
            Assert.IsTrue(FlightDeckScanNormalizer.ParseTime("99:99").ParseError);
        }

        [TestMethod]
        public void ParseDate_HandlesDdmmmyyDdmmmAndBlanks()
        {
            Assert.AreEqual("2026-08-07", FlightDeckScanNormalizer.ParseDate("07AUG26", ReferenceDateUtc).Iso);
            Assert.AreEqual("2020-07-08", FlightDeckScanNormalizer.ParseDate("08JUL20", ReferenceDateUtc).Iso);
            Assert.IsNull(FlightDeckScanNormalizer.ParseDate(null, ReferenceDateUtc));
            Assert.IsNull(FlightDeckScanNormalizer.ParseDate("----", ReferenceDateUtc));

            // No year printed: should infer the most recent non-future occurrence.
            DateValue inferred = FlightDeckScanNormalizer.ParseDate("09AUG", ReferenceDateUtc);
            Assert.AreEqual("2026-08-09", inferred.Iso); // Aug 9 has already happened by Aug 19 2026
            Assert.IsTrue(inferred.YearInferred);

            DateValue inferredFuture = FlightDeckScanNormalizer.ParseDate("25DEC", ReferenceDateUtc);
            Assert.AreEqual("2025-12-25", inferredFuture.Iso); // Dec 25 hasn't happened yet in 2026 -> last year
        }

        [TestMethod]
        public void Elapsed_WrapsCorrectlyPastMidnight()
        {
            Assert.AreEqual("00:20", FlightDeckScanNormalizer.Elapsed("23:50", "00:10"));
            Assert.AreEqual("01:44", FlightDeckScanNormalizer.Elapsed("13:31", "15:15"));
        }

        [TestMethod]
        public void Sample1_BoeingAocStatusRotatedPhoto_FullCompleteFlight()
        {
            var raw = new RawExtraction
            {
                IsFlightDeckDisplay = true,
                ScreenType = "AOC STATUS",
                FlightNumber = "G66111",
                DateRaw = "07AUG26",
                Origin = "KAEX",
                Destination = "KJAX",
                OutRaw = "1331Z",
                OffRaw = "1344Z",
                OnRaw = "1510Z",
                InRaw = "1515Z",
                BlockRaw = "0144",
                FlightTimeRaw = "0126",
                ExtractionNotes = "Photo is rotated 90 degrees."
            };
            ScanResult r = FlightDeckScanNormalizer.BuildResult(raw, ReferenceDateUtc);

            Assert.IsTrue(r.Success);
            Assert.AreEqual("G66111", r.FlightNumber);
            Assert.AreEqual("2026-08-07", r.Date.Iso);
            Assert.AreEqual("KAEX", r.Origin);
            Assert.AreEqual("KJAX", r.Destination);
            Assert.AreEqual("13:31", r.Times.Out.Hhmm);
            Assert.AreEqual("15:15", r.Times.In.Hhmm);
            Assert.AreEqual("01:44", r.BlockTime.Hhmm);
            Assert.AreEqual("printed", r.BlockTime.Source);
            Assert.AreEqual("01:26", r.FlightTime.Hhmm);
            Assert.AreEqual("complete", r.Stage);
            Assert.IsFalse(r.IsInProgress ?? true);
            Assert.AreEqual(0, r.Warnings.Count);
        }

        [TestMethod]
        public void Sample2_PurpleAcarsFlightLog_BlankFlightNumberAndDestination()
        {
            var raw = new RawExtraction
            {
                IsFlightDeckDisplay = true,
                ScreenType = "ACARS FLIGHT LOG",
                FlightNumber = null,
                DateRaw = null,
                Origin = "LEAL",
                Destination = null,
                OutRaw = "0832",
                OffRaw = "0844",
                OnRaw = "1047",
                InRaw = "1052",
                BlockRaw = "0220",
                FlightTimeRaw = "0203",
                ExtractionNotes = null
            };
            ScanResult r = FlightDeckScanNormalizer.BuildResult(raw, ReferenceDateUtc);

            Assert.IsTrue(r.Success);
            Assert.IsNull(r.FlightNumber);
            Assert.IsNull(r.Destination);
            Assert.AreEqual("LEAL", r.Origin);
            Assert.AreEqual("02:20", r.BlockTime.Hhmm);
            Assert.AreEqual("02:03", r.FlightTime.Hhmm);
            Assert.AreEqual("complete", r.Stage);
            Assert.IsTrue(r.Warnings.Any(w => w.Contains("Flight number")));
            Assert.IsTrue(r.Warnings.Any(w => w.Contains("Destination")));
            Assert.IsTrue(r.Warnings.Any(w => w.Contains("Date")));
        }

        [TestMethod]
        public void Sample3_AcarsFltLogPrev_HhmmssTimesDateWithNoYear()
        {
            var raw = new RawExtraction
            {
                IsFlightDeckDisplay = true,
                ScreenType = "ACARS-FLT LOG-PREV",
                FlightNumber = "AFR66SQ",
                DateRaw = "09AUG",
                Origin = "GMME",
                Destination = "LFPG",
                OutRaw = "05:02:40",
                OffRaw = "05:12:26",
                OnRaw = "07:37:06",
                InRaw = "07:48:13",
                BlockRaw = "02:46",
                FlightTimeRaw = "02:25",
                ExtractionNotes = "Ignored the current-time field shown next to BLOCK/FLIGHT."
            };
            ScanResult r = FlightDeckScanNormalizer.BuildResult(raw, ReferenceDateUtc);

            Assert.IsTrue(r.Success);
            Assert.AreEqual("2026-08-09", r.Date.Iso);
            Assert.IsTrue(r.Date.YearInferred);
            Assert.AreEqual("02:46", r.BlockTime.Hhmm);
            Assert.AreEqual("02:25", r.FlightTime.Hhmm);
            Assert.AreEqual("complete", r.Stage);
            Assert.IsFalse(r.Warnings.Any(w => w.Contains("does not match")));
        }

        [TestMethod]
        public void Sample4_AcarsFltLogCurr_InProgressFlightNoInNoDate()
        {
            var raw = new RawExtraction
            {
                IsFlightDeckDisplay = true,
                ScreenType = "ACARS-FLT LOG-CURR",
                FlightNumber = "792F",
                DateRaw = null,
                Origin = "EHAM",
                Destination = "EBBR",
                OutRaw = "12:28",
                OffRaw = "12:41",
                OnRaw = "13:01",
                InRaw = null,
                BlockRaw = null,
                FlightTimeRaw = "00:20",
                ExtractionNotes = null
            };
            ScanResult r = FlightDeckScanNormalizer.BuildResult(raw, ReferenceDateUtc);

            Assert.IsTrue(r.Success);
            Assert.IsTrue(r.IsInProgress ?? false);
            Assert.AreEqual("landed_taxiing", r.Stage);
            Assert.IsNull(r.Times.In);
            Assert.IsNull(r.BlockTime); // can't compute without IN, and none printed
            Assert.AreEqual("00:20", r.FlightTime.Hhmm);
            Assert.IsTrue(r.Warnings.Any(w => w.Contains("in progress")));
            Assert.IsTrue(r.Warnings.Any(w => w.Contains("Date")));
        }

        [TestMethod]
        public void Sample5_AcarsOoOiStatus_FullCompleteFlight()
        {
            var raw = new RawExtraction
            {
                IsFlightDeckDisplay = true,
                ScreenType = "ACARS OOOI STATUS",
                FlightNumber = "DA04",
                DateRaw = "08JUL20",
                Origin = "LILC",
                Destination = "LIEO",
                OutRaw = "0953Z",
                OffRaw = "1010Z",
                OnRaw = "1205Z",
                InRaw = "1212Z",
                BlockRaw = "0219",
                FlightTimeRaw = "0155",
                ExtractionNotes = null
            };
            ScanResult r = FlightDeckScanNormalizer.BuildResult(raw, ReferenceDateUtc);

            Assert.IsTrue(r.Success);
            Assert.AreEqual("2020-07-08", r.Date.Iso);
            Assert.AreEqual("02:19", r.BlockTime.Hhmm);
            Assert.AreEqual("01:55", r.FlightTime.Hhmm);
            Assert.AreEqual("complete", r.Stage);
            Assert.AreEqual(0, r.Warnings.Count);
        }

        [TestMethod]
        public void NonMcduPhoto_YieldsCleanFailure_NotACrash()
        {
            var raw = new RawExtraction
            {
                IsFlightDeckDisplay = false,
                ExtractionNotes = "This is a photo of a sandwich."
            };
            ScanResult r = FlightDeckScanNormalizer.BuildResult(raw, ReferenceDateUtc);

            Assert.IsFalse(r.Success);
            StringAssert.Contains(r.Error, "does not appear to show");
        }

        [TestMethod]
        public void FlightDeckDisplayButNoReadableData_StillYieldsCleanFailure()
        {
            var raw = new RawExtraction
            {
                IsFlightDeckDisplay = true,
                ScreenType = "unknown",
                ExtractionNotes = "Screen is too glared-out to read anything."
            };
            ScanResult r = FlightDeckScanNormalizer.BuildResult(raw, ReferenceDateUtc);

            Assert.IsFalse(r.Success);
            StringAssert.Contains(r.Error, "No flight number, date, route, or OOOI times");
        }

        [TestMethod]
        public void PrintedBlockTimeDisagreeingWithOutIn_IsKeptButFlagged()
        {
            var raw = new RawExtraction
            {
                IsFlightDeckDisplay = true,
                ScreenType = "test",
                FlightNumber = "TST1",
                DateRaw = "01JAN26",
                Origin = "KXXX",
                Destination = "KYYY",
                OutRaw = "10:00",
                OffRaw = "10:10",
                OnRaw = "11:00",
                InRaw = "11:10",
                BlockRaw = "05:00", // wildly wrong on purpose
                FlightTimeRaw = "00:50",
                ExtractionNotes = null
            };
            ScanResult r = FlightDeckScanNormalizer.BuildResult(raw, ReferenceDateUtc);

            Assert.AreEqual("05:00", r.BlockTime.Hhmm); // printed value still wins
            Assert.AreEqual("printed", r.BlockTime.Source);
            Assert.IsTrue(r.Warnings.Any(w => w.Contains("does not match")));
        }
    }
}
