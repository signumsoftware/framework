import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { Button, OverlayTrigger, Tooltip, MenuItem } from "react-bootstrap"
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName  } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as ContextualOperations from '../../../Framework/Signum.React/Scripts/Operations/ContextualOperations'
import { ToolbarEntity, ToolbarMenuEntity, ToolbarElementEntity, ToolbarElementType } from './Signum.Entities.Toolbar'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'

export function start(options: { routes: JSX.Element[] }) {
    Navigator.addSettings(new EntitySettings(ToolbarEntity, t => new ViewPromise(resolve => require(['./Templates/Toolbar'], resolve))));
    Navigator.addSettings(new EntitySettings(ToolbarMenuEntity, t => new ViewPromise(resolve => require(['./Templates/ToolbarMenu'], resolve))));
    Navigator.addSettings(new EntitySettings(ToolbarElementEntity, t => new ViewPromise(resolve => require(['./Templates/ToolbarElement'], resolve))));   

    Constructor.registerConstructor(ToolbarElementEntity, tn => ToolbarElementEntity.New(e => e.type = "Link"));
}

export namespace API {
    export function getCurrentToolbar(): Promise<ToolbarResponse> {
        return ajaxGet<ToolbarResponse>({ url: `~/api/toolbar/current` });
    }
}

export interface ToolbarResponse {
    type: ToolbarElementType;
    label?: string;
    lite?: Lite<Entity>;
    elements?: Array<ToolbarResponse>;
}