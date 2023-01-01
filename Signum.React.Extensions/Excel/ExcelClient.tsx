import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ajaxPostRaw, ajaxGet, saveFile, ajaxPost } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { QueryRequest, QueryToken } from '@framework/FindOptions'
import { Entity, Lite } from '@framework/Signum.Entities'
import { ExcelReportEntity, ExcelMessage, ExcelPermission, ImportExcelModel } from './Signum.Entities.Excel'
import * as AuthClient from '../Authorization/AuthClient'
import * as ChartClient from '../Chart/ChartClient'
import { ChartPermission } from '../Chart/Signum.Entities.Chart'
import ExcelMenu from './ExcelMenu'
import { ImportExcelProgressModal } from './ImportExcelProgressModal';
import { TypeInfo } from '@framework/Reflection';
import { softCast } from '@framework/Globals';
import { QueryString } from '@framework/QueryString';

export function start(options: { routes: JSX.Element[], plainExcel: boolean, importFromExcel: boolean, excelReport: boolean }) {

  if (options.excelReport) {
    Navigator.addSettings(new EntitySettings(ExcelReportEntity, e => import('./Templates/ExcelReport')));
  }

  if (options.importFromExcel) {
    Navigator.addSettings(new EntitySettings(ImportExcelModel, e => import('./Templates/ImportExcelModel')));
  }

  Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {

    if (!ctx.searchControl.props.showBarExtension ||
      !(ctx.searchControl.props.showBarExtensionOption?.showExcelMenu ?? ctx.searchControl.props.largeToolbarButtons))
      return undefined;

    if (!(options.plainExcel && AuthClient.isPermissionAuthorized(ExcelPermission.PlainExcel)) &&
      !(options.excelReport && Navigator.isViewable(ExcelReportEntity)))
      return undefined;

    return {
      button: <ExcelMenu searchControl={ctx.searchControl}
        plainExcel={options.plainExcel}
        importFromExcel={options.importFromExcel}
        excelReport={options.excelReport && Navigator.isViewable(ExcelReportEntity)} />
    };
  });

  if (options.plainExcel) {
    ChartClient.ButtonBarChart.onButtonBarElements.push(ctx => {
      if (!AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting) || !AuthClient.isPermissionAuthorized(ExcelPermission.PlainExcel))
        return undefined;

      return (
        <button
          className="sf-query-button sf-chart-script-edit btn btn-light"
          onClick={() => { API.generatePlainExcel(ChartClient.API.getRequest(ctx.chartRequest)); }}>
          <FontAwesomeIcon icon={["far", "file-excel"]} /> &nbsp; {ExcelMessage.ExcelReport.niceToString()}
        </button>
      );
    });
  }
}

export namespace API {

  export function generatePlainExcel(request: QueryRequest, overrideFileName?: string, forImport?: boolean): void {
    ajaxPostRaw({ url: "~/api/excel/plain?" + QueryString.stringify({ forImport }) }, request)
      .then(response => saveFile(response, overrideFileName));
  }

  export function forQuery(queryKey: string): Promise<Lite<ExcelReportEntity>[]> {
    return ajaxGet({ url: "~/api/excel/reportsFor/" + queryKey });
  }


  export function generateExcelReport(queryRequest: QueryRequest, excelReport: Lite<ExcelReportEntity>): void {
    ajaxPostRaw({ url: "~/api/excel/excelReport" }, { queryRequest, excelReport })
      .then(response => saveFile(response));
  }

  export function validateForImport(queryRequest: QueryRequest): Promise<QueryToken | undefined> {
    return ajaxPost({ url: "~/api/excel/validateForImport" }, queryRequest);
  }

  export function importFromExcel(qr: QueryRequest, model: ImportExcelModel, type: TypeInfo): Promise<ImportFromExcelReport> {
    var abortController = new AbortController();
    return ImportExcelProgressModal.show(abortController, type,
      () => ajaxPostRaw({ url: "~/api/excel/import", signal: abortController.signal }, softCast<ImportFromExcelRequest>({ importModel: model, queryRequest : qr }))
    );
  }
}



export interface ImportFromExcelRequest {
  importModel: ImportExcelModel;
  queryRequest: QueryRequest;
}

export interface ImportResult {
  totalRows: number;
  rowIndex: string;
  entity?: Lite<Entity>;
  action: ImportAction; 
  error?: string;
}

export type ImportAction = "Updated" | "Inserted" | "NoChanges";

export interface ImportFromExcelReport {
  results: ImportResult[];
  error?: any;
}

declare module '@framework/SearchControl/SearchControlLoaded' {

  export interface ShowBarExtensionOption {
    showExcelMenu?: boolean;
  }
}
