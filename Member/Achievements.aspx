<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="Achievements.aspx.cs" Inherits="Member_Achievements" %>
<%@ MasterType  VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbBadgeSet.ascx" tagname="mfbBadgeSet" tagprefix="uc1" %>
<%@ Register Src="~/Controls/mfbRecentAchievements.ascx" TagPrefix="uc1" TagName="mfbRecentAchievements" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblAchievementsHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:MultiView ID="mvBadges" runat="server">
        <asp:View ID="vwNoBadges" runat="server">
            <p><asp:Label ID="lblNoBadges" runat="server" Text="<%$ Resources:Achievements, errNoBadgesEarned %>"></asp:Label></p>
        </asp:View>
        <asp:View ID="vwBadges" runat="server">
            <asp:Repeater ID="rptBadgeset" runat="server">
                <ItemTemplate>
                    <div><uc1:mfbBadgeSet runat="server" ID="mfbBadgeSet" BadgeSet='<%# Container.DataItem %>' /></div>
                </ItemTemplate>
            </asp:Repeater>
        </asp:View>
    </asp:MultiView>
    <uc1:mfbRecentAchievements runat="server" ID="mfbRecentAchievements" />
    <asp:LinkButton ID="lnkShowCalendar" runat="server" Text="<%$ Resources:Achievements, RecentAchievementsViewCalendar %>" OnClick="lnkShowCalendar_Click"></asp:LinkButton>
    <asp:Label ID="lblErr" CssClass="error" runat="server" EnableViewState="false" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
    <asp:LinkButton ID="lnkRecompute" runat="server" OnClick="lnkRecompute_Click" Text="<%$ Resources:Achievements, btnForceRefresh %>"></asp:LinkButton>
</asp:Content>
