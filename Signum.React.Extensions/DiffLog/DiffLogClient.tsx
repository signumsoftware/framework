import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { Button, Tooltip, DropdownItem } from "reactstrap"
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

export function start(options: { routes: JSX.Element[] }) {

    Navigator.addSettings(new EntitySettings(OperationLogEntity, e => import('./Templates/OperationLog')))    
}

export namespace API {

    export function diffLog(id: string | number): Promise<DiffLogResult> {
        return ajaxGet<DiffLogResult>({ url: "~/api/diffLog/" + id });
    }
}

export interface DiffLogResult {
    prev: Lite<OperationLogEntity>;
    diffPrev: Array<DiffPair<Array<DiffPair<string>>>>;
    diff: Array<DiffPair<Array<DiffPair<string>>>>;
    diffNext: Array<DiffPair<Array<DiffPair<string>>>>;
    next: Lite<OperationLogEntity>;
}

export interface DiffPair<T> {
    Action: "Equal" | "Added" | "Removed";
    Value: T ;
}


