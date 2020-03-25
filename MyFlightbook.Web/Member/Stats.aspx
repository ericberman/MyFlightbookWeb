<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="Stats.aspx.cs" Inherits="Member_Stats" Title="Statistics" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="../Controls/adminStats.ascx" tagname="adminStats" tagprefix="uc1" %>
<%@ Register src="../Controls/GoogleChart.ascx" tagname="GoogleChart" tagprefix="uc2" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="server">
    <uc1:adminStats ID="adminStats1" runat="server" />
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
    <h3>User Activity</h3>
    <asp:SqlDataSource ID="sqlUserActivity" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT
    x1.SortPeriod,
    x1.DisplayPeriod,
    x1.ActivityYear,
    x1.ActivityMonth,
    x1.UsersWithSessions
FROM (SELECT
        CONCAT(YEAR(users.LastActivityDate), LPAD(MONTH(users.LastActivityDate),2,'0')) AS 'SortPeriod',
        CAST(CONCAT(YEAR(users.LastActivityDate), '-', MONTHNAME(users.LastActivityDate)) AS CHAR) AS 'DisplayPeriod',
        YEAR(users.lastActivityDate) AS ActivityYear,
        MONTH(users.lastActivityDate) AS ActivityMonth,
        COUNT(DISTINCT(PKID)) AS 'UsersWithSessions'
    FROM users
    GROUP BY SortPeriod
    ORDER BY SortPeriod ASC
    ) AS x1
INNER JOIN (SELECT
        CONCAT(YEAR(users.LastActivityDate), LPAD(MONTH(users.LastActivityDate),2,'0')) AS 'SortPeriod',
        CAST(CONCAT(YEAR(users.LastActivityDate), '-', MONTHNAME(users.LastActivityDate)) AS CHAR) AS 'DisplayPeriod',
        COUNT(DISTINCT(PKID)) AS 'UsersWithSessions'
    FROM users
    GROUP BY SortPeriod
    ORDER BY SortPeriod ASC
    ) AS x2
ON x1.SortPeriod &gt;= x2.SortPeriod
GROUP BY DisplayPeriod
ORDER BY SortPeriod ASC"></asp:SqlDataSource>
    <uc2:GoogleChart ID="gcUserActivity" LegendType="right" UseMonthYearDate="true" XDataType="date" Title="User Activity" XLabel="Date of Last Activity" YLabel="Users" SlantAngle="90" ChartType="LineChart" runat="server" />

    <!-- Flights per user -->
    <h3>Flights per user</h3>
    <div>
        <asp:DropDownList ID="cmbNewUserAge" runat="server" OnSelectedIndexChanged="cmbNewUserAge_SelectedIndexChanged" AutoPostBack="true">
            <asp:ListItem Selected="True" Value="" Text="All"></asp:ListItem>
            <asp:ListItem Value="1" Text="1 Month"></asp:ListItem>
            <asp:ListItem Value="2" Text="2 Months"></asp:ListItem>
            <asp:ListItem Value="3" Text="3 Months"></asp:ListItem>
            <asp:ListItem Value="4" Text="4 Months"></asp:ListItem>
            <asp:ListItem Value="5" Text="5 Months"></asp:ListItem>
            <asp:ListItem Value="6" Text="6 Months"></asp:ListItem>
            <asp:ListItem Value="12" Text="1 Year"></asp:ListItem>
            <asp:ListItem Value="24" Text="2 Years"></asp:ListItem>
        </asp:DropDownList>
    </div>
    <uc2:GoogleChart ID="gcFlightsPerUser" Title="Flights/User" XDataType="string" YDataType="number" Y2DataType="number" XLabel="Flights/User" YLabel="Users - All" TickSpacing="10" SlantAngle="90" Width="1000" Height="500" ChartType="ColumnChart" runat="server" />
    <p><asp:Label ID="lblShowFlightsPerUser" runat="server" Text="" EnableViewState="false"></asp:Label></p>
    <asp:Panel ID="pnlFlightPerUser" runat="server" Height="0px" Style="overflow: hidden;">
        <asp:GridView ID="gvFlightPerUser" runat="server" AutoGenerateColumns="false">
            <Columns>
                <asp:BoundField DataField="DisplayName" HeaderText="Range" />
                <asp:BoundField DataField="Flights" HeaderText="Flights" />
                <asp:BoundField DataField="Flights Running Total" HeaderText="Flights (running total)" />
            </Columns>
        </asp:GridView>
    </asp:Panel>
    <cc1:CollapsiblePanelExtender ID="cpeFlightsPerUser" runat="server"
        Enabled="True" TargetControlID="pnlFlightPerUser" CollapsedText="Click to show flights per user"
        Collapsed="true" CollapseControlID="lblShowFlightsPerUser" ExpandControlID="lblShowFlightsPerUser"
        ExpandedText="Click to hide flights per user data" TextLabelID="lblShowFlightsPerUser">
    </cc1:CollapsiblePanelExtender>
    <!-- Flight trends -->
    <h3>Flights on the site:</h3>
    <uc2:GoogleChart ID="gcFlightsOnSite" LegendType="bottom" Title="Flights recorded / month" XDataType="string" YDataType="number" UseMonthYearDate="true" Y2DataType="number" XLabel="Flights/Month" TickSpacing="36" YLabel="Flights" Y2Label="Running Total" SlantAngle="90" ChartType="LineChart" Width="1000" runat="server" Height="500" />
    <p><asp:Label ID="lblShowFlightsData" runat="server" Text="" EnableViewState="false"></asp:Label></p>
    <asp:Panel ID="pnlShowFlightsData" runat="server" Height="0px" Style="overflow: hidden;">
        <asp:GridView ID="gvFlightsData" runat="server" AutoGenerateColumns="false">
            <Columns>
                <asp:BoundField DataField="DisplayName" HeaderText="Range" />
                <asp:BoundField DataField="Flights" HeaderText="Flights" />
                <asp:BoundField DataField="Flights Running Total" HeaderText="Flights (running total)" />
            </Columns>
        </asp:GridView>
    </asp:Panel>
    <cc1:CollapsiblePanelExtender ID="cpeFlightsOnSite" runat="server"
        Enabled="True" TargetControlID="pnlShowFlightsData" CollapsedText="Click to show flights data"
        Collapsed="true" CollapseControlID="lblShowFlightsData" ExpandControlID="lblShowFlightsData"
        ExpandedText="Click to hide flights data" TextLabelID="lblShowFlightsData">
    </cc1:CollapsiblePanelExtender>
</asp:Content>

