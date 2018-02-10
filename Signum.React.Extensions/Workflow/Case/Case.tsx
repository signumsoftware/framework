import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { getMixin, toLite, JavascriptMessage, is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ColorTypeaheadLine } from '../../Basics/Templates/ColorTypeahead'
import { CaseEntity, WorkflowEntity, WorkflowEntitiesDictionary, CaseActivityEntity, WorkflowActivityMessage, WorkflowActivityEntity } from '../Signum.Entities.Workflow'
import {
    ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip,
    EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, EntityTable
} from '../../../../Framework/Signum.React/Scripts/Lines'
import { API, CaseFlow } from '../WorkflowClient'
import CaseFlowViewerComponent from '../Bpmn/CaseFlowViewerComponent'
import InlineCaseTags from "../Case/InlineCaseTags";
import { SearchControl } from "../../../../Framework/Signum.React/Scripts/Search";
import { Tab, Tabs, UncontrolledTabs } from '../../../../Framework/Signum.React/Scripts/Components/Tabs';

interface CaseComponentProps {
    ctx: TypeContext<CaseEntity>;
    caseActivity?: CaseActivityEntity;
}

interface CaseComponentState {
    initialXmlDiagram?: string;
    entities?: WorkflowEntitiesDictionary;
    caseFlow?: CaseFlow;
}

export default class CaseComponent extends React.Component<CaseComponentProps, CaseComponentState> {

    constructor(props: CaseComponentProps) {
        super(props);

        this.state = { };
    }

    caseFlowViewerComponent?: CaseFlowViewerComponent | null;

    loadState(props: CaseComponentProps) {
        API.getWorkflowModel(toLite(props.ctx.value.workflow))
            .then(model => this.setState({
                initialXmlDiagram: model.diagramXml,
                entities: model.entities.toObject(mle => mle.element.bpmnElementId, mle => mle.element.model)
            }))
            .done();

        API.caseFlow(toLite(props.ctx.value))
            .then(caseFlow => this.setState({
                caseFlow: caseFlow
            }))
            .done();
    }

    componentWillReceiveProps(newProps: CaseComponentProps) {
        if (!is(this.props.ctx.value, newProps.ctx.value))
            this.loadState(newProps);
    }

    componentWillMount() {
        this.loadState(this.props);
    }

    render() {
        var ctx = this.props.ctx.subCtx({ readOnly: true, labelColumns: 4 });
        return (
            <div>
                <div className="inline-tags"> <InlineCaseTags case={toLite(this.props.ctx.value)} /></div>
                <br />
                <div className="row">
                    <div className="col-sm-6">
                        <EntityLine ctx={ctx.subCtx(a => a.workflow)} />
                        <EntityLine ctx={ctx.subCtx(a => a.parentCase)} />
                        <ValueLine ctx={ctx.subCtx(a => a.startDate)} />
                    </div>
                    <div className="col-sm-6">
                        <EntityLine ctx={ctx.subCtx(a => a.mainEntity)} />
                        <ValueLine ctx={ctx.subCtx(a => a.description)} />
                        <ValueLine ctx={ctx.subCtx(a => a.finishDate)} />
                    </div>
                </div>

                <UncontrolledTabs id="caseTabs">
                    <Tab eventKey="CaseFlow" title={WorkflowActivityMessage.CaseFlow.niceToString()}>
                        {this.state.initialXmlDiagram && this.state.entities && this.state.caseFlow ?
                            <div className="code-container">
                                <CaseFlowViewerComponent ref={m => this.caseFlowViewerComponent = m}
                                    diagramXML={this.state.initialXmlDiagram}
                                    entities={this.state.entities}
                                    caseFlow={this.state.caseFlow}
                                    case={ctx.value}
                                    caseActivity={this.props.caseActivity}
                                /></div> :
                            <h3>{JavascriptMessage.loading.niceToString()}</h3>}
                    </Tab>
                    <Tab eventKey="CaseActivities" title={WorkflowActivityEntity.nicePluralName()}>
                        <SearchControl findOptions={{
                            queryName: CaseActivityEntity,
                            parentColumn: "Case",
                            parentValue: ctx.value,
                            orderOptions: [{
                                columnName: "StartDate",
                                orderType: "Ascending",
                            }]
                        }} />
                    </Tab>
                </UncontrolledTabs>
            </div>
        );
    }
}