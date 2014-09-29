/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
/// <reference path="ChartUtils.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Finder", "Framework/Signum.Web/Signum/Scripts/Validator", "Framework/Signum.Web/Signum/Scripts/Operations", "ChartUtils", "colorbrewer", "d3"], function(require, exports, Finder, Validator, Operations, ChartUtils, colorbrewer, d3) {
    var rubish = colorbrewer.hasOwnProperty;

    function openChart(prefix, url) {
        Finder.getFor(prefix).then(function (sc) {
            return SF.submit(url, sc.requestDataForSearch(1 /* FindOptions */));
        });
    }
    exports.openChart = openChart;

    function deleteUserChart(options, url) {
        options.avoidReturnRedirect = true;
        if (!Operations.confirmIfNecessary(options))
            return;

        Operations.deleteAjax(options).then(function (a) {
            if (!options.prefix)
                location.href = url;
        });
    }
    exports.deleteUserChart = deleteUserChart;

    function createUserChart(prefix, url) {
        exports.getFor(prefix).then(function (cb) {
            return SF.submit(url, cb.requestProcessedData());
        });
    }
    exports.createUserChart = createUserChart;

    function exportData(prefix, validateUrl, exportUrl) {
        exports.getFor(prefix).then(function (cb) {
            return cb.exportData(validateUrl, exportUrl);
        });
    }
    exports.exportData = exportData;

    var ChartBuilder = (function () {
        function ChartBuilder(options) {
            var _this = this;
            this.isDrawn = function (isEquiv) {
                return false;
            };
            this.fastRedraw = function () {
            };
            this.options = options;

            this.container = options.prefix.child("sfChartBuilderContainer").get();
            this.container.data("SF-control", this);

            this.container.on("click", ".sf-chart-img", function (e) {
                var img = $(e.currentTarget);
                img.closest(".sf-chart-type").find(".sf-chart-type-value").val(img.attr("data-related"));

                var isFastRedraw = _this.isDrawn(img.hasClass("sf-chart-img-equiv"));

                _this.updateChartBuilder("-1").then(function () {
                    if (isFastRedraw)
                        _this.fastRedraw();
                });
            });

            this.container.on("change", ".sf-chart-group-trigger", function (e) {
                _this.container.find(".sf-chart-group-results").val($(e.currentTarget).is(":checked").toString());
                _this.updateChartBuilder();
            });

            this.container.on("click", ".sf-chart-token-config-trigger", function (e) {
                $(e.currentTarget).closest(".sf-chart-token").next().toggle();
            });

            this.container.on("change", ".sf-query-token select", function (e) {
                var token = $(e.currentTarget);
                var id = token.attr("id");
                Finder.QueryTokenBuilder.clearChildSubtokenCombos(token, id.before("_ddlTokens_"), parseInt(id.after("_ddlTokens_")));
                _this.updateChartBuilder(token.closest("tr").attr("data-token"));
            });

            this.container.SFControlFullfill(this);
        }
        ChartBuilder.prototype.updateChartBuilder = function (tokenChanged) {
            var _this = this;
            var data = this.requestData();
            if (!SF.isEmpty(tokenChanged)) {
                data["lastTokenChanged"] = tokenChanged;
            }

            return SF.ajaxPost({
                url: this.options.updateChartBuilderUrl,
                data: data
            }).then(function (result) {
                _this.container.html(result);
            });
        };

        ChartBuilder.prototype.requestData = function () {
            var result = this.container.find(":input").serializeObject();

            result["webQueryName"] = this.options.webQueryName;

            return result;
        };
        return ChartBuilder;
    })();
    exports.ChartBuilder = ChartBuilder;

    function getFor(prefix) {
        return prefix.child("sfChartControl").get().SFControl();
    }
    exports.getFor = getFor;
    ;

    (function (ChartRequestMode) {
        ChartRequestMode[ChartRequestMode["Complete"] = 0] = "Complete";
        ChartRequestMode[ChartRequestMode["Chart"] = 1] = "Chart";
        ChartRequestMode[ChartRequestMode["Data"] = 2] = "Data";
    })(exports.ChartRequestMode || (exports.ChartRequestMode = {}));
    var ChartRequestMode = exports.ChartRequestMode;

    var ChartRequest = (function () {
        function ChartRequest(options) {
            var _this = this;
            this.options = options;

            this.chartControl = options.prefix.child("sfChartControl").get();
            this.chartControl.data("SF-control", this);

            this.divResults = options.prefix.child("divResults").get();

            this.filterBuilder = new Finder.FilterBuilder(this.options.prefix.child("tblFilterBuilder").get(), this.options.prefix, this.options.webQueryName, this.options.addFilterUrl);

            this.chartBuilder = new ChartBuilder(this.options);

            if (this.options.mode == 0 /* Complete */) {
                this.chartBuilder.isDrawn = function (isEqiv) {
                    if (isEqiv && _this.divResults.find("svg").length > 0)
                        return true;

                    _this.divResults.html("");
                    return false;
                };

                this.chartBuilder.fastRedraw = function () {
                    return _this.reDraw();
                };

                $(this.chartControl).on("change", ".sf-chart-redraw-onchange", function () {
                    _this.reDraw();
                });

                this.options.prefix.child("qbDraw").get().click(function (e) {
                    e.preventDefault();
                    _this.request();
                });

                window.changeTextArea = function (value, runtimeInfo) {
                    if ($("#ChartScript_sfRuntimeInfo").val() == runtimeInfo) {
                        var $textArea = _this.chartControl.find("textarea.sf-chart-currentScript");

                        $textArea.val(value);
                        _this.reDraw();
                    }
                };

                window.getExceptionNumber = function () {
                    if (_this.exceptionLine == null || _this.exceptionLine == undefined)
                        return null;

                    var temp = _this.exceptionLine;
                    _this.exceptionLine = null;
                    return temp;
                };

                this.options.prefix.child("qbEdit").get().on("click", function (e) {
                    e.preventDefault();
                    var $textArea = _this.chartControl.find("textarea.sf-chart-currentScript");
                    var win = window.open($textArea.data("url"));
                });
            } else {
                this.request();
            }

            this.options.prefix.child("sfFullScreen").get().on("mouseup", function (e) {
                e.preventDefault();
                _this.fullScreen(e);
            });

            this.chartControl.SFControlFullfill(this);
        }
        ChartRequest.prototype.serializeOrders = function () {
            return Finder.serializeOrders(this.options.orders);
        };

        ChartRequest.prototype.request = function () {
            var _this = this;
            SF.ajaxPost({
                url: this.options.drawUrl,
                data: $.extend(this.requestData(), { "mode": this.options.mode })
            }).then(function (result) {
                if (typeof result === "object") {
                    if (typeof result.ModelState != "undefined") {
                        var modelState = result.ModelState;
                        Validator.showErrors({}, modelState);
                        SF.Notify.error(lang.signum.error, 2000);
                    }
                } else {
                    Validator.showErrors({}, null);
                    _this.chartControl.find(".sf-search-results-container").html(result);

                    if (_this.options.mode == 0 /* Complete */ || _this.options.mode == 2 /* Data */) {
                        _this.initOrders();
                    }

                    if (_this.options.mode == 0 /* Complete */ || _this.options.mode == 1 /* Chart */) {
                        _this.reDraw();
                    }
                }
            });
        };

        ChartRequest.prototype.requestData = function () {
            var result = this.chartBuilder.requestData();

            result["filters"] = this.filterBuilder.serializeFilters();
            result["orders"] = this.serializeOrders();

            return result;
        };

        ChartRequest.prototype.showError = function (e, __baseLineNumber__, chart) {
            var message = e.toString();

            var regex = /(DrawChart.*@.*:(.*))|(DrawChart .*:(.*):.*\)\))|(DrawChart .*:(.*):.*\))/;
            var match;
            if (e.stack != undefined && (match = regex.exec(e.stack)) != null) {
                var lineNumber = parseInt(match[2] || match[4] || match[6]) - __baseLineNumber__;
                if (isNaN(lineNumber))
                    lineNumber = 1;
                this.exceptionLine = lineNumber;
                message = "Line " + lineNumber + ": " + message;
            } else {
                this.exceptionLine = 1;
            }

            chart.select(".sf-chart-error").remove();
            chart.append('svg:rect').attr('class', 'sf-chart-error').attr("y", (chart.attr("height") / 2) - 10).attr("fill", "#FBEFFB").attr("stroke", "#FAC0DB").attr("width", chart.attr("width") - 1).attr("height", 20);
            chart.append('svg:text').attr('class', 'sf-chart-error').attr("y", chart.attr("height") / 2).attr("fill", "red").attr("dy", 5).attr("dx", 4).text(message);
        };

        ChartRequest.prototype.reDraw = function () {
            var _this = this;
            var $chartContainer = this.chartControl.find(".sf-chart-container");

            $chartContainer.html("");

            var data = JSON.parse($chartContainer.attr("data-json"));
            ChartUtils.fillAllTokenValueFuntions(data);

            $(".sf-chart-redraw-onchange[id]", this.chartControl).each(function (i, element) {
                var $element = $(element);
                var name = $element.attr("id");
                if (!SF.isEmpty(_this.options.prefix)) {
                    name = name.substring(_this.options.prefix.length + 1, name.length);
                }
                var nameParts = name.split('_');
                if (nameParts.length == 3 && nameParts[0] == "Columns") {
                    var column = data.columns["c" + nameParts[1]];

                    if (!column)
                        data.columns["c" + nameParts[1]] = column = {};

                    switch (nameParts[2]) {
                        case "DisplayName":
                            column.title = $element.val();
                            break;
                        case "Parameter1":
                            column.parameter1 = $element.val();
                            break;
                        case "Parameter2":
                            column.parameter2 = $element.val();
                            break;
                        case "Parameter3":
                            column.parameter3 = $element.val();
                            break;
                        default:
                            break;
                    }
                }
            });

            var width = $chartContainer.width();
            var height = $chartContainer.height();

            var code = "(" + this.chartControl.find('textarea.sf-chart-currentScript').val() + ")";

            var chart = d3.select('#' + this.chartControl.attr("id") + " .sf-chart-container").append('svg:svg').attr('width', width).attr('height', height);

            var func;
            var __baseLineNumber__;
            try  {
                var getClickKeys = ChartUtils.getClickKeys;
                var translate = ChartUtils.translate;
                var scale = ChartUtils.scale;
                var rotate = ChartUtils.rotate;
                var skewX = ChartUtils.skewX;
                var skewY = ChartUtils.skewY;
                var matrix = ChartUtils.matrix;
                var scaleFor = ChartUtils.scaleFor;
                var rule = ChartUtils.rule;
                __baseLineNumber__ = new Error().lineNumber;
                func = eval(code);
            } catch (e) {
                this.showError(e, __baseLineNumber__, chart);
                return;
            }

            try  {
                func(chart, data);
                this.bindMouseClick($chartContainer);
            } catch (e) {
                this.showError(e, __baseLineNumber__, chart);
            }

            if (this.exceptionLine == null)
                this.exceptionLine = -1;
        };

        ChartRequest.prototype.exportData = function (validateUrl, exportUrl) {
            var data = this.requestData();

            if (Validator.entityIsValid({ prefix: this.options.prefix, controllerUrl: validateUrl, requestExtraJsonData: data }))
                SF.submitOnly(exportUrl, data);
        };

        ChartRequest.prototype.requestProcessedData = function () {
            return {
                webQueryName: this.options.webQueryName,
                filters: this.filterBuilder.serializeFilters(),
                orders: this.serializeOrders()
            };
        };

        ChartRequest.prototype.fullScreen = function (evt) {
            evt.preventDefault();

            var url = this.options.fullScreenUrl;

            url += (url.indexOf("?") < 0 ? "?" : "&") + $.param(this.requestData());

            if (evt.ctrlKey || evt.which == 2) {
                window.open(url);
            } else if (evt.which == 1) {
                window.location.href = url;
            }
        };

        ChartRequest.prototype.initOrders = function () {
            var _this = this;
            this.options.prefix.child("tblResults").get().on("click", "th", function (e) {
                Finder.SearchControl.newSortOrder(_this.options.orders, $(e.currentTarget), e.shiftKey);
                _this.chartControl.find(".sf-chart-draw").click();
                return false;
            });
        };

        ChartRequest.prototype.bindMouseClick = function ($chartContainer) {
            var _this = this;
            $chartContainer.find('[data-click]').click(function (e) {
                var url = _this.options.openUrl;

                var options = _this.chartControl.find(":input").not(_this.chartControl.find(".sf-filters-list :input")).serializeObject();
                options["webQueryName"] = _this.options.webQueryName;
                options["orders"] = _this.serializeOrders();
                options["filters"] = _this.filterBuilder.serializeFilters();

                var params = $.param(options) + $(e.currentTarget).data("click");
                window.open(url + (url.indexOf("?") >= 0 ? "&" : "?") + params);
            });
        };
        return ChartRequest;
    })();
    exports.ChartRequest = ChartRequest;
});
//# sourceMappingURL=Chart.js.map
