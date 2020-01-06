<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_FindFlights" Title="" Codebehind="FindFlights.aspx.cs" %>
<%@ Register Src="../Controls/mfbLogbook.ascx" TagName="mfbLogbook" TagPrefix="uc6" %>
<%@ Register Src="../Controls/mfbSimpleTotals.ascx" TagName="mfbSimpleTotals" TagPrefix="uc3" %>
<%@ Register Src="../Controls/mfbCurrency.ascx" TagName="mfbCurrency" TagPrefix="uc2" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbSearchAndTotals.ascx" tagname="mfbSearchAndTotals" tagprefix="uc7" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="cc1" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblUserName" runat="server"></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <uc7:mfbSearchAndTotals ID="mfbSearchAndTotals1" runat="server" 
        InitialCollapseState="True" OnQuerySubmitted="FilterResults" />
    <p class="noprint">
        <asp:LinkButton ID="lnkPrinterView" runat="server" 
            onclick="lnkPrinterView_Click" Text="<%$ Resources:LocalizedText, FindFlightsPrinterViewTotals %>"></asp:LinkButton>
        <asp:LinkButton ID="lnkRegularView" runat="server" 
            onclick="lnkRegularView_Click" Visible="false" Text="<%$ Resources:LocalizedText, FindFlightsRegularViewTotals %>"></asp:LinkButton>
    </p>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
    <uc6:mfbLogbook ID="MfbLogbook1" runat="server" />
</asp:Content>

