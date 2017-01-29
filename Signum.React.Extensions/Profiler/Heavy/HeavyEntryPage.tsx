import * as React from 'react'
import { Link } from 'react-router'
import * as d3 from 'd3'
import * as numbro from 'numbro'
import * as moment from 'moment'
import { } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import EntityLink from '../../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import {ValueSearchControl, SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, HeavyProfilerEntry, StackTraceTS} from '../ProfilerClient'

require("./Profiler.css");

interface HeavyEntryProps extends ReactRouter.RouteComponentProps<{}, {selectedIndex : string }> {

}

export default class HeavyEntry extends React.Component<HeavyEntryProps, { entries?: HeavyProfilerEntry[], stackTrace?: StackTraceTS[]}> {

    constructor(props: HeavyEntryProps) {
        super(props);
        this.state = { };
    }

    componentWillMount() {
      
        this.loadEntries(this.props);

        this.loadStackTrace(this.props);
    }

    componentWillReceiveProps(newProps: HeavyEntryProps){
        if(this.state.entries == undefined || !this.state.entries.some(a=>a.FullIndex == newProps.routeParams.selectedIndex))
            this.loadEntries(newProps);

        this.loadStackTrace(newProps);
    }

    loadEntries(props: HeavyEntryProps) {

        let selectedIndex = props.routeParams.selectedIndex;

        return API.Heavy.details(selectedIndex.tryBefore(".") || selectedIndex)
            .then(entries => this.setState({entries}))
            .done();
    }


    loadStackTrace(props: HeavyEntryProps){
        return API.Heavy.stackTrace(props.routeParams.selectedIndex)
            .then(stackTrace => this.setState({stackTrace}))
            .done();
    }
    
    handleDownload = () => {

        let selectedIndex = this.props.routeParams.selectedIndex;

        API.Heavy.download(selectedIndex.tryBefore(".") || selectedIndex);
    }


    render() {

        const index = this.props.routeParams.selectedIndex;

        document.title = "Heavy Profiler > Entry " + index;
        if (this.state.entries == undefined)
            return <h3>Heavy Profiler > Entry {index} (loading...) </h3>;

        let current = this.state.entries.filter(a => a.FullIndex == this.props.routeParams.selectedIndex).single();
        return (
            <div>
                <h2><Link to="~/profiler/heavy">Heavy Profiler</Link> > Entry {index}</h2>
                <br />
                {this.state.entries && < HeavyProfilerDetailsD3 entries={this.state.entries} selected={current}/>}
                <br />
                <table className="table table-nonfluid">
                    <tbody>
                        <tr>
                            <th>Role</th>
                            <td>{current.Role}</td>
                        </tr>
                        <tr>
                            <th>Time</th>
                            <td>{current.Elapsed}</td>
                        </tr>
                        <tr>
                            <td colSpan={2}><button onClick={this.handleDownload} className="btn btn-info">Download</button></td>
                        </tr>
                    </tbody>
                </table>
                <br />
                <h3>Aditional Data</h3>
                <div>
                    <pre><code>{current.AdditionalData}</code></pre>
                </div>
                <br />
                <h3>StackTrace</h3>
                {
                    this.state.stackTrace == undefined ? <span>No Stacktrace</span> : 
                        <StackFrameTable stackTrace={this.state.stackTrace}/>
                }
            </div>
        );
    }



    chartContainer: HTMLDivElement;

   
}


export class StackFrameTable extends React.Component<{stackTrace : StackTraceTS[]}, void>{

    render(){
        if(this.props.stackTrace == undefined)
            return <span>No StackTrace</span>;

        return (
            <table className="table table-condensed">
                <thead>
                    <tr>
                         <th>Namespace
                        </th>
                        <th>Type
                        </th>
                        <th>Method
                        </th>
                        <th>FileLine
                        </th>
                    </tr>
                </thead>
                <tbody>
                    { this.props.stackTrace.map((sf, i)=> 
                        <tr key={i}>
                             <td>
                                {sf.Namespace && <span style={{color:sf.Color}}>{sf.Namespace}</span> }
                            </td>
                            <td>
                                {sf.Type && <span style={{color:sf.Color}}>{sf.Type}</span> }
                            </td>
                            <td>
                                {sf.Method}
                            </td>
                            <td>
                                {sf.FileName} {sf.LineNumber > 0 && "(" + sf.LineNumber + ")"}
                            </td>
                        </tr>
                    )}
                </tbody>
            </table>
        );
    }
}


function lerp(min: number, ratio: number, max: number) {
    return min * (1-ratio) + max * ratio;
}

export class HeavyProfilerDetailsD3 extends React.Component<{entries: HeavyProfilerEntry[], selected : HeavyProfilerEntry }, {min: number, max: number}>{
    
    
    componentWillMount(){
        this.resetZoom(this.props.selected);
    }

    resetZoom(current: HeavyProfilerEntry){
        this.setState({
            min: lerp(current.BeforeStart, -0.1, current.End),
            max: lerp(current.BeforeStart, 1.1, current.End)
        });
    }
    
    componentDidMount(){
        this.mountChart();
    }

    componentDidUpdate() {
        this.updateChart();
    }

    chartContainer:HTMLDivElement;

    handleWeel = (e: React.WheelEvent<any>)=>{

        e.preventDefault();

        let dist = this.state.max - this.state.min;

        const inc = 1.2;

        let delta = 1 - (e.deltaY > 0 ? (1 / inc) : inc);

        let elem = e.currentTarget as HTMLElement;

        let ne = e.nativeEvent as MouseEvent;

        const rect = elem.getBoundingClientRect();

        const ratio = (ne.clientX - rect.left) / rect.width;

        let newMin = this.state.min - dist * delta * (ratio);
        let newMax = this.state.max + dist * delta * (1 - ratio);

        this.setState({ 
            min: newMin,
            max: newMax 
        });
    }

    render() {
        return (<div className="sf-profiler-chart" ref={div => this.chartContainer = div} onWheel={this.handleWeel}></div>);
    }

    chart: d3.Selection<any>;
    groups: d3.Selection<HeavyProfilerEntry>;
    rects: d3.Selection<HeavyProfilerEntry>;
    rectsBefore: d3.Selection<HeavyProfilerEntry>;
    labelTop: d3.Selection<HeavyProfilerEntry>;
    labelBottom: d3.Selection<HeavyProfilerEntry>;

    updateChart() {

        let {min, max} = this.state;
        let width = this.chartContainer.getBoundingClientRect().width;
        let sel = this.props.selected;
        let x = d3.scale.linear()
            .domain([min, max])
            .range([0, width]);

        this.chart.attr('width', width);

        this.groups.style("display", a => a.End > min && a.BeforeStart < max ? "inline" : "none");

        this.rects
            .attr('x', v => x(Math.max(min, v.BeforeStart)))
            .attr('width', v => Math.max(0, x(Math.min(max, v.End)) - x(Math.max(min, v.BeforeStart))))
            .attr('stroke', v => v == sel ? '#000' : '#ccc');

        this.rectsBefore
            .attr('x', v => x(Math.max(min, v.BeforeStart)))
            .attr('width', v => Math.max(0, x(Math.min(max, v.Start)) - x(Math.max(min, v.BeforeStart))));

        this.labelTop
            .attr('dx', v => x(Math.max(min, v.Start)) + 3)
            .attr('fill', v => v == sel ? '#000' : '#fff');

        this.labelBottom
            .attr('dx', v => x(Math.max(min, v.Start)) + 3)
            .attr('fill', v => v == sel ? '#000' : '#fff');
    }

    mountChart (){

        if (this.chartContainer == undefined)
            throw new Error("chartContainer not mounted!");

        let data = this.props.entries;

        if (data == undefined)
            throw new Error("no entries");

        let fontSize = 12;
        let fontPadding = 3;
        let minDepth = d3.min(data, e=> e.Depth);
        let maxDepth = d3.max(data, e=> e.Depth);
   
        let height = ((fontSize * 2) + (3 * fontPadding)) * (maxDepth + 1);
        this.chartContainer.style.height = height + "px";



        let y = d3.scale.linear()
            .domain([0, maxDepth + 1])
            .range([0, height]);

        let entryHeight = y(1);


        d3.select(this.chartContainer).select("svg").remove();

        this.chart = d3.select(this.chartContainer)
            .append('svg:svg').attr('height', height);

        this.groups = this.chart.selectAll("g.entry").data(data).enter()
            .append('svg:g').attr('class', 'entry');

        this.rects = this.groups.append('svg:rect').attr('class', 'shape')
            .attr('y', v => y(v.Depth))            
            .attr('height', entryHeight - 1)
            .attr('fill', v => v.Color);

        this.rectsBefore = this.groups.append('svg:rect').attr('class', 'shape-before')          
            .attr('y', v => y(v.Depth) + 1)            
            .attr('height', entryHeight - 2)
            .attr('fill', '#fff');

        this.labelTop = this.groups.append('svg:text').attr('class', 'label label-top')
            .attr('dy', v => y(v.Depth))
            .attr('y', fontPadding + fontSize)
            .text(v => v.Elapsed);

        this.labelBottom = this.groups.append('svg:text').attr('class', 'label label-bottom')
            .attr('dy', v => y(v.Depth))
            .attr('y', (2 * fontPadding) + (2 * fontSize))
            .text(v => v.Role + (v.AdditionalData ? (" - " + v.AdditionalData.etc(30)) : ""));

        this.groups.append('svg:title').text(v => v.Role +  v.Elapsed);

        this.groups.on("click", e=> {

            if(e == this.props.selected)
            {
                this.resetZoom(e);
            }
            else
            {
                let url = Navigator.currentHistory.createHref("~/profiler/heavy/entry/" + e.FullIndex);

                if (d3.event.ctrlKey) {
                    window.open(url);
                }
                else {
                    Navigator.currentHistory.push(url);
                }
            }
        });

        this.updateChart();
    }
}