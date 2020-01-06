<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbFileUpload" Codebehind="mfbFileUpload.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<asp:Panel ID="Panel1" runat="server">
    <div style="width:225px; display:inline-block;"><asp:FileUpload ID="FileUpload1" runat="server" /></div>
    <div style="display:inline-block">
        <asp:TextBox ID="txtComment" runat="server"></asp:TextBox>
        <asp:TextBoxWatermarkExtender ID="TextBoxWatermarkExtender1" runat="server" WatermarkText="<%$ Resources:LocalizedText, FileUploadComment %>" WatermarkCssClass="watermark" TargetControlID="txtComment"></asp:TextBoxWatermarkExtender>
    </div>
    <asp:Panel ID="pnlAddAnother" runat="server"><asp:HyperLink ID="lnkAddAnother" NavigateUrl="#" runat="server" Text="<%$ Resources:LocalizedText, FileUploadAddAnother %>"></asp:HyperLink></asp:Panel>
</asp:Panel>