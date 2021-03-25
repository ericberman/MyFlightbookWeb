<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="LayoutPCAA.ascx.cs" Inherits="MyFlightbook.Printing.Layouts.LayoutPCAA" %>
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
                <th class="headerBig thickRight" style="width:10ch;" rowspan="3"><% =Resources.LogbookEntry.PrintHeaderDate %></th>
                <th class="headerBig thickRight" style="width:1.2cm;" colspan="2"><% =Resources.LogbookEntry.PrintHeaderAircraft %></th>
                <th class="headerBig thickRight" colspan="2"><% =Resources.LogbookEntry.PrintHeaderCrew %></th>
                <th class="headerBig thickRight" colspan="2" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderRoute %></th>
                <th class="headerBig thickRight" rowspan="3"><%=Resources.LogbookEntry.PrintHeaderRemarks %></th>
                <th class="headerBig thickRight" colspan="2"><% =Resources.LogbookEntry.PrintHeaderFlightTimes %></th>

                <th class="headerSmall" rowspan="3" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderLanding %></th>
                <th class="headerSmall thickRight" rowspan="3" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderApproaches %></th>

                <th class="headerBig thickRight" colspan="3" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderDay %></th>
                <th class="headerBig thickRight" colspan="3" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderNight %></th>
                <th class="headerBig thickRight" colspan="3"><% =Resources.LogbookEntry.PrintHeaderInstrumentTime %></th>

                <th class="headerBig thickRight" runat="server" id="headerXC" style="border-bottom:none;" colspan="2"><% =Resources.LogbookEntry.PrintHeaderCrossCountry %></th>

                <th class="headerBig" style="width:1cm;" rowspan="3" runat="server" id="headerCFI" visible="<%# CurrentUser.IsInstructor %>"><% =Resources.LogbookEntry.FieldCFI %></th>

                <th class="headerBig" rowspan="3" style="width:1cm" runat="server" id="optColumn1" Visible="<%# ShowOptionalColumn(0) %>"><div><%# OptionalColumnName(0) %></div></th>
                <th class="headerBig" rowspan="3" style="width:1cm" runat="server" id="optColumn2" Visible="<%# ShowOptionalColumn(1) %>"><div><%# OptionalColumnName(1) %></div></th>
                <th class="headerBig" rowspan="3" style="width:1cm" runat="server" id="optColumn3" Visible="<%# ShowOptionalColumn(2) %>"><div><%# OptionalColumnName(2) %></div></th>
                <th class="headerBig" rowspan="3" style="width:1cm" runat="server" id="optColumn4" Visible="<%# ShowOptionalColumn(3) %>"><div><%# OptionalColumnName(3) %></div></th>

                <th class="headerBig" rowspan="3"><% =Resources.LogbookEntry.PrintHeaderSignature %></th>
            </tr>
            <tr class="bordered">
                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderTypeShort %></th>
                <th class="headerSmall thickRight" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderRegistration %></th>

                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderP1 %></th>
                <th class="headerSmall thickRight" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderOtherNames %></th>

                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderDeparture %></th>
                <th class="headerSmall thickRight" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderArrival %></th>

                <th class="headerSmall" style="width:1cm" rowspan="2"><% =Resources.LogbookEntry.FieldSimIMC %></th>
                <th class="headerSmall" style="width:1cm" rowspan="2"><% =Resources.LogbookEntry.FieldIMC %></th>
                <th class="headerSmall thickRight" style="width:1cm" rowspan="2"><% =Resources.LogbookEntry.FieldGroundSim %></th>

                <th class="headerSmall thickRight" colspan="2" style="border-top:none;"><% =Resources.LogbookEntry.PrintHeaderP1Only %></th>
            </tr>
            <tr class="bordered">
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderFrom %></th>
                <th class="headerSmall thickRight" style="width:1cm;"><% =Resources.LogbookEntry.PrintHeaderTo %></th>

                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderP1 %></th>
                <th class="headerSmall" style="width:1cm;"><% =Resources.LogbookEntry.PrintHeaderP2%></th>
                <th class="headerSmall thickRight" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderP3 %></th>

                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderP1 %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderP2 %></th>
                <th class="headerSmall thickRight" style="width:1cm;"><% =Resources.LogbookEntry.PrintHeaderP3 %></th>

                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderDay %></th>
                <th class="headerSmall thickRight" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderNight %></th>
            </tr>
            </thead>
            <asp:Repeater ID="rptFlight" runat="server" OnItemDataBound="rptFlight_ItemDataBound">
                <ItemTemplate>
                    <tr class="bordered <%# Container.ItemIndex % 2 == 0 ? "evenRow" : "oddRow" %> <%# Container.ItemIndex == 0 ? "topThick" : string.Empty %>" <%# ColorForFlight(Container.DataItem) %>>
                        <td class="centered thickRight" style="font-weight:bold;"><%# ((DateTime) Eval("Date")).ToShortDateString() %></td>

                        <td class="centered"><div><%#: Eval("ModelDisplay") %> (<%#: Eval("CatClassDisplay") %>)</div></td>
                        <td class="centered thickRight"><%#: Eval("TailNumOrSimDisplay") %></td>

                        <td class="centered"><%#: Eval("PICName") %></td>
                        <td class="centered thickRight"><%#: Eval("SICName") %> <%#: Eval("StudentName") %></td>

                        <td class="centered"><%#: Eval("Departure") %></td>
                        <td class="centered thickRight"><%#: Eval("Destination") %></td>

                        <td class="thickRight">
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
                            <asp:Panel EnableViewState="false" ID="pnlFlightImages" runat="server" Visible="<%# IncludeImages %>">
                                <uc1:mfbImageList ID="mfbilFlights" runat="server" Columns="3" CanEdit="false" ImageClass="Flight" IncludeDocs="false" MaxImage="3" />
                            </asp:Panel>
                        </td>
                        <td class="centered"><%# ((DateTime) Eval("DepartureTime")).UTCFormattedStringOrEmpty(CurrentUser.UsesUTCDateOfFlight).Replace(" ", "<br />") %></td>
                        <td class="centered thickRight"><%# ((DateTime) Eval("ArrivalTime")).UTCFormattedStringOrEmpty(CurrentUser.UsesUTCDateOfFlight).Replace(" ", "<br />") %></td>

                        <td class="centered"><%# Eval("LandingDisplay") %></td>
                        <td class="centered thickRight"><%# Eval("Approaches").FormatInt() %></td>

                        <td class="centered"><%# Math.Max(((decimal)Eval("PIC")) - ((decimal)Eval("Nighttime")), 0.0M).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td class="centered"><%# Math.Max(((decimal)Eval("SIC")) - ((decimal)Eval("Nighttime")), 0.0M).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td class="centered thickRight"><%# Math.Max(((decimal)Eval("Dual")) - ((decimal)Eval("Nighttime")), 0.0M).FormatDecimal(CurrentUser.UsesHHMM) %></td>

                        <td class="centered"><%# Math.Min(((decimal)Eval("PIC")), ((decimal)Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td class="centered"><%# Math.Min(((decimal)Eval("SIC")), ((decimal)Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td class="centered thickRight"><%# Math.Min(((decimal)Eval("Dual")), ((decimal)Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>

                        <td class="centered"><%# Eval("SimulatedIFR").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="centered"><%# Eval("IMC").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="centered thickRight"><%# Eval("GroundSim").FormatDecimal(CurrentUser.UsesHHMM)%></td>

                        <td class="centered"><%# Math.Min((decimal)Eval("PIC"), (decimal) Eval("DayXC")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td class="centered thickRight"><%# Math.Min(((decimal)Eval("PIC")), (decimal)Eval("NightXC")).FormatDecimal(CurrentUser.UsesHHMM) %></td>

                        <td class="centered thickRight" runat="server" visible='<%# CurrentUser.IsInstructor %>'><%# ((decimal)Eval("CFI")).FormatDecimal(CurrentUser.UsesHHMM) %></td>

                        <td class="centered" runat="server" id="td1" visible="<%# ShowOptionalColumn(0) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(0) %></div></td>
                        <td class="centered" runat="server" id="td2" visible="<%# ShowOptionalColumn(1) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(1) %></div></td>
                        <td class="centered" runat="server" id="td3" visible="<%# ShowOptionalColumn(2) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(2) %></div></td>
                        <td class="centered" runat="server" id="td4" visible="<%# ShowOptionalColumn(3) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(3) %></div></td>

                        <td><uc1:mfbSignature runat="server" ID="mfbSignature" EnableViewState="false" /></td>
                    </tr>
                </ItemTemplate>
            </asp:Repeater>
            <asp:Repeater ID="rptSubtotalCollections" runat="server" EnableViewState="false" OnItemDataBound="rptSubtotalCollections_ItemDataBound">
                <ItemTemplate>
                    <tr class="subtotal" <%# Container.ItemIndex == 0 ? "style=\"border-top: 3px solid;\"" : string.Empty %>>
                        <td class="subtotalLabel" colspan="4" rowspan='<%# Eval("SubtotalCount") %>'></td>
                        <td rowspan='<%# Eval("SubtotalCount") %>' colspan="5"><%# Eval("GroupTitle") %></td>
                        <asp:Repeater ID="rptSubtotals" runat="server">
                            <ItemTemplate>
                                <%# (Container.ItemIndex != 0) ? "<tr class=\"subtotal" + (Container.ItemIndex % 2 == 0 ? " evenRow" : " oddRow") + "\">" : string.Empty %>
                                <td class="thickRight"><%# ((LogbookEntryDisplay) Container.DataItem).CatClassDisplay %></td>

                                <td><%# ((LogbookEntryDisplay) Container.DataItem).Landings.FormatInt() %></td>
                                <td class="thickRight"><%# ((LogbookEntryDisplay) Container.DataItem).Approaches.FormatInt() %></td>

                                <td><%# Math.Max(((LogbookEntryDisplay) Container.DataItem).PIC - ((LogbookEntryDisplay) Container.DataItem).NightPICTotal, 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td class="thickRight"><%# Math.Max(((LogbookEntryDisplay) Container.DataItem).SIC - ((LogbookEntryDisplay) Container.DataItem).NightSICTotal, 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# Math.Max(((LogbookEntryDisplay) Container.DataItem).Dual - ((LogbookEntryDisplay) Container.DataItem).NightDualTotal, 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>

                                <td><%# ((LogbookEntryDisplay) Container.DataItem).NightPICTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td class="thickRight"><%# ((LogbookEntryDisplay) Container.DataItem).NightSICTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).NightDualTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>

                                <td><%# ((LogbookEntryDisplay) Container.DataItem).SimulatedIFR.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).IMC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td class="thickRight"><%# ((LogbookEntryDisplay) Container.DataItem).GroundSim.FormatDecimal(CurrentUser.UsesHHMM) %></td>

                                <td><%# Math.Max((((LogbookEntryDisplay) Container.DataItem).XCPICTotal - ((LogbookEntryDisplay) Container.DataItem).XCNightPICTotal), 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td class="thickRight"><%# ((LogbookEntryDisplay) Container.DataItem).XCNightPICTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>

                                <td class="thickRight" runat="server" id="CFISubtotal" visible='<%# CurrentUser.IsInstructor %>'><%# ((LogbookEntryDisplay) Container.DataItem).CFI.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td class="thickRight" runat="server" id="SICSubtotal" visible='<%# CurrentUser.TracksSecondInCommandTime %>'><%# ((LogbookEntryDisplay) Container.DataItem).SIC.FormatDecimal(CurrentUser.UsesHHMM) %></td>

                                <td runat="server" id="tdoptColumnTotal1" visible="<%# ShowOptionalColumn(0) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(0, CurrentUser.UsesHHMM) %></div></td>
                                <td runat="server" id="tdoptColumnTotal2" visible="<%# ShowOptionalColumn(1) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(1, CurrentUser.UsesHHMM) %></div></td>
                                <td runat="server" id="tdoptColumnTotal3" visible="<%# ShowOptionalColumn(2) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(2, CurrentUser.UsesHHMM) %></div></td>
                                <td runat="server" id="tdoptColumnTotal4" visible="<%# ShowOptionalColumn(3) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(3, CurrentUser.UsesHHMM) %></div></td>

                                <td></td>
                                <%# (Container.ItemIndex != 0) ? "</tr>" : string.Empty %>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tr>
                </ItemTemplate>
            </asp:Repeater>
        </table>
        <uc1:pageFooter runat="server" ID="pageFooter" ShowFooter="<%# ShowFooter %>" UserName="<%# CurrentUser.UserName %>" PageNum='<%#Eval("PageNum") %>' TotalPages='<%# Eval("TotalPages") %>'>
            <LayoutNotes>
                <div><% = (CurrentUser.UsesUTCDateOfFlight) ? Resources.LogbookEntry.PrintLabelUTCDates : Resources.LogbookEntry.PrintLabelLocalDates %></div>
            </LayoutNotes>
        </uc1:pageFooter>
    </ItemTemplate>
</asp:Repeater>
