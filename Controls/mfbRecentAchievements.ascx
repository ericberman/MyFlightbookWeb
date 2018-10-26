<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbRecentAchievements.ascx.cs" Inherits="Controls_mfbRecentAchievements" %>
<%@ Register src="../Controls/mfbBadgeSet.ascx" tagname="mfbBadgeSet" tagprefix="uc1" %>
<asp:Panel ID="pnlStatsAndAchievements" runat="server">
    <h2><asp:Label ID="lblTitle" runat="server" /></h2>
    <div>
        <ul>
            <asp:Repeater ID="rptRecentAchievements" runat="server">
                <ItemTemplate>
                    <li>
                        <asp:Label ID="lblTitle" runat="server" Text='<%# Eval("Title") %>'></asp:Label>
                        <asp:MultiView ID="mvAchievement" runat="server" ActiveViewIndex='<%# String.IsNullOrEmpty((string) Eval("TargetLink")) ? 0 : 1 %>'>
                            <asp:View ID="vwStatic" runat="server">
                                <asp:Label ID="lblStatic" runat="server" Text='<%# Eval("MatchingEventText") %>'></asp:Label>
                            </asp:View>
                            <asp:View ID="vwLink" runat="server">
                                <asp:HyperLink ID="lnkLinked" runat="server" Text='<%# Eval("MatchingEventText") %>' NavigateUrl='<%# Eval("TargetLink") %>'></asp:HyperLink>
                            </asp:View>
                        </asp:MultiView>
                    </li>
                </ItemTemplate>
            </asp:Repeater>
        </ul>
    </div>
    <asp:Repeater ID="rptRecentlyearnedBadges" runat="server">
        <ItemTemplate>
            <div><uc1:mfbBadgeSet runat="server" ID="mfbBadgeSet" BadgeSet='<%# Container.DataItem %>' /></div>
        </ItemTemplate>
    </asp:Repeater>
</asp:Panel>
