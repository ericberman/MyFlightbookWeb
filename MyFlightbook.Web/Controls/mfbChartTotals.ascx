<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbChartTotals" Codebehind="mfbChartTotals.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="GoogleChart.ascx" tagname="GoogleChart" tagprefix="uc3" %>
<asp:Panel ID="Panel1" runat="server" style="padding:5px;">
    <asp:Localize ID="locTotalsHeader" runat="server" Text="<%$ Resources:LocalizedText, ChartTotalsHeader %>" />
    <asp:DropDownList ID="cmbFieldToview" runat="server" AutoPostBack="True" 
        onselectedindexchanged="cmbFieldToview_SelectedIndexChanged" >
        <asp:ListItem Value="TotalFlightTime" Selected="True" Text="<%$ Resources:LocalizedText, ChartTotalsTotalFlyingTime %>"></asp:ListItem>
        <asp:ListItem Value="Landings" Text="<%$ Resources:LocalizedText, ChartTotalsTotalLandings %>"></asp:ListItem>
        <asp:ListItem Value="Approaches" Text="<%$ Resources:LocalizedText, ChartTotalsTotalApproaches %>"></asp:ListItem>
        <asp:ListItem Value="Night" Text="<%$ Resources:LocalizedText, ChartTotalsTotalNight %>">Night</asp:ListItem >
        <asp:ListItem Value="SimulatedIMC" Text="<%$ Resources:LocalizedText, ChartTotalsTotalSimIMC %>"></asp:ListItem>
        <asp:ListItem Value="IMC" Text="<%$ Resources:LocalizedText, ChartTotalsTotalIMC %>"></asp:ListItem>
        <asp:ListItem Value="XC" Text="<%$ Resources:LocalizedText, ChartTotalsXC %>"></asp:ListItem>
        <asp:ListItem Value="Dual" Text="<%$ Resources:LocalizedText, ChartTotalsTotalDual %>"></asp:ListItem>
        <asp:ListItem Value="GroundSim" Text="<%$ Resources:LocalizedText, ChartTotalsGroundSim %>"></asp:ListItem>
        <asp:ListItem Value="PIC" Text="<%$ Resources:LocalizedText, ChartTotalsTotalPIC %>"></asp:ListItem>
        <asp:ListItem Value="CFI" Text="<%$ Resources:LocalizedText, ChartTotalsTotalCFI %>"></asp:ListItem>
        <asp:ListItem Value="SIC" Text="<%$ Resources:LocalizedText, ChartTotalsTotalSIC %>"></asp:ListItem>
        <asp:ListItem Value="Flights" Text="<%$ Resources:LocalizedText, ChartTotalsTotalFlights %>"></asp:ListItem>
        <asp:ListItem Value="FlightDays" Text="<%$ Resources:LocalizedText, ChartTotalsTotalFlightDays %>"></asp:ListItem>
    </asp:DropDownList>
    <asp:Label ID="lblGroupBy" runat="server" Text="<%$ Resources:LocalizedText, ChartTotalsGroupPrompt %>"></asp:Label>
    <asp:DropDownList ID="cmbGrouping" runat="server" AutoPostBack="true" OnSelectedIndexChanged="cmbGrouping_SelectedIndexChanged">
        <asp:ListItem Value="Year" Text="<%$ Resources:LocalizedText, ChartTotalsGroupYear %>"></asp:ListItem>
        <asp:ListItem Selected="True" Value="Month" Text="<%$ Resources:LocalizedText, ChartTotalsGroupMonth %>"></asp:ListItem>
        <asp:ListItem Value="Week" Text="<%$ Resources:LocalizedText, ChartTotalsGroupWeek %>"></asp:ListItem>
        <asp:ListItem Value="Day" Text="<%$ Resources:LocalizedText, ChartTotalsGroupDay %>"></asp:ListItem>
    </asp:DropDownList>
    <asp:Panel ID="pnlChart" runat="server">
        <uc3:GoogleChart ID="gcTrends" Width="750" Height="340" ChartType="ColumnChart" Chart2Type="line" SlantAngle="90" LegendType="bottom" XDataType="date" YDataType="number" Y2DataType="number" runat="server" />
        <p>
            <asp:Literal ID="Literal2" runat="server" 
                Text="<%$ Resources:LocalizedText, ChartTotalsMouseHint1 %>" />
            <span style="font-weight:bold; text-decoration:underline;">
            <asp:Label ID="lblShowData" runat="server" 
                Text="<%$ Resources:LocalizedText, ChartTotalsMouseHintClickHere %>"></asp:Label>
            </span>
            <asp:Literal ID="Literal3" runat="server" 
                Text="<%$ Resources:LocalizedText, ChartTotalsMouseHint2 %>" />
        </p>
        <asp:Panel ID="pnlRawData" runat="server" Height="0px" style="overflow:hidden;">
            <asp:GridView ID="gvRawData" runat="server" AutoGenerateColumns="False" CellPadding="3"
                OnRowDataBound="gvRawData_RowDataBound" EnableModelValidation="True">
                <Columns>
                    <asp:BoundField HeaderText="<%$ Resources:LocalizedText, ChartDataPeriod %>" DataField="DisplayName" />
                    <asp:TemplateField HeaderText="<%$ Resources:LocalizedText, ChartDataTotal %>">
                        <ItemTemplate>
                            <asp:HyperLink runat="server" Target="_blank" ID="lnkValue"></asp:HyperLink>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="RunningTotal" HeaderText="<%$ Resources:LocalizedText, ChartDataRunningTotal %>" />
                </Columns>
            </asp:GridView>
        </asp:Panel>
        <cc1:CollapsiblePanelExtender ID="CollapsiblePanelExtender1" runat="server" 
            CollapseControlID="lblShowData" SuppressPostBack="True" ExpandedText="here" 
            CollapsedText="here" ExpandControlID="lblShowData" TargetControlID="pnlRawData" 
            Collapsed="True">
        </cc1:CollapsiblePanelExtender>
    </asp:Panel>
</asp:Panel>
