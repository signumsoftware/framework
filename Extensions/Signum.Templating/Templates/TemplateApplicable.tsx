import * as React from 'react'
import { PropertyRoute } from '@framework/Reflection'
import { TextAreaLine, TypeContext } from '@framework/Lines'
import { Finder } from '@framework/Finder'
import CSharpCodeMirror from '../../Signum.CodeMirror/CSharpCodeMirror'
import EvalScriptTS from '../../Signum.Eval/EvalScriptTS'
import AutoLineModal from '@framework/AutoLineModal'
import { TemplateApplicableEval } from "../Signum.Templating";
import TypeHelpButtonBarComponent from "../../Signum.Eval/TypeHelp/TypeHelpButtonBarComponent";
import TypeHelpComponent from "../../Signum.Eval/TypeHelp/TypeHelpComponent";
import { QueryEntity } from "@framework/Signum.Basics";
import { useForceUpdate, useAPI } from '@framework/Hooks'

interface TemplateApplicableProps {
  ctx: TypeContext<TemplateApplicableEval>;
  query: QueryEntity;
}

export default function TemplateApplicable(p: TemplateApplicableProps): React.JSX.Element | null {

  const typeName = useAPI(() => p.query && Finder.getQueryDescription(p.query.key).then(qd => qd.columns["Entity"].type.name.split(",")[0] ?? "Entity"), [p.query.key]);

  const forceUpdate = useForceUpdate();

  function handleCodeChange(newScript: string) {
    const evalEntity = p.ctx.value;
    evalEntity.modified = true;
    evalEntity.scriptCS = newScript;
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
  if (!typeName)
    return null;

  return (
    <div>

      <div>
        <br />
        <div className="row">
          <div className="col-sm-7">
            <div className="code-container">
              <TypeHelpButtonBarComponent typeName={typeName} mode="CSharp" ctx={p.ctx} />
              <pre style={{ border: "0px", margin: "0px" }}>{"bool IsApplicable(" + (typeName ? (typeName + "Entity") : "Entity?") + " e)\n{"}</pre>
              <CSharpCodeMirror script={ctx.value.scriptCS ?? ""} onChange={handleCodeChange} />
              <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
              <EvalScriptTS value={ctx.value.scriptTS} onChange={v => { ctx.value.scriptTS = v; ctx.value.modified = true; forceUpdate(); }} />
            </div>
          </div>
          <div className="col-sm-5">
            <TypeHelpComponent initialType={typeName} mode="CSharp" onMemberClick={handleTypeHelpClick} />
          </div>
        </div>
      </div>
    </div>
  );
}
