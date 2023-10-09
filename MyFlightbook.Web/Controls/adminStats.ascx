<%@ Control Language="C#" AutoEventWireup="true" Codebehind="adminStats.ascx.cs" Inherits="MyFlightbook.Web.Admin.adminStats" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
    <%@ Register src="GoogleChart.ascx" tagname="GoogleChart" tagprefix="uc1" %>
    <h2>Users:</h2>
    <asp:GridView ID="gvUserStats" runat="server" CellPadding="3" AutoGenerateColumns="false" CssClass="stickyHeaderTable">
        <Columns>
            <asp:BoundField HeaderText="# Users" DataField="Users" DataFormatString="{0:#,##0}" />
            <asp:BoundField HeaderText="Month To Date Users" DataField="UsersMonthToDate" DataFormatString="{0:#,##0}" />
            <asp:BoundField HeaderText="Email Subscriptions" DataField="EmailSubscriptions" DataFormatString="{0:#,##0}" />
            <asp:BoundField HeaderText="Blacklists" DataField="PropertyBlacklists" DataFormatString="{0:#,##0}" />
            <asp:BoundField HeaderText="Dropbox Users" DataField="DropboxUsers" DataFormatString="{0:#,##0}" />
            <asp:BoundField HeaderText="GDrive Users" DataField="GDriveUsers" DataFormatString="{0:#,##0}" />
            <asp:BoundField HeaderText="1Drive Users" DataField="OneDriveUsers" DataFormatString="{0:#,##0}" />
            <asp:BoundField HeaderText="Multiple Cloud Users" DataField="CloudStorageUsers" DataFormatString="{0:#,##0}" />
            <asp:BoundField HeaderText="DefaultCloud Users" DataField="DefaultedCloudUsers" DataFormatString="{0:#,##0}" />
        </Columns>
    </asp:GridView>
    <asp:GridView ID="gvOAuthAndPass" runat="server" CellPadding="3" AutoGenerateColumns="false" CssClass="stickyHeaderTable">
        <Columns>
            <asp:BoundField HeaderText="# Nonce" DataField="NonceCount" />
            <asp:BoundField HeaderText="# oAuth Accounts" DataField="OAuthAccounts" DataFormatString="{0:#,##0}" />
            <asp:BoundField HeaderText="# Password Resets" DataField="PasswordResets" DataFormatString="{0:#,##0}" />
        </Columns>
    </asp:GridView>
    <div>
        <asp:Button ID="btnTrimAuthenticate" runat="server" Text="Trim all but latest authuser/expired tokens" onclick="btnTrimAuthenticate_Click" />
        <asp:Button ID="btnTrimOAuth" runat="server" OnClick="btnTrimOAuth_Click" Text="Trim old oAuth authentications / password resets" />
    </div>
    <div><asp:Label ID="lblTrimErr" runat="server" EnableViewState="false" Font-Bold="true" /></div>
    <h2>Usage:</h2>
    <asp:GridView ID="gvMiscStats" runat="server" AutoGenerateColumns="false" CssClass="stickyHeaderTable">
        <Columns>
            <asp:BoundField HeaderText="# Students" DataField="Students" DataFormatString="{0:#,##0}" />
            <asp:BoundField HeaderText="# Pub Notes" DataField="PublicNotes" DataFormatString="{0:#,##0}" />
            <asp:BoundField HeaderText="# Private Notes" DataField="PrivateNotes" DataFormatString="{0:#,##0}" />
            <asp:BoundField HeaderText="# Linked videos" DataField="EmbeddedVideos" DataFormatString="{0:#,##0}" />
            <asp:BoundField HeaderText="# AWS Videos" DataField="AWSVideos" DataFormatString="{0:#,##0}" />
            <asp:BoundField HeaderText="# Clubs" DataField="Clubs" DataFormatString="{0:#,##0}" />
        </Columns>
    </asp:GridView>
    <table>
        <tr style="vertical-align:top;">
            <td>
                <asp:GridView ID="gvUserSources" runat="server" AutoGenerateColumns="false" CssClass="stickyHeaderTable">
                    <Columns>
                        <asp:BoundField HeaderText="Source Key" DataField="SourceKey" />
                        <asp:BoundField HeaderText="# Users" ItemStyle-HorizontalAlign="Right" DataField="NumUsers" DataFormatString="{0:#,##0}" />
                    </Columns>
                </asp:GridView>
            </td>
            <td>
                <asp:GridView ID="gvWSEvents" runat="server" AutoGenerateColumns="false" CssClass="stickyHeaderTable">
                    <Columns>
                        <asp:BoundField HeaderText="Event Type" DataField="EventType" />
                        <asp:BoundField HeaderText="# Events" DataField="EventCount" ItemStyle-HorizontalAlign="Right" DataFormatString="{0:#,##0}" />
                    </Columns>
                </asp:GridView>
            </td>
        </tr>
        <tr style="vertical-align:top;">
            <td>
                <asp:GridView ID="gvPaymentsXActions" runat="server" AutoGenerateColumns="false" CssClass="stickyHeaderTable">
                    <Columns>
                        <asp:BoundField HeaderText="# Payments" DataField="NumPayments" ItemStyle-HorizontalAlign="Right" DataFormatString="{0:#,##0}" />
                        <asp:BoundField HeaderText="# Users" DataField="NumUsers" ItemStyle-HorizontalAlign="Right" DataFormatString="{0:#,##0}" />
                    </Columns>
                </asp:GridView>
            </td>
            <td>
                <asp:GridView ID="gvPaymentAmounts" runat="server" AutoGenerateColumns="false" CssClass="stickyHeaderTable">
                    <Columns>
                        <asp:BoundField HeaderText="# Xactions" DataField="NumTransactions" ItemStyle-HorizontalAlign="Right" DataFormatString="{0:#,##0}" />
                        <asp:BoundField HeaderText="Amount" DataField="TransactionValue" ItemStyle-HorizontalAlign="Right" DataFormatString="{0:C}" />
                    </Columns>
                </asp:GridView>
            </td>
        </tr>
    </table>
    <h2>Flights and Aircraft:</h2>
    <table>
        <tr style="vertical-align:top">
            <td>
                <asp:GridView ID="gvMisc" runat="server" AutoGenerateColumns="false" CssClass="stickyHeaderTable">
                    <Columns>
                        <asp:BoundField HeaderText="# Flights" DataField="FlightCount" DataFormatString="{0:#,##0}" />
                        <asp:BoundField HeaderText="# Flights w/Telemetry" DataField="TelemetryCount" DataFormatString="{0:#,##0}" />
                        <asp:BoundField HeaderText="# Models" DataField="ModelsCount" DataFormatString="{0:#,##0}" />
                        <asp:BoundField HeaderText="# User Airports" DataField="UserAirportCount" DataFormatString="{0:#,##0}" />
                        <asp:BoundField HeaderText="# WS Flights" DataField="WSCommittedFlights" DataFormatString="{0:#,##0}" />
                        <asp:BoundField HeaderText="# Imported Flights" DataField="ImportedFlights" DataFormatString="{0:#,##0}" />
                    </Columns>
                </asp:GridView>
            </td>
            <td>
                <asp:GridView ID="gvAircraft" runat="server" AutoGenerateColumns="false" CssClass="stickyHeaderTable">
                    <Columns>
                        <asp:BoundField HeaderText="Instance Type" DataField="InstanceType" />
                        <asp:BoundField HeaderText="# Aircraft" ItemStyle-HorizontalAlign="Right" DataField="NumAircraft" DataFormatString="{0:#,##0}" />
                    </Columns>
                </asp:GridView>
            </td>
        </tr>
    </table>
    <!-- Daily new users -->
    <h2>New Users: <asp:Label ID="lblShowUsersData" runat="server" Text="(Click to show)"></asp:Label></h2>
    <asp:Panel ID="pnlShowUSersData" runat="server" Height="0px" Style="overflow: hidden;">
        <h2>Daily new users:</h2>
        <asp:GridView ID="gvDailyUsers" runat="server" AutoGenerateColumns="False" CssClass="stickyHeaderTable">
            <Columns>
                <asp:BoundField DataField="DisplayPeriod" HeaderText="Month" ReadOnly="True" DataFormatString="{0:d}" />
                <asp:BoundField DataField="NewUsers" ItemStyle-HorizontalAlign="Right" HeaderText="New Users" DataFormatString="{0:#,##0}" />
                <asp:BoundField DataField="RunningTotal" ItemStyle-HorizontalAlign="Right" HeaderText="Running Total" DataFormatString="{0:#,##0}" SortExpression="RunningTotal" />
            </Columns>
        </asp:GridView>
        <h2>Monthly new users:</h2>
        <asp:GridView ID="gvMonthlyUsers" AllowSorting="true" runat="server" AutoGenerateColumns="false" CssClass="stickyHeaderTable">
            <Columns>
                <asp:BoundField DataField="DisplayPeriod" HeaderText="Month" DataFormatString="{0:yyyy-MMM}" />
                <asp:BoundField DataField="NewUsers" HeaderText="New Users" ItemStyle-HorizontalAlign="Right" DataFormatString="{0:#,##0}" />
                <asp:BoundField DataField="RunningTotal" HeaderText="Running Total" ItemStyle-HorizontalAlign="Right" DataFormatString="{0:#,##0}" />
            </Columns>
        </asp:GridView>
    </asp:Panel>
    <uc1:GoogleChart ID="gcNewUsers" XDataType="date" YDataType="number" Y2DataType="number" UseMonthYearDate="true" Title="Number of New Users" LegendType="bottom" TickSpacing="1" SlantAngle="0" XLabel="Year/Month" YLabel="New Users" Y2Label="Cumulative Users" ChartType="LineChart" runat="server" />
   <cc1:CollapsiblePanelExtender ID="CollapsiblePanelExtender1" runat="server" Enabled="True"
        TargetControlID="pnlShowUSersData" CollapsedText="Click to show user data" Collapsed="true"
        CollapseControlID="lblShowUsersData" ExpandControlID="lblShowUsersData" ExpandedText="Click to hide user data"
        TextLabelID="lblShowUsersData">
    </cc1:CollapsiblePanelExtender>

