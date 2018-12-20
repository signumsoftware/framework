import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { toLite, JavascriptMessage, is } from '@framework/Signum.Entities'
import { CaseEntity, WorkflowEntitiesDictionary, CaseActivityEntity, WorkflowActivityMessage, WorkflowActivityEntity } from '../Signum.Entities.Workflow'
import { ValueLine, EntityLine, TypeContext } from '@framework/Lines'
import { API, CaseFlow } from '../WorkflowClient'
import * as OrderUtils from '@framework/Frames/OrderUtils'
import CaseFlowViewerComponent from '../Bpmn/CaseFlowViewerComponent'
import InlineCaseTags from "../Case/InlineCaseTags";
import { SearchControl, SearchControlLoaded } from "@framework/Search";
import * as Navigator from "@framework/Navigator";
import { Tab, Tabs } from '@framework/Components/Tabs';
import { UncontrolledTooltip } from "@framework/Components";
import { ResultRow } from '@framework/FindOptions';

type CaseTab = "CaseFlow" | "CaseActivities" | "InprogressCaseActivities";

interface CaseComponentProps {
  ctx: TypeContext<CaseEntity>;
  caseActivity?: CaseActivityEntity;
}

interface CaseComponentState {
  initialXmlDiagram?: string;
  entities?: WorkflowEntitiesDictionary;
  caseFlow?: CaseFlow;
  activeEventKey: CaseTab;
}

export default class CaseComponent extends React.Component<CaseComponentProps, CaseComponentState> {

  constructor(props: CaseComponentProps) {
    super(props);

    this.state = { activeEventKey: "CaseFlow" };
  }

  caseFlowViewerComponent?: CaseFlowViewerComponent | null;

  loadState(props: CaseComponentProps) {
    API.getWorkflowModel(toLite(props.ctx.value.workflow))
      .then(pair => this.setState({
        initialXmlDiagram: pair.model.diagramXml,
        entities: pair.model.entities.toObject(mle => mle.element.bpmnElementId, mle => mle.element.model)
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
            <EntityLine ctx={ctx.subCtx(a => a.mainEntity)} view={false} />
            <ValueLine ctx={ctx.subCtx(a => a.description)} />
            <ValueLine ctx={ctx.subCtx(a => a.finishDate)} />
          </div>
        </div>

        <Tabs id="caseTabs" hideOnly={true} activeEventKey={this.state.activeEventKey} toggle={this.handleToggle}>
          <Tab eventKey={"CaseFlow" as CaseTab} title={WorkflowActivityMessage.CaseFlow.niceToString()}>
            {this.state.initialXmlDiagram && this.state.entities && this.state.caseFlow ?
              <div>
                <CaseFlowViewerComponent ref={m => this.caseFlowViewerComponent = m}
                  diagramXML={this.state.initialXmlDiagram}
                  entities={this.state.entities}
                  caseFlow={this.state.caseFlow}
                  case={ctx.value}
                  caseActivity={this.props.caseActivity}
                /></div> :
              <h3>{JavascriptMessage.loading.niceToString()}</h3>}
          </Tab>
          <Tab eventKey={"CaseActivities" as CaseTab} title={WorkflowActivityEntity.nicePluralName()}>
            <SearchControl
              showContextMenu="Basic"
              navigate={false}
              findOptions={{
                queryName: CaseActivityEntity,
                parentToken: CaseActivityEntity.token(e => e.case),
                parentValue: ctx.value,
                columnOptionsMode: "Replace",
                columnOptions: [
                  { token: CaseActivityEntity.token(e => e.id) },
                  { token: CaseActivityEntity.token(e => e.workflowActivity) },
                  { token: CaseActivityEntity.token(e => e.startDate) },
                  { token: CaseActivityEntity.token(e => e.doneDate) },
                  { token: CaseActivityEntity.token(e => e.doneBy) },
                  { token: CaseActivityEntity.token(a => a.previous).expression("ToString") },
                ],
                orderOptions: [{
                  token: CaseActivityEntity.token(e => e.startDate),
                  orderType: "Ascending",
                }],
              }}
              extraButtons={sc => [
                OrderUtils.setOrder(-1.1, <CaseActivityStatsButtonComponent sc={sc} caseFlowViewer={this.caseFlowViewerComponent!} />),
                OrderUtils.setOrder(-1.2, <WorkflowActivityLocateButtonComponent sc={sc} caseFlowViewer={this.caseFlowViewerComponent!} onLocated={this.handleOnDiagramNodeLocated} />),
              ]}
            />
          </Tab>
          <Tab eventKey={"InprogressCaseActivities" as CaseTab} title={WorkflowActivityMessage.InprogressWorkflowActivities.niceToString()}>
            <SearchControl
              showContextMenu="Basic"
              navigate={false}
              findOptions={{
                queryName: CaseActivityEntity,
                parentToken: CaseActivityEntity.token(e => e.case),
                parentValue: ctx.value,
                filterOptions: [
                  { token: CaseActivityEntity.token(e => e.doneDate), operation: "EqualTo", value: null, frozen: true },
                ],
                columnOptionsMode: "Replace",
                columnOptions: [
                  { token: CaseActivityEntity.token(e => e.id) },
                  { token: CaseActivityEntity.token(e => e.workflowActivity) },
                  { token: CaseActivityEntity.token(e => e.startDate) },
                  { token: CaseActivityEntity.token(e => e.doneDate) },
                  { token: CaseActivityEntity.token(e => e.doneBy) },
                  { token: CaseActivityEntity.token(a => a.previous).expression("ToString") },
                ],
                orderOptions: [{
                  token: CaseActivityEntity.token(e => e.startDate),
                  orderType: "Descending",
                }]
              }}
              extraButtons={sc => [
                OrderUtils.setOrder(-1.1, <CaseActivityStatsButtonComponent sc={sc} caseFlowViewer={this.caseFlowViewerComponent!} />),
                OrderUtils.setOrder(-1.2, <WorkflowActivityLocateButtonComponent sc={sc} caseFlowViewer={this.caseFlowViewerComponent!} onLocated={this.handleOnDiagramNodeLocated} />),
              ]}
            />
          </Tab>
        </Tabs>
      </div>
    );
  }

  handleToggle = (eventKey: string | number) => {
    if (this.state.activeEventKey !== eventKey)
      this.setState({ activeEventKey: eventKey as CaseTab });
  }

  handleOnDiagramNodeLocated = () => {
    this.setState({ activeEventKey: "CaseFlow" });
  }
}

interface CaseActivityButtonBaseProps {
  sc: SearchControlLoaded;
  caseFlowViewer: CaseFlowViewerComponent;
}

class CaseActivityStatsButtonComponent extends React.Component<CaseActivityButtonBaseProps> {

  render() {
    const sc = this.props.sc;
    let Div: HTMLDivElement | null;

    const enabled = sc.state.selectedRows && sc.state.selectedRows.length == 1;

    return (
      [
        <div ref={comp => Div = comp}>
          <a className={classes("sf-line-button btn btn-light", enabled ? undefined : "disabled")}
            onClick={() => this.handleOnClick(sc.state.selectedRows![0])}>
            <FontAwesomeIcon icon="list" />
          </a>
        </div>,
        <UncontrolledTooltip placement="top" key="tooltip" target={() => Div!}>
          {WorkflowActivityMessage.OpenCaseActivityStats.niceToString()}
        </UncontrolledTooltip>
      ]
    );
  }

  handleOnClick(rr: ResultRow) {
    if (rr.entity)
      Navigator.API.fetchAndForget(rr.entity).then(caseActivity => {
        const bpmnElementID = ((caseActivity as CaseActivityEntity).workflowActivity as any).bpmnElementId;
        this.props.caseFlowViewer.showCaseActivityStatsModal(bpmnElementID);
      }).done();
  }
}

interface WorkflowActivityLocateButtonComponentProps extends CaseActivityButtonBaseProps {
  onLocated?: () => void;
}

class WorkflowActivityLocateButtonComponent extends React.Component<WorkflowActivityLocateButtonComponentProps> {

  render() {
    const sc = this.props.sc;
    let Div: HTMLDivElement | null;

    const enabled = sc.state.selectedRows && sc.state.selectedRows.length == 1;
    return (
      [
        <div ref={comp => Div = comp}>
          <a className={classes("sf-line-button btn btn-light", enabled ? undefined : "disabled")}
            onClick={() => this.handleOnClick(sc.state.selectedRows![0])}>
            <FontAwesomeIcon icon="map-marker" />
          </a>
        </div>,
        <UncontrolledTooltip placement="top" key="tooltip" target={() => Div!}>
          {WorkflowActivityMessage.LocateWorkflowActivityInDiagram.niceToString()}
        </UncontrolledTooltip>
      ]
    );
  }

  handleOnClick(rr: ResultRow) {
    if (rr.entity) {
      Navigator.API.fetchAndForget(rr.entity).then(caseActivity => {
        const bpmnElementID = ((caseActivity as CaseActivityEntity).workflowActivity as any).bpmnElementId;
        this.props.caseFlowViewer.focusElement(bpmnElementID);

        if (this.props.onLocated)
          this.props.onLocated();
      }).done();
    }
  }
}

