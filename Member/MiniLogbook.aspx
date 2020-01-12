<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    CodeFile="MiniLogbook.aspx.cs" Inherits="Member_MiniLogbook" Title="Flying Logbook" culture="auto" meta:resourcekey="PageResource1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbEditFlight.ascx" tagname="mfbEditFlight" tagprefix="uc4" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    <asp:Label ID="lblUserName" runat="server" meta:resourcekey="lblUserNameResource1"></asp:Label>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <asp:Panel ID="pnlSuccess" runat="server" Visible="false" EnableViewState="false"
        meta:resourcekey="pnlSuccessResource1">
        <asp:Label ID="lblSuccessText" runat="server" Font-Bold="True" 
        Text="Entry Successful!" meta:resourcekey="lblSuccessTextResource1"></asp:Label> 
        <asp:Localize ID="Localize1" runat="server" Text="You may enter another one below" 
        meta:resourcekey="Localize1Resource1"></asp:Localize>
        <br />
        <asp:HyperLink ID="lnk" NavigateUrl="~/DefaultMini.aspx" runat="server" 
        Text="Go back" meta:resourcekey="lnkResource1"></asp:HyperLink>.<br />
    </asp:Panel>    
    <div style="width:600px; clear:both;" id="miniedit">
        <uc4:mfbEditFlight ID="mfbEditFlight1" runat="server" OnFlightUpdated="FlightUpdated" />
    </div>
</asp:Content>
