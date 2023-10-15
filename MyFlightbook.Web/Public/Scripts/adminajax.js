/******************************************************
 *
 * Copyright(c) 2022-2023 MyFlightbook LLC
 * Contact myflightbook - at - gmail.com for more information
 *
*******************************************************/
function makeDefault(sender, idAircraft) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/Admin/AdminService.asmx/MakeDefault',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) { sender.disabled = true; }
        });
}

function migrateGeneric(sender, idAircraft, onsuccess) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/Admin/AdminService.asmx/MigrateGeneric',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) { onsuccess(sender); }
        });
}

function migrateSim(sender, idAircraft, onsuccess) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/Admin/AdminService.asmx/MigrateSim',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) { onsuccess(sender); }
        });
}

function toggleLock(sender, idAircraft) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/Admin/AdminService.asmx/ToggleLock',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) { window.alert(response.d ? "Aircraft is LOCKED" : "Aircraft is UNLOCKED"); }
        });
}
function mergeMain(sender, idAircraftToMerge, idTargetAircraft) {
    var params = new Object();
    params.idAircraftToMerge = idAircraftToMerge;
    params.idTargetAircraft = idTargetAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/Admin/AdminService.asmx/MergeAircraft',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) { location.reload(); }
        });
}

function convertOandI(sender, idAircraft) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/Admin/AdminService.asmx/ConvertOandI',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) { document.getElementById(sender).parentElement.parentElement.className = 'handled'; }
        });
}
function trimLeadingN(sender, idAircraft) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/Admin/AdminService.asmx/TrimLeadingN',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) { document.getElementById(sender).parentElement.parentElement.className = 'handled'; }
        });
}

function trimN0(sender, idAircraft) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/Admin/AdminService.asmx/TrimN0',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) { document.getElementById(sender).parentElement.parentElement.className = 'handled'; }
        });
}
function ignorePseudoGeneric(sender, idaircraft) {
    var params = new Object();
    params.idAircraft = idaircraft;
    var d = JSON.stringify(params);
    $.ajax({
        url: '/logbook/Admin/AdminService.asmx/IgnorePseudo',
        type: "POST", data: d, dataType: "json", contentType: "application/json",
        error: function (xhr, status, error) {
            window.alert(xhr.responseJSON.Message);
        },
        complete: function (response) { },
        success: function (response) { document.getElementById(sender).parentElement.parentElement.className = 'handled'; }
    });
}
function viewFlights(idAircraft, tail) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/Admin/AdminService.asmx/ViewFlights',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) {
                var div = $("#pnlFlightContent");
                div.html(response.d);
                div.dialog({ autoOpen: false, closeOnEscape: true, height: 400, width: 350, modal: true, title: "Flights for aircraft " + tail });
                div.dialog("open");
            }
        });
}
