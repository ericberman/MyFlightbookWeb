function addExistingAircraft(aircraftID, idSender, idSuccess) {
    var params = new Object();
    params.aircraftID = aircraftID;
    $.ajax(
    {
        type: "POST",
        data: $.toJSON(params),
        url: "ImpAircraftService.aspx/AddExistingAircraft",
        dataType: "json",
        contentType: "application/json",
        error: function (xhr, status, error) {
            window.alert(xhr.responseJSON.Message);
        },
        complete: function (response) {
        },
        success: function (response) {
            document.getElementById(idSender).style.display = 'none';
            document.getElementById(idSuccess).style.display = 'block';
        }
    });
}
function addNewAircraft(idSender, idSuccess, idModel, szTail, instanceType) {
    var params = new Object();
    params.idModel = idModel;
    params.szTail = szTail;
    params.instanceType = instanceType;
    $.ajax(
        {
            type: "POST",
            data: $.toJSON(params),
            url: "ImpAircraftService.aspx/AddNewAircraft",
            dataType: "json",
            contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
            },
            complete: function (response) {
            },
            success: function (response) {
                document.getElementById(idSender).style.display = 'none';
                document.getElementById(idSuccess).style.display = 'block';
            }
        });
}
