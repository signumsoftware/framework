import * as React from 'react'
import { DateTime } from 'luxon'
import { useLocation, useParams } from 'react-router'
import * as AppContext from '@framework/AppContext'
import { Navigator } from '@framework/Navigator'
import { SearchControl } from '@framework/Search'
import { WorkflowClient } from '../WorkflowClient'
import { CaseActivityEntity, WorkflowActivityType, WorkflowPermission, CaseActivityOperation, WorkflowActivityEntity } from '../Signum.Workflow'
import { Tabs, Tab } from 'react-bootstrap';
import { useAPIWithReload, useInterval } from '@framework/Hooks'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { OperationLogEntity } from '@framework/Signum.Operations'
import { CopyHealthCheckButton } from '../../../Signum/React/Components/CopyHealthCheckButton'
import { LinkButton } from '@framework/Basics/LinkButton'

export default function WorkflowPanelPage(): React.JSX.Element {

  return (
    <div>
      <h2 className="display-6"><FontAwesomeIcon icon={["fas", "shuffle"]} /> Workflow Panel</h2>

      <Tabs id="workflowTabs">
        <Tab title="Script Runner" eventKey="scriptRunner">
          <WorkflowScriptRunnerTab />
        </Tab>
        <Tab title="Timers" eventKey="timers">
          <LinkButton title={undefined} className="sf-button btn btn-link" onClick={e => { window.open(AppContext.toAbsoluteUrl("/scheduler/view")); }}>Open Scheduler Panel</LinkButton>
        </Tab>
      </Tabs>
    </div>
  );
}


export function WorkflowScriptRunnerTab(p: {}): React.JSX.Element {

  const [state, reloadState] = useAPIWithReload(() => {
    AppContext.assertPermissionAuthorized(WorkflowPermission.ViewWorkflowPanel);
    return WorkflowClient.API.view();
  }, [], { avoidReset: true });

  const tick = useInterval(state == null || state.running ? 500 : null, 0, n => n + 1);

  React.useEffect(() => {
    reloadState();
  }, [tick]);

  function handleStop(e: React.MouseEvent<any>) {
    WorkflowClient.API.stop().then(() => reloadState());
  }

  function handleStart(e: React.MouseEvent<any>) {
    WorkflowClient.API.start().then(() => reloadState());
  }

  var title = "WorkflowScriptRunner State";

  if (state == undefined)
    return <h4>{title} (loading...) </h4>;

  const s = state;
  const url = window.location;

  return (
    <div>
      <h4>{title} <CopyHealthCheckButton
        name={url.hostname + " Workflow Runner"}
        healthCheckUrl={url.origin + AppContext.toAbsoluteUrl('/api/workflow/healthCheck')}
        clickUrl={url.href}
      /></h4>
      <div className="btn-toolbar mt-3">
        <button className={classes("sf-button btn", s.running ? "btn-success disabled" : "btn-outline-success")} onClick={!s.running ? handleStart : undefined}><FontAwesomeIcon icon="play" /> Start</button>
        <button className={classes("sf-button btn", !s.running ? "btn-danger disabled" : "btn-outline-danger")} onClick={s.running ? handleStop : undefined}><FontAwesomeIcon icon="stop" /> Stop</button>
      </div >

      <div>
        State: <strong>
          {state.running ?
            <span style={{ color: "green" }}> RUNNING </span> :
            <span style={{ color: state.initialDelayMilliseconds == null ? "gray" : "red" }}> STOPPED </span>
          }</strong> <a className="ms-2" href={AppContext.toAbsoluteUrl("/api/workflow/scriptRunner/simpleStatus")} target="_blank">SimpleStatus</a>
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
                { token: CaseActivityEntity.token(a => a.case) },
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
