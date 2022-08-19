<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="TandC.aspx.cs" Inherits="Public_TandC" Title="Terms and Conditions" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="contentHeader" ContentPlaceHolderID="cpPageTitle" runat="server">
    <% =Resources.LocalizedText.TermsAndConditionsHeader %>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <% =MyFlightbook.Branding.ReBrand(Resources.LocalizedText.TermsAndConditions) %>
</asp:Content>

