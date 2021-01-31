<%@ Control Language="C#" AutoEventWireup="true" Codebehind="layoutCASA.ascx.cs" Inherits="Controls_PrintingLayouts_layoutCASA" %>
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
                <th class="headerSmall" colspan="2"><% =Resources.LogbookEntry.PrintHeaderAircraft %></th>
                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderPICName %></th>
                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderCoPilotOrStudent %></th>
                <th class="headerBig" rowspan="2"><%=Resources.LogbookEntry.PrintHeaderFlightDetails %></th>
                <th class="headerSmall" rowspan="2" runat="server" id="optcolumnHeader1" style="width:1cm" Visible="<%# ShowOptionalColumn(0) %>"><div><%# OptionalColumnName(0) %></div></th>
                <th class="headerSmall" rowspan="2" runat="server" id="optcolumnHeader2" style="width:1cm" Visible="<%# ShowOptionalColumn(1) %>"><div><%# OptionalColumnName(1) %></div></th>
                <th class="headerSmall" rowspan="2" runat="server" id="optcolumnHeader3" style="width:1cm" Visible="<%# ShowOptionalColumn(2) %>"><div><%# OptionalColumnName(2) %></div></th>
                <th class="headerSmall" rowspan="2" runat="server" id="optcolumnHeader4" style="width:1cm" Visible="<%# ShowOptionalColumn(3) %>"><div><%# OptionalColumnName(3) %></div></th>
                <th class="headerSmall" colspan="2" style="width:1cm"><%=Resources.LogbookEntry.FieldDual %></th>
                <th class="headerSmall" colspan="2" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderPICUS %></th>
                <th class="headerSmall" colspan="2" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderPIC2 %></th>
                <th class="headerSmall" colspan="2" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderCoPilot %></th>
                <th class="headerSmall" colspan="2"><% =Resources.LogbookEntry.PrintHeaderInstrumentShort %></th>
            </tr>
            <tr class="bordered">
                <th class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderTypeShort %></th>
                <th class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderRegistration %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderDay %></th>
                <th class="headerSmall nightCol" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderNight %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderDay %></th>
                <th class="headerSmall nightCol" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderNight %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderDay %></th>
                <th class="headerSmall nightCol" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderNight %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderDay %></th>
                <th class="headerSmall nightCol" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderNight %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderIMC %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderSimInstrument %></th>
            </tr>
        </thead>
        <asp:Repeater EnableViewState="false" ID="rptFlight" runat="server" OnItemDataBound="rptFlight_ItemDataBound">
            <ItemTemplate>
                <tr class="bordered" <%# ColorForFlight(Container.DataItem) %>>
                    <td><%# ((DateTime) Eval("Date")).ToShortDateString() %></td>
                    <td>
                        <div><%#: Eval("ModelDisplay") %></div>
                    </td>
                    <td><%#: Eval("TailNumDisplay") %></td>
                    <td><%#: Eval("PICName") %></td>
                    <td><%#: Eval("SICName") %> <%#: Eval("StudentName") %></td>
                    <td>
                        <span style="text-transform:uppercase; font-style:italic; font-weight:bold;"><%#: Eval("Route") %></span>
                        <span style="white-space:pre-line;" dir="auto"><%# Eval("RedactedCommentWithReplacedApproaches") %></span>
                        <div style="white-space:pre-line;"><%#: Eval("CustPropertyDisplay") %></div>
                        <div><uc1:mfbSignature runat="server" ID="mfbSignature" EnableViewState="false" /></div>
                        <asp:Panel EnableViewState="false" ID="pnlFlightImages" runat="server" Visible="<%# IncludeImages %>">
                            <uc1:mfbImageList ID="mfbilFlights" runat="server" Columns="3" CanEdit="false" ImageClass="Flight" IncludeDocs="false" MaxImage="3" />
                        </asp:Panel>
                    </td>
                    <td runat="server" id="tdOptColumnCatClass1" visible="<%# ShowOptionalColumn(0) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(0) %></div></td>
                    <td runat="server" id="tdOptColumnCatClass2" visible="<%# ShowOptionalColumn(1) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(1) %></div></td>
                    <td runat="server" id="tdOptColumnCatClass3" visible="<%# ShowOptionalColumn(2) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(2) %></div></td>
                    <td runat="server" id="tdOptColumnCatClass4" visible="<%# ShowOptionalColumn(3) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(3) %></div></td>
                    <td><%# ((decimal) Eval("Dual") - Math.Min((decimal) Eval("Dual"), (decimal) Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td class="nightCol"><%# Math.Min((decimal) Eval("Dual"), (decimal) Eval("Nighttime")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# ((decimal) Eval("PICUSTime") - Math.Min((decimal) Eval("PICUSTime"), (decimal) Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td class="nightCol"><%# Math.Min((decimal) Eval("PICUSTime"), (decimal) Eval("Nighttime")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# ((decimal) Eval("PIC") - Math.Min((decimal) Eval("PIC"), (decimal) Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td class="nightCol"><%# Math.Min((decimal) Eval("PIC"), (decimal) Eval("Nighttime")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# ((decimal) Eval("SIC") - Math.Min((decimal) Eval("SIC"), (decimal) Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td class="nightCol"><%# Math.Min((decimal) Eval("SIC"), (decimal) Eval("Nighttime")).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("IMC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("SimulatedIFR").FormatDecimal(CurrentUser.UsesHHMM) %></td>
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
                            <td runat="server" id="tdoptColumnTotal1" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 0, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(0, CurrentUser.UsesHHMM) %></div></td>
                            <td runat="server" id="tdoptColumnTotal2" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 1, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(1, CurrentUser.UsesHHMM) %></div></td>
                            <td runat="server" id="tdoptColumnTotal3" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 2, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(2, CurrentUser.UsesHHMM) %></div></td>
                            <td runat="server" id="tdoptColumnTotal4" visible="<%# OptionalColumn.ShowOptionalColumn(OptionalColumns, 3, OptionalColumn.OptionalColumnRestriction.NotCatClass) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(3, CurrentUser.UsesHHMM) %></div></td>
                            <td><%# (((LogbookEntryDisplay) Container.DataItem).Dual - ((LogbookEntryDisplay) Container.DataItem).NightDualTotal).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td class="nightCol"><%# ((LogbookEntryDisplay) Container.DataItem).NightDualTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# (((LogbookEntryDisplay) Container.DataItem).PICUSTotal - ((LogbookEntryDisplay) Container.DataItem).NightPICUSTotal).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td class="nightCol"><%# ((LogbookEntryDisplay) Container.DataItem).NightPICUSTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# (((LogbookEntryDisplay) Container.DataItem).PIC - ((LogbookEntryDisplay) Container.DataItem).NightPICTotal).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td class="nightCol"><%# ((LogbookEntryDisplay) Container.DataItem).NightPICTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# (((LogbookEntryDisplay) Container.DataItem).SIC - ((LogbookEntryDisplay) Container.DataItem).NightSICTotal).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td class="nightCol"><%# ((LogbookEntryDisplay) Container.DataItem).NightSICTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).IMC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).SimulatedIFR.FormatDecimal(CurrentUser.UsesHHMM) %></td>
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
