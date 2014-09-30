/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/d3/d3.d.ts"/>
define(["require", "exports", "d3"], function(require, exports, d3) {
    function initStats() {
        $(document).on("click", "table.sf-stats-table a.sf-stats-show", function (e) {
            e.preventDefault();
            $(this).closest("tr").next().toggle();
        });

        exports.init();
    }
    exports.initStats = initStats;

    function init() {
        var $profileEnable = $("#sfProfileEnable");
        var $profileDisable = $("#sfProfileDisable");

        $profileEnable.click(function (e) {
            e.preventDefault();
            $.ajax({
                url: $(this).attr("href"),
                success: function () {
                    $profileEnable.hide();
                    $profileDisable.show();
                    window.location.href = window.location.href;
                }
            });
        });

        $profileDisable.click(function (e) {
            e.preventDefault();
            $.ajax({
                url: $(this).attr("href"),
                success: function () {
                    $profileDisable.hide();
                    $profileEnable.show();
                    window.location.href = window.location.href;
                }
            });
        });

        $("#sfProfileClear").click(function (e) {
            e.preventDefault();
            $.ajax({
                url: $(this).attr("href"),
                success: function () {
                    window.location.href = window.location.href;
                }
            });
        });
    }
    exports.init = init;

    function heavyDetailsChart(data, currentDepth) {
        var fontSize = 12;
        var fontPadding = 3;
        var minDepth = d3.min(data, function (e) {
            return e.Depth;
        });
        var maxDepth = d3.max(data, function (e) {
            return e.Depth;
        });

        var $chartContainer = $(".sf-profiler-chart");
        var width = $chartContainer.width();
        var height = ((fontSize * 2) + (3 * fontPadding)) * (maxDepth + 1);
        $chartContainer.height(height);

        var currentEntry = data.filter(function (e) {
            return e.Depth == currentDepth;
        })[0];

        var x = d3.scale.linear().domain([currentEntry.BeforeStart, currentEntry.End]).range([0, width]);

        var y = d3.scale.linear().domain([0, maxDepth + 1]).range([0, height]);

        var entryHeight = y(1);

        var chart = d3.select('.sf-profiler-chart').append('svg:svg').attr('width', width).attr('height', height);

        var groups = chart.selectAll("g.entry").data(data).enter().append('svg:g').attr('class', 'entry').attr('data-full-index', function (v) {
            return v.FullIndex;
        });

        var rectangles = groups.append('svg:rect').attr('class', 'shape').attr('x', function (v) {
            return v.Depth < currentDepth ? x(currentEntry.BeforeStart) : x(v.BeforeStart);
        }).attr('y', function (v) {
            return y(v.Depth);
        }).attr('width', function (v) {
            return v.Depth < currentDepth ? x(currentEntry.End) - x(currentEntry.BeforeStart) : x(v.End) - x(v.BeforeStart);
        }).attr('height', entryHeight - 1).attr('fill', function (v) {
            return v.Color;
        }).attr('stroke', function (v) {
            return v.Depth == currentDepth ? '#000' : '#ccc';
        });

        groups.append('svg:rect').attr('class', 'shape-before').attr('x', function (v) {
            return v.Depth < currentDepth ? 0 : x(v.BeforeStart);
        }).attr('y', function (v) {
            return v.Depth < currentDepth ? 0 : y(v.Depth) + 1;
        }).attr('width', function (v) {
            return v.Depth < currentDepth ? 0 : x(v.Start) - x(v.BeforeStart);
        }).attr('height', function (v) {
            return v.Depth < currentDepth ? 0 : entryHeight - 2;
        }).attr('fill', '#fff');

        var labelsTop = groups.append('svg:text').attr('class', 'label label-top').attr('dx', function (v) {
            return v.Depth < currentDepth ? x(currentEntry.Start) + 3 : x(v.Start) + 3;
        }).attr('dy', function (v) {
            return y(v.Depth);
        }).attr('y', fontPadding + fontSize).attr('fill', function (v) {
            return v.Depth == currentDepth ? '#000' : '#fff';
        }).text(function (v) {
            return v.Elapsed;
        });

        var labelsBottom = groups.append('svg:text').attr('class', 'label label-bottom').attr('dx', function (v) {
            return v.Depth < currentDepth ? x(currentEntry.Start) + 3 : x(v.Start) + 3;
        }).attr('dy', function (v) {
            return y(v.Depth);
        }).attr('y', (2 * fontPadding) + (2 * fontSize)).attr('fill', function (v) {
            return v.Depth == currentDepth ? '#000' : '#fff';
        }).text(function (v) {
            return v.Role + " - " + v.AdditionalData;
        });

        rectangles.append('svg:title').text(function (v) {
            return v.Elapsed + " - " + v.AdditionalData;
        });
        labelsTop.append('svg:title').text(function (v) {
            return v.Elapsed + " - " + v.AdditionalData;
        });
        labelsBottom.append('svg:title').text(function (v) {
            return v.Elapsed + " - " + v.AdditionalData;
        });

        $('g.entry').on('click', function (evt) {
            var $this = $(this);
            var url = $this.closest('.sf-profiler-chart').attr('data-detail-url');
            url = url + (url.indexOf("?") >= 0 ? "&indices=" : "?indices=") + $this.attr('data-full-index');
            if (evt.ctrlKey) {
                window.open(url);
            } else {
                window.location.href = url;
            }
        });
    }
    exports.heavyDetailsChart = heavyDetailsChart;

    function heavyListChart(data) {
        exports.init();

        var fontSize = 12;
        var fontPadding = 4;
        var characterWidth = 7;
        var labelWidth = 60 * characterWidth;
        var rightMargin = 10 * characterWidth;

        var $chartContainer = $(".sf-profiler-chart");
        var width = $chartContainer.width();
        var height = (fontSize + (2 * fontPadding)) * (data.length);
        $chartContainer.height(height);

        var minStart = d3.min($.map(data, function (e) {
            return e.Start;
        }));
        var maxEnd = d3.max($.map(data, function (e) {
            return e.End;
        }));

        var x = d3.scale.linear().domain([minStart, maxEnd]).range([labelWidth + 3, width - rightMargin]);

        var y = d3.scale.linear().domain([0, data.length]).range([0, height - 1]);

        var entryHeight = y(1);

        var chart = d3.select('.sf-profiler-chart').append('svg:svg').attr('width', width).attr('height', height);

        var groups = chart.selectAll("g.entry").data(data).enter().append('svg:g').attr('class', 'entry').attr('data-full-index', function (v) {
            return v.FullIndex;
        });

        groups.append('svg:rect').attr('class', 'left-background').attr('x', 0).attr('y', function (v, i) {
            return y(i);
        }).attr('width', labelWidth).attr('height', entryHeight).attr('fill', '#ddd').attr('stroke', '#fff');

        var labelsLeft = groups.append('svg:text').attr('class', 'label label-left').attr('dy', function (v, i) {
            return y(i);
        }).attr('y', fontPadding + fontSize).attr('fill', '#000').text(function (v) {
            return v.AdditionalData;
        });

        groups.append('svg:rect').attr('class', 'right-background').attr('x', labelWidth).attr('y', function (v, i) {
            return y(i);
        }).attr('width', width - labelWidth).attr('height', entryHeight).attr('fill', '#fff').attr('stroke', '#ddd');

        var rectangles = groups.append('svg:rect').attr('class', 'shape').attr('x', function (v) {
            return x(v.Start);
        }).attr('y', function (v, i) {
            return y(i);
        }).attr('width', function (v) {
            return x(v.End) - x(v.Start);
        }).attr('height', entryHeight).attr('fill', function (v) {
            return v.Color;
        });

        var labelsRight = groups.append('svg:text').attr('class', 'label label-right').attr('dx', function (v) {
            return x(v.End) + 3;
        }).attr('dy', function (v, i) {
            return y(i);
        }).attr('y', fontPadding + fontSize).attr('fill', '#000').text(function (v) {
            return v.Elapsed;
        });

        rectangles.append('svg:title').text(function (v) {
            return v.Elapsed + " - " + v.AdditionalData;
        });
        labelsLeft.append('svg:title').text(function (v) {
            return v.Elapsed + " - " + v.AdditionalData;
        });
        labelsRight.append('svg:title').text(function (v) {
            return v.Elapsed + " - " + v.AdditionalData;
        });

        $('g.entry').on('click', function (evt) {
            var $this = $(this);
            var url = $this.closest('.sf-profiler-chart').attr('data-detail-url');
            url = url + (url.indexOf("?") >= 0 ? "&indices=" : "?indices=") + $this.attr('data-full-index');
            if (evt.ctrlKey) {
                window.open(url);
            } else {
                window.location.href = url;
            }
        });

        $('g.entry').on('mouseover', '.left-background,.label,.right-background,.shape', function () {
            var $this = $(this);
            var $group = $this.closest('.entry');
            $group.find('.left-background').attr('fill', '#aaa');
            $group.find('.label-left').attr('fill', '#fff');
            $group.find('.right-background').attr('fill', '#eee');
        });

        $('g.entry').on('mouseleave', '.left-background,.label,.right-background,.shape', function () {
            var $this = $(this);
            var $group = $this.closest('.entry');
            $group.find('.left-background').attr('fill', '#ddd');
            $group.find('.label-left').attr('fill', '#000');
            $group.find('.right-background').attr('fill', '#fff');
        });
    }
    exports.heavyListChart = heavyListChart;
});
//# sourceMappingURL=Profiler.js.map
