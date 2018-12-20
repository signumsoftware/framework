import * as React from 'react'
import { WorkflowConnectionModel, WorkflowConditionEntity, WorkflowActionEntity, WorkflowMessage } from '../Signum.Entities.Workflow'
import { ValueLine, EntityLine, TypeContext } from '@framework/Lines'

export default class WorkflowConnectionModelComponent extends React.Component<{ ctx: TypeContext<WorkflowConnectionModel> }> {
  render() {
    var ctx = this.props.ctx;
    return (
      <div>
        <ValueLine ctx={ctx.subCtx(e => e.name)} />
        <ValueLine ctx={ctx.subCtx(e => e.type)} />

        {ctx.value.needCondition ?
          ctx.value.mainEntityType ?
            <EntityLine ctx={ctx.subCtx(e => e.condition)} findOptions={{
              queryName: WorkflowConditionEntity,
              filterOptions: [
                { token: WorkflowConditionEntity.token().entity(e => e.mainEntityType), operation: "EqualTo", value: ctx.value.mainEntityType }
              ]
            }} /> : <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSetTheWorkflow1.niceToString(ctx.niceName(e => e.condition), ctx.niceName(e => e.mainEntityType))}</div>
          : undefined}

        {ctx.value.mainEntityType ?
          <EntityLine ctx={ctx.subCtx(e => e.action)} findOptions={{
            queryName: WorkflowActionEntity,
            filterOptions: [
              { token: WorkflowActionEntity.token().entity(e => e.mainEntityType), operation: "EqualTo", value: ctx.value.mainEntityType }
            ]
          }} />
          : <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSetTheWorkflow1.niceToString(ctx.niceName(e => e.action), ctx.niceName(e => e.mainEntityType))}</div>}

        {ctx.value.needOrder && <ValueLine ctx={ctx.subCtx(e => e.order)} />}
      </div>
    );
  }
}
