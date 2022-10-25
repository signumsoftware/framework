import * as Navigator from '@framework/Navigator'
import { Location } from 'history'
import * as React from 'react'
import { IconColor, ToolbarConfig, ToolbarResponse } from '../Toolbar/ToolbarClient'
import * as UserChartClient from './UserChart/UserChartClient'
import * as ChartClient from './ChartClient'
import { UserChartEntity } from './Signum.Entities.Chart'
import { liteKey } from '@framework/Signum.Entities'
import { parseIcon } from '../Basics/Templates/IconTypeahead'

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
