import * as React from 'react'
import { Route } from 'react-router'
import { Dic } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { Entity, Lite, liteKey, MList } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { PseudoType, QueryKey, getQueryKey } from '../../../Framework/Signum.React/Scripts/Reflection'
import { FindOptions, FilterOption, FilterOperation, OrderOption, ColumnOption,
    FilterRequest, QueryRequest, Pagination, QueryTokenType, QueryToken, FilterType, SubTokensOptions, ResultTable, OrderRequest } from '../../../Framework/Signum.React/Scripts/FindOptions'
import * as AuthClient  from '../../../Extensions/Signum.React.Extensions/Authorization/AuthClient'
import { QueryFilterEntity, QueryColumnEntity, QueryOrderEntity } from '../UserQueries/Signum.Entities.UserQueries'

import { UserChartEntity, ChartPermission, ChartMessage, ChartColumnEntity, ChartParameterEntity, ChartScriptEntity, ChartRequest, GroupByChart, ChartColumnType, IChartBase } from './Signum.Entities.Chart'
import { QueryTokenEntity } from '../UserAssets/Signum.Entities.UserAssets'
import ChartButton from './ChartButton'
import ChartRequestView from './Templates/ChartRequestView'
import * as UserChartClient from './UserChart/UserChartClient'


export function start(options: { routes: JSX.Element[] }) {

    options.routes.push(<Route path="chart">
        <Route path=":queryName" getComponent={ (loc, cb) => require(["./Templates/ChartRequestView"], (Comp) => cb(null, Comp.default)) } />
    </Route>);

    Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
        if (!AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting))
            return null;

        return <ChartButton searchControl={ctx.searchControl}/>;
    });

    UserChartClient.start({ routes: options.routes });
}


export namespace ButtonBarChart {

    interface ButtonBarChartContext {
        chartRequestView: ChartRequestView;
        chartRequest: ChartRequest;
    }

    export var onButtonBarElements: ((ctx: ButtonBarChartContext) => React.ReactElement<any>)[] = [];

    export function getButtonBarElements(ctx: ButtonBarChartContext): React.ReactElement<any>[] {
        return onButtonBarElements.map(f => f(ctx)).filter(a => a != null);
    }
}

export var chartScripts: ChartScriptEntity[][];
export function getChartScripts(): Promise<ChartScriptEntity[][]> {
    if (chartScripts)
        return Promise.resolve(chartScripts);

    return API.fetchScripts().then(cs => chartScripts = cs);
}

export var colorPalettes: string[];
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
        chartScript.columns.map(mle => mle.element),
        chartBase.columns.map(mle => mle.element), (s, c) => {

            if (s == null)
                return c.token == null;

            if (c == null || c.token == null)
                return s.isOptional;

            if (!isChartColumnType(c.token.token, s.columnType))
                return false;

            if (c.token.token.queryTokenType == QueryTokenType.Aggregate)
                return !s.isGroupKey;
            else
                return s.isGroupKey || !chartBase.groupResults;
        }).every(b => b);
}

export function zipOrDefault<T, S, R>(arrayT: T[], arrayS: S[], selector: (t: T, s: S) => R): R[] {
    var max = Math.max(arrayT.length, arrayS.length);

    var result: R[] = [];
    for (var i = 0; i < max; i++) {
        result.push(selector(
            i < arrayT.length ? arrayT[i] : null,
            i < arrayS.length ? arrayS[i] : null));
    }

    return result;
}

export function isChartColumnType(token: QueryToken, ct: ChartColumnType): boolean {
    if (token == null)
        return false;

    var type = getChartColumnType(token);

    if (type == null)
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

export function getChartColumnType(token: QueryToken): ChartColumnType {

    switch (token.filterType) {
        case "Lite": return "Lite";
        case "Boolean":
        case "Enum": return "Enum";
        case "String":
        case "Guid": return "String";
        case "Integer": return "Integer";
        case "Decimal": return token.isGroupable ? "RealGroupable": "Real";
        case "DateTime": return token.isGroupable ? "Date" : "DateTime";
    }

    return null;
}

export function removeAggregates(chart: IChartBase) {
    chart.columns.map(mle => mle.element).forEach(cc => {
        if (cc.token && cc.token.token.queryTokenType == QueryTokenType.Aggregate) {
            var parentToken = cc.token.token.parent;
            cc.token = QueryTokenEntity.New(t => {
                t.tokenString = parentToken && parentToken.fullKey;
                t.token = parentToken;
            });
        }
    });
}

export module Encoder {

    export function chartRequestPath(cr: ChartRequest): string {
        const query = {
            script: cr.chartScript.name,
        };

        Finder.Encoder.encodeFilters(query, cr.filterOptions);
        Finder.Encoder.encodeOrders(query, cr.orderOptions);
        encodeParameters(query, cr.parameters);

        encodeColumn(query, cr.columns);

        return Navigator.currentHistory.createPath({ pathname: "/Chart/" + cr.queryKey, query: query });
    }

    var scapeTilde = Finder.Encoder.scapeTilde;

    export function encodeColumn(query: any, columns: MList<ChartColumnEntity>) {
        if (columns)
            columns.forEach((co, i) => query["column" + i] = co.element.token.tokenString + (co.element.displayName ? ("~" + scapeTilde(co.element.displayName)) : ""));
    }
    export function encodeParameters(query: any, parameters: MList<ChartParameterEntity>) {
        if (parameters)
            parameters.map((p, i) => query["param" + i] = scapeTilde(p.element.name) + "~" + scapeTilde(p.element.value));
    }
}

export function parseTokens(chartRequest: ChartRequest): Promise<ChartRequest> {

    const completer = new Finder.TokenCompleter(chartRequest.queryKey);

    var promises: Promise<void>[] = [];

    if (chartRequest.filterOptions)
        promises.push(...chartRequest.filterOptions.map(fo => completer.complete(fo, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | SubTokensOptions.CanAggregate)));

    if (chartRequest.orderOptions)
        promises.push(...chartRequest.orderOptions.map(oo => completer.complete(oo, SubTokensOptions.CanElement | SubTokensOptions.CanAggregate)));

    if (chartRequest.columns)
        promises.push(...chartRequest.columns.map(a => a.element.token).map(tok => {
            if (tok.token && tok.token.fullKey == tok.tokenString)
                return Promise.resolve(null);

            return completer.request(tok.tokenString, SubTokensOptions.CanAggregate | SubTokensOptions.CanElement).then(t => {
                tok.token = t;
            });
        }));

    completer.finished();

    return Promise.all(promises)
        .then(() => Finder.parseFilterValues(chartRequest.filterOptions))
        .then(() => chartRequest);
}

export module Decoder {

    export function parseChartRequest(queryName: string, query: any): Promise<ChartRequest> {

        const chartRequest = ChartRequest.New(cr => {
            cr.queryKey = getQueryKey(queryName),
            cr.filterOptions = Finder.Decoder.decodeFilters(query);
            cr.orderOptions = Finder.Decoder.decodeOrders(query);
            cr.columns = Decoder.decodeColumns(query);
            cr.parameters = Decoder.decodeParameters(query);
        });

        return getChartScripts().then(scripts => { 

            chartRequest.chartScript = scripts.flatMap(a => a).filter(cs => cs.name == query.script).single(`ChartScript '${query.queryKey}'`);

            return parseTokens(chartRequest);
        });
    }


   


    var unscapeTildes = Finder.Decoder.unscapeTildes;
    var valuesInOrder = Finder.Decoder.valuesInOrder;

    export function decodeColumns(query: any): MList<ChartColumnEntity> {
        return valuesInOrder(query, "column").map(val => ({
            rowId: null,
            element: ChartColumnEntity.New(cc=> {
                cc.token = QueryTokenEntity.New(qte=> {
                    qte.tokenString = val.tryBefore("~") || val;
                });
                cc.displayName = unscapeTildes(val.tryAfter("~"));
            })
        }));
    }

    export function decodeParameters(query: any): MList<ChartParameterEntity> {
        return valuesInOrder(query, "param").map(val => ({
            rowId: null,
            element: ChartParameterEntity.New(cp => {
                cp.name = unscapeTildes(val.before("~"));
                cp.value = unscapeTildes(val.after("~"));
            })
        }));
    }
}


export module API {

    export interface ChartValue {
        key: string,
        toStr: string,
        color: string
    }

    export interface ChartTable {
        columns: {
            [name: string]: {
                title?: string;
                displayName?: string;
                token?: string;
                isGroupKey?: boolean;
                type?: string;
            }
        },
        parameters: { [name: string]: string },
        rows: { [name: string]: ChartValue }[]
    }

    export interface ExecuteChartResult {
        resultTable: ResultTable;
        chartTable: ChartTable;
    }

    export function cleanedChartRequest(request: ChartRequest) {
        var clone = Dic.copy(request);

        clone.orders = clone.orderOptions.map(oo => ({ token: oo.token.fullKey, orderType: oo.orderType }) as OrderRequest);
        delete clone.orderOptions;

        clone.filters = clone.filterOptions.map(fo => ({ token: fo.token.fullKey, operation: fo.operation, value: fo.value }) as FilterRequest);
        delete clone.filterOptions;

        return clone;
    }

    export function executeChart(request: ChartRequest): Promise<ExecuteChartResult> {

        var clone = cleanedChartRequest(request);

        return ajaxPost<ExecuteChartResult>({
            url: "/api/chart/execute"
        }, clone);
    }

    export function fetchScripts(): Promise<ChartScriptEntity[][]> {
        return ajaxGet<ChartScriptEntity[][]>({
            url: "/api/chart/scripts"
        });
    }

    export function fetchColorPalettes(): Promise<string[]> {
        return ajaxGet<string[]>({
            url: "/api/chart/colorPalettes"
        });
    }

    export function setChartScript(chart: IChartBase, script: ChartScriptEntity): Promise<void> {


        var clone = Dic.copy(chart);

        delete (clone as UserChartEntity).orders;
        delete (clone as ChartRequest).orders;
        delete (clone as ChartRequest).orderOptions;

        delete (clone as UserChartEntity).filters;
        delete (clone as ChartRequest).filters;
        delete (clone as ChartRequest).filterOptions;

        return ajaxPost<IChartBase>({
            url: "/api/chart/setChartScript"
        }, { chart: clone, script }).then(newChart => {

            if (script.groupBy == "Always")
                chart.groupResults = true;

            if (script.groupBy == "Never") {
                chart.groupResults = false;
                removeAggregates(chart);
            }

            chart.chartScript = newChart.chartScript;
            chart.parameters = newChart.parameters;
            chart.columns = newChart.columns;
        });
    }
}
