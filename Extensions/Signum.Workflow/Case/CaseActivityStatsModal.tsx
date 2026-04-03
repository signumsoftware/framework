import { DateTime, Duration } from 'luxon'
import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { openModal, IModalProps } from '@framework/Modals';
import { timeToString, toNumberFormat } from '@framework/Reflection';
import { Finder } from '@framework/Finder';
import { Navigator } from '@framework/Navigator';
import { getToString, JavascriptMessage } from '@framework/Signum.Entities'
import { WorkflowClient } from "../WorkflowClient";
import { FormGroup, StyleContext } from "@framework/Lines";
import { CaseActivityEntity, WorkflowActivityEntity, WorkflowActivityMessage, DoneType, CaseNotificationEntity, CaseActivityMessage, WorkflowActivityType, CaseEntity, WorkflowEventType } from "../Signum.Workflow";
import { EntityLink, SearchControl } from "@framework/Search";
import { Tab, Tabs, Modal } from 'react-bootstrap';
import { OperationLogEntity } from '@framework/Signum.Operations';

interface CaseActivityStatsModalProps extends IModalProps<undefined> {
  case: CaseEntity;
  caseActivityStats: WorkflowClient.CaseActivityStats[];
}

function CaseActivityStatsModal(p: CaseActivityStatsModalProps): React.JSX.Element {

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
        <h5 className="modal-title">{getToString(caseActivityStats.first().workflowActivity)} ({caseActivityStats.length} {caseActivityStats.length == 1 ? CaseActivityEntity.niceName() : CaseActivityEntity.nicePluralName()})</h5>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCloseClicked}/>
      </div>
      <div className="modal-body">
        {
          <div>
            {caseActivityStats.length == 1 ? <CaseActivityStatsComponent stats={caseActivityStats.first()} caseEntity={p.case} /> :
              <Tabs id="statsTabs">
                {
                  caseActivityStats.map(a =>
                    <Tab key={a.caseActivity.id!.toString()} eventKey={a.caseActivity.id!.toString()}
                      title={a.doneDate == null ? CaseActivityMessage.Pending.niceToString() : <span>{getToString(a.doneBy)} {DoneType.niceToString(a.doneType!)} <mark>({DateTime.fromISO(a.doneDate).toRelative()})</mark></span> as any}>
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

namespace CaseActivityStatsModal{
  export function show(caseEntity: CaseEntity, caseActivityStats: WorkflowClient.CaseActivityStats[]): Promise<any> {
    return openModal<any>(<CaseActivityStatsModal case={caseEntity} caseActivityStats={caseActivityStats} />);
  };
}

export default CaseActivityStatsModal;

interface CaseActivityStatsComponentProps {
  caseEntity: CaseEntity;
  stats: WorkflowClient.CaseActivityStats;
}

export function CaseActivityStatsComponent(p : CaseActivityStatsComponentProps): React.JSX.Element {

  function renderTaskExtra() {
    return (
      <div>
        <h3>{CaseNotificationEntity.nicePluralName()}</h3>
        <SearchControl findOptions={{ queryName: CaseNotificationEntity, filterOptions: [{ token: CaseNotificationEntity.token(e => e.caseActivity), value: p.stats.caseActivity }]}} />
      </div>
    );
  }

  function renderScriptTaskExtra() {
    return (
      <div>
        <h3>{OperationLogEntity.nicePluralName()}</h3>
        <SearchControl findOptions={{ queryName: OperationLogEntity, filterOptions: [{ token: OperationLogEntity.token(e => e.target), value: p.stats.caseActivity }]}} />
      </div>
    );
  }

  function handleClick(e: React.MouseEvent<HTMLButtonElement>) {

    Finder.find<CaseEntity>({
      queryName: CaseEntity,
      filterOptions: [
        { token: CaseEntity.token(e => e.entity.parentCase), value: p.caseEntity, frozen: true },
        { token: CaseEntity.token(e => e.entity).expression<CaseActivityEntity>("DecompositionSurrogateActivity"), value: p.stats.caseActivity },
      ]
    }, { autoSelectIfOne: true })
      .then(c => c && Navigator.view(c));
  }

  function renderSubWorkflowExtra(ctx: StyleContext) {
    return (
      <FormGroup ctx={ctx} >
        {() => <button className="btn btn-light" onClick={handleClick}>
          <FontAwesomeIcon icon="shuffle" color="green" /> {WorkflowActivityMessage.CaseFlow.niceToString()}
        </button>}      
      </FormGroup>
    );
  }

  var ctx = new StyleContext(undefined, { labelColumns: 3 });
  var stats = p.stats;

  return (
    <div>
      <FormGroup ctx={ctx} label={CaseActivityEntity.niceName()}>
        {() => getToString(stats.caseActivity)}
      </FormGroup>
      <FormGroup ctx={ctx} label={CaseActivityEntity.nicePropertyName(a => a.doneBy)}>
        {() => stats.doneBy && <EntityLink lite={stats.doneBy} />}
      </FormGroup>
      <FormGroup ctx={ctx} label={CaseActivityEntity.nicePropertyName(a => a.startDate)}>
        {() => formatDate(stats.startDate)}
      </FormGroup>
      <FormGroup ctx={ctx} label={CaseActivityEntity.nicePropertyName(a => a.doneDate)}>
        {() => formatDate(stats.doneDate)}
      </FormGroup>
      <FormGroup ctx={ctx} label={CaseActivityEntity.nicePropertyName(a => a.doneType)}>
        {() => stats.doneType && DoneType.niceToString(stats.doneType)}
      </FormGroup>
      <FormGroup ctx={ctx} label={WorkflowActivityEntity.nicePropertyName(a => a.estimatedDuration)}>
        {() => formatMinutes(stats.estimatedDuration)}
      </FormGroup>
      <FormGroup ctx={ctx} label={WorkflowActivityMessage.AverageDuration.niceToString()}>
        {() => formatMinutes(stats.averageDuration)}
      </FormGroup>
      <FormGroup ctx={ctx} label={CaseActivityEntity.nicePropertyName(a => a.duration)}>
        {() => formatMinutes(stats.duration)}
      </FormGroup>
      {stats.workflowActivityType && <FormGroup ctx={ctx} label={WorkflowActivityType.niceTypeName()}>
          {() => stats.workflowActivityType && WorkflowActivityType.niceToString(stats.workflowActivityType)}
        </ FormGroup>
      }
      {stats.workflowEventType && <FormGroup ctx={ctx} label={WorkflowEventType.niceTypeName()}>
        {() => stats.workflowEventType && WorkflowEventType.niceToString(stats.workflowEventType)}
        </FormGroup>
      }
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

  return <span>{DateTime.fromISO(date).toFormat("FFF")} <mark>({DateTime.fromISO(date).toRelative()})</mark></span>
}

function formatMinutes(duration: number | undefined) {
  if (duration == undefined)
    return undefined;

  var unit = CaseActivityEntity.memberInfo(a => a.duration).unit;

  var formatNumber = toNumberFormat("0.00");

  return <span>{formatNumber.format(duration)} {unit} <mark>({WorkflowClient.formatDuration(Duration.fromObject({ minute: duration }))})</mark></span>
}
