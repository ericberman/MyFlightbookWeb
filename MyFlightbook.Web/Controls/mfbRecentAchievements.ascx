<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbRecentAchievements.ascx.cs" Inherits="Controls_mfbRecentAchievements" %>
<%@ Register src="../Controls/mfbBadgeSet.ascx" tagname="mfbBadgeSet" tagprefix="uc1" %>
<style type="text/css">
    .raContainerTile {
        display: inline-block;
        width: 180px;
        height: 180px;
        border: 3px solid lightgray;
        border-radius: 12px;
        margin: 10pt;
        padding: 3pt;
        vertical-align: top;
    }
    .raContainerTileContent {
        display: flex;
        flex-direction: column;
        height: 100%;
        width: 100%;
        justify-content: space-between;
        text-align: center;
    }
    .raTileIcon {
        width: 40pt;
        height: 40pt;
    }
    .raStatHeader {
        font-size: 120%;
    }
    .raStatResult {
        font-weight: bold;
        font-size: 120%;
    }
</style>
<asp:Panel ID="pnlStatsAndAchievements" runat="server">
    <div style="margin-left: auto; margin-right: auto;">
        <asp:Repeater ID="rptRecentAchievements" runat="server">
            <ItemTemplate>
                <div class="raContainerTile">
                    <div class="raContainerTileContent">
                        <div style="text-align:center">
                            <div><img src='<%# ((string) Eval("CategoryImage")).ToAbsoluteURL(Request) %>' class="raTileIcon" /></div>
                            <div class="raStatHeader"><asp:Label ID="Label1" runat="server" Text='<%# Eval("Title") %>'></asp:Label></div>
                        </div>
                        <div class="raStatResult">
                            <asp:MultiView ID="mvAchievement" runat="server" ActiveViewIndex='<%# String.IsNullOrEmpty((string) Eval("TargetLink")) || IsReadOnly ? 0 : 1 %>'>
                                <asp:View ID="vwStatic" runat="server">
                                    <asp:Label ID="lblStatic" runat="server" Text='<%# Eval("MatchingEventText") %>'></asp:Label>
                                </asp:View>
                                <asp:View ID="vwLink" runat="server">
                                    <asp:HyperLink ID="lnkLinked" runat="server" Text='<%# Eval("MatchingEventText") %>' NavigateUrl='<%# ((string)Eval("TargetLink")).ToAbsoluteURL(Request) %>'></asp:HyperLink>
                                </asp:View>
                            </asp:MultiView>
                        </div>
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>
    <asp:Repeater ID="rptRecentlyearnedBadges" runat="server">
        <ItemTemplate>
            <div><uc1:mfbBadgeSet runat="server" ID="mfbBadgeSet" BadgeSet='<%# Container.DataItem %>' /></div>
        </ItemTemplate>
    </asp:Repeater>
</asp:Panel>
