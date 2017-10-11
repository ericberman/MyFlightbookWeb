<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbEndorsementList.ascx.cs" Inherits="Controls_mfbEndorsementList" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="asp" %>
<%@ Register src="mfbEndorsement.ascx" tagname="mfbEndorsement" tagprefix="uc1" %>
<asp:Label ID="lblPreviousEndorsements" runat="server" Text="<%$ Resources:SignOff, EditEndorsementDisclaimer %>"></asp:Label>
<asp:GridView ID="gvExistingEndorsements" OnRowDataBound="gvExistingEndorsements_RowDataBound" GridLines="None" runat="server" AutoGenerateColumns="false" ShowFooter="false" ShowHeader="false" OnRowCommand="gvExistingEndorsements_RowCommand">
    <Columns>
        <asp:TemplateField>
            <ItemTemplate>
                <uc1:mfbEndorsement ID="mfbEndorsement1" runat="server" />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField >
            <ItemTemplate>
                <asp:LinkButton CausesValidation="false" ID="lnkDelete" CommandName="_Delete" CommandArgument='<%# Eval("ID") %>' runat="server" Visible='<%# (bool) Eval("IsExternalEndorsement") %>'>
                    <asp:Image ID="imgDelete" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:SignOff, ExternalEndorsementDeleteTooltip %>" ToolTip="<%$ Resources:SignOff, ExternalEndorsementDeleteTooltip %>" runat="server" /></asp:LinkButton>
                <asp:ConfirmButtonExtender ID="ConfirmButtonExtender1" runat="server" TargetControlID="lnkDelete" ConfirmOnFormSubmit="True" ConfirmText="<%$ Resources:SignOff, ExternalEndorsementConfirmDelete %>">
                </asp:ConfirmButtonExtender>
            </ItemTemplate>
            <ItemStyle CssClass="noprint" />
        </asp:TemplateField>
    </Columns>
    <EmptyDataTemplate>
        <asp:Label ID="lblNoEndorsements" runat="server" Text="<%$ Resources:Signoff, NoExistingEndorsements %>"></asp:Label>
    </EmptyDataTemplate>
</asp:GridView>
<asp:LinkButton ID="lnkDownload" runat="server" Text="<%$ Resources:Signoff, DownloadCSVEndorsements %>" OnClick="lnkDownload_Click"></asp:LinkButton>
<asp:GridView ID="gvDownload" runat="server" AutoGenerateColumns="false">
    <Columns>
        <asp:BoundField DataField="Date" HeaderText="<%$ Resources:Signoff, EditEndorsementDatePrompt %>" DataFormatString="{0:d}" />
        <asp:BoundField DataField="FullTitleAndFar" HeaderText="<%$ Resources:Signoff, DownloadEndorsementFARRef %>" />
        <asp:BoundField DataField="EndorsementText" HeaderText="<%$ Resources:Signoff, DownloadEndorsementText %>" />
        <asp:BoundField DataField="CreationDate" HeaderText="<%$ Resources:Signoff, EditEndorsementDateCreatedPrompt %>" DataFormatString="{0:d}" />
        <asp:BoundField DataField="StudentDisplayName" HeaderText="<%$ Resources:Signoff, EditEndorsementStudentPrompt %>" />
        <asp:BoundField DataField="CFIDisplayName" HeaderText="<%$ Resources:Signoff, EditEndorsementInstructorPrompt %>" />
        <asp:BoundField DataField="CFICertificate" HeaderText="<%$ Resources:Signoff, EditEndorsementCFIPrompt %>" />
        <asp:BoundField DataField="CFIExpirationDate" HeaderText="<%$ Resources:Signoff, EditEndorsementExpirationPrompt %>" DataFormatString="{0:d}" />
    </Columns>
</asp:GridView>
<asp:Label ID="lblErr" runat="server" Text="" EnableViewState="false" CssClass="error"></asp:Label>
