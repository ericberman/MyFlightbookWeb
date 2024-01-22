<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbEndorsementList.ascx.cs" Inherits="MyFlightbook.Instruction.mfbEndorsementList" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="asp" %>
<%@ Register src="mfbEndorsement.ascx" tagname="mfbEndorsement" tagprefix="uc1" %>
<asp:HiddenField ID="hdnCurSort" runat="server" Value="Date" />
<asp:HiddenField ID="hdnCurSortDir" runat="server" Value="Descending" />
<asp:LinkButton ID="lnkDownload" runat="server" style="vertical-align:middle" OnClick="lnkDownload_Click">
    <asp:Image ID="imgDownloadCSV" ImageUrl="~/images/download.png" runat="server" style="padding-right: 5px; vertical-align:middle" />
    <asp:Image ID="imgCSVIcon" ImageAlign="Middle" runat="server" ImageUrl="~/images/csvicon_sm.png" style="padding-right: 5px; vertical-align:middle;" />
    <asp:Label ID="lblDownload" runat="server" Text="<%$ Resources:Signoff, DownloadCSVEndorsements %>" />
</asp:LinkButton>
<asp:GridView ID="gvExistingEndorsements" OnRowDataBound="gvExistingEndorsements_RowDataBound" GridLines="None" runat="server" AutoGenerateColumns="false" ShowFooter="false" ShowHeader="true" OnRowCommand="gvExistingEndorsements_RowCommand">
    <Columns>
        <asp:TemplateField>
            <HeaderTemplate>
                <asp:Label ID="lblSort" runat="server" Font-Bold="false" Text="<%$ Resources:SignOff, EndorsementSort %>" />&nbsp;&nbsp;&nbsp;<asp:LinkButton ID="lnkSortDate" CausesValidation="false" runat="server" Text="<%$ Resources:SignOff, EndorsementSortDate %>" OnClick="lnkSortDate_Click" />&nbsp;&nbsp;&nbsp;<asp:LinkButton ID="lnkSortTitle" runat="server" CausesValidation="false" Text="<%$ Resources:SignOff, EndorsementSortTitle %>" OnClick="lnkSortTitle_Click" />
            </HeaderTemplate>
            <ItemTemplate>
                <uc1:mfbEndorsement ID="mfbEndorsement1" runat="server" />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField >
            <ItemTemplate>
                <div>
                <asp:LinkButton CausesValidation="false" ID="lnkDeleteExternal" CommandName="_DeleteExternal" CommandArgument='<%# Eval("ID") %>' runat="server" Visible='<%# (bool) Eval("IsExternalEndorsement") %>'>
                    <asp:Image ID="imgDelete" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:SignOff, ExternalEndorsementDeleteTooltip %>" ToolTip="<%$ Resources:SignOff, ExternalEndorsementDeleteTooltip %>" runat="server" /></asp:LinkButton>
                <asp:ConfirmButtonExtender ID="ConfirmButtonExtender1" runat="server" TargetControlID="lnkDeleteExternal" ConfirmOnFormSubmit="True" ConfirmText="<%$ Resources:SignOff, ExternalEndorsementConfirmDelete %>">
                </asp:ConfirmButtonExtender>
                <asp:LinkButton CausesValidation="false" ID="lnkDeleteOwned" CommandName="_DeleteOwned" CommandArgument='<%# Eval("ID") %>' runat="server" Visible='<%# CanDelete((MyFlightbook.Instruction.Endorsement) Container.DataItem) %>'>
                    <asp:Image ID="Image1" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:SignOff, OwnedEndorsementDeleteTooltip %>" ToolTip="<%$ Resources:SignOff, OwnedEndorsementDeleteTooltip %>" runat="server" /></asp:LinkButton>
                <asp:ConfirmButtonExtender ID="ConfirmButtonExtender2" runat="server" TargetControlID="lnkDeleteOwned" ConfirmOnFormSubmit="True" ConfirmText="<%$ Resources:SignOff, OwnedEndorsementConfirmDelete %>">
                </asp:ConfirmButtonExtender>
                </div>
                <div>
                    <asp:LinkButton CausesValidation="false" ID="lnkCopy" CommandName="_Copy" CommandArgument='<%# Eval("ID") %>' runat="server" Visible='<%# CanCopy((MyFlightbook.Instruction.Endorsement) Container.DataItem) %>'>
                        <asp:Image ID="img2" runat="server" ImageUrl="~/images/copyflight.png"  
                    AlternateText="<%$ Resources:Signoff, CopyEndorsement %>" ToolTip="<%$ Resources:Signoff, CopyEndorsement %>" /></asp:LinkButton>
                </div>
            </ItemTemplate>
            <ItemStyle CssClass="noprint" />
        </asp:TemplateField>
    </Columns>
    <EmptyDataTemplate>
        <asp:Label ID="lblNoEndorsements" runat="server" Text="<%$ Resources:Signoff, NoExistingEndorsements %>"></asp:Label>
    </EmptyDataTemplate>
</asp:GridView>
<asp:Label ID="lblErr" runat="server" Text="" EnableViewState="false" CssClass="error"></asp:Label>
