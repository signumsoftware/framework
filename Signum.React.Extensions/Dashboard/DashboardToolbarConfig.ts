import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '../../../Framework/Signum.React/Scripts/Services';
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { QueryEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { Lite, Entity, EntityPack, liteKey } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName } from '../../../Framework/Signum.React/Scripts/Reflection'
import { ToolbarEntity, ToolbarMenuEntity, ToolbarElementEmbedded, ToolbarElementType } from '../Toolbar/Signum.Entities.Toolbar'
import { ToolbarConfig, ToolbarResponse } from '../Toolbar/ToolbarClient'
import * as DashboardClient from './DashboardClient'
import { DashboardEntity } from './Signum.Entities.Dashboard'

export default class DashboardToolbarConfig extends ToolbarConfig<DashboardEntity> {

    constructor() {
        super(DashboardEntity);
    }

    getIcon(element: ToolbarResponse<DashboardEntity>) {
        return ToolbarConfig.coloredIcon(element.iconName || "glyphicon glyphicon-th-large", element.iconColor || "darkslateblue");
    }

    navigateTo(element: ToolbarResponse<DashboardEntity>): Promise<string> {
        return Promise.resolve(DashboardClient.dashboardUrl(element.content!));
    }
}
