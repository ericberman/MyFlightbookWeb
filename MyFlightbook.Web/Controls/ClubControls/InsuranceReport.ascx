<%@ Control Language="C#" AutoEventWireup="true" Codebehind="InsuranceReport.ascx.cs" Inherits="Controls_ClubControls_InsuranceReport" %>
<asp:GridView ID="gvInsuranceReport" runat="server" AutoGenerateColumns="false" AlternatingRowStyle-CssClass="logbookAlternateRow" ShowFooter="false" CellPadding="4" GridLines="None" OnRowDataBound="gvInsuranceReport_RowDataBound">
    <Columns>
        <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderPilotName %>" ItemStyle-VerticalAlign="Top" >
            <ItemTemplate>
                <asp:Label ID="lblName" runat="server" Font-Bold="true" Text='<%# ((MyFlightbook.Clubs.ClubInsuranceReportItem) Container.DataItem).User.UserFullName %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderInsurancePilotStatus %>" ItemStyle-VerticalAlign="Top" >
            <ItemTemplate>
                <asp:Repeater ID="rptPilotStatus" runat="server">
                    <ItemTemplate>
                        <div><asp:Label ID="lblTitle" runat="server" CssClass="currencylabel" Text='<%# Eval("Attribute") %>'></asp:Label>: <asp:Label ID="lblStatus" runat="server" CssClass='<%# CSSForItem((MyFlightbook.Currency.CurrencyState) Eval("Status")) %>' Text='<%# Eval("Value") %>'></asp:Label></div>
                    </ItemTemplate>
                </asp:Repeater>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField HeaderText="<%$ Resources:Club, ReportHeaderInsuranceFlightsInPeriod %>" ItemStyle-Width="1.5cm" DataField="FlightsInInterval" ItemStyle-VerticalAlign="Top"  />
        <asp:BoundField HeaderText="<%$ Resources:Club, ReportHeaderInsuranceLastFlightInClubPlane %>" ItemStyle-Width="2cm" DataField="MostRecentFlight" DataFormatString="{0:d}" ItemStyle-VerticalAlign="Top"  />
        <asp:BoundField HeaderText="<%$ Resources:Club, ReportHeaderInsuranceTotalTime %>" DataField="TotalTime" ItemStyle-Width="1.5cm" DataFormatString="{0:#,##0.0}" ItemStyle-VerticalAlign="Top" />
        <asp:BoundField HeaderText="<%$ Resources:Club, ReportheaderInsuranceComplexTime %>" DataField="ComplexTime" ItemStyle-Width="1.5cm" DataFormatString="{0:#,##0.0}" ItemStyle-VerticalAlign="Top" />
        <asp:BoundField HeaderText="<%$ Resources:Club, ReportheaderInsuranceHighPerformance %>" DataField="HighPerformanceTime" ItemStyle-Width="1.5cm" DataFormatString="{0:#,##0.0}" ItemStyle-VerticalAlign="Top" />
        <asp:TemplateField HeaderText="<%$ Resources:Club, ReportheaderInsuranceTimeInClubAircraft %>" ItemStyle-VerticalAlign="Top" >
            <ItemTemplate>
                <asp:GridView ID="gvAircraftTime" runat="server" AutoGenerateColumns="false" ShowHeader="false" ShowFooter="false" GridLines="None" CellPadding="4">
                    <Columns>
                        <asp:BoundField DataField="key" ItemStyle-Font-Bold="true" />
                        <asp:BoundField DataField="value" DataFormatString="{0:#,##0.0}" />
                    </Columns>
                </asp:GridView>
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
</asp:GridView>