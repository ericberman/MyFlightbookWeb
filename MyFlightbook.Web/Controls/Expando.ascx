<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_Expando" Codebehind="Expando.ascx.cs" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="asp" %>
<asp:CollapsiblePanelExtender ID="CollapsiblePanelExtender1" runat="server"
    ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>" CollapsedText ="<%$ Resources:LocalizedText, ClickToShow %>"
    ExpandControlID="pnlHeader" CollapseControlID="pnlHeader"
    Collapsed="true" TargetControlID="pnlBody" TextLabelID="lblShowHide">
</asp:CollapsiblePanelExtender>
<asp:Panel ID="pnlHeader" runat="server">
    <asp:PlaceHolder ID="plcExpandoHeader" runat="server"></asp:PlaceHolder>
    <asp:Label ID="lblShowHide" runat="server" Text=""></asp:Label>
</asp:Panel>
<asp:Panel ID="pnlBody" runat="server" Height="0px" style="overflow:hidden;" >
    <asp:PlaceHolder ID="plcExpandoBody" runat="server"></asp:PlaceHolder>
</asp:Panel>
