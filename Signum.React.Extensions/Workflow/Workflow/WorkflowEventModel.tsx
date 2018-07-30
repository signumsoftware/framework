import * as React from 'react'
import { ValueLine, EntityLine, TypeContext, FormGroup, EntityStrip, EntityDetail, RenderEntity } from '@framework/Lines'
import { ScheduledTaskEntity } from '../../Scheduler/Signum.Entities.Scheduler'
import { is } from '@framework/Signum.Entities'
import { WorkflowEventModel, WorkflowEventTaskModel, WorkflowEventTaskActionEval, WorkflowEventTaskConditionEval, WorkflowMessage, WorkflowEventType, TriggeredOn, WorkflowTimerEmbedded } from '../Signum.Entities.Workflow'
import WorkflowEventTaskConditionComponent from './WorkflowEventTaskConditionComponent'
import WorkflowEventTaskActionComponent from './WorkflowEventTaskActionComponent'
import MessageModal from "@framework/Modals/MessageModal";
import { NormalWindowMessage } from "@framework/Signum.Entities";


interface WorkflowEventModelComponentProps {
    ctx: TypeContext<WorkflowEventModel>;
}

export default class WorkflowEventModelComponent extends React.Component<WorkflowEventModelComponentProps> {

    isSchedulesStart() {
        return (this.props.ctx.value.type == "ScheduledStart");
    }

    isConditional() {
        return this.props.ctx.value.task!.triggeredOn != TriggeredOn.value("Always");
    }

    isTimer(type: WorkflowEventType) {
        return type == "BoundaryForkTimer" ||
            type == "BoundaryInterruptingTimer" ||
            type == "IntermediateTimer";
    }

    loadTask() {
        var ctx = this.props.ctx;

        if (!this.isSchedulesStart())
        {
            ctx.value.task = null;
            ctx.value.modified = true;
        }
        else {
            if (!ctx.value.task)
            {
                ctx.value.task = WorkflowEventTaskModel.New({
                    triggeredOn: "Always",
                    action: WorkflowEventTaskActionEval.New(),
                });
                ctx.value.modified = true;
            }
        }

        this.forceUpdate();
    }

    componentWillMount() {
        this.loadTask();
    }

    handleTypeChange = () => {
        this.loadTask();
    }

    render() {
        var ctx = this.props.ctx;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(we => we.name)} />
                <ValueLine ctx={ctx.subCtx(we => we.type)} readOnly={this.isTimer(ctx.value.type!)} comboBoxItems={this.getTypeComboItems()} onChange={this.handleTypeChange} />
                {this.isSchedulesStart() && this.renderTaskModel(ctx)}
                {ctx.value.timer && <RenderEntity ctx={ctx.subCtx(a => a.timer)} />}
            </div>
        );
    }

    getTypeComboItems = () => {
        const ctx = this.props.ctx;
        return this.isTimer(ctx.value.type!) ?
            [WorkflowEventType.value(ctx.value.type!)] :
            WorkflowEventType.values().filter(a => !this.isTimer(a)).map(a => WorkflowEventType.value(a));
    }

    renderTaskModel(ctx: TypeContext<WorkflowEventModel>) {
        if (!ctx.value.mainEntityType)
            return <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSetTheWorkflow1.niceToString(ctx.niceName(e => e.task), ctx.niceName(e => e.mainEntityType))}</div>;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(we => we.task!.suspended)} />
                <EntityDetail ctx={ctx.subCtx(we => we.task!.rule)} />

                <ValueLine ctx={ctx.subCtx(we => we.task!.triggeredOn)} onChange={this.handleTriggeredOnChange} />

                {this.isConditional() &&
                    <WorkflowEventTaskConditionComponent ctx={ctx.subCtx(we => we.task!.condition)} />}
                <WorkflowEventTaskActionComponent ctx={ctx.subCtx(we => we.task!.action!)} mainEntityType={ctx.value.mainEntityType} />
            </div>
        );
    }

    handleTriggeredOnChange = () => {

        const ctx = this.props.ctx;
        const task = ctx.value.task!;

        if (this.isConditional() && !task.condition) {
            task.condition = WorkflowEventTaskConditionEval.New();
            task.modified = true;
        }
        
        this.forceUpdate();
    }
}

