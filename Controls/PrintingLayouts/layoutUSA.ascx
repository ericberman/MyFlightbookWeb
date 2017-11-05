<%@ Control Language="C#" AutoEventWireup="true" CodeFile="layoutUSA.ascx.cs" Inherits="Controls_PrintingLayouts_layoutUSA" %>
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
                <th class="headerSmall" style="width:3cm" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderModel %></th>
                <th class="headerSmall" style="width:1.2cm" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderAircraft %></th>
                <th class="headerBig" colspan="2"><% =Resources.LogbookEntry.PrintHeaderRoute %></th>
                <th class="headerBig" rowspan="2"><%=Resources.LogbookEntry.PrintHeaderRemarks %></th>
                <th class="headerSmall" rowspan="2" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderApproaches %></th>
                <th class="headerSmall" rowspan="2" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderLanding %></th>
                <th class="headerBig" colspan="3" style="width:3cm"><%=Resources.LogbookEntry.PrintHeaderCategory2 %></th>
                <th class="headerBig" colspan="3"><%=Resources.LogbookEntry.PrintHeaderCondition2 %></th>
                <th class="headerSmall" rowspan="2" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderSim %></th>
                <th class="headerBig" colspan="5"><%=Resources.LogbookEntry.PrintHeaderPilotFunction2 %></th>
                <th class="headerSmall" rowspan="2" style="width:1cm"><%=Resources.LogbookEntry.PrintHeaderTotalTime %></th>
            </tr>
            <tr class="bordered">
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderFrom %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderTo %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderSEL %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderMEL %></th>
                <th class="headerSmall" style="width:1cm">&nbsp;</th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderNight %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderIMC %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderSimInstrument %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderCrossCountry %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.FieldCFI %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.FieldDual %></th>
                <th class="headerSmall" style="width:1cm"><% =Resources.LogbookEntry.FieldSIC %></th>
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
                        <div style="clear:left; white-space:pre-line;" dir="auto"><%#: Eval("Comment") %></div>
                        <div><%#: Eval("CustPropertyDisplay") %></div>
                        <div><uc1:mfbSignature runat="server" ID="mfbSignature" EnableViewState="false" /></div>
                    </td>
                    <td><%# Eval("Approaches") %></td>
                    <td><%# Eval("Landings") %></td>
                    <td><%#: (((int)Eval("EffectiveCatClass")) == (int) CategoryClass.CatClassID.ASEL) ? Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM) : string.Empty %></td>
                    <td><%#: (((int)Eval("EffectiveCatClass")) == (int) CategoryClass.CatClassID.AMEL) ? Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM) : string.Empty %></td>
                    <td><%#: (((int)Eval("EffectiveCatClass")) != (int) CategoryClass.CatClassID.ASEL && ((int)Eval("EffectiveCatClass")) != (int) CategoryClass.CatClassID.AMEL) ? Eval("CatClassDisplay") + ": " + Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM) : string.Empty %></td>
                    <td><%# Eval("Nighttime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("IMC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("SimulatedIFR").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("GroundSim").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("CrossCountry").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("CFI").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("Dual").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("SIC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
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
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Landings.ToString(System.Globalization.CultureInfo.CurrentCulture) %></td>
                            <td colspan="3"><%# ((LogbookEntryDisplay) Container.DataItem).TotalFlightTime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Nighttime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).IMC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).SimulatedIFR.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).GroundSim.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).CrossCountry.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).CFI.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Dual.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).SIC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
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
