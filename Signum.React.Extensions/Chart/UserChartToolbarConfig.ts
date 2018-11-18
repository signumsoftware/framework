import * as Navigator from '@framework/Navigator'
import { ToolbarConfig, ToolbarResponse } from '../Toolbar/ToolbarClient'
import * as UserChartClient from './UserChart/UserChartClient'
import * as ChartClient from './ChartClient'
import { UserChartEntity } from './Signum.Entities.Chart'
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
