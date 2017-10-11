<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbTooltip.ascx.cs" Inherits="Controls_mfbTooltip" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<asp:Label ID="lblTip" runat="server" Text="[?]" CssClass="hint"></asp:Label>
<cc1:HoverMenuExtender ID="hmeHover" runat="server" OffsetX="10" OffsetY="10" TargetControlID="lblTip" PopupControlID="pnlTip"></cc1:HoverMenuExtender>
<asp:Panel ID="pnlTip" runat="server" CssClass="hintPopup">
    <asp:Literal ID="litPop" runat="server"></asp:Literal>
    <asp:PlaceHolder ID="plcCustom" runat="server"></asp:PlaceHolder>
</asp:Panel>
