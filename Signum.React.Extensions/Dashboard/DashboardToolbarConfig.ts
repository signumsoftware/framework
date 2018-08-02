import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '@framework/Globals';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '@framework/Services';
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { QueryEntity } from '@framework/Signum.Entities.Basics'
import { Lite, Entity, EntityPack, liteKey } from '@framework/Signum.Entities'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName } from '@framework/Reflection'
import { ToolbarEntity, ToolbarMenuEntity, ToolbarElementEmbedded, ToolbarElementType } from '../Toolbar/Signum.Entities.Toolbar'
import { ToolbarConfig, ToolbarResponse } from '../Toolbar/ToolbarClient'
import * as DashboardClient from './DashboardClient'
import { DashboardEntity } from './Signum.Entities.Dashboard'
import { parseIcon } from './Admin/Dashboard';

export default class DashboardToolbarConfig extends ToolbarConfig<DashboardEntity> {

    constructor() {
        var type = DashboardEntity;
        super(type);
    }

    getIcon(element: ToolbarResponse<DashboardEntity>) {
        return ToolbarConfig.coloredIcon(element.iconName ? parseIcon(element.iconName) : "th-large", element.iconColor || "darkslateblue");
    }

    navigateTo(element: ToolbarResponse<DashboardEntity>): Promise<string> {
        return Promise.resolve(DashboardClient.dashboardUrl(element.content!));
    }
}
