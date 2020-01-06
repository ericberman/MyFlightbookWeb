<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_PrintingLayouts_layoutCanada" Codebehind="layoutCanada.ascx.cs" %>
<%@ Register Src="~/Controls/mfbImageList.ascx" TagPrefix="uc1" TagName="mfbImageList" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageHeader.ascx" TagPrefix="uc1" TagName="pageHeader" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageFooter.ascx" TagPrefix="uc1" TagName="pageFooter" %>
<%@ Register Src="~/Controls/mfbSignature.ascx" TagPrefix="uc1" TagName="mfbSignature" %>

<asp:Repeater ID="rptPages" runat="server" OnItemDataBound="rptPages_ItemDataBound" EnableViewState="false">
    <ItemTemplate>
        <uc1:pageHeader runat="server" ID="pageHeader" UserName="<%# CurrentUser.UserName %>" />
        <table class="pageTable">
            <thead class="header bordered">
            <tr class="bordered">
                <th class="headerBig" style="width:10ch; border-right: 2px solid black;" rowspan="3"><% =Resources.LogbookEntry.PrintHeaderDate %></th>
                <th class="headerBig" style="width:1.2cm; border-right: 2px solid black;" colspan="2"><% =Resources.LogbookEntry.PrintHeaderAircraft %></th>
                <th class="headerBig" style="width:1cm" rowspan="3"><% =Resources.LogbookEntry.PrintHeaderPICName %></th>
                <th class="headerBig" style="width:1cm; border-right: 2px solid black;" rowspan="3"><% =Resources.LogbookEntry.PrintHeaderCoPilotOrStudent %></th>
                <th class="headerBig" colspan="2" rowspan="2" style="border-right: 2px solid black;"><% =Resources.LogbookEntry.PrintHeaderRoute %></th>
                <th class="headerBig" rowspan="3" style="border-right: 2px solid black;"><%=Resources.LogbookEntry.PrintHeaderRemarks %></th>
                <th class="headerBig" colspan="3" rowspan="2" style="border-right: 2px solid black;"><% =Resources.LogbookEntry.PrintHeaderDay %></th>
                <th class="headerBig" colspan="3" rowspan="2" style="border-right: 2px solid black;"><% =Resources.LogbookEntry.PrintHeaderNight %></th>
                <th class="headerBig" colspan="4" style="border-right: 2px solid black;"><% =Resources.LogbookEntry.PrintHeaderInstrumentTime %></th>
                <th class="headerBig" runat="server" id="headerXC" colspan="<%# CurrentUser.TracksSecondInCommandTime ? 6 : 4 %>" style="border-right: 2px solid black;"><% =Resources.LogbookEntry.PrintHeaderCrossCountry %></th>
                <th class="headerBig" style="width:1cm; border-right: 2px solid black;" rowspan="3" runat="server" id="headerCFI" visible="<%# CurrentUser.IsInstructor %>"><% =Resources.LogbookEntry.FieldCFI %></th>
                <th class="headerBig" style="width:1cm; border-right: 2px solid black;" rowspan="3" runat="server" id="headerSIC" visible="<%# CurrentUser.TracksSecondInCommandTime %>"><% =Resources.LogbookEntry.FieldSIC %></th>
                <th class="headerBig" colspan="2" rowspan="2" style="border-right: 2px solid black;"><% =Resources.LogbookEntry.PrintHeaderTakeoffsLandings %></th>
                <th class="headerBig" rowspan="3" style="width:1cm" runat="server" id="optColumn1" Visible="<%# ShowOptionalColumn(0) %>"><div><%# OptionalColumnName(0) %></div></th>
                <th class="headerBig" rowspan="3" style="width:1cm" runat="server" id="optColumn2" Visible="<%# ShowOptionalColumn(1) %>"><div><%# OptionalColumnName(1) %></div></th>
                <th class="headerBig" rowspan="3" style="width:1cm" runat="server" id="optColumn3" Visible="<%# ShowOptionalColumn(2) %>"><div><%# OptionalColumnName(2) %></div></th>
                <th class="headerBig" rowspan="3" style="width:1cm" runat="server" id="optColumn4" Visible="<%# ShowOptionalColumn(3) %>"><div><%# OptionalColumnName(3) %></div></th>
                <th class="headerBig" rowspan="3" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderTotalTime %></th>
            </tr>
            <tr class="bordered">
                <th class="headerSmall oddRow" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderModel %></th>
                <th class="headerSmall oddRow" rowspan="2" style="border-right: 2px solid black;"><% =Resources.LogbookEntry.PrintHeaderRegistration %></th>

                <th class="headerSmall oddRow" style="width:1cm" rowspan="2"><% =Resources.LogbookEntry.FieldIMC %></th>
                <th class="headerSmall oddRow" style="width:1cm" rowspan="2"><% =Resources.LogbookEntry.FieldSimIMC %></th>
                <th class="headerSmall oddRow" style="width:1cm" rowspan="2"><% =Resources.LogbookEntry.FieldGroundSim %></th>
                <th class="headerSmall oddRow" style="width:1cm; border-right: 2px solid black;" rowspan="2"><%=Resources.LogbookEntry.PrintHeaderApproaches %></th>
                <th class="headerSmall oddRow" style="border-right: 2px solid black;" colspan="<%# CurrentUser.TracksSecondInCommandTime ? 3 :2 %>"><% =Resources.LogbookEntry.PrintHeaderDay %></th>
                <th class="headerSmall oddRow" style="border-right: 2px solid black;" colspan="<%# CurrentUser.TracksSecondInCommandTime ? 3 : 2 %>"><% =Resources.LogbookEntry.PrintHeaderNight %></th>
            </tr>
            <tr class="bordered">
                <th class="headerSmall oddRow" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderFrom %></th>
                <th class="headerSmall oddRow" style="width:1cm; border-right: 2px solid black;"><% =Resources.LogbookEntry.PrintHeaderTo %></th>

                <th class="headerSmall oddRow" style="width:1cm"><% =Resources.LogbookEntry.FieldDual %></th>
                <th class="headerSmall oddRow" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderPIC2 %></th>
                <th class="headerSmall oddRow" style="width:1cm; border-right: 2px solid black;"><% =Resources.LogbookEntry.FieldSIC %></th>

                <th class="headerSmall oddRow" style="width:1cm"><% =Resources.LogbookEntry.FieldDual %></th>
                <th class="headerSmall oddRow" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderPIC2 %></th>
                <th class="headerSmall oddRow" style="width:1cm; border-right: 2px solid black;"><% =Resources.LogbookEntry.FieldSIC %></th>

                <th class="headerSmall oddRow" style="width:1cm"><% =Resources.LogbookEntry.FieldDual %></th>
                <th class="headerSmall oddRow" style="width:1cm;" runat="server" visible="<%# CurrentUser.TracksSecondInCommandTime %>"><% =Resources.LogbookEntry.FieldSIC %></th>
                <th class="headerSmall oddRow" style="width:1cm; border-right: 2px solid black"><% =Resources.LogbookEntry.PrintHeaderPIC2 %></th>
                <th class="headerSmall oddRow" style="width:1cm"><% =Resources.LogbookEntry.FieldDual %></th>
                <th class="headerSmall oddRow" style="width:1cm;" runat="server" visible="<%# CurrentUser.TracksSecondInCommandTime %>"><% =Resources.LogbookEntry.FieldSIC %></th>
                <th class="headerSmall oddRow" style="width:1cm; border-right: 2px solid black"><% =Resources.LogbookEntry.PrintHeaderPIC2 %></th>
                <th class="headerSmall oddRow"><% =Resources.LogbookEntry.PrintHeaderDay %></th>
                <th class="headerSmall oddRow" style="border-right: 2px solid black;"><% =Resources.LogbookEntry.PrintHeaderNight %></th>
            </tr>
            </thead>
            <asp:Repeater ID="rptFlight" runat="server" OnItemDataBound="rptFlight_ItemDataBound">
                <ItemTemplate>
                    <tr class="bordered <%# Container.ItemIndex % 2 == 0 ? "evenRow" : "oddRow" %> <%# Container.ItemIndex == 0 ? "topThick" : string.Empty %>">
                        <td class="centered" style="font-weight:bold; border-right: 2px solid black;"><%# ((DateTime) Eval("Date")).ToShortDateString() %></td>
                        <td class="centered"><div><%#: Eval("ModelDisplay") %> (<%#: Eval("CatClassDisplay") %>)</div></td>
                        <td class="centered" style="border-right: 2px solid black;"><%#: Eval("TailNumDisplay") %></td>
                        <td class="centered"><%#: Eval("PICName") %></td>
                        <td class="centered" style="border-right: 2px solid black;"><%#: Eval("SICName") %> <%#: Eval("StudentName") %></td>
                        <td class="centered"><%#: Eval("Departure") %></td>
                        <td class="centered" style="border-right: 2px solid black;"><%#: Eval("Destination") %></td>
                        <td style="border-right: 2px solid black;">
                            <div><%#: ((string[]) Eval("Airports")).Count() <= 2 ? string.Empty : String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.PrintFullRoute, Eval("Route")) %></div>
                            <span runat="server" id="divComments" dir="auto"><%# Eval("RedactedCommentWithReplacedApproaches") %></span></div>
                            <asp:Panel ID="pnlFlightTimes" runat="server" Visible="<%# CurrentUser.DisplayTimesByDefault %>">
                                <asp:Panel EnableViewState="false" ID="pnlEngineTime" runat="server">
                                    <%# Eval("EngineTimeDisplay") %>
                                </asp:Panel>
                                <asp:Panel EnableViewState="false" ID="pnlFlightTime" runat="server">
                                    <%# Eval("FlightTimeDisplay") %>
                                </asp:Panel>
                                <asp:Panel EnableViewState="false" ID="pnlHobbs" runat="server">
                                    <%# Eval("HobbsDisplay") %>
                                </asp:Panel>
                            </asp:Panel>
                            <div style="white-space:pre-line;"><%#: Eval("CustPropertyDisplay").ToString() %></div>
                            <uc1:mfbSignature runat="server" ID="mfbSignature" EnableViewState="false" />
                            <asp:Panel EnableViewState="false" ID="pnlFlightImages" runat="server" Visible="<%# IncludeImages %>">
                                <uc1:mfbImageList ID="mfbilFlights" runat="server" Columns="3" CanEdit="false" ImageClass="Flight" IncludeDocs="false" MaxImage="3" />
                            </asp:Panel>
                        </td>
                        <td><%# Math.Max(((decimal)Eval("Dual")) - ((decimal)Eval("Nighttime")), 0.0M).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Math.Max(((decimal)Eval("PIC")) - ((decimal)Eval("Nighttime")), 0.0M).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td style="border-right: 2px solid black;"><%# Math.Max(((decimal)Eval("SIC")) - ((decimal)Eval("Nighttime")), 0.0M).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Math.Min(((decimal)Eval("Dual")), ((decimal)Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Math.Min(((decimal)Eval("PIC")), ((decimal)Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td style="border-right: 2px solid black;"><%# Math.Min(((decimal)Eval("SIC")), ((decimal)Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>

                        <td><%# Eval("IMC").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td><%# Eval("SimulatedIFR").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td><%# Eval("GroundSim").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td style="border-right: 2px solid black;"><%# ((int) Eval("Approaches")).FormatInt() %></td>

                        <td><%# Math.Min((decimal)Eval("Dual"), (decimal) Eval("DayXC")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td runat="server" visible="<%# CurrentUser.TracksSecondInCommandTime %>"><%# Math.Min((decimal)Eval("SIC"), (decimal) Eval("DayXC")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td style="border-right: 2px solid black;"><%# Math.Min((decimal)Eval("PIC"), (decimal) Eval("DayXC")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Math.Min(((decimal)Eval("Dual")), (decimal)Eval("NightXC")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td runat="server" visible="<%# CurrentUser.TracksSecondInCommandTime %>"><%# Math.Min(((decimal)Eval("SIC")), (decimal)Eval("NightXC")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td style="border-right: 2px solid black;"><%# Math.Min(((decimal)Eval("PIC")), (decimal)Eval("NightXC")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td runat="server" visible='<%# CurrentUser.IsInstructor %>' style="border-right: 2px solid black;"><%# ((decimal)Eval("CFI")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td runat="server" visible='<%# CurrentUser.TracksSecondInCommandTime %>' style="border-right: 2px solid black;"><%# ((decimal)Eval("SIC")).FormatDecimal(CurrentUser.UsesHHMM) %></td>

                        <td class="centered"><%# Eval("DayTakeoffs") %>&nbsp;/&nbsp;<%# Math.Max((int) Eval("Landings") - (int) Eval("NightLandings") - (int) Eval("NightTouchAndGoLandings"), 0) %></td>
                        <td class="centered" style="border-right: 2px solid black;"><%# Eval("NightTakeoffs") %>&nbsp;/&nbsp;<%# (int) Eval("NightLandings") + (int) Eval("NightTouchAndGoLandings") %></td>

                        <td runat="server" id="td1" visible="<%# ShowOptionalColumn(0) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(0) %></div></td>
                        <td runat="server" id="td2" visible="<%# ShowOptionalColumn(1) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(1) %></div></td>
                        <td runat="server" id="td3" visible="<%# ShowOptionalColumn(2) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(2) %></div></td>
                        <td runat="server" id="td4" visible="<%# ShowOptionalColumn(3) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(3) %></div></td>

                        <td><%# Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    </tr>
                </ItemTemplate>
            </asp:Repeater>
            <asp:Repeater ID="rptSubtotalCollections" runat="server" EnableViewState="false" OnItemDataBound="rptSubtotalCollections_ItemDataBound">
                <ItemTemplate>
                    <tr class="subtotal" <%# Container.ItemIndex == 0 ? "style=\"border-top: 3px solid black;\"" : string.Empty %>>
                        <td class="subtotalLabel" colspan="4" rowspan='<%# Eval("SubtotalCount") %>'></td>
                        <td rowspan='<%# Eval("SubtotalCount") %>' colspan="3"><%# Eval("GroupTitle") %></td>
                        <asp:Repeater ID="rptSubtotals" runat="server">
                            <ItemTemplate>
                                <%# (Container.ItemIndex != 0) ? "<tr class=\"subtotal" + (Container.ItemIndex % 2 == 0 ? " evenRow" : " oddRow") + "\">" : string.Empty %>
                                <td style="border-right: 2px solid black;"><%# ((LogbookEntryDisplay) Container.DataItem).CatClassDisplay %></td>

                                <td><%# Math.Max(((LogbookEntryDisplay) Container.DataItem).Dual - ((LogbookEntryDisplay) Container.DataItem).NightDualTotal, 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# Math.Max(((LogbookEntryDisplay) Container.DataItem).PIC - ((LogbookEntryDisplay) Container.DataItem).NightPICTotal, 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td style="border-right: 2px solid black;"><%# Math.Max(((LogbookEntryDisplay) Container.DataItem).SIC - ((LogbookEntryDisplay) Container.DataItem).NightSICTotal, 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).NightDualTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).NightPICTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td style="border-right: 2px solid black;"><%# ((LogbookEntryDisplay) Container.DataItem).NightSICTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>

                                <td><%# ((LogbookEntryDisplay) Container.DataItem).IMC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).SimulatedIFR.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).GroundSim.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td style="border-right: 2px solid black;"><%# ((LogbookEntryDisplay) Container.DataItem).Approaches.FormatInt() %></td>

                                <td><%# Math.Max((((LogbookEntryDisplay) Container.DataItem).XCDualTotal - ((LogbookEntryDisplay) Container.DataItem).XCNightDualTotal), 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td runat="server" visible="<%# CurrentUser.TracksSecondInCommandTime %>"><%# Math.Max((((LogbookEntryDisplay) Container.DataItem).XCSICTotal - ((LogbookEntryDisplay) Container.DataItem).XCNightSICTotal), 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td style="border-right: 2px solid black;"><%# Math.Max((((LogbookEntryDisplay) Container.DataItem).XCPICTotal - ((LogbookEntryDisplay) Container.DataItem).XCNightPICTotal), 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).XCNightDualTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td runat="server" visible="<%# CurrentUser.TracksSecondInCommandTime %>"><%# ((LogbookEntryDisplay) Container.DataItem).XCNightSICTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td style="border-right: 2px solid black;"><%# ((LogbookEntryDisplay) Container.DataItem).XCNightPICTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>

                                <td runat="server" style="border-right: 2px solid black;" id="CFISubtotal" visible='<%# CurrentUser.IsInstructor %>'><%# ((LogbookEntryDisplay) Container.DataItem).CFI.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td runat="server" style="border-right: 2px solid black;" id="SICSubtotal" visible='<%# CurrentUser.TracksSecondInCommandTime %>'><%# ((LogbookEntryDisplay) Container.DataItem).SIC.FormatDecimal(CurrentUser.UsesHHMM) %></td>

                                <td class="centered"><%# (int) Eval("DayTakeoffTotal") %>&nbsp;/&nbsp;<%# (int) Eval("LandingsTotal") - (int) Eval("NightLandings") - (int) Eval("NightTouchAndGoLandings") %></td>
                                <td class="centered" style="border-right: 2px solid black;"><%# (int) Eval("NightTakeoffTotal") %>&nbsp;/&nbsp;<%# (int) Eval("NightLandings") + (int) Eval("NightTouchAndGoLandings") %></td>

                                <td runat="server" id="tdoptColumnTotal1" visible="<%# ShowOptionalColumn(0) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(0, CurrentUser.UsesHHMM) %></div></td>
                                <td runat="server" id="tdoptColumnTotal2" visible="<%# ShowOptionalColumn(1) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(1, CurrentUser.UsesHHMM) %></div></td>
                                <td runat="server" id="tdoptColumnTotal3" visible="<%# ShowOptionalColumn(2) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(2, CurrentUser.UsesHHMM) %></div></td>
                                <td runat="server" id="tdoptColumnTotal4" visible="<%# ShowOptionalColumn(3) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(3, CurrentUser.UsesHHMM) %></div></td>

                                <td><%# ((LogbookEntryDisplay) Container.DataItem).TotalFlightTime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <%# (Container.ItemIndex != 0) ? "</tr>" : string.Empty %>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tr>
                </ItemTemplate>
            </asp:Repeater>
        </table>
        <uc1:pageFooter runat="server" ID="pageFooter" ShowFooter="<%# ShowFooter %>" UserName="<%# CurrentUser.UserName %>" PageNum='<%#Eval("PageNum") %>' TotalPages='<%# Eval("TotalPages") %>' />
    </ItemTemplate>
</asp:Repeater>
