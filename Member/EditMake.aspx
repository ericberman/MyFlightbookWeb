<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" CodeFile="EditMake.aspx.cs" Inherits="EditMake" Title="MyFlightbook: Edit aircraft makes and models" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbEditMake.ascx" tagname="mfbEditMake" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbATDFTD.ascx" tagname="mfbATDFTD" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbTotalSummary.ascx" tagname="mfbTotalSummary" tagprefix="uc3" %>
<%@ Register src="../Controls/mfbLogbook.ascx" tagname="mfbLogbook" tagprefix="uc4" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblEditModel" runat="server" Text="Add Model"></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <div class="noprint">
        <uc2:mfbATDFTD ID="mfbATDFTD1" runat="server" />
        <uc1:mfbEditMake ID="mfbEditMake1" runat="server" OnMakeUpdated="MakeUpdated" />
        <div><asp:HyperLink ID="lnkViewAircraft" runat="server" Visible="false" Target="_blank">ADMIN - View Aircraft</asp:HyperLink>&nbsp;</div>
        <br />
    </div>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpMain" runat="Server">
    <asp:Panel ID="pnlStats" runat="server" Visible="false">    
        <br />
        <h2><asp:Label ID="lblMake" runat="server" Text=""></asp:Label></h2>
        <hr />
        <div class="logbookForm" style="float:left; clear:right; border: 1px solid black; padding: 3px;">
            <asp:Label ID="lblMakeStatsHeader" Font-Bold="true" runat="server" Text="<%$ Resources:Makes, makeStatsTotals %>"></asp:Label>
            <br />
            <uc3:mfbTotalSummary ID="mfbTotalSummary1" runat="server" /></div>
        <div style="float:left; padding:3px;">
            <uc4:mfbLogbook ID="mfbLogbook1" runat="server" />
        </div>
    </asp:Panel>   
</asp:content>
