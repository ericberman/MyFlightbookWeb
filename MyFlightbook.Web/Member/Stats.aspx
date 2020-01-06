<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_Stats" Title="Statistics" Codebehind="Stats.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="../Controls/adminStats.ascx" tagname="adminStats" tagprefix="uc1" %>
<%@ Register src="../Controls/GoogleChart.ascx" tagname="GoogleChart" tagprefix="uc2" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="server">
    <uc1:adminStats ID="adminStats1" runat="server" />
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
    <h3>User Activity <asp:Button ID="btnUserActivity" runat="server" Text="Refresh" 
            onclick="btnUserActivity_Click" /></h3>
    <asp:Panel ID="pnlUserActivity" runat="server" Visible="false">
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
ORDER BY SortPeriod ASC" onselecting="sqlUserActivity_Selecting"></asp:SqlDataSource>
        <uc2:GoogleChart ID="gcUserActivity" LegendType="right" UseMonthYearDate="true" XDataType="date" Title="User Activity" XLabel="Date of Last Activity" YLabel="Users" SlantAngle="90" ChartType="LineChart" runat="server" />
    </asp:Panel>

    <!-- Flights per user -->
    <h3>Flights per user</h3>
<asp:SqlDataSource ID="sqlFlightsPerUser" runat="server" 
        ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
        
        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT
   CAST(IF (f.numflights IS NULL, 0, if (f.numflights = 0, 0, CONCAT(IF(f.numFlights &lt; 100, '1', TRUNCATE(f.numFlights, -2)), '-', TRUNCATE(f.numFlights, -2)+99))) AS CHAR) AS NumFlightsBucket,
   SUM(f.numflights) AS NumFlights,
   COUNT(f.username) AS NumUsers,
   f.class AS UserClass,
   IF (f.numflights = 0, -1, TRUNCATE(f.numFlights, -2)) AS ordinal
FROM (
  SELECT
    u.username,
    COUNT(f.idflight) AS numFlights,
    COUNT(DISTINCT(u.username)) AS totalUsers,
    IF (u.CreationDate &gt; DATE_ADD(CURDATE(), INTERVAL - 30 DAY), 'new', 'existing') AS class
  FROM users u LEFT JOIN flights f ON u.username=f.username
  GROUP BY username
  ORDER BY numFlights DESC) f
GROUP BY NumFlightsBucket, UserClass
ORDER BY ordinal ASC" onselecting="sqlFlightsPerUser_Selecting">
</asp:SqlDataSource>
<uc2:GoogleChart ID="gcFlightsPerUser" Title="Flights/User" XDataType="string" YDataType="number" Y2DataType="number" XLabel="Flights/User" YLabel="Users - All" Y2Label="Users - New (&lt; 30 days)" SlantAngle="90" Width="1000" Height="500" ChartType="ColumnChart" runat="server" />

    <!-- Flight trends -->
    <h3>Flights on the site: <asp:Button ID="btnUpdateFlights" runat="server" Text="Refresh" 
        onclick="btnUpdateFlights_Click" /></h3>
    <asp:Panel ID="pnlFlightsChart" runat="server" Visible="false">
        <uc2:GoogleChart ID="gcFlightsOnSite" LegendType="bottom" Title="Flights recorded / month" XDataType="string" YDataType="number" UseMonthYearDate="true" Y2DataType="number" XLabel="Flights/Month" YLabel="Flights" Y2Label="Running Total" SlantAngle="90" ChartType="LineChart" Width="1000" runat="server" Height="500" />
        <p><asp:Label ID="lblShowFlightsData" runat="server" Text="" EnableViewState="false"></asp:Label></p>
        <asp:Panel ID="pnlShowFlightsData" runat="server" Height="0px" Style="overflow: hidden;">
            <asp:GridView ID="gvFlightsData" runat="server">
            </asp:GridView>
        </asp:Panel>
        <cc1:CollapsiblePanelExtender ID="pnlShowFlightsData_CollapsiblePanelExtender" runat="server"
            Enabled="True" TargetControlID="pnlShowFlightsData" CollapsedText="Click to show flights data"
            Collapsed="true" CollapseControlID="lblShowFlightsData" ExpandControlID="lblShowFlightsData"
            ExpandedText="Click to hide flights data" TextLabelID="lblShowFlightsData">
        </cc1:CollapsiblePanelExtender>
    </asp:Panel>
    <asp:SqlDataSource ID="sqlFlightsTrend" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
        
        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT
  x1.SortPeriod,
  x1.DisplayPeriod,
  x1.NewFlights AS NewFlights,
  x1.flightYear,
  x1.flightMonth,
  SUM(x2.NewFlights) AS RunningTotal
FROM (SELECT
      CONCAT(YEAR(flights.date), LPAD(MONTH(flights.date),2,'0')) AS 'SortPeriod',
      CAST(CONCAT(YEAR(flights.date), '-', MONTHNAME(flights.date)) AS CHAR) AS 'DisplayPeriod',
      YEAR(flights.date) AS flightYear,
      MONTH(flights.date) AS flightMonth,
      COUNT(DISTINCT(idFlight)) AS 'NewFlights'
  FROM flights
  GROUP BY SortPeriod
  ORDER BY SortPeriod ASC
  ) AS x1
INNER JOIN (SELECT
      CONCAT(YEAR(flights.date), LPAD(MONTH(flights.date),2,'0')) AS 'SortPeriod',
      CAST(CONCAT(YEAR(flights.date), '-', MONTHNAME(flights.date)) AS CHAR) AS 'DisplayPeriod',
      COUNT(DISTINCT(idFlight)) AS 'NewFlights'
  FROM flights
  GROUP BY SortPeriod
  ORDER BY SortPeriod ASC
  ) AS x2
ON x1.SortPeriod &gt;= x2.SortPeriod
GROUP BY DisplayPeriod
ORDER BY SortPeriod ASC" onselecting="sqlFlightsTrend_Selecting"></asp:SqlDataSource>

</asp:Content>

