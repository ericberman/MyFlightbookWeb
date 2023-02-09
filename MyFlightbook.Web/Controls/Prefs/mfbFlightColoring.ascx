<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="mfbFlightColoring.ascx.cs" Inherits="MyFlightbook.Web.Controls.Prefs.mfbFlightColoring" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<script type="text/javascript">
    function qNameForElement(e) {
        return $(e).siblings("input[type='hidden']")[0].value;
    }

    function setColor(sender) {
        var params = new Object();
        params.queryName = qNameForElement(sender.get_element());
        params.color = "#" + sender.get_selectedColor();
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: '<% =ResolveUrl("~/Member/Ajax.asmx/SetColorForQuery") %>',
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.responseJSON.Message);
                },
                complete: function (response) { },
                success: function (response) { }
            });
    }

    function clearColor(sender) {
        var params = new Object();
        params.queryName = qNameForElement(sender);
        params.color = null;
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: '<% =ResolveUrl("~/Member/Ajax.asmx/SetColorForQuery") %>',
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

    function setMapColors(sender) {
        var params = new Object();
        // update the text field with the new color
        sender.get_element().value = "#" + sender.get_selectedColor();

        params.pathColor = $('#' + '<% =txtPathSample.ClientID %>')[0].value;
        params.routeColor = $('#' + '<% =txtRteSample.ClientID%>')[0].value;
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: '<% =ResolveUrl("~/Member/Ajax.asmx/SetMapColors") %>',
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.responseJSON.Message);
                },
                complete: function (response) { },
                success: function (response) { }
            });
    }

    function resetMapColors(sender) {
        var params = new Object();

        params.pathColor = "";
        params.routeColor = "";
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: '<% =ResolveUrl("~/Member/Ajax.asmx/SetMapColors") %>',
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.responseJSON.Message);
                },
                complete: function (response) { },
                success: function (response) {
                    $("#" + "<% = lblPathSample.ClientID %>").css("background-color", "<% =MyFlightbook.Mapping.MFBGoogleMapOptions.DefaultPathColor %>");
                    $("#" + "<% = lblRteSample.ClientID %>").css("background-color", "<% =MyFlightbook.Mapping.MFBGoogleMapOptions.DefaultRouteColor %>");
                }
            });
    }
</script>
<div style="margin-left: 5px; margin-bottom: 10px">
    <h2><% =Resources.Preferences.FlightColoringFlightsHeader %></h2>
    <p>
        <asp:Localize ID="locFlightColoringDesc" runat="server" Text="<%$ Resources:Preferences, FlightColoringDescription %>" />
        <asp:HyperLink ID="lnkLearnMore" runat="server" Text="<%$ Resources:Preferences, FlightColoringDescriptionLearnMore %>" NavigateUrl="https://myflightbookblog.blogspot.com/2021/03/saved-searches-and-flight-coloring.html" Target="_blank" />
    </p>
    <asp:GridView ID="gvCanned" runat="server" ShowHeader="false" ShowFooter="false" GridLines="None" CellPadding="3" AutoGenerateColumns="false" OnRowDataBound="gvCanned_RowDataBound">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <div><asp:Label ID="lblName" runat="server" Text='<%#: Eval("QueryName") %>' Font-Bold="true" /></div>
                    <div><asp:Label ID="lblDesc" runat="server" Text='<%#: Eval("Description") %>' CssClass="fineprint" /></div>
                </ItemTemplate>
                <ItemStyle VerticalAlign="Top" />
            </asp:TemplateField>
            <asp:TemplateField>
                <ItemTemplate>
                    <div>
                        <asp:Label ID="lblQSamp" runat="server" style="padding: 3px" Text="<%$ Resources:Preferences, FlightColoringSample %>" />
                        <a href="#" onclick="clearColor(this);return false;"><%# Resources.Preferences.FlightColoringNoColor %></a>
                        <asp:TextBox ID="txtQsamp" runat="server" style="visibility:hidden; width: 4px;" />
                        <asp:HiddenField ID="hdnQName" runat="server" Value='<%# Eval("Queryname") %>' />
                    </div>
                    <cc1:ColorPickerExtender ID="cpeQ" runat="server" TargetControlID="txtQsamp" PopupButtonID="lblQSamp" SampleControlID="lblQSamp" PaletteStyle="Continuous" OnClientColorSelectionChanged="setColor" />
                </ItemTemplate>
                <ItemStyle VerticalAlign="Top" />
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>
            <p>
                <asp:Localize ID="locNoQueries" runat="server" Text="<%$ Resources:Preferences, FlightColoringNoSaveQueries %>" />
                <asp:HyperLink ID="lnkLearnMore" NavigateUrl="https://myflightbookblog.blogspot.com/2021/03/saved-searches-and-flight-coloring.html" runat="server" Text="<%$ Resources:Preferences, FlightColoringLearnMore %>" Target="_blank" />
            </p>
        </EmptyDataTemplate>
    </asp:GridView>
    <h2><% =Resources.Preferences.MapColorsHeader %></h2>
    <table>
        <tr>
            <td><% =Resources.Preferences.MapColorRoute %></td>
            <td>
                <asp:Label ID="lblRteSample" runat="server" style="padding: 3px;" Text="<%$ Resources:Preferences, FlightColoringSample %>" />
                <asp:TextBox ID="txtRteSample" runat="server" style="visibility:hidden; width: 4px;" />
                <cc1:ColorPickerExtender ID="cpeRte" runat="server" TargetControlID="txtRteSample" PopupButtonID="lblRteSample" SampleControlID="lblRteSample" PaletteStyle="Continuous" OnClientColorSelectionChanged="setMapColors" />
            </td>
        </tr>
        <tr>
            <td><% =Resources.Preferences.MapColorPath %></td>
            <td>
                <asp:Label ID="lblPathSample" runat="server" style="padding: 3px;" Text="<%$ Resources:Preferences, FlightColoringSample %>" />
                <asp:TextBox ID="txtPathSample" runat="server" style="visibility:hidden; width: 4px;" />
                <cc1:ColorPickerExtender ID="cpePath" runat="server" TargetControlID="txtPathSample" PopupButtonID="lblPathSample" SampleControlID="lblPathSample" PaletteStyle="Continuous" OnClientColorSelectionChanged="setMapColors" />
            </td>
        </tr>
    </table>
    <div><a href="#" onclick="javascript:resetMapColors(this);"><% =Resources.Preferences.MapColorsReset %></a></div>
    <script type="text/javascript">
        $(document).ready(function () {
            $("#" + "<% =lblRteSample.ClientID %>").css("background-color", $("#" + "<%=txtRteSample.ClientID %>")[0].value);
            $("#" + "<% =lblPathSample.ClientID %>").css("background-color", $("#" + "<%=txtPathSample.ClientID %>")[0].value);
        })
    </script>
</div>
