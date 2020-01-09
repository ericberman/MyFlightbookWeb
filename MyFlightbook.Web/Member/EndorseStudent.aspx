<%@ Page Title="Endorse Student" Language="C#" MasterPageFile="~/MasterPage.master"
    AutoEventWireup="true" Inherits="Member_EndorseStudent" Codebehind="EndorseStudent.aspx.cs" %>

<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbEndorsementList.ascx" tagname="mfbEndorsementList" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbEditEndorsement.ascx" tagname="mfbEditEndorsement" tagprefix="uc3" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblPageHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:Panel ID="pnlEndorsement" runat="server">
        <h2><asp:Label ID="lblNewEndorsementHeader" runat="server" Text=""></asp:Label></h2>
        <p>
            <asp:Label ID="lblNote" Font-Bold="true" runat="server" Text="<%$ Resources:LocalizedText, Note %>"></asp:Label>
            <asp:Localize ID="locEndorsementDisclaimer" runat="server" Text="<%$ Resources:Profile, EndorsementDisclaimer %>"></asp:Localize>
            <asp:HyperLink ID="lnkCFISigs" Text='<%$ Resources:SignOff, CFISigsLinkLabel %>' Target="_blank" runat="server" NavigateUrl="~/Public/CFISigs.aspx" ></asp:HyperLink>
        </p>
        <h2><%=Resources.SignOff.EndorsementPickTemplate %></h2>
        <p>
            <asp:DropDownList style="border: 1px solid black"
                ID="cmbTemplates" runat="server" AutoPostBack="True" 
                onselectedindexchanged="cmbTemplates_SelectedIndexChanged">
            </asp:DropDownList><br />
            <asp:Localize ID="locRequestEndorsement" runat="server" Text="<%$ Resources:Profile, EndorsementRequestPrompt %>"></asp:Localize>
            <asp:HyperLink ID="lnkContactUs" NavigateUrl="~/Public/ContactMe.aspx" runat="server" Text="<%$ Resources:Profile, EndorsementRequest %>"></asp:HyperLink>
        </p>
        <h2><%= Resources.SignOff.EndorsementEditTemplate %></h2>
        <uc3:mfbEditEndorsement ID="mfbEditEndorsement1" runat="server" OnNewEndorsement="OnNewEndorsement" />
    </asp:Panel>
    <asp:Panel ID="pnlError" runat="server" Visible="false">
        <asp:Label ID="lblError" CssClass="error" runat="server" Text=""></asp:Label>
        <br />
        <asp:HyperLink ID="lnkReturnHome" NavigateUrl="~/Member/Training.aspx/instStudents" runat="server" Text="<%$ Resources:Profile, EndorsementReturnToProfile %>"></asp:HyperLink>
    </asp:Panel>
    <br />
    <asp:Panel ID="pnlExistingEndorsements" runat="server">
        <h2><asp:Label ID="lblExistingEndorsementHeader" runat="server" Text=""></asp:Label></h2>
        <uc2:mfbEndorsementList ID="mfbEndorsementList1" runat="server" />
    </asp:Panel>
    <asp:HiddenField ID="hdnLastTemplate" runat="server" />
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
</asp:Content>
