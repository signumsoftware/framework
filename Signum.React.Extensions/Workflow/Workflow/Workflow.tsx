import * as React from 'react'
import { WorkflowEntity, WorkflowModel, WorkflowEntitiesDictionary, BpmnEntityPairEmbedded, WorkflowOperation, WorkflowMessage, WorkflowIssueType } from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, EntityLine, LiteAutocompleteConfig } from '../../../../Framework/Signum.React/Scripts/Lines'
import { is, JavascriptMessage, toLite, ModifiableEntity, Lite, Entity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Entities from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals';
import { API, executeWorkflowSave } from '../WorkflowClient'
import BpmnModelerComponent from '../Bpmn/BpmnModelerComponent'
import MessageModal from "../../../../Framework/Signum.React/Scripts/Modals/MessageModal";

interface WorkflowProps {
    ctx: TypeContext<WorkflowEntity>;
}

interface WorkflowState {
    initialXmlDiagram?: string;
    entities?: WorkflowEntitiesDictionary;
    issues?: Array<API.WorkflowIssue>;
}

export default class Workflow extends React.Component<WorkflowProps, WorkflowState> {

    constructor(props: WorkflowProps) {
        super(props);
        this.state = {};
    }

    bpmnModelerComponent?: BpmnModelerComponent | null;

    getXml(): Promise<string> {
        return this.bpmnModelerComponent!.getXml();
    }

    getSvg(): Promise<string> {
        return this.bpmnModelerComponent!.getSvg();
    }

    componentWillMount() {
        this.loadXml(this.props.ctx.value);
    }

    componentWillReceiveProps(newProps: WorkflowProps) {
        if (!is(this.props.ctx.value, newProps.ctx.value, true)) {
            this.loadXml(newProps.ctx.value);
        }
    }

    updateState(model: WorkflowModel) {
        this.setState({
            initialXmlDiagram: model.diagramXml,
            entities: model.entities.toObject(mle => mle.element.bpmnElementId, mle => mle.element.model)
        });
    }

    setIssues(issues: Array<API.WorkflowIssue>) {
        this.setState({ issues: issues });
    }

    loadXml(w: WorkflowEntity) {
        if (w.isNew) {
            require(["raw-loader!./InitialWorkflow.xml"], (xml) =>
                this.updateState(WorkflowModel.New({
                    diagramXml: xml,
                    entities: [],
                })));
        }
        else
            API.getWorkflowModel(toLite(w))
                .then(pair => {
                    this.updateState(pair.model);
                    this.setIssues(pair.issues);
                })
                .done();
    }

    handleHighlightClick = (e: React.MouseEvent<HTMLAnchorElement>, issue: API.WorkflowIssue) => {
        e.preventDefault();
        if (this.bpmnModelerComponent)
            this.bpmnModelerComponent.focusElement(issue.BpmnElementId);
    }

    render() {
        var ctx = this.props.ctx.subCtx({ labelColumns: 4 });
        return (
            <div>
                <div className="row">
                    <div className="col-sm-6">
                <ValueLine ctx={ctx.subCtx(d => d.name)} />
                <EntityLine ctx={ctx.subCtx(d => d.mainEntityType)}
                    autoComplete={new LiteAutocompleteConfig((abortController, str) => API.findMainEntityType({ subString: str, count: 5 }), false, false)}
                    find={false}
                    onRemove={this.handleMainEntityTypeChange} />
                    </div>
                    <div className="col-sm-6">
                        <ValueLine ctx={ctx.subCtx(d => d.mainEntityStrategy)} />
                        <ValueLine ctx={ctx.subCtx(d => d.expirationDate)} />
                    </div>
                </div>
                {this.renderIssues()}
                <fieldset>
                    {this.state.initialXmlDiagram ?
                        <div>
                            <BpmnModelerComponent ref={m => this.bpmnModelerComponent = m}
                                workflow={ctx.value}
                                diagramXML={this.state.initialXmlDiagram}
                                entities={this.state.entities!}
                            /></div> :
                        <h3>{JavascriptMessage.loading.niceToString()}</h3>}

                </fieldset>
            </div>
        );
    }

    renderIssues() {

        if (this.state.issues == null)
            return null;

        var color = this.state.issues.length == 0 ? "success" :
            this.state.issues.some(a => a.Type == "Error") ? "danger" : "warning";

        return (
            <div className={classes(`card border-${color}`, "code-container")} role="alert">
                <h5 className={`card-header border-${color} text-${color}`}>Worflow Issues</h5>
                <ul style={{ listStyleType: "none", marginBottom: "0px" }} className="card-body">

                    {this.state.issues.length == 0 ?
                        <li>
                            <i className="fa fa-check text-success mr-1" aria-hidden="true" />
                            {"-- No issues --"}
                        </li> : 
                        this.state.issues.map((issue, i) =>

                        <li key={i}>
                            {issue.Type == "Error" ?
                                <i className="fa fa-times-circle text-danger mr-1" aria-hidden="true" /> :
                                <i className="fa fa-exclamation-triangle text-warning mr-1" aria-hidden="true" />}

                            {issue.BpmnElementId && <span className="mr-1">(in <a href="#" onClick={e => this.handleHighlightClick(e, issue)}>{issue.BpmnElementId}</a>)</span>}
                            {issue.Message}

                        </li>
                    )}
                </ul>
            </div>
        );
    }

    handleMainEntityTypeChange = (entity: ModifiableEntity | Lite<Entity>): Promise<boolean> => {
        if (this.bpmnModelerComponent!.existsMainEntityTypeRelatedNodes()) {
            return MessageModal.show({
                title: JavascriptMessage.error.niceToString(),
                message: WorkflowMessage.ChangeWorkflowMainEntityTypeIsNotAllowedBecauseWeHaveNodesThatUseIt.niceToString(),
                buttons: "ok",
                icon: "warning",
                style: "warning",
            }).then(a=>false)
        }
        else
            return Promise.resolve(true);
    }
}