SF.Chart.Utils = (function () {
  
    Array.prototype.enterData = function (data, tag, cssClass) {
        return this.selectAll(tag + "." + cssClass).data(data)
        .enter().append("svg:" + tag)
        .attr("class", cssClass);
    };

    var result = {

        getLabel: function (tokenValue) {
            return (tokenValue !== null && tokenValue.toStr !== undefined ? tokenValue.toStr : tokenValue) || "[ null ]";
        },

        getKey: function (tokenValue) {
            return (tokenValue !== null && tokenValue.key !== undefined ? tokenValue.key : tokenValue) || "null";
        },

        getColor: function (tokenValue, color) {
            return ((tokenValue !== null && tokenValue.color != /*or null*/undefined) ? tokenValue.color : null) || color(SF.Chart.Utils.getKey(tokenValue));
        },

        getClickKeys: function(row, columns)
        {
            var options = "";
            for(var k in columns)
            {
                var col = columns[k];
                if(col.isGroupKey == true)
                {
                    var cell = row[k];
                    var value = SF.Chart.Utils.getKey(cell);
                    options += "&" + k + "=" + (value === null ? "": value);
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

        toObject : function(array, keySelector, valueSelector) {
            var result = {};
            var ks = $.isFunction(keySelector) ? keySelector : function(obj) { return obj[keySelector]; };
            var vs = valueSelector == null ? null: $.isFunction(valueSelector) ? valueSelector : function(obj) { return obj[valueSelector]; };
            for (var i = 0; i < array.length; i++) 
            {
              var value = array[i];
              var key = ks(value, i);
              if(result[key] !== undefined)
                throw new Error("Duplicated key " + key);
              result[key] = vs == null? value: vs(value)
            }
            return result;
        },

        groupBy: function (array, keySelector, reducer) {
            var result = {};
            var ks = _.isFunction(keySelector) ? keySelector : function (obj) { return obj[keySelector]; };
            for (var i = 0; i < array.length; i++) 
            {
                var value = array[i];
                var key = ks(value, i);
                (result[key] || (result[key] = [])).push(value);
            }

            if(reducer != undefined)
            {
                for(var a in result)
                    result[a] = reducer(result[a]);
            }
          
            return result;
        },


        distinct : function(array, keySelector) {
            var set = {};
            var result = [];
            var ks = $.isFunction(keySelector) ? keySelector : function(obj) { return obj[keySelector]; };
            for (var i = 0; i < array.length; i++) 
            {
              var value = array[i];
              var key = ks(value, i);
              if(set[key] === undefined)
              {
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


//    getMaxValue1: function (series) {
//        var completeArray = [];
//        $.each(series, function (i, s) {
//            $.merge(completeArray, s.values);
//        });
//        var self = this;
//        return d3.max($.map(completeArray, function (e) { return self.getTokenKey(e); }));
//    },

//    createEmptyCountArray: function (length) {
//        var countArray = [];
//        for (var i = 0; i < length; i++) {
//            countArray.push(0);
//        }
//        return countArray;
//    },

//    createCountArray: function (series) {
//        if (series.length == 0) {
//            return [];
//        }

//        var dimensionCount = series[0].values.length;
//        var countArray = this.createEmptyCountArray(dimensionCount);

//        var self = this;
//        $.each(series, function (i, serie) {
//            for (var i = 0; i < dimensionCount; i++) {
//                var v = serie.values[i];
//                if (!SF.isEmpty(v)) {
//                    countArray[i] += self.getTokenKey(v);
//                }
//            }
//        });

//        return countArray;
//    },



//    getSizeScale: function (data, area) {
//        var sum = 0;
//        var self = this;
//        $.each(data.points, function (i, p) {
//            sum += self.getTokenKey(p.value2);
//        });

//        return d3.scale.linear()
//            .domain([0, sum])
//            .range([0, area]);
//    },

