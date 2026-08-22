using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightDeckScan
{
    /// <summary>
    /// The results of a scan, no cruft, for encapsulating the ultimate output of a scan in a single object. This is the interface that the rest of the app should use, rather than ScanResult, which is more of a "raw" JSON shape.
    /// </summary>
    public class ScannedFlight
    {
        public string FlightNumber { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? Out { get; set; }
        public DateTime? Off { get; set; }
        public DateTime? On { get; set; }
        public DateTime? In { get; set; }
        public decimal? Block { get; set; }
    }

    /// <summary>
    /// Shared JSON settings for everything this project serializes (ScanResult,
    /// FlightDeckScanDebugResult) - camelCase to match what a JS/Kotlin/Swift
    /// client expects, and nulls included so callers can see "we asked and got
    /// nothing" rather than a missing key.
    /// </summary>
    internal static class FlightDeckScanJson
    {
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Include
        };
    }

    /// <summary>
    /// Exactly what we ask the vision model to read off the screen, with zero
    /// interpretation - see FlightDeckScanSchema for the tool definition sent
    /// to the model and FlightDeckScanPrompt for the accompanying instructions.
    /// All string fields are null if the corresponding value isn't printed on
    /// screen (blank, dashed-out, or simply absent) - the model is instructed
    /// never to guess or fill one in.
    /// </summary>
    public class RawExtraction
    {
        public bool IsFlightDeckDisplay { get; set; }
        public string ScreenType { get; set; }
        public string FlightNumber { get; set; }
        public string DateRaw { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string OutRaw { get; set; }
        public string OffRaw { get; set; }
        public string OnRaw { get; set; }
        public string InRaw { get; set; }
        public string BlockRaw { get; set; }
        public string FlightTimeRaw { get; set; }
        public string ExtractionNotes { get; set; }
    }

    /// <summary>
    /// A normalized OOOI time. Hhmm is null if Raw was present but couldn't be parsed
    /// (see ParseError) - callers should treat that as "couldn't read this", not "not present".
    /// </summary>
    public class TimeValue
    {
        public string Raw { get; set; }
        public string Hhmm { get; set; }
        public bool HasSeconds { get; set; }
        public bool ParseError { get; set; }

        public DateTime? ToDateTime(DateTime? date = null)
        {
            if (string.IsNullOrEmpty(Hhmm) || ParseError)
                return null;
            if (!DateTime.TryParseExact(Hhmm, "HH:mm", null, System.Globalization.DateTimeStyles.None, out var time))
                return null;
            return (date.HasValue) ? new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, time.Hour, time.Minute, 0, date.Value.Kind)
                : time;
        }
    }

    /// <summary>
    /// A normalized date. Iso is null if DateRaw was absent or unparseable.
    /// YearInferred is true when no year was printed and we picked the most
    /// recent non-future occurrence of that month/day.
    /// </summary>
    public class DateValue
    {
        public string Raw { get; set; }
        public string Iso { get; set; }
        public bool YearInferred { get; set; }
        public bool ParseError { get; set; }
    }

    /// <summary>
    /// BLOCK or FLIGHT time, either taken verbatim from the screen ("printed")
    /// or derived from OUT/IN or OFF/ON when not printed ("computed_from_out_in" /
    /// "computed_from_off_on").
    /// </summary>
    public class DurationValue
    {
        public string Hhmm { get; set; }
        public string Source { get; set; }

        /// <summary>
        /// The duration, expressed as hours IF hhmm can be parsed
        /// </summary>
        public decimal? Hours { get; set; }
    }

    public class OoiTimes
    {
        public TimeValue Out { get; set; }
        public TimeValue Off { get; set; }
        public TimeValue On { get; set; }
        public TimeValue In { get; set; }
    }

    /// <summary>
    /// The response returned to callers (mirrors the JSON shape of the original
    /// Node prototype so client apps don't need different parsing logic for
    /// each backend). When Success is false, Error explains why - either the
    /// image wasn't a recognizable flight-log screen, or nothing on it could
    /// be identified. Never treat a false Success as a partial record.
    /// </summary>
    public class ScanResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public string ScreenType { get; set; }
        public string Notes { get; set; }
        public string FlightNumber { get; set; }
        /// <summary>
        /// Departure date IN ZULU.
        /// </summary>
        public DateValue Date { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public OoiTimes Times { get; set; }
        public DurationValue BlockTime { get; set; }
        public DurationValue FlightTime { get; set; }
        public bool? IsInProgress { get; set; }
        public string Stage { get; set; }
        public RawExtraction Raw { get; set; }

        public ScannedFlight ParsedResults
        {
            get
            {
                DateTime? dtNow = DateTime.TryParse(Date?.Iso, out var dtParsed) ? DateTime.SpecifyKind(dtParsed, DateTimeKind.Utc) : (DateTime?)null;

                // "Date" is essentially "now" - the UTC day as of roughly when the flight
                // reached its latest milestone. Anchor that day to whichever OOOI event is
                // the LATEST one actually present, then walk backwards (IN -> ON -> OFF ->
                // OUT), subtracting a day anywhere the naive same-day placement would put
                // an earlier event chronologically after the one that follows it.
                TimeValue[] reverseChain = { Times?.In, Times?.On, Times?.Off, Times?.Out };
                DateTime?[] resolved = new DateTime?[4];
                DateTime? anchorInstant = null;
                bool haveAnchor = false;

                for (int i = 0; i < reverseChain.Length; i++)
                {
                    DateTime? naive = reverseChain[i]?.ToDateTime(dtNow);
                    if (!naive.HasValue)
                        continue;

                    if (haveAnchor && naive.Value > anchorInstant.Value)
                        naive = naive.Value.AddDays(-1);

                    resolved[i] = naive;
                    anchorInstant = naive;
                    haveAnchor = true;
                }

                return new ScannedFlight
                {
                    FlightNumber = FlightNumber,
                    Origin = Origin,
                    Destination = Destination,
                    Date = dtNow,
                    In = resolved[0],
                    On = resolved[1],
                    Off = resolved[2],
                    Out = resolved[3],
                    Block = BlockTime?.Hours
                };
            }
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this, FlightDeckScanJson.SerializerSettings);
        }

        public static ScanResult FailureSample
        {
            get
            {
                return new ScanResult
                {
                    Success = false,
                    Error = "Sample failure: image not recognized as a flight-deck display."
                };
            }
        }

        public static ScanResult SuccessSample
        {
            get
            {
                return new ScanResult
                {
                    Success = true,
                    ScreenType = "G1000",
                    FlightNumber = "AB123",
                    Date = new DateValue { Raw = "2024-06-15", Iso = "2024-06-15", YearInferred = false, ParseError = false },
                    Origin = "KJFK",
                    Destination = "KLAX",
                    Times = new OoiTimes
                    {
                        Out = new TimeValue { Raw = "08:00", Hhmm = "0800", HasSeconds = false, ParseError = false },
                        Off = new TimeValue { Raw = "08:15", Hhmm = "0815", HasSeconds = false, ParseError = false },
                        On = new TimeValue { Raw = "11:30", Hhmm = "1130", HasSeconds = false, ParseError = false },
                        In = new TimeValue { Raw = "11:45", Hhmm = "1145", HasSeconds = false, ParseError = false }
                    },
                    BlockTime = new DurationValue { Hhmm = "03:45", Source = "printed" },
                    FlightTime = new DurationValue { Hhmm = "03:15", Source = "printed" },
                    IsInProgress = false,
                    Stage = null,
                    Notes = null,
                    Warnings = new List<string>(),
                    Raw = new RawExtraction
                    {
                        IsFlightDeckDisplay = true,
                        ScreenType = "G1000",
                        FlightNumber = "AB123",
                        DateRaw = "2024-06-15",
                        Origin = "KJFK",
                        Destination = "KLAX",
                        OutRaw = "08:00",
                        OffRaw = "08:15",
                        OnRaw = "11:30",
                        InRaw = "11:45",
                        BlockRaw = "03:45",
                        FlightTimeRaw = "03:15",
                        ExtractionNotes = null
                    }
                };
            }
        }
    }
}
