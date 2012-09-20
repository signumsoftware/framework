var SF = SF || {};

SF.Chart = SF.Chart || {};

SF.Chart.Utils = (function () {

    Array.prototype.enterData = function (data, tag, cssClass) {
        return this.selectAll(tag + "." + cssClass).data(data)
        .enter().append("svg:" + tag)
        .attr("class", cssClass);
    };

    var result = {

        getLabel: function (tokenValue) {
            var result = (tokenValue !== null && tokenValue.toStr !== undefined ? tokenValue.toStr : tokenValue);

            if (result === null || result === undefined)
                return tokenValue.key !== undefined ? "[ no text ]" : "[ null ]";

            return result;
        },

        getKey: function (tokenValue) {
            var result = (tokenValue !== null && tokenValue.key !== undefined ? tokenValue.key : tokenValue);

            if (result === null || result === undefined)
                return "null";

            return result;
        },

        getColor: function (tokenValue, color) {
            return ((tokenValue !== null && tokenValue.color != /*or null*/undefined) ? tokenValue.color : null) || color(SF.Chart.Utils.getKey(tokenValue));
        },

        getClickKeys: function (row, columns) {
            var options = "";
            for (var k in columns) {
                var col = columns[k];
                if (col.isGroupKey == true) {
                    var cell = row[k];
                    var value = SF.Chart.Utils.getKey(cell);
                    options += "&" + k + "=" + (value === null ? "" : value);
                }
            }

            return options;
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

        toObject: function (array, keySelector, valueSelector) {
            var result = {};
            var ks = $.isFunction(keySelector) ? keySelector : function (obj) { return obj[keySelector]; };
            var vs = valueSelector == null ? null : $.isFunction(valueSelector) ? valueSelector : function (obj) { return obj[valueSelector]; };
            for (var i = 0; i < array.length; i++) {
                var value = array[i];
                var key = ks(value, i);
                if (result[key] !== undefined)
                    throw new Error("Duplicated key " + key);
                result[key] = vs == null ? value : vs(value)
            }
            return result;
        },

        groupBy: function (array, keySelector, reducer) {
            var result = {};
            var ks = _.isFunction(keySelector) ? keySelector : function (obj) { return obj[keySelector]; };
            for (var i = 0; i < array.length; i++) {
                var value = array[i];
                var key = ks(value, i);
                (result[key] || (result[key] = [])).push(value);
            }

            if (reducer != undefined) {
                for (var a in result)
                    result[a] = reducer(result[a]);
            }

            return result;
        },


        distinct: function (array, keySelector) {
            var set = {};
            var result = [];
            var ks = $.isFunction(keySelector) ? keySelector : function (obj) { return obj[keySelector]; };
            for (var i = 0; i < array.length; i++) {
                var value = array[i];
                var key = ks(value, i);
                if (set[key] === undefined) {
                    set[key] = true;
                    result.push(key);
                }
            }
            return result;
        },

        translate: function (x, y) {
            if (y === undefined)
                return 'translate(' + x + ')';

            return 'translate(' + x + ',' + y + ')';
        },

        scale: function (x, y) {
            if (y === undefined)
                return 'scale(' + x + ')';

            return 'scale(' + x + ',' + y + ')';
        },

        rotate: function (angle, x, y) {
            if (x === undefined || y == undefined)
                return 'rotate(' + angle + ')';

            return 'rotate(' + angle + ',' + y + ',' + y + ')';
        },

        skewX: function (angle) {
            return 'skewX(' + angle + ')';
        },

        skewY: function (angle) {
            return 'skewY(' + angle + ')';
        },

        matrix: function (a, b, c, d, e, f) {
            return 'matrix(' + a + ',' + b + ',' + c + ',' + d + ',' + e + ',' + f + ')';
        },

        rule: function (object, totalSize) {
            function isStar(val) {
                return typeof val === 'string' & val[val.length - 1] == '*';
            }

            function getStar(val) {
                if (val === '*')
                    return 1;

                return parseFloat(val.substring(0, val.length - 1));
            }

            var fixed = 0;
            var proportional = 0;
            for (var p in object) {
                var value = object[p];
                if (typeof value === 'number')
                    fixed += value;
                else if (isStar(value))
                    proportional += getStar(value);
                else
                    throw new Error("values should be numbers or *");
            }

            var remaining = totalSize - fixed;
            var star = proportional <= 0 ? 0 : remaining / proportional;

            var sizes = {};

            for (var p in object) {
                var value = object[p];
                if (typeof value === 'number')
                    sizes[p] = value;
                else if (isStar(value))
                    sizes[p] = getStar(value) * star;
            }

            var starts = {};
            var ends = {};
            var acum = 0;

            for (var p in sizes) {
                starts[p] = acum;
                acum += sizes[p];
                ends[p] = acum;
            }

            return {

                size: function (name) {
                    return sizes[name];
                },

                start: function (name) {
                    return starts[name];
                },

                end: function (name) {
                    return ends[name];
                },

                middle: function(name){
                    return starts[name] + sizes[name] / 2;
                },

                debugX: function (chart) {

                    var keys = _.keys(sizes);

                    //paint x-axis rule
                    chart.append('svg:g').attr('class', 'x-rule-tick')
                        .enterData(keys, 'line', 'x-rule-tick')
                        .attr('x1', function (d) { return ends[d]; })
                        .attr('x2', function (d) { return ends[d]; })
                        .attr('y1', 0)
                        .attr('y2', 10000)
                        .style('stroke-width', 2)
                        .style('stroke', 'Pink');

                    //paint y-axis rule labels
                    chart.append('svg:g').attr('class', 'x-axis-rule-label')
                        .enterData(keys, 'text', 'x-axis-rule-label')
                        .attr('transform', function (d, i) { return º.translate(starts[d] + sizes[d] / 2 - 5, 10 + 100 * (i % 3)) + º.rotate(90); })
                        .attr('fill', 'DeepPink')
                        .text(function (d) { return d; });
                },

                debugY: function (chart) {

                    var keys = _.keys(sizes);

                    //paint y-axis rule
                    chart.append('svg:g').attr('class', 'y-rule-tick')
                        .enterData(keys, 'line', 'y-rule-tick')
                        .attr('x1', 0)
                        .attr('x2', 10000)
                        .attr('y1', function (d) { return ends[d]; })
                        .attr('y2', function (d) { return ends[d]; })
                        .style('stroke-width', 2)
                        .style('stroke', 'Violet');

                    //paint y-axis rule labels
                    chart.append('svg:g').attr('class', 'y-axis-rule-label')
                        .enterData(keys, 'text', 'y-axis-rule-label')
                        .attr('transform', function (d, i) { return º.translate(100 * (i % 3), starts[d] + sizes[d] / 2 + 4); })
                        .attr('fill', 'DarkViolet')
                        .text(function (d) { return d; });

                }
            };
        }

    };

    º = result;

    return result;

})();
