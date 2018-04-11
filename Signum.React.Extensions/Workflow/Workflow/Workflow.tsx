import * as React from 'react'
import { WorkflowEntity, WorkflowModel, WorkflowEntitiesDictionary, BpmnEntityPairEmbedded, WorkflowOperation, WorkflowMessage } from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, EntityLine, LiteAutocompleteConfig } from '../../../../Framework/Signum.React/Scripts/Lines'
import { is, JavascriptMessage, toLite, ModifiableEntity, Lite, Entity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Entities from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals';
import { API, executeWorkflowSave } from '../WorkflowClient'
import BpmnModelerComponent from '../Bpmn/BpmnModelerComponent'
import MessageModal from "../../../../Framework/Signum.React/Scripts/Modals/MessageModal";

interface WorkflowProps {
    ctx: TypeContext<WorkflowEntity>;
}

interface WorkflowState {
    initialXmlDiagram?: string;
    entities?: WorkflowEntitiesDictionary;
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
                .then(model => this.updateState(model))
                .done();
    }

    render() {
        var ctx = this.props.ctx;
        return (
            <div>
                <ValueLine ctx={ctx.subCtx(d => d.name)} />
                <EntityLine ctx={ctx.subCtx(d => d.mainEntityType)}
                    autoComplete={new LiteAutocompleteConfig((abortController, str) => API.findMainEntityType({ subString: str, count: 5 }), false)}
                    find={false}
                    onRemove={this.handleMainEntityTypeChange} />

                <ValueLine ctx={ctx.subCtx(d => d.mainEntityStrategy)} />
                <fieldset>
                    {this.state.initialXmlDiagram ?
                        <div className="code-container">
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