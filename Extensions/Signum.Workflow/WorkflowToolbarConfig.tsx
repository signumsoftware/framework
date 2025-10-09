import { Location } from 'react-router'
import { ToolbarClient, ToolbarResponse } from '../Signum.Toolbar/ToolbarClient'
import { IconColor, ToolbarConfig } from '../Signum.Toolbar/ToolbarConfig'
import { WorkflowClient } from './WorkflowClient'
import { WorkflowEntity, WorkflowMainEntityStrategy } from './Signum.Workflow'
import { Entity, is, Lite } from '@framework/Signum.Entities'
import SelectorModal from '@framework/SelectorModal'
import { IconProp } from '@fortawesome/fontawesome-svg-core';

export default class WorkflowToolbarConfig extends ToolbarConfig<WorkflowEntity> {

  constructor() {
    var type = WorkflowEntity;
    super(type);
  }

  getDefaultIcon(): IconProp {
    return "shuffle";
  }

  async navigateTo(element: ToolbarResponse<WorkflowEntity>): Promise<string | null> {
    var starts = await WorkflowClient.API.starts();

    var strategies = starts.single(s => is(s, element.content)).mainEntityStrategies.map(a => a.element);

    var strategy = await SelectorModal.chooseEnum(WorkflowMainEntityStrategy, strategies)

    if (strategy == null)
      return null;

    return WorkflowClient.workflowStartUrl(element.content!, strategy);
  }

  isCompatibleWithUrlPrio(res: ToolbarResponse<WorkflowEntity>, location: Location, query: any): { prio: number, inferredEntity?: Lite<Entity> } | null {
    return location.pathname.startsWith(WorkflowClient.workflowStartUrl(res.content!)) ? ({ prio: 2 }) : null;
  }
}
