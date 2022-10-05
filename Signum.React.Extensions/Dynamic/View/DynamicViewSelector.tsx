import * as React from 'react'
import { classes } from '@framework/Globals'
import { DynamicViewSelectorEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { EntityLine, TypeContext } from '@framework/Lines'
import { Entity, JavascriptMessage, is, SaveChangesMessage } from '@framework/Signum.Entities'
import { Binding, PropertyRoute } from '@framework/Reflection'
import JavascriptCodeMirror from '../../Codemirror/JavascriptCodeMirror'
import * as DynamicViewClient from '../DynamicViewClient'
import * as Navigator from '@framework/Navigator'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import ValueLineModal from '@framework/ValueLineModal'
import MessageModal from '@framework/Modals/MessageModal'
import { Dropdown, DropdownButton } from 'react-bootstrap';
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { ModulesHelp } from "./ModulesHelp";


export default function DynamicViewSelectorComponent(p: { ctx: TypeContext<DynamicViewSelectorEntity> }) {

  const forceUpdate = useForceUpdate();
  const viewNames = useAPI(() => !p.ctx.value.entityType ? Promise.resolve(undefined) : Navigator.viewDispatcher.getViewNames(p.ctx.value.entityType!.cleanName), [p.ctx.value.entityType]);

  const exampleEntityRef = React.useRef<Entity | undefined>(undefined);
  const scriptChangedRef = React.useRef(false);

  const [syntaxError, setSyntaxError] = React.useState<string | undefined>(undefined);
  const [testResult, setTestResult] = React.useState<{ type: "ERROR", error: string } | { type: "RESULT", result: string | undefined } | undefined>(undefined);

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

  function handleTypeHelpClick(pr: PropertyRoute | undefined) {
    if (!pr)
      return;

    ValueLineModal.show({
      type: { name: "string" },
      initialValue: TypeHelpComponent.getExpression("e", pr, "TypeScript"),
      valueLineType: "TextArea",
      title: "Property Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    });
  }


  function renderTest() {
    const ctx = p.ctx;
    const res = testResult;
    return (
      <fieldset>
        <legend>TEST</legend>
        {renderExampleEntity(ctx.value.entityType!.cleanName)}
        {res?.type == "ERROR" && <div className="alert alert-danger">ERROR: {res.error}</div>}
        {res?.type == "RESULT" && <div className={classes("alert", getTestAlertType(res.result))}>RESULT: {res.result === undefined ? "undefined" : JSON.stringify(res.result)}</div>}
      </fieldset>
    );
  }

  function getTestAlertType(result: string | undefined) {
    if (!result)
      return "alert-danger";

    if (allViewNames().contains(result))
      return "alert-success";

    return "alert-danger";
  }

  function renderExampleEntity(typeName: string) {
    const exampleCtx = new TypeContext<Entity | undefined>(undefined, undefined, PropertyRoute.root(typeName), Binding.create(exampleEntityRef, s => s.current));

    return (
      <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} view={true} onView={handleOnView} onChange={() => evaluateTest()}
        type={{ name: typeName }} label={DynamicViewMessage.ExampleEntity.niceToString()} />
    );
  }

  function handleOnView(exampleEntity: Entity) {
    return Navigator.view(exampleEntity, { requiresSaveOperation: false, isOperationVisible: eoc => false });
  }

  function handleCodeChange(newCode: string) {
    var dvs = p.ctx.value;

    if (dvs.script != newCode) {
      dvs.script = newCode;
      dvs.modified = true;
      scriptChangedRef.current = true;
      evaluateTest();
    };
  }

  function evaluateTest() {
    setSyntaxError(undefined);
    setTestResult(undefined);

    const dvs = p.ctx.value;
    let func: (e: Entity) => any;
    try {
      func = DynamicViewClient.asSelectorFunction(dvs);
    } catch (e) {
      setSyntaxError((e as Error).message);
      return;
    }

    if (exampleEntityRef.current) {
      try {
        setTestResult({
          type: "RESULT",
          result: func(exampleEntityRef.current!)
        });
      } catch (e) {
        setTestResult({
          type: "ERROR",
          error: (e as Error).message
        });
      }
    }
  }

  function allViewNames() {
    return ["NEW", "STATIC", "CHOOSE"].concat(viewNames ?? []);
  }

  function handleViewNameClick(viewName: string) {
    ValueLineModal.show({
      type: { name: "string" },
      initialValue: `"${viewName}"`,
      valueLineType: "TextArea",
      title: "View Name",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    });
  }

  function renderViewNameButtons() {
    return (
      <DropdownButton variant="success" title="View Names" id="views_dropdown">
        {allViewNames().map((vn, i) =>
          <Dropdown.Item key={i} onClick={() => handleViewNameClick(vn)}>{vn}</Dropdown.Item>)}
      </DropdownButton>
    );
  }

  function renderEditor() {
    const ctx = p.ctx;
    return (
      <div className="code-container">
        <div className="btn-toolbar btn-toolbar-small">
          {renderViewNameButtons()}
        </div>
        <pre style={{ border: "0px", margin: "0px", overflow: "visible" }}>{"(e: " + ctx.value.entityType!.className + ", "}
          <div style={{ display: "inline-flex" }}>
            <ModulesHelp cleanName={ctx.value.entityType!.className} />{") =>"}
          </div>
        </pre>
        <JavascriptCodeMirror code={ctx.value.script ?? ""} onChange={handleCodeChange} />
        {syntaxError && <div className="alert alert-danger">{syntaxError}</div>}
      </div>
    );
  }
  const ctx = p.ctx;

  return (
    <div>
      <EntityLine ctx={ctx.subCtx(a => a.entityType)} onChange={forceUpdate} onRemove={handleTypeRemove} />

      {ctx.value.entityType &&
        <div>
          <br />
          <div className="row">
            <div className="col-sm-7">
              {renderEditor()}
              {renderTest()}
            </div>
            <div className="col-sm-5">
              <TypeHelpComponent initialType={ctx.value.entityType.cleanName} mode="TypeScript" onMemberClick={handleTypeHelpClick} />
            </div>
          </div>
        </div>
      }
    </div>
  );
}

