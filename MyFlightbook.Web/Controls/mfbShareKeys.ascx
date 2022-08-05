<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="mfbShareKeys.ascx.cs" Inherits="MyFlightbook.Web.Sharing.mfbShareKeys" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<div>
    <asp:TextBox ID="txtShareLinkName" runat="server" placeholder="<%$ Resources:LocalizedText, ShareKeyNamePrompt %>" />
    <asp:CheckBox ID="ckShareLinkFlights" runat="server" Checked="true" Text="<%$ Resources:LocalizedText, ShareKeyPermissionViewFlights %>" />
    <asp:CheckBox ID="ckShareLinkTotals" runat="server" Checked="true" Text="<%$ Resources:LocalizedText, ShareKeyPermissionViewTotals %>" />
    <asp:CheckBox ID="ckShareLinkCurrency" runat="server" Checked="true" Text="<%$ Resources:LocalizedText, ShareKeyPermissionViewCurrency %>" />
    <asp:CheckBox ID="ckShareLinkAchievements" runat="server" Checked="true" Text="<%$ Resources:LocalizedText, ShareKeyPermissionViewAchievements %>" />
    <asp:CheckBox ID="ckShareLinkAirports" runat="server" Checked="true" Text="<%$ Resources:LocalizedText, ShareKeyPermissionViewAirports %>" />
    <asp:Button ID="btnCreateShareLink" runat="server" Text="Create Link" OnClick="btnCreateShareLink_Click" />
</div>
<div>
    <asp:Label ID="lblShareLinkNameHintBody" runat="server" CssClass="fineprint" Text="Suggestion: use the name of the recipient"></asp:Label>
</div>
<div>
    <asp:Label ID="lblShareKeyErr" runat="server" CssClass="error" EnableViewState="false"></asp:Label>
</div>
    <asp:GridView ID="gvShareKeys" runat="server" AutoGenerateColumns="false"  GridLines="None" CellPadding="3"
        OnRowDataBound="gvShareKeys_RowDataBound" OnRowEditing="gvShareKeys_RowEditing" OnRowCancelingEdit="gvShareKeys_RowCancelingEdit"
        OnRowUpdating="gvShareKeys_RowUpdating" OnRowCommand="gvShareKeys_RowCommand" DataKeyNames="ID">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:ImageButton ID="imgDelete" runat="server" 
                        AlternateText="<%$ Resources:Currency, CustomCurrencyDeleteTooltip %>" CommandArgument='<%# Bind("ID") %>' 
                        CommandName="_Delete" ImageUrl="~/images/x.gif" 
                        ToolTip="<%$ Resources:Currency, CustomCurrencyDeleteTooltip %>" />
                    <cc1:ConfirmButtonExtender ID="cbeDelete" runat="server" 
                        ConfirmOnFormSubmit="True" 
                        ConfirmText="<%$ Resources:LocalizedText, ShareKeyDeleteConfirm %>" 
                        TargetControlID="imgDelete">
                    </cc1:ConfirmButtonExtender>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField ItemStyle-Font-Bold="true" DataField="Name" />
            <asp:CheckBoxField DataField="CanViewFlights" ItemStyle-HorizontalAlign="Center" HeaderText="<%$ Resources:LocalizedText, ShareKeyPermissionViewFlights %>" />
            <asp:CheckBoxField DataField="CanViewTotals" ItemStyle-HorizontalAlign="Center" HeaderText="<%$ Resources:LocalizedText, ShareKeyPermissionViewTotals %>" />
            <asp:CheckBoxField DataField="CanViewCurrency" ItemStyle-HorizontalAlign="Center" HeaderText="<%$ Resources:LocalizedText, ShareKeyPermissionViewCurrency %>" />
            <asp:CheckBoxField DataField="CanViewAchievements" ItemStyle-HorizontalAlign="Center" HeaderText="<%$ Resources:LocalizedText, ShareKeyPermissionViewAchievements %>" />
            <asp:CheckBoxField DataField="CanViewVisitedAirports" ItemStyle-HorizontalAlign="Center" HeaderText="<%$ Resources:LocalizedText, ShareKeyPermissionViewAirports %>" />
            <asp:BoundField DataField="LastAccessDisplay" HeaderText="<%$ Resources:LocalizedText, ShareKeyLastAccessHeader %>" ReadOnly="true" />
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:TextBox ID="txtShareLink" runat="server" ReadOnly="true" Width="200px" Text='<%# Eval("ShareLink") %>'></asp:TextBox>
                    <asp:ImageButton ID="imgCopyLink" style="vertical-align:text-bottom" ImageUrl="~/images/copyflight.png" AlternateText="<%$ Resources:LocalizedText, CopyToClipboard %>" ToolTip="<%$ Resources:LocalizedText, CopyToClipboard %>" runat="server" />
                </ItemTemplate>
            </asp:TemplateField>
            <asp:CommandField ButtonType="Link" ShowEditButton="True" EditImageUrl="~/images/pencilsm.png">
                <ItemStyle VerticalAlign="Top" />
            </asp:CommandField>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:Label ID="lblLinkCopied" runat="server" Text="<%$ Resources:LocalizedText, CopiedToClipboard %>" CssClass="hintPopup" style="display:none; font-weight:bold; font-size: 10pt; color:black; "></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>
            <ul><li><asp:Localize ID="locNoShareKeys" runat="server" Text="<%$ Resources:LocalizedText, ShareKeyNoKeysFound %>"></asp:Localize></li></ul>
        </EmptyDataTemplate>
    </asp:GridView>