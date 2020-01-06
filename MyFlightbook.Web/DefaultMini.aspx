<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="DefaultMini" culture="auto" meta:resourcekey="PageResource1" Codebehind="DefaultMini.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpTopForm" runat="Server">
    <asp:Localize ID="locDefaultMiniPrompt" runat="server" 
        Text="What would you like to do?" 
        meta:resourcekey="locDefaultMiniPromptResource1"></asp:Localize>
    <ul>
        <li><asp:HyperLink ID="lnkNewFlight" runat="server" 
                NavigateUrl="~/Member/MiniLogbook.aspx" Text="Enter a flight" 
                meta:resourcekey="lnkNewFlightResource1"></asp:HyperLink><br /></li>
        <li><asp:HyperLink ID="lnkViewTotals" runat="server" 
                NavigateUrl="~/Member/MiniTotals.aspx" Text="View Totals and Currency" 
                meta:resourcekey="lnkViewTotalsResource1"></asp:HyperLink><br /></li>
        <li><asp:HyperLink ID="lnkViewAircraft" runat="server" 
                NavigateUrl="~/Member/Aircraft.aspx" Text="View Aircraft" 
                meta:resourcekey="lnkViewAircraftResource1"></asp:HyperLink><br /></li>
        <li><asp:HyperLink ID="lnkRecentFlights" runat="server" 
                NavigateUrl="~/Member/MiniRecents.aspx" Text="View recent flights" 
                meta:resourcekey="lnkRecentFlightsResource1"></asp:HyperLink><br /></li>
        <li><asp:HyperLink ID="lnkProfile" runat="server" 
                NavigateUrl="~/Member/EditProfile.aspx" Text="Edit your Profile" 
                meta:resourcekey="lnkProfileResource1"></asp:HyperLink><br /></li>
    </ul>
</asp:content>
