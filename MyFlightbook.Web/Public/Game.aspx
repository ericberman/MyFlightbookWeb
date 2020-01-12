<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    Codebehind="Game.aspx.cs" Inherits="Game_Game" Title="" %>
<%@ Register Src="../Controls/mfbAirportIDGame.ascx" TagName="mfbAirportIDGame" TagPrefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblAirportGameHeader" runat="server" Text="<%$Resources:Airports, airportGameTitle %>"></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <uc1:mfbAirportIDGame id="MfbAirportIDGame1" runat="server" BluffCount="4" QuestionCount="10">
    </uc1:mfbAirportIDGame>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
</asp:Content>
