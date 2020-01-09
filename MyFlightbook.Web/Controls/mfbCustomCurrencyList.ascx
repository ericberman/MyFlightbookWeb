<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbCustomCurrencyList" Codebehind="mfbCustomCurrencyList.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="~/Controls/mfbCustCurrency.ascx" TagPrefix="uc1" TagName="mfbCustCurrency" %>

<asp:Panel ID="pnlAddCustomCurrency" runat="server" style="overflow:hidden; padding: 3px;">
    <uc1:mfbCustCurrency runat="server" id="mfbCustCurrency" OnCurrencyAdded="mfbCustCurrency_CurrencyAdded" />
</asp:Panel>
<cc1:CollapsiblePanelExtender ID="pnlAddCustomCurrency_CollapsiblePanelExtender" 
    runat="server" CollapseControlID="lblShowcurrency" Collapsed="True" 
    CollapsedSize="0" CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" 
    ExpandControlID="lblShowcurrency" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>" 
    TargetControlID="pnlAddCustomCurrency" TextLabelID="lblShowcurrency" BehaviorID="pnlAddCustomCurrency_CollapsiblePanelExtender"></cc1:CollapsiblePanelExtender>
<asp:GridView ID="gvCustomCurrency" runat="server" AutoGenerateColumns="False" 
    BorderStyle="None" BorderWidth="0px" CellPadding="3" GridLines="None" OnRowDataBound="gvCustomCurrency_RowDataBound" OnRowEditing="gvCustomCurrency_RowEditing" 
    OnRowCommand="gvCustomCurrency_RowCommand" ShowHeader="False" OnRowUpdating="gvCustomCurrency_RowUpdating" OnRowCancelingEdit="gvCustomCurrency_RowCancelingEdit">
    <Columns>
        <asp:TemplateField>
            <ItemTemplate>
                <asp:ImageButton ID="imgDelete" runat="server" 
                    AlternateText="<%$ Resources:Currency, CustomCurrencyDeleteTooltip %>" CommandArgument='<%# Bind("ID") %>' 
                    CommandName="_Delete" ImageUrl="~/images/x.gif" 
                    ToolTip="<%$ Resources:Currency, CustomCurrencyDeleteTooltip %>" />
                <cc1:ConfirmButtonExtender ID="ConfirmButtonExtender1" runat="server" 
                    ConfirmOnFormSubmit="True" 
                    ConfirmText="<%$ Resources:Currency, CustomCurrencyDeleteConfirmation %>" 
                    TargetControlID="imgDelete"></cc1:ConfirmButtonExtender>
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" />
        </asp:TemplateField>
        <asp:HyperLinkField Target="_blank" DataTextField="DisplayName" DataNavigateUrlFields="FlightQueryJSON"
            DataNavigateUrlFormatString="~/member/logbooknew.aspx?fq={0}">
            <ItemStyle Font-Bold="True" VerticalAlign="Top" />
        </asp:HyperLinkField>
        <asp:TemplateField>
            <EditItemTemplate>
                <uc1:mfbCustCurrency ID="mfbEditCustCurrency" runat="server" />
            </EditItemTemplate>
            <ItemTemplate>
                <asp:Label ID="lblDisplay" runat="server" Text='<%# Eval("DisplayString") %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" />
        </asp:TemplateField>
        <asp:CommandField ButtonType="Link" ShowEditButton="True" EditImageUrl="~/images/pencilsm.png">
        <ItemStyle VerticalAlign="Top" />
        </asp:CommandField>
    </Columns>
    <EmptyDataTemplate>
        <ul>
            <li>
                <asp:Label ID="lblNoRules" runat="server" Font-Italic="True" Text="<%$ Resources:Currency, CustomCurrencyNoneDefined %>"></asp:Label>
            </li>
        </ul>
    </EmptyDataTemplate>
</asp:GridView>
