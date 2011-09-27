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

    paintLegend: function () {
        return "";
    },

    getTokenLabel: function (tokenValue) {
        return tokenValue.toStr || tokenValue;
    },

    getPathPoints: function (points) {
        var result = "";
        var jump = true;
        $.each(points, function (i, p) {
            if (p.x == null || p.y == null) {
                jump = true;
            }
            else {
                result += (jump ? " M " : " ") + p.x + " " + p.y;
                jump = false;
            }
        });
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
            "color = (data.serie.length < 10 ? d3.scale.category10() : d3.scale.category20()).domain([0, $.map(data.serie, function(v) { return JSON.stringify(v); })]);" + this.br + this.br;
    },

    getXAxis: function () {
        return "//x axis scale" + this.br +
            "var x = d3.scale.linear()" + this.brt +
            ".domain([0, d3.max($.map(data.serie, function (e) { return myChart.getTokenLabel(e.value1); }))])" + this.brt +
            ".range([0, width - yAxisLeftPosition - padding]);" + this.br + this.br;
    },

    getYAxis: function () {
        return "//y axis scale" + this.br +
            "var y = d3.scale.ordinal()" + this.brt +
            ".domain($.map(data.serie, function (e) { return JSON.stringify(e); }))" + this.brt +
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
            ".text(data.labels.value1);" + this.br + this.br;
    },

    paintYAxisRuler: function () {
        return "//paint y-axis ticks" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength) + ', ' + (padding + chartAxisPadding + (y.rangeBand() / 2)) + ')')" + this.brt +
            ".enterData(data.serie, 'line', 'y-axis-tick')" + this.brt +
            ".attr('x2', ticksLength)" + this.brt +
            ".attr('y1', function (v) { return y(JSON.stringify(v)); })" + this.brt +
            ".attr('y2', function (v) { return y(JSON.stringify(v)); });" + this.br +
            this.br +
            "//paint y-axis tick labels" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick-label').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength - labelMargin) + ', ' + (padding + chartAxisPadding + (y.rangeBand() * 2 / 3)) + ')')" + this.brt +
            ".enterData(data.serie, 'text', 'y-axis-tick-label')" + this.brt +
            ".attr('y', function (v) { return y(JSON.stringify(v)); })" + this.brt +
            ".attr('text-anchor', 'end')" + this.brt +
            ".attr('width', yAxisLabelWidth)" + this.brt +
            ".text(function (v) { return myChart.getTokenLabel(v.dimension1); });" + this.br +
            this.br +
            "//paint y-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-token-label').attr('transform', 'translate(' + fontSize + ', ' + (xAxisTopPosition / 2) + ') rotate(270)')" + this.brt +
            ".append('svg:text').attr('class', 'y-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'start')" + this.brt +
            ".text(data.labels.dimension1);" + this.br + this.br;
    },

    paintGraph: function () {
        return "//paint graph" + this.br +
            "chart.append('svg:g').attr('class', 'shape').attr('transform' ,'translate(' + yAxisLeftPosition + ', ' + (padding + chartAxisPadding) + ')')" + this.brt +
            ".enterData(data.serie, 'rect', 'shape')" + this.brt +
            ".attr('width', function (v) { return x(myChart.getTokenLabel(v.value1)); })" + this.brt +
            ".attr('height', y.rangeBand)" + this.brt +
            ".attr('y', function (v) { return y(JSON.stringify(v)); })" + this.brt +
            ".attr('fill', function (v) { return color(JSON.stringify(v)); })" + this.brt +
            ".attr('stroke', '#fff')" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(d) { return myChart.getTokenLabel(d.dimension1) + ': ' + myChart.getTokenLabel(d.value1); });" + this.br + this.br;
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
            "color = (data.serie.length < 10 ? d3.scale.category10() : d3.scale.category20()).domain([0, $.map(data.serie, function(v) { return JSON.stringify(v); })]);" + this.br + this.br;
    },

    getXAxis: function () {
        return "//x axis scale" + this.br +
            "var x = d3.scale.ordinal()" + this.brt +
            ".domain($.map(data.serie, function (e) { return JSON.stringify(e); }))" + this.brt +
            ".rangeBands([0, width - yAxisLeftPosition - padding - (2 * chartAxisPadding)]);" + this.br + this.br;
    },

    getYAxis: function () {
        return "//y axis scale" + this.br +
            "var y = d3.scale.linear()" + this.brt +
            ".domain([0, d3.max($.map(data.serie, function (e) { return myChart.getTokenLabel(e.value1); }))])" + this.brt +
            ".range([0, xAxisTopPosition - padding]);" + this.br + this.br;
    },

    paintXAxisRuler: function () {
        return "//paint x-axis ticks" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick').attr('transform', 'translate(' + (yAxisLeftPosition + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(data.serie, 'line', 'x-axis-tick')" + this.brt +
            ".attr('y2', function (v, i) { return (i%2 == 0 ? ticksLength : (ticksLength + fontSize + labelMargin)); })" + this.brt +
            ".attr('x1', function (v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('x2', function (v) { return x(JSON.stringify(v)); });" + this.br +
            this.br +
            "//paint x-axis tick labels" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick-label').attr('transform', 'translate(' + (yAxisLeftPosition + (x.rangeBand() / 2)) + ', ' + (xAxisTopPosition + ticksLength + labelMargin + fontSize) + ')')" + this.brt +
            ".enterData(data.serie, 'text', 'x-axis-tick-label')" + this.brt +
            ".attr('x', function (v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('y', function (v, i) { return (i%2 == 0 ? 0 : (fontSize + labelMargin)); })" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(function (v) { return myChart.getTokenLabel(v.dimension1); });" + this.br +
            this.br +
            "//paint x-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-token-label').attr('transform', 'translate(' + (yAxisLeftPosition + ((width - yAxisLeftPosition) / 2)) + ', ' + (height) + ')')" + this.brt +
            ".append('svg:text').attr('class', 'x-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(data.labels.dimension1);" + this.br + this.br;
    },

    paintYAxisRuler: function () {
        return "//paint y-axis - ruler" + this.br +
            "var yTicks = y.ticks(8);" + this.br +
            "chart.append('svg:g').attr('class', 'y-ruler').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(yTicks, 'line', 'y-ruler')" + this.brt +
            ".attr('x2', width - yAxisLeftPosition - padding)" + this.brt +
            ".attr('y1', function(t) { return -y(t); })" + this.brt +
            ".attr('y2', function(t) { return -y(t); });" + this.br +
            this.br +
            "//paint y-axis - ticks" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength) + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(yTicks, 'line', 'y-axis-tick')" + this.brt +
            ".attr('x2', ticksLength)" + this.brt +
            ".attr('y1', function(t) { return -y(t); })" + this.brt +
            ".attr('y2', function(t) { return -y(t); });" + this.br +
            this.br +
            "//paint y-axis - tick labels" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick-label').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength - labelMargin) + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(yTicks, 'text', 'y-axis-tick-label')" + this.brt +
            ".attr('y', function(t) { return -y(t); })" + this.brt +
            ".attr('dominant-baseline', 'middle')" + this.brt +
            ".attr('text-anchor', 'end')" + this.brt +
            ".text(String);" + this.br +
            this.br +
            "//paint y-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-token-label').attr('transform', 'translate(' + fontSize + ', ' + (xAxisTopPosition / 2) + ') rotate(270)')" + this.brt +
            ".append('svg:text').attr('class', 'y-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(data.labels.value1);" + this.br + this.br;
    },

    paintGraph: function () {
        return "//paint graph" + this.br +
            "chart.append('svg:g').attr('class', 'shape').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(data.serie, 'rect', 'shape')" + this.brt +
            ".attr('height', function (v) { return y(myChart.getTokenLabel(v.value1)); })" + this.brt +
            ".attr('width', x.rangeBand)" + this.brt +
            ".attr('x', function (v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('fill', function (v) { return color(JSON.stringify(v)); })" + this.brt +
            ".attr('stroke', '#fff')" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(d) { return myChart.getTokenLabel(d.dimension1) + ': ' + myChart.getTokenLabel(d.value1); });" + this.br + this.br;
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
            ".attr('d', myChart.getPathPoints($.map(data.serie, function(v) { return {x: x(JSON.stringify(v)), y: y(myChart.getTokenLabel(v.value1))};})))" + this.br +
            this.br +
            "//paint graph - hover area trigger" + this.br +
            "chart.append('svg:g').attr('class', 'hover-trigger').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(data.serie, 'circle', 'hover-trigger')" + this.brt +
            ".attr('cx', function(v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('cy', function(v) { return y(myChart.getTokenLabel(v.value1)); })" + this.brt +
            ".attr('r', 15)" + this.brt +
            ".attr('fill', '#fff')" + this.brt +
            ".attr('fill-opacity', 0)" + this.brt +
            ".attr('stroke', 'none')" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(d) { return myChart.getTokenLabel(d.dimension1) + ': ' + myChart.getTokenLabel(d.value1); })" + this.br +
            this.br +
            "//paint graph - points" + this.br +
            "chart.append('svg:g').attr('class', 'point').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(data.serie, 'circle', 'point')" + this.brt +
            ".attr('fill', color)" + this.brt +
            ".attr('r', 5)" + this.brt +
            ".attr('cx', function(v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('cy', function(v) { return y(myChart.getTokenLabel(v.value1)); });" + this.br + this.br;
    }
});

SF.Chart.TypeTypeValue = function () {
    SF.Chart.ChartBase.call(this);
}
SF.Chart.TypeTypeValue.prototype = $.extend({}, new SF.Chart.ChartBase(), {

    getMaxValue1: function (series) {
        var completeArray = [];
        $.each(series, function (i, s) {
            $.merge(completeArray, s.values);
        });
        return d3.max(completeArray);
    },

    createEmptyCountArray: function (length) {
        var countArray = [];
        for (var i = 0; i < length; i++) {
            countArray.push(0);
        }
        return countArray;
    },

    createCountArray: function (series) {
        var dimensionCount = series[0].values.length;
        var countArray = this.createEmptyCountArray(dimensionCount);

        $.each(series, function (i, serie) {
            for (var i = 0; i < dimensionCount; i++) {
                var v = serie.values[i];
                if (!SF.isEmpty(v)) {
                    countArray[i] += v;
                }
            }
        });

        return countArray;
    },

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
            "color = (data.series.length < 10 ? d3.scale.category10() : d3.scale.category20()).domain([0, $.map(data.series, function(s) { return JSON.stringify(s.dimension2); })]);" + this.br + this.br;
    },

    getXAxis: function () {
        return "//x axis scale" + this.br +
            "var x = d3.scale.ordinal()" + this.brt +
            ".domain($.map(data.dimension1, function (v) { return JSON.stringify(v); }))" + this.brt +
            ".rangeBands([0, width - yAxisLeftPosition - padding - (2 * chartAxisPadding)]);" + this.br + this.br;
    },

    getYAxis: function () {
        return "//y axis scale" + this.br +
            "var y = d3.scale.linear()" + this.brt +
            ".domain([0, myChart.getMaxValue1(data.series)])" + this.brt +
            ".range([0, xAxisTopPosition - padding - fontSize - labelMargin]);" + this.br + this.br;
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
            "chart.append('svg:g').attr('class', 'y-axis').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + (padding + fontSize + labelMargin) + ')')" + this.brt +
            ".append('svg:line')" + this.brt +
            ".attr('class', 'y-axis')" + this.brt +
            ".attr('y2', xAxisTopPosition - padding - fontSize - labelMargin);" + this.br + this.br;
    },

    paintXAxisRuler: function () {
        return "//paint x-axis ticks" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick').attr('transform', 'translate(' + (yAxisLeftPosition + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(data.dimension1, 'line', 'x-axis-tick')" + this.brt +
            ".attr('y2', function (v, i) { return (i%2 == 0 ? ticksLength : (ticksLength + fontSize + labelMargin)); })" + this.brt +
            ".attr('x1', function (v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('x2', function (v) { return x(JSON.stringify(v)); });" + this.br +
            this.br +
            "//paint x-axis tick labels" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick-label').attr('transform', 'translate(' + (yAxisLeftPosition + (x.rangeBand() / 2)) + ', ' + (xAxisTopPosition + ticksLength + labelMargin + fontSize) + ')')" + this.brt +
            ".enterData(data.dimension1, 'text', 'x-axis-tick-label')" + this.brt +
            ".attr('x', function (v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('y', function (v, i) { return (i%2 == 0 ? 0 : (fontSize + labelMargin)); })" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(function (v) { return myChart.getTokenLabel(v); });" + this.br +
            this.br +
            "//paint x-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-token-label').attr('transform', 'translate(' + (yAxisLeftPosition + ((width - yAxisLeftPosition) / 2)) + ', ' + (height) + ')')" + this.brt +
            ".append('svg:text').attr('class', 'x-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(data.labels.dimension1);" + this.br + this.br;
    },

    paintYAxisRuler: function () {
        return "//paint y-axis - ruler" + this.br +
            "var yTicks = y.ticks(10);" + this.br +
            "chart.append('svg:g').attr('class', 'y-ruler').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(yTicks, 'line', 'y-ruler')" + this.brt +
            ".attr('x2', width - yAxisLeftPosition - padding)" + this.brt +
            ".attr('y1', function(t) { return -y(t); })" + this.brt +
            ".attr('y2', function(t) { return -y(t); });" + this.br +
            this.br +
            "//paint y-axis - ticks" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength) + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(yTicks, 'line', 'y-axis-tick')" + this.brt +
            ".attr('x2', ticksLength)" + this.brt +
            ".attr('y1', function(t) { return -y(t); })" + this.brt +
            ".attr('y2', function(t) { return -y(t); });" + this.br +
            this.br +
            "//paint y-axis - tick labels" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick-label').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength - labelMargin) + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(yTicks, 'text', 'y-axis-tick-label')" + this.brt +
            ".attr('y', function(t) { return -y(t); })" + this.brt +
            ".attr('dominant-baseline', 'middle')" + this.brt +
            ".attr('text-anchor', 'end')" + this.brt +
            ".text(String);" + this.br +
            this.br +
            "//paint y-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-token-label').attr('transform', 'translate(' + fontSize + ', ' + ((xAxisTopPosition - fontSize - labelMargin) / 2) + ') rotate(270)')" + this.brt +
            ".append('svg:text').attr('class', 'y-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(data.labels.value1);" + this.br + this.br;
    },

    paintLegend: function () {
        return "//paint color legend" + this.br +
            "var legendScale = d3.scale.ordinal().domain($.map(data.series, function (s, i) { return i; })).rangeBands([0, width - yAxisLeftPosition - padding])," + this.brt +
            "legendRectWidth = 10," + this.brt +
            "legendLabelWidth = legendScale.rangeBand() - (2 * labelMargin) - legendRectWidth;" + this.br +
            this.br +
            "chart.append('svg:g').attr('class', 'color-legend').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + padding + ')')" + this.brt +
            ".enterData(data.series, 'rect', 'color-rect')" + this.brt +
            ".attr('x', function(e, i) { return (legendRectWidth + legendLabelWidth + (2 * labelMargin)) * i; })" + this.brt +
            ".attr('width', legendRectWidth).attr('height', fontSize)" + this.brt +
            ".attr('fill', function(s) { return color(JSON.stringify(s.dimension2)); });" + this.br +
            this.br +
            "chart.append('svg:g').attr('class', 'color-legend').attr('transform', 'translate(' + (yAxisLeftPosition + labelMargin + legendRectWidth) + ', ' + (padding + fontSize) + ')')" + this.brt +
            ".enterData(data.series, 'text', 'color-text')" + this.brt +
            ".attr('x', function(e, i) { return (legendRectWidth + legendLabelWidth + (2 * labelMargin)) * i; })" + this.brt +
            ".text(function(s) { return myChart.getTokenLabel(s.dimension2); });" + this.br + this.br;
    }
});

SF.Chart.MultiLines = function () {
    SF.Chart.TypeTypeValue.call(this);
}
SF.Chart.MultiLines.prototype = $.extend({}, new SF.Chart.TypeTypeValue(), {

    paintGraph: function () {
        return "//paint graph - line" + this.br +
            "chart.enterData(data.series, 'g', 'shape-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".append('svg:path').attr('class', 'shape')" + this.brt +
            ".attr('stroke', function(s) { return color(JSON.stringify(s.dimension2)); })" + this.brt +
            ".attr('fill', 'none')" + this.brt +
            ".attr('stroke-width', 3)" + this.brt +
            ".attr('shape-rendering', 'initial')" + this.brt +
            ".attr('d', function(s) { return myChart.getPathPoints($.map(s.values, function(v, i) { return { x: x(JSON.stringify(data.dimension1[i])), y: (SF.isEmpty(v) ? null : y(v)) }; }) )})" + this.br +
            this.br +
            "//paint graph - hover area trigger" + this.br +
            "chart.enterData(data.series, 'g', 'hover-trigger-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(function(s) { return s.values; }, 'circle', 'hover-trigger')" + this.brt +
            ".attr('cx', function(v, i) { return x(JSON.stringify(data.dimension1[i])); })" + this.brt +
            ".attr('cy', function(v) { return SF.isEmpty(v) ? 0 : y(myChart.getTokenLabel(v)); })" + this.brt +
            ".attr('r', function(v) { return SF.isEmpty(v) ? 0 : 15; })" + this.brt +
            ".attr('fill', '#fff')" + this.brt +
            ".attr('fill-opacity', 0)" + this.brt +
            ".attr('stroke', 'none')" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(v) { return v; })" + this.br +
            this.br +
            "//paint graph - points" + this.br +
            "chart.enterData(data.series, 'g', 'point-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(function(s) { return $.map(s.values, function(v){ return { dimension2: s.dimension2, value: v }; }); }, 'circle', 'point')" + this.brt +
            ".attr('fill', function(pair) { return color(JSON.stringify(pair.dimension2)); })" + this.brt +
            ".attr('fill-opacity', function(pair) { SF.isEmpty(pair.value) ? 0 : 100; })" + this.brt +
            ".attr('r', function(pair) { return SF.isEmpty(pair.value) ? 0 : 5; })" + this.brt +
            ".attr('cx', function(pair, i) { return x(JSON.stringify(data.dimension1[i])); })" + this.brt +
            ".attr('cy', function(pair) { return SF.isEmpty(pair.value) ? 0 : y(myChart.getTokenLabel(pair.value)); });" + this.br + this.br;
    }
});

SF.Chart.MultiColumns = function () {
    SF.Chart.TypeTypeValue.call(this);
};
SF.Chart.MultiColumns.prototype = $.extend({}, new SF.Chart.TypeTypeValue(), {

    paintGraph: function () {
        return "//graph x-subscale" + this.br +
            "var xSubscale = d3.scale.ordinal()" + this.brt +
            ".domain($.map(data.series, function (s) { return JSON.stringify(s.dimension2); }))" + this.brt +
            ".rangeBands([0, x.rangeBand()]);" + this.br +
            this.br +
            "//paint graph" + this.br +
            "chart.enterData(data.series, 'g', 'shape-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(function(s) { return $.map(s.values, function(v){ return { dimension2: s.dimension2, value: v }; }); }, 'rect', 'shape')" + this.brt +
            ".attr('stroke', function(pair) { return SF.isEmpty(pair.value) ? 'none' : '#fff'; })" + this.brt +
            ".attr('fill', function(pair) { return SF.isEmpty(pair.value) ? 'none' : color(JSON.stringify(pair.dimension2)); })" + this.brt +
            ".attr('x', function(pair, i) { return xSubscale(JSON.stringify(pair.dimension2)); })" + this.brt +
            ".attr('transform',  function(pair, i) { return 'translate(' + x(JSON.stringify(data.dimension1[i])) + ', 0)'; })" + this.brt +
            ".attr('width', xSubscale.rangeBand())" + this.brt +
            ".attr('height', function(pair, i) { return SF.isEmpty(pair.value) ? 0 : y(myChart.getTokenLabel(pair.value)); })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(pair, i) { return SF.isEmpty(pair.value) ? null : myChart.getTokenLabel(data.dimension1[i]) + ', ' + myChart.getTokenLabel(pair.dimension2) + ': ' + myChart.getTokenLabel(pair.value); })" + this.br +
             + this.br + this.br;
    }
});

SF.Chart.StackedColumns = function () {
    SF.Chart.TypeTypeValue.call(this);
};
SF.Chart.StackedColumns.prototype = $.extend({}, new SF.Chart.TypeTypeValue(), {

    getYAxis: function () {
        return "//y axis scale" + this.br +
            "var y = d3.scale.linear()" + this.brt +
            ".domain([0, d3.max(myChart.createCountArray(data.series))])" + this.brt +
            ".range([0, xAxisTopPosition - padding - fontSize - labelMargin]);" + this.br + this.br;
    },

    paintGraph: function () {
        return "//paint graph" + this.br +
            "var countArray = myChart.createEmptyCountArray(data.dimension1.length);" + this.br +
            "chart.enterData(data.series, 'g', 'shape-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(function(s) { return $.map(s.values, function(v){ return { dimension2: s.dimension2, value: v }; }); }, 'rect', 'shape')" + this.brt +
            ".attr('stroke', function(pair) { return SF.isEmpty(pair.value) ? 'none' : '#fff'; })" + this.brt +
            ".attr('fill', function(pair) { return SF.isEmpty(pair.value) ? 'none' : color(JSON.stringify(pair.dimension2)); })" + this.brt +
            ".attr('transform',  function(pair, i) { return 'translate(' + x(JSON.stringify(data.dimension1[i])) + ', 0)'; })" + this.brt +
            ".attr('width', x.rangeBand())" + this.brt +
            ".attr('height', function(pair, i) { return SF.isEmpty(pair.value) ? 0 : y(myChart.getTokenLabel(pair.value)); })" + this.brt +
            ".attr('y', function(pair, i) { if (SF.isEmpty(pair.value)) { return 0; } else { var offset = y(countArray[i]); countArray[i] += pair.value; return offset; } })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(pair, i) { return SF.isEmpty(pair.value) ? null : myChart.getTokenLabel(data.dimension1[i]) + ', ' + myChart.getTokenLabel(pair.dimension2) + ': ' + myChart.getTokenLabel(pair.value); })" + this.br +
            this.br + this.br;
    }
});

SF.Chart.TotalColumns = function () {
    SF.Chart.TypeTypeValue.call(this);
};
SF.Chart.TotalColumns.prototype = $.extend({}, new SF.Chart.TypeTypeValue(), {

    getYAxis: function () {
        return "//y axis scale" + this.br +
            "var y = d3.scale.linear()" + this.brt +
            ".domain([0, 100])" + this.brt +
            ".range([0, xAxisTopPosition - padding - fontSize - labelMargin]);" + this.br + this.br;
    },

    paintGraph: function () {
        return "//paint graph" + this.br +
            "var countArray = myChart.createCountArray(data.series);" + this.br +
            "var emptyCountArray = myChart.createEmptyCountArray(data.dimension1.length);" + this.br +
            "chart.enterData(data.series, 'g', 'shape-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(function(s) { return $.map(s.values, function(v){ return { dimension2: s.dimension2, value: v }; }); }, 'rect', 'shape')" + this.brt +
            ".attr('stroke', function(pair) { return SF.isEmpty(pair.value) ? 'none' : '#fff'; })" + this.brt +
            ".attr('fill', function(pair) { return SF.isEmpty(pair.value) ? 'none' : color(JSON.stringify(pair.dimension2)); })" + this.brt +
            ".attr('transform',  function(pair, i) { return 'translate(' + x(JSON.stringify(data.dimension1[i])) + ', 0)'; })" + this.brt +
            ".attr('width', x.rangeBand())" + this.brt +
            ".attr('height', function(pair, i) { return SF.isEmpty(pair.value) ? 0 : y((100 *pair.value) / countArray[i]); })" + this.brt +
            ".attr('y', function(pair, i) { if (SF.isEmpty(pair.value)) { return 0; } else { var offset = emptyCountArray[i]; emptyCountArray[i] += pair.value; return y((100 * offset) / countArray[i]); } })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(pair, i) { return SF.isEmpty(pair.value) ? null : myChart.getTokenLabel(data.dimension1[i]) + ', ' + myChart.getTokenLabel(pair.dimension2) + ': ' + myChart.getTokenLabel(pair.value); })" + this.br +
            this.br + this.br;
    }
});

(function () {
    var dataTV =
    {
        labels: { dimension1: "Color", value1: "Altura" },
        serie: [{ dimension1: "Negro", value1: "1,80" },
               { dimension1: "Blanco", value1: "1,70"}]
    };

    var dataTTV =
    {
        labels: { dimension1: "Color", dimension2: "Sexo", value1: "Altura" },
        dimension1: ["Negro", "Blanco", "Amarillo"],
        series: [{ dimension2: "Hombre", values: [1.80, null, 1.60] },
                { dimension2: "Mujer", values: [1.70, 1.80, null]}]

    };

    var dataPoints =
    {
        labels: { value1: "Sexo", dimension1: "Altura", dimension2: "Peso" },
        points: [{ value1: "Hombre", dimension1: "1,80", dimension2: "70" },
               { value1: "Hombre", dimension1: "1,80", dimension2: "70"}]
    };

    var dataBubbles =
    {
        labels: { value1: "Sexo", dimension1: "Altura", dimension2: "Peso", value2: "Edad" },
        points: [{ value1: "Hombre", dimension1: "1,80", dimension2: "70", value2: "0" },
               { value1: "Hombre", dimension1: "1,80", dimension2: "70", value2: "1"}]
    };

    //    var dataTV = {
    //        labels: { dimension1:"Album", value1:"Id" },
    //        serie:[
    //            {"dimension1":{"key":"Album;1","toStr":"Siamese Dream"},"value1":1},
    //            {"dimension1":{"key":"Album;2","toStr":"Mellon Collie and the Infinite Sadness"},"value1":2},
    //            {"dimension1":{"key":"Album;3","toStr":"Zeitgeist"},"value1":3},
    //            {"dimension1":{"key":"Album;4","toStr":"American Gothic"},"value1":4},
    //            {"dimension1":{"key":"Album;5","toStr":"Ben"},"value1":5},
    //            {"dimension1":{"key":"Album;6","toStr":"Thriller"},"value1":6},
    //            {"dimension1":{"key":"Album;7","toStr":"Bad"},"value1":7},
    //            {"dimension1":{"key":"Album;8","toStr":"Dangerous"},"value1":8},
    //            {"dimension1":{"key":"Album;9","toStr":"HIStory"},"value1":9},
    //            {"dimension1":{"key":"Album;10","toStr":"Blood on the Dance Floor"},"value1":10},
    //            {"dimension1":{"key":"Album;11","toStr":"Ágaetis byrjun"},"value1":11},
    //            {"dimension1":{"key":"Album;12","toStr":"Takk..."},"value1":12}
    //        ]
    //    };

//    var dataMultiLine = {
//        labels: { dimension1: "Album", dimension2: "Author", value1: "[Num] de Songs" },
//        dimension1: [{ "key": "Album;1", "toStr": "Siamese Dream" },
//                     { "key": "Album;2", "toStr": "Mellon Collie and the Infinite Sadness" },
//                     { "key": "Album;3", "toStr": "Zeitgeist" },
//                     { "key": "Album;4", "toStr": "American Gothic" },
//                     { "key": "Album;5", "toStr": "Ben" },
//                     { "key": "Album;6", "toStr": "Thriller" },
//                     { "key": "Album;7", "toStr": "Bad" },
//                     { "key": "Album;8", "toStr": "Dangerous" },
//                     { "key": "Album;9", "toStr": "HIStory" },
//                     { "key": "Album;10", "toStr": "Blood on the Dance Floor" },
//                     { "key": "Album;11", "toStr": "Ágaetis byrjun" },
//                     { "key": "Album;12", "toStr": "Takk..."}],
//        series: [{ "dimension2": { "key": "Band;1", "toStr": "Smashing Pumpkins" }, "values": [1, 3, 1, 1, null, null, null, null, null, null, null, null] },
//                 { "dimension2": { "key": "Artist;5", "toStr": "Michael Jackson" }, "values": [null, null, null, null, 1, 3, 4, 3, 2, 2, null, null] },
//                 { "dimension2": { "key": "Band;2", "toStr": "Sigur Ros" }, "values": [null, null, null, null, null, null, null, null, null, null, 1, 3] }
//                ]
//    };

    var data = { 
        labels: { dimension1: "Author", dimension2: "Album", value1: "[Num] de Songs" },
        dimension1: [
            {"key":"Band;1","toStr":"Smashing Pumpkins"},
            {"key":"Artist;5","toStr":"Michael Jackson"},
            {"key":"Band;2","toStr":"Sigur Ros"}],
        series:[
            {"dimension2":{"key":"Album;1","toStr":"Siamese Dream"},"values":[1,null,null]},
            {"dimension2":{"key":"Album;2","toStr":"Mellon Collie and the Infinite Sadness"},"values":[3,null,null]},
            {"dimension2":{"key":"Album;3","toStr":"Zeitgeist"},"values":[1,null,null]},
            {"dimension2":{"key":"Album;4","toStr":"American Gothic"},"values":[1,null,null]},
            {"dimension2":{"key":"Album;5","toStr":"Ben"},"values":[null,1,null]},
            {"dimension2":{"key":"Album;6","toStr":"Thriller"},"values":[null,3,null]},
            {"dimension2":{"key":"Album;7","toStr":"Bad"},"values":[null,4,null]},
            {"dimension2":{"key":"Album;8","toStr":"Dangerous"},"values":[null,3,null]},
            {"dimension2":{"key":"Album;9","toStr":"HIStory"},"values":[null,2,null]},
            {"dimension2":{"key":"Album;10","toStr":"Blood on the Dance Floor"},"values":[null,2,null]},
            {"dimension2":{"key":"Album;11","toStr":"Ágaetis byrjun"},"values":[null,null,1]},
            {"dimension2":{"key":"Album;12","toStr":"Takk..."},"values":[null,null,3]}
        ]
    };

    var $chartContainer = $('.sf-chart-container');
    var width = $chartContainer.width();
    var height = $chartContainer.height();

    var myChart = SF.Chart.Factory.getGraphType('TotalColumns');

    var code = SF.Chart.Factory.createChartSVG('.sf-chart-container') +
        myChart.paintChart();

    SF.log(code);
    eval(code);

})();