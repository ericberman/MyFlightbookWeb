<%@ Control Language="C#" AutoEventWireup="true" Codebehind="FlyingReport.ascx.cs" Inherits="MyFlightbook.Clubs.ClubControls.FlyingReport" %>
<asp:SqlDataSource ID="sqlDSReports" runat="server" 
ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" >
</asp:SqlDataSource>
    <asp:GridView ID="gvFlyingReport" AutoGenerateColumns="false" ShowFooter="false" CellPadding="4" GridLines="None" runat="server">
        <Columns>
            <asp:BoundField DataField="Date" DataFormatString="{0:d}" HeaderText="<%$ Resources:Club, ReportHeaderDate %>" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderMonth %>">
                <ItemStyle CssClass="PaddedCell" />
                <HeaderStyle CssClass="PaddedCell" />
                <ItemTemplate>
                    <asp:Label ID="lblMonth" runat="server" Text='<%# MonthForDate(Convert.ToDateTime(Eval("Date"))) %>'></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="Aircraft" HeaderText="<%$ Resources:Club, ReportHeaderAircraft %>" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderPilotName %>" >
                <ItemStyle CssClass="PaddedCell" />
                <HeaderStyle CssClass="PaddedCell" />
                <ItemTemplate>
                    <asp:Label ID="lblName" runat="server" Text='<%# FullName(Eval("Firstname").ToString(), Eval("Lastname").ToString(), Eval("Email").ToString()) %>'></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="IsInstruction" HeaderText="<%$ Resources:Club, ReportHeaderInstruction %>" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:BoundField DataField="FlightRules" HeaderText="<%$ Resources:Club, ReportHeaderFlightRules %>" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" /> 
            <asp:BoundField DataField="Route" HeaderText="<%$ Resources:Club, ReportHeaderRoute %>" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:BoundField DataField="Total Time" HeaderText="<%$ Resources:Club, ReportHeaderTotalTime %>" DataFormatString="{0:0.0#}" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:BoundField DataField="Hobbs Start" HeaderText="<%$ Resources:Club, ReportHeaderHobbsStart %>" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:BoundField DataField="Hobbs End" HeaderText="<%$ Resources:Club, ReportHeaderHobbsEnd %>" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:BoundField DataField="Total Hobbs" HeaderText="<%$ Resources:Club, ReportHeaderTotalHobbs %>" DataFormatString="{0:0.0#}" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:BoundField DataField="Tach Start" HeaderText="<%$ Resources:Club, ReportHeaderTachStart %>" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:BoundField DataField="Tach End" HeaderText="<%$ Resources:Club, ReportHeaderTachEnd %>" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:BoundField DataField="Total Tach" HeaderText="<%$ Resources:Club, ReportHeaderTotalTach %>" DataFormatString="{0:0.0#}" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderFlightStart %>" >
                <ItemStyle CssClass="PaddedCell" />
                <HeaderStyle CssClass="PaddedCell" />
                <ItemTemplate>
                    <asp:Label ID="lblFlightStart" runat="server" Text='<%# FormattedUTCDate(Eval("Flight Start")) %>'></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderFlightEnd %>" >
                <ItemStyle CssClass="PaddedCell" />
                <HeaderStyle CssClass="PaddedCell" />
                <ItemTemplate>
                    <asp:Label ID="lblflightEnd" runat="server" Text='<%# FormattedUTCDate(Eval("Flight End")) %>'></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="Total Flight" HeaderText="<%$ Resources:Club, ReportHeaderTotalFlight %>" DataFormatString="{0:0.0#}"  HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderEngineStart %>" >
                <ItemStyle CssClass="PaddedCell" />
                <HeaderStyle CssClass="PaddedCell" />
                <ItemTemplate>
                    <asp:Label ID="lblEngineStart" runat="server" Text='<%# FormattedUTCDate(Eval("Engine Start")) %>'></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderEngineEnd %>" >
                <ItemStyle CssClass="PaddedCell" />
                <HeaderStyle CssClass="PaddedCell" />
                <ItemTemplate>
                    <asp:Label ID="lblEngineEnd" runat="server" Text='<%# FormattedUTCDate(Eval("Engine End")) %>'></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="Total Engine" HeaderText="<%$ Resources:Club, ReportHeaderTotalEngine %>" DataFormatString="{0:0.0#}" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:BoundField DataField="Oil Added" HeaderText="<%$ Resources:Club, ReportHeaderOilAdded %>" DataFormatString="{0:0.0#}" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:BoundField DataField="Oil Added 2nd" HeaderText="<%$ Resources:Club, ReportHeaderOilAdded2ndEngine %>" DataFormatString="{0:0.0#}" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:BoundField DataField="Oil Level" HeaderText="<%$ Resources:Club, ReportHeaderOilLevel %>" DataFormatString="{0:0.0#}" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:BoundField DataField="Fuel Added" HeaderText="<%$ Resources:Club, ReportHeaderFuelAdded %>" DataFormatString="{0:0.0#}" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:BoundField DataField="Fuel Remaining" HeaderText="<%$ Resources:Club, ReportHeaderFuelRemaining %>" DataFormatString="{0:0.0#}" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
            <asp:BoundField DataField="Fuel Cost" HeaderText="<%$ Resources:Club, ReportHeaderFuelCost %>" DataFormatString="{0:C}" HeaderStyle-CssClass="PaddedCell" ItemStyle-CssClass="PaddedCell" />
        </Columns>
        <EmptyDataTemplate>
            <p><% =Resources.Club.ReportNoData %></p>
        </EmptyDataTemplate>
    </asp:GridView>
