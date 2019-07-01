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
        ev.dataTransfer.setData("text", id.toString());
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
        this.clickRight(ev.dataTransfer.getData("text"));
    };

    this.leftListDrop = function (ev) {
        ev.preventDefault();
        this.clickLeft(ev.dataTransfer.getData("text"));
    };

    this.moveProp = function(ev, prefix, idTargetList, idSrcSet, idDstSet) {
        ev.preventDefault();
        const value = ev.dataTransfer.getData("text");
        const valInt = parseInt(value);

        const srcObj = document.getElementById(idSrcSet);
        const dstObj = document.getElementById(idDstSet);

        const setSrc = new Set(JSON.parse(srcObj.value));
        const setDst = new Set(JSON.parse(dstObj.value));

        // Detect drop on source.  No-op if this happens.
        if (setDst.has(valInt))
            return;

        setDst.add(valInt);
        setSrc.delete(valInt);

        srcObj.value = JSON.stringify(Array.from(setSrc));
        dstObj.value = JSON.stringify(Array.from(setDst));

        const draggedObject = document.getElementById(prefix + value);
        idTargetList.appendChild(draggedObject);

        // Sort the items and then bring it into view
        var toSort = idTargetList.children;
        toSort = Array.prototype.slice.call(toSort, 0);
        toSort.sort(function (a, b) {
            return a.innerHTML.toUpperCase().localeCompare(b.innerHTML.toUpperCase());
        });
        for (var i = 0; i < toSort.length; i++)
            idTargetList.appendChild(toSort[i]);
    };
}