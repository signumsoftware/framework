/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function toPlainExcel(prefix: string, url: string) {
    var info = Finder.getFor(prefix).requestDataForSearch();

    return SF.submitOnly(url, info); 
}

export function toExcelReport(prefix: string, url: string, excelReportKey : string) {
    var info = Finder.getFor(prefix).requestDataForSearch();

    return SF.submitOnly(url, $.extend({excelReport: excelReportKey}, info));
}


export function administerExcelReports(prefix: string, excelReportQueryName: string, queryKey: string) {

    Finder.explore({
        create: false,
        prefix: SF.compose(prefix, "New"),
        webQueryName: excelReportQueryName,
        searchOnLoad: true,
        filters: [{ columnName: "Query", operation: Finder.FilterOperation.EqualTo, value: queryKey }],
    });
}

export function createExcelReports(prefix: string, url: string, query: string) {
    Navigator.navigatePopup(Entities.EntityHtml.withoutType(SF.compose(prefix, "New")), {
        controllerUrl: url,
        requestExtraJsonData: { query: query }
    });
}
