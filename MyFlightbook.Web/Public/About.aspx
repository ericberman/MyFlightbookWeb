<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="About.aspx.cs" Inherits="MyFlightbook.Web.PublicPages.About" MasterPageFile="~/MasterPage.master" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" runat="server">
    <% =Branding.ReBrand(Resources.LocalizedText.AboutTitle) %>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" runat="Server">
    <% =Branding.ReBrand(Resources.LocalizedText.About) %>
    <p style="text-align:center">
        <asp:Image ID="imgLogo2" runat="server" ImageUrl="~/images/mfbicon.png" style="vertical-align:middle;" />
        <asp:HyperLink ID="lnkContact" NavigateUrl="~/Public/ContactMe.aspx" style="margin-right: 10px; font-weight:bold;" runat="server" Text="<%$ Resources:LocalizedText, AboutContact %>">
        </asp:HyperLink>
        
        <asp:Image ID="imgLogo" runat="server" ImageUrl="~/images/mfbicon.png" style="vertical-align:middle;" />
        <asp:HyperLink ID="lnkFeatures" runat="server" Font-Bold="true" NavigateUrl="~/Public/FeatureChart.aspx" style="vertical-align:middle">
        </asp:HyperLink>
    </p>
    <div style="text-align:center;">
        <asp:Image ID="imgFacebook" runat="server" style="vertical-align:middle"
            ImageUrl="~/images/f_logo_32.png" AlternateText="Facebook"
            ToolTip="Facebook" />&nbsp;
        <asp:HyperLink ID="lnkFacebook" Target="_blank" runat="server" style="margin-right: 10px; font-weight:bold">
            <asp:Label ID="lblFollowFacebook" runat="server" style="vertical-align:middle"></asp:Label>
        </asp:HyperLink>
        <asp:Image ID="imgTwitter" runat="server" style="vertical-align:middle" 
            ImageUrl="~/images/twitter_round_32.png" AlternateText="Twitter" 
            ToolTip="Twitter" />&nbsp;
        <asp:HyperLink ID="lnkTwitter" Font-Bold="true" runat="server" Target="_blank">
            <asp:Label ID="lblFollowTwitter" runat="server" style="vertical-align:middle"></asp:Label>
        </asp:HyperLink>
    </div>
    <p style="text-align:center">
        <asp:Label ID="locRecentStats" runat="server" Font-Bold="true"></asp:Label>
        <asp:Repeater ID="rptStats" runat="server">
            <ItemTemplate>
                ● <%# Container.DataItem %></li>
            </ItemTemplate>                        
        </asp:Repeater>
    </p>
    <p style="text-align:center"><asp:Hyperlink ID="lblRecentFlightsStats" Font-Bold="true" runat="server" NavigateUrl="~/Public/MyFlights.aspx"></asp:Hyperlink></p>
</asp:Content>
