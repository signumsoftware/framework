import * as React from 'react'
import { Location } from 'history'
import { ToolbarConfig, ToolbarResponse } from '../Toolbar/ToolbarClient'
import * as DashboardClient from './DashboardClient'
import { DashboardEntity } from './Signum.Entities.Dashboard'
import { parseIcon } from './Admin/Dashboard';
import { coalesceIcon } from '@framework/Operations/ContextualOperations';
import * as Navigator from '@framework/Navigator';

export default class DashboardToolbarConfig extends ToolbarConfig<DashboardEntity> {
   
  constructor() {
    var type = DashboardEntity;
    super(type);
  }

  getIcon(element: ToolbarResponse<DashboardEntity>) {
    return ToolbarConfig.coloredIcon(coalesceIcon(parseIcon(element.iconName), "th-large"), element.iconColor ?? "darkslateblue");
  }

  navigateTo(element: ToolbarResponse<DashboardEntity>): Promise<string> {
    return Promise.resolve(DashboardClient.dashboardUrl(element.content!));
  } 

  isCompatibleWithUrl(res: ToolbarResponse<DashboardEntity>, location: Location, query: any): boolean {
    return location.pathname == Navigator.toAbsoluteUrl(DashboardClient.dashboardUrl(res.content!));
  }
}
