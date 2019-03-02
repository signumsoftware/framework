import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as d3 from 'd3'
import * as Navigator from '@framework/Navigator'
import { API, HeavyProfilerEntry } from '../ProfilerClient'
import "./Profiler.css"

interface HeavyListProps extends RouteComponentProps<{}> {

}

export default class HeavyList extends React.Component<HeavyListProps, { enabled?: boolean; entries?: HeavyProfilerEntry[], fileToUpload?: File, fileVer: number }> {

  constructor(props: HeavyListProps) {
    super(props);
    this.state = { fileVer: 0 };
  }

  componentWillMount() {
    this.loadIsEnabled().done()
    this.loadEntries().done();
    Navigator.setTitle("Heavy Profiler");
  }

  componentWillUnmount() {
    Navigator.setTitle();
  }

  async loadEntries() {
    const entries = await API.Heavy.entries();
    return this.setState({ entries });
  }

  handleClear = (e: React.MouseEvent<any>) => {
    API.Heavy.clear()
      .then(() => this.loadEntries())
      .done();
  }

  handleUpdate = (e: React.MouseEvent<any>) => {
    this.loadEntries().done();
    this.loadIsEnabled().done();
  }

  async loadIsEnabled() {
    const enabled = await API.Heavy.isEnabled();
    return this.setState({ enabled });
  }

  handleSetEnabled(value: boolean) {
    API.Heavy.setEnabled(value)
      .then(() => this.loadIsEnabled())
      .then(() => this.loadEntries())
      .done();
  }


  handleDownload = () => {
    API.Heavy.download(undefined);
  }

  handleInputChange = (e: React.FormEvent<any>) => {
    let f = (e.currentTarget as HTMLInputElement).files![0];
    this.setState({ fileToUpload: f });
  }

  handleUpload = () => {
    let fileReader = new FileReader();
    fileReader.onerror = e => { setTimeout(() => { throw (e as any).error; }, 0); };
    fileReader.onload = e => {
      let content = ((e.target as any).result as string).after("base64,");
      let fileName = this.state.fileToUpload!.name;

      API.Heavy.upload({ fileName, content })
        .then(() => this.setState({ fileToUpload: undefined, fileVer: this.state.fileVer + 1 }))
        .then(() => this.loadEntries())
        .done();
    };
    fileReader.readAsDataURL(this.state.fileToUpload!);
  }

  render() {
    if (this.state.entries == undefined)
      return <h3 className="display-6">Heavy Profiler (loading...) </h3>;

    return (
      <div>
        <h2 className="display-6">Heavy Profiler</h2>
        <br />
        <div className="btn-toolbar" style={{ float: "right" }}>
          <input key={this.state.fileVer} type="file" className="form-control" onChange={this.handleInputChange} style={{ display: "inline", float: "left", width: "inherit" }} />
          <button onClick={this.handleUpload} className="btn btn-info" disabled={!this.state.fileToUpload}><FontAwesomeIcon icon="cloud-upload" /> Upload</button>
        </div>
        <div className="btn-toolbar">
          {!this.state.enabled ? <button onClick={() => this.handleSetEnabled(true)} className="btn btn-light primary">Enable</button> :
            <button onClick={() => this.handleSetEnabled(false)} className="btn btn-light" style={{ color: "red" }}>Disable</button>
          }
          <button onClick={this.handleUpdate} className="btn btn-light">Update</button>
          <button onClick={this.handleClear} className="btn btn-light">Clear</button>
          <button onClick={this.handleDownload} className="btn btn-info"><FontAwesomeIcon icon="cloud-download-alt" /> Download</button>
        </div>
        <br />
        <p className="help-block">Upload previous runs to compare performance.</p>
        <p className="help-block">Enable the profiler with the debugger with <code>HeavyProfiler.Enabled = true</code> and save the results with <code>HeavyProfiler.ExportXml().Save("profile.xml") </code>.</p>

        <br />
        <h3>Entries</h3>
        <div className="sf-profiler-chart" ref={d => this.chartContainer = d}>
        </div>
      </div>
    );
  }

  componentDidUpdate() {
    this.mountChart();
  }

  chartContainer?: HTMLDivElement | null;

  mountChart() {

    if (this.chartContainer == undefined)
      return;

    let data = this.state.entries!;

    let fontSize = 12;
    let fontPadding = 4;
    let characterWidth = 7;
    let labelWidth = 60 * characterWidth; //Max characters: 100
    let rightMargin = 10 * characterWidth; //Aproximate elapsed time length: 10

    let width = this.chartContainer.getBoundingClientRect().width;
    let height = (fontSize + (2 * fontPadding)) * (data.length);
    this.chartContainer.style.height = height + "px";

    let minStart = data.map(a => a.beforeStart).min()!;
    let maxEnd = data.map(a => a.end).max()!;

    let x = d3.scaleLinear()
      .domain([minStart, maxEnd])
      .range([labelWidth + 3, width - rightMargin]);

    let y = d3.scaleLinear()
      .domain([0, data.length])
      .range([0, height - 1]);

    let entryHeight = y(1);

    d3.select(this.chartContainer).selectAll("svg").remove();

    let chart = d3.select(this.chartContainer)
      .append('svg:svg').attr('width', width).attr('height', height);

    let groups = chart.selectAll("g.entry").data(data).enter()
      .append('svg:g').attr('class', 'entry')
      .attr('data-full-index', function (v) { return v.fullIndex; });

    groups.append('svg:rect').attr('class', 'left-background')
      .attr('x', 0)
      .attr('y', function (v, i) { return y(i); })
      .attr('width', labelWidth)
      .attr('height', entryHeight)
      .attr('fill', '#ddd')
      .attr('stroke', '#fff');

    let labelsLeft = groups.append('svg:text').attr('class', 'label label-left')
      .attr('dy', function (v, i) { return y(i); })
      .attr('y', fontPadding + fontSize)
      .attr('fill', '#000')
      .text(function (v) { return v.role + " " + v.additionalData; });

    groups.append('svg:rect').attr('class', 'right-background')
      .attr('x', labelWidth)
      .attr('y', function (v, i) { return y(i); })
      .attr('width', width - labelWidth)
      .attr('height', entryHeight)
      .attr('fill', '#fff')
      .attr('stroke', '#ddd');

    let rectangles = groups.append('svg:rect').attr('class', 'shape')
      .attr('x', function (v) { return x(v.start); })
      .attr('y', function (v, i) { return y(i); })
      .attr('width', function (v) { return x(v.end) - x(v.start); })
      .attr('height', entryHeight)
      .attr('fill', function (v) { return v.color; });

    let labelsRight = groups.append('svg:text').attr('class', 'label label-right')
      .attr('dx', function (v) { return x(v.end) + 3; })
      .attr('dy', function (v, i) { return y(i); })
      .attr('y', fontPadding + fontSize)
      .attr('fill', '#000')
      .text(function (v) { return v.elapsed; });

    groups.append('svg:title').text(function (v) { return v.elapsed + " - " + v.additionalData; });

    groups.on("click", e => {
      let url = "~/profiler/heavy/entry/" + e.fullIndex;

      if (d3.event.ctrlKey) {
        window.open(Navigator.toAbsoluteUrl(url));
      } else {
        Navigator.history.push(url);
      }
    });
  }
}



