import * as moment from 'moment'
import * as numbro from 'numbro'
import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { openModal, IModalProps } from '@framework/Modals';
import * as Finder from '@framework/Finder';
import * as Navigator from '@framework/Navigator';
import { JavascriptMessage } from '@framework/Signum.Entities'
import { CaseActivityStats } from "../WorkflowClient";
import { FormGroup, StyleContext } from "@framework/Lines";
import { CaseActivityEntity, WorkflowActivityEntity, WorkflowActivityMessage, DoneType, CaseNotificationEntity, CaseActivityMessage, WorkflowActivityType, CaseEntity } from "../Signum.Entities.Workflow";
import { EntityLink, SearchControl } from "@framework/Search";
import { OperationLogEntity } from "@framework/Signum.Entities.Basics";
import { Tab, UncontrolledTabs } from '@framework/Components/Tabs';
import { Modal } from '@framework/Components';

interface CaseActivityStatsModalProps extends React.Props<CaseActivityStatsModal>, IModalProps {
  case: CaseEntity;
  caseActivityStats: CaseActivityStats[];
}

export default class CaseActivityStatsModal extends React.Component<CaseActivityStatsModalProps, { show: boolean }>  {
  constructor(props: CaseActivityStatsModalProps) {
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
    var caseActivityStats = this.props.caseActivityStats;
    return (
      <Modal size="lg" onHide={this.handleCloseClicked} show={this.state.show} onExited={this.handleOnExited}>
        <div className="modal-header">
          <h5 className="modal-title">{caseActivityStats.first().workflowActivity.toStr} ({caseActivityStats.length} {caseActivityStats.length == 1 ? CaseActivityEntity.niceName() : CaseActivityEntity.nicePluralName()})</h5>
          <button type="button" className="close" data-dismiss="modal" aria-label="Close" onClick={this.handleCloseClicked}>
            <span aria-hidden="true">&times;</span>
          </button>
        </div>
        <div className="modal-body">
          {
            <div>
              {caseActivityStats.length == 1 ? <CaseActivityStatsComponent stats={caseActivityStats.first()} caseEntity={this.props.case} /> :
                <UncontrolledTabs id="statsTabs">
                  {
                    caseActivityStats.map(a =>
                      <Tab key={a.caseActivity.id!.toString()} eventKey={a.caseActivity.id!}
                        title={a.doneDate == null ? CaseActivityMessage.Pending.niceToString() : <span>{a.doneBy.toStr} {DoneType.niceToString(a.doneType!)} <mark>({moment(a.doneDate).fromNow()})</mark></span> as any}>
                        <CaseActivityStatsComponent stats={a} caseEntity={this.props.case} />
                      </Tab>)
                  }
                </UncontrolledTabs>
              }
            </div>
          }
        </div>
        <div className="modal-footer">
          <button className="btn btn-primary sf-entity-button sf-ok-button" onClick={this.handleCloseClicked}>
            {JavascriptMessage.ok.niceToString()}
          </button>
        </div>
      </Modal>
    );
  }

  static show(caseEntity: CaseEntity, caseActivityStats: CaseActivityStats[]): Promise<any> {
    return openModal<any>(<CaseActivityStatsModal case={caseEntity} caseActivityStats={caseActivityStats} />);
  }
}

interface CaseActivityStatsComponentProps {
  caseEntity: CaseEntity;
  stats: CaseActivityStats;
}

export class CaseActivityStatsComponent extends React.Component<CaseActivityStatsComponentProps> {
  render() {

    var ctx = new StyleContext(undefined, { labelColumns: 3 });
    var stats = this.props.stats;

    return (
      <div>
        <FormGroup ctx={ctx} labelText={CaseActivityEntity.niceName()}>{stats.caseActivity.toStr}</FormGroup>
        <FormGroup ctx={ctx} labelText={CaseActivityEntity.nicePropertyName(a => a.doneBy)}>{stats.doneBy && <EntityLink lite={stats.doneBy} />}</FormGroup>
        <FormGroup ctx={ctx} labelText={CaseActivityEntity.nicePropertyName(a => a.startDate)}>{formatDate(stats.startDate)}</FormGroup>
        <FormGroup ctx={ctx} labelText={CaseActivityEntity.nicePropertyName(a => a.doneDate)}>{formatDate(stats.doneDate)}</FormGroup>
        <FormGroup ctx={ctx} labelText={CaseActivityEntity.nicePropertyName(a => a.doneType)}>{stats.doneType && DoneType.niceToString(stats.doneType)}</FormGroup>
        <FormGroup ctx={ctx} labelText={WorkflowActivityEntity.nicePropertyName(a => a.estimatedDuration)}>{formatDuration(stats.estimatedDuration)}</FormGroup>
        <FormGroup ctx={ctx} labelText={WorkflowActivityMessage.AverageDuration.niceToString()}>{formatDuration(stats.averageDuration)}</FormGroup>
        <FormGroup ctx={ctx} labelText={CaseActivityEntity.nicePropertyName(a => a.duration)}>{formatDuration(stats.duration)}</FormGroup>
        <FormGroup ctx={ctx} labelText={WorkflowActivityType.niceTypeName()}>{WorkflowActivityType.niceToString(stats.workflowActivityType)}</FormGroup>
        {
          stats.workflowActivityType == "Task" || stats.workflowActivityType == "Decision" ? this.renderTaskExtra() :
            stats.workflowActivityType == "Script" ? this.renderScriptTaskExtra() :
              stats.workflowActivityType == "CallWorkflow" || stats.workflowActivityType == "DecompositionWorkflow" ? this.renderSubWorkflowExtra(ctx) :
                undefined

        }
      </div>
    );
  }

  renderTaskExtra() {
    var stats = this.props.stats;

    return (
      <div>
        <h3>{CaseNotificationEntity.nicePluralName()}</h3>
        <SearchControl findOptions={{ queryName: CaseNotificationEntity, parentToken: CaseNotificationEntity.token(e => e.caseActivity), parentValue: stats.caseActivity }} />
      </div>
    );
  }

  renderScriptTaskExtra() {
    var stats = this.props.stats;

    return (
      <div>
        <h3>{OperationLogEntity.nicePluralName()}</h3>
        <SearchControl findOptions={{ queryName: OperationLogEntity, parentToken: OperationLogEntity.token(e => e.target), parentValue: stats.caseActivity }} />
      </div>
    );
  }

  handleClick = (e: React.MouseEvent<HTMLButtonElement>) => {
    e.preventDefault();

    Finder.find<CaseEntity>({
      queryName: CaseEntity,
      filterOptions: [{ token: CaseEntity.token().entity(e => e.parentCase), value: this.props.caseEntity, frozen: true }]
    }, { autoSelectIfOne: true })
      .then(c => c && Navigator.navigate(c))
      .done();
  }

  renderSubWorkflowExtra(ctx: StyleContext) {
    var stats = this.props.stats;

    return (
      <FormGroup ctx={ctx}>
        <button className="btn btn-light" onClick={this.handleClick}>
          <FontAwesomeIcon icon="random" color="green" /> {WorkflowActivityMessage.CaseFlow.niceToString()}
        </button>
      </FormGroup>
    );
  }
}

function formatDate(date: string | undefined) {
  if (date == undefined)
    return undefined;

  return <span>{moment(date).format("L LT")} <mark>({moment(date).fromNow()})</mark></span>
}

function formatDuration(duration: number | undefined) {
  if (duration == undefined)
    return undefined;

  var unit = CaseActivityEntity.memberInfo(a => a.duration).unit;

  return <span>{numbro(duration).format("0.00")} {unit} <mark>({moment.duration(duration, "minutes").format("d[d] h[h] m[m] s[s]")})</mark></span>
}
