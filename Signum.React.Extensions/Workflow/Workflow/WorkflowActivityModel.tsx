import * as React from 'react'
import {
  WorkflowActivityModel, WorkflowMessage, SubWorkflowEmbedded, SubEntitiesEval, WorkflowScriptEntity, WorkflowScriptPartEmbedded, WorkflowEntity
} from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, EntityLine, FormGroup, EntityRepeater } from '@framework/Lines'
import { TypeEntity } from '@framework/Signum.Entities.Basics'
import { Binding } from '@framework/Reflection';
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import TypeHelpComponent from "../../TypeHelp/TypeHelpComponent";
import HtmlEditor from '../../HtmlEditor/HtmlEditor'
import * as Navigator from '@framework/Navigator'

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
    }

    this.handleTypeChange();
  }

  handleViewNameChange = (e: React.SyntheticEvent<HTMLSelectElement>) => {
    this.props.ctx.value.viewName = (e.currentTarget as HTMLSelectElement).value;
    this.props.ctx.value.modified = true;
    this.forceUpdate();
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

  render() {
    var ctx = this.props.ctx;

    const mainEntityType = this.props.ctx.value.mainEntityType;

    return (
      <div>
        <ValueLine ctx={ctx.subCtx(d => d.name)} onChange={() => this.forceUpdate()} />
        <ValueLine ctx={ctx.subCtx(d => d.type)} onChange={this.handleTypeChange} />
        <ValueLine ctx={ctx.subCtx(a => a.estimatedDuration)} />

        {ctx.value.type != "DecompositionWorkflow" && ctx.value.type != "CallWorkflow" && ctx.value.type != "Script" &&
          <div>
            {ctx.value.mainEntityType ?
              <FormGroup ctx={ctx.subCtx(d => d.viewName)} labelText={ctx.niceName(d => d.viewName)}>
                {
                  <select value={ctx.value.viewName ? ctx.value.viewName : ""} className="form-control form-control-sm" onChange={this.handleViewNameChange}>
                    <option value="">{" - "}</option>
                    {(this.state.viewNames || []).map((v, i) => <option key={i} value={v}>{v}</option>)}
                  </select>
                }
              </FormGroup>
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
