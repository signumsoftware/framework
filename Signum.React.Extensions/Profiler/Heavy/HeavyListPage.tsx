import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as d3 from 'd3'
import * as Navigator from '@framework/Navigator'
import { API, HeavyProfilerEntry } from '../ProfilerClient'
import "./Profiler.css"
import { useTitle, useAPI, useAPIWithReload, useSize } from '../../../../Framework/Signum.React/Scripts/Hooks'

interface HeavyListProps extends RouteComponentProps<{}> {

}

export default function HeavyList(p: HeavyListProps) {

  const [enabled, reloadEnabled] = useAPIWithReload(() => API.Heavy.isEnabled(), [], { avoidReset: true });
  const [entries, reloadEntries] = useAPIWithReload(() => API.Heavy.entries(), [], { avoidReset: true });

  const [fileToUpload, setFileToUpload] = React.useState<File | undefined>(undefined);
  const [fileVer, setFileVer] = React.useState<number>(0)

  useTitle("Heavy Profiler");

  function handleClear(e: React.MouseEvent<any>) {
    API.Heavy.clear()
      .then(() => reloadEntries())
      .done();
  }

  function handleUpdate(e: React.MouseEvent<any>) {
    reloadEntries();
    reloadEnabled();
  }

  function handleSetEnabled(value: boolean) {
    API.Heavy.setEnabled(value)
      .then(() => { reloadEntries(); reloadEnabled(); })
      .done();
  }

  function handleDownload() {
    API.Heavy.download(undefined);
  }

  function handleInputChange(e: React.FormEvent<any>) {
    let f = (e.currentTarget as HTMLInputElement).files![0];
    setFileToUpload(f);
  }

  function handleUpload() {
    let fileReader = new FileReader();
    fileReader.onerror = e => { setTimeout(() => { throw (e as any).error; }, 0); };
    fileReader.onload = e => {
      let content = ((e.target as any).result as string).after("base64,");
      let fileName = fileToUpload!.name;

      API.Heavy.upload({ fileName, content })
        .then(() => {
          setFileToUpload(undefined);
          setFileVer(fileVer + 1);
          reloadEntries();
        })
        .done();
    };
    fileReader.readAsDataURL(fileToUpload!);
  }

  const { size, setContainer } = useSize();
  
  if (entries == undefined)
    return <h3 className="display-6">Heavy Profiler (loading...) </h3>;

  return (
    <div>
      <h2 className="display-6">Heavy Profiler</h2>
      <br />
      <div className="btn-toolbar" style={{ float: "right" }}>
        <input key={fileVer} type="file" className="form-control" onChange={handleInputChange} style={{ display: "inline", float: "left", width: "inherit" }} />
        <button onClick={handleUpload} className="btn btn-info" disabled={!fileToUpload}><FontAwesomeIcon icon="cloud-upload-alt" /> Upload</button>
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
      <div className="sf-profiler-chart" ref={setContainer}>
        {size && <EntrieListPath entries={entries} width={size.width} />}
      </div>
    </div>
  );
}

function EntrieListPath({ width, entries }: { width: number, entries: HeavyProfilerEntry[] }) {

  let data = entries!;

  let fontSize = 12;
  let fontPadding = 4;
  let characterWidth = 7;
  let labelWidth = 60 * characterWidth; //Max characters: 100
  let rightMargin = 10 * characterWidth; //Aproximate elapsed time length: 10

  let height = (fontSize + (2 * fontPadding)) * (data.length);

  let minStart = data.map(a => a.beforeStart).min()!;
  let maxEnd = data.map(a => a.end).max()!;

  let x = d3.scaleLinear()
    .domain([minStart, maxEnd])
    .range([labelWidth + 3, width - rightMargin]);

  let y = d3.scaleLinear()
    .domain([0, data.length])
    .range([0, height - 1]);

  let entryHeight = y(1);

  function handleOnClick(e: React.MouseEvent, v: HeavyProfilerEntry) {
    let url = "~/profiler/heavy/entry/" + v.fullIndex;

    if (e.ctrlKey) {
      window.open(Navigator.toAbsoluteUrl(url));
    } else {
      Navigator.history.push(url);
    }
  }

  return (
    <svg width={width + "px"} height={height + "px"}>
      {data.map((v, i) => <g className="entry" data-full-key={v.fullIndex} onClick={e => handleOnClick(e, v)}>
        <rect className="left-background" x={0} y={y(i)} width={labelWidth} height={entryHeight} fill="#ddd" stroke="#fff" />
        <text className="label label-left" y={y(i)} dy={fontPadding + fontSize} fill="#000">{v.role + " " + v.additionalData}</text>
        <rect className="right-background" x={labelWidth} y={y(i)} width={width - labelWidth} height={entryHeight} fill="#fff" stroke="#ddd" />
        <rect className="shape" x={x(v.start)} y={y(i)} width={x(v.end) - x(v.start)} height={entryHeight} fill={v.color} />
        <text className="label label-right" x={x(v.end) + 3} y={y(i)} width={x(v.end) - x(v.start)} dy={fontPadding + fontSize} height={entryHeight} fill='#000'>{v.elapsed}</text>
        <title>{v.elapsed + " - " + v.additionalData}</title>
      </g>)}
    </svg>
  );
}



