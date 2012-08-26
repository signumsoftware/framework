var SF = SF || {};

SF.Chart = (function () {
    var getFor = function (prefix) {
        return $("#" + SF.compose(prefix, "sfChartBuilderContainer")).data("chartBuilder");
    };

    return {
        getFor: getFor
    };
})();

(function ($) {

    $.widget("SF.chartBuilder", $.SF.findNavigator, {

        options: {},

        _create: function() {
            var self = this;
            var $chartControl = self.element.closest(".sf-chart-control");
            self.$chartControl = $chartControl;
            $(".sf-chart-img").live("click", function () {
                var $this = $(this);
                $this.closest(".sf-chart-type").find(".ui-widget-header .sf-chart-type-value").val($this.attr("data-related"));
                self.updateChartBuilder();

               
                var $resultsContainer = $chartControl.find(".sf-search-results-container");
                if ($this.hasClass("sf-chart-img-equiv")) {
                    if ($resultsContainer.find("svg").length > 0) {
                        self.reDraw();
                    }
                }
                else {
                    $resultsContainer.html("");
                }
            });

            $(".sf-chart-group-trigger").live("change", function () {
                self.element.find(".sf-chart-group-results").val($(this).is(":checked"));
                self.updateChartBuilder();
            });

            $(".sf-chart-token-config-trigger").live("click", function () {
                $(this).closest(".sf-chart-token").next().toggle();
            });

            $(".sf-query-token select").live("change", function () {
                var $this = $(this);
                self.updateChartBuilder($this.closest("tr").attr("data-token"));
            });

            $(".sf-chart-draw").live("click", function(e){
                e.preventDefault();
                var $this = $(this);
                $.ajax({
                    url: $this.attr("data-url"),
                    data: self.requestData(),
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
                            self.reDraw();
                        }
                    }
                });
            });

            $(".sf-chart-script-edit").live("click", function (e) {
                e.preventDefault();
                var $textArea = self.element.find("textarea.sf-chart-currentScript");

                var win = window.open($textArea.data("url"));

                window.changeTextArea = function(value)
                {
                     $textArea.val(value);
                     self.reDraw();
                }; 

                window.getExceptionNumber = function(){
                   if(self.exceptionLine == null || self.exceptionLine == undefined)
                       return null;

                   var temp = self.exceptionLine;
                   self.exceptionLine = null;
                   return temp;
                }
            });

            this.$chartControl.on("sf-new-subtokens-combo", function(event, idSelectedCombo, index) {
                self.newSubTokensComboAdded($("#" + idSelectedCombo), index);
            });

            var originalNewSubtokensCombo = SF.FindNavigator.newSubTokensCombo;

            SF.FindNavigator.newSubTokensCombo = function (webQueryName, prefix, index, url) {
                var $selectedCombo = $("#" + SF.compose(prefix, "ddlTokens_" + index));
                if ($selectedCombo.closest(".sf-chart-builder").length == 0) {
                    if ($chartControl.find(".sf-chart-group-trigger:checked").length > 0) {
                        url = $chartControl.attr("data-subtokens-url");
                        originalNewSubtokensCombo.call(this, webQueryName, prefix, index, url);
                    }
                    else {
                        originalNewSubtokensCombo.call(this, webQueryName, prefix, index, url);
                    }
                }
                else {
                    SF.FindNavigator.clearChildSubtokenCombos($selectedCombo, prefix, index);
                    $("#" + SF.compose($chartControl.attr("data-prefix"), "sfOrders")).val('');
                    $chartControl.find('th').removeClass("sf-header-sort-up sf-header-sort-down");
                }
            };
        },

        requestProcessedData: function () {
            return {
                webQueryName: this.options.webQueryName,
                filters: this.serializeFilters(),
                orders: this.serializeOrders(this.options.orders)
            };
        },

        requestData: function () {
            return this.$chartControl.find(":input:not(#webQueryName)").serialize() +
                "&webQueryName=" + this.options.webQueryName +
                "&filters=" + this.serializeFilters() +
                "&orders=" + this.serializeOrders(this.options.orders);
        },

        updateChartBuilder: function (tokenChanged) {
            var $chartBuilder = this.$chartControl.find(".sf-chart-builder");
            var data = this.requestData();
            if (!SF.isEmpty(tokenChanged)) {
                data += "&lastTokenChanged=" + tokenChanged;
            }
            var self = this;
            $.ajax({
                url: $chartBuilder.attr("data-url"),
                data: data,
                success: function (result) {
                    $chartBuilder.replaceWith(result);
                    SF.triggerNewContent(self.$chartControl.find(".sf-chart-builder"));
                }
            });
        },    

        originalAddFilter: $.SF.findNavigator.prototype.addFilter,

        addFilter: function () {
            var $addFilter = $(this.pf("btnAddFilter"));
            if ($addFilter.closest(".sf-chart-builder").length == 0) {
                if (this.$chartControl.find(".sf-chart-group-trigger:checked").length > 0) {
                    url = this.$chartControl.attr("data-add-filter-url");
                    this.originalAddFilter.call(this, url);
                }
                else {
                    this.originalAddFilter.call(this);
                }
            }
        },

        showError: function(e, __baseLineNumber__ , chart){
            var message = e.toString();
            if(e.lineNumber != undefined)
            {
                var lineNumber = (e.lineNumber - __baseLineNumber__); 
                this.exceptionLine = lineNumber;
                message = "Line " + lineNumber + ": " + message; 
            }else{
                this.exceptionLine = 0;
            }

            chart.select(".sf-chart-error").remove();
            chart.append('svg:text').attr('class', 'sf-chart-error').attr("y", chart.attr("height") / 2).text(message);
        },

        reDraw: function () {
            var $chartContainer = this.$chartControl.find(".sf-chart-container");

            $chartContainer.html("");

            var data = $chartContainer.data("json");
            var width = $chartContainer.width();
            var height = $chartContainer.height();

            var code = "(" + this.$chartControl.find('textarea.sf-chart-currentScript').val() + ")";

            var chart = d3.select('#' + this.$chartControl.attr("id") + " .sf-chart-container")
                .append('svg:svg').attr('width', width).attr('height', height);

            var func;
            var __baseLineNumber__;
            try {
                var getLabel = SF.Chart.Utils.getLabel;
                var getKey = SF.Chart.Utils.getKey;
                var getColor = SF.Chart.Utils.getColor;
                __baseLineNumber__ = new Error().lineNumber; 
                func = eval(code);
            }catch(e){
               this.showError(e, __baseLineNumber__, chart);
               return; 
            }

            var hasError = false;
            try{
            func(chart, data);
            }catch(e){
               this.showError(e,__baseLineNumber__, chart);
            }

            if(this.exceptionLine == null)
                this.exceptionLine = -1;
        },

        exportData: function (validateUrl, exportUrl) {
            var $inputs = this.$chartControl.find(":input").not(":button");

            var data = this.requestProcessedData();
            $inputs.each(function () {
                data[this.id] = $(this).val();
            });

            SF.EntityIsValid({ prefix: this.options.prefix, controllerUrl: validateUrl, requestExtraJsonData: data }, function () {
                SF.submitOnly(exportUrl, data)
            });
        },

        fullScreen: function (evt) {
            evt.preventDefault();
            var url = this.$chartControl.find(".sf-chart-container").attr("data-fullscreen-url")
                    + "&" + this.requestData();
            if (evt.ctrlKey || evt.which == 2) {
                window.open(url);
            }
            else if (evt.which == 1) {
                window.location.href = url;
            }
        },

        initOrders: function () {
            var self = this;
            $(this.pf("tblResults") + " th").mousedown(function (e) {
                this.onselectstart = function () { return false };
                return false;
            })
            .click(function (e) {
                self.newSortOrder($(e.target), e.shiftKey);
                self.$chartControl.find(".sf-chart-draw").click();
                return false;
            });
        }
    });
})(jQuery);

SF.Chart.Utils = (function () {
  
    Array.prototype.enterData = function (data, tag, cssClass) {
        return this.selectAll(tag + "." + cssClass).data(data)
        .enter().append("svg:" + tag)
        .attr("class", cssClass);
    };

    return {

        getLabel: function (tokenValue) {
           return tokenValue !== null && tokenValue.toStr != undefined ? tokenValue.toStr : tokenValue;
        },

        getKey: function (tokenValue) {
           return tokenValue !== null && tokenValue.key != undefined ? tokenValue.key : tokenValue;
        },

        getColor: function (tokenValue, color) {
            return ((tokenValue !== null  && tokenValue.color != undefined) ? tokenValue.color : null) || color(JSON.stringify(tokenValue));
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
                    var $chartControl = $shape.closest(".sf-chart-control");
                    var findNavigator = SF.Chart.getFor($chartControl.attr("data-prefix"));

                    var data = $chartControl.find(":input").not($chartControl.find(".sf-filters-list :input")).serialize();
                    data += "&webQueryName=" + findNavigator.options.webQueryName;
                    data += "&orders=" + findNavigator.serializeOrders(findNavigator.options.orders);
                    data += "&filters=" + findNavigator.serializeFilters();
                    data += extractAttribute($shape, "d1");
                    data += extractAttribute($shape, "d2");
                    data += extractAttribute($shape, "v1");
                    data += extractAttribute($shape, "v2");
                    data += extractAttribute($shape, "entity");
                    return data;
                };

                var url = $this.closest('.sf-chart-container').attr('data-open-url');
                window.open(url + (url.indexOf("?") >= 0 ? "&" : "?") + serializeData($this));
            });
        }
    };

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

