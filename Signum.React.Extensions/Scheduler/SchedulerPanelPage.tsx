import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import * as moment from 'moment'
import * as Navigator from '@framework/Navigator'
import { SearchControl, ValueSearchControlLine } from '@framework/Search'
import EntityLink from '@framework/SearchControl/EntityLink'
import * as Operations from '@framework/Operations'
import { getTypeInfos } from '@framework/Reflection'
import { API, SchedulerState } from './SchedulerClient'
import { ScheduledTaskLogEntity, ScheduledTaskEntity, ScheduledTaskLogOperation } from './Signum.Entities.Scheduler'
import { Lite } from "@framework/Signum.Entities";
import { StyleContext } from "@framework/Lines";

interface SchedulerPanelProps extends RouteComponentProps<{}> {

}

export default class SchedulerPanelPage extends React.Component<SchedulerPanelProps, SchedulerState> {
  componentWillMount() {
    this.loadState().done();

    Navigator.setTitle("SchedulerLogic state");
  }

  componentWillUnmount() {
    Navigator.setTitle();
  }

  loadState() {
    return API.view()
      .then(s => this.setState(s));
  }

  handleUpdate = (e: React.MouseEvent<any>) => {
    e.preventDefault();
    this.loadState().done();
  }

  handleStop = (e: React.MouseEvent<any>) => {
    e.preventDefault();
    API.stop().then(() => this.loadState()).done();
  }

  handleStart = (e: React.MouseEvent<any>) => {
    e.preventDefault();
    API.start().then(() => this.loadState()).done();
  }

  render() {
    if (this.state == undefined)
      return <h2 className="display-6">SchedulerLogic state (loading...) </h2>;

    const s = this.state;

    const ctx = new StyleContext(undefined, undefined);

    return (
      <div>
        <h2 className="display-6">SchedulerLogic state</h2>
        <div className="btn-toolbar">
          {s.running && <a href="#" className="sf-button btn btn-light active" style={{ color: "red" }} onClick={this.handleStop}>Stop</a>}
          {!s.running && <a href="#" className="sf-button btn btn-light" style={{ color: "green" }} onClick={this.handleStart}>Start</a>}
          <a href="#" className="sf-button btn btn-light" onClick={this.handleUpdate}>Update</a>
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
          {this.renderInMemoryQueue()}
          {this.renderRunningTasks()}

          <h4>Available Tasks</h4>
          <div>
            {getTypeInfos(ScheduledTaskEntity.memberInfo(a => a.task).type).map(t =>
              <ValueSearchControlLine key={t.name} ctx={ctx} findOptions={{ queryName: t.name }} onExplored={() => this.loadState().done()} />)}
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

  renderInMemoryQueue() {
    const s = this.state;
    return (
      <div>
        <h4>In Memory Queue</h4>
        {s.queue.length == 0 ? <p> -- There is no active ScheduledTask -- </p> :
          <table className="sf-search-results sf-stats-table">
            <thead>
              <tr>
                <th>ScheduledTask</th>
                <th>Rule</th>
                <th>NextDate</th>
              </tr>
            </thead>
            <tbody>
              {s.queue.map((item, i) =>
                <tr key={i}>
                  <td><EntityLink lite={item.scheduledTask} inSearch={true} onNavigated={() => this.loadState().done()} /></td>
                  <td>{item.rule} </td>
                  <td>{item.nextDate} ({moment(item.nextDate).fromNow()})</td>
                </tr>)
              }
            </tbody>
          </table>}
      </div>
    );
  }

  handleCancelClick = (e: React.MouseEvent<any>, taskLog: Lite<ScheduledTaskLogEntity>) => {
    e.preventDefault();

    Operations.API.executeLite(taskLog, ScheduledTaskLogOperation.CancelRunningTask)
      .then(() => this.loadState())
      .done();
  }

  renderRunningTasks() {
    const s = this.state;
    return (
      <div>
        <h4>Running Tasks</h4>
        {s.runningTask.length == 0 ? <p> -- There are not tasks running --</p> :
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
              {s.runningTask.map((item, i) =>
                <tr key={i}>
                  <td><EntityLink lite={item.schedulerTaskLog} inSearch={true} onNavigated={() => this.loadState().done()} /></td>
                  <td>{item.startTime} ({moment(item.startTime).fromNow()})</td>
                  <td><pre>{item.remarks}</pre></td>
                  <td><button className="btn btn-light btn-xs btn-danger" type="button" onClick={e => this.handleCancelClick(e, item.schedulerTaskLog)}>Cancel</button></td>
                </tr>)
              }
            </tbody>
          </table>
        }
      </div>
    );
  }
}



