<%@ Control Language="C#" AutoEventWireup="true" Codebehind="popmenu.ascx.cs" Inherits="Controls_popmenu" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="asp" %>
<asp:Image ID="imgPop" AlternateText="<%$ Resources:LocalizedText, PopMenuAltText %>" ImageUrl="~/images/MenuChevron.png" runat="server" CssClass="popMenuAccess" />
<asp:Panel ID="pnlMenuContent" runat="server" CssClass="popMenuContent" style="display:none;">
    <asp:PlaceHolder ID="plcMenuContent" runat="server"></asp:PlaceHolder>
</asp:Panel>
<asp:HoverMenuExtender ID="hme" HoverCssClass="hoverPopMenu" runat="server" TargetControlID="imgPop" PopupControlID="pnlMenuContent" PopupPosition="Bottom"></asp:HoverMenuExtender>
<% =SafariHackScript %>