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
<%@ Register Src="~/Controls/mfbRecentAchievements.ascx" TagPrefix="uc1" TagName="mfbRecentAchievements" %>
<%@ Register Src="~/Controls/mfbBadgeSet.ascx" TagPrefix="uc1" TagName="mfbBadgeSet" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>
<%@ Register Src="~/Controls/mfbGoogleMapManager.ascx" TagPrefix="uc1" TagName="mfbGoogleMapManager" %>





<asp:Content id="Content2" contentplaceholderid="cpPageTitle" runat="Server">
    <asp:Label ID="lblHeader" runat="server"></asp:Label>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpTopForm" runat="Server">
    <uc1:mfbAccordionProxyExtender runat="server" ID="mfbAccordionProxyExtender" AccordionControlID="AccordionCtrl" HeaderProxyIDs="apcFilter,apcTotals,apcCurrency,apcAnalysis,apcAchievements,apcAirports" />
    <asp:Panel ID="pnlAccordionMenuContainer" CssClass="accordionMenuContainer" runat="server">
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, LogTabFilter %>" ID="apcFilter" />
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, LogTabTotals %>" ID="apcTotals" />
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, LogTabCurrency %>" ID="apcCurrency" />
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, LogTabAnalysis %>" ID="apcAnalysis" />
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, ShareKeyPermissionViewAchievements %>" ID="apcAchievements" />
        <uc1:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:LocalizedText, ShareKeyPermissionViewAirports %>" ID="apcAirports" />
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
            <cc1:AccordionPane runat="server" ID="acpPaneAchievements">
                <Content>
                    <asp:MultiView ID="mvBadges" runat="server" Visible="false">
                        <asp:View ID="vwNoBadges" runat="server">
                            <p><asp:Label ID="lblNoBadges" runat="server" Text="<%$ Resources:Achievements, errNoBadgesEarned %>"></asp:Label></p>
                        </asp:View>
                        <asp:View ID="vwBadges" runat="server">
                            <asp:Repeater ID="rptBadgeset" runat="server">
                                <ItemTemplate>
                                    <div><uc1:mfbBadgeSet runat="server" ID="mfbBadgeSet" BadgeSet='<%# Container.DataItem %>' IsReadOnly="true" /></div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </asp:View>
                    </asp:MultiView>
                    <h2><asp:Label ID="lblRecentAchievementsTitle" runat="server"></asp:Label></h2>
                    <uc1:mfbRecentAchievements runat="server" ID="mfbRecentAchievements" Visible="false" IsReadOnly="true" AutoDateRange="true" />
                </Content>
            </cc1:AccordionPane>
            <cc1:AccordionPane runat="server" ID="acpPaneAirports">
                <Content>
                    <p>
                        <asp:Label ID="lblNumAirports" runat="server" Font-Bold="True"></asp:Label>  
                    </p>
                    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                        <ContentTemplate>
                            <asp:Panel ID="Panel1" runat="server" ScrollBars="Vertical" Height="300px">
                                <asp:GridView ID="gvAirports" runat="server" AllowSorting="True" 
                                    AutoGenerateColumns="False" EnableModelValidation="True" OnRowDataBound="gvAirports_DataBound"
                                    BorderStyle="None" onsorting="gvAirports_Sorting"
                                    CellPadding="3" GridLines="None">
                                    <AlternatingRowStyle BackColor="#E0E0E0" />
                                    <Columns>
                                        <asp:TemplateField HeaderText="<%$ Resources:Airports, airportCode %>" SortExpression="Code">
                                            <ItemTemplate>
                                                <asp:PlaceHolder ID="plcZoomCode" runat="server"></asp:PlaceHolder>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField DataField="FacilityName" HeaderText="<%$ Resources:Airports, airportName %>" SortExpression="FacilityName" />
                                        <asp:TemplateField SortExpression="NumberOfVisits">
                                            <HeaderTemplate>
                                                <asp:LinkButton ID="lnkVisits" runat="server" CommandArgument="NumberOfVisits" CommandName="Sort"  Text="<%$ Resources:Airports, airportVisits %>"></asp:LinkButton>
                                                <span style="font-weight:normal"><uc1:mfbTooltip ID="mfbTooltip1" runat="server" BodyContent="<%$ Resources:Airports, vistedAirportsCountTip %>" /></span>
                                                </HeaderTemplate>
                                                <ItemTemplate>
                                                    <%# Eval("NumberOfVisits") %>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        <asp:BoundField DataField="EarliestVisitDate" DataFormatString="{0:d}" 
                                            HeaderText="<%$ Resources:Airports, airportEarliestVisit %>"  
                                            SortExpression="EarliestVisitDate" />
                                        <asp:BoundField DataField="LatestVisitDate" DataFormatString="{0:d}" 
                                            HeaderText="<%$ Resources:Airports, airportLatestVisit %>"  
                                            SortExpression="LatestVisitDate" />
                                    </Columns>
                                    <HeaderStyle HorizontalAlign="Center"></HeaderStyle>
                                    <RowStyle VerticalAlign="Top" />
                                </asp:GridView>
                            </asp:Panel>
                            <asp:HiddenField ID="hdnLastSortDirection" runat="server" Value="0" />
                            <asp:HiddenField ID="hdnLastSortExpression" runat="server" />
                            <table style="width:100%">
                                <tr style="vertical-align:top;">
                                    <td style="width:50%; padding: 3px;">
                                        <asp:Panel ID="pnlViewGoogleEarth" runat="server">
                                            <asp:LinkButton ID="lnkViewKML" runat="server" onclick="btnGetTotalKML">
                                                <asp:Image ID="imgDownloadKML" ImageUrl="~/images/download.png" runat="server" ImageAlign="Middle" style="padding-right: 5px;" />
                                                <asp:Image ID="imgKMLIcon" ImageAlign="Middle" runat="server" ImageUrl="~/images/kmlicon_med.png" style="padding-right: 5px;" />
                                                <div style="display:inline-block;vertical-align:middle"><asp:Localize ID="locViewGoogleEarth" runat="server" Text="<%$ Resources:Airports, DownloadKML %>"></asp:Localize><br /><asp:Label ID="locKMLSlow" runat="server" Text="<%$ Resources:Airports, WarningSlow %>"></asp:Label></div> 
                                            </asp:LinkButton>
                                        </asp:Panel>
                                    </td>
                                    <td style="width:50%; padding: 3px;">
                                        <asp:UpdatePanel ID="UpdatePanel2" runat="server">
                                            <ContentTemplate>
                                                <div>
                                                    <asp:LinkButton ID="btnEstimateDistance" runat="server" EnableViewState="false" onclick="btnEstimateDistance_Click">
                                                        <asp:Image ID="Image1" ImageUrl="~/images/download.png" runat="server" ImageAlign="Middle" style="padding-right: 5px; visibility:hidden;" />
                                                        <asp:Image ID="Image2" ImageAlign="Middle" runat="server" ImageUrl="~/images/ruler.png" style="padding-right: 5px;" />
                                                        <div style="display:inline-block;vertical-align:middle"><asp:Localize ID="locEstimateDistance" runat="server" Text="<%$ Resources:Airports, EstimateDistance %>"></asp:Localize><br /><asp:Label ID="lblSlow" runat="server" Text="<%$ Resources:Airports, WarningSlow %>"></asp:Label></div>
                                                    </asp:LinkButton>
                                                    <asp:Label ID="Label1" runat="server" CssClass="error"></asp:Label>
                                                    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel2">
                                                        <ProgressTemplate>
                                                            <p>
                                                                <asp:Label ID="lblComputing" runat="server" Font-Bold="True" meta:resourcekey="lblComputingResource1" Text="<%$ Resources:Airports, visitedAirportComputing %>"></asp:Label>
                                                            </p>
                                                            <p>
                                                                <asp:Image ID="imgProgress" runat="server" ImageUrl="~/images/ajax-loader.gif" />
                                                            </p>
                                                        </ProgressTemplate>
                                                    </asp:UpdateProgress>
                                                    <asp:Panel ID="pnlDistanceResults" runat="server" EnableViewState="false" meta:resourcekey="pnlDistanceResultsResource1" Visible="False">
                                                        <asp:Label ID="lblDistanceEstimate" runat="server" Font-Bold="True" meta:resourcekey="lblDistanceEstimateResource1"></asp:Label>
                                                        <br />
                                                        <asp:Label ID="lblNote2" runat="server" Font-Bold="True" meta:resourcekey="lblNote2Resource1" Text="<%$ Resources:LocalizedText, Note %>"></asp:Label>
                                                        <asp:Localize ID="locDistance" runat="server" meta:resourcekey="locDistanceResource1" Text="Estimate is based on airport-to-airport distance in the route of your flight or telemetry, if present."></asp:Localize>
                                                    </asp:Panel>
                                                </div>
                                            </ContentTemplate>
                                        </asp:UpdatePanel>
                                    </td>
                                </tr>
                            </table>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                    <p>
                        <asp:HyperLink ID="lnkZoomOut" runat="server" Text="<%$ Resources:Airports, MapZoomAllAirports %>"></asp:HyperLink>
                    </p>
                    <div style="width:100%;">
                        <uc1:mfbGoogleMapManager ID="mfbGoogleMapManager1" ShowRoute="false" runat="server" AllowResize="false" Height="400px" />
                        <br />
                        <br />
                    </div>
                </Content>
            </cc1:AccordionPane>
        </Panes>
    </cc1:Accordion>
    <asp:Label ID="lblErr" CssClass="error" runat="server" Text=""></asp:Label>
</asp:content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
    <uc1:mfblogbook runat="server" id="mfbLogbook" Visible="false" IsReadOnly="true" />
</asp:Content>