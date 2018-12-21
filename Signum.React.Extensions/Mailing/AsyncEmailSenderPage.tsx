import * as React from 'react'
import * as moment from 'moment'
import { RouteComponentProps } from 'react-router'
import * as Navigator from '@framework/Navigator'
import { SearchControl } from '@framework/Search'
import { API, AsyncEmailSenderState } from './MailingClient'
import { EmailMessageEntity } from './Signum.Entities.Mailing'

interface AsyncEmailSenderPageProps extends RouteComponentProps<{}> {

}

export default class AsyncEmailSenderPage extends React.Component<AsyncEmailSenderPageProps, AsyncEmailSenderState> {
  componentWillMount() {
    this.loadState().done();
    Navigator.setTitle("AsyncEmailSender state");
  }

  componentWillUnmount() {
    Navigator.setTitle();
  }

  loadState() {
    return API.view()
      .then(s => this.setState(s));
  }

  handleStop = (e: React.MouseEvent<any>) => {
    e.preventDefault();
    API.stop().then(() => this.loadState()).done();
  }

  handleStart = (e: React.MouseEvent<any>) => {
    e.preventDefault();
    API.start().then(() => this.loadState()).done();
  }


  render() {

    if (this.state == undefined)
      return <h2>AsyncEmailSender state (loading...) </h2>;

    const s = this.state;

    return (
      <div>
        <h2>AsyncEmailSender State</h2>
        <div className="btn-toolbar">
          {s.running && <a href="#" className="sf-button btn btn-light active" style={{ color: "red" }} onClick={this.handleStop}>Stop</a>}
          {!s.running && <a href="#" className="sf-button btn btn-light" style={{ color: "green" }} onClick={this.handleStart}>Start</a>}
        </div >

        <div>
          <br />
          State: <strong>
            {s.running ?
              <span style={{ color: "Green" }}> RUNNING </span> :
              <span style={{ color: "Red" }}> STOPPED </span>
            }</strong>
          <br />
          CurrentProcessIdentifier: {s.currentProcessIdentifier}
          <br />
          AsyncSenderPeriod: {s.asyncSenderPeriod} sec
                    <br />
          NextPlannedExecution: {s.nextPlannedExecution} ({s.nextPlannedExecution == undefined ? "-None-" : moment(s.nextPlannedExecution).fromNow()})
                    <br />
          IsCancelationRequested: {s.isCancelationRequested}
          <br />
          QueuedItems: {s.queuedItems}
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
}
