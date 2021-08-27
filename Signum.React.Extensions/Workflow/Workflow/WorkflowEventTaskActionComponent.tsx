import * as React from 'react'
import { WorkflowEventTaskActionEval } from '../Signum.Entities.Workflow'
import { TypeContext, PropertyRoute } from '@framework/Lines'
import { TypeEntity } from '@framework/Signum.Entities.Basics'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import ValueLineModal from '@framework/ValueLineModal'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror';
import { useForceUpdate } from '@framework/Hooks'

export interface WorkflowEventTaskActionComponentProps {
  ctx: TypeContext<WorkflowEventTaskActionEval>;
  mainEntityType: TypeEntity;
}

export default function WorkflowEventTaskActionComponent(p : WorkflowEventTaskActionComponentProps){
  const forceUpdate = useForceUpdate();

  function handleCodeChange(newScript: string) {
    const evalEntity = p.ctx.value;
    evalEntity.script = newScript;
    evalEntity.modified = true;
    forceUpdate();
  }

  function handleTypeHelpClick(pr: PropertyRoute | undefined) {
    if (!pr)
      return;

    ValueLineModal.show({
      type: { name: "string" },
      initialValue: TypeHelpComponent.getExpression("e", pr, "CSharp"),
      valueLineType: "TextArea",
      title: "Property Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    }).done();
  }

  function handleCreateCaseClick() {
    ValueLineModal.show({
      type: { name: "string" },
      initialValue: `CreateCase(new ${p.mainEntityType.cleanName}Entity(){ initial properties here... });`,
      valueLineType: "TextArea",
      title: "Create case Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
      valueHtmlAttributes: { style: { height: "115px" } },
    }).done();
  }
  var ctx = p.ctx;

  return (
    <fieldset>
      <legend>{"Action"}</legend>
      <div className="row">
        <div className="col-sm-7">
          <div className="code-container">
            <div className="btn-group" style={{ marginBottom: "3px" }}>
              <input type="button" className="btn btn-success btn-sm sf-button" value="Create case" onClick={handleCreateCaseClick} />
            </div>
            <pre style={{ border: "0px", margin: "0px" }}>{"public void CustomAction() \n{"}</pre>
            <CSharpCodeMirror script={ctx.value.script ?? ""} onChange={handleCodeChange} />
            <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
          </div>
        </div>
        <div className="col-sm-5">
          <TypeHelpComponent initialType={p.mainEntityType.cleanName} mode="CSharp" onMemberClick={handleTypeHelpClick} />
        </div>
      </div>
    </fieldset>
  );
}
