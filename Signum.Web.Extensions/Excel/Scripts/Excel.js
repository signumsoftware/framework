/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Navigator", "Framework/Signum.Web/Signum/Scripts/Finder"], function(require, exports, Entities, Navigator, Finder) {
    function toPlainExcel(prefix, url) {
        Finder.getFor(prefix).then(function (sc) {
            var info = sc.requestDataForSearch(0 /* QueryRequest */);

            return SF.submitOnly(url, info);
        });
    }
    exports.toPlainExcel = toPlainExcel;

    function toExcelReport(prefix, url, excelReportKey) {
        Finder.getFor(prefix).then(function (sc) {
            var info = sc.requestDataForSearch(0 /* QueryRequest */);

            return SF.submitOnly(url, $.extend({ excelReport: excelReportKey }, info));
        });
    }
    exports.toExcelReport = toExcelReport;

    function administerExcelReports(prefix, excelReportQueryName, queryKey) {
        Finder.explore({
            create: false,
            prefix: prefix.child("New"),
            webQueryName: excelReportQueryName,
            searchOnLoad: true,
            filters: [{ columnName: "Query", operation: 0 /* EqualTo */, value: queryKey }]
        });
    }
    exports.administerExcelReports = administerExcelReports;

    function createExcelReports(prefix, url, query) {
        Navigator.navigatePopup(Entities.EntityHtml.withoutType(prefix.child("New")), {
            controllerUrl: url,
            requestExtraJsonData: { query: query }
        });
    }
    exports.createExcelReports = createExcelReports;
});
//# sourceMappingURL=Excel.js.map
