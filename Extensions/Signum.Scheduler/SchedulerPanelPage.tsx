import * as React from 'react'
import { useLocation, useParams } from 'react-router'
import { DateTime } from 'luxon'
import { Navigator } from '@framework/Navigator'
import { SearchControl, SearchValueLine } from '@framework/Search'
import EntityLink from '@framework/SearchControl/EntityLink'
import { Operations } from '@framework/Operations'
import { tryGetTypeInfos, getTypeInfos } from '@framework/Reflection'
import { SchedulerClient } from './SchedulerClient'
import { ScheduledTaskLogEntity, ScheduledTaskEntity, ScheduledTaskLogOperation, ScheduledTaskMessage } from './Signum.Scheduler'
import { Lite } from "@framework/Signum.Entities";
import { StyleContext } from "@framework/Lines";
import { useAPIWithReload, useInterval } from '@framework/Hooks'
import { toAbsoluteUrl, useTitle } from '@framework/AppContext'
import { classes } from '@framework/Globals'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as AppContext from '@framework/AppContext';
import { CopyHealthCheckButton } from '@framework/Components/CopyHealthCheckButton'
import { AccessibleTable } from '../../Signum/React/Basics/AccessibleTable'


export default function SchedulerPanelPage(): React.JSX.Element {

  const [state, reloadState] = useAPIWithReload(() => SchedulerClient.API.view(), [], { avoidReset: true });

  const tick = useInterval(state == null || state.running ? 500 : null, 0, n => n + 1);

  React.useEffect(() => {
    reloadState();
  }, [tick]);

  useTitle("Scheduler Task Runner");
 

  function handleStop(e: React.MouseEvent<any>) {
    SchedulerClient.API.stop().then(() => reloadState());
  }

  function handleStart(e: React.MouseEvent<any>) {
    SchedulerClient.API.start().then(() => reloadState());
  }

  
  if (state == undefined)
    return <h2 className="display-6">SchedulerLogic state (loading...) </h2>;

  const s = state;

  const ctx = new StyleContext(undefined, undefined);
  const url = window.location;

  return (
    <div>
      <h2 className="display-6"><FontAwesomeIcon aria-hidden="true" icon="clock" />  {ScheduledTaskMessage.SchedulePanel.niceToString()} <CopyHealthCheckButton
        name={url.hostname + " Scheduler Task Runner"}
        healthCheckUrl={url.origin + AppContext.toAbsoluteUrl('/api/scheduler/healthCheck')}
        clickUrl={url.href}
      /></h2>
      <div className="btn-toolbar">
        <button type="button" className={classes("sf-button btn", s.running ? "btn-success disabled" : "btn-outline-success")} onClick={!s.running ? handleStart : undefined}><FontAwesomeIcon aria-hidden="true" icon="play" />  {ScheduledTaskMessage.Start.niceToString()}</button>
        <button type="button" className={classes("sf-button btn", !s.running ? "btn-danger disabled" : "btn-outline-danger")} onClick={s.running ? handleStop : undefined}><FontAwesomeIcon aria-hidden="true" icon="stop" />  {ScheduledTaskMessage.Stop.niceToString()}</button>
      </div >
      <div id="processMainDiv">
        {ScheduledTaskMessage.State.niceToString()}: <strong>
          {s.running ?
            <span style={{ color: "green" }}> {ScheduledTaskMessage.Running.niceToString()} </span> :
            <span style={{ color: state.initialDelayMilliseconds == null ? "gray" : "red" }}> {ScheduledTaskMessage.Stopped.niceToString()} </span>
          }</strong>
        <a className="ms-2" href={toAbsoluteUrl("/api/scheduler/healthCheck")} target="_blank">{ScheduledTaskMessage.SimpleStatus.niceToString()}</a>
        <br />
        {ScheduledTaskMessage.InitialDelayMilliseconds.niceToString()}: {s.initialDelayMilliseconds}
        <br />
        {ScheduledTaskMessage.SchedulerMargin.niceToString()}: {s.schedulerMargin}
        <br />
        {ScheduledTaskMessage.MachineName.niceToString()}: {s.machineName}
        <br />
        {ScheduledTaskMessage.ApplicationName.niceToString()}: {s.applicationName}
        <br />
        {ScheduledTaskMessage.NextExecution.niceToString()}: {s.nextExecution} ({s.nextExecution == undefined ? ScheduledTaskMessage.None.niceToString() : DateTime.fromISO(s.nextExecution).toRelative()})
        <br />
        <InMemoryQueue queue={s.queue} onReload={reloadState} />
        <RunningTasks runningTasks={s.runningTask} onReload={reloadState} />

        <h4>{ScheduledTaskMessage.AvailableTasks.niceToString()}</h4>
        <div>
          {getTypeInfos(ScheduledTaskEntity.memberInfo(a => a.task).type).map(t =>
            <SearchValueLine key={t.name} ctx={ctx} findOptions={{ queryName: t.name }} onExplored={reloadState} />)}
        </div>
        <h4>{ScheduledTaskEntity.niceName()}</h4>
        <SearchControl
          findOptions={{
            queryName: ScheduledTaskEntity,
            pagination: { elementsPerPage: 10, mode: "Firsts" }
          }} />

        <h4>{ScheduledTaskLogEntity.niceName()}</h4>
        <SearchControl
          findOptions={{
            queryName: ScheduledTaskLogEntity,
            orderOptions: [{ token: ScheduledTaskLogEntity.token(e => e.startTime), orderType: "Descending" }],
            pagination: { elementsPerPage: 10, mode: "Firsts" }
          }} />
      </div>
    </div>
  );
}

function InMemoryQueue({ queue, onReload }: { queue: SchedulerClient.SchedulerItemState[], onReload: () => void }) {
  return (
    <div>
      <h4>{ScheduledTaskMessage.InMemoryQueue.niceToString()}</h4>
      {queue.length === 0 ? <p> -- {ScheduledTaskMessage.ThereIsNoActiveScheduledTask.niceToString()} -- </p> :
        <AccessibleTable
          caption={ScheduledTaskMessage.InMemoryQueue.niceToString()}
          className="sf-search-results sf-stats-table">
          <thead>
            <tr>
              <th>{ScheduledTaskMessage.ScheduledTask.niceToString()}</th>
              <th>{ScheduledTaskMessage.Rule.niceToString()}</th>
              <th>{ScheduledTaskMessage.NextDate.niceToString()}</th>
            </tr>
          </thead>
          <tbody>
            {queue.map((item, i) =>
              <tr key={i}>
                <td><EntityLink lite={item.scheduledTask} inSearch="main" onNavigated={onReload} /></td>
                <td>{item.rule} </td>
                <td>{item.nextDate} ({DateTime.fromISO(item.nextDate).toRelative()})</td>
              </tr>)}
          </tbody>
        </AccessibleTable>
      }
    </div>
  );
}


function RunningTasks({ runningTasks, onReload }: { runningTasks: SchedulerClient.SchedulerRunningTaskState[], onReload: () => void }) {

  function handleCancelClick(e: React.MouseEvent<any>, taskLog: Lite<ScheduledTaskLogEntity>) {
    e.preventDefault();

    Operations.API.executeLite(taskLog, ScheduledTaskLogOperation.CancelRunningTask)
      .then(() => onReload());
  }

  return (
    <div>
      <h4>{ScheduledTaskMessage.RunningTasks.niceToString()}</h4>
      {runningTasks.length === 0 ? <p> -- {ScheduledTaskMessage.ThereAreNoTasksRunning.niceToString()} --</p> :
      <AccessibleTable
          caption={ScheduledTaskMessage.RunningTasks.niceToString()}
          className="sf-search-results sf-stats-table">
          <thead>
            <tr>
              <th>{ScheduledTaskMessage.SchedulerTaskLog.niceToString()}</th>
              <th>{ScheduledTaskMessage.StartTime.niceToString()}</th>
              <th>{ScheduledTaskMessage.Remarks.niceToString()}</th>
              <th>{ScheduledTaskMessage.Cancel.niceToString()}</th>
            </tr>
          </thead>
          <tbody>
            {runningTasks.map((item, i) =>
              <tr key={i}>
                <td><EntityLink lite={item.schedulerTaskLog} inSearch="main" onNavigated={onReload} /></td>
                <td>{item.startTime} ({DateTime.fromISO(item.startTime).toRelative()})</td>
                <td><pre>{item.remarks}</pre></td>
                <td><button className="btn btn-tertiary btn-xs btn-danger" type="button" onClick={e => handleCancelClick(e, item.schedulerTaskLog)}>{ScheduledTaskMessage.Cancel.niceToString()}</button></td>
              </tr>)}
          </tbody>
        </AccessibleTable>
      }
    </div>
  );

}


