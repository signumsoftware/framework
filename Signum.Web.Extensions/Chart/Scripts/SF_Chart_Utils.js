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

        rule : function(object, totalSize) {
            function isStar(val)
            {
                return typeof val === 'string' & val[val.length-1] == '*';
            }

            function getStar(val)
            {
                return parseFloat(val.substring(0, val.length - 1));
            }

            var fixed = 0;
            var proportional = 0;
            for(var p in object){
                var value = object[p];
                if(typeof value === 'number')
                    fixed += value;
                else if(isStar(value))
                    proportional += getStar(value);
                else 
                    throw new Error("values should be numbers or *");
            }

            var remaining = totalSize - fixed;
            var star = proportional <= 0? 0: remaining / proportional;

            var calculated = {};

            for(var p in object){
                 var value = object[p];
                if(typeof value === 'number')
                    calculated[p] = value;
                else if(isStar(value))
                    calculated[p] = getStar(value) * star;
            }

            var acumulated = {};
            var acum = 0;

            for(var p in calculated){
                acumulated[p] = acum;
                acum += calculated[p]; 
            }

            acumulated["end"] = acum;

            return {
                to: function(name){
                    return acumulated[name];
                },
                from: function(fromName){
                    return {
                        to : function(toName)
                        {
                            return acumulated[toName] - acumulated[name];
                        }
                    };
                },
                
                debugX : function(chart){
                  
                  var keys = _.keys(acumulated);  

                  //paint x-axis rule
                  chart.append('svg:g').attr('class', 'x-rule-tick')
                    .enterData(keys, 'line', 'x-rule-tick')
                      .attr('x1', function (d) { return acumulated[d]; })
                      .attr('x2', function (d) { return acumulated[d]; })
                      .attr('y1', 0)
                      .attr('y2', 100)
                      .style('stroke', 'DeepPink');
  
                  //paint y-axis rule labels
                  chart.append('svg:g').attr('class', 'x-axis-rule-label').attr('transform', 'rotate(90)')
                      .enterData(keys, 'text', 'x-axis-rule-label')
                        .attr('y', 50)
                        .attr('x', function (d) { return acumulated[d]; })
                        .attr('fill', 'DarkViolet')
                        .text(function (d) { return d; });
                },
            };
        }

    };

    º = result;

    return result;

})();
