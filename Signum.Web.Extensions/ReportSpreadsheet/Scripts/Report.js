/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Navigator", "Framework/Signum.Web/Signum/Scripts/Finder"], function(require, exports, Entities, Navigator, Finder) {
    function toPlainExcel(prefix, url) {
        var info = Finder.getFor(prefix).requestDataForSearch();

        return SF.submitOnly(url, info);
    }
    exports.toPlainExcel = toPlainExcel;

    function toExcelReport(prefix, url, excelReportKey) {
        var info = Finder.getFor(prefix).requestDataForSearch();

        return SF.submitOnly(url, $.extend({ excelReport: excelReportKey }, info));
    }
    exports.toExcelReport = toExcelReport;

    function administerExcelReports(prefix, excelReportQueryName, queryKey) {
        Finder.explore({
            create: false,
            prefix: prefix,
            webQueryName: excelReportQueryName,
            searchOnLoad: true,
            filters: [{ columnName: "Query", operation: 0 /* EqualTo */, value: queryKey }]
        });
    }
    exports.administerExcelReports = administerExcelReports;

    function createExcelReports(prefix, url) {
        Navigator.navigatePopup(Entities.EntityHtml.withoutType(prefix), {
            controllerUrl: url,
            requestExtraJsonData: {}
        });
    }
    exports.createExcelReports = createExcelReports;
});
//# sourceMappingURL=Report.js.map
