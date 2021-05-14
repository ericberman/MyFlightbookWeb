<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbMiniFacebook.ascx.cs" Inherits="MyFlightbook.SocialMedia.mfbMiniFacebook" %>
<asp:HyperLink ID="lnkFBAdd" runat="server" Target="_blank">
    <asp:Image ID="imgFBButton" ImageUrl="~/images/f_logo_20.png" runat="server" style="padding-right:8px;"
        Tooltip="<%$ Resources:LocalizedText, MiniFacebookAddToFacebook %>" AlternateText="<%$ Resources:LocalizedText, MiniFacebookAddToFacebook %>" ></asp:Image>
    <asp:Label ID="lblPostFB" runat="server" Text="<%$ Resources:LocalizedText, MiniFacebookAddToFacebook %>"></asp:Label>
</asp:HyperLink>