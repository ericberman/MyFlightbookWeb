<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_SignFlight" Codebehind="SignFlight.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbSignFlight.ascx" tagname="mfbSignFlight" tagprefix="uc1" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:Panel ID="pnlSign" runat="server" Visible="false">
        <uc1:mfbSignFlight ID="mfbSignFlight1" runat="server" OnCancel="GoBack" OnSigningFinished="GoBack" SigningMode="Authenticated" />
    </asp:Panel>
    <asp:HiddenField ID="hdnReturnURL" runat="server" />
    <asp:HiddenField ID="hdnFlightID" runat="server" />
    <asp:Label ID="lblError" runat="server" CssClass="error"></asp:Label>
</asp:Content>

