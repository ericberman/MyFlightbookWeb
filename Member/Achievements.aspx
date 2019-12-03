<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="Achievements.aspx.cs" Inherits="Member_Achievements" %>
<%@ MasterType  VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbBadgeSet.ascx" tagname="mfbBadgeSet" tagprefix="uc1" %>
<%@ Register Src="~/Controls/mfbRecentAchievements.ascx" TagPrefix="uc1" TagName="mfbRecentAchievements" %>
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
            <script>
                // Credit to https://hackernoon.com/copying-text-to-clipboard-with-javascript-df4d4988697f
                function copyAchievements() {
                    const el = document.createElement('textarea');  // Create a <textarea> element
                    var title = document.getElementById('<% =lblRecentAchievementsTitle.ClientID %>').innerText;
                    var body = document.getElementById('raContainer').innerText;
                    el.value = title + '\n' + body;   // Set its value to the string that you want copied
                    el.setAttribute('readonly', '');                // Make it readonly to be tamper-proof
                    el.style.position = 'absolute';
                    el.style.left = '-9999px';                      // Move outside the screen to make it invisible
                    document.body.appendChild(el);                  // Append the <textarea> element to the HTML document
                    const selected =
                        document.getSelection().rangeCount > 0        // Check if there is any content selected previously
                            ? document.getSelection().getRangeAt(0)     // Store selection if found
                            : false;                                    // Mark as false to know no selection existed before
                    el.select();                                    // Select the <textarea> content
                    document.execCommand('copy');                   // Copy - only works as a result of a user action (e.g. click events)
                    document.body.removeChild(el);                  // Remove the <textarea> element
                    if (selected) {                                 // If a selection existed before copying
                        document.getSelection().removeAllRanges();    // Unselect everything on the HTML document
                        document.getSelection().addRange(selected);   // Restore the original selection
                    }
                }
            </script>
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
                </asp:DropDownList>
                <asp:ImageButton ID="imgCopy" style="vertical-align:text-bottom" OnClientClick="javascript:copyAchievements();return false;" ImageUrl="~/images/copyflight.png" AlternateText="<%$ Resources:LocalizedText, CopyToClipboard %>" ToolTip="<%$ Resources:LocalizedText, CopyToClipboard %>" runat="server" />
            </h2>
            <div id="raContainer">
                <uc1:mfbRecentAchievements runat="server" ID="mfbRecentAchievements" />
                <div><asp:Label ID="lblNoStats" runat="server" Visible="false" Text="<%$ Resources:LocalizedText, errNoMatchingFlightsFound %>"></asp:Label></div>
            </div>
            <asp:LinkButton ID="lnkShowCalendar" runat="server" Text="<%$ Resources:Achievements, RecentAchievementsViewCalendar %>" OnClick="lnkShowCalendar_Click"></asp:LinkButton>
            <asp:Label ID="lblErr" CssClass="error" runat="server" EnableViewState="false" Text=""></asp:Label>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
