<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbATDFTD.ascx.cs" Inherits="Controls_mfbATDFTD" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>

<div class="callout calloutSmall calloutBackground shadowed">
    <asp:Label ID="lblShowHide" runat="server"><h3><asp:Literal ID="Literal1" runat="server" Text="<%$ Resources:LocalizedText, ATDFTDNoticeHead %>" />&nbsp;<asp:Label ID="lblPrompt" runat="server" /></h3></asp:Label>
    <asp:Panel ID="pnlText" runat="server" Height="0px" EnableViewState="true" style="overflow:hidden">
        <%= MyFlightbook.Branding.ReBrand(Resources.LocalizedText.ATDFTDNoticeBody) %>
    </asp:Panel>
    <cc1:CollapsiblePanelExtender ID="cpeViewText" runat="server" TargetControlID="pnlText" CollapsedSize="0" ExpandControlID="lblShowHide"
    CollapseControlID="lblShowHide" Collapsed="true" EnableViewState="true" CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" TextLabelID="lblPrompt" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>"></cc1:CollapsiblePanelExtender>
</div>
