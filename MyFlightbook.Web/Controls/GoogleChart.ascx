<%@ Control Language="C#" AutoEventWireup="true" Codebehind="GoogleChart.ascx.cs" Inherits="MyFlightbook.Charting.Controls_GoogleChart" %>
<asp:Panel ID="pnlChart" runat="server" style="display: inline-block; margin: 0 auto !important;">
</asp:Panel>
<script>
      var chart<%=ID %>;
      var data<%=ID %>;
    function drawChart<%=ID %>() {
        var data = new google.visualization.DataTable();
        data<%=ID %> = data;
        data.addColumn('<%=XDataTypeString %>', '<%=XLabel %>');
        data.addColumn('<%=YDataTypeString %>', '<%=YLabel %>');
        <% if (Tooltips.Any()) { %>
        data.addColumn({ type: 'string', role: 'tooltip', 'p': { 'html': true } });
        <% } 
        if (HasY2) { %>
        data.addColumn('<%=Y2DataTypeString %>', '<% =Y2Label %>');
        <% }
        if (ShowAverage) { %> 
        data.addColumn('<% =AverageDataTypeString %>', '<% =AverageLabel %>');
        <% } %>

        data.addRows([
            <%= Data %>
        ]);

        <% if (XDataType == MyFlightbook.Charting.GoogleColumnDataType.date || XDataType == MyFlightbook.Charting.GoogleColumnDataType.datetime) { %>
        // format first column
        var format = new google.visualization.DateFormat({
            pattern: "<% =XDataFormat %>"
        });
        format.format(data, 0);
        <% } %>

    var options = {
          chartArea:{left:80,top:10,width:'80%',height:'70%'},
          backgroundColor: 'transparent',
          width: <%= Width %>, height: <%= Height %>,
          title: '<% =Title %>',
          fontSize: 10,
          fontName: 'Arial',
          lineWidth: 1,
          pointSize: 2, 
          bar: {groupWidth:'95%'},
          legend: { position: '<% =LegendType %>' },
          <% if(Tooltips.Any()) { %>
                tooltip: { isHtml: true },
          <% } %>

          <% if (HasY2) { %>
            vAxes: [{title: '<%= YLabel %>' }, {title: '<%= Y2Label %>'}],
            series: { 0: { targetAxisIndex: 0 }, 1: { targetAxisIndex: 1, type: '<%=Chart2TypeString %>' }, 2: { targetAxisIndex: 0, type: 'line' } },
          <% } else { %>
            vAxis: { title: '<%=YLabel %>' },
            series: { 1: {type: 'line' }},
          <% } %>
          
          hAxis: {title: '<%=XLabel %>', 
            titleTextStyle: { color: 'black', fontName: 'Arial', fontSize: 10},
            slantedTextAngle: <%=SlantAngle %>, 
            slantedText: <%=UseSlantedTextString %>, 
            format: "<%=XDataFormat %>", 
            showTextEvery: <%=TickSpacing %>}
        };

        var oneDay = (24 * 60 * 60 * 1000);
        var dateRange = data.getColumnRange(0);
        if (data.getNumberOfRows() === 1) {
            dateRange.min = new Date(dateRange.min.getTime() - oneDay);
            dateRange.max = new Date(dateRange.max.getTime() + oneDay);
            options["hAxis"]["viewWindow"] = dateRange;
        }

        chart<%=ID %> = new google.visualization.<%=ChartTypeString %>(document.getElementById('<%=pnlChart.ClientID %>'));
        google.visualization.events.removeAllListeners(chart<%=ID %>);

        google.visualization.events.addListener(chart<%=ID %>, 'select', itemClicked);
        chart<%=ID %>.draw(data, options);
      }

    function itemClicked() {
        var selectedItem = chart<%=ID %>.getSelection()[0];
        if (selectedItem) {
            var value = data<%=ID %>.getValue(selectedItem.row, selectedItem.column);
            var xvalue = data<%=ID %>.getValue(selectedItem.row, 0);
            <% = ClickHandlerJS %>
        }
    }

chartsToDraw.push(drawChart<%=ID %>);
</script>
