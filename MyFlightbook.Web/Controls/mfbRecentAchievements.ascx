<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbRecentAchievements" Codebehind="mfbRecentAchievements.ascx.cs" %>
<%@ Register src="../Controls/mfbBadgeSet.ascx" tagname="mfbBadgeSet" tagprefix="uc1" %>
<asp:Panel ID="pnlStatsAndAchievements" runat="server">
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
    <style type="text/css">
        .monthContainer {
            display: inline-block;
            margin: 3px;
            vertical-align:top;
        }

        .monthHeader {
            text-align:center;
            background-color: darkgray;
            font-weight:bold;
            font-size: 9pt;
        }

        .adjacentDay {
            color: gray;
            background-color:lightgray;
            height: 30px;
            width: 30px;
        }

        .includedDay {
            background-color:white;
            color: black;
            height: 30px;
            width: 30px;
        }

        .dayOfMonth {
            font-size: 7pt;
            font-weight:normal;
            vertical-align:top;
        }

        .dateContent {
            display:block;
            font-size: 8pt;
            font-weight:bold;
            text-align:center;
            vertical-align:middle;
            width: 16px;
            height: 16px;
            border-radius: 8px;
        }

        .dateContentValue {
            background-color: #00ff00;
        }

        .dateContent:hover {
            color: blue;
        }

        .dateContent:link, .dateContent:visited {
            color:black;
        }
    </style>
    <asp:Panel ID="pnlCal" runat="server" Visible="false" >
        <asp:PlaceHolder ID="plcFlyingCalendar" runat="server" EnableViewState="false"></asp:PlaceHolder>
    </asp:Panel>
</asp:Panel>
