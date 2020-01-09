<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Title="" Inherits="SearchTotals" Codebehind="SearchTotals.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbLogbook.ascx" TagName="mfbLogbook" TagPrefix="uc2" %>
<%@ Register src="../Controls/mfbTotalSummary.ascx" tagname="mfbTotalSummary" tagprefix="uc3" %>
<%@ Register src="../Controls/mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc4" %>
<%@ Register src="../Controls/mfbSearchForm.ascx" tagname="mfbSearchForm" tagprefix="uc5" %>
<%@ Register src="../Controls/mfbSearchAndTotals.ascx" tagname="mfbSearchAndTotals" tagprefix="uc6" %>
<asp:content id="Content1" contentplaceholderid="cpMain" runat="Server">
    <div>

       <uc6:mfbSearchAndTotals ID="mfbSearchAndTotals1" runat="server" />

       <asp:LinkButton ID="lnkShowFlights" runat="server" OnClick="lnkShowFlights_Click" Text="<%$ Resources:Totals, lnkShowMatchingFlights %>"></asp:LinkButton>
       <asp:LinkButton ID="lnkHideFlights" runat="server" Visible="false" OnClick="lnkHideFlights_Click" Text="<%$ Resources:Totals, lnkHideMatchingFlights %>"></asp:LinkButton>
       <uc2:mfbLogbook ID="MfbLogbook1" runat="server" Visible="false" PageSize="10" AllowPaging="true" />
    </div>
</asp:content>
