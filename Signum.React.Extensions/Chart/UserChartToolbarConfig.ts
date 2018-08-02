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
import * as UserChartClient from './UserChart/UserChartClient'
import * as ChartClient from './ChartClient'
import { UserChartEntity  } from './Signum.Entities.Chart'
import { parseIcon } from '../Dashboard/Admin/Dashboard';

export default class UserChartToolbarConfig extends ToolbarConfig<UserChartEntity> {

    constructor() {
        var type = UserChartEntity;
        super(type);
    }

    getIcon(element: ToolbarResponse<UserChartEntity>) {
        return ToolbarConfig.coloredIcon(element.iconName ? parseIcon(element.iconName) : "chart-bar", element.iconColor || "darkviolet");
    }
    
    navigateTo(element: ToolbarResponse<UserChartEntity>): Promise<string> {
        return Navigator.API.fetchAndForget(element.content!)
            .then(a => UserChartClient.Converter.toChartRequest(a, undefined))
            .then(cr => ChartClient.Encoder.chartPath(cr, element.content!));
    }
}
