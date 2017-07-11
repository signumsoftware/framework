import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { getMixin } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { WorkflowConnectionModel, WorkflowConditionEntity, WorkflowActionEntity, WorkflowMessage } from '../Signum.Entities.Workflow'
import {
    ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip,
    EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, EntityTable
} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, ValueSearchControl } from '../../../../Framework/Signum.React/Scripts/Search'

export default class WorkflowConnectionModelComponent extends React.Component<{ ctx: TypeContext<WorkflowConnectionModel> }> {

    render() {
        var ctx = this.props.ctx;
        return (
            <div>
                <ValueLine ctx={ctx.subCtx(e => e.name)} />
                {ctx.value.needDecisonResult && <ValueLine ctx={ctx.subCtx(e => e.decisonResult)} />}

                {ctx.value.needCondition ?
                    ctx.value.mainEntityType ?
                        <EntityLine ctx={ctx.subCtx(e => e.condition)} findOptions={{
                            queryName: WorkflowConditionEntity,
                            filterOptions: [
                                { columnName: "Entity.MainEntityType", operation: "EqualTo", value: ctx.value.mainEntityType }
                            ]
                        }} /> : <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSetTheWorkflow1.niceToString(ctx.niceName(e => e.condition), ctx.niceName(e => e.mainEntityType))}</div>
                    : undefined}

                {ctx.value.mainEntityType ?
                    <EntityLine ctx={ctx.subCtx(e => e.action)} findOptions={{
                        queryName: WorkflowActionEntity,
                        filterOptions: [
                            { columnName: "Entity.MainEntityType", operation: "EqualTo", value: ctx.value.mainEntityType }
                        ]
                    }} />
                    : <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSetTheWorkflow1.niceToString(ctx.niceName(e => e.action), ctx.niceName(e => e.mainEntityType))}</div>}                        

                {ctx.value.needOrder && <ValueLine ctx={ctx.subCtx(e => e.order)} />}
            </div>
        );
    }
}