<%@ Control Language="C#" AutoEventWireup="true" Codebehind="GoogleChart.ascx.cs" Inherits="MyFlightbook.Charting.Controls_GoogleChart" %>
<asp:Panel ID="pnlChart" runat="server" style="display: inline-block; margin: 0 auto !important;">
</asp:Panel>
<script>
    chartsToDraw.push(<% =ChartDataSerialized %>);
</script>
