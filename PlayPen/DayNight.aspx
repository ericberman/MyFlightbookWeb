<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    CodeFile="DayNight.aspx.cs" Inherits="Public_DayNight" %>

<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbGoogleMapManager.ascx" TagName="mfbGoogleMapManager"
    TagPrefix="uc1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
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
            <asp:Button ID="btnTimes" runat="server" Text="Get Times" OnClick="btnTimes_Click" /><br />
            Sunrise:
            <asp:Label ID="lblSunRise" runat="server" Text=""></asp:Label><br />
            Sunset:
            <asp:Label ID="lblSunSet" runat="server" Text=""></asp:Label>
    <uc1:mfbGoogleMapManager ID="mfbGoogleMapManager1" runat="server" Height="400px"
        Width="100%" />
    <asp:Table runat="server" ID="tblDayNight">
    </asp:Table>

    <script type="text/javascript"> 
//<![CDATA[
        function clickForAirport(point) {
            if (point != null) {
                document.getElementById('<% =txtLat.ClientID %>').value = point.lat();
                document.getElementById('<% =txtLon.ClientID %>').value = point.lng();
            }
        }
//]]>
    </script>
</asp:Content>
