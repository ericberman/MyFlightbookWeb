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
- Ignore any ""current UTC time"" / live clock field that is not one of OUT/OFF/ON/IN/BLOCK/FLIGHT (some ACARS pages show the current time in a corner - that is not a flight time and must not be reported as one).
- Do not do any arithmetic, cross-checking, or consistency analysis between fields (for example, whether BLOCK matches OUT/IN, or FLIGHT matches OFF/ON) - that is handled downstream with the correct formulas. Just report what is printed for each field independently. Do not mention apparent mismatches between fields in extractionNotes - you do not have the correct formula and will get it wrong.
- If the photo does not show this kind of flight-log/status screen at all, set isFlightDeckDisplay to false and leave the other fields null.
- If multiple flight-log pages/entries are visible in one photo, use the one that is the primary/focused/in-frame page, and mention which in extractionNotes.";

        public const string UserInstruction =
            "Read this flight-deck display and call record_mcdu_scan with exactly what is printed on it.";
    }
}
