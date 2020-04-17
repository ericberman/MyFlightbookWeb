<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="About.aspx.cs" Inherits="MyFlightbook.Web.Public.About" MasterPageFile="~/MasterPage.master" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" runat="server">
    <% =Branding.ReBrand(Resources.LocalizedText.AboutTitle) %>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" runat="Server">
    <% =Branding.ReBrand(Resources.LocalizedText.About) %>
    <p style="text-align:center"><asp:HyperLink ID="lnkFeatures" runat="server" Font-Bold="true" NavigateUrl="~/Public/FeatureChart.aspx"></asp:HyperLink></p>
    <div style="text-align:center;">
        <asp:HyperLink ID="lnkFacebook" Font-Bold="true" Target="_blank" runat="server">
            <div style="display:inline-block; width: 40px; text-align:center;"><asp:Image ID="imgFacebook" runat="server"
            ImageUrl="~/images/facebookicon.gif" AlternateText="Facebook" 
            ToolTip="Facebook" meta:resourcekey="imgFacebookResource1" /></div>
            <asp:Label ID="lblFollowFacebook" runat="server"></asp:Label>
        </asp:HyperLink>
        <asp:HyperLink ID="lnkTwitter" Font-Bold="true" runat="server" Target="_blank">
            <div style="display:inline-block; width: 40px; text-align:center;"><asp:Image ID="imgTwitter" runat="server" Height="16px" Width="16px" 
                ImageUrl="~/images/twitter20x20.png" AlternateText="Twitter" 
                ToolTip="Twitter" meta:resourcekey="imgTwitterResource1" /></div>
            <asp:Label ID="lblFollowTwitter" runat="server"></asp:Label>
        </asp:HyperLink>
    </div>
    <p style="text-align:center">
        <asp:HyperLink ID="lnkContact" Font-Bold="true" NavigateUrl="~/Public/ContactMe.aspx" 
                runat="server" Text="<%$ Resources:LocalizedText, AboutContact %>"></asp:HyperLink>
    </p>
</asp:Content>
