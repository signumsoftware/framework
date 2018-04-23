import * as React from 'react'
import {
    WorkflowActivityEntity, WorkflowActivityModel, WorkflowMessage, WorkflowActivityMessage, WorkflowConditionEntity, WorkflowActionEntity,
    WorkflowJumpEmbedded, WorkflowTimerEmbedded, IWorkflowNodeEntity, SubWorkflowEmbedded, SubEntitiesEval, WorkflowScriptEntity, WorkflowScriptPartEmbedded, WorkflowScriptEval, WorkflowEntity, WorkflowActivityType
} from '../Signum.Entities.Workflow'
import * as WorkflowClient from '../WorkflowClient'
import * as DynamicViewClient from '../../Dynamic/DynamicViewClient'
import { TypeContext, ValueLine, ValueLineType, EntityLine, EntityTable, EntityDetail, FormGroup, LiteAutocompleteConfig, RenderEntity, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { is, JavascriptMessage, Lite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { DynamicValidationEntity } from '../../Dynamic/Signum.Entities.Dynamic'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals';
import { Binding } from '../../../../Framework/Signum.React/Scripts/Reflection';
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import TypeHelpComponent from "../../TypeHelp/TypeHelpComponent";
import HtmlEditor from '../../HtmlEditor/HtmlEditor'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { API } from '../WorkflowClient'
import { newMListElement } from '../../../../Framework/Signum.React/Scripts/Signum.Entities';
import { TimeSpanEmbedded } from '../../Basics/Signum.Entities.Basics';

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
        if (!allowsTimers(wa.type))
            wa.timers.clear();
        
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
            wa.reject = null;
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
                <ValueLine ctx={ctx.subCtx(d => d.type)} onChange={this.handleTypeChange} readOnly={ctx.value.type == "Delay" || undefined} comboBoxItems={WorkflowActivityType.values().filter(a => a != "Delay")} />
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
                        <EntityDetail ctx={ctx.subCtx(a => a.reject)} />

                    {allowsTimers(ctx.value.type) ? ctx.value.workflow ?
                        <EntityRepeater ctx={ctx.subCtx(a => a.timers)} create={ctx.value.type != "Delay"} remove={ctx.value.type != "Delay"} getComponent={(tctx: TypeContext<WorkflowTimerEmbedded>) =>
                            <div className="row">
                                <div className="col-sm-6">
                                    <EntityLine ctx={tctx.subCtx(t => t.condition)} />
                                    <EntityDetail ctx={tctx.subCtx(t => t.duration)} />
                                </div>
                                <div className="col-sm-6">
                                    <ValueLine ctx={tctx.subCtx(t => t.interrupting)} visible={ctx.value.type != "Delay"} />
                                    <EntityLine
                                        ctx={tctx.subCtx(t => t.to)}
                                        autoComplete={new LiteAutocompleteConfig((ac, str) => API.findNode({ workflowId: ctx.value.workflow!.id, subString: str, count: 5, excludes: this.getCurrentJumpsTo() }, ac), false)}
                                        find={false} />
                                    <EntityLine ctx={tctx.subCtx(t => t.action)} findOptions={{
                                        queryName: WorkflowActionEntity,
                                        parentColumn: "Entity.MainEntityType",
                                        parentValue: ctx.value.mainEntityType
                                    }} />
                                </div>
                            </div>
                        } /> : <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSaveWorkflow.niceToString(ctx.niceName(e => e.timers))}</div>
                        : undefined}

                        {ctx.value.workflow ?
                            <EntityTable
                                labelText={<div>{ctx.niceName(d => d.jumps)} : <small style={{ fontWeight: "normal" }}>{WorkflowMessage.ToUseNewNodesOnJumpsYouSouldSaveWorkflow.niceToString()}</small></div>}
                                ctx={ctx.subCtx(d => d.jumps)}
                                columns={EntityTable.typedColumns<WorkflowJumpEmbedded>([
                                    {
                                        property: wj => wj.to,
                                        template: (jctx, row, state) => {
                                            return <EntityLine
                                                ctx={jctx.subCtx(wj => wj.to)}
                                                autoComplete={new LiteAutocompleteConfig((ac, str) => API.findNode({ workflowId: ctx.value.workflow!.id, subString: str, count: 5, excludes: this.getCurrentJumpsTo() }, ac), false)}
                                                find={false} />
                                        },
                                        headerHtmlAttributes: { style: { width: "40%" } }
                                    },
                                    {
                                        property: wj => wj.action,
                                        headerHtmlAttributes: { style: { width: "30%" } },
                                        template: (jctx, row, state) => {
                                            return <EntityLine ctx={jctx.subCtx(wj => wj.action)} findOptions={{
                                                queryName: WorkflowActionEntity,
                                                parentColumn: "Entity.MainEntityType",
                                                parentValue: ctx.value.mainEntityType
                                            }} />
                                        },
                                    },
                                    {
                                        property: wj => wj.condition,
                                        headerHtmlAttributes: { style: { width: "20%" } },
                                        template: (jctx, row, state) => {
                                            return <EntityLine ctx={jctx.subCtx(wj => wj.condition)} findOptions={{
                                                queryName: WorkflowConditionEntity,
                                                parentColumn: "Entity.MainEntityType",
                                                parentValue: ctx.value.mainEntityType
                                            }} />
                                        },
                                    },
                                ])} /> :
                            <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSaveWorkflow.niceToString(ctx.niceName(e => e.jumps))}</div>
                        }

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

    getCurrentJumpsTo() {
        var result: Lite<IWorkflowNodeEntity>[] = [];
        var ctx = this.props.ctx;
        if (ctx.value.workflowActivity)
            result.push(ctx.value.workflowActivity);
        ctx.value.jumps.forEach(j => j.element.to && result.push(j.element.to));
        return result;
    }
}

function allowsTimers(type: WorkflowActivityType | null | undefined) {
    return type == "Task" || type == "Decision" || type == "Delay";
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
                    parentColumn: "Entity.MainEntityType",
                    parentValue: this.props.mainEntityType
                }} />
                <EntityLine ctx={ctx.subCtx(s => s.retryStrategy)} />
                <EntityLine
                    ctx={ctx.subCtx(s => s.onFailureJump)}
                    autoComplete={new LiteAutocompleteConfig((ac, str) => API.findNode({ workflowId: this.props.workflow.id, subString: str, count: 5 }, ac), false)}
                    find={false}
                    helpText={WorkflowMessage.ToUseNewNodesOnJumpsYouSouldSaveWorkflow.niceToString()} />

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
