<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbGoogleMapMgr" Codebehind="mfbGoogleMapManager.ascx.cs" %>
<asp:MultiView ID="mvMap" runat="server" ActiveViewIndex="0">
    <asp:View ID="vwDynamic" runat="server">
        <asp:Panel ID="pnlMap" runat="server" EnableViewState="false" Width="100%" Height="400px">
        </asp:Panel>
        <ajaxToolkit:ResizableControlExtender ID="ResizableControlExtender1" OnClientResize="onResizeMapContainer" TargetControlID="pnlMap" HandleCssClass="resizeHandle"
             MinimumHeight="250" MinimumWidth="350" runat="server" />
    </asp:View>
    <asp:View ID="vwStatic" runat="server">
        <div style="text-align:center; width:100%">
            <asp:Image ID="imgMap" runat="server" EnableViewState="false" Width="600px" Height="400px" />
        </div>
    </asp:View>
</asp:MultiView>
