import * as React from 'react'
import { RouteComponentProps } from 'react-router-dom'
import * as Navigator from '@framework/Navigator'
import EntityLink from '@framework/SearchControl/EntityLink'
import { API, ProcessLogicState } from './ProcessClient'
import { ProcessEntity } from './Signum.Entities.Processes'
import { SearchControl } from '@framework/Search';
import * as AppContext from '@framework/AppContext'
import { useAPI, useAPIWithReload } from '@framework/Hooks'
import { useTitle } from '@framework/AppContext'
import { toNumberFormat } from '@framework/Reflection'

export default function ProcessPanelPage(p: RouteComponentProps<{}>) {
  const [state, reloadState] = useAPIWithReload(() => API.view(), []);

  useTitle("ProcessLogic state");

  function handleStop(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.stop().then(() => reloadState());
  }

  function handleStart(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.start().then(() => reloadState());
  }


  if (state == undefined)
    return <h2>ProcesLogic state (loading...) </h2>;

  const s = state;

  var percentageFormat = toNumberFormat("P2");

  return (
    <div>
      <h2>ProcessLogic state</h2>
      <div className="btn-toolbar">
        {s.running && <a href="#" className="sf-button btn bg-light active" style={{ color: "red" }} onClick={handleStop}>Stop</a>}
        {!s.running && <a href="#" className="sf-button btn bg-light" style={{ color: "green" }} onClick={handleStart}>Start</a>}
      </div >
      <div id="processMainDiv">
        <br />
        State: <strong>
          {s.running ?
            <span style={{ color: "green" }}> RUNNING </span> :
            <span style={{ color: state.initialDelayMilliseconds == null ? "gray" : "red" }}> STOPPED </span>
          }</strong>
          <a className="ms-2" href={AppContext.toAbsoluteUrl("~/api/processes/simpleStatus")} target="_blank">SimpleStatus</a>
        <br />
        JustMyProcesses: {s.justMyProcesses.toString()}
        <br />
        MachineName: {s.machineName}
        <br />
        ApplicatonName: {s.applicationName}
        <br />
        MaxDegreeOfParallelism: {s.maxDegreeOfParallelism}
        <br />
        InitialDelayMilliseconds: {s.initialDelayMilliseconds}
        <br />
        NextPlannedExecution: {s.nextPlannedExecution ?? "-None-"}
        <br />
        <table className="table">
          <thead>
            <tr>
              <th>Process</th>
              <th>State</th>
              <th>Progress</th>
              <th>MachineName</th>
              <th>ApplicationName</th>
              <th>IsCancellationRequested</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td colSpan={6}>
                <b> {s.executing.length} processes executing in {s.machineName} / {s.applicationName}</b>
              </td>
            </tr>
            {s.executing.map((item, i) =>
              <tr key={i}>
                <td> <EntityLink lite={item.process} inSearch={true} /> </td>
                <td> {item.state} </td>
                <td> {percentageFormat.format(item.progress)} </td>
                <td> {item.machineName} </td>
                <td> {item.applicationName} </td>
                <td> {item.isCancellationRequested} </td>
              </tr>
            )}
          </tbody>
        </table>

        <br />
        <h2>Latest Processes</h2>
        <SearchControl findOptions={{
          queryName: ProcessEntity,
          orderOptions: [{ token: ProcessEntity.token(e => e.creationDate), orderType: "Descending" }],
          pagination: { elementsPerPage: 10, mode: "Firsts" }
        }}
        />
      </div>
    </div>
  );
}
