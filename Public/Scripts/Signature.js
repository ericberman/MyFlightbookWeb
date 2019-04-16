
/******************************************************
 * 
 * Copyright (c) 2015-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/
/*
    Thanks to http://www.codeproject.com/Articles/432675/Building-a-Signature-Control-Using-Canvas?msg=4327580#xx4327580xx for the basic outline of how to do this.
    Had to do a bunch of work to get it to work with touch, and I added the work as well to send the strokes rather than the raw data.
    CPOL license (http://www.codeproject.com/info/cpol10.aspx).  So below is a derivative work.
*/
(function (ns) {
    "use strict";

    ns.SignatureControl = function (options) {
        var label = options && options.label || "Signature",
            cWidth = options && options.width || "300px",
            cHeight = options && options.height || "300px",
            imgDataControlId = options && options.imgDataControlId || "",
            btnClearId,
            canvas,
            ctx;

        function initControl() {
            createControlElements();
            wireButtonEvents();
            canvas = document.getElementById("signatureCanvas");
            canvas.addEventListener("touchstart", touchStart, false);
            canvas.addEventListener("touchend", touchEnd, false);
            canvas.addEventListener("mousedown", mouseDown, false);
            canvas.addEventListener("mouseup", mouseUp, false);
            ctx = canvas.getContext("2d");
            ctx.strokeStyle = '#0000ff';
            ctx.lineWidth = 2;
        }

        function createControlElements() {
        }

        function mouseDown(evt) {
            updateStatus("mouse down");
            ctx.beginPath();
            ctx.moveTo(evt.offsetX, evt.offsetY);
            canvas.addEventListener("mousemove", paint, false);
        }

        function mouseUp(evt) {
            canvas.removeEventListener("mousemove", paint);
            paint(evt);
            setData();
            updateStatus("mouse up");
        }

        function touchStart(evt) {
            updateStatus("touch start");
            evt.preventDefault();
            ctx.beginPath();
            ctx.moveTo(evt.offsetX, evt.offsetY);
            canvas.addEventListener("touchmove", paintTouch, false);
        }

        function touchEnd(evt) {
            canvas.removeEventListener("touchmove", paintTouch);
            paintTouch(evt);
            updateStatus("touch end");
            setData();
        }

        function paint(evt) {
            updateStatus("paint: " + evt.offsetX + ", " + evt.offsetY);
            ctx.lineTo(evt.offsetX, evt.offsetY);
            ctx.stroke();
        }

        function paintTouch(evt) {
            evt.preventDefault();
            var touches = evt.changedTouches;
            var lastTouch = touches[touches.length - 1];
            var x = lastTouch.pageX - canvas.offsetLeft;
            var y = lastTouch.pageY - canvas.offsetTop;
            updateStatus("paintTouch: " + x + ", " + y);
            ctx.lineTo(x, y);
            ctx.stroke();
        }

        function wireButtonEvents() {
            var btnClear = document.getElementById("btnClear");
            btnClear.addEventListener("click", function () {
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                var hdnControl = document.getElementById(imgDataControlId);
                if (hdnControl)
                    hdnControl.value = "";
            }, false);
        }

        function getSignatureImage() {
            return ctx.getImageData(0, 0, canvas.width, canvas.height).data;
        }

        function updateStatus(s) {
            // var item = document.getElementById("statusText");
            // item.innerText = s + ' width:' + ctx.lineWidth;
        }

        function setData() {
            var hdnControl = document.getElementById(imgDataControlId);
            if (hdnControl) {
                var durl = canvas.toDataURL("image/png");
                hdnControl.value = durl;
            }
        }

        return {
            init: initControl,
            getSignatureImage: getSignatureImage
        };
    }
})(this.ns = this.ns || {});