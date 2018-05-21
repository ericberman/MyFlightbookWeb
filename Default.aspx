<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="Public_Home" Title="" %>
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
<asp:content id="Content1" contentplaceholderid="cpTopForm" runat="Server">
    <asp:LinkButton ID="lnkViewMobile" runat="server" Visible="false" OnClick="lnkViewMobile_Click">Switch to Mobile View<br /></asp:LinkButton>
    <asp:Panel ID="pnlWelcome" runat="server" CssClass="welcomeHeader">
        <% =Branding.ReBrand(Resources.Profile.NewAccountPromo) %>
    </asp:Panel>
    <div style="float:right; min-width:250px; margin:5px;"><uc6:mfbFacebookFan ID="mfbFacebookFan1" runat="server" ShowStream="true" /></div>
    <div style="margin-left: auto; margin-right:auto; text-align:center;">
        <asp:Repeater ID="rptFeatures" runat="server">
            <ItemTemplate>
                <div style="border: 3px solid #cccccc; border-radius: 10px; margin: 10px; padding: 10px; height: 120px; vertical-align:middle; display:inline-block; width: 180px;">
                    <div style="text-align:center; font-size:larger; font-weight:bold;"><asp:HyperLink ID="lnkTitle" runat="server" Text='<%# Eval("Title") %>' NavigateUrl='<%# Eval("Link") %>'></asp:HyperLink></div>
                    <div><%# Eval("Description") %></div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>
    <h2><asp:Localize ID="locRecentFlightsHeader" runat="server"></asp:Localize></h2>
    <p><asp:Label ID="lblRecentFlightsStats" Font-Bold="true" runat="server" Text="Label"></asp:Label></p>
    <div style="max-width: 480px; margin-left:auto; margin-right:auto;">
        <uc1:imageSlider runat="server" ID="imageSlider" />
    </div>
    <div style="text-align:center">
        <uc5:mfbGoogleAdSense ID="mfbGoogleAdSense1" runat="server" LayoutStyle="adStyleHorizontal" />
    </div>
</asp:content>
