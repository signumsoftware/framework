import * as React from 'react'
import { WorkflowReplacementModel, WorkflowReplacementItemEntity, CaseActivityEntity } from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, PropertyRoute } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, CountSearchControlLine } from '../../../../Framework/Signum.React/Scripts/Search'
import { PreviewTask } from '../WorkflowClient'

export default class WorkflowReplacementComponent extends React.Component<{ ctx: TypeContext<WorkflowReplacementModel>, previewTasks: PreviewTask[] }, void> {

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
                                        <CountSearchControlLine ctx={ectx}
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
                                            ctx={ectx.subCtx(a => a.newTask)}
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

export class WorkflowReplacementItemCombo extends React.Component<{ ctx: TypeContext<string | null | undefined>, previewTasks: PreviewTask[] }, void> {

    handleChange = (e: React.FormEvent) => {
        this.props.ctx.value = (e.currentTarget as HTMLSelectElement).value;
        this.forceUpdate();
    }

    render() {
        const ctx = this.props.ctx;
        return (
            <select value={ctx.value || ""} className="form-control" onChange={this.handleChange}>
                {this.props.previewTasks.map(pt => <option value={pt.BpmnId}>{pt.Name}</option>)}
            </select>
        );
    }
}
