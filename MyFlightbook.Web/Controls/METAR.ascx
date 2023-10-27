<%@ Control Language="C#" AutoEventWireup="true" Codebehind="METAR.ascx.cs" Inherits="Controls_METAR" %>
<script type="text/javascript">

    function getMetars() {
        var target = $('#<% =pnlMetar.ClientID %>');
        var params = new Object();
        params.szRoute = $('#<% =hdnAirports.ClientID %>')[0].value;
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: '/logbook/mvc/Airport/METARSForRoute',
                type: "POST", data: d, dataType: "html", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.statusText);
                },
                complete: function (response) { },
                success: function (response) { target.html(response); }
            });
        return false;
    }
</script>
<asp:HiddenField ID="hdnAirports" runat="server" />
<asp:Panel ID="pnlMetar" runat="server">
    <a href="javascript:getMetars();"><% =Resources.Weather.GetMETARSPrompt %></a>
</asp:Panel>
