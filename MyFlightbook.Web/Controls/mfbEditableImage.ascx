<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbEditableImage" Codebehind="mfbEditableImage.ascx.cs" %>
<div>
    <asp:Literal ID="litVideoOpenTag" runat="server"></asp:Literal>
    <asp:HyperLink ID="lnkFullPicture" runat="server" Target="_blank">
        <asp:Image ID="img" runat="server" />
    </asp:HyperLink>
    <asp:Literal ID="litVideoCloseTag" runat="server"></asp:Literal>
    <div style="display:inline-block; vertical-align:top; margin-left: -3px; background-color: #DDDDDD; border-bottom-right-radius: 8px; border-top-right-radius: 8px;" runat="server" id="divActions">
        <div runat="server" id="divDel"><asp:ImageButton ID="lnkDelete" CssClass="ilToolbarItem" ImageUrl="~/images/x.gif" runat="server" Visible="false" ToolTip="<%$ Resources:LocalizedText, EditableImageDelete %>" AlternateText="<%$ Resources:LocalizedText, EditableImageDelete %>" OnClick="DeleteImage" style="padding: 4px;" /></div>
        <div runat="server" id="divZoom"><asp:HyperLink ID="lnkZoom" CssClass="ilToolbarItem" runat="server" Visible="false"><asp:Image ID="Image1" ImageUrl="~/images/mapmarkersm.png" runat="server" ToolTip="<%$ Resources:LocalizedText, EditableImageViewOnMap %>" AlternateText="<%$ Resources:LocalizedText, EditableImageViewOnMap %>" style="padding: 4px;" /></asp:HyperLink></div>
        <div runat="server" id="divDefault"><asp:ImageButton ID="lnkMakeDefault" runat="server" CssClass="ilToolbarItem" ImageUrl="~/images/favoritesm.png" ToolTip="<%$ Resources:LocalizedText, EditableImageMakeFavorite %>" AlternateText="<%$ Resources:LocalizedText, EditableImageMakeFavorite %>" OnClick="lnkMakeDefault_Click" Visible="false" style="padding: 4px;" /></div>
        <div runat="server" id="divAnnot"><asp:HyperLink ID="lnkAnnotate" CssClass="ilToolbarItem" runat="server" Visible="false"><asp:Image ID="imgEdit" ImageUrl="~/images/pencilsm.png" ToolTip="<%$ Resources:LocalizedText, EditableImageEditPrompt %>" AlternateText="<%$ Resources:LocalizedText, EditableImageEditPrompt %>" runat="server" style="padding: 4px;" /></asp:HyperLink></div>
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
