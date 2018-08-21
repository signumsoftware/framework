import * as React from 'react'
import { Dic } from '@framework/Globals'
import { getMixin } from '@framework/Signum.Entities'
import { WorkflowConnectionModel, WorkflowConditionEntity, WorkflowActionEntity, WorkflowMessage } from '../Signum.Entities.Workflow'
import {
    ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip,
    EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, EntityTable
} from '@framework/Lines'
import { SearchControl, ValueSearchControl } from '@framework/Search'

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
                                { token: "Entity.MainEntityType", operation: "EqualTo", value: ctx.value.mainEntityType }
                            ]
                        }} /> : <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSetTheWorkflow1.niceToString(ctx.niceName(e => e.condition), ctx.niceName(e => e.mainEntityType))}</div>
                    : undefined}

                {ctx.value.mainEntityType ?
                    <EntityLine ctx={ctx.subCtx(e => e.action)} findOptions={{
                        queryName: WorkflowActionEntity,
                        filterOptions: [
                            { token: "Entity.MainEntityType", operation: "EqualTo", value: ctx.value.mainEntityType }
                        ]
                    }} />
                    : <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSetTheWorkflow1.niceToString(ctx.niceName(e => e.action), ctx.niceName(e => e.mainEntityType))}</div>}                        

                {ctx.value.needOrder && <ValueLine ctx={ctx.subCtx(e => e.order)} />}
            </div>
        );
    }
}