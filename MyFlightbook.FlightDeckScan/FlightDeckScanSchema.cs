using Newtonsoft.Json.Linq;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightDeckScan
{
    /// <summary>
    /// The forced-tool-use schema sent to the Anthropic API, so the model's
    /// reply is always this exact shape rather than free-form prose. Mirrors
    /// the field-by-field extraction contract described in FlightDeckScanPrompt.
    /// </summary>
    internal static class FlightDeckScanSchema
    {
        public const string ToolName = "record_mcdu_scan";

        private static JObject StringOrNullProp(string description)
        {
            return new JObject
            {
                ["type"] = new JArray("string", "null"),
                ["description"] = description
            };
        }

        /// <summary>
        /// Builds the tool definition object (name/description/input_schema) to
        /// include in the "tools" array of the Anthropic Messages API request.
        /// </summary>
        public static JObject BuildToolDefinition()
        {
            var properties = new JObject
            {
                ["isFlightDeckDisplay"] = new JObject
                {
                    ["type"] = "boolean",
                    ["description"] =
                        "True if this image shows an MCDU, FMC, CDU, or ACARS-style flight-log/status display of the kind that reports OOOI times (OUT/OFF/ON/IN) or a flight/block time summary. False for any other kind of image (e.g. unrelated photo, boarding pass, random cockpit panel with no flight-log data)."
                },
                ["screenType"] = StringOrNullProp(
                    "A short label for the screen layout/title as printed, e.g. \"AOC STATUS\", \"ACARS FLIGHT LOG\", \"ACARS-FLT LOG-PREV\", \"ACARS-FLT LOG-CURR\", \"ACARS OOOI STATUS\". Null if not applicable."),
                ["flightNumber"] = StringOrNullProp(
                    "The flight number / flight ID exactly as printed (e.g. \"G66111\", \"AFR66SQ\", \"792F\", \"DA04\"). Null if blank, dashed-out, or absent."),
                ["dateRaw"] = StringOrNullProp(
                    "The date exactly as printed, verbatim, e.g. \"07AUG26\", \"09AUG\" (no year shown), \"08JUL20\". Null if no date is printed anywhere on the screen (or shown only as dashes)."),
                ["origin"] = StringOrNullProp(
                    "Origin airport code exactly as printed (e.g. \"KAEX\", \"LEAL\", \"EHAM\", \"LILC\"). Null if blank/dashed-out."),
                ["destination"] = StringOrNullProp(
                    "Destination airport code exactly as printed. Null if blank/dashed-out."),
                ["crewRaw"] = StringOrNullProp(
                    "The CREW time exactly as printed, if shown - a Zulu time labeled \"CREW\" that some airlines (e.g. United) print a few minutes before OUT, marking when the crew's duty/trip time officially begins. This is DIFFERENT from OUT and is used by that airline to compute BLOCK/TRIP time instead of OUT (e.g. CREW 1331, OUT 1335, IN 1535 -> printed TRIP is 2:04, not 2:00 - i.e. CREW to IN, not OUT to IN). Report null if no such field/label is present on this screen - most screens do not have one, and it must not be confused with OUT."),
                ["outRaw"] = StringOrNullProp(
                    "The OUT (block out / gate departure) time exactly as printed, including seconds if shown (e.g. \"1331\", \"1331Z\", \"05:02:40\", \"0953Z\"). Null if not printed or shown only as dashes."),
                ["offRaw"] = StringOrNullProp(
                    "The OFF (takeoff) time exactly as printed. Null if not printed."),
                ["onRaw"] = StringOrNullProp(
                    "The ON (landing) time exactly as printed. Null if not printed."),
                ["inRaw"] = StringOrNullProp(
                    "The IN (block in / gate arrival) time exactly as printed. Null if not printed, blank, or shown only as dashes (this is normal for a flight still in progress)."),
                ["blockRaw"] = StringOrNullProp(
                    "The BLOCK time exactly as printed (elapsed time from OUT to IN, or from CREW to IN if a CREW time is shown) - e.g. \"0144\", \"02:46\". Some airlines (e.g. United) label this field \"TRIP\" instead of \"BLOCK\" - treat TRIP as the same concept and report its value here. Null if neither BLOCK nor TRIP is printed on screen. Do not compute this yourself if it is not shown."),
                ["flightTimeRaw"] = StringOrNullProp(
                    "The FLIGHT time exactly as printed (airborne time from OFF to ON) - ONLY if this is a SEPARATE, distinctly-labeled field from BLOCK/TRIP (e.g. a screen showing both \"BLOCK 0204\" and \"FLIGHT 0141\" as two different fields). If the screen shows only ONE elapsed-duration field total (labeled BLOCK, TRIP, or anything else), that single field is BLOCK/TRIP - report it as blockRaw and leave flightTimeRaw null. Do not populate flightTimeRaw just because a duration-shaped value is present; it must have its own distinct label separate from BLOCK/TRIP. Null if no separate FLIGHT-labeled field exists on screen."),
                ["extractionNotes"] = StringOrNullProp(
                    "Optional short note about anything that affects confidence in what you READ: glare, blur, rotated image, partially obscured digits, multiple flight-log pages visible and which page number was used, etc. Null if nothing notable. Do NOT use this field to cross-check or comment on whether values are consistent with each other (e.g. whether BLOCK time matches OUT/IN, or FLIGHT time matches OFF/ON) - that comparison is done downstream with the correct formula, and a guess here is likely to be wrong.")
            };

            var required = new JArray(
                "isFlightDeckDisplay", "screenType", "flightNumber", "dateRaw", "origin", "destination",
                "crewRaw", "outRaw", "offRaw", "onRaw", "inRaw", "blockRaw", "flightTimeRaw", "extractionNotes");

            return new JObject
            {
                ["name"] = ToolName,
                ["description"] =
                    "Record exactly what is printed on the photographed MCDU/FMC/ACARS flight-log screen. Only report values that are actually visible as text on the screen. Never guess, infer, or fill in a value that is not printed. If a field is blank, shown as dashes (e.g. \"----\", \"-----------\"), or not present on the screen, report it as null.",
                ["input_schema"] = new JObject
                {
                    ["type"] = "object",
                    ["properties"] = properties,
                    ["required"] = required
                }
            };
        }
    }
}
