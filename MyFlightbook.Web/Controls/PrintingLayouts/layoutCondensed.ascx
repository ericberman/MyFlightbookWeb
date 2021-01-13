<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="layoutCondensed.ascx.cs" Inherits="Controls_PrintingLayouts_layoutCondensed" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageFooter.ascx" TagPrefix="uc1" TagName="pageFooter" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageHeader.ascx" TagPrefix="uc1" TagName="pageHeader" %>
<%@ Register Src="~/Controls/mfbSignature.ascx" TagPrefix="uc1" TagName="mfbSignature" %>
<%@ Register Src="~/Controls/mfbImageList.ascx" TagPrefix="uc1" TagName="mfbImageList" %>
<asp:Repeater ID="rptPages" runat="server" OnItemDataBound="rptPages_ItemDataBound">
    <ItemTemplate>
        <uc1:pageHeader runat="server" ID="pageHeader" UserName="<%# CurrentUser.UserName %>" />
        <table class="pageTable">
            <thead>
                <tr class="bordered">
                    <th class="headerSmall" style="width:10ch" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderDate %></th>
                    <th class="headerSmall" style="width:1cm" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderSegments %></th>
                    <th class="headerSmall clipped"><% =Resources.LogbookEntry.PrintHeaderModel %></th>
                    <th class="headerSmall clipped"><% =Resources.LogbookEntry.PrintHeaderRoute %></th>
                    <th class="headerSmall" runat="server" id="optcolumnHeader1" style="width:1cm" Visible="<%# ShowOptionalColumn(0) %>"><div><%# OptionalColumnName(0) %></div></th>
                    <th class="headerSmall" runat="server" id="optcolumnHeader2" style="width:1cm" Visible="<%# ShowOptionalColumn(1) %>"><div><%# OptionalColumnName(1) %></div></th>
                    <th class="headerSmall" runat="server" id="optcolumnHeader3" style="width:1cm" Visible="<%# ShowOptionalColumn(2) %>"><div><%# OptionalColumnName(2) %></div></th>
                    <th class="headerSmall" runat="server" id="optcolumnHeader4" style="width:1cm" Visible="<%# ShowOptionalColumn(3) %>"><div><%# OptionalColumnName(3) %></div></th>
                    <th class="headerSmall" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderLandingsShort %></th>
                    <th class="headerSmall" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderApproachesShort %></th>
                    <th class="headerSmall" style="width:1cm"><%=Resources.LogbookEntry.FieldNight %></th>
                    <th class="headerSmall" style="width:1cm"><%=Resources.LogbookEntry.FieldXCountry %></th>
                    <th class="headerSmall" style="width:1cm"><%=Resources.LogbookEntry.FieldIMC %></th>
                    <th class="headerSmall" style="width:1cm"><%=Resources.LogbookEntry.FieldSimIMC %></th>
                    <th class="headerSmall" style="width:1cm"><%=Resources.LogbookEntry.FieldDual %></th>
                    <th class="headerSmall" style="width:1cm"><%=Resources.LogbookEntry.FieldPIC %></th>
                    <th class="headerSmall" style="width:1cm"><%=Resources.LogbookEntry.FieldCFI %></th>
                    <th class="headerSmall" style="width:1cm"><%=Resources.LogbookEntry.FieldSIC %></th>
                    <th class="headerSmall" style="width:1cm"><%=Resources.LogbookEntry.FieldTotal %></th>
                </tr>
            </thead>
            <asp:Repeater EnableViewState="false" ID="rptFlight" runat="server">
                <ItemTemplate>
                    <tr class="bordered" <%# FlightColor(Container.DataItem) %>>
                        <td><%# ((DateTime) Eval("Date")).ToShortDateString() %></td>
                        <td><%# ((int) Eval("FlightCount")).FormatInt() %></td>
                        <td class="clipped"><%#: Eval("ModelDisplay") %></td>
                        <td class="clipped"><span style="font-weight:bold"><%# Eval("Route") %></span> <%#Eval("RedactedCommentWithReplacedApproaches") %></td>
                        <td runat="server" id="tdOptColumnCatClass1" visible="<%# ShowOptionalColumn(0) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(0) %></div></td>
                        <td runat="server" id="tdOptColumnCatClass2" visible="<%# ShowOptionalColumn(1) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(1) %></div></td>
                        <td runat="server" id="tdOptColumnCatClass3" visible="<%# ShowOptionalColumn(2) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(2) %></div></td>
                        <td runat="server" id="tdOptColumnCatClass4" visible="<%# ShowOptionalColumn(3) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(3) %></div></td>
                        <td><%# Eval("Landings").FormatInt() %></td>
                        <td><%# Eval("Approaches").FormatInt() %></td>
                        <td><%# Eval("Nighttime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Eval("CrossCountry").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Eval("IMC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Eval("SimulatedIFR").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Eval("Dual").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Eval("PIC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Eval("CFI").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Eval("SIC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                        <td><%# Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    </tr>
                </ItemTemplate>
            </asp:Repeater>
            <asp:Repeater EnableViewState="false" ID="rptSubtotalCollections" runat="server" OnItemDataBound="rptSubtotalCollections_ItemDataBound">
                <ItemTemplate>
                    <tr class="subtotal">
                        <td colspan="2" class="clipped" rowspan='<%# Eval("SubtotalCount") %>'><%# Eval("GroupTitle") %></td>
                        <asp:Repeater ID="rptSubtotals" runat="server">
                            <ItemTemplate>
                                <%# (Container.ItemIndex != 0) ? "<tr class=\"subtotal clipped\">" : string.Empty %>
                                <td colspan="2"><%# ((LogbookEntryDisplay) Container.DataItem).CatClassDisplay %></td>
                                <td runat="server" id="tdoptColumnTotal1" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 0, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(0, CurrentUser.UsesHHMM) %></div></td>
                                <td runat="server" id="tdoptColumnTotal2" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 1, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(1, CurrentUser.UsesHHMM) %></div></td>
                                <td runat="server" id="tdoptColumnTotal3" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 2, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(2, CurrentUser.UsesHHMM) %></div></td>
                                <td runat="server" id="tdoptColumnTotal4" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 3, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(3, CurrentUser.UsesHHMM) %></div></td>
                                <td><%# (((LogbookEntryDisplay) Container.DataItem).LandingsTotal).FormatInt() %></td>
                                <td><%# (((LogbookEntryDisplay) Container.DataItem).Approaches).FormatInt() %></td>
                                <td><%# (((LogbookEntryDisplay) Container.DataItem).Nighttime).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# (((LogbookEntryDisplay) Container.DataItem).CrossCountry).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# (((LogbookEntryDisplay) Container.DataItem).IMC).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# (((LogbookEntryDisplay) Container.DataItem).SimulatedIFR).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# (((LogbookEntryDisplay) Container.DataItem).Dual).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# (((LogbookEntryDisplay) Container.DataItem).PIC).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# (((LogbookEntryDisplay) Container.DataItem).CFI).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# (((LogbookEntryDisplay) Container.DataItem).SIC).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# (((LogbookEntryDisplay) Container.DataItem).TotalFlightTime).FormatDecimal(CurrentUser.UsesHHMM) %></td>
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
