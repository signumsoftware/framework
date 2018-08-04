import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '@framework/Globals';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '@framework/Services';
import { getQueryNiceName } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { QueryEntity } from '@framework/Signum.Entities.Basics'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '@framework/Signum.Entities'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName } from '@framework/Reflection'
import { ToolbarEntity, ToolbarMenuEntity, ToolbarElementEmbedded, ToolbarElementType } from './Signum.Entities.Toolbar'
import { ToolbarConfig, ToolbarResponse } from './ToolbarClient'

export default class QueryToolbarConfig extends ToolbarConfig<QueryEntity> {

    constructor() {
        var type = QueryEntity;
        super(type);
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
