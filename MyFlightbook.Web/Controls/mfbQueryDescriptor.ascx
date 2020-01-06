<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbQueryDescriptor" Codebehind="mfbQueryDescriptor.ascx.cs" %>
<%@ Register src="mfbQueryDescriptorItem.ascx" tagname="mfbQueryDescriptorItem" tagprefix="uc1" %>
<asp:PlaceHolder ID="PlaceHolder1" runat="server"></asp:PlaceHolder>
<asp:Repeater ID="rptItems" runat="server" Visible="false">
    <ItemTemplate>
        <uc1:mfbQueryDescriptorItem runat="server" ID="filterItem" OnDeleteItemClicked="filterItem_DeleteItemClicked" Title='<%# Eval("FilterName") %>' Description='<%# Eval("FilterValue") %>' PropName='<%# Eval("PropertyName") %>' />
    </ItemTemplate>
</asp:Repeater>
<asp:Panel ID="pnlNoFilter" runat="server" Visible="true">
    <asp:Label ID="lblAllItems" runat="server" Text="<%$ Resources:LocalizedText, SearchAndTotalsAllFlights %>"></asp:Label>
</asp:Panel>
