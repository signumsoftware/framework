import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { toLite, JavascriptMessage, is } from '@framework/Signum.Entities'
import { CaseEntity, WorkflowEntitiesDictionary, CaseActivityEntity, WorkflowActivityMessage, WorkflowActivityEntity, WorkflowPermission } from '../Signum.Entities.Workflow'
import { ValueLine, EntityLine, TypeContext } from '@framework/Lines'
import { API, CaseFlow } from '../WorkflowClient'
import CaseFlowViewerComponent from '../Bpmn/CaseFlowViewerComponent'
import InlineCaseTags from "../Case/InlineCaseTags";
import { SearchControl, SearchControlLoaded } from "@framework/Search";
import * as Navigator from "@framework/Navigator";
import { Tooltip, Tab, Tabs, OverlayTrigger } from "react-bootstrap";
import { ResultRow } from '@framework/FindOptions';
import * as AuthClient from '../../Authorization/AuthClient'
import { useAPI } from '../../../../Framework/Signum.React/Scripts/Hooks'

type CaseTab = "CaseFlow" | "CaseActivities" | "InprogressCaseActivities";

interface CaseComponentProps {
  ctx: TypeContext<CaseEntity>;
  caseActivity?: CaseActivityEntity;
}

export default function CaseComponent(p: CaseComponentProps) {

  const [activeEventKey, setActiveEventKey] = React.useState<CaseTab>("CaseFlow");

  const caseFlowViewerComponentRef = React.useRef<CaseFlowViewerComponent>(null);

  const model = useAPI(() =>
    !AuthClient.isPermissionAuthorized(WorkflowPermission.ViewCaseFlow) ? Promise.resolve(undefined) :
      API.getWorkflowModel(toLite(p.ctx.value.workflow)).then(pair => ({
        initialXmlDiagram: pair.model.diagramXml,
        entities: pair.model.entities.toObject(mle => mle.element.bpmnElementId, mle => mle.element.model!)
      })), [p.ctx.value.workflow]);

  const caseFlow = useAPI(() => !AuthClient.isPermissionAuthorized(WorkflowPermission.ViewCaseFlow) ? Promise.resolve(undefined) : API.caseFlow(toLite(p.ctx.value)), [p.ctx.value]);

  function handleToggle(eventKey: unknown) {
    if (activeEventKey !== eventKey)
      setActiveEventKey(eventKey as CaseTab);
  }

  function handleOnDiagramNodeLocated() {
    setActiveEventKey("CaseFlow");
  }

  var ctx = p.ctx.subCtx({ readOnly: true, labelColumns: 4 });
  return (
    <div>
      <div className="inline-tags"> <InlineCaseTags case={toLite(p.ctx.value)} /></div>
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

      {AuthClient.isPermissionAuthorized(WorkflowPermission.ViewCaseFlow) &&
        <Tabs id="caseTabs" unmountOnExit={false} activeKey={activeEventKey} onSelect={handleToggle}>
          <Tab eventKey={"CaseFlow" as CaseTab} title={WorkflowActivityMessage.CaseFlow.niceToString()}>
            {model && caseFlow ?
              <div>
                <CaseFlowViewerComponent ref={caseFlowViewerComponentRef}
                  diagramXML={model.initialXmlDiagram}
                  entities={model.entities}
                  caseFlow={caseFlow}
                  case={ctx.value}
                  caseActivity={p.caseActivity}
                /></div> :
              <h3>{JavascriptMessage.loading.niceToString()}</h3>}
          </Tab>
          <Tab eventKey={"CaseActivities" as CaseTab} title={WorkflowActivityEntity.nicePluralName()}>
            <SearchControl
              showContextMenu={fo => "Basic"}
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
                { order: -1.1, button: <CaseActivityStatsButtonComponent sc={sc} caseFlowViewer={caseFlowViewerComponentRef.current!} /> },
                { order: -1.2, button: <WorkflowActivityLocateButtonComponent sc={sc} caseFlowViewer={caseFlowViewerComponentRef.current!} onLocated={handleOnDiagramNodeLocated} /> },
              ]}
            />
          </Tab>
          <Tab eventKey={"InprogressCaseActivities" as CaseTab} title={WorkflowActivityMessage.InprogressWorkflowActivities.niceToString()}>
            <SearchControl
              showContextMenu={fo => "Basic"}
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
                { order: -1.1, button: <CaseActivityStatsButtonComponent sc={sc} caseFlowViewer={caseFlowViewerComponentRef.current!} /> },
                { order: -1.2, button: <WorkflowActivityLocateButtonComponent sc={sc} caseFlowViewer={caseFlowViewerComponentRef.current!} onLocated={handleOnDiagramNodeLocated} /> },
              ]}
            />
          </Tab>
        </Tabs>}
    </div>
  );
}

interface CaseActivityButtonBaseProps {
  sc: SearchControlLoaded;
  caseFlowViewer: CaseFlowViewerComponent;
}

function CaseActivityStatsButtonComponent(p: CaseActivityButtonBaseProps) {

  function handleOnClick(rr: ResultRow) {
    if (rr.entity)
      Navigator.API.fetchAndForget(rr.entity).then(caseActivity => {
        const bpmnElementID = ((caseActivity as CaseActivityEntity).workflowActivity as any).bpmnElementId;
        p.caseFlowViewer.showCaseActivityStatsModal(bpmnElementID);
      }).done();
  }
  const sc = p.sc;

  const enabled = sc.state.selectedRows && sc.state.selectedRows.length == 1;

  return (
    <OverlayTrigger overlay={<Tooltip placement="top" key="tooltip" id="caseStatsTooltip">
      {WorkflowActivityMessage.OpenCaseActivityStats.niceToString()}
    </Tooltip>}>
      <div>
        <a className={classes("sf-line-button btn btn-light", enabled ? undefined : "disabled")}
          onClick={() => handleOnClick(sc.state.selectedRows![0])}>
          <FontAwesomeIcon icon="list" />
        </a>
      </div>
    </OverlayTrigger>
  );
}

interface WorkflowActivityLocateButtonComponentProps extends CaseActivityButtonBaseProps {
  onLocated?: () => void;
}

function WorkflowActivityLocateButtonComponent(p: WorkflowActivityLocateButtonComponentProps) {

  function handleOnClick(rr: ResultRow) {
    if (rr.entity) {
      Navigator.API.fetchAndForget(rr.entity).then(caseActivity => {
        const bpmnElementID = ((caseActivity as CaseActivityEntity).workflowActivity as any).bpmnElementId;
        p.caseFlowViewer.focusElement(bpmnElementID);

        if (p.onLocated)
          p.onLocated();
      }).done();
    }
  }
  const sc = p.sc;

  const enabled = sc.state.selectedRows && sc.state.selectedRows.length == 1;
  return (
    <OverlayTrigger overlay={<Tooltip placement="top" id="activityLocatorPopupt">
      {WorkflowActivityMessage.LocateWorkflowActivityInDiagram.niceToString()}
    </Tooltip>}>
      <div>
        <a className={classes("sf-line-button btn btn-light", enabled ? undefined : "disabled")}
          onClick={() => handleOnClick(sc.state.selectedRows![0])}>
          <FontAwesomeIcon icon="map-marker" />
        </a>
      </div>
    </OverlayTrigger>
  );
}

