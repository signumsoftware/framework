import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import * as numbro from 'numbro'
import * as moment from 'moment'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { ValueSearchControl, SearchControl, ValueSearchControlLine } from '../../../Framework/Signum.React/Scripts/Search'
import EntityLink from '../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { QueryDescription, SubTokensOptions } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, SchedulerState } from './SchedulerClient'
import { ScheduledTaskLogEntity, ScheduledTaskEntity, ScheduledTaskLogOperation } from './Signum.Entities.Scheduler'
import { Lite } from "../../../Framework/Signum.React/Scripts/Signum.Entities";
import { StyleContext } from "../../../Framework/Signum.React/Scripts/Lines";

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
            return <h2>SchedulerLogic state (loading...) </h2>;

        const s = this.state;

        const ctx = new StyleContext(undefined, undefined);

        return (
            <div>
                <h2>SchedulerLogic state</h2>
                <div className="btn-toolbar">
                    {s.Running && <a href="#" className="sf-button btn btn-light active" style={{ color: "red" }} onClick={this.handleStop}>Stop</a>}
                    {!s.Running && <a href="#" className="sf-button btn btn-light" style={{ color: "green" }} onClick={this.handleStart}>Start</a>}
                    <a href="#" className="sf-button btn btn-light" onClick={this.handleUpdate}>Update</a>
                </div >
                <div id="processMainDiv">
                    <br />
                    State: <strong>
                        {s.Running ?
                            <span style={{ color: "Green" }}> RUNNING </span> :
                            <span style={{ color: "Red" }}> STOPPED </span>
                        }</strong>
                    <br />
                    SchedulerMargin: {s.SchedulerMargin}
                    <br />
                    NextExecution: {s.NextExecution} ({s.NextExecution == undefined ? "-None-" : moment(s.NextExecution).fromNow()})
                    <br />
                    {this.renderInMemoryQueue()}
                    {this.renderRunningTasks()}

                    <h3>Available Tasks</h3>
                    <div>
                        {getTypeInfos(ScheduledTaskEntity.memberInfo(a => a.task).type).map(t =>
                            <ValueSearchControlLine key={t.name} ctx={ctx} findOptions={{ queryName: t.name }} onExplored={() => this.loadState().done()} />)}
                    </div>
                    <h3>{ScheduledTaskEntity.niceName()}</h3>
                    <SearchControl
                        findOptions={{
                            queryName: ScheduledTaskEntity,
                            pagination: { elementsPerPage: 10, mode: "Firsts" }
                        }} />

                    <h3>{ScheduledTaskLogEntity.niceName()}</h3>
                    <SearchControl 
                        findOptions={{
                            queryName: ScheduledTaskLogEntity,
                            orderOptions: [{ columnName: "StartTime", orderType: "Descending" }],
                            pagination: { elementsPerPage: 10, mode: "Firsts" }
                        }}/>
                </div>
            </div>
        );
    }

    renderInMemoryQueue() {
        const s = this.state;
        return (
            <div>
                <h3>In Memory Queue</h3>
                {s.Queue.length == 0 ? <p> -- There is no active ScheduledTask -- </p> :
                    <table className="sf-search-results sf-stats-table">
                        <thead>
                            <tr>
                                <th>ScheduledTask</th>
                                <th>Rule</th>
                                <th>NextDate</th>
                            </tr>
                        </thead>
                        <tbody>
                            {s.Queue.map((item, i) =>
                                <tr key={i}>
                                    <td><EntityLink lite={item.ScheduledTask} inSearch={true} onNavigated={() => this.loadState().done()} /></td>
                                    <td>{item.Rule} </td>
                                    <td>{item.NextDate} ({moment(item.NextDate).fromNow()})</td>
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
                <h3>Running Tasks</h3>
                {s.RunningTask.length == 0 ? <p> -- There are not tasks running --</p> :
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
                            {s.RunningTask.map((item, i) =>
                                <tr key={i}>
                                    <td><EntityLink lite={item.SchedulerTaskLog} inSearch={true} onNavigated={() => this.loadState().done()} /></td>
                                    <td>{item.StartTime} ({moment(item.StartTime).fromNow()})</td>
                                    <td><pre>{item.Remarks}</pre></td>
                                    <td><button className="btn btn-light btn-xs btn-danger" type="button" onClick={e => this.handleCancelClick(e, item.SchedulerTaskLog)}>Cancel</button></td>
                                </tr>)
                            }
                        </tbody>
                    </table>
                }
            </div>
        );
    }
}



