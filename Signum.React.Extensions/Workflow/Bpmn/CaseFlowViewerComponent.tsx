import * as React from 'react'
import {
    WorkflowEntitiesDictionary, WorkflowActivityModel, WorkflowActivityType, WorkflowPoolModel, WorkflowLaneModel, WorkflowConnectionModel, WorkflowEventModel, WorkflowEntity,
    IWorkflowNodeEntity, CaseFlowColor, CaseActivityEntity, CaseEntity, WorkflowMessage
} from '../Signum.Entities.Workflow'
import { JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { CaseFlow } from '../WorkflowClient'
import * as NavigatedViewer from "bpmn-js/lib/NavigatedViewer"
import * as caseFlowRenderer from './CaseFlowRenderer'
import * as connectionIcons from './ConnectionIcons'
import * as searchPad from 'bpmn-js/lib/features/search'
import * as BpmnUtils from './BpmnUtils'
import CaseActivityStatsModal from "../Case/CaseActivityStatsModal"
import "bpmn-js/dist/assets/bpmn-font/css/bpmn-embedded.css"
import "diagram-js/assets/diagram-js.css"
import "./Bpmn.css"
import { Button, UncontrolledDropdown, DropdownToggle, DropdownMenu, DropdownItem } from '../../../../Framework/Signum.React/Scripts/Components';

export interface CaseFlowViewerComponentProps {
    diagramXML?: string;
    entities: WorkflowEntitiesDictionary;
    caseFlow: CaseFlow;
    case: CaseEntity,
    caseActivity?: CaseActivityEntity;
}

export interface CaseFlowViewerComponentState {
    caseFlowColor: CaseFlowColor;
}

class CustomViewer extends NavigatedViewer {

}

CustomViewer.prototype._modules =
    CustomViewer.prototype._modules.concat([caseFlowRenderer]);

export default class CaseFlowViewerComponent extends React.Component<CaseFlowViewerComponentProps, CaseFlowViewerComponentState> {

    constructor(props: CaseFlowViewerComponentProps) {
        super(props);

        this.state = { caseFlowColor: CaseFlowColor.value("CaseMaxDuration") };
    }

    viewer!: NavigatedViewer;
    divArea!: HTMLDivElement;

    handleOnModelError = (err: string) => {
        if (err)
            throw new Error('Error rendering the model ' + err);
        else {

            if (this.props.caseActivity) {
                var sp = this.viewer.get("searchPad") as any;
                sp._search(this.props.caseActivity.workflowActivity.bpmnElementId);
            }
        }
    }

    componentDidMount() {
        this.viewer = new CustomViewer({
            container: this.divArea,
            keyboard: {
                bindTo: document
            },
            height: 500,
            additionalModules: [
                connectionIcons,
                searchPad,
            ]
        });
        this.configureModules();
        if (this.props.diagramXML && this.props.diagramXML.trim() != "") {
            this.viewer.on('element.dblclick', 1500, this.handleElementDoubleClick as (obj: BPMN.Event) => void);
            this.viewer.importXML(this.props.diagramXML, this.handleOnModelError);
        }
    }

    handleElementDoubleClick = (obj: BPMN.DoubleClickEvent) => {

        const stats = this.props.caseFlow.Activities[obj.element.id];
        if (stats) {
            obj.preventDefault();
            obj.stopPropagation();

            CaseActivityStatsModal.show(this.props.case, stats);
        }
    }

    componentWillUnmount() {
        this.viewer.destroy();
    }

    componentWillReceiveProps(nextProps: CaseFlowViewerComponentProps) {
        if (this.viewer) {
            if (nextProps.diagramXML !== undefined && this.props.diagramXML !== nextProps.diagramXML) {
                this.viewer.importXML(nextProps.diagramXML, this.handleOnModelError);
            }
        }
    }

    configureModules() {
        var conIcons = this.viewer.get<connectionIcons.ConnectionIcons>('connectionIcons');
        conIcons.hasAction = con => {
            var mod = this.props.entities[con.id] as (WorkflowConnectionModel | undefined);
            return mod && mod.action || undefined;
        };

        conIcons.hasCondition = con => {
            var mod = this.props.entities[con.id] as (WorkflowConnectionModel | undefined);
            return mod && mod.condition || undefined;
        };

        var caseFlowRenderer = this.viewer.get<caseFlowRenderer.CaseFlowRenderer>('caseFlowRenderer');
        caseFlowRenderer.getDecisionResult = con => {
            var mod = this.props.entities[con.id] as (WorkflowConnectionModel | undefined);
            return mod && mod.decisonResult || undefined;
        }

        caseFlowRenderer.viewer = this.viewer;
        caseFlowRenderer.caseFlow = this.props.caseFlow;
        caseFlowRenderer.maxDuration = Dic.getValues(this.props.caseFlow.Activities).map(a => a.map(a => a.Duration || 0).sum()).max()!;
        caseFlowRenderer.caseFlowColor = this.state.caseFlowColor;


        conIcons.show();
    }

    handleChangeColor = (eventKey: any) => {
        this.setState({ caseFlowColor: eventKey });
        var caseFlowRenderer = this.viewer.get<caseFlowRenderer.CaseFlowRenderer>('caseFlowRenderer');
        caseFlowRenderer.caseFlowColor = eventKey;

        var reg = this.viewer.get<BPMN.ElementRegistry>("elementRegistry");
        var gFactory = this.viewer.get<BPMN.GraphicsFactory>("graphicsFactory");
        reg.getAll().forEach(a => {

            const type = BpmnUtils.isConnection(a.type) ? "connection" : "shape";
            const gfx = reg.getGraphics(a);
            gFactory.update(type, a, gfx);
        });
    }

    handleSearchClick = (e: React.MouseEvent<HTMLButtonElement>) => {
        var searchPad = this.viewer.get<any>("searchPad");
        searchPad.toggle();
    }

    handleZoomClick = (e: React.MouseEvent<HTMLButtonElement>) => {
        var zoomScroll = this.viewer.get<any>("zoomScroll");
        zoomScroll.reset();
    }

    render() {
        return (
            <div>
                <div className="btn-toolbar">
                    <Button color="light" onClick={this.handleZoomClick}>{WorkflowMessage.ResetZoom.niceToString()}</Button>
                    <UncontrolledDropdown id="colorMenu">
                        <DropdownToggle color="light" caret>
                            {WorkflowMessage.Color.niceToString() + CaseFlowColor.niceToString(this.state.caseFlowColor)}
                        </DropdownToggle>
                        <DropdownMenu>
                            {this.menuItem("CaseMaxDuration")}
                            {this.menuItem("AverageDuration")}
                            {this.menuItem("EstimatedDuration")}
                        </DropdownMenu>
                    </UncontrolledDropdown>
                    <Button color="light" onClick={this.handleSearchClick}>{JavascriptMessage.search.niceToString()}</Button>
                </div>
                <div ref={de => this.divArea = de!} />
            </div>
        );
    }


    menuItem(color: CaseFlowColor) {
        return (
            <DropdownItem onClick={() => this.handleChangeColor(color)} active={this.state.caseFlowColor == color}>
                {CaseFlowColor.niceToString(color)}
            </DropdownItem>
        );
    }
}
