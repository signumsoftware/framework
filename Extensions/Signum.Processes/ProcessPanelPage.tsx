import * as React from 'react'
import EntityLink from '@framework/SearchControl/EntityLink'
import { ProcessClient } from './ProcessClient'
import { ProcessEntity, ProcessMessage } from './Signum.Processes'
import { SearchControl } from '@framework/Search';
import { useAPIWithReload, useInterval } from '@framework/Hooks'
import { useTitle } from '@framework/AppContext'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { ProcessProgressBar } from './Templates/Process'
import { FrameMessage } from '../../Signum/React/Signum.Entities';
import { Overlay, Tooltip } from "react-bootstrap";
import * as AppContext from '@framework/AppContext';
import { CopyHealthCheckButton } from '@framework/Components/CopyHealthCheckButton';
import { AccessibleTable } from '../../Signum/React/Basics/AccessibleTable';

export default function ProcessPanelPage(): React.JSX.Element {


  const [state, reloadState] = useAPIWithReload(() => ProcessClient.API.view(), [], { avoidReset: true });

  const tick = useInterval(state == null || state.running ? 500 : null, 0, n => n + 1);

  React.useEffect(() => {
    reloadState();
  }, [tick]);

  useTitle("Process Runner");

  function handleStop(e: React.MouseEvent<any>) {
    ProcessClient.API.stop().then(() => reloadState());
  }

  function handleStart(e: React.MouseEvent<any>) {
    ProcessClient.API.start().then(() => reloadState());
  }

  if (state == undefined)
    return <h2>{ProcessMessage.ProcessLogicStateLoading.niceToString()}</h2>;

  const s = state;
  const url = window.location;

  return (
    <div>
      <div className='d-flex align-items-center'><h2 className="display-6"><FontAwesomeIcon aria-hidden="true" icon={"gears"} /> {ProcessMessage.ProcessPanel.niceToString()} <CopyHealthCheckButton
        name={url.hostname + " Process Runner"}
        healthCheckUrl={url.origin + AppContext.toAbsoluteUrl('/api/processes/healthCheck')}
        clickUrl={url.href}
      /></h2></div>
      <div className="btn-toolbar mt-3">
        <button type="button" className={classes("sf-button btn", s.running ? "btn-success disabled" : "btn-outline-success")} onClick={!s.running ? handleStart : undefined}><FontAwesomeIcon aria-hidden="true" icon="play" /> {ProcessMessage.Start.niceToString()}</button>
        <button type="button" className={classes("sf-button btn", !s.running ? "btn-danger disabled" : "btn-outline-danger")} onClick={s.running ? handleStop : undefined}><FontAwesomeIcon aria-hidden="true" icon="stop" /> {ProcessMessage.Stop.niceToString()}</button>
      </div >
      <div id="processMainDiv">
        {ProcessMessage.State.niceToString()}: <strong>
          {s.running ?
            <span style={{ color: "green" }}> {ProcessMessage.Running.niceToString()} </span> :
            <span style={{ color: state.initialDelayMilliseconds == null ? "gray" : "red" }}> {ProcessMessage.Stopped.niceToString()} </span>
          }</strong>
        <a className="ms-2" href={AppContext.toAbsoluteUrl("/api/processes/simpleStatus")} target="_blank">{ProcessMessage.SimpleStatus.niceToString()}</a>
        <br />
        {ProcessMessage.JustMyProcesses.niceToString()}: {s.justMyProcesses.toString()}
        <br />
        {ProcessMessage.MachineName.niceToString()}: {s.machineName}
        <br />
        {ProcessMessage.ApplicationName.niceToString()}: {s.applicationName}
        <br />
        {ProcessMessage.MaxDegreeOfParallelism.niceToString()}: {s.maxDegreeOfParallelism}
        <br />
        {ProcessMessage.InitialDelayMilliseconds.niceToString()}: {s.initialDelayMilliseconds}
        <br />
        {ProcessMessage.NextPlannedExecution.niceToString()}: {s.nextPlannedExecution ?? ProcessMessage.None.niceToString() }
        <br />
        <AccessibleTable
          caption={ProcessMessage.ExecutingProcesses.niceToString()}
          className="table"
          multiselectable={false}>
          <thead>
            <tr>
              <th>{ProcessMessage.Process.niceToString()}</th>
              <th>{ProcessMessage.State.niceToString()}</th>
              <th style={{ minWidth: "30%" }}>{ProcessMessage.Progress.niceToString()}</th>
              <th>{ProcessMessage.MachineName.niceToString()}</th>
              <th>{ProcessMessage.ApplicationName.niceToString()}</th>
              <th>{ProcessMessage.IsCancellationRequest.niceToString()}</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td colSpan={6}>
                <b>{ProcessMessage._0ProcessesExcecutingIn1_2.niceToString(s.executing.length, s.machineName, s.applicationName)}</b>
              </td>
            </tr>
            {s.executing.map((item, i) =>
              <tr key={i}>
                <td> <EntityLink lite={item.process} inSearch="main" /> </td>
                <td> {item.state} </td>
                <td style={{ verticalAlign: "middle" }}>  <ProcessProgressBar state={item.state} progress={item.progress} /></td>
                <td> {item.machineName} </td>
                <td> {item.applicationName} </td>
                <td> {item.isCancellationRequested} </td>
              </tr>
            )}
          </tbody>
        </AccessibleTable>
        <br />
        <h2>{ProcessMessage.LatestProcesses.niceToString()}</h2>
        <SearchControl findOptions={{
          queryName: ProcessEntity,
          orderOptions: [{ token: ProcessEntity.token(e => e.creationDate), orderType: "Descending" }],
          pagination: { elementsPerPage: 10, mode: "Firsts" }
        }}
          deps={[state?.executing.map(a => a.process.id!.toString()).join(",")]}
        />
      </div>
      <pre>
        {s.log}
      </pre>
    </div>
  );
}
