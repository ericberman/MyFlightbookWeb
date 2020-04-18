<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="FeatureChart.aspx.cs" MasterPageFile="~/MasterPage.master" Inherits="MyFlightbook.Web.PublicPages.FeatureChart" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" runat="server">
    <% =Branding.ReBrand(Resources.LocalizedText.FeaturesHeader) %>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" runat="Server">
    <% =Branding.ReBrand(Resources.LocalizedText.FeatureTable) %>
</asp:Content>
