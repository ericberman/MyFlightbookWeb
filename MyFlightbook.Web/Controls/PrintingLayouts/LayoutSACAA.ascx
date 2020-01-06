<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_PrintingLayouts_LayoutSACAA" Codebehind="LayoutSACAA.ascx.cs" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageFooter.ascx" TagPrefix="uc1" TagName="pageFooter" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageHeader.ascx" TagPrefix="uc1" TagName="pageHeader" %>
<%@ Register Src="~/Controls/mfbSignature.ascx" TagPrefix="uc1" TagName="mfbSignature" %>

<asp:Repeater ID="rptPages" runat="server" OnItemDataBound="rptPages_ItemDataBound">
<ItemTemplate>
    <uc1:pageHeader runat="server" ID="pageHeader" UserName="<%# CurrentUser.UserName %>" />
    <table class="pageTable">
        <thead>
            <tr class="bordered">
                <th class="headerSmall">1</th>
                <th class="headerSmall">2</th>
                <th class="headerSmall">3</th>
                <th class="headerSmall">4</th>
                <th class="headerSmall">5</th>
                <th class="headerSmall">6</th>
                <th class="headerSmall">7</th>
                <th class="headerSmall">8</th>
                <th class="headerSmall">9</th>
                <th class="headerSmall">10</th>
                <th class="headerSmall">11</th>
                <th class="headerSmall">12</th>
                <th class="headerSmall">13</th>
                <th class="headerSmall">14</th>
                <th class="headerSmall">15</th>
                <th class="headerSmall">16</th>
                <th class="headerSmall">17</th>
                <th class="headerSmall">18</th>
                <th class="headerSmall">19</th>
                <th class="headerSmall">20</th>
                <th class="headerSmall">21</th>
            </tr>
            <tr class="borderedBold">
                <th rowspan="3" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderDate %></th>
                <th rowspan="3" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderModel %></th>
                <th rowspan="3" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderCategory2 %></th>
                <th rowspan="3" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderAircraft %></th>
                <th rowspan="3" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderPICName %></th>
                <th rowspan="3" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderRoute %></th>
                <th colspan="2" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderInstrumentTime %></th>
                <th rowspan="3" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderInstructorTime %></th>
                <th rowspan="3" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderFSTDTotalTime %></th>
                <th colspan="8" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderAircraftTimes %></th>
                <th rowspan="2" colspan="2" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderLanding %></th>
                <th rowspan="3" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderRemarks %></th>
            </tr>
            <tr class="borderedBold">
                <th rowspan="2" class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderTime %></th>
                <th rowspan="2" class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderFSTDInstrumentTime %></th>

                <th colspan="4" class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderDay %></th>
                <th colspan="4" class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderNight %></th>

            </tr>                       
            <tr class="borderedBold">
                <th class="headerSmall"><%=Resources.LogbookEntry.FieldDual %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.FieldPIC %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderPICUS %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderCoPilot %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.FieldDual %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.FieldPIC %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderPICUS %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderCoPilot %></th>

                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderDay %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderNight %></th>
            </tr>
        </thead>
        <asp:Repeater EnableViewState="false" ID="rptFlight" runat="server" OnItemDataBound="rptFlight_ItemDataBound">
            <ItemTemplate>
                <tr class="bordered">
                    <td><%# ((DateTime) Eval("Date")).ToShortDateString() %></td>
                    <td><%#: Eval("ModelDisplay") %></td>
                    <td><%#: Eval("CatClassDisplay") %></td>
                    <td><%#: Eval("TailNumDisplay") %></td>
                    <td><%#: Eval("PICName") %></td>
                    <td><%#: Eval("Route") %></td>
                    <td><%# Eval("IMC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# ((bool) Eval("IsFSTD")) ? Eval("SimulatedIFR").FormatDecimal(CurrentUser.UsesHHMM) : string.Empty %></td>
                    <td><%# Eval("CFI").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("GroundSim").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Math.Max(((decimal)Eval("Dual")) - ((decimal)Eval("Nighttime")), 0.0M).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Math.Max(((decimal)Eval("PIC")) - ((decimal)Eval("Nighttime")), 0.0M).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Math.Max(((decimal)Eval("PICUSTime")) - ((decimal)Eval("Nighttime")), 0.0M).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Math.Max(((decimal)Eval("SIC")) - ((decimal)Eval("Nighttime")), 0.0M).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Math.Min(((decimal)Eval("Dual")), ((decimal)Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Math.Min(((decimal)Eval("PIC")), ((decimal)Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Math.Min(((decimal)Eval("PICUSTime")), ((decimal)Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Math.Min(((decimal)Eval("SIC")), ((decimal)Eval("Nighttime"))).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Convert.ToInt32(Eval("NetDayLandings")) %></td>
                    <td><%# Eval("NetNightLandings") %></td>
                    <td>
                        <div style="clear:left; white-space:pre-line;" dir="auto"><%# ((string) Eval("RedactedCommentWithReplacedApproaches")) %></div>
                        <div style="white-space:pre-line;"><%#: Eval("CustPropertyDisplay") %></div>
                        <div><uc1:mfbSignature runat="server" ID="mfbSignature" EnableViewState="false" /></div>
                    </td>
                </tr>
            </ItemTemplate>
        </asp:Repeater>
        <asp:Repeater EnableViewState="false" ID="rptSubtotalCollections" runat="server" OnItemDataBound="rptSubtotalCollections_ItemDataBound">
            <ItemTemplate>
                <tr class="subtotal">
                    <td class="subtotalLabel" colspan="2" rowspan='<%# Eval("SubtotalCount") %>'></td>
                    <td colspan="2" rowspan='<%# Eval("SubtotalCount") %>'><%# Eval("GroupTitle") %></td>
                    <asp:Repeater ID="rptSubtotals" runat="server">
                        <ItemTemplate>
                            <%# (Container.ItemIndex != 0) ? "<tr class=\"subtotal\">" : string.Empty %>
                            <td colspan="2"><%# ((LogbookEntryDisplay) Container.DataItem).CatClassDisplay %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).InstrumentAircraftTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).InstrumentFSTDTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).CFI.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).GroundSim.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# Math.Max(((LogbookEntryDisplay) Container.DataItem).Dual - ((LogbookEntryDisplay) Container.DataItem).NightDualTotal, 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# Math.Max(((LogbookEntryDisplay) Container.DataItem).PIC - ((LogbookEntryDisplay) Container.DataItem).NightPICTotal, 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# Math.Max(((LogbookEntryDisplay) Container.DataItem).PICUSTotal - ((LogbookEntryDisplay) Container.DataItem).NightPICUSTotal, 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# Math.Max(((LogbookEntryDisplay) Container.DataItem).SIC - ((LogbookEntryDisplay) Container.DataItem).NightSICTotal, 0).FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).NightDualTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).NightPICTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).NightPICUSTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).NightSICTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).NetDayLandings %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).NetNightLandings %></td>
                            <td></td>
                            <td class="subtotalLabel"></td>
                            <%# (Container.ItemIndex != 0) ? "</tr>" : string.Empty %>
                        </ItemTemplate>
                    </asp:Repeater>
                </tr>
            </ItemTemplate>
        </asp:Repeater>
    </table>
    <uc1:pageFooter runat="server" ID="pageFooter" ShowFooter="<%# ShowFooter %>" UserName="<%# CurrentUser.UserName %>" PageNum='<%#Eval("PageNum") %>' TotalPages='<%# Eval("TotalPages") %>'>
    </uc1:pageFooter>
</ItemTemplate>
</asp:Repeater>