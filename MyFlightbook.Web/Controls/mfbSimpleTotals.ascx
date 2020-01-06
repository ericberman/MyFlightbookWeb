<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbSimpleTotals" Codebehind="mfbSimpleTotals.ascx.cs" %>
<%@ Register src="mfbTotalSummary.ascx" tagname="mfbTotalSummary" tagprefix="uc1" %>
<div>Flight Totals -
<asp:DropDownList ID="cmbTotals" runat="server" AutoPostBack="True" OnSelectedIndexChanged="cmbTotals_SelectedIndexChanged">
    <asp:ListItem Selected="True" Value="All" Text="<%$ Resources:LocalizedText, DatesAll %>"></asp:ListItem>
    <asp:ListItem Value="MTD" Text="<%$ Resources:LocalizedText, DatesThisMonth %>"></asp:ListItem>
    <asp:ListItem Value="PrevMonth" Text="<%$ Resources:LocalizedText, DatesPrevMonth %>"></asp:ListItem>
    <asp:ListItem Value="YTD" Text="<%$ Resources:LocalizedText, DatesYearToDate %>"></asp:ListItem>
    <asp:ListItem Value="Trailing6" Text="<%$ Resources:LocalizedText, DatesPrev6Month %>"></asp:ListItem>
    <asp:ListItem Value="Trailing12" Text="<%$ Resources:LocalizedText, DatesPrev12Month %>">12-month</asp:ListItem>
</asp:DropDownList></div>

<uc1:mfbTotalSummary ID="mfbTotalSummary1" runat="server" />


