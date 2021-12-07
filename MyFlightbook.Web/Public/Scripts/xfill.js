/******************************************************
 *
 * Copyright (c) 2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/*
 * setXFillValue calls fillfunc, which retrieves a value, and must in turn call a lambda parameter to cause the value to be set.
 * E.g., if cross fill has to call an ajax function, then this function calls the fillfunc, which calls the ajax; on the result of the ajax returning, it calls the 
 * lambda to set the value's text.
 * 
 * so the idea is that the target field's cross-fill image setXFillValue([self], fillfunc) on click.
 * You provide a fillfunc, which retrieves the value and passes the result here, which knows how to put
 * that result into the target.
 * 
 * Several fill functions (total, hobbs, and tach) follow.
 */
function setXFillValue(target, fillfunc) {
    fillfunc((result) => {
        if (result)
            $find(target).set_text(result);
    });
};

function getTotalFillFunc(idTotal) {
    return function (onResult) {
        onResult($('#' + idTotal)[0].value);
    }
}

var _xfillElementMap = new Object();
function addXFillMap(key, value) {
    _xfillElementMap[key] = value;
}

function getXFillElement(key) {
    return _xfillElementMap[key];
}

function getTachFill(currentlySelectedAircraft) {
    return function (onResult) {
        if (!currentlySelectedAircraft)
            return;

        var id = currentlySelectedAircraft();

        if (id === null || id === '')
            return;

        var params = new Object();
        params.idAircraft = id;
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: 'LogbookNew.aspx/HighWaterMarkTachForAircraft',
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.responseJSON.Message);
                    if (onError !== null)
                        onError();
                },
                complete: function () { },
                success: function (response) {
                    onResult(response.d);
                }
            });
    }
}

function getHobbsFill(currentlySelectedAircraft) {
    return function (onResult) {
        if (!currentlySelectedAircraft)
            return;

        var id = currentlySelectedAircraft();

        if (id === null || id === '')
            return;

        var params = new Object();
        params.idAircraft = id;
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: 'LogbookNew.aspx/HighWaterMarkHobbsForAircraft',
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.responseJSON.Message);
                    if (onError !== null)
                        onError();
                },
                complete: function () { },
                success: function (response) {
                    onResult(response.d);
                }
            });
    }
}

function getTaxiFill() {
    return function (onResult) {
        if (!currentlySelectedAircraft)
            return;

        var id = currentlySelectedAircraft();

        if (id === null || id === '')
            return;

        var params = new Object();
        params.fsStart = $('#' + getXFillElement('fStart'))[0].value;
        params.fsEnd = $('#' + getXFillElement('fEnd'))[0].value;
        params.szTotal = $('#' + getXFillElement('total'))[0].value;
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: 'LogbookNew.aspx/TaxiTime',
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.responseJSON.Message);
                    if (onError !== null)
                        onError();
                },
                complete: function () { },
                success: function (response) {
                    if (response.d != '')
                        onResult(response.d);
                }
            });
    }
}