import * as React from 'react'
import { DateTime } from 'luxon'
import * as Navigator from '@framework/Navigator'
import { API, TimeTrackerEntry } from '../ProfilerClient'
import { RouteComponentProps } from "react-router";

import "./Times.css"
import { Tab, Tabs } from 'react-bootstrap';
import { useAPI, useAPIWithReload } from '@framework/Hooks';
import { useTitle } from '@framework/AppContext';

interface TimesPageProps extends RouteComponentProps<{}> {

}

export default function TimesPage(p: TimesPageProps) {

  const [times, reloadTimes] = useAPIWithReload(() => API.Times.fetchInfo(), []);
  useTitle("Times state");

  function handleClear(e: React.MouseEvent<any>) {
    API.Times.clear().then(() => reloadTimes());
  }

  if (times == undefined)
    return <h3>Times (loading...)</h3>;

  return (
    <div>
      <h3 className="display-6">Times</h3>
      <div className="btn-toolbar">
        <button onClick={() => reloadTimes()} className="btn btn-light">Reload</button>
        <button onClick={handleClear} className="btn btn-warning">Clear</button>
      </div>
      <br />
      <Tabs id="timeMachineTabs">
        <Tab eventKey="bars" title="Bars">
          <TimesBars times={times} />
        </Tab>
        <Tab eventKey="table" title="Table">
          <TimesTable times={times} />
        </Tab>
      </Tabs>
    </div>
  );
}

function TimesBars({ times }: { times: TimeTrackerEntry[] }) {
  const maxWith = 600;

  const maxValue = times.map(a => a.maxTime).max()!;
  const maxTotal = times.map(a => a.totalTime).max()!;

  const ratio = maxWith / maxValue;

  return (
    <table className="table">
      <tbody>
        {
          times.orderByDescending(a => a.totalTime).map((pair, i) =>
            <tr className="task" key={i}>
              <td>
                <div>
                  <span className="processName"> {pair.key.tryBefore(' ') ?? pair.key}</span>
                  {pair.key.tryAfter(' ') != undefined && <span className="entityName"> {pair.key.after(' ')} </span>}
                </div>
                <div>
                  <span className="numTimes">Executed {pair.count} {pair.count == 1 ? "time" : "times"} Total {pair.totalTime} ms </span>
                </div>
                <div className="sum" style={{ width: (100 * pair.totalTime / maxTotal) + "%" }}></div>
              </td>
              <td>
                <table>
                  <tr>
                    <td>Max</td>
                    <td className="leftBorder">
                      <span className="max" style={{ width: (pair.maxTime * ratio) + "px" }}></span> {pair.maxTime} ms ({DateTime.fromISO(pair.maxDate).toRelative()})
                    </td>
                  </tr>
                  <tr>
                    <td>Average</td>
                    <td className="leftBorder">
                      <span className="med" style={{ width: (pair.averageTime * ratio) + "px" }}></span> {pair.averageTime} ms
                  </td>
                  </tr>
                  <tr>
                    <td>Min</td>
                    <td className="leftBorder">
                      <span className="min" style={{ width: (pair.minTime * ratio) + "px" }}></span> {pair.minTime} ms ({DateTime.fromISO(pair.minDate).toRelative()})
                  </td>
                  </tr>
                  <tr>
                    <td>Last</td>
                    <td className="leftBorder">
                      <span className="last" style={{ width: (pair.lastTime * ratio) + "px" }}></span> {pair.lastTime} ms ({DateTime.fromISO(pair.lastDate).toRelative()})
                  </td>
                  </tr>
                </table>
              </td>
            </tr>
          )}
      </tbody>
    </table>
  );
}

function TimesTable({ times }: { times: TimeTrackerEntry[] }) {
  const getColor = (f: number) => `rgb(255, ${(1 - f) * 255}, ${(1 - f) * 255})`;

  const max = {
    count: times.map(a => a.count).max()!,
    lastTime: times.map(a => a.lastTime).max()!,
    minTime: times.map(a => a.minTime).max()!,
    averageTime: times.map(a => a.averageTime).max()!,
    maxTime: times.map(a => a.maxTime).max()!,
    totalTime: times.map(a => a.totalTime).max()!,
  };

  return (
    <table className="table table-nonfluid">
      <thead>
        <tr>
          <th>Name</th>
          <th>Entity</th>
          <th>Executions</th>
          <th>Last Time</th>
          <th>Min</th>
          <th>Average</th>
          <th>Max</th>
          <th>Total</th>
        </tr>
      </thead>
      <tbody>
        {times.map((pair, i) =>
          <tr style={{ background: "#FFFFFF" }} key={i}>
            <td>
              <span className="processName"> {pair.key.tryBefore(' ') ?? pair.key}</span>
            </td>
            <td>
              {pair.key.tryAfter(' ') && <span className="entityName">{pair.key.tryAfter(' ')}</span>}
            </td>
            <td style={{ textAlign: "center", background: getColor(pair.count / max.count) }}>{pair.count}</td>
            <td style={{ textAlign: "right", background: getColor(pair.lastTime / max.lastTime) }}>{pair.lastTime} ms</td>
            <td style={{ textAlign: "right", background: getColor(pair.minTime / max.minTime) }}>{pair.minTime} ms</td>
            <td style={{ textAlign: "right", background: getColor(pair.averageTime / max.averageTime) }}>{pair.averageTime} ms</td>
            <td style={{ textAlign: "right", background: getColor(pair.maxTime / max.maxTime) }}>{pair.maxTime} ms</td>
            <td style={{ textAlign: "right", background: getColor(pair.totalTime / max.totalTime) }}>{pair.totalTime} ms</td>
          </tr>
        )}
      </tbody>
    </table>
  );
}






