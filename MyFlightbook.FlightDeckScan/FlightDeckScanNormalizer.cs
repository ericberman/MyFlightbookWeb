using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightDeckScan
{
    /// <summary>
    /// Pure, dependency-free normalization of the raw per-screen extraction into
    /// the stable ScanResult the callers (web page, Android, iOS) consume.
    /// Deliberately kept out of the model prompt so it's unit-testable without
    /// hitting the network. This is a straight C# port of the Node prototype's
    /// normalize.js, validated against the 5 sample screens from
    /// https://github.com/ericberman/MyFlightbookWeb/issues/1577 - see
    /// FlightDeckScanNormalizerTests for the same 5 cases ported to C#.
    /// </summary>
    public static class FlightDeckScanNormalizer
    {
        private static readonly Dictionary<string, int> Months = new Dictionary<string, int>
        {
            { "JAN", 1 }, { "FEB", 2 }, { "MAR", 3 }, { "APR", 4 }, { "MAY", 5 }, { "JUN", 6 },
            { "JUL", 7 }, { "AUG", 8 }, { "SEP", 9 }, { "OCT", 10 }, { "NOV", 11 }, { "DEC", 12 }
        };

        private static readonly Regex HmsRegex = new Regex(@"^(\d{1,2}):(\d{2}):(\d{2})$", RegexOptions.Compiled);
        private static readonly Regex HmRegex = new Regex(@"^(\d{1,2}):(\d{2})$", RegexOptions.Compiled);
        private static readonly Regex HhmmRegex = new Regex(@"^(\d{2})(\d{2})$", RegexOptions.Compiled);
        private static readonly Regex HhmmssRegex = new Regex(@"^(\d{2})(\d{2})(\d{2})$", RegexOptions.Compiled);
        private static readonly Regex DateRegex = new Regex(@"^(\d{1,2})([A-Z]{3})(\d{2})?$", RegexOptions.Compiled);
        private static readonly Regex DashesRegex = new Regex(@"^-+$", RegexOptions.Compiled);

        public static decimal? ParseElapsedHhMm(string hhmm)
        {
            if (String.IsNullOrEmpty(hhmm))
                return null;

            string cleaned = hhmm.Trim();
            Match m = HmRegex.Match(cleaned);
            if (m.Success)
            {
                int h = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                int mi = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                return h + mi / 60.0m;
            }
            m = HmsRegex.Match(cleaned);
            if (m.Success)
            {
                int h = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                int mi = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                return h + mi / 60.0m;
            }
            if (HhmmRegex.IsMatch(cleaned))
            {
                int h = int.Parse(cleaned.Substring(0, 2), CultureInfo.InvariantCulture);
                int mi = int.Parse(cleaned.Substring(2, 2), CultureInfo.InvariantCulture);
                return h + mi / 60.0m;
            }
            return null;
        }

        /// <summary>
        /// Parses a raw OOOI time string ("1331Z", "13:31", "05:02:40", "1331", "050240", "----")
        /// into a TimeValue, or null if raw is null/blank/dashed-out. If raw is present
        /// but not recognizable, returns a TimeValue with ParseError = true and Hhmm = null
        /// so callers can warn instead of silently dropping data.
        /// </summary>
        public static TimeValue ParseTime(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return null;

            string cleaned = raw.Trim().ToUpperInvariant();
            if (cleaned.EndsWith("Z", StringComparison.Ordinal))
                cleaned = cleaned.Substring(0, cleaned.Length - 1);

            if (cleaned.Length == 0 || DashesRegex.IsMatch(cleaned))
                return null;

            Match m = HmsRegex.Match(cleaned);
            if (m.Success)
            {
                int h = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                int mi = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                int s = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                if (h > 23 || mi > 59 || s > 59)
                    return new TimeValue { Raw = raw, Hhmm = null, ParseError = true };
                return new TimeValue { Raw = raw, Hhmm = $"{h:D2}:{mi:D2}", HasSeconds = true };
            }

            m = HmRegex.Match(cleaned);
            if (m.Success)
            {
                int h = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                int mi = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                if (h > 23 || mi > 59)
                    return new TimeValue { Raw = raw, Hhmm = null, ParseError = true };
                return new TimeValue { Raw = raw, Hhmm = $"{h:D2}:{mi:D2}", HasSeconds = false };
            }

            m = HhmmRegex.Match(cleaned);
            if (m.Success)
            {
                int h = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                int mi = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                if (h > 23 || mi > 59)
                    return new TimeValue { Raw = raw, Hhmm = null, ParseError = true };
                return new TimeValue { Raw = raw, Hhmm = $"{h:D2}:{mi:D2}", HasSeconds = false };
            }

            // Six bare digits, no separators - "HHMMSS" (e.g. "050240" for 05:02:40).
            // Checked after HhmmRegex/HmRegex rather than before: length alone disambiguates
            // (4 digits vs 6), so order doesn't affect correctness, but this keeps every
            // "no separator" variant grouped with its colon-separated counterpart above it.
            m = HhmmssRegex.Match(cleaned);
            if (m.Success)
            {
                int h = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                int mi = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                int s = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                if (h > 23 || mi > 59 || s > 59)
                    return new TimeValue { Raw = raw, Hhmm = null, ParseError = true };
                return new TimeValue { Raw = raw, Hhmm = $"{h:D2}:{mi:D2}", HasSeconds = true };
            }

            return new TimeValue { Raw = raw, Hhmm = null, ParseError = true };
        }

        /// <summary>
        /// Parses a raw date string (DDMMMYY, DDMMM, or blank) into a DateValue, or
        /// null if raw is null/blank/dashed-out. referenceDateUtc is used to pick a
        /// year when none is printed: the most recent occurrence of that month/day
        /// that is not in the future relative to referenceDateUtc.
        /// </summary>
        public static DateValue ParseDate(string raw, DateTime referenceDateUtc)
        {
            if (string.IsNullOrEmpty(raw))
                return null;

            string cleaned = raw.Trim().ToUpperInvariant();
            if (cleaned.Length == 0 || DashesRegex.IsMatch(cleaned))
                return null;

            Match m = DateRegex.Match(cleaned);
            if (!m.Success)
                return new DateValue { Raw = raw, Iso = null, YearInferred = false, ParseError = true };

            int day = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            string monAbbr = m.Groups[2].Value;
            string yearStr = m.Groups[3].Success ? m.Groups[3].Value : null;

            if (!Months.TryGetValue(monAbbr, out int month) || day < 1 || day > 31)
                return new DateValue { Raw = raw, Iso = null, YearInferred = false, ParseError = true };

            if (yearStr != null)
            {
                int year = 2000 + int.Parse(yearStr, CultureInfo.InvariantCulture);
                return new DateValue { Raw = raw, Iso = $"{year:D4}-{month:D2}-{day:D2}", YearInferred = false };
            }

            // No year printed: pick the most recent non-future occurrence.
            int refYear = referenceDateUtc.Year;
            DateTime candidate;
            try
            {
                candidate = new DateTime(refYear, month, day, 0, 0, 0, DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException)
            {
                return new DateValue { Raw = raw, Iso = null, YearInferred = false, ParseError = true };
            }
            int inferredYear = candidate > referenceDateUtc ? refYear - 1 : refYear;
            return new DateValue { Raw = raw, Iso = $"{inferredYear:D4}-{month:D2}-{day:D2}", YearInferred = true };
        }

        private static int HhmmToMinutes(string hhmm)
        {
            string[] parts = hhmm.Split(':');
            return int.Parse(parts[0], CultureInfo.InvariantCulture) * 60 + int.Parse(parts[1], CultureInfo.InvariantCulture);
        }

        private static string MinutesToHhmm(int totalMinutes)
        {
            int m = ((totalMinutes % 1440) + 1440) % 1440;
            return $"{m / 60:D2}:{m % 60:D2}";
        }

        /// <summary>
        /// Elapsed time from startHhmm to endHhmm ("HH:MM" strings), wrapping past midnight.
        /// Returns null if either argument is null.
        /// </summary>
        public static string Elapsed(string startHhmm, string endHhmm)
        {
            if (startHhmm == null || endHhmm == null)
                return null;
            int diff = HhmmToMinutes(endHhmm) - HhmmToMinutes(startHhmm);
            if (diff < 0)
                diff += 1440;
            return MinutesToHhmm(diff);
        }

        private static bool WithinOneMinute(string hhmmA, string hhmmB)
        {
            if (hhmmA == null || hhmmB == null)
                return false;
            int diff = Math.Abs(HhmmToMinutes(hhmmA) - HhmmToMinutes(hhmmB));
            return diff <= 1 || diff >= 1439; // tolerate midnight-wrap rounding
        }

        private static ScanResult NotAFlightDeckDisplay(RawExtraction raw)
        {
            return new ScanResult
            {
                Success = false,
                Error = "This image does not appear to show an MCDU/FMC/ACARS flight-log screen with recognizable flight data.",
                ScreenType = raw?.ScreenType,
                Notes = raw?.ExtractionNotes,
                Times = new OoiTimes()
            };
        }

        private static ScanResult NothingIdentified(RawExtraction raw)
        {
            return new ScanResult
            {
                Success = false,
                Error = "No flight number, date, route, or OOOI times could be identified in this image.",
                ScreenType = raw.ScreenType,
                Notes = raw.ExtractionNotes,
                Times = new OoiTimes()
            };
        }

        /// <summary>
        /// Builds the final ScanResult from one raw model extraction.
        /// </summary>
        /// <param name="raw">The model's structured extraction (see RawExtraction).</param>
        /// <param name="referenceDateUtc">"Now" (UTC), for inferring a missing year.</param>
        /// <param name="includeDebug">If true, include the raw extraction notes in the result.</param>
        public static ScanResult BuildResult(RawExtraction raw, DateTime referenceDateUtc, bool includeDebug = false)
        {
            if (raw == null || !raw.IsFlightDeckDisplay)
                return NotAFlightDeckDisplay(raw);

            DateValue date = ParseDate(raw.DateRaw, referenceDateUtc);
            TimeValue crewT = ParseTime(raw.CrewRaw);
            TimeValue outT = ParseTime(raw.OutRaw);
            TimeValue offT = ParseTime(raw.OffRaw);
            TimeValue onT = ParseTime(raw.OnRaw);
            TimeValue inT = ParseTime(raw.InRaw);

            bool nothingIdentified =
                string.IsNullOrEmpty(raw.FlightNumber) &&
                date == null &&
                string.IsNullOrEmpty(raw.Origin) &&
                string.IsNullOrEmpty(raw.Destination) &&
                outT == null && offT == null && onT == null && inT == null;

            if (nothingIdentified)
                return NothingIdentified(raw);

            var warnings = new List<string>();

            if (string.IsNullOrEmpty(raw.FlightNumber)) warnings.Add("Flight number not shown on screen.");
            if (string.IsNullOrEmpty(raw.Origin)) warnings.Add("Origin airport not shown on screen.");
            if (string.IsNullOrEmpty(raw.Destination)) warnings.Add("Destination airport not shown on screen.");
            if (date == null)
                warnings.Add("Date not shown on screen.");
            else if (date.ParseError)
                warnings.Add($"Could not parse printed date \"{date.Raw}\".");
            else if (date.YearInferred)
                warnings.Add($"Year not shown on screen; inferred {date.Iso.Substring(0, 4)} from the date.");

            void WarnIfParseError(string label, TimeValue t)
            {
                if (t != null && t.ParseError)
                    warnings.Add($"Could not parse printed {label} time \"{t.Raw}\".");
            }
            WarnIfParseError("OUT", outT);
            WarnIfParseError("OFF", offT);
            WarnIfParseError("CREW", crewT);
            WarnIfParseError("ON", onT);
            WarnIfParseError("IN", inT);

            // Flight stage, from the OOOI data actually present.
            string stage = "unknown";
            if (inT?.Hhmm != null) stage = "complete";
            else if (onT?.Hhmm != null) stage = "landed_taxiing";
            else if (offT?.Hhmm != null) stage = "airborne";
            else if (outT?.Hhmm != null) stage = "not_departed";
            bool isInProgress = stage != "complete" && stage != "unknown";
            if (isInProgress)
                warnings.Add("Flight appears to still be in progress (IN time not yet recorded).");

            // BLOCK: prefer the printed value; fall back to OUT->IN or, if present, CREW->IN
            // (some airlines, e.g. United, compute BLOCK/TRIP from CREW rather than OUT).
            TimeValue blockPrinted = ParseTime(raw.BlockRaw);
            string blockComputedOutIn = Elapsed(outT?.Hhmm, inT?.Hhmm);
            string blockComputedCrewIn = Elapsed(crewT?.Hhmm, inT?.Hhmm);
            DurationValue blockTime = null;
            if (blockPrinted?.Hhmm != null)
            {
                bool matchesOutIn = blockComputedOutIn != null && WithinOneMinute(blockPrinted.Hhmm, blockComputedOutIn);
                bool matchesCrewIn = blockComputedCrewIn != null && WithinOneMinute(blockPrinted.Hhmm, blockComputedCrewIn);
                blockTime = new DurationValue
                {
                    Hhmm = blockPrinted.Hhmm,
                    Source = matchesCrewIn && !matchesOutIn ? "printed_matches_crew_in" : "printed",
                    Hours = ParseElapsedHhMm(blockPrinted.Hhmm)
                };
                if (!matchesOutIn && !matchesCrewIn && (blockComputedOutIn != null || blockComputedCrewIn != null))
                    warnings.Add($"Printed BLOCK time ({blockPrinted.Hhmm}) does not match OUT/IN times (computed {blockComputedOutIn ?? "n/a"}){(crewT?.Hhmm != null ? $" or CREW/IN times (computed {blockComputedCrewIn})" : "")}; using the printed value.");
            }
            else if (blockComputedOutIn != null)
            {
                blockTime = new DurationValue { Hhmm = blockComputedOutIn, Source = "computed_from_out_in", Hours = ParseElapsedHhMm(blockComputedOutIn) };
            }
            else if (blockComputedCrewIn != null)
            {
                blockTime = new DurationValue { Hhmm = blockComputedCrewIn, Source = "computed_from_crew_in", Hours = ParseElapsedHhMm(blockComputedCrewIn) };
            }
            
            // FLIGHT: prefer the printed value; fall back to OFF->ON.
            TimeValue flightPrinted = ParseTime(raw.FlightTimeRaw);
            string flightComputed = Elapsed(offT?.Hhmm, onT?.Hhmm);
            DurationValue flightTime = null;
            if (flightPrinted?.Hhmm != null)
            {
                flightTime = new DurationValue { Hhmm = flightPrinted.Hhmm, Source = "printed", Hours = ParseElapsedHhMm(flightPrinted.Hhmm) };
                if (flightComputed != null && !WithinOneMinute(flightPrinted.Hhmm, flightComputed))
                    warnings.Add($"Printed FLIGHT time ({flightPrinted.Hhmm}) does not match OFF/ON times (computed {flightComputed}); using the printed value.");
            }
            else if (flightComputed != null)
            {
                flightTime = new DurationValue { Hhmm = flightComputed, Source = "computed_from_off_on", Hours = ParseElapsedHhMm(flightComputed) };
            }

            return new ScanResult
            {
                Success = true,
                Error = null,
                Warnings = warnings,
                ScreenType = raw.ScreenType,
                Notes = raw.ExtractionNotes,
                FlightNumber = string.IsNullOrEmpty(raw.FlightNumber) ? null : raw.FlightNumber,
                Date = date,
                Origin = string.IsNullOrEmpty(raw.Origin) ? null : raw.Origin,
                Destination = string.IsNullOrEmpty(raw.Destination) ? null : raw.Destination,
                Times = new OoiTimes
                {
                    Crew = crewT?.Hhmm != null ? crewT : null,
                    Out = outT?.Hhmm != null ? outT : null,
                    Off = offT?.Hhmm != null ? offT : null,
                    On = onT?.Hhmm != null ? onT : null,
                    In = inT?.Hhmm != null ? inT : null
                },
                BlockTime = blockTime,
                FlightTime = flightTime,
                IsInProgress = isInProgress,
                Stage = stage,
                Raw = includeDebug ? raw : null
            };
        }
    }
}
