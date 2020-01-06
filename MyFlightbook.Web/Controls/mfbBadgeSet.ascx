<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbBadgeSet" Codebehind="mfbBadgeSet.ascx.cs" %>
<asp:Panel ID="pnlBadges" runat="server">
    <h2><asp:Label ID="lblCategory" runat="server" Text=""></asp:Label></h2>
    <asp:Repeater ID="repeaterBadges" runat="server">
        <ItemTemplate>
            <div style="display:inline-block; text-align:center; vertical-align:top; margin-left: 5px; margin-right:5px; width:140px">
                <div style="position:relative; display:inline;">
                    <asp:Image ID="imgBadge" runat="server" Width="70" Height="113" ImageUrl='<%# VirtualPathUtility.ToAbsolute((string) Eval("BadgeImage")) %>' ToolTip='<%# Eval("BadgeImageAltText") %>' AlternateText='<%# Eval("BadgeImageAltText") %>' />
                    <asp:Image ID="imgOverlay" runat="server" Width="70" ImageUrl='<%# String.IsNullOrEmpty((string) Eval("BadgeImageOverlay")) ? string.Empty : VirtualPathUtility.ToAbsolute((string) Eval("BadgeImageOverlay")) %>' ToolTip="" AlternateText="" Visible='<%# Eval("BadgeImageOverlay").ToString().Length > 0 %>' style="position:absolute; bottom: 0; left: 0; z-index:1;" />
                </div>
                <div><asp:Label ID="lblBadgeName" runat="server" Text='<%# Eval("Name") %>' Font-Bold="true"></asp:Label></div>
                <div style="font-size:smaller">
                    <asp:MultiView ID="mvEarned" runat="server" ActiveViewIndex='<%# ViewIndexForBadge((MyFlightbook.Achievements.Badge) Container.DataItem) %>'>
                        <asp:View ID="vwNotAchieved" runat="server">
                        </asp:View>
                        <asp:View ID="vwAchievedNoFlight" runat="server">
                            <asp:Label ID="lblDateEarned" runat="server" Text='<%# Eval("EarnedDateString") %>'></asp:Label>
                        </asp:View>
                        <asp:View ID="vwAchievedWithFlight" runat="server">
                            <asp:HyperLink ID="lnkFlightEarned" Text='<%# Eval("EarnedDateString") %>' runat="server" NavigateUrl='<%# VirtualPathUtility.ToAbsolute(String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Member/FlightDetail.aspx/{0}", Eval("IDFlightEarned"))) %>'></asp:HyperLink>
                        </asp:View>
                    </asp:MultiView>
                </div>
            </div>
        </ItemTemplate>
    </asp:Repeater>
</asp:Panel>


