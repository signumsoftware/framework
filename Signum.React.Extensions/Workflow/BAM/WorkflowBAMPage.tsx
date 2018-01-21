import * as React from 'react'
import { Tabs, Tab, Modal } from "react-bootstrap";
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getMixin, toLite, JavascriptMessage, is, Lite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { WorkflowEntity, WorkflowEntitiesDictionary, WorkflowActivityMessage, WorkflowActivityEntity, WorkflowOperation, WorkflowModel, WorkflowBAMMessage, CaseActivityEntity } from '../Signum.Entities.Workflow'
import {
    ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip,
    EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, EntityTable
} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { API, WorkflowBAM, WorkflowBAMRequest } from '../WorkflowClient'
import BAMViewerComponent from '../Bpmn/BAMViewerComponent'
import { SearchControl } from "../../../../Framework/Signum.React/Scripts/Search";
import { ColumnOptionParsed, FilterOptionParsed } from '../../../../Framework/Signum.React/Scripts/FindOptions';
import { RouteComponentProps } from "react-router";
import { newLite } from '../../../../Framework/Signum.React/Scripts/Reflection';
import * as WorkflowClient from '../WorkflowClient';
import { FilterRequest } from '../../../../Framework/Signum.React/Scripts/FindOptions';
import { ColumnRequest } from '../../../../Framework/Signum.React/Scripts/FindOptions';
import FilterBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/FilterBuilder';
import { SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions';
import { QueryDescription } from '../../../../Framework/Signum.React/Scripts/FindOptions';

export interface WorkflowBAMConfig {
    workflow: Lite<WorkflowEntity>;
    filters: FilterOptionParsed[];
    columns: ColumnOptionParsed[];
}

interface WorkflowBAMPageProps extends RouteComponentProps<{ workflowId: string }> {
}

interface WorkflowBAMPageState {
    config?: WorkflowBAMConfig;
    lastConfig?: WorkflowBAMConfig | undefined;
    workflowModel?: WorkflowModel;
    workflowBAM?: WorkflowBAM;
}

export default class WorkflowBAMPage extends React.Component<WorkflowBAMPageProps, WorkflowBAMPageState> {

    constructor(props: WorkflowBAMPageProps) {
        super(props);

        this.state = {};
    }

    BAMViewerComponent?: BAMViewerComponent | null;

    loadState(props: WorkflowBAMPageProps) {
        var workflow = newLite(WorkflowEntity, props.match.params.workflowId);
        Navigator.API.fillToStrings(workflow)
            .then(() => {
                var config: WorkflowBAMConfig = {
                    workflow: workflow,
                    filters: [],
                    columns: []
                };

                this.setState({ config });

                var clone = JSON.parse(JSON.stringify(config));
                
                API.workflowBAM(toRequest(config))
                    .then(result => this.setState({
                        workflowBAM: result,
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
        API.workflowBAM(toRequest(this.state.config!))
            .then(result => this.setState({
                workflowBAM: result,
                lastConfig: clone,
            })).done();
    }

    componentWillReceiveProps(newProps: WorkflowBAMPageProps) {
        if (this.props.match.params.workflowId != newProps.match.params.workflowId)
            this.loadState(newProps);
    }

    componentWillMount() {
        this.loadState(this.props);
    }

    render() {
        return (
            <div>
                <h4 className="modal-title">
                    {WorkflowBAMMessage.BusinessActivityMonitor.niceToString()}
                    {this.state.config && <br/>}
                    {this.state.config && <small>{this.state.config.workflow.toStr}</small>}
                </h4>
                {this.state.config && <WorkflowBAMConfigComponent config={this.state.config} />}

                {!this.state.workflowModel || !this.state.workflowBAM || !this.state.lastConfig ?
                    <h3>{JavascriptMessage.loading.niceToString()}</h3> :
                    <div className="code-container">
                        <BAMViewerComponent ref={m => this.BAMViewerComponent = m}
                            onDraw={this.handleDraw}
                            workflowModel={this.state.workflowModel}
                            workflowBAM={this.state.workflowBAM}
                            workflowConfig={this.state.lastConfig} />
                    </div>
                }
            </div>
        );
    }
}

function toRequest(conf: WorkflowBAMConfig): WorkflowBAMRequest {
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


interface WorkflowBAMConfigComponentProps {
    config: WorkflowBAMConfig;
}

interface WorkflowBAMConfigComponentState {
    queryDescription?: QueryDescription;
}

export class WorkflowBAMConfigComponent extends React.Component<WorkflowBAMConfigComponentProps, WorkflowBAMConfigComponentState> {

    constructor(props: WorkflowBAMConfigComponentProps) {
        super(props);
        this.state = {};
    }

    componentWillMount() {
        this.loadData(this.props);
    }  

    loadData(props: WorkflowBAMConfigComponentProps) {
        Finder.getQueryDescription(CaseActivityEntity)
            .then(qd => this.setState({ queryDescription: qd }))
            .done();
    }

    render() {
        const options = SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement;
        const qd = this.state.queryDescription;

        return (qd == null ? null :
            <div>
                <FilterBuilder queryDescription={qd} subTokensOptions={options}
                    filterOptions={this.props.config.filters} />
            </div>
        );
    }
    
}

