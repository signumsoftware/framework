import { Location } from 'react-router'
import { IconColor, ToolbarConfig, ToolbarResponse } from '../Signum.Toolbar/ToolbarClient'
import * as DashboardClient from './DashboardClient'
import { DashboardEntity } from './Signum.Dashboard'

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
    return location.pathname == DashboardClient.dashboardUrl(res.content!) ? 2 : 0;
  }
}
