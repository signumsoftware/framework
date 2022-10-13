import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ValueLine, EntityLine, TypeContext, FormGroup, ValueLineType } from '@framework/Lines'
import { PropertyRoute, Binding } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import { Entity } from '@framework/Signum.Entities'
import { DynamicTypeConditionEntity } from '../Signum.Entities.Dynamic'
import { DynamicTypeConditionTestResponse, API } from '../DynamicTypeConditionClient'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import ValueLineModal from '@framework/ValueLineModal'
import { useForceUpdate } from '@framework/Hooks'

interface DynamicTypeConditionComponentProps {
  ctx: TypeContext<DynamicTypeConditionEntity>;
}

export default function DynamicTypeConditionComponent(p: DynamicTypeConditionComponentProps) {


  const [response, setResponse] = React.useState<DynamicTypeConditionTestResponse | undefined>(undefined);
  const exampleEntityRef = React.useRef<Entity | undefined>(undefined);
  const forceUpdate = useForceUpdate();

  function handleEntityTypeChange() {
    exampleEntityRef.current = undefined;
    setResponse(undefined);
    handleCodeChange("");
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

  var ctx = p.ctx;

  return (
    <div>
      <EntityLine ctx={ctx.subCtx(dt => dt.symbolName)} />
      <EntityLine ctx={ctx.subCtx(dt => dt.entityType)} onChange={handleEntityTypeChange} />
      {ctx.value.entityType &&
        <div>
          <br />
          <div className="row">
            <div className="col-sm-7">

              {exampleEntityRef && <button className="btn btn-success" onClick={handleEvaluate}><FontAwesomeIcon icon="play" /> Evaluate</button>}

              <div className="code-container">
                <pre style={{ border: "0px", margin: "0px" }}>{"boolean Evaluate(" + ctx.value.entityType.cleanName + "Entity e) =>"}</pre>
                <CSharpCodeMirror script={ctx.value.eval!.script ?? ""} onChange={handleCodeChange} />
              </div>
              {renderTest()}
            </div>
            <div className="col-sm-5">
              <TypeHelpComponent initialType={ctx.value.entityType.cleanName} mode="CSharp" onMemberClick={handleTypeHelpClick} />
            </div>
          </div>
        </div>}
    </div>
  );


  function handleEvaluate() {
    if (exampleEntityRef == undefined)
      setResponse(undefined);
    else {
      API.typeConditionTest({
        dynamicTypeCondition: p.ctx.value,
        exampleEntity: exampleEntityRef.current!,
      })
        .then(r => setResponse(r));
    }
  }

  function renderTest() {
    const ctx = p.ctx;
    const res = response;
    return (
      <fieldset>
        <legend>TEST</legend>
        {renderExampleEntity(ctx.value.entityType!.cleanName)}
        {res && renderMessage(res)}
      </fieldset>
    );
  }

  function renderExampleEntity(typeName: string) {
    const exampleCtx = new TypeContext<Entity | undefined>(undefined, undefined, PropertyRoute.root(typeName), Binding.create(exampleEntityRef, e => e.current));

    return (
      <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} view={true} onView={handleOnView} onChange={handleEvaluate}
        type={{ name: typeName }} label="Example Entity" />
    );
  }

  function handleOnView(exampleEntity: Entity) {
    return Navigator.view(exampleEntity, { requiresSaveOperation: false, isOperationVisible: eoc => false });
  }

  function renderMessage(res: DynamicTypeConditionTestResponse) {
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
}

