import * as React from 'react'
import { Link } from 'react-router'
import * as numbro from 'numbro'
import * as moment from 'moment'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { CountSearchControl, SearchControl } from '../../../Framework/Signum.React/Scripts/Search'
import EntityLink from '../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { QueryDescription, SubTokensOptions } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, SchedulerState } from './SchedulerClient'
import { ScheduledTaskLogEntity, ScheduledTaskEntity} from './Signum.Entities.Scheduler'

interface SchedulerPanelProps extends ReactRouter.RouteComponentProps<{}, {}> {

}

export default class SchedulerPanelPage extends React.Component<SchedulerPanelProps, SchedulerState> {

    componentWillMount() {
        this.loadState().done();
    }

    loadState() {
        return API.view()
            .then(s => this.setState(s));
    }

    handleStop = (e: React.MouseEvent) => {
        API.stop().then(() => this.loadState()).done();
    }

    handleStart = (e: React.MouseEvent) => {
        API.start().then(() => this.loadState()).done();
    }


    render() {
        document.title = "SchedulerLogic state";

        if (this.state == undefined)
            return <h2>SchedulerLogic state (loading...) </h2>;

        const s = this.state;

        return (
            <div>
                <h2>SchedulerLogic state</h2>
                <div className="btn-toolbar">
                    {s.Running && <a href="#" className="sf-button btn btn-default active" style={{ color: "red" }} onClick={this.handleStop}>Stop</a> }
                    {!s.Running && <a href="#" className="sf-button btn btn-default" style={{ color: "green" }} onClick={this.handleStart}>Start</a> }
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
                    NextExecution: { s.NextExecution} ({ s.NextExecution == undefined ? "-None-" : moment(s.NextExecution).toNow() })
                    <br />
                    { this.renderTable() }
                    <br />
                    <br />
                    <h2>{ScheduledTaskEntity.niceName() }</h2>
                    <SearchControl findOptions={{
                        queryName: ScheduledTaskEntity,
                        searchOnLoad: true,
                        showFilters: false,
                        pagination: { elementsPerPage: 10, mode: "Firsts" }
                    }}/>


                    <br />
                    <h2>{ScheduledTaskLogEntity.niceName() }</h2>
                    <SearchControl findOptions={{
                        queryName: ScheduledTaskLogEntity,
                        orderOptions: [{ columnName: "StartTime", orderType: "Descending" }],
                        searchOnLoad: true,
                        showFilters: false,
                        pagination: { elementsPerPage: 10, mode: "Firsts" }
                    }}/>
                </div>
            </div>
        );
    }

    renderTable() {
        const s = this.state;
        return (
            <div>
                <h3>In Memory Queue</h3>
                <table className="sf-search-results sf-stats-table">
                    <thead>
                        <tr>
                            <th>ScheduledTask
                            </th>
                            <th>Rule
                            </th>
                            <th>NextExecution
                            </th>
                        </tr>
                    </thead>
                    <tbody>
                        { s.Queue.map((item, i) =>
                            <tr key={i}>
                                <td><EntityLink lite={item.ScheduledTask} inSearch={true} /></td>
                                <td>{ item.Rule } </td>
                                <td>{ item.NextExecution} ({ item.NextExecution == undefined ? "-None-" : moment(item.NextExecution).toNow() }) </td>
                            </tr>)
                        }
                    </tbody>
                </table>
            </div>
        );
    }
}



