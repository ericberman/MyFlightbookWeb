<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbAccordionProxyControl.ascx.cs" Inherits="Controls_mfbAccordionProxyControl" %>
<asp:Panel ID="pnlContainer" runat="server">
    <asp:Image ID="imgIcon" runat="server" />
    <asp:Label ID="lblLabel" runat="server" Text=""></asp:Label>
    <asp:Image ID="imgState" runat="server" Height="10pt" ImageAlign="Baseline" />
    <asp:HiddenField ID="hdnIsEnhanced" runat="server" />
    <asp:Button ID="btnPostback" runat="server" style="display:none;" OnClick="btnPostback_Click" Visible="false" />
</asp:Panel>
