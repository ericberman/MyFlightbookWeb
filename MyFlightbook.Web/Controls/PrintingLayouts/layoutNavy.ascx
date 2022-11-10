<%@ Control Language="C#" AutoEventWireup="true" Codebehind="layoutNavy.ascx.cs" Inherits="MyFlightbook.Printing.Layouts.LayoutNavy" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageHeader.ascx" TagPrefix="uc1" TagName="pageHeader" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageFooter.ascx" TagPrefix="uc1" TagName="pageFooter" %>
<%@ Register Src="~/Controls/mfbSignature.ascx" TagPrefix="uc1" TagName="mfbSignature" %>
<%@ Register Src="~/Controls/mfbImageList.ascx" TagPrefix="uc1" TagName="mfbImageList" %>


<asp:Repeater ID="rptPages" runat="server" OnItemDataBound="rptPages_ItemDataBound">
<ItemTemplate>
    <uc1:pageHeader runat="server" ID="pageHeader" UserName="<%# CurrentUser.UserName %>" />
    <table class="pageTable">
        <thead>
            <tr class="bordered">
                <th class="headerSmall" style="width:10ch" rowspan="3"><% =Resources.LogbookEntry.PrintHeaderDate %></th>
                <th class="headerSmall" style="width:10ch" colspan="2"><% =Resources.LogbookEntry.PrintHeaderAircraft %></th>
                <th class="headerSmall" style="width:10ch" rowspan="3"><% =Resources.LogbookEntry.PrintHeaderKindOfFlight %></th>
                <th class="headerSmall" colspan="4"><% =Resources.LogbookEntry.PrintHeaderPilotTime %></th>
                <th class="headerSmall" rowspan="3"><% =Resources.LogbookEntry.PrintHeaderSpecialCrew %></th>
                <th class="headerSmall" colspan="2"><% =Resources.LogbookEntry.PrintHeaderInstrumentTime %></th>
                <th class="headerSmall" rowspan="3"><% =Resources.LogbookEntry.FieldNight %></th>
                <th class="headerSmall" colspan="5"><% =Resources.LogbookEntry.FieldLanding %></th>
                <th class="headerSmall" rowspan="3"><span class="rotated"><% =Resources.LogbookEntry.PrintHeaderCatapult %></span></th>
                <th class="headerSmall" rowspan="3"><% =Resources.LogbookEntry.FieldApproaches %></th>
                <th class="headerSmall" rowspan="3" runat="server" id="Th1" style="width:1cm" Visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 0, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# OptionalColumn.OptionalColumnName(OptionalColumns, 0) %></div></th>
                <th class="headerSmall" rowspan="3" runat="server" id="Th2" style="width:1cm" Visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 1, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# OptionalColumn.OptionalColumnName(OptionalColumns, 1) %></div></th>
                <th class="headerSmall" rowspan="3" runat="server" id="Th3" style="width:1cm" Visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 2, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# OptionalColumn.OptionalColumnName(OptionalColumns, 2) %></div></th>
                <th class="headerSmall" rowspan="3" runat="server" id="Th4" style="width:1cm" Visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 3, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# OptionalColumn.OptionalColumnName(OptionalColumns, 3) %></div></th>

                <th class="headerSmall" rowspan="3"><% =Resources.LogbookEntry.PrintHeaderRemarks %></th>
            </tr>
            <tr class="bordered">
                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderModel %></th>
                <th class="headerSmall" style="width:1.2cm" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderSerialNumber %></th>

                <th class="headerSmall" style="width:1.2cm" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderTotalTime %></th>
                <th class="headerSmall" style="width:1.2cm" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderFirstPIlot %></th>
                <th class="headerSmall" style="width:1.2cm" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderCoPilot %></th>
                <th class="headerSmall" style="width:1.2cm" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderCommander %></th>

                <th class="headerSmall" style="width:1.2cm" rowspan="2"><% =Resources.LogbookEntry.FieldIMC %></th>
                <th class="headerSmall" style="width:1.2cm" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderSimInstrument %></th>

                <th class="headerSmall" colspan="3"><% =Resources.LogbookEntry.PrintHeaderCarrier %></th>
                <th class="headerSmall" rowspan="2"><span class="rotated"><% =Resources.LogbookEntry.PrintHeaderFCLP %></th>
                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.FieldLanding %></th>
            </tr>
            <tr class="bordered">
                <th class="headerSmall"><span class="rotated"><% =Resources.LogbookEntry.PrintHeaderCarrierArrest %></span></th>
                <th class="headerSmall"><span class="rotated"><% =Resources.LogbookEntry.PrintHeaderCarrierTouchAndGo %></span></th>
                <th class="headerSmall"><span class="rotated"><% =Resources.LogbookEntry.PrintHeaderCarrierBolt %></span></th>
            </tr>
        </thead>
        <asp:Repeater EnableViewState="false" ID="rptFlight" runat="server" OnItemDataBound="rptFlight_ItemDataBound">
            <ItemTemplate>
                <tr class="bordered" <%# ColorForFlight(Container.DataItem) %>>
                    <td><%# ChangeMarkerForFlight(Container.DataItem) %><%# ((DateTime) Eval("Date")).ToShortDateString() %></td>
                    <td><%#: Eval("ModelDisplay") %></td>
                    <td><%#: Eval("TailNumOrSimDisplay") %></td>

                    <td><%#: ((LogbookEntryDisplay) Container.DataItem).CustomProperties.StringValueForProperty(CustomPropertyType.KnownProperties.IDPropMilitaryKindOfFlightCode) %></td>
                    <td><%# Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("MilitaryFirstPilotTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td runat="server"><%# ((decimal) Eval("SIC") + (decimal) Eval("CoPilotTime")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("MilitaryACCommanderTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("SpecialCrewTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("IMC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("SimulatedIFR").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("Nighttime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%#: Eval("CarrierArrests").FormatInt() %></td>
                    <td><%#: Eval("CarrierTandG").FormatInt() %></td>
                    <td><%#: Eval("CarrierBolts").FormatInt() %></td>
                    <td><%#: Eval("FCLP").FormatInt() %></td>
                    <td class="numericColumn"><%# Eval("LandingDisplay") %></td>
                    <td><%#: Eval("Catapults").FormatInt() %></td>
                    <td><%# Eval("Approaches").FormatInt() %></td>
                    <td runat="server" id="tdoptColumn1" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 0, OptionalColumn.OptionalColumnRestriction.None) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(0) %></div></td>
                    <td runat="server" id="tdoptColumn2" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 1, OptionalColumn.OptionalColumnRestriction.None) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(1) %></div></td>
                    <td runat="server" id="tdoptColumn3" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 2, OptionalColumn.OptionalColumnRestriction.None) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(2) %></div></td>
                    <td runat="server" id="tdoptColumn4" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 3, OptionalColumn.OptionalColumnRestriction.None) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(3) %></div></td>
                    <td>
                        <div><%#: ((string[]) Eval("Airports")).Any() ? String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.PrintFullRoute, Eval("Route")) : String.Empty  %></div>
                        <div style="clear:left; white-space:pre-line;" dir="auto"><%# Eval("RedactedCommentWithReplacedApproaches") %></div>
                        <div style="white-space:pre-line;"><%#: Eval("CustPropertyDisplay") %></div>
                        <div><uc1:mfbSignature runat="server" ID="mfbSignature" EnableViewState="false" /></div>
                        <asp:Panel EnableViewState="false" ID="pnlFlightImages" runat="server" Visible="<%# IncludeImages %>">
                            <uc1:mfbImageList ID="mfbilFlights" runat="server" Columns="3" CanEdit="false" ImageClass="Flight" IncludeDocs="false" MaxImage="3" />
                        </asp:Panel>
                    </td>
                </tr>
            </ItemTemplate>
        </asp:Repeater>
        <asp:Repeater EnableViewState="false" ID="rptSubtotalCollections" runat="server" OnItemDataBound="rptSubtotalCollections_ItemDataBound">
            <ItemTemplate>
                <tr class="subtotal">
                    <td colspan="2" rowspan='<%# Eval("SubtotalCount") %>'><%# Eval("GroupTitle") %></td>
                    <asp:Repeater ID="rptSubtotals" runat="server">
                        <ItemTemplate>
                            <%# (Container.ItemIndex != 0) ? "<tr class=\"subtotal\">" : string.Empty %>
                            <td colspan="2"><%# ((LogbookEntryDisplay) Container.DataItem).CatClassDisplay %></td>

                            <td><%# ((LogbookEntryDisplay) Container.DataItem).TotalFlightTime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%#: Eval("FirstPIlotTotal").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%#: ((decimal)Eval("SIC") + (decimal)Eval("CoPilotTotal")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).CommanderTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).SpecialCrewTimeTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).IMC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).SimulatedIFR.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Nighttime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).CarrierArrestsTotal.FormatInt() %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).CarrierTandGTotal.FormatInt() %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).CarrierBoltsTotal.FormatInt() %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).FCLPTotal.FormatInt() %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Landings.FormatInt() %></td>
                            <td><%# ((LogbookEntryDisplay)Container.DataItem).CatapultsTotal.FormatInt() %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Approaches.FormatInt() %></td>

                            <td runat="server" id="tdoptColumnTotal1" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 0, OptionalColumn.OptionalColumnRestriction.None) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(0, CurrentUser.UsesHHMM) %></div></td>
                            <td runat="server" id="tdoptColumnTotal2" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 1, OptionalColumn.OptionalColumnRestriction.None) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(1, CurrentUser.UsesHHMM) %></div></td>
                            <td runat="server" id="tdoptColumnTotal3" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 2, OptionalColumn.OptionalColumnRestriction.None) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(2, CurrentUser.UsesHHMM) %></div></td>
                            <td runat="server" id="tdoptColumnTotal4" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 3, OptionalColumn.OptionalColumnRestriction.None) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(3, CurrentUser.UsesHHMM) %></div></td>
                            <td></td>
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
