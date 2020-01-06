<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_FlyingTrends" Title="" Codebehind="FlyingTrends.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbLogbook.ascx" TagName="mfbLogbook" TagPrefix="uc6" %>
<%@ Register Src="../Controls/mfbSimpleTotals.ascx" TagName="mfbSimpleTotals" TagPrefix="uc3" %>
<%@ Register Src="../Controls/mfbCurrency.ascx" TagName="mfbCurrency" TagPrefix="uc2" %>
<%@ Register src="../Controls/mfbSearchAndTotals.ascx" tagname="mfbSearchAndTotals" tagprefix="uc7" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="cc1" %>
<%@ Register src="../Controls/mfbChartTotals.ascx" tagname="mfbChartTotals" tagprefix="uc8" %>
<%@ Register src="../Controls/mfbSearchForm.ascx" tagname="mfbSearchForm" tagprefix="uc5" %>
<%@ Register Src="~/Controls/mfbQueryDescriptor.ascx" TagPrefix="uc2" TagName="mfbQueryDescriptor" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblUserName" runat="server"></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:MultiView ID="mvTrends" runat="server" ActiveViewIndex="0">
        <asp:View runat="server" ID="vwChart">
            <asp:Button ID="btnChangeQuery" runat="server" Text="<%$ Resources:LocalizedText, ChangeQuery %>" OnClick="btnChangeQuery_Click" />
            <uc2:mfbQueryDescriptor runat="server" ID="mfbQueryDescriptor" ShowEmptyFilter="true" OnQueryUpdated="mfbQueryDescriptor_QueryUpdated" />
            <uc8:mfbChartTotals ID="mfbChartTotals1" runat="server" />
        </asp:View>
        <asp:View runat="server" ID="vwQuery">
            <uc5:mfbSearchForm ID="mfbSearchForm1" runat="server" OnQuerySubmitted="mfbSearchForm1_QuerySubmitted" OnReset="mfbSearchForm1_Reset" InitialCollapseState="true" />
        </asp:View>
    </asp:MultiView>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
    <uc6:mfbLogbook ID="MfbLogbook1" runat="server" />
</asp:Content>

