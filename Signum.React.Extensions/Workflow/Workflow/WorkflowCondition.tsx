import * as React from 'react'
import { ValueLine, EntityLine, TypeContext, FormGroup, ValueLineType, LiteAutocompleteConfig } from '../../../../Framework/Signum.React/Scripts/Lines'
import { PropertyRoute, Binding } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import { WorkflowConditionEntity, ICaseMainEntity, DecisionResult } from '../Signum.Entities.Workflow'
import { WorkflowConditionTestResponse, API, DecisionResultValues, showWorkflowTransitionContextCodeHelp } from '../WorkflowClient'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import ValueLineModal from '../../../../Framework/Signum.React/Scripts/ValueLineModal'

interface WorkflowConditionComponentProps {
    ctx: TypeContext<WorkflowConditionEntity>;
}

interface WorkflowConditionComponentState {
    exampleEntity?: ICaseMainEntity;
    decisionResult: DecisionResult;
    response?: WorkflowConditionTestResponse;
}

export default class WorkflowConditionComponent extends React.Component<WorkflowConditionComponentProps, WorkflowConditionComponentState> {

    constructor(props: WorkflowConditionComponentProps) {
        super(props);
        this.state = { decisionResult: "Approve" };
    }

    handleMainEntityTypeChange = () => {
        this.props.ctx.value.eval!.script = "";
        this.setState({
            exampleEntity: undefined,
            decisionResult: "Approve",
            response: undefined
        });
    }

    handleCodeChange = (newScript: string) => {
        const evalEntity = this.props.ctx.value.eval!;
        evalEntity.script = newScript;
        evalEntity.modified = true;
        this.forceUpdate();
    }

    render() {
        var ctx = this.props.ctx;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(wc => wc.name)} />
                <EntityLine ctx={ctx.subCtx(wc => wc.mainEntityType)}
                    onChange={this.handleMainEntityTypeChange}
                    autoComplete={new LiteAutocompleteConfig((ac, str) => API.findMainEntityType({ subString: str, count: 5 }, ac), false)}
                    find={false} />
                {ctx.value.mainEntityType &&
                    <div>
                        <br />
                        <div className="row">
                            <div className="col-sm-7">
                                {this.state.exampleEntity && <button className="btn btn-success" onClick={this.handleEvaluate}><i className="fa fa-play" aria-hidden="true"></i> Evaluate</button>}
                                <div className="btn-group" style={{ marginBottom: "3px" }}>
                                    <input type="button" className="btn btn-success btn-xs sf-button" value="ctx" onClick={() => showWorkflowTransitionContextCodeHelp()} />
                                </div>
                                <div className="code-container">
                                    <pre style={{ border: "0px", margin: "0px" }}>{"boolean Evaluate(" + ctx.value.mainEntityType.cleanName + "Entity e, WorkflowTransitionContext ctx)\n{"}</pre>
                                    <CSharpCodeMirror script={ctx.value.eval!.script || ""} onChange={this.handleCodeChange} />
                                    <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
                                </div>
                                {this.renderTest()}
                            </div>
                            <div className="col-sm-5">
                                <TypeHelpComponent initialType={ctx.value.mainEntityType.cleanName} mode="CSharp" onMemberClick={this.handleTypeHelpClick} />
                            </div>
                        </div>
                    </div>}
            </div>
        );
    }

    handleTypeHelpClick = (pr: PropertyRoute | undefined) => {
        if (!pr)
            return;

        ValueLineModal.show({
            type: { name: "string" },
            initialValue: TypeHelpComponent.getExpression("e", pr, "CSharp"),
            valueLineType: "TextArea",
            title: "Property Template",
            message: "Copy to clipboard: Ctrl+C, ESC",
            initiallyFocused: true,
        }).done();
    }

    handleEvaluate = () => {

        if (this.state.exampleEntity == undefined)
            this.setState({ response : undefined });
        else {
            API.conditionTest({
                workflowCondition: this.props.ctx.value,
                exampleEntity: this.state.exampleEntity,
                decisionResult: this.state.decisionResult
            })
                .then(r => this.setState({ response: r }))
                .done();
        }
    }

    renderTest() {
        const ctx = this.props.ctx;
        const res = this.state.response;
        return (
            <fieldset>
                <legend>TEST</legend>
                {this.renderExampleEntity(ctx.value.mainEntityType!.cleanName)}

                <FormGroup ctx={ctx} labelText="Decision Result">
                    <select value={this.state.decisionResult} className="form-control" onChange={this.handleDecisionResultChange}>
                        {DecisionResultValues.map((v, i) => <option key={i} value={v}>{v}</option>)}
                    </select>
                </FormGroup>
                <br />
                {res && this.renderMessage(res)}
            </fieldset>
        );
    }

    renderExampleEntity(typeName: string) {
        const exampleCtx = new TypeContext<ICaseMainEntity | undefined>(undefined, undefined, PropertyRoute.root(typeName), Binding.create(this.state, s => s.exampleEntity));

        return (
            <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} view={true} onView={this.handleOnView} onChange={this.handleEvaluate}
                type={{ name: typeName }} labelText="Example Entity" />
        );
    }

    handleDecisionResultChange = (e: React.SyntheticEvent<HTMLSelectElement>) => {
        this.setState({ decisionResult: e.currentTarget.value as DecisionResult }, () =>
            this.handleEvaluate());
    }

    handleOnView = (exampleEntity: ICaseMainEntity) => {
        return Navigator.view(exampleEntity, { requiresSaveOperation: false });
    }

    renderMessage(res: WorkflowConditionTestResponse) {
        if (res.compileError)
            return <div className="alert alert-danger">COMPILE ERROR: {res.compileError}</div >;

        if (res.validationException)
            return <div className="alert alert-danger">EXCEPTION: {res.validationException}</div>;

        return (
            <div>
                {
                    res.validationResult == true ?
                        <div className="alert alert-success">True</div> :
                        <div className="alert alert-warning">False</div>
                        
                }
            </div>
        );
    }
}

