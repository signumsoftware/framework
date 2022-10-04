import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic } from '@framework/Globals'
import { MemberInfo, getTypeInfo, PropertyRoute, Binding, TypeInfo } from '@framework/Reflection'
import { DynamicValidationEntity, DynamicValidationMessage, DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, ValueLineType, FormGroup } from '@framework/Lines'
import { Entity } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { API, DynamicValidationTestResponse } from '../DynamicValidationClient'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import TypeHelpButtonBarComponent from '../../TypeHelp/TypeHelpButtonBarComponent'
import ValueLineModal from '@framework/ValueLineModal'
import { ContextMenuPosition } from '@framework/SearchControl/ContextMenu'
import PropertyRouteCombo from "../../Basics/Templates/PropertyRouteCombo";
import { ModifiableEntity } from '@framework/Signum.Entities';
import { Lite } from '@framework/Signum.Entities';
import { PropertyRouteEntity } from '@framework/Signum.Entities.Basics';
import { Dropdown, DropdownButton } from 'react-bootstrap';
import { useForceUpdate, useAPI } from '@framework/Hooks'

interface DynamicValidationProps {
  ctx: TypeContext<DynamicValidationEntity>;
}

export default function DynamicValidation(p: DynamicValidationProps) {

  const exampleEntityRef = React.useRef<Entity | undefined>(undefined);
  const dv = p.ctx.value;
  const routeTypeName = useAPI(() => dv.subEntity ? API.routeTypeName(dv.subEntity) : dv.entityType ? Promise.resolve(dv.entityType.className) : Promise.resolve(undefined), [dv.subEntity, dv.entityType]);

  const [response, setResponse] = React.useState<DynamicValidationTestResponse | undefined>(undefined);

  const forceUpdate = useForceUpdate();

  function handleEntityTypeChange() {
    p.ctx.value.subEntity = null;
    exampleEntityRef.current = undefined;
    setResponse(undefined);
    handleCodeChange("");
  }

  function handleCodeChange(newScript: string) {
    const evalEntity = p.ctx.value.eval;
    evalEntity.modified = true;
    evalEntity.script = newScript;
    forceUpdate();
  }

  function getCurrentRoute(rootName: string): PropertyRoute {

    const ctx = p.ctx;
    return ctx.value.subEntity ?
      PropertyRoute.parse(rootName, ctx.value.subEntity.path) :
      PropertyRoute.root(rootName);
  }

  function castToTop(pr: PropertyRoute): string {
    if (pr.propertyRouteType == "Root")
      return "e";
    else if (pr.propertyRouteType == "Mixin")
      return `((${pr.parent!.typeReference().name}Entity)${castToTop(pr.parent!)}.MainEntity)`;
    else
      return `((${pr.parent!.typeReference().name}Entity)${castToTop(pr.parent!)}.GetParentEntity())`;
  }

  function handleTypeHelpClick(pr: PropertyRoute | undefined) {
    if (!pr || !p.ctx.value.entityType)
      return;

    const ppr = getCurrentRoute(p.ctx.value.entityType.cleanName);
    const prefix = castToTop(ppr);

    ValueLineModal.show({
      type: { name: "string" },
      initialValue: TypeHelpComponent.getExpression(prefix, pr, "CSharp"),
      valueLineType: "TextArea",
      title: "Mixin Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    });
  }


  function handleEvaluate() {
    if (exampleEntityRef.current == undefined)
      setResponse(undefined);
    else {
      API.validationTest({
        dynamicValidation: p.ctx.value,
        exampleEntity: exampleEntityRef.current,
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
    const exampleCtx = new TypeContext<Entity | undefined>(undefined, undefined, PropertyRoute.root(typeName), Binding.create(exampleEntityRef, s => s.current));

    return (
      <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} view={true} onView={handleOnView} onChange={handleEvaluate}
        type={{ name: typeName }} label={DynamicViewMessage.ExampleEntity.niceToString()} labelColumns={3} />
    );
  }

  function handleOnView(exampleEntity: Lite<Entity> | ModifiableEntity) {
    return Navigator.view(exampleEntity, { requiresSaveOperation: false, isOperationVisible: eoc => false });
  }

  function renderMessage(res: DynamicValidationTestResponse) {
    if (res.compileError)
      return <div className="alert alert-danger">COMPILE ERROR: {res.compileError}</div >;

    if (res.validationException)
      return <div className="alert alert-danger">EXCEPTION: {res.validationException}</div>;

    const errors = res.validationResult!.filter(vr => !!vr.validationResult);

    return (
      <div>
        {
          (errors.length > 0) ?
            <ul style={{ listStyleType: "none" }} className="alert alert-danger">
              {errors.orderBy(e => e.propertyName).map((e, i) => <li key={i}>{e.propertyName} - {e.validationResult}</li>)}
            </ul> :
            <div className="alert alert-success">VALID: null</div>
        }
      </div>
    );
  }
  var ctx = p.ctx;
  return (
    <div>
      <ValueLine ctx={ctx.subCtx(d => d.name)} />
      <EntityLine ctx={ctx.subCtx(d => d.entityType)} onChange={handleEntityTypeChange} />
      <FormGroup ctx={ctx.subCtx(d => d.subEntity)}>
        {ctx.value.entityType && <PropertyRouteCombo ctx={ctx.subCtx(d => d.subEntity)} type={ctx.value.entityType} onChange={forceUpdate} routes={PropertyRoute.generateAll(ctx.value.entityType.cleanName).filter(a => a.propertyRouteType == "Mixin" || a.typeReference().isEmbedded && !a.typeReference().isCollection)} />}
      </FormGroup>
      {ctx.value.entityType &&
        <div>
          <br />
          <div className="row">
            <div className="col-sm-7">
              {exampleEntityRef && <button className="btn btn-success" onClick={handleEvaluate}><FontAwesomeIcon icon="play" /> Evaluate</button>}
              <div className="code-container">
                <TypeHelpButtonBarComponent typeName={ctx.value.entityType.cleanName} mode="CSharp" ctx={ctx} extraButtons={
                  <PropertyIsHelpComponent route={getCurrentRoute(ctx.value.entityType.cleanName)} />
                } />
                <pre style={{ border: "0px", margin: "0px" }}>{"string PropertyValidate(" + (routeTypeName ?? "ModifiableEntity") + " e, PropertyInfo pi)\n{"}</pre>
                <CSharpCodeMirror script={ctx.value.eval.script ?? ""} onChange={handleCodeChange} />
                <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
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
}

interface PropertyIsHelpComponentProps {
  route: PropertyRoute;
}

export function PropertyIsHelpComponent(p: PropertyIsHelpComponentProps) {

  return (
    <DropdownButton id="property_dropdown" variant="info" title={DynamicValidationMessage.PropertyIs.niceToString()}>
      {Dic.map(p.route.subMembers(), (key, memberInfo) =>
        <Dropdown.Item style={{ paddingTop: "0", paddingBottom: "0" }} key={key} onClick={() => handlePropertyIsClick(key)}>{key}</Dropdown.Item>)}
    </DropdownButton>
  );

  function handlePropertyIsClick(key: string) {

    var text = `if (pi.Name == nameof(e.${key}) && e.${key} == )
{
    return "error";
}

return null;`;

    ValueLineModal.show({
      type: { name: "string" },
      initialValue: text,
      valueLineType: "TextArea",
      title: DynamicValidationMessage.PropertyIs.niceToString(),
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
      valueHtmlAttributes: { style: { height: "200px" } },
    });
  }
}

