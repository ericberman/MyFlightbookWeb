<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="Achievements.aspx.cs" Inherits="Member_Achievements" %>
<%@ MasterType  VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbBadgeSet.ascx" tagname="mfbBadgeSet" tagprefix="uc1" %>
<%@ Register Src="~/Controls/mfbRecentAchievements.ascx" TagPrefix="uc1" TagName="mfbRecentAchievements" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc1" TagName="mfbTypeInDate" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblAchievementsHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <div><asp:LinkButton ID="lnkRecompute" runat="server" OnClick="lnkRecompute_Click" Text="<%$ Resources:Achievements, btnForceRefresh %>"></asp:LinkButton></div>
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
    <asp:UpdatePanel ID="updRA" runat="server">
        <ContentTemplate>
            <h2>
                <asp:Label ID="lblRecentAchievementsTitle" runat="server"></asp:Label>
                <asp:DropDownList ID="cmbAchievementDates" runat="server" AutoPostBack="True" OnSelectedIndexChanged="cmbAchievementDates_SelectedIndexChanged">
                    <asp:ListItem Selected="True" Value="AllTime" Text="<%$ Resources:LocalizedText, DatesAll %>"></asp:ListItem>
                    <asp:ListItem Value="Tailing6Months" Text="<%$ Resources:FlightQuery, DatesPrev6Month %>"></asp:ListItem>
                    <asp:ListItem Value="Trailing12Months" Text="<%$ Resources:FlightQuery, DatesPrev12Month %>"></asp:ListItem>
                    <asp:ListItem Value="Trailing30" Text="<%$ Resources:FlightQuery, DatesPrev30Days %>"></asp:ListItem>
                    <asp:ListItem Value="Trailing90" Text="<%$ Resources:FlightQuery, DatesPrev90Days %>"></asp:ListItem>
                    <asp:ListItem Value="YTD" Text="<%$ Resources:FlightQuery, DatesYearToDate %>">12-month</asp:ListItem>
                    <asp:ListItem Value="ThisMonth" Text="<%$ Resources:FlightQuery, DatesThisMonth %>"></asp:ListItem>
                    <asp:ListItem Value="PrevMonth" Text="<%$ Resources:FlightQuery, DatesPrevMonth %>"></asp:ListItem>
                    <asp:ListItem Value="PrevYear" Text="<%$ Resources:FlightQuery, DatesPrevYear %>"></asp:ListItem>
                    <asp:ListItem Value="Custom" Text="<%$ Resources:Achievements, RecentAchievementCustomDates %>"></asp:ListItem>
                </asp:DropDownList>
                <asp:ImageButton ID="imgCopy" style="vertical-align:text-bottom" ImageUrl="~/images/copyflight.png" AlternateText="<%$ Resources:LocalizedText, CopyToClipboard %>" ToolTip="<%$ Resources:LocalizedText, CopyToClipboard %>" runat="server" />
                <asp:Label ID="lblCopied" runat="server" Text="<%$ Resources:LocalizedText, CopiedToClipboard %>" CssClass="hintPopup" style="display:none; font-weight:bold; font-size: 10pt; color:black; "></asp:Label>
            </h2>
            <asp:Panel runat="server" ID="pnlCustomDates" Visible="false">
                <table>
                    <tr>
                        <td>
                            <asp:Label ID="Label1" runat="server" Text="<%$ Resources:Achievements, RecentAchievementCustomDateFrom %>"></asp:Label>
                        </td>
                        <td>
                            <uc1:mfbTypeInDate runat="server" ID="mfbTypeInDateFrom" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <asp:Label ID="Label2" runat="server" Text="<%$ Resources:Achievements, RecentAchievementCustomDateTo %>"></asp:Label>
                        </td>
                        <td>
                            <uc1:mfbTypeInDate runat="server" ID="mfbTypeInDateTo" />
                        </td>
                    </tr>
                    <tr>
                        <td></td>
                        <td><asp:Button ID="btnOK" runat="server" Text="<%$ Resources:LocalizedText, OK %>" OnClick="btnOK_Click" /></td>
                    </tr>
                </table>
            </asp:Panel>
            <div id="raContainer">
                <uc1:mfbRecentAchievements runat="server" ID="mfbRecentAchievements" />
                <div><asp:Label ID="lblNoStats" runat="server" Visible="false" Text="<%$ Resources:LocalizedText, errNoMatchingFlightsFound %>"></asp:Label></div>
            </div>
            <asp:LinkButton ID="lnkShowCalendar" runat="server" Text="<%$ Resources:Achievements, RecentAchievementsViewCalendar %>" OnClick="lnkShowCalendar_Click"></asp:LinkButton>
            <asp:Label ID="lblErr" CssClass="error" runat="server" EnableViewState="false" Text=""></asp:Label>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
