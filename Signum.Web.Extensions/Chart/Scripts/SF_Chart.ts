/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/d3/d3.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/references.ts"/>
/// <reference path="SF_Chart_Utils.ts"/>

module SF.Chart
{
    export function getFor(prefix: string) {
        return $("#" + SF.compose(prefix, "sfChartBuilderContainer")).data("SF-chartBuilder");
    };

    export class ChartBuilder extends SF.SearchControl
    {
        exceptionLine: number;
        $chartControl : JQuery;
        reDrawOnUpdateBuilder : boolean;

        public _create() {
            var self = this;
            this.$chartControl = self.element.closest(".sf-chart-control");
            $(document).on("click", ".sf-chart-img", function () {
                var $this = $(this);
                $this.closest(".sf-chart-type").find(".ui-widget-header .sf-chart-type-value").val($this.attr("data-related"));


                var $resultsContainer = self.$chartControl.find(".sf-search-results-container");
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

            $(document).on("change", ".sf-chart-group-trigger", function () {
                self.element.find(".sf-chart-group-results").val($(this).is(":checked").toString());
                self.updateChartBuilder();
            });

            $(document).on("click", ".sf-chart-token-config-trigger", function () {
                $(this).closest(".sf-chart-token").next().toggle();
            });

            $(document).on("change", ".sf-query-token select", function () {
                var $this = $(this);
                self.updateChartBuilder($this.closest("tr").attr("data-token"));
            });

            $(this.$chartControl).on("change", ".sf-chart-redraw-onchange", function(){
                self.reDraw();
            });

            $(document).on("click", ".sf-chart-draw", function (e) {
                e.preventDefault();
                var $this = $(this);
                $.ajax({
                    url: $this.attr("data-url"),
                    data: self.requestData(),
                    success: function (result) {
                        if (typeof result === "object") {
                            if (typeof result.ModelState != "undefined") {
                                var modelState = result.ModelState;
                                new SF.Validator().showErrors(modelState, true);
                                SF.Notify.error(lang.signum.error, 2000);
                            }
                        }
                        else {
                            new SF.Validator().showErrors(null);
                            self.$chartControl.find(".sf-search-results-container").html(result);
                            SF.triggerNewContent(self.$chartControl.find(".sf-search-results-container"));
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

            $(document).on("click", ".sf-chart-script-edit", function (e) {
                e.preventDefault();

                var $textArea = self.element.find("textarea.sf-chart-currentScript");

                var win = window.open($textArea.data("url"));
            });

            $(document).on("mousedown", this.pf("sfFullScreen"), function (e) {
                e.preventDefault();
                self.fullScreen(e);
            });

            this.$chartControl.on("sf-new-subtokens-combo", function (event,  ...args) {
                self.newSubTokensComboAdded($("#" + args[0] /*idSelectedCombo*/));
            });

            var originalNewSubtokensCombo = SF.FindNavigator.newSubTokensCombo;

            SF.FindNavigator.newSubTokensCombo = function (webQueryName, prefix, index, url) {
                var $selectedCombo = $("#" + SF.compose(prefix, "ddlTokens_" + index));
                if ($selectedCombo.closest(".sf-chart-builder").length == 0) {
                    if (self.$chartControl.find(".sf-chart-group-trigger:checked").length > 0) {
                        url = self.$chartControl.attr("data-subtokens-url");
                        originalNewSubtokensCombo.call(this, webQueryName, prefix, index, url);
                    }
                    else {
                        originalNewSubtokensCombo.call(this, webQueryName, prefix, index, url);
                    }
                }
                else {
                    SF.FindNavigator.clearChildSubtokenCombos($selectedCombo, prefix, index);
                    $("#" + SF.compose(self.$chartControl.attr("data-prefix"), "sfOrders")).val('');
                    self.$chartControl.find('th').removeClass("sf-header-sort-up sf-header-sort-down");
                }
            };
        }

        requestData() : string {
            return this.$chartControl.find(":input:not(#webQueryName)").serialize() +
                "&webQueryName=" + this.options.webQueryName +
                "&filters=" + this.serializeFilters() +
                "&orders=" + this.serializeOrders();
        }

        updateChartBuilder(tokenChanged? : string) {
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
        }    


        addFilter() {
            var $addFilter = $(this.pf("btnAddFilter"));
            if ($addFilter.closest(".sf-chart-builder").length == 0) {
                if (this.$chartControl.find(".sf-chart-group-trigger:checked").length > 0) {
                    var url = this.$chartControl.attr("data-add-filter-url");
                    super.addFilter(url);
                }
                else {
                    super.addFilter();
                }
            }
        }

        showError(e, __baseLineNumber__, chart) {
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
        }

        reDraw() {
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
            var __baseLineNumber__ : number;
            try {
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
        }

        exportData(validateUrl, exportUrl) {
            var $inputs = this.$chartControl.find(":input").not(":button");

            var data = this.requestProcessedData();
            $inputs.each(function () {
                data[this.id] = $(this).val();
            });

            SF.EntityIsValid({ prefix: this.options.prefix, controllerUrl: validateUrl, requestExtraJsonData: data }, function () {
                SF.submitOnly(exportUrl, data)
            });
        }

        requestProcessedData() {
            return {
                webQueryName: this.options.webQueryName,
                filters: this.serializeFilters(),
                orders: this.serializeOrders()
            };
        }

        fullScreen(evt) {
            evt.preventDefault();

            var url = this.$chartControl.find(".sf-chart-container").attr("data-fullscreen-url") ||
                this.$chartControl.attr("data-fullscreen-url");

            url += (url.indexOf("?") < 0 ? "?" : "&") + this.requestData();

            if (evt.ctrlKey || evt.which == 2) {
                window.open(url);
            }
            else if (evt.which == 1) {
                window.location.href = url;
            }
        }

        initOrders() {
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

        bindMouseClick($chartContainer: JQuery) {

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

    }
}


interface Window {
    changeTextArea(value: string, runtimeInfo: string);

    getExceptionNumber(): number;
}

interface Error {
    lineNumber: number;
}
