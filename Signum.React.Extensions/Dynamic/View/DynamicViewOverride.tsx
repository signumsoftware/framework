import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { DynamicViewOverrideEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { EntityLine, TypeContext, FormGroup } from '@framework/Lines'
import { Entity, JavascriptMessage, SaveChangesMessage } from '@framework/Signum.Entities'
import { Binding, PropertyRoute, ReadonlyBinding } from '@framework/Reflection'
import JavascriptCodeMirror from '../../Codemirror/JavascriptCodeMirror'
import * as DynamicViewClient from '../DynamicViewClient'
import * as Navigator from '@framework/Navigator'
import { ViewReplacer } from '@framework/Frames/ReactVisitor';
import * as TypeHelpClient from '../../TypeHelp/TypeHelpClient'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import TypeHelpButtonBarComponent from '../../TypeHelp/TypeHelpButtonBarComponent'
import ValueLineModal from '@framework/ValueLineModal'
import MessageModal from '@framework/Modals/MessageModal'
import * as Nodes from '../../Dynamic/View/Nodes';
import { Dropdown, DropdownButton } from 'react-bootstrap';
import { useForceUpdate, useAPI } from '@framework/Hooks'
import { ModulesHelp } from "./ModulesHelp";
import { EntityFrame } from '@framework/TypeContext'
import { ErrorBoundary } from '@framework/Components'

interface DynamicViewOverrideComponentProps {
  ctx: TypeContext<DynamicViewOverrideEntity>;
}

export default function DynamicViewOverrideComponent(p: DynamicViewOverrideComponentProps) {

  const typeName: string | null = p.ctx.value.entityType?.cleanName;
  const typeHelp = useAPI(() => typeName ? TypeHelpClient.API.typeHelp(typeName, "CSharp") : Promise.resolve(undefined), [typeName]);
  const viewNames = useAPI(() => typeName ? Navigator.viewDispatcher.getViewNames(typeName) : Promise.resolve(undefined), [typeName]);

  const scriptChangedRef = React.useRef(false);

  const forceUpdate = useForceUpdate();

  const exampleEntityRef = React.useRef<Entity | undefined>(undefined);
  const componentTypeRef = React.useRef<React.ComponentType<{ ctx: TypeContext<Entity> }> | null>(null);
  function setComponentType(ct: React.ComponentType<{ ctx: TypeContext<Entity> }> | null) {
    componentTypeRef.current = ct;
    forceUpdate();
  }


  const [syntaxError, setSyntaxError] = React.useState<string | undefined>(undefined);
  const [viewOverride, setViewOverride] = React.useState<{ func: (vr: ViewReplacer<Entity>) => void } | undefined>(undefined);


  function handleTypeRemove() {
    if (scriptChangedRef.current)
      return MessageModal.show({
        title: SaveChangesMessage.ThereAreChanges.niceToString(),
        message: JavascriptMessage.loseCurrentChanges.niceToString(),
        buttons: "yes_no",
        icon: "warning",
        style: "warning"
      }).then(result => { return result == "yes" });

    return Promise.resolve(true);
  }

  function handleRemoveClick(lambda: string) {
    setTimeout(() => showPropmt("Remove", `vr.removeLine(${lambda})`), 0);
  }

  function handleInsertBeforeClick(lambda: string) {
    setTimeout(() => showPropmt("InsertBefore", `vr.insertBeforeLine(${lambda}, ctx => [yourElement]);`), 0);
  }

  function handleInsertAfterClick(lambda: string) {
    setTimeout(() => showPropmt("InsertAfter", `vr.insertAfterLine(${lambda}, ctx => [yourElement]);`), 0);
  }

  function handleRenderContextualMenu(pr: PropertyRoute) {
    const lambda = "e => " + TypeHelpComponent.getExpression("e", pr, "TypeScript");
    return (
      <Dropdown.Item>
        <Dropdown.Header>{pr.propertyPath()}</Dropdown.Header>
        <Dropdown.Divider />
        <Dropdown.Item onClick={() => handleRemoveClick(lambda)}><FontAwesomeIcon icon="trash" />&nbsp; Remove</Dropdown.Item>
        <Dropdown.Item onClick={() => handleInsertBeforeClick(lambda)}><FontAwesomeIcon icon="arrow-up" />&nbsp; Insert Before</Dropdown.Item>
        <Dropdown.Item onClick={() => handleInsertAfterClick(lambda)}><FontAwesomeIcon icon="arrow-down" />&nbsp; Insert After</Dropdown.Item>
      </Dropdown.Item>
    );
  }

  function handleTypeHelpClick(pr: PropertyRoute | undefined) {
    if (!pr || !pr.member || !pr.parent || pr.parent.propertyRouteType != "Mixin")
      return;

    var node = Nodes.NodeConstructor.appropiateComponent(pr.member, pr.propertyPath());
    if (!node)
      return;

    const expression = TypeHelpComponent.getExpression("o", pr, "TypeScript");
    const text = `modules.React.createElement(${node.kind}, { ctx: ctx.subCtx(o => ${expression}) })`;

    ValueLineModal.show({
      type: { name: "string" },
      initialValue: text,
      valueLineType: "TextArea",
      title: "Mixin Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    });
  }


  function handleViewNameChange(e: React.SyntheticEvent<HTMLSelectElement>) {
    p.ctx.value.viewName = (e.currentTarget as HTMLSelectElement).value != "" ? (e.currentTarget as HTMLSelectElement).value : null;
    p.ctx.value.modified = true;
    forceUpdate();
  };

  function renderTest() {
    const ctx = p.ctx;
    return (
      <div>
        {exampleEntityRef.current && componentTypeRef.current &&
          <ErrorBoundary>
          <RenderWithReplacements entity={exampleEntityRef.current}
            componentType={componentTypeRef.current}
            viewOverride={viewOverride && viewOverride.func} />
          </ErrorBoundary>
          }
      </div>
    );
  }

  function renderExampleEntity(typeName: string) {
    const exampleCtx = new TypeContext<Entity | undefined>(undefined, undefined, PropertyRoute.root(typeName), Binding.create(exampleEntityRef, s => s.current));

    return (
      <div className="code-container">
        <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} view={true} onView={handleOnView} onChange={handleEntityChange} formGroupStyle="Basic"
          type={{ name: typeName }} label={DynamicViewMessage.ExampleEntity.niceToString()} />
      </div>
    );
  }

  function handleOnView(exampleEntity: Entity) {
    return Navigator.view(exampleEntity, { requiresSaveOperation: false, isOperationVisible: eoc => false });
  }

  function handleCodeChange(newCode: string) {
    var dvo = p.ctx.value;

    if (dvo.script != newCode) {
      dvo.script = newCode;
      dvo.modified = true;
      scriptChangedRef.current = true;
      compileFunction();
    };
  }

  function handleEntityChange() {
    if (!exampleEntityRef.current)
      setComponentType(null);
    else {

      const entity = exampleEntityRef.current;
      const settings = Navigator.getSettings(entity.Type);

      if (!settings)
        setComponentType(null);
      else {
        const ctx = p.ctx;
        return Navigator.viewDispatcher.getViewPromise(entity, ctx.value.viewName ?? undefined).promise.then(func => {
          var tempCtx = new TypeContext(undefined, undefined, PropertyRoute.root(entity.Type), new ReadonlyBinding(entity, "example"));
          var re = func(tempCtx);
          setComponentType(re.type as React.ComponentType<{ ctx: TypeContext<Entity> }>);
          compileFunction();
        });
      }
    }
  }

  function compileFunction() {
    setSyntaxError(undefined);
    setViewOverride(undefined);

    const dvo = p.ctx.value;
    let func: (rep: ViewReplacer<Entity>) => void;
    try {
      func = DynamicViewClient.asOverrideFunction(dvo);
      setViewOverride({ func });
    } catch (e) {
      setSyntaxError((e as Error).message);
      return;
    }
  }

  function renderEditor() {
    const ctx = p.ctx;
    return (
      <div className="code-container">
        <div className="btn-toolbar btn-toolbar-small">
          {viewNames && renderViewNameButtons()}
          {allExpressions().length > 0 && renderExpressionsButtons()}
          <TypeHelpButtonBarComponent typeName={ctx.value.entityType!.cleanName} mode="TypeScript" ctx={ctx} />
        </div>
        <pre style={{ border: "0px", margin: "0px", overflow: "visible" }}>{`(vr: ViewReplacer<${ctx.value.entityType!.className}>, `}
          <div style={{ display: "inline-flex" }}>
            <ModulesHelp cleanName={ctx.value.entityType!.className} />{") =>"}
          </div>
        </pre>
        <JavascriptCodeMirror code={ctx.value.script ?? ""} onChange={handleCodeChange} />
        {syntaxError && <div className="alert alert-danger">{syntaxError}</div>}
      </div>
    );
  }

  function handleViewNameClick(viewName: string) {
    showPropmt("View", `modules.React.createElement(RenderEntity, {ctx: ctx, getViewPromise: ctx => "${viewName}"})`);
  }

  function renderViewNameButtons() {
    return (
      <DropdownButton variant="success" title="View Names" id="view_dropdown">
        {viewNames!.map((vn, i) =>
          <Dropdown.Item key={i} onClick={() => handleViewNameClick(vn)}>{vn}</Dropdown.Item>)}
      </DropdownButton>
    );
  }

  function allExpressions() {
    if (!typeHelp)
      return [];

    return typeHelp.members.filter(m => m.name && m.isExpression == true);
  }

  function handleExpressionClick(member: TypeHelpClient.TypeMemberHelp) {
    var paramValue = member.cleanTypeName ? `queryName : "${member.cleanTypeName}Entity"` : `valueToken: "Entity.${member.name}"`;
    showPropmt("Expression", `modules.React.createElement(SearchValueLine, {ctx: ctx, ${paramValue}})`);
  }

  function renderExpressionsButtons() {
    return (
      <DropdownButton variant="warning" title="Expressions" id="expression_dropdown">
        {allExpressions().map((m, i) =>
          <Dropdown.Item key={i} onClick={() => handleExpressionClick(m)}>{m.name}</Dropdown.Item>)}
      </DropdownButton>
    );
  }

  function showPropmt(title: string, text: string) {
    ValueLineModal.show({
      type: { name: "string" },
      initialValue: text,
      valueLineType: "TextArea",
      title: title,
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    });
  }
  const ctx = p.ctx;

  return (
    <div>
      <EntityLine ctx={ctx.subCtx(a => a.entityType)} onChange={forceUpdate} onRemove={handleTypeRemove} />
      {
        ctx.value.entityType && viewNames &&
        <FormGroup ctx={ctx.subCtx(d => d.viewName)} label={ctx.niceName(d => d.viewName)}>
          {
            <select value={ctx.value.viewName ? ctx.value.viewName : ""} className="form-select" onChange={handleViewNameChange}>
              <option value="">{" - "}</option>
              {(viewNames ?? []).map((v, i) => <option key={i} value={v}>{v}</option>)}
            </select>
          }
        </FormGroup>
      }

      {ctx.value.entityType &&
        <div>
          <br />
          <div className="row">
            <div className="col-sm-7">
              {renderExampleEntity(ctx.value.entityType!.cleanName)}
              {renderEditor()}
            </div>
            <div className="col-sm-5">
              <TypeHelpComponent
                initialType={ctx.value.entityType.cleanName}
                mode="TypeScript"
                renderContextMenu={handleRenderContextualMenu}
                onMemberClick={handleTypeHelpClick} />
              <br />
            </div>
          </div>
          <hr />
          {renderTest()}
        </div>
      }
    </div>
  );
}


interface RenderWithReplacementsProps {
  entity: Entity;
  componentType: React.ComponentType<{ ctx: TypeContext<Entity> }>;
  viewOverride?: (vr: ViewReplacer<Entity>) => void;
}

export function RenderWithReplacements(p: RenderWithReplacementsProps) {

  const originalRenderRef = React.useRef<Function | undefined>(undefined);

  React.useEffect(() => {
    if (p.componentType.prototype.render)
      originalRenderRef.current = p.componentType.prototype.render;

    return () => { p.componentType.prototype.render = originalRenderRef.current; };
  }, []);

  function applyViewOverrides(vo?: (vr: ViewReplacer<Entity>) => void) {
    if (p.componentType.prototype.render)
      DynamicViewClient.unPatchComponent((p.componentType as React.ComponentClass<{ ctx: TypeContext<Entity> }>));

    if (!vo)
      return p.componentType;

    if (p.componentType.prototype.render) {
      DynamicViewClient.patchComponent((p.componentType as React.ComponentClass<{ ctx: TypeContext<Entity> }>), vo);
      return p.componentType;
    }
    else
      return Navigator.surroundFunctionComponent((p.componentType as React.FunctionComponent<{ ctx: TypeContext<Entity> }>), [{ override: vo }]);
  }

  var frame = { refreshCount: 0 } as EntityFrame;
  
  var ctx = new TypeContext(undefined, { frame: frame }, PropertyRoute.root(p.entity.Type), new ReadonlyBinding(p.entity, "example"));

  return React.createElement(applyViewOverrides(p.viewOverride), { ctx: ctx });
}
