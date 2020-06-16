import * as Navigator from '@framework/Navigator'
import { Location } from 'history'
import * as React from 'react'
import { ToolbarConfig, ToolbarResponse } from '../Toolbar/ToolbarClient'
import * as UserChartClient from './UserChart/UserChartClient'
import * as ChartClient from './ChartClient'
import { UserChartEntity } from './Signum.Entities.Chart'
import { parseIcon } from '../Dashboard/Admin/Dashboard';
import { coalesceIcon } from '@framework/Operations/ContextualOperations';
import { liteKey } from '@framework/Signum.Entities'

export default class UserChartToolbarConfig extends ToolbarConfig<UserChartEntity> {
  constructor() {
    var type = UserChartEntity;
    super(type);
  }

  getIcon(element: ToolbarResponse<UserChartEntity>) {
    return ToolbarConfig.coloredIcon(coalesceIcon(parseIcon(element.iconName), "chart-bar"), element.iconColor ?? "darkviolet");
  }

  navigateTo(element: ToolbarResponse<UserChartEntity>): Promise<string> {
    return Navigator.API.fetchAndForget(element.content!)
      .then(a => UserChartClient.Converter.toChartRequest(a, undefined))
      .then(cr => ChartClient.Encoder.chartPathPromise(cr, element.content!));
  }

  isCompatibleWithUrl(res: ToolbarResponse<UserChartEntity>, location: Location, query: any): boolean {
    return query["userChart"] == liteKey(res.content!);
  }
}
