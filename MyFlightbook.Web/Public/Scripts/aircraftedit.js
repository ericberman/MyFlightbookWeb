/******************************************************
 * 
 * Copyright (c) 2024-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/*
    model autocomplete/search helpers
     * onModelChange - function to call when a model is selected, deselected, or changed.
     * searchInput - JQuery of the search text box
     * fRegisteredOnly - true if autocompletion of models should be limited to registered aircraft types (i.e., no sims/anonymous-only)
     * manDrop - the manufacturer drop-down jquery
     * modelDrop - the model drop-down jquery
     * autoCompleteHREF - reference for the model auto-completion service
     * onModelSelected - function to call with the selected model from the drop-down (onModelChange is subsequently called))
*/
function setUpAutocompleteModels(options) {
    options.modelDrop.on("change", options.onModelChange);
    options.manDrop.on("change", function () {
        options.modelDrop.val("");
        options.onModelChange();
    });

    options.searchInput.autocomplete({
        source: function (request, response) {
            var params = new Object();
            params.prefixText = request.term;
            params.count = 20;
            params.fRegisteredOnly = options.fRegisteredOnly;
            var d = JSON.stringify(params);
            $.ajax({
                url: options.autoCompleteHREF,
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) { window.alert(xhr.responseText); },
                success: function (r) { response(r); }
            });
        },
        select: function (event, ui) {
            options.onModelSelected(ui.item.value);
            options.onModelChange();
        },
        minLength: 2,
        classes: { "ui-autocomplete": "AutoExtender AutoExtenderList" }
    });
}

/*
    setUpNewAircraft - sets up the page for editing a NEW aircraft.
   * container - the root container
   * Options - contains identifiers:
     * majorTypeName - name for the input that selects real, anonymous or sim
     * simTypeName - name for the input that selects the specific level of sim
     * tailNumberInput - JQuery for the tail number text input
     * autoCompleteHREF - reference for the tail number auto-completion service
     * autoCompleteProg - JQuery of the progress spinner to show/hide
     * onModelChange - function to call when a model is selected, deselected, or changed.
     * onAircraftSelected - function to call when an existing aircraft is selected
*/
function setUpNewAircraft(container, options) {
    container.find(`input[name='${options.majorTypeName}']`).on("change", options.onModelChange);
    container.find(`input[name='${options.simTypeName}']`).on("change", options.onModelChange);
    options.tailNumberInput.autocomplete({
        source: function (request, response) {
            var params = new Object();
            params.prefixText = request.term;
            params.count = 20;
            params.contextKey = null;
            var d = JSON.stringify(params);
            $.ajax({
                url: options.autoCompleteHREF,
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    options.autoCompleteProg.hide();
                    window.alert(xhr.responseJSON.Message);
                },
                success: function (r) { response(r); }
            });
        },
        select: function (event, ui) {
            return options.onAircraftSelected(event, ui);
        },
        minLength: 2,
        classes: { "ui-autocomplete": "AutoExtender AutoExtenderList" }
    });
}

// New aircraft specific functions
function updateCountry(sender, target) {
    var params = new Object();
    params.prefix = sender.value;
    params.oldTail = target.val();
    var d = JSON.stringify(params);
    $.ajax({
        url: '/logbook/mvc/aircraft/ChangeCountryForTail',
        type: "POST", data: d, dataType: "text", contentType: "application/json",
        error: function (xhr, status, error) { window.alert(xhr.responseText); },
        success: function (response) {
            target.val(response);
        }
    });
}

/*
    setUpExistingAircraft - sets up the page for editing an EXISTING aircraft
    * Options - contains identifiers:
*/
function setUpExistingAircraft(options) {
    window.alert('todo!');
}

// Existing aircraft specific functions
function updateSchedules(idAircraft, targetID) {
    var params = new Object();
    params.idAircraft = idAircraft;
    $.ajax({
        type: "POST",
        data: JSON.stringify(params),
        url: "/logbook/mvc/club/SchedulesForAircraft",
        dataType: "html",
        contentType: "application/json",
        error: function (xhr) { window.alert(xhr.responseText); },
        complete: function (response) { },
        success: function (response) {
            $(targetID).html(response);
        }
    });
}

function updateHighWatermarks(idAircraft, targetID) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/Aircraft/HighWatermarksForAircraft',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) {
                window.alert(xhr.responseText);
            },
            success: function (r) { $(targetID).text(r); }
        });
}

function makeImageDefault(sender, imageClass, key, thumbnail) {
    var params = new Object();
    params.idAircraft = key;
    params.szThumb = thumbnail;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/Aircraft/SetDefaultImage',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseText);
            },
            success: function () {
                var container = $(sender).closest("div.ilItem");
                container.parent().find("img.favoriteIcon").attr("src", "/logbook/images/favoritesm.png");
                container.prependTo(container.parent());
                $(sender).attr("src", "/logbook/images/favoritefilledsm.png");
            }
        });
}

function getMaintenancePage(idAircraft, start, pageSize, target) {
    var params = new Object();
    params.start = start;
    params.pageSize = pageSize;
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/Aircraft/MaintenanceLogTable',
            type: "POST", data: d, dataType: "html", contentType: "application/json",
            error: function (xhr, status, error) { },
            complete: function (response) { },
            success: function (response) {
                target.html(response);
            }
        });
}

function addOilInterval(idAircraft, interval, lastoil, deadlineContainer, maintenanceContainer, postEdit) {
    var params = new Object();
    params.idAircraft = idAircraft;
    params.interval = interval;
    params.curValue = lastoil;
    params.postEdit = postEdit;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/Prefs/AddAircraftOilDeadline',
            type: "POST", data: d, dataType: "html", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseText);
            },
            success: function (response) {
                deadlineContainer.html(response);
                getMaintenancePage(idAircraft, 0, 10, maintenanceContainer);
            }
        });
}

// Images
function deleteAircraftImage(sender, confirmText, imageClass, key, thumbnail) {
    deleteImage(confirmText, imageClass, key, thumbnail, true, function (r) {
        $(sender).parents("div[name='editImage']").hide();
    });
}

function updateAircraftImage(sender, imageClass, key, thumbnail, newComment) {
    updateComment(imageClass, key, thumbnail, newComment, true, function (r) {
        var parent = $(sender).parents("div[name='editImage']");
        parent.find("[name='commentLabel']").text(newComment);
        parent.find("[name='statComment']").show();
        parent.find("[name='dynComment']").hide();
    });
}

// Switch Aircraft 
function switchAircraft(form, target, migrate) {
    form.find("input[name='switchTargetAircraftID']").val(target);
    form.find("input[name='switchMigrateFlights']").val(migrate);
    form.submit();
}

// Admin
function clone(f) {
    $.ajax({
        url: "/logbook/mvc/Aircraft/AdminCloneAircraft",
        type: "POST", data: new FormData(f[0]), dataType: "html", contentType: false, processData: false,
        error: function (xhr, status, error) { window.alert(xhr.responseText); },
        complete: function (response) { },
        success: function (response) {
            window.location.reload();
        }
    });
}