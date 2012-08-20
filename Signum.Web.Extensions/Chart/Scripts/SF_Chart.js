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

            $(".sf-chart-img").live("click", function () {
                var $this = $(this);
                $this.closest(".sf-chart-type").find(".ui-widget-header .sf-chart-type-value").val($this.attr("data-related"));
                self.updateChartBuilder();

                var $chartControl = self.element.closest(".sf-chart-control");
                var $resultsContainer = $chartControl.find(".sf-search-results-container");
                if ($this.hasClass("sf-chart-img-equiv")) {
                    if ($resultsContainer.find("svg").length > 0) {
                        self.reDraw($chartControl.find(".sf-chart-container"), true);
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
                            var $chartControl = self.element.closest(".sf-chart-control");
                            $chartControl.find(".sf-search-results-container").html(result);
                            SF.triggerNewContent($chartControl.find(".sf-search-results-container"));
                        }
                    }
                });
            });

            $(".sf-save-script").live("click", function (e) {
                e.preventDefault();
                var $this = $(this);
                var $chartControl = this.element.closest(".sf-chart-control");

                $.ajax({
                    url: $this.attr("data-url"),
                    data: {
                       code : $chartControl.find('.sf-chart-code-container textarea').val(),
                       script : $chartControl.find(".ui-widget-header .sf-chart-type-value").val()
                    },
                    success: function (result) {
                       alert("Saved!");
                    }
                });
            });

            

            this.element.closest(".sf-chart-control").on("sf-new-subtokens-combo", function(event, idSelectedCombo, index) {
                self.newSubTokensComboAdded($("#" + idSelectedCombo), index);
            });

            var originalNewSubtokensCombo = SF.FindNavigator.newSubTokensCombo;

            SF.FindNavigator.newSubTokensCombo = function (webQueryName, prefix, index, url) {
                var $selectedCombo = $("#" + SF.compose(prefix, "ddlTokens_" + index));
                var $chartControl = self.element.closest(".sf-chart-control");
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
            return this.element.closest(".sf-chart-control").find(":input:not(#webQueryName)").serialize() +
                "&webQueryName=" + this.options.webQueryName +
                "&filters=" + this.serializeFilters() +
                "&orders=" + this.serializeOrders(this.options.orders);
        },

        updateChartBuilder: function (tokenChanged) {
            var $chartControl = this.element.closest(".sf-chart-control");
            var $chartBuilder = $chartControl.find(".sf-chart-builder");
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
                    SF.triggerNewContent($chartControl.find(".sf-chart-builder"));
                }
            });
        },    

        originalAddFilter: $.SF.findNavigator.prototype.addFilter,

        addFilter: function () {
            var $addFilter = $(this.pf("btnAddFilter"));
            if ($addFilter.closest(".sf-chart-builder").length == 0) {
                var $chartControl = $addFilter.closest(".sf-chart-control");
                if ($chartControl.find(".sf-chart-group-trigger:checked").length > 0) {
                    url = $chartControl.attr("data-add-filter-url");
                    this.originalAddFilter.call(this, url);
                }
                else {
                    this.originalAddFilter.call(this);
                }
            }
        },

        reDraw: function ($chartContainer, force) {
            $chartContainer.html("");

            var data = $chartContainer.data("data");
            var width = $chartContainer.width();
            var height = $chartContainer.height();

            var $chartControl = this.element.closest(".sf-chart-control");
            
            var code = $chartControl.find('.sf-chart-code-container textarea').val();

            var chart = d3.select('#' + $chartControl.attr("id") + " .sf-chart-container")
                .append('svg:svg').attr('width', width).attr('height', height);

            try{
            eval(code);
            }catch(e){
               chart.append('svg:text').attr('class', 'sf-chart-error').text(e);
            }
        },

        exportData: function (validateUrl, exportUrl) {
            var $inputs = this.element.closest(".sf-chart-control").find(":input").not(":button");

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
            var url = this.element.closest(".sf-chart-control").find(".sf-chart-container").attr("data-fullscreen-url")
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
                self.element.closest(".sf-chart-control").find(".sf-chart-draw").click();
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

