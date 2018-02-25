import * as React from 'react'
import * as moment from 'moment'
import { RouteComponentProps } from 'react-router'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import EntityLink from '../../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import {ValueSearchControl, SearchControl, OrderType } from '../../../../Framework/Signum.React/Scripts/Search'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import {
    ModifiableEntity, EntityControlMessage, Entity,
    parseLite, getToString, JavascriptMessage
} from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import {
   OperationLogEntity
} from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { API, WorkflowScriptRunnerState } from '../WorkflowClient'
import { CaseActivityEntity, WorkflowActivityType, DoneType, WorkflowScriptRunnerPanelPermission, CaseActivityOperation } from '../Signum.Entities.Workflow'
import * as AuthClient from '../../Authorization/AuthClient'
import { UncontrolledTabs, Tab } from '../../../../Framework/Signum.React/Scripts/Components/Tabs';


interface WorkflowScriptRunnerPanelPageProps extends RouteComponentProps<{}> {

}

export default class WorkflowScriptRunnerPanelPage extends React.Component<WorkflowScriptRunnerPanelPageProps, WorkflowScriptRunnerState> {

    componentWillMount() {
        this.loadState().done();
        AuthClient.asserPermissionAuthorized(WorkflowScriptRunnerPanelPermission.ViewWorkflowScriptRunnerPanel);
        Navigator.setTitle("WorkflowScriptRunner State");
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

        var title = "WorkflowScriptRunner State";

        if (this.state == undefined)
            return <h2>{title} (loading...) </h2>;

        const s = this.state;

        return (
            <div>
                <h2>{title}</h2>
                <div className="btn-toolbar">
                    {s.Running && <a href="#" className="sf-button btn btn-light active" style={{ color: "red" }} onClick={this.handleStop}>Stop</a> }
                    {!s.Running && <a href="#" className="sf-button btn btn-light" style={{ color: "green" }} onClick={this.handleStart}>Start</a> }
                </div >

                <div>
                    <br />
                        State: <strong>
                            {s.Running ?
                                <span style={{ color: "Green" }}> RUNNING </span> :
                                <span style={{ color: "Red" }}> STOPPED </span>
                            }</strong>
                    <br />
                    CurrentProcessIdentifier: { s.CurrentProcessIdentifier }
                    <br />
                    ScriptRunnerPeriod: {s.ScriptRunnerPeriod} sec
                    <br />
                    NextPlannedExecution: {s.NextPlannedExecution} ({s.NextPlannedExecution == undefined ? "-None-" : moment(s.NextPlannedExecution).fromNow()})
                    <br />
                    IsCancelationRequested: { s.IsCancelationRequested }
                    <br />
                    QueuedItems: { s.QueuedItems }
                </div>
                <br />
                <h3>Next activities to execute</h3>
                <SearchControl findOptions={{
                    queryName: CaseActivityEntity,
                    filterOptions: [
                        { columnName: "Entity.WorkflowActivity.Type", operation: "EqualTo", value: WorkflowActivityType.value("Script") },
                        { columnName: "Entity.DoneDate", operation: "EqualTo", value: null }
                    ],
                    columnOptionsMode: "Replace",
                    columnOptions: [
                        { columnName: "Id" },
                        { columnName: "StartDate" },
                        { columnName: "WorkflowActivity.Lane.Pool.Workflow" },
                        { columnName: "WorkflowActivity" },
                        { columnName: "Case" },
                        { columnName: "Entity.ScriptExecution.NextExecution" },
                        { columnName: "Entity.ScriptExecution.RetryCount" },
                    ],
                    orderOptions: [
                        { columnName: "Entity.ScriptExecution.NextExecution", orderType: "Ascending" }
                    ],
                    pagination: { elementsPerPage: 10, mode: "Firsts" }
                }} />
                <UncontrolledTabs>
                    <Tab title="Last operation logs" eventKey="logs">
                        <SearchControl findOptions={{
                            queryName: OperationLogEntity,
                            filterOptions: [
                                {
                                    columnName: "Operation", operation: "IsIn", value: [
                                        CaseActivityOperation.ScriptExecute,
                                        CaseActivityOperation.ScriptScheduleRetry,
                                        CaseActivityOperation.ScriptFailureJump,
                                    ]
                                },
                            ],
                            pagination: { elementsPerPage: 10, mode: "Firsts" }
                        }} />
                    </Tab>
                    <Tab title="Last executed activities" eventKey="lastActivities">
                        <SearchControl findOptions={{
                            queryName: CaseActivityEntity,
                            filterOptions: [
                                { columnName: "Entity.WorkflowActivity.Type", operation: "EqualTo", value: WorkflowActivityType.value("Script") },
                                { columnName: "Entity.DoneDate", operation: "DistinctTo", value: null }
                            ],
                            columnOptionsMode: "Replace",
                            columnOptions: [
                                { columnName: "Id" },
                                { columnName: "StartDate" },
                                { columnName: "WorkflowActivity.Lane.Pool.Workflow" },
                                { columnName: "WorkflowActivity" },
                                { columnName: "Case" },
                                { columnName: "Entity.DoneDate" },
                                { columnName: "Entity.DoneType" },
                                { columnName: "Entity.ScriptExecution.NextExecution" },
                                { columnName: "Entity.ScriptExecution.RetryCount" },
                            ],
                            orderOptions: [
                                { columnName: "Entity.DoneDate", orderType: "Descending" }
                            ],
                            pagination: { elementsPerPage: 10, mode: "Firsts" }
                        }} />
                    </Tab>
                </UncontrolledTabs>
           </div>
        );
    }
}



