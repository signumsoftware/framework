import * as React from 'react'
import * as moment from 'moment'
import { RouteComponentProps } from 'react-router'
import * as Navigator from '@framework/Navigator'
import { SearchControl } from '@framework/Search'
import { OperationLogEntity } from '@framework/Signum.Entities.Basics'
import { API, WorkflowScriptRunnerState } from '../WorkflowClient'
import { CaseActivityEntity, WorkflowActivityType, WorkflowPermission, CaseActivityOperation, WorkflowActivityEntity } from '../Signum.Entities.Workflow'
import * as AuthClient from '../../Authorization/AuthClient'
import { Tabs, Tab } from 'react-bootstrap';
import { useAPIWithReload } from '../../../../Framework/Signum.React/Scripts/Hooks'

export default function WorkflowPanelPage(p: RouteComponentProps<{}>, {}){
  function componentWillMount() {
    AuthClient.assertPermissionAuthorized(WorkflowPermission.ViewWorkflowPanel);
    Navigator.setTitle("WorkflowPanel State");
  }

  function componentWillUnmount() {
    Navigator.setTitle();
  }

  return (
    <div>
      <h2 className="display-6">Workflow Panel</h2>

      <Tabs id="workflowTabs">
        <Tab title="Script Runner" eventKey="scriptRunner">
          <WorkflowScriptRunnerTab />
        </Tab>
        <Tab title="Timers" eventKey="timers">
          <a href="#" className="sf-button btn btn-link" onClick={e => { e.preventDefault(); window.open(Navigator.toAbsoluteUrl("~/scheduler/view")); }}>Open Scheduler Panel</a>
        </Tab>
      </Tabs>
    </div>
  );
}


export function WorkflowScriptRunnerTab(p: {}) {

  const [srs, reloadState] = useAPIWithReload(() => {
    AuthClient.assertPermissionAuthorized(WorkflowPermission.ViewWorkflowPanel);
    return API.view();
  }, []);

  function handleStop(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.stop().then(() => reloadState()).done();
  }

  function handleStart(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.start().then(() => reloadState()).done();
  }

  var title = "WorkflowScriptRunner State";

  if (srs == undefined)
    return <h4>{title} (loading...) </h4>;

  return (
    <div>
      <h4>{title}</h4>
      <div className="btn-toolbar">
        {srs.running && <a href="#" className="sf-button btn btn-light active" style={{ color: "red" }} onClick={handleStop}>Stop</a>}
        {!srs.running && <a href="#" className="sf-button btn btn-light" style={{ color: "green" }} onClick={handleStart}>Start</a>}
      </div >

      <div>
        <br />
        State: <strong>
          {srs.running ?
            <span style={{ color: "Green" }}> RUNNING </span> :
            <span style={{ color: "Red" }}> STOPPED </span>
          }</strong>
        <br />
        CurrentProcessIdentifier: {srs.currentProcessIdentifier}
        <br />
        ScriptRunnerPeriod: {srs.scriptRunnerPeriod} sec
                  <br />
        NextPlannedExecution: {srs.nextPlannedExecution} ({srs.nextPlannedExecution == undefined ? "-None-" : moment(srs.nextPlannedExecution).fromNow()})
                  <br />
        IsCancelationRequested: {srs.isCancelationRequested}
        <br />
        QueuedItems: {srs.queuedItems}
      </div>
      <br />
      <h4>Next activities to execute</h4>
      <SearchControl
        showContextMenu={fo => "Basic"}
        navigate={false}
        findOptions={{
          queryName: CaseActivityEntity,
          filterOptions: [
            { token: CaseActivityEntity.token().entity(a => a.workflowActivity).cast(WorkflowActivityEntity).append(a => a.type), operation: "EqualTo", value: WorkflowActivityType.value("Script") },
            { token: CaseActivityEntity.token().entity(e => e.doneDate), operation: "EqualTo", value: null }
          ],
          columnOptionsMode: "Replace",
          columnOptions: [
            { token: CaseActivityEntity.token(e => e.id) },
            { token: CaseActivityEntity.token(e => e.startDate) },
            { token: CaseActivityEntity.token(e => e.workflowActivity).cast(WorkflowActivityEntity).append(a => a.lane!.pool!.workflow) },
            { token: CaseActivityEntity.token(e => e.workflowActivity) },
            { token: CaseActivityEntity.token(e => e.case) },
            { token: CaseActivityEntity.token().entity(e => e.scriptExecution!.nextExecution) },
            { token: CaseActivityEntity.token().entity(e => e.scriptExecution!.retryCount) },
          ],
          orderOptions: [
            { token: CaseActivityEntity.token().entity(e => e.scriptExecution!.nextExecution), orderType: "Ascending" }
          ],
          pagination: { elementsPerPage: 10, mode: "Firsts" }
        }} />
      <Tabs id="workflowScriptTab">
        <Tab title="Last operation logs" eventKey="logs">
          <SearchControl findOptions={{
            queryName: OperationLogEntity,
            filterOptions: [
              {
                token: OperationLogEntity.token(e => e.operation), operation: "IsIn", value: [
                  CaseActivityOperation.ScriptExecute,
                  CaseActivityOperation.ScriptScheduleRetry,
                  CaseActivityOperation.ScriptFailureJump,
                ]
              },
            ],
            pagination: { elementsPerPage: 10, mode: "Firsts" }
          }} />
        </Tab>
        <Tab title="Last executed activities" eventKey="lastActivities">
          <SearchControl
            showContextMenu={fo => "Basic"}
            navigate={false}
            findOptions={{
              queryName: CaseActivityEntity,
              filterOptions: [
                { token: CaseActivityEntity.token().entity(e => e.workflowActivity).cast(WorkflowActivityEntity).append(a => a.type), operation: "EqualTo", value: WorkflowActivityType.value("Script") },
                { token: CaseActivityEntity.token().entity(e => e.doneDate), operation: "DistinctTo", value: null }
              ],
              columnOptionsMode: "Replace",
              columnOptions: [
                { token: CaseActivityEntity.token(a => a.id) },
                { token: CaseActivityEntity.token(e => e.startDate) },
                { token: CaseActivityEntity.token(a => a.workflowActivity).cast(WorkflowActivityEntity).append(a => a.lane!.pool!.workflow) },
                { token: CaseActivityEntity.token(a => a.workflowActivity) },
                { token: CaseActivityEntity.token(a=>a.case) },
                { token: CaseActivityEntity.token().entity(e => e.doneDate) },
                { token: CaseActivityEntity.token().entity(e => e.doneType) },
                { token: CaseActivityEntity.token().entity(a => a.scriptExecution!.nextExecution) },
                { token: CaseActivityEntity.token().entity(a => a.scriptExecution!.retryCount) },
              ],
              orderOptions: [
                { token: CaseActivityEntity.token().entity(e => e.doneDate), orderType: "Descending" }
              ],
              pagination: { elementsPerPage: 10, mode: "Firsts" }
            }} />
        </Tab>
      </Tabs>
    </div>
  );
}




