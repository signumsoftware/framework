import { Location } from 'react-router'
import { Navigator } from '@framework/Navigator'
import { ToolbarClient, ToolbarResponse } from '../../Signum.Toolbar/ToolbarClient'
import { IconColor, ToolbarConfig } from '../../Signum.Toolbar/ToolbarConfig'
import { UserChartClient } from '../UserChart/UserChartClient'
import { ChartClient } from '../ChartClient'
import { Entity, Lite, liteKey, parseLite } from '@framework/Signum.Entities'
import { UserChartEntity } from './Signum.Chart.UserChart'
import { IconProp } from '@fortawesome/fontawesome-svg-core';



export default class UserChartToolbarConfig extends ToolbarConfig<UserChartEntity> {
  constructor() {
    var type = UserChartEntity;
    super(type);
  }

  getDefaultIcon(): IconProp {
    return "chart-bar";
  }

  navigateTo(element: ToolbarResponse<UserChartEntity>): Promise<string> {
    return Navigator.API.fetch(element.content!)
      .then(a => UserChartClient.Converter.toChartRequest(a, undefined))
      .then(cr => ChartClient.Encoder.chartPathPromise(cr, element.content!));
  }

  isCompatibleWithUrlPrio(res: ToolbarResponse<UserChartEntity>, location: Location, query: any, entityType?: string): { prio: number, inferredEntity?: Lite<Entity> } | null {
    if (query["userChart"] == liteKey(res.content!)) {
      return { prio: 2, inferredEntity: query["entity"] && parseLite(query["entity"]) }
    }

    return null;
  }
}
