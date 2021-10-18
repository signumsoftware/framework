import * as React from 'react'
import { ValueLine, EntityLine, TypeContext } from '@framework/Lines'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import { WorkflowScriptEntity } from '../Signum.Entities.Workflow'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import ValueLineModal from '@framework/ValueLineModal'
import { useForceUpdate } from '@framework/Hooks'

interface WorkflowScriptComponentProps {
  ctx: TypeContext<WorkflowScriptEntity>;
}

export default function WorkflowScriptComponent(p : WorkflowScriptComponentProps){
  const forceUpdate = useForceUpdate();
  function handleMainEntityTypeChange() {
    p.ctx.value.eval!.script = "";
    forceUpdate();
  }

  function handleScriptChange(newScript: string) {
    const evalEntity = p.ctx.value.eval!;
    evalEntity.script = newScript;
    evalEntity.modified = true;
    forceUpdate();
  }

  function handleCustomTypesChange(newScript: string) {
    const scriptEval = p.ctx.value.eval!;
    scriptEval.customTypes = newScript;
    scriptEval.modified = true;
    forceUpdate();
  }



  function handleRestClick() {
    ValueLineModal.show({
      type: { name: "string" },
      initialValue: `// REST
var response = HttpClient.Post<MyResponse>("Your URL", new { paramName = e.[Property Name], ... });
e.[Property Name] = response.[Property Name];

class MyResponse {}`,
      valueLineType: "TextArea",
      title: "REST Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
      valueHtmlAttributes: { style: { height: "115px" } },
    }).done();
  }

  function handleSoapClick() {
    ValueLineModal.show({
      type: { name: "string" },
      initialValue: `// SOAP
var lib = Assembly.Load("[Assembly full path name]").GetType("[Type Name]").GetMethod("[Method Name]").Invoke(e.[Property Name]);
e.[Property Name] = lib;`,
      valueLineType: "TextArea",
      title: "SOAP Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
      valueHtmlAttributes: { style: { height: "115px" } },
    }).done();
  }

  function handleCtxClick() {
    const hint = "WorkflowScriptContext Members";
    ValueLineModal.show({
      type: { name: "string" },
      initialValue: `// ${hint}
CaseActivityEntity CaseActivity;
int RetryCount;`,
      valueLineType: "TextArea",
      title: hint,
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
      valueHtmlAttributes: { style: { height: "115px" } },
    }).done();
  }

  function handleTryCatchClick() {
    ValueLineModal.show({
      type: { name: "string" },
      initialValue: `try
{

}
catch (Exception e)
{
    throw e;
}`,
      valueLineType: "TextArea",
      title: "Try/Catch block",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
      valueHtmlAttributes: { style: { height: "180px" } },
    }).done();
  }

  const ctx = p.ctx;
  return (
    <div>
      <ValueLine ctx={ctx.subCtx(ws => ws.name)} />
      <EntityLine ctx={ctx.subCtx(ws => ws.mainEntityType)} onChange={handleMainEntityTypeChange} />

      {ctx.value.mainEntityType &&
        <div>
          <br />
          <div className="row">
            <div className="col-sm-7">
              <div className="btn-group" style={{ marginBottom: "3px" }}>
                <input type="button" className="btn btn-danger btn-sm sf-button" value="try-catch" onClick={handleTryCatchClick} />
                <input type="button" className="btn btn-success btn-sm sf-button" value="REST" onClick={handleRestClick} />
                <input type="button" className="btn btn-warning btn-sm sf-button" value="SOAP" onClick={handleSoapClick} />
                <input type="button" className="btn btn-danger btn-sm sf-button" value="ctx" onClick={handleCtxClick} />
              </div>
              <div className="code-container">
                <pre style={{ border: "0px", margin: "0px" }}>{`public static void ScriptCode(${ctx.value.mainEntityType.cleanName}Entity e, WorkflowScriptContext ctx)\n{`}</pre>
                <CSharpCodeMirror script={ctx.value.eval!.script ?? ""} onChange={handleScriptChange} />
                <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
              </div>
              <div className="code-container">
                <pre style={{ border: "0px", margin: "0px" }}>{`namespace MyCustomTypes {`}</pre>
                <CSharpCodeMirror script={ctx.value.eval!.customTypes ?? ""} onChange={handleCustomTypesChange} />
                <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
              </div>
            </div>
            <div className="col-sm-5">
              <TypeHelpComponent initialType={ctx.value.mainEntityType.cleanName} mode="CSharp" />
            </div>
          </div>
        </div>}
    </div>
  );
}

