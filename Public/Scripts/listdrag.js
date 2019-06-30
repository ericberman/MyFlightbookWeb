/******************************************************
 *
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

// Creates a drag/drop between two lists.  
// Needs the client id of a text field and 2 buttons (all hidden from display) to respond.
// The text field contains the value, and the buttons initiates the server response to the drag/drop
function listDragger(idTxt, idBtnLeft, idBtnRight) {
    this.longTouchTimer = null;
    this.idTxt = idTxt;
    this.idBtnLeft = idBtnLeft;
    this.idBtnRight = idBtnRight;

    this.allowDrop = function (ev) {
        ev.preventDefault();
    };

    this.startLeftTouch = function (id) {
        if (this.longTouchTimer !== null)
            clearTimeout(this.longTouchTimer);
        var self = this;
        this.longTouchTimer = setTimeout(function () {
            self.clickRight(id);
        }, 1000);
    };

    this.startRightTouch = function (id) {
        if (this.longTouchTimer !== null)
            clearTimeout(this.longTouchTimer);
        var self = this;
        this.longTouchTimer = setTimeout(function () {
            self.clickLeft(id);
        }, 1000);
    };

    this.resetTouch = function () {
        if (this.longTouchTimer !== null)
            clearTimeout(this.longTouchTimer);
        this.longTouchTimer = null;
    };

    this.drag = function (ev, id) {
        ev.dataTransfer.setData("Text", id.toString());
    };

    this.clickRight = function (id) {
        document.getElementById(this.idTxt).value = id;
        document.getElementById(this.idBtnRight).click();
    };

    this.clickLeft = function (id) {
        document.getElementById(this.idTxt).value = id;
        document.getElementById(this.idBtnLeft).click();
    };

    this.rightListDrop = function (ev) {
        ev.preventDefault();
        this.clickRight(ev.dataTransfer.getData("Text"));
    };

    this.leftListDrop = function (ev) {
        ev.preventDefault();
        this.clickLeft(ev.dataTransfer.getData("Text"));
    };
}