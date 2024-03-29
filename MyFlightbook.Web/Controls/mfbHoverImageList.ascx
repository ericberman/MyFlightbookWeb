﻿<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbHoverImageList.ascx.cs" Inherits="Controls_mfbHoverImageList" %>
<%@ Register src="mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc1" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<asp:Image ID="imgThumb" runat="server" ImageUrl="~/images/noimage.png" ToolTip="<%$ Resources:LocalizedText, NoImageTooltip %>" AlternateText="<%$ Resources:LocalizedText, NoImageTooltip %>" style="max-width:150px;" />
<cc1:hovermenuextender runat="server" ID="hmImages" PopupControlID="pnlImages" TargetControlID="imgThumb" OffsetX="-14" OffsetY="-14" ></cc1:hovermenuextender>
<asp:Panel ID="pnlImages" runat="server" CssClass="hintPopup" style="max-width:500px; text-align:left;">
    <uc1:mfbImageList ID="mfbil" runat="server" CanEdit="false" IncludeDocs="true" Columns="3" MaxImage="-1" />
</asp:Panel>
