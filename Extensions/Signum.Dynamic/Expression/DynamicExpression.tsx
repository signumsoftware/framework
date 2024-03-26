import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { AutoLine, EntityCombo, EntityLine, TextAreaLine, TypeContext } from '@framework/Lines'
import { PropertyRoute, Binding, isTypeEntity } from '@framework/Reflection'
import { Navigator } from '@framework/Navigator'
import CSharpCodeMirror from '../../Signum.CodeMirror/CSharpCodeMirror'
import { Entity } from '@framework/Signum.Entities'
import { DynamicExpressionClient, DynamicExpressionTestResponse } from '../DynamicExpressionClient'
import { TypeHelpClient } from '../../Signum.Eval/TypeHelp/TypeHelpClient';
import TypeHelpComponent from '../../Signum.Eval/TypeHelp/TypeHelpComponent'
import AutoLineModal from '@framework/AutoLineModal'
import { ModifiableEntity } from '@framework/Signum.Entities';
import { Lite } from '@framework/Signum.Entities';
import { Typeahead } from '@framework/Components';
import { useForceUpdate } from '@framework/Hooks'
import { DynamicExpressionEntity } from '../Signum.Dynamic.Expression'
import TextArea from '@framework/Components/TextArea'

interface DynamicExpressionComponentProps {
  ctx: TypeContext<DynamicExpressionEntity>;
}

export default function DynamicExpressionComponent(p: DynamicExpressionComponentProps) {




  const exampleEntity = React.useRef<Entity | null>(null);
  const [response, setResponse] = React.useState<DynamicExpressionTestResponse | undefined>(undefined);

  const forceUpdate = useForceUpdate();

  function handleCodeChange(newScript: string) {
    const entity = p.ctx.value;
    entity.body = newScript;
    entity.modified = true;
    forceUpdate();
  }

  function handleTypeHelpClick(pr: PropertyRoute | undefined) {
    if (!pr)
      return;

    AutoLineModal.show({
      type: { name: "string" },
      initialValue: TypeHelpComponent.getExpression("e", pr, "CSharp"),
      customComponent: p => <TextAreaLine {...p}/>,
      title: "Property Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
    });
  }


  function handleGetItems(query: string, type: "ReturnType" | "FromType") {
    return TypeHelpClient.DynamicExpressionClient.API.autocompleteType({ query: query, limit: 5, includeBasicTypes: true, includeEntities: true, includeModelEntities: type == "ReturnType", includeQueriable: type == "ReturnType" });
  }

  function renderTypeAutocomplete(ctx: TypeContext<string | null | undefined>) {
    return (
      <Typeahead
        inputAttrs={{
          className: "input-code",
          placeholder: ctx.niceName(),
          size: ctx.value ? ctx.value.length : ctx.niceName().length
        }}
        getItems={query => handleGetItems(query, ctx.propertyRoute!.member!.name == "ReturnType" ? "ReturnType" : "FromType")}
        value={ctx.value ?? undefined}
        onChange={txt => { ctx.value = txt; forceUpdate(); }} />
    );
  }

  function renderInput(ctx: TypeContext<string | null | undefined>) {
    return (
      <input type="text"
        className="input-code"
        placeholder={ctx.niceName()}
        size={ctx.value ? ctx.value.length : ctx.niceName().length}
        value={ctx.value ?? undefined}
        onChange={e => {
          ctx.value = (e.currentTarget as HTMLInputElement).value;
          forceUpdate();
        }} />
    );
  }

  function handleEvaluate() {
    if (exampleEntity.current == undefined)
      setResponse(undefined);
    else {
      DynamicExpressionClient.API.expressionTest({
        dynamicExpression: p.ctx.value,
        exampleEntity: exampleEntity.current,
      })
        .then(r => setResponse(r));
    }
  }

  function renderTest(cleanFromType: string) {
    const res = response;

    return (
      <fieldset>
        <legend>TEST</legend>
        {renderExampleEntity(cleanFromType)}
        {res && renderMessage(res)}
      </fieldset>
    );
  }

  function renderExampleEntity(typeName: string) {
    const exampleCtx = new TypeContext<Entity | null>(undefined, undefined, PropertyRoute.root(typeName), Binding.create(exampleEntity, s => s.current));

    return (
      <EntityCombo ctx={exampleCtx} create={true} find={true} remove={true} view={true} onView={handleOnView} onChange={handleEvaluate}
        type={{ name: typeName }} label="Example Entity" />
    );
  }

  function handleOnView(exampleEntity: Entity) {
    return Navigator.view<Entity>(exampleEntity, { requiresSaveOperation: false, isOperationVisible: eoc => false });
  }

  function renderMessage(res: DynamicExpressionTestResponse) {
    if (res.compileError)
      return <div className="alert alert-danger">COMPILE ERROR: {res.compileError}</div >;

    if (res.validationException)
      return <div className="alert alert-danger">EXCEPTION: {res.validationException}</div>;

    return <div className="alert alert-success">VALUE: {res.validationResult}</div>;
  }

  var ctx = p.ctx;

  let cleanFromType = ctx.value.fromType || undefined;

  if (cleanFromType?.endsWith("Entity"))
    cleanFromType = cleanFromType.beforeLast("Entity");

  if (cleanFromType && !isTypeEntity(cleanFromType))
    cleanFromType = undefined;

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(dt => dt.translation)} />
      <div className="row">
        <div className="col-sm-6">
          <AutoLine ctx={ctx.subCtx(dt => dt.format)} labelColumns={4}
            helpText={<span>See <a href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/formatting-types" target="_blank">formatting types</a></span>} />
        </div>
        <div className="col-sm-6">
          <AutoLine ctx={ctx.subCtx(dt => dt.unit)} labelColumns={4} />
        </div>
      </div>
      <br />
      <div className="row">
        <div className="col-sm-7">
          {exampleEntity && <button className="btn btn-success" onClick={handleEvaluate}><FontAwesomeIcon icon="play"></FontAwesomeIcon> Evaluate</button>}
          <div className="code-container">
            <pre style={{ border: "0px", margin: "0px", display: "flex", overflow: "visible" }}>
              {renderTypeAutocomplete(ctx.subCtx(dt => dt.returnType))}&nbsp;{renderInput(ctx.subCtx(dt => dt.name))}&nbsp;({renderTypeAutocomplete(ctx.subCtx(dt => dt.fromType))}e) {"=>"}
            </pre>
            <CSharpCodeMirror script={ctx.value.body ?? ""} onChange={handleCodeChange} />
          </div>
          {ctx.value.body && cleanFromType && renderTest(cleanFromType)}
        </div>
        <div className="col-sm-5">
          <TypeHelpComponent initialType={cleanFromType} mode="CSharp" onMemberClick={handleTypeHelpClick} />
        </div>
      </div>
    </div>
  );
}

