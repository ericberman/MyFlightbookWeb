<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbFileUpload.ascx.cs" Inherits="MyFlightbook.Controls.ImageControls.mfbFileUpload" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<asp:Panel ID="Panel1" runat="server">
    <div style="width:225px; display:inline-block;"><asp:FileUpload ID="FileUpload1" runat="server" /></div>
    <div style="display:inline-block">
        <asp:TextBox ID="txtComment" runat="server" placeholder="<%$ Resources:LocalizedText, FileUploadComment %>" />
    </div>
    <asp:Panel ID="pnlAddAnother" runat="server" Visible="false"><asp:HyperLink ID="lnkAddAnother" NavigateUrl="#" runat="server" Text="<%$ Resources:LocalizedText, FileUploadAddAnother %>"></asp:HyperLink></asp:Panel>
</asp:Panel>