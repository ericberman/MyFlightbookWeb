<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="SecurityError" Title="Untitled Page" Codebehind="SecurityError.aspx.cs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="server">
    Content Blocked
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <p>For security reasons, certain characters such as "<" may not be used in certain content.</p>
    <p>Please <a href="javascript:history.go(-1)">go back to the page you were just on</a> and try again.</p>
</asp:Content>

