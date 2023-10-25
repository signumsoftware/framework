import * as React from 'react'
import { AutoLine, EntityLine, TypeContext, LiteAutocompleteConfig, TextAreaLine } from '@framework/Lines'
import { PropertyRoute } from '@framework/Reflection'
import CSharpCodeMirror from '../../Signum.CodeMirror/CSharpCodeMirror'
import { WorkflowActionEntity } from '../Signum.Workflow'
import { API, showWorkflowTransitionContextCodeHelp } from '../WorkflowClient'
import TypeHelpComponent from "../../Signum.Eval/TypeHelp/TypeHelpComponent";
import AutoLineModal from '@framework/AutoLineModal'
import { useForceUpdate } from '@framework/Hooks'

interface WorkflowConditionComponentProps {
  ctx: TypeContext<WorkflowActionEntity>;
}

export default function WorkflowConditionComponent(p: WorkflowConditionComponentProps) {
  const forceUpdate = useForceUpdate();
  function handleMainEntityTypeChange() {
    p.ctx.value.eval!.script = "";
    forceUpdate();
  }

  function handleCodeChange(newScript: string) {
    const evalEntity = p.ctx.value.eval!;
    evalEntity.script = newScript;
    evalEntity.modified = true;
    forceUpdate();
  }


  function handleTypeHelpClick(pr: PropertyRoute | undefined) {
    if (!pr)
      return;

    AutoLineModal.show({
      type: { name: "string" },
      initialValue: TypeHelpComponent.getExpression("e", pr, "CSharp"),
      customComponent: props => <TextAreaLine {...props} />,
      title: "Property Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
    });
  }
  var ctx = p.ctx;

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(wc => wc.name)} />
      <EntityLine ctx={ctx.subCtx(wc => wc.mainEntityType)}
        onChange={handleMainEntityTypeChange}
        autocomplete={new LiteAutocompleteConfig((signal, str) => API.findMainEntityType({ subString: str, count: 5 }, signal))}
        find={false} />
      {ctx.value.mainEntityType &&
        <div>
          <br />
          <div className="row">
            <div className="col-sm-7">
              <div className="code-container">
                <div className="btn-group" style={{ marginBottom: "3px" }}>
                  <input type="button" className="btn btn-success btn-sm sf-button" value="ctx" onClick={() => showWorkflowTransitionContextCodeHelp()} />
                </div>
                <pre style={{ border: "0px", margin: "0px" }}>{"void Action(" + ctx.value.mainEntityType.cleanName + "Entity e, WorkflowTransitionContext ctx)\n{"}</pre>
                <CSharpCodeMirror script={ctx.value.eval!.script ?? ""} onChange={handleCodeChange} onInit={cm => cm.setSize(null, 600)} />
                <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
              </div>
            </div>
            <div className="col-sm-5">
              <TypeHelpComponent initialType={ctx.value.mainEntityType.cleanName} mode="CSharp" onMemberClick={handleTypeHelpClick} />
            </div>
          </div>
        </div>}
    </div>
  );
}

