/******************************************************
 * 
 * Copyright (c) 2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

function setImg(src, idImg, idDivViewImg, idDismiss) {
    var img = document.getElementById(idImg);
    img.onload = function () {
        viewImg(this, idDivViewImg);
    }
    img.src = src;

    var dismiss = document.getElementById(idDismiss);
    dismiss.onclick = function () {
        dismissImg(idDivViewImg);
    }
}

function viewImg(img, idDivViewImg) {
    var maxFactor = 0.95;
    var xRatio = img.naturalWidth / window.innerWidth;
    var yRatio = img.naturalHeight / window.innerHeight;
    var maxRatio = (xRatio > yRatio) ? xRatio : yRatio;
    if (maxRatio > maxFactor) {
        img.width = maxFactor * (img.naturalWidth / maxRatio);
        img.height = maxFactor * (img.naturalHeight / maxRatio);
    }
    else {
        img.width = img.naturalWidth;
        img.height = img.naturalHeight;
    }
    var div = $("#" + idDivViewImg);
    div.dialog({ autoOpen: false, closeOnEscape: true, height: img.height, width: img.width, modal: true, resizable: false, draggable: false, title: null });
    div.dialog("open");
    $(".ui-dialog-titlebar").hide();    // hide the title bar
    $(".ui-dialog .ui-dialog-content").css("padding", "0");
    $(".ui-dialog").css("padding", "0");
    $(".ui-widget.ui-widget-content").css("border", "none");
    $(".ui-widget.ui-widget-content").css("border-radius", "0");
    $(".modalpopup").css("border-radius", "0");
    div.width(img.width);
    div.height(img.height);
}

function dismissImg(idDivViewImg) {
    $("#" + idDivViewImg).dialog("close");
}

function dismissDlg(idDlg) {
    $(idDlg).dialog("close");
}

function showModalById(id, szTitle, width) {
    // the dialog is placed outside of the main form, so asp.net postbacks get lost.  Thus we append these to the parent form.
    $("#" + id).dialog({ autoOpen: true, closeOnEscape: true, width: (width || 400), modal: true, title: szTitle || "" }).parent().appendTo(jQuery("form:first"));
}

function convertFdUpJsonDate(fdUpDate) {
    return new Date(parseInt(fdUpDate.replace("/Date(", "").replace(")/", "")));
}