import * as React from 'react'
import { Link } from 'react-router-dom'
import * as d3 from 'd3'
import { } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import { API, HeavyProfilerEntry, StackTraceTS } from '../ProfilerClient'
import { RouteComponentProps } from "react-router";
import "./Profiler.css"
import { useAPI } from '../../../../Framework/Signum.React/Scripts/Hooks'

interface HeavyEntryProps extends RouteComponentProps<{ selectedIndex: string }> {

}

export default function HeavyEntry(p: HeavyEntryProps) {

  const selectedIndex = p.match.params.selectedIndex;
  const rootIndex = selectedIndex.tryBefore("-") || selectedIndex;
  const entries = useAPI(() => API.Heavy.details(rootIndex), [rootIndex]);
  const stackTrace = useAPI(() => API.Heavy.stackTrace(selectedIndex), [selectedIndex]);
  const [asyncDepth, setAsyncDepth] = React.useState<boolean>(false);

  function handleDownload() {
    let selectedIndex = p.match.params.selectedIndex;
    API.Heavy.download(selectedIndex.tryBefore("-") || selectedIndex);
  }

  function handleUpdate() {
    loadEntries(p);
  }

  const index = p.match.params.selectedIndex;
  Navigator.setTitle("Heavy Profiler > Entry " + index);
  if (entries == undefined)
    return <h3 className="display-6">Heavy Profiler > Entry {index} (loading...) </h3>;

  let current = entries.filter(a => a.fullIndex == p.match.params.selectedIndex).single();
  return (
    <div>
      <h2 className="display-6"><Link to="~/profiler/heavy">Heavy Profiler</Link> > Entry {index}</h2>
      <label><input type="checkbox" checked={asyncDepth} onChange={a => setAsyncDepth(a.currentTarget.checked)} />Async Stack</label>
      <br />
      {entries && <HeavyProfilerDetailsD3 entries={entries} selected={current} asyncDepth={asyncDepth} />}
      <br />
      <table className="table table-nonfluid">
        <tbody>
          <tr>
            <th>Role</th>
            <td>{current.role}</td>
          </tr>
          <tr>
            <th>Time</th>
            <td>{current.elapsed}</td>
          </tr>
          <tr>
            <td colSpan={2}>
              <div className="btn-toolbar">
                <button onClick={handleDownload} className="btn btn-info">Download</button>
                {!current.isFinished && <button onClick={handleUpdate} className="btn btn-light">Update</button>}
              </div>
            </td>
          </tr>
        </tbody>
      </table>
      <br />
      <h3>Aditional Data</h3>
      <pre style={{ maxWidth: "1000px", overflowY: "scroll" }}><code>{current.additionalData}</code></pre>
      <br />
      <h3>StackTrace</h3>
      {
        stackTrace == undefined ? <span>No Stacktrace</span> :
          <StackFrameTable stackTrace={stackTrace} />
      }
    </div>
  );
}


export function StackFrameTable(p : { stackTrace: StackTraceTS[] }){
  if (p.stackTrace == undefined)
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
        {p.stackTrace.map((sf, i) =>
          <tr key={i}>
            <td>
              {sf.namespace && <span style={{ color: sf.color }}>{sf.namespace}</span>}
            </td>
            <td>
              {sf.type && <span style={{ color: sf.color }}>{sf.type}</span>}
            </td>
            <td>
              {sf.method}
            </td>
            <td>
              {sf.fileName} {sf.lineNumber > 0 && "(" + sf.lineNumber + ")"}
            </td>
          </tr>
        )}
      </tbody>
    </table>
  );
}


function lerp(min: number, ratio: number, max: number) {
  return min * (1 - ratio) + max * ratio;
}


interface HeavyProfilerDetailsD3Props {
  entries: HeavyProfilerEntry[];
  selected: HeavyProfilerEntry;
  asyncDepth: boolean;
}


interface MinMax {
  min: number;
  max: number;
}

export function HeavyProfilerDetailsD3(p: HeavyProfilerDetailsD3Props) {


  const [minMax, setMinMax] = React.useState<MinMax>(() => resetZoom(p.selected))


  const chartContainer = React.useRef<HTMLDivElement>(null);

  function resetZoom(current: HeavyProfilerEntry): MinMax {
    return ({
      min: lerp(current.beforeStart, -0.1, current.end),
      max: lerp(current.beforeStart, 1.1, current.end)
    });
  }

  React.useEffect(() => {
    mountChart(p);

    chartContainer.current!.addEventListener("wheel", handleWeel, { passive: false, capture: true });

    return () => {
      chartContainer.current!.removeEventListener("wheel", handleWeel);
    };
  });

  React.useEffect(() => {
    mountChart(p);
  }, [p.asyncDepth, p.selected]);

  
  function handleWeel(e: WheelEvent) {
    e.preventDefault();
    e.stopPropagation();

    let dist = minMax.max - minMax.min;

    const inc = 1.2;

    let delta = 1 - (e.deltaY > 0 ? (1 / inc) : inc);

    let elem = e.currentTarget as HTMLElement;

    let ne = e/*.nativeEvent*/ as MouseEvent;

    const rect = elem.getBoundingClientRect();

    const ratio = (ne.clientX - rect.left) / rect.width;

    let newMin = minMax.min - dist * delta * (ratio);
    let newMax = minMax.max + dist * delta * (1 - ratio);

    setMinMax({
      min: newMin,
      max: newMax
    });
  }


  function mountChart(props: HeavyProfilerDetailsD3Props) {
    if (chartContainer == undefined)
      throw new Error("chartContainer not mounted!");

    let data = props.entries;

    if (data == undefined)
      throw new Error("no entries");

    var getDepth = props.asyncDepth ?
      (e: HeavyProfilerEntry) => e.asyncDepth :
      (e: HeavyProfilerEntry) => e.depth;

    let fontSize = 12;
    let fontPadding = 3;
    let maxDepth = d3.max(data, getDepth)!;

    let height = ((fontSize * 2) + (3 * fontPadding)) * (maxDepth + 1);
    chartContainer.style.height = height + "px";

    let y = d3.scaleLinear()
      .domain([0, maxDepth + 1])
      .range([0, height]);

    let entryHeight = y(1);

    d3.select(chartContainer.current!).select("svg").remove();

    const chart = d3.select(chartContainer.current!)
      .append<SVGElement>('svg:svg').attr('height', height);


    function updateChar () {

      let { min, max } = minMax;
      let width = chartContainer.current.getBoundingClientRect().width;
      let sel = p.selected;
      let x = d3.scaleLinear()
        .domain([min, max])
        .range([0, width]);

      var filteredData = data.filter(a => a.end > min && a.beforeStart < max && (x(a.end) - x(a.beforeStart)) > 1);

      const selection = chart.selectAll<SVGGElement, any>("g.entry").data(filteredData, a => (a as HeavyProfilerEntry).fullIndex);

      selection.exit().remove();

      var newGroups = selection.enter()
        .append<SVGGElement>('svg:g')
        .attr('class', 'entry')
        .attr('data-key', a => a.fullIndex);

      newGroups.append<SVGRectElement>('svg:rect').attr('class', 'shape')
        .attr('y', v => y(getDepth(v)))
        .attr('height', entryHeight - 1)
        .attr('fill', v => v.color);

      newGroups.append<SVGRectElement>('svg:rect').attr('class', 'shape-before')
        .attr('y', v => y(getDepth(v)) + 1)
        .attr('height', entryHeight - 2)
        .attr('fill', '#fff');

      newGroups.append<SVGTextElement>('svg:text').attr('class', 'label label-top')
        .attr('dy', v => y(getDepth(v)))
        .attr('y', fontPadding + fontSize)
        .text(v => v.elapsed);

      newGroups.append<SVGTextElement>('svg:text').attr('class', 'label label-bottom')
        .attr('dy', v => y(getDepth(v)))
        .attr('y', (2 * fontPadding) + (2 * fontSize))
        .text(v => v.role + (v.additionalData ? (" - " + v.additionalData.etc(30)) : ""));

      newGroups.append('svg:title').text(v => v.role + v.elapsed);

      newGroups.on("click", e => {

        if (e == p.selected) {

        }
        else {
          let url = "~/profiler/heavy/entry/" + e.fullIndex;

          if (d3.event.ctrlKey) {
            window.open(Navigator.toAbsoluteUrl(url));
          }
          else {
            Navigator.history.push(url);
          }
        }
      });

      newGroups.on("dblclick", e => {
         setMinMax(resetZoom(e));
      });

      chart.attr('width', width);

      var updateGroups = newGroups.merge(selection);

      updateGroups.select<SVGRectElement>("rect.shape")
        .attr('x', v => x(Math.max(min, v.beforeStart)))
        .attr('width', v => Math.max(0, x(Math.min(max, v.end)) - x(Math.max(min, v.beforeStart))))
        .attr('stroke', v => v == sel ? '#000' : '#ccc');

      updateGroups.select<SVGRectElement>("rect.shape-before")
        .attr('x', v => x(Math.max(min, v.beforeStart)))
        .attr('width', v => Math.max(0, x(Math.min(max, v.start)) - x(Math.max(min, v.beforeStart))));

      updateGroups.select<SVGTextElement>("text.label.label-top")
        .attr('dx', v => x(Math.max(min, v.start)) + 3)
        .attr('fill', v => v == sel ? '#000' : '#fff');

      updateGroups.select<SVGTextElement>("text.label.label-bottom")
        .attr('dx', v => x(Math.max(min, v.start)) + 3)
        .attr('fill', v => v == sel ? '#000' : '#fff');
    };

  }

  return (
    <div className="sf-profiler-chart" ref={chartContainer}>


    </div>
  );
}
