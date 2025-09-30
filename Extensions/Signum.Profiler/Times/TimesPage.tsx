import * as React from 'react'
import { DateTime, Duration } from 'luxon'
import { Navigator } from '@framework/Navigator'
import { ProfilerClient } from '../ProfilerClient'
import { useLocation, useParams } from "react-router";

import "./Times.css"
import { Tab, Tabs } from 'react-bootstrap';
import { useAPI, useAPIWithReload } from '@framework/Hooks';
import { useTitle } from '@framework/AppContext';
import { Finder } from '@framework/Finder';
import { toNumberFormat } from '@framework/Reflection';
import { isDurationKey } from '@framework/Lines/TimeLine';
import { Color } from '@framework/Basics/Color';
import { getToString } from '@framework/Signum.Entities';


export default function TimesPage(): React.JSX.Element {

  const [times, reloadTimes] = useAPIWithReload(() => ProfilerClient.API.Times.fetchInfo(), []);
  useTitle("Times state");

  function handleClear(e: React.MouseEvent<any>) {
    ProfilerClient.API.Times.clear().then(() => reloadTimes());
  }

  if (times == undefined)
    return <h3>Times (loading...)</h3>;

  return (
    <div>
      <h3 className="display-6">Times</h3>
      <div className="btn-toolbar">
        <button onClick={() => reloadTimes()} className="btn btn-tertiary">Reload</button>
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


function toHumanMilis(milis: number) {
  var result = Duration.fromMillis(milis).shiftTo("hour", "minute", "second", "millisecond").toObject();
  for (var key in result) {
    if ((result as any)[key] == 0)
      delete (result as any)[key];
  }

  return Duration.fromObject(result).toHuman();
}

function TimesBars({ times }: { times: ProfilerClient.TimeTrackerEntry[] }) {

  var nf = toNumberFormat("C0");
  function formatMilis(milis: number) {
    return <span title={ toHumanMilis(milis)}>{nf.format(milis)} ms</span>
  }
  const maxWidth = 600;

  const maxDuration = times.map(a => a.max.duration).max()!;
  const maxTotal = times.map(a => a.totalDuration).max()!;

  const ratio = maxWidth / maxDuration;

  function drawLineRow(label: string, time: ProfilerClient.TimeTrackerTime, className: string) {
    return (
      <tr>
        <td>{label}</td>
        <td className="leftBorder" title={time && `${time.date} (${DateTime.fromISO(time.date).toRelative()})
      ${time.url}
      ${getToString(time.user)}
      `}>
          <span className={className} style={{ width: (time.duration * ratio) + "px", marginTop: "8px" }}></span> {formatMilis(time.duration)} ({DateTime.fromISO(time.date).toRelative()})
        </td>
      </tr>
    );
  }

  return (
    <table className="table">
      <tbody>
        {
          times.orderByDescending(a => a.totalDuration).map((pair, i) =>
            <tr className="st-tt-entry" key={i}>
              <td>
                <div>
                  <span className="processName"> {pair.identifier.tryBefore(' ') ?? pair.identifier}</span>
                  {pair.identifier.tryAfter(' ') != undefined && <span className="sf-tt-entityname"> {pair.identifier.after(' ')} </span>}
                </div>
                <div>
                  <span className="numTimes">Executed {pair.count} {pair.count == 1 ? "time" : "times"} Total {formatMilis(pair.totalDuration)}</span>
                </div>
                <div className="sum" style={{ width: (100 * pair.totalDuration / maxTotal) + "%"}}></div>
              </td>
              <td>
                <table>
                  {drawLineRow("Last", pair.last, "last")}
                  {drawLineRow("Max", pair.max, "max")}
                  {pair.max2 && drawLineRow("Max 2", pair.max2, "max")}
                  {pair.max3 &&drawLineRow("Max 3", pair.max3, "max")}
                  <tr>
                    <td>Average</td>
                    <td className="leftBorder">
                      <span className="med" style={{ width: (pair.averageDuration * ratio) + "px", marginTop: "8px"  }}></span> {formatMilis(pair.averageDuration)}
                  </td>
                  </tr>
                  {drawLineRow("Min", pair.min, "min")}

                </table>
              </td>
            </tr>
          )}
      </tbody>
    </table>
  );
}

function TimesTable({ times }: { times: ProfilerClient.TimeTrackerEntry[] }) {

  var white = Color.White;
  var blue = Color.parse("#2980B9");
  const getColorCount = (f: number) => white.lerp(f, blue).toString();
  var red = Color.parse("#C0392B");
  const getColorMax = (f: number) => white.lerp(f, red).toString();
  var violet = Color.parse("#6C3483");
  const getColorTotal = (f: number) => white.lerp(f, violet).toString();

  const max = {
    count: times.max(a => a.count)!,
    duration: times.max(a => a.max.duration)!,
    totalDuration: times.max(a => a.totalDuration)!,
  };

  var nf = toNumberFormat("C0");

  function getTimeCell(time?: ProfilerClient.TimeTrackerTime) {
    if (time == null)
      return <td></td>;

    return (
      <td style={{ textAlign: "right", background: getColorMax(time.duration / max.duration) }}
        title={`${time.date} (${DateTime.fromISO(time.date).toRelative()})
      ${time.url}
      ${getToString(time.user)}
      `}
      >{nf.format(time.duration)} ms</td>
    );
  }

  return (
    <table className="table table-nonfluid">
      <thead>
        <tr>
          <th>Name</th>
          <th>Entity</th>
          <th>Count</th>
          <th>Min</th>
          <th>Average</th>
          <th>Max 3</th>
          <th>Max 2</th>
          <th>Max</th>
          <th>Last</th>
          <th>Total</th>
        </tr>
      </thead>
      <tbody>
        {times.orderByDescending(a => a.totalDuration).map((pair, i) =>
          <tr style={{ background: "#FFFFFF" }} key={i}>
            <td>
              <span className="processName"> {pair.identifier.tryBefore(' ') ?? pair.identifier}</span>
            </td>
            <td>
              {pair.identifier.tryAfter(' ') && <span className="sf-tt-entityname">{pair.identifier.tryAfter(' ')}</span>}
            </td>
            <td style={{ textAlign: "end", background: getColorCount(pair.count / max.count) }}>{pair.count}</td>
            {getTimeCell(pair.min)}
            <td style={{ textAlign: "end", background: getColorMax(pair.averageDuration / max.duration) }}>{pair.count}</td>
            {getTimeCell(pair.max3)}
            {getTimeCell(pair.max2)}
            {getTimeCell(pair.max)}
            {getTimeCell(pair.last)}
            <td style={{ textAlign: "end", background: getColorTotal(pair.totalDuration / max.totalDuration) }}>{nf.format(pair.totalDuration)} ms</td>
          </tr>
        )}
      </tbody>
    </table>
  );
}






