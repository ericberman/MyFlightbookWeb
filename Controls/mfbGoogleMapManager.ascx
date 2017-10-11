<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbGoogleMapManager.ascx.cs" Inherits="Controls_mfbGoogleMapMgr" %>
<asp:Panel ID="pnlMap" runat="server" EnableViewState="false" Width="100%" Height="300px">
</asp:Panel>
<ajaxToolkit:ResizableControlExtender ID="ResizableControlExtender1" OnClientResize="onResizeMapContainer" TargetControlID="pnlMap" HandleCssClass="resizeHandle"
     MinimumHeight="250" MinimumWidth="350" runat="server" />
