import * as React from 'react'
import { WorkflowEntitiesDictionary, WorkflowConnectionModel, CaseFlowColor, CaseActivityEntity, CaseEntity, WorkflowMessage, WorkflowEventEntity, WorkflowActivityEntity, IWorkflowNodeEntity } from '../Signum.Workflow'
import { JavascriptMessage } from '@framework/Signum.Entities'
import { Dic } from '@framework/Globals'
import { WorkflowClient } from '../WorkflowClient'
import NavigatedViewer from "bpmn-js/lib/NavigatedViewer"
import * as caseFlowRenderer from './CaseFlowRenderer'
import * as connectionIcons from './ConnectionIcons'
import searchPad from 'bpmn-js/lib/features/search'
import * as customMinimap from './CustomMinimap'
import * as BpmnUtils from './BpmnUtils'
import CaseActivityStatsModal from "../Case/CaseActivityStatsModal"
import "diagram-js-minimap/assets/diagram-js-minimap.css"
import "bpmn-js/dist/assets/bpmn-font/css/bpmn-embedded.css"
import "diagram-js/assets/diagram-js.css"
import "./Bpmn.css"
import { DropdownButton, Dropdown, Button } from 'react-bootstrap';


export interface CaseFlowViewerComponentProps {
  diagramXML?: string;
  entities: WorkflowEntitiesDictionary;
  caseFlow: WorkflowClient.CaseFlow;
  case: CaseEntity,
  workflowActivity?: IWorkflowNodeEntity;
}

export interface CaseFlowViewerComponentState {
  caseFlowColor: CaseFlowColor;
}

class CustomViewer extends NavigatedViewer {

}

CustomViewer.prototype._modules =
  CustomViewer.prototype._modules.concat([caseFlowRenderer, customMinimap]);

export default class CaseFlowViewerComponent extends React.Component<CaseFlowViewerComponentProps, CaseFlowViewerComponentState> {

  constructor(props: CaseFlowViewerComponentProps) {
    super(props);

    this.state = { caseFlowColor: CaseFlowColor.value("CaseMaxDuration") };
  }

  viewer!: NavigatedViewer;
  divArea!: HTMLDivElement;

  handleOnModelError = (err: string): void => {
    if (err)
      throw new Error('Error rendering the model ' + err);
    else {
      this.resetZoom();
      if (this.props.workflowActivity) {
        var selection = this.viewer.get("selection") as any;
        selection.select((this.props.workflowActivity as (WorkflowEventEntity | WorkflowActivityEntity)).bpmnElementId);
      }
    }
  }

  componentDidMount(): void {
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

  handleElementDoubleClick = (obj: BPMN.DoubleClickEvent): void => {

    obj.preventDefault();
    obj.stopPropagation();
    this.showCaseActivityStatsModal(obj.element.id);
  }

  showCaseActivityStatsModal(bpmnElementId: string): void {
    const stats = this.props.caseFlow.activities[bpmnElementId];
    if (stats)
      CaseActivityStatsModal.show(this.props.case, stats);
  }

  componentWillUnmount(): void {
    this.viewer.destroy();
  }

  componentWillReceiveProps(nextProps: CaseFlowViewerComponentProps): void {
    if (this.viewer) {
      if (nextProps.diagramXML !== undefined && this.props.diagramXML !== nextProps.diagramXML) {
        this.viewer.importXML(nextProps.diagramXML, this.handleOnModelError);
      }
    }
  }

  configureModules(): void {
    var conIcons = this.viewer.get<connectionIcons.ConnectionIcons>('connectionIcons');
    conIcons.hasAction = con => {
      var mod = this.props.entities[con.id] as (WorkflowConnectionModel | undefined);
      return mod?.action || undefined;
    };

    conIcons.hasCondition = con => {
      var mod = this.props.entities[con.id] as (WorkflowConnectionModel | undefined);
      return mod?.condition || undefined;
    };

    var caseFlowRenderer = this.viewer.get<caseFlowRenderer.CaseFlowRenderer>('caseFlowRenderer');
    caseFlowRenderer.getConnectionType = con => {
      var mod = this.props.entities[con.id] as (WorkflowConnectionModel | undefined);
      return mod?.type || undefined;
    }

    caseFlowRenderer.getDecisionStyle = con => BpmnUtils.findDecisionStyle(con, this.props.entities);

    caseFlowRenderer.viewer = this.viewer;
    caseFlowRenderer.caseFlow = this.props.caseFlow;
    caseFlowRenderer.maxDuration = Dic.getValues(this.props.caseFlow.activities).map(a => a.map(a => a.duration || 0).sum()).max()!;
    caseFlowRenderer.caseFlowColor = this.state.caseFlowColor;


    conIcons.show();
  }

  handleChangeColor = (eventKey: any): void => {
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

  handleSearchClick = (e: React.MouseEvent<HTMLButtonElement>): void => {
    var searchPad = this.viewer.get<any>("searchPad");
    searchPad.toggle();
  }

  handleZoomClick = (e: React.MouseEvent<HTMLButtonElement>): void => {
    this.resetZoom();
  }

  resetZoom(): void {
    var zoomScroll = this.viewer.get<any>("zoomScroll");
    zoomScroll.reset();
  }

  render(): React.JSX.Element {
    return (
      <div>
        <div className="btn-toolbar">
          <Button variant="light" onClick={this.handleZoomClick}>{WorkflowMessage.ResetZoom.niceToString()}</Button>
          <DropdownButton id="colorMenu" variant="light" title={WorkflowMessage.Color.niceToString() + CaseFlowColor.niceToString(this.state.caseFlowColor)}>
            {this.menuItem("CaseMaxDuration")}
            {this.menuItem("AverageDuration")}
            {this.menuItem("EstimatedDuration")}
          </DropdownButton>
          <Button variant="light" onClick={this.handleSearchClick}>{JavascriptMessage.search.niceToString()}</Button>
        </div>
        <div ref={de => { this.divArea = de!; }} />
      </div>
    );
  }

  focusElement(bpmnElementId: string): void {
    var searchPad = this.viewer.get<any>("searchPad");
    searchPad._search(bpmnElementId);
    searchPad._resetOverlay();
  }

  menuItem(color: CaseFlowColor): React.JSX.Element {
    return (
      <Dropdown.Item onClick={() => this.handleChangeColor(color)} active={this.state.caseFlowColor == color}>
        {CaseFlowColor.niceToString(color)}
      </Dropdown.Item>
    );
  }
}
