<%@ Control Language="C#" AutoEventWireup="true" CodeFile="popmenu.ascx.cs" Inherits="Controls_popmenu" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="asp" %>
<asp:Image ID="imgPop" AlternateText="<%$ Resources:LocalizedText, PopMenuAltText %>" ImageUrl="~/images/MenuChevron.png" runat="server" />
<asp:Panel ID="pnlMenuContent" runat="server" BackColor="White" BorderColor="Black" style="padding: 3px; display:none;" BorderWidth="1px">
    <asp:PlaceHolder ID="plcMenuContent" runat="server"></asp:PlaceHolder>
</asp:Panel>
<asp:dropshadowextender ID="DropShadowExtender1" runat="server" TargetControlID="pnlMenuContent" Opacity=".5"></asp:dropshadowextender>
<asp:HoverMenuExtender ID="HoverMenuExtender1" HoverCssClass="hoverPopMenu" runat="server" TargetControlID="imgPop" PopupControlID="pnlMenuContent" PopupPosition="Bottom"></asp:HoverMenuExtender>
