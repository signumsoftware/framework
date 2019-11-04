import * as React from 'react'
import { ValueLine, EntityLine, TypeContext, EntityCombo } from '@framework/Lines'
import * as Navigator from '@framework/Navigator'
import { WorkflowEventTaskEntity, WorkflowEventEntity, WorkflowEventTaskActionEval, WorkflowEventType, TriggeredOn, WorkflowEventTaskConditionEval, WorkflowEventTaskModel } from '../Signum.Entities.Workflow'
import WorkflowEventTaskConditionComponent from './WorkflowEventTaskConditionComponent'
import WorkflowEventTaskActionComponent from './WorkflowEventTaskActionComponent'
import { useForceUpdate } from '@framework/Hooks'

interface WorkflowEventTaskComponentProps {
  ctx: TypeContext<WorkflowEventTaskEntity>;
}

export default function WorkflowEventTaskComponent(p : WorkflowEventTaskComponentProps){
  const forceUpdate = useForceUpdate();

  React.useEffect(handleWorkflowChange, [p.ctx.value.workflow]);

  function handleWorkflowChange() {
    var ctx = p.ctx;
    if (!ctx.value.workflow) {
      ctx.value.condition = null;
      ctx.value.action = null;
      forceUpdate();
    }
    else
      Navigator.API.fetchAndRemember(p.ctx.value.workflow!)
        .then(wf => {
          if (!ctx.value.action)
            ctx.value.action = WorkflowEventTaskActionEval.New();
          forceUpdate();
        }).done();
  }

  function isConditional() {
    return p.ctx.value.triggeredOn != TriggeredOn.value("Always")
  }

  function handleTriggeredOnChange() {
    const ctx = p.ctx;
    const task = ctx.value;
    if (isConditional() && !task.condition) {
      task.condition = WorkflowEventTaskConditionEval.New();
      task.modified = true;
    }

    forceUpdate();
  }
  var ctx = p.ctx;

  return (
    <div>
      <EntityLine ctx={ctx.subCtx(wet => wet.workflow)} onChange={handleWorkflowChange} />
      {ctx.value.workflow && ctx.value.workflow.entity &&
        <div>
          <EntityCombo ctx={ctx.subCtx(wet => wet.event)} findOptions={{
          queryName: WorkflowEventEntity,
          parentToken: WorkflowEventEntity.token().entity(a => a.lane!.pool!.workflow),
            parentValue: ctx.value.workflow,
            filterOptions: [
              { token: WorkflowEventEntity.token(e => e.type), operation: "EqualTo", value: WorkflowEventType.value("ScheduledStart") }
            ]
          }} />

          <ValueLine ctx={ctx.subCtx(wet => wet.triggeredOn)} onChange={handleTriggeredOnChange} />

          {isConditional() &&
            <WorkflowEventTaskConditionComponent ctx={ctx.subCtx(wet => wet.condition)} />}
          <WorkflowEventTaskActionComponent ctx={ctx.subCtx(wet => wet.action!)} mainEntityType={ctx.value.workflow!.entity!.mainEntityType!} />
        </div>}
    </div>
  );
}

