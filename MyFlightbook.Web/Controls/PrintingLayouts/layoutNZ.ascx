<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_PrintingLayouts_layoutNZ" Codebehind="layoutNZ.ascx.cs" %>
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
                <th class="headerBig" style="width:10ch" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderDate %></th>
                <th class="headerBig" style="width:1.2cm" colspan="2"><% =Resources.LogbookEntry.PrintHeaderAircraft %></th>
                <th class="headerBig" style="width:1cm" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderPICName %></th>
                <th class="headerBig" style="width:1cm" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderCoPilotOrStudent %></th>
                <th class="headerBig" rowspan="2"><%=Resources.LogbookEntry.PrintHeaderRemarks %></th>
                <th class="headerBig" colspan="3"><% =Resources.LogbookEntry.PrintHeaderDay %></th>
                <th class="headerBig" colspan="3"><% =Resources.LogbookEntry.PrintHeaderNight %></th>
                <th class="headerBig" colspan="3"><% =Resources.LogbookEntry.PrintHeaderInstrumentTime %></th>
                <th class="headerBig" style="width:1cm" rowspan="2" runat="server" id="headerCFI" visible="<%# CurrentUser.IsInstructor %>"><% =Resources.LogbookEntry.FieldCFI %></th>
                <th class="headerBig" rowspan="2" style="width:1cm" runat="server" id="optColumn1" Visible="<%# ShowOptionalColumn(0) %>"><div><%# OptionalColumnName(0) %></div></th>
                <th class="headerBig" rowspan="2" style="width:1cm" runat="server" id="optColumn2" Visible="<%# ShowOptionalColumn(1) %>"><div><%# OptionalColumnName(1) %></div></th>
                <th class="headerBig" rowspan="2" style="width:1cm" runat="server" id="optColumn3" Visible="<%# ShowOptionalColumn(2) %>"><div><%# OptionalColumnName(2) %></div></th>
                <th class="headerBig" rowspan="2" style="width:1cm" runat="server" id="optColumn4" Visible="<%# ShowOptionalColumn(3) %>"><div><%# OptionalColumnName(3) %></div></th>
                <th class="headerBig" rowspan="2" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderTotalTime %></th>
            </tr>
            <tr class="bordered">
                <th class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderModel %></th>
                <th class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderRegistration %></th>

                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.FieldDual %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.FieldSIC %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderPIC2 %></th>

                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.FieldDual %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.FieldSIC %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderPIC2 %></th>

                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.FieldIMC %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.FieldSimIMC %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.FieldGroundSim %></th>
            </tr>
            </thead>
            <asp:Repeater ID="rptFlight" runat="server" OnItemDataBound="rptFlight_ItemDataBound">
                <ItemTemplate>
                    <tr class="bordered">
                        <td style="font-weight:bold"><%# ((DateTime) Eval("Date")).ToShortDateString() %></td>
                        <td><div><%#: Eval("ModelDisplay") %> (<%#: Eval("CatClassDisplay") %>)</div></td>
                        <td><%#: Eval("TailNumDisplay") %></td>
                        <td><%#: Eval("PICName") %></td>
                        <td><%#: Eval("SICName") %> <%#: Eval("StudentName") %></td>
                        <td>
                            <div><span style="font-weight:bold"><%#: Eval("Route") %></span>
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
                        <td><%# Math.Max(((decimal)Eval("SIC")) - ((decimal)Eval("Nighttime")), 0.0M).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Math.Max(((decimal)Eval("PIC")) - ((decimal)Eval("Nighttime")), 0.0M).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Math.Min(((decimal)Eval("Dual")), ((decimal)Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Math.Min(((decimal)Eval("SIC")), ((decimal)Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Math.Min(((decimal)Eval("PIC")), ((decimal)Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>

                        <td><%# Eval("IMC").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td><%# Eval("SimulatedIFR").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td><%# Eval("GroundSim").FormatDecimal(CurrentUser.UsesHHMM)%></td>

                        <td runat="server" id="CFIrow" visible='<%# CurrentUser.IsInstructor %>' ><%# ((decimal)Eval("CFI")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
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
                    <tr class="subtotal">
                        <td class="subtotalLabel" colspan="4" rowspan='<%# Eval("SubtotalCount") %>'></td>
                        <td rowspan='<%# Eval("SubtotalCount") %>'><%# Eval("GroupTitle") %></td>
                        <asp:Repeater ID="rptSubtotals" runat="server">
                            <ItemTemplate>
                                <%# (Container.ItemIndex != 0) ? "<tr class=\"subtotal\">" : string.Empty %>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).CatClassDisplay %></td>

                                <td><%# Math.Max(((LogbookEntryDisplay) Container.DataItem).Dual - ((LogbookEntryDisplay) Container.DataItem).NightDualTotal, 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# Math.Max(((LogbookEntryDisplay) Container.DataItem).SIC - ((LogbookEntryDisplay) Container.DataItem).NightSICTotal, 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# Math.Max(((LogbookEntryDisplay) Container.DataItem).PIC - ((LogbookEntryDisplay) Container.DataItem).NightPICTotal, 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).NightDualTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).NightSICTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).NightPICTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>

                                <td><%# ((LogbookEntryDisplay) Container.DataItem).IMC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).SimulatedIFR.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).GroundSim.FormatDecimal(CurrentUser.UsesHHMM) %></td>

                                <td runat="server" id="nightCFISubtotal" visible='<%# CurrentUser.IsInstructor %>'><%# ((LogbookEntryDisplay) Container.DataItem).CFI.FormatDecimal(CurrentUser.UsesHHMM) %></td>
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
