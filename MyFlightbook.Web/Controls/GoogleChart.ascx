<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_GoogleChart" Codebehind="GoogleChart.ascx.cs" %>
<asp:Panel ID="pnlChart" runat="server">
</asp:Panel>
<script>
      var chart<%=ID %>;
      var data<%=ID %>;
      function drawChart<%=ID %>() {
        var data = new google.visualization.DataTable();
        data<%=ID %> = data;
        data.addColumn('<%=XDataTypeString %>', '<%=XLabel %>');
        data.addColumn('<%=YDataTypeString %>', '<%=YLabel %>');
        <% if (HasY2) { %>
            data.addColumn('<%=Y2DataTypeString %>', '<% =Y2Label %>');
        <% } %>
        data.addRows([
            <%= Data %>
        ]);

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
          legend: {position: '<% =LegendType %>' },
          <% if (HasY2) { %>
            vAxes: [{title: '<%= YLabel %>' }, {title: '<%= Y2Label %>'}],
            series:{0:{targetAxisIndex:0}, 1:{targetAxisIndex:1, type:'<%=Chart2TypeString %>'}},
          <% } else { %>
            vAxis: {title: '<%=YLabel %>'},
          <% } %>
          hAxis: {title: '<%=XLabel %>', 
            titleTextStyle: { color: 'black', fontName: 'Arial', fontSize: 10},
            slantedTextAngle: <%=SlantAngle %>, 
            slantedText: <%=UseSlantedTextString %>, 
            format: '<%=XDataFormat %>', 
            showTextEvery: <%=TickSpacing %>}
        };

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
