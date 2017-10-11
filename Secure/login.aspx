<%@ Page Language="C#" Title="" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="login.aspx.cs" Inherits="login" %>
<%@ Register Src="../Controls/mfbSignIn.ascx" TagName="mfbSignIn" TagPrefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="contentTitle" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Localize ID="locHeader" runat="server"></asp:Localize>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpTopForm" runat="Server">
    <uc1:mfbSignIn ID="mfbSignIn1" runat="server" />
</asp:content>
