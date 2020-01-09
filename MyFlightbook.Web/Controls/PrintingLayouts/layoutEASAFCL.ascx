<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_PrintingLayouts_layoutEASAFCL" Codebehind="layoutEASAFCL.ascx.cs" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageFooter.ascx" TagPrefix="uc1" TagName="pageFooter" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageHeader.ascx" TagPrefix="uc1" TagName="pageHeader" %>
<%@ Register Src="~/Controls/mfbSignature.ascx" TagPrefix="uc1" TagName="mfbSignature" %>

<asp:Repeater ID="rptPages" runat="server" OnItemDataBound="rptPages_ItemDataBound">
<ItemTemplate>
    <uc1:pageHeader runat="server" ID="pageHeader" UserName="<%# CurrentUser.UserName %>" />
    <table class="pageTable">
        <thead>
            <tr class="bordered">
                <th class="headerSmall" rowspan="3">#</th>
                <th class="headerSmall">1</th>
                <th class="headerSmall" colspan="2">2</th>
                <th class="headerSmall" colspan="2">3</th>
                <th class="headerSmall" colspan="2">4</th>
                <th class="headerSmall">5</th>
                <th class="headerSmall">6</th>
                <th class="headerSmall">7</th>
                <th class="headerSmall" colspan="2">8</th>
                <th class="headerSmall" colspan="2">9</th>
                <th class="headerSmall" colspan="4">10</th>
                <th class="headerSmall">11</th>
                <th class="headerSmall">12</th>
            </tr>
            <tr class="borderedBold">
                <th rowspan="2" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderDate %></th>
                <th colspan="2" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderDeparture %></th>
                <th colspan="2" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderArrival %></th>
                <th colspan="2" class="headerBig"><% =Resources.LogbookEntry.PrintHeaderAircraft %></th>
                <th rowspan="2" class="headerBig"><%=Resources.LogbookEntry.PrintHeaderCategory %></th>
                <th rowspan="2" class="headerBig"><%=Resources.LogbookEntry.PrintHeaderTotalTime %></th>
                <th rowspan="2" class="headerBig"><%=Resources.LogbookEntry.PrintHeaderPICName %></th>
                <th colspan="2" class="headerBig"><%=Resources.LogbookEntry.PrintHeaderLanding %></th>
                <th colspan="2" class="headerBig"><%=Resources.LogbookEntry.PrintHeaderCondition %></th>
                <th colspan="4" class="headerBig"><%=Resources.LogbookEntry.PrintHeaderPilotFunction %></th>
                <th class="headerBig"><%=Resources.LogbookEntry.PrintHeaderFSTD %></th>
                <th rowspan="2" class="headerBig"><%=Resources.LogbookEntry.PrintHeaderRemarks %></th>
            </tr>
            <tr class="borderedBold">
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderPlace %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderTime %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderPlace %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderTime %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderModel %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderRegistration %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderDay %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderNight %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderNight %></th>
                <th class="headerSmall"><div><%=Resources.LogbookEntry.PrintHeaderIFR %></div><div style="text-align:center; font-size:smaller"><%=Resources.LogbookEntry.PrintHeaderIFRSubhead %></div></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.FieldPIC %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.FieldSIC %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.FieldDual %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.FieldCFI %></th>
                <th class="headerSmall"><%=Resources.LogbookEntry.PrintHeaderFSTDTime %></th>
            </tr>                       
        </thead>
        <asp:Repeater EnableViewState="false" ID="rptFlight" runat="server" OnItemDataBound="rptFlight_ItemDataBound">
            <ItemTemplate>
                <tr class="bordered">
                    <td><%# Eval("Index") %></td>
                    <td><%# ((DateTime) Eval("Date")).ToShortDateString() %></td>
                    <td><%#: Eval("Departure") %></td>
                    <td><%# ((DateTime) Eval("DepartureTime")).UTCFormattedStringOrEmpty(CurrentUser.UsesUTCDateOfFlight).Replace(" ", "<br />") %></td>
                    <td><%#: Eval("Destination") %></td>
                    <td><%# ((DateTime) Eval("ArrivalTime")).UTCFormattedStringOrEmpty(CurrentUser.UsesUTCDateOfFlight).Replace(" ", "<br />") %></td>
                    <td><%#: Eval("ModelDisplay") %></td>
                    <td><%#: Eval("TailNumDisplay") %></td>
                    <td><%#: Eval("CatClassDisplay") %></td>
                    <td><%# Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%#: Eval("PICName") %></td>
                    <td><%# Eval("NetDayLandings") %></td>
                    <td><%# Eval("NetNightLandings") %></td>
                    <td><%# Eval("Nighttime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("IFRTime").FormatDecimal(CurrentUser.UsesHHMM) %><div><%# Eval("InstrumentTimeDisplay") %></div></td>
                    <td><%# Eval("PIC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("SIC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("Dual").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("CFI").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("GroundSim").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td>
                        <div style="clear:left; white-space:pre-line;" dir="auto"><%# Eval("RedactedCommentWithReplacedApproaches") %></div>
                        <div style="white-space:pre-line;"><%#: Eval("CustPropertyDisplay") %></div>
                        <div><uc1:mfbSignature runat="server" ID="mfbSignature" EnableViewState="false" /></div>
                    </td>
                </tr>
            </ItemTemplate>
        </asp:Repeater>
        <asp:Repeater EnableViewState="false" ID="rptSubtotalCollections" runat="server" OnItemDataBound="rptSubtotalCollections_ItemDataBound">
            <ItemTemplate>
                <tr class="subtotal">
                    <td class="subtotalLabel" colspan="5" rowspan='<%# Eval("SubtotalCount") %>'></td>
                    <td colspan="2" rowspan='<%# Eval("SubtotalCount") %>'><%# Eval("GroupTitle") %></td>
                    <asp:Repeater ID="rptSubtotals" runat="server">
                        <ItemTemplate>
                            <%# (Container.ItemIndex != 0) ? "<tr class=\"subtotal\">" : string.Empty %>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).CatClassDisplay %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).FlightCount.ToString(System.Globalization.CultureInfo.CurrentCulture) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).TotalFlightTime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).NetDayLandings %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).NetNightLandings %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Nighttime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).IFRTimeTotal.FormatDecimal(CurrentUser.UsesHHMM) %> <%# ((LogbookEntryDisplay) Container.DataItem).InstrumentTimeDisplay %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).PIC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).SIC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Dual.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).CFI.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).GroundSim.FormatDecimal(CurrentUser.UsesHHMM) %></td>
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
