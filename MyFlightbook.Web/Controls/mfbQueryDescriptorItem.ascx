<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbQueryDescriptorItem" Codebehind="mfbQueryDescriptorItem.ascx.cs" %>
<div class="queryFilterItem">
    <asp:Label ID="lblTitle" Font-Bold="true" runat="server" Text=""></asp:Label>: <asp:Label ID="lblDescriptor" runat="server" Text="Label"></asp:Label>
    <asp:ImageButton ID="btnDelete" runat="server" ImageAlign="AbsMiddle" ImageUrl="~/images/x.gif" OnClick="btnDelete_Click" />
    <asp:HiddenField ID="hdnPropName" runat="server" />
</div>