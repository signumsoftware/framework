import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import * as moment from 'moment'
import * as Navigator from '@framework/Navigator'
import { SearchControl, ValueSearchControlLine } from '@framework/Search'
import EntityLink from '@framework/SearchControl/EntityLink'
import * as Operations from '@framework/Operations'
import { getTypeInfos } from '@framework/Reflection'
import { API, SchedulerState, SchedulerItemState, SchedulerRunningTaskState } from './SchedulerClient'
import { ScheduledTaskLogEntity, ScheduledTaskEntity, ScheduledTaskLogOperation } from './Signum.Entities.Scheduler'
import { Lite } from "@framework/Signum.Entities";
import { StyleContext } from "@framework/Lines";
import { useAPIWithReload, useTitle } from '../../../Framework/Signum.React/Scripts/Hooks'

interface SchedulerPanelProps extends RouteComponentProps<{}> {

}

export default function SchedulerPanelPage(p: SchedulerPanelProps) {

  const [state, reloadState] = useAPIWithReload(() => API.view(), []);

  useTitle("SchedulerLogic state");
 
  function handleUpdate(e: React.MouseEvent<any>) {
    e.preventDefault();
    reloadState();
  }

  function handleStop(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.stop().then(() => reloadState()).done();
  }

  function handleStart(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.start().then(() => reloadState()).done();
  }

  
  if (state == undefined)
    return <h2 className="display-6">SchedulerLogic state (loading...) </h2>;

  const s = state;

  const ctx = new StyleContext(undefined, undefined);

  return (
    <div>
      <h2 className="display-6">SchedulerLogic state</h2>
      <div className="btn-toolbar">
        {s.running && <a href="#" className="sf-button btn btn-light active" style={{ color: "red" }} onClick={handleStop}>Stop</a>}
        {!s.running && <a href="#" className="sf-button btn btn-light" style={{ color: "green" }} onClick={handleStart}>Start</a>}
        <a href="#" className="sf-button btn btn-light" onClick={handleUpdate}>Update</a>
      </div >
      <div id="processMainDiv">
        <br />
        State: <strong>
          {s.running ?
            <span style={{ color: "Green" }}> RUNNING </span> :
            <span style={{ color: "Red" }}> STOPPED </span>
          }</strong>
        <br />
        SchedulerMargin: {s.schedulerMargin}
        <br />
        NextExecution: {s.nextExecution} ({s.nextExecution == undefined ? "-None-" : moment(s.nextExecution).fromNow()})
        <br />
        <InMemoryQueue queue={s.queue} onReload={reloadState} />
        <RunningTasks runningTasks={s.runningTask} onReload={reloadState} />

        <h4>Available Tasks</h4>
        <div>
          {getTypeInfos(ScheduledTaskEntity.memberInfo(a => a.task).type).map(t =>
            <ValueSearchControlLine key={t.name} ctx={ctx} findOptions={{ queryName: t.name }} onExplored={reloadState} />)}
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
                <td>{item.nextDate} ({moment(item.nextDate).fromNow()})</td>
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
      .then(() => onReload())
      .done();
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
                <td>{item.startTime} ({moment(item.startTime).fromNow()})</td>
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


