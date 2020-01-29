<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" CodeBehind="ViewSharedLogbook.aspx.cs" Inherits="MyFlightbook.Web.Public.ViewSharedLogbook" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/mfbLogbook.ascx" TagPrefix="uc1" TagName="mfbLogbook" %>
<%@ Register Src="~/Controls/mfbCurrency.ascx" TagPrefix="uc1" TagName="mfbCurrency" %>
<%@ Register Src="~/Controls/mfbTotalSummary.ascx" TagPrefix="uc1" TagName="mfbTotalSummary" %>
<%@ Register Src="~/Controls/mfbAccordionProxyControl.ascx" TagPrefix="uc1" TagName="mfbAccordionProxyControl" %>
<%@ Register Src="~/Controls/mfbAccordionProxyExtender.ascx" TagPrefix="uc1" TagName="mfbAccordionProxyExtender" %>
<%@ Register Src="~/Controls/mfbSearchForm.ascx" TagPrefix="uc1" TagName="mfbSearchForm" %>
<%@ Register Src="~/Controls/mfbQueryDescriptor.ascx" TagPrefix="uc1" TagName="mfbQueryDescriptor" %>
<%@ Register Src="~/Controls/mfbChartTotals.ascx" TagPrefix="uc1" TagName="mfbChartTotals" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>

<asp:Content id="Content2" contentplaceholderid="cpPageTitle" runat="Server">
    <asp:Label ID="lblHeader" runat="server"></asp:Label>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpTopForm" runat="Server">
    <uc1:mfbAccordionProxyExtender runat="server" ID="mfbAccordionProxyExtender" AccordionControlID="AccordionCtrl" HeaderProxyIDs="apcFilter,apcTotals,apcCurrency,apcAnalysis" />
    <asp:Panel ID="pnlAccordionMenuContainer" CssClass="accordionMenuContainer" runat="server">
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, LogTabFilter %>" ID="apcFilter" />
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, LogTabTotals %>" ID="apcTotals" />
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, LogTabCurrency %>" ID="apcCurrency" />
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, LogTabAnalysis %>" ID="apcAnalysis" />
    </asp:Panel>
    <asp:Panel ID="pnlFilter" runat="server" CssClass="filterApplied" Visible="false" >
        <div style="display:inline-block;"><%=Resources.LocalizedText.ResultsFiltered %></div>
        <uc1:mfbQueryDescriptor runat="server" ID="mfbQueryDescriptor" OnQueryUpdated="mfbQueryDescriptor_QueryUpdated" />
    </asp:Panel>
    <cc1:Accordion ID="AccordionCtrl"  RequireOpenedPane="False" SelectedIndex="-1" runat="server" HeaderCssClass="accordionMenuHeader" HeaderSelectedCssClass="accordionMenuHeaderSelected" ContentCssClass="accordionMenuContent" TransitionDuration="250">
        <Panes>
            <cc1:AccordionPane runat="server" ID="acpPaneFilter">
                <Content>
                    <uc1:mfbSearchForm runat="server" ID="mfbSearchForm" OnQuerySubmitted="ShowResults" OnReset="ClearForm" InitialCollapseState="true" />
                </Content>
            </cc1:AccordionPane>
            <cc1:AccordionPane runat="server" ID="acpPaneTotals">
                <Content>
                    <uc1:mfbTotalSummary runat="server" ID="mfbTotalSummary" Visible="false" LinkTotalsToQuery="false" />
                </Content>
            </cc1:AccordionPane>
            <cc1:AccordionPane runat="server" ID="acpPaneCurrency">
                <Content>
                    <uc1:mfbCurrency runat="server" ID="mfbCurrency" Visible="false" LinkAssociatedResources="false" />
                </Content>
            </cc1:AccordionPane>
            <cc1:AccordionPane runat="server" ID="acpPaneAnalysis">
                <Content>
                    <uc1:mfbChartTotals runat="server" ID="mfbChartTotals" Visible="false" />
                </Content>
            </cc1:AccordionPane>
        </Panes>
    </cc1:Accordion>
    <asp:Label ID="lblErr" CssClass="error" runat="server" Text=""></asp:Label>
    <uc1:mfblogbook runat="server" id="mfbLogbook" Visible="false" IsReadOnly="true" />
</asp:content>