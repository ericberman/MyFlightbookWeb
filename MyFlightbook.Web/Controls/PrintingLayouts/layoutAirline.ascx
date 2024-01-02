<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="layoutAirline.ascx.cs" Inherits="MyFlightbook.Printing.Layouts.layoutAirline" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageFooter.ascx" TagPrefix="uc1" TagName="pageFooter" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageHeader.ascx" TagPrefix="uc1" TagName="pageHeader" %>
<%@ Register Src="~/Controls/mfbSignature.ascx" TagPrefix="uc1" TagName="mfbSignature" %>

<asp:Repeater ID="rptPages" runat="server" OnItemDataBound="rptPages_ItemDataBound">
<ItemTemplate>
    <uc1:pageHeader runat="server" ID="pageHeader" UserName="<%# CurrentUser.UserName %>" />
    <table class="pageTable">
        <thead>
            <tr class="bordered">
                <th class="headerSmall" rowspan="3"><% =Resources.LogbookEntry.PrintHeaderDate %></th>
                <th class="headerSmall" rowspan="2" colspan="2"><% =Resources.LogbookEntry.PrintHeaderAircraft %></th>
                <th rowspan="3" class="headerBig"><%=Resources.LogbookEntry.PrintHeaderPICName %></th>
                <th class="headerSmall" rowspan="2" colspan="4"><% =Resources.LogbookEntry.PrintHeaderRoute %></th>
                <th class="headerSmall" rowspan="3"><% =Resources.LogbookEntry.PrintHeaderFlightNumber %></th>
                <th class="headerSmall" rowspan="3"><% =Resources.LogbookEntry.PrintHeaderTotalTime %></th>
                <th class="headerSmall" rowspan="3"><% =Resources.LogbookEntry.PrintHeaderMultiPilot %></th>
                <th rowspan="3" runat="server" Visible="<%# ShowOptionalColumn(0) %>"><div class="custColumn"><%# OptionalColumnName(0) %></div></th>
                <th rowspan="3" runat="server" Visible="<%# ShowOptionalColumn(1) %>"><div class="custColumn"><%# OptionalColumnName(1) %></div></th>
                <th rowspan="3" runat="server" Visible="<%# ShowOptionalColumn(2) %>"><div class="custColumn"><%# OptionalColumnName(2) %></div></th>
                <th rowspan="3" runat="server" Visible="<%# ShowOptionalColumn(3) %>"><div class="custColumn"><%# OptionalColumnName(3) %></div></th>
                <th colspan="2" class="headerBig"><%=Resources.LogbookEntry.PrintHeaderTakeoffs %></th>
                <th colspan="2" class="headerBig"><%=Resources.LogbookEntry.PrintHeaderLanding %></th>

                <th colspan="5" class="headerBig"><%=Resources.LogbookEntry.PrintHeaderCondition2 %></th>

                <th rowspan="3" class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderApproaches %></th>
                <th rowspan="3" class="headerSmall"><% =Resources.LogbookEntry.FieldHold %></th>
                <th rowspan="3" class="headerSmall"><% =Resources.LogbookEntry.FieldGroundSim %></th>

                <th colspan="7" class="headerBig"><%=Resources.LogbookEntry.PrintHeaderPilotFunction %></th>
                <th rowspan="3" class="headerBig"><%=Resources.LogbookEntry.PrintHeaderRemarks %></th>
            </tr>
            <tr class="borderedBold">
                <th rowspan="2" class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderDay %></th>
                <th rowspan="2" class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderNight %></th>

                <th rowspan="2" class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderDay %></th>
                <th rowspan="2" class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderNight %></th>

                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderCrossCountry %></th>
                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderNight %></th>
                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderSolo %></th>
                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderIMC %></th>
                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderSimInstrument %></th>

                <th rowspan="2" class="headerSmall"><%=Resources.LogbookEntry.FieldCFI %></th>
                <th rowspan="2" class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderFlightEngineer %></th>
                <th rowspan="2" class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderPIC2%></th>
                <th rowspan="2" class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderPICUS %></th>
                <th rowspan="2" class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderCoPilot %></th>
                <th rowspan="2" class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderReliefPilot %></th>
                <th rowspan="2" class="headerSmall"><%=Resources.LogbookEntry.FieldDual %></th>
            </tr>
            <tr class="borderedBold">
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderModel %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderRegistration %></th>

                <th class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderFrom %></th>
                <th class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderDeparture %></th>
                <th class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderArrival %></th>
                <th class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderTo %></th>
            </tr>                       
        </thead>
        <asp:Repeater EnableViewState="false" ID="rptFlight" runat="server" OnItemDataBound="rptFlight_ItemDataBound">
            <ItemTemplate>
                <tr class="bordered" <%# ColorForFlight(Container.DataItem) %>>
                    <td><%# ChangeMarkerForFlight(Container.DataItem) %><%# ((DateTime) Eval("Date")).ToShortDateString() %></td>
                    <td><%#: Eval("TailNumOrSimDisplay") %></td>
                    <td><%#: Eval("ModelDisplay") %> <%#: Eval("CatClassDisplay") %></td>
                    <td><%#: Eval("PICName") %></td>
                    <td><%#: Eval("Departure") %></td>
                    <td><%# ((DateTime) Eval("DepartureTime")).UTCFormattedStringOrEmpty(CurrentUser.UsesUTCDateOfFlight).Replace(" ", "<br />") %></td>
                    <td><%# ((DateTime) Eval("ArrivalTime")).UTCFormattedStringOrEmpty(CurrentUser.UsesUTCDateOfFlight).Replace(" ", "<br />") %></td>
                    <td><%#: Eval("Destination") %></td>
                    <td><%#: ((LogbookEntryDisplay) Container.DataItem).CustomProperties.StringValueForProperty(CustomPropertyType.KnownProperties.IDPropFlightNumber) %></td>
                    <td><%# Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%#: ((LogbookEntryDisplay) Container.DataItem).CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropMultiPilotTime).FormatDecimal(CurrentUser.UsesHHMM) %></td>

                    <td class="numericColumn" runat="server" id="tdoptColumn1" visible="<%# ShowOptionalColumn(0) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(0) %></div></td>
                    <td class="numericColumn" runat="server" id="tdoptColumn2" visible="<%# ShowOptionalColumn(1) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(1) %></div></td>
                    <td class="numericColumn" runat="server" id="tdoptColumn3" visible="<%# ShowOptionalColumn(2) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(2) %></div></td>
                    <td class="numericColumn" runat="server" id="tdoptColumn4" visible="<%# ShowOptionalColumn(3) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(3) %></div></td>

                    <td class="numericColumn"><%# Eval("DayTakeoffs").FormatInt() %></td>
                    <td class="numericColumn"><%# Eval("NightTakeoffs").FormatInt() %></td>
                    <td class="numericColumn"><%# Eval("NetDayLandings").FormatInt() %></td>
                    <td class="numericColumn"><%# Eval("NetNightLandings").FormatInt() %></td>

                        <td class="numericColumn"><%# Eval("CrossCountry").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                    <td class="numericColumn"><%# Eval("Nighttime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td class="numericColumn"><%# Eval("SoloTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td class="numericColumn"><%# Eval("IMC").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                    <td class="numericColumn"><%# Eval("SimulatedIFR").FormatDecimal(CurrentUser.UsesHHMM)%></td>

                    <td class="numericColumn"><%# Eval("Approaches").FormatInt() %></td>
                    <td class="numericColumn"><asp:Image ID="imgHolding" runat="server" ImageUrl="~/images/checkmark.png" style="height:10pt; width: 10pt;" GenerateEmptyAlternateText="true" Visible='<%# (bool) Eval("fHoldingProcedures") %>' /></td>

                    <td class="numericColumn"><%# Eval("GroundSim").FormatDecimal(CurrentUser.UsesHHMM) %></td>

                    <td class="numericColumn"><%# Eval("CFI").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                    <td class="numericColumn"><%# Eval("FlightEngineerTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td class="numericColumn"><%# Eval("PIC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td class="numericColumn"><%# Eval("PICUSTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td class="numericColumn"><%# Eval("CoPilotTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td class="numericColumn"><%# Eval("ReliefPilotTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td class="numericColumn"><%# Eval("Dual").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td>
                        <div><%#: ((string[]) Eval("Airports")).Count() <= 2 ? string.Empty : String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.PrintFullRoute, Eval("Route")) %></div>
                        <div style="clear:left; white-space:pre-line;" dir="auto"><%# Eval("RedactedCommentWithReplacedApproaches") %></div>
                        <div><%# ((LogbookEntryDisplay) Container.DataItem).CrossCountry > 0 ? String.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}: {1}", Resources.LogbookEntry.PrintHeaderCrossCountry, ((LogbookEntryDisplay) Container.DataItem).CrossCountry.FormatDecimal(CurrentUser.UsesHHMM)) : string.Empty %></div>
                        <div style="white-space:pre-line;"><%#: Eval("CustPropertyDisplay") %></div>
                        <div><uc1:mfbSignature runat="server" ID="mfbSignature" EnableViewState="false" /></div>
                    </td>
                </tr>
            </ItemTemplate>
        </asp:Repeater>
        <asp:Repeater EnableViewState="false" ID="rptSubtotalCollections" runat="server" OnItemDataBound="rptSubtotalCollections_ItemDataBound">
            <ItemTemplate>
                <tr class="subtotal">
                    <td class="subtotalLabel" colspan="4" rowspan='<%# Eval("SubtotalCount") %>'></td>
                    <td colspan="2" rowspan='<%# Eval("SubtotalCount") %>'><%# Eval("GroupTitle") %></td>
                    <asp:Repeater ID="rptSubtotals" runat="server">
                        <ItemTemplate>
                            <%# (Container.ItemIndex != 0) ? "<tr class=\"subtotal\">" : string.Empty %>
                            <td colspan="3"><%# ((LogbookEntryDisplay) Container.DataItem).CatClassDisplay %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).TotalFlightTime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).MultiPilotTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>

                            <td class="numericColumn" runat="server" id="tdoptColumnTotal1" visible="<%# ShowOptionalColumn(0) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(0, CurrentUser.UsesHHMM) %></div></td>
                            <td class="numericColumn" runat="server" id="tdoptColumnTotal2" visible="<%# ShowOptionalColumn(1) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(1, CurrentUser.UsesHHMM) %></div></td>
                            <td class="numericColumn" runat="server" id="tdoptColumnTotal3" visible="<%# ShowOptionalColumn(2) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(2, CurrentUser.UsesHHMM) %></div></td>
                            <td class="numericColumn" runat="server" id="tdoptColumnTotal4" visible="<%# ShowOptionalColumn(3) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(3, CurrentUser.UsesHHMM) %></div></td>

                            <td><%# ((LogbookEntryDisplay) Container.DataItem).DayTakeoffTotal.FormatInt() %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).NightTakeoffTotal.FormatInt() %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).NetDayLandings.FormatInt() %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).NetNightLandings.FormatInt() %></td>

                            <td><%# ((LogbookEntryDisplay) Container.DataItem).CrossCountry.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Nighttime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).SoloTime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).IMC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).SimulatedIFR.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Approaches.FormatInt() %></td>
                            <td>--</td>

                            <td><%# ((LogbookEntryDisplay) Container.DataItem).GroundSim.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).CFI.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).FlightEngineerTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).PIC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).PICUSTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).CoPilotTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).ReliefPilotTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Dual.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td class="subtotalLabel"></td>
                            <%# (Container.ItemIndex != 0) ? "</tr>" : string.Empty %>
                        </ItemTemplate>
                    </asp:Repeater>
                </tr>
            </ItemTemplate>
        </asp:Repeater>
    </table>
    <uc1:pageFooter runat="server" ID="pageFooter" ShowFooter="<%# ShowFooter %>" UserName="<%# CurrentUser.UserName %>" PageNum='<%#Eval("PageNum") %>' TotalPages='<%# Eval("TotalPages") %>'>
        <LayoutNotes>
            <div><%=Resources.LogbookEntry.PrintLabelEASA %></div>
            <div><% = (CurrentUser.UsesUTCDateOfFlight) ? Resources.LogbookEntry.PrintLabelUTCDates : Resources.LogbookEntry.PrintLabelLocalDates %></div>
        </LayoutNotes>
    </uc1:pageFooter>
</ItemTemplate>
</asp:Repeater>
