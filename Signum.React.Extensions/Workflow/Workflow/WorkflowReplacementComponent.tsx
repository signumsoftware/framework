import * as React from 'react'
import { WorkflowReplacementModel, WorkflowReplacementItemEmbedded, CaseActivityEntity, WorkflowOperation } from '../Signum.Entities.Workflow'
import { TypeContext } from '@framework/Lines'
import { ValueSearchControlLine } from '@framework/Search'
import { symbolNiceName } from '@framework/Reflection'
import { PreviewTask } from '../WorkflowClient'
import { is } from "@framework/Signum.Entities";

export default class WorkflowReplacementComponent extends React.Component<{ ctx: TypeContext<WorkflowReplacementModel>, previewTasks: PreviewTask[] }> {

  render() {
    var ctx = this.props.ctx;
    return (
      <div>
        {ctx.value.replacements.length > 0 &&
          <table className="table">
            <thead>
              <tr>
                <td>{WorkflowReplacementModel.nicePropertyName(a => a.replacements[0].element.oldNode)}</td>
                <td>{WorkflowReplacementModel.nicePropertyName(a => a.replacements[0].element.newNode)}</td>
              </tr>
            </thead>
            <tbody>
              {ctx.mlistItemCtxs(a => a.replacements).map(ectx =>
                <tr>
                  <td>
                    <ValueSearchControlLine ctx={ectx}
                      labelText={ectx.value.oldNode.toStr}
                      findOptions={{
                        queryName: CaseActivityEntity,
                        filterOptions: [
                          { token: CaseActivityEntity.token(e => e.workflowActivity), value: ectx.value.oldNode },
                          { token: CaseActivityEntity.token(e => e.doneDate), value: null }
                        ]
                      }} />
                  </td>
                  <td>
                    <WorkflowReplacementItemCombo
                      ctx={ectx}
                      previewTasks={this.props.previewTasks} />
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
    this.props.ctx.subCtx(a => a.newNode).value = (e.currentTarget as HTMLSelectElement).value;
    this.forceUpdate();
  }

  render() {
    const ctx = this.props.ctx;
    return (
      <select value={ctx.value.newNode || ""} className="form-control form-control-sm" onChange={this.handleChange}>
        <option value=""> - {symbolNiceName(WorkflowOperation.Delete).toUpperCase()} - </option>
        {this.props.previewTasks.filter(pt => is(pt.subWorkflow, ctx.value.subWorkflow))
          .map(pt => <option value={pt.bpmnId}>{pt.name}</option>)}
      </select>
    );
  }
}
