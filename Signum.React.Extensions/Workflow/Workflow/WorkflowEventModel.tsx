import * as React from 'react'
import { ValueLine, TypeContext, EntityDetail, RenderEntity } from '@framework/Lines'
import { WorkflowEventModel, WorkflowEventTaskModel, WorkflowEventTaskActionEval, WorkflowEventTaskConditionEval, WorkflowMessage, WorkflowEventType, TriggeredOn, WorkflowTimerEmbedded } from '../Signum.Entities.Workflow'
import WorkflowEventTaskConditionComponent from './WorkflowEventTaskConditionComponent'
import WorkflowEventTaskActionComponent from './WorkflowEventTaskActionComponent'
import { useForceUpdate } from '@framework/Hooks'

interface WorkflowEventModelComponentProps {
  ctx: TypeContext<WorkflowEventModel>;
}

export default function WorkflowEventModelComponent(p : WorkflowEventModelComponentProps){
  const forceUpdate = useForceUpdate();
  function isSchedulesStart() {
    return (p.ctx.value.type == "ScheduledStart");
  }

  function isConditional() {
    return p.ctx.value.task!.triggeredOn != TriggeredOn.value("Always");
  }

  function isTimer(type: WorkflowEventType) {
    return type == "BoundaryForkTimer" ||
      type == "BoundaryInterruptingTimer" ||
      type == "IntermediateTimer";
  }

  React.useEffect(loadTask, []);

  function loadTask() {
    var ctx = p.ctx;

    if (!isSchedulesStart()) {
      ctx.value.task = null;
      ctx.value.modified = true;
    }
    else {
      if (!ctx.value.task) {
        ctx.value.task = WorkflowEventTaskModel.New({
          triggeredOn: "Always",
          action: WorkflowEventTaskActionEval.New(),
        });
        ctx.value.modified = true;
      }
    }

    forceUpdate();
  }

  function getTypeComboItems() {
    const ctx = p.ctx;
    return isTimer(ctx.value.type!) ?
      [WorkflowEventType.value(ctx.value.type!)] :
      WorkflowEventType.values().filter(a => !isTimer(a)).map(a => WorkflowEventType.value(a));
  }

  function renderTaskModel(ctx: TypeContext<WorkflowEventModel>) {
    if (!ctx.value.mainEntityType)
      return <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSetTheWorkflow1.niceToString(ctx.niceName(e => e.task), ctx.niceName(e => e.mainEntityType))}</div>;

    return (
      <div>
        <ValueLine ctx={ctx.subCtx(we => we.task!.suspended)} />
        <EntityDetail ctx={ctx.subCtx(we => we.task!.rule)} />

        <ValueLine ctx={ctx.subCtx(we => we.task!.triggeredOn)} onChange={handleTriggeredOnChange} />

        {isConditional() &&
          <WorkflowEventTaskConditionComponent ctx={ctx.subCtx(we => we.task!.condition)} />}
        <WorkflowEventTaskActionComponent ctx={ctx.subCtx(we => we.task!.action!)} mainEntityType={ctx.value.mainEntityType} />
      </div>
    );
  }

  function handleTriggeredOnChange() {
    const ctx = p.ctx;
    const task = ctx.value.task!;

    if (isConditional() && !task.condition) {
      task.condition = WorkflowEventTaskConditionEval.New();
      task.modified = true;
    }

    forceUpdate();
  }
  var ctx = p.ctx;

  return (
    <div>
      <ValueLine ctx={ctx.subCtx(we => we.name)} />
      <ValueLine ctx={ctx.subCtx(we => we.type)} readOnly={isTimer(ctx.value.type!)} comboBoxItems={getTypeComboItems()} onChange={loadTask} />
      {isSchedulesStart() && renderTaskModel(ctx)}
      {ctx.value.timer && <RenderEntity ctx={ctx.subCtx(a => a.timer)} />}
    </div>
  );
}

