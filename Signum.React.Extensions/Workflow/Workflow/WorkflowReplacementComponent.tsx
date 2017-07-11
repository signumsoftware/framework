import * as React from 'react'
import { WorkflowReplacementModel, WorkflowReplacementItemEmbedded, CaseActivityEntity, WorkflowOperation, WorkflowEntity } from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, PropertyRoute } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, ValueSearchControlLine } from '../../../../Framework/Signum.React/Scripts/Search'
import { symbolNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { PreviewTask } from '../WorkflowClient'
import { is } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";

export default class WorkflowReplacementComponent extends React.Component<{ ctx: TypeContext<WorkflowReplacementModel>, previewTasks: PreviewTask[] }> {

    render() {
        var ctx = this.props.ctx;
        return (
            <div>
                {ctx.value.replacements.length > 0 &&
                    <table className="table">
                        <thead>
                            <tr>
                                <td>{WorkflowReplacementModel.nicePropertyName(a => a.replacements[0].element.oldTask)}</td>
                                <td>{WorkflowReplacementModel.nicePropertyName(a => a.replacements[0].element.newTask)}</td>
                            </tr>
                        </thead>
                        <tbody>
                            {ctx.mlistItemCtxs(a => a.replacements).map(ectx =>
                                <tr>
                                    <td>
                                        <ValueSearchControlLine ctx={ectx}
                                            labelText={ectx.value.oldTask.toStr}
                                            findOptions={{
                                                queryName: CaseActivityEntity,
                                                filterOptions: [
                                                    { columnName: "WorkflowActivity", value: ectx.value.oldTask },
                                                    { columnName: "DoneDate", value: null }
                                                ]
                                            }}/>
                                    </td>
                                    <td>
                                        <WorkflowReplacementItemCombo
                                            ctx={ectx}
                                            previewTasks={this.props.previewTasks}/>
                                    </td>
                                </tr>)
                            }
                        </tbody>
                    </table>}
            </div>
        );
    }
}

export class WorkflowReplacementItemCombo extends React.Component<{ ctx: TypeContext<WorkflowReplacementItemEmbedded>, previewTasks: PreviewTask[] }> {

    handleChange = (e: React.FormEvent<any>) => {
        this.props.ctx.subCtx(a => a.newTask).value = (e.currentTarget as HTMLSelectElement).value;
        this.forceUpdate();
    }

    render() {
        const ctx = this.props.ctx;
        return (
            <select value={ctx.value.newTask || ""} className="form-control" onChange={this.handleChange}>
                <option value=""> - {symbolNiceName(WorkflowOperation.Delete).toUpperCase()} - </option>
                {this.props.previewTasks.filter(pt => is(pt.SubWorkflow, ctx.value.subWorkflow))
                    .map(pt => <option value={pt.BpmnId}>{pt.Name}</option>)}
            </select>
        );
    }
}
