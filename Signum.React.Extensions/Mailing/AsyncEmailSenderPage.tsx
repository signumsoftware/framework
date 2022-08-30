import * as React from 'react'
import { DateTime } from 'luxon'
import { RouteComponentProps } from 'react-router'
import * as Navigator from '@framework/Navigator'
import { SearchControl } from '@framework/Search'
import { API, AsyncEmailSenderState } from './MailingClient'
import { EmailMessageEntity } from './Signum.Entities.Mailing'
import { useAPI, useAPIWithReload } from '@framework/Hooks'
import { toAbsoluteUrl, useTitle } from '@framework/AppContext'

export default function AsyncEmailSenderPage(p: RouteComponentProps<{}>) {

  useTitle("AsyncEmailSender state");

  const [state, reloadState] = useAPIWithReload(() => API.view(), [], { avoidReset: true });

  function handleStop(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.stop().then(() => reloadState());
  }

  function handleStart(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.start().then(() => reloadState());
  }

  if (state == undefined)
    return <h2>AsyncEmailSender state (loading...) </h2>;

  return (
    <div>
      <h2>AsyncEmailSender State</h2>
      <div className="btn-toolbar">
        {state.running && <a href="#" className="sf-button btn btn-light active" style={{ color: "red" }} onClick={handleStop}>Stop</a>}
        {!state.running && <a href="#" className="sf-button btn btn-light" style={{ color: "green" }} onClick={handleStart}>Start</a>}
      </div >

      <div>
        <br />
        State: <strong>
          {state.running ?
            <span style={{ color: "green" }}> RUNNING </span> :
            <span style={{ color: state.initialDelayMilliseconds == null ? "gray" : "red" }}> STOPPED </span>
          }</strong>
        <a className="ms-2" href={toAbsoluteUrl("~/api/asyncEmailSender/simpleStatus")} target="_blank">SimpleStatus</a>
        <br />
        InitialDelayMilliseconds: {state.initialDelayMilliseconds}
        <br/>
        MachineName: {state.machineName}
        <br />
        CurrentProcessIdentifier: {state.currentProcessIdentifier}
        <br />
        AsyncSenderPeriod: {state.asyncSenderPeriod} sec
        <br />
        NextPlannedExecution: {state.nextPlannedExecution} ({state.nextPlannedExecution == undefined ? "-None-" : DateTime.fromISO(state.nextPlannedExecution).toRelative()})
        <br />
        IsCancelationRequested: {state.isCancelationRequested}
        <br />
        QueuedItems: {state.queuedItems}
      </div>
      <br />
      <h2>{EmailMessageEntity.niceName()}</h2>
      <SearchControl findOptions={{
        queryName: EmailMessageEntity,
        orderOptions: [{ token: EmailMessageEntity.token(e => e.entity.creationDate), orderType: "Descending" }],
        pagination: { elementsPerPage: 10, mode: "Firsts" }
      }} />
    </div>
  );
}
