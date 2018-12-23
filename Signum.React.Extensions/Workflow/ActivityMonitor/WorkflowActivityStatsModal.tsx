import * as moment from 'moment'
import * as numbro from 'numbro'
import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { openModal, IModalProps } from '@framework/Modals';
import * as Navigator from '@framework/Navigator';
import { JavascriptMessage, toLite } from '@framework/Signum.Entities'
import { WorkflowActivityStats } from "../WorkflowClient";
import { FormGroup, StyleContext, FormControlReadonly } from "@framework/Lines";
import { WorkflowActivityEntity, WorkflowActivityModel, WorkflowActivityMonitorMessage, CaseActivityEntity } from "../Signum.Entities.Workflow";
import { SearchControl, ColumnOption } from "@framework/Search";
import * as WorkflowClient from '../WorkflowClient';
import { WorkflowActivityMonitorConfig } from './WorkflowActivityMonitorPage';
import { Modal } from '@framework/Components';
import { ModalHeaderButtons } from '@framework/Components/Modal';
import { toFilterOptions, isAggregate } from '@framework/Finder';

interface WorkflowActivityStatsModalProps extends React.Props<WorkflowActivityStatsModal>, IModalProps {
  stats: WorkflowActivityStats;
  config: WorkflowActivityMonitorConfig;
  activity: WorkflowActivityModel;
}

export default class WorkflowActivityStatsModal extends React.Component<WorkflowActivityStatsModalProps, { show: boolean }>  {

  constructor(props: WorkflowActivityStatsModalProps) {
    super(props);

    this.state = {
      show: true,
    };
  }

  handleCloseClicked = () => {
    this.setState({ show: false });
  }

  handleOnExited = () => {
    this.props.onExited!(undefined);
  }

  render() {
    var ctx = new StyleContext(undefined, { labelColumns: 3 });
    var activity = this.props.activity;
    var config = this.props.config;
    var stats = this.props.stats;
    return <Modal size="lg" onHide={this.handleCloseClicked} show={this.state.show} onExited={this.handleOnExited}>
      <ModalHeaderButtons onClose={this.handleCloseClicked}>
        {stats.workflowActivity.toStr}
      </ModalHeaderButtons>
      <div className="modal-body">
        {
          <div>
            <FormGroup ctx={ctx} labelText={CaseActivityEntity.nicePluralName()}><FormControlReadonly ctx={ctx}>{stats.caseActivityCount}</FormControlReadonly></FormGroup>
            {config.columns.map((col, i) =>
              <FormGroup ctx={ctx} labelText={col.displayName || col.token!.niceName}><FormControlReadonly ctx={ctx}>{stats.customValues[i]}</FormControlReadonly></FormGroup>
            )}
            {activity.type == "CallWorkflow" || activity.type == "DecompositionWorkflow" ?
              this.renderSubWorkflowExtra(ctx) :
              this.renderTaskExtra()
            }
          </div>
        }
      </div>
      <div className="modal-footer">
        <button className="btn btn-primary sf-entity-button sf-ok-button" onClick={this.handleCloseClicked}>
          {JavascriptMessage.ok.niceToString()}
        </button>
      </div>
    </Modal>;
  }

  renderTaskExtra() {
    var stats = this.props.stats;

    return (
      <div>
        <h3>{CaseActivityEntity.nicePluralName()}</h3>
        <SearchControl
          showGroupButton={true}
          findOptions={{
            queryName: CaseActivityEntity,
            parentToken: CaseActivityEntity.token().entity(e => e.workflowActivity),
            parentValue: stats.workflowActivity,
            filterOptions: toFilterOptions(this.props.config.filters.filter(f => !isAggregate(f))),
            columnOptionsMode: "Add",
            columnOptions: this.props.config.columns
              .filter(c => c.token && c.token.fullKey.contains("."))
              .map(c => ({ token: c.token!.fullKey.beforeLast(".") }) as ColumnOption),
          }} />
      </div>
    );
  }

  renderSubWorkflowExtra(ctx: StyleContext) {
    var stats = this.props.stats;

    return (
      <FormGroup ctx={ctx}>
        <button className="btn btn-default" onClick={this.handleClick}>
          <FontAwesomeIcon icon="tachometer-alt" color="green" /> {WorkflowActivityMonitorMessage.WorkflowActivityMonitor.niceToString()}
        </button>
      </FormGroup>
    );
  }

  handleClick = (e: React.MouseEvent<HTMLButtonElement>) => {
    e.preventDefault();

    Navigator.API.fetchAndForget(this.props.stats.workflowActivity)
      .then(wa => window.open(WorkflowClient.workflowActivityMonitorUrl(toLite(wa.subWorkflow!.workflow!))))
      .done();
  }

  static show(stats: WorkflowActivityStats, config: WorkflowActivityMonitorConfig, activity: WorkflowActivityModel): Promise<any> {
    return openModal<any>(<WorkflowActivityStatsModal stats={stats} config={config} activity={activity} />);
  }
}

function formatDuration(duration: number | undefined, unit: string | undefined) {
  if (duration == undefined)
    return undefined;

  if (unit == "min")


    var unit = WorkflowActivityEntity.memberInfo(a => a.estimatedDuration).unit;

  return <span>{numbro(duration).format("0.00")} {unit} <mark>({moment.duration(duration, "minutes").format("d[d] h[h] m[m] s[s]")})</mark></span>
}




