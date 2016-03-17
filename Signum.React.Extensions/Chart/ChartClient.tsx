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
import { QueryFilterEntity, QueryFilterEntity_Type, QueryColumnEntity, QueryColumnEntity_Type, QueryOrderEntity, QueryOrderEntity_Type } from '../UserQueries/Signum.Entities.UserQueries'

import { UserChartEntity, UserChartEntity_Type, ChartPermission, ChartMessage, ChartColumnEntity, ChartColorEntity_Type, ChartParameterEntity, ChartParameterEntity_Type,
    ChartColumnEntity_Type, ChartScriptEntity, ChartRequest, GroupByChart, ChartColumnType, ChartRequest_Type, IChartBase } from './Signum.Entities.Chart'
import { QueryTokenEntity, QueryTokenEntity_Type } from '../UserAssets/Signum.Entities.UserAssets'
import ChartButton from './ChartButton'
import ChartRequestView from './Templates/ChartRequestView'




export function start(options: { routes: JSX.Element[] }) {

    options.routes.push(<Route path="chart">
        <Route path=":queryName" getComponent={ (loc, cb) => require(["./Templates/ChartRequestView"], (Comp) => cb(null, Comp.default)) } />
    </Route>);

    Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
        if (!AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting))
            return null;

        return <ChartButton searchControl={ctx.searchControl}/>;
    });

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
    if (chartScript.groupBy == GroupByChart.Always && !chartBase.groupResults)
        return false;

    if (chartScript.groupBy == GroupByChart.Never && chartBase.groupResults)
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

        case ChartColumnType.Groupable: return [
            ChartColumnType.RealGroupable,
            ChartColumnType.Integer,
            ChartColumnType.Date,
            ChartColumnType.String,
            ChartColumnType.Lite,
            ChartColumnType.Enum].contains(type);

        case ChartColumnType.Magnitude: return [
            ChartColumnType.Integer,
            ChartColumnType.Real,
            ChartColumnType.RealGroupable].contains(type);

        case ChartColumnType.Positionable: return [
            ChartColumnType.Integer,
            ChartColumnType.Real,
            ChartColumnType.RealGroupable,
            ChartColumnType.Date,
            ChartColumnType.DateTime,
            ChartColumnType.Enum].contains(type);
    }


    return false;
}

export function getChartColumnType(token: QueryToken): ChartColumnType {

    switch (token.filterType) {
        case FilterType.Lite: return ChartColumnType.Lite;
        case FilterType.Boolean:
        case FilterType.Enum: return ChartColumnType.Enum;
        case FilterType.String:
        case FilterType.Guid: return ChartColumnType.String;
        case FilterType.Integer: return ChartColumnType.Integer;
        case FilterType.Decimal: return token.isGroupable ? ChartColumnType.RealGroupable : ChartColumnType.Real;
        case FilterType.DateTime: return token.isGroupable ? ChartColumnType.Date : ChartColumnType.DateTime;
    }

    return null;
}

export function removeAggregates(chart: IChartBase) {
    chart.columns.map(mle => mle.element).forEach(cc => {
        if (cc.token && cc.token.token.queryTokenType == QueryTokenType.Aggregate) {
            var parentToken = cc.token.token.parent;
            cc.token = QueryTokenEntity_Type.New({ Type: null,  tokenString: parentToken.fullKey, token: parentToken });
        }
    });
}

export module Encoder {

    export function chartRequestPath(cr: ChartRequest): string {
        const query = {
            script: cr.chartScript.name,
            filters: Finder.Encoder.encodeFilters(cr.filterOptions),
            columns: Encoder.encodeColumn(cr.columns),
            orders: Finder.Encoder.encodeOrders(cr.ordersOptions),
            parameters: Encoder.encodeParameters(cr.parameters),
        };

        return Navigator.currentHistory.createPath({ pathname: "/Chart/" + cr.queryKey, query: query });
    }

    var scapeTilde = Finder.Encoder.scapeTilde;

    export function encodeColumn(columns: MList<ChartColumnEntity>): string[] {
        return !columns ? undefined : columns.map(co => co.element.token.tokenString + (co.element.displayName ? ("~" + scapeTilde(co.element.displayName)) : ""));
    }
    export function encodeParameters(parameters: MList<ChartParameterEntity>): string[] {
        return !parameters ? undefined : parameters.map(p => scapeTilde(p.element.name) + "~" + scapeTilde(p.element.value));
    }
}

export module Decoder {

    export function parseChartRequest(queryName: string, query: any): Promise<ChartRequest> {

        const chartRequest = ChartRequest_Type.New({
            Type: null,
            queryKey: getQueryKey(queryName),
            filterOptions: Finder.Decoder.decodeFilters(query.filters) || [],
            ordersOptions: Finder.Decoder.decodeOrders(query.orders) || [],
            columns: Decoder.decodeColumns(query.columns) || [],
            parameters: Decoder.decodeParameters(query.parameters) || [],
        });

        return getChartScripts().then(scripts => {

            chartRequest.chartScript = scripts.flatMap(a => a).filter(cs => cs.name == query.script).single(`ChartScript '${query.queryKey}'`);

            const completer = new Finder.TokenCompleter(queryName);

            var promises: Promise<void>[] = [];

            if (chartRequest.filterOptions)
                promises.push(...chartRequest.filterOptions.map(fo => completer.complete(fo, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | SubTokensOptions.CanAggregate)));

            if (chartRequest.ordersOptions)
                promises.push(...chartRequest.ordersOptions.map(oo => completer.complete(oo, SubTokensOptions.CanElement | SubTokensOptions.CanAggregate)));

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
                .then(() => Finder.parseFilterValues(chartRequest.filterOptions).then(() => chartRequest));
        });
    }


    var unscapeTildes = Finder.Decoder.unscapeTildes;
    var asArray = Finder.Decoder.asArray;

    export function decodeColumns(columns: string | string[]): MList<ChartColumnEntity> {

        if (!columns)
            return undefined;

        return asArray(columns).map(val => ({
            rowId: null,
            element: ChartColumnEntity_Type.New({
                Type: null,
                token: QueryTokenEntity_Type.New({
                    Type: null,
                    tokenString: val.tryBefore("~") || val
                }),
                displayName: unscapeTildes(val.tryAfter("~"))
            })
        }));
    }

    export function decodeParameters(columns: string | string[]): MList<ChartParameterEntity> {

        if (!columns)
            return undefined;

        return asArray(columns).map(val => ({
            rowId: null,
            element: ChartParameterEntity_Type.New({
                Type: null,
                name: unscapeTildes(val.before("~")),
                value: unscapeTildes(val.after("~"))
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

    export function executeChart(request: ChartRequest): Promise<ExecuteChartResult> {

        var clone = Dic.copy(request);

        clone.orders = clone.ordersOptions.map(oo => ({ token: oo.token.fullKey, orderType: oo.orderType }) as OrderRequest);
        delete clone.ordersOptions;

        clone.filters = clone.filterOptions.map(fo => ({ token: fo.token.fullKey, operation: fo.operation, value: fo.value }) as FilterRequest);
        delete clone.filterOptions;

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
        delete (clone as ChartRequest).ordersOptions;

        delete (clone as UserChartEntity).filters;
        delete (clone as ChartRequest).filters;
        delete (clone as ChartRequest).filterOptions;

        return ajaxPost<IChartBase>({
            url: "/api/chart/setChartScript"
        }, { chart: clone, script }).then(newChart => {

            if (script.groupBy == GroupByChart.Always)
                chart.groupResults = true;

            if (script.groupBy == GroupByChart.Never) {
                chart.groupResults = false;
                removeAggregates(chart);
            }

            chart.chartScript = newChart.chartScript;
            chart.parameters = newChart.parameters;
            chart.columns = newChart.columns;
        });
    }
}
