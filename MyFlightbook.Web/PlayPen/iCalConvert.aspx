<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Inherits="Public_iCalConvert" Codebehind="iCalConvert.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
    <asp:Panel ID="Panel1" runat="server" DefaultButton="btnUpload">
        <p>Gimme a CSV file:</p>
        <asp:FileUpload ID="FileUpload1" runat="server" />
        <br />
        <br />
        Provide a title for the calendar:<br />
        <asp:TextBox ID="txtTitle" runat="server"></asp:TextBox>
        <asp:RequiredFieldValidator ID="RequiredFieldValidator1" ControlToValidate="txtTitle" runat="server" CssClass="error" Display="Dynamic" ErrorMessage="Provide a title"></asp:RequiredFieldValidator>
        <div><asp:Button ID="btnUpload" runat="server" Text="Upload!" OnClick="btnUpload_Click" /></div>
        <div><asp:Label ID="lblErr" CssClass="error" runat="server" Text="" EnableViewState="false"></asp:Label></div>
    </asp:Panel>
</asp:Content>