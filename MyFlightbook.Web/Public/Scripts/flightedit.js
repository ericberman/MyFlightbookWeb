/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
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

function aircraftSelected(sender, fHHMM) {
    var lastTail = $("#hdnLastTail");
    var cmbAircraft = $("#cmbAircraft");
    if ($(sender).val() == "") {
        var lastID = lastTail.val();
        $("#cmbAircraft option.inactiveAircraft").show();
        $("#cmbAircraft option.showAllAircraft").hide();
        if (lastID == '' || lastID == '-1')
            cmbAircraft.prop("selectedIndex", 0);
        else
            cmbAircraft.val(lastID);
    } else {
        updateTemplates(-1, fHHMM);
    }
    lastTail.val(cmbAircraft.val());
}

function currentlySelectedAircraft() {
    return $('#cmbAircraft').val();
}

function updatePropertyTuples(newPropTypeID) {
    var props = [];
    $("#pnlProps div.propItemFlow").each(function (index, element) {
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
    var f = validateForm();
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
    var f = validateForm();
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

function addGoogleItem(sender, idx) {
    var f = validateForm();
    if (f.valid()) {
        $("#hdnGPhotoClickedItem").val(idx);
        updatePropertyTuples(-1);
        $.ajax({
            url: "/logbook/mvc/flightedit/AddGooglePhoto",
            type: "POST", data: new FormData(f[0]), dataType: "html", contentType: false, processData: false,
            error: function (xhr, status, error) { window.alert(xhr.responseText); },
            success: function (r) {
                $(sender).parent().parent().parent().hide();
                var photoResult = $("#divGooglePhotoResult");
                photoResult.detach();
                $("#pnlFlightEditorBody").html(r);
                $("#divGooglePhotoResult").append(photoResult);
            }
        });
    }
}

function getGooglePhoto(date, lastResponse) {
    var lastDate = $("#hdnGPhotoLastDate");
    var params = {
        date: date,
        dtLast: lastDate.val(),
        lastResponseJSON: lastResponse
    };
    lastDate.val(date);
    $("#imgGPhotoProg").show();
    $.ajax({
        url: "/logbook/mvc/Image/GetGooglePhotos",
        type: "POST", data: JSON.stringify(params), dataType: "html", contentType: "application/json",
        error: function (xhr, status, error) { window.alert(xhr.responseText); },
        complete: function () { $("#imgGPhotoProg").hide(); },
        success: function (r) {
            $("#divGooglePhotoResult").html(r);
        }
    });
}

// Telemetry and autofill
function deleteTelemetry() {
    var f = validateForm();
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
    var f = validateForm();
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
    var f = validateForm();

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

