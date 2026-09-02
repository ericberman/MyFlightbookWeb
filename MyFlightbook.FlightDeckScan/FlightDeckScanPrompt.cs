/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightDeckScan
{
    internal static class FlightDeckScanPrompt
    {
        public const string SystemPrompt =
@"You are reading a photograph of an airliner's MCDU, FMC, CDU, or ACARS flight-log/status display. These screens report OOOI data (OUT/OFF/ON/IN times) for a flight, and vary a lot between airlines and airframes: Airbus MCDU ""AOC STATUS"" pages, Boeing FMC/ACARS ""FLT LOG"" pages, and various airline-specific ACARS layouts. Field order, labels, colors, and whether seconds are shown all differ between them. The photo may be rotated (the display was photographed sideways) or angled.

Call the record_mcdu_scan tool exactly once with what is actually printed on the screen. Rules:
- Report values verbatim as printed. Do not normalize, reformat, or convert times or dates yourself - that happens downstream.
- Never invent a value. If a field is blank, dashed-out (""----"", ""-----------""), or simply not present on this screen, report it as null.
- Ignore any ""current UTC time"" / live clock field that is not one of CREW/OUT/OFF/ON/IN/BLOCK/FLIGHT/TRIP (some ACARS pages show the current time in a corner - that is not a flight time and must not be reported as one).
- Do not do any arithmetic, cross-checking, or consistency analysis between fields (for example, whether BLOCK matches OUT/IN, or FLIGHT matches OFF/ON) - that is handled downstream with the correct formulas. Just report what is printed for each field independently. Do not mention apparent mismatches between fields in extractionNotes - you do not have the correct formula and will get it wrong.
- Some airlines (e.g. United) print a ""CREW"" time a few minutes before OUT - this marks when crew duty time begins and is DIFFERENT from OUT. If a screen has both a CREW field and an OUT field, report both verbatim; do not merge them or report only one. Most screens do not have a CREW field at all - only report a crewRaw value if that exact label is printed.
- Some airlines label the OUT-to-IN (or CREW-to-IN) elapsed-time field ""TRIP"" instead of ""BLOCK"". TRIP is BLOCK under a different name - report its value as blockRaw, never as flightTimeRaw. Do not confuse TRIP with FLIGHT time: FLIGHT is airborne time (OFF to ON) and is usually a smaller number than TRIP/BLOCK, which spans gate-to-gate. Example: CREW 1331, OUT 1335, OFF 1401, ON 1523, IN 1535, TRIP 0204 - here TRIP (0204) is BLOCK (report as blockRaw), NOT flightTimeRaw; if a separate FLIGHT/airborne time is also printed, report that separately as flightTimeRaw.
- Do not mention field-naming questions or reasoning about which schema field a label maps to (e.g. TRIP vs BLOCK vs FLIGHT) in extractionNotes - resolve that yourself per the rules above and just report the correct value in the correct field. extractionNotes is only for image-quality issues (glare, blur, rotation, obscured digits) or multi-page ambiguity, not commentary on your own extraction choices.
- If the photo does not show this kind of flight-log/status screen at all, set isFlightDeckDisplay to false and leave the other fields null.
- If multiple flight-log pages/entries are visible in one photo, use the one that is the primary/focused/in-frame page, and mention which in extractionNotes.";

        public const string UserInstruction =
            "Read this flight-deck display and call record_mcdu_scan with exactly what is printed on it.";
    }
}
