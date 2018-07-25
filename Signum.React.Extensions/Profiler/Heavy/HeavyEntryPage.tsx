import * as React from 'react'
import { Link } from 'react-router-dom'
import * as d3 from 'd3'
import * as numbro from 'numbro'
import * as moment from 'moment'
import { } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import EntityLink from '@framework/SearchControl/EntityLink'
import { ValueSearchControl, SearchControl } from '@framework/Search'
import { QueryDescription, SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '@framework/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '@framework/Signum.Entities'
import { API, HeavyProfilerEntry, StackTraceTS } from '../ProfilerClient'
import { RouteComponentProps } from "react-router";

import "./Profiler.css"

interface HeavyEntryProps extends RouteComponentProps<{ selectedIndex: string }> {

}

export default class HeavyEntry extends React.Component<HeavyEntryProps, { entries?: HeavyProfilerEntry[], stackTrace?: StackTraceTS[], asyncDepth: boolean }> {

    constructor(props: HeavyEntryProps) {
        super(props);
        this.state = { asyncDepth: true };
    }

    componentWillMount() {

        this.loadEntries(this.props);

        this.loadStackTrace(this.props);
    }

    componentWillReceiveProps(newProps: HeavyEntryProps) {
        if (this.state.entries == undefined || !this.state.entries.some(a => a.FullIndex == newProps.match.params.selectedIndex))
            this.loadEntries(newProps);

        this.loadStackTrace(newProps);
    }

    loadEntries(props: HeavyEntryProps) {

        let selectedIndex = props.match.params.selectedIndex;

        return API.Heavy.details(selectedIndex.tryBefore(".") || selectedIndex)
            .then(entries => this.setState({ entries }))
            .done();
    }


    loadStackTrace(props: HeavyEntryProps) {
        return API.Heavy.stackTrace(props.match.params.selectedIndex)
            .then(stackTrace => this.setState({ stackTrace }))
            .done();
    }

    handleDownload = () => {

        let selectedIndex = this.props.match.params.selectedIndex;

        API.Heavy.download(selectedIndex.tryBefore(".") || selectedIndex);
    }

    handleUpdate = () => {
        this.loadEntries(this.props);
    }


    render() {

        const index = this.props.match.params.selectedIndex;
        Navigator.setTitle("Heavy Profiler > Entry " + index);
        if (this.state.entries == undefined)
            return <h3 className="display-6">Heavy Profiler > Entry {index} (loading...) </h3>;

        let current = this.state.entries.filter(a => a.FullIndex == this.props.match.params.selectedIndex).single();
        return (
            <div>
                <h2 className="display-6"><Link to="~/profiler/heavy">Heavy Profiler</Link> > Entry {index}</h2>
                <label><input type="checkbox" checked={this.state.asyncDepth} onChange={a => this.setState({ asyncDepth: a.currentTarget.checked })} />Async Stack</label>
                <br />
                {this.state.entries && <HeavyProfilerDetailsD3 entries={this.state.entries} selected={current} asyncDepth={this.state.asyncDepth} />}
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
                            <td colSpan={2}>
                                <div className="btn-toolbar">
                                    <button onClick={this.handleDownload} className="btn btn-info">Download</button>
                                    {!current.IsFinished && <button onClick={this.handleUpdate} className="btn btn-light">Update</button>}
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
                <br />
                <h3>Aditional Data</h3>
                <pre style={{ maxWidth: "1000px", overflowY: "scroll" }}><code>{current.AdditionalData}</code></pre>
                <br />
                <h3>StackTrace</h3>
                {
                    this.state.stackTrace == undefined ? <span>No Stacktrace</span> :
                        <StackFrameTable stackTrace={this.state.stackTrace} />
                }
            </div>
        );
    }
}


export class StackFrameTable extends React.Component<{ stackTrace: StackTraceTS[] }>{

    render() {
        if (this.props.stackTrace == undefined)
            return <span>No StackTrace</span>;

        return (
            <table className="table table-sm">
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
                    {this.props.stackTrace.map((sf, i) =>
                        <tr key={i}>
                            <td>
                                {sf.Namespace && <span style={{ color: sf.Color }}>{sf.Namespace}</span>}
                            </td>
                            <td>
                                {sf.Type && <span style={{ color: sf.Color }}>{sf.Type}</span>}
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
    return min * (1 - ratio) + max * ratio;
}


interface HeavyProfilerDetailsD3Props {
    entries: HeavyProfilerEntry[];
    selected: HeavyProfilerEntry;
    asyncDepth: boolean;
}


interface HeavyProfilerDetailsD3State {
    min: number;
    max: number;
}



export class HeavyProfilerDetailsD3 extends React.Component<HeavyProfilerDetailsD3Props, HeavyProfilerDetailsD3State>{

    componentWillMount() {
        this.resetZoom(this.props.selected);
    }

    resetZoom(current: HeavyProfilerEntry) {
        this.setState({
            min: lerp(current.BeforeStart, -0.1, current.End),
            max: lerp(current.BeforeStart, 1.1, current.End)
        });
    }

    componentDidMount() {
        this.mountChart(this.props);
    }

    componentWillReceiveProps(newProps: HeavyProfilerDetailsD3Props) {
        if (newProps.asyncDepth != this.props.asyncDepth) {
            this.mountChart(newProps);
        }
        else if (newProps.selected != this.props.selected) {
            this.mountChart(newProps);
        }
    }

    componentDidUpdate() {
        this.updateChart!();
    }

    chartContainer!: HTMLDivElement;

    handleWeel = (e: React.WheelEvent<any>) => {

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
        return (<div className="sf-profiler-chart" ref={div => this.chartContainer = div!} onWheel={this.handleWeel}></div>);
    }

    mountChart(props: HeavyProfilerDetailsD3Props) {

        if (this.chartContainer == undefined)
            throw new Error("chartContainer not mounted!");

        let data = props.entries;

        if (data == undefined)
            throw new Error("no entries");

        var getDepth = props.asyncDepth ?
            (e: HeavyProfilerEntry) => e.AsyncDepth :
            (e: HeavyProfilerEntry) => e.Depth;

        let fontSize = 12;
        let fontPadding = 3;
        let maxDepth = d3.max(data, getDepth)!;

        let height = ((fontSize * 2) + (3 * fontPadding)) * (maxDepth + 1);
        this.chartContainer.style.height = height + "px";

        let y = d3.scaleLinear()
            .domain([0, maxDepth + 1])
            .range([0, height]);

        let entryHeight = y(1);

        d3.select(this.chartContainer).select("svg").remove();

        const chart = d3.select(this.chartContainer)
            .append<SVGElement>('svg:svg').attr('height', height);


        this.updateChart = () => {

            let { min, max } = this.state;
            let width = this.chartContainer.getBoundingClientRect().width;
            let sel = this.props.selected;
            let x = d3.scaleLinear()
                .domain([min, max])
                .range([0, width]);

            var filteredData = data.filter(a => a.End > min && a.BeforeStart < max && (x(a.End) - x(a.BeforeStart)) > 1);

            const selection = chart.selectAll<SVGGElement, any>("g.entry").data(filteredData, a => (a as HeavyProfilerEntry).FullIndex);

            selection.exit().remove();

            var newGroups = selection.enter()
                .append<SVGGElement>('svg:g')
                .attr('class', 'entry')
                .attr('data-key', a => a.FullIndex);

            newGroups.append<SVGRectElement>('svg:rect').attr('class', 'shape')
                .attr('y', v => y(getDepth(v)))
                .attr('height', entryHeight - 1)
                .attr('fill', v => v.Color);

            newGroups.append<SVGRectElement>('svg:rect').attr('class', 'shape-before')
                .attr('y', v => y(getDepth(v)) + 1)
                .attr('height', entryHeight - 2)
                .attr('fill', '#fff');

            newGroups.append<SVGTextElement>('svg:text').attr('class', 'label label-top')
                .attr('dy', v => y(getDepth(v)))
                .attr('y', fontPadding + fontSize)
                .text(v => v.Elapsed);

            newGroups.append<SVGTextElement>('svg:text').attr('class', 'label label-bottom')
                .attr('dy', v => y(getDepth(v)))
                .attr('y', (2 * fontPadding) + (2 * fontSize))
                .text(v => v.Role + (v.AdditionalData ? (" - " + v.AdditionalData.etc(30)) : ""));

            newGroups.append('svg:title').text(v => v.Role + v.Elapsed);

            newGroups.on("click", e => {

                if (e == this.props.selected) {

                }
                else {
                    let url = "~/profiler/heavy/entry/" + e.FullIndex;

                    if (d3.event.ctrlKey) {
                        window.open(Navigator.toAbsoluteUrl(url));
                    }
                    else {
                        Navigator.history.push(url);
                    }
                }
            });
            
            newGroups.on("dblclick", e => {
                this.resetZoom(e);
            });

            chart.attr('width', width);

            var updateGroups = newGroups.merge(selection);

            updateGroups.select<SVGRectElement>("rect.shape")
                .attr('x', v => x(Math.max(min, v.BeforeStart)))
                .attr('width', v => Math.max(0, x(Math.min(max, v.End)) - x(Math.max(min, v.BeforeStart))))
                .attr('stroke', v => v == sel ? '#000' : '#ccc');

            updateGroups.select<SVGRectElement>("rect.shape-before")
                .attr('x', v => x(Math.max(min, v.BeforeStart)))
                .attr('width', v => Math.max(0, x(Math.min(max, v.Start)) - x(Math.max(min, v.BeforeStart))));

            updateGroups.select<SVGTextElement>("text.label.label-top")
                .attr('dx', v => x(Math.max(min, v.Start)) + 3)
                .attr('fill', v => v == sel ? '#000' : '#fff');

            updateGroups.select<SVGTextElement>("text.label.label-bottom")
                .attr('dx', v => x(Math.max(min, v.Start)) + 3)
                .attr('fill', v => v == sel ? '#000' : '#fff');
        };

        this.updateChart();
    }

    updateChart?: () => void;
}