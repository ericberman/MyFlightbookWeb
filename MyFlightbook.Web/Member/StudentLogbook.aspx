<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="StudentLogbook.aspx.cs" Inherits="MyFlightbook.Instruction.StudentLogbook" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbCurrency.ascx" tagname="mfbCurrency" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbLogbook.ascx" tagname="mfbLogbook" tagprefix="uc3" %>
<%@ Register src="../Controls/mfbSimpleTotals.ascx" tagname="mfbSimpleTotals" tagprefix="uc4" %>
<%@ Register Src="~/Controls/mfbAccordionProxyControl.ascx" TagPrefix="uc1" TagName="mfbAccordionProxyControl" %>
<%@ Register Src="~/Controls/mfbAccordionProxyExtender.ascx" TagPrefix="uc1" TagName="mfbAccordionProxyExtender" %>
<%@ Register Src="~/Controls/mfbTotalSummary.ascx" TagPrefix="uc1" TagName="mfbTotalSummary" %>
<%@ Register Src="~/Controls/mfbSearchForm.ascx" TagPrefix="uc1" TagName="mfbSearchForm" %>
<%@ Register Src="~/Controls/mfbQueryDescriptor.ascx" TagPrefix="uc1" TagName="mfbQueryDescriptor" %>
<%@ Register Src="~/Controls/mfbChartTotals.ascx" TagPrefix="uc1" TagName="mfbChartTotals" %>
<%@ Register Src="~/Controls/mfbEditFlight.ascx" TagPrefix="uc1" TagName="mfbEditFlight" %>
<%@ Register Src="~/Controls/mfbDownload.ascx" TagPrefix="uc1" TagName="mfbDownload" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <div style="padding:5px">
        <asp:Image ID="ib" ImageAlign="AbsMiddle" ImageUrl="~/images/back.png" runat="server" /><asp:HyperLink ID="lnkReturn" runat="server" NavigateUrl="~/Member/Training.aspx/instStudents" Text="<%$ Resources:Profile, ReturnToProfile %>"></asp:HyperLink>
    </div>
    <uc1:mfbAccordionProxyExtender runat="server" ID="mfbAccordionProxyExtender" AccordionControlID="AccordionCtrl" HeaderProxyIDs="apcNewFlight,apcFilter,apcTotals,apcCurrency,apcAnalysis,apcPrintView" />
    <asp:Panel ID="pnlAccordionMenuContainer" CssClass="accordionMenuContainer" runat="server">
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, LogTabNewFlight %>" ID="apcNewFlight" />
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, LogTabFilter %>" ID="apcFilter" />
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, LogTabTotals %>" ID="apcTotals" />
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, LogTabCurrency %>" ID="apcCurrency" />
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, LogTabAnalysis %>" ID="apcAnalysis" LazyLoad="true" OnControlClicked="apcAnalysis_ControlClicked" />
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, LogTabPrint %>" ID="apcPrintView" />
    </asp:Panel>
    <asp:Panel ID="pnlFilter" runat="server" CssClass="filterApplied" >
        <div style="display:inline-block;"><%=Resources.LocalizedText.ResultsFiltered %></div>
        <uc1:mfbQueryDescriptor runat="server" ID="mfbQueryDescriptor" OnQueryUpdated="mfbQueryDescriptor_QueryUpdated" />
    </asp:Panel>
    <ajaxToolkit:Accordion ID="AccordionCtrl"  RequireOpenedPane="False" SelectedIndex="-1" runat="server" HeaderCssClass="accordionMenuHeader" HeaderSelectedCssClass="accordionMenuHeaderSelected" ContentCssClass="accordionMenuContent" TransitionDuration="250">
        <Panes>
            <ajaxToolkit:AccordionPane runat="server" ID="acpPaneNew">
                <Content>
                    <uc1:mfbEditFlight runat="server" ID="mfbEditFlight" OnFlightWillBeSaved="mfbEditFlight_FlightWillBeSaved" OnFlightUpdated="mfbEditFlight_FlightUpdated" />
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane runat="server" ID="acpPaneFilter">
                <Content>
                    <uc1:mfbSearchForm runat="server" ID="mfbSearchForm" OnQuerySubmitted="ShowResults" OnReset="ClearForm" InitialCollapseState="true" />
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane runat="server" ID="acpPaneTotals">
                <Content>
                    <uc1:mfbTotalSummary runat="server" ID="mfbTotalSummary" LinkTotalsToQuery="false" />
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane runat="server" ID="acpPaneCurrency">
                <Content>
                    <uc1:mfbCurrency ID="mfbCurrency1" runat="server" />
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane runat="server" ID="acpPaneAnalysis">
                <Content>
                    <uc1:mfbChartTotals runat="server" ID="mfbChartTotals" Visible="false" />
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane ID="acpPrint" runat="server">
                <Content>
                    <div style="margin-left: auto; margin-right:auto; text-align:center">
                        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                            <ContentTemplate>
                                <div style="padding:5px; margin-left: auto; margin-right:auto; text-align:left; width: 50%">
                                    <h3><%=Resources.LocalizedText.PrintViewTabFilter %></h3>
                                    <table>
                                        <tr>
                                            <td>
                                                <asp:CheckBox ID="ckIncludeCoverSheet" runat="server" Checked="true" AutoPostBack="true"  OnCheckedChanged="IncludeParametersChanged" />
                                            </td>
                                            <td>
                                                <asp:Label ID="lblCoverSheet" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeCoverSheet %>" AssociatedControlID="ckIncludeCoverSheet"></asp:Label>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                <asp:CheckBox ID="ckTotals" runat="server" Checked="true" AutoPostBack="true" OnCheckedChanged="IncludeParametersChanged" />
                                            </td>
                                            <td>
                                                <asp:Label ID="lblIncludeTotals" AssociatedControlID="ckTotals" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeTotals %>"></asp:Label>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                <asp:CheckBox ID="ckEndorsements" runat="server" Checked="true" AutoPostBack="true" OnCheckedChanged="IncludeParametersChanged" />
                                            </td>
                                            <td>
                                                <asp:Label ID="lblIncludeEndorsements" AssociatedControlID="ckEndorsements" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeEndorsements %>"></asp:Label>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td></td>
                                            <td>
                                                <div style="float: left">
                                                    <asp:CheckBox ID="ckIncludeEndorsementImages" runat="server" Checked="true" AutoPostBack="true" OnCheckedChanged="IncludeParametersChanged" /></div>
                                                <div style="float:left">
                                                    <asp:Label ID="lblIncludeWhat" AssociatedControlID="ckIncludeEndorsementImages" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeJPEGEndorsements %>"></asp:Label>
                                                    <div class="fineprint"><% =Resources.LocalizedText.PrintViewNoEmbeddedPDFsNote %></div>
                                                </div>
                                            </td>
                                        </tr>
                                    </table>
                                </div>
                                <div>
                                    <asp:HyperLink ID="lnkPrintView" runat="server" Target="_blank">
                                        <asp:Image ID="imgOpenPrintView" ImageUrl="~/images/rightarrow.png" ImageAlign="Middle" runat="server" />&nbsp;<%=Resources.LocalizedText.OpenPrintView %></asp:HyperLink>
                                </div>
                            </ContentTemplate>
                        </asp:UpdatePanel>

                        <div>
                            <asp:LinkButton ID="lnkDownloadCSV" runat="server" OnClick="lnkDownloadCSV_Click" style="vertical-align:middle">
                                <asp:Image ID="imgDownloadCSV" ImageUrl="~/images/download.png" runat="server" style="padding-right: 5px; vertical-align:middle" />
                                <asp:Image ID="imgCSVIcon" ImageAlign="Middle" runat="server" ImageUrl="~/images/csvicon_sm.png" style="padding-right: 5px; vertical-align:middle;" />
                                <span style="vertical-align:middle"><asp:Localize ID="locDownloadCSV" runat="server" Text="<%$ Resources:LocalizedText, DownloadFlyingStats %>"></asp:Localize></span>
                            </asp:LinkButton>
                        </div>
                        <uc1:mfbDownload runat="server" ID="mfbDownload1" />
                    </div>
                </Content>
            </ajaxToolkit:AccordionPane>
        </Panes>
    </ajaxToolkit:Accordion>
    <asp:Label ID="lblErr" CssClass="error" runat="server" Text=""></asp:Label>
    <asp:Panel ID="pnlLogbook" runat="server">
        <uc3:mfbLogbook ID="mfbLogbook1" runat="server" DetailsPageUrlFormatString="~/Member/FlightDetail.aspx/{0}"
         EditPageUrlFormatString="~/Member/FlightDetail.aspx/{0}" AnalysisPageUrlFormatString="~/Member/FlightDetail.aspx/{0}?tabID=Chart"  />
    </asp:Panel>
    <asp:HiddenField ID="hdnStudent" runat="server" />
</asp:Content>
