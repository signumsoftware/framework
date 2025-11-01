import * as React from 'react'
import { DateTime } from 'luxon'
import { useLocation, useParams } from 'react-router'
import { Navigator } from '@framework/Navigator'
import { SearchControl } from '@framework/Search'
import { MailingClient } from './MailingClient'
import { EmailMessageEntity } from './Signum.Mailing'
import { useAPI, useAPIWithReload, useInterval } from '@framework/Hooks'
import { toAbsoluteUrl, useTitle } from '@framework/AppContext'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import * as AppContext from '@framework/AppContext';
import { CopyHealthCheckButton } from '@framework/Components/CopyHealthCheckButton'

export default function AsyncEmailSenderPage(): React.JSX.Element {

  useTitle("AsyncEmailSender");

  const [state, reloadState] = useAPIWithReload(() => MailingClient.API.view(), [], { avoidReset: true });

  const tick = useInterval(state == null || state.running ? 500 : null, 0, n => n + 1);

  React.useEffect(() => {
    reloadState();
  }, [tick]);

  function handleStop(e: React.MouseEvent<any>) {
    MailingClient.API.stop().then(() => reloadState());
  }

  function handleStart(e: React.MouseEvent<any>) {
    MailingClient.API.start().then(() => reloadState());
  }

  if (state == undefined)
    return <h2>AsyncEmailSender state (loading...) </h2>;

  const s = state;
  const url = window.location;

  return (
    <div>
      <h2 className="display-6"><FontAwesomeIcon icon={"envelopes-bulk"} /> AsyncEmailSender <CopyHealthCheckButton
        name={url.hostname + " Async Email Sender"}
        healthCheckUrl={url.origin + AppContext.toAbsoluteUrl('/api/asyncEmailSender/healthCheck')}
        clickUrl={url.href}
      /></h2>
      <div className="btn-toolbar mt-3">
        <button className={classes("sf-button btn", s.running ? "btn-success disabled" : "btn-outline-success")} onClick={!s.running ? handleStart : undefined}><FontAwesomeIcon aria-hidden={true} icon="play" /> Start</button>
        <button className={classes("sf-button btn", !s.running ? "btn-danger disabled" : "btn-outline-danger")} onClick={s.running ? handleStop : undefined}><FontAwesomeIcon aria-hidden={true} icon="stop" /> Stop</button>
      </div >
      <div>
        <br />
        State: <strong>
          {state.running ?
            <span style={{ color: "green" }}> RUNNING </span> :
            <span style={{ color: state.initialDelayMilliseconds == null ? "gray" : "red" }}> STOPPED </span>
          }</strong>
        <a className="ms-2" href={toAbsoluteUrl("/api/asyncEmailSender/simpleStatus")} target="_blank">SimpleStatus</a>
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
        LastExecutionFinishedOn: {state.lastExecutionFinishedOn} ({state.lastExecutionFinishedOn == undefined ? "-None-" : DateTime.fromISO(state.lastExecutionFinishedOn).toRelative()})
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
      }} deps={[state.lastExecutionFinishedOn]}/>
    </div>
  );
}
