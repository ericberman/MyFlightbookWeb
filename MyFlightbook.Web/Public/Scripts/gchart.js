/******************************************************
 * 
 * Copyright (c) 2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

// from https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/eval - to enable safer running of the clickhandler
function drawGChart(chartdata) {
    var data = new google.visualization.DataTable();
    data.addColumn(chartdata.XDataType, chartdata.XLabel);
    data.addColumn(chartdata.YDataType, chartdata.YLabel);
    if (chartdata.HasTooltips > 0)
        data.addColumn({ type: 'string', role: 'tooltip', 'p': { 'html': true } });
    if (chartdata.HasY2)
        data.addColumn(chartdata.Y2DataType, chartdata.Y2Label);
    if (chartdata.showaverage)
        data.addColumn('number', chartdata.AverageLabel);

    data.addRows(chartdata.Data);

    if (chartdata.XDataType == 'date' || chartdata.XDataType == 'datetime') {
        // format first column
        var format = new google.visualization.DateFormat({ pattern: chartdata.XFormatString });
        format.format(data, 0);
    }

    var options = {
        chartArea: { left: 80, top: 10, width: '80%', height: '70%' },
        backgroundColor: 'transparent',
        width: chartdata.Width,
        height: chartdata.Height,
        title: chartdata.Title,
        fontSize: 10,
        fontName: 'Arial',
        lineWidth: 1,
        pointSize: 2,
        bar: { groupWidth: '95%' },
        legend: { position: chartdata.LegendType },
        hAxis: {
            title: chartdata.XLabel, titleTextStyle: { color: 'black', fontName: 'Arial', fontSize: 10 },
            slantedTextAngle: chartdata.SlantAngle,
            slantedText: (chartdata.SlantAngle > 0),
            format: chartdata.XFormatString,
            showTextEvery: chartdata.TickSpacing
        }
    }

    if (chartdata.HasTooltips) {
        options.tooltip = { isHtml: true };
    }

    if (chartdata.HasY2) {
        options.vAxes = [{ title: chartdata.YLabel }, { title: chartdata.Y2Label }];
        options.series = {
            0: { targetAxisIndex: 0 },
            1: { targetAxisIndex: 1, type: chartdata.Chart2Type },
            2: { targetAxisIndex: 0, type: 'line' }
        };
    }
    else {
        options.vAxes = [{ title: chartdata.YLabel }];
        options.series = {
            0: { targetAxisIndex: 0 },
            1: { type: 'line' }
        };
    }

    var oneDay = (24 * 60 * 60 * 1000);
    var dateRange = data.getColumnRange(0);
    if (data.getNumberOfRows() === 1) {
        dateRange.min = new Date(dateRange.min.getTime() - oneDay);
        dateRange.max = new Date(dateRange.max.getTime() + oneDay);
        options["hAxis"]["viewWindow"] = dateRange;
    }

    var container = document.getElementById(chartdata.ContainerID);
    var chart;
    switch (chartdata.ChartType) {
        case "LineChart":
            chart = new google.visualization.LineChart(container);
            break;
        case "ColumnChart":
            chart = new google.visualization.ColumnChart(container);
            break;
        case "ComboChart":
            chart = new google.visualization.ComboChart(container);
            break;
    }
    google.visualization.events.removeAllListeners(chart);

    google.visualization.events.addListener(chart, 'select', function () {
        var selectedItem = chart.getSelection()[0];
        if (selectedItem) {
            var value = data.getValue(selectedItem.row, selectedItem.column);
            var xvalue = data.getValue(selectedItem.row, 0);
            if (chartdata.ClickHandlerFunction)
                chartdata.ClickHandlerFunction(selectedItem.row, selectedItem.column, xvalue, value);
        }
    });
    chart.draw(data, options);
};

