<%@ Control Language="C#" AutoEventWireup="true" Codebehind="MaintenanceReport.ascx.cs" Inherits="Controls_ClubControls_MaintenanceReport" %>
<asp:GridView ID="gvMaintenance" AutoGenerateColumns="false" ShowFooter="false" CellPadding="4" GridLines="None" AlternatingRowStyle-CssClass="logbookAlternateRow" runat="server">
    <Columns>
        <asp:BoundField DataField="DisplayTailnumber" ItemStyle-Font-Bold="true" HeaderText="<%$ Resources:Aircraft, AircraftHeader %>" ItemStyle-VerticalAlign="Top" />
        <asp:TemplateField HeaderText="<%$ Resources:Club, ClubAircraftTime %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblLastAnnual" runat="server" Text='<%# ValueString (Eval("HighWater")) %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceAnnual %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblLastAnnual" runat="server" Text='<%# ValueString (Eval("LastAnnual")) %>' /></ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceAnnualDue %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblAnnualDue" runat="server" CssClass='<%# CSSForDate((DateTime) Eval("Maintenance.NextAnnual")) %>' Text='<%# ValueString (Eval("Maintenance.NextAnnual")) %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceTransponder %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblLastXponder" runat="server" Text='<%# ValueString (Eval("LastTransponder")) %>' /></ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceTransponderDue %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblXPonderDue" runat="server" CssClass='<%# CSSForDate((DateTime) Eval("Maintenance.NextTransponder")) %>' Text='<%# ValueString (Eval("Maintenance.NextTransponder")) %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenancePitotStatic %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblLastPitot" runat="server" Text='<%# ValueString (Eval("LastStatic")) %>' /></ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenancePitotStaticDue %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblPitotDue" runat="server" CssClass='<%# CSSForDate((DateTime) Eval("Maintenance.NextStatic")) %>' Text='<%# ValueString (Eval("Maintenance.NextStatic")) %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceAltimeter %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblLastAltimeter" runat="server" Text='<%# ValueString (Eval("LastAltimeter")) %>' /></ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceAltimeterDue %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblAltimeterDue" runat="server" CssClass='<%# CSSForDate((DateTime) Eval("Maintenance.NextAltimeter")) %>' Text='<%# ValueString (Eval("Maintenance.NextAltimeter")) %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceELT %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblLastELT" runat="server" Text='<%# ValueString (Eval("LastELT")) %>' /></ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceELTDue %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblELTDue" runat="server" CssClass='<%# CSSForDate((DateTime) Eval("Maintenance.NextELT")) %>' Text='<%# ValueString (Eval("Maintenance.NextELT")) %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceVOR %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblLastVOR" runat="server" Text='<%# ValueString (Eval("LastVOR")) %>' /></ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceVORDue %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblVORDue" runat="server" CssClass='<%# CSSForDate((DateTime) Eval("Maintenance.NextVOR")) %>' Text='<%# ValueString (Eval("Maintenance.NextVOR")) %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, Maintenance100 %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblLast100" runat="server" Text='<%# ValueString (Eval("Last100")) %>' /></ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, Maintenance100Due %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblNext100" runat="server" CssClass='<%# CSSForValue((decimal) Eval("HighWater"), (decimal) Eval("Maintenance.Next100"), 10) %>' Text='<%# ValueString (Eval("Maintenance.Next100")) %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceOil %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblLastOil" runat="server" Text='<%# ValueString (Eval("LastOilChange")) %>' /></ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceOilDue25 %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblOil25" runat="server" CssClass='<%# CSSForValue((decimal) Eval("HighWater"), (decimal) Eval("LastOilChange"), 5, 25) %>' Text='<%# ValueString (Eval("LastOilChange"), 25) %>' /></ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceOilDue50 %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblOil50" runat="server" CssClass='<%# CSSForValue((decimal) Eval("HighWater"), (decimal) Eval("LastOilChange"), 10, 50) %>' Text='<%# ValueString (Eval("LastOilChange"), 50) %>' /></ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceOilDue100 %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblOil100" runat="server" CssClass='<%# CSSForValue((decimal) Eval("HighWater"), (decimal) Eval("LastOilChange"), 15, 100) %>' Text='<%# ValueString (Eval("LastOilChange"), 100) %>' /></ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceEngine %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblLastEngine" runat="server" Text='<%# ValueString (Eval("LastNewEngine")) %>' /></ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceRegistration %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblRegistration" runat="server" CssClass='<%# CSSForDate((DateTime) Eval("RegistrationDue")) %>' Text='<%# ValueString (Eval("RegistrationDue")) %>' /></ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Currency, deadlinesHeaderDeadlines %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblDeadlines" runat="server" Text='<%# MyFlightbook.Currency.DeadlineCurrency.CoalescedDeadlinesForAircraft(null, (int) Eval("AircraftID")) %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderNotes %>" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblNotes" runat="server" Text='<%# Eval("PublicNotes") %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
    <EmptyDataTemplate>
        <p><% =Resources.Club.ReportNoData %></p>
    </EmptyDataTemplate>
</asp:GridView>
