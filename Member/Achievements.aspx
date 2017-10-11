<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="Achievements.aspx.cs" Inherits="Member_Achievements" %>
<%@ MasterType  VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbBadgeSet.ascx" tagname="mfbBadgeSet" tagprefix="uc1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblAchievementsHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:PlaceHolder ID="plcBadges" runat="server"></asp:PlaceHolder>
    <asp:Panel ID="pnlNoBadges" Visible="false" runat="server">
        <p><asp:Label ID="lblNoBadges" runat="server" Text="<%$ Resources:Achievements, errNoBadgesEarned %>"></asp:Label>
        </p>
    </asp:Panel>
    <asp:Label ID="lblErr" CssClass="error" runat="server" EnableViewState="false" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
    <asp:LinkButton ID="lnkRecompute" runat="server" OnClick="lnkRecompute_Click" Text="<%$ Resources:Achievements, btnForceRefresh %>"></asp:LinkButton>
</asp:Content>

