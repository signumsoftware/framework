import * as React from 'react'
import {
    WorkflowActivityEntity, WorkflowActivityModel, WorkflowActivityValidationEntity, WorkflowActivityMessage, WorkflowConditionEntity, WorkflowActionEntity,
    WorkflowJumpEntity, WorkflowTimeoutEntity, IWorkflowNodeEntity, SubWorkflowEntity, SubEntitiesEval, WorkflowScriptEntity, WorkflowScriptEval, WorkflowEntity
} from '../Signum.Entities.Workflow'
import * as WorkflowClient from '../WorkflowClient'
import * as DynamicViewClient from '../../../../Extensions/Signum.React.Extensions/Dynamic/DynamicViewClient'
import { TypeContext, ValueLine, ValueLineType, EntityLine, EntityTable, EntityDetail, FormGroup, LiteAutocompleteConfig, RenderEntity } from '../../../../Framework/Signum.React/Scripts/Lines'
import { is, JavascriptMessage, Lite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { DynamicValidationEntity } from '../../../../Extensions/Signum.React.Extensions/Dynamic/Signum.Entities.Dynamic'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals';
import { Binding } from '../../../../Framework/Signum.React/Scripts/Reflection';
import CSharpCodeMirror from '../../../../Extensions/Signum.React.Extensions/Codemirror/CSharpCodeMirror'
import TypeHelpComponent from '../../Dynamic/Help/TypeHelpComponent'
import HtmlEditor from '../../../../Extensions/Signum.React.Extensions/HtmlEditor/HtmlEditor'
import Typeahead from '../../../../Framework/Signum.React/Scripts/Lines/Typeahead'
import { API } from '../WorkflowClient'
import ValueLineModal from '../../../../Framework/Signum.React/Scripts/ValueLineModal'

interface WorkflowActivityModelComponentProps {
    ctx: TypeContext<WorkflowActivityModel>;
}

interface WorkflowActivityModelComponentState {
    viewInfo: { [name: string]: "Static" | "Dynamic" };

}

export default class WorkflowActivityModelComponent extends React.Component<WorkflowActivityModelComponentProps, WorkflowActivityModelComponentState> {

    constructor(props: WorkflowActivityModelComponentProps) {
        super(props);

        this.state = { viewInfo: {} };
    }

    componentWillMount() {

        const typeName = this.props.ctx.value.mainEntityType.cleanName;

        const registeredViews = WorkflowClient.getViewNames(typeName).toObject(k => k, v => "Static") as { [name: string]: "Static" | "Dynamic" };

        DynamicViewClient.API.getDynamicViewNames(typeName)
            .then(dynamicViews => {
                dynamicViews.forEach(dv => {
                    if (registeredViews[dv])
                        throw Error(WorkflowActivityMessage.DuplicateViewNameFound0.niceToString(`"${dv}"`));
                    else
                        registeredViews[dv] = "Dynamic";
                });

                this.setState({ viewInfo: registeredViews });
            }).done();
    }

    getViewNameColor(viewName: string) {

        if (this.state.viewInfo[viewName] == "Dynamic")
            return { color: "blue" };

        return { color: "black" };
    }

    handleViewNameChange = (e: React.SyntheticEvent<HTMLSelectElement>) => {
        this.props.ctx.value.viewName = (e.currentTarget as HTMLSelectElement).value;
        this.props.ctx.value.modified = true;
        this.forceUpdate();
    };

    handleTypeChange = () => {

        var wa = this.props.ctx.value;
        if (wa.type != "Task")
            wa.timeout = null;

        if (wa.type == "Script") {
            if (!wa.script)
                wa.script = WorkflowScriptEntity.New({
                    eval: WorkflowScriptEval.New(),
                });
            wa.subWorkflow = null;
        }

        if (wa.type == "DecompositionWorkflow" || wa.type == "CallWorkflow") {
            if (!wa.subWorkflow)
                wa.subWorkflow = SubWorkflowEntity.New({
                    subEntitiesEval: SubEntitiesEval.New()
                });
            wa.script = null;
        }

        if (wa.type == "DecompositionWorkflow" || wa.type == "CallWorkflow" || wa.type == "Script") {
            wa.viewName = null;
            wa.requiresOpen = false;
            wa.reject = null;
            wa.validationRules = [];
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

                {ctx.value.type != "DecompositionWorkflow" && ctx.value.type != "CallWorkflow" && ctx.value.type != "Script" &&
                    <div>
                        <FormGroup ctx={ctx.subCtx(d => d.viewName)} labelText={ctx.niceName(d => d.viewName)}>
                            {
                                <select value={ctx.value.viewName ? ctx.value.viewName : ""} className="form-control" onChange={this.handleViewNameChange} style={this.getViewNameColor(ctx.value.viewName || "")} >
                                    {!ctx.value.viewName && <option value="">{" - "}</option>}
                                    {Dic.getKeys(this.state.viewInfo).map((v, i) => <option key={i} value={v} style={this.getViewNameColor(v)}>{v}</option>)}
                                </select>
                            }
                        </FormGroup>

                        <ValueLine ctx={ctx.subCtx(a => a.requiresOpen)} />
                        <EntityTable ctx={ctx.subCtx(d => d.validationRules)} columns={EntityTable.typedColumns<WorkflowActivityValidationEntity>([
                            {
                                property: wav => wav.rule,
                                headerProps: { style: { width: "100%" } },
                                template: ctx => <EntityLine ctx={ctx.subCtx(wav => wav.rule)} findOptions={{
                                    queryName: DynamicValidationEntity,
                                    filterOptions: [
                                        { columnName: "Entity.EntityType", value: mainEntityType },
                                        { columnName: "Entity.IsGlobalyEnabled", value: false },
                                    ]
                                }} />
                            },
                            ctx.value.type == "Decision" ? {
                                property: wav => wav.onAccept,
                                cellProps: ctx => ({ style: { verticalAlign: "middle" } }),
                                template: ctx => <ValueLine ctx={ctx.subCtx(wav => wav.onAccept)} formGroupStyle="None" valueHtmlProps={{ style: { margin: "0 auto" } }} />,
                            } : null,
                            ctx.value.type == "Decision" ? {
                                property: wav => wav.onDecline,
                                cellProps: ctx => ({ style: { verticalAlign: "middle" } }),
                                template: ctx => <ValueLine ctx={ctx.subCtx(wav => wav.onDecline)} formGroupStyle="None" valueHtmlProps={{ style: { margin: "0 auto" } }} />,
                            } : null,
                        ])} />
                        <EntityDetail ctx={ctx.subCtx(a => a.reject)} />

                        {ctx.value.type == "Task" ? ctx.value.workflow ?
                            <EntityDetail ctx={ctx.subCtx(a => a.timeout)} getComponent={(tctx: TypeContext<WorkflowTimeoutEntity>) =>
                                <div>
                                    <FormGroup ctx={tctx.subCtx(t => t.timeout)} >
                                        <RenderEntity ctx={tctx.subCtx(t => t.timeout)} />
                                    </FormGroup>
                                    <EntityLine
                                        ctx={tctx.subCtx(t => t.to)}
                                        autoComplete={new LiteAutocompleteConfig(str => API.findNode(({ workflowId: ctx.value.workflow!.id, subString: str, count: 5, excludes: this.getCurrentJumpsTo() })), false)}
                                        find={false} />
                                    <EntityLine ctx={tctx.subCtx(t => t.action)} findOptions={{
                                        queryName: WorkflowActionEntity,
                                        parentColumn: "Entity.MainEntityType",
                                        parentValue: ctx.value.mainEntityType
                                    }} />
                            </div>
                        } /> : <div className="alert alert-warning">{WorkflowActivityMessage.ToUse0YouSouldSaveWorkflow.niceToString('Timeout')}</div>
                        : undefined}

                        {ctx.value.workflow ?
                            <EntityTable ctx={ctx.subCtx(d => d.jumps)} columns={EntityTable.typedColumns<WorkflowJumpEntity>([
                                {
                                    property: wj => wj.to,
                                    template: (jctx, row, state) => {
                                        return <EntityLine
                                            ctx={jctx.subCtx(wj => wj.to)}
                                            autoComplete={new LiteAutocompleteConfig(str => API.findNode(({ workflowId: ctx.value.workflow!.id, subString: str, count: 5, excludes: this.getCurrentJumpsTo() })), false)}
                                            find={false} />
                                    },
                                    headerProps: { width: "32%" }
                                },
                                {
                                    property: wj => wj.condition,
                                    headerProps: { width: "30%" },
                                    template: (jctx, row, state) => {
                                        return <EntityLine ctx={jctx.subCtx(wj => wj.condition)} findOptions={{
                                            queryName: WorkflowConditionEntity,
                                            parentColumn: "Entity.MainEntityType",
                                            parentValue: ctx.value.mainEntityType
                                        }} />
                                    },
                                },
                                {
                                    property: wj => wj.action,
                                    headerProps: { width: "30%" },
                                    template: (jctx, row, state) => {
                                        return <EntityLine ctx={jctx.subCtx(wj => wj.action)} findOptions={{
                                            queryName: WorkflowActionEntity,
                                            parentColumn: "Entity.MainEntityType",
                                            parentValue: ctx.value.mainEntityType
                                        }} />
                                    },
                                },
                        ])} /> :
                        <div className="alert alert-warning">{WorkflowActivityMessage.ToUse0YouSouldSaveWorkflow.niceToString('Jumps')}</div>
                        }

                        <fieldset>
                            <legend>{WorkflowActivityModel.nicePropertyName(a => a.userHelp)}</legend>
                            <HtmlEditor binding={Binding.create(ctx.value, a => a.userHelp)} />
                        </fieldset>
                        <ValueLine ctx={ctx.subCtx(d => d.comments)} />
                    </div>
                }

                {ctx.value.script ?
                    ctx.value.workflow ? <ScriptComponent ctx={ctx.subCtx(a => a.script!)} workflow={ctx.value.workflow!} />
                        : <div className="alert alert-warning">{WorkflowActivityMessage.ToUse0YouSouldSaveWorkflow.niceToString('Script')}</div>
                    : undefined
                }

                {ctx.value.subWorkflow &&
                    <DecompositionComponent ctx={ctx.subCtx(a => a.subWorkflow!)} mainEntityType={ctx.value.mainEntityType} />}
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

class ScriptComponent extends React.Component<{ ctx: TypeContext<WorkflowScriptEntity>, workflow: WorkflowEntity }, void>{
    handleScriptChange = (newScript: string) => {
        const scriptEval = this.props.ctx.value.eval!;
        scriptEval.script = newScript;
        scriptEval.modified = true;
        this.forceUpdate();
    }

    handleCustomTypesChange = (newScript: string) => {
        const scriptEval = this.props.ctx.value.eval!;
        scriptEval.customTypes = newScript;
        scriptEval.modified = true;
        this.forceUpdate();
    }

    render() {
        const ctx = this.props.ctx;
        const mainEntityName = this.props.workflow.mainEntityType!.cleanName;
        return (
            <fieldset>
                <legend>{ctx.niceName()}</legend>
                <EntityLine ctx={ctx.subCtx(s => s.retryStrategy)} />
                <EntityLine
                    ctx={ctx.subCtx(s => s.onFailureJump)}
                    autoComplete={new LiteAutocompleteConfig(str => API.findNode(({ workflowId: this.props.workflow.id, subString: str, count: 5 })), false)}
                    find={false} />
                &nbsp;
                <div className="row">
                    <div className="col-sm-7">
                        <div className="btn-group" style={{ marginBottom: "3px" }}>
                           <input type="button" className="btn btn-danger btn-xs sf-button" value="try-catch" onClick={this.handleTryCatchClick} />
                           <input type="button" className="btn btn-success btn-xs sf-button" value="REST" onClick={this.handleRestClick} />
                           <input type="button" className="btn btn-warning btn-xs sf-button" value="SOAP" onClick={this.handleSoapClick} />
                           <input type="button" className="btn btn-danger btn-xs sf-button" value="ctx" onClick={this.handleCtxClick} />
                        </div>
                        <div className="code-container">
                            <pre style={{ border: "0px", margin: "0px" }}>{`public static void CallScript(${mainEntityName}Entity e, WorkflowScriptContext ctx)\n{`}</pre>
                            <CSharpCodeMirror script={ctx.value.eval!.script || ""} onChange={this.handleScriptChange} />
                            <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
                        </div>
                        <div className="code-container">
                            <pre style={{ border: "0px", margin: "0px" }}>{`namespace MyCustomTypes {`}</pre>
                            <CSharpCodeMirror script={ctx.value.eval!.customTypes || ""} onChange={this.handleCustomTypesChange} />
                            <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
                        </div>
                    </div>
                    <div className="col-sm-5">
                        <TypeHelpComponent initialType={mainEntityName} mode="CSharp" />
                    </div>
                </div>
            </fieldset>
        );
    }

    handleRestClick = () => {
        ValueLineModal.show({
            type: { name: "string" },
            initialValue: `// REST
var response = HttpClient.Post<MyResponse>("Your URL", new { paramName = e.[Property Name], ... });
e.[Property Name] = response.[Property Name];

class MyResponse {}`,
            valueLineType: ValueLineType.TextArea,
            title: "REST Template",
            message: "Copy to clipboard: Ctrl+C, ESC",
            initiallyFocused: true,
            valueHtmlProps: { style: { height: "115px" } },
        });
    }

    handleSoapClick = () => {
        ValueLineModal.show({
            type: { name: "string" },
            initialValue: `// SOAP
var lib = Assembly.Load("[Assembly full path name]").GetType("[Type Name]").GetMethod("[Method Name]").Invoke(e.[Property Name]);
e.[Property Name] = lib;`,
            valueLineType: ValueLineType.TextArea,
            title: "SOAP Template",
            message: "Copy to clipboard: Ctrl+C, ESC",
            initiallyFocused: true,
            valueHtmlProps: { style: { height: "115px" } },
        });
    }

    handleCtxClick = () => {
        const hint = "WorkflowScriptContext Members";
        ValueLineModal.show({
            type: { name: "string" },
            initialValue: `// ${hint}
CaseActivityEntity CaseActivity; 
int RetryCount;`,
            valueLineType: ValueLineType.TextArea,
            title: hint,
            message: "Copy to clipboard: Ctrl+C, ESC",
            initiallyFocused: true,
            valueHtmlProps: { style: { height: "115px" } },
        });
    }

    handleTryCatchClick = () => {
        ValueLineModal.show({
            type: { name: "string" },
            initialValue: `try
{

}
catch (Exception e)
{
    throw e;
}`,
            valueLineType: ValueLineType.TextArea,
            title: "Try/Catch block",
            message: "Copy to clipboard: Ctrl+C, ESC",
            initiallyFocused: true,
            valueHtmlProps: { style: { height: "180px" } },
        });
    }
}

class DecompositionComponent extends React.Component<{ ctx: TypeContext<SubWorkflowEntity>, mainEntityType: TypeEntity }, void>{

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
                    </div>}
            </fieldset>
        );
    }
}
