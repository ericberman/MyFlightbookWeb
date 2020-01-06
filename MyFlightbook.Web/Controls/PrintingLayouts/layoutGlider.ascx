<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_PrintingLayouts_layoutGlider" Codebehind="layoutGlider.ascx.cs" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageHeader.ascx" TagPrefix="uc1" TagName="pageHeader" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageFooter.ascx" TagPrefix="uc1" TagName="pageFooter" %>
<%@ Register Src="~/Controls/mfbSignature.ascx" TagPrefix="uc1" TagName="mfbSignature" %>

<asp:Repeater ID="rptPages" runat="server" OnItemDataBound="rptPages_ItemDataBound">
<ItemTemplate>
    <uc1:pageHeader runat="server" ID="pageHeader" UserName="<%# CurrentUser.UserName %>" />
    <table class="pageTable">
        <thead>
            <tr class="bordered">
                <th class="headerSmall" rowspan="2">#</th>
                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderDate %></th>
                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderModel %></th>
                <th class="headerSmall" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderAircraft %></th>
                <th class="headerBig" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderRoute %></th>

                <th class="headerSmall" colspan="2"><% =Resources.LogbookEntry.PrintHeaderGliderAltitude %></th>
                <th class="headerSmall" colspan="3"><% =Resources.LogbookEntry.PrintHeaderLaunchMethod %></th>

                <th class="headerSmall" rowspan="2" style="width:1cm"><% =Resources.LogbookEntry.PrintHeaderGliderLandings %></th>

                <th class="headerBig" rowspan="2"><% =Resources.LogbookEntry.PrintHeaderGroundTrainingReceived %></th>
                <th class="headerSmall" colspan="<% =4 + (CurrentUser.IsInstructor ? 1 : 0) %>"><% =Resources.LogbookEntry.PrintHeaderPilotFunction2 %></th>
                <th class="headerBig" rowspan="2"><%=Resources.LogbookEntry.PrintHeaderRemarks %></th>
            </tr>
            <tr class="bordered">
                <th class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderGliderReleaseAltitude %></th>
                <th class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderGliderMaxAltitude %></th>

                <th class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderLaunchMethodAero %></th>
                <th class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderLaunchMethodGround %></th>
                <th class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderLaunchMethodSelf %></th>

                <th class="headerSmall"><% =Resources.LogbookEntry.FieldDual %></th>
                <th class="headerSmall"><% =Resources.LogbookEntry.PrintHeaderSolo %></th>
                <th class="headerSmall"><% =Resources.LogbookEntry.FieldPIC %></th>
                <th class="headerSmall" runat="server" visible="<%# CurrentUser.IsInstructor %>"><% =Resources.LogbookEntry.FieldCFI %></th>
                <th class="headerSmall"><% =Resources.LogbookEntry.FieldTotal %></th>
            </tr>
        </thead>
        <asp:Repeater EnableViewState="false" ID="rptFlight" runat="server" OnItemDataBound="rptFlight_ItemDataBound">
            <ItemTemplate>
                <tr class="bordered">
                    <td><%# Eval("Index") %></td>
                    <td><%# ((DateTime) Eval("Date")).ToShortDateString() %></td>
                    <td><%#: Eval("ModelDisplay") %></td>
                    <td><%#: Eval("TailNumDisplay") %></td>
                    <td><%#: Eval("Route") %></td>

                    <td><%# ((int) Eval("ReleaseAltitude")) == 0 ? string.Empty : ((int) Eval("ReleaseAltitude")).ToString("#,###") %></td>
                    <td><%# ((int) Eval("MaxAltitude")) == 0 ? string.Empty : ((int) Eval("MaxAltitude")).ToString("#,###") %></td>

                    <td><%# Eval("AeroLaunches").FormatInt() %></td>
                    <td><%# Eval("GroundLaunches").FormatInt() %></td>
                    <td><%# Eval("SelfLaunches").FormatInt() %></td>

                    <td><%# Eval("Landings") %></td>
                    <td><%# Eval("GroundInstruction").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("Dual").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("SoloTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("PIC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td runat="server" visible="<%# CurrentUser.IsInstructor %>"><%# Eval("CFI").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                    <td><%# Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
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
                    <td colspan="2" class="subtotalLabel" rowspan='<%# Eval("SubtotalCount") %>'></td>
                    <td colspan="3" rowspan='<%# Eval("SubtotalCount") %>'><%# Eval("GroupTitle") %></td>
                    <asp:Repeater ID="rptSubtotals" runat="server">
                        <ItemTemplate>
                            <%# (Container.ItemIndex != 0) ? "<tr class=\"subtotal\">" : string.Empty %>
                            <td colspan="2"><%# ((LogbookEntryDisplay) Container.DataItem).CatClassDisplay %></td>  
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).AeroLaunchTotal.FormatInt() %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).GroundLaunchTotal.FormatInt() %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).SelfLaunchTotal.FormatInt() %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).LandingsTotal.FormatInt() %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).GroundInstructionTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).Dual.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).SoloTotal.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).PIC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td runat="server" visible="<%# CurrentUser.IsInstructor %>"><%# ((LogbookEntryDisplay) Container.DataItem).CFI.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                            <td><%# ((LogbookEntryDisplay) Container.DataItem).TotalFlightTime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
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
