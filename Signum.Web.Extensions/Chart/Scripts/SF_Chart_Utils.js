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
        }
    };

    º = result;

    return result;

})();
