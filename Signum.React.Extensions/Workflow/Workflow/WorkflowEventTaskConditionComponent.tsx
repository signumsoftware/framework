import * as React from 'react'
import { WorkflowEventTaskConditionEval } from '../Signum.Entities.Workflow'
import { TypeContext, EntityDetail } from '@framework/Lines'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror';
import { useForceUpdate } from '@framework/Hooks'

export interface WorkflowEventTaskConditionComponentProps {
  ctx: TypeContext<WorkflowEventTaskConditionEval | undefined | null>;
}

export default function WorkflowEventTaskConditionComponent(p : WorkflowEventTaskConditionComponentProps){
  const forceUpdate = useForceUpdate();

  function handleCodeChange(newScript: string) {
    const evalEntity = p.ctx.value!;
    evalEntity.script = newScript;
    evalEntity.modified = true;
    forceUpdate();
  }
  var ctx = p.ctx;

  return (
    <EntityDetail ctx={ctx} onChange={() => forceUpdate()} remove={false} getComponent={(ctx: TypeContext<WorkflowEventTaskConditionEval>) =>
      <div className="code-container">
        <pre style={{ border: "0px", margin: "0px" }}>{"public bool CustomCondition() \n{"}</pre>
        <CSharpCodeMirror script={ctx.value.script ?? ""} onChange={handleCodeChange} />
        <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
      </div>} />
  );
}
