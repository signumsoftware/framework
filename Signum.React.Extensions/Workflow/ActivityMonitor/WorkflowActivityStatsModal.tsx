import { DateTime, Duration } from 'luxon'
import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { openModal, IModalProps } from '@framework/Modals';
import * as Navigator from '@framework/Navigator';
import { getToString, JavascriptMessage, toLite } from '@framework/Signum.Entities'
import { WorkflowActivityStats } from "../WorkflowClient";
import { FormGroup, StyleContext, FormControlReadonly } from "@framework/Lines";
import { WorkflowActivityEntity, WorkflowActivityModel, WorkflowActivityMonitorMessage, CaseActivityEntity } from "../Signum.Entities.Workflow";
import { SearchControl, ColumnOption } from "@framework/Search";
import * as WorkflowClient from '../WorkflowClient';
import { WorkflowActivityMonitorConfig } from './WorkflowActivityMonitorPage';
import { Modal } from 'react-bootstrap';
import { ModalHeaderButtons } from '@framework/Components/ModalHeaderButtons';
import { toFilterOptions, isAggregate } from '@framework/Finder';

interface WorkflowActivityStatsModalProps extends IModalProps<undefined> {
  stats: WorkflowActivityStats;
  config: WorkflowActivityMonitorConfig;
  activity: WorkflowActivityModel;
}

export default function WorkflowActivityStatsModal(p: WorkflowActivityStatsModalProps) {

const [show, setShow] = React.useState<boolean>(true);

  function handleCloseClicked() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(undefined);
  }


  function renderTaskExtra() {
    var stats = p.stats;

    return (
      <div>
        <h3>{CaseActivityEntity.nicePluralName()}</h3>
        <SearchControl
          showGroupButton={true}
          findOptions={{
            queryName: CaseActivityEntity,
            filterOptions: [
              { token: CaseActivityEntity.token(e => e.entity.workflowActivity), value: stats.workflowActivity },
              ...toFilterOptions(p.config.filters.filter(f => !isAggregate(f)))
            ],
            columnOptionsMode: "Add",
            columnOptions: p.config.columns
              .filter(c => c.token && c.token.fullKey.contains("."))
              .map(c => ({ token: c.token!.fullKey.beforeLast(".") }) as ColumnOption),
          }} />
      </div>
    );
  }

  function renderSubWorkflowExtra(ctx: StyleContext) {
    var stats = p.stats;

    return (
      <FormGroup ctx={ctx}>
        <button className="btn btn-default" onClick={handleClick}>
          <FontAwesomeIcon icon="gauge" color="green" /> {WorkflowActivityMonitorMessage.WorkflowActivityMonitor.niceToString()}
        </button>
      </FormGroup>
    );
  }

  function handleClick(e: React.MouseEvent<HTMLButtonElement>) {
    e.preventDefault();

    Navigator.API.fetch(p.stats.workflowActivity)
      .then(wa => window.open(WorkflowClient.workflowActivityMonitorUrl(toLite(wa.subWorkflow!.workflow!))));
  }

  var ctx = new StyleContext(undefined, { labelColumns: 3 });
  var activity = p.activity;
  var config = p.config;
  var stats = p.stats;
  return <Modal size="lg" onHide={handleCloseClicked} show={show} onExited={handleOnExited}>
    <ModalHeaderButtons onClose={handleCloseClicked}>
      {getToString(stats.workflowActivity)}
    </ModalHeaderButtons>
    <div className="modal-body">
      {
        <div>
          <FormGroup ctx={ctx} label={CaseActivityEntity.nicePluralName()}><FormControlReadonly ctx={ctx}>{stats.caseActivityCount}</FormControlReadonly></FormGroup>
          {config.columns.map((col, i) =>
            <FormGroup ctx={ctx} label={col.displayName || col.token!.niceName}><FormControlReadonly ctx={ctx}>{stats.customValues[i]}</FormControlReadonly></FormGroup>
          )}
          {activity.type == "CallWorkflow" || activity.type == "DecompositionWorkflow" ?
            renderSubWorkflowExtra(ctx) :
            renderTaskExtra()
          }
        </div>
      }
    </div>
    <div className="modal-footer">
      <button className="btn btn-primary sf-entity-button sf-ok-button" onClick={handleCloseClicked}>
        {JavascriptMessage.ok.niceToString()}
      </button>
    </div>
  </Modal>;
}

WorkflowActivityStatsModal.show = (stats: WorkflowActivityStats, config: WorkflowActivityMonitorConfig, activity: WorkflowActivityModel): Promise<any> => {
  return openModal<any>(<WorkflowActivityStatsModal stats={stats} config={config} activity={activity} />);
}
