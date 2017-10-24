import * as React from 'react'
import { ValueLine, EntityLine, TypeContext, FormGroup, EntityStrip, EntityDetail } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ScheduledTaskEntity } from '../../Scheduler/Signum.Entities.Scheduler'
import { is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { WorkflowEventModel, WorkflowEventTaskModel, WorkflowEventTaskActionEval, WorkflowEventTaskConditionEval, WorkflowMessage, WorkflowEventType, TriggeredOn } from '../Signum.Entities.Workflow'
import WorkflowEventTaskConditionComponent from './WorkflowEventTaskConditionComponent'
import WorkflowEventTaskActionComponent from './WorkflowEventTaskActionComponent'
import MessageModal from "../../../../Framework/Signum.React/Scripts/Modals/MessageModal";
import { NormalWindowMessage } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";


interface WorkflowEventModelComponentProps {
    ctx: TypeContext<WorkflowEventModel>;
}

export default class WorkflowEventModelComponent extends React.Component<WorkflowEventModelComponentProps> {

    isTimerStart() {
        return (this.props.ctx.value.type == "TimerStart");
    }

    isConditional() {
        return this.props.ctx.value.task!.triggeredOn != TriggeredOn.value("Always");
    }

    loadTask() {
        var ctx = this.props.ctx;

        if (!this.isTimerStart())
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
                <ValueLine ctx={ctx.subCtx(we => we.type)}
                    comboBoxItems={[WorkflowEventType.value("Start"), WorkflowEventType.value("TimerStart")]}
                    onChange={this.handleTypeChange} />
                {this.isTimerStart() && this.renderTaskModel(ctx)}
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

