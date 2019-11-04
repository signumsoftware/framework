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
import { useAPI, useAPIWithReload } from '../../../../Framework/Signum.React/Scripts/Hooks'

export interface WorkflowActivityMonitorConfig {
  workflow: Lite<WorkflowEntity>;
  filters: FilterOptionParsed[];
  columns: ColumnOptionParsed[];
}

interface WorkflowActivityMonitorPageState {
  lastConfig: WorkflowActivityMonitorConfig;
  workflowActivityMonitor: WorkflowActivityMonitor;
}

export default function WorkflowActivityMonitorPage(p: RouteComponentProps<{ workflowId: string }>) {

  var workflow = useAPI(() => {
    const lite = newLite(WorkflowEntity, p.match.params.workflowId);
    return Navigator.API.fillToStrings(lite).then(() => lite);
  }, [p.match.params.workflowId]);

  const config = React.useMemo(() => workflow == null ? undefined : ({
    workflow: workflow,
    filters: [],
    columns: []
  }) as WorkflowActivityMonitorConfig, [workflow]);

  const [result, reloadResult] = useAPIWithReload<WorkflowActivityMonitorPageState | undefined>(() => {
    if (config == null)
      return Promise.resolve(undefined);

    const clone = JSON.parse(JSON.stringify(config)) as WorkflowActivityMonitorConfig;
    return API.workflowActivityMonitor(toRequest(config))
      .then(result => ({
        workflowActivityMonitor: result,
        lastConfig: clone,
      }));
  }, [config]);

  const workflowModel = useAPI(() => workflow == null ? Promise.resolve(undefined) : API.getWorkflowModel(workflow).then(wmi => wmi.model), [workflow]);

  return (
    <div>
      <h3 className="modal-title">
        {!config ? JavascriptMessage.loading.niceToString() : config.workflow.toStr}
        {config && Navigator.isViewable(WorkflowEntity) &&
          <small>&nbsp;<a href={Navigator.navigateRoute(config.workflow)} target="blank"><FontAwesomeIcon icon="pencil" /></a></small>}
        <br />
        <small>{WorkflowActivityMonitorMessage.WorkflowActivityMonitor.niceToString()}</small>
      </h3>
      {config && <WorkflowActivityMonitorConfigComponent config={config} />}

      {!workflowModel || !result ?
        <h3>{JavascriptMessage.loading.niceToString()}</h3> :
        <div className="code-container">
          <WorkflowActivityMonitorViewerComponent
            onDraw={reloadResult}
            workflowModel={workflowModel}
            workflowActivityMonitor={result.workflowActivityMonitor}
            workflowConfig={result.lastConfig} />
        </div>
      }
    </div>
  );
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

export function WorkflowActivityMonitorConfigComponent(p: WorkflowActivityMonitorConfigComponentProps) {

  const qd = useAPI(() => Finder.getQueryDescription(CaseActivityEntity), []);

  const filterOpts = SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement;
  const columnOpts = SubTokensOptions.CanAggregate | SubTokensOptions.CanElement;

  return (qd == null ? null :
    <div>
      <FilterBuilder title={WorkflowActivityMonitorMessage.Filters.niceToString()}
        queryDescription={qd} subTokensOptions={filterOpts}
        filterOptions={p.config.filters} />
      <ColumnBuilder title={WorkflowActivityMonitorMessage.Columns.niceToString()}
        queryDescription={qd} subTokensOptions={columnOpts}
        columnOptions={p.config.columns} />
    </div>
  );
}

