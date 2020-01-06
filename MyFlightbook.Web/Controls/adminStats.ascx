<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_adminStats" Codebehind="adminStats.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
    <%@ Register src="GoogleChart.ascx" tagname="GoogleChart" tagprefix="uc1" %>
    <h1>Site Stats</h1>
    <h3>Users:</h3>
    <asp:GridView ID="gvUserStats" runat="server" DataSourceID="sqlUserStats" CellPadding="3">
    </asp:GridView>
    <asp:SqlDataSource ID="sqlUserStats" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
        SelectCommand="SELECT COUNT(username) AS 'Users', 
	SUM(emailsubscriptions&lt;&gt; 0) AS Subscriptions, 
    SUM(FacebookAccessToken &lt;&gt; '') 'Facebook Users', 
    SUM(TwitterAccessToken &lt;&gt; '') AS 'Twitter Users', 
    SUM(propertyblacklist &lt;&gt; '') AS blacklistcount,
    SUM(DropboxAccesstoken &lt;&gt; '') AS dropboxusers,
    SUM(GoogleDriveAccessToken &lt;&gt; '') AS googleusers,
    SUM(OnedriveaccessToken &lt;&gt; '') AS oneDriveUsers,
    SUM(IF(DropboxAccesstoken &lt;&gt; '', 1, 0) + IF(GoogleDriveAccessToken&lt;&gt;'', 1, 0) + IF(OneDriveAccessToken &lt;&gt; '', 1, 0) &gt; 1) as multicloudusers,
    SUM(DefaultCloudDriveID &lt;&gt; 0) AS multiusers,
    SUM(month(creationdate)=month(now()) AND year(creationdate)=year(now())) AS 'New Users this month' 
FROM users;" 
        onselecting="setTimeout">
    </asp:SqlDataSource>
    <asp:Button ID="btnTrimAuthenticate" runat="server" Text="Trim Authentications" 
        onclick="btnTrimAuthenticate_Click" />
    <asp:GridView ID="gvOAuthAndPass" runat="server" DataSourceID="sqlOAuthAndPass" CellPadding="3"></asp:GridView>
    <asp:SqlDataSource ID="sqlOAuthAndPass" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
        SelectCommand="SELECT 
            (SELECT count(*) FROM nonce) AS NonceCount,
            (SELECT count(*) FROM oauthclientauthorization) AS 'oAuthClient Auths',
            (SELECT count(*) FROM passwordresetrequests) AS PasswordResetCount;"          
        onselecting="setTimeout">
    </asp:SqlDataSource>
    <asp:Button ID="btnTrimOAuth" runat="server" OnClick="btnTrimOAuth_Click" Text="Trim old oAuth authentications" />
    <h3>Usage:</h3>
    <asp:GridView ID="gvMiscStats" runat="server" DataSourceID="sqlMiscStats">
    </asp:GridView>
    <asp:SqlDataSource ID="SqlMiscStats" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
        SelectCommand="SELECT
                        (SELECT COUNT(*) FROM students) AS Students, 
                        (SELECT count(*) from aircraft where publicnotes &lt;&gt; '') AS publicnotescount,
                        (SELECT count(*) from useraircraft where privatenotes &lt;&gt; '') AS privatenotescount,
                        (SELECT count(*) from flightvideos) AS flightVideoCount,
                        (SELECT count(*) from images where virtpathid=0 and imagetype=3) AS AWSVideoCount,
                        (SELECT count(*) from clubs) AS clubcount" 
        onselecting="setTimeout">
    </asp:SqlDataSource>
    <table>
        <tr>
            <td>
                <asp:GridView ID="gvUserSources" runat="server" DataSourceID="sqlUserSources">
                </asp:GridView>
                <asp:SqlDataSource ID="sqlUserSources" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                    SelectCommand="select count(eventID) AS 'Users', RIGHT(description, LENGTH(description) - LOCATE(' - ', description) - 2) AS Source from wsevents where eventType=6 AND description LIKE '% - %' GROUP BY Source" 
                    onselecting="setTimeout">
                </asp:SqlDataSource>
            </td>
            <td>
                <asp:GridView ID="gvWSEvents" runat="server" DataSourceID="sqlDSWebEvents">
                </asp:GridView>
                <asp:SqlDataSource ID="sqlDSWebEvents" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                    SelectCommand="SELECT ELT(eventtype, 'AuthUser', 'GetAircraft', 'FlightsByDate', 'CommitFlightDEPRECATED', 'CreateAircraft', 'CreateUser', 'CreateUserAttemptDEPRECATED', 'CreateUserError', 'ExpiredToken') AS 'Event Type', COUNT(*) AS 'Number of Events' FROM wsevents GROUP BY eventtype;" 
                    onselecting="setTimeout">
                </asp:SqlDataSource>
            </td>
        </tr>
        <tr>
            <td>
                <asp:GridView ID="gvPayments" DataSourceID="sqlDSPayments" runat="server"></asp:GridView>
                <asp:SqlDataSource ID="sqlDSPayments" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                    SelectCommand="SELECT numpayments AS 'Number of payments', COUNT(numpayments) AS 'Number of Users' FROM (SELECT COUNT(username) AS numpayments FROM payments WHERE TransactionType=0 GROUP BY username) p GROUP BY p.numpayments ORDER BY p.numpayments" >
                </asp:SqlDataSource>
            </td>
            <td>
                <asp:GridView ID="gvPaymentStats" DataSourceID="sqlDSPaymentsStats" runat="server"></asp:GridView>
                <asp:SqlDataSource ID="sqlDSPaymentsStats" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                    SelectCommand="SELECT count(Amount) AS 'Number of transactions', Amount FROM payments WHERE TransactionType=0 GROUP BY amount ORDER BY Amount ASC" >
                </asp:SqlDataSource>
            </td>
        </tr>
    </table>
    <h3>Flights and Aircraft:</h3>
    <table>
        <tr valign="top">
            <td>
                <asp:GridView ID="GridViewMisc" runat="server" DataSourceID="sqlSiteOther">
                </asp:GridView>
                <asp:SqlDataSource ID="SqlSiteOther" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT
                   (SELECT COUNT(*) FROM flights) AS 'Flights',
                   (SELECT COUNT(*) from flighttelemetry) AS 'Telemetry Count',
                   (SELECT COUNT(*) FROM models) AS 'Models',
                   (SELECT COUNT(*) FROM airports where sourceusername &lt;&gt; '') AS UserAirports,
                   (SELECT WSCommittedFlights FROM eventcounts WHERE id=1) AS 'WS Committed Flights',
                   (SELECT ImportedFlights FROM eventcounts WHERE id=1) AS 'Imported Flights'" 
                    onselecting="setTimeout">
                </asp:SqlDataSource>
            </td>
            <td>
                <asp:GridView ID="gvAircraft" runat="server" DataSourceID="sqlAircraftStats">
                </asp:GridView>
                <asp:SqlDataSource ID="sqlAircraftStats" runat="server" 
                    ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT IF(ac.instancetype=1, IF(ac.Tailnumber LIKE '#%', 'Anonymous', 'Real'), aic.Description) AS AircraftInstance, COUNT(ac.idaircraft) AS 'Number of Aircraft'
                    FROM Aircraft ac INNER JOIN aircraftinstancetypes aic ON ac.instancetype=aic.id
                    GROUP BY AircraftInstance
                    ORDER BY ac.instancetype ASC" onselecting="setTimeout">
                </asp:SqlDataSource>
            </td>
        </tr>
    </table>
    <asp:Label ID="lblTrimErr" runat="server" Text="" CssClass="error"></asp:Label>
    <!-- Daily new users -->
    <h3>New Users: <asp:Label ID="lblShowUsersData" runat="server" Text="(Click to show)"></asp:Label></h3>
    <asp:Panel ID="pnlShowUSersData" runat="server" Height="0px" Style="overflow: hidden;">
        <h3>Daily new users:</h3>
        <asp:SqlDataSource ID="sqlDSDaily" runat="server" 
            ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="select  CAST(Date_Format(creationdate, '%m/%e/%Y') AS CHAR) AS Date, count(username) AS 'New Users'
            from users
            group by date(creationdate)
            order by creationdate desc
            limit 40" onselecting="setTimeout">
        </asp:SqlDataSource>
        <asp:GridView ID="gvUsers" runat="server" AutoGenerateColumns="False" 
            DataSourceID="sqlDSDaily" EnableModelValidation="True">
            <Columns>
                <asp:BoundField DataField="New Users" HeaderText="New Users" ReadOnly="True" 
                    SortExpression="New Users" />
                <asp:BoundField DataField="Date" HeaderText="Date" ReadOnly="True" 
                    SortExpression="Date" />
            </Columns>
        </asp:GridView>
        <h3>Monthly new users:</h3>
        <asp:GridView ID="gvUserData" AllowSorting="true" runat="server" AutoGenerateColumns="false">
            <Columns>
                <asp:BoundField DataField="DisplayPeriod" HeaderText="Month" ReadOnly="True" 
                    SortExpression="SortPeriod" />
                <asp:BoundField DataField="NewUsers" HeaderText="New Users" ReadOnly="True" 
                    SortExpression="NewUSers" />
                <asp:BoundField DataField="RunningTotal" HeaderText="Running Total" ReadOnly="True" 
                    SortExpression="RunningTotal" />
            </Columns>
        </asp:GridView>
    </asp:Panel>
    <asp:SqlDataSource ID="sqlUserData" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT
    x1.SortPeriod,
    x1.DisplayPeriod,
    CreationYear,
    CreationMonth,
    x1.NewUsers AS NewUsers,
    SUM(x2.NewUsers) AS RunningTotal
    FROM (SELECT
    CONCAT(YEAR(CreationDate), LPAD(MONTH(CreationDate),2,'0')) AS 'SortPeriod',
    CAST(CONCAT(YEAR(CreationDate), '-', MONTHNAME(CreationDate)) AS CHAR) AS 'DisplayPeriod',
    YEAR(CreationDate) AS CreationYear,
    MONTH(CreationDate) AS CreationMonth,
    COUNT(DISTINCT(username)) AS 'NewUsers'
    FROM users
    GROUP BY SortPeriod
    ORDER BY SortPeriod ASC
    ) AS x1
    INNER JOIN (SELECT
    CONCAT(YEAR(CreationDate), LPAD(MONTH(CreationDate),2,'0')) AS 'SortPeriod',
    CAST(CONCAT(YEAR(CreationDate), '-', MONTHNAME(CreationDate)) AS CHAR) AS 'DisplayPeriod',
    COUNT(DISTINCT(username)) AS 'NewUsers'
    FROM users
    GROUP BY SortPeriod
    ORDER BY SortPeriod ASC
    ) AS x2
    ON x1.SortPeriod &gt;= x2.SortPeriod
    GROUP BY DisplayPeriod
    ORDER BY SortPeriod ASC" onselecting="setTimeout"></asp:SqlDataSource>
    <uc1:GoogleChart ID="gcNewUsers" XDataType="date" YDataType="number" Y2DataType="number" UseMonthYearDate="true" Title="Number of New Users" LegendType="bottom" TickSpacing="1" SlantAngle="0" XLabel="Year/Month" YLabel="New Users" Y2Label="Cumulative Users" ChartType="LineChart" runat="server" />
   <cc1:CollapsiblePanelExtender ID="CollapsiblePanelExtender1" runat="server" Enabled="True"
        TargetControlID="pnlShowUSersData" CollapsedText="Click to show user data" Collapsed="true"
        CollapseControlID="lblShowUsersData" ExpandControlID="lblShowUsersData" ExpandedText="Click to hide user data"
        TextLabelID="lblShowUsersData">
    </cc1:CollapsiblePanelExtender>

