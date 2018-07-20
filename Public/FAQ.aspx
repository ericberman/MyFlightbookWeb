<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="FAQ.aspx.cs" Inherits="Public_FAQ" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="cc1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" runat="server">
    <% =Branding.ReBrand(Resources.LocalizedText.FAQHeader) %>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
        <ContentTemplate>
            <asp:Panel ID="pnlSearch" runat="server" DefaultButton="btnSearch" style="padding-top:3px;">
                <% =Branding.ReBrand(Resources.LocalizedText.FAQDesc) %>
                <asp:HyperLink ID="lnkContact" runat="server" NavigateUrl="~/Public/ContactMe.aspx" Text="<%$ Resources:LocalizedText, FAQContactUs %>"></asp:HyperLink>
                <div style="display:inline-block; vertical-align:middle;">
                    <div style="border: 1px solid darkgray; border-radius: 14px; height: 24px; display: table-cell; vertical-align: middle; text-align:left; padding-left: 8px; padding-right:3px; ">
                        <asp:Image ID="Image1" runat="server" ImageUrl="~/images/Search.png" ImageAlign="AbsMiddle" Height="20px" />
                        <asp:TextBox ID="txtSearch" runat="server" Width="120px" Font-Size="8pt" BorderStyle="None" style="vertical-align:middle"></asp:TextBox>
                        <cc1:TextBoxWatermarkExtender
                            ID="TextBoxWatermarkExtender1" runat="server" TargetControlID="txtSearch" EnableViewState="false"
                            WatermarkText="<%$ Resources:LocalizedText, FAQSearchWatermark %>" WatermarkCssClass="watermark">
                        </cc1:TextBoxWatermarkExtender>
                    </div>
                </div>
                <asp:Button ID="btnSearch" style="display:none" runat="server" Text="<%$ Resources:LocalizedText, SearchBoxGo %>" CausesValidation="false" onclick="btnSearch_Click" Font-Size="9px" CssClass="itemlabel" />
                <asp:Label ID="lblErr" CssClass="error" runat="server" EnableViewState="false"></asp:Label>
            </asp:Panel>
            <asp:Repeater ID="rptFAQGroup" runat="server" OnItemDataBound="rptFAQGroup_ItemDataBound">
                <ItemTemplate>
                    <h2><%# Eval("Category") %></h2>
                    <cc1:Accordion ID="accFAQGroup" RequireOpenedPane="false" SelectedIndex="-1" runat="server" HeaderCssClass="accordianHeader" HeaderSelectedCssClass="accordianHeaderSelected" ContentCssClass="accordianContent" TransitionDuration="250" >
                        <HeaderTemplate>
                            <%# Eval("Question") %>
                        </HeaderTemplate>
                        <ContentTemplate>
                            <div style="padding: 5px">
                                <a name='<%# Eval("idFAQ") %>'></a>
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

