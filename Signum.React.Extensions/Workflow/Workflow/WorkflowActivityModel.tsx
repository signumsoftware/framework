import * as React from 'react'
import {
  WorkflowActivityModel, WorkflowMessage, SubWorkflowEmbedded, SubEntitiesEval, WorkflowScriptEntity, WorkflowScriptPartEmbedded, WorkflowEntity, ViewNamePropEmbedded
} from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, EntityLine, FormGroup, EntityRepeater, EntityTable } from '@framework/Lines'
import { TypeEntity } from '@framework/Signum.Entities.Basics'
import { Binding } from '@framework/Reflection';
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import TypeHelpComponent from "../../TypeHelp/TypeHelpComponent";
import * as DynamicViewClient from '../../Dynamic/DynamicViewClient'
import HtmlEditor from '../../HtmlEditor/HtmlEditor'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { newMListElement, ModifiableEntity } from '@framework/Signum.Entities';
import { Button } from '@framework/Components';
import { useFetchAndForget } from '../../../../Framework/Signum.React/Scripts/Hooks';
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals';
import { isFunctionOrStringOrNull } from '../../Dynamic/View/NodeUtils';


interface WorkflowActivityModelComponentProps {
  ctx: TypeContext<WorkflowActivityModel>;
}

interface WorkflowActivityModelComponentState {
  viewNames?: string[];
}

export default class WorkflowActivityModelComponent extends React.Component<WorkflowActivityModelComponentProps, WorkflowActivityModelComponentState> {
  constructor(props: WorkflowActivityModelComponentProps) {
    super(props);

    this.state = {};
  }

  componentWillMount() {

    if (this.props.ctx.value.mainEntityType) {

      const typeName = this.props.ctx.value.mainEntityType.cleanName;

      Navigator.viewDispatcher.getViewNames(typeName)
        .then(vn => this.setState({ viewNames: vn }))
        .done();

      this.fillViewProps();
    }

    this.handleTypeChange();
  }

  isNamedView(typeName: string, viewName: string): boolean {
    const es = Navigator.getSettings(typeName);
    return (es && es.namedViews && Dic.getKeys(es.namedViews) || []).contains(viewName);
  }

  fillViewProps() {

    const typeName = this.props.ctx.value.mainEntityType.cleanName;
    const viewName = this.props.ctx.value.viewName;

    const isStaticView = !viewName || viewName == "" || this.isNamedView(typeName, viewName);

    if (isStaticView) {
      this.props.ctx.value.viewNameProps = [];
      this.props.ctx.value.modified = true;
      this.forceUpdate();
      return;
    }

    const oldViewNameProps = this.props.ctx.value.viewNameProps.toObject(a => a.element.name, a => a.element.expression);
    DynamicViewClient.API.getDynamicViewProps(typeName, viewName!).then(dvp => {

      if (dvp.length > 0) {

        var newViewNameProps = dvp.map(p => {

          const oldExpr = oldViewNameProps[p.name];
          return newMListElement(ViewNamePropEmbedded.New({
            name: p.name,
            expression: oldExpr,
          }))
        });

        this.props.ctx.value.viewNameProps = newViewNameProps;
      }
      else
        this.props.ctx.value.viewNameProps = [];

      this.props.ctx.value.modified = true;
      this.forceUpdate();
    }).done();
  }

  handleViewNameChange = (e: React.SyntheticEvent<HTMLSelectElement>) => {
    debugger;
    this.props.ctx.value.viewName = (e.currentTarget as HTMLSelectElement).value;
    this.fillViewProps();
  };

  handleTypeChange = () => {

    var wa = this.props.ctx.value;

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

    wa.modified = true;

    this.forceUpdate();
  }

  handleCheckView = () => {

    const typeName = this.props.ctx.value.mainEntityType.cleanName;
    const viewName = this.props.ctx.value.viewName;
    const props = this.props.ctx.value.viewNameProps.map(a => a.element).toObject(a => a.name, a => eval(a.expression));

    const isStaticView = !viewName || viewName == "" || this.isNamedView(typeName, viewName);

    if (isStaticView)
      Finder.find({ queryName: typeName }).then(lite => {
        if (!lite)
          return Promise.resolve(undefined);

        return Navigator.API.fetchAndForget(lite).then(entity => {

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
      }).done();
    else
      DynamicViewClient.API.getDynamicView(typeName, viewName!)
        .then(dv => {
          Navigator.navigate(dv, { extraProps: props });
        }).done();
  }

  render() {
    var ctx = this.props.ctx;

    const mainEntityType = this.props.ctx.value.mainEntityType;

    return (
      <div>
        <ValueLine ctx={ctx.subCtx(d => d.name)} onChange={() => this.forceUpdate()} />
        <ValueLine ctx={ctx.subCtx(d => d.type)} onChange={this.handleTypeChange} valueColumns={5} />
        <ValueLine ctx={ctx.subCtx(a => a.estimatedDuration)} valueColumns={5} />

        {ctx.value.type != "DecompositionWorkflow" && ctx.value.type != "CallWorkflow" && ctx.value.type != "Script" &&
          <div>
            {ctx.value.mainEntityType ? <>
              <FormGroup ctx={ctx.subCtx(d => d.viewName)} labelText={ctx.niceName(d => d.viewName)}>
                {
                  <div className="row">
                    <div className="col-sm-6">
                      <select value={ctx.value.viewName ? ctx.value.viewName : ""} className="form-control form-control-sm" onChange={this.handleViewNameChange}>
                        <option value="">{" - "}</option>
                        {(this.state.viewNames || []).map((v, i) => <option key={i} value={v}>{v}</option>)}
                      </select>
                    </div>
                    <div className="col-sm-6">
                      <Button color="success" size="sm" onClick={this.handleCheckView}>
                        Check View …
                  </Button>
                    </div>
                  </div>
                }
              </FormGroup>
              <FormGroup ctx={ctx.subCtx(d => d.viewNameProps)}>
                <EntityTable ctx={ctx.subCtx(d => d.viewNameProps)} avoidFieldSet />
              </FormGroup>
            </>
              : <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSetTheWorkflow1.niceToString(ctx.niceName(e => e.viewName), ctx.niceName(e => e.mainEntityType))}</div>}


            <ValueLine ctx={ctx.subCtx(a => a.requiresOpen)} />

            {ctx.value.workflow ? <EntityRepeater ctx={ctx.subCtx(a => a.boundaryTimers)} readOnly={true} /> :
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
}

class ScriptComponent extends React.Component<{ ctx: TypeContext<WorkflowScriptPartEmbedded>, mainEntityType: TypeEntity, workflow: WorkflowEntity }>{


  render() {
    const ctx = this.props.ctx;
    const mainEntityName = this.props.workflow.mainEntityType!.cleanName;
    return (
      <fieldset>
        <legend>{ctx.niceName()}</legend>
        <EntityLine ctx={ctx.subCtx(p => p.script)} findOptions={{
          queryName: WorkflowScriptEntity,
          parentToken: WorkflowScriptEntity.token().entity(e => e.mainEntityType),
          parentValue: this.props.mainEntityType
        }} />
        <EntityLine ctx={ctx.subCtx(s => s.retryStrategy)} />
      </fieldset>
    );
  }
}

class DecompositionComponent extends React.Component<{ ctx: TypeContext<SubWorkflowEmbedded>, mainEntityType: TypeEntity }>{

  handleCodeChange = (newScript: string) => {
    const subEntitiesEval = this.props.ctx.value.subEntitiesEval!;
    subEntitiesEval.script = newScript;
    subEntitiesEval.modified = true;
    this.forceUpdate();
  }

  render() {
    const ctx = this.props.ctx;
    const mainEntityName = this.props.mainEntityType.cleanName;
    return (
      <fieldset>
        <legend>{ctx.niceName()}</legend>
        <EntityLine ctx={ctx.subCtx(a => a.workflow)} onChange={() => this.forceUpdate()} />
        {ctx.value.workflow &&
          <div>
            <br />
            <div className="row">
              <div className="col-sm-7">
                <div className="code-container">
                  <pre style={{ border: "0px", margin: "0px" }}>{`IEnumerable<${ctx.value.workflow.mainEntityType!.cleanName}Entity> SubEntities(${mainEntityName}Entity e, WorkflowTransitionContext ctx)\n{`}</pre>
                  <CSharpCodeMirror script={ctx.value.subEntitiesEval!.script || ""} onChange={this.handleCodeChange} />
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
}
