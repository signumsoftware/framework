import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { DateTime } from 'luxon'
import * as Navigator from '@framework/Navigator'
import { SearchControl, SearchValueLine } from '@framework/Search'
import EntityLink from '@framework/SearchControl/EntityLink'
import * as Operations from '@framework/Operations'
import { tryGetTypeInfos, getTypeInfos } from '@framework/Reflection'
import { API, SchedulerState, SchedulerItemState, SchedulerRunningTaskState } from './SchedulerClient'
import { ScheduledTaskLogEntity, ScheduledTaskEntity, ScheduledTaskLogOperation } from './Signum.Entities.Scheduler'
import { Lite } from "@framework/Signum.Entities";
import { StyleContext } from "@framework/Lines";
import { useAPIWithReload, useInterval } from '@framework/Hooks'
import { toAbsoluteUrl, useTitle } from '@framework/AppContext'
import { classes } from '../../Signum.React/Scripts/Globals'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'

interface SchedulerPanelProps extends RouteComponentProps<{}> {

}

export default function SchedulerPanelPage(p: SchedulerPanelProps) {

  const [state, reloadState] = useAPIWithReload(() => API.view(), [], { avoidReset: true });

  const tick = useInterval(state == null || state.running ? 500 : null, 0, n => n + 1);

  React.useEffect(() => {
    reloadState();
  }, [tick]);

  useTitle("SchedulerLogic state");
 

  function handleStop(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.stop().then(() => reloadState());
  }

  function handleStart(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.start().then(() => reloadState());
  }

  
  if (state == undefined)
    return <h2 className="display-6">SchedulerLogic state (loading...) </h2>;

  const s = state;

  const ctx = new StyleContext(undefined, undefined);

  return (
    <div>
      <h2 className="display-6"><FontAwesomeIcon icon={["far", "clock"]} /> Scheduler Panel</h2>
      <div className="btn-toolbar">
        <button className={classes("sf-button btn", s.running ? "btn-success disabled" : "btn-outline-success")} onClick={!s.running ? handleStart : undefined}><FontAwesomeIcon icon="play" /> Start</button>
        <button className={classes("sf-button btn", !s.running ? "btn-danger disabled" :  "btn-outline-danger")} onClick={s.running ? handleStop : undefined}><FontAwesomeIcon icon="stop" /> Stop</button>
      </div >
      <div id="processMainDiv">
        State: <strong>
          {s.running ?
            <span style={{ color: "green" }}> RUNNING </span> :
            <span style={{ color: state.initialDelayMilliseconds == null ? "gray" : "red" }}> STOPPED </span>
          }</strong>
        <a className="ms-2" href={toAbsoluteUrl("~/api/scheduler/simpleStatus")} target="_blank">SimpleStatus</a>
        <br />
        InitialDelayMilliseconds: {s.initialDelayMilliseconds}
        <br />
        SchedulerMargin: {s.schedulerMargin}
        <br />
        MachineName: {s.machineName}
        <br />
        ApplicatonName: {s.applicationName}
        <br />
        NextExecution: {s.nextExecution} ({s.nextExecution == undefined ? "-None-" : DateTime.fromISO(s.nextExecution).toRelative()})
        <br />
        <InMemoryQueue queue={s.queue} onReload={reloadState} />
        <RunningTasks runningTasks={s.runningTask} onReload={reloadState} />

        <h4>Available Tasks</h4>
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

function InMemoryQueue({ queue, onReload }: { queue: SchedulerItemState[], onReload : ()=> void }) {
  return (
    <div>
      <h4>In Memory Queue</h4>
      {queue.length == 0 ? <p> -- There is no active ScheduledTask -- </p> :
        <table className="sf-search-results sf-stats-table">
          <thead>
            <tr>
              <th>ScheduledTask</th>
              <th>Rule</th>
              <th>NextDate</th>
            </tr>
          </thead>
          <tbody>
            {queue.map((item, i) =>
              <tr key={i}>
                <td><EntityLink lite={item.scheduledTask} inSearch={true} onNavigated={onReload} /></td>
                <td>{item.rule} </td>
                <td>{item.nextDate} ({DateTime.fromISO(item.nextDate).toRelative()})</td>
              </tr>)
            }
          </tbody>
        </table>}
    </div>
  );
}

function RunningTasks({ runningTasks, onReload }: { runningTasks: SchedulerRunningTaskState[], onReload: () => void }) {

  function handleCancelClick(e: React.MouseEvent<any>, taskLog: Lite<ScheduledTaskLogEntity>) {
    e.preventDefault();

    Operations.API.executeLite(taskLog, ScheduledTaskLogOperation.CancelRunningTask)
      .then(() => onReload());
  }

  return (
    <div>
      <h4>Running Tasks</h4>
      {runningTasks.length == 0 ? <p> -- There are not tasks running --</p> :
        <table className="sf-search-results sf-stats-table">
          <thead>
            <tr>
              <th>SchedulerTaskLog</th>
              <th>StartTime</th>
              <th>Remarks</th>
              <th>Cancel</th>
            </tr>
          </thead>
          <tbody>
            {runningTasks.map((item, i) =>
              <tr key={i}>
                <td><EntityLink lite={item.schedulerTaskLog} inSearch={true} onNavigated={onReload} /></td>
                <td>{item.startTime} ({DateTime.fromISO(item.startTime).toRelative()})</td>
                <td><pre>{item.remarks}</pre></td>
                <td><button className="btn btn-light btn-xs btn-danger" type="button" onClick={e => handleCancelClick(e, item.schedulerTaskLog)}>Cancel</button></td>
              </tr>)
            }
          </tbody>
        </table>
      }
    </div>
  );
}


