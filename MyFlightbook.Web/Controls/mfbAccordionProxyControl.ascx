<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbAccordionProxyControl.ascx.cs" Inherits="Controls_mfbAccordionProxyControl" %>
<asp:Panel ID="pnlContainer" runat="server">
    <asp:Label ID="lblLabel" runat="server" Text=""></asp:Label>
    <asp:HiddenField ID="hdnIsEnhanced" runat="server" />
    <asp:Button ID="btnPostback" runat="server" style="display:none;" OnClick="btnPostback_Click" Visible="false" />
</asp:Panel>
