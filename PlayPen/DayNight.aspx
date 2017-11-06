<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    CodeFile="DayNight.aspx.cs" Inherits="Public_DayNight" %>

<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbGoogleMapManager.ascx" TagName="mfbGoogleMapManager"
    TagPrefix="uc1" %>
    <asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
    <div style="float:right;">
                <table style="border: 1px solid black; border-collapse:collapse;">
            <tr>
                <td style="font-weight:bold">Key:</td>
            </tr>
            <tr>
                <td style="background-color:white; border:1px solid black;">Day</td>
            </tr>
            <tr>
                <td style="background-color:LightGray; border:1px solid black;">Night - between sunrise and sunset </td>
            </tr>
            <tr>
                <td style="background-color:BlanchedAlmond; border:1px solid black;">Night - FAA Civil Night (sun < 6° below horizon)</td>
            </tr>
            <tr>
                <td style="background-color:darkgray; border:1px solid black;">Night - 1 hour past sunset to 1 hour before sunrise</td>
            </tr>
        </table>
    </div>
    <table>
        <tr>
            <td>
                Latitude:
            </td>
            <td>
                <asp:TextBox ID="txtLat" runat="server"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td>
                Longitude:
            </td>
            <td>
                <asp:TextBox ID="txtLon" runat="server"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td>
                Date:
            </td>
            <td>
                <asp:TextBox ID="txtDate" runat="server"></asp:TextBox>
            </td>
        </tr>
    </table>
    <p>
    <asp:Button ID="btnTimes" runat="server" Text="Get Times" OnClick="btnTimes_Click" /></p>
    <p>Sunrise: <asp:Label ID="lblSunRise" runat="server" Text=""></asp:Label></p>
    <p>Sunset: <asp:Label ID="lblSunSet" runat="server" Text=""></asp:Label></p>
    <uc1:mfbGoogleMapManager ID="mfbGoogleMapManager1" runat="server" Height="400px"
        Width="100%" />
    <asp:Table runat="server" ID="tblDayNight" EnableViewState="false">
    </asp:Table>

    <script type="text/javascript"> 
//<![CDATA[
        function clickForAirport(point) {
            if (point != null) {
                document.getElementById('<% =txtLat.ClientID %>').value = point.lat();
                document.getElementById('<% =txtLon.ClientID %>').value = point.lng();
                getMfbMap().clickMarker(point, 'Clicked Location', 'A', "<a href=\"javascript:zoomForAirport();\">Zoom in</a>");
            }
        }
//]]>
    </script>
</asp:Content>
