import * as React from 'react'
import { RouteComponentProps } from 'react-router-dom'
import numbro from 'numbro'
import * as Navigator from '@framework/Navigator'
import EntityLink from '@framework/SearchControl/EntityLink'
import { API, ProcessLogicState } from './ProcessClient'
import { ProcessEntity } from './Signum.Entities.Processes'
import { SearchControl } from '@framework/Search';
import { useAPI, useTitle, useAPIWithReload } from '../../../Framework/Signum.React/Scripts/Hooks'

export default function ProcessPanelPage(p: RouteComponentProps<{}>) {
  const [state, reloadState] = useAPIWithReload(() => API.view(), []);

  useTitle("ProcessLogic state");

  function handleStop(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.stop().then(() => reloadState()).done();
  }

  function handleStart(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.start().then(() => reloadState()).done();
  }


  if (state == undefined)
    return <h2>ProcesLogic state (loading...) </h2>;

  const s = state;

  return (
    <div>
      <h2>ProcessLogic state</h2>
      <div className="btn-toolbar">
        {s.running && <a href="#" className="sf-button btn btn-light active" style={{ color: "red" }} onClick={handleStop}>Stop</a>}
        {!s.running && <a href="#" className="sf-button btn btn-light" style={{ color: "green" }} onClick={handleStart}>Start</a>}
      </div >
      <div id="processMainDiv">
        <br />
        State: <strong>
          {s.running ?
            <span style={{ color: "Green" }}> RUNNING </span> :
            <span style={{ color: "Red" }}> STOPPED </span>
          }</strong>
        <br />
        JustMyProcesses: {s.justMyProcesses.toString()}
        <br />
        MaxDegreeOfParallelism: {s.maxDegreeOfParallelism}
        <br />
        InitialDelayMiliseconds: {s.initialDelayMiliseconds}
        <br />
        NextPlannedExecution: {s.nextPlannedExecution ?? "-None-"}
        <br />
        <table className="table">
          <thead>
            <tr>
              <th>Process
                              </th>
              <th>State
                              </th>
              <th>Progress
                              </th>
              <th>MachineName
                              </th>
              <th>ApplicationName
                              </th>
              <th>IsCancellationRequested
                              </th>

            </tr>
          </thead>
          <tbody>
            <tr>
              <td colSpan={4}>
                <b> {s.executing.length} processes executing in {s.machineName}</b>
              </td>
            </tr>
            {s.executing.map((item, i) =>
              <tr key={i}>
                <td> <EntityLink lite={item.process} inSearch={true} /> </td>
                <td> {item.state} </td>
                <td> {numbro(item.progress).format("0.00 %")} </td>
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
