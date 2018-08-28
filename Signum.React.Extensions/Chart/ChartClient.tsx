import * as React from 'react'
import { Route } from 'react-router'
import * as moment from 'moment'
import * as numbro from 'numbro'
import * as QueryString from 'query-string'
import { Dic } from '@framework/Globals';
import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { EntityOperationSettings } from '@framework/Operations'
import { Entity, Lite, liteKey, MList } from '@framework/Signum.Entities'
import * as Constructor from '@framework/Constructor'
import * as Operations from '@framework/Operations'
import * as QuickLinks from '@framework/QuickLinks'
import { PseudoType, QueryKey, getQueryKey } from '@framework/Reflection'
import {
    FindOptions, FilterOption, FilterOptionParsed, FilterOperation, OrderOption, OrderOptionParsed, ColumnOption,
    FilterRequest, QueryRequest, Pagination, QueryTokenType, QueryToken, FilterType, SubTokensOptions, ResultTable, OrderRequest
} from '@framework/FindOptions'
import * as AuthClient from '../Authorization/AuthClient'
import { QueryFilterEmbedded, QueryColumnEmbedded, QueryOrderEmbedded } from '../UserQueries/Signum.Entities.UserQueries'

import {
    UserChartEntity, ChartPermission, ChartMessage, ChartColumnEmbedded, ChartParameterEmbedded, ChartScriptEntity, ChartScriptParameterEmbedded, ChartRequest,
    GroupByChart, ChartColumnType, IChartBase
} from './Signum.Entities.Chart'
import { QueryTokenEmbedded } from '../UserAssets/Signum.Entities.UserAssets'
import ChartButton from './ChartButton'
import ChartRequestView from './Templates/ChartRequestView'
import * as ChartUtils from './Templates/ChartUtils'
import * as UserChartClient from './UserChart/UserChartClient'
import { ImportRoute } from "@framework/AsyncImport";
import { ColumnRequest } from '@framework/FindOptions';
import { toMomentFormat } from '@framework/Reflection';
import { toNumbroFormat } from '@framework/Reflection';
import { toFilterRequests, toFilterOptions } from '@framework/Finder';

export function start(options: { routes: JSX.Element[] }) {

    options.routes.push(<ImportRoute path="~/chart/:queryName" onImportModule={() => import("./Templates/ChartRequestPage")} />);

    Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
        if (!ctx.searchControl.props.showBarExtension ||
            !AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting) ||
            (ctx.searchControl.props.showBarExtensionOption && ctx.searchControl.props.showBarExtensionOption.showChartButton == false))
            return undefined;

        return <ChartButton searchControl={ctx.searchControl} />;
    });

    Navigator.addSettings(new EntitySettings(ChartScriptEntity, e => import('./ChartScript/ChartScript')));

    UserChartClient.start({ routes: options.routes });
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

export let chartScripts: ChartScriptEntity[][];
export function getChartScripts(): Promise<ChartScriptEntity[][]> {
    if (chartScripts)
        return Promise.resolve(chartScripts);

    return API.fetchScripts().then(cs => chartScripts = cs);
}

export function getChartScript(name: string): Promise<ChartScriptEntity> {
    return getChartScripts().then(cs => cs.flatMap(arr => arr).single(a => a.name == name));
}

export let colorPalettes: string[];
export function getColorPalettes(): Promise<string[]> {
    if (colorPalettes)
        return Promise.resolve(colorPalettes);

    return API.fetchColorPalettes().then(cs => colorPalettes = cs);
}



export function isCompatibleWith(chartScript: ChartScriptEntity, chartBase: IChartBase): boolean {
    if (chartScript.groupBy == "Always" && !chartBase.groupResults)
        return false;

    if (chartScript.groupBy == "Never" && chartBase.groupResults)
        return false;

    return zipOrDefault(
        chartScript.columns!.map(mle => mle.element),
        chartBase.columns!.map(mle => mle.element), (s, c) => {

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




export function synchronizeColumns(chart: IChartBase) {

    const chartScript = chart.chartScript!;

    if (chart.columns == null ||
        chartScript.columns == null ||
        chartScript.parameters == null ||
        chart.parameters == null)
        throw Error("no Columns");

    if (chartScript == undefined) {
        chart.columns!.clear();
    }

    for (let i = 0; i < chartScript.columns!.length; i++) {
        if (chart.columns.length <= i) {
            chart.columns.push({ rowId: null, element: ChartColumnEmbedded.New() });
        }
    }

    if (chart.columns.length > chartScript.columns.length) {
        chart.columns.splice(chartScript.columns.length, chart.columns.length - chartScript.columns.length);
    }


    if (chart.parameters.map(a => a.element.name!).orderBy(n => n).join(" ") !=
        chartScript.parameters.map(a => a.element.name!).orderBy(n => n).join(" ")) {

        const byName = chart.parameters.map(a => a.element).toObject(a => a.name!);
        chart.parameters.clear();

        chartScript.parameters.forEach(sp => {
            let cp = byName[sp.element.name!];

            if (cp == undefined) {
                cp = ChartParameterEmbedded.New();
                cp.name = sp.element.name;
                const column = sp.element.columnIndex == undefined ? undefined : chart.columns![sp.element.columnIndex].element;
                cp.value = defaultParameterValue(sp.element, column && column.token && column.token.token);
            }
            else {
                const column = sp.element.columnIndex == undefined ? undefined : chart.columns![sp.element.columnIndex].element;
                if (!isValidParameterValue(cp.value, sp.element, column && column.token && column.token.token))
                    cp.value = defaultParameterValue(sp.element, column && column.token && column.token.token);
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

            const sc = chart.chartScript!.columns![i]
            if (chart.groupResults == false || sc && sc.element.isGroupKey) {
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

        const keys = chart.columns.filter((a, i) => a.element.token && chartScript.columns![i].element.isGroupKey).map(a => a.element.token!.tokenString);

        cr.orderOptions = cr.orderOptions!.filter(o => {
            if (chart.groupResults)
                return o.token!.queryTokenType == "Aggregate" || keys.contains(o.token!.fullKey);
            else
                return o.token!.queryTokenType != "Aggregate";
        });
    }
}

function isValidParameterValue(value: string | null | undefined, scrptParameter: ChartScriptParameterEmbedded, relatedColumn: QueryToken | null | undefined) {

    switch (scrptParameter.type) {
        case "Enum": return scrptParameter.enumValues.filter(a => a.typeFilter == undefined || relatedColumn == undefined || isChartColumnType(relatedColumn, a.typeFilter)).some(a => a.name == value);
        case "Number": return !isNaN(parseFloat(value!));
        case "String": return true;
        default: throw new Error("Unexpected parameter type");
    }

}

function defaultParameterValue(scriptParameter: ChartScriptParameterEmbedded, relatedColumn: QueryToken | null | undefined) {

    switch (scriptParameter.type) {
        case "Enum": return scriptParameter.enumValues.filter(a => a.typeFilter == undefined || relatedColumn == undefined || isChartColumnType(relatedColumn, a.typeFilter)).first().name;
        case "Number": return parseFloat(scriptParameter.valueDefinition!).toString();
        case "String": return scriptParameter.valueDefinition;
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
            chartScript: cr.chartScript && cr.chartScript.name || undefined,
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
                        chartScript: query.script == undefined ? scripts.first("ChartScript").first() :
                            scripts.flatMap(a => a).filter(cs => cs.name == query.script).single(`ChartScript '${query.queryKey}'`),
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

    export function getConverter(token: QueryToken | null | undefined): ((val: any) => ChartValue) | null {
        if (!token)
            return null;

        if (token.type.isLite)
            return v => {
                var lite = v as Lite<Entity> | undefined;
                return {
                    key: lite && liteKey(lite),
                    toStr: lite && lite.toStr || "",
                    color: lite == null ? "#555" : null,
                };
            };

        if (token.filterType == "Enum")
            return v => {
                var value = v as string | undefined;
                return {
                    key: value,
                    toStr: value,
                    color: value == null ? "#555" : null,
                };
            };

        if (token.filterType == "DateTime")
            return v => {
                var date = v as string | undefined;
                var format = token.format && toMomentFormat(token.format);
                return {
                    key: date,
                    keyForFilter: date && moment(date).format(),
                    toStr: date && moment(date).format(format),
                };
            };

        if (token.format && (token.filterType == "Decimal" || token.filterType == "Integer"))
            return v => {
                var number = v as number | undefined;
                var format = token.format && toNumbroFormat(token.format);
                return {
                    key: number,
                    toStr: number == null ? null : numbro(number).format(format),
                };
            };

        return v => {
            return {
                key: v,
                toStr: v,
            };
        };
    }

    export function toChartResult(request: ChartRequest, rt: ResultTable): ExecuteChartResult {

        var cols = request.columns.map((mle, i) => {
            const token = mle.element.token && mle.element.token.token;
            const scriptCol = request.chartScript.columns[i].element;
            return ({
                name: "c" + i,
                displayName: scriptCol.displayName,
                title: (mle.element.displayName || token && token.niceName) + (token && token.unit ? ` (${token.unit})` : ""),
                token: token && token.fullKey,
                type: token && toChartColumnType(token),
                isGroupKey: !request.groupResults ? undefined : scriptCol.isGroupKey,
            }) as ChartColumn;
        });

        var index = 0;
        var converters = request.columns.map(mle => {
            var conv = getConverter(mle.element.token && mle.element.token.token);

            if (conv == null)
                return null;

            return {
                conv,
                index: index++
            }
        });

        if (!request.groupResults) {
            cols.insertAt(0, {
                name: "entity",
                displayName: "Entity",
                title: "",
                token: "Lite",
                type: "entity",
                isGroupKey: true,
            });
        }

        var params = request.parameters.toObject(a => a.element.name!, a => a.element.value)

        var rows = rt.rows.map(row => {
            var cr = request.columns.map((c, i) => {
                var tuple = converters[i];

                if (tuple == null)
                    return null;

                var val = row.columns[tuple.index];

                return { colName: "c" + i, cValue: tuple.conv!(val) };
            }).filter(a => a != null).toObject(a => a!.colName, a => a!.cValue) as ChartRow;

            if (!request.groupResults) {
                cr["entity"] = {
                    key: liteKey(row.entity!),
                    toStr: row.entity!.toStr || "",
                    color: row.entity == null ? "#555" : null,
                };
            }

            return cr;
        });

        var chartTable: ChartTable = {
            columns: cols.toObjectDistinct(a => a.name),
            parameters: params,
            rows: rows
        };

        return {
            resultTable: rt,
            chartTable: chartTable,
        };
    }

    export function executeChart(request: ChartRequest, abortController?: FetchAbortController): Promise<ExecuteChartResult> {

        const queryRequest = getRequest(request);

        return Finder.API.executeQuery(queryRequest, abortController).then(rt => toChartResult(request, rt));

    }
    export interface ExecuteChartResult {
        resultTable: ResultTable;
        chartTable: ChartTable;
    }

    export function fetchScripts(): Promise<ChartScriptEntity[][]> {
        return ajaxGet<ChartScriptEntity[][]>({
            url: "~/api/chart/scripts"
        });
    }

    export function fetchColorPalettes(): Promise<string[]> {
        return ajaxGet<string[]>({
            url: "~/api/chart/colorPalettes"
        });
    }
}


export interface ChartValue {
    key: string | number | null | undefined,
    keyForFilter?: string | number | null,
    toStr: string | undefined | null,
    color?: string | null,
    niceToString?(): string;
}

export interface ChartTable {
    columns: { [name: string]: ChartColumn },
    parameters: { [name: string]: string | null | undefined },
    rows: ChartRow[]
}

export interface ChartRow {
    [name: string]: ChartValue
}


export interface ChartColumn {
    name: string;
    title?: string;
    displayName?: string;
    token?: string;
    isGroupKey?: boolean;
    type?: string;
}

declare module '@framework/SearchControl/SearchControlLoaded' {

    export interface ShowBarExtensionOption {
        showChartButton?: boolean;
    }
}

