import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ValueLine, EntityLine, TypeContext, LiteAutocompleteConfig } from '@framework/Lines'
import { PropertyRoute, Binding } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import { WorkflowConditionEntity, ICaseMainEntity } from '../Signum.Entities.Workflow'
import { WorkflowConditionTestResponse, API, showWorkflowTransitionContextCodeHelp } from '../WorkflowClient'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import ValueLineModal from '@framework/ValueLineModal'
import { useForceUpdate, useAPI, useAPIWithReload } from '@framework/Hooks'

interface WorkflowConditionComponentProps {
  ctx: TypeContext<WorkflowConditionEntity>;
}

interface WorkflowConditionComponentState {
  exampleEntity?: ICaseMainEntity;
  response?: WorkflowConditionTestResponse;
}

export default function WorkflowConditionComponent(p: WorkflowConditionComponentProps) {

  const exampleEntityRef = React.useRef<ICaseMainEntity | undefined>(undefined);

  const [response, reloadResponse] = useAPIWithReload(() => exampleEntityRef.current == undefined ?
    Promise.resolve(undefined) :
    API.conditionTest({
      workflowCondition: p.ctx.value,
      exampleEntity: exampleEntityRef.current,
    }), []);

  const forceUpdate = useForceUpdate();

  function handleMainEntityTypeChange() {
    p.ctx.value.eval!.script = "";
    exampleEntityRef.current = undefined;
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
    });
  }


  function renderTest() {
    const ctx = p.ctx;
    const res = response;
    return (
      <fieldset>
        <legend>TEST</legend>
        {renderExampleEntity(ctx.value.mainEntityType!.cleanName)}
        <br />
        {res && renderMessage(res)}
      </fieldset>
    );
  }

  function renderExampleEntity(typeName: string) {
    const exampleCtx = new TypeContext<ICaseMainEntity | undefined>(undefined, undefined, PropertyRoute.root(typeName), Binding.create(exampleEntityRef, s => s.current));

    return (
      <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} view={true} onView={handleOnView} onChange={forceUpdate}
        type={{ name: typeName }} label="Example Entity" />
    );
  }

  function handleOnView(exampleEntity: ICaseMainEntity) {
    return Navigator.view(exampleEntity, { requiresSaveOperation: false, isOperationVisible: eoc => false });
  }

  function renderMessage(res: WorkflowConditionTestResponse) {
    if (res.compileError)
      return <div className="alert alert-danger">COMPILE ERROR: {res.compileError}</div >;

    if (res.validationException)
      return <div className="alert alert-danger">EXCEPTION: {res.validationException}</div>;

    return (
      <div>
        {
          res.validationResult == true ?
            <div className="alert alert-success">True</div> :
            <div className="alert alert-warning">False</div>

        }
      </div>
    );
  }
  var ctx = p.ctx;

  return (
    <div>
      <ValueLine ctx={ctx.subCtx(wc => wc.name)} />
      <EntityLine ctx={ctx.subCtx(wc => wc.mainEntityType)}
        onChange={handleMainEntityTypeChange}
        autocomplete={new LiteAutocompleteConfig((ac, str) => API.findMainEntityType({ subString: str, count: 5 }, ac))}
        find={false} />
      {ctx.value.mainEntityType &&
        <div>
          <br />
          <div className="row">
            <div className="col-sm-7">
              {exampleEntityRef.current && <button className="btn btn-success" onClick={reloadResponse}><FontAwesomeIcon icon="play" /> Evaluate</button>}
              <div className="btn-group" style={{ marginBottom: "3px" }}>
                <input type="button" className="btn btn-success btn-sm sf-button" value="ctx" onClick={() => showWorkflowTransitionContextCodeHelp()} />
              </div>
              <div className="code-container">
                <pre style={{ border: "0px", margin: "0px" }}>{"bool Evaluate(" + ctx.value.mainEntityType.cleanName + "Entity e, WorkflowTransitionContext ctx)\n{"}</pre>
                <CSharpCodeMirror script={ctx.value.eval!.script ?? ""} onChange={handleCodeChange} />
                <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
              </div>
              {renderTest()}
            </div>
            <div className="col-sm-5">
              <TypeHelpComponent initialType={ctx.value.mainEntityType.cleanName} mode="CSharp" onMemberClick={handleTypeHelpClick} />
            </div>
          </div>
        </div>
      }
    </div>
  );
}

