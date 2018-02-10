import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '../../../Framework/Signum.React/Scripts/Services';
import { getQueryNiceName } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { QueryEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName } from '../../../Framework/Signum.React/Scripts/Reflection'
import { ToolbarEntity, ToolbarMenuEntity, ToolbarElementEmbedded, ToolbarElementType } from './Signum.Entities.Toolbar'
import { ToolbarConfig, ToolbarResponse } from './ToolbarClient'

export default class QueryToolbarConfig extends ToolbarConfig<QueryEntity> {

    constructor() {
        super(QueryEntity);
    }

    getLabel(res: ToolbarResponse<QueryEntity>) {
        return res.label || getQueryNiceName(res.content!.toStr!);
    }

    handleNavigateClick(e: React.MouseEvent<any>, res: ToolbarResponse<any>) {
        if (!res.openInPopup)
            super.handleNavigateClick(e, res);
        else {
            Finder.explore({ queryName: res.content!.toStr! }).done()
        }
    }

    navigateTo(res: ToolbarResponse<QueryEntity>): Promise<string> {
        return Promise.resolve(Finder.findOptionsPath({ queryName: res.content!.toStr! }));
    }
}
