<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_Home" Title="" Codebehind="Default.aspx.cs" %>
<%@ Register Src="Controls/mfbCurrency.ascx" TagName="mfbCurrency" TagPrefix="uc3" %>
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
        <div id="homePagePromo" class="welcomeHeader">
            <asp:HyperLink ID="lnkFeature" runat="server" NavigateUrl="~/mvc/pub/FeatureChart">
                <% =Branding.ReBrand(Resources.Profile.NewAccountPromo) %>
            </asp:HyperLink>
        </div>
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
                    <p>
                        <asp:Label ID="locRecentStats" runat="server" Font-Bold="true" />
                        <asp:Repeater ID="rptStats" runat="server">
                            <ItemTemplate>
                                ● <asp:MultiView ID="mvStat" runat="server" ActiveViewIndex='<%# String.IsNullOrWhiteSpace(((LinkedString) Container.DataItem).Link) ? 0 : 1 %>'>
                                    <asp:View ID="v1" runat="server"><%#: ((LinkedString) Container.DataItem).Value %></asp:View>
                                    <asp:View ID="v2" runat="server"><asp:HyperLink ID="lnkStat" runat="server" NavigateUrl='<%# ((LinkedString) Container.DataItem).Link %>' Target="_blank" Text="<%# ((LinkedString) Container.DataItem).Value %>" /></asp:View>
                                  </asp:MultiView>
                            </ItemTemplate>                        
                        </asp:Repeater>
                    </p>
                    <div style="max-width: 480px; margin-left:auto; margin-right:auto;">
                        <uc1:imageSlider runat="server" ID="imageSlider" />
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
