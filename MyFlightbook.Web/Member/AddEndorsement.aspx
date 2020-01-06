<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_AddEndorsement" Codebehind="AddEndorsement.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="~/Controls/mfbEditEndorsement.ascx" TagPrefix="uc1" TagName="mfbEditEndorsement" %>
<%@ Register Src="~/Controls/mfbSearchbox.ascx" TagPrefix="uc1" TagName="mfbSearchbox" %>


<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <script src="https://code.jquery.com/jquery-1.10.1.min.js"></script>
    <script src='<%= ResolveUrl("~/public/Scripts/jquery.json-2.4.min.js") %>'></script>
    <asp:Label ID="lblAddEndorsement" runat="server" Text="<%$ Resources:SignOff, EditEndorsementAddEndorsement %>"></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <h2><% =Resources.SignOff.EditEndorsementAddEndorsement %></h2>
    <asp:MultiView ID="mvAddEndorsement" runat="server">
        <asp:View ID="vwPickInstructor" runat="server">
            <asp:Label ID="Label2" runat="server" Text="<%$ Resources:SignOff, ChooseInstructorsPrompt %>"></asp:Label>
            <asp:DropDownList ID="cmbInstructors" runat="server" AutoPostBack="true" AppendDataBoundItems="true" DataValueField="UserName" DataTextField="UserFullName" 
                onselectedindexchanged="cmbInstructors_SelectedIndexChanged">
                <asp:ListItem Selected="True" Text="<%$ Resources:SignOff, NewInstructor %>" Value=""></asp:ListItem>
            </asp:DropDownList>
            <asp:Button ID="btnNewInstructor" runat="server" 
                Text="<%$ Resources:LocalizedText, NextPrompt %>" onclick="btnNewInstructor_Click" 
                />
        </asp:View>
        <asp:View ID="vwAcceptTerms" runat="server">
            <p><asp:Label ID="lblDisclaimerResponse" runat="server" Text=""></asp:Label></p>
            <p style="font-weight:bold"><asp:Label ID="lblDisclaimerResponse2" runat="server" Text=""></asp:Label></p>
            <p><asp:CheckBox ID="ckAccept" runat="server" 
                Text="<%$ Resources:Signoff, SignAcceptResponsibility %>" /></p>
            <p style="text-align:center"><asp:Button ID="btnAcceptResponsibility" runat="server" 
                Text="<%$ Resources:LocalizedText, NextPrompt %>" 
                onclick="btnAcceptResponsibility_Click" /></p>
        </asp:View>
        <asp:View ID="vwEndorse" runat="server">
            <h3><%=Resources.SignOff.EndorsementPickTemplate %></h3>
            <div style="display:inline-block; vertical-align:middle;">
                <uc1:mfbSearchbox runat="server" ID="mfbSearchTemplates" Hint="<%$ Resources:SignOff, EndorsementsSearchPrompt %>" OnSearchClicked="mfbSearchbox_SearchClicked" />
            </div>
            <div style="display:inline-block; vertical-align:middle;">
            <asp:DropDownList style="border: 1px solid black"
                ID="cmbTemplates" runat="server" AutoPostBack="True" 
                onselectedindexchanged="cmbTemplates_SelectedIndexChanged">
            </asp:DropDownList>
            </div>
            <p>
                <asp:Localize ID="locRequestEndorsement" runat="server" Text="<%$ Resources:Profile, EndorsementRequestPrompt %>"></asp:Localize>
                <asp:HyperLink ID="lnkContactUs" NavigateUrl="~/Public/ContactMe.aspx" runat="server" Text="<%$ Resources:Profile, EndorsementRequest %>"></asp:HyperLink>
                <asp:HiddenField ID="hdnLastTemplate" runat="server" />
            </p>
            <h3><%= Resources.SignOff.EndorsementEditTemplate %></h3>
            <uc1:mfbEditEndorsement runat="server" ID="mfbEditEndorsement1" OnNewEndorsement="mfbEditEndorsement1_NewEndorsement" />
        </asp:View>
    </asp:MultiView>
    <asp:Label ID="lblErr" CssClass="error" runat="server" Text="" EnableViewState="false"></asp:Label>
</asp:Content>