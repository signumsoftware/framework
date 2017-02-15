import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { Button, OverlayTrigger, Tooltip, MenuItem, } from "react-bootstrap"
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '../../../Framework/Signum.React/Scripts/Services';
import { getQueryNiceName } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { QueryEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName } from '../../../Framework/Signum.React/Scripts/Reflection'
import { ToolbarEntity, ToolbarMenuEntity, ToolbarElementEntity, ToolbarElementType } from './Signum.Entities.Toolbar'
import { ToolbarConfig, ToolbarResponse } from './ToolbarClient'

export default class QueryToolbarConfig extends ToolbarConfig<QueryEntity> {

    constructor() {
        super();
        this.type = QueryEntity;
    }

    getLabel(element: ToolbarResponse<QueryEntity>) {
        return element.label || getQueryNiceName(element.lite!.toStr!);
    }

    navigateTo(element: ToolbarResponse<QueryEntity>): Promise<string> {
        return Promise.resolve(Finder.findOptionsPath({ queryName: element.lite!.toStr! }));
    }
}
