<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="Privacy.aspx.cs" Inherits="Public_Privacy" Title="" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="ContentTitle" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblPrivacy" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <% =Branding.ReBrand(Resources.LocalizedText.Privacy) %>
</asp:Content>