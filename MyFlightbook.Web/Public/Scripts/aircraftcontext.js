/******************************************************
 *
 * Copyright(c) 2022 MyFlightbook LLC
 * Contact myflightbook - at - gmail.com for more information
 *
*******************************************************/

function toggleFavorite(idAircraft, fIsActive, uri) {
    var params = new Object();
    params.idAircraft = idAircraft;
    params.fIsActive = fIsActive;
    var d = JSON.stringify(params);

    $.ajax(
        {
            url: uri,
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            async: true,
            error: function (xhr) { window.alert(xhr.responseJSON.Message); },
            complete: function () { },
            success: function () { }
        });
}

function setRole(idAircraft, role, addPICName, ckAddPIC, uri) {
    var params = new Object();
    params.idAircraft = idAircraft;
    params.Role = role;
    params.fAddPICName = addPICName;
    var d = JSON.stringify(params);

    if (role != "PIC")
        ckAddPIC.checked = false;

    $.ajax(
        {
            url: uri,
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            async: true,
            error: function (xhr) { window.alert(xhr.responseJSON.Message); },
            complete: function () { },
            success: function () { }
        });
}