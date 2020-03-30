<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbChartTotals.ascx.cs" Inherits="Controls_mfbChartTotals" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="GoogleChart.ascx" tagname="GoogleChart" tagprefix="uc3" %>
<asp:Panel ID="Panel1" runat="server" style="padding:5px;">
    <asp:Localize ID="locTotalsHeader" runat="server" Text="<%$ Resources:LocalizedText, ChartTotalsHeader %>" />
    <asp:DropDownList ID="cmbFieldToView" DataTextField="DataName" DataValueField="DataField" runat="server" AutoPostBack="True" onselectedindexchanged="cmbFieldToview_SelectedIndexChanged" >
    </asp:DropDownList>
    <asp:Label ID="lblGroupBy" runat="server" Text="<%$ Resources:LocalizedText, ChartTotalsGroupPrompt %>"></asp:Label>
    <asp:DropDownList ID="cmbGrouping" runat="server" AutoPostBack="true" DataTextField="DisplayName" DataValueField="DisplayName" OnSelectedIndexChanged="cmbGrouping_SelectedIndexChanged">
    </asp:DropDownList>
    <asp:CheckBox ID="ckIncludeAverage" runat="server" Text="<%$ Resources:LocalizedText, AnalysisShowAverages %>" OnCheckedChanged="ckIncludeAverage_CheckedChanged" AutoPostBack="true" />
    <div style="float:right">
        <asp:LinkButton ID="lnkDownloadCSV" runat="server" OnClick="lnkDownloadCSV_Click" style="vertical-align:middle">
            <asp:Image ID="imgDownloadCSV" ImageUrl="~/images/download.png" runat="server" style="padding-right: 5px; vertical-align:middle" />
            <asp:Image ID="imgCSVIcon" ImageAlign="Middle" runat="server" ImageUrl="~/images/csvicon_sm.png" style="padding-right: 5px; vertical-align:middle;" />
            <span style="vertical-align:middle"><asp:Localize ID="locDownloadCSV" runat="server" Text="<%$ Resources:LocalizedText, DownloadFlyingStats %>"></asp:Localize></span>
        </asp:LinkButton>
    </div>
    <asp:Panel ID="pnlChart" runat="server">
        <uc3:GoogleChart ID="gcTrends" Width="750" Height="340" AverageFormatString="<%$ Resources:LocalizedText, AnalysisAverageFormatString %>" ChartType="ColumnChart" Chart2Type="line" SlantAngle="90" LegendType="bottom" XDataType="date" YDataType="number" Y2DataType="number" runat="server" />
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
        <asp:Panel ID="pnlRawData" runat="server" Height="0px" ScrollBars="Horizontal" >
            <asp:GridView ID="gvRawData" runat="server" AutoGenerateColumns="False" CellPadding="3">
                <Columns>
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
