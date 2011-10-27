var SF = SF || {};

SF.Chart = SF.Chart || {};

SF.Chart.Builder = (function () {

    var requestProcessedData = function (prefix) {
        return { filters: new SF.FindNavigator({ prefix: prefix }).serializeFilters() };
    };

    var updateChartBuilder = function ($chartControl, tokenChanged, callback) {
        var $chartBuilder = $chartControl.find(".sf-chart-builder");
        var data = $chartControl.find(":input").serialize() + "&filters=" + new SF.FindNavigator({ prefix: $chartControl.attr("data-prefix") }).serializeFilters();
        if (!SF.isEmpty(tokenChanged)) {
            data += "&lastTokenChanged=" + tokenChanged;
        }
        $.ajax({
            url: $chartBuilder.attr("data-url"),
            data: data,
            success: function (result) {
                $chartBuilder.replaceWith(result);
                SF.triggerNewContent($chartControl.find(".sf-chart-builder"));
                if (typeof callback != "undefined") {
                    callback();
                }
            }
        });
    };

    $(".sf-chart-img").live("click", function () {
        var $this = $(this);
        $this.closest(".sf-chart-type").find(".ui-widget-header .sf-chart-type-value").val($this.attr("data-related"));
        var $chartControl = $this.closest(".sf-chart-control");
        updateChartBuilder($chartControl);

        var $resultsContainer = $chartControl.find(".sf-search-results-container");
        if ($this.hasClass("sf-chart-img-equiv")) {
            if ($resultsContainer.find("svg").length > 0) {
                SF.Chart.Builder.reDraw($chartControl.find(".sf-chart-container"), true);
            }
        }
        else {
            $resultsContainer.html("");
        }
    });

    $(".sf-chart-group-trigger").live("change", function () {
        var $this = $(this);
        $this.closest(".sf-chart-builder").find(".sf-chart-group-results").val($this.is(":checked"));
        updateChartBuilder($this.closest(".sf-chart-control"));
    });

    $(".sf-chart-token-config-trigger").live("click", function () {
        $(this).closest(".sf-chart-token").next().toggle();
    });

    $(".sf-chart-token-aggregate").live("change", function () {
        var $this = $(this);
        if ($this.val() == "Count") {
            var id = this.id;
            updateChartBuilder($this.closest(".sf-chart-control"), null, function () {
                $("#" + id).closest(".sf-chart-token").find(".sf-query-token").hide();
            });
        }
        else {
            $this.closest(".sf-chart-token").find(".sf-query-token").show();
        }
    });

    $(".sf-query-token select").live("change", function () {
        var $this = $(this);
        updateChartBuilder($this.closest(".sf-chart-control"), $this.closest("tr").attr("data-token"));
    });

    $(".sf-chart-draw").live("click", function (e) {
        e.preventDefault();
        var $this = $(this);
        var $chartControl = $this.closest(".sf-chart-control");
        $.ajax({
            url: $this.attr("data-url"),
            data: $chartControl.find(":input").serialize() + "&filters=" + new SF.FindNavigator().serializeFilters(),
            success: function (result) {
                if (typeof result === "object") {
                    if (typeof result.ModelState != "undefined") {
                        var modelState = result.ModelState;
                        returnValue = new SF.Validator().showErrors(modelState, true);
                        SF.Notify.error(lang.signum.error, 2000);
                    }
                }
                else {
                    new SF.Validator().showErrors(null);
                    $chartControl.find(".sf-search-results-container").html(result);
                    SF.triggerNewContent($chartControl.find(".sf-search-results-container"));
                }
            }
        });
    });

    var reDraw = function ($chartContainer, force) {
        $chartContainer.html("");

        var data = $chartContainer.data("data");
        var width = $chartContainer.width();
        var height = $chartContainer.height();

        var $chartControl = $chartContainer.closest(".sf-chart-control");
        var myChart = SF.Chart.Factory.getGraphType($chartControl.find(".ui-widget-header .sf-chart-type-value").val());

        var $codeArea = $chartControl.find('.sf-chart-code-container textarea');
        if ($codeArea.val() == '' || force) {
            var containerSelector = "#" + $chartControl.attr("id") + " .sf-chart-container";
            $codeArea.val(myChart.createChartSVG(containerSelector) + myChart.paintChart(containerSelector));
        }

        eval($codeArea.val());
    };

    return {
        requestProcessedData: requestProcessedData,
        reDraw: reDraw
    };
})();

SF.Chart.Factory = (function () {
    var br = "\n",
        brt = "\n\t";

    var getGraphType = function (key) {
        return new SF.Chart[key]();
    };

    Array.prototype.enterData = function (data, tag, cssClass) {
        return this.selectAll(tag + "." + cssClass).data(data)
        .enter().append("svg:" + tag)
        .attr("class", cssClass);
    };

    return {
        br: br,
        brt: brt,
        getGraphType: getGraphType
    };
})();

SF.Chart.ChartBase = function () {
    var $span = $('<span>&nbsp;</span>').appendTo($('.sf-chart-container'));
    this.fontSize = $span.height();
    $span.remove();
};
SF.Chart.ChartBase.prototype = {

    br: SF.Chart.Factory.br,
    brt: SF.Chart.Factory.brt,
    ticksLength: 4,
    labelMargin: 5,
    chartAxisPadding: 5,
    padding: 5,

    createChartSVG: function (selector) {
        return "var chart = d3.select('" + selector + "')" + this.brt +
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
        return !SF.isEmpty(tokenValue) ? (typeof tokenValue.toStr != "undefined" ? tokenValue.toStr : tokenValue) : tokenValue;
    },

    getTokenKey: function (tokenValue) {
        return !SF.isEmpty(tokenValue) ? (typeof tokenValue.key != "undefined" ? tokenValue.key : tokenValue) : tokenValue;
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

    bindEvents: function (containerSelector) {
        return "//bind mouse events" + this.br +
            "myChart.bindMouseClick($('" + containerSelector + "'));" + this.br;
    },

    paintChart: function (containerSelector) {
        return this.init() +
        this.getXAxis() +
        this.getYAxis() +
        this.paintXAxisRuler() +
        this.paintYAxisRuler() +
        this.paintGraph() +
        this.paintXAxis() +
        this.paintYAxis() +
        this.paintLegend() +
        this.bindEvents(containerSelector);
    }, 

    bindMouseClick: function ($chartContainer) {
        $chartContainer.find('.shape,.slice,.hover-trigger,.point').not('g').click(function () {
            var $this = $(this);

            var extractAttribute = function ($shape, attrName) {
                var att = $shape.attr("data-" + attrName);
                if (typeof att != "undefined") {
                    return "&" + attrName + "=" + att;
                }
                else {
                    return "";
                }
            };

            var serializeData = function ($shape) {
                var data = $shape.closest(".sf-chart-control").find(":input").serialize();
                data += "&filters=" + new SF.FindNavigator().serializeFilters();
                data += extractAttribute($shape, "d1");
                data += extractAttribute($shape, "d2");
                data += extractAttribute($shape, "v1");
                data += extractAttribute($shape, "v2");
                data += extractAttribute($shape, "entity");
                return data;
            };

            $.ajax({
                url: $this.closest('.sf-chart-container').attr('data-open-url'),
                data: serializeData($this),
                success: function (popup) {
                    new SF.ViewNavigator({ prefix: "New" }).showViewOk(popup);
                }
            })
        });
    }
};

SF.Chart.Bars = function () {
    SF.Chart.ChartBase.call(this);
};
SF.Chart.Bars.prototype = $.extend({}, new SF.Chart.ChartBase(), {
    init: function () {
        return "//config variables" + this.br +
            "var fontSize= " + this.fontSize + "," + this.brt +
            "ticksLength= " + this.ticksLength + "," + this.brt +
            "labelMargin= " + this.labelMargin + "," + this.brt +
            "chartAxisPadding= " + this.chartAxisPadding + "," + this.brt +
            "padding= " + this.padding + "," + this.brt +
            "yAxisLeftPosition = padding + fontSize + (2 * labelMargin) + ticksLength," + this.brt +
            "xAxisTopPosition = height - padding - (fontSize * 2) - (labelMargin * 2) - ticksLength," + this.brt +
            "color = (data.serie.length < 10 ? d3.scale.category10() : d3.scale.category20()).domain([0, $.map(data.serie, function(v) { return JSON.stringify(v); })]);" + this.br + this.br;
    },

    getXAxis: function () {
        return "//x axis scale" + this.br +
            "var x = d3.scale.linear()" + this.brt +
            ".domain([0, d3.max($.map(data.serie, function (e) { return myChart.getTokenKey(e.value1); }))])" + this.brt +
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
            "//paint y-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-token-label').attr('transform', 'translate(' + fontSize + ', ' + (xAxisTopPosition / 2) + ') rotate(270)')" + this.brt +
            ".append('svg:text').attr('class', 'y-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(data.labels.dimension1);" + this.br + this.br;
    },

    paintGraph: function () {
        return "//paint graph" + this.br +
            "chart.append('svg:g').attr('class', 'shape').attr('transform' ,'translate(' + yAxisLeftPosition + ', ' + (padding + chartAxisPadding) + ')')" + this.brt +
            ".enterData(data.serie, 'rect', 'shape')" + this.brt +
            ".attr('width', function (v) { return x(myChart.getTokenKey(v.value1)); })" + this.brt +
            ".attr('height', y.rangeBand)" + this.brt +
            ".attr('y', function (v) { return y(JSON.stringify(v)); })" + this.brt +
            ".attr('fill', function (v) { return color(JSON.stringify(v)); })" + this.brt +
            ".attr('stroke', '#fff')" + this.brt +
            ".attr('data-d1', function(v) { return myChart.getTokenKey(v.dimension1); })" + this.brt +
            ".attr('data-entity', function(v) { return v.entity || ''; })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(d) { return myChart.getTokenLabel(d.dimension1) + ': ' + myChart.getTokenLabel(d.value1); });" + this.br +
            this.br +
            "//paint y-axis tick labels" + this.br +
            "var xHalf = (width - yAxisLeftPosition - padding) / 2;" + this.br +
            "if (y.rangeBand() > fontSize) {" + this.brt +
            "chart.append('svg:g').attr('class', 'y-axis-tick-label').attr('transform', 'translate(' + (yAxisLeftPosition + labelMargin) + ', ' + (padding + chartAxisPadding + (y.rangeBand() / 2) + (fontSize / 2)) + ')')" + this.brt +
            ".enterData(data.serie, 'text', 'y-axis-tick-label sf-chart-strong')" + this.brt +
            ".attr('x', function(v) { var posx = x(myChart.getTokenKey(v.value1)); return posx >= xHalf ? 0 : posx; })" + this.brt +
            ".attr('y', function (v) { return y(JSON.stringify(v)); })" + this.brt +
            ".attr('fill', function(v) { return x(myChart.getTokenKey(v.value1)) >= xHalf ? '#fff' : color(JSON.stringify(v)); })" + this.brt +
            ".text(function (v) { return myChart.getTokenLabel(v.dimension1); });" + this.br +
            "}" + this.br + this.br;
    }
});

SF.Chart.Columns = function () {
    SF.Chart.ChartBase.call(this);
};
SF.Chart.Columns.prototype = $.extend({}, new SF.Chart.ChartBase(), {

    init: function () {
        return "//config variables" + this.br +
            "var yAxisLabelWidth = 60," + this.brt +
            "fontSize= " + this.fontSize + "," + this.brt +
            "ticksLength= " + this.ticksLength + "," + this.brt +
            "labelMargin= " + this.labelMargin + "," + this.brt +
            "chartAxisPadding= " + this.chartAxisPadding + "," + this.brt +
            "padding= " + this.padding + "," + this.brt +
            "yAxisLeftPosition = padding + fontSize + yAxisLabelWidth + (2 * labelMargin) + ticksLength," + this.brt +
            "xAxisTopPosition = height - padding - fontSize - labelMargin - ticksLength," + this.brt +
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
            ".domain([0, d3.max($.map(data.serie, function (e) { return myChart.getTokenKey(e.value1); }))])" + this.brt +
            ".range([0, xAxisTopPosition - padding]);" + this.br + this.br;
    },

    paintXAxisRuler: function () {
        return "//paint x-axis ticks" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick').attr('transform', 'translate(' + (yAxisLeftPosition + (x.rangeBand() / 2) + chartAxisPadding) + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(data.serie, 'line', 'x-axis-tick')" + this.brt +
            ".attr('y2', ticksLength)" + this.brt +
            ".attr('x1', function (v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('x2', function (v) { return x(JSON.stringify(v)); });" + this.br +
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
            ".attr('height', function (v) { return y(myChart.getTokenKey(v.value1)); })" + this.brt +
            ".attr('width', x.rangeBand)" + this.brt +
            ".attr('x', function (v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('fill', function (v) { return color(JSON.stringify(v)); })" + this.brt +
            ".attr('stroke', '#fff')" + this.brt +
            ".attr('data-d1', function(v) { return myChart.getTokenKey(v.dimension1); })" + this.brt +
            ".attr('data-entity', function(v) { return v.entity || ''; })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(d) { return myChart.getTokenLabel(d.dimension1) + ': ' + myChart.getTokenLabel(d.value1); });" + this.br +
            this.br +
            "//paint x-axis tick labels" + this.br +
            "var yHalf = (xAxisTopPosition - padding) / 2;" + this.br +
            "if (x.rangeBand() > fontSize) {" + this.brt +
            "chart.append('svg:g').attr('class', 'x-axis-tick-label').attr('transform', 'translate(' + (yAxisLeftPosition + (x.rangeBand() / 2) + (fontSize / 2)) + ', ' + xAxisTopPosition + ') rotate(270)')" + this.brt +
            ".enterData(data.serie, 'text', 'x-axis-tick-label sf-chart-strong')" + this.brt +
            ".attr('x', function (v) { var posy = y(myChart.getTokenKey(v.value1)); return posy >= yHalf ? posy - labelMargin : posy + labelMargin; })" + this.brt +
            ".attr('y', function (v) { return x(JSON.stringify(v)) })" + this.brt +
            ".attr('text-anchor', function(v) { return y(myChart.getTokenKey(v.value1)) >= yHalf ? 'end' : 'start'; })" + this.brt +
	        ".attr('fill', function(v) { return y(myChart.getTokenKey(v.value1)) >= yHalf ? '#fff' : color(JSON.stringify(v)); })" + this.brt +
            ".text(function (v) { return myChart.getTokenLabel(v.dimension1); });" + this.br +
            "}" + this.br + +this.br;
    }
});

SF.Chart.Lines = function () {
    SF.Chart.Columns.call(this);
}
SF.Chart.Lines.prototype = $.extend({}, new SF.Chart.Columns(), {

    init: function () {
        return "//config variables" + this.br +
            "var yAxisLabelWidth = 60," + this.brt +
            "fontSize= " + this.fontSize + "," + this.brt +
            "ticksLength= " + this.ticksLength + "," + this.brt +
            "labelMargin= " + this.labelMargin + "," + this.brt +
            "chartAxisPadding= " + this.chartAxisPadding + "," + this.brt +
            "padding= " + this.padding + "," + this.brt +
            "yAxisLeftPosition = padding + fontSize + yAxisLabelWidth + (2 * labelMargin) + ticksLength," + this.brt +
            "xAxisTopPosition = height - padding - (fontSize * 3) - (labelMargin * 3) - ticksLength," + this.brt +
            "color = 'steelblue';" + this.br + this.br;
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

    paintGraph: function () {
        return "//paint graph - line" + this.br +
            "chart.append('svg:g').attr('class', 'shape').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".append('svg:path').attr('class', 'shape')" + this.brt +
            ".attr('stroke', color)" + this.brt +
            ".attr('fill', 'none')" + this.brt +
            ".attr('stroke-width', 3)" + this.brt +
            ".attr('shape-rendering', 'initial')" + this.brt +
            ".attr('d', myChart.getPathPoints($.map(data.serie, function(v) { return {x: x(JSON.stringify(v)), y: y(myChart.getTokenKey(v.value1))};})))" + this.br +
            this.br +
            "//paint graph - hover area trigger" + this.br +
            "chart.append('svg:g').attr('class', 'hover-trigger').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(data.serie, 'circle', 'hover-trigger')" + this.brt +
            ".attr('cx', function(v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('cy', function(v) { return y(myChart.getTokenKey(v.value1)); })" + this.brt +
            ".attr('r', 15)" + this.brt +
            ".attr('fill', '#fff')" + this.brt +
            ".attr('fill-opacity', 0)" + this.brt +
            ".attr('stroke', 'none')" + this.brt +
            ".attr('data-d1', function(v) { return myChart.getTokenKey(v.dimension1); })" + this.brt +
            ".attr('data-entity', function(v) { return v.entity || ''; })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(v) { return myChart.getTokenLabel(v.dimension1) + ': ' + myChart.getTokenLabel(v.value1); })" + this.br +
            this.br +
            "//paint graph - points" + this.br +
            "chart.append('svg:g').attr('class', 'point').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(data.serie, 'circle', 'point')" + this.brt +
            ".attr('fill', color)" + this.brt +
            ".attr('r', 5)" + this.brt +
            ".attr('cx', function(v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('cy', function(v) { return y(myChart.getTokenKey(v.value1)); })" + this.brt +
            ".attr('data-d1', function(v) { return myChart.getTokenKey(v.dimension1); })" + this.brt +
            ".attr('data-entity', function(v) { return v.entity || ''; })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(v) { return myChart.getTokenLabel(v.dimension1) + ': ' + myChart.getTokenLabel(v.value1); })" + this.br +
            this.br;
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
        var self = this;
        return d3.max($.map(completeArray, function (e) { return self.getTokenKey(e); }));
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

        var self = this;
        $.each(series, function (i, serie) {
            for (var i = 0; i < dimensionCount; i++) {
                var v = serie.values[i];
                if (!SF.isEmpty(v)) {
                    countArray[i] += self.getTokenKey(v);
                }
            }
        });

        return countArray;
    },

    init: function () {
        return "//config variables" + this.br +
            "var yAxisLabelWidth = 60," + this.brt +
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
            ".attr('d', function(s) { return myChart.getPathPoints($.map(s.values, function(v, i) { return { x: x(JSON.stringify(data.dimension1[i])), y: (SF.isEmpty(v) ? null : y(myChart.getTokenKey(v))) }; }) )})" + this.br +
            this.br +
            "//paint graph - hover area trigger" + this.br +
            "chart.enterData(data.series, 'g', 'hover-trigger-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(function(s) { return $.map(s.values, function(v){ return { dimension2: s.dimension2, value: v }; }); }, 'circle', 'point')" + this.brt +
            ".attr('cx', function(pair, i) { return x(JSON.stringify(data.dimension1[i])); })" + this.brt +
            ".attr('cy', function(pair) { return SF.isEmpty(pair) ? 0 : y(myChart.getTokenKey(pair.value)); })" + this.brt +
            ".attr('r', function(pair) { return SF.isEmpty(pair.value) ? 0 : 15; })" + this.brt +
            ".attr('fill', '#fff')" + this.brt +
            ".attr('fill-opacity', 0)" + this.brt +
            ".attr('stroke', 'none')" + this.brt +
            ".attr('data-d1', function(pair, i) { return myChart.getTokenKey(data.dimension1[i]); })" + this.brt +
            ".attr('data-d2', function(pair) { return myChart.getTokenKey(pair.dimension2); })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(pair, i) { return SF.isEmpty(pair.value) ? null : myChart.getTokenLabel(data.dimension1[i]) + ', ' + myChart.getTokenLabel(pair.dimension2) + ': ' + myChart.getTokenLabel(pair.value); });" + this.br +
            this.br +
            "//paint graph - points" + this.br +
            "chart.enterData(data.series, 'g', 'point-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(function(s) { return $.map(s.values, function(v){ return { dimension2: s.dimension2, value: v }; }); }, 'circle', 'point')" + this.brt +
            ".attr('fill', function(pair) { return color(JSON.stringify(pair.dimension2)); })" + this.brt +
            ".attr('fill-opacity', function(pair) { SF.isEmpty(pair.value) ? 0 : 100; })" + this.brt +
            ".attr('r', function(pair) { return SF.isEmpty(pair.value) ? 0 : 5; })" + this.brt +
            ".attr('cx', function(pair, i) { return x(JSON.stringify(data.dimension1[i])); })" + this.brt +
            ".attr('cy', function(pair) { return SF.isEmpty(pair.value) ? 0 : y(myChart.getTokenKey(pair.value)); })" + this.brt +
            ".attr('data-d1', function(pair, i) { return myChart.getTokenKey(data.dimension1[i]); })" + this.brt +
            ".attr('data-d2', function(pair) { return myChart.getTokenKey(pair.dimension2); })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(pair, i) { return SF.isEmpty(pair.value) ? null : myChart.getTokenLabel(data.dimension1[i]) + ', ' + myChart.getTokenLabel(pair.dimension2) + ': ' + myChart.getTokenLabel(pair.value); });" + this.br +
            this.br;
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
            ".attr('height', function(pair, i) { return SF.isEmpty(pair.value) ? 0 : y(myChart.getTokenKey(pair.value)); })" + this.brt +
            ".attr('data-d1', function(pair, i) { return myChart.getTokenKey(data.dimension1[i]); })" + this.brt +
            ".attr('data-d2', function(pair) { return myChart.getTokenKey(pair.dimension2); })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(pair, i) { return SF.isEmpty(pair.value) ? null : myChart.getTokenLabel(data.dimension1[i]) + ', ' + myChart.getTokenLabel(pair.dimension2) + ': ' + myChart.getTokenLabel(pair.value); });" + this.br +
            this.br;
    }
});

SF.Chart.StackedColumns = function () {
    SF.Chart.TypeTypeValue.call(this);
};
SF.Chart.StackedColumns.prototype = $.extend({}, new SF.Chart.TypeTypeValue(), {

    init: function () {
        return "//config variables" + this.br +
            "var yAxisLabelWidth = 60," + this.brt +
            "fontSize= " + this.fontSize + "," + this.brt +
            "ticksLength= " + this.ticksLength + "," + this.brt +
            "labelMargin= " + this.labelMargin + "," + this.brt +
            "chartAxisPadding= " + this.chartAxisPadding + "," + this.brt +
            "padding= " + this.padding + "," + this.brt +
            "yAxisLeftPosition = padding + fontSize + yAxisLabelWidth + (2 * labelMargin) + ticksLength," + this.brt +
            "xAxisTopPosition = height - padding - fontSize - labelMargin - ticksLength," + this.brt +
            "color = (data.series.length < 10 ? d3.scale.category10() : d3.scale.category20()).domain([0, $.map(data.series, function(s) { return JSON.stringify(s.dimension2); })]);" + this.br + this.br;
    },

    getYAxis: function () {
        return "//y axis scale" + this.br +
            "var y = d3.scale.linear()" + this.brt +
            ".domain([0, d3.max(myChart.createCountArray(data.series))])" + this.brt +
            ".range([0, xAxisTopPosition - padding - fontSize - labelMargin]);" + this.br + this.br;
    },

    paintXAxisRuler: function () {
        return "//paint x-axis ticks" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick').attr('transform', 'translate(' + (yAxisLeftPosition + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(data.dimension1, 'line', 'x-axis-tick')" + this.brt +
            ".attr('y2', ticksLength)" + this.brt +
            ".attr('x1', function (v) { return x(JSON.stringify(v)); })" + this.brt +
            ".attr('x2', function (v) { return x(JSON.stringify(v)); });" + this.br +
            this.br +
            "//paint x-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-token-label').attr('transform', 'translate(' + (yAxisLeftPosition + ((width - yAxisLeftPosition) / 2)) + ', ' + (height) + ')')" + this.brt +
            ".append('svg:text').attr('class', 'x-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(data.labels.dimension1);" + this.br + this.br;
    },

    paintGraph: function () {
        return "//paint graph" + this.br +
            "var emptyCountArray = myChart.createEmptyCountArray(data.dimension1.length);" + this.br +
            "chart.enterData(data.series, 'g', 'shape-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".enterData(function(s) { return $.map(s.values, function(v){ return { dimension2: s.dimension2, value: v }; }); }, 'rect', 'shape')" + this.brt +
            ".attr('stroke', function(pair) { return SF.isEmpty(pair.value) ? 'none' : '#fff'; })" + this.brt +
            ".attr('fill', function(pair) { return SF.isEmpty(pair.value) ? 'none' : color(JSON.stringify(pair.dimension2)); })" + this.brt +
            ".attr('transform', function(pair, i) { return 'translate(' + x(JSON.stringify(data.dimension1[i])) + ', 0)'; })" + this.brt +
            ".attr('width', x.rangeBand())" + this.brt +
            ".attr('height', function(pair, i) { return SF.isEmpty(pair.value) ? 0 : y(myChart.getTokenKey(pair.value)); })" + this.brt +
            ".attr('y', function(pair, i) { if (SF.isEmpty(pair.value)) { return 0; } else { var offset = y(emptyCountArray[i]); emptyCountArray[i] += pair.value; return offset; } })" + this.brt +
            ".attr('data-d1', function(pair, i) { return myChart.getTokenKey(data.dimension1[i]); })" + this.brt +
            ".attr('data-d2', function(pair) { return myChart.getTokenKey(pair.dimension2); })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(pair, i) { return SF.isEmpty(pair.value) ? null : myChart.getTokenLabel(data.dimension1[i]) + ', ' + myChart.getTokenLabel(pair.dimension2) + ': ' + myChart.getTokenLabel(pair.value); });" + this.br +
            this.br +
            "//paint x-axis tick labels" + this.br +
            "var countArray = myChart.createCountArray(data.series);" + this.br +
            "var yHalf = (xAxisTopPosition - padding) / 2;" + this.br +
            "if (x.rangeBand() > fontSize) {" + this.brt +
            "chart.append('svg:g').attr('class', 'x-axis-tick-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') rotate(270)')" + this.brt +
            ".enterData(data.series[0].values, 'text', 'x-axis-tick-label sf-chart-strong')" + this.brt +
            ".attr('x', function (v, i) { var posy = y(countArray[i]); return posy >= yHalf ? posy - labelMargin : posy + labelMargin; })" + this.brt +
            ".attr('y', function (v, i) { return x(JSON.stringify(data.dimension1[i])) })" + this.brt +
            ".attr('text-anchor', function (v, i) { var posy = y(countArray[i]); return posy >= yHalf ? 'end' : 'start'; })" + this.brt +
            ".attr('fill', function(v, i) { return y(countArray[i]) >= yHalf ? '#fff' : '#000'; })" + this.brt +
            ".text(function (v, i) { return myChart.getTokenLabel(data.dimension1[i]); });" + this.br +
            "}" + this.br +
            this.br;
    }
});

SF.Chart.TotalColumns = function () {
    SF.Chart.StackedColumns.call(this);
};
SF.Chart.TotalColumns.prototype = $.extend({}, new SF.Chart.StackedColumns(), {

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
            ".attr('height', function(pair, i) { return SF.isEmpty(pair.value) ? 0 : y((100 * myChart.getTokenKey(pair.value)) / countArray[i]); })" + this.brt +
            ".attr('y', function(pair, i) { if (SF.isEmpty(pair.value)) { return 0; } else { var offset = emptyCountArray[i]; emptyCountArray[i] += myChart.getTokenKey(pair.value); return y((100 * offset) / countArray[i]); } })" + this.brt +
            ".attr('data-d1', function(pair, i) { return myChart.getTokenKey(data.dimension1[i]); })" + this.brt +
            ".attr('data-d2', function(pair) { return myChart.getTokenKey(pair.dimension2); })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(pair, i) { return SF.isEmpty(pair.value) ? null : myChart.getTokenLabel(data.dimension1[i]) + ', ' + myChart.getTokenLabel(pair.dimension2) + ': ' + myChart.getTokenLabel(pair.value); });" + this.br +
            this.br +
            "//paint x-axis tick labels" + this.br +
            "var yHalf = (xAxisTopPosition - padding) / 2;" + this.br +
            "if (x.rangeBand() > fontSize) {" + this.brt +
            "chart.append('svg:g').attr('class', 'x-axis-tick-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + chartAxisPadding + (x.rangeBand() / 2)) + ', 0) rotate(270)')" + this.brt +
            ".enterData(data.series[0].values, 'text', 'x-axis-tick-label sf-chart-strong')" + this.brt +
            ".attr('x', -yHalf)" + this.brt +
            ".attr('y', function (v, i) { return x(JSON.stringify(data.dimension1[i])) })" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".attr('fill', '#fff')" + this.brt +
            ".text(function (v, i) { return myChart.getTokenLabel(data.dimension1[i]); });" + this.br +
            "}" + this.br +
            this.br;
    }
});

SF.Chart.HorizontalTypeTypeValue = function () {
    SF.Chart.TypeTypeValue.call(this);
}
SF.Chart.HorizontalTypeTypeValue.prototype = $.extend({}, new SF.Chart.TypeTypeValue(), {
    init: function () {
        return "//config variables" + this.br +
            "var fontSize= " + this.fontSize + "," + this.brt +
            "ticksLength= " + this.ticksLength + "," + this.brt +
            "labelMargin= " + this.labelMargin + "," + this.brt +
            "chartAxisPadding= " + this.chartAxisPadding + "," + this.brt +
            "padding= " + this.padding + "," + this.brt +
            "yAxisLeftPosition = padding + fontSize + (2 * labelMargin) + ticksLength," + this.brt +
            "xAxisTopPosition = height - padding - (fontSize * 2) - (labelMargin * 2) - ticksLength," + this.brt +
            "color = (data.series.length < 10 ? d3.scale.category10() : d3.scale.category20()).domain([0, $.map(data.series, function(s) { return JSON.stringify(s.dimension2); })]);" + this.br + this.br;
    },

    getXAxis: function () {
        return "//x axis scale" + this.br +
            "var x = d3.scale.linear()" + this.brt +
            ".domain([0, myChart.getMaxValue1(data.series)])" + this.brt +
            ".range([0, width - yAxisLeftPosition - padding]);" + this.br + this.br;
    },

    getYAxis: function () {
        return "//y axis scale" + this.br +
            "var y = d3.scale.ordinal()" + this.brt +
            ".domain($.map(data.dimension1, function (v) { return JSON.stringify(v); }))" + this.brt +
            ".rangeBands([0, xAxisTopPosition - padding - fontSize - labelMargin - (2 * chartAxisPadding)]);" + this.br + this.br;
    },

    paintXAxisRuler: function () {
        return "//paint x-axis - ruler" + this.br +
            "var xTicks = x.ticks(10);" + this.br +
            "chart.append('svg:g').attr('class', 'x-ruler').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + (padding + fontSize + labelMargin) + ')')" + this.brt +
            ".enterData(xTicks, 'line', 'x-ruler')" + this.brt +
            ".attr('x1', x)" + this.brt +
            ".attr('x2', x)" + this.brt +
            ".attr('y2', xAxisTopPosition - padding - fontSize - labelMargin);" + this.br +
            this.br +
            "//paint x-axis ticks" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(xTicks, 'line', 'x-axis-tick')" + this.brt +
            ".attr('y2', ticksLength)" + this.brt +
            ".attr('x1', x)" + this.brt +
            ".attr('x2', x);" + this.br +
            this.br +
            "//paint x-axis tick labels" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick-label').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + (xAxisTopPosition + ticksLength + labelMargin + fontSize) + ')')" + this.brt +
            ".enterData(xTicks, 'text', 'x-axis-tick-label')" + this.brt +
            ".attr('x', x)" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(String);" + this.br +
            this.br +
            "//paint x-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-token-label').attr('transform', 'translate(' + (yAxisLeftPosition + ((width - yAxisLeftPosition) / 2)) + ', ' + (height) + ')')" + this.brt +
            ".append('svg:text').attr('class', 'x-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(data.labels.value1);" + this.br + this.br;
    },

    paintYAxisRuler: function () {
        return "//paint y-axis - ticks" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength) + ', ' + (padding + fontSize + labelMargin + chartAxisPadding + (y.rangeBand() / 2)) + ')')" + this.brt +
            ".enterData(data.dimension1, 'line', 'y-axis-tick')" + this.brt +
            ".attr('x2', ticksLength)" + this.brt +
            ".attr('y1', function (v) { return y(JSON.stringify(v)); })" + this.brt +
            ".attr('y2', function (v) { return y(JSON.stringify(v)); });" + this.br +
            this.br +
            "//paint y-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-token-label').attr('transform', 'translate(' + fontSize + ', ' + ((xAxisTopPosition - fontSize - labelMargin) / 2) + ') rotate(270)')" + this.brt +
            ".append('svg:text').attr('class', 'y-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(data.labels.dimension1);" + this.br + this.br;
    }
});

SF.Chart.MultiBars = function () {
    SF.Chart.HorizontalTypeTypeValue.call(this);
};
SF.Chart.MultiBars.prototype = $.extend({}, new SF.Chart.HorizontalTypeTypeValue(), {

    init: function () {
        return "//config variables" + this.br +
            "var yAxisLabelWidth = 100," + this.brt +
            "fontSize= " + this.fontSize + "," + this.brt +
            "ticksLength= " + this.ticksLength + "," + this.brt +
            "labelMargin= " + this.labelMargin + "," + this.brt +
            "chartAxisPadding= " + this.chartAxisPadding + "," + this.brt +
            "padding= " + this.padding + "," + this.brt +
            "yAxisLeftPosition = padding + fontSize + + yAxisLabelWidth + (2 * labelMargin) + ticksLength," + this.brt +
            "xAxisTopPosition = height - padding - (fontSize * 2) - (labelMargin * 2) - ticksLength," + this.brt +
            "color = (data.series.length < 10 ? d3.scale.category10() : d3.scale.category20()).domain([0, $.map(data.series, function(s) { return JSON.stringify(s.dimension2); })]);" + this.br + this.br;
    },

    paintYAxisRuler: function () {
        return "//paint y-axis - ticks" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength) + ', ' + (padding + fontSize + labelMargin + chartAxisPadding + (y.rangeBand() / 2)) + ')')" + this.brt +
            ".enterData(data.dimension1, 'line', 'y-axis-tick')" + this.brt +
            ".attr('x2', ticksLength)" + this.brt +
            ".attr('y1', function (v) { return y(JSON.stringify(v)); })" + this.brt +
            ".attr('y2', function (v) { return y(JSON.stringify(v)); });" + this.br +
            this.br +
            "//paint y-axis - tick labels" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-tick-label').attr('transform', 'translate(' + (yAxisLeftPosition - ticksLength - labelMargin) + ', ' + (padding + fontSize + labelMargin + chartAxisPadding + (y.rangeBand() / 2)) + ')')" + this.brt +
            ".enterData(data.dimension1, 'text', 'y-axis-tick-label')" + this.brt +
            ".attr('y', function (v) { return y(JSON.stringify(v)); })" + this.brt +
            ".attr('dominant-baseline', 'middle')" + this.brt +
            ".attr('text-anchor', 'end')" + this.brt +
            ".text(function (v) { return myChart.getTokenLabel(v); });" + this.br +
            this.br +
            "//paint y-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'y-axis-token-label').attr('transform', 'translate(' + fontSize + ', ' + ((xAxisTopPosition - fontSize - labelMargin) / 2) + ') rotate(270)')" + this.brt +
            ".append('svg:text').attr('class', 'y-axis-token-label')" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(data.labels.dimension1);" + this.br + this.br;
    },

    paintGraph: function () {
        return "//graph y-subscale" + this.br +
            "var ySubscale = d3.scale.ordinal()" + this.brt +
            ".domain($.map(data.series, function (s) { return JSON.stringify(s.dimension2); }))" + this.brt +
            ".rangeBands([0, y.rangeBand()]);" + this.br +
            this.br +
            "//paint graph" + this.br +
            "chart.enterData(data.series, 'g', 'shape-serie').attr('transform' ,'translate(' + yAxisLeftPosition + ', ' + (padding + fontSize + labelMargin + chartAxisPadding) + ')')" + this.brt +
            ".enterData(function(s) { return $.map(s.values, function(v){ return { dimension2: s.dimension2, value: v }; }); }, 'rect', 'shape')" + this.brt +
            ".attr('stroke', function(pair) { return SF.isEmpty(pair.value) ? 'none' : '#fff'; })" + this.brt +
            ".attr('fill', function(pair) { return SF.isEmpty(pair.value) ? 'none' : color(JSON.stringify(pair.dimension2)); })" + this.brt +
            ".attr('y', function(pair, i) { return ySubscale(JSON.stringify(pair.dimension2)); })" + this.brt +
            ".attr('transform',  function(pair, i) { return 'translate(0, ' + y(JSON.stringify(data.dimension1[i])) + ')'; })" + this.brt +
            ".attr('height', ySubscale.rangeBand())" + this.brt +
            ".attr('width', function(pair, i) { return SF.isEmpty(pair.value) ? 0 : x(myChart.getTokenLabel(pair.value)); })" + this.brt +
            ".attr('data-d1', function(pair, i) { return myChart.getTokenKey(data.dimension1[i]); })" + this.brt +
            ".attr('data-d2', function(pair) { return myChart.getTokenKey(pair.dimension2); })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(pair, i) { return SF.isEmpty(pair.value) ? null : myChart.getTokenLabel(data.dimension1[i]) + ', ' + myChart.getTokenLabel(pair.dimension2) + ': ' + myChart.getTokenLabel(pair.value); })" + this.br +
             this.br;
    }
});

SF.Chart.StackedBars = function () {
    SF.Chart.HorizontalTypeTypeValue.call(this);
};
SF.Chart.StackedBars.prototype = $.extend({}, new SF.Chart.HorizontalTypeTypeValue(), {

    getXAxis: function () {
        return "//x axis scale" + this.br +
            "var x = d3.scale.linear()" + this.brt +
            ".domain([0, d3.max(myChart.createCountArray(data.series))])" + this.brt +
            ".range([0, width - yAxisLeftPosition - padding]);" + this.br + this.br;
    },

    paintGraph: function () {
        return "//paint graph" + this.br +
            "var emptyCountArray = myChart.createEmptyCountArray(data.dimension1.length);" + this.br +
            "chart.enterData(data.series, 'g', 'shape-serie').attr('transform' ,'translate(' + yAxisLeftPosition + ', ' + (padding + fontSize + labelMargin + chartAxisPadding) + ')')" + this.brt +
            ".enterData(function(s) { return $.map(s.values, function(v){ return { dimension2: s.dimension2, value: v }; }); }, 'rect', 'shape')" + this.brt +
            ".attr('stroke', function(pair) { return SF.isEmpty(pair.value) ? 'none' : '#fff'; })" + this.brt +
            ".attr('fill', function(pair) { return SF.isEmpty(pair.value) ? 'none' : color(JSON.stringify(pair.dimension2)); })" + this.brt +
            ".attr('transform',  function(pair, i) { return 'translate(0, ' + y(JSON.stringify(data.dimension1[i])) + ')'; })" + this.brt +
            ".attr('height', y.rangeBand())" + this.brt +
            ".attr('width', function(pair, i) { return SF.isEmpty(pair.value) ? 0 : x(myChart.getTokenLabel(pair.value)); })" + this.brt +
            ".attr('x', function(pair, i) { if (SF.isEmpty(pair.value)) { return 0; } else { var offset = x(emptyCountArray[i]); emptyCountArray[i] += pair.value; return offset; } })" + this.brt +
            ".attr('data-d1', function(pair, i) { return myChart.getTokenKey(data.dimension1[i]); })" + this.brt +
            ".attr('data-d2', function(pair) { return myChart.getTokenKey(pair.dimension2); })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(pair, i) { return SF.isEmpty(pair.value) ? null : myChart.getTokenLabel(data.dimension1[i]) + ', ' + myChart.getTokenLabel(pair.dimension2) + ': ' + myChart.getTokenLabel(pair.value); });" + this.br +
            this.br +
            "//paint y-axis - tick labels" + this.br +
            "if (y.rangeBand() > fontSize) {" + this.brt +
            "var xHalf = (width - padding - yAxisLeftPosition) / 2;" + this.brt +
            "var countArray = myChart.createCountArray(data.series);" + this.brt +
            "chart.append('svg:g').attr('class', 'y-axis-tick-label').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + (padding + fontSize + labelMargin + chartAxisPadding + (y.rangeBand() / 2) + (fontSize / 2)) + ')')" + this.brt +
            ".enterData(data.dimension1, 'text', 'y-axis-tick-label sf-chart-strong')" + this.brt +
            ".attr('x', function (v, i) { var posx = x(countArray[i]); return posx >= xHalf ? posx - labelMargin : posx + labelMargin; })" + this.brt +
            ".attr('y', function (v) { return y(JSON.stringify(v)); })" + this.brt +
            ".attr('text-anchor', function (v, i) { var posx = x(countArray[i]); return posx >= xHalf ? 'end' : 'start'; })" + this.brt +
            ".attr('fill', function (v, i) { var posx = x(countArray[i]); return posx >= xHalf ? '#fff' : '#000'; })" + this.brt +
            ".text(function (v, i) { return myChart.getTokenLabel(data.dimension1[i]); });" + this.br +
            "}" + this.br +
            this.br;
    }
});

SF.Chart.TotalBars = function () {
    SF.Chart.HorizontalTypeTypeValue.call(this);
};
SF.Chart.TotalBars.prototype = $.extend({}, new SF.Chart.HorizontalTypeTypeValue(), {

    getXAxis: function () {
        return "//x axis scale" + this.br +
            "var x = d3.scale.linear()" + this.brt +
            ".domain([0, 100])" + this.brt +
            ".range([0, width - yAxisLeftPosition - padding]);" + this.br + this.br;
    },

    paintGraph: function () {
        return "//paint graph" + this.br +
            "var countArray = myChart.createCountArray(data.series);" + this.br +
            "var emptyCountArray = myChart.createEmptyCountArray(data.dimension1.length);" + this.br +
            "chart.enterData(data.series, 'g', 'shape-serie').attr('transform' ,'translate(' + yAxisLeftPosition + ', ' + (padding + fontSize + labelMargin + chartAxisPadding) + ')')" + this.brt +
            ".enterData(function(s) { return $.map(s.values, function(v){ return { dimension2: s.dimension2, value: v }; }); }, 'rect', 'shape')" + this.brt +
            ".attr('stroke', function(pair) { return SF.isEmpty(pair.value) ? 'none' : '#fff'; })" + this.brt +
            ".attr('fill', function(pair) { return SF.isEmpty(pair.value) ? 'none' : color(JSON.stringify(pair.dimension2)); })" + this.brt +
            ".attr('transform',  function(pair, i) { return 'translate(0, ' + y(JSON.stringify(data.dimension1[i])) + ')'; })" + this.brt +
            ".attr('height', y.rangeBand())" + this.brt +
            ".attr('width', function(pair, i) { return SF.isEmpty(pair.value) ? 0 : x((100 * pair.value) / countArray[i]); })" + this.brt +
            ".attr('x', function(pair, i) { if (SF.isEmpty(pair.value)) { return 0; } else { var offset = emptyCountArray[i]; emptyCountArray[i] += pair.value; return x((100 * offset) / countArray[i]); } })" + this.brt +
            ".attr('data-d1', function(pair, i) { return myChart.getTokenKey(data.dimension1[i]); })" + this.brt +
            ".attr('data-d2', function(pair) { return myChart.getTokenKey(pair.dimension2); })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(pair, i) { return SF.isEmpty(pair.value) ? null : myChart.getTokenLabel(data.dimension1[i]) + ', ' + myChart.getTokenLabel(pair.dimension2) + ': ' + myChart.getTokenLabel(pair.value); })" + this.br +
            this.br +
            "//paint y-axis - tick labels" + this.br +
            "if (y.rangeBand() > fontSize) {" + this.brt +
            "var xHalf = (width - padding - yAxisLeftPosition) / 2;" + this.brt +
            "chart.append('svg:g').attr('class', 'y-axis-tick-label').attr('transform', 'translate(' + xHalf + ', ' + (padding + fontSize + labelMargin + chartAxisPadding + (y.rangeBand() / 2) + (fontSize / 2)) + ')')" + this.brt +
            ".enterData(data.dimension1, 'text', 'y-axis-tick-label sf-chart-strong')" + this.brt +
            ".attr('y', function (v) { return y(JSON.stringify(v)); })" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".attr('fill', '#fff')" + this.brt +
            ".text(function (v, i) { return myChart.getTokenLabel(data.dimension1[i]); });" + this.br +
            "}" + this.br +
            this.br;
    }
});

SF.Chart.StackedAreas = function () {
    SF.Chart.TypeTypeValue.call(this);
};
SF.Chart.StackedAreas.prototype = $.extend({}, new SF.Chart.TypeTypeValue(), {

    getYAxis: function () {
        return "//y axis scale" + this.br +
            "var y = d3.scale.linear()" + this.brt +
            ".domain([0, d3.max(myChart.createCountArray(data.series))])" + this.brt +
            ".range([0, xAxisTopPosition - padding - fontSize - labelMargin]);" + this.br + this.br;
    },

    paintGraph: function () {
        return "//paint graph" + this.br +
            "var countArray = myChart.createEmptyCountArray(data.dimension1.length);" + this.br +
            "chart.enterData(data.series, 'g', 'shape-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".append('svg:path').attr('class', 'shape')" + this.brt +
            ".attr('stroke', function(s) { return color(JSON.stringify(s.dimension2)); })" + this.brt +
            ".attr('fill', function(s) { return color(JSON.stringify(s.dimension2)); })" + this.brt +
            ".attr('shape-rendering', 'initial')" + this.brt +
            ".attr('d', function(s) { return myChart.getPathPoints($.merge(" + this.brt + "\t" +
            "$.map(countArray, function(v, i) { return { x: x(JSON.stringify(data.dimension1[i])), y: y(countArray[i]) }; }).reverse(), " + this.brt + "\t" +
            "$.map(s.values, function(v, i) { var offset = y(countArray[i]); countArray[i] += v; return { x: x(JSON.stringify(data.dimension1[i])), y: offset + y(SF.isEmpty(v) ? 0 : v) }; }) ))})" + this.brt +
            ".attr('data-d2', function(v) { return myChart.getTokenKey(v.dimension2); })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(s) { return myChart.getTokenLabel(s.dimension2); })" + this.br +
             this.br;
    }
});

SF.Chart.TotalAreas = function () {
    SF.Chart.TypeTypeValue.call(this);
};
SF.Chart.TotalAreas.prototype = $.extend({}, new SF.Chart.TypeTypeValue(), {

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
            "chart.enterData(data.series, 'g', 'shape-serie').attr('transform' ,'translate(' + (yAxisLeftPosition + (x.rangeBand() / 2)) + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".append('svg:path').attr('class', 'shape')" + this.brt +
            ".attr('stroke', function(s) { return color(JSON.stringify(s.dimension2)); })" + this.brt +
            ".attr('fill', function(s) { return color(JSON.stringify(s.dimension2)); })" + this.brt +
            ".attr('shape-rendering', 'initial')" + this.brt +
            ".attr('d', function(s) { return myChart.getPathPoints($.merge(" + this.brt + "\t" +
            "$.map(countArray, function(v, i) { return { x: x(JSON.stringify(data.dimension1[i])), y: y((100 * emptyCountArray[i]) / countArray[i]) }; }).reverse(), " + this.brt + "\t" +
            "$.map(s.values, function(v, i) { var offset = emptyCountArray[i]; emptyCountArray[i] += v; return { x: x(JSON.stringify(data.dimension1[i])), y: y((100 * (offset + (SF.isEmpty(v) ? 0 : v))) / countArray[i]) }; }) ))})" + this.brt +
            ".attr('data-d2', function(v) { return myChart.getTokenKey(v.dimension2); })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(s) { return myChart.getTokenLabel(s.dimension2); })" + this.br +
             this.br;
    }
});

SF.Chart.Points = function () {
    SF.Chart.ChartBase.call(this);
};
SF.Chart.Points.prototype = $.extend({}, new SF.Chart.ChartBase(), {

    init: function () {
        return "//config variables" + this.br +
            "var yAxisLabelWidth = 60," + this.brt +
            "fontSize= " + this.fontSize + "," + this.brt +
            "ticksLength= " + this.ticksLength + "," + this.brt +
            "labelMargin= " + this.labelMargin + "," + this.brt +
            "chartAxisPadding= " + this.chartAxisPadding + "," + this.brt +
            "padding= " + this.padding + "," + this.brt +
            "yAxisLeftPosition = padding + fontSize + yAxisLabelWidth + (2 * labelMargin) + ticksLength," + this.brt +
            "xAxisTopPosition = height - padding - (fontSize * 2) - (labelMargin * 2) - ticksLength," + this.brt +
            "color = (data.points.length < 10 ? d3.scale.category10() : d3.scale.category20()).domain([0, $.map(data.points, function(v) { return JSON.stringify(v); })]);" + this.br + this.br;
    },

    getXAxis: function () {
        return "//x axis scale" + this.br +
            "var x = d3.scale.linear()" + this.brt +
            ".domain([0, d3.max($.map(data.points, function (e) { return myChart.getTokenKey(e.dimension1); }))])" + this.brt +
            ".range([0, width - yAxisLeftPosition - padding]);" + this.br + this.br;
    },

    getYAxis: function () {
        return "//y axis scale" + this.br +
            "var y = d3.scale.linear()" + this.brt +
            ".domain([0, d3.max($.map(data.points, function (e) { return myChart.getTokenKey(e.dimension2); }))])" + this.brt +
            ".range([0, xAxisTopPosition - padding]);" + this.br + this.br;
    },

    paintXAxisRuler: function () {
        return "//paint x-axis - ticks" + this.br +
            "var xTicks = x.ticks(10);" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + xAxisTopPosition + ')')" + this.brt +
            ".enterData(xTicks, 'line', 'x-axis-tick')" + this.brt +
            ".attr('x1', x)" + this.brt +
            ".attr('x2', x)" + this.brt +
            ".attr('y2', ticksLength);" + this.br +
            this.br +
            "//paint x-axis - tick labels" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-tick-label').attr('transform', 'translate(' + yAxisLeftPosition + ', ' + (xAxisTopPosition + ticksLength + labelMargin + fontSize) + ')')" + this.brt +
            ".enterData(xTicks, 'text', 'x-axis-tick-label')" + this.brt +
            ".attr('x', x)" + this.brt +
            ".attr('text-anchor', 'middle')" + this.brt +
            ".text(String);" + this.br +
            this.br +
            "//paint x-axis - token label" + this.br +
            "chart.append('svg:g').attr('class', 'x-axis-token-label').attr('transform', 'translate(' + (yAxisLeftPosition + ((width - yAxisLeftPosition) / 2)) + ', ' + height + ')')" + this.brt +
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
            ".text(data.labels.dimension2);" + this.br + this.br;
    },

    paintGraph: function () {
        return "//paint graph" + this.br +
            "chart.enterData(data.points, 'g', 'shape-serie').attr('transform' ,'translate(' + yAxisLeftPosition + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".append('svg:circle').attr('class', 'shape')" + this.brt +
            ".attr('stroke', function(p) { return color(JSON.stringify(p)); })" + this.brt +
            ".attr('fill', function(p) { return color(JSON.stringify(p)); })" + this.brt +
            ".attr('shape-rendering', 'initial')" + this.brt +
            ".attr('r', 5)" + this.brt +
            ".attr('cx', function(p) { return x(myChart.getTokenKey(p.dimension1)); })" + this.brt +
            ".attr('cy', function(p) { return y(myChart.getTokenKey(p.dimension2)); })" + this.brt +
            ".attr('data-v1', function(p) { return myChart.getTokenKey(p.value1); })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(p) { return myChart.getTokenLabel(p.value1) + ': ' + myChart.getTokenLabel(p.dimension1) + ', ' + myChart.getTokenLabel(p.dimension2); })" + this.br +
             this.br;
    }
});

SF.Chart.Bubbles = function () {
    SF.Chart.Points.call(this);
};
SF.Chart.Bubbles.prototype = $.extend({}, new SF.Chart.Points(), {

    getSizeScale: function (data, area) {
        var sum = 0;
        var self = this;
        $.each(data.points, function (i, p) {
            sum += self.getTokenKey(p.value2);
        });

        return d3.scale.linear()
            .domain([0, sum])
            .range([0, area]);
    },

    paintGraph: function () {
        return "//paint graph" + this.br +
            "var sizeScale = myChart.getSizeScale(data, (width - yAxisLeftPosition) * (height - xAxisTopPosition));" + this.br +
            "chart.enterData(data.points, 'g', 'shape-serie').attr('transform' ,'translate(' + yAxisLeftPosition + ', ' + xAxisTopPosition + ') scale(1, -1)')" + this.brt +
            ".append('svg:circle').attr('class', 'shape')" + this.brt +
            ".attr('stroke', function(p) { return color(JSON.stringify(p)); })" + this.brt +
            ".attr('fill', function(p) { return color(JSON.stringify(p)); })" + this.brt +
            ".attr('shape-rendering', 'initial')" + this.brt +
            ".attr('r', function(p) { return Math.sqrt(sizeScale(myChart.getTokenKey(p.value2))/Math.PI); })" + this.brt +
            ".attr('cx', function(p) { return x(myChart.getTokenKey(p.dimension1)); })" + this.brt +
            ".attr('cy', function(p) { return y(myChart.getTokenKey(p.dimension2)); })" + this.brt +
            ".attr('data-v1', function(p) { return myChart.getTokenKey(p.value1); })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(p) { return '(' + myChart.getTokenLabel(p.dimension1) + ', ' + myChart.getTokenLabel(p.dimension2) + ') ' + myChart.getTokenLabel(p.value1) + ': ' + myChart.getTokenLabel(p.value2); })" + this.br +
             this.br;
    }
});

SF.Chart.Pie = function () {
    SF.Chart.ChartBase.call(this);
};
SF.Chart.Pie.prototype = $.extend({}, new SF.Chart.ChartBase(), {

    init: function () {
        return "//config variables" + this.br +
            "var fontSize= " + this.fontSize + "," + this.brt +
            "labelMargin= " + this.labelMargin + "," + this.brt +
            "padding= " + this.padding + "," + this.brt +
            "r = d3.min([((width - padding) / 2), (height - padding)]) / 3;" + this.brt +
            "rInner = 0," + this.brt +
            "color = (data.serie.length < 10 ? d3.scale.category10() : d3.scale.category20()).domain([0, $.map(data.serie, function(v) { return JSON.stringify(v); })]);" + this.br + this.br;
    },

    getXAxis: function () { return ""; },

    getYAxis: function () { return ""; },

    paintXAxisRuler: function () { return ""; },

    paintYAxisRuler: function () { return ""; },

    paintXAxis: function () { return ""; },

    paintYAxis: function () { return ""; },

    paintLegend: function () {
        return "//paint color legend" + this.br +
            "var cx = (width / 2)," + this.brt +
            "cy = (height / 2)," + this.brt +
            "legendRadius = 1.2;" + this.br +
            "chart.append('svg:g').data([data.serie]).attr('class', 'color-legend').attr('transform', 'translate(' + cx + ', ' + cy + ')')" + this.brt +
            ".enterData(pie, 'g', 'color-legend')" + this.brt +
            ".append('svg:text').attr('class', 'color-legend sf-chart-strong')" + this.brt +
            ".attr('x', function(slice) { var m = (slice.endAngle + slice.startAngle) / 2; return Math.sin(m) * r * legendRadius; })" + this.brt +
            ".attr('y', function(slice) { var m = (slice.endAngle + slice.startAngle) / 2; return -Math.cos(m) * r * legendRadius; })" + this.brt +
            ".attr('text-anchor', function(slice) { var m = (slice.endAngle + slice.startAngle) / 2; var cuadr = Math.floor(m * 4 / Math.PI); return (cuadr == 1 || cuadr == 2) ? 'start' : (cuadr == 5 || cuadr == 6) ? 'end' : 'middle'; })" + this.brt +
            ".attr('fill', function(slice) { return color(JSON.stringify(slice.data)); })" + this.brt +
            ".text(function(slice){ return ((slice.endAngle - slice.startAngle) >= (Math.PI / 16)) ? myChart.getTokenLabel(slice.data.dimension1) : ''; });" + this.br +
            this.br;
    },

    paintGraph: function () {
        return "//paint graph" + this.br +
            "var arc = d3.svg.arc().outerRadius(r).innerRadius(rInner);" + this.br +
            "var pie = d3.layout.pie().value(function(v) { return myChart.getTokenKey(v.value1); });" + this.br + this.br +
            "chart.append('svg:g').data([data.serie]).attr('class', 'shape').attr('transform', 'translate(' + (width / 2) + ', ' + (height / 2) + ')')" + this.brt +
            ".enterData(pie, 'g', 'slice')" + this.brt +
            ".append('svg:path').attr('class', 'shape')" + this.brt +
            ".attr('d', arc)" + this.brt +
            ".attr('fill', function(slice) { return color(JSON.stringify(slice.data)); })" + this.brt +
            ".attr('shape-rendering', 'initial')" + this.brt +
            ".attr('data-d1', function(slice) { return myChart.getTokenKey(slice.data.dimension1); })" + this.brt +
            ".attr('data-entity', function(slice) { return slice.data.entity || ''; })" + this.brt +
            ".append('svg:title')" + this.brt +
            ".text(function(slice) { return myChart.getTokenLabel(slice.data.dimension1) + ': ' + myChart.getTokenLabel(slice.data.value1); });" + this.br + this.br;
    }
});

SF.Chart.Doughnout = function () {
    SF.Chart.Pie.call(this);
};
SF.Chart.Doughnout.prototype = $.extend({}, new SF.Chart.Pie(), {

    init: function () {
        return "//config variables" + this.br +
            "var fontSize= " + this.fontSize + "," + this.brt +
            "labelMargin= " + this.labelMargin + "," + this.brt +
            "padding= " + this.padding + "," + this.brt +
            "r = d3.min([width, height - fontSize - labelMargin]) / 3;" + this.brt +
            "rInner = r / 2," + this.brt +
            "color = (data.serie.length < 10 ? d3.scale.category10() : d3.scale.category20()).domain([0, $.map(data.serie, function(v) { return JSON.stringify(v); })]);" + this.br + this.br;
    }
});