<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_PrintingLayouts_layoutUSA" Codebehind="layoutUSA.ascx.cs" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageHeader.ascx" TagPrefix="uc1" TagName="pageHeader" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageFooter.ascx" TagPrefix="uc1" TagName="pageFooter" %>
<%@ Register Src="~/Controls/mfbSignature.ascx" TagPrefix="uc1" TagName="mfbSignature" %>

<asp:Repeater ID="rptPages" runat="server" OnItemDataBound="rptPages_ItemDataBound">
<ItemTemplate>
    <uc1:pageHeader runat="server" ID="pageHeader" UserName="<%# CurrentUser.UserName %>" />
    <table class="pageTable">
        <thead>
            <tr class="bordered">
                <th class="headerSmall" style="width:10ch" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderDate %></th>
                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderModel %></th>
                <th class="headerSmall" style="width:1.2cm" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderAircraft %></th>
                <th class="headerBig" colspan="2"><% =Resources.LogbookEntry.PrintHeaderRoute %></th>
                <th class="headerBig" rowspan="2"><%=Resources.LogbookEntry.PrintHeaderRemarks %></th>
                <th class="headerSmall" rowspan="2" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderApproaches %></th>
                <th class="headerBig" colspan="2" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderLanding %></th>
                <th class="headerBig" colspan="<% = 3 + OptionalColumn.CatClassColumnCount(OptionalColumns) %>" style="width:3cm"><%=Resources.LogbookEntry.PrintHeaderCategory2 %></th>
                <th class="headerBig" colspan="3"><%=Resources.LogbookEntry.PrintHeaderCondition2 %></th>
                <th class="headerSmall" rowspan="2" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderSim %></th>
                <th class="headerSmall" rowspan="2" runat="server" id="optcolumnHeader1" style="width:1cm" Visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 0, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# OptionalColumn.OptionalColumnName(OptionalColumns, 0) %></div></th>
                <th class="headerSmall" rowspan="2" runat="server" id="optcolumnHeader2" style="width:1cm" Visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 1, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# OptionalColumn.OptionalColumnName(OptionalColumns, 1) %></div></th>
                <th class="headerSmall" rowspan="2" runat="server" id="optcolumnHeader3" style="width:1cm" Visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 2, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# OptionalColumn.OptionalColumnName(OptionalColumns, 2) %></div></th>
                <th class="headerSmall" rowspan="2" runat="server" id="optcolumnHeader4" style="width:1cm" Visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 3, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# OptionalColumn.OptionalColumnName(OptionalColumns, 3) %></div></th>
                <th class="headerBig" colspan="<% = 3 + (CurrentUser.TracksSecondInCommandTime ? 1 : 0) + (CurrentUser.IsInstructor ? 1 : 0) %>"><%=Resources.LogbookEntry.PrintHeaderPilotFunction2 %></th>
                <th class="headerSmall" rowspan="2" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderTotalTime %></th>
            </tr>
            <tr class="bordered">
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderFrom %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderTo %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderDay %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderNight %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderSEL %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderMEL %></th>
                <th class="headerSmall" style="width:1cm" runat="server" id="optcolumnHeaderCatClass1" Visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 0, OptionalColumn.OptionalColumnRestriction.CatClassOnly) %>"><%# OptionalColumn.OptionalColumnName(OptionalColumns, 0) %></th>
                <th class="headerSmall" style="width:1cm" runat="server" id="optcolumnHeaderCatClass2" Visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 1, OptionalColumn.OptionalColumnRestriction.CatClassOnly) %>"><%# OptionalColumn.OptionalColumnName(OptionalColumns, 1) %></th>
                <th class="headerSmall" style="width:1cm" runat="server" id="optcolumnHeaderCatClass3" Visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 2, OptionalColumn.OptionalColumnRestriction.CatClassOnly) %>"><%# OptionalColumn.OptionalColumnName(OptionalColumns, 2) %></th>
                <th class="headerSmall" style="width:1cm" runat="server" id="optcolumnHeaderCatClass4" Visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 3, OptionalColumn.OptionalColumnRestriction.CatClassOnly) %>"><%# OptionalColumn.OptionalColumnName(OptionalColumns, 3) %></th>
                <th class="headerSmall" style="width:1cm">&nbsp;</th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderNight %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderIMC %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderSimInstrument %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderCrossCountry %></th>
                <th class="headerSmall" style="width:1cm" runat="server" visible="<%# CurrentUser.IsInstructor %>"><% =Resources.LogbookEntry.FieldCFI %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.FieldDual %></th>
                <th class="headerSmall" style="width:1cm" runat="server" visible="<%# CurrentUser.TracksSecondInCommandTime %>"><% =Resources.LogbookEntry.FieldSIC %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderPIC2 %></th>
            </tr>
        </thead>
        <asp:Repeater EnableViewState="false" ID="rptFlight" runat="server" OnItemDataBound="rptFlight_ItemDataBound">
            <ItemTemplate>
                <tr class="bordered">
                    <td><%# ((DateTime) Eval("Date")).ToShortDateString() %></td>
                    <td><%#: Eval("ModelDisplay") %></td>
                    <td><%#: Eval("TailNumDisplay") %></td>
                    <td><%#: Eval("Departure") %></td>
                    <td><%#: Eval("Destination") %></td>
                    <td>
                        <div><%#: ((string[]) Eval("Airports")).Count() <= 2 ? string.Empty : String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.PrintFullRoute, Eval("Route")) %></div>
                        <div style="clear:left; white-space:pre-line;" dir="auto"><%# Eval("RedactedCommentWithReplacedApproaches") %></div>
                        <div style="white-space:pre-line;"><%#: Eval("CustPropertyDisplay") %></div>
                        <div><uc1:mfbSignature runat="server" ID="mfbSignature" EnableViewState="false" /></div>
                    </td>
                    <td><%# Eval("Approaches") %></td>
                    <td><%# Eval("NetDayLandings") %></td>
                    <td><%# Eval("NetNightLandings") %></td>
                    <td><%#: (((int)Eval("EffectiveCatClass")) == (int) CategoryClass.CatClassID.ASEL) ? Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM) : string.Empty %></td>
                    <td><%#: (((int)Eval("EffectiveCatClass")) == (int) CategoryClass.CatClassID.AMEL) ? Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM) : string.Empty %></td>
                    <td runat="server" id="tdOptColumnCatClass1" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 0, OptionalColumn.OptionalColumnRestriction.CatClassOnly) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(0) %></div></td>
                    <td runat="server" id="tdOptColumnCatClass2" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 1, OptionalColumn.OptionalColumnRestriction.CatClassOnly) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(1) %></div></td>
                    <td runat="server" id="tdOptColumnCatClass3" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 2, OptionalColumn.OptionalColumnRestriction.CatClassOnly) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(2) %></div></td>
                    <td runat="server" id="tdOptColumnCatClass4" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 3, OptionalColumn.OptionalColumnRestriction.CatClassOnly) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(3) %></div></td>
                    <td><%#: OtherCatClassValue((LogbookEntryDisplay) Container.DataItem) %></td>
                    <td><%# Eval("Nighttime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("IMC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("SimulatedIFR").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("GroundSim").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td runat="server" id="tdoptColumn1" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 0, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(0) %></div></td>
                    <td runat="server" id="tdoptColumn2" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 1, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(1) %></div></td>
                    <td runat="server" id="tdoptColumn3" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 2, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(2) %></div></td>
                    <td runat="server" id="tdoptColumn4" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 3, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(3) %></div></td>
                    <td><%# Eval("CrossCountry").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td runat="server" visible="<%# CurrentUser.IsInstructor %>"><%# Eval("CFI").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("Dual").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td runat="server" visible="<%# CurrentUser.TracksSecondInCommandTime %>"><%# Eval("SIC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("PIC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                </tr>
            </ItemTemplate>
        </asp:Repeater>
        <asp:Repeater EnableViewState="false" ID="rptSubtotalCollections" runat="server" OnItemDataBound="rptSubtotalCollections_ItemDataBound">
            <ItemTemplate>
                <tr class="subtotal">
                    <td colspan="3" class="subtotalLabel" rowspan='<%# Eval("SubtotalCount") %>'></td>
                    <td colspan="2" rowspan='<%# Eval("SubtotalCount") %>'><%# Eval("GroupTitle") %></td>
                    <asp:Repeater ID="rptSubtotals" runat="server">
                        <ItemTemplate>
                            <%# (Container.ItemIndex != 0) ? "<tr class=\"subtotal\">" : string.Empty %>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).CatClassDisplay %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Approaches.ToString(System.Globalization.CultureInfo.CurrentCulture) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).NetDayLandings %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).NetNightLandings %></td>
                            <td colspan="<% = 3 + OptionalColumn.CatClassColumnCount(OptionalColumns) %>"><%# ((LogbookEntryDisplay) Container.DataItem).TotalFlightTime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Nighttime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).IMC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).SimulatedIFR.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).GroundSim.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td runat="server" id="tdoptColumnTotal1" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 0, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(0, CurrentUser.UsesHHMM) %></div></td>
                            <td runat="server" id="tdoptColumnTotal2" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 1, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(1, CurrentUser.UsesHHMM) %></div></td>
                            <td runat="server" id="tdoptColumnTotal3" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 2, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(2, CurrentUser.UsesHHMM) %></div></td>
                            <td runat="server" id="tdoptColumnTotal4" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 3, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(3, CurrentUser.UsesHHMM) %></div></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).CrossCountry.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td runat="server" visible="<%# CurrentUser.IsInstructor %>"><%# ((LogbookEntryDisplay) Container.DataItem).CFI.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Dual.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td runat="server" visible="<%# CurrentUser.TracksSecondInCommandTime %>"><%# ((LogbookEntryDisplay) Container.DataItem).SIC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).PIC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
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
