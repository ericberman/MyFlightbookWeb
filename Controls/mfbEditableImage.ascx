<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbEditableImage.ascx.cs" Inherits="Controls_mfbEditableImage" %>
<div>
    <asp:Literal ID="litVideoOpenTag" runat="server"></asp:Literal>
    <asp:HyperLink ID="lnkFullPicture" runat="server" Target="_blank">
        <asp:Image ID="img" runat="server" />
    </asp:HyperLink>
    <asp:Literal ID="litVideoCloseTag" runat="server"></asp:Literal>
    <div style="display:inline-block; vertical-align:top; padding: 0; margin-left: -3px">
        <div><asp:ImageButton ID="lnkDelete" CssClass="ilToolbarItem" ImageUrl="~/images/x.gif" runat="server" Visible="false" ToolTip="<%$ Resources:LocalizedText, EditableImageDelete %>" AlternateText="<%$ Resources:LocalizedText, EditableImageDelete %>" OnClick="DeleteImage" style="margin-bottom: 8px" /></div>
        <div><asp:HyperLink ID="lnkZoom" CssClass="ilToolbarItem" runat="server" Visible="false"><asp:Image ID="Image1" ImageUrl="~/images/mapmarkersm.png" runat="server" ToolTip="<%$ Resources:LocalizedText, EditableImageViewOnMap %>" AlternateText="<%$ Resources:LocalizedText, EditableImageViewOnMap %>" style="margin-bottom: 8px" /></asp:HyperLink></div>
        <div><asp:ImageButton ID="lnkMakeDefault" runat="server" CssClass="ilToolbarItem" ImageUrl="~/images/favoritesm.png" ToolTip="<%$ Resources:LocalizedText, EditableImageMakeFavorite %>" AlternateText="<%$ Resources:LocalizedText, EditableImageMakeFavorite %>" OnClick="lnkMakeDefault_Click" Visible="false" style="margin-bottom: 8px" /></div>
        <div><asp:HyperLink ID="lnkAnnotate" CssClass="ilToolbarItem" runat="server" Visible="false"><asp:Image ID="imgEdit" ImageUrl="~/images/pencilsm.png" ToolTip="<%$ Resources:LocalizedText, EditableImageEditPrompt %>" AlternateText="<%$ Resources:LocalizedText, EditableImageEditPrompt %>" runat="server" style="margin-bottom: 8px" /></asp:HyperLink></div>
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
