/******************************************************
 *
 * Copyright(c) 2022-2025 MyFlightbook LLC
 * Contact myflightbook - at - gmail.com for more information
 *
*******************************************************/
function makeDefault(sender, idAircraft) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/AdminAircraft/MakeDefault',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function () { sender.disabled = true; }
        });
}

function migrateGeneric(sender, idAircraft, onsuccess) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/AdminAircraft/MigrateGeneric',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function () { onsuccess(sender); }
        });
}

function migrateSim(sender, idAircraft, onsuccess) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/AdminAircraft/MigratePsuedoSim',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function () { onsuccess(sender); }
        });
}

function toggleLock(sender, idAircraft) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/AdminAircraft/ToggleLock',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function (b) { window.alert(b ? "Aircraft is LOCKED" : "Aircraft is UNLOCKED"); }
        });
}
function mergeMain(sender, idAircraftToMerge, idTargetAircraft) {
    var params = new Object();
    params.idAircraftToMerge = idAircraftToMerge;
    params.idTargetAircraft = idTargetAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/AdminAircraft/MergeAircraft',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function () { location.reload(); }
        });
}

function convertOandI(sender, idAircraft) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/AdminAircraft/ConvertOandI',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function (response) { document.getElementById(sender).parentElement.parentElement.className = 'handled'; }
        });
}
function trimLeadingN(sender, idAircraft) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/AdminAircraft/TrimLeadingN',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function (response) { document.getElementById(sender).parentElement.parentElement.className = 'handled'; }
        });
}

function trimN0(sender, idAircraft) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/AdminAircraft/TrimN0',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function (response) { document.getElementById(sender).parentElement.parentElement.className = 'handled'; }
        });
}

function ReHyphenate(sender, idAircraft, newTail, successTarget) {
    var params = new Object();
    params.idAircraft = idAircraft;
    params.newTail = newTail;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/AdminAircraft/ReHyphenate',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function (r) { successTarget.text(r); sender.val(r); }
        });
}

function ignorePseudoGeneric(sender, idaircraft) {
    var params = new Object();
    params.idAircraft = idaircraft;
    var d = JSON.stringify(params);
    $.ajax({
        url: '/logbook/mvc/AdminAircraft/IgnorePseudo',
        type: "POST", data: d, dataType: "text", contentType: "application/json",
        error: function (xhr) { window.alert(xhr.responseText); },
        success: function () { document.getElementById(sender).parentElement.parentElement.className = 'handled'; }
    });
}
function viewFlights(idAircraft, tail) {
    var params = new Object();
    params.idAircraft = idAircraft;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/AdminAircraft/ViewFlights',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function (h) {
                var div = $("#pnlFlightContent");
                div.html(h);
                div.dialog({ autoOpen: false, closeOnEscape: true, height: 400, width: 350, modal: true, title: "Flights for aircraft " + tail });
                div.dialog("open");
            }
        });
}

function unlockUser(sender, szUser) {
    var params = new Object();
    params.szUser = szUser;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/Admin/UnlockUser',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function () {
                sender.disabled = true;
                window.alert("User " + szUser + " UNLOCKED");
            }
        });
    return false;
}

function resetPassword(sender, szUserPKID, szUser) {
    var params = new Object();
    params.szPKID = szUserPKID;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/Admin/ResetPasswordForUser',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function () {
                sender.disabled = true;
                window.alert("Password reset and email sent for user " + szUser);
            }
        });
    return false;
}

function deleteUser(sender, szUserPKID, szUser, szEmail) {
    if (!window.confirm("Are you sure you want to DELETE THIS USER?  This action cannot be undone!"))
        return;

    var params = new Object();
    params.szPKID = szUserPKID;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/Admin/DeleteUserAccount',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function () {
                sender.disabled = true;
                window.alert("User " + szUser + " (" + szEmail + ") account deleted");
            }
        });
    return false;
}
function deleteFlightsForUser(sender, szUserPKID, szUser, szEmail) {
    if (!window.confirm("Are you sure you want to delete the FLIGHTS for this user?  This action cannot be undone!"))
        return;

    var params = new Object();
    params.szPKID = szUserPKID;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/Admin/DeleteFlightsForUser',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) {
                window.alert(xhr.responseText);
            },
            success: function () {
                sender.disabled = true;
                window.alert("FLIGHTS for user " + szUser + " (" + szEmail + ") successfully deleted");
            }
        });
    return false;
}

function disable2FAForUser(sender, szUserPKID, szUser) {
    var params = new Object();
    params.szPKID = szUserPKID;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/Admin/Disable2FA',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function () {
                sender.disabled = true;
                window.alert("2fa turned off for user " + szUser);
            }
        });
    return false;
}

function endowClubCreation(sender, szUserPKID) {
    var params = new Object();
    params.szPKID = szUserPKID;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/Admin/EndowClubCreation',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function () {
                sender.disabled = true;
                window.alert("Club creation endowment bestowed!")
            }
        });
    return false;
}

function adminFixSignature(idFlight, forceValid) {
    var params = new Object();
    params.idFlight = idFlight;
    params.fForceValid = forceValid;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/mvc/Admin/FixSignature',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr) { window.alert(xhr.responseText); },
            success: function () { window.location = window.location; }
        });
}
