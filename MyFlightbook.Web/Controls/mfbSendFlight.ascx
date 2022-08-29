<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbSendFlight.ascx.cs" Inherits="MyFlightbook.Controls.mfbSendFlight" %>
<div id="divSendFlightPop" style="display:none">
    <asp:HiddenField ID="hdnFlightToSend" runat="server" />
    <asp:HiddenField ID="hdnFlightSendToTarget" runat="server" Value="~/Member/LogbookNew.aspx" />
    <table>
        <tr>
            <td>
                <asp:Localize ID="locEmailRecipient" runat="server" Text="<%$ Resources:LogbookEntry, SendFlightEmailPrompt %>"></asp:Localize>
            </td>
            <td>
                <asp:TextBox ID="txtSendFlightEmail" runat="server"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td style="vertical-align:top">
                <asp:Localize ID="locSendFlightMessage" runat="server" Text="<%$ Resources:LogbookEntry, SendFlightMessagePrompt %>"></asp:Localize>
            </td>
            <td>
                <asp:TextBox ID="txtSendFlightMessage" TextMode="MultiLine" Rows="3" runat="server"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td></td>
            <td>
                <table>
                    <tr style="vertical-align:top">
                        <td><asp:Label Font-Bold="true" ID="lblDates" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightDate %>"></asp:Label></td>
                        <td><span id="sendFlightDate"></span></td>
                    </tr>
                    <tr style="vertical-align:top">
                        <td><asp:Label Font-Bold="true" ID="lblAircraft" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightAircraft %>"></asp:Label></td>
                        <td><span id="sendFlightTail"></span></td>
                    </tr>
                    <tr style="vertical-align:top">
                        <td><asp:Label Font-Bold="true" ID="lblRoute" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightRoute %>"></asp:Label></td>
                        <td><span id="sendFlightRoute"></span></td>
                    </tr>
                    <tr style="vertical-align:top">
                        <td><asp:Label Font-Bold="true" ID="lblComments" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightComments %>"></asp:Label></td>
                        <td><span id="sendFlightComment"></span></td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
    <div style="text-align:center">
        <asp:Button  ID="btnSendFlight" OnClientClick="sendFlightSubmit();" runat="server" Text="<%$ Resources:LogbookEntry, SendFlightButton %>" />
    </div>
</div>
<script>
    function sendFlight(idFlight) {
        $("#sendFlightDate").text("");
        $("#sendFlightTail").text("");
        $("#sendFlightRoute").text("");
        $("#sendFlightComment").text("");
        var params = new Object();
        params.idFlight = idFlight;
        params.fIncludeImages = false;
        params.fIncludeTelemetry = false;
        $("#" + "<% =hdnFlightToSend.ClientID %>")[0].value = idFlight; // make sure that's up-to-date.
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: '<% =ResolveUrl("~/Member/Ajax.asmx/GetFlight") %>',
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.responseJSON.Message);
                },
                complete: function (response) { },
                success: function (response) {
                    popSendFormForFlight(response.d);
                }
            });
    }

    function popSendFormForFlight(le) {
        // Bind the relevant fields above
        $("#sendFlightDate").text(convertFdUpJsonDate(le.Date).toLocaleDateString());
        $("#sendFlightTail").text(le.TailNumDisplay);
        $("#sendFlightRoute").text(le.Route);
        $("#sendFlightComment").text(le.Comment);

        var div = $("#divSendFlightPop");
        div.dialog({ autoOpen: false, closeOnEscape: true, width:530, modal: true, title: "<%=Resources.LogbookEntry.SendFlightPrompt %>" });
        div.dialog("open");
    }

    function sendFlightSubmit() {
        var params = new Object();
        params.idFlight = $("#" + "<% =hdnFlightToSend.ClientID %>")[0].value;
        params.szTargetEmail = $("#" + "<% =txtSendFlightEmail.ClientID %>")[0].value;
        params.szMessage = $("#" + "<% =txtSendFlightMessage.ClientID %>")[0].value;
        params.szSendPageTarget = $("#" + "<% =hdnFlightSendToTarget.ClientID %>")[0].value;
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: '<% =ResolveUrl("~/Member/Ajax.asmx/sendFlight") %>',
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.responseJSON.Message);
                },
                complete: function (response) { },
                success: function (response) {
                    dismissDlg("#divSendFlightPop");
                }
            });
    }
</script>