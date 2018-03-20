<%@ Control Language="C#" AutoEventWireup="true" CodeFile="layoutNative.ascx.cs" Inherits="Controls_PrintingLayouts_layoutNative" %>
<%@ Register Src="~/Controls/mfbLogbook.ascx" TagPrefix="uc1" TagName="mfbLogbook" %>
<%@ Register Src="~/Controls/mfbImageList.ascx" TagPrefix="uc1" TagName="mfbImageList" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageHeader.ascx" TagPrefix="uc1" TagName="pageHeader" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageFooter.ascx" TagPrefix="uc1" TagName="pageFooter" %>
<%@ Register Src="~/Controls/mfbSignature.ascx" TagPrefix="uc1" TagName="mfbSignature" %>

<asp:Repeater ID="rptPages" runat="server" OnItemDataBound="rptPages_ItemDataBound" EnableViewState="false">
    <ItemTemplate>
        <uc1:pageHeader runat="server" ID="pageHeader" UserName="<%# CurrentUser.UserName %>" />
        <table class="pageTable">
            <thead class="header bordered">
                <th colspan="2"></th>
                <th><%=Resources.LogbookEntry.PrintHeaderAircraft %></th>
                <th><div><%=Resources.LogbookEntry.PrintHeaderApproachesShort %></div></th>
                <th><div><%=Resources.LogbookEntry.FieldHold %></div></th>
                <th><div><%=Resources.LogbookEntry.PrintHeaderLandingsShort %></div></th>
                <th><div><%=Resources.LogbookEntry.FieldXCountry %></div></th>
                <th><div><%=Resources.LogbookEntry.FieldNight %></div></th>
                <th><div><%=Resources.LogbookEntry.FieldSimIMC %></div></th>
                <th><div><%=Resources.LogbookEntry.FieldIMC %></div></th>
                <th><div><%=Resources.LogbookEntry.FieldGroundSim %></div></th>
                <th><div><%=Resources.LogbookEntry.FieldDual %></div></th>
                <th><div><%=Resources.LogbookEntry.FieldCFI %></div></th>
                <th><div><%=Resources.LogbookEntry.FieldSIC %></div></th>
                <th><div><%=Resources.LogbookEntry.FieldPIC %></div></th>
                <th><div><%=Resources.LogbookEntry.FieldTotal %></div></th>
            </thead>
            <asp:Repeater ID="rptFlight" runat="server" OnItemDataBound="rptFlight_ItemDataBound">
                <ItemTemplate>
                    <tr class="bordered">
                        <td colspan="2">
                            <asp:Label Font-Bold="true" EnableViewState="false" ID="lblStaticDate" runat="server" Text='<%# ((DateTime) Eval("Date")).ToShortDateString() %>'></asp:Label>&nbsp;<%#: Eval("Route") %>
                            <div runat="server" id="divComments" style="clear:left; white-space:pre-line;" dir="auto"><%# ((string) Eval("Comment")).Linkify() %></div>
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
                            <asp:Panel EnableViewState="false" ID="pnlProps" runat="server">
                                <%#: Eval("CustPropertyDisplay").ToString() %>
                            </asp:Panel>
                            <uc1:mfbSignature runat="server" ID="mfbSignature" EnableViewState="false" />
                            <asp:Panel EnableViewState="false" ID="pnlFlightImages" runat="server" Visible="<%# IncludeImages %>">
                                <uc1:mfbImageList ID="mfbilFlights" runat="server" Columns="3" CanEdit="false" ImageClass="Flight" IncludeDocs="false" MaxImage="3" />
                            </asp:Panel>
                        </td>
                        <td style="width: 3cm">
                            <div style="font-weight:bold"><%#: Eval("TailNumDisplay") %></div>
                            <div style="font-size:smaller"><%#: Eval("ModelDisplay") %> <%#: Eval("CatClassDisplay") %></div>
                        </td>
                        <td class="numericColumn"><%# Eval("Approaches").FormatInt() %></td>
                        <td class="numericColumn"><%# Eval("fHoldingProcedures").FormatBoolean() %></td>
                        <td class="numericColumn"><%# Eval("LandingDisplay") %></td>
                        <td class="numericColumn"><%# Eval("CrossCountry").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("Nighttime").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("SimulatedIFR").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("IMC").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("GroundSim").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("Dual").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("CFI").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("SIC").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("PIC").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                    </tr>
                </ItemTemplate>
            </asp:Repeater>
            <asp:Repeater ID="rptSubtotalCollections" runat="server" EnableViewState="false" OnItemDataBound="rptSubtotalCollections_ItemDataBound">
                <ItemTemplate>
                    <tr class="subtotal">
                        <td class="noborder" rowspan='<%# Eval("SubtotalCount") %>'></td>
                        <td class="subtotalLabel" rowspan='<%# Eval("SubtotalCount") %>'><%# Eval("GroupTitle") %></td>
                        <asp:Repeater ID="rptSubtotals" runat="server">
                            <ItemTemplate>
                                <%# (Container.ItemIndex != 0) ? "<tr class=\"subtotal\">" : string.Empty %>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).CatClassDisplay %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).Approaches.ToString(System.Globalization.CultureInfo.CurrentCulture) %></td>
                                <td></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).Landings.ToString(System.Globalization.CultureInfo.CurrentCulture) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).CrossCountry.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).Nighttime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).SimulatedIFR.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).IMC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).GroundSim.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).Dual.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).CFI.FormatDecimal(CurrentUser.UsesHHMM) %></td>
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
