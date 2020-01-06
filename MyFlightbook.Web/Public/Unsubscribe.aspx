<%@ Page Title="Unsubscribe" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_Unsubscribe" Codebehind="Unsubscribe.aspx.cs" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Localize ID="Localize1" runat="server" Text="<%$ Resources:Profile, UnsubscribeHeader %>"></asp:Localize>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <p><asp:Label ID="lblErr" EnableViewState="false" CssClass="error" runat="server" Text=""></asp:Label>
    <asp:Label ID="lblSuccess" runat="server" Text=""></asp:Label></p>
</asp:Content>

