<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" CodeBehind="ViewSharedLogbook.aspx.cs" Inherits="MyFlightbook.Web.Public.ViewSharedLogbook" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/mfbLogbook.ascx" TagPrefix="uc1" TagName="mfbLogbook" %>
<%@ Register Src="~/Controls/mfbCurrency.ascx" TagPrefix="uc1" TagName="mfbCurrency" %>
<%@ Register Src="~/Controls/mfbTotalSummary.ascx" TagPrefix="uc1" TagName="mfbTotalSummary" %>



<asp:Content id="Content2" contentplaceholderid="cpPageTitle" runat="Server">
    <asp:Label ID="lblHeader" runat="server"></asp:Label>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpTopForm" runat="Server">
    <uc1:mfbCurrency runat="server" ID="mfbCurrency" Visible="false" />
    <uc1:mfbTotalSummary runat="server" ID="mfbTotalSummary" Visible="false" />
    <uc1:mfblogbook runat="server" id="mfbLogbook" Visible="false" />
</asp:content>