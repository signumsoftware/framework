import { Location } from 'react-router'
import type { ToolbarResponse } from '../Signum.Toolbar/ToolbarClient'
import { IconColor, ToolbarConfig, ToolbarContext } from '../Signum.Toolbar/ToolbarConfig'
import { DashboardClient } from './DashboardClient'
import { DashboardEntity } from './Signum.Dashboard'
import { Entity, Lite, parseLite } from '@framework/Signum.Entities'

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

  isCompatibleWithUrlPrio(res: ToolbarResponse<DashboardEntity>, location: Location, query: any, entityType?: string): { prio: number, inferredEntity?: Lite<Entity> } | null {

    if (location.pathname == DashboardClient.dashboardUrl(res.content!)) {
      return { prio: 2, inferredEntity: query["entity"] && parseLite(query["entity"]) }
    }

    return null;
  }
}
