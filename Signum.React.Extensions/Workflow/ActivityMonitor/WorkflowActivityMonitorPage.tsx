import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getMixin, toLite, JavascriptMessage, is, Lite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { WorkflowEntity, WorkflowEntitiesDictionary, WorkflowActivityMessage, WorkflowActivityEntity, WorkflowOperation, WorkflowModel, WorkflowActivityMonitorMessage, CaseActivityEntity } from '../Signum.Entities.Workflow'
import {
    ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip,
    EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, EntityTable
} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { API, WorkflowActivityMonitor, WorkflowActivityMonitorRequest } from '../WorkflowClient'
import WorkflowActivityMonitorViewerComponent from '../Bpmn/WorkflowActivityMonitorViewerComponent'
import { SearchControl } from "../../../../Framework/Signum.React/Scripts/Search";
import { ColumnOptionParsed, FilterOptionParsed, SubTokensOptions, QueryDescription, FilterRequest, ColumnRequest } from '../../../../Framework/Signum.React/Scripts/FindOptions';
import { RouteComponentProps } from "react-router";
import { newLite } from '../../../../Framework/Signum.React/Scripts/Reflection';
import * as WorkflowClient from '../WorkflowClient';
import FilterBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/FilterBuilder';
import ColumnBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/ColumnBuilder';

export interface WorkflowActivityMonitorConfig {
    workflow: Lite<WorkflowEntity>;
    filters: FilterOptionParsed[];
    columns: ColumnOptionParsed[];
}

interface WorkflowActivityMonitorPageProps extends RouteComponentProps<{ workflowId: string }> {
}

interface WorkflowActivityMonitorPageState {
    config?: WorkflowActivityMonitorConfig;
    lastConfig?: WorkflowActivityMonitorConfig | undefined;
    workflowModel?: WorkflowModel;
    workflowActivityMonitor?: WorkflowActivityMonitor;
}

export default class WorkflowActivityMonitorPage extends React.Component<WorkflowActivityMonitorPageProps, WorkflowActivityMonitorPageState> {

    constructor(props: WorkflowActivityMonitorPageProps) {
        super(props);

        this.state = {};
    }

    WorkflowActvityMonitorViewerComponent?: WorkflowActivityMonitorViewerComponent | null;

    loadState(props: WorkflowActivityMonitorPageProps) {
        var workflow = newLite(WorkflowEntity, props.match.params.workflowId);
        Navigator.API.fillToStrings(workflow)
            .then(() => {
                var config: WorkflowActivityMonitorConfig = {
                    workflow: workflow,
                    filters: [],
                    columns: []
                };

                this.setState({ config });

                var clone = JSON.parse(JSON.stringify(config));
                
                API.workflowActivityMonitor(toRequest(config))
                    .then(result => this.setState({
                        workflowActivityMonitor: result,
                        lastConfig: clone ,
                    })).done();
            })
            .done();

        API.getWorkflowModel(workflow)
            .then(model => this.setState({
                workflowModel: model,
            }))
            .done();
    }

    handleDraw = () => {
        var clone = JSON.parse(JSON.stringify(this.state.config));
        API.workflowActivityMonitor(toRequest(this.state.config!))
            .then(result => this.setState({
                workflowActivityMonitor: result,
                lastConfig: clone,
            })).done();
    }

    componentWillReceiveProps(newProps: WorkflowActivityMonitorPageProps) {
        if (this.props.match.params.workflowId != newProps.match.params.workflowId)
            this.loadState(newProps);
    }

    componentWillMount() {
        this.loadState(this.props);
    }

    render() {
        return (
            <div>
                <h3 className="modal-title">
                    {!this.state.config ? JavascriptMessage.loading.niceToString() : this.state.config.workflow.toStr}
                    {this.state.config && Navigator.isViewable(WorkflowEntity) &&
                        <small>&nbsp;<a href={Navigator.navigateRoute(this.state.config.workflow)} target="blank"><i className="fa fa-pencil" aria-hidden="true"></i></a></small>}
                    <br />
                    <small>{WorkflowActivityMonitorMessage.WorkflowActivityMonitor.niceToString()}</small>
                </h3>
                {this.state.config && <WorkflowActivityMonitorConfigComponent config={this.state.config} />}

                {!this.state.workflowModel || !this.state.workflowActivityMonitor || !this.state.lastConfig ?
                    <h3>{JavascriptMessage.loading.niceToString()}</h3> :
                    <div className="code-container">
                        <WorkflowActivityMonitorViewerComponent ref={m => this.WorkflowActvityMonitorViewerComponent = m}
                            onDraw={this.handleDraw}
                            workflowModel={this.state.workflowModel}
                            workflowActivityMonitor={this.state.workflowActivityMonitor}
                            workflowConfig={this.state.lastConfig} />
                    </div>
                }
            </div>
        );
    }
}

function toRequest(conf: WorkflowActivityMonitorConfig): WorkflowActivityMonitorRequest {
    return {
        workflow: conf.workflow,
        filters: conf.filters.filter(f => f.token != null && f.operation != undefined).map(f => ({
            token: f.token!.fullKey,
            operation: f.operation,
            value: f.value,
        }) as FilterRequest),
        columns: conf.columns.filter(c => c.token != null).map(c => ({
            token: c.token!.fullKey,
            displayName: c.token!.niceName
        }) as ColumnRequest)
    };
}


interface WorkflowActivityMonitorConfigComponentProps {
    config: WorkflowActivityMonitorConfig;
}

interface WorkflowActivityMonitorConfigComponentState {
    queryDescription?: QueryDescription;
}

export class WorkflowActivityMonitorConfigComponent extends React.Component<WorkflowActivityMonitorConfigComponentProps, WorkflowActivityMonitorConfigComponentState> {

    constructor(props: WorkflowActivityMonitorConfigComponentProps) {
        super(props);
        this.state = {};
    }

    componentWillMount() {
        this.loadData(this.props);
    }  

    loadData(props: WorkflowActivityMonitorConfigComponentProps) {
        Finder.getQueryDescription(CaseActivityEntity)
            .then(qd => this.setState({ queryDescription: qd }))
            .done();
    }

    render() {
        const filterOpts = SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement;
        const columnOpts = SubTokensOptions.CanAggregate | SubTokensOptions.CanElement;
        const qd = this.state.queryDescription;

        return (qd == null ? null :
            <div>
                <FilterBuilder title={WorkflowActivityMonitorMessage.Filters.niceToString()}
                    queryDescription={qd} subTokensOptions={filterOpts}
                    filterOptions={this.props.config.filters} />
                <ColumnBuilder title={WorkflowActivityMonitorMessage.Columns.niceToString()}
                    queryDescription={qd} subTokensOptions={columnOpts}
                    columnOptions={this.props.config.columns} />
            </div>
        );
    }
    
}

