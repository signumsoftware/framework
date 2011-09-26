var SF = SF || {};

SF.Chart = SF.Chart || {};

Array.prototype.enterData = function (data, tag, cssClass) {
    return this.selectAll(tag + "." + cssClass).data(data)
        .enter().append("svg:" + tag)
        .attr("class", cssClass);
};

SF.Chart.Factory = (function () {
    var br = "\n",
        brt = "\n\t";

    var getGraphType = function (key) {
        return new SF.Chart[key]();
    };

    var createChartSVG = function (selector) {
        return "var chart = d3.select('" + selector + "')" + this.brt +
            ".append('svg:svg').attr('width', width).attr('height', height)" + this.br + this.br;
    };

    return {
        br: br,
        brt: brt,
        getGraphType: getGraphType,
        createChartSVG: createChartSVG
    };
})();

SF.Chart.ChartBase = function () { };
SF.Chart.ChartBase.prototype = {

    br: SF.Chart.Factory.br,
    brt: SF.Chart.Factory.brt,
    fontSize: $('<span>&nbsp;</span>').appendTo($('body')).outerHeight(true),
    ticksLength: 4,
    labelMargin: 5,
    chartAxisPadding: 5,
    padding: 5,

    getGraphType: function (key) {
        return SF.Chart[key];
    },

    createChartSVG: function () {
        return "var chart = d3.select('.chart')" + this.brt +
            ".append('svg:svg').attr('width', width).attr('height', height);" + this.br + this.br;
    },

    paintXAxis: function () {
        return "//paint x-axis" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".append('svg:line')" + this.brt +
            ".attr('class', 'x-axis')" + this.brt +
            ".attr('x2', width - yAxisLeftPosition - padding);" + this.br + this.br;
    },

    paintYAxis: function () {
        return "//paint y-axis" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + padding + ')')" + this.brt +
            ".append('svg:line')" + this.brt +
            ".attr('class', 'y-axis')" + this.brt +
            ".attr('y2', xAxisTopPosition - padding);" + this.br + this.br;
    },

    defaultColorLegend: function () {
        return "//paint color legend" + this.br +
            "var legendScale = d3.scale.ordinal().domain($.map(data.series, function (s, i) { return i; })).rangeBands([0, width - yAxisLeftPosition - padding])," + this.brt +
            "legendRectWidth = 10," + this.brt +
            "legendLabelWidth = legendScale.rangeBand() - (2 * labelMargin) - legendRectWidth;" + this.br +
            this.br +
            "chart.append('svg:g').attr('class', 'color-legend').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + padding + ')')" + this.brt +
            ".enterData(data.series, 'rect', 'color-rect')" + this.brt +
            ".attr('x', function(e, i) { return (legendRectWidth + legendLabelWidth + (2 * labelMargin)) * i; })" + this.brt +
            ".attr('width', legendRectWidth).attr('height', fontSize)" + this.brt +
            ".attr('fill', function(s) { return color(JSON.stringify(s.token2)); });" + this.br +
            this.br +
            "chart.append('svg:g').attr('class', 'color-legend').attr('transform', 'translate(' + (yAxisLeftPosition + labelMargin + legendRectWidth) + ', ' + (padding + fontSize) + ')')" + this.brt +
            ".enterData(data.series, 'text', 'color-text')" + this.brt +
            ".attr('x', function(e, i) { return (legendRectWidth + legendLabelWidth + (2 * labelMargin)) * i; })" + this.brt +
            ".text(function(s) { return myChart.getTokenLabel(s.token2); });" + this.br + this.br;
    },

    paintLegend: function () {
        return "";
    },

    getPathPoints: function (points) {
        var result = "M";
        $.each(points, function (i, p) {
            result += " " + p.x + " " + p.y;
        });
        return result;
    },

    getTokenLabel: function (tokenValue) {
        return tokenValue.toStr || tokenValue;
    },

    getValuesInAllSeries: function (series, tokenName) {
        var result = [];
        for (var s = 0; s < series.length; s++) {
            for (var v = 0; v < series[s].values.length; v++) {
                var value = series[s].values[v];
                if (result.indexOf(value[tokenName]) < 0) {
                    result.push(value[tokenName]);
                }
            }
        }
        return result;
    },

    paintChart: function () {
        return this.init() +
        this.getXAxis() +
        this.getYAxis() +
        this.paintXAxisRuler() +
        this.paintYAxisRuler() +
        this.paintGraph() +
        this.paintXAxis() +
        this.paintYAxis() +
        this.paintLegend();
    }
};

SF.Chart.Bars = function(){
    SF.Chart.ChartBase.call(this);
};
SF.Chart.Bars.prototype = $.extend({}, new SF.Chart.ChartBase(), {
    init: function () {
        return "//config variables" + this.br +
            "var yAxisLabelWidth = 150," + this.brt +
            "fontSize= " + this.fontSize + "," + this.brt +
            "ticksLength= " + this.ticksLength + "," + this.brt +
            "labelMargin= " + this.labelMargin + "," + this.brt +
            "chartAxisPadding= " + this.chartAxisPadding + "," + this.brt +
            "padding= " + this.padding + "," + this.brt +
            "yAxisLeftPosition = padding + fontSize + yAxisLabelWidth + (2 * labelMargin) + ticksLength," + this.brt +
            "xAxisTopPosition = height - padding - (fontSize * 2) - (labelMargin * 2) - ticksLength," + this.brt +
            "color = (data.values.length < 10 ? d3.scale.category10() : d3.scale.category20()).domain([0, $.map(data.values, function(v) { return JSON.stringify(v); })]);" + this.br + this.br;
    },

    getXAxis: function () {
        return "//x axis scale" + this.br +
            "var x = d3.scale.linear()" + this.brt +
            ".domain([0, d3.max($.map(data.values, function (e) { return myChart.getTokenLabel(e.token2); }))])" + this.brt +
            ".range([0, width - yAxisLeftPosition - padding]);" + this.br + this.br;
    },

    getYAxis: function () {
        return "//y axis scale" + this.br +
            "var y = d3.scale.ordinal()" + this.brt +
            ".domain($.map(data.values, function (e) { return JSON.stringify(e); }))" + this.brt +
            ".rangeBands([0, xAxisTopPosition - padding- (2 * chartAxisPadding)]);" + this.br + this.br;
    },

    paintXAxisRuler: function () {
        return "//paint x-axis - ruler" + this.br +
            "chart.append('svg:g').attr('class', 'x-ruler').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + padding + ')')" + this.brt +
            ".enterData(x.ticks(8), 'line', 'x-ruler')" + this.brt +
            ".attr('x1', x)" + this.brt +
            ".attr('x2', x)" + this.brt +
            ".attr('y2', xAxisTopPosition - padding);" + this.br +
            this.br +
            "//paint x-axis - ticks" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(x.ticks(8), 'line', 'x-axis-tick')" + this.brt +
            ".attr('x1', x)" + this.brt +
            ".attr('x2', x)" + this.brt +
            ".attr('y2', ticksLength);" + this.br +
            this.br +
            "//paint x-axis - tick labels" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick-label').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + (xAxisTopPosition + ticksLength + labelMargin + fontSize) + ')')" + this.brt +
            ".enterData(x.ticks(8), 'text', 'x-axis-tick-label')" + this.brt +
            ".attr('x', x)" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(String);" + this.br +
            this.br +
            "//paint x-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-token-label').attr('transform', 'translate(' + (yAxisLeftPosition + ((width - yAxisLeftPosition) / 2)) + ', ' + height + ')')" + this.brt +
            ".append('svg:text').attr('class', 'x-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(data.labels.token2);" + this.br + this.br;
    },

    paintYAxisRuler: function () {
        return "//paint y-axis ticks" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength) + ', ' + (padding + chartAxisPadding + (y.rangeBand() / 2)) + ')')" + this.brt +
            ".enterData(data.values, 'line', 'y-axis-tick')" + this.brt +
            ".attr('x2', ticksLength)" + this.brt +
            ".attr('y1', function (v) { return y(JSON.stringify(v)); })" + this.brt +
            ".attr('y2', function (v) { return y(JSON.stringify(v)); });" + this.br +
            this.br +
            "//paint y-axis tick labels" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick-label').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength - labelMargin) + ', ' + (padding + chartAxisPadding + (y.rangeBand() * 2 / 3)) + ')')" + this.brt +
            ".enterData(data.values, 'text', 'y-axis-tick-label')" + this.brt +
            ".attr('y', function (v) { return y(JSON.stringify(v)); })" + this.brt +
            ".attr('text-anchor', 'end')" + this.brt +
            ".attr('width', yAxisLabelWidth)" + this.brt +
            ".text(function (v) { return myChart.getTokenLabel(v.token1); });" + this.br +
            this.br +
            "//paint y-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-token-label').attr('transform', 'translate(' + fontSize + ', ' + (xAxisTopPosition / 2) + ') rotate(270)')" + this.brt +
            ".append('svg:text').attr('class', 'y-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'start')" + this.brt +
            ".text(data.labels.token1);" + this.br + this.br;
    },

    paintGraph: function () {
        return "//paint graph" + this.br +
            "chart.append('svg:g').attr('class', 'shape').attr('transform' ,'translate(' + yAxisLeftPosition + ', ' + (padding + chartAxisPadding) + ')')" + this.brt +
            ".enterData(data.values, 'rect', 'shape')" + this.brt +
            ".attr('width', function (v) { return x(myChart.getTokenLabel(v.token2)); })" + this.brt +
            ".attr('height', y.rangeBand)" + this.brt +
            ".attr('y', function (v) { return y(JSON.stringify(v)); })" + this.brt +
            ".attr('fill', function (v) { return color(JSON.stringify(v)); })" + this.brt +
            ".attr('stroke', '#fff')" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(d) { return data.labels.token1 + ': ' + myChart.getTokenLabel(d.token1) + ', ' + data.labels.token2 + ': ' + d.token2; });" + this.br + this.br;
    }
});

SF.Chart.Columns = function () {
    SF.Chart.ChartBase.call(this);
};
SF.Chart.Columns.prototype = $.extend({}, new SF.Chart.ChartBase(), {

    init: function () {
        return "//config variables" + this.br +
            "var yAxisLabelWidth = 25," + this.brt +
            "fontSize= " + this.fontSize + "," + this.brt +
            "ticksLength= " + this.ticksLength + "," + this.brt +
            "labelMargin= " + this.labelMargin + "," + this.brt +
            "chartAxisPadding= " + this.chartAxisPadding + "," + this.brt +
            "padding= " + this.padding + "," + this.brt +
            "yAxisLeftPosition = padding + fontSize + yAxisLabelWidth + (2 * labelMargin) + ticksLength," + this.brt +
            "xAxisTopPosition = height - padding - (fontSize * 3) - (labelMargin * 3) - ticksLength," + this.brt +
            "color = (data.values.length < 10 ? d3.scale.category10() : d3.scale.category20()).domain([0, $.map(data.values, function(v) { return JSON.stringify(v); })]);" + this.br + this.br;
    },

    getXAxis: function () {
        return "//x axis scale" + this.br +
            "var x = d3.scale.ordinal()" + this.brt +
            ".domain($.map(data.values, function (e) { return JSON.stringify(e); }))" + this.brt +
            ".rangeBands([0, width - yAxisLeftPosition - padding - (2 * chartAxisPadding)]);" + this.br + this.br;
    },

    getYAxis: function () {
        return "//y axis scale" + this.br +
            "var y = d3.scale.linear()" + this.brt +
            ".domain([0, d3.max($.map(data.values, function (e) { return myChart.getTokenLabel(e.token2); }))])" + this.brt +
            ".range([0, xAxisTopPosition - padding]);" + this.br + this.br;
    },

    paintXAxisRuler: function () {
        return "//paint x-axis ticks" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick').attr('transform', 'translate(' + (yAxisLeftPosition + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(data.values, 'line', 'x-axis-tick')" + this.brt +
            ".attr('y2', function (v, i) { return (i%2 == 0 ? ticksLength : (ticksLength + fontSize + labelMargin)); })" + this.brt +
            ".attr('x1', function (v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('x2', function (v) { return x(JSON.stringify(v)); });" + this.br +
            this.br +
            "//paint x-axis tick labels" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick-label').attr('transform', 'translate(' + (yAxisLeftPosition + (x.rangeBand() / 2)) + ', ' + (xAxisTopPosition + ticksLength + labelMargin + fontSize) + ')')" + this.brt +
            ".enterData(data.values, 'text', 'x-axis-tick-label')" + this.brt +
            ".attr('x', function (v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('y', function (v, i) { return (i%2 == 0 ? 0 : (fontSize + labelMargin)); })" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(function (v) { return myChart.getTokenLabel(v.token1); });" + this.br +
            this.br +
            "//paint x-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-token-label').attr('transform', 'translate(' + (yAxisLeftPosition + ((width - yAxisLeftPosition) / 2)) + ', ' + (height) + ')')" + this.brt +
            ".append('svg:text').attr('class', 'x-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(data.labels.token1);" + this.br + this.br;
    },

    paintYAxisRuler: function () {
        return "//paint y-axis - ruler" + this.br +
            "var yTicks = y.ticks(8);" + this.br +
            "chart.append('svg:g').attr('class', 'y-ruler').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + padding + ')')" + this.brt +
            ".enterData(yTicks, 'line', 'y-ruler')" + this.brt +
            ".attr('x2', width - yAxisLeftPosition - padding)" + this.brt +
            ".attr('y1', y)" + this.brt +
            ".attr('y2', y);" + this.br +
            this.br +
            "//paint y-axis - ticks" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength) + ', ' + padding + ')')" + this.brt +
            ".enterData(yTicks, 'line', 'y-axis-tick')" + this.brt +
            ".attr('x2', ticksLength)" + this.brt +
            ".attr('y1', y)" + this.brt +
            ".attr('y2', y);" + this.br +
            this.br +
            "//paint y-axis - tick labels" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick-label').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength - labelMargin) + ', ' + padding + ')')" + this.brt +
            ".enterData(yTicks, 'text', 'y-axis-tick-label')" + this.brt +
            ".attr('y', function(t) { return y(yTicks[yTicks.length-1] - t); })" + this.brt +
            ".attr('dominant-baseline', 'middle')" + this.brt +
            ".attr('text-anchor', 'end')" + this.brt +
            ".text(String);" + this.br +
            this.br +
            "//paint y-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-token-label').attr('transform', 'translate(' + fontSize + ', ' + (xAxisTopPosition / 2) + ') rotate(270)')" + this.brt +
            ".append('svg:text').attr('class', 'y-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(data.labels.token2);" + this.br + this.br;
    },

    paintGraph: function () {
        return "//paint graph" + this.br +
            "chart.append('svg:g').attr('class', 'shape').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(data.values, 'rect', 'shape')" + this.brt +
            ".attr('height', function (v) { return y(myChart.getTokenLabel(v.token2)); })" + this.brt +
            ".attr('width', x.rangeBand)" + this.brt +
            ".attr('x', function (v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('fill', function (v) { return color(JSON.stringify(v)); })" + this.brt +
            ".attr('stroke', '#fff')" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(d) { return data.labels.token1 + ': ' + myChart.getTokenLabel(d.token1) + ', \\n' + data.labels.token2 + ': ' + myChart.getTokenLabel(d.token2); });" + this.br + this.br;
    }
});

SF.Chart.Lines = function () {
    SF.Chart.Columns.call(this);
}
SF.Chart.Lines.prototype = $.extend({}, new SF.Chart.Columns(), {

    init: function () {
        return "//config variables" + this.br +
            "var yAxisLabelWidth = 25," + this.brt +
            "fontSize= " + this.fontSize + "," + this.brt +
            "ticksLength= " + this.ticksLength + "," + this.brt +
            "labelMargin= " + this.labelMargin + "," + this.brt +
            "chartAxisPadding= " + this.chartAxisPadding + "," + this.brt +
            "padding= " + this.padding + "," + this.brt +
            "yAxisLeftPosition = padding + fontSize + yAxisLabelWidth + (2 * labelMargin) + ticksLength," + this.brt +
            "xAxisTopPosition = height - padding - (fontSize * 3) - (labelMargin * 3) - ticksLength," + this.brt +
            "color = 'steelblue';" + this.br + this.br;
    },

    paintGraph: function () {
        return "//paint graph - line" + this.br +
            "chart.append('svg:g').attr('class', 'shape').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".append('svg:path').attr('class', 'shape')" + this.brt +
            ".attr('stroke', color)" + this.brt +
            ".attr('fill', 'none')" + this.brt +
            ".attr('stroke-width', 3)" + this.brt +
            ".attr('shape-rendering', 'initial')" + this.brt +
            ".attr('d', myChart.getPathPoints($.map(data.values, function(v) { return {x: x(JSON.stringify(v)), y: y(myChart.getTokenLabel(v.token2))};})))" + this.br +
            this.br +
            "//paint graph - hover area trigger" + this.br +
            "chart.append('svg:g').attr('class', 'hover-trigger').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(data.values, 'circle', 'hover-trigger')" + this.brt +
            ".attr('cx', function(v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('cy', function(v) { return y(myChart.getTokenLabel(v.token2)); })" + this.brt +
            ".attr('r', 15)" + this.brt +
            ".attr('fill', '#fff')" + this.brt +
            ".attr('fill-opacity', 0)" + this.brt +
            ".attr('stroke', 'none')" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(d) { return data.labels.token1 + ': ' + myChart.getTokenLabel(d.token1) + ', \\n' + data.labels.token2 + ': ' + myChart.getTokenLabel(d.token2); })" + this.br +
            this.br +
            "//paint graph - points" + this.br +
            "chart.append('svg:g').attr('class', 'point').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(data.values, 'circle', 'point')" + this.brt +
            ".attr('fill', color)" + this.brt +
            ".attr('r', 5)" + this.brt +
            ".attr('cx', function(v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('cy', function(v) { return y(myChart.getTokenLabel(v.token2)); });" + this.br + this.br;
    }
});


SF.Chart.MultiLines = function () {
    SF.Chart.Lines.call(this);
}
SF.Chart.MultiLines.prototype = $.extend({}, new SF.Chart.Lines(), {

    init: function () {
        return "//config variables" + this.br +
            "var yAxisLabelWidth = 25," + this.brt +
            "fontSize= " + this.fontSize + "," + this.brt +
            "ticksLength= " + this.ticksLength + "," + this.brt +
            "labelMargin= " + this.labelMargin + "," + this.brt +
            "chartAxisPadding= " + this.chartAxisPadding + "," + this.brt +
            "padding= " + this.padding + "," + this.brt +
            "yAxisLeftPosition = padding + fontSize + yAxisLabelWidth + (2 * labelMargin) + ticksLength," + this.brt +
            "xAxisTopPosition = height - padding - (fontSize * 3) - (labelMargin * 3) - ticksLength," + this.brt +
            "xValues = myChart.getValuesInAllSeries(data.series, 'token1')," + this.brt +
            "yValues = myChart.getValuesInAllSeries(data.series, 'token3')," + this.brt +
            "color = (data.series.length < 10 ? d3.scale.category10() : d3.scale.category20()).domain([0, $.map(data.series, function(s) { return JSON.stringify(s.token2); })]);" + this.br + this.br;
    },

    getXAxis: function () {
        return "//x axis scale" + this.br +
            "var x = d3.scale.ordinal()" + this.brt +
            ".domain($.map(xValues, function (v) { return JSON.stringify(v); }))" + this.brt +
            ".rangeBands([0, width - yAxisLeftPosition - padding - (2 * chartAxisPadding)]);" + this.br + this.br;
        //            "//x axis subscale" + this.br
        //        "var xSubscale = d3.scale.ordinal()" + this.brt +
        //            ".domain(d3.range(data.series.length))" + this.brt +
        //            ".rangeBands([0, x.rangeBand()]);" + this.br + this.br;
    },

    getYAxis: function () {
        return "//y axis scale" + this.br +
            "var y = d3.scale.linear()" + this.brt +
            ".domain([0, d3.max($.map(yValues, function (v) { return myChart.getTokenLabel(v); }))])" + this.brt +
            ".range([0, xAxisTopPosition - padding - fontSize - labelMargin]);" + this.br + this.br;
    },

    paintYAxis: function () {
        return "//paint y-axis" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + (padding + fontSize + labelMargin) + ')')" + this.brt +
            ".append('svg:line')" + this.brt +
            ".attr('class', 'y-axis')" + this.brt +
            ".attr('y2', xAxisTopPosition - padding - fontSize - labelMargin);" + this.br + this.br;
    },

    paintXAxisRuler: function () {
        return "//paint x-axis ticks" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick').attr('transform', 'translate(' + (yAxisLeftPosition + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(xValues, 'line', 'x-axis-tick')" + this.brt +
            ".attr('y2', function (v, i) { return (i%2 == 0 ? ticksLength : (ticksLength + fontSize + labelMargin)); })" + this.brt +
            ".attr('x1', function (v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('x2', function (v) { return x(JSON.stringify(v)); });" + this.br +
            this.br +
            "//paint x-axis tick labels" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick-label').attr('transform', 'translate(' + (yAxisLeftPosition + (x.rangeBand() / 2)) + ', ' + (xAxisTopPosition + ticksLength + labelMargin + fontSize) + ')')" + this.brt +
            ".enterData(xValues, 'text', 'x-axis-tick-label')" + this.brt +
            ".attr('x', function (v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('y', function (v, i) { return (i%2 == 0 ? 0 : (fontSize + labelMargin)); })" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(function (v) { return myChart.getTokenLabel(v); });" + this.br +
            this.br +
            "//paint x-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-token-label').attr('transform', 'translate(' + (yAxisLeftPosition + ((width - yAxisLeftPosition) / 2)) + ', ' + (height) + ')')" + this.brt +
            ".append('svg:text').attr('class', 'x-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(data.labels.token1);" + this.br + this.br;
    },

    paintYAxisRuler: function () {
        return "//paint y-axis - ruler" + this.br +
            "var yTicks = y.ticks(8);" + this.br +
            "chart.append('svg:g').attr('class', 'y-ruler').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + (padding + fontSize + labelMargin) + ')')" + this.brt +
            ".enterData(yTicks, 'line', 'y-ruler')" + this.brt +
            ".attr('x2', width - yAxisLeftPosition - padding)" + this.brt +
            ".attr('y1', y)" + this.brt +
            ".attr('y2', y);" + this.br +
            this.br +
            "//paint y-axis - ticks" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength) + ', ' + (padding + fontSize + labelMargin) + ')')" + this.brt +
            ".enterData(yTicks, 'line', 'y-axis-tick')" + this.brt +
            ".attr('x2', ticksLength)" + this.brt +
            ".attr('y1', y)" + this.brt +
            ".attr('y2', y);" + this.br +
            this.br +
            "//paint y-axis - tick labels" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick-label').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength - labelMargin) + ', ' + (padding + fontSize + labelMargin) + ')')" + this.brt +
            ".enterData(yTicks, 'text', 'y-axis-tick-label')" + this.brt +
            ".attr('y', function(t) { return y(yTicks[yTicks.length-1] - t); })" + this.brt +
            ".attr('dominant-baseline', 'middle')" + this.brt +
            ".attr('text-anchor', 'end')" + this.brt +
            ".text(String);" + this.br +
            this.br +
            "//paint y-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-token-label').attr('transform', 'translate(' + fontSize + ', ' + ((xAxisTopPosition - fontSize - labelMargin) / 2) + ') rotate(270)')" + this.brt +
            ".append('svg:text').attr('class', 'y-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(data.labels.token3);" + this.br + this.br;
    },

    paintLegend: function () {
        return this.defaultColorLegend();
    },

    paintGraph: function () {
        return "//paint graph - line" + this.br +
            "chart.enterData(data.series, 'g', 'shape-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".append('svg:path').attr('class', 'shape')" + this.brt +
            ".attr('stroke', function(s) { return color(JSON.stringify(s.token2)); })" + this.brt +
            ".attr('fill', 'none')" + this.brt +
            ".attr('stroke-width', 3)" + this.brt +
            ".attr('shape-rendering', 'initial')" + this.brt +
            ".attr('d', function(serie) { return myChart.getPathPoints($.map(serie.values, function(v) { return {x: x(JSON.stringify(v.token1)), y: y(myChart.getTokenLabel(v.token3))};})); });" + this.br +
            this.br +
            "//paint graph - hover area trigger" + this.br +
            "chart.enterData(data.series, 'g', 'hover-trigger-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(function(s) { return s.values; }, 'circle', 'hover-trigger')" + this.brt +
            ".attr('cx', function(v) { return x(JSON.stringify(v.token1)); })" + this.brt +
            ".attr('cy', function(v) { return y(myChart.getTokenLabel(v.token3)); })" + this.brt +
            ".attr('r', 15)" + this.brt +
            ".attr('fill', '#fff')" + this.brt +
            ".attr('fill-opacity', 0)" + this.brt +
            ".attr('stroke', 'none')" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(d) { return data.labels.token1 + ': ' + myChart.getTokenLabel(d.token1) + ', \\n' + data.labels.token3 + ': ' + myChart.getTokenLabel(d.token3); })" + this.br +
            this.br +
            "//paint graph - points" + this.br +
            "chart.enterData(data.series, 'g', 'point-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(function(s) { return $.map(s.values, function(v){ return { token2: s.token2, value: v }; }); }, 'circle', 'point')" + this.brt +
            ".attr('fill', function(pair) { return color(JSON.stringify(pair.token2)); })" + this.brt +
            ".attr('r', 5)" + this.brt +
            ".attr('cx', function(pair) { return x(JSON.stringify(pair.value.token1)); })" + this.brt +
            ".attr('cy', function(pair) { return y(myChart.getTokenLabel(pair.value.token3)); });" + this.br + this.br;
    }
});

(function () { 
    var data = {
        labels:{"token1":"Album","token2":"Author","token3":"Id"},
        series:[
            {"token2": {"key":"Band;1","toStr":"Smashing Pumpkins"}, "values":[
                {"token1":{"key":"Album;1","toStr":"Siamese Dream"},"token3":1},
                {"token1":{"key":"Album;2","toStr":"Mellon Collie and the Infinite Sadness"},"token3":2},
                {"token1":{"key":"Album;3","toStr":"Zeitgeist"},"token3":3},
                {"token1":{"key":"Album;4","toStr":"American Gothic"},"token3":4}
            ]},
            {"token2":{"key":"Artist;5","toStr":"Michael Jackson"},"values":[
                {"token1":{"key":"Album;5","toStr":"Ben"},"token3":5},
                {"token1":{"key":"Album;6","toStr":"Thriller"},"token3":6},
                {"token1":{"key":"Album;7","toStr":"Bad"},"token3":7},
                {"token1":{"key":"Album;8","toStr":"Dangerous"},"token3":8},
                {"token1":{"key":"Album;9","toStr":"HIStory"},"token3":9},
                {"token1":{"key":"Album;10","toStr":"Blood on the Dance Floor"},"token3":10}
            ]},
            {"token2":{"key":"Band;2","toStr":"Sigur Ros"},"values":[
                {"token1":{"key":"Album;11","toStr":"Ágaetis byrjun"},"token3":11},
                {"token1":{"key":"Album;12","toStr":"Takk..."},"token3":12}
            ]}
        ]};

    var $chartContainer = $('.sf-chart-container');
    var width = $chartContainer.width();
    var height = $chartContainer.height();

    var myChart = SF.Chart.Factory.getGraphType('MultiLines');
                
    var code = SF.Chart.Factory.createChartSVG('.sf-chart-container') + 
        myChart.paintChart();

    SF.log(code);
    eval(code);

})();