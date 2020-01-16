<%@ Control Language="C#" AutoEventWireup="true" Codebehind="FlyingReport.ascx.cs" Inherits="Controls_ClubControls_FlyingReport" %>
<asp:SqlDataSource ID="sqlDSReports" runat="server" 
ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
SelectCommand="SELECT f.idflight, f.date AS Date, f.TotalFlightTime AS 'Total Time', f.Route, f.HobbsStart AS 'Hobbs Start', f.HobbsEnd AS 'Hobbs End', u.username AS 'Username', u.Firstname, u.LastName, u.Email, ac.Tailnumber AS 'Aircraft',  
fp.decValue AS 'Tach Start', fp2.decValue AS 'Tach End',
f.dtFlightStart AS 'Flight Start', f.dtFlightEnd AS 'Flight End', 
f.dtEngineStart AS 'Engine Start', f.dtEngineEnd AS 'Engine End',
IF (YEAR(f.dtFlightEnd) &gt; 1 AND YEAR(f.dtFlightStart) &gt; 1, (UNIX_TIMESTAMP(f.dtFlightEnd)-UNIX_TIMESTAMP(f.dtFlightStart))/3600, 0) AS 'Total Flight',
IF (YEAR(f.dtEngineEnd) &gt; 1 AND YEAR(f.dtEngineStart) &gt; 1, (UNIX_TIMESTAMP(f.dtEngineEnd)-UNIX_TIMESTAMP(f.dtEngineStart))/3600, 0) AS 'Total Engine',
f.HobbsEnd - f.HobbsStart AS 'Total Hobbs', 
fp2.decValue - fp.decValue AS 'Total Tach',
fp3.decValue AS 'Oil Added',
fp4.decValue AS 'Fuel Added',
fp5.decValue AS 'Fuel Cost',
fp6.decValue AS 'Oil Level'
FROM flights f 
INNER JOIN clubmembers cm ON f.username = cm.username
INNER JOIN users u ON u.username=cm.username
INNER JOIN clubs c ON c.idclub=cm.idclub
INNER JOIN clubaircraft ca ON ca.idaircraft=f.idaircraft
INNER JOIN aircraft ac ON ca.idaircraft=ac.idaircraft
LEFT JOIN flightproperties fp on (fp.idflight=f.idflight AND fp.idproptype=95)
LEFT JOIN flightproperties fp2 on (fp2.idflight=f.idflight AND fp2.idproptype=96)
LEFT JOIN flightproperties fp3 on (fp3.idflight=f.idflight AND fp3.idproptype=365)
LEFT JOIN flightproperties fp4 on (fp4.idflight=f.idflight AND fp4.idproptype=94)
LEFT JOIN flightproperties fp5 on (fp5.idflight=f.idflight AND fp5.idproptype=159)
LEFT JOIN flightproperties fp6 on (fp6.idflight=f.idflight AND fp6.idproptype=650)
WHERE
c.idClub = ?idClub AND
f.date &gt;= GREATEST(?startDate, cm.joindate, c.creationDate) AND
f.date &lt;= ?endDate
ORDER BY f.DATE ASC
">
</asp:SqlDataSource>
    <asp:GridView ID="gvFlyingReport" AutoGenerateColumns="false" AlternatingRowStyle-CssClass="logbookAlternateRow" ShowFooter="false" CellPadding="4" GridLines="None" runat="server">
        <Columns>
            <asp:BoundField DataField="Date" DataFormatString="{0:d}" HeaderText="<%$ Resources:Club, ReportHeaderDate %>" />
            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderMonth %>">
                <ItemTemplate>
                    <asp:Label ID="lblMonth" runat="server" Text='<%# MonthForDate(Convert.ToDateTime(Eval("Date"))) %>'></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="Aircraft" HeaderText="<%$ Resources:Club, ReportHeaderAircraft %>"  />
            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderPilotName %>" >
                <ItemTemplate>
                    <asp:Label ID="lblName" runat="server" Text='<%# FullName(Eval("Firstname").ToString(), Eval("Lastname").ToString(), Eval("Email").ToString()) %>'></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="Route" HeaderText="<%$ Resources:Club, ReportHeaderRoute %>" />
            <asp:BoundField DataField="Total Time" HeaderText="<%$ Resources:Club, ReportHeaderTotalTime %>" DataFormatString="{0:0.0#}" />
            <asp:BoundField DataField="Hobbs Start" HeaderText="<%$ Resources:Club, ReportHeaderHobbsStart %>" />
            <asp:BoundField DataField="Hobbs End" HeaderText="<%$ Resources:Club, ReportHeaderHobbsEnd %>" />
            <asp:BoundField DataField="Total Hobbs" HeaderText="<%$ Resources:Club, ReportHeaderTotalHobbs %>" DataFormatString="{0:0.0#}" />
            <asp:BoundField DataField="Tach Start" HeaderText="<%$ Resources:Club, ReportHeaderTachStart %>" />
            <asp:BoundField DataField="Tach End" HeaderText="<%$ Resources:Club, ReportHeaderTachEnd %>" />
            <asp:BoundField DataField="Total Tach" HeaderText="<%$ Resources:Club, ReportHeaderTotalTach %>" DataFormatString="{0:0.0#}" />
            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderFlightStart %>" >
                <ItemTemplate>
                    <asp:Label ID="lblFlightStart" runat="server" Text='<%# FormattedUTCDate(Eval("Flight Start")) %>'></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderFlightEnd %>" >
                <ItemTemplate>
                    <asp:Label ID="lblflightEnd" runat="server" Text='<%# FormattedUTCDate(Eval("Flight End")) %>'></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="Total Flight" HeaderText="<%$ Resources:Club, ReportHeaderTotalFlight %>" DataFormatString="{0:0.0#}"  />
            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderEngineStart %>" >
                <ItemTemplate>
                    <asp:Label ID="lblEngineStart" runat="server" Text='<%# FormattedUTCDate(Eval("Engine Start")) %>'></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderEngineEnd %>" >
                <ItemTemplate>
                    <asp:Label ID="lblEngineEnd" runat="server" Text='<%# FormattedUTCDate(Eval("Engine End")) %>'></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="Total Engine" HeaderText="<%$ Resources:Club, ReportHeaderTotalEngine %>" DataFormatString="{0:0.0#}" />
            <asp:BoundField DataField="Oil Added" HeaderText="<%$ Resources:Club, ReportHeaderOilAdded %>" DataFormatString="{0:0.0#}" />
            <asp:BoundField DataField="Oil Level" HeaderText="<%$ Resources:Club, ReportHeaderOilLevel %>" DataFormatString="{0:0.0#}" />
            <asp:BoundField DataField="Fuel Added" HeaderText="<%$ Resources:Club, ReportHeaderFuelAdded %>" DataFormatString="{0:0.0#}" />
            <asp:BoundField DataField="Fuel Cost" HeaderText="<%$ Resources:Club, ReportHeaderFuelCost %>" DataFormatString="{0:C}" />
        </Columns>
        <EmptyDataTemplate>
            <p><% =Resources.Club.ReportNoData %></p>
        </EmptyDataTemplate>
    </asp:GridView>
