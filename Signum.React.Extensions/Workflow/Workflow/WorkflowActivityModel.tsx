import * as React from 'react'
import {
  WorkflowActivityModel, WorkflowMessage, SubWorkflowEmbedded, SubEntitiesEval, WorkflowScriptEntity, WorkflowScriptPartEmbedded, WorkflowEntity, ViewNamePropEmbedded, ButtonOptionEmbedded, WorkflowActivityMessage,
} from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, EntityLine, FormGroup, EntityRepeater, EntityTable, EntityDetail } from '@framework/Lines'
import { TypeEntity } from '@framework/Signum.Entities.Basics'
import { Binding } from '@framework/Reflection';
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import TypeHelpComponent from "../../TypeHelp/TypeHelpComponent";
import * as DynamicViewClient from '../../Dynamic/DynamicViewClient'
import HtmlEditor from '../../HtmlEditor/HtmlEditor'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { newMListElement, ModifiableEntity } from '@framework/Signum.Entities';
import { Button } from 'react-bootstrap';
import { Dic } from '@framework/Globals';
import { isFunctionOrStringOrNull } from '../../Dynamic/View/NodeUtils';
import { useForceUpdate } from '@framework/Hooks'


interface WorkflowActivityModelComponentProps {
  ctx: TypeContext<WorkflowActivityModel>;
}

export default function WorkflowActivityModelComponent(p : WorkflowActivityModelComponentProps){
  const forceUpdate = useForceUpdate();

  const [viewNames, setViewNames] = React.useState<string[] | undefined>(undefined);
  const [viewProps, setViewProps] = React.useState<DynamicViewClient.DynamicViewProps[] | undefined>(undefined);

  React.useEffect(() => {
    if (p.ctx.value.mainEntityType) {

      const typeName = p.ctx.value.mainEntityType.cleanName;

      Navigator.viewDispatcher.getViewNames(typeName)
        .then(vn => setViewNames(vn));

      fillViewProps();
    }

    handleTypeChange();
  }, []);

  function isNamedView(typeName: string, viewName: string) : boolean {
    const es = Navigator.getSettings(typeName);
    return ((es?.namedViews && Dic.getKeys(es.namedViews)) ?? []).contains(viewName);
  }

  function fillViewProps() {
    const typeName = p.ctx.value.mainEntityType.cleanName;
    const viewName = p.ctx.value.viewName;

    const isStaticView = !viewName || viewName == "" || isNamedView(typeName, viewName);

    if (isStaticView) {
      setViewProps(undefined);
      p.ctx.value.viewNameProps = [];
      p.ctx.value.modified = true;
      forceUpdate();
      return;
    }

    const oldViewNameProps = p.ctx.value.viewNameProps.toObject(a => a.element.name, a => a.element.expression);
    DynamicViewClient.API.getDynamicViewProps(typeName, viewName!).then(dvp => {

      setViewProps(dvp);
      if (dvp.length > 0) {

        var newViewNameProps = dvp.map(p => {

          const oldExpr = oldViewNameProps[p.name];
          return newMListElement(ViewNamePropEmbedded.New({
            name: p.name,
            expression: oldExpr,
          }))
        });

        p.ctx.value.viewNameProps = newViewNameProps;
      }
      else
        p.ctx.value.viewNameProps = [];

      p.ctx.value.modified = true;
      forceUpdate();
    });
  }

  function handleViewNameChange(e: React.SyntheticEvent<HTMLSelectElement>) {
    p.ctx.value.viewName = (e.currentTarget as HTMLSelectElement).value;
    fillViewProps();
  };

  function handleTypeChange() {
    var wa = p.ctx.value;

    if (wa.type == "Script") {
      if (!wa.script)
        wa.script = WorkflowScriptPartEmbedded.New({
        });
      wa.subWorkflow = null;
    }

    if (wa.type == "DecompositionWorkflow" || wa.type == "CallWorkflow") {
      if (!wa.subWorkflow)
        wa.subWorkflow = SubWorkflowEmbedded.New({
          subEntitiesEval: SubEntitiesEval.New()
        });
      wa.script = null;
    }

    if (wa.type == "DecompositionWorkflow" || wa.type == "CallWorkflow" || wa.type == "Script") {
      wa.viewName = null;
      wa.requiresOpen = false;
    }
    else {
      wa.subWorkflow = null;
      wa.script = null;
    }

    if (wa.type == "Decision")
    {
      if (ctx.value.decisionOptions.length == 0) {
        ctx.value.decisionOptions.push(newMListElement(ButtonOptionEmbedded.New({ name: WorkflowActivityMessage.Approve.niceToString(), style: "Success" })));
        ctx.value.decisionOptions.push(newMListElement(ButtonOptionEmbedded.New({ name: WorkflowActivityMessage.Decline.niceToString(), style: "Danger" })));
      }
    }
    else
      wa.decisionOptions = [];


    if (wa.type != "Task") 
      wa.customNextButton = null;


    wa.modified = true;

    forceUpdate();
  }

  function handleCheckView() {
    const typeName = p.ctx.value.mainEntityType.cleanName;
    const viewName = p.ctx.value.viewName;
    const props = p.ctx.value.viewNameProps.map(a => a.element).toObject(a => a.name, a => !a.expression ? undefined : eval(a.expression));

    const isStaticView = !viewName || viewName == "" || isNamedView(typeName, viewName);

    if (isStaticView)
      Finder.find({ queryName: typeName }).then(lite => {
        if (!lite)
          return Promise.resolve(undefined);

        return Navigator.API.fetch(lite).then(entity => {

          const vp = Navigator.viewDispatcher.getViewPromise(entity, viewName || undefined);
          return Navigator.view(entity,
            {
              getViewPromise: e => vp,
              extraProps: props,
              isOperationVisible: eoc => false,
              avoidPromptLoseChange: true,
              readOnly: true,
            })
        })
      });
    else
      DynamicViewClient.API.getDynamicView(typeName, viewName!)
        .then(dv => {
          Navigator.view(dv, { extraProps: props });
        });
  }

  function getViewNamePropsExpressionHelpText(ctx: TypeContext<ViewNamePropEmbedded>) {
    const vp = viewProps;
    const p = vp?.singleOrNull(a => a.name == ctx.value.name);

    return !vp ? undefined :
      !p ? <div style={{ color: "#a94442" }}><strong>Property not found</strong></div> :
        <strong>{p.type}</strong>;
  }

  function getViewNamePropsIsMandatory(ctx: TypeContext<ViewNamePropEmbedded>) {
    const vp = viewProps;
    const p = vp?.singleOrNull(a => a.name == ctx.value.name);

    return p != null && !p.type.endsWith("?");
  }

  var ctx = p.ctx;

  const mainEntityType = p.ctx.value.mainEntityType;

  return (
    <div>
      <ValueLine ctx={ctx.subCtx(d => d.name)} onChange={() => forceUpdate()} />
      <ValueLine ctx={ctx.subCtx(d => d.type)} onChange={handleTypeChange} valueColumns={5} />
      <ValueLine ctx={ctx.subCtx(a => a.estimatedDuration)} valueColumns={5} />

      {ctx.value.type != "DecompositionWorkflow" && ctx.value.type != "CallWorkflow" && ctx.value.type != "Script" &&
        <div>
          {ctx.value.mainEntityType ? <>
            <FormGroup ctx={ctx.subCtx(d => d.viewName)} label={ctx.niceName(d => d.viewName)}>
              {
                <div className="row">
                  <div className="col-sm-6">
                  <select value={ctx.value.viewName ? ctx.value.viewName : ""} className="form-select form-select-sm" onChange={handleViewNameChange}>
                      <option value="">{" - "}</option>
                      {(viewNames ?? []).map((v, i) => <option key={i} value={v}>{v}</option>)}
                    </select>
                  </div>
                  <div className="col-sm-6">
                  <Button variant="success" size="sm" onClick={handleCheckView}>Check View …</Button>
                  </div>
                </div>
              }
            </FormGroup>
            <FormGroup ctx={ctx.subCtx(d => d.viewNameProps)}>
              <EntityTable avoidFieldSet
                ctx={ctx.subCtx(d => d.viewNameProps)}
                columns={EntityTable.typedColumns<ViewNamePropEmbedded>([
                  {
                    property: a => a.name,
                    template: ctx => <ValueLine ctx={ctx.subCtx(a => a.name)} />
                  },
                  {
                    property: a => a.expression,
                    template: (ctx: TypeContext<ViewNamePropEmbedded>) =>
                      <ValueLine ctx={ctx.subCtx(a => a.expression)} helpText={getViewNamePropsExpressionHelpText(ctx)} mandatory={getViewNamePropsIsMandatory(ctx)}
                      />
                  }
                ])} />
            </FormGroup>
          </>
            : <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSetTheWorkflow1.niceToString(ctx.niceName(e => e.viewName), ctx.niceName(e => e.mainEntityType))}</div>}

        <ValueLine ctx={ctx.subCtx(a => a.requiresOpen)} />

        {ctx.value.type == "Decision" ? <EntityTable ctx={ctx.subCtx(a => a.decisionOptions)} /> : null}

        {ctx.value.type == "Task" ? <EntityDetail ctx={ctx.subCtx(a => a.customNextButton)} labelColumns={1} valueColumns={4} /> : null}

          {ctx.value.workflow ? <EntityRepeater ctx={ctx.subCtx(a => a.boundaryTimers)} readOnly={false} /> :
            <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSaveWorkflow.niceToString(ctx.niceName(e => e.boundaryTimers))}</div>}

          <fieldset>
            <legend>{WorkflowActivityModel.nicePropertyName(a => a.userHelp)}</legend>
            <HtmlEditor binding={Binding.create(ctx.value, a => a.userHelp)} />
          </fieldset>
          <ValueLine ctx={ctx.subCtx(d => d.comments)} />
        </div>
      }

      {ctx.value.script ?
        ctx.value.workflow ? <ScriptComponent ctx={ctx.subCtx(a => a.script!)} mainEntityType={ctx.value.mainEntityType} workflow={ctx.value.workflow!} />
          : <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSaveWorkflow.niceToString(ctx.niceName(e => e.script))}</div>
        : undefined
      }

      {ctx.value.subWorkflow ?
        ctx.value.mainEntityType ? <DecompositionComponent ctx={ctx.subCtx(a => a.subWorkflow!)} mainEntityType={ctx.value.mainEntityType} />
          : <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSetTheWorkflow1.niceToString(ctx.niceName(e => e.subWorkflow), ctx.niceName(e => e.mainEntityType))}</div>
        : undefined}
    </div>
  );
}

function ScriptComponent(p : { ctx: TypeContext<WorkflowScriptPartEmbedded>, mainEntityType: TypeEntity, workflow: WorkflowEntity }){
  const ctx = p.ctx;
  const mainEntityName = p.workflow.mainEntityType!.cleanName;
  return (
    <fieldset>
      <legend>{ctx.niceName()}</legend>
      <EntityLine ctx={ctx.subCtx(p => p.script)} findOptions={{
        queryName: WorkflowScriptEntity,
        filterOptions: [{ token: WorkflowScriptEntity.token(e => e.entity.mainEntityType), value: p.mainEntityType}]
      }} />
      <EntityLine ctx={ctx.subCtx(s => s.retryStrategy)} />
    </fieldset>
  );
}

function DecompositionComponent(p : { ctx: TypeContext<SubWorkflowEmbedded>, mainEntityType: TypeEntity }){
  const forceUpdate = useForceUpdate();
  function handleCodeChange(newScript: string) {
    const subEntitiesEval = p.ctx.value.subEntitiesEval!;
    subEntitiesEval.script = newScript;
    subEntitiesEval.modified = true;
    forceUpdate();
  }

  const ctx = p.ctx;
  const mainEntityName = p.mainEntityType.cleanName;
  return (
    <fieldset>
      <legend>{ctx.niceName()}</legend>
      <EntityLine ctx={ctx.subCtx(a => a.workflow)} onChange={() => forceUpdate()} />
      {ctx.value.workflow &&
        <div>
          <br />
          <div className="row">
            <div className="col-sm-7">
              <div className="code-container">
                <pre style={{ border: "0px", margin: "0px" }}>{`IEnumerable<${ctx.value.workflow.mainEntityType!.cleanName}Entity> SubEntities(${mainEntityName}Entity e, WorkflowTransitionContext ctx)\n{`}</pre>
                <CSharpCodeMirror script={ctx.value.subEntitiesEval!.script ?? ""} onChange={handleCodeChange} />
                <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
              </div>
            </div>
            <div className="col-sm-5">
              <TypeHelpComponent initialType={mainEntityName} mode="CSharp" />
            </div>
          </div>
        </div>}
    </fieldset>
  );
}
