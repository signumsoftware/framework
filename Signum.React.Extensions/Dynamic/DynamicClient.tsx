
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet, WebApiHttpError } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityData, EntityKind } from '../../../Framework/Signum.React/Scripts/Reflection'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import { StyleContext } from '../../../Framework/Signum.React/Scripts/TypeContext'

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { DynamicTypeEntity, DynamicTypeOperation, DynamicPanelPermission, DynamicSqlMigrationEntity } from './Signum.Entities.Dynamic'
import * as AuthClient from '../Authorization/AuthClient'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import { ImportRoute } from "../../../Framework/Signum.React/Scripts/AsyncImport";

export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<ImportRoute path="~/dynamic/panel" onImportModule={() => import("./DynamicPanelPage")} />);


    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel),
        key: "DynamicPanel",
        onClick: () => Promise.resolve("~/dynamic/panel")
    });
}

export namespace Options {
    //export let onGetDynamicLineForPanel: ({ line: (ctx: StyleContext) => React.ReactNode, needsCompiling: boolean })[] = [];
    export let onGetDynamicLineForPanel: ((ctx: StyleContext) => React.ReactNode)[] = [];
    export let onGetDynamicLineForType: ((ctx: StyleContext, type: string) => React.ReactNode)[] = [];
    export let getDynaicMigrationsStep: (() => React.ReactElement<any>) | undefined = undefined;
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
    

}


