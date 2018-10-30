import * as React from 'react'
import * as moment from 'moment'
import * as numbro from 'numbro'
import * as QueryString from 'query-string'
import { ajaxGet } from '@framework/Services';
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Entity, Lite, liteKey, MList } from '@framework/Signum.Entities'
import { getQueryKey, getEnumInfo } from '@framework/Reflection'
import {
    FilterOption, OrderOption, OrderOptionParsed, QueryRequest, QueryToken, SubTokensOptions, ResultTable, OrderRequest
} from '@framework/FindOptions'
import * as AuthClient from '../Authorization/AuthClient'
import {
    UserChartEntity, ChartPermission, ChartColumnEmbedded, ChartParameterEmbedded, ChartRequest,
    IChartBase, GroupByChart, ChartColumnType, ChartParameterType, ChartScriptSymbol, D3ChartScript, GoogleMapsCharScript
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


    registerChartScrtiptComponent(D3ChartScript.Bars, () => import("./Scripts/Bars"));
}

interface ChartScriptModule {
    default: React.ComponentClass<any /* { data: ChartTable }*/>
}

const registeredChartScriptComponents: { [key: string]: () => Promise<ChartScriptModule> } = {};

export function registerChartScrtiptComponent(symbol: ChartScriptSymbol, module: () => Promise<ChartScriptModule>) {
    registeredChartScriptComponents[symbol.key] = module;
}

export function getRegisteredChartScriptComponent(symbol: ChartScriptSymbol) {

    var result = registeredChartScriptComponents[symbol.key];
    if (!result)
        throw new Error("No chartScriptComponent registered in ChartClient for " + symbol.key);

    return result;
}

export namespace ButtonBarChart {

    interface ButtonBarChartContext {
        chartRequestView: ChartRequestView;
        chartRequest: ChartRequest;
    }

    export const onButtonBarElements: ((ctx: ButtonBarChartContext) => React.ReactElement<any> | undefined)[] = [];

    export function getButtonBarElements(ctx: ButtonBarChartContext): React.ReactElement<any>[] {
        return onButtonBarElements.map(f => f(ctx)).filter(a => a != undefined).map(a => a!);
    }
}

export interface ChartScript {
    symbol: ChartScriptSymbol; 
    icon: { fileName: string; bytes: string };
    groupBy: GroupByChart;
    columns: ChartScriptColumn[];
    parameters: ChartScriptParameter[];
    columnStructure: string;
}

export interface ChartScriptColumn {
    displayName: string;
    isOptional: boolean;
    columnType: ChartColumnType;
    isGroupKey: boolean;
}

export interface ChartScriptParameter {
    name: string;
    columnIndex?: number;
    type: ChartParameterType;
    valueDefinition: NumberInterval | EnumValueList | null;
}

export interface NumberInterval {
    defaultValue: number;
    minValue: number | null;
    maxValue: number | null;
}

export interface EnumValueList extends Array<EnumValue> {

};

export interface EnumValue {
    name: string;
    typeFilter?: ChartColumnType;
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



export function isCompatibleWith(chartScript: ChartScript, chartBase: IChartBase): boolean {
    if (chartScript.groupBy == "Always" && !chartBase.groupResults)
        return false;

    if (chartScript.groupBy == "Never" && chartBase.groupResults)
        return false;

    return zipOrDefault(
        chartScript.columns,
        chartBase.columns.map(mle => mle.element), (s, c) => {

            if (s == undefined)
                return c!.token == undefined;

            if (c == undefined || c.token == undefined)
                return s.isOptional!;

            if (!isChartColumnType(c.token.token, s.columnType!))
                return false;

            if (c.token.token!.queryTokenType == "Aggregate")
                return !s.isGroupKey;
            else
                return s.isGroupKey || !chartBase.groupResults;
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
            "DateTime",
            "Enum"].contains(type);
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


    if (chart.parameters.map(a => a.element.name!).orderBy(n => n).join(" ") !=
        chartScript.parameters.map(a => a.name!).orderBy(n => n).join(" ")) {

        const byName = chart.parameters.map(a => a.element).toObject(a => a.name!);
        chart.parameters.clear();

        chartScript.parameters.forEach(sp => {
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

    if (chart.groupResults == undefined) {
        chart.groupResults = true;
    }

    if (chartScript.groupBy == "Always" && chart.groupResults == false) {
        chart.groupResults = true;
    }
    else if (chartScript.groupBy == "Never" && chart.groupResults == true) {
        chart.groupResults = false;
    }

    chart.columns.map(mle => mle.element).forEach((cc, i) => {
        if (cc.token && cc.token.token!.queryTokenType == "Aggregate") {

            const sc = chartScript.columns[i]
            if (chart.groupResults == false || sc && sc.isGroupKey) {
                const parentToken = cc.token.token!.parent;
                cc.token = parentToken == undefined ? undefined : QueryTokenEmbedded.New({
                    tokenString: parentToken && parentToken.fullKey,
                    token: parentToken
                });
                cc.modified = true;
            }
        }
    });

    if (chart.Type == ChartRequest.typeName) {
        const cr = chart as ChartRequest;

        const keys = chart.columns.filter((a, i) => a.element.token && chartScript.columns![i].isGroupKey).map(a => a.element.token!.tokenString);

        cr.orderOptions = cr.orderOptions!.filter(o => {
            if (chart.groupResults)
                return o.token!.queryTokenType == "Aggregate" || keys.contains(o.token!.fullKey);
            else
                return o.token!.queryTokenType != "Aggregate";
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
        case "String": return null;
        default: throw new Error("Unexpected parameter type");
    }

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
    token?: string;
    displayName?: string;
}

export interface ChartParameterOption {
    name: string;
    value: string;
}

export module Encoder {

    export function toChartOptions(cr: ChartRequest): ChartOptions {
        return {
            queryName: cr.queryKey,
            chartScript: cr.chartScript && cr.chartScript.key.after(".") || undefined,
            groupResults: cr.groupResults,
            filterOptions: toFilterOptions(cr.filterOptions),
            orderOptions: cr.orderOptions.map(oo => ({ token: oo.token!.fullKey, orderType: oo.orderType } as OrderOption)),
            columnOptions: cr.columns.map(co => ({ token: co.element.token && co.element.token.tokenString, displayName: co.element.displayName }) as ChartColumnOption),
            parameters: cr.parameters.map(p => ({ name: p.element.name, value: p.element.value }) as ChartParameterOption)
        };
    }

    export function chartPath(cr: ChartOptions | ChartRequest, userChart?: Lite<UserChartEntity>): string {

        var co = ChartRequest.isInstance(cr) ? toChartOptions(cr) : cr;

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
            columns.forEach((co, i) => query["column" + i] = (co.token || "") + (co.displayName ? ("~" + scapeTilde(co.displayName)) : ""));
    }

    export function encodeParameters(query: any, parameters: ChartParameterOption[] | undefined) {
        if (parameters)
            parameters.map((p, i) => query["param" + i] = scapeTilde(p.name!) + "~" + scapeTilde(p.value!));
    }
}

export module Decoder {

    export function parseChartRequest(queryName: string, query: any): Promise<ChartRequest> {

        return getChartScripts().then(scripts => {
            return Finder.getQueryDescription(queryName).then(qd => {

                const completer = new Finder.TokenCompleter(qd);

                const fos = Finder.Decoder.decodeFilters(query);
                fos.forEach(fo => completer.requestFilter(fo, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | SubTokensOptions.CanAggregate));

                const oos = Finder.Decoder.decodeOrders(query);
                oos.forEach(oo => completer.request(oo.token, SubTokensOptions.CanElement | SubTokensOptions.CanAggregate));

                const cols = Decoder.decodeColumns(query);
                cols.map(a => a.element.token).filter(te => te != undefined).forEach(te => completer.request(te!.tokenString!, SubTokensOptions.CanAggregate | SubTokensOptions.CanElement));

                return completer.finished().then(() => {

                    cols.filter(a => a.element.token != null).forEach(a => a.element.token!.token = completer.get(a.element.token!.tokenString));

                    const chartRequest = ChartRequest.New({
                        chartScript: query.script == undefined ? scripts.first("ChartScript").symbol :
                            scripts
                                .filter(cs => cs.symbol.key.after(".") == query.script)
                                .single(`ChartScript '${query.queryKey}'`)
                                .symbol,
                        queryKey: getQueryKey(queryName),
                        groupResults: query.groupResults == "true",
                        filterOptions: fos.map(fo => completer.toFilterOptionParsed(fo)),
                        orderOptions: oos.map(oo => ({ token: completer.get(oo.token), orderType: oo.orderType }) as OrderOptionParsed),
                        columns: cols,
                        parameters: Decoder.decodeParameters(query),
                    });

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
            const ts = (val.contains("~") ? val.before("~") : val).trim();

            return ({
                rowId: null,
                element: ChartColumnEmbedded.New({
                    token: !!ts ? QueryTokenEmbedded.New({
                        tokenString: ts,
                    }) : undefined,
                    displayName: unscapeTildes(val.tryAfter("~")),
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

    export function getRequest(request: ChartRequest): QueryRequest {

        return {
            queryKey: request.queryKey,
            groupResults: request.groupResults,
            filters: toFilterRequests(request.filterOptions),
            columns: request.columns.map(mle => mle.element).filter(cce => cce.token != null).map(co => ({ token: co.token!.token!.fullKey }) as ColumnRequest),
            orders: request.orderOptions.map(oo => ({ token: oo.token.fullKey, orderType: oo.orderType }) as OrderRequest),
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

    export function toChartResult(request: ChartRequest, rt: ResultTable, chartScript: ChartScript): ExecuteChartResult {

        var cols = request.columns.map((mle, i) => {
            const token = mle.element.token && mle.element.token.token;

            if (token == null)
                return null;

            const scriptCol = chartScript.columns[i];

            var value: (r: ChartRow) => undefined = function (r: ChartRow) { return (r as any)["c" + i]; };
            var key = getKey(token);
            var niceName = getNiceName(token);
            var color = getColor(token);
            

            return {
                name: "c" + i,
                displayName: scriptCol.displayName,
                title: (mle.element.displayName || token && token.niceName) + (token && token.unit ? ` (${token.unit})` : ""),
                token: token && token.fullKey,
                type: token && toChartColumnType(token),
                isGroupKey: !request.groupResults ? undefined : scriptCol.isGroupKey,
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

        if (!request.groupResults) {
            cols.insertAt(0, Object.assign(function (r: ChartRow) { return r.entity; }, {
                name: "entity",
                displayName: "Entity",
                title: "",
                token: "Lite",
                type: "entity",
                isGroupKey: true,
                getColor: v => v ? "#555" : null,
                getKey: v => v ? liteKey(v as Lite<Entity>) : undefined,
                getNiceName: v => v ? (v as Lite<Entity>).toStr : undefined,
            } as ChartColumn<unknown>) as ChartColumn<unknown>);
        }

        var params = request.parameters.toObject(a => a.element.name!, a => a.element.value)

        var rows = rt.rows.map(row => {
            var cr = request.columns.map((c, i) => {
                var tuple = chartColToTableCol[i];

                if (tuple == null)
                    return null;

                var val = row.columns[tuple.index];

                return { colName: "c" + i, cValue: val };
            }).filter(a => a != null).toObject(a => a!.colName, a => a!.cValue) as ChartRow;

            if (!request.groupResults) {
                cr.entity = row.entity;
            }

            return cr;
        });

        var chartTable: ChartTable = {
            columns: cols.filter(c => c != null).toObjectDistinct(c => c!.name) as any,
            parameters: params,
            rows: rows
        };

        return {
            resultTable: rt,
            chartTable: chartTable,
        };
    }

    export function executeChart(request: ChartRequest, chartScript: ChartScript, abortSignal?: AbortSignal): Promise<ExecuteChartResult> {

        const queryRequest = getRequest(request);

        return Finder.API.executeQuery(queryRequest, abortSignal)
            .then(rt => toChartResult(request, rt, chartScript));

    }
    export interface ExecuteChartResult {
        resultTable: ResultTable;
        chartTable: ChartTable<unknown, unknown, unknown, unknown, unknown, unknown, unknown, unknown>;
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

export interface ChartTable<V0 = unknown, V1 = unknown, V2 = unknown, V3 = unknown, V4 = unknown, V5 = unknown, V6 = unknown, V7 = unknown> {
    columns: {
        entity?: ChartColumn<Lite<Entity>>;
        c0?: ChartColumn<V0>;
        c1?: ChartColumn<V1>;
        c2?: ChartColumn<V2>;
        c3?: ChartColumn<V3>;
        c4?: ChartColumn<V4>;
        c5?: ChartColumn<V5>;
        c6?: ChartColumn<V6>;
        c7?: ChartColumn<V7>;
    },
    parameters: { [name: string]: string | null | undefined },
    rows: ChartRow<V0, V1, V2, V3, V4, V5, V6, V7>[]
}

export interface ChartRow<V0 = unknown, V1 = unknown, V2 = unknown, V3 = unknown, V4 = unknown, V5 = unknown, V6 = unknown, V7 = unknown> {
    entity?: Lite<Entity>;
    c0: V0;
    c1: V1;
    c2: V2;
    c3: V3;
    c4: V4;
    c5: V5;
    c6: V6;
    c7: V7;
}


export interface ChartColumn<V> {
    name: string;
    title: string;
    displayName: string;
    token: string;
    isGroupKey?: boolean;
    type: string;

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

