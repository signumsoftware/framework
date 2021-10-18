import * as React from 'react'
import { ValueLine, TypeContext, EntityStrip, EntityDetail } from '@framework/Lines'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import { WorkflowLaneModel, WorkflowLaneActorsEval, WorkflowMessage } from '../Signum.Entities.Workflow'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import { useForceUpdate } from '@framework/Hooks'

interface WorkflowLaneModelComponentProps {
  ctx: TypeContext<WorkflowLaneModel>;
}

export default function WorkflowLaneModelComponent(p : WorkflowLaneModelComponentProps){
  const forceUpdate = useForceUpdate();

  function handleCodeChange(newScript: string) {
    const actorsEval = p.ctx.value.actorsEval!;
    actorsEval.script = newScript;
    actorsEval.modified = true;
    forceUpdate();
  }

  function renderActorEval(ectx: TypeContext<WorkflowLaneActorsEval>) {
    var mainEntityName = p.ctx.value.mainEntityType.cleanName;
    return (
      <div className="row">
        <div className="col-sm-7">
          <div className="code-container">
            <pre style={{ border: "0px", margin: "0px" }}>{"IEnumerable<Lite<Entity>> GetActors(" + mainEntityName + "Entity e, WorkflowTransitionContext ctx)\n{"}</pre>
            <CSharpCodeMirror script={ectx.value.script ?? ""} onChange={handleCodeChange} />
            <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
          </div>
        </div>
        <div className="col-sm-5">
          <TypeHelpComponent initialType={mainEntityName} mode="CSharp" />
        </div>
      </div>
    );
  }
  var ctx = p.ctx;

  return (
    <div>
      <ValueLine ctx={ctx.subCtx(wc => wc.name)} />
      <EntityStrip ctx={ctx.subCtx(wc => wc.actors)} />
      {ctx.value.mainEntityType ?
        <EntityDetail ctx={ctx.subCtx(wc => wc.actorsEval)} getComponent={renderActorEval} onCreate={() => Promise.resolve(WorkflowLaneActorsEval.New({ script: "new List<Lite<Entity>>{ e.YourProperty }" }))} />
        :
        <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSetTheWorkflow1.niceToString(ctx.niceName(e => e.actorsEval), ctx.niceName(e => e.mainEntityType))}</div>}
    </div>
  );
}
