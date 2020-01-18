<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SponsoredAd.ascx.cs" Inherits="Controls_SponsoredAd" %>
<div style="padding: 5px;">
    <div style="text-align:center"><asp:Label ID="lblPleaseVisit" runat="server" Text="<%$ Resources:LocalizedText, SponsoredAdHeader %>" CssClass="fineprint"></asp:Label></div>
    <asp:HyperLink ID="lnkAd" runat="server" Target="_blank">
        <asp:Image ID="imgAd" runat="server" />
    </asp:HyperLink>
</div>
<asp:HiddenField ID="hdnAdID" runat="server" />
