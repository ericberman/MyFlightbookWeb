/******************************************************
 * 
 * Copyright (c) 2016-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

function addExistingAircraft(d) {
    var mr = d["matchRow"];
    var params = new Object();
    params.aircraftID = mr.BestMatchAircraft.AircraftID;
    var idPopup = d["progressID"];
    $find(idPopup).show();
    $.ajax(
    {
        type: "POST",
        data: $.toJSON(params),
        url: "ImpAircraftService.aspx/AddExistingAircraft",
        dataType: "json",
        contentType: "application/json",
        error: function (xhr, status, error) {
            $find(idPopup).hide();
            window.alert(xhr.responseJSON.Message);
        },
        complete: function (response) {
            $find(idPopup).hide();
        },
        success: function (response) {
            $find(idPopup).hide();
            document.getElementById(d["btnAdd"]).style.display = 'none';
            document.getElementById(d["lblAllGood"]).style.display = 'block';
        }
    });
}
function addNewAircraft(d) {
    var mr = d["matchRow"];
    var params = new Object();
    params.idModel = document.getElementById(d["mdlID"]).value;
    params.szTail = mr.BestMatchAircraft.TailNumber;
    var ddlInst = document.getElementById(d["cmbInstance"]);
    params.instanceType = ddlInst.options[ddlInst.selectedIndex].value;
    var idPopup = d["progressID"];
    $find(idPopup).show();
    $.ajax(
        {
            type: "POST",
            data: $.toJSON(params),
            url: "ImpAircraftService.aspx/AddNewAircraft",
            dataType: "json",
            contentType: "application/json",
            error: function (xhr, status, error) {
                $find(idPopup).hide();
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) {
                $find(idPopup).hide();
            },
            success: function (response) {
                $find(idPopup).hide();
                document.getElementById(d["btnAdd"]).style.display = 'none';
                document.getElementById(d["lblAllGood"]).style.display = 'block';
            }
        });
}

function Validate(d) {
    // reset for all to be OK.
    document.getElementById(d["btnAdd"]).style.display = 'block';
    document.getElementById(d["lblAllGood"]).style.display = 'none';
    document.getElementById(d["lblErr"]).style.display = 'none';
    document.getElementById(d["lblErr"]).innerText = "";

    var params = new Object();
    params.idModel = document.getElementById(d["mdlID"]).value;
    if (params.idModel === "")
        params.idModel = -1;
    var mr = d["matchRow"];
    params.szTail = mr.BestMatchAircraft.TailNumber;
    var ddlInst = document.getElementById(d["cmbInstance"]);
    params.instanceType = ddlInst.options[ddlInst.selectedIndex].value;

    var idPopup = d["progressID"];
    $find(idPopup).show();

    $.ajax(
        {
            type: "POST",
            data: $.toJSON(params),
            url: "ImpAircraftService.aspx/ValidateAircraft",
            dataType: "json",
            contentType: "application/json",
            error: function (xhr, status, error) {
                $find(idPopup).hide();
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) {
                $find(idPopup).hide();
            },
            success: function (response) {
                $find(idPopup).hide();
                var err = response.d;
                if (err === "") {
                    document.getElementById(d["btnAdd"]).style.display = 'block';
                    document.getElementById(d["lblErr"]).style.display = 'none';
                    toggleModelEdit(d["pnlStaticMake"], d["pnlEditMake"]);
                } else {
                    document.getElementById(d["btnAdd"]).style.display = 'none';
                    document.getElementById(d["lblErr"]).style.display = 'block';
                    document.getElementById(d["lblErr"]).innerText = err;
                }
            }
        }
    );
}

function ImportModelSelected(source, eventArgs) {
    var d = JSON.parse(eventArgs.get_value());

    document.getElementById(d["mdlID"]).value = d["modelID"];
    document.getElementById(d["lblID"]).innerText = d["modelDisplay"];

    Validate(d);

    source.get_element().value = "";
}

function toggleModelEdit(id1, id2) {
    var obj1 = document.getElementById(id1);
    var obj2 = document.getElementById(id2);

    if (obj1.style.display === "none") {
        obj1.style.display = "block";
        obj2.style.display = "none";
    } else {
        obj1.style.display = "none";
        obj2.style.display = "block";
    }
}

function updateInstanceDesc(idInst, idDesc, idContext) {
    var ddl = document.getElementById(idInst);
    var lblDesc = document.getElementById(idDesc);
    lblDesc.innerText = ddl.options[ddl.selectedIndex].innerText;

    Validate(JSON.parse(document.getElementById(idContext).value));
}
