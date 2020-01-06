<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_FAQ" Codebehind="FAQ.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="cc1" %>
<%@ Register Src="~/Controls/mfbSearchbox.ascx" TagPrefix="uc1" TagName="mfbSearchbox" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" runat="server">
    <% =Branding.ReBrand(Resources.LocalizedText.FAQHeader) %>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
        <ContentTemplate>
            <div style="padding-top:3px;">
                <% =Branding.ReBrand(Resources.LocalizedText.FAQDesc) %>
                <asp:HyperLink ID="lnkContact" runat="server" NavigateUrl="~/Public/ContactMe.aspx" Text="<%$ Resources:LocalizedText, FAQContactUs %>"></asp:HyperLink>
                <div style="display:inline-block; vertical-align:middle;">
                    <uc1:mfbSearchbox runat="server" ID="mfbSearchbox" OnSearchClicked="btnSearch_Click" Hint="<%$ Resources:LocalizedText, FAQSearchWatermark %>" />
                </div>
                <asp:Label ID="lblErr" CssClass="error" runat="server" EnableViewState="false"></asp:Label>
            </div>
            <asp:Repeater ID="rptFAQGroup" runat="server" OnItemDataBound="rptFAQGroup_ItemDataBound">
                <ItemTemplate>
                    <h2><%# Eval("Category") %></h2>
                    <cc1:Accordion ID="accFAQGroup" RequireOpenedPane="false" SelectedIndex="-1" runat="server" HeaderCssClass="accordianHeader" HeaderSelectedCssClass="accordianHeaderSelected" ContentCssClass="accordianContent" TransitionDuration="250" >
                        <HeaderTemplate>
                            <a name='<%# Eval("idFAQ") %>'></a><%# Eval("Question") %>
                        </HeaderTemplate>
                        <ContentTemplate>
                            <div style="padding: 5px">
                                <div style="float:right">
                                    <asp:HyperLink ID="lnkPermalink" NavigateUrl='<%# "~/Public/FAQ.aspx?q=" + Eval("idFAQ") + "#" + Eval("idFAQ") %>' runat="server">
                                        <asp:Image ID="imgPermalink" style="opacity: 0.5;" ToolTip="<%$ Resources:LocalizedText, FAQPermaLink %>" ImageUrl="~/images/Link.png" runat="server" />
                                    </asp:HyperLink>
                                </div>
                                <%# Eval("Answer") %>
                            </div>
                        </ContentTemplate>
                    </cc1:Accordion>
                </ItemTemplate>
            </asp:Repeater>
        </ContentTemplate>
    </asp:UpdatePanel>
    <div><br />&nbsp;<br />&nbsp;<br />&nbsp;</div>
</asp:Content>

