import * as React from 'react'
import { Location } from 'history'
import { IconColor, ToolbarConfig, ToolbarResponse } from '../Toolbar/ToolbarClient'
import * as DashboardClient from './DashboardClient'
import { DashboardEntity } from './Signum.Entities.Dashboard'
import * as AppContext from '@framework/AppContext'
import { parseIcon } from '../Basics/Templates/IconTypeahead'

export default class DashboardToolbarConfig extends ToolbarConfig<DashboardEntity> {
   
  constructor() {
    var type = DashboardEntity;
    super(type);
  }

  getDefaultIcon(): IconColor{
    return ({
      icon: "table-cells-large",
      iconColor: "darkslateblue",
    });
  }

  navigateTo(element: ToolbarResponse<DashboardEntity>): Promise<string> {
    return Promise.resolve(DashboardClient.dashboardUrl(element.content!));
  } 

  isCompatibleWithUrlPrio(res: ToolbarResponse<DashboardEntity>, location: Location, query: any): number {
    return location.pathname == AppContext.toAbsoluteUrl(DashboardClient.dashboardUrl(res.content!)) ? 2 : 0;
  }
}
