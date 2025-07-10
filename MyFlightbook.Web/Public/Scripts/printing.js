/******************************************************
 *
 * Copyright (c) 2024-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/*
  Handles preference management for printing
   - Container - the containing DIV in which this lives
   - Options:
     * includeFlightsSection (whether or not to show the "include flights" section)
     * onChangeFunc (javascript function to call when preferences change)
*/
function printingSections(container, options) {
    this.options = options;
    this.container = container;

    this.updPrntSects = function() {
        var ckPrintSectionEndorsements = $('#ckPrintSectionEndorsements')[0];
        var ckIncludeImgs = $('#ckPrintSectionEndorsementImages')[0];
        if (!ckPrintSectionEndorsements.checked)
            ckIncludeImgs.checked = false;
        ckIncludeImgs.disabled = !ckPrintSectionEndorsements.checked;
        var ckPrintSectionTotals = $('#ckPrintSectionTotals')[0];
        var ckPrintSectionCompactTotals = $('#ckPrintSectionCompactTotals')[0]
        if (!ckPrintSectionTotals.checked)
            ckPrintSectionCompactTotals.checked = false;
        ckPrintSectionCompactTotals.disabled = !ckPrintSectionTotals.checked;

        var fCoverPage = $('#ckPrintSectionsCoverSheet')[0].checked;
        var ckDOB = $("#ckPrintSectionsDOB");
        ckDOB[0].disabled = !fCoverPage;
        ckDOB[0].checked = ckDOB[0].checked && fCoverPage;

        var sects = new Object();
        sects["Endorsements"] = ckPrintSectionEndorsements.checked ? (ckIncludeImgs.checked ? "DigitalAndPhotos" : "DigitalOnly") : "None";
        sects["IncludeCoverPage"] = $('#ckPrintSectionsCoverSheet')[0].checked;
        sects["IncludeFlights"] = !options.includeFlightsSection || ($("#ckPrintsectionFlights").length > 0 && $("#ckPrintSectionFlights")[0].checked);
        sects["IncludeTotals"] = $('#ckPrintSectionTotals')[0].checked;
        sects["CompactTotals"] = ckPrintSectionCompactTotals.checked;
        sects["IncludeDOB"] = $("#ckPrintSectionsDOB")[0].checked;
        sects["IncludeAddress"] = $("#ckPrintSectionsAddress")[0].checked;
        sects["IncludeHeadshot"] = $("#ckPvHeadshot")[0].checked;
        options.onChangeFunc(sects)
        return false;
    }

    this.initForm = function () {
        $("#divPrefPrintingSections").find("input[type='checkbox']").on("change", updPrntSects);
    }

    this.initForm();
}