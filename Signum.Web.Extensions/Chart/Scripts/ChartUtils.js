/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
define(["require", "exports", "d3"], function (require, exports, d3) {
    "use strict";
    //import d3sc = require("d3-scale-chromatic");
    var d3sc = {};
    var ChartUtils;
    (function (ChartUtils) {
        d3.select(document).__proto__.enterData = function (data, tag, cssClass) {
            return this.selectAll(tag + "." + cssClass).data(data)
                .enter().append("svg:" + tag)
                .attr("class", cssClass);
        };
        function fillAllTokenValueFuntions(data) {
            for (var i = 0;; i++) {
                if (data.columns['c' + i] == undefined)
                    break;
                for (var j = 0; j < data.rows.length; j++) {
                    makeItTokenValue(data.rows[j]['c' + i]);
                }
            }
        }
        ChartUtils.fillAllTokenValueFuntions = fillAllTokenValueFuntions;
        function makeItTokenValue(value) {
            if (value == undefined)
                return;
            value.toString = function () {
                var key = (this.key !== undefined ? this.key : this);
                if (key == undefined)
                    return "null";
                return key.toString();
            };
            value.valueOf = function () { return this.key; };
            value.niceToString = function () {
                var result = (this.toStr !== undefined ? this.toStr : this);
                if (result == undefined)
                    return this.key != undefined ? "[ no text ]" : "[ null ]";
                return result.toString();
            };
        }
        ChartUtils.makeItTokenValue = makeItTokenValue;
        function ellipsis(elem, width, padding, ellipsisSymbol) {
            if (ellipsisSymbol == undefined)
                ellipsisSymbol = '…';
            if (padding)
                width -= padding * 2;
            var self = d3.select(elem);
            var textLength = self.node().getComputedTextLength();
            var text = self.text();
            while (textLength > width && text.length > 0) {
                text = text.slice(0, -1);
                while (text[text.length - 1] == ' ' && text.length > 0)
                    text = text.slice(0, -1);
                self.text(text + ellipsisSymbol);
                textLength = self.node().getComputedTextLength();
            }
        }
        ChartUtils.ellipsis = ellipsis;
        function getClickKeys(row, columns) {
            var options = "";
            for (var k in columns) {
                var col = columns[k];
                if (col.isGroupKey == true) {
                    var tokenValue = row[k];
                    if (tokenValue != undefined) {
                        options += "&" + k + "=" + (tokenValue.keyForFilter || tokenValue.toString());
                    }
                }
            }
            return options;
        }
        ChartUtils.getClickKeys = getClickKeys;
        function translate(x, y) {
            if (y == undefined)
                return 'translate(' + x + ')';
            return 'translate(' + x + ',' + y + ')';
        }
        ChartUtils.translate = translate;
        function scale(x, y) {
            if (y == undefined)
                return 'scale(' + x + ')';
            return 'scale(' + x + ',' + y + ')';
        }
        ChartUtils.scale = scale;
        function rotate(angle, x, y) {
            if (x == undefined || y == undefined)
                return 'rotate(' + angle + ')';
            return 'rotate(' + angle + ',' + y + ',' + y + ')';
        }
        ChartUtils.rotate = rotate;
        function skewX(angle) {
            return 'skewX(' + angle + ')';
        }
        ChartUtils.skewX = skewX;
        function skewY(angle) {
            return 'skewY(' + angle + ')';
        }
        ChartUtils.skewY = skewY;
        function matrix(a, b, c, d, e, f) {
            return 'matrix(' + a + ',' + b + ',' + c + ',' + d + ',' + e + ',' + f + ')';
        }
        ChartUtils.matrix = matrix;
        function scaleFor(column, values, minRange, maxRange, scaleName) {
            if (scaleName == "Elements")
                return d3.scaleBand()
                    .domain(values)
                    .range([minRange, maxRange]);
            if (scaleName == "ZeroMax")
                return d3.scaleLinear()
                    .domain([0, d3.max(values)])
                    .range([minRange, maxRange]);
            if (scaleName == "MinMax") {
                if (column.type == "Date" || column.type == "DateTime") {
                    var scale_1 = d3.scaleTime()
                        .domain([new Date(d3.min(values)), new Date(d3.max(values))])
                        .range([minRange, maxRange]);
                    var f = function (d) { return scale_1(new Date(d)); };
                    f.ticks = scale_1.ticks;
                    f.tickFormat = scale_1.tickFormat;
                    return f;
                }
                else {
                    return d3.scaleLinear()
                        .domain([d3.min(values), d3.max(values)])
                        .range([minRange, maxRange]);
                }
            }
            if (scaleName == "Log")
                return d3.scaleLog()
                    .domain([d3.min(values),
                    d3.max(values)])
                    .range([minRange, maxRange]);
            if (scaleName == "Sqrt")
                return d3.scalePow().exponent(.5)
                    .domain([d3.min(values),
                    d3.max(values)])
                    .range([minRange, maxRange]);
            throw Error("Unexpected scale: " + scaleName);
        }
        ChartUtils.scaleFor = scaleFor;
        function rule(object, totalSize) {
            return new Rule(object, totalSize);
        }
        ChartUtils.rule = rule;
        var Rule = /** @class */ (function () {
            function Rule(object, totalSize) {
                this.sizes = {};
                this.starts = {};
                this.ends = {};
                var fixed = 0;
                var proportional = 0;
                for (var p in object) {
                    var value = object[p];
                    if (typeof value === 'number')
                        fixed += value;
                    else if (Rule.isStar(value))
                        proportional += Rule.getStar(value);
                    else
                        throw new Error("values should be numbers or *");
                }
                if (!totalSize) {
                    if (proportional)
                        throw new Error("totalSize is mandatory if * is used");
                    totalSize = fixed;
                }
                this.totalSize = totalSize;
                var remaining = totalSize - fixed;
                var star = proportional <= 0 ? 0 : remaining / proportional;
                for (var p in object) {
                    var value = object[p];
                    if (typeof value === 'number')
                        this.sizes[p] = value;
                    else if (Rule.isStar(value))
                        this.sizes[p] = Rule.getStar(value) * star;
                }
                var acum = 0;
                for (var p in this.sizes) {
                    this.starts[p] = acum;
                    acum += this.sizes[p];
                    this.ends[p] = acum;
                }
            }
            Rule.isStar = function (val) {
                return typeof val === 'string' && val[val.length - 1] == '*';
            };
            Rule.getStar = function (val) {
                if (val === '*')
                    return 1;
                return parseFloat(val.substring(0, val.length - 1));
            };
            Rule.prototype.size = function (name) {
                return this.sizes[name];
            };
            Rule.prototype.start = function (name) {
                return this.starts[name];
            };
            Rule.prototype.end = function (name) {
                return this.ends[name];
            };
            Rule.prototype.middle = function (name) {
                return this.starts[name] + this.sizes[name] / 2;
            };
            Rule.prototype.debugX = function (chart) {
                var _this = this;
                var keys = d3.keys(this.sizes);
                //paint x-axis rule
                chart.append('svg:g').attr('class', 'x-rule-tick')
                    .enterData(keys, 'line', 'x-rule-tick')
                    .attr('x1', function (d) { return _this.ends[d]; })
                    .attr('x2', function (d) { return _this.ends[d]; })
                    .attr('y1', 0)
                    .attr('y2', 10000)
                    .style('stroke-width', 2)
                    .style('stroke', 'Pink');
                //paint y-axis rule labels
                chart.append('svg:g').attr('class', 'x-axis-rule-label')
                    .enterData(keys, 'text', 'x-axis-rule-label')
                    .attr('transform', function (d, i) {
                    return translate(_this.starts[d] + _this.sizes[d] / 2 - 5, 10 + 100 * (i % 3)) +
                        rotate(90);
                })
                    .attr('fill', 'DeepPink')
                    .text(function (d) { return d; });
            };
            Rule.prototype.debugY = function (chart) {
                var _this = this;
                var keys = d3.keys(this.sizes);
                //paint y-axis rule
                chart.append('svg:g').attr('class', 'y-rule-tick')
                    .enterData(keys, 'line', 'y-rule-tick')
                    .attr('x1', 0)
                    .attr('x2', 10000)
                    .attr('y1', function (d) { return _this.ends[d]; })
                    .attr('y2', function (d) { return _this.ends[d]; })
                    .style('stroke-width', 2)
                    .style('stroke', 'Violet');
                //paint y-axis rule labels
                chart.append('svg:g').attr('class', 'y-axis-rule-label')
                    .enterData(keys, 'text', 'y-axis-rule-label')
                    .attr('transform', function (d, i) { return translate(100 * (i % 3), _this.starts[d] + _this.sizes[d] / 2 + 4); })
                    .attr('fill', 'DarkViolet')
                    .text(function (d) { return d; });
            };
            return Rule;
        }());
        ChartUtils.Rule = Rule;
        function getStackOffset(curveName) {
            switch (curveName) {
                case "zero": return d3.stackOffsetNone;
                case "expand": return d3.stackOffsetExpand;
                case "silhouette": return d3.stackOffsetSilhouette;
                case "wiggle": return d3.stackOffsetWiggle;
            }
            return undefined;
        }
        ChartUtils.getStackOffset = getStackOffset;
        function getStackOrder(schemeName) {
            switch (schemeName) {
                case "none": return d3.stackOrderNone;
                case "ascending": return d3.stackOrderAscending;
                case "descending": return d3.stackOrderDescending;
                case "insideOut": return d3.stackOrderInsideOut;
                case "reverse": return d3.stackOrderReverse;
            }
            return undefined;
        }
        ChartUtils.getStackOrder = getStackOrder;
        function getCurveByName(curveName) {
            switch (curveName) {
                case "basis": return d3.curveBasis;
                case "bundle": return d3.curveBundle.beta(0.5);
                case "cardinal": return d3.curveCardinal;
                case "catmull-rom": return d3.curveCatmullRom;
                case "linear": return d3.curveLinear;
                case "monotone": return d3.curveMonotoneX;
                case "natural": return d3.curveNatural;
                case "step": return d3.curveStep;
                case "step-after": return d3.curveStepAfter;
                case "step-before": return d3.curveStepBefore;
            }
            return undefined;
        }
        ChartUtils.getCurveByName = getCurveByName;
        function getColorInterpolation(interpolationName) {
            switch (interpolationName) {
                case "YlGn": return d3sc.interpolateYlGn;
                case "YlGnBu": return d3sc.interpolateYlGnBu;
                case "GnBu": return d3sc.interpolateGnBu;
                case "BuGn": return d3sc.interpolateBuGn;
                case "PuBuGn": return d3sc.interpolatePuBuGn;
                case "PuBu": return d3sc.interpolatePuBu;
                case "BuPu": return d3sc.interpolateBuPu;
                case "RdPu": return d3sc.interpolateRdPu;
                case "PuRd": return d3sc.interpolatePuRd;
                case "OrRd": return d3sc.interpolateOrRd;
                case "YlOrRd": return d3sc.interpolateYlOrRd;
                case "YlOrBr": return d3sc.interpolateYlOrBr;
                case "Purples": return d3sc.interpolatePurples;
                case "Blues": return d3sc.interpolateBlues;
                case "Greens": return d3sc.interpolateGreens;
                case "Oranges": return d3sc.interpolateOranges;
                case "Reds": return d3sc.interpolateReds;
                case "Greys": return d3sc.interpolateGreys;
                case "PuOr": return d3sc.interpolatePuOr;
                case "BrBG": return d3sc.interpolateBrBG;
                case "PRGn": return d3sc.interpolatePRGn;
                case "PiYG": return d3sc.interpolatePiYG;
                case "RdBu": return d3sc.interpolateRdBu;
                case "RdGy": return d3sc.interpolateRdGy;
                case "RdYlBu": return d3sc.interpolateRdYlBu;
                case "Spectral": return d3sc.interpolateSpectral;
                case "RdYlGn": return d3sc.interpolateRdYlGn;
            }
            return undefined;
        }
        ChartUtils.getColorInterpolation = getColorInterpolation;
        function getColorScheme(schemeName) {
            switch (schemeName) {
                case "category10": return d3.schemeCategory10;
                case "category20": return d3.schemeCategory20;
                case "category20b": return d3.schemeCategory20b;
                case "category20c": return d3.schemeCategory20c;
                case "accent": return d3sc.schemeAccent;
                case "dark2": return d3sc.schemeDark2;
                case "paired": return d3sc.schemePaired;
                case "pastel1": return d3sc.schemePastel1;
                case "pastel2": return d3sc.schemePastel2;
                case "set1": return d3sc.schemeSet1;
                case "set2": return d3sc.schemeSet2;
                case "set3": return d3sc.schemeSet3;
            }
            return undefined;
        }
        ChartUtils.getColorScheme = getColorScheme;
        function stratifyTokens(data, keyColumn, /*Employee*/ keyColumnParent) {
            var folders = data.rows
                .filter(function (r) { return r[keyColumnParent] && r[keyColumnParent].key; })
                .map(function (r) { return ({ folder: r[keyColumnParent] }); })
                .toObjectDistinct(function (r) { return r.folder.key.toString(); });
            var root = { isRoot: true };
            var NullConst = "- Null -";
            var dic = data.rows.filter(function (r) { return r[keyColumn].key != null; }).toObjectDistinct(function (r) { return r[keyColumn].key; });
            var getParent = function (d) {
                if (d.isRoot)
                    return null;
                if (d.folder) {
                    var r = dic[d.folder.key];
                    if (!r)
                        return root;
                    var parentValue = r[keyColumnParent];
                    if (!parentValue || !parentValue.key)
                        return root; //Either null
                    return folders[parentValue.key]; // Parent folder
                }
                if (d[keyColumn]) {
                    var r = d;
                    var fold = r[keyColumn] && r[keyColumn].key != null && folders[r[keyColumn].key];
                    if (fold)
                        return fold; //My folder
                    var parentValue = r[keyColumnParent];
                    var parentFolder = parentValue && parentValue.key && folders[parentValue.key];
                    if (!parentFolder)
                        return root; //No key an no parent
                    return folders[parentFolder.folder.key]; //only parent
                }
                throw new Error("Unexpected " + JSON.stringify(d));
            };
            var getKey = function (r) {
                if (r.isRoot)
                    return "#Root";
                if (r.folder)
                    return "F#" + r.folder.key;
                var cr = r;
                if (cr[keyColumn].key != null)
                    return cr[keyColumn].key;
                return NullConst;
            };
            var rootNode = d3.stratify()
                .id(getKey)
                .parentId(function (r) {
                var parent = getParent(r);
                return parent ? getKey(parent) : null;
            })([root].concat(Object.values(folders), data.rows));
            return rootNode;
        }
        ChartUtils.stratifyTokens = stratifyTokens;
        function toPivotTable(data, col0, /*Employee*/ otherCols) {
            var usedCols = otherCols
                .filter(function (cn) { return data.columns[cn].token != undefined; });
            var rows = data.rows
                .map(function (r) {
                return {
                    rowValue: r[col0],
                    values: usedCols.toObject(function (cn) { return cn; }, function (cn) { return ({
                        rowClick: r,
                        value: r[cn],
                    }); })
                };
            });
            var title = otherCols
                .filter(function (cn) { return data.columns[cn].token != undefined; })
                .map(function (cn) { return data.columns[cn].title; })
                .join(" | ");
            return {
                title: title,
                columns: d3.values(usedCols.toObject(function (c) { return c; }, function (c) { return ({
                    color: null,
                    key: c,
                    niceName: data.columns[c].title,
                }); })),
                rows: rows,
            };
        }
        ChartUtils.toPivotTable = toPivotTable;
        function groupedPivotTable(data, col0, /*Employee*/ colSplit, colValue) {
            var columns = d3.values(data.rows.toObjectDistinct(function (cr) { return cr[colSplit].key; }, function (cr) { return ({
                niceName: cr[colSplit].niceToString(),
                color: cr[colSplit].color,
                key: cr[colSplit].key,
            }); }));
            var rows = data.rows.groupBy(function (r) { return "k" + r[col0].key; })
                .map(function (gr) {
                return {
                    rowValue: gr.elements[0][col0],
                    values: gr.elements.toObject(function (r) { return r[colSplit].key; }, function (r) { return ({ value: r[colValue], rowClick: r }); }),
                };
            });
            var title = data.columns.c2.title + " / " + data.columns.c1.title;
            return {
                title: title,
                columns: columns,
                rows: rows,
            };
        }
        ChartUtils.groupedPivotTable = groupedPivotTable;
    })(ChartUtils || (ChartUtils = {}));
    return ChartUtils;
});
//# sourceMappingURL=ChartUtils.js.map