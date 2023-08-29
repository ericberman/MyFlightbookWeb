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
        <asp:HyperLink ID="lnkFeatures" runat="server" Font-Bold="true" NavigateUrl="~/mvc/pub/FeatureChart" style="vertical-align:middle">
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
    <div style="margin-left:auto; margin-right:auto; max-width: 800px; margin-top: 10px; padding: 5px; border-radius: 5px; border: 1px solid gray;">
        <h2><asp:Label ID="locRecentStats" runat="server" /></h2>
        <ul>
            <asp:Repeater ID="rptStats" runat="server">
                <ItemTemplate>
                    <li>
                        <asp:MultiView ID="mvStat" runat="server" ActiveViewIndex='<%# String.IsNullOrWhiteSpace(((LinkedString) Container.DataItem).Link) ? 0 : 1 %>'>
                            <asp:View ID="v1" runat="server"><%#: ((LinkedString) Container.DataItem).Value %></asp:View>
                            <asp:View ID="v2" runat="server"><asp:HyperLink ID="lnkStat" runat="server" NavigateUrl='<%# ((LinkedString) Container.DataItem).Link %>' Target="_blank" Text="<%# ((LinkedString) Container.DataItem).Value %>" /></asp:View>
                        </asp:MultiView>
                    </li>
                </ItemTemplate>                        
            </asp:Repeater>
        </ul>
        <asp:Panel ID="pnlLazyStats" runat="server" Visible="false" style="margin-left:auto; margin-right: auto;">
            <div style="display:inline-block; vertical-align:top;">
                <h3><asp:Label ID="lblAp" runat="server" Text="<%$ Resources:LocalizedText, DefaultPageRecentStatsPopularAirports %>" /></h3>
                <ol>
                    <asp:Repeater ID="rptTopAirports" runat="server">
                        <ItemTemplate>
                            <li>
                                <div><asp:HyperLink ID="lnkAp" runat="server" Target="_blank" Text='<%# Eval("FullName") %>' NavigateUrl='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Public/MapRoute2.aspx?sm=1&Airports={0}", Eval("Code")) %>' /></div>
                                <div class="fineprint"><asp:Label ID="lblApStat" runat="server" Text='<%# Eval("StatsDisplay") %>' /> <asp:Image ID="imgMods" runat="server" ImageUrl="~/images/expand.png" /></div>
                                <asp:Panel ID="pnlMods" runat="server">
                                    <asp:Repeater ID="rptModes" runat="server" DataSource='<%# Eval("ModelsUsed") %>'>
                                        <ItemTemplate>
                                            <div><%# Container.DataItem.ToString() %></div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </asp:Panel>
                                <ajaxToolkit:CollapsiblePanelExtender ID="cpeMods" runat="server" Collapsed="true" CollapseControlID="imgMods" ExpandControlID="imgMods" CollapsedImage="~/images/expand.png" ExpandedImage="~/images/collapse.png"
                                    TargetControlID="pnlMods" ImageControlID="imgMods" />
                            </li>
                        </ItemTemplate>
                    </asp:Repeater>
                </ol>
            </div>
            <div style="display:inline-block; vertical-align:top;">
                <h3><asp:Label ID="lblModel" runat="server" Text="<%$ Resources:LocalizedText, DefaultPageRecentStatsPopularModels %>" /></h3>
                <ol>
                    <asp:Repeater ID="rptTopModels" runat="server">
                        <ItemTemplate>
                            <li><%# Container.DataItem %></li>
                        </ItemTemplate>
                    </asp:Repeater>
                </ol>
            </div>
        </asp:Panel>
    </div>
</asp:Content>
