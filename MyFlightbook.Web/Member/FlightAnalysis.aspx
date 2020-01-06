<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_FlightAnalysis" Title="Analyze flight data" culture="auto" meta:resourcekey="PageResource1" Codebehind="FlightAnalysis.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Reference Control="~/Controls/mfbLogbookSidebar.ascx" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="../Controls/mfbGoogleMapManager.ascx" TagName="mfbGoogleMapManager"
    TagPrefix="uc1" %>
<%@ Register src="../Controls/GoogleChart.ascx" tagname="GoogleChart" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbAccordionProxyControl.ascx" tagname="mfbAccordionProxyControl" tagprefix="uc3" %>
<%@ Register src="../Controls/mfbAccordionProxyExtender.ascx" tagname="mfbAccordionProxyExtender" tagprefix="uc4" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <h1><asp:Localize ID="locPageHeader" runat="server" Text="Flight Data Analysis" 
            meta:resourcekey="locPageHeaderResource1"></asp:Localize></h1>
    <p>
        <asp:Localize ID="locOverview" runat="server" 
            Text="View telemetry data for your flight graphically, on a map, or in a table.  You can also download it to Excel or other analysis tools." 
            meta:resourcekey="locOverviewResource1"></asp:Localize>
        <asp:HyperLink ID="lnkKey" NavigateUrl="~/Public/FlightDataKey.aspx" 
            runat="server" Target="_blank" Text="Learn More" 
            meta:resourcekey="lnkKeyResource1"></asp:HyperLink>
    </p>
    <p>
        <asp:Localize ID="locImport" runat="server" Text="Got data from other sources that you'd like to attach to a flights?" meta:resourcekey="locImportResource1"></asp:Localize> <asp:HyperLink ID="lnkBulkImport" NavigateUrl="~/Member/ImportTelemetry.aspx" runat="server" Text="Bulk Import Telemetry" meta:resourcekey="lnkBulkImportResource1"></asp:HyperLink>
    </p>
    <div style="padding:5px; margin-left: auto; margin-right:auto; margin-top: 5px; margin-bottom: 15px; text-align:center; background-color:#eeeeee; border: 1px solid darkgray; border-radius: 6px; box-shadow: 6px 6px 5px #888888; ">
        <asp:Label ID="lblFlightDate" Font-Bold="True" runat="server" meta:resourcekey="lblFlightDateResource1"></asp:Label>
        <asp:Label ID="lblFlightDesc" runat="server" meta:resourcekey="lblFlightDescResource1"></asp:Label>
    </div>
    <uc3:mfbAccordionProxyControl ID="mfbAccordionProxyControl1" runat="server" />
    <script>
        function dropPin(p, s) {
            var gm = getMfbMap();
            gm.oms.addMarker(gm.addEventMarker(p, s));
        }
    </script>
    <uc4:mfbAccordionProxyExtender runat="server" ID="mfbAccordionProxyExtender" AccordionControlID="AccordionCtrl" HeaderProxyIDs="apcChart,apcRaw,apcDownload" />
    <asp:Panel ID="pnlAccordionMenuContainer" CssClass="accordionMenuContainer" runat="server" meta:resourcekey="pnlAccordionMenuContainerResource1">
        <uc3:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:Tabs, AnalysisChart %>" ID="apcChart" />
        <uc3:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:Tabs, AnalysisRaw %>" ID="apcRaw" LazyLoad="true" OnControlClicked="apcRaw_ControlClicked" />
        <uc3:mfbAccordionProxyControl runat="server" LabelText="<%$ Resources:Tabs, AnalysisDownload %>" ID="apcDownload" />
    </asp:Panel>
    <ajaxToolkit:Accordion ID="AccordionCtrl"  RequireOpenedPane="False" SelectedIndex="-1" runat="server" 
        HeaderCssClass="accordionMenuHeader" HeaderSelectedCssClass="accordionMenuHeaderSelected" ContentCssClass="accordionMenuContent" TransitionDuration="250" meta:resourcekey="AccordionCtrlResource1" >
        <Panes>
            <ajaxToolkit:AccordionPane runat="server" ID="acpChart" meta:resourcekey="acpChartResource1">
                <Content>
                    <div style="width:90%; margin-left:5%; margin-right:5%; text-align:center;">
                        <asp:Localize ID="locDataToChart" runat="server" Text="Data to chart:" 
                            meta:resourcekey="locDataToChartResource1"></asp:Localize>
                        <br />
                        <asp:DropDownList ID="cmbYAxis1" runat="server" AutoPostBack="True" 
                            OnSelectedIndexChanged="cmbYAxis1_SelectedIndexChanged" 
                            meta:resourcekey="cmbYAxis1Resource1">
                        </asp:DropDownList><br />
                        <asp:Localize ID="locY2AxisSelection" runat="server" Text="2nd data to chart:" 
                            meta:resourcekey="locY2AxisSelectionResource1"></asp:Localize><br />
                        <asp:DropDownList ID="cmbYAxis2" runat="server" AutoPostBack="True" 
                            OnSelectedIndexChanged="cmbYAxis2_SelectedIndexChanged" 
                            meta:resourcekey="cmbYAxis2Resource1">
                        </asp:DropDownList><br />
                        <asp:Localize ID="locXAxisSelection" runat="server" Text="X-axis:" 
                            meta:resourcekey="locXAxisSelectionResource1"></asp:Localize><br />
                        <asp:DropDownList ID="cmbXAxis" runat="server" AutoPostBack="True" 
                            OnSelectedIndexChanged="cmbXAxis_SelectedIndexChanged" 
                            meta:resourcekey="cmbXAxisResource1">
                        </asp:DropDownList>
                        <br />
                        <uc2:GoogleChart ID="gcData" SlantAngle="0" LegendType="bottom" Width="800" Height="500" runat="server" />
                    </div>
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane runat="server" ID="acpRaw" meta:resourcekey="acpRawResource1">
                <Content>
                    <asp:Panel ID="Panel1" runat="server" 
                        style="width:90%; text-align:center; margin-left:5%; margin-right:5%; background-color: #DDDDDD; border: solid 1px gray;" 
                        Height="400px" ScrollBars="Auto" meta:resourcekey="Panel1Resource1">
                        <asp:GridView ID="gvData"  runat="server" CellPadding="3" EnableViewState="False"
                            onrowdatabound="OnRowDatabound" meta:resourcekey="gvDataResource1">
                            <Columns>
                                <asp:TemplateField meta:resourcekey="TemplateFieldResource1">
                                    <ItemTemplate>
                                        <asp:Image ID="imgPin" Height="20px" runat="server" 
                                            ImageUrl="~/Images/Pushpinsm.png" meta:resourcekey="imgPinResource1" /> 
                                        <asp:HyperLink
                                                ID="lnkZoom" runat="server" Text="<%$ Resources:FlightData, ZoomIn %>" meta:resourcekey="lnkZoomResource1"></asp:HyperLink>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Position" 
                                    meta:resourcekey="TemplateFieldResource2">
                                    <ItemTemplate>
                                        <%# ((MyFlightbook.Geography.LatLong) Eval("Position")).ToDegMinSecString() %>
                                    </ItemTemplate>
                                    <ItemStyle HorizontalAlign="Left" />
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </asp:Panel>
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane runat="server" ID="acpDownload" meta:resourcekey="acpDownloadResource1">
                <Content>
                    <div>&nbsp;<asp:Localize ID="locDownloadPrompt" runat="server" 
                            Text="Download this data as:" meta:resourcekey="locDownloadPromptResource1"></asp:Localize>
                        <br />
                        <asp:DropDownList ID="cmbFormat" runat="server" 
                            meta:resourcekey="cmbFormatResource1">
                            <asp:ListItem Selected="True" Value="Original" Text="Original Format" 
                                meta:resourcekey="ListItemResource1"></asp:ListItem>
                            <asp:ListItem Value="CSV" Text="Text (CSV / Spreadsheet)" 
                                meta:resourcekey="ListItemResource2"></asp:ListItem>
                            <asp:ListItem Enabled="False" Value="KML" Text="KML (Google Earth)" 
                                meta:resourcekey="ListItemResource3"></asp:ListItem>
                            <asp:ListItem Enabled="False" Value="GPX" Text="GPX" 
                                meta:resourcekey="ListItemResource4"></asp:ListItem>
                        </asp:DropDownList>
                        <br />
                        <asp:Localize ID="locSpeedUnitsPrompt" runat="server" Text="Speed units are:" 
                            meta:resourcekey="locSpeedUnitsPromptResource1"></asp:Localize>
                        <br />
                        <asp:DropDownList ID="cmbSpeedUnits" runat="server" 
                            meta:resourcekey="cmbSpeedUnitsResource1">
                            <asp:ListItem Selected="True" Value="0" Text="Knots" 
                                meta:resourcekey="ListItemResource5"></asp:ListItem>
                            <asp:ListItem Value="1" Text="Miles/Hour" meta:resourcekey="ListItemResource6"></asp:ListItem>
                            <asp:ListItem Value="2" Text="Meters/Second" 
                                meta:resourcekey="ListItemResource7"></asp:ListItem>
                            <asp:ListItem Value="3" Text="Feet/Second" meta:resourcekey="ListItemResource8"></asp:ListItem>
                        </asp:DropDownList>
                        <br />
                        <asp:Localize ID="locAltUnitsPrompt" runat="server" Text="Altitude units are:" 
                            meta:resourcekey="locAltUnitsPromptResource1"></asp:Localize>
                        <br />
                        <asp:DropDownList ID="cmbAltUnits" runat="server" 
                            meta:resourcekey="cmbAltUnitsResource1">
                            <asp:ListItem Selected="True" Value="0" Text="Feet" 
                                meta:resourcekey="ListItemResource9"></asp:ListItem>
                            <asp:ListItem Value="1" Text="Meters" meta:resourcekey="ListItemResource10"></asp:ListItem>
                        </asp:DropDownList>
                        <br />
                        <br />
                        <asp:Button ID="btnDownload" runat="server" CausesValidation="False" OnClick="btnDownload_Click"
                            Text="Download" meta:resourcekey="btnDownloadResource1" />
                    </div>
                </Content>
            </ajaxToolkit:AccordionPane>
        </Panes>
    </ajaxToolkit:Accordion>
    <asp:Panel ID="pnlErrors" runat="server" Visible="False" CssClass="callout" 
        style="width:90%; margin-left:5%; margin-right:5%; text-align:center; " 
        meta:resourcekey="pnlErrorsResource1">
        <span style="text-align:center; font-weight:bold">
        <asp:Localize ID="locErrorsFound" runat="server" 
            Text="Errors were found in loading flight data" 
            meta:resourcekey="locErrorsFoundResource1"></asp:Localize> 
            <asp:Label ID="lblShowerrors" CssClass="error" runat="server" 
            meta:resourcekey="lblShowerrorsResource1"></asp:Label></span>
        <asp:Panel ID="pnlErrorDetail" runat="server" 
            style="height:0px; overflow:hidden" meta:resourcekey="pnlErrorDetailResource1">
            <asp:Label ID="lblErr" runat="server" 
                meta:resourcekey="lblErrResource1"></asp:Label>
            <cc1:CollapsiblePanelExtender
                ID="CollapsiblePanelExtender1" runat="server" Collapsed="True" TargetControlID="pnlErrorDetail" 
                CollapseControlID="lblShowerrors" ExpandControlID="lblShowerrors" 
                CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>"
                TextLabelID="lblShowerrors" BehaviorID="CollapsiblePanelExtender1"
                >
            </cc1:CollapsiblePanelExtender>
        </asp:Panel>
    </asp:Panel>        
    <asp:Panel ID="pnlMap" runat="server" Visible="False" 
        meta:resourcekey="pnlMapResource1">
        <div style="width: 80%; margin-left: 10%; margin-right: 10%;">
            <asp:HyperLink ID="lnkZoomToFit" runat="server" 
                Text="Zoom to fit all flight data" meta:resourcekey="lnkZoomToFitResource1"></asp:HyperLink>&nbsp;|&nbsp;<asp:LinkButton 
                ID="lnkClearEvents" runat="server" 
                onclick="lnkClearEvents_Click" Text="Clear added markers" 
                meta:resourcekey="lnkClearEventsResource1"></asp:LinkButton>
            <uc1:mfbGoogleMapManager ID="mfbGoogleMapManager1" runat="server" Height="600px" />
            <asp:Panel ID="pnlDistance" runat="server" Visible="False" 
                meta:resourcekey="pnlDistanceResource1">
                <asp:Label ID="lblDistance" runat="server" 
                    meta:resourcekey="lblDistanceResource1"></asp:Label>
            </asp:Panel>
        </div>
    </asp:Panel>
</asp:Content>
