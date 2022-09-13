<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbEditableImage.ascx.cs" Inherits="Controls_mfbEditableImage" %>

<div>
    <asp:Literal ID="litVideoOpenTag" runat="server"></asp:Literal>
    <asp:Image ID="img" runat="server" />
    <asp:Literal ID="litVideoCloseTag" runat="server"></asp:Literal>
    <div class="imageMenu" runat="server" id="divActions">
        <div runat="server" id="divDel"><asp:ImageButton ID="lnkDelete" CssClass="ilToolbarItem" ImageUrl="~/images/x.gif" runat="server" Visible="false" ToolTip="<%$ Resources:LocalizedText, EditableImageDelete %>" AlternateText="<%$ Resources:LocalizedText, EditableImageDelete %>" OnClick="DeleteImage" /></div>
        <div runat="server" id="divZoom"><asp:HyperLink ID="lnkZoom" CssClass="ilToolbarItem" runat="server" Visible="false"><asp:Image ID="Image1" ImageUrl="~/images/mapmarkersm.png" runat="server" ToolTip="<%$ Resources:LocalizedText, EditableImageViewOnMap %>" AlternateText="<%$ Resources:LocalizedText, EditableImageViewOnMap %>" /></asp:HyperLink></div>
        <div runat="server" id="divDefault"><asp:ImageButton ID="lnkMakeDefault" runat="server" CssClass="ilToolbarItem" ImageUrl="~/images/favoritesm.png" ToolTip="<%$ Resources:LocalizedText, EditableImageMakeFavorite %>" AlternateText="<%$ Resources:LocalizedText, EditableImageMakeFavorite %>" OnClick="lnkMakeDefault_Click" Visible="false" /></div>
        <div runat="server" id="divAnnot"><asp:HyperLink ID="lnkAnnotate" CssClass="ilToolbarItem" runat="server" Visible="false"><asp:Image ID="imgEdit" ImageUrl="~/images/pencilsm.png" ToolTip="<%$ Resources:LocalizedText, EditableImageEditPrompt %>" AlternateText="<%$ Resources:LocalizedText, EditableImageEditPrompt %>" runat="server" /></asp:HyperLink></div>
    </div>
</div>
<asp:Panel ID="pnlStatic" runat="server" style="max-width: 200px; text-align:center;">
    <asp:Label ID="lblComments" runat="server"></asp:Label>
</asp:Panel>
<asp:Panel ID="pnlEdit" runat="server" DefaultButton="btnUpdateComments" style="display:none">
    <div style="padding:5px;">
        <asp:TextBox ID="txtComments" runat="server" Width="130px"></asp:TextBox>
        <asp:Button ID="btnUpdateComments" runat="server" Text="<%$ Resources:LocalizedText, OK %>" OnClick="btnUpdateComments_Click" />
    </div>
</asp:Panel>
