import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as d3 from 'd3'
import * as Navigator from '@framework/Navigator'
import { API, HeavyProfilerEntry } from '../ProfilerClient'
import "./Profiler.css"

interface HeavyListProps extends RouteComponentProps<{}> {

}

export default function HeavyList(p : HeavyListProps, { enabled?: boolean; entries?: HeavyProfilerEntry[], fileToUpload?: File, fileVer: number }){
  function constructor(props: HeavyListProps) {
    super(props);
    state = { fileVer: 0 };
  }

  function componentWillMount() {
    loadIsEnabled().done()
    loadEntries().done();
    Navigator.setTitle("Heavy Profiler");
  }

  function componentWillUnmount() {
    Navigator.setTitle();
  }

 function loadEntries() {
    const entries = await API.Heavy.entries();
    return setState({ entries });
  }

  function handleClear(e: React.MouseEvent<any>) {
    API.Heavy.clear()
      .then(() => loadEntries())
      .done();
  }

  function handleUpdate(e: React.MouseEvent<any>) {
    loadEntries().done();
    loadIsEnabled().done();
  }

 function loadIsEnabled() {
    const enabled = await API.Heavy.isEnabled();
    return setState({ enabled });
  }

  function handleSetEnabled(value: boolean) {
    API.Heavy.setEnabled(value)
      .then(() => loadIsEnabled())
      .then(() => loadEntries())
      .done();
  }


  function handleDownload() {
    API.Heavy.download(undefined);
  }

  function handleInputChange(e: React.FormEvent<any>) {
    let f = (e.currentTarget as HTMLInputElement).files![0];
    setState({ fileToUpload: f });
  }

  function handleUpload() {
    let fileReader = new FileReader();
    fileReader.onerror = e => { setTimeout(() => { throw (e as any).error; }, 0); };
    fileReader.onload = e => {
      let content = ((e.target as any).result as string).after("base64,");
      let fileName = fileToUpload!.name;

      API.Heavy.upload({ fileName, content })
        .then(() => setState({ fileToUpload: undefined, fileVer: fileVer + 1 }))
        .then(() => loadEntries())
        .done();
    };
    fileReader.readAsDataURL(fileToUpload!);
  }


  function componentDidUpdate() {
    mountChart();
  }

  chartContainer?: HTMLDivElement | null;

  function mountChart() {
    if (chartContainer == undefined)
      return;

    let data = entries!;

    let fontSize = 12;
    let fontPadding = 4;
    let characterWidth = 7;
    let labelWidth = 60 * characterWidth; //Max characters: 100
    let rightMargin = 10 * characterWidth; //Aproximate elapsed time length: 10

    let width = chartContainer.getBoundingClientRect().width;
    let height = (fontSize + (2 * fontPadding)) * (data.length);
    chartContainer.style.height = height + "px";

    let minStart = data.map(a => a.beforeStart).min()!;
    let maxEnd = data.map(a => a.end).max()!;

    let x = d3.scaleLinear()
      .domain([minStart, maxEnd])
      .range([labelWidth + 3, width - rightMargin]);

    let y = d3.scaleLinear()
      .domain([0, data.length])
      .range([0, height - 1]);

    let entryHeight = y(1);

    d3.select(chartContainer).selectAll("svg").remove();

    let chart = d3.select(chartContainer)
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
  if (entries == undefined)
    return <h3 className="display-6">Heavy Profiler (loading...) </h3>;

  return (
    <div>
      <h2 className="display-6">Heavy Profiler</h2>
      <br />
      <div className="btn-toolbar" style={{ float: "right" }}>
        <input key={fileVer} type="file" className="form-control" onChange={handleInputChange} style={{ display: "inline", float: "left", width: "inherit" }} />
        <button onClick={handleUpload} className="btn btn-info" disabled={!fileToUpload}><FontAwesomeIcon icon="cloud-upload" /> Upload</button>
      </div>
      <div className="btn-toolbar">
        {!enabled ? <button onClick={() => handleSetEnabled(true)} className="btn btn-light primary">Enable</button> :
          <button onClick={() => handleSetEnabled(false)} className="btn btn-light" style={{ color: "red" }}>Disable</button>
        }
        <button onClick={handleUpdate} className="btn btn-light">Update</button>
        <button onClick={handleClear} className="btn btn-light">Clear</button>
        <button onClick={handleDownload} className="btn btn-info"><FontAwesomeIcon icon="cloud-download-alt" /> Download</button>
      </div>
      <br />
      <p className="help-block">Upload previous runs to compare performance.</p>
      <p className="help-block">Enable the profiler with the debugger with <code>HeavyProfiler.Enabled = true</code> and save the results with <code>HeavyProfiler.ExportXml().Save("profile.xml") </code>.</p>

      <br />
      <h3>Entries</h3>
      <div className="sf-profiler-chart" ref={d => chartContainer = d}>
      </div>
    </div>
  );
}



