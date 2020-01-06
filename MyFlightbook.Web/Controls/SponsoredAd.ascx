<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_SponsoredAd" Codebehind="SponsoredAd.ascx.cs" %>
<div style="padding: 5px;">
    <div style="text-align:center"><asp:Label ID="lblPleaseVisit" runat="server" Text="<%$ Resources:LocalizedText, SponsoredAdHeader %>" CssClass="fineprint"></asp:Label></div>
    <asp:HyperLink ID="lnkAd" runat="server" Target="_blank">
        <asp:Image ID="imgAd" runat="server" />
    </asp:HyperLink>
</div>
<asp:HiddenField ID="hdnAdID" runat="server" />
