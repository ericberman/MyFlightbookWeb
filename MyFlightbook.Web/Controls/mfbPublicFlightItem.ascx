<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbPublicFlightItem" Codebehind="mfbPublicFlightItem.ascx.cs" %>
<%@ Register src="mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc3" %>
<tr style="vertical-align:top">
    <td>
        <asp:Label ID="lblDate" Font-Bold="true" runat="server" Text=""></asp:Label> - 
        <asp:Label Font-Bold="true" ID="lblTail" runat="server" CssClass="hintTrigger"></asp:Label> <asp:Label ID="lblDetails" runat="server" Text=""></asp:Label>
        <div style="text-align:center">
            <asp:Label ID="lblModel" runat="server" Text=""></asp:Label> <asp:Label ID="lblCatClass" runat="server" Text=""></asp:Label>
        </div>
        <uc3:mfbImageList ID="mfbILAircraft" CanEdit="false" ImageClass="Aircraft" MaxImage="1" Columns="1" runat="server" />
    </td>
    <td>
        <asp:HyperLink ID="lnkFlight" runat="server"><asp:Label ID="lblroute" runat="server"></asp:Label></asp:HyperLink><br />
        <asp:Label ID="lblComments" runat="server" style="white-space:pre-line;"></asp:Label>
        <uc3:mfbImageList ID="mfbIlFlight" runat="server" />
    </td>
</tr>