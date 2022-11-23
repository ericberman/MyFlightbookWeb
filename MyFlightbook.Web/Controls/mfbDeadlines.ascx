<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbDeadlines.ascx.cs" Inherits="Controls_mfbDeadlines" %>
<%@ Register Src="~/Controls/mfbDecimalEdit.ascx" TagPrefix="uc1" TagName="mfbDecimalEdit" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc1" TagName="mfbTypeInDate" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<asp:UpdatePanel runat="server" ID="updDeadlines">
    <ContentTemplate>
        <asp:GridView ID="gvDeadlines" runat="server" GridLines="None"
            AutoGenerateColumns="False" ShowHeader="False"
            OnRowCommand="gvDeadlines_RowCommand" BorderStyle="None" BorderWidth="0px" CellPadding="3"
            OnRowEditing="gvDeadlines_RowEditing" OnRowDataBound="gvDeadlines_RowDataBound"
            OnRowCancelingEdit="gvDeadlines_RowCancelingEdit"
            OnRowUpdating="gvDeadlines_RowUpdating">
            <Columns>
                <asp:TemplateField>
                    <ItemTemplate>
                        <asp:ImageButton ID="imgDelete" runat="server"
                            AlternateText="<%$ Resources:Currency, deadlineDeleteTooltip %>" CommandArgument='<%# Bind("ID") %>'
                            CommandName="_Delete" ImageUrl="~/images/x.gif"
                            ToolTip="<%$ Resources:Currency, deadlineDeleteTooltip %>" />
                        <cc1:ConfirmButtonExtender ID="confirmDeleteDeadline" runat="server"
                            ConfirmOnFormSubmit="True"
                            ConfirmText="<%$ Resources:Currency, deadlineDeleteConfirmation %>"
                            TargetControlID="imgDelete"></cc1:ConfirmButtonExtender>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField>
                    <EditItemTemplate>
                        <div>
                            <asp:Label ID="lblName" runat="server" Text='<%#: Bind("DisplayName") %>' Font-Bold="True" />
                            <asp:Label ID="lblDue" runat="server" Text='<%#: Bind("ExpirationDisplay") %>' />
                        </div>
                        <div>
                            <asp:Label ID="lblEnterRegenDate" runat="server" Text='<%#: Bind("RegenPrompt") %>' />
                            <asp:MultiView ID="mvRegenTarget" runat="server" ActiveViewIndex='<%# ((bool) Eval("UsesHours")) ? 1 : 0 %>'>
                                <asp:View ID="vwRegenDate" runat="server">
                                    <uc1:mfbTypeInDate ID="mfbUpdateDeadlineDate" DefaultType="None" runat="server" />
                                </asp:View>
                                <asp:View ID="vwRegenHours" runat="server">
                                    <uc1:mfbDecimalEdit ID="decNewHours" runat="server" EditingMode="Decimal" Width="40" />
                                </asp:View>
                            </asp:MultiView>
                        </div>
                    </EditItemTemplate>
                    <ItemTemplate>
                        <div>
                            <asp:Label ID="lblName" runat="server" Font-Bold="True" Text='<%#: Bind("DisplayName") %>' />
                            <asp:Label ID="lblDue" runat="server" Text='<%#: Bind("ExpirationDisplay") %>' />
                        </div>
                        <div class="fineprint">
                            <asp:Label ID="lblRegenRule" runat="server" Text='<%#: Bind("RegenDescription") %>' />
                        </div>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:CommandField ShowEditButton="True" EditImageUrl="~/images/pencilsm.png">
                    <ItemStyle VerticalAlign="Top" />
                </asp:CommandField>
            </Columns>
            <EmptyDataTemplate>
                <ul>
                    <li><asp:Label ID="lblNoDeadlines" runat="server" Font-Italic="True" Text="<%$ Resources:Currency, deadlinesNoDeadlinesDefined %>" /></li>
                </ul>
            </EmptyDataTemplate>
        </asp:GridView>
        <div><asp:Label ID="lblAddDeadlines" runat="server" Font-Bold="True" /></div>
        <asp:Panel ID="pnlAddDeadlines" runat="server" DefaultButton="btnAddDeadline" style="overflow:hidden">
            <table style="border-spacing: 5px;">
                <tr style="vertical-align:middle">
                    <td>
                        <asp:Label ID="lblDeadlineName" runat="server" Text="<%$ Resources:Currency, DeadlineName %>" />
                    </td>
                    <td>
                        <asp:TextBox ID="txtDeadlineName" runat="server" />
                    </td>
                </tr>
                <tr style="vertical-align:top">
                    <td runat="server">
                        <asp:Panel ID="pnlAircraftLabel" runat="server">
                            <asp:Label ID="lblDeadlineAircraft" runat="server" Text="<%$ Resources:Currency, DeadlineAssociatedAircraft %>" />
                            <div class="fineprint">
                                <asp:Label ID="lblDeadlineAircraftOptional" runat="server" Text="<%$ Resources:Currency, DeadlineAssociatedAircraftOptional %>" />
                            </div>
                        </asp:Panel>
                    </td>
                    <td>
                        <asp:DropDownList ID="cmbDeadlineAircraft" runat="server" AppendDataBoundItems="True" AutoPostBack="True" DataTextField="TailNumber" DataValueField="AircraftID" OnSelectedIndexChanged="cmbDeadlineAircraft_SelectedIndexChanged">
                            <asp:ListItem Selected="True" Text="(None)" Value=""></asp:ListItem>
                        </asp:DropDownList>
                        <asp:CheckBox ID="ckDeadlineUseHours" runat="server" AutoPostBack="True" OnCheckedChanged="ckDeadlineUseHours_CheckedChanged" Text="Deadline is determined using aircraft hours, not a date" Visible="False" />
                    </td>
                </tr>
                <tr style="vertical-align:middle">
                    <td>
                        <asp:Label ID="lblInitialDueDate" runat="server" Text="<%$ Resources:Currency, DeadlineDueDate %>" />
                    </td>
                    <td>
                        <asp:MultiView ID="mvDeadlineDue" runat="server" ActiveViewIndex="0">
                            <asp:View ID="vwDeadlineDueDate" runat="server">
                                <uc1:mfbTypeInDate ID="mfbDeadlineDate" runat="server" DefaultType="None" />
                            </asp:View>
                            <asp:View ID="vwDeadlineDueHours" runat="server">
                                <uc1:mfbDecimalEdit ID="decDueHours" runat="server" EditingMode="Decimal" Width="40" />
                            </asp:View>
                        </asp:MultiView>
                    </td>
                </tr>
                <tr style="vertical-align:top">
                    <td>
                        <asp:Label ID="lblRegen" runat="server" Text="<%$ Resources:Currency, DeadlineRegen %>" />
                    </td>
                    <td>
                        <asp:Label ID="lblRegenExplain" runat="server" Text="<%$ Resources:Currency, DeadlineRegenAction %>" />
                        <br />
                        <table>
                            <tr style="vertical-align:middle">
                                <td style="vertical-align:top">
                                    <asp:RadioButton ID="rbRegenManual" runat="server" Checked="True" GroupName="regenType" />
                                </td>
                                <td style="vertical-align:top">
                                    <asp:Label ID="lblDeadlineManual" runat="server" AssociatedControlID="rbRegenManual" Text="<%$ Resources:Currency, DeadlineExtendManually %>" />
                                </td>
                            </tr>
                            <tr style="vertical-align:middle">
                                <td style="vertical-align:top">
                                    <asp:RadioButton ID="rbRegenInterval" runat="server" GroupName="regenType" />
                                </td>
                                <td style="vertical-align:top">
                                    <asp:Label ID="lblDeadlineDays" runat="server" AssociatedControlID="rbRegenInterval" Text="<%$ Resources:Currency, DeadlineExtendDeadline %>" />
                                    <uc1:mfbDecimalEdit ID="decRegenInterval" runat="server" EditingMode="Integer" Width="40" />
                                    <asp:MultiView ID="mvRegenInterval" runat="server" ActiveViewIndex="0">
                                        <asp:View ID="vwDeadlineCalendarRange" runat="server">
                                            <asp:DropDownList ID="cmbRegenRange" runat="server">
                                                <asp:ListItem Selected="True" Text="<%$ Resources:Currency, DeadlineDays %>" Value="Days" />
                                                <asp:ListItem Text="<%$ Resources:Currency, DeadlineCalendarMonths %>" Value="CalendarMonths" />
                                            </asp:DropDownList>
                                        </asp:View>
                                        <asp:View ID="vwDeadlineHours" runat="server">
                                            <asp:Label ID="lblHours" runat="server" Text="<%$ Resources:Currency, DeadlineHours %>" />
                                            <asp:Label ID="lblHoursTip" runat="server" CssClass="fineprint" Text="<%$ Resources:Currency, DeadlineHoursTip %>" />
                                        </asp:View>
                                    </asp:MultiView>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
                <tr style="vertical-align:middle">
                    <td>&nbsp;</td>
                    <td>
                        <asp:Button ID="btnAddDeadline" runat="server" OnClick="btnAddDeadline_Click" Text="<%$ Resources:Currency, DeadlineAddDeadline %>" ValidationGroup="vgAddDeadlines" />
                        <asp:Label ID="lblErrDeadline" runat="server" CssClass="error" EnableViewState="False" />
                    </td>
                </tr>
            </table>
        </asp:Panel>
        <cc1:CollapsiblePanelExtender ID="cpeDeadlines" runat="server" BehaviorID="cpeDeadlines" CollapseControlID="lblAddDeadlines" Collapsed="True" CollapsedSize="0" CollapsedText="(Click to create a new deadline)" ExpandControlID="lblAddDeadlines" ExpandedText="(Click to hide)" TargetControlID="pnlAddDeadlines" TextLabelID="lblAddDeadlines" />
    </ContentTemplate>
    <Triggers>
        <asp:AsyncPostBackTrigger ControlID="btnAddDeadline" />
        <asp:AsyncPostBackTrigger ControlID="cmbDeadlineAircraft" />
        <asp:AsyncPostBackTrigger ControlID="ckDeadlineUseHours" />
    </Triggers>
</asp:UpdatePanel>
<asp:HiddenField ID="hdnUser" runat="server" />
<asp:HiddenField ID="hdnAircraft" runat="server" />
<asp:HiddenField ID="hdnCreateShared" runat="server" />
