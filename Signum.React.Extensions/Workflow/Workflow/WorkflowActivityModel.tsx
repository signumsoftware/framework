import * as React from 'react'
import {
    WorkflowActivityEntity, WorkflowActivityModel, WorkflowActivityValidationEntity, WorkflowActivityMessage, WorkflowConditionEntity, WorkflowActionEntity,
    WorkflowJumpEntity, IWorkflowNodeEntity, SubWorkflowEntity, SubEntitiesEval
} from '../Signum.Entities.Workflow'
import * as WorkflowClient from '../WorkflowClient'
import * as DynamicViewClient from '../../../../Extensions/Signum.React.Extensions/Dynamic/DynamicViewClient'
import { TypeContext, ValueLine, ValueLineType, EntityLine, EntityTable, EntityDetail, FormGroup, LiteAutocompleteConfig } from '../../../../Framework/Signum.React/Scripts/Lines'
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
        if (wa.type == "DecompositionWorkflow" || wa.type == "CallWorkflow") {
            if (!wa.subWorkflow)
                wa.subWorkflow = SubWorkflowEntity.New({
                    subEntitiesEval: SubEntitiesEval.New()
                });
            wa.viewName = null;
            wa.requiresOpen = false;
            wa.validationRules = [];
        }
        else
            wa.subWorkflow = null;

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

                {ctx.value.type != "DecompositionWorkflow" && ctx.value.type != "CallWorkflow" &&
                    <div>
                        <FormGroup ctx={ctx.subCtx(d => d.viewName)} labelText={ctx.niceName(d => d.viewName)}>
                            {
                                <select value={ctx.value.viewName ? ctx.value.viewName : ""} className="form-control" onChange={this.handleViewNameChange} style={this.getViewNameColor(ctx.value.viewName || "")} >
                                    {!ctx.value.viewName && <option value="">{" - "}</option>}
                                    {Dic.getKeys(this.state.viewInfo).map((v, i) => <option key={i} value={v} style={this.getViewNameColor(v)}>{v}</option>)}
                                </select>
                            }
                        </FormGroup>

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
                            ctx.value.type == "DecisionTask" ? {
                                property: wav => wav.onAccept,
                                cellProps: ctx => ({ style: { verticalAlign: "middle" } }),
                                template: ctx => <ValueLine ctx={ctx.subCtx(wav => wav.onAccept)} formGroupStyle="None" valueHtmlProps={{ style: { margin: "0 auto" } }} />,
                            } : null,
                            ctx.value.type == "DecisionTask" ? {
                                property: wav => wav.onDecline,
                                cellProps: ctx => ({ style: { verticalAlign: "middle" } }),
                                template: ctx => <ValueLine ctx={ctx.subCtx(wav => wav.onDecline)} formGroupStyle="None" valueHtmlProps={{ style: { margin: "0 auto" } }} />,
                            } : null,
                        ])} />
                        <EntityDetail ctx={ctx.subCtx(a => a.reject)} />
                        <EntityTable ctx={ctx.subCtx(d => d.jumps)} columns={EntityTable.typedColumns<WorkflowJumpEntity>([
                            {
                                property: wj => wj.to,
                                template: (jCtx, row, state) => {
                                    return <EntityLine
                                        ctx={jCtx.subCtx(wj => wj.to)}
                                        autoComplete={new LiteAutocompleteConfig(str => API.findNode(({ workflowId: ctx.value.workflow!.id, subString: str, count: 5, excludes: this.getCurrentJumpsTo() })), false)}
                                        find={false} />
                                },
                                headerProps: { width: "32%" }
                            },
                            {
                                property: wj => wj.condition,
                                headerProps: { width: "30%" },
                                template: (jCtx, row, state) => {
                                    return <EntityLine ctx={jCtx.subCtx(wj => wj.condition)} findOptions={{
                                        queryName: WorkflowConditionEntity,
                                        parentColumn: "Entity.MainEntityType",
                                        parentValue: ctx.value.mainEntityType
                                    }} />
                                },
                            },
                            {
                                property: wj => wj.action,
                                headerProps: { width: "30%" },
                                template: (jCtx, row, state) => {
                                    return <EntityLine ctx={jCtx.subCtx(wj => wj.action)} findOptions={{
                                        queryName: WorkflowActionEntity,
                                        parentColumn: "Entity.MainEntityType",
                                        parentValue: ctx.value.mainEntityType
                                    }} />
                                },
                            },
                        ])} />
                        <ValueLine ctx={ctx.subCtx(a => a.requiresOpen)} />
                        <fieldset>
                            <legend>{WorkflowActivityModel.nicePropertyName(a => a.userHelp)}</legend>
                            <HtmlEditor binding={Binding.create(ctx.value, a => a.userHelp)} />
                        </fieldset>
                        <ValueLine ctx={ctx.subCtx(d => d.comments)} />
                    </div>
                }
                {ctx.value.subWorkflow &&
                    <DecompositionComponent ctx={ctx.subCtx(a => a.subWorkflow!)} mainEntityType={ctx.value.mainEntityType} />}
            </div>
        );
    }

    getCurrentJumpsTo()
    {
        var result: Lite<IWorkflowNodeEntity>[] = [];
        var ctx = this.props.ctx;
        if (ctx.value.workflowActivity)
            result.push(ctx.value.workflowActivity);
        ctx.value.jumps.forEach(j => j.element.to && result.push(j.element.to));
        return result;
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
        var ctx = this.props.ctx;
        const mainEntityName = this.props.mainEntityType.cleanName;
        return (
            <fieldset>
                <legend>{ctx.niceName()}</legend>
                <EntityLine ctx={ctx.subCtx(a => a.workflow)} onChange={() => this.forceUpdate()} />
                {ctx.value.workflow &&
                    <div className="row">
                        <div className="col-sm-7">
                        <div className="code-container">
                            <pre style={{ border: "0px", margin: "0px" }}>{`IEnumerable<${ctx.value.workflow.mainEntityType!.cleanName}Entity> SubEntities(${mainEntityName}Entity e, WorkflowEvaluationContext ctx)\n{`}</pre>
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
