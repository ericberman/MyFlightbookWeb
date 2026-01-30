/******************************************************
 * 
 * Copyright (c) 2024-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/*
   Creates an aircraft editor that uses a web service; will ultimately replace impaircraft.js
   * aircraftImportEndpoint - contains the fully-qualified path to the webservice endpoint for local preferences
   * options - contains identifiers of elements that matter for this
     * summaryContainer - the id of the div to display the summary of what has been imported
     * unmatchedContainer - the id of the div to display the summary of aircraft that are not found
*/
class aircraftImportEditor {
    constructor(aircraftImportEndpoint, options) {
        this.dictModelMapping = null;

        this.renderSummary = function () {
            var params = new Object();
            var d = JSON.stringify(params);
            $.ajax({
                url: aircraftImportEndpoint + "/ImportSummary",
                type: "POST", data: d, dataType: "html", contentType: "application/json",
                error: function (xhr, status, error) { window.alert(xhr.responseText); },
                success: function (response) {
                    $("#" + options.summaryContainer).html(response);
                }
            });
        }

        this.renderUnmatched = function () {
            var params = new Object();
            var d = JSON.stringify(params);
            $.ajax({
                url: aircraftImportEndpoint + "/ReviewNewAircraft",
                type: "POST", data: d, dataType: "html", contentType: "application/json",
                error: function (xhr, status, error) { window.alert(xhr.responseText); },
                success: function (response) {
                    $("#" + options.unmatchedContainer).html(response);
                }
            });
        }

        this.addExistingAircraft = function (sender, id, progress) {
            var prg = $("#" + progress).dialog({ modal: true, resizable: false, draggable: false });
            $(".ui-dialog-titlebar").hide();
            var params = new Object();
            params.aircraftID = id;
            var d = JSON.stringify(params);
            $.ajax({
                url: aircraftImportEndpoint + "/AddExistingAircraft",
                type: "POST", data: d, dataType: "text", contentType: "application/json",
                error: function (xhr, status, error) { window.alert(xhr.responseText); },
                complete: function () { prg.dialog('close'); },
                success: function (response) {
                    $(sender).hide();
                    $(sender).next().text(response);
                }
            });
        }

        this.addAllExisting = function (sender, progress, onSuccess) {
            var prg = $("#" + progress).dialog({ modal: true, resizable: false, draggable: false });
            $(".ui-dialog-titlebar").hide();
            var params = new Object();
            var d = JSON.stringify(params);
            $.ajax({
                url: aircraftImportEndpoint + "/AddAllExisting",
                type: "POST", data: d, dataType: "text", contentType: "application/json",
                error: function (xhr) { window.alert(xhr.responseText); },
                complete: function () { prg.dialog('close'); },
                success: function (response) {

                    onSuccess();
                }
            });
        }

        this.addNew = function (sender, spec, onSuccess) {
            var params = new Object();
            params.spec = spec;
            params.szJSonMapping = this.dictModelMapping;
            var pthis = this;

            var d = JSON.stringify(params);
            $.ajax({
                url: aircraftImportEndpoint + "/AddNewAircraft",
                type: "POST", data: d, dataType: "text", contentType: "application/json",
                error: function (xhr) { window.alert(xhr.responseText); },
                success: function (response) {
                    pthis.dictModelMapping = response;
                    onSuccess(sender);
                }
            });
        }

        this.addAllNew = function (progress, rgSpecs, onSuccess)
        {
            var prg = $("#" + progress).dialog({ modal: true, resizable: false, draggable: false });
            $(".ui-dialog-titlebar").hide();
            var params = new Object();
            params.specs = rgSpecs;
            params.szJSonMapping = this.dictModelMapping;
            var pthis = this;
            var d = JSON.stringify(params);
            $.ajax({
                url: aircraftImportEndpoint + "/AddAllNewAircraft",
                type: "POST", data: d, dataType: "text", contentType: "application/json",
                error: function (xhr, status, error) { window.alert(xhr.responseText); },
                complete: function () { prg.dialog('close'); },
                success: function (response) {
                    pthis.dictModelMapping = response;
                    onSuccess();
                }
            });
        }

        this.modelEditorAutocomplete = function (targetHRef, onSelect) {
            $("input[name='importModel']").autocomplete({
                source: function (request, response) {
                    var params = new Object();
                    params.prefixText = request.term;
                    params.count = 20;
                    params.contextKey = null;
                    var d = JSON.stringify(params);
                    $.ajax({
                        url: targetHRef,
                        type: "POST", data: d, dataType: "json", contentType: "application/json",
                        error: function (xhr, status, error) { window.alert(xhr.responseJSON.Message); },
                        success: function (r) { response(r); }
                    });
                },
                select: function (event, ui) {
                    return onSelect(event, ui);
                },
                minLength: 2,
                classes: { "ui-autocomplete": "AutoExtender AutoExtenderList" }
            });
        }

        this.validateProposedAircraft = function (szTail, idModel, instanceType, onsuccess, onfailure) {
            var params = new Object();
            params.szTail = szTail;
            params.idModel = idModel;
            params.instanceType = instanceType;

            $.ajax(
                {
                    type: "POST",
                    data: JSON.stringify(params),
                    url: aircraftImportEndpoint + "/ValidateAircraft",
                    dataType: "text",
                    contentType: "application/json",
                    error: function (xhr, status, error) { window.alert(xhr.responseText); },
                    success: function (response) {
                        if (response == "")
                            onsuccess();
                        else
                            onfailure(response);
                    }
                }
            );
        }
    }
}