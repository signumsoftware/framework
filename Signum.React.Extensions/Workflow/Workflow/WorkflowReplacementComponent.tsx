import * as React from 'react'
import { WorkflowReplacementModel, WorkflowReplacementItemEmbedded, CaseActivityEntity, WorkflowOperation, NewTasksEmbedded } from '../Signum.Entities.Workflow'
import { TypeContext } from '@framework/Lines'
import { SearchValueLine } from '@framework/Search'
import { symbolNiceName } from '@framework/Reflection'
import { PreviewTask } from '../WorkflowClient'
import { getToString, is } from "@framework/Signum.Entities";
import { useForceUpdate } from '@framework/Hooks'

export default function WorkflowReplacementComponent(p: { ctx: TypeContext<WorkflowReplacementModel> }) {
  var ctx = p.ctx;
  var newTasks = ctx.value.newTasks.map(a => a.element);
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
                  <SearchValueLine ctx={ectx}
                    label={getToString(ectx.value.oldNode)}
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
                    previewTasks={newTasks} />
                </td>
              </tr>)
            }
          </tbody>
        </table>}
    </div>
  );
}

export function WorkflowReplacementItemCombo(p: { ctx: TypeContext<WorkflowReplacementItemEmbedded>, previewTasks: NewTasksEmbedded[] }) {
  const forceUpdate = useForceUpdate();
  function handleChange(e: React.FormEvent<any>) {
    p.ctx.subCtx(a => a.newNode).value = (e.currentTarget as HTMLSelectElement).value;
    forceUpdate();
  }

  const ctx = p.ctx;
  return (
    <select value={ctx.value.newNode ?? ""} className="form-select form-select-sm" onChange={handleChange}>
      <option value=""> - {symbolNiceName(WorkflowOperation.Delete).toUpperCase()} - </option>
      {p.previewTasks.filter(pt => is(pt.subWorkflow, ctx.value.subWorkflow))
        .map(pt => <option value={pt.bpmnId}>{pt.name}</option>)}
    </select>
  );
}
