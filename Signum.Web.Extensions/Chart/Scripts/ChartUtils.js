/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/d3/d3.d.ts"/>
var ChartUtils;
(function (ChartUtils) {
    Array.prototype.enterData = function (data, tag, cssClass) {
        return this.selectAll(tag + "." + cssClass).data(data)
            .enter().append("svg:" + tag)
            .attr("class", cssClass);
    };
    function fillAllTokenValueFuntions(data) {
        for (var i = 0;; i++) {
            if (data.columns['c' + i] === undefined)
                break;
            for (var j = 0; j < data.rows.length; j++) {
                makeItTokenValue(data.rows[j]['c' + i]);
            }
        }
    }
    ChartUtils.fillAllTokenValueFuntions = fillAllTokenValueFuntions;
    function makeItTokenValue(value) {
        if (value === null || value === undefined)
            return;
        value.toString = function () {
            var key = (this.key !== undefined ? this.key : this);
            if (key === null || key === undefined)
                return "null";
            return key;
        };
        value.niceToString = function () {
            var result = (this.toStr !== undefined ? this.toStr : this);
            if (result === null || result === undefined)
                return this.key != undefined ? "[ no text ]" : "[ null ]";
            return result.toString();
        };
    }
    ChartUtils.makeItTokenValue = makeItTokenValue;
    function ellipsis(elem, width, padding, ellipsisSymbol) {
        if (ellipsisSymbol === null || ellipsisSymbol == undefined)
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
                if (tokenValue != null) {
                    options += "&" + k + "=" + (tokenValue.keyForFilter || tokenValue.toString());
                }
            }
        }
        return options;
    }
    ChartUtils.getClickKeys = getClickKeys;
    function translate(x, y) {
        if (y === undefined)
            return 'translate(' + x + ')';
        return 'translate(' + x + ',' + y + ')';
    }
    ChartUtils.translate = translate;
    function scale(x, y) {
        if (y === undefined)
            return 'scale(' + x + ')';
        return 'scale(' + x + ',' + y + ')';
    }
    ChartUtils.scale = scale;
    function rotate(angle, x, y) {
        if (x === undefined || y == undefined)
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
            return d3.scale.ordinal()
                .domain(values)
                .rangeBands([minRange, maxRange]);
        if (scaleName == "ZeroMax")
            return d3.scale.linear()
                .domain([0, d3.max(values)])
                .range([minRange, maxRange]);
        if (scaleName == "MinMax") {
            if (column.type == "Date" || column.type == "DateTime") {
                var scale = d3.time.scale()
                    .domain([new Date(d3.min(values)), new Date(d3.max(values))])
                    .range([minRange, maxRange]);
                var f = function (d) { return scale(new Date(d)); };
                f.ticks = scale.ticks;
                f.tickFormat = scale.tickFormat;
                return f;
            }
            else {
                return d3.scale.linear()
                    .domain([d3.min(values), d3.max(values)])
                    .range([minRange, maxRange]);
            }
        }
        if (scaleName == "Log")
            return d3.scale.log()
                .domain([d3.min(values),
                d3.max(values)])
                .range([minRange, maxRange]);
        if (scaleName == "Sqrt")
            return d3.scale.pow().exponent(.5)
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
    var Rule = (function () {
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
            var keys = d3.keys(this.sizes);
            //paint x-axis rule
            chart.append('svg:g').attr('class', 'x-rule-tick')
                .enterData(keys, 'line', 'x-rule-tick')
                .attr('x1', function (d) { return this.ends[d]; })
                .attr('x2', function (d) { return this.ends[d]; })
                .attr('y1', 0)
                .attr('y2', 10000)
                .style('stroke-width', 2)
                .style('stroke', 'Pink');
            //paint y-axis rule labels
            chart.append('svg:g').attr('class', 'x-axis-rule-label')
                .enterData(keys, 'text', 'x-axis-rule-label')
                .attr('transform', function (d, i) {
                return translate(this.starts[d] + this.sizes[d] / 2 - 5, 10 + 100 * (i % 3)) +
                    rotate(90);
            })
                .attr('fill', 'DeepPink')
                .text(function (d) { return d; });
        };
        Rule.prototype.debugY = function (chart) {
            var keys = d3.keys(this.sizes);
            //paint y-axis rule
            chart.append('svg:g').attr('class', 'y-rule-tick')
                .enterData(keys, 'line', 'y-rule-tick')
                .attr('x1', 0)
                .attr('x2', 10000)
                .attr('y1', function (d) { return this.ends[d]; })
                .attr('y2', function (d) { return this.ends[d]; })
                .style('stroke-width', 2)
                .style('stroke', 'Violet');
            //paint y-axis rule labels
            chart.append('svg:g').attr('class', 'y-axis-rule-label')
                .enterData(keys, 'text', 'y-axis-rule-label')
                .attr('transform', function (d, i) { return translate(100 * (i % 3), this.starts[d] + this.sizes[d] / 2 + 4); })
                .attr('fill', 'DarkViolet')
                .text(function (d) { return d; });
        };
        return Rule;
    })();
    ChartUtils.Rule = Rule;
    function toTree(elements, getKey, getParent) {
        var root = { item: null, children: [] };
        var dic = {};
        function getOrCreateNode(elem) {
            var key = getKey(elem);
            if (dic[key])
                return dic[key];
            var node = { item: elem, children: [] };
            var parent = getParent(elem);
            if (parent) {
                var parentNode = getOrCreateNode(parent);
                parentNode.children.push(node);
            }
            else {
                root.children.push(node);
            }
            dic[key] = node;
            return node;
        }
        elements.forEach(getOrCreateNode);
        return root.children;
    }
    ChartUtils.toTree = toTree;
})(ChartUtils || (ChartUtils = {}));
//# sourceMappingURL=ChartUtils.js.map