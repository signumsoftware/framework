import * as React from 'react'
import { WorkflowEventTaskConditionEval } from '../Signum.Workflow'
import { TypeContext, EntityDetail } from '@framework/Lines'
import CSharpCodeMirror from '../../Signum.CodeMirror/CSharpCodeMirror'
import EvalScriptTS from '../../Signum.Eval/EvalScriptTS';
import { useForceUpdate } from '@framework/Hooks'

export interface WorkflowEventTaskConditionComponentProps {
  ctx: TypeContext<WorkflowEventTaskConditionEval | null>;
}

export default function WorkflowEventTaskConditionComponent(p : WorkflowEventTaskConditionComponentProps): React.JSX.Element {
  const forceUpdate = useForceUpdate();

  function handleCodeChange(newScript: string) {
    const evalEntity = p.ctx.value!;
    evalEntity.scriptCS = newScript;
    evalEntity.modified = true;
    forceUpdate();
  }
  var ctx = p.ctx;

  return (
    <EntityDetail ctx={ctx} onChange={() => forceUpdate()} remove={false} getComponent={ctx =>
      <div className="code-container">
        <pre style={{ border: "0px", margin: "0px" }}>{"public bool CustomCondition() \n{"}</pre>
        <CSharpCodeMirror script={ctx.value.scriptCS ?? ""} onChange={handleCodeChange} />
        <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
        <EvalScriptTS value={ctx.value.scriptTS} onChange={v => { ctx.value.scriptTS = v; ctx.value.modified = true; forceUpdate(); }} />
      </div>} />
  );
}
