<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbPublicFlightItem.ascx.cs" Inherits="Controls_mfbPublicFlightItem" %>
<%@ Register src="fbComment.ascx" tagname="fbComment" tagprefix="uc1" %>
<%@ Register src="mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc3" %>
<tr valign="top">
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
        <span style="white-space:pre-line;"><asp:Label ID="lblComments" runat="server"></asp:Label></span>
        <uc3:mfbImageList ID="mfbIlFlight" runat="server" />
    </td>
</tr>