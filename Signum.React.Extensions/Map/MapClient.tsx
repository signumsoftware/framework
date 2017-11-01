   
import * as React from 'react'
import { Route, } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { Entity, Lite, liteKey, MList, toLite, is, EntityPack } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { PseudoType, QueryKey, getQueryKey, Type, EntityData, EntityKind } from '../../../Framework/Signum.React/Scripts/Reflection'
import { TypeContext } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { WidgetContext, onEmbeddedWidgets, EmbeddedWidgetPosition } from '../../../Framework/Signum.React/Scripts/Frames/Widgets'
import { FindOptions, FilterOption, FilterOperation, OrderOption, ColumnOption,
    FilterRequest, QueryRequest, Pagination, QueryTokenType, QueryToken, FilterType, SubTokensOptions, ResultTable, OrderRequest } from '../../../Framework/Signum.React/Scripts/FindOptions'
import * as AuthClient  from '../Authorization/AuthClient'
import { SchemaMapInfo, ClientColorProvider } from './Schema/SchemaMap'
import { OperationMapInfo } from './Operation/OperationMap'

import { } from './Signum.Entities.Map'
import { ImportRoute } from "../../../Framework/Signum.React/Scripts/AsyncImport";


export const getProviders: Array<(info: SchemaMapInfo) => Promise<ClientColorProvider[]>> = []; 

export function getAllProviders(info: SchemaMapInfo): Promise<ClientColorProvider[]>{
    return Promise.all(getProviders.map(func => func(info))).then(result => result.filter(ps => !!ps).flatMap(ps => ps).filter(p => !!p));
}

export function start(options: { routes: JSX.Element[], auth: boolean; cache: boolean; disconnected: boolean; isolation: boolean }) {

    options.routes.push(
        <ImportRoute path="~/map" exact onImportModule={() => import("./Schema/SchemaMapPage")} />,
        <ImportRoute path="~/map/:type" onImportModule={() => import("./Operation/OperationMapPage")} />
    );

    getProviders.push(smi => import("./Schema/ColorProviders/Default").then((c: any) => c.default(smi)));
    if (options.auth)
        getProviders.push(smi => import("./Schema/ColorProviders/Auth").then((c: any) => c.default(smi)));
    if (options.cache)
        getProviders.push(smi => import("./Schema/ColorProviders/Cache").then((c: any) => c.default(smi)));
    if (options.disconnected)
        getProviders.push(smi => import("./Schema/ColorProviders/Disconnected").then((c: any) => c.default(smi)));
    if (options.isolation)
        getProviders.push(smi => import("./Schema/ColorProviders/Isolation").then((c: any) => c.default(smi)));
}

export namespace API {
    export function types(): Promise<SchemaMapInfo> {
        return ajaxGet<SchemaMapInfo>({ url: "~/api/map/types" });
    }

    export function operations(typeName: string): Promise<OperationMapInfo> {
        return ajaxGet<OperationMapInfo>({ url: "~/api/map/operations/" + typeName });
    }
}
