<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbDeadlines" Codebehind="mfbDeadlines.ascx.cs" %>
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
            OnRowUpdating="gvDeadlines_RowUpdating" meta:resourcekey="gvDeadlinesResource1">
            <Columns>
                <asp:TemplateField meta:resourcekey="TemplateFieldResource1">
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
                <asp:TemplateField meta:resourcekey="TemplateFieldResource2">
                    <EditItemTemplate>
                        <div>
                            <asp:Label ID="lblName" runat="server" Text='<%# Bind("DisplayName") %>'
                                Font-Bold="True" meta:resourcekey="lblNameResource1"></asp:Label>
                            <asp:Label ID="lblDue" runat="server" Text='<%# Bind("ExpirationDisplay") %>' meta:resourcekey="lblDueResource1"></asp:Label>
                        </div>
                        <div>
                            <asp:Label ID="lblEnterRegenDate" runat="server"
                                Text='<%# Bind("RegenPrompt") %>' meta:resourcekey="lblEnterRegenDateResource1"></asp:Label>
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
                            <asp:Label ID="lblName" runat="server" Font-Bold="True"
                                Text='<%# Bind("DisplayName") %>' meta:resourcekey="lblNameResource2"></asp:Label>
                            <asp:Label ID="lblDue" runat="server"
                                Text='<%# Bind("ExpirationDisplay") %>' meta:resourcekey="lblDueResource2"></asp:Label>
                        </div>
                        <div class="fineprint">
                            <asp:Label ID="lblRegenRule" runat="server"
                                Text='<%# Bind("RegenDescription") %>' meta:resourcekey="lblRegenRuleResource1"></asp:Label>
                        </div>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:CommandField ShowEditButton="True" EditImageUrl="~/images/pencilsm.png" meta:resourcekey="CommandFieldResource1" >
                <ItemStyle VerticalAlign="Top" />
                </asp:CommandField>
            </Columns>
            <EmptyDataTemplate>
                <ul>
                    <li>
                        <asp:Label ID="lblNoDeadlines" runat="server" Font-Italic="True"
                            Text="<%$ Resources:Currency, deadlinesNoDeadlinesDefined %>" meta:resourcekey="lblNoDeadlinesResource1"></asp:Label>
                    </li>
                </ul>
            </EmptyDataTemplate>
        </asp:GridView>
        <div><asp:Label ID="lblAddDeadlines" runat="server" Font-Bold="True" meta:resourcekey="lblAddDeadlinesResource1"></asp:Label></div>
        <asp:Panel ID="pnlAddDeadlines" runat="server" DefaultButton="btnAddDeadline" style="overflow:hidden" meta:resourcekey="pnlAddDeadlinesResource1">
            <table style="border-spacing: 5px;">
                <tr style="vertical-align:middle">
                    <td>
                        <asp:Label ID="lblDeadlineName" runat="server" Text="Name" meta:resourcekey="lblDeadlineNameResource1"></asp:Label>
                    </td>
                    <td>
                        <asp:TextBox ID="txtDeadlineName" runat="server" meta:resourcekey="txtDeadlineNameResource1"></asp:TextBox>
                    </td>
                </tr>
                <tr style="vertical-align:top">
                    <td runat="server">
                        <asp:Panel ID="pnlAircraftLabel" runat="server">
                            <asp:Label ID="lblDeadlineAircraft" runat="server" Text="Associated Aircraft:"></asp:Label>
                            <div class="fineprint">
                                <asp:Label ID="lblDeadlineAircraftOptional" runat="server" Text="(Optional)"></asp:Label>
                            </div>
                        </asp:Panel>
                    </td>
                    <td>
                        <asp:DropDownList ID="cmbDeadlineAircraft" runat="server" AppendDataBoundItems="True" AutoPostBack="True" DataTextField="TailNumber" DataValueField="AircraftID" meta:resourcekey="cmbDeadlineAircraftResource1" OnSelectedIndexChanged="cmbDeadlineAircraft_SelectedIndexChanged">
                            <asp:ListItem Selected="True" Text="(None)" Value=""></asp:ListItem>
                        </asp:DropDownList>
                        <asp:CheckBox ID="ckDeadlineUseHours" runat="server" AutoPostBack="True" OnCheckedChanged="ckDeadlineUseHours_CheckedChanged" Text="Deadline is determined using aircraft hours, not a date" Visible="False" />
                    </td>
                </tr>
                <tr style="vertical-align:middle">
                    <td>
                        <asp:Label ID="lblInitialDueDate" runat="server" Text="Deadline is due:" meta:resourcekey="lblInitialDueDateResource1"></asp:Label>
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
                        <asp:Label ID="lblRegen" runat="server" Text="Regeneration:" meta:resourcekey="lblRegenResource1"></asp:Label>
                    </td>
                    <td>
                        <asp:Label ID="lblRegenExplain" runat="server" Text="When the deadline has passed and you've done whatever is required:" meta:resourcekey="lblRegenExplainResource1"></asp:Label>
                        <br />
                        <table>
                            <tr style="vertical-align:middle">
                                <td style="vertical-align:top">
                                    <asp:RadioButton ID="rbRegenManual" runat="server" Checked="True" GroupName="regenType" meta:resourcekey="rbRegenManualResource1" />
                                </td>
                                <td style="vertical-align:top">
                                    <asp:Label ID="lblDeadlineManual" runat="server" AssociatedControlID="rbRegenManual" Text="Manually update or delete the deadline" meta:resourcekey="lblDeadlineManualResource1"></asp:Label>
                                </td>
                            </tr>
                            <tr style="vertical-align:middle">
                                <td style="vertical-align:top">
                                    <asp:RadioButton ID="rbRegenInterval" runat="server" GroupName="regenType" meta:resourcekey="rbRegenIntervalResource1" />
                                </td>
                                <td style="vertical-align:top">
                                    <asp:Label ID="lblDeadlineDays" runat="server" AssociatedControlID="rbRegenInterval" Text="Extend the deadline by:" meta:resourcekey="lblDeadlineDaysResource1"></asp:Label>
                                    <uc1:mfbDecimalEdit ID="decRegenInterval" runat="server" EditingMode="Integer" Width="40" />
                                    <asp:MultiView ID="mvRegenInterval" runat="server" ActiveViewIndex="0">
                                        <asp:View ID="vwDeadlineCalendarRange" runat="server">
                                            <asp:DropDownList ID="cmbRegenRange" runat="server" meta:resourcekey="cmbRegenRangeResource1">
                                                <asp:ListItem Selected="True" Text="Days" Value="Days" meta:resourcekey="ListItemResource1"></asp:ListItem>
                                                <asp:ListItem Text="Calendar Months" Value="CalendarMonths" meta:resourcekey="ListItemResource2"></asp:ListItem>
                                            </asp:DropDownList>
                                        </asp:View>
                                        <asp:View ID="vwDeadlineHours" runat="server">
                                            <asp:Label ID="lblHours" runat="server" Text="Hours" meta:resourcekey="lblHoursResource1"></asp:Label>
                                            <asp:Label ID="lblHoursTip" runat="server" CssClass="fineprint" Text="(Typically tach or hobbs)" meta:resourcekey="lblHoursTipResource1"></asp:Label>
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
                        <asp:Button ID="btnAddDeadline" runat="server" OnClick="btnAddDeadline_Click" Text="Add Deadline" ValidationGroup="vgAddDeadlines" meta:resourcekey="btnAddDeadlineResource1" />
                        <asp:Label ID="lblErrDeadline" runat="server" CssClass="error" EnableViewState="False" meta:resourcekey="lblErrDeadlineResource1"></asp:Label>
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
