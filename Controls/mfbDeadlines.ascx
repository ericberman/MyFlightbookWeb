<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbDeadlines.ascx.cs" Inherits="Controls_mfbDeadlines" %>
<%@ Register Src="~/Controls/mfbDecimalEdit.ascx" TagPrefix="uc1" TagName="mfbDecimalEdit" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc1" TagName="mfbTypeInDate" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
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
                    <asp:Label ID="lblName" runat="server" Text='<%# Bind("DisplayName") %>'
                        Font-Bold="True"></asp:Label>
                    <asp:Label ID="lblDue" runat="server" Text='<%# Bind("ExpirationDisplay") %>'></asp:Label>
                </div>
                <div>
                    <asp:Label ID="lblEnterRegenDate" runat="server"
                        Text='<%# Bind("RegenPrompt") %>'></asp:Label>
                    <asp:MultiView ID="mvRegenTarget" runat="server" ActiveViewIndex='<%# ((bool) Eval("UsesHours")) ? 1 : 0  %>'>
                        <asp:View ID="vwRegenDate" runat="server">
                            <uc1:mfbTypeInDate ID="mfbUpdateDeadlineDate" runat="server" />
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
                        Text='<%# Bind("DisplayName") %>'></asp:Label>
                    <asp:Label ID="lblDue" runat="server"
                        Text='<%# Bind("ExpirationDisplay") %>'></asp:Label>
                </div>
                <div class="fineprint">
                    <asp:Label ID="lblRegenRule" runat="server"
                        Text='<%# Bind("RegenDescription") %>'></asp:Label>
                </div>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:CommandField ButtonType="Link" ShowEditButton="true" EditImageUrl="~/images/pencilsm.png" ItemStyle-VerticalAlign="Top" />
    </Columns>
    <EmptyDataTemplate>
        <ul>
            <li>
                <asp:Label ID="lblNoDeadlines" runat="server" Font-Italic="True"
                    Text="<%$ Resources:Currency, deadlinesNoDeadlinesDefined %>"></asp:Label>
            </li>
        </ul>
    </EmptyDataTemplate>
</asp:GridView>
<asp:HiddenField ID="hdnUser" runat="server" />
<asp:HiddenField ID="hdnAircraft" runat="server" />
