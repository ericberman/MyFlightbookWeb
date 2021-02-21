<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    Codebehind="8710Form.aspx.cs" Inherits="Member_8710Form" culture="auto" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbLogbook.ascx" TagName="mfbLogbook" TagPrefix="uc6" %>
<%@ Register src="../Controls/mfbSearchAndTotals.ascx" tagname="mfbSearchAndTotals" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbSearchForm.ascx" tagname="mfbSearchForm" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbQueryDescriptor.ascx" tagname="mfbQueryDescriptor" tagprefix="uc3" %>
<%@ Register Src="~/Controls/mfbTotalsByTimePeriod.ascx" TagPrefix="uc1" TagName="mfbTotalsByTimePeriod" %>
<%@ Register Src="~/Controls/mfbAccordionProxyControl.ascx" TagPrefix="uc1" TagName="mfbAccordionProxyControl" %>
<%@ Register Src="~/Controls/mfbAccordionProxyExtender.ascx" TagPrefix="uc1" TagName="mfbAccordionProxyExtender" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server"><asp:Label ID="lblUserName" runat="server"></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <uc1:mfbAccordionProxyExtender runat="server" AccordionControlID="accReports" id="mfbAccordionProxyExtender" HeaderProxyIDs="apcFilter,apc8710,apcModelRollup,apcTimeRollup" />
    <asp:Panel ID="pnlAcc" runat="server" CssClass="accordionMenuContainer">
        <uc1:mfbAccordionProxyControl runat="server" id="apcFilter" LabelText="<%$ Resources:LocalizedText, LogTabFilter %>" />
        <uc1:mfbAccordionProxyControl runat="server" id="apc8710" LabelText="<%$ Resources:Totals, CommonReports8710 %>"/>
        <uc1:mfbAccordionProxyControl runat="server" id="apcModelRollup" LabelText="<%$ Resources:Totals, CommonReportsAirline %>"/>
        <uc1:mfbAccordionProxyControl runat="server" id="apcTimeRollup" LabelText="<%$ Resources:Totals, CommonReportsByTime %>"/>
    </asp:Panel>
    <asp:Panel ID="pnlFilter" runat="server" CssClass="filterApplied" Visible="false" >
        <div style="display:inline-block;"><%=Resources.LocalizedText.ResultsFiltered %></div>
        <uc3:mfbQueryDescriptor ID="mfbQueryDescriptor1" runat="server" ShowEmptyFilter="true" OnQueryUpdated="mfbQueryDescriptor1_QueryUpdated" />
    </asp:Panel>
    <asp:HiddenField ID="hdnLastViewedPaneIndex" runat="server" />
    <script>
        function onAccordionPaneShown(idx) {
            if (idx != 0)
                document.getElementById("<% =hdnLastViewedPaneIndex.ClientID %>").value = idx;
        }
    </script>
    <ajaxToolkit:Accordion ID="accReports" SelectedIndex="1" RequireOpenedPane="false" runat="server" HeaderCssClass="accordionMenuHeader" HeaderSelectedCssClass="accordionMenuHeaderSelected" ContentCssClass="accordionMenuContent" TransitionDuration="250">
        <Panes>
            <ajaxToolkit:AccordionPane ID="acpFilter" runat="server">
                <Content>
                    <div style="margin-left:auto; margin-right:auto;">
                        <uc2:mfbSearchForm ID="mfbSearchForm1" InitialCollapseState="True" runat="server" OnQuerySubmitted="ShowResults" OnReset="ClearForm" />
                    </div>
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane ID="acp8710" runat="server">
                <Content>
                    <asp:GridView ID="gv8710" runat="server" Font-Size="8pt" CellPadding="0" GridLines="None" OnRowDataBound="gv8710_RowDataBound"
                    AutoGenerateColumns="False" style="margin-left:auto; margin-right:auto; margin-top: 10px; margin-bottom: 10px;">
                        <Columns>
                            <asp:BoundField DataField="Category">
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Center"></ItemStyle>
                            </asp:BoundField>
                            <asp:TemplateField HeaderText="<%$ Resources:Totals, Total %>">
                                <ItemTemplate>
                                    <%# Eval("TotalTime").FormatDecimal(UseHHMM, false) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Totals, 8710InstructionReceived %>">
                                <ItemTemplate>
                                    <%# Eval("InstructionReceived").FormatDecimal(UseHHMM, false)%>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Totals, 8710Solo %>">
                                <ItemTemplate>
                                    <%# Eval("SoloTime").FormatDecimal(UseHHMM, false)%>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Totals, 8710PICAndSIC %>">
                                <ItemTemplate>
                                    <table style="width: 100%; font-size:8pt; border-spacing:0px">
                                        <tr>
                                            <td style="border-bottom: 1px solid black; padding: 3px">
                                                <asp:Localize ID="locPICMain" runat="server" Text="<%$ Resources:Totals, PIC %>" /></td>
                                            <td style="border-bottom: 1px solid black; padding:3px"><%# Eval("PIC").FormatDecimal(UseHHMM, false)%>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="padding: 3px">
                                                <asp:Localize ID="locSICHeaderPIC" runat="server" Text="<%$ Resources:Totals, SIC %>" />
                                            </td>
                                            <td style="padding: 3px">
                                                <%# Eval("SIC").FormatDecimal(UseHHMM, false)%>
                                            </td>
                                        </tr>
                                    </table>
                                </ItemTemplate>
                                <ItemStyle CssClass="PaddedCell" />
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                            </asp:TemplateField>
                            <asp:TemplateField>
                                <HeaderTemplate>
                                    <asp:Localize ID="locCrossCountryDualHeader" runat="server" 
                                        Text="<%$ Resources:Totals, 8710XCDual %>" /><span class="FootNote">1</span>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <%# Eval("CrossCountryDual").FormatDecimal(UseHHMM, false)%>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField>
                                <HeaderTemplate>
                                    <asp:Localize ID="locXCSoloHeader" runat="server" 
                                        Text="<%$ Resources:Totals, 8710XCSolo %>" /><span class="FootNote">1</span>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <%# Eval("CrossCountrySolo").FormatDecimal(UseHHMM, false)%>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField>
                                <HeaderTemplate>
                                    <asp:Localize ID="locXCPICHeader" runat="server" 
                                        Text="<%$ Resources:Totals, 8710XCPICSIC %>" /><span class="FootNote">1</span>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <table style="border-spacing:0; width: 100%; font-size:8pt">
                                        <tr>
                                            <td style="border-bottom: 1px solid black; padding: 3px">
                                                <asp:Localize ID="locPICHeader" runat="server" Text="<%$ Resources:Totals, PIC %>" /></td>
                                                <td style="border-bottom: 1px solid black; padding: 3px"><%# Eval("CrossCountryPIC").FormatDecimal(UseHHMM, false)%>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="padding: 3px">
                                                <asp:Localize ID="locSICHeaderXCPIC" runat="server" Text="<%$ Resources:Totals, SIC %>" /></td>
                                            <td style="padding: 3px"><%# Eval("CrossCountrySIC").FormatDecimal(UseHHMM, false)%>
                                            </td>
                                        </tr>
                                    </table>
                                </ItemTemplate>
                                <ItemStyle CssClass="PaddedCell" />
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                            </asp:TemplateField>
                            <asp:TemplateField>
                                <HeaderTemplate>
                                    <asp:Localize ID="locInstrumentHeader" runat="server" Text="<%$ Resources:Totals, 8710Instrument %>" /><span class="FootNote">2</span>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <%# Eval("InstrumentTime").FormatDecimal(UseHHMM, false)%>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField>
                                <HeaderTemplate>
                                    <asp:Localize ID="locNightDual" runat="server" 
                                        Text="<%$ Resources:Totals, 8710NightDual %>"></asp:Localize><span class="FootNote">1</span>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <%# Eval("NightDual").FormatDecimal(UseHHMM, false)%>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Totals, 8710NightTakeoffLandings %>">
                                <ItemTemplate>
                                    <%# Eval("NightTakeoffs") %>
                                    /
                                    <%# Eval("NightLandings") %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Center" />
                            </asp:TemplateField>
                            <asp:TemplateField>
                                <HeaderTemplate><asp:Localize ID="locNightPICHeader" runat="server" 
                                        Text="<%$ Resources:Totals, 8710NightPICSIC %>" /><span class="FootNote">1</span>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <table style="width: 100%; font-size:8pt; border-spacing: 0px;">
                                        <tr>
                                            <td style="border-bottom: 1px solid black; padding: 3px">
                                                <asp:Localize ID="locPICNight" runat="server" Text="<%$ Resources:Totals, PIC %>"></asp:Localize>
                                            </td>
                                            <td style="border-bottom: 1px solid black; padding: 3px">
                                                <%# Eval("NightPIC").FormatDecimal(UseHHMM, false)%>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="padding: 3px">
                                                <asp:Localize ID="locSICNight" runat="server" Text="<%$ Resources:Totals, SIC %>"></asp:Localize>
                                            </td>
                                            <td style="padding: 3px">
                                                <%# Eval("NightSIC").FormatDecimal(UseHHMM, false)%>       
                                            </td>
                                        </tr>
                                    </table>
                                </ItemTemplate>
                                <ItemStyle CssClass="PaddedCell" />
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                            </asp:TemplateField>
                            <asp:TemplateField>
                                <HeaderTemplate><asp:Localize ID="locNightTakeoffLandingHeader" runat="server" 
                                        Text="<%$ Resources:Totals, 8710NightPICTakeoffLanding %>"></asp:Localize><span class="FootNote">3</span></HeaderTemplate>
                                <ItemTemplate>
                                    <table style="width: 100%; font-size:8pt; border-spacing: 0px">
                                        <tr>
                                            <td style="border-bottom: 1px solid black;padding: 3px">
                                                <asp:Localize ID="locNightLandingPIC" runat="server" Text="<%$ Resources:Totals, PIC %>"></asp:Localize>
                                            </td>
                                            <td style="border-bottom: 1px solid black;padding: 3px">
                                                <%# Eval("NightPICTakeoffs") %>&nbsp;/&nbsp;<%# Eval("NightPICLandings") %></td>
                                        </tr>
                                        <tr>
                                            <td style="padding: 3px">
                                                <asp:Localize ID="locNightLandingSIC" runat="server" Text="<%$ Resources:Totals, SIC %>"></asp:Localize>
                                            </td>
                                            <td style="padding: 3px">
                                                <%# Eval("NightSICTakeoffs") %>&nbsp;/&nbsp;<%# Eval("NightSICLandings") %></td>
                                        </tr>
                                    </table>
                                </ItemTemplate>
                                <ItemStyle CssClass="PaddedCell" />
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Totals, 8710ClassTotals %>">
                                <ItemTemplate>
                                    <asp:Panel ID="pnlClassTotals" runat="server" Visible="False" style="width:100%;">
                                        <table style="border-collapse:collapse; width: 100%; font-size:smaller">
                                            <tr>
                                                <td style="border-right: 1px solid darkgray;"></td>
                                                <td style="border-right: 1px solid darkgray;"><% =Resources.LogbookEntry.FieldTotal %></td>
                                                <td style="border-right: 1px solid darkgray;"><% =Resources.LogbookEntry.FieldPIC %></td>
                                                <td><% =Resources.LogbookEntry.FieldSIC %></td>
                                            </tr>
                                            <asp:Repeater ID="rptClassTotals" runat="server">
                                                <ItemTemplate>
                                                    <tr>
                                                        <td style="border-right: 1px solid darkgray; border-top: 1px solid darkgray;"><%# Eval("ClassName") %></td>
                                                        <td style="border-right: 1px solid darkgray; border-top: 1px solid darkgray;"><%# Eval("Total").FormatDecimal(UseHHMM, false) %></td>
                                                        <td style="border-right: 1px solid darkgray; border-top: 1px solid darkgray;"><%# Eval("PIC").FormatDecimal(UseHHMM, false) %></td>
                                                        <td style="border-top: 1px solid darkgray;"><%# Eval("SIC").FormatDecimal(UseHHMM, false) %></td>
                                                    </tr>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </table>
                                    </asp:Panel>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell" />
                                <ItemStyle CssClass="PaddedCell" />
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Totals, 8710NumberOf %>">
                                <ItemTemplate>
                                    <div><%# (Convert.ToInt32(Eval("NumberOfFlights")) > 0) ? String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText._8710NumberFlights, Eval("NumberOfFlights")) : string.Empty %></div>
                                    <div><%# (Convert.ToInt32(Eval("AeroTows")) > 0) ? String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.GliderAeroTows, Eval("AeroTows")) : string.Empty %></div>
                                    <div><%# (Convert.ToInt32(Eval("WinchedLaunches")) > 0) ? String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.GliderGroundLaunches, Eval("WinchedLaunches")) : string.Empty %></div>
                                    <div><%# (Convert.ToInt32(Eval("SelfLaunches")) > 0) ? String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.GliderSelfLaunches, Eval("SelfLaunches")) : string.Empty %></div>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell" />
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Center" />
                            </asp:TemplateField>
                        </Columns>
                        <EmptyDataTemplate>
                            <p><asp:Label ID="lblNoMatchingFlights" CssClass="error" runat="server" Text="<%$ Resources:LocalizedText, errNoMatchingFlightsFound %>" /></p>
                        </EmptyDataTemplate>
                    </asp:GridView>                    
                    <p><asp:Localize ID="loc8710Notes" runat="server" Text="<%$ Resources:Totals, 8710Notes %>"></asp:Localize></p>
                    <p><span class="FootNote">1</span><asp:Localize ID="loc8710Footnote1" 
                            runat="server" Text="<%$ Resources:Totals, 8710Footnote1 %>" /></p>
                    <p><span class="FootNote">2</span><asp:Localize ID="loc8710Footnote2" 
                            runat="server"  Text="<%$ Resources:Totals, 8710Footnote2 %>" /></p>
                    <p><span class="FootNote">3</span><asp:Localize ID="loc8710Footnote3" 
                            runat="server" Text="<%$ Resources:Totals, 8710Footnote3 %>" /></p>
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane ID="acpModel" runat="server">
                <Content>
                    <asp:GridView ID="gvRollup" runat="server" AutoGenerateColumns="False" GridLines="None" Font-Size="8pt" CellPadding="0" style="margin-left:auto; margin-right:auto; margin-top: 10px; margin-bottom: 10px;">
                        <Columns>
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <%#: ModelDisplay(Container.DataItem) %>
                                </ItemTemplate>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Center"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="<%$ Resources:LogbookEntry, FieldDual %>">
                                <ItemTemplate>
                                    <%# Eval("DualReceived").FormatDecimal(UseHHMM, false) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="Flight<br />Eng.">
                                <ItemTemplate>
                                    <%# Eval("flightengineer").FormatDecimal(UseHHMM, false) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="Military">
                                <ItemTemplate>
                                    <%# Eval("MilTime").FormatDecimal(UseHHMM, false) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="<%$ Resources:LogbookEntry, FieldCFI %>">
                                <ItemTemplate>
                                    <%# Eval("CFI").FormatDecimal(UseHHMM, false) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="<%$ Resources:LogbookEntry, FieldNight %>">
                                <ItemTemplate>
                                    <%# Eval("Night").FormatDecimal(UseHHMM, false) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="<%$ Resources:LogbookEntry, FieldIMC %>">
                                <ItemTemplate>
                                    <%# Eval("IMC").FormatDecimal(UseHHMM, false) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="<%$ Resources:LogbookEntry, FieldSimIMC %>">
                                <ItemTemplate>
                                    <%# Eval("SimIMC").FormatDecimal(UseHHMM, false) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="<%$ Resources:LogbookEntry, FieldXCountry %>">
                                <ItemTemplate>
                                    <%# Eval("XC").FormatDecimal(UseHHMM, false) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldLanding %>">
                                <ItemTemplate>
                                    <%# Eval("landings").FormatInt() %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Totals, RollupApproach %>">
                                <ItemTemplate>
                                    <%# FormatMultiInt(" / ", Eval("approaches"), Eval("_6MonthApproaches"), Eval("_12MonthApproaches")) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Totals, RollupPICSIC %>">
                                <ItemTemplate>
                                    <%# FormatMultiDecimal(UseHHMM, " / ", Eval("PIC"), Eval("SIC")) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources: Totals, RollupPICSICTurboprop %>">
                                <ItemTemplate>
                                    <%# FormatMultiDecimal(UseHHMM, " / ", Eval("TurboPropPIC"), Eval("TurboPropSIC")) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Totals, RollupJetPICSIC %>">
                                <ItemTemplate>
                                    <%# FormatMultiDecimal(UseHHMM, " / ", Eval("JetPIC"), Eval("JetSIC")) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Totals, RollupMultiPICSIC %>">
                                <ItemTemplate>
                                    <%# FormatMultiDecimal(UseHHMM, " / ", Eval("MultiPIC"), Eval("MultiSIC")) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Totals, RollupTotalByPeriod %>">
                                <ItemTemplate>
                                    <%# FormatMultiDecimal(UseHHMM, " / ", Eval("Total"), Eval("_12MonthTotal"), Eval("_24MonthTotal")) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:BoundField HeaderText="Last Flight" DataField="LastFlight" HeaderStyle-CssClass="PaddedCell" DataFormatString="{0:d}">
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Center" />
                            </asp:BoundField>
                        </Columns>
                    </asp:GridView>
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane ID="acpByTime" runat="server">
                <Content>
                    <uc1:mfbTotalsByTimePeriod runat="server" ID="mfbTotalsByTimePeriod" />
                </Content>
            </ajaxToolkit:AccordionPane>
        </Panes>
    </ajaxToolkit:Accordion>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
    <uc6:mfbLogbook ID="MfbLogbook1" runat="server" />
</asp:Content>
