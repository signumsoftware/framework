import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { QueryRequest } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName  } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { ExcelReportEntity, ExcelMessage } from './Signum.Entities.Excel'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as ChartClient from '../Chart/ChartClient'
import { ChartPermission } from '../Chart/Signum.Entities.Chart'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import ExcelMenu from './ExcelMenu'

export function start(options: { routes: JSX.Element[], plainExcel: boolean, excelReport: boolean }) {
    
    if (options.excelReport) {
        Navigator.addSettings(new EntitySettings(ExcelReportEntity, e => import('./Templates/ExcelReport')));
    }

    Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {

        if (!ctx.searchControl.props.showBarExtension || (ctx.searchControl.props.showBarExtensionOption && ctx.searchControl.props.showBarExtensionOption.showExcelMenu == false))
            return undefined;

        return <ExcelMenu searchControl={ctx.searchControl} plainExcel={options.plainExcel} excelReport={options.excelReport} />;
    }); 

    if (options.plainExcel) {
        ChartClient.ButtonBarChart.onButtonBarElements.push(ctx => {
            if (!AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting))
                return undefined;

            return (
                <button
                    className="sf-query-button sf-chart-script-edit btn btn-light"
                    onClick={() => { API.generatePlanExcel(ChartClient.API.getRequest(ctx.chartRequest)); }}>
                    <i className="fa fa-file-excel-o"></i> &nbsp; {ExcelMessage.ExcelReport.niceToString()}
                </button>
            );
        });
    }
}

export namespace API {

    export function generatePlanExcel(request: QueryRequest): void {
        ajaxPostRaw({ url: "~/api/excel/plain" }, request)
            .then(response => saveFile(response))
            .done();
    }

    export function forQuery(queryKey: string): Promise<Lite<ExcelReportEntity>[]> {
        return ajaxGet<Lite<ExcelReportEntity>[]>({ url: "~/api/excel/reportsFor/" + queryKey });
    }


    export function generateExcelReport(queryRequest: QueryRequest, excelReport: Lite<ExcelReportEntity>): void{
        ajaxPostRaw({ url: "~/api/excel/excelReport" }, { queryRequest, excelReport })
            .then(response => saveFile(response))
            .done();
    }
}

declare module '../../../Framework/Signum.React/Scripts/SearchControl/SearchControlLoaded' {

    export interface ShowBarExtensionOption {
        showExcelMenu?: boolean;
    }
}
