import { Location } from 'react-router'
import type { ToolbarResponse } from '../Signum.Toolbar/ToolbarClient'
import { IconColor, ToolbarConfig, ToolbarContext } from '../Signum.Toolbar/ToolbarConfig'
import { DashboardClient } from './DashboardClient'
import { DashboardEntity } from './Signum.Dashboard'
import { Entity, Lite, parseLite } from '@framework/Signum.Entities'
import { IconProp } from '@fortawesome/fontawesome-svg-core';

export default class DashboardToolbarConfig extends ToolbarConfig<DashboardEntity> {
   
  constructor() {
    var type = DashboardEntity;
    super(type);
  }

  getDefaultIcon(): IconProp {
    return "table-cells-large";
  }

  override navigateTo(element: ToolbarResponse<DashboardEntity>, selectedEntity: Lite<Entity> | null): Promise<string> {
    return Promise.resolve(DashboardClient.dashboardUrl(element.content!, selectedEntity ?? undefined));
  } 

  isCompatibleWithUrlPrio(res: ToolbarResponse<DashboardEntity>, location: Location, query: any, entityType?: string): { prio: number, inferredEntity?: Lite<Entity> } | null {

    if (location.pathname == DashboardClient.dashboardUrl(res.content!)) {
      return { prio: 2, inferredEntity: query["entity"] && parseLite(query["entity"]) }
    }

    return null;
  }
}
