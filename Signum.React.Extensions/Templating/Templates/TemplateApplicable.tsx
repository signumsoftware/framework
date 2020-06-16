import * as React from 'react'
import { PropertyRoute } from '@framework/Reflection'
import { TypeContext } from '@framework/Lines'
import * as Finder from '@framework/Finder'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import TypeHelpButtonBarComponent from '../../TypeHelp/TypeHelpButtonBarComponent'
import ValueLineModal from '@framework/ValueLineModal'
import { TemplateApplicableEval } from "../Signum.Entities.Templating";
import { QueryEntity } from "@framework/Signum.Entities.Basics";
import { useForceUpdate, useAPI } from '@framework/Hooks'

interface TemplateApplicableProps {
  ctx: TypeContext<TemplateApplicableEval>;
  query: QueryEntity;
}

export default function TemplateApplicable(p: TemplateApplicableProps) {

  const typeName = useAPI(() => Finder.getQueryDescription(p.query.key).then(qd => qd.columns["Entity"].type.name.split(",")[0] ?? "Entity"), [p.query.key]);

  const forceUpdate = useForceUpdate();

  function handleCodeChange(newScript: string) {
    const evalEntity = p.ctx.value;
    evalEntity.modified = true;
    evalEntity.script = newScript;
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
              <pre style={{ border: "0px", margin: "0px" }}>{"bool IsApplicable(" + typeName + "Entity e)\n{"}</pre>
              <CSharpCodeMirror script={ctx.value.script ?? ""} onChange={handleCodeChange} />
              <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
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
