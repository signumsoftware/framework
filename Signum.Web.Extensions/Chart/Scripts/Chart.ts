/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
/// <reference path="ChartUtils.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

import ChartUtils = require("ChartUtils");

import d3 = require("d3")

export function openChart(prefix: string, url: string) {
    Finder.getFor(prefix)
        .then(sc=> sc.requestDataForSearch(Finder.RequestType.FindOptions))
        .then(data=> SF.submit(url, data));
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



export interface ChartBuilderOptions {
    webQueryName: string;
    prefix: string;
    updateChartBuilderUrl: string;
}

export class ChartBuilder {

    options: ChartBuilderOptions;

    container: JQuery;

    isDrawn = (isEquiv: boolean) => false;
    fastRedraw = () => { };

    constructor(options: ChartBuilderOptions) {

        this.options = options;

        this.container = options.prefix.child("sfChartBuilderContainer").get();
        this.container.data("SF-control", this);

        this.container.on("click", ".sf-chart-img", e => {
            var img = $(e.currentTarget);
            img.closest(".sf-chart-type").find(".sf-chart-type-value").val(img.attr("data-related"));

            var isFastRedraw = this.isDrawn(img.hasClass("sf-chart-img-equiv"));

            this.updateChartBuilder("-1").then(() => {
                if (isFastRedraw)
                    this.fastRedraw();
            });
        });

        this.container.on("change", ".sf-chart-group-trigger", e=> {
            this.container.find(".sf-chart-group-results").val($(e.currentTarget).is(":checked").toString());
            this.updateChartBuilder();
        });

        this.container.on("click", ".sf-chart-token-config-trigger", e => {
            $(e.currentTarget).closest(".sf-chart-token").next().toggle();
        });

        this.container.on("change", ".sf-query-token select", e => {
            var token = $(e.currentTarget);
            var id = token.attr("id");
            Finder.QueryTokenBuilder.clearChildSubtokenCombos(token);
            this.updateChartBuilder(token.closest("tr").attr("data-token"));
        });

        this.container.SFControlFullfill(this);
    }

    updateChartBuilder(tokenChanged?: string): Promise<void> {
        var data = this.requestData();
        if (!SF.isEmpty(tokenChanged)) {
            data["lastTokenChanged"] = tokenChanged;
        }

        return SF.ajaxPost({
            url: this.options.updateChartBuilderUrl,
            data: data,
        }).then((result) => {
            this.container.html(result);
        });
    }

    requestData(): FormObject {

        var result = this.container.find(":input").serializeObject();

        result["webQueryName"] = this.options.webQueryName;

        return result;
    }
}


export function getFor(prefix: string): Promise<ChartRequest> {
    return prefix.child("sfChartControl").get().SFControl<ChartRequest>();
};


export interface ChartRequestOptions extends ChartBuilderOptions {
    prefix: string;

    addFilterUrl: string;
    fullScreenUrl: string;
    drawUrl: string;
    openUrl: string;

    orders?: Finder.OrderOption[];

    mode: ChartRequestMode
}

export enum ChartRequestMode {
    Complete,
    Chart,
    Data
}


export class ChartRequest {

    options: ChartRequestOptions;
    filterBuilder: Finder.FilterBuilder;
    chartBuilder: ChartBuilder;
    exceptionLine: number;
    chartControl: JQuery;
    divResults: JQuery;

    serializeOrders() {
        return Finder.serializeOrders(this.options.orders);
    }

    constructor(options: ChartRequestOptions) {

        this.options = options;

        this.chartControl = options.prefix.child("sfChartControl").get();
        this.chartControl.data("SF-control", this);

        this.divResults = options.prefix.child("divResults").get();

        this.filterBuilder = new Finder.FilterBuilder(
            this.options.prefix.child("tblFilterBuilder").get(),
            this.options.prefix,
            this.options.webQueryName,
            this.options.addFilterUrl);

        this.chartBuilder = new ChartBuilder(this.options);

        if (this.options.mode == ChartRequestMode.Complete) {

            this.chartBuilder.isDrawn = (isEqiv) => {

                if (isEqiv && this.divResults.find("svg").length > 0)
                    return true;

                this.divResults.html("");
                return false;
            };

            this.chartBuilder.fastRedraw = () => this.reDraw();

            $(this.chartControl).on("change", ".sf-chart-redraw-onchange",() => {
                this.reDraw();
            });



            this.options.prefix.child("qbDraw").get().click(e => {
                e.preventDefault();
                this.request();
            });

            window.changeTextArea = (value, runtimeInfo) => {
                if ($("#ChartScript_sfRuntimeInfo").val() == runtimeInfo) {
                    var $textArea = this.chartControl.find("textarea.sf-chart-currentScript");

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

            this.options.prefix.child("qbEdit").get().on("click", e => {
                e.preventDefault();
                var $textArea = this.chartControl.find("textarea.sf-chart-currentScript");
                var win = window.open($textArea.data("url"));
            });

        } else {
            this.request();
        }

        this.options.prefix.child("sfFullScreen").get().on("mouseup", e => {
            e.preventDefault();
            this.fullScreen(e);
        });


        this.chartControl.SFControlFullfill(this);
    }


    request() {
        SF.ajaxPost({
            url: this.options.drawUrl,
            data: $.extend(this.requestData(), { "mode": this.options.mode })
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
                this.chartControl.find(".sf-search-results-container").html(result);

                if (this.options.mode == ChartRequestMode.Complete || this.options.mode == ChartRequestMode.Data) {
                    this.initOrders();
                }

                if (this.options.mode == ChartRequestMode.Complete || this.options.mode == ChartRequestMode.Chart) {
                    this.reDraw();
                }
            }
        });
    }

    requestData(): FormObject {

        var result = this.chartBuilder.requestData();

        result["filters"] = this.filterBuilder.serializeFilters();
        result["orders"] = this.serializeOrders();

        return result;
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
        chart.append('svg:rect').attr('class', 'sf-chart-error').attr("y",(chart.attr("height") / 2) - 10).attr("fill", "#FBEFFB").attr("stroke", "#FAC0DB").attr("width", chart.attr("width") - 1).attr("height", 20);
        chart.append('svg:text').attr('class', 'sf-chart-error').attr("y", chart.attr("height") / 2).attr("fill", "red").attr("dy", 5).attr("dx", 4).text(message);
    }

    reDraw() {
        var $chartContainer = this.chartControl.find(".sf-chart-container");

        $chartContainer.html("");
        

        var data = JSON.parse($chartContainer.attr("data-json"));
        ChartUtils.fillAllTokenValueFuntions(data);

        $(".sf-chart-redraw-onchange[id]", this.chartControl).each((i, element) => {
            var $element = $(element);
            var name = $element.attr("id");
            if (!SF.isEmpty(this.options.prefix)) {
                name = name.substring(this.options.prefix.length + 1, name.length);
            }
            var nameParts = name.split('_');
            if (nameParts.length == 3 && nameParts[0] == "Columns" && nameParts[2] == "DisplayName") {
                var column = data.columns["c" + nameParts[1]];

                if (column)
                    column.title = $element.val();

            } else if (nameParts.length == 3 && nameParts[0] == "Parameters" && nameParts[2] == "Value") {

                var nameId = $element.attr("id").beforeLast("_Value") + "_Name";

                data.parameters[nameId.get().val()] = $element.val();
            }
        });

        var width = $chartContainer.width();
        var height = $chartContainer.height();

        var code = "(" + this.chartControl.find('textarea.sf-chart-currentScript').val() + ")";

        var chart = d3.select('#' + this.chartControl.attr("id") + " .sf-chart-container")
            .append('svg:svg').attr('width', width).attr('height', height);



        var func;
        var __baseLineNumber__: number;
        try {
            const getClickKeys = ChartUtils.getClickKeys;
            const translate = ChartUtils.translate;
            const scale = ChartUtils.scale;
            const rotate = ChartUtils.rotate;
            const skewX = ChartUtils.skewX;
            const skewY = ChartUtils.skewY;
            const matrix = ChartUtils.matrix;
            const scaleFor = ChartUtils.scaleFor;
            const rule = ChartUtils.rule;
            const ellipsis = ChartUtils.ellipsis;
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

        if (Validator.entityIsValid({ prefix: this.options.prefix, controllerUrl: validateUrl, requestExtraJsonData: data }))
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

        var url = this.options.fullScreenUrl;

        url += (url.indexOf("?") < 0 ? "?" : "&") + $.param(this.requestData());

        if (evt.ctrlKey || evt.which == 2) {
            window.open(url);
        }
        else if (evt.which == 1) {
            window.location.href = url;
        }
    }

    initOrders() {
        this.options.prefix.child("tblResults").get().on("click", "th", e => {
            Finder.SearchControl.newSortOrder(this.options.orders, $(e.currentTarget), e.shiftKey);
            this.chartControl.find(".sf-chart-draw").click();
            return false;
        });
    }

    bindMouseClick($chartContainer: JQuery) {

        $chartContainer.find('[data-click]').click(e=> {

            var url = this.options.openUrl;

            var options = this.chartControl.find(":input").not(this.chartControl.find(".sf-filters-list :input, .sf-chart-parameters :input")).serializeObject();
            options["webQueryName"] = this.options.webQueryName;
            options["orders"] = this.serializeOrders();
            options["filters"] = this.filterBuilder.serializeFilters();

            var params = $.param(options) + $(e.currentTarget).data("click");
            window.open(url + (url.indexOf("?") >= 0 ? "&" : "?") + params);
        });
    }
}

