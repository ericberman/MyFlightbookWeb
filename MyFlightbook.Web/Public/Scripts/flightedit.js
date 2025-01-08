/******************************************************
 * 
 * Copyright (c) 2024-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

// Properties and templates
function updateTemplates(newPropTypeID, fHHMM) {
    updatePropertyTuples(newPropTypeID);
    var params = new Object();
    params.idFlight = $("#hdnIDFlight").val();
    params.szTargetUser = $("#hdnTargetUser").val();
    params.propTuples = $("#hdnPropTuples").val();
    params.fHHMM = fHHMM;
    params.fStripDefault = newPropTypeID <= 0;
    params.idAircraft = parseInt($("#cmbAircraft").val());
    params.dtDefault = $("#txtDate").val();
    var rgTemplates = [];
    $("input[name='activeTemplateIDs']:checked").each(function (index, element) {
        rgTemplates.push(parseInt(element.value));
    });
    params.activeTemplateIDs = rgTemplates;

    $("#pnlUpdatePropProgress").show();
    var d = JSON.stringify(params);
    $.ajax({
        url: '/logbook/mvc/flightedit/UpdatePropset',
        type: "POST", data: d, dataType: "html", contentType: "application/json",
        error: function (xhr, status, error) { window.alert(xhr.responseText); },
        complete: function (response) { $("#pnlUpdatePropProgress").hide(); },
        success: function (response) {
            $("#divPropSet").html(response);
        }
    });
}

function aircraftSelected(sender, fHHMM, allAircraft) {
    var lastTail = $("#hdnLastTail");
    var cmbAircraft = $(sender);
    if (cmbAircraft.val() == "") {
        var lastID = lastTail.val();
        cmbAircraft.find("option").remove();
        allAircraft.forEach((ac) => {
            var opt = $("<option></option>");
            opt.val(ac.v);
            opt.text(ac.d);
            cmbAircraft.append(opt);
        });
        if (lastID == '' || lastID == '-1')
            cmbAircraft.prop("selectedIndex", 0);
        else
            cmbAircraft.val(lastID);
    } else {
        // Issue #1269 - remove all active templates
        $("input[name='activeTemplateIDs']:checked").each(function (index, element) {
            element.checked = false;
        });
        updateTemplates(-1, fHHMM);
    }
    lastTail.val(cmbAircraft.val());
}

function currentlySelectedAircraft() {
    return $('#cmbAircraft').val();
}

function currentRouteOfFlight() {
    return $("#txtRoute").val();
}

function updatePropertyTuples(newPropTypeID) {
    var props = [];
    $("div.propItemFlow").each(function (index, element) {
        var inputs = $(element).find("input");
        var prop = new Object();
        prop.PropID = inputs[0].value;
        prop.PropTypeID = inputs[1].value;
        var vEle = $(inputs[2]);
        var v = vEle.val();
        prop.ValueString = vEle.is(":checkbox") ? JSON.stringify(vEle[0].checked) : v;
        props.push(prop);
    });

    if (newPropTypeID > 0) {
        var newProp = new Object();
        newProp.PropID = -1;
        newProp.PropTypeID = newPropTypeID;
        newProp.ValueString = "";
        props.push(newProp);
    }

    $("#hdnPropTuples").val(JSON.stringify(props));
}

function toggleSearchBox() {
    var divFilter = $("#divPropFilter");
    if (divFilter.is(":hidden")) {
        divFilter.show();
        $("#txtFilter").focus();
    }
    else {
        divFilter.hide();
    }
}

// Video references
function deleteVideoRef(id) {
    var f = $("#frmEditFlight");
    $("#hdnVidToDelete").val(id);
    $.ajax({
        url: '/logbook/mvc/flightedit/DeleteVideoRef',
        type: "POST", data: new FormData(f[0]), dataType: "html", contentType: false, processData: false,
        error: function (xhr, status, error) { window.alert(xhr.responseText); },
        complete: function (response) { },
        success: function (response) {
            $("#pnlEmbeddedVideo").html(response);
        }
    });
}

function addVideoRef(szComment, szVidRef) {
    var f = $("#frmEditFlight");
    $.ajax({
        url: '/logbook/mvc/flightedit/AddVideoRef',
        type: "POST", data: new FormData(f[0]), dataType: "html", contentType: false, processData: false,
        error: function (xhr, status, error) { $("#lblVideoError").text(xhr.responseText); },
        complete: function (response) { },
        success: function (response) {
            $("#pnlEmbeddedVideo").html(response);
        }
    });
}

// Image mgmt
function deleteFlightImage(sender, confirmText, imageClass, key, thumbnail) {
    deleteImage(confirmText, imageClass, key, thumbnail, true, function (r) {
        $(sender).parents("div[name='editImage']").hide();
    });
}
function updateFlightImage(sender, imageClass, key, thumbnail, newComment) {
    updateComment(imageClass, key, thumbnail, newComment, true, function (r) {
        var parent = $(sender).parents("div[name='editImage']");
        parent.find("[name='commentLabel']").text(newComment);
        parent.find("[name='statComment']").show();
        parent.find("[name='dynComment']").hide();
    });
}

function getGooglePhotoNew() {
    $("#divGooglePhotoResult").text("");
    $("#gPhotoPrg").show();
    var params = {};
    $.ajax({
        url: "/logbook/mvc/oAuth/GooglePhotoPickerSession",
        type: "POST", data: JSON.stringify(params), dataType: "json", contentType: "application/json",
        error: function (xhr) {
            window.alert(xhr.responseText);
            $("#gPhotoPrg").hide();
        },
        success: function (r) {
            window.open(r.sess.pickerHref, "_blank");
            setTimeout(function () { pollGS(r.sess, r.token); }, googleTimeToMS(r.sess.pollingConfig.pollInterval));
        }
    });
}

function googleTimeToMS(s) {
    return (s == "") ? 3000 : parseInt(s.slice(0, -1)) * 1000;
}

function pollGS(sess, token) {
    $.ajax({
        url: "https://photospicker.googleapis.com/v1/sessions/" + sess.id,
        beforeSend: function (xhr) { xhr.setRequestHeader('Authorization', 'Bearer ' + token); },
        type: "GET", contentType: "text",
        error: function (xhr, a, b) { $("#gPhotoPrg").hide(); },
        success: function (newsession) {
            if (newsession.mediaItemsSet) {
                const lstTarget = new URL("https://photospicker.googleapis.com/v1/mediaItems");
                lstTarget.searchParams.append("sessionId", newsession.id);
                lstTarget.searchParams.append("pageSize", 10);

                $.ajax({
                    url: lstTarget,
                    type: "GET", contentType: "text",
                    beforeSend: function (xhr) { xhr.setRequestHeader('Authorization', 'Bearer ' + token); },
                    error: function (xhr, a, b) { window.alert(xhr.responseText); $("#gPhotoPrg").hide(); },
                    success: function (items) {
                        $("#hdnGPhotoLastResponse").val(JSON.stringify(items));
                        var f = $("#frmEditFlight");
                        $.ajax({
                            url: '/logbook/mvc/flightedit/AddGooglePhotosNew',
                            type: "POST", data: new FormData(f[0]), dataType: "html", contentType: false, processData: false,
                            error: function (xhr) { $("#divGooglePhotoResult").text(xhr.responseText); },
                            success: function (r) {
                                $("#pnlFlightEditorBody").html(r);
                            }
                        });
                    }
                });
            } else {
                setTimeout(function () { pollGS(newsession, token); }, googleTimeToMS(newsession.pollingConfig.pollInterval));
            }
        }
    });
}

// Telemetry and autofill
function deleteTelemetry() {
    var f = $("#frmEditFlight");
    if (f.valid()) {
        updatePropertyTuples(-1);
        $.ajax({
            url: "/logbook/mvc/flightedit/DeleteTelemetry",
            type: "POST", data: new FormData(f[0]), dataType: "html", contentType: false, processData: false,
            error: function (xhr, status, error) { showError.text(xhr.responseText); },
            complete: function (response) { },
            success: function (r) {
                $("#pnlFlightEditorBody").html(r);
            }
        });
    }
}

function autoFill() {
    var f = $("#frmEditFlight");
    if (f.valid()) {
        updatePropertyTuples(-1);
        $("#imgAutofillPrg").show();
        $.ajax({
            url: "/logbook/mvc/flightedit/AutoFillFlight",
            type: "POST", data: new FormData(f[0]), dataType: "html", contentType: false, processData: false,
            error: function (xhr, status, error) { showError.text(xhr.responseText); },
            complete: function (response) { $("imgAutofillPrg").hide(); },
            success: function (r) {
                $("#pnlFlightEditorBody").html(r);
            }
        });
    }
}

// Form Submission
function showError(e) {
    var err = $("#lblFlightError");
    err.text(e);
    err.show();
}

function postFlightWithAction(f, onSuccess, resultType, url) {
    $.ajax({
        url: url || "/logbook/mvc/flightedit/CommitFlight",
        type: "POST", data: f, dataType: resultType || "text", contentType: false, processData: false,
        error: function (xhr, status, error) { showError(xhr.responseText); },
        success: function (r) {
            if (onSuccess)
                onSuccess(r);
        }
    });
}

function sbmtFlightFrm(requestedAction, confirmFunc) {
    document.activeElement.blur();
    var f = $("#frmEditFlight");

    if (f.valid() && (confirmFunc === undefined || confirmFunc())) {
        // Properties are not named and need to be distilled, so put them into a tuples list.
        updatePropertyTuples(-1);
        requestedAction(new FormData(f[0]));
    }
    return false;
}

function sbmtFlightFrmNoValidate(requestedAction) {
    var f = $("#frmEditFlight");
    updatePropertyTuples(-1);
    requestedAction(new FormData(f[0]));
}

function addAircraft(target) {
    var f = $("#frmEditFlight");
    updatePropertyTuples(-1);
    $.ajax({
        url: "/logbook/mvc/FlightEdit/SaveFlightToSession",
        type: "POST", data: new FormData(f[0]), dataType: "text", contentType: false, processData: false,
        error: function (xhr, status, error) { window.alert(xhr.responseText); },
        success: function (r) {
            window.location = target;
        }
    });
}

// Approach helper
function populateApproachHelper(containerID, cmbType, cmbTypeSuffix, cmbRnwy, cmbRnwySuffix, txtApt, txtRte) {
    for (i = 1; i <= 36; i++) {
        cmbRnwy.append($("<option />").val(i).text(i));
    }

    ['', 'L', 'C', 'R'].forEach((s) => cmbRnwySuffix.append($("<option />").val(s).text(s)));

    ['', '-A', '-B', '-C', '-D', '-V', '-W', '-X', '-Y', '-Z'].forEach((s) => cmbTypeSuffix.append($("<option />").val(s).text(s)));

    ['CONTACT', 'COPTER', 'GCA', 'GLS', 'ILS', 'ILS (Cat I)', 'ILS (Cat II)', 'ILS (Cat III)', 'ILS/PRM',
        'JPLAS', 'LAAS', 'LDA', 'LOC', 'LOC-BC', 'MLS', 'NDB', 'OSAP', 'PAR', 'RNAV/GPS', 'RNAV/GPS (RNP)', 'SDF', 'SRA/ASR',
        'TACAN', 'TYPE1', 'TYPE2', 'TYPE3', 'TYPE4', 'TYPEA', 'TYPEB', 'VISUAL', 'VOR', 'VOR/DME', 'VOR/DME-ARC'].forEach((s) =>
            cmbType.append($("<option />").val(s).text(s)));

    txtApt.autocomplete({
        minLength: 2,
        classes: { "ui-autocomplete": "AutoExtender AutoExtenderList" },
        appendTo: containerID,
        source: function (request, response) {
            var words = txtRte.val().toUpperCase().match(/\w+/g) ?? [];
            var term = request.term.toUpperCase();
            var result = words.filter((s) => s.startsWith(term));
            response(result);
        }
    });
}

function addAppchDesc() {
    sbmtFlightFrm((f) => { postFlightWithAction(f, function (r) { $("#pnlFlightEditorBody").html(r); }, "html", '/logbook/mvc/FlightEdit/AddApproachDesc'); });
}

// Pending flights
function deletePendingFlight(pfID) {
    $("#hdnPendingID").val(pfID);
    var f = $("#frmActPending").serialize();
    $.ajax({
        url: '/logbook/mvc/flightedit/deletependingflight',
        type: "POST", data: f, dataType: "text", contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
        error: function (xhr, status, error) { window.alert(xhr.responseText); },
        complete: function () { },
        success: function () {
            window.location = window.location;
        }
    });
    return false;
}

function deleteAllPendingFlights() {
    $("#hdnPendingID").val('');
    var f = $("#frmActPending").serialize();
    $.ajax({
        url: '/logbook/mvc/flightedit/DeleteAllPendingFlights',
        type: "POST", data: f, dataType: "text", contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
        error: function (xhr, status, error) { window.alert(xhr.responseText); },
        complete: function () { },
        success: function () {
            window.location = window.location;
        }
    });
    return false;
}

function cancelEdit() {
    window.location = '/logbook/mvc/FlightEdit/Pending';
    return false;
}

function flightSaved() {
    window.location = '/logbook/mvc/FlightEdit/Pending';
    return false;
}

function navigateToPendingPage(page, pageSize, sortField) {
    var params = new Object();
    params.offset = page * pageSize;
    params.pageSize = pageSize;

    var hdnsortField = $("#hdnPendingSortField");
    var hdnsortdir = $("#hdnLastPendingSortDir");
    hdnsortdir.val(sortField == hdnsortField.val() && hdnsortdir.val() == "Descending" ? "Ascending" : "Descending");
    hdnsortField.val(sortField ?? hdnsortField.val());
    params.sortField = hdnsortField.val();
    params.sortDirection = hdnsortdir.val();

    $("#prgPendingPager").show();
    $.ajax({
        url: '/logbook/mvc/flightedit/pendingflightsinrange',
        type: "POST", data: JSON.stringify(params), dataType: "html", contentType: "application/json",
        error: function (xhr, status, error) { window.alert(xhr.responseText); },
        complete: function () { $("#prgPendingPager").hide(); },
        success: function (r) {
            $("#divPendingTable").html(r);
        }
    });
}