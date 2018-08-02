import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Route } from 'react-router'
import { Dic, classes } from '@framework/Globals';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '@framework/Services';
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { QueryRequest } from '@framework/FindOptions'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '@framework/Signum.Entities'
import { EntityOperationSettings } from '@framework/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName  } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import { ExcelReportEntity, ExcelMessage } from './Signum.Entities.Excel'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as ChartClient from '../Chart/ChartClient'
import { ChartPermission } from '../Chart/Signum.Entities.Chart'
import * as QuickLinks from '@framework/QuickLinks'
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
                    <FontAwesomeIcon icon={["far", "file-excel"]} /> &nbsp; {ExcelMessage.ExcelReport.niceToString()}
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

declare module '@framework/SearchControl/SearchControlLoaded' {

    export interface ShowBarExtensionOption {
        showExcelMenu?: boolean;
    }
}
