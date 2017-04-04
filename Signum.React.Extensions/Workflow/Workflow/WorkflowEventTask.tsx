import * as React from 'react'
import { ValueLine, EntityLine, TypeContext, FormGroup, EntityStrip, EntityDetail, EntityCombo} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { ScheduledTaskEntity } from '../../../../Extensions/Signum.React.Extensions/Scheduler/Signum.Entities.Scheduler'
import { WorkflowEventTaskEntity, WorkflowEventEntity, WorkflowEventTaskActionEval, WorkflowEventType } from '../Signum.Entities.Workflow'
import WorkflowEventTaskConditionComponent from './WorkflowEventTaskConditionComponent'
import WorkflowEventTaskActionComponent from './WorkflowEventTaskActionComponent'


interface WorkflowEventTaskComponentProps {
    ctx: TypeContext<WorkflowEventTaskEntity>;
}

export default class WorkflowEventTaskComponent extends React.Component<WorkflowEventTaskComponentProps, void> {

    loadWorkflow() {
        var ctx = this.props.ctx;

        if (!ctx.value.workflow) {
            ctx.value.condition = null;
            ctx.value.action = null;
            this.forceUpdate();
        }
        else
            Navigator.API.fetchAndRemember(this.props.ctx.value.workflow!)
                .then(wf => {
                    if (!ctx.value.action)
                        ctx.value.action = WorkflowEventTaskActionEval.New();
                    this.forceUpdate();
                }).done();
    }

    componentWillMount() {
        this.loadWorkflow();
    }

    render() {
        var ctx = this.props.ctx;

        return (
            <div>
                <EntityLine ctx={ctx.subCtx(wet => wet.workflow)} onChange={this.handleWorkflowChange} />
                {ctx.value.workflow && ctx.value.workflow.entity &&
                    <div>
                        <EntityCombo ctx={ctx.subCtx(wet => wet.event)} findOptions={{
                            queryName: WorkflowEventEntity,
                            parentColumn: "Entity.Lane.Pool.Workflow",
                            parentValue: ctx.value.workflow,
                            filterOptions: [
                                {
                                    columnName: "Type", operation: "IsIn", value: [
                                        WorkflowEventType.value("TimerStart"),
                                        WorkflowEventType.value("ConditionalStart")
                                    ]
                                }
                            ]
                        }} />

                        <ValueLine ctx={ctx.subCtx(wet => wet.triggeredOn)} />
                        <WorkflowEventTaskConditionComponent ctx={ctx.subCtx(wet => wet.condition)} />
                        <WorkflowEventTaskActionComponent ctx={ctx.subCtx(wet => wet.action!)} mainEntityType={ctx.value.workflow!.entity!.mainEntityType!} />
                    </div>}
            </div>
        );
    }

    handleWorkflowChange = () => {
        this.loadWorkflow();
    }
}

