import * as React from 'react'
import { Location } from 'history'
import { IconColor, ToolbarConfig, ToolbarResponse } from '../Toolbar/ToolbarClient'
import * as WorkflowClient from './WorkflowClient'
import { WorkflowEntity, WorkflowMainEntityStrategy } from './Signum.Entities.Workflow'
import { is } from '@framework/Signum.Entities'
import * as AppContext from '@framework/AppContext'
import { parseIcon } from '../Basics/Templates/IconTypeahead'
import SelectorModal from '../../Signum.React/Scripts/SelectorModal'

export default class WorkflowToolbarConfig extends ToolbarConfig<WorkflowEntity> {

  constructor() {
    var type = WorkflowEntity;
    super(type);
  }

  getDefaultIcon(): IconColor {
    return ({
      icon: "shuffle",
      iconColor: "darkslateblue",
    });
  }

  async navigateTo(element: ToolbarResponse<WorkflowEntity>): Promise<string | null> {
    var starts = await WorkflowClient.API.starts();

    var strategies = starts.single(s => is(s, element.content)).mainEntityStrategies.map(a => a.element);

    var strategy = await SelectorModal.chooseEnum(WorkflowMainEntityStrategy, strategies)

    if (strategy == null)
      return null;

    return WorkflowClient.workflowStartUrl(element.content!, strategy);
  }

  isCompatibleWithUrlPrio(res: ToolbarResponse<WorkflowEntity>, location: Location, query: any): number {
    return location.pathname.startsWith(AppContext.toAbsoluteUrl(WorkflowClient.workflowStartUrl(res.content!))) ? 2 : 0;
  }
}
