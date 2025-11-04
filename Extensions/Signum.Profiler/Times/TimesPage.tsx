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
import { TimeMessage } from '../Signum.Profiler';
import { AccessibleRow, AccessibleTable } from '../../../Signum/React/Basics/AccessibleTable';


export default function TimesPage(): React.JSX.Element {

  const [times, reloadTimes] = useAPIWithReload(() => ProfilerClient.API.Times.fetchInfo(), []);
  useTitle("Times state");

  function handleClear(e: React.MouseEvent<any>) {
    ProfilerClient.API.Times.clear().then(() => reloadTimes());
  }

  if (times == undefined)
    return <h3>{TimeMessage.TimesLoading.niceToString()}</h3>;

  return (
    <div>
      <h3 className="display-6">{TimeMessage.Times.niceToString()}</h3>
      <div className="btn-toolbar">
        <button type="button" onClick={() => reloadTimes()} className="btn btn-tertiary">{TimeMessage.Reload.niceToString()}</button>
        <button type="button" onClick={handleClear} className="btn btn-warning">{TimeMessage.Clear.niceToString()}</button>
      </div>
      <br />
      <Tabs id="timeMachineTabs">
        <Tab eventKey="bars" title={TimeMessage.Bars.niceToString()}>
          <TimesBars times={times} />
        </Tab>
        <Tab eventKey="table" title={TimeMessage.Table.niceToString()}>
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

  function drawLineRowDiv(label: string, time: ProfilerClient.TimeTrackerTime, className: string) {
    if (!time) return null;

    return (
      <div className="stat-row" key={label}>
        <div className="stat-cell label">{label}</div>
        <div
          className="stat-cell leftBorder"
          title={`${time.date} (${DateTime.fromISO(time.date).toRelative()})
${time.url}
${getToString(time.user)}`}
        >
          <span
            className={className}
            style={{ width: (time.duration * ratio) + "px", marginTop: "8px" }}
          ></span>
          &nbsp;{formatMilis(time.duration)} ({DateTime.fromISO(time.date).toRelative()})
        </div>
      </div>
    );
  }



  return (
    <AccessibleTable
      aria-label={TimeMessage.TimesOverview.niceToString()}
      className="table"
      multiselectable={false}>
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
                  <span className="numTimes">{TimeMessage.Executed.niceToString()} {pair.count} {pair.count == 1 ? "time" : "times"} {TimeMessage.Total.niceToString()} {formatMilis(pair.totalDuration)}</span>
                </div>
                <div className="sum" style={{ width: (100 * pair.totalDuration / maxTotal) + "%"}}></div>
              </td>
              <td>
                <div className="stat-table" role="group" aria-label={TimeMessage.TimeStatistics.niceToString()}>
                  {drawLineRowDiv("Last", pair.last, "last")}
                  {drawLineRowDiv("Max", pair.max, "max")}
                  {pair.max2 && drawLineRowDiv("Max 2", pair.max2, "max")}
                  {pair.max3 && drawLineRowDiv("Max 3", pair.max3, "max")}

                  <div className="stat-row">
                    <div className="stat-cell label">{TimeMessage.Average.niceToString()}</div>
                    <div className="stat-cell leftBorder">
                      <span className="med" style={{ width: (pair.averageDuration * ratio) + "px", marginTop: "8px" }}></span>
                      &nbsp;{formatMilis(pair.averageDuration)}
                    </div>
                  </div>

                  {drawLineRowDiv("Min", pair.min, "min")}
                </div>
              </td>
            </tr>
          )}
      </tbody>
    </AccessibleTable>
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

  function GetTimeRow(p: { pair: ProfilerClient.TimeTrackerEntry, i: number }) {
    const pair = p.pair;

    return (
      <AccessibleRow style={{ background: "#FFFFFF" }} key={p.i}>
        <td>
          <span className="processName"> {pair.identifier.tryBefore(' ') ?? pair.identifier}</span>
        </td>
        <td>
          {pair.identifier.tryAfter(' ') && <span className="sf-tt-entityname">{pair.identifier.tryAfter(' ')}</span>}
        </td>
        <td style={{ textAlign: "end", background: getColorCount(pair.count / max.count) }}>{pair.count}</td>
        {pair.min == null ? <td>{TimeMessage.NoDuration.niceToString()}</td> : <td style={{ textAlign: "right", background: getColorMax(pair.min.duration / max.duration) }}
          title={`${pair.min.date} (${DateTime.fromISO(pair.min.date).toRelative()})
          ${pair.min.url}
          ${getToString(pair.min.user)}`}>{nf.format(pair.min.duration)} ms
        </td>}
        <td style={{ textAlign: "end", background: getColorMax(pair.averageDuration / max.duration) }}>{pair.count}</td>
        {pair.max3 == null ? <td>{TimeMessage.NoDuration.niceToString()}</td> : <td style={{ textAlign: "right", background: getColorMax(pair.max3.duration / max.duration) }}
          title={`${pair.max3.date} (${DateTime.fromISO(pair.max3.date).toRelative()})
          ${pair.max3.url}
          ${getToString(pair.max3.user)}`}>{nf.format(pair.max3.duration)} ms
        </td>}

        {pair.max2 == null ? <td>{TimeMessage.NoDuration.niceToString()}</td> : <td style={{ textAlign: "right", background: getColorMax(pair.max2.duration / max.duration) }}
          title={`${pair.max2.date} (${DateTime.fromISO(pair.max2.date).toRelative()})
          ${pair.max2.url}
          ${getToString(pair.max2.user)}`}>{nf.format(pair.max2.duration)} ms
        </td>}

        {pair.max == null ? <td>{TimeMessage.NoDuration.niceToString()}</td> : <td style={{ textAlign: "right", background: getColorMax(pair.max.duration / max.duration) }}
          title={`${pair.max.date} (${DateTime.fromISO(pair.max.date).toRelative()})
          ${pair.max.url}
          ${getToString(pair.max.user)}`}>{nf.format(pair.max.duration)} ms
        </td>}

        {pair.last == null ? <td>{TimeMessage.NoDuration.niceToString()}</td> : <td style={{ textAlign: "right", background: getColorMax(pair.last.duration / max.duration) }}
          title={`${pair.last.date} (${DateTime.fromISO(pair.last.date).toRelative()})
          ${pair.last.url}
          ${getToString(pair.last.user)}`}>{nf.format(pair.last.duration)} ms
        </td>}
        <td style={{ textAlign: "end", background: getColorTotal(pair.totalDuration / max.totalDuration) }}>{nf.format(pair.totalDuration)} ms</td>
      </AccessibleRow>
    );
  }


  return (
    <AccessibleTable
      aria-label={TimeMessage.TimesOverview.niceToString()}
      className="table table-nonfluid"
      mapCustomComponents={new Map([[GetTimeRow, "tr"]])}
      multiselectable={false}>
      <thead>
        <tr>
          <th>{TimeMessage.Name.niceToString()}</th>
          <th>{TimeMessage.Entity.niceToString()}</th>
          <th>{TimeMessage.Count.niceToString()}</th>
          <th>{TimeMessage.Min.niceToString()}</th>
          <th>{TimeMessage.Average.niceToString()}</th>
          <th>{`${TimeMessage.Max.niceToString()} 3`}</th>
          <th>{`${TimeMessage.Max.niceToString()} 2`}</th>
          <th>{TimeMessage.Max.niceToString()}</th>
          <th>{TimeMessage.Last.niceToString()}</th>
          <th>{TimeMessage.Total.niceToString()}</th>
        </tr>
      </thead>
      <tbody>
        {times.orderByDescending(a => a.totalDuration).map((pair, i) => <GetTimeRow pair={pair} i={i} />)}
      </tbody>
    </AccessibleTable>
  );
}






