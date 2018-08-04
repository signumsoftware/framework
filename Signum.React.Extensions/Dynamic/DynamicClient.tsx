
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet, WebApiHttpError } from '@framework/Services';
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { EntityData, EntityKind, PseudoType } from '@framework/Reflection'
import { EntityOperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import { Entity, Lite } from '@framework/Signum.Entities'
import * as Constructor from '@framework/Constructor'
import { StyleContext } from '@framework/TypeContext'

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '@framework/Lines'
import { DynamicTypeEntity, DynamicTypeOperation, DynamicPanelPermission, DynamicSqlMigrationEntity } from './Signum.Entities.Dynamic'
import * as AuthClient from '../Authorization/AuthClient'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import { ImportRoute } from "@framework/AsyncImport";
import { QueryRequest, QueryEntitiesRequest } from '@framework/FindOptions';

export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<ImportRoute path="~/dynamic/panel" onImportModule={() => import("./DynamicPanelPage")} />);


    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel),
        key: "DynamicPanel",
        onClick: () => Promise.resolve("~/dynamic/panel")
    });
}

export interface CompilationError {
    fileName: string;
    line: number;
    column: number;
    errorNumber: string;
    errorText: string;
    fileContent: string;
}

export namespace API {
    export function getCompilationErrors(): Promise<CompilationError[]> {
        return ajaxPost<CompilationError[]>({ url: `~/api/dynamic/compile` }, null);
    }

    export function restartServer(): Promise<void> {
        return ajaxPost<void>({ url: `~/api/dynamic/restartServer` }, null);
    }

    export function getStartErrors(): Promise<WebApiHttpError[]> {
        return ajaxGet<WebApiHttpError[]>({ url: `~/api/dynamic/startErrors` });
    }

    export function getEvalErrors(request: QueryEntitiesRequest): Promise<EvalEntityError[]> {
        return ajaxPost<EvalEntityError[]>({ url: `~/api/dynamic/evalErrors` }, request);
    }
}

export interface EvalEntityError {
    lite: Lite<Entity>;
    error: string;
}



