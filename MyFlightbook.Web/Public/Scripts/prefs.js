/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 * 
 * Contains functions for managing prefs in javascript
 *
*******************************************************/

/*
   Creates a prefs editor that uses a web service
   * localPrefEndPoint - contains the fully-qualified path to the webservice endpoint for local preferences
   * options - contains identifiers of elements that matter for this
     * divUnused - the id of the div containing unused core fields
     * divCoreFields - the id of the div containing used core fields
     * ckSIC - the id of the checkbox that indicates whether the SIC field is shown
     * ckCFI - the id of the checkbox that indicates whether the Insturctor field is shown
     * defaultFields - a jquery object with the set of all core fields
   * 
   * NOTE: many of the ids are hadcoded at this point.
*/
function prefsFlightEntryEditor(localPrefEndPoint, options) {
    // set up drag/drop
    var pe = this;
    this.setLocalPref = function(name, value) {
        var params = new Object();
        params.prefName = name;
        params.prefValue = value;
        var d = JSON.stringify(params);
        $.ajax({
            url: localPrefEndPoint,
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function () { },
            success: function () { }
        });
    }
    this.setLocalPrefValue = function(name, sender) {
        this.setLocalPref(name, sender.value);
    }

    this.setLocalPrefChecked = function(name, sender) {
        this.setLocalPref(name, sender.checked);
    }

    this.move = function (event, append) {
        var idx = parseInt(event.originalEvent.dataTransfer.getData("Index"));
        var id = event.originalEvent.dataTransfer.getData("ID");
        var ele = document.getElementById(id);
        var targ = document.getElementById(append ? options.divCoreFields : options.divUnused);
        targ.insertBefore(ele, targ.firstElementChild);
        var perms = this.getPermutations();

        if (append && perms.indexOf(idx) < 0) {
            perms.splice(0, 0, idx);
            this.setPermutations(perms);
        }
        else {
            perms = perms.filter(function (s) { return s != idx; });
            this.setPermutations(perms);
        }

        event.preventDefault();
        event.stopPropagation();
        return false;
    }

    this.getPermutations = function() {
        var elePerms = document.getElementById(options.hdnPermutations);
        return (elePerms.value === "") ? [] : JSON.parse(elePerms.value);
    }

    this.setPermutations = function (perms) {
        var elePerms = document.getElementById(options.hdnPermutations);
        elePerms.value = JSON.stringify(perms);
        this.setLocalPref("FIELDDISPLAY", elePerms.value);
    }

    this.updatePermuation = function(insert, before) {
        var perms = this.getPermutations();

        // Remove "insert" if it's in the list
        var permsFiltered = perms.filter(function (s) { return s != insert; });

        if (before !== null) {
            // Now add it before "before", if before is in the arrawy
            var i = permsFiltered.indexOf(before);
            permsFiltered.splice(i >= 0 ? i : 0, 0, insert);
        }
        this.setPermutations(permsFiltered);
    }

    this.fullDefaults = function() {
        var perms = new Array(0, 1, 2, 3, 4, 5);
        if ($('#' + options.ckCFI)[0].checked)
            perms.push(6);
        if ($('#' + options.ckSIC)[0].checked)
            perms.push(7);
        perms.push(8);
        perms.push(9);
        return perms;
    }

    this.resetPermutations = function () {
        var perms = this.fullDefaults();
        options.defaultFields.each(function () {
            $(this).appendTo($("#" + options.divCoreFields));
        });
        this.setPermutations(perms);
    }

    this.initPermutations = function (perms) {
        if (perms.length == 0)
            perms = this.fullDefaults();

        var pthis = this;
        var i = 0;
        srcFields = options.defaultFields;
        var coreContainer = $("#" + options.divCoreFields);
        var unusedContainer = $("#" + options.divUnused);

        srcFields.each(function () {
            var sp = $(this);
            sp.addClass("draggableItem");
            sp.attr("draggable", true);
            sp.appendTo(unusedContainer);
            sp.on("dragstart", { index: i, id: sp.attr("id") }, function (e) {
                e.originalEvent.dataTransfer.setData("ID", e.data.id);
                e.originalEvent.dataTransfer.setData("Index", e.data.index);
            });

            sp.on("drop", { index: i, id: sp.attr("id") }, function (e) {
                var idx = parseInt(e.originalEvent.dataTransfer.getData("Index"));
                var id = e.originalEvent.dataTransfer.getData("ID");
                var ele = document.getElementById(id);
                var divUnused = document.getElementById(options.divUnused);
                e.target.parentNode.insertBefore(ele, e.target);
                if (e.target.parentNode === divUnused)
                    pthis.move(e, false);
                else
                    pthis.updatePermuation(idx, e.data.index);
                e.preventDefault();
                e.stopPropagation();
                return false;
            });
            sp.on("dragover", function (e) {
                e.preventDefault();
            });
            i++;
        });

        perms.forEach((i, idx) => {
            $(srcFields[i]).appendTo(coreContainer);
        });

        coreContainer.addClass("dragTarget");
        unusedContainer.addClass("dragTarget");

        coreContainer.on("dragover", function (e) {
            e.preventDefault();
        });
        coreContainer.on("drop", function (e) {
            pthis.move(e, true);
        });
        unusedContainer.on("dragover", function (e) {
            e.preventDefault();
        });
        unusedContainer.on("drop", function (e) {
            pthis.move(e, false);
        });

        this.setPermutations(perms);
    }
}

/*
 Creates a prefs editor for flight coloring
  * baseEndpoint - resolved base for the webservices calls here 
  * options - identifiers that matter here:
    * pathSampleID = identifier of the path sample
    * routeSampleID = identifier of the route sample
    * defPathColor = default path color
    * defRouteColor = default color for routes
*/
function prefsFlightColorEditor(baseEndpoint, options) {
    this.qNameForElement = function(e) {
        return $(e).siblings("input[type='hidden']")[0].value;
    }

    this.sampleForElement = function (e) {
        return $(e).parent().parent().find("div[name='sample']");
    }

    this.setColor = function(sender) {
        this.sampleForElement(sender).css("background-color", sender.value);
        var params = new Object();
        params.queryName = this.qNameForElement(sender);
        params.color = sender.value;
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: baseEndpoint + "/SetColorForQuery",
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.responseJSON.Message);
                },
                complete: function (response) { },
                success: function (response) { }
            });
    }

    this.clearColor = function (sender) {
        this.sampleForElement(sender).css("background-color", "");
        var params = new Object();
        params.queryName = this.qNameForElement(sender);
        params.color = null;
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: baseEndpoint + "/SetColorForQuery",
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.responseJSON.Message);
                },
                complete: function (response) { },
                success: function (response) {
                    $(sender).siblings("span").css("background-color", "");
                }
            });
        return false;
    }

    this.setMapColors = function() {
        var params = new Object();
        params.pathColor = $('#' + options.pathSampleID)[0].value;
        params.routeColor = $('#' + options.routeSampleID)[0].value;
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: baseEndpoint + "/SetMapColors",
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.responseJSON.Message);
                },
                complete: function (response) { },
                success: function (response) { }
            });
    }

    this.resetMapColors = function(sender) {
        var params = new Object();

        params.pathColor = "";
        params.routeColor = "";
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: baseEndpoint + '/SetMapColors',
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.responseJSON.Message);
                },
                complete: function (response) { },
                success: function (response) {
                    $("#" + options.pathSampleID).val(options.defPathColor);
                    $("#" + options.routeSampleID).val(options.defRouteColor);
                }
            });
    }
}

/*
  Creates a property tempalte editor.  Currently no options
*/
function prefsPropsTemplateEditor(baseEndpoint, options) {
    this.editBlockedProperty = function (idPropType, fAllow) {
        var params = new Object();
        params.id = idPropType;
        params.fAllow = fAllow;
        var d = JSON.stringify(params);
        $.ajax({
            url: baseEndpoint + "EditPropBlockList",
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseText);
            },
            complete: function () { },
            success: function () { }
        });
    }

    this.editTemplate = function (containerID, idTemplate, fCopy) {
        var params = new Object();
        params.idTemplate = idTemplate;
        params.containerID = containerID;
        params.fCopy = fCopy;
        var d = JSON.stringify(params);
        $.ajax({
            url: baseEndpoint + "PropTemplateEditor",
            type: "POST", data: d, dataType: "html", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseText);
            },
            complete: function () { },
            success: function (result) {
                $("#" + containerID).html(result);
                showModalById(containerID, '', 800);
            }
        });
    }

    this.commitPropTemplate = function (form, onError, onSuccess) {
        if (!form.valid())
            return false;
        var f = form.serialize();
        $.ajax({
            url: baseEndpoint + "CommitPropTemplate",
            type: "POST", data: f, dataType: "text", contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
            error: function (xhr, status, error) { onError(xhr.responseText); },
            success: function () {
                onSuccess();
            }
        });
    }

    this.setPropTemplateFlags = function (idTemplate, fPublic, fDefault) {
        var params = new Object();
        params.idTemplate = idTemplate;
        params.fPublic = fPublic;
        params.fDefault = fDefault;
        var d = JSON.stringify(params);
        $.ajax({
            url: baseEndpoint + "SetTemplateFlags",
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseText);
            },
        });
    }

    this.deletePropTemplate = function (form, confirmText, onError, onSuccess) {
        if (confirm(confirmText)) {
            var f = form.serialize();
            $.ajax({
                url: baseEndpoint + "DeletePropTemplate",
                type: "POST", data: f, dataType: "text", contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
                error: function (xhr, status, error) { onError(xhr.responseText); },
                success: function () {
                    onSuccess();
                }
            });
        }
    }
}

/*
 Creates a prefs editor for editing/managing custom currencies.
  * baseEndpoint - resolved base for the webservices calls here
  * options - 
    * deleteConfirmation - the message to display to confirm deletion
    * onCancel - lambda called when cancel is clicked
    * onCommit - lambda called when currency is committed
    * onEdit - lambda called with the html passed of an editor for the specified currency
    * errorID - the id of a span or div that can display an error message
*/
function prefsCustCurrencyEditor(baseEndpoint, options) {
    this.deleteCustCurrency = function(sender, form) {
        if (confirm(options.deleteConfirmation)) {
            $.ajax({
                url: baseEndpoint + "DeleteCustCurrency",
                type: "POST", data: form.serialize(), dataType: "text", contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
                error: function (xhr, status, error) {
                    $("#" + options.errorID).text(xhr.responseText);
                },
                success: function () {
                    options.onCommit();
                }
            });
        }
    }

    this.setActiveCustCurrency = function(sender, id) {
        var params = new Object();
        params.idCustCurrency = id;
        params.fActive = sender.checked;
        var d = JSON.stringify(params);
        $.ajax({
            url: baseEndpoint + "SetCustCurrencyActive",
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr, status, error) {
                $("#" + options.errorID).text(xhr.responseText);
            }
        });
    }

    this.getCustCurrencyList = function (destination) {
        $.ajax({
            url: baseEndpoint + "CustCurrencyList",
            type: "POST", data: JSON.stringify(new Object()), dataType: "text", contentType: "application/json",
            error: function (xhr, status, error) {
                $("#" + options.errorID).text(xhr.responseText);
            },
            success: function (r) {
                destination.html(r);
            }
        });
    } 

    this.editCustCurrency = function(sender, id) {
        var params = new Object();
        params.idCustCurrency = id;
        var d = JSON.stringify(params);
        $.ajax({
            url: baseEndpoint + "CustCurrencyEditor",
            type: "POST", data: d, dataType: "html", contentType: "application/json",
            error: function (xhr, status, error) {
                $("#" + options.errorID).text(xhr.responseText);
            },
            success: function (r) {
                options.onEdit(r);
            }
        });
    }

    this.submitCustCurrency = function(form) {
        if (form.valid()) {
            var f = form.serialize();
            $.ajax({
                url: baseEndpoint + "SubmitCustCurrency",
                type: "POST", data: f, dataType: "text", contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
                error: function (xhr, status, error) { $("#" + options.errorID).text(xhr.responseText); },
                success: function () {
                    options.onCommit();
                }
            });
        }
    }

    this.cancelCustCurrencyEdit = function () {
        options.onCancel();
    }
}

/*
 Creates a prefs editor for editing/managing deadlines.
  * baseEndpoint - resolved base for the webservices calls here
  * options - 
    * deleteConfirmation - the message to display to confirm deletion
    * onCancel - lambda called when cancel is clicked
    * onCommit - lambda called when currency is committed
    * onEdit - lambda called with the html passed of an editor for the specified currency
    * errorID - the id of a span or div that can display an error message
    * fShared - true to create a shared deadline
*/
function prefsDeadlineEditor(baseEndpoint, options) {
    this.deleteDeadline = function (sender, form) {
        if (confirm(options.deleteConfirmation)) {
            $.ajax({
                url: baseEndpoint + "DeleteDeadline",
                type: "POST", data: form.serialize(), dataType: "text", contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
                error: function (xhr, status, error) {
                    $("#" + options.errorID).text(xhr.responseText);
                },
                success: function () {
                    options.onCommit();
                }
            });
        }
    }

    this.editDeadline = function (sender, id) {
        var params = new Object();
        params.idDeadline = id;
        params.fShared = options.fShared;
        var d = JSON.stringify(params);
        $.ajax({
            url: baseEndpoint + "DeadlineEditor",
            type: "POST", data: d, dataType: "html", contentType: "application/json",
            error: function (xhr, status, error) {
                $("#" + options.errorID).text(xhr.responseText);
            },
            success: function (r) {
                options.onEdit(r);
            }
        });
    }

    this.cancelDeadlineEdit = function () {
        options.onCancel();
    }

    this.setUseHours = function (fUseHours, rootId) {
        var root = $("#" + rootId);
        if (fUseHours) {
            root.find("input[name='deadlineNewDate']").val("");
            root.find(".deadlineDateType").hide();
            root.find(".deadlineHourType").show();
        } else {
            root.find("input[name='deadlineNewHours']").val("0");
            root.find(".deadlineDateType").show();
            root.find(".deadlineHourType").hide();
        }
    }

    this.aircraftSelected = function (sender, rootId, ckUseHoursId) {
        var ckUseHours = $("#" + ckUseHoursId);
        if (sender.value == "") {
            this.setUseHours(false, rootId);
            ckUseHours.prop("checked", false);
            ckUseHours.parent().hide();
        }
        else {
            this.setUseHours(ckUseHours.is(":checked"), rootId);
            ckUseHours.parent().show();
        }
    }

    this.submitDeadline = function (form) {
        if (form.valid()) {
            var f = form.serialize();
            $.ajax({
                url: baseEndpoint + "SubmitDeadline",
                type: "POST", data: f, dataType: "text", contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
                error: function (xhr, status, error) { $("#" + options.errorID).text(xhr.responseText); },
                success: function () {
                    options.onCommit();
                }
            });
        }
    }

    this.updateDeadline = function (root, id) {
        var form = root.find("form");
        var f = form.serialize();
        $.ajax({
            url: baseEndpoint + "UpdateDeadline",
            type: "POST", data: f, dataType: "text", contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
            error: function (xhr, status, error) { $("#" + options.errorID).text(xhr.responseText); },
            success: function () {
                options.onCommit();
            }
        });
    }

    this.getDeadlineList = function (destination) {
        $.ajax({
            url: baseEndpoint + "DeadlineList",
            type: "POST", data: JSON.stringify(new Object()), dataType: "text", contentType: "application/json",
            error: function (xhr, status, error) {
                $("#" + options.errorID).text(xhr.responseText);
            },
            success: function (r) {
                destination.html(r);
            }
        });
    } 

    ///
    this.setEditMode = function (root, fEditMode) {

        if (fEditMode) {
            root.find(".staticDeadlineView").hide();
            root.find(".editDeadlineView").show();
        } else {
            root.find(".staticDeadlineView").show();
            root.find(".editDeadlineView").hide();
        }
    }
}

/*
 Creates a prefs editor for editing/managing share keys.
  * baseEndpoint - resolved base for the webservices calls here
  * options - 
    * deleteConfirmation - the message to display to confirm deletion
*/
function sharingEditor(baseEndpoint, options) {
    this.deleteShareKey = function (root, id) {
        if (confirm(options.deleteConfirmation)) {
            var params = new Object();
            params.id = id;
            var d = JSON.stringify(params);
            $.ajax({
                url: baseEndpoint + "DeleteShareKey",
                type: "POST", data: d, dataType: "text", contentType: "application/json",
                error: function (xhr, status, error) {
                    $("#" + options.errorID).text(xhr.responseText);
                },
                success: function () {
                    root.hide();
                }
            });
        }
    }

    this.setEditMode = function(root, mode) {
        if (mode) {
            root.find(".shareKeyEdit").show();
            root.find(".shareKeyStatic").hide();
        } else {
            root.find(".shareKeyEdit").hide();
            root.find(".shareKeyStatic").show();
        }
        root.find("input[type='checkbox']").prop("disabled", !mode);
    }

    this.updateShareKey = function (root, id) {
        var pThis = this;
        var params = new Object();
        params.idShareKey = id;
        params.fFlights = root.find("input[name='skCanViewFlights']").is(":checked");
        params.fTotals = root.find("input[name='skCanViewTotals']").is(":checked");
        params.fCurrency = root.find("input[name='skCanViewCurrency']").is(":checked");
        params.fAchievements = root.find("input[name='skCanViewAchievements']").is(":checked");
        params.fAirports = root.find("input[name='skCanViewAirports']").is(":checked");
        var d = JSON.stringify(params);
        $.ajax({
            url: baseEndpoint + "UpdateShareKey",
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr, status, error) {
                $("#" + options.errorID).text(xhr.responseText);
            },
            success: function () {
                pThis.setEditMode(root, false);
            }
        });
    }

    this.setPermissions = function (root, flights, totals, currency, achievements, airports) {
        root.find("input[name='skCanViewFlights']")[0].checked = flights;
        root.find("input[name='skCanViewTotals']")[0].checked = totals;
        root.find("input[name='skCanViewCurrency']")[0].checked = currency;
        root.find("input[name='skCanViewAchievements']")[0].checked = achievements;
        root.find("input[name='skCanViewAirports']")[0].checked = airports;
    }
}

function oAuthAppsEditor(baseEndpoint) {
    this.deleteClient = function (root, idClient) {
        var params = new Object();
        params.idClient = idClient;
        var d = JSON.stringify(params);
        $.ajax({
            url: baseEndpoint + "DeAuthClient",
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseText);
            },
            success: function () {
                root.hide();
            }
        });
    }
}