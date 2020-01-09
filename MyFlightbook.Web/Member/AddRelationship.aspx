<%@ Page Title="Add Instructor/Student" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_AddRelationship" Codebehind="AddRelationship.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server"><asp:Localize ID="locConfirmHeader" Text="<%$ Resources:LocalizedText, AddRelationshipHeader %>" runat="server"></asp:Localize></asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:Panel ID="pnlReviewRequest" runat="server">
    <asp:Label ID="lblRequestDesc" runat="server" Text=""></asp:Label>
        <br /><br />
        <asp:Panel ID="pnlConfirm" runat="server">
            <asp:Localize ID="Localize1" Text="<%$ Resources:LocalizedText, AddRelationshipConfirmPrompt %>" runat="server"></asp:Localize>
            <br /><br />
            <asp:Button ID="btnConfirm" runat="server" Text="<%$ Resources:LocalizedText, AddRelationshipConfirmYes %>" 
                onclick="btnConfirm_Click" /> &nbsp;&nbsp;&nbsp;&nbsp;<asp:Button ID="btnCancel"
                runat="server" Text="<%$ Resources:LocalizedText, AddRelationshipConfirmNo %>" onclick="btnCancel_Click" />
        </asp:Panel>
    </asp:Panel>
    <asp:Label ID="lblError" runat="server" CssClass="error" Text="">
</asp:Label>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
</asp:Content>

