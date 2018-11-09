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
