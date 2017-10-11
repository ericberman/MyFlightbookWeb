<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="SignFlight.aspx.cs" Inherits="Member_SignFlight" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbSignFlight.ascx" tagname="mfbSignFlight" tagprefix="uc1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
    <asp:Panel ID="pnlSign" runat="server" Visible="false">
        <h2><asp:Label ID="lblHeader" runat="server" Text=""></asp:Label></h2>
        <uc1:mfbSignFlight ID="mfbSignFlight1" runat="server" OnCancel="GoBack" OnSigningFinished="GoBack" SigningMode="Authenticated" />
    </asp:Panel>
    <asp:HiddenField ID="hdnReturnURL" runat="server" />
    <asp:HiddenField ID="hdnFlightID" runat="server" />
    <asp:Label ID="lblError" runat="server" CssClass="error"></asp:Label>
</asp:Content>

