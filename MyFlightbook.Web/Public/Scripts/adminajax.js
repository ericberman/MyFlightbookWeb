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

function unlockUser(sender, szUser) {
    var params = new Object();
    params.szUser = szUser;
    var d = JSON.stringify(params);
    $.ajax(
        {
            url: '/logbook/Admin/AdminService.asmx/UnlockUser',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) {
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
            url: '/logbook/Admin/AdminService.asmx/ResetPasswordForUser',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) {
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
            url: '/logbook/Admin/AdminService.asmx/DeleteUserAccount',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) {
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
            url: '/logbook/Admin/AdminService.asmx/DeleteFlightsForUser',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) {
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
            url: '/logbook/Admin/AdminService.asmx/Disable2FA',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) {
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
            url: '/logbook/Admin/AdminService.asmx/EndowClubCreation',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) { },
            success: function (response) {
                sender.disabled = true;
                window.alert("Club creation endowment bestowed!")
            }
        });
    return false;
}

