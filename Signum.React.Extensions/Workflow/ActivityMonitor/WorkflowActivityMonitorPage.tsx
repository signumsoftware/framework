import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as Finder from '@framework/Finder'
import { JavascriptMessage, Lite } from '@framework/Signum.Entities'
import { WorkflowEntity, WorkflowModel, WorkflowActivityMonitorMessage, CaseActivityEntity } from '../Signum.Entities.Workflow'
import * as Navigator from '@framework/Navigator'
import { API, WorkflowActivityMonitor, WorkflowActivityMonitorRequest } from '../WorkflowClient'
import WorkflowActivityMonitorViewerComponent from '../Bpmn/WorkflowActivityMonitorViewerComponent'
import { ColumnOptionParsed, FilterOptionParsed, SubTokensOptions, QueryDescription, ColumnRequest } from '@framework/FindOptions';
import { RouteComponentProps } from "react-router";
import { newLite } from '@framework/Reflection';
import FilterBuilder from '@framework/SearchControl/FilterBuilder';
import ColumnBuilder from '@framework/SearchControl/ColumnBuilder';
import { toFilterRequests } from '@framework/Finder';

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

  workflowActvityMonitorViewerComponent?: WorkflowActivityMonitorViewerComponent | null;

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
            lastConfig: clone,
          })).done();
      })
      .done();

    API.getWorkflowModel(workflow)
      .then(pair => this.setState({
        workflowModel: pair.model,
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
            <small>&nbsp;<a href={Navigator.navigateRoute(this.state.config.workflow)} target="blank"><FontAwesomeIcon icon="pencil" /></a></small>}
          <br />
          <small>{WorkflowActivityMonitorMessage.WorkflowActivityMonitor.niceToString()}</small>
        </h3>
        {this.state.config && <WorkflowActivityMonitorConfigComponent config={this.state.config} />}

        {!this.state.workflowModel || !this.state.workflowActivityMonitor || !this.state.lastConfig ?
          <h3>{JavascriptMessage.loading.niceToString()}</h3> :
          <div className="code-container">
            <WorkflowActivityMonitorViewerComponent ref={m => this.workflowActvityMonitorViewerComponent = m}
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
    filters: toFilterRequests(conf.filters),
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

