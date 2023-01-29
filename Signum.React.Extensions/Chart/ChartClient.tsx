import * as React from 'react'
import { DateTime } from 'luxon'
import { ajaxGet } from '@framework/Services';
import * as Navigator from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import * as Finder from '@framework/Finder'
import { Entity, getToString, Lite, liteKey, MList, parseLite, toMList } from '@framework/Signum.Entities'
import { getQueryKey, getEnumInfo, QueryTokenString, getTypeInfos, tryGetTypeInfos, timeToString, toFormatWithFixes } from '@framework/Reflection'
import {
  FilterOption, OrderOption, OrderOptionParsed, QueryRequest, QueryToken, SubTokensOptions, ResultTable, OrderRequest, OrderType, FilterOptionParsed, hasAggregate, ColumnOption, withoutAggregate, FilterConditionOption, QueryDescription, FindOptions
} from '@framework/FindOptions'
import * as AuthClient from '../Authorization/AuthClient'
import {
  UserChartEntity, ChartPermission, ChartColumnEmbedded, ChartParameterEmbedded, ChartRequestModel,
  IChartBase, ChartColumnType, ChartParameterType, ChartScriptSymbol, D3ChartScript, GoogleMapsChartScript, HtmlChartScript, SvgMapsChartScript, SpecialParameterType
} from './Signum.Entities.Chart'
import { QueryTokenEmbedded } from '../UserAssets/Signum.Entities.UserAssets'
import ChartButton from './ChartButton'
import ChartRequestView, { ChartRequestViewHandle } from './Templates/ChartRequestView'
import * as UserChartClient from './UserChart/UserChartClient'
import * as ColorPaletteClient from './ColorPalette/ColorPaletteClient'
import { ImportRoute } from "@framework/AsyncImport";
import { ColumnRequest } from '@framework/FindOptions';
import { toLuxonFormat } from '@framework/Reflection';
import { toNumberFormat } from '@framework/Reflection';
import { toFilterRequests, toFilterOptions } from '@framework/Finder';
import { QueryString } from '@framework/QueryString';
import { MemoRepository } from './D3Scripts/Components/ReactChart';
import { DashboardFilter } from '../Dashboard/View/DashboardFilterController';
import { Dic, softCast } from '../../Signum.React/Scripts/Globals';
import { colorInterpolators, colorSchemes } from './ColorPalette/ColorUtils';
import { getColorInterpolation } from './D3Scripts/Components/ChartUtils';
import { UserQueryEntity } from '../UserQueries/Signum.Entities.UserQueries';
import * as UserAssetClient from '../UserAssets/UserAssetClient'

export function start(options: { routes: JSX.Element[], googleMapsApiKey?: string, svgMap?: boolean }) {
  
  options.routes.push(<ImportRoute path="~/chart/:queryName" onImportModule={() => import("./Templates/ChartRequestPage")} />);

  AppContext.clearSettingsActions.push(ButtonBarChart.clearOnButtonBarElements);
 
  Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
    if (!ctx.searchControl.props.showBarExtension ||
      !AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting) ||
      !(ctx.searchControl.props.showBarExtensionOption?.showChartButton ?? ctx.searchControl.props.largeToolbarButtons))
      return undefined;

    return { button: <ChartButton searchControl={ctx.searchControl} /> };
  });

  UserChartClient.start({ routes: options.routes });
  ColorPaletteClient.start({ routes: options.routes });

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
  registerChartScriptComponent(D3ChartScript.Treemap, () => import("./D3Scripts/TreeMap"));

  registerChartScriptComponent(HtmlChartScript.PivotTable, () => import("./HtmlScripts/PivotTable"));

  if (options.googleMapsApiKey) {
    window.__google_api_key = options.googleMapsApiKey;
    registerChartScriptComponent(GoogleMapsChartScript.Heatmap, () => import("./GoogleMapScripts/Heatmap"));
    registerChartScriptComponent(GoogleMapsChartScript.Markermap, () => import("./GoogleMapScripts/Markermap"));
  }

  if (options.svgMap) {
    registerChartScriptComponent(SvgMapsChartScript.SvgMap, () => import("./SvgMap/SvgMap"));
  }
}

export interface ChartScriptProps {
  data?: ChartTable;
  parameters: { [name: string]: string },
  loading: boolean;
  onDrillDown: (row: ChartRow, e: React.MouseEvent<any> | MouseEvent) => void;
  onReload: (() => void) | undefined;
  width: number;
  height: number;
  initialLoad: boolean;
  memo: MemoRepository;
  chartRequest: ChartRequestModel;
  dashboardFilter?: DashboardFilter;
}

interface ChartScriptModule {
  default: ((p: ChartScriptProps) => React.ReactNode);
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

export function getCustomDrilldownsFindOptions(queryKey: string, qd: QueryDescription, groupResults: boolean) {
  var fos: FilterConditionOption[] = [];

  if (groupResults)
    fos.push(...[
      { token: UserQueryEntity.token(e => e.query.key), value: queryKey },
      { token: UserQueryEntity.token(e => e.entity.appendFilters), value: true }
    ]);
  else
    fos.push({ token: UserQueryEntity.token(e => e.entityType?.entity?.cleanName), value: qd!.columns["Entity"].type.name });

  const result = {
    queryName: UserQueryEntity,
    filterOptions: fos.map(fo => { fo.frozen = true; return fo; }),
  } as FindOptions;

  return result;
}

export namespace ButtonBarChart {

  interface ButtonBarChartContext {
    chartRequestView: ChartRequestViewHandle;
    chartRequest: ChartRequestModel;
  }

  export const onButtonBarElements: ((ctx: ButtonBarChartContext) => React.ReactElement<any> | undefined)[] = [];

  export function getButtonBarElements(ctx: ButtonBarChartContext): React.ReactElement<any>[] {
    return onButtonBarElements.map(f => f(ctx)).filter(a => a != undefined).map(a => a!);
  }

  export function clearOnButtonBarElements() {
    ButtonBarChart.onButtonBarElements.clear();
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
  valueDefinition: NumberInterval | EnumValueList | StringValue | SpecialParameter | null;
}

export interface NumberInterval {
  defaultValue: number;
  minValue: number | null;
  maxValue: number | null;
}

export interface SpecialParameter {
  specialParameterType: SpecialParameterType; 
}

export interface EnumValueList extends Array<EnumValue> {

}

export interface EnumValue {
  name: string;
  typeFilter?: ChartColumnType;
}

export interface StringValue {
  defaultValue: string;
}


let chartScripts: Promise<ChartScript[]>;
export function getChartScripts(): Promise<ChartScript[]> {
  return chartScripts ?? (chartScripts = API.fetchScripts());
}

export function getChartScript(symbol: ChartScriptSymbol): Promise<ChartScript> {
  if (symbol.key == null)
    throw new Error("User has not access to ChartScriptSymbol");

  return getChartScripts().then(cs => cs.single(a => a.symbol.key == symbol.key));
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
      "DateOnly",
      "String",
      "Lite",
      "Enum"
    ].contains(type);

    case "Magnitude": return [
      "Integer",
      "Real",
      "RealGroupable"
    ].contains(type);

    case "Positionable": return [
      "Integer",
      "Real",
      "RealGroupable",
      "DateOnly",
      "DateTime",
      "Time"
    ].contains(type);
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
    case "DateTime": return token.isGroupable ? "DateOnly" : "DateTime";
    case "Time": return "Time";
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


  const byName = chart.parameters.map(a => a.element).toObject(a => a.name!);
  chart.parameters.clear();

  allChartScriptParameters.forEach(sp => {
    let cp = byName[sp.name!];

    if (cp == undefined) {
      cp = ChartParameterEmbedded.New();
      cp.name = sp.name;
      const column = sp.columnIndex == undefined ? undefined : chart.columns![sp.columnIndex].element;
      cp.value = defaultParameterValue(sp, column?.token && column.token.token);
    }
    else {
      const column = sp.columnIndex == undefined ? undefined : chart.columns![sp.columnIndex].element;
      if (!isValidParameterValue(cp.value, sp, column?.token && column.token.token)) {
        cp.value = defaultParameterValue(sp, column?.token && column.token.token);
      }
      cp.modified = true;
    }

    chart.parameters!.push({ rowId: null, element: cp });
  });

}

function isValidParameterValue(value: string | null | undefined, scriptParameter: ChartScriptParameter, relatedColumn: QueryToken | null | undefined) : boolean{

  switch (scriptParameter.type) {
    case "Enum": return (scriptParameter.valueDefinition as EnumValueList).filter(a => a.typeFilter == undefined || relatedColumn == undefined || isChartColumnType(relatedColumn, a.typeFilter)).some(a => a.name == value);
    case "Number": return !isNaN(parseFloat(value!));
    case "String": return true;
    case "Special": {
      const specialParameterType = (scriptParameter.valueDefinition as SpecialParameter).specialParameterType;
      switch (specialParameterType) {
        case "ColorCategory": return value != null && colorSchemes[value] != null;
        case "ColorInterpolate": return value != null && getColorInterpolation(value) != null;
        default: throw new Error("Unexpected parameter type " + specialParameterType);
      }

    }
    default: throw new Error("Unexpected parameter type");
  }

}

export function defaultParameterValue(scriptParameter: ChartScriptParameter, relatedColumn: QueryToken | null | undefined) {
  switch (scriptParameter.type) {
    case "Enum": return (scriptParameter.valueDefinition as EnumValueList).filter(a => a.typeFilter == undefined || relatedColumn == undefined || isChartColumnType(relatedColumn, a.typeFilter)).first().name;
    case "Number": return (scriptParameter.valueDefinition as NumberInterval).defaultValue?.toString();
    case "String": return (scriptParameter.valueDefinition as StringValue).defaultValue?.toString();
    case "Special": {
      const specialParameterType = (scriptParameter.valueDefinition as SpecialParameter).specialParameterType;
      switch (specialParameterType) {
        case "ColorCategory": return Dic.getKeys(colorSchemes)[0];
        case "ColorInterpolate": return Dic.getKeys(colorInterpolators)[0];
        default: throw new Error("Unexpected parameter type " + specialParameterType);
      }
    }
    default: throw new Error("Unexpected parameter type");
  }

}

export function cleanedChartRequest(request: ChartRequestModel): ChartRequestModel {
  const clone = { ...request };
  
  clone.filters = toFilterRequests(clone.filterOptions);
  delete (clone as any).filterOptions;

  return clone;
}


export interface ChartOptions {
  queryName: any,
  chartScript?: string,
  maxRows?: number | null,
  groupResults?: boolean,
  filterOptions?: (FilterOption | null | undefined)[];
  orderOptions?: (OrderOption | null | undefined)[];
  columnOptions?: (ChartColumnOption | null | undefined)[];
  parameters?: (ChartParameterOption | null | undefined)[];
  customDrilldowns?: MList<Lite<Entity>>;
}

export interface ChartColumnOption {
  token?: string | QueryTokenString<any>;
  displayName?: string;
  format?: string;
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
      a.element.modified = true;
    });

    col.orderByType = newOrder;
    col.orderByIndex = 1;
  } else {

    col.orderByType = newOrder;
    if (col.orderByIndex == null)
      col.orderByIndex = (cr.columns.max(a => a.element.orderByIndex) ?? 0) + 1;
  }
  
  col.modified = true;
}

export module Encoder {

  export function toChartOptions(cr: ChartRequestModel, cs: ChartScript | null): ChartOptions {

    var params = cs?.parameterGroups.flatMap(a => a.parameters).toObject(a => a.name);

    return {
      queryName: cr.queryKey,
      chartScript: cr.chartScript?.key.after(".") ?? undefined,
      maxRows: cr.maxRows,
      filterOptions: toFilterOptions(cr.filterOptions),
      columnOptions: cr.columns.map(co => ({
        token: co.element.token && co.element.token.tokenString,
        displayName: co.element.displayName,
        format: co.element.format,
        orderByIndex: co.element.orderByIndex,
        orderByType: co.element.orderByType,
      }) as ChartColumnOption),
      parameters: cr.parameters
        .filter(p => {
          if (params == null)
            return true;

          var scriptParam = params![p.element.name!];

          var c = scriptParam.columnIndex != null ? cr.columns[scriptParam.columnIndex].element : null;

          return p.element.value != defaultParameterValue(scriptParam, c?.token && c.token.token);
        })
        .map(p => ({ name: p.element.name, value: p.element.value }) as ChartParameterOption),
      customDrilldowns: cr.customDrilldowns,
    };
  }

  export function chartPathPromise(cr: ChartRequestModel, userChart?: Lite<UserChartEntity>): Promise<string> {
    var csPromise: Promise<null | ChartScript> = cr.chartScript == null ? Promise.resolve(null) : getChartScript(cr.chartScript);

    return csPromise.then(cs => chartPath(toChartOptions(cr, cs), userChart));
  }
  
  export function chartPath(co: ChartOptions, userChart?: Lite<UserChartEntity>): string {
    const query = {
      script: co.chartScript,
      maxRows:
        co.maxRows === null ? "null" : 
        co.maxRows === undefined || co.maxRows == Decoder.DefaultMaxRows ? undefined : co.maxRows,
      groupResults: co.groupResults,
      userChart: userChart && liteKey(userChart)
    };

    Finder.Encoder.encodeFilters(query, co.filterOptions?.notNull());
    Finder.Encoder.encodeOrders(query, co.orderOptions?.notNull());
    encodeParameters(query, co.parameters?.notNull());

    encodeColumn(query, co.columnOptions?.notNull());
    UserAssetClient.Encoder.encodeCustomDrilldowns(query, co.customDrilldowns);

    return AppContext.toAbsoluteUrl(`~/chart/${getQueryKey(co.queryName)}?` + QueryString.stringify(query));

  }

  const scapeTilde = Finder.Encoder.scapeTilde;

  export function encodeColumn(query: any, columns: ChartColumnOption[] | undefined) {
    if (columns)
      columns.forEach((co, i) => query["column" + i] =
        (co.orderByIndex != null ? (co.orderByIndex! + (co.orderByType == "Ascending" ? "A" : "D") + "~") : "") +
        (co.token ?? "") +
        (co.displayName || co.format ? ("~" + (co.displayName == null ? "" : scapeTilde(co.displayName))) : "") +
        (co.format ? "~" + scapeTilde(co.format) : ""));
  }

  export function encodeParameters(query: any, parameters: ChartParameterOption[] | undefined) {
    if (parameters)
      parameters.map((p, i) => query["param" + i] = scapeTilde(p.name!) + "~" + scapeTilde(p.value!));
  }
}

export module Decoder {

  export let DefaultMaxRows = 1000;

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
            maxRows: query.maxRows == "null" ? null : query.maxRows || Decoder.DefaultMaxRows,
            queryKey: getQueryKey(queryName),
            filterOptions: fos.map(fo => completer.toFilterOptionParsed(fo)),
            columns: cols,
            parameters: Decoder.decodeParameters(query),
            customDrilldowns: UserAssetClient.Decoder.decodeCustomDrilldowns(query),
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
    return valuesInOrder(query, "column").map(p => {

      var parts = p.value.split("~");

      let order: string | undefined;
      let token: string;
      let displayName: string | null;
      let format: string | null;

      if (parts.length >= 2 && /\d+[AD]/.test(parts[0]))
        [order, token, displayName, format] = parts;
      else
        [token, displayName, format] = parts;
 
      return ({
        rowId: null,
        element: ChartColumnEmbedded.New({
          token: Boolean(token) ? QueryTokenEmbedded.New({
            tokenString: token,
          }) : undefined,
          orderByType: order == null ? null : (order.charAt(order.length -1) == "A" ? "Ascending" : "Descending"),
          orderByIndex: order == null ? null : (parseInt(order.substr(0, order.length - 1))),
          format: unscapeTildes(format),
          displayName: unscapeTildes(displayName),
        })
      });
    });
  }

  export function decodeParameters(query: any): MList<ChartParameterEmbedded> {
    return valuesInOrder(query, "param").map(p => ({
      rowId: null,
      element: ChartParameterEmbedded.New({
        name: unscapeTildes(p.value.before("~")),
        value: unscapeTildes(p.value.after("~")),
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
      pagination: request.maxRows == null ? { mode: "All" } : { mode: "Firsts", elementsPerPage: request.maxRows }
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
      case "DateTime": return token.isGroupable ? "DateOnly" : "DateTime";
      case "Time": return "Time";
      default: return null;
    }
  }

  export function getKey(token: QueryToken): ((val: unknown) => string) {

    if (token.type.isLite)
      return v => String(v && liteKey(v as Lite<Entity>));

    return v => String(v);
  }

  export function getColor(token: QueryToken, palettes: { [type: string]: ColorPaletteClient.ColorPalette | null }): ((val: unknown) => string | null) {

    var tis = tryGetTypeInfos(token.type);

    if (tis[0] && tis[0].kind == "Enum") {
      var typeName = tis[0].name;
      return v => {
        if (v == null)
          return "#555";

        var cp = palettes[typeName];
        return cp && cp.getColor(v as string) || null;
      }
    }

    if (tis.some(a => a && a.kind == "Entity")) {
      return v => {
        if (v == null)
          return "#555";

        var lite = (v as Lite<Entity>);

        var cp = palettes[lite.EntityType];
        return cp && cp.getColor(lite.id!.toString()) || null;
      };
    }

    return v => v == null ? "#555" : null;
  }

  export function getNiceName(token: QueryToken, chartColumn: ChartColumnEmbedded): ((val: unknown, width?: number) => string) {

    if (token.type.isLite)
      return v => {
        var lite = v as Lite<Entity> | null;
        return String(getToString(lite) ?? "");
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
      return (v, width) => {
        var date = v as string | null;
        if (date == null)
          return String(null);

        var luxonFormat = toLuxonFormat(chartColumn.format || token.format, token.type.name as "DateOnly" | "DateTime");
        var result = toFormatWithFixes(DateTime.fromISO(date), luxonFormat);
        if (luxonFormat == "D" && width != null && width < 80) {
          var year = DateTime.fromISO(date).toFormat("yyyy");
          return result.replace(year, "").replace(/^[\/\-.]/, "").replace(/[\/\-.]$/, "");
        }
        return result;
      };

    if (token.filterType == "Time")
      return v => {
        var date = v as string | null;
        var format = chartColumn.format || token.format;
        return date == null ? String(null) : timeToString(date, format);
      };

    if ((token.filterType == "Decimal" || token.filterType == "Integer"))
      return v => {
        var number = v as number | null;
        var format = chartColumn.format || (token.key == "Sum" ? "0.#K" : undefined) || token.format || "0";
        var numFormat = toNumberFormat(format);
        return number == null ? String(null) : numFormat.format(number);
      };

    return v => String(v);
  }

  export function getParameterWithDefault(request: ChartRequestModel, chartScript: ChartScript): { [parameter: string]: string } {

    var defaultValues = chartScript.parameterGroups.flatMap(g => g.parameters).toObject(a => a.name, a => {
      var col = a.columnIndex == null ? null : request.columns[a.columnIndex];
      return defaultParameterValue(a, col?.element && col.element.token && col.element.token.token);
    });

    return request.parameters.toObject(a => a.element.name!, a => a.element.value ?? defaultValues[a.element.name!])
  }

  export function toChartResult(request: ChartRequestModel, rt: ResultTable, chartScript: ChartScript, palettes: { [type: string]: ColorPaletteClient.ColorPalette | null }): ExecuteChartResult {

    var cols = request.columns.map((mle, i) => {
      const token = mle.element.token && mle.element.token.token;

      if (token == null)
        return null;

      const scriptCol = chartScript.columns[i];

      const value: (r: ChartRow) => undefined = function (r: ChartRow) { return (r as any)["c" + i]; };
      const key = getKey(token);

      const niceName = getNiceName(token, mle.element /*capture format by ref*/);
      const color = getColor(token, palettes);

      return softCast<ChartColumn<unknown>>({
        name: "c" + i,
        displayName: scriptCol.displayName,
        title: (mle.element.displayName || token?.niceName) + (token?.unit ? ` (${token.unit})` : ""),
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
      })
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
      const niceName = (v: Lite<Entity> | undefined) => getToString(v);
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
      date: DateTime.local().toISO(),
      columns: cols.filter(c => c != null).toObjectDistinct(c => c!.name) as any,
      rows: rows
    };

    return {
      resultTable: rt,
      chartTable: chartTable,
    };
  }


  export function executeChart(request: ChartRequestModel, chartScript: ChartScript, abortSignal?: AbortSignal): Promise<ExecuteChartResult> {

    var palettesPromise = getPalletes(request);

    const queryRequest = getRequest(request);
    return Finder.API.executeQuery(queryRequest, abortSignal)
      .then(rt => palettesPromise.then(palettes => toChartResult(request, rt, chartScript, palettes)));
  }

  export function getPalletes(request: ChartRequestModel): Promise<{ [type: string]: ColorPaletteClient.ColorPalette | null }> {
    var allTypes = request.columns
      .map(c => c.element.token)
      .notNull()
      .map(a => a.token && a.token.type.name)
      .notNull()
      .flatMap(a => tryGetTypeInfos(a))
      .notNull()
      .distinctBy(a => a.name);

    var palettesPromise = Promise.all(allTypes.map(ti => ColorPaletteClient.getColorPalette(ti).then(cp => ({ type: ti.name, palette: cp }))))
      .then(list => list.toObject(a => a.type, a => a.palette));

    return palettesPromise;
  }

  export interface ExecuteChartResult {
    resultTable: ResultTable;
    chartTable: ChartTable;
  }

  export function fetchScripts(): Promise<ChartScript[]> {
    return ajaxGet({
      url: "~/api/chart/scripts"
    });
  }
}

export interface ChartTable {
  date: string;
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
    c9?: ChartColumn<unknown>;
    c10?: ChartColumn<unknown>;
    c11?: ChartColumn<unknown>;
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
  c9?: unknown;
  c10?: unknown;
  c11?: unknown;
}


export interface ChartColumn<V> {
  name: string;
  title: string;
  displayName: string;
  token?: QueryToken; //Null for QueryToken
  type: ChartColumnType | null;
  orderByIndex?: number | null;
  orderByType?: OrderType | null;

  getKey: (v: V | null) => string;
  getNiceName: (v: V | null, width?: number) => string;
  getColor: (v: V | null) => string | null;

  getValue: (row: ChartRow) => V;
  getValueKey: (row: ChartRow) => string;
  getValueNiceName: (row: ChartRow, width?: number) => string;
  getValueColor: (row: ChartRow) => string | null;
}

declare module '@framework/SearchControl/SearchControlLoaded' {

  export interface ShowBarExtensionOption {
    showChartButton?: boolean;
  }
}

