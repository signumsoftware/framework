import { Location } from 'react-router'
import * as Navigator from '@framework/Navigator'
import { IconColor, ToolbarConfig, ToolbarResponse } from '../Signum.Toolbar/ToolbarClient'
import * as UserChartClient from './UserChart/UserChartClient'
import * as ChartClient from './ChartClient'
import { liteKey } from '@framework/Signum.Entities'
import { UserChartEntity } from './Signum.Chart.UserChart'

export default class UserChartToolbarConfig extends ToolbarConfig<UserChartEntity> {
  constructor() {
    var type = UserChartEntity;
    super(type);
  }

  getDefaultIcon(): IconColor {
    return ({
      icon: "chart-bar",
      iconColor: "darkviolet",
    });
  }

  navigateTo(element: ToolbarResponse<UserChartEntity>): Promise<string> {
    return Navigator.API.fetch(element.content!)
      .then(a => UserChartClient.Converter.toChartRequest(a, undefined))
      .then(cr => ChartClient.Encoder.chartPathPromise(cr, element.content!));
  }

  isCompatibleWithUrlPrio(res: ToolbarResponse<UserChartEntity>, location: Location, query: any): number {
    return query["userChart"] == liteKey(res.content!) ? 2 : 0;
  }
}
