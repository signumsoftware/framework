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
import * as UserChartClient from './UserChart/UserChartClient'
import * as ChartClient from './ChartClient'
import { UserChartEntity  } from './Signum.Entities.Chart'

export default class UserChartToolbarConfig extends ToolbarConfig<UserChartEntity> {

    constructor() {
        super(UserChartEntity);
    }

    getIcon(element: ToolbarResponse<UserChartEntity>) {
        return ToolbarConfig.coloredIcon(element.iconName || "glyphicon glyphicon-stats", element.iconColor || "darkviolet");
    }
    
    navigateTo(element: ToolbarResponse<UserChartEntity>): Promise<string> {
        return Navigator.API.fetchAndForget(element.content!)
            .then(a => UserChartClient.Converter.toChartRequest(a, undefined))
            .then(cr => ChartClient.Encoder.chartPath(cr, element.content!));
    }
}
