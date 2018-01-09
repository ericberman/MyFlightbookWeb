function addExistingAircraft(aircraftID, idSender, idSuccess, idPopup) {
    var params = new Object();
    params.aircraftID = aircraftID;
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
            document.getElementById(idSender).style.display = 'none';
            document.getElementById(idSuccess).style.display = 'block';
        }
    });
}
function addNewAircraft(idSender, idSuccess, idModel, szTail, instanceType, idPopup) {
    var params = new Object();
    params.idModel = idModel;
    params.szTail = szTail;
    params.instanceType = instanceType;
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
                document.getElementById(idSender).style.display = 'none';
                document.getElementById(idSuccess).style.display = 'block';
            }
        });
}
