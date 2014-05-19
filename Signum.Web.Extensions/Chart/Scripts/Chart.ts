/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
/// <reference path="ChartUtils.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

import ChartUtils = require("ChartUtils"); ChartUtils;
import colorbrewer = require("colorbrewer"); colorbrewer;
import d3 = require("d3")



export function openChart(prefix: string, url: string) {
    Finder.getFor(prefix).then(sc=>
        SF.submit(url, sc.requestDataForSearch(Finder.RequestType.FindOptions)));
}

export function attachShowCurrentEntity(el: Lines.EntityLine) {
    var showOnEntity = function () {
        el.element.nextAll("p.messageEntity").toggle(!!Entities.RuntimeInfo.getFromPrefix(el.options.prefix));
    };

    showOnEntity();

    el.entityChanged = showOnEntity;
}

export function deleteUserChart(options: Operations.EntityOperationOptions, url: string) {
    options.avoidReturnRedirect = true;
    if (!Operations.confirmIfNecessary(options))
        return;

    Operations.deleteAjax(options).then(a=> {
        if (!options.prefix)
            location.href = url; 
    }); 
}

export function createUserChart(prefix: string, url: string) {
    getFor(prefix).then(cb=>
        SF.submit(url, cb.requestProcessedData()));
}

export function exportData(prefix: string, validateUrl: string, exportUrl: string) {
    getFor(prefix).then(cb=> cb.exportData(validateUrl, exportUrl)); 
}



export function getFor(prefix: string): Promise<ChartBuilder> {
    return prefix.child("sfChartBuilderContainer").get().SFControl<ChartBuilder>();
};

export class ChartBuilder extends Finder.SearchControl {
    exceptionLine: number;
    $chartControl: JQuery;
    reDrawOnUpdateBuilder: boolean;

    public _create() {

        this.$chartControl = this.element.closest(".sf-chart-control");

        this.filterBuilder = new Finder.FilterBuilder(
            this.prefix.child("tblFilterBuilder").get(),
            this.options.prefix,
            this.options.webQueryName,
            this.$chartControl.attr("data-add-filter-url"));

        $(document).on("click", ".sf-chart-img", e => {
            var img = $(e.currentTarget);
            img.closest(".sf-chart-type").find(".sf-chart-type-value").val(img.attr("data-related"));


            var $resultsContainer = this.$chartControl.find(".sf-search-results-container");
            if (img.hasClass("sf-chart-img-equiv")) {
                if ($resultsContainer.find("svg").length > 0) {
                    this.reDrawOnUpdateBuilder = true;
                }
            }
            else {
                $resultsContainer.html("");
            }

            this.updateChartBuilder();
        });

        $(document).on("change", ".sf-chart-group-trigger", e=> {
            this.element.find(".sf-chart-group-results").val($(e.currentTarget).is(":checked").toString());
            this.updateChartBuilder();
        });

        $(document).on("click", ".sf-chart-token-config-trigger", e => {
            $(e.currentTarget).closest(".sf-chart-token").next().toggle();
        });

        $(document).on("change", ".sf-query-token select", e => {
            var token = $(e.currentTarget);
            var id = token.attr("id"); 
            Finder.QueryTokenBuilder.clearChildSubtokenCombos(token, id.before("_ddlTokens_"), parseInt(id.after("_ddlTokens_")));
            this.updateChartBuilder(token.closest("tr").attr("data-token"));
        });

        $(this.$chartControl).on("change", ".sf-chart-redraw-onchange", () => {
            this.reDraw();
        });

        $(document).on("click", ".sf-chart-draw", e => {
            e.preventDefault();
            var drawBtn = $(e.currentTarget);
            SF.ajaxPost({
                url: drawBtn.attr("data-url"),
                data: this.requestData(),
            }).then((result) => {
                if (typeof result === "object") {
                    if (typeof result.ModelState != "undefined") {
                        var modelState = result.ModelState;
                        Validator.showErrors({}, modelState);
                        SF.Notify.error(lang.signum.error, 2000);
                    }
                }
                else {
                    Validator.showErrors({}, null);
                    this.$chartControl.find(".sf-search-results-container").html(result);
                    this.initOrders();
                    this.reDraw();
                }
            });
        });


        window.changeTextArea =  (value, runtimeInfo) => {
            if ($("#ChartScript_sfRuntimeInfo").val() == runtimeInfo) {
                var $textArea = this.element.find("textarea.sf-chart-currentScript");

                $textArea.val(value);
                this.reDraw();
            }
        };

        window.getExceptionNumber = () => {
            if (this.exceptionLine == null || this.exceptionLine == undefined)
                return null;

            var temp = this.exceptionLine;
            this.exceptionLine = null;
            return temp;
        };

        $(document).on("click", ".sf-chart-script-edit", e => {
            e.preventDefault();

            var $textArea = this.element.find("textarea.sf-chart-currentScript");

            var win = window.open($textArea.data("url"));
        });

        $(document).on("mousedown", this.prefix.child("sfFullScreen"), e => {
            e.preventDefault();
            this.fullScreen(e);
        });
    }

    requestData(): FormObject {

        var result = this.$chartControl.find(":input:not(#webQueryName)").serializeObject();

        result["webQueryName"] = this.options.webQueryName;
        result["filters"] = this.filterBuilder.serializeFilters();
        result["orders"] = this.serializeOrders();

        return result;
    }

    updateChartBuilder(tokenChanged?: string) {
        var $chartBuilder = this.$chartControl.find(".sf-chart-builder");
        var data = this.requestData();
        if (!SF.isEmpty(tokenChanged)) {
            data["lastTokenChanged"] = tokenChanged;
        }
        $.ajax({
            url: $chartBuilder.attr("data-url"),
            data: data,
        }).then((result) => {
            $chartBuilder.replaceWith(result);
            if (this.reDrawOnUpdateBuilder) {
                this.reDraw();
                this.reDrawOnUpdateBuilder = false;
            }
        });
    }


    showError(e, __baseLineNumber__, chart) {
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
    }

    reDraw() {
        var $chartContainer = this.$chartControl.find(".sf-chart-container");

        $chartContainer.html("");

        var data = $chartContainer.data("json");
        ChartUtils.fillAllTokenValueFuntions(data);

        $(".sf-chart-redraw-onchange", this.$chartControl).each((i, element) => {
            var $element = $(element);
            var name = $element.attr("id");
            if (!SF.isEmpty(this.options.prefix)) {
                name = name.substring(this.options.prefix.length + 1, name.length);
            }
            var nameParts = name.split('_');
            if (nameParts.length == 3 && nameParts[0] == "Columns") {
                var column = data.columns["c" + nameParts[1]];
                switch (nameParts[2]) {
                    case "DisplayName": column.title = $element.val(); break;
                    case "Parameter1": column.parameter1 = $element.val(); break;
                    case "Parameter2": column.parameter2 = $element.val(); break;
                    case "Parameter3": column.parameter3 = $element.val(); break;
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
        var __baseLineNumber__: number;
        try {
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
        var data = this.requestData();

        if (Validator.entityIsValid({ prefix: this.options.prefix, controllerUrl: validateUrl, requestExtraJsonData: data}))
            SF.submitOnly(exportUrl, data);
    }

    requestProcessedData() {
        return {
            webQueryName: this.options.webQueryName,
            filters: this.filterBuilder.serializeFilters(),
            orders: this.serializeOrders()
        };
    }

    fullScreen(evt) {
        evt.preventDefault();

        var url = this.$chartControl.find(".sf-chart-container").attr("data-fullscreen-url") ||
            this.$chartControl.attr("data-fullscreen-url");

        url += (url.indexOf("?") < 0 ? "?" : "&") + $.param(this.requestData());

        if (evt.ctrlKey || evt.which == 2) {
            window.open(url);
        }
        else if (evt.which == 1) {
            window.location.href = url;
        }
    }

    initOrders() {
        this.prefix.child("tblResults").get().on("click", "th", e => {
            this.newSortOrder($(e.currentTarget), e.shiftKey);
            this.$chartControl.find(".sf-chart-draw").click();
            return false;
        });
    }

    bindMouseClick($chartContainer: JQuery) {

        $chartContainer.find('[data-click]').click(e=> {

            var url = $chartContainer.attr('data-open-url');

            var win = window.open("about:blank");

            var $chartControl = $chartContainer.closest(".sf-chart-control");
            getFor($chartControl.attr("data-prefix")).then(cb=> {
                var options = $chartControl.find(":input").not($chartControl.find(".sf-filters-list :input")).serialize();
                options += "&webQueryName=" + cb.options.webQueryName;
                options += "&orders=" + cb.serializeOrders();
                options += "&filters=" + cb.filterBuilder.serializeFilters();
                options += $(e.currentTarget).data("click");

                win.location.href = (url + (url.indexOf("?") >= 0 ? "&" : "?") + options);
            }); 
        });
    }
}

