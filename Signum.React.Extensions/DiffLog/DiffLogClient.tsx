import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { QueryRequest } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { OperationLogEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName  } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { TimeMachineMessage } from './Signum.Entities.DiffLog';
import { ImportRoute } from '../../../Framework/Signum.React/Scripts/AsyncImport';
import { getTypeInfo } from '../../../Framework/Signum.React/Scripts/Reflection';

export function start(options: { routes: JSX.Element[], timeMachine: boolean }) {

    Navigator.addSettings(new EntitySettings(OperationLogEntity, e => import('./Templates/OperationLog')));

    if (options.timeMachine) {

        QuickLinks.registerGlobalQuickLink(ctx => getTypeInfo(ctx.lite.EntityType).isSystemVersioned ? new QuickLinks.QuickLinkLink("TimeMachine", TimeMachineMessage.TimeMachine.niceToString(), "~/timeMachine/" + ctx.lite.EntityType + "/" + ctx.lite.id, {
            icon: "fa fa-history",
            iconColor: "blue"
        }) : undefined);

        options.routes.push(<ImportRoute path="~/timeMachine/:type/:id" onImportModule={() => import("./Templates/TimeMachinePage")} />);
    }
}

export namespace API {

    export function diffLog(id: string | number): Promise<DiffLogResult> {
        return ajaxGet<DiffLogResult>({ url: "~/api/diffLog/" + id });
    }

    export function retrieveVersion(lite: Lite<Entity>, asOf: string,): Promise<Entity> {
        return ajaxGet<Entity>({ url: `~/api/retrieveVersion/${lite.EntityType}/${lite.id}?asOf=${asOf}`});
    }

    export function diffVersions(lite: Lite<Entity>, from: string, to: string): Promise<DiffBlock> {
        return ajaxGet<DiffBlock>({ url: `~/api/diffVersions/${lite.EntityType}/${lite.id}?from=${from}&to=${to}` });
    }
}

export interface DiffLogResult {
    prev: Lite<OperationLogEntity>;
    diffPrev: DiffBlock;
    diff: DiffBlock;
    diffNext: DiffBlock;
    next: Lite<OperationLogEntity>;
}

export type DiffBlock = Array<DiffPair<Array<DiffPair<string>>>>;

export interface DiffPair<T> {
    Action: "Equal" | "Added" | "Removed";
    Value: T ;
}


