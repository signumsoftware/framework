/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
/// <reference path="ChartUtils.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Finder", "Framework/Signum.Web/Signum/Scripts/Validator", "Framework/Signum.Web/Signum/Scripts/Operations", "ChartUtils", "colorbrewer", "d3"], function(require, exports, Entities, Finder, Validator, Operations, ChartUtils, colorbrewer, d3) {
    ChartUtils;
    colorbrewer;

    function openChart(prefix, url) {
        Finder.getFor(prefix).then(function (sc) {
            return SF.submit(url, sc.requestDataForSearch(1 /* FindOptions */));
        });
    }
    exports.openChart = openChart;

    function attachShowCurrentEntity(el) {
        var showOnEntity = function () {
            el.element.nextAll("p.messageEntity").toggle(!!Entities.RuntimeInfo.getFromPrefix(el.options.prefix));
        };

        showOnEntity();

        el.entityChanged = showOnEntity;
    }
    exports.attachShowCurrentEntity = attachShowCurrentEntity;

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

    function getFor(prefix) {
        return prefix.child("sfChartBuilderContainer").get().SFControl();
    }
    exports.getFor = getFor;
    ;

    var ChartBuilder = (function (_super) {
        __extends(ChartBuilder, _super);
        function ChartBuilder() {
            _super.apply(this, arguments);
        }
        ChartBuilder.prototype._create = function () {
            var _this = this;
            this.$chartControl = this.element.closest(".sf-chart-control");

            this.filterBuilder = new Finder.FilterBuilder(this.prefix.child("tblFilterBuilder").get(), this.options.prefix, this.options.webQueryName, this.$chartControl.attr("data-add-filter-url"));

            $(document).on("click", ".sf-chart-img", function (e) {
                var img = $(e.currentTarget);
                img.closest(".sf-chart-type").find(".sf-chart-type-value").val(img.attr("data-related"));

                var $resultsContainer = _this.$chartControl.find(".sf-search-results-container");
                if (img.hasClass("sf-chart-img-equiv")) {
                    if ($resultsContainer.find("svg").length > 0) {
                        _this.reDrawOnUpdateBuilder = true;
                    }
                } else {
                    $resultsContainer.html("");
                }

                _this.updateChartBuilder();
            });

            $(document).on("change", ".sf-chart-group-trigger", function (e) {
                _this.element.find(".sf-chart-group-results").val($(e.currentTarget).is(":checked").toString());
                _this.updateChartBuilder();
            });

            $(document).on("click", ".sf-chart-token-config-trigger", function (e) {
                $(e.currentTarget).closest(".sf-chart-token").next().toggle();
            });

            $(document).on("change", ".sf-query-token select", function (e) {
                var token = $(e.currentTarget);
                var id = token.attr("id");
                Finder.QueryTokenBuilder.clearChildSubtokenCombos(token, id.before("_ddlTokens_"), parseInt(id.after("_ddlTokens_")));
                _this.updateChartBuilder(token.closest("tr").attr("data-token"));
            });

            $(this.$chartControl).on("change", ".sf-chart-redraw-onchange", function () {
                _this.reDraw();
            });

            $(document).on("click", ".sf-chart-draw", function (e) {
                e.preventDefault();
                var drawBtn = $(e.currentTarget);
                SF.ajaxPost({
                    url: drawBtn.attr("data-url"),
                    data: _this.requestData()
                }).then(function (result) {
                    if (typeof result === "object") {
                        if (typeof result.ModelState != "undefined") {
                            var modelState = result.ModelState;
                            Validator.showErrors({}, modelState);
                            SF.Notify.error(lang.signum.error, 2000);
                        }
                    } else {
                        Validator.showErrors({}, null);
                        _this.$chartControl.find(".sf-search-results-container").html(result);
                        _this.initOrders();
                        _this.reDraw();
                    }
                });
            });

            window.changeTextArea = function (value, runtimeInfo) {
                if ($("#ChartScript_sfRuntimeInfo").val() == runtimeInfo) {
                    var $textArea = _this.element.find("textarea.sf-chart-currentScript");

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

            $(document).on("click", ".sf-chart-script-edit", function (e) {
                e.preventDefault();

                var $textArea = _this.element.find("textarea.sf-chart-currentScript");

                var win = window.open($textArea.data("url"));
            });

            $(document).on("mousedown", this.prefix.child("sfFullScreen"), function (e) {
                e.preventDefault();
                _this.fullScreen(e);
            });
        };

        ChartBuilder.prototype.requestData = function () {
            var result = this.$chartControl.find(":input:not(#webQueryName)").serializeObject();

            result["webQueryName"] = this.options.webQueryName;
            result["filters"] = this.filterBuilder.serializeFilters();
            result["orders"] = this.serializeOrders();

            return result;
        };

        ChartBuilder.prototype.updateChartBuilder = function (tokenChanged) {
            var _this = this;
            var $chartBuilder = this.$chartControl.find(".sf-chart-builder");
            var data = this.requestData();
            if (!SF.isEmpty(tokenChanged)) {
                data["lastTokenChanged"] = tokenChanged;
            }
            $.ajax({
                url: $chartBuilder.attr("data-url"),
                data: data
            }).then(function (result) {
                $chartBuilder.replaceWith(result);
                if (_this.reDrawOnUpdateBuilder) {
                    _this.reDraw();
                    _this.reDrawOnUpdateBuilder = false;
                }
            });
        };

        ChartBuilder.prototype.showError = function (e, __baseLineNumber__, chart) {
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

        ChartBuilder.prototype.reDraw = function () {
            var _this = this;
            var $chartContainer = this.$chartControl.find(".sf-chart-container");

            $chartContainer.html("");

            var data = $chartContainer.data("json");
            ChartUtils.fillAllTokenValueFuntions(data);

            $(".sf-chart-redraw-onchange", this.$chartControl).each(function (i, element) {
                var $element = $(element);
                var name = $element.attr("id");
                if (!SF.isEmpty(_this.options.prefix)) {
                    name = name.substring(_this.options.prefix.length + 1, name.length);
                }
                var nameParts = name.split('_');
                if (nameParts.length == 3 && nameParts[0] == "Columns") {
                    var column = data.columns["c" + nameParts[1]];
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

            var code = "(" + this.$chartControl.find('textarea.sf-chart-currentScript').val() + ")";

            var chart = d3.select('#' + this.$chartControl.attr("id") + " .sf-chart-container").append('svg:svg').attr('width', width).attr('height', height);

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

        ChartBuilder.prototype.exportData = function (validateUrl, exportUrl) {
            var data = this.requestData();

            if (Validator.entityIsValid({ prefix: this.options.prefix, controllerUrl: validateUrl, requestExtraJsonData: data }))
                SF.submitOnly(exportUrl, data);
        };

        ChartBuilder.prototype.requestProcessedData = function () {
            return {
                webQueryName: this.options.webQueryName,
                filters: this.filterBuilder.serializeFilters(),
                orders: this.serializeOrders()
            };
        };

        ChartBuilder.prototype.fullScreen = function (evt) {
            evt.preventDefault();

            var url = this.$chartControl.find(".sf-chart-container").attr("data-fullscreen-url") || this.$chartControl.attr("data-fullscreen-url");

            url += (url.indexOf("?") < 0 ? "?" : "&") + $.param(this.requestData());

            if (evt.ctrlKey || evt.which == 2) {
                window.open(url);
            } else if (evt.which == 1) {
                window.location.href = url;
            }
        };

        ChartBuilder.prototype.initOrders = function () {
            var _this = this;
            this.prefix.child("tblResults").get().on("click", "th", function (e) {
                _this.newSortOrder($(e.currentTarget), e.shiftKey);
                _this.$chartControl.find(".sf-chart-draw").click();
                return false;
            });
        };

        ChartBuilder.prototype.bindMouseClick = function ($chartContainer) {
            $chartContainer.find('[data-click]').click(function (e) {
                var url = $chartContainer.attr('data-open-url');

                var win = window.open("about:blank");

                var $chartControl = $chartContainer.closest(".sf-chart-control");
                exports.getFor($chartControl.attr("data-prefix")).then(function (cb) {
                    var options = $chartControl.find(":input").not($chartControl.find(".sf-filters-list :input")).serialize();
                    options += "&webQueryName=" + cb.options.webQueryName;
                    options += "&orders=" + cb.serializeOrders();
                    options += "&filters=" + cb.filterBuilder.serializeFilters();
                    options += $(e.currentTarget).data("click");

                    win.location.href = (url + (url.indexOf("?") >= 0 ? "&" : "?") + options);
                });
            });
        };
        return ChartBuilder;
    })(Finder.SearchControl);
    exports.ChartBuilder = ChartBuilder;
});
//# sourceMappingURL=Chart.js.map
