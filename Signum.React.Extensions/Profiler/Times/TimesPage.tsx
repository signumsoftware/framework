import * as React from 'react'
import { Link } from 'react-router'
import { Tabs, Tab } from 'react-bootstrap'
import * as numbro from 'numbro'
import * as moment from 'moment'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import EntityLink from '../../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import {CountSearchControl, SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, TimeTrackerEntry} from '../ProfilerClient'

require("./Times.css");

interface TimesPageProps extends ReactRouter.RouteComponentProps<{}, {}> {

}

export default class TimesPage extends React.Component<TimesPageProps, { times?: TimeTrackerEntry[]}> {

    constructor(props){
        super(props);
        this.state =  { times : null};
    }

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


  

        return (
            <div>
                <h3>Times</h3>
                <div className="btn-toolbar">
                <button onClick={() => this.loadState()} className="btn btn-default">Reload</button>
                <button onClick={this.handleClear} className="btn btn-warning">Clear</button>
                    </div>
                <br/>
                <Tabs id="timesTabs">
                    <Tab eventKey="bars" title="Bars">
                        {this.renderBars()}
                    </Tab>
                    <Tab eventKey="table" title="Table">
                        {this.renderTable()}
                    </Tab>
                </Tabs>
            </div>
        );
    }


    renderBars(){

        var maxWith = 600;

        var maxValue = this.state.times.map(a => a.maxTime).max();
        var maxTotal = this.state.times.map(a => a.totalTime).max();

        var ratio = maxWith / maxValue;

        return (
             <table className="table">
                    {
                        this.state.times.orderByDescending(a => a.totalTime).map((pair, i)=>
                            <tr className="task" key={i}>
                                <td width="300">
                                    <div>
                                        <span className="processName"> { pair.key.tryBefore(' ') || pair.key }</span>
                                                { pair.key.tryAfter(' ') != null && <span className="entityName"> { pair.key.after(' ') } </span> }
                                    </div>
                                    <div>
                                         <span className="numTimes">Executed { pair.count } {pair.count == 1? "time": "times"} Total { pair.totalTime} ms </span>
                                    </div>
                                    <div className="sum" style={{ width: (100 * pair.totalTime / maxTotal) +"%" }}></div>
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
                        )}
                </table>
            );
        }

    
    renderTable(){
        
        var getColor = f => `rgb(255, ${(1-f)*255}, ${(1-f)*255})`;

        var times = this.state.times;

         var max =  {
            count : times.map(a => a.count).max(),
            lastTime : times.map(a => a.lastTime).max(),
            minTime : times.map(a => a.minTime).max(),
            averageTime :times.map(a => a.averageTime).max(),
            maxTime : times.map(a => a.maxTime).max(),
            totalTime : times.map(a => a.totalTime).max(),
        };

        return (
            <table className="table table-nonfluid">
                <thead>
                    <tr>
                        <th>
                            Name
                        </th>
                        <th>
                            Entity
                        </th>
                        <th>
                            Executions
                        </th>
                        <th>
                            Last Time
                        </th>
                        <th>
                            Min
                        </th>
                        <th>
                            Average
                        </th>
                        <th>
                            Max
                        </th>
                        <th>
                            otal
                        </th>
                    </tr>
                </thead>
                <tbody>
                    { times.map((pair, i)=>            
                    <tr style={{background: "#FFFFFF"}} key={i}>
                        <td>
                            <span className="processName"> { pair.key.tryBefore(' ') || pair.key}</span>
                        </td>
                        <td>
                            {pair.key.tryAfter(' ') &&<span className="entityName">{pair.key.tryAfter(' ')}</span>}
                        </td>
                        <td style={{textAlign:"center", background: getColor(pair.count / max.count) }}>{pair.count }
                        </td>
                        <td style={{textAlign:"right", background: getColor(pair.lastTime / max.lastTime) }}>{pair.lastTime } ms
                        </td>
                        <td style={{textAlign:"right", background: getColor(pair.minTime / max.minTime) }}>{pair.minTime } ms
                        </td>
                        <td style={{textAlign:"right", background: getColor(pair.averageTime / max.averageTime) }}>{pair.averageTime } ms
                        </td>
                        <td style={{textAlign:"right", background: getColor(pair.maxTime / max.maxTime) }}>{pair.maxTime } ms
                        </td>
                        <td style={{textAlign:"right", background: getColor(pair.totalTime / max.totalTime) }}>{pair.totalTime } ms
                        </td>
                    </tr>
                    )}
                </tbody>
            </table>
        );
    }
}



