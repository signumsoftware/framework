var SF = SF || {};

SF.Chart = (function () {
    var getFor = function (prefix) {
        return $("#" + SF.compose(prefix, "sfChartBuilderContainer")).data("SF-chartBuilder");
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


                var $resultsContainer = $chartControl.find(".sf-search-results-container");
                if ($this.hasClass("sf-chart-img-equiv")) {
                    if ($resultsContainer.find("svg").length > 0) {
                        self.reDrawOnUpdateBuilder = true;
                    }
                }
                else {
                    $resultsContainer.html("");
                }

                self.updateChartBuilder();
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

            $($chartControl).on("change", ".sf-chart-redraw-onchange", function(){
                self.reDraw();
            });

            $(".sf-chart-draw").live("click", function (e) {
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
                            self.initOrders();
                            self.reDraw();
                        }
                    }
                });
            });

            
            window.changeTextArea = function (value, runtimeInfo) {
                if($("#ChartScript_sfRuntimeInfo").val() == runtimeInfo)
                {
                    var $textArea = self.element.find("textarea.sf-chart-currentScript");

                    $textArea.val(value);
                    self.reDraw();
                }
            };

            window.getExceptionNumber = function () {
                if (self.exceptionLine == null || self.exceptionLine == undefined)
                    return null;

                var temp = self.exceptionLine;
                self.exceptionLine = null;
                return temp;
            };

            $(".sf-chart-script-edit").live("click", function (e) {
                e.preventDefault();

                var $textArea = self.element.find("textarea.sf-chart-currentScript");

                var win = window.open($textArea.data("url"));
            });

            this.$chartControl.on("sf-new-subtokens-combo", function (event, idSelectedCombo, index) {
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
                    if (self.reDrawOnUpdateBuilder) {
                        self.reDraw();
                        self.reDrawOnUpdateBuilder = false;
                    }

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

        showError: function (e, __baseLineNumber__, chart) {
            var message = e.toString();

            var regex = /(DrawChart.*@.*:(.*))|(DrawChart .*:(.*):.*\)\))|(DrawChart .*:(.*):.*\))/;
            var match;
            if (e.stack != undefined && (match = regex.exec(e.stack)) != null) {
                var lineNumber = parseInt(match[2] || match[4] || match[6]) - __baseLineNumber__;
                if(isNaN(lineNumber))
                   lineNumber = 1;
                this.exceptionLine = lineNumber;
                message = "Line " + lineNumber + ": " + message;
            } else {
                this.exceptionLine = 1;
            }

            chart.select(".sf-chart-error").remove();
            chart.append('svg:rect').attr('class', 'sf-chart-error').attr("y", (chart.attr("height") / 2) - 10).attr("fill", "#FBEFFB").attr("stroke", "#FAC0DB").attr("width", chart.attr("width") - 1).attr("height", 20);
            chart.append('svg:text').attr('class', 'sf-chart-error').attr("y", chart.attr("height") / 2).attr("fill", "red").attr("dy", 5).attr("dx", 4).text(message);
        },

        reDraw: function () {
            var $chartContainer = this.$chartControl.find(".sf-chart-container");

            $chartContainer.html("");

            var data = $chartContainer.data("json");
            SF.Chart.Utils.fillAllTokenValueFuntions(data);
        
            var self = this;
            $(".sf-chart-redraw-onchange", this.$chartControl).each(function(i, element){
                var $element = $(element);
                var name = $element.attr("id");
                if (!SF.isEmpty(self.options.prefix)) {
                    name = name.substring(self.options.prefix.length + 1, name.length);
                }
                var nameParts = name.split('_');
                if(nameParts.length == 3 && nameParts[0] == "Columns"){
                    var column = data.columns["c" + nameParts[1]];
                    switch (nameParts[2]){
                        case "DisplayName": column.title = $element.val(); break;
                        case "Parameter1":  column.parameter1 = $element.val(); break;
                        case "Parameter2":  column.parameter2 = $element.val(); break;
                        case "Parameter3":  column.parameter3 = $element.val(); break;
                        default: break;
                    }
                }
            });

            var width = $chartContainer.width();
            var height = $chartContainer.height();

            var code = "(" + this.$chartControl.find('textarea.sf-chart-currentScript').val() + ")";

            var chart = d3.select('#' + this.$chartControl.attr("id") + " .sf-chart-container")
                .append('svg:svg').attr('width', width).attr('height', height);

                

            var func;
            var __baseLineNumber__;
            try {
                var getColor = SF.Chart.Utils.getColor;
                var getClickKeys = SF.Chart.Utils.getClickKeys;
                var translate = SF.Chart.Utils.translate;
                var scale = SF.Chart.Utils.scale;
                var rotate = SF.Chart.Utils.rotate;
                var skewX = SF.Chart.Utils.skewX;
                var skewY = SF.Chart.Utils.skewY;
                var matrix = SF.Chart.Utils.matrix;
                var scaleFor = SF.Chart.Utils.scaleFor;
                var rule = SF.Chart.Utils.rule;
                __baseLineNumber__ = new Error().lineNumber;
                func = eval(code);
            } catch (e) {
                this.showError(e, __baseLineNumber__, chart);
                return;
            }

            try {
                func(chart, data);
                this.bindMouseClick($chartContainer);
            } catch (e) {
                this.showError(e, __baseLineNumber__, chart);
            }

            if (this.exceptionLine == null)
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

        requestProcessedData: function () {
            return {
                webQueryName: this.options.webQueryName,
                filters: this.serializeFilters(),
                orders: this.serializeOrders(this.options.orders)
            };
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
    },

        bindMouseClick: function ($chartContainer) {

            $chartContainer.find('[data-click]').click(function () {

                var url = $chartContainer.attr('data-open-url');

                var $chartControl = $chartContainer.closest(".sf-chart-control");
                var findNavigator = SF.Chart.getFor($chartControl.attr("data-prefix"));


                var options = $chartControl.find(":input").not($chartControl.find(".sf-filters-list :input")).serialize();
                options += "&webQueryName=" + findNavigator.options.webQueryName;
                options += "&orders=" + findNavigator.serializeOrders(findNavigator.options.orders);
                options += "&filters=" + findNavigator.serializeFilters();
                options += $(this).data("click");

                window.open(url + (url.indexOf("?") >= 0 ? "&" : "?") + options);
});
}


        });
})(jQuery);

