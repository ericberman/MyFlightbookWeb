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
        <div><asp:HyperLink ID="lnkViewTotals" runat="server" Visible="false"></asp:HyperLink></div>
        <br />
    </div>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpMain" runat="Server">
</asp:content>
