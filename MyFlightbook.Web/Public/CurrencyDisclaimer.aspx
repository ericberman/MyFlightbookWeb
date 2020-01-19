<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    Codebehind="CurrencyDisclaimer.aspx.cs" Inherits="Public_CurrencyDisclaimer" Title="Currency Computations" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    <asp:Localize ID="locDisclaimerHeader" runat="server" Text="<%$ Resources:Currency, CurrencyImportantNotes %>"></asp:Localize>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <% =Branding.ReBrand(Resources.Currency.CurrencyDisclaimer) %>
</asp:Content>
