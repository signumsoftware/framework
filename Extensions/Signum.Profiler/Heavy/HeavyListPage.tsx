import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as d3 from 'd3'
import * as AppContext from '@framework/AppContext'
import { ProfilerClient } from '../ProfilerClient'
import "./Profiler.css"
import { useAPIWithReload, useInterval, useSize } from '@framework/Hooks'
import { useTitle } from '@framework/AppContext'
import { classes } from '@framework/Globals'

export default function HeavyList(): React.JSX.Element {

  const [ignoreProfilerHeavyEntries, setIgnoreProfilerHeavyEntries] = React.useState<boolean>(true)


  const [enabled, reloadEnabled] = useAPIWithReload(() => ProfilerClient.API.Heavy.isEnabled(), [], { avoidReset: true });
  const [entries, reloadEntries] = useAPIWithReload(() => ProfilerClient.API.Heavy.entries(ignoreProfilerHeavyEntries), [], { avoidReset: true });

  const [fileToUpload, setFileToUpload] = React.useState<File | undefined>(undefined);
  const [fileVer, setFileVer] = React.useState<number>(0)


  var tick = useInterval(enabled ? 500 : null, 0, a => a + 1);

  React.useEffect(() => {
    reloadEnabled();
    reloadEntries();
  }, [tick]);

  useTitle("Heavy Profiler");

  function handleClear(e: React.MouseEvent<any>) {
    ProfilerClient.API.Heavy.clear()
      .then(() => reloadEntries());
  }

  function handleUpdate(e: React.MouseEvent<any>) {
    reloadEntries();
    reloadEnabled();
  }

  function handleSetEnabled(value: boolean) {
    ProfilerClient.API.Heavy.setEnabled(value)
      .then(() => { reloadEntries(); reloadEnabled(); });
  }

  function handleDownload() {
    ProfilerClient.API.Heavy.download(undefined);
  }

  function handleInputChange(e: React.FormEvent<any>) {
    let f = (e.currentTarget as HTMLInputElement).files![0];
    setFileToUpload(f);
  }

  function handleUpload() {
    let fileReader = new FileReader();
    fileReader.onerror = e => { window.setTimeout(() => { throw (e as any).error; }, 0); };
    fileReader.onload = e => {
      let content = ((e.target as any).result as string).after("base64,");
      let fileName = fileToUpload!.name;

      ProfilerClient.API.Heavy.upload({ fileName, content })
        .then(() => {
          setFileToUpload(undefined);
          setFileVer(fileVer + 1);
          reloadEntries();
        });
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
        <button onClick={handleUpload} className="btn btn-info" disabled={!fileToUpload}><FontAwesomeIcon icon="cloud-arrow-up" /> Upload</button>
      </div>
      <div className="btn-toolbar">
        <button className={classes("btn btn-light", enabled ? "btn-outline-danger" : "btn-tertiary")} onClick={() => handleSetEnabled(!enabled)}><FontAwesomeIcon icon={["fas", "circle"]} /> Record</button>
        <button onClick={handleUpdate} className="btn btn-light"><FontAwesomeIcon icon="refresh" /> Update</button>
        <button onClick={handleClear} className="btn btn-light"><FontAwesomeIcon icon="trash" /> Clear</button>
        <button onClick={handleDownload} className="btn btn-light btn-outline-info"><FontAwesomeIcon icon="cloud-arrow-down" /> Download</button>
      </div>
      <label>
        <input type="checkbox" className="form-check-input me-1" checked={ignoreProfilerHeavyEntries} onChange={e => setIgnoreProfilerHeavyEntries(e.currentTarget.checked)} />
        Ignore Heavy Profiler Entries
      </label>
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

function EntrieListPath({ width, entries }: { width: number, entries: ProfilerClient.HeavyProfilerEntry[] }) {

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

  function handleOnClick(e: React.MouseEvent, v: ProfilerClient.HeavyProfilerEntry) {
    let url = "/profiler/heavy/entry/" + v.fullIndex;

    if (e.ctrlKey) {
      window.open(AppContext.toAbsoluteUrl(url));
    } else {
      AppContext.navigate(url);
    }
  }

  return (
    <svg width={width + "px"} height={height + "px"}>
      {data.map((v, i) => {
        var isPH = v.kind == "Web.ProfilerClient.API GET" && v.additionalData != null && v.additionalData.contains("/api/profilerHeavy/");
        return (<g className="entry" data-full-key={v.fullIndex} key={v.fullIndex} onClick={e => handleOnClick(e, v)} opacity={isPH ? 0.5 : undefined}>
          <rect className="left-background" x={0} y={y(i)} width={labelWidth} height={entryHeight} fill="#ddd" stroke="#fff" />
          <text className="label label-left" y={y(i)} dy={fontPadding + fontSize} fill="#000">{v.kind + " " + v.additionalData}</text>
          <rect className="right-background" x={labelWidth} y={y(i)} width={width - labelWidth} height={entryHeight} fill="#fff" stroke="#ddd" />
          <rect className="shape" x={x(v.start)} y={y(i)} width={x(v.end)! - x(v.start)!} height={entryHeight} fill={v.color} />
          <text className="label label-right" x={x(v.end)! + 3} y={y(i)} width={x(v.end)! - x(v.start)!} dy={fontPadding + fontSize} height={entryHeight} fill='#000'>{v.elapsed}</text>
          <title>{v.elapsed + " - " + v.additionalData}</title>
        </g>)

      })}
    </svg>
  );
}



