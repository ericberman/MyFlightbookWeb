<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="ReviewPendingFlights.aspx.cs" Inherits="Member_ReviewPendingFlights" Culture="auto" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/mfbLogbook.ascx" TagPrefix="uc1" TagName="mfbLogbook" %>
<%@ Register Src="~/Controls/mfbEditFlight.ascx" TagPrefix="uc1" TagName="mfbEditFlight" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Localize ID="locHeader" runat="server" Text="<%$ Resources: LogbookEntry,ReviewPendingFlightsHeader %>"></asp:Localize>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:MultiView ID="mvPendingFlights" runat="server" ActiveViewIndex="0">
        <asp:View ID="vwList" runat="server">
            <p>
                <asp:Label ID="lblReviewPending" runat="server" Text="<%$ Resources: LogbookEntry,ReviewPendingFlightsPrompt %>"></asp:Label></p>
            <asp:GridView ID="gvPendingFlights" runat="server" AutoGenerateColumns="false" CellPadding="3" DataKeyNames="PendingID" AllowSorting="True"
                ShowHeader="true" ShowFooter="true" UseAccessibleHeader="true" GridLines="None" CssClass="lbTable"
                AllowPaging="false" OnSorting="gvPendingFlights_Sorting" OnRowCommand="gvPendingFlights_RowCommand">
                <EmptyDataTemplate>
                    <ul><li><%=Resources.LogbookEntry.ReviewPendingFlightsNoFlights %></li></ul>
                </EmptyDataTemplate>
                <HeaderStyle CssClass="gvhDefault" />
                <Columns>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldFlight %>" SortExpression="Date">
                        <HeaderStyle CssClass="headerBase headerSortDesc gvhLeft" />
                        <ItemTemplate>
                            <div>
                                <asp:LinkButton ID="lnkEditFlight" Font-Bold="true" runat="server" Text='<%# ((DateTime) Eval("Date")).ToShortDateString() %>' Font-Size="Larger" CommandName="_Edit" CommandArgument='<%# Eval("PendingID") %>'></asp:LinkButton>
                                <asp:Label Font-Bold="true" ID="lnkRoute" runat="server" Text='<%#: Eval("Route") %>'></asp:Label>
                                <span runat="server" id="divComments" style="clear: left; white-space: pre-line;" dir="auto">
                                    <asp:Label ID="lblComments" runat="server" Text='<%# ApproachDescription.ReplaceApproaches(Eval("Comment").ToString().Linkify()) %>'></asp:Label></span>
                            </div>
                            <asp:Panel ID="pnlProps" runat="server">
                                <asp:Panel ID="pnlFlightTimes" runat="server" Visible="<%# Viewer.DisplayTimesByDefault %>">
                                    <div>
                                        <%# LogbookEntryDisplay.FormatEngineTime((DateTime) Eval("EngineStart"), (DateTime) Eval("EngineEnd"), Viewer.UsesUTCDateOfFlight, Viewer.UsesHHMM) %>
                                    </div>
                                    <div>
                                        <%# LogbookEntryDisplay.FormatFlightTime((DateTime) Eval("FlightStart"), (DateTime) Eval("FlightEnd"), Viewer.UsesUTCDateOfFlight, Viewer.UsesHHMM) %>
                                    </div>
                                    <div>
                                        <%# LogbookEntryDisplay.FormatHobbs((decimal) Eval("HobbsStart"), (decimal) Eval("HobbsEnd")) %>
                                    </div>
                                </asp:Panel>
                                <asp:Repeater ID="rptProps" runat="server" DataSource='<%# CustomFlightProperty.PropDisplayAsList((IEnumerable<CustomFlightProperty>)Eval("CustomProperties"), Viewer.UsesHHMM, true, true) %>'>
                                    <ItemTemplate>
                                        <div><%# Container.DataItem %></div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </asp:Panel>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldTail %>" SortExpression="TailNumDisplay">
                        <ItemStyle CssClass="gvcCentered" />
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemTemplate>
                            <asp:Label ID="lblTail" runat="server" Text='<%# Eval("TailNumDisplay") %>'></asp:Label>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldApproaches %>" SortExpression="Approaches">
                        <ItemStyle CssClass="gvcCentered" />
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemTemplate>
                            <%# Eval("Approaches").FormatInt() %>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldHold %>" SortExpression="fHoldingProcedures">
                        <ItemStyle CssClass="gvcCentered largeBold" />
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemTemplate>
                            <%# Eval("fHoldingProcedures").FormatBoolean() %>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldLanding %>" SortExpression="Landings">
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemStyle CssClass="gvcCentered" />
                        <ItemTemplate>
                            <%# LogbookEntryDisplay.LandingDisplayForFlight((LogbookEntry) Container.DataItem) %>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldXCountry %>" SortExpression="CrossCountry">
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemStyle CssClass="gvcRight" />
                        <ItemTemplate>
                            <%# Eval("CrossCountry").FormatDecimal(Viewer.UsesHHMM)%>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldNight %>" SortExpression="Nighttime">
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemStyle CssClass="gvcRight" />
                        <ItemTemplate>
                            <%# Eval("Nighttime").FormatDecimal(Viewer.UsesHHMM)%>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldSimIMC %>" SortExpression="SimulatedIFR">
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemStyle CssClass="gvcRight" />
                        <ItemTemplate>
                            <%# Eval("SimulatedIFR").FormatDecimal(Viewer.UsesHHMM)%>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldIMC %>" SortExpression="IMC">
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemStyle CssClass="gvcRight" />
                        <ItemTemplate>
                            <%# Eval("IMC").FormatDecimal(Viewer.UsesHHMM)%>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldGroundSim %>" SortExpression="GroundSim">
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemStyle CssClass="gvcRight" />
                        <ItemTemplate>
                            <%# Eval("GroundSim").FormatDecimal(Viewer.UsesHHMM)%>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldDual %>" SortExpression="Dual">
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemStyle CssClass="gvcRight" />
                        <ItemTemplate>
                            <%# Eval("Dual").FormatDecimal(Viewer.UsesHHMM)%>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldCFI %>" SortExpression="CFI">
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemStyle CssClass="gvcRight" />
                        <ItemTemplate>
                            <%# Eval("CFI").FormatDecimal(Viewer.UsesHHMM)%>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldSIC %>" SortExpression="SIC">
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemStyle CssClass="gvcRight" />
                        <ItemTemplate>
                            <%# Eval("SIC").FormatDecimal(Viewer.UsesHHMM)%>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldPIC %>" SortExpression="PIC">
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemStyle CssClass="gvcRight" />
                        <ItemTemplate>
                            <%# Eval("PIC").FormatDecimal(Viewer.UsesHHMM)%>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldTotal %>" SortExpression="TotalFlightTime">
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemStyle CssClass="gvcRight" />
                        <ItemTemplate>
                            <%# Eval("TotalFlightTime").FormatDecimal(Viewer.UsesHHMM)%>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField>
                        <HeaderStyle CssClass="headerBase gvhCentered" />
                        <ItemTemplate>
                            <asp:LinkButton ID="lnkDelete" CommandName="_Delete" CommandArgument='<%# Eval("PendingID") %>' runat="server">
                                <asp:Image ID="imgDelete" Style="padding-right: 10px" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:LogbookEntry, LogbookDeleteTooltip %>" ToolTip="<%$ Resources:LogbookEntry, LogbookDeleteTooltip %>" runat="server" />
                            </asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
                <AlternatingRowStyle CssClass="logbookAlternateRow" />
                <RowStyle CssClass="logbookRow" />
            </asp:GridView>
            <div>
                <asp:LinkButton ID="lnkDeleteAll" Visible="false" runat="server" Text="<%$ Resources:LogbookEntry, ReviewPendingFlightsDelete %>" OnClick="lnkDeleteAll_Click" />
                <ajaxToolkit:ConfirmButtonExtender ID="confirmDeleteAll" runat="server" ConfirmText="<%$ Resources:LogbookEntry, ReviewPendingFlightsDeleteConfirm %>" TargetControlID="lnkDeleteAll" />
            </div>
        </asp:View>
        <asp:View ID="vwEdit" runat="server">
            <uc1:mfbEditFlight runat="server" ID="mfbEditFlight" OnFlightEditCanceled="mfbEditFlight_FlightEditCanceled" OnFlightUpdated="mfbEditFlight_FlightUpdated" CanCancel="true" />
        </asp:View>
    </asp:MultiView>
</asp:Content>
<asp:Content ID="ContentMain" ContentPlaceHolderID="cpMain" runat="Server">
</asp:Content>
