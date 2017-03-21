/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function toPlainExcel(prefix: string, url: string) {
    Finder.getFor(prefix)
        .then(sc=> sc.requestDataForSearch(Finder.RequestType.QueryRequest))
        .then(info=> SF.submitOnly(url, info));
}

export function toExcelReport(prefix: string, url: string, excelReportKey : string) {
    //Finder.getFor(prefix).then(sc=>
    //{
    //    var info = sc.requestDataForSearch(Finder.RequestType.QueryRequest);

    //    return SF.submitOnly(url, $.extend({ excelReport: excelReportKey }, info));
    //});


    Finder.getFor(prefix)
        .then(sc => sc.requestDataForSearch(Finder.RequestType.QueryRequest))
        .then(info => SF.submitOnly(url, $.extend({ excelReport: excelReportKey }, info)));
      

        
   
}

export function administerExcelReports(prefix: string, excelReportQueryName: string, queryKey: string) {

    Finder.explore({
        create: false,
        prefix: prefix.child("New"),
        webQueryName: excelReportQueryName,
        searchOnLoad: true,
        filters: [{ columnName: "Query", operation: Finder.FilterOperation.EqualTo, value: queryKey }],
    });
}

export function createExcelReports(prefix: string, url: string, query: string) {
    Navigator.navigatePopup(Entities.EntityHtml.withoutType(prefix.child("New")), {
        controllerUrl: url,
        requestExtraJsonData: { query: query }
    });
}


export function attachUserQuery(el: Lines.EntityLine, relatedId: string, controllerUrl: string) {

    function refreshImplementations(userQueryRI: Entities.RuntimeInfo) {
        relatedId.get().toggle(!!userQueryRI);

        if (userQueryRI)
            SF.ajaxPost({ url: controllerUrl, data: { userQuery: userQueryRI.key() } }).then((types: Entities.TypeInfo[]) => {
                relatedId.get().toggle(!!types.length);

                relatedId.get().SFControl<Lines.EntityLine>().then(el2=> {

                    if (el2.getRuntimeInfo() && !types.some(t=> t.name == el2.getRuntimeInfo().type))
                        el2.setEntity(null);

                    el2.options.types = types;
                    el2.options.create = false;
                    el2.options.find = true;
                });
            });
    }

    refreshImplementations(el.getRuntimeInfo());
    el.entityChanged = (entity) => refreshImplementations(entity ? entity.runtimeInfo : null);

}

