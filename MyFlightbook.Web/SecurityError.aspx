<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="SecurityError.aspx.cs" Inherits="SecurityError" Title="Untitled Page" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
<h1>Content Blocked</h1>
<p>For security reasons, certain characters such as "<" may not be used in certain content.</p>
    <p>Please <a href="javascript:history.go(-1)">go back to the page you were just on</a> and try again.</p>
</asp:Content>

