<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="MiniTotals.aspx.cs" Inherits="Member_MiniTotals" Title="Totals and currency" %>
<%@ Register Src="../Controls/mfbSimpleTotals.ascx" TagName="mfbSimpleTotals" TagPrefix="uc2" %>
<%@ Register Src="../Controls/mfbCurrency.ascx" TagName="mfbCurrency" TagPrefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:content id="Content1" contentplaceholderid="cpMain" runat="Server">
    <uc1:mfbCurrency ID="MfbCurrency1" runat="server" />
    <br />
    <uc2:mfbSimpleTotals ID="MfbSimpleTotals1" runat="server" MobileView="true" />
</asp:content>
