<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" CodeFile="PostFlight.aspx.cs" Inherits="Member_PostFlight" %>
<%@ Register src="../Controls/mfbTwitter.ascx" tagname="mfbTwitter" tagprefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:content id="Content1" contentplaceholderid="cpMain" runat="Server">
    <uc1:mfbTwitter ID="mfbTwitter" runat="server" />
        <p><asp:Label ID="Label1" runat="server" Text="<%$ Resources:LocalizedText, FacebookAuthRedir %>"></asp:Label>
        <asp:HyperLink ID="HyperLink1" NavigateUrl="~/Default.aspx" runat="server" Text="<%$ Resources:LocalizedText, FacebookAuthRedirNotHappening %>"></asp:HyperLink></p>
</asp:content>
