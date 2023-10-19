import * as React from 'react'
import { AutoLine, TypeContext, EntityDetail, RenderEntity, EntityLine, EnumLine } from '@framework/Lines'
import { WorkflowEventModel, WorkflowEventTaskModel, WorkflowEventTaskActionEval, WorkflowEventTaskConditionEval, WorkflowMessage, WorkflowEventType, TriggeredOn, WorkflowTimerEmbedded, WorkflowTimerConditionEntity } from '../Signum.Workflow'
import WorkflowEventTaskConditionComponent from './WorkflowEventTaskConditionComponent'
import WorkflowEventTaskActionComponent from './WorkflowEventTaskActionComponent'
import { useForceUpdate } from '@framework/Hooks'
import { TypeEntity } from '@framework/Signum.Basics'

interface WorkflowEventModelComponentProps {
  ctx: TypeContext<WorkflowEventModel>;
}

export default function WorkflowEventModelComponent(p: WorkflowEventModelComponentProps) {
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

  var ctx = p.ctx;

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(we => we.name)} />
      <EnumLine ctx={ctx.subCtx(we => we.type)} readOnly={isTimer(ctx.value.type!)} optionItems={getTypeComboItems()} onChange={loadTask} />
      {ctx.value.type == "BoundaryForkTimer" && <AutoLine ctx={ctx.subCtx(a => a.runRepeatedly)} />}
      {ctx.value.type == "BoundaryInterruptingTimer" && <AutoLine ctx={ctx.subCtx(a => a.decisionOptionName)} />}
      {ctx.value.task && <WorkflowEventTask ctx={ctx.subCtx(a => a.task!)} mainEntityType={ctx.value.mainEntityType} isConditional={isConditional()} />}
      {ctx.value.timer && <WorkflowTimer ctx={ctx.subCtx(a => a.timer!)} mainEntityType={ctx.value.mainEntityType}/>}
    </div>
  );
}



function WorkflowEventTask(p: { ctx: TypeContext<WorkflowEventTaskModel>, mainEntityType: TypeEntity, isConditional: boolean }) {

  const forceUpdate = useForceUpdate();

  const ctx = p.ctx;

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(te => te.suspended)} />
      <EntityDetail ctx={ctx.subCtx(te => te.rule)} />
      <AutoLine ctx={ctx.subCtx(te => te.triggeredOn)} onChange={handleTriggeredOnChange} />
      {p.isConditional && <WorkflowEventTaskConditionComponent ctx={ctx.subCtx(t => t.condition)} />}
      <WorkflowEventTaskActionComponent ctx={ctx.subCtx(t => t.action!)} mainEntityType={p.mainEntityType} />
    </div>
  );

  function handleTriggeredOnChange() {
    const task = p.ctx.value;

    if (p.isConditional && !task.condition) {
      task.condition = WorkflowEventTaskConditionEval.New();
      task.modified = true;
    }

    forceUpdate();
  }
}

function WorkflowTimer(p: { ctx: TypeContext<WorkflowTimerEmbedded>, mainEntityType: TypeEntity }) {

  const ctx = p.ctx;

  return (
    <div>
      <EntityDetail ctx={ctx.subCtx(te => te.duration)} />
      <EntityLine ctx={ctx.subCtx(te => te.condition)}
        findOptions={{ queryName: WorkflowTimerConditionEntity, filterOptions: [{ token: WorkflowTimerConditionEntity.token(a => a.entity.mainEntityType), value: p.mainEntityType }] }} />
      <ValueLine ctx={ctx.subCtx(te => te.avoidExecuteConditionByTimer)} />
    </div>
  );
}

