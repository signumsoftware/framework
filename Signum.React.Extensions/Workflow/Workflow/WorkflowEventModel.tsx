import * as React from 'react'
import { ValueLine, EntityLine, TypeContext, FormGroup, EntityStrip, EntityDetail } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ScheduledTaskEntity } from '../../../../Extensions/Signum.React.Extensions/Scheduler/Signum.Entities.Scheduler'
import { is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { WorkflowEventModel, WorkflowEventTaskModel, WorkflowEventTaskActionEval, WorkflowEventTaskConditionEval, WorkflowMessage, WorkflowEventType } from '../Signum.Entities.Workflow'
import WorkflowEventTaskConditionComponent from './WorkflowEventTaskConditionComponent'
import WorkflowEventTaskActionComponent from './WorkflowEventTaskActionComponent'


interface WorkflowEventModelComponentProps {
    ctx: TypeContext<WorkflowEventModel>;
}

export default class WorkflowEventModelComponent extends React.Component<WorkflowEventModelComponentProps, void> {

    isTimerOrConditionalStart() {
        return (this.props.ctx.value.type == "TimerStart" || this.props.ctx.value.type == "ConditionalStart");
    }

    loadTask() {
        var ctx = this.props.ctx;

        if (!this.isTimerOrConditionalStart())
            ctx.value.task = null
        else {
            if (!ctx.value.task)
                ctx.value.task = WorkflowEventTaskModel.New({
                    triggeredOn: "Always",
                    action: WorkflowEventTaskActionEval.New(),
                });
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
                <ValueLine ctx={ctx.subCtx(we => we.type)}
                    comboBoxItems={[WorkflowEventType.value("Start"), WorkflowEventType.value("TimerStart"), WorkflowEventType.value("ConditionalStart")]}
                    onChange={this.handleTypeChange} />
                {this.isTimerOrConditionalStart() && this.renderTaskModel(ctx)}
            </div>
        );
    }

    renderTaskModel(ctx: TypeContext<WorkflowEventModel>) {
        if (!ctx.value.mainEntityType)
            return <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSetTheWorkflow1.niceToString(ctx.niceName(e => e.task), ctx.niceName(e => e.mainEntityType))}</div>;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(we => we.task!.suspended)} />
                <EntityDetail ctx={ctx.subCtx(we => we.task!.rule)} />
                <ValueLine ctx={ctx.subCtx(we => we.task!.triggeredOn)} />
                <WorkflowEventTaskConditionComponent ctx={ctx.subCtx(we => we.task!.condition)} />
                <WorkflowEventTaskActionComponent ctx={ctx.subCtx(we => we.task!.action!)} mainEntityType={ctx.value.mainEntityType} />
            </div>
        );
    }
}

