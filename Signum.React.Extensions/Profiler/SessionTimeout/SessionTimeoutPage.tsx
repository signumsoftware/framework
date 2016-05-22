import * as React from 'react'
import { Link } from 'react-router'
import * as numbro from 'numbro'
import * as moment from 'moment'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import EntityLink from '../../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import {CountSearchControl, SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, TimeTrackerEntry} from '../ProfilerClient'



interface TimesPageProps extends ReactRouter.RouteComponentProps<{}, {}> {

}

export default class TimesPage extends React.Component<TimesPageProps, { times: TimeTrackerEntry[]}> {

    componentWillMount() {
        this.loadState().done();
    }

    loadState() {
        return API.Times.fetchInfo()
            .then(s => this.setState({times: s}));
    }
    
    handleClear = (e: React.MouseEvent) => {
        API.Times.clear().then(() => this.loadState()).done();
    }


    render() {
        document.title = "Times state";

        if (this.state.times == null)
            return <h3>Times (loading...)</h3>;

        var s = this.state;

        var maxWith = 600;

        var maxValue = this.state.times.map(a => a.maxTime).max();

        var ratio = maxWith / maxValue;

        return (
            <div>
                <h3>Times</h3>
                <button onClick={() => this.loadState()} className="btn btn-default">Reload</button>
                <button onClick={this.handleClear} className="btn btn-warning">Clear</button>
                <ul id="tasks">
                    {
                        this.state.times.orderBy(a => a.averageTime).map((pair, i)=>
                        <li className="task">
                            <table>
                                <tr>
                                    <td>
                                        <table>
                                            <tr>
                                                <td width="300">
                                                    <span className="processName"> { pair.key.tryBefore(' ') || pair.key }</span>
                                                    { pair.key.tryAfter(' ') != null &&
                                                        <div>
                                                        <br />
                                                        <span className="entityName"> { pair.key.after(' ') } </span>
                                                        </div>
                                                    }
                                                </td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <span className="numTimes">Executed { pair.count } {pair.count == 1? "time": "times"} </span>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                    <td>
                                        <table>
                                                <tr>
                                                    <td width="40">Max
                                                    </td>
                                                    <td className="leftBorder">
                                                        <span className="max" style={{ width: (pair.maxTime * ratio) +"px" }}></span> {pair.maxTime} ms ({ moment(pair.maxDate).fromNow()})
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td width="40">Average
                                                    </td>
                                                    <td className="leftBorder">
                                                        <span className="med" style={{ width: (pair.averageTime * ratio) +"px" }}></span> {pair.averageTime} ms
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td width="40">Min
                                                    </td>
                                                    <td className="leftBorder">
                                                        <span className="min" style={{ width: (pair.minTime * ratio) +"px" }}></span> {pair.minTime} ms ({ moment(pair.minDate).fromNow()})
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td width="40">Last
                                                    </td>
                                                    <td className="leftBorder">
                                                        <span className="last" style={{ width: (pair.lastTime * ratio) +"px" }}></span> {pair.lastTime} ms ({ moment(pair.lastDate).fromNow()})
                                                    </td>
                                                </tr> 
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </li>
                            )
                    }
                </ul>
            </div>
        );
    }
}



