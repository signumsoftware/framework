import * as React from 'react'
import { WorkflowEntity, WorkflowModel, WorkflowEntitiesDictionary, BpmnEntityPair, WorkflowOperation } from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, EntityLine, LiteAutocompleteConfig } from '../../../../Framework/Signum.React/Scripts/Lines'
import { is, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { createEntityOperationContext } from '../../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import * as Entities from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals';
import { API, executeWorkflowSave } from '../WorkflowClient'
import BpmnModelerComponent from '../../Workflow/Templates/BpmnModelerComponent'

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
        this.state = {  };
    }

    private bpmnModelerComponent: BpmnModelerComponent | undefined;

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
        if (w.isNew)
        {
            var model = WorkflowModel.New({
                diagramXml : require<string>("raw!./InitialWorkflow.xml"),
                entities : [],
            });

            this.updateState(model);
        }
        else
            API.getWorkflowModel(w.id)
                .then(model => this.updateState(model))
                .done();
    }

    render() {
        var ctx = this.props.ctx;
        return (
            <div>
                <ValueLine ctx={ctx.subCtx(d => d.name)} />
                <EntityLine ctx={ctx.subCtx(d => d.mainEntityType)}
                    autoComplete={new LiteAutocompleteConfig(str => API.findMainEntityType({ subString: str, count: 5 }), false)}
                    find={false}  />
               
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
}