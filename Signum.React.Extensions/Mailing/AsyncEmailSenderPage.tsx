import * as React from 'react'
import * as moment from 'moment'
import { RouteComponentProps } from 'react-router'
import * as Navigator from '@framework/Navigator'
import { SearchControl } from '@framework/Search'
import { API, AsyncEmailSenderState } from './MailingClient'
import { EmailMessageEntity } from './Signum.Entities.Mailing'
import { useTitle, useAPI, useAPIWithReload } from '../../../Framework/Signum.React/Scripts/Hooks'

export default function AsyncEmailSenderPage(p: RouteComponentProps<{}>) {

  useTitle("AsyncEmailSender state");

  const [state, reloadState] = useAPIWithReload(() => API.view(), [], { avoidReset: true });

  function handleStop(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.stop().then(() => reloadState()).done();
  }

  function handleStart(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.start().then(() => reloadState()).done();
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
            <span style={{ color: "Green" }}> RUNNING </span> :
            <span style={{ color: "Red" }}> STOPPED </span>
          }</strong>
        <br />
        CurrentProcessIdentifier: {state.currentProcessIdentifier}
        <br />
        AsyncSenderPeriod: {state.asyncSenderPeriod} sec
        <br />
        NextPlannedExecution: {state.nextPlannedExecution} ({state.nextPlannedExecution == undefined ? "-None-" : moment(state.nextPlannedExecution).fromNow()})
        <br />
        IsCancelationRequested: {state.isCancelationRequested}
        <br />
        QueuedItems: {state.queuedItems}
      </div>
      <br />
      <h2>{EmailMessageEntity.niceName()}</h2>
      <SearchControl findOptions={{
        queryName: EmailMessageEntity,
        orderOptions: [{ token: EmailMessageEntity.token().entity(e => e.creationDate), orderType: "Descending" }],
        pagination: { elementsPerPage: 10, mode: "Firsts" }
      }} />
    </div>
  );
}
