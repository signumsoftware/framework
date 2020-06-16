import * as React from 'react'
import { ValueLine, EntityLine, TypeContext, LiteAutocompleteConfig } from '@framework/Lines'
import { PropertyRoute } from '@framework/Reflection'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import { WorkflowTimerConditionEntity } from '../Signum.Entities.Workflow'
import { API } from '../WorkflowClient'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import ValueLineModal from '@framework/ValueLineModal'
import { useForceUpdate } from '@framework/Hooks'

interface WorkflowTimerConditionComponentProps {
  ctx: TypeContext<WorkflowTimerConditionEntity>;
}

export default function WorkflowTimerConditionComponent(p : WorkflowTimerConditionComponentProps){
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

    ValueLineModal.show({
      type: { name: "string" },
      initialValue: TypeHelpComponent.getExpression("e", pr, "CSharp"),
      valueLineType: "TextArea",
      title: "Property Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    }).done();
  }
  var ctx = p.ctx;

  return (
    <div>
      <ValueLine ctx={ctx.subCtx(wc => wc.name)} />
      <EntityLine ctx={ctx.subCtx(wc => wc.mainEntityType)}
        onChange={handleMainEntityTypeChange}
        autocomplete={new LiteAutocompleteConfig((ac, str) => API.findMainEntityType({ subString: str, count: 5 }, ac), false, false)}
        find={false} />
      {ctx.value.mainEntityType &&
        <div>
          <br />
          <div className="row">
            <div className="col-sm-7">
              <div className="code-container">
                <pre style={{ border: "0px", margin: "0px" }}>{"boolean Evaluate(CaseActivityEntity ca, " + ctx.value.mainEntityType.cleanName + "Entity e, DateTime now)\n{"}</pre>
                <CSharpCodeMirror script={ctx.value.eval!.script ?? ""} onChange={handleCodeChange} />
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

