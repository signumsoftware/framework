import * as React from 'react'
import { DateTime } from 'luxon'
import { RouteComponentProps } from 'react-router'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import { SearchControl } from '@framework/Search'
import { OperationLogEntity } from '@framework/Signum.Entities.Basics'
import { API, WorkflowScriptRunnerState } from '../WorkflowClient'
import { CaseActivityEntity, WorkflowActivityType, WorkflowPermission, CaseActivityOperation, WorkflowActivityEntity } from '../Signum.Entities.Workflow'
import * as AuthClient from '../../Authorization/AuthClient'
import { Tabs, Tab } from 'react-bootstrap';
import { useAPIWithReload } from '@framework/Hooks'

export default function WorkflowPanelPage(p: RouteComponentProps<{}>, {}){
  function componentWillMount() {
    AuthClient.assertPermissionAuthorized(WorkflowPermission.ViewWorkflowPanel);
    AppContext.setTitle("WorkflowPanel State");
  }

  function componentWillUnmount() {
    AppContext.setTitle();
  }

  return (
    <div>
      <h2 className="display-6">Workflow Panel</h2>

      <Tabs id="workflowTabs">
        <Tab title="Script Runner" eventKey="scriptRunner">
          <WorkflowScriptRunnerTab />
        </Tab>
        <Tab title="Timers" eventKey="timers">
          <a href="#" className="sf-button btn btn-link" onClick={e => { e.preventDefault(); window.open(AppContext.toAbsoluteUrl("~/scheduler/view")); }}>Open Scheduler Panel</a>
        </Tab>
      </Tabs>
    </div>
  );
}


export function WorkflowScriptRunnerTab(p: {}) {

  const [state, reloadState] = useAPIWithReload(() => {
    AuthClient.assertPermissionAuthorized(WorkflowPermission.ViewWorkflowPanel);
    return API.view();
  }, []);

  function handleStop(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.stop().then(() => reloadState());
  }

  function handleStart(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.start().then(() => reloadState());
  }

  var title = "WorkflowScriptRunner State";

  if (state == undefined)
    return <h4>{title} (loading...) </h4>;

  return (
    <div>
      <h4>{title}</h4>
      <div className="btn-toolbar">
        {state.running && <a href="#" className="sf-button btn bg-light active" style={{ color: "red" }} onClick={handleStop}>Stop</a>}
        {!state.running && <a href="#" className="sf-button btn bg-light" style={{ color: "green" }} onClick={handleStart}>Start</a>}
      </div >

      <div>
        <br />
        State: <strong>
          {state.running ?
            <span style={{ color: "green" }}> RUNNING </span> :
            <span style={{ color: state.initialDelayMilliseconds == null ? "gray" : "red" }}> STOPPED </span>
          }</strong> <a className="ms-2" href={AppContext.toAbsoluteUrl("~/api/workflow/scriptRunner/simpleStatus")} target="_blank">SimpleStatus</a>
        <br />
        InitialDelayMilliseconds: {state.initialDelayMilliseconds}
        <br />
        CurrentProcessIdentifier: {state.currentProcessIdentifier}
        <br />
        ScriptRunnerPeriod: {state.scriptRunnerPeriod} sec
                  <br />
        NextPlannedExecution: {state.nextPlannedExecution} ({state.nextPlannedExecution == undefined ? "-None-" : DateTime.fromISO(state.nextPlannedExecution).toRelative()})
                  <br />
        IsCancelationRequested: {state.isCancelationRequested}
        <br />
        QueuedItems: {state.queuedItems}
      </div>
      <br />
      <h4>Next activities to execute</h4>
      <SearchControl
        showContextMenu={fo => "Basic"}
        view={false}
        findOptions={{
          queryName: CaseActivityEntity,
          filterOptions: [
            { token: CaseActivityEntity.token(a => a.entity.workflowActivity).cast(WorkflowActivityEntity).append(a => a.type), operation: "EqualTo", value: WorkflowActivityType.value("Script") },
            { token: CaseActivityEntity.token(e => e.entity.doneDate), operation: "EqualTo", value: null }
          ],
          columnOptionsMode: "ReplaceAll",
          columnOptions: [
            { token: CaseActivityEntity.token(e => e.id) },
            { token: CaseActivityEntity.token(e => e.startDate) },
            { token: CaseActivityEntity.token(e => e.workflowActivity).cast(WorkflowActivityEntity).append(a => a.lane!.pool!.workflow) },
            { token: CaseActivityEntity.token(e => e.workflowActivity) },
            { token: CaseActivityEntity.token(e => e.case) },
            { token: CaseActivityEntity.token(e => e.entity.scriptExecution!.nextExecution) },
            { token: CaseActivityEntity.token(e => e.entity.scriptExecution!.retryCount) },
          ],
          orderOptions: [
            { token: CaseActivityEntity.token(e => e.entity.scriptExecution!.nextExecution), orderType: "Ascending" }
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
            view={false}
            findOptions={{
              queryName: CaseActivityEntity,
              filterOptions: [
                { token: CaseActivityEntity.token(e => e.entity.workflowActivity).cast(WorkflowActivityEntity).append(a => a.type), operation: "EqualTo", value: WorkflowActivityType.value("Script") },
                { token: CaseActivityEntity.token(e => e.entity.doneDate), operation: "DistinctTo", value: null }
              ],
              columnOptionsMode: "ReplaceAll",
              columnOptions: [
                { token: CaseActivityEntity.token(a => a.id) },
                { token: CaseActivityEntity.token(e => e.startDate) },
                { token: CaseActivityEntity.token(a => a.workflowActivity).cast(WorkflowActivityEntity).append(a => a.lane!.pool!.workflow) },
                { token: CaseActivityEntity.token(a => a.workflowActivity) },
                { token: CaseActivityEntity.token(a=>a.case) },
                { token: CaseActivityEntity.token(e => e.entity.doneDate) },
                { token: CaseActivityEntity.token(e => e.entity.doneType) },
                { token: CaseActivityEntity.token(a => a.entity.scriptExecution!.nextExecution) },
                { token: CaseActivityEntity.token(a => a.entity.scriptExecution!.retryCount) },
              ],
              orderOptions: [
                { token: CaseActivityEntity.token(e => e.entity.doneDate), orderType: "Descending" }
              ],
              pagination: { elementsPerPage: 10, mode: "Firsts" }
            }} />
        </Tab>
      </Tabs>
    </div>
  );
}




