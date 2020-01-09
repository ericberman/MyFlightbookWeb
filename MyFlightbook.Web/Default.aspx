<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_Home" Title="" Codebehind="Default.aspx.cs" %>
<%@ Register Src="Controls/mfbCurrency.ascx" TagName="mfbCurrency" TagPrefix="uc3" %>
<%@ Register Src="Controls/RSSCurrency.ascx" TagName="RSSCurrency" TagPrefix="uc2" %>
<%@ Register Src="Controls/mfbImageList.ascx" TagName="mfbImageList" TagPrefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="Controls/mfbEditFlight.ascx" tagname="mfbEditFlight" tagprefix="uc4" %>
<%@ Register src="Controls/mfbFacebookFan.ascx" tagname="mfbFacebookFan" tagprefix="uc6" %>
<%@ Register src="Controls/mfbGoogleAdSense.ascx" tagname="mfbGoogleAdSense" tagprefix="uc5" %>
<%@ Register Src="~/Controls/imageSlider.ascx" TagPrefix="uc1" TagName="imageSlider" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpMain" runat="Server">
    <asp:Panel ID="pnlWelcome" runat="server">
        <div id="homePagePromo" class="welcomeHeader"><% =Branding.ReBrand(Resources.Profile.NewAccountPromo) %></div>
    </asp:Panel>
    <table style="width:100%">
        <tr style="vertical-align:top">
            <td>
                <div style="margin-left: auto; margin-right:auto; max-width:700px; text-align:center;">
                    <asp:Repeater ID="rptFeatures" runat="server">
                        <ItemTemplate>
                            <div style="display:inline-block; vertical-align:middle; ">
                                <div id="<%# "tabID" + Eval("TabID") %>" class="featureAreaDescriptorIcon">&nbsp;</div>
                                <div style="border: 3px solid #aaaaaa; border-radius: 10px; margin: 10px; padding: 10px; height: 120px; width: 180px;">
                                    <div class="featureAreaDescriptionHeader"><asp:HyperLink ID="lnkTitle" runat="server" Text='<%# Eval("Title") %>' NavigateUrl='<%# Eval("Link") %>'></asp:HyperLink></div>
                                    <div class="featureAreaDescriptionBody"><%# Eval("Description") %></div>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                    <h2><asp:Localize ID="locRecentFlightsHeader" runat="server"></asp:Localize></h2>
                    <p><asp:Hyperlink ID="lblRecentFlightsStats" Font-Bold="true" runat="server" NavigateUrl="~/Public/MyFlights.aspx"></asp:Hyperlink></p>
                    <div style="max-width: 480px; margin-left:auto; margin-right:auto;">
                        <uc1:imageSlider runat="server" ID="imageSlider" />
                    </div>
                    <h2><asp:Localize ID="locRecentStats" runat="server"></asp:Localize></h2>
                    <div style="display:inline-block">
                        <div style="text-align:left">
                            <ul>
                                <asp:Repeater ID="rptStats" runat="server">
                                    <ItemTemplate>
                                        <li><%# Container.DataItem %></li>
                                    </ItemTemplate>                        
                                </asp:Repeater>
                            </ul>
                        </div>
                    </div>
                </div>
            </td>
            <td>
                <div style="min-width:250px; margin:5px;"><uc6:mfbFacebookFan ID="mfbFacebookFan1" runat="server" /></div>
            </td>
        </tr>
    </table>
    <div style="text-align:center">
        <uc5:mfbGoogleAdSense ID="mfbGoogleAdSense1" runat="server" LayoutStyle="adStyleHorizontal" />
    </div>
</asp:content>
