/******************************************************
 *
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/* 
    Creates a drag/drop between two lists.
    options - contains relevant pieces
      * divLeft - the jquery div containing the left items (all spans with text and a hidden control containing the value)
      * divRight - the jquery div containing the right items
      * onDropLeft - function to call whan an item from the right is dropped on the left; parameter is the the dropped element (jquery object) and the value 
      * onDropRight - function to call when an item from the left is dropped on the right
*/
function listDraggerNew(options) {
   this.initdragItem = function (sp, options) {
        sp.addClass("draggableItem");
        sp.attr("draggable", true);

        var value = sp.children('input[type="hidden"]').val();
        sp.on("dragstart", { val: value, itemID: sp.attr("id") }, function (e) {
            e.originalEvent.dataTransfer.setData("val", e.data.val);
            e.originalEvent.dataTransfer.setData("itemID", e.data.itemID);
        });

        sp.on("drop", { val: value, itemID: sp.attr("id") }, function (e) {
            var srcItem = $("#" + e.originalEvent.dataTransfer.getData("itemID"));
            srcItem.insertBefore(sp);
            if ($(this).closest("#" + options.divLeft[0].id).length > 0)
                options.onDropLeft(srcItem, e.originalEvent.dataTransfer.getData("val"));
            else
                options.onDropRight(srcItem, e.originalEvent.dataTransfer.getData("val"));

            e.preventDefault();
            e.stopPropagation();
            return false;
        });
        sp.on("dragover", function (e) {
            e.preventDefault();
        });
    }

    this.init = function () {
        var leftFields = options.divLeft.children('span');
        var rightFields = options.divRight.children('span');
        var pthis = this;

        leftFields.each(function () {
            pthis.initdragItem($(this), options);
        });

        rightFields.each(function () {
            pthis.initdragItem($(this), options);
        });

        options.divLeft.addClass("dragTarget");
        options.divLeft.on("dragover", function (e) {
            e.preventDefault();
        });
        options.divLeft.on("drop", function (e) {
            var srcItem = $("#" + e.originalEvent.dataTransfer.getData("itemID"));
            srcItem.appendTo(options.divLeft);
            options.onDropLeft(srcItem, e.originalEvent.dataTransfer.getData("val"));
            e.preventDefault();
        });
        options.divRight.addClass("dragTarget");
        options.divRight.on("dragover", function (e) {
            e.preventDefault();
        });
        options.divRight.on("drop", function (e) {
            var srcItem = $("#" + e.originalEvent.dataTransfer.getData("itemID"));
            srcItem.appendTo(options.divRight);
            options.onDropRight(srcItem, e.originalEvent.dataTransfer.getData("val"));
            e.preventDefault();
        });
    }

    this.init();
}