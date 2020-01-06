<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_ImportTable" Codebehind="ImportTable.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblImportHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:Literal ID="litTableMainFields" runat="server"></asp:Literal>
    <asp:Table ID="tblAdditionalProps" runat="server">
    </asp:Table>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
</asp:Content>

