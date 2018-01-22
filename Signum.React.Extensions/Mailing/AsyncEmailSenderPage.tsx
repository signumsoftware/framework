import * as React from 'react'
import * as moment from 'moment'
import { RouteComponentProps } from 'react-router'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import EntityLink from '../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import {ValueSearchControl, SearchControl, OrderType } from '../../../Framework/Signum.React/Scripts/Search'
import { QueryDescription, SubTokensOptions } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
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
                    {s.Running && <a href="#" className="sf-button btn btn-light active" style={{ color: "red" }} onClick={this.handleStop}>Stop</a>}
                    {!s.Running && <a href="#" className="sf-button btn btn-light" style={{ color: "green" }} onClick={this.handleStart}>Start</a>}
                </div >

                <div>
                    <br />
                    State: <strong>
                        {s.Running ?
                            <span style={{ color: "Green" }}> RUNNING </span> :
                            <span style={{ color: "Red" }}> STOPPED </span>
                        }</strong>
                    <br />
                    CurrentProcessIdentifier: {s.CurrentProcessIdentifier}
                    <br />
                    AsyncSenderPeriod: {s.AsyncSenderPeriod} sec
                    <br />
                    NextPlannedExecution: {s.NextPlannedExecution} ({s.NextPlannedExecution == undefined ? "-None-" : moment(s.NextPlannedExecution).fromNow()})
                    <br />
                    IsCancelationRequested: {s.IsCancelationRequested}
                    <br />
                    QueuedItems: {s.QueuedItems}
                </div>
                <br />
                <h2>{EmailMessageEntity.niceName()}</h2>
                <SearchControl findOptions={{
                    queryName: EmailMessageEntity,
                    orderOptions: [{ columnName: "Entity.CreationDate", orderType: "Descending" }],
                    pagination: { elementsPerPage: 10, mode: "Firsts" }
                }} />
            </div>
        );
    }
}



