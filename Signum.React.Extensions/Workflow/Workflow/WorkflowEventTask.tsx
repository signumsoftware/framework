import * as React from 'react'
import { ValueLine, EntityLine, TypeContext, FormGroup, EntityStrip, EntityDetail, EntityCombo} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { ScheduledTaskEntity } from '../../Scheduler/Signum.Entities.Scheduler'
import { WorkflowEventTaskEntity, WorkflowEventEntity, WorkflowEventTaskActionEval, WorkflowEventType, TriggeredOn, WorkflowEventTaskConditionEval, WorkflowEventTaskModel } from '../Signum.Entities.Workflow'
import WorkflowEventTaskConditionComponent from './WorkflowEventTaskConditionComponent'
import WorkflowEventTaskActionComponent from './WorkflowEventTaskActionComponent'


interface WorkflowEventTaskComponentProps {
    ctx: TypeContext<WorkflowEventTaskEntity>;
}

export default class WorkflowEventTaskComponent extends React.Component<WorkflowEventTaskComponentProps> {

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

    isConditional() {
        return this.props.ctx.value.triggeredOn != TriggeredOn.value("Always");
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
                                    columnName: "Type", operation: "EqualTo", value: WorkflowEventType.value("TimerStart")
                                }
                            ]
                        }} />

                        <ValueLine ctx={ctx.subCtx(wet => wet.triggeredOn)} onChange={this.handleTriggeredOnChange} />

                        { this.isConditional() &&
                            <WorkflowEventTaskConditionComponent ctx={ctx.subCtx(wet => wet.condition)} />}
                        <WorkflowEventTaskActionComponent ctx={ctx.subCtx(wet => wet.action!)} mainEntityType={ctx.value.workflow!.entity!.mainEntityType!} />
                    </div>}
            </div>
        );
    }

    handleWorkflowChange = () => {
        this.loadWorkflow();
    }

    handleTriggeredOnChange = () => {

        const ctx = this.props.ctx;
        const task = ctx.value;

        if (this.isConditional() && !task.condition) {
            task.condition = WorkflowEventTaskConditionEval.New();
            task.modified = true;
        }

        this.forceUpdate();
    }
}

