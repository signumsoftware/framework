import * as React from 'react'
import * as moment from 'moment'
import * as numbro from 'numbro'
import * as QueryString from 'query-string'
import { ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Entity, Lite, liteKey, MList } from '@framework/Signum.Entities'
import { getQueryKey, getEnumInfo, QueryTokenString } from '@framework/Reflection'
import {
  FilterOption, OrderOption, OrderOptionParsed, QueryRequest, QueryToken, SubTokensOptions, ResultTable, OrderRequest, OrderType, FilterOptionParsed, hasAggregate, ColumnOption, withoutAggregateAndPinned
} from '@framework/FindOptions'
import * as AuthClient from '../Authorization/AuthClient'
import {
  UserChartEntity, ChartPermission, ChartColumnEmbedded, ChartParameterEmbedded, ChartRequestModel,
  IChartBase, ChartColumnType, ChartParameterType, ChartScriptSymbol, D3ChartScript, GoogleMapsCharScript
} from './Signum.Entities.Chart'
import { QueryTokenEmbedded } from '../UserAssets/Signum.Entities.UserAssets'
import ChartButton from './ChartButton'
import ChartRequestView from './Templates/ChartRequestView'
import * as UserChartClient from './UserChart/UserChartClient'
import { ImportRoute } from "@framework/AsyncImport";
import { ColumnRequest } from '@framework/FindOptions';
import { toMomentFormat } from '@framework/Reflection';
import { toNumbroFormat } from '@framework/Reflection';
import { toFilterRequests, toFilterOptions } from '@framework/Finder';
import { Dic } from '@framework/Globals';
import { resetPassword } from '../Authorization/AuthClient';

export function start(options: { routes: JSX.Element[], googleMapsApiKey?: string }) {

  options.routes.push(<ImportRoute path="~/chart/:queryName" onImportModule={() => import("./Templates/ChartRequestPage")} />);

  Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
    if (!ctx.searchControl.props.showBarExtension ||
      !AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting) ||
      (ctx.searchControl.props.showBarExtensionOption && ctx.searchControl.props.showBarExtensionOption.showChartButton == false))
      return undefined;

    return <ChartButton searchControl={ctx.searchControl} />;
  });

  UserChartClient.start({ routes: options.routes });


  registerChartScriptComponent(D3ChartScript.Bars, () => import("./D3Scripts/Bars"));
  registerChartScriptComponent(D3ChartScript.BubblePack, () => import("./D3Scripts/BubblePack"));
  registerChartScriptComponent(D3ChartScript.Bubbleplot, () => import("./D3Scripts/Bubbleplot"));
  registerChartScriptComponent(D3ChartScript.CalendarStream, () => import("./D3Scripts/CalendarStream"));
  registerChartScriptComponent(D3ChartScript.Columns, () => import("./D3Scripts/Columns"));
  registerChartScriptComponent(D3ChartScript.Line, () => import("./D3Scripts/Line"));
  registerChartScriptComponent(D3ChartScript.MultiBars, () => import("./D3Scripts/MultiBars"));
  registerChartScriptComponent(D3ChartScript.MultiColumns, () => import("./D3Scripts/MultiColumns"));
  registerChartScriptComponent(D3ChartScript.MultiLines, () => import("./D3Scripts/MultiLines"));
  registerChartScriptComponent(D3ChartScript.ParallelCoordinates, () => import("./D3Scripts/ParallelCoordiantes"));
  registerChartScriptComponent(D3ChartScript.Pie, () => import("./D3Scripts/Pie"));
  registerChartScriptComponent(D3ChartScript.Punchcard, () => import("./D3Scripts/Punchcard"));
  registerChartScriptComponent(D3ChartScript.Scatterplot, () => import("./D3Scripts/Scatterplot"));
  registerChartScriptComponent(D3ChartScript.StackedBars, () => import("./D3Scripts/StackedBars"));
  registerChartScriptComponent(D3ChartScript.StackedColumns, () => import("./D3Scripts/StackedColumns"));
  registerChartScriptComponent(D3ChartScript.StackedLines, () => import("./D3Scripts/StackedLines"));
  registerChartScriptComponent(D3ChartScript.Treemap, () => import("./D3Scripts/Treemap"));

  if (options.googleMapsApiKey) {
    window.__google_api_key = options.googleMapsApiKey;
    registerChartScriptComponent(GoogleMapsCharScript.Heatmap, () => import("./GoogleMapScripts/Heatmap"));
    registerChartScriptComponent(GoogleMapsCharScript.Markermap, () => import("./GoogleMapScripts/Markermap"));
  }
  }

export interface ChartComponentProps {
  data?: ChartTable;
  parameters: { [name: string]: string },
  loading: boolean;
  onDrillDown: (e: ChartRow) => void;
}

export interface ChartScriptProps extends ChartComponentProps {
  width: number;
  height: number;
  initialLoad: boolean;
}

interface ChartScriptModule {
  default: (React.ComponentClass<ChartComponentProps>) | ((p: ChartScriptProps) => React.ReactNode);
}

const registeredChartScriptComponents: { [key: string]: () => Promise<ChartScriptModule> } = {};

export function registerChartScriptComponent(symbol: ChartScriptSymbol, module: () => Promise<ChartScriptModule>) {
  registeredChartScriptComponents[symbol.key] = module;
}

export function getRegisteredChartScriptComponent(symbol: ChartScriptSymbol): ()=> Promise<ChartScriptModule> {

  var result = registeredChartScriptComponents[symbol.key];
  if (!result)
    throw new Error("No chartScriptComponent registered in ChartClient for " + symbol.key);

  return result;
}

export namespace ButtonBarChart {

  interface ButtonBarChartContext {
    chartRequestView: ChartRequestView;
    chartRequest: ChartRequestModel;
  }

  export const onButtonBarElements: ((ctx: ButtonBarChartContext) => React.ReactElement<any> | undefined)[] = [];

  export function getButtonBarElements(ctx: ButtonBarChartContext): React.ReactElement<any>[] {
    return onButtonBarElements.map(f => f(ctx)).filter(a => a != undefined).map(a => a!);
  }
}

export interface ChartScript {
  symbol: ChartScriptSymbol;
  icon: { fileName: string; bytes: string };
  columns: ChartScriptColumn[];
  parameterGroups: ChartScriptParameterGroup[];
}

export interface ChartScriptColumn {
  displayName: string;
  isOptional: boolean;
  columnType: ChartColumnType;
}

export interface ChartScriptParameterGroup {
  name: string;
  parameters: ChartScriptParameter[];
}

export interface ChartScriptParameter {
  name: string;
  columnIndex?: number;
  type: ChartParameterType;
  valueDefinition: NumberInterval | EnumValueList | StringValue | null;
}

export interface NumberInterval {
  defaultValue: number;
  minValue: number | null;
  maxValue: number | null;
}

export interface EnumValueList extends Array<EnumValue> {

}

export interface EnumValue {
  name: string;
  typeFilter?: ChartColumnType;
}

export interface StringValue {
  defaultValue: number;
}


export let chartScripts: ChartScript[];
export function getChartScripts(): Promise<ChartScript[]> {
  if (chartScripts)
    return Promise.resolve(chartScripts);

  return API.fetchScripts().then(cs => chartScripts = cs);
}

export function getChartScript(symbol: ChartScriptSymbol): Promise<ChartScript> {
  return getChartScripts().then(cs => cs.single(a => a.symbol.key == symbol.key));
}

export let colorPalettes: string[];
export function getColorPalettes(): Promise<string[]> {
  if (colorPalettes)
    return Promise.resolve(colorPalettes);

  return API.fetchColorPalettes().then(cs => colorPalettes = cs);
}

export function hasAggregates(chartBase: IChartBase): boolean {
  if (chartBase.columns.map(c => c.element.token).some(t => t != null && t.token != null && t.token.queryTokenType == "Aggregate"))
    return true;

  if (UserChartEntity.isInstance(chartBase))
    return chartBase.filters.map(f => f.element && f.element.token).some(t => t != null && t.token != null && t.token.queryTokenType == "Aggregate");
  else
    return chartBase.filterOptions.some(fo => Finder.isAggregate(fo));
}

export function isCompatibleWith(chartScript: ChartScript, chartBase: IChartBase): boolean {

  return zipOrDefault(
    chartScript.columns,
    chartBase.columns.map(mle => mle.element), (s, c) => {

      if (s == undefined)
        return c!.token == undefined;

      if (c == undefined || c.token == undefined)
        return s.isOptional!;

      if (!isChartColumnType(c.token.token, s.columnType!))
        return false;

      return true;
    }).every(b => b);
}

export function zipOrDefault<T, S, R>(arrayT: T[], arrayS: S[], selector: (t: T | undefined, s: S | undefined) => R): R[] {
  const max = Math.max(arrayT.length, arrayS.length);

  const result: R[] = [];
  for (let i = 0; i < max; i++) {
    result.push(selector(
      i < arrayT.length ? arrayT[i] : undefined,
      i < arrayS.length ? arrayS[i] : undefined));
  }

  return result;
}

export function isChartColumnType(token: QueryToken | undefined, ct: ChartColumnType): boolean {
  if (token == undefined)
    return false;

  const type = getChartColumnType(token);

  if (type == undefined)
    return false;

  if (ct == type)
    return true;


  switch (ct) {

    case "Groupable": return [
      "RealGroupable",
      "Integer",
      "Date",
      "String",
      "Lite",
      "Enum"].contains(type);

    case "Magnitude": return [
      "Integer",
      "Real",
      "RealGroupable"].contains(type);

    case "Positionable": return [
      "Integer",
      "Real",
      "RealGroupable",
      "Date",
      "DateTime"].contains(type);
  }


  return false;
}

export function getChartColumnType(token: QueryToken): ChartColumnType | undefined {

  switch (token.filterType) {
    case "Lite": return "Lite";
    case "Boolean":
    case "Enum": return "Enum";
    case "String":
    case "Guid": return "String";
    case "Integer": return "Integer";
    case "Decimal": return token.isGroupable ? "RealGroupable" : "Real";
    case "DateTime": return token.isGroupable ? "Date" : "DateTime";
  }

  return undefined;
}


export function synchronizeColumns(chart: IChartBase, chartScript: ChartScript) {

  if (chart.columns == null ||
    chart.parameters == null)
    throw Error("no Columns");

  for (let i = 0; i < chartScript.columns!.length; i++) {
    if (chart.columns.length <= i) {
      chart.columns.push({ rowId: null, element: ChartColumnEmbedded.New() });
    }
  }

  if (chart.columns.length > chartScript.columns.length) {
    chart.columns.splice(chartScript.columns.length, chart.columns.length - chartScript.columns.length);
  }

  var allChartScriptParameters = chartScript.parameterGroups.flatMap(a => a.parameters);

  if (chart.parameters.map(a => a.element.name!).orderBy(n => n).join(" ") !=
    allChartScriptParameters.map(a => a.name!).orderBy(n => n).join(" ")) {

    const byName = chart.parameters.map(a => a.element).toObject(a => a.name!);
    chart.parameters.clear();

    allChartScriptParameters.forEach(sp => {
      let cp = byName[sp.name!];

      if (cp == undefined) {
        cp = ChartParameterEmbedded.New();
        cp.name = sp.name;
        const column = sp.columnIndex == undefined ? undefined : chart.columns![sp.columnIndex].element;
        cp.value = defaultParameterValue(sp, column && column.token && column.token.token);
      }
      else {
        const column = sp.columnIndex == undefined ? undefined : chart.columns![sp.columnIndex].element;
        if (!isValidParameterValue(cp.value, sp, column && column.token && column.token.token))
          cp.value = defaultParameterValue(sp, column && column.token && column.token.token);
        cp.modified = true;
      }

      chart.parameters!.push({ rowId: null, element: cp });
    });
  }
}

function isValidParameterValue(value: string | null | undefined, scriptParameter: ChartScriptParameter, relatedColumn: QueryToken | null | undefined) {

  switch (scriptParameter.type) {
    case "Enum": return (scriptParameter.valueDefinition as EnumValueList).filter(a => a.typeFilter == undefined || relatedColumn == undefined || isChartColumnType(relatedColumn, a.typeFilter)).some(a => a.name == value);
    case "Number": return !isNaN(parseFloat(value!));
    case "String": return true;
    default: throw new Error("Unexpected parameter type");
  }

}

function defaultParameterValue(scriptParameter: ChartScriptParameter, relatedColumn: QueryToken | null | undefined) {
  switch (scriptParameter.type) {
    case "Enum": return (scriptParameter.valueDefinition as EnumValueList).filter(a => a.typeFilter == undefined || relatedColumn == undefined || isChartColumnType(relatedColumn, a.typeFilter)).first().name;
    case "Number": return (scriptParameter.valueDefinition as NumberInterval).defaultValue.toString();
    case "String": return (scriptParameter.valueDefinition as StringValue).defaultValue.toString();
    default: throw new Error("Unexpected parameter type");
  }

}

export function cleanedChartRequest(request: ChartRequestModel): ChartRequestModel {
  const clone = { ...request };
  
  clone.filters = toFilterRequests(clone.filterOptions);
  delete clone.filterOptions;

  return clone;
}


export interface ChartOptions {
  queryName: any,
  chartScript?: string,
  groupResults?: boolean,
  filterOptions?: FilterOption[];
  orderOptions?: OrderOption[];
  columnOptions?: ChartColumnOption[];
  parameters?: ChartParameterOption[];
}

export interface ChartColumnOption {
  token?: string | QueryTokenString<any>;
  displayName?: string;
  orderByIndex?: number;
  orderByType?: OrderType;
}

export interface ChartParameterOption {
  name: string;
  value: string;
}

export function handleOrderColumn(cr: IChartBase, col: ChartColumnEmbedded, isShift: boolean) {

  var newOrder = col.orderByType == "Ascending" ? "Descending" : "Ascending" as OrderType;

  if (!isShift) {
    cr.columns.forEach(a => {
      a.element.orderByType = null;
      a.element.orderByIndex = null;
    });

    col.orderByType = newOrder;
    col.orderByIndex = 1;
  } else {

    col.orderByType = newOrder;
    if (col.orderByIndex == null)
      col.orderByIndex = (cr.columns.max(a => a.element.orderByIndex) || 0) + 1;
  }
  
  col.modified = true;
}

export module Encoder {

  export function toChartOptions(cr: ChartRequestModel, cs: ChartScript | null): ChartOptions {

    var params = cs && cs.parameterGroups.flatMap(a => a.parameters).toObject(a => a.name);

    return {
      queryName: cr.queryKey,
      chartScript: cr.chartScript && cr.chartScript.key.after(".") || undefined,
      filterOptions: toFilterOptions(cr.filterOptions),
      columnOptions: cr.columns.map(co => ({
        token: co.element.token && co.element.token.tokenString,
        displayName: co.element.displayName,
        orderByIndex: co.element.orderByIndex,
        orderByType: co.element.orderByType,
      }) as ChartColumnOption),
      parameters: cr.parameters
        .filter(p => {
          if (params == null)
            return true;

          var scriptParam = params![p.element.name!];

          var c = scriptParam.columnIndex != null ? cr.columns[scriptParam.columnIndex].element : null;

          return p.element.value != defaultParameterValue(scriptParam, c && c.token && c.token.token);
        })
        .map(p => ({ name: p.element.name, value: p.element.value }) as ChartParameterOption)
    };
  }

  export function chartPathPromise(cr: ChartRequestModel, userChart?: Lite<UserChartEntity>): Promise<string> {
    var csPromise: Promise<null | ChartScript> = cr.chartScript == null ? Promise.resolve(null) : getChartScript(cr.chartScript);

    return csPromise.then(cs => chartPath(toChartOptions(cr, cs), userChart));
  }
  
  export function chartPath(co: ChartOptions, userChart?: Lite<UserChartEntity>): string {

    const query = {
      script: co.chartScript,
      groupResults: co.groupResults,
      userChart: userChart && liteKey(userChart)
    };

    Finder.Encoder.encodeFilters(query, co.filterOptions);
    Finder.Encoder.encodeOrders(query, co.orderOptions);
    encodeParameters(query, co.parameters);

    encodeColumn(query, co.columnOptions);

    return Navigator.toAbsoluteUrl(`~/chart/${getQueryKey(co.queryName)}?` + QueryString.stringify(query));

  }

  const scapeTilde = Finder.Encoder.scapeTilde;

  export function encodeColumn(query: any, columns: ChartColumnOption[] | undefined) {
    if (columns)
      columns.forEach((co, i) => query["column" + i] =
        (co.orderByIndex != null ? (co.orderByIndex! + (co.orderByType == "Ascending" ? "A" : "D") + "~") : "") +
        (co.token || "") +
        (co.displayName ? ("~" + scapeTilde(co.displayName)) : ""));
  }

  export function encodeParameters(query: any, parameters: ChartParameterOption[] | undefined) {
    if (parameters)
      parameters.map((p, i) => query["param" + i] = scapeTilde(p.name!) + "~" + scapeTilde(p.value!));
  }
}

export module Decoder {

  export function parseChartRequest(queryName: string, query: any): Promise<ChartRequestModel> {

    return getChartScripts().then(scripts => {
      return Finder.getQueryDescription(queryName).then(qd => {

        const completer = new Finder.TokenCompleter(qd);

        const fos = Finder.Decoder.decodeFilters(query);
        fos.forEach(fo => completer.requestFilter(fo, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | SubTokensOptions.CanAggregate));

        const oos = Finder.Decoder.decodeOrders(query);
        oos.forEach(oo => completer.request(oo.token.toString(), SubTokensOptions.CanElement | SubTokensOptions.CanAggregate));

        const cols = Decoder.decodeColumns(query);
        cols.map(a => a.element.token).filter(te => te != undefined).forEach(te => completer.request(te!.tokenString!, SubTokensOptions.CanAggregate | SubTokensOptions.CanElement));

        return completer.finished().then(() => {

          cols.filter(a => a.element.token != null).forEach(a => a.element.token!.token = completer.get(a.element.token!.tokenString));

          var cr = query.script == undefined ? scripts.first("ChartScript") :
            scripts
              .filter(cs => cs.symbol.key.after(".") == query.script)
              .single(`ChartScript '${query.queryKey}'`);
              

          const chartRequest = ChartRequestModel.New({
            chartScript: cr.symbol,
            queryKey: getQueryKey(queryName),
            filterOptions: fos.map(fo => completer.toFilterOptionParsed(fo)),
            columns: cols,
            parameters: Decoder.decodeParameters(query),
          });

          synchronizeColumns(chartRequest, cr);

          return Finder.parseFilterValues(chartRequest.filterOptions)
            .then(() => chartRequest);
        });
      });
    });
  }


  const unscapeTildes = Finder.Decoder.unscapeTildes;
  const valuesInOrder = Finder.Decoder.valuesInOrder;

  export function decodeColumns(query: any): MList<ChartColumnEmbedded> {
    return valuesInOrder(query, "column").map(val => {

      var parts = val.split("~");

      let order, token, displayName: string | null;

      if (parts.length == 3 || parts.length == 2 && /\d+[AD]/.test(parts[0]))
        [order, token, displayName] = parts;
      else
        [token, displayName] = parts;
 
      return ({
        rowId: null,
        element: ChartColumnEmbedded.New({
          token: Boolean(token) ? QueryTokenEmbedded.New({
            tokenString: token,
          }) : undefined,
          orderByType: order == null ? null : (order.charAt(order.length -1) == "A" ? "Ascending" : "Descending"),
          orderByIndex: order == null ? null : (parseInt(order.substr(0, order.length - 1))),
          displayName: unscapeTildes(displayName),
        })
      });
    });
  }

  export function decodeParameters(query: any): MList<ChartParameterEmbedded> {
    return valuesInOrder(query, "param").map(val => ({
      rowId: null,
      element: ChartParameterEmbedded.New({
        name: unscapeTildes(val.before("~")),
        value: unscapeTildes(val.after("~")),
      })
    }));
  }
}


export module API {

  export function getRequest(request: ChartRequestModel): QueryRequest {

    return {
      queryKey: request.queryKey,
      groupResults: hasAggregates(request),
      filters: toFilterRequests(request.filterOptions),
      columns: request.columns.map(mle => mle.element).filter(cce => cce.token != null).map(co => ({ token: co.token!.token!.fullKey }) as ColumnRequest),
      orders: request.columns.filter(mle => mle.element.orderByType != null && mle.element.token != null).orderBy(mle => mle.element.orderByIndex).map(mle => ({ token: mle.element.token!.token!.fullKey, orderType: mle.element.orderByType! }) as OrderRequest),
      pagination: { mode: "All" }
    };
  }

  export function toChartColumnType(token: QueryToken): ChartColumnType | null {
    switch (token.filterType) {
      case "Lite": return "Lite";
      case "Boolean":
      case "Enum": return "Enum";
      case "String":
      case "Guid": return "String";
      case "Integer": return "Integer";
      case "Decimal": return token.isGroupable ? "RealGroupable" : "Real";
      case "DateTime": return token.isGroupable ? "Date" : "DateTime";
      default: return null;
    }
  }

  export function getKey(token: QueryToken): ((val: unknown) => string) {

    if (token.type.isLite)
      return v => String(v && liteKey(v as Lite<Entity>));

    return v => String(v);
  }

  export function getColor(token: QueryToken): ((val: unknown) => string | null) {

    return v => v == null ? "#555" : null;
  }

  export function getNiceName(token: QueryToken): ((val: unknown) => string) {

    if (token.type.isLite)
      return v => {
        var lite = v as Lite<Entity> | null;
        return String(lite && lite.toStr || "");
      };

    if (token.filterType == "Enum")
      return v => {
        var value = v as string | null;

        if (!value)
          return String(null);

        var ei = getEnumInfo(token.type.name, value as any as number);
        return ei ? ei.niceName : value;
      };

    if (token.filterType == "DateTime")
      return v => {
        var date = v as string | null;
        var format = token.format && toMomentFormat(token.format);
        return date == null ? String(null) : moment(date).format(format);
      };

    if (token.format && (token.filterType == "Decimal" || token.filterType == "Integer"))
      return v => {
        var number = v as number | null;
        var format = token.format && toNumbroFormat(token.format);
        return number == null ? String(null) : numbro(number).format(format);
      };

    return v => String(v);
  }

  export function getParameterWithDefault(request: ChartRequestModel, chartScript: ChartScript): { [parameter: string]: string } {

    var defaultValues = chartScript.parameterGroups.flatMap(g => g.parameters).toObject(a => a.name, a => {
      var col = a.columnIndex == null ? null : request.columns[a.columnIndex];
      return defaultParameterValue(a, col && col.element && col.element.token && col.element.token.token);
    });

    return request.parameters.toObject(a => a.element.name!, a => a.element.value || defaultValues[a.element.name!])
  }

  export function toChartResult(request: ChartRequestModel, rt: ResultTable, chartScript: ChartScript): ExecuteChartResult {

    var cols = request.columns.map((mle, i) => {
      const token = mle.element.token && mle.element.token.token;

      if (token == null)
        return null;

      const scriptCol = chartScript.columns[i];

      const value: (r: ChartRow) => undefined = function (r: ChartRow) { return (r as any)["c" + i]; };
      const key = getKey(token);
      const niceName = getNiceName(token);
      const color = getColor(token);


      return {
        name: "c" + i,
        displayName: scriptCol.displayName,
        title: (mle.element.displayName || token && token.niceName) + (token && token.unit ? ` (${token.unit})` : ""),
        token: token,
        type: token && toChartColumnType(token),
        orderByIndex: mle.element.orderByIndex,
        orderByType: mle.element.orderByType,
        getKey: key,
        getNiceName: niceName,
        getColor: color,
        getValue: value,
        getValueKey: row => key(value(row)),
        getValueNiceName: row => niceName(value(row)),
        getValueColor: row => color(value(row)),
      } as ChartColumn<unknown>
    });

    var index = 0;
    var chartColToTableCol = request.columns.map(mle => {
      var token = mle.element.token && mle.element.token.token;

      if (token == null)
        return null;

      return {
        index: index++
      }
    });

    if (!hasAggregates(request)) {
      const value = (r: ChartRow) => r.entity;
      const color = (v: Lite<Entity> | undefined) => !v ? "#555" : null;
      const niceName = (v: Lite<Entity> | undefined) => v && v.toStr;
      const key = (v: Lite<Entity> | undefined) => v ? liteKey(v) : String(v);
      cols.insertAt(0, ({
        name: "entity",
        displayName: "Entity",
        title: "",
        token: undefined,
        type: "Lite",
        getKey: key,
        getNiceName: niceName,
        getColor: color,
        getValue: value,
        getValueKey: row => key(value(row)),
        getValueNiceName: row => niceName(value(row)),
        getValueColor: row => color(value(row)),
      } as ChartColumn<Lite<Entity> | undefined>) as ChartColumn<unknown>);
    }

    var rows = rt.rows.map(row => {
      var cr = request.columns.map((c, i) => {
        var tuple = chartColToTableCol[i];

        if (tuple == null)
          return null;

        var val = row.columns[tuple.index];

        return { colName: "c" + i, cValue: val };
      }).filter(a => a != null).toObject(a => a!.colName, a => a!.cValue) as ChartRow;

      cr.entity = row.entity;

      return cr;
    });

    var chartTable: ChartTable = {
      columns: cols.filter(c => c != null).toObjectDistinct(c => c!.name) as any,
      rows: rows
    };

    return {
      resultTable: rt,
      chartTable: chartTable,
    };
  }


  export function executeChart(request: ChartRequestModel, chartScript: ChartScript, abortSignal?: AbortSignal): Promise<ExecuteChartResult> {
    return Navigator.API.validateEntity(cleanedChartRequest(request)).then(cr => {

      const queryRequest = getRequest(request);

      return Finder.API.executeQuery(queryRequest, abortSignal)
        .then(rt => toChartResult(request, rt, chartScript));
    });
  }

  export interface ExecuteChartResult {
    resultTable: ResultTable;
    chartTable: ChartTable;
  }

  export function fetchScripts(): Promise<ChartScript[]> {
    return ajaxGet<ChartScript[]>({
      url: "~/api/chart/scripts"
    });
  }

  export function fetchColorPalettes(): Promise<string[]> {
    return ajaxGet<string[]>({
      url: "~/api/chart/colorPalettes"
    });
  }
}

export interface ChartTable {
  columns: {
    entity?: ChartColumn<Lite<Entity>>;
    c0?: ChartColumn<unknown>;
    c1?: ChartColumn<unknown>;
    c2?: ChartColumn<unknown>;
    c3?: ChartColumn<unknown>;
    c4?: ChartColumn<unknown>;
    c5?: ChartColumn<unknown>;
    c6?: ChartColumn<unknown>;
    c7?: ChartColumn<unknown>;
    c8?: ChartColumn<unknown>;
  },
  rows: ChartRow[]
}

export interface ChartRow {
  entity?: Lite<Entity>;
  c0?: unknown;
  c1?: unknown;
  c2?: unknown;
  c3?: unknown;
  c4?: unknown;
  c5?: unknown;
  c6?: unknown;
  c7?: unknown;
  c8?: unknown;
}


export interface ChartColumn<V> {
  name: string;
  title: string;
  displayName: string;
  token?: QueryToken; //Null for QueryToken
  type: ChartColumnType;
  orderByIndex?: number | null;
  orderByType?: OrderType | null;

  getKey: (v: V | null) => string;
  getNiceName: (v: V | null) => string;
  getColor: (v: V | null) => string | null;

  getValue: (row: ChartRow) => V;
  getValueKey: (row: ChartRow) => string;
  getValueNiceName: (row: ChartRow) => string;
  getValueColor: (row: ChartRow) => string;
}

declare module '@framework/SearchControl/SearchControlLoaded' {

  export interface ShowBarExtensionOption {
    showChartButton?: boolean;
  }
}

