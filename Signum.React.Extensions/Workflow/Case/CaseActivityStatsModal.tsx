import * as moment from 'moment'
import numbro from 'numbro'
import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { openModal, IModalProps } from '@framework/Modals';
import { durationToString } from '@framework/Reflection';
import * as Finder from '@framework/Finder';
import * as Navigator from '@framework/Navigator';
import { JavascriptMessage } from '@framework/Signum.Entities'
import { CaseActivityStats, durationFormat } from "../WorkflowClient";
import { FormGroup, StyleContext } from "@framework/Lines";
import { CaseActivityEntity, WorkflowActivityEntity, WorkflowActivityMessage, DoneType, CaseNotificationEntity, CaseActivityMessage, WorkflowActivityType, CaseEntity } from "../Signum.Entities.Workflow";
import { EntityLink, SearchControl } from "@framework/Search";
import { OperationLogEntity } from "@framework/Signum.Entities.Basics";
import { Tab, Tabs, Modal } from 'react-bootstrap';

interface CaseActivityStatsModalProps extends IModalProps<undefined> {
  case: CaseEntity;
  caseActivityStats: CaseActivityStats[];
}

export default function CaseActivityStatsModal(p: CaseActivityStatsModalProps) {

  const [show, setShow] = React.useState<boolean>(true);

  function handleCloseClicked() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(undefined);
  }

  var caseActivityStats = p.caseActivityStats;
  return (
    <Modal size="lg" onHide={handleCloseClicked} show={show} onExited={handleOnExited}>
      <div className="modal-header">
        <h5 className="modal-title">{caseActivityStats.first().workflowActivity.toStr} ({caseActivityStats.length} {caseActivityStats.length == 1 ? CaseActivityEntity.niceName() : CaseActivityEntity.nicePluralName()})</h5>
        <button type="button" className="close" data-dismiss="modal" aria-label="Close" onClick={handleCloseClicked}>
          <span aria-hidden="true">&times;</span>
        </button>
      </div>
      <div className="modal-body">
        {
          <div>
            {caseActivityStats.length == 1 ? <CaseActivityStatsComponent stats={caseActivityStats.first()} caseEntity={p.case} /> :
              <Tabs id="statsTabs">
                {
                  caseActivityStats.map(a =>
                    <Tab key={a.caseActivity.id!.toString()} eventKey={a.caseActivity.id!}
                      title={a.doneDate == null ? CaseActivityMessage.Pending.niceToString() : <span>{a.doneBy.toStr} {DoneType.niceToString(a.doneType!)} <mark>({moment(a.doneDate).fromNow()})</mark></span> as any}>
                      <CaseActivityStatsComponent stats={a} caseEntity={p.case} />
                    </Tab>)
                }
              </Tabs>
            }
          </div>
        }
      </div>
      <div className="modal-footer">
        <button className="btn btn-primary sf-entity-button sf-ok-button" onClick={handleCloseClicked}>
          {JavascriptMessage.ok.niceToString()}
        </button>
      </div>
    </Modal>
  );
}

CaseActivityStatsModal.show = (caseEntity: CaseEntity, caseActivityStats: CaseActivityStats[]): Promise<any> => {
  return openModal<any>(<CaseActivityStatsModal case={caseEntity} caseActivityStats={caseActivityStats} />);
};

interface CaseActivityStatsComponentProps {
  caseEntity: CaseEntity;
  stats: CaseActivityStats;
}

export function CaseActivityStatsComponent(p : CaseActivityStatsComponentProps){

  function renderTaskExtra() {
    return (
      <div>
        <h3>{CaseNotificationEntity.nicePluralName()}</h3>
        <SearchControl findOptions={{ queryName: CaseNotificationEntity, parentToken: CaseNotificationEntity.token(e => e.caseActivity), parentValue: p.stats.caseActivity }} />
      </div>
    );
  }

  function renderScriptTaskExtra() {
    return (
      <div>
        <h3>{OperationLogEntity.nicePluralName()}</h3>
        <SearchControl findOptions={{ queryName: OperationLogEntity, parentToken: OperationLogEntity.token(e => e.target), parentValue: p.stats.caseActivity }} />
      </div>
    );
  }

  function handleClick(e: React.MouseEvent<HTMLButtonElement>) {
    e.preventDefault();

    Finder.find<CaseEntity>({
      queryName: CaseEntity,
      filterOptions: [{ token: CaseEntity.token().entity(e => e.parentCase), value: p.caseEntity, frozen: true }]
    }, { autoSelectIfOne: true })
      .then(c => c && Navigator.navigate(c))
      .done();
  }

  function renderSubWorkflowExtra(ctx: StyleContext) {
    return (
      <FormGroup ctx={ctx}>
        <button className="btn btn-light" onClick={handleClick}>
          <FontAwesomeIcon icon="random" color="green" /> {WorkflowActivityMessage.CaseFlow.niceToString()}
        </button>
      </FormGroup>
    );
  }

  var ctx = new StyleContext(undefined, { labelColumns: 3 });
  var stats = p.stats;

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
        stats.workflowActivityType == "Task" || stats.workflowActivityType == "Decision" ? renderTaskExtra() :
          stats.workflowActivityType == "Script" ? renderScriptTaskExtra() :
            stats.workflowActivityType == "CallWorkflow" || stats.workflowActivityType == "DecompositionWorkflow" ? renderSubWorkflowExtra(ctx) :
              undefined

      }
    </div>
  );
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

  return <span>{numbro(duration).format("0.00")} {unit} <mark>({durationFormat(moment.duration(duration, "minutes"))})</mark></span>
}
