import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Finder } from '@framework/Finder'
import { getToString, JavascriptMessage, Lite } from '@framework/Signum.Entities'
import { WorkflowEntity, WorkflowModel, WorkflowActivityMonitorMessage, CaseActivityEntity } from '../Signum.Workflow'
import { Navigator } from '@framework/Navigator'
import { WorkflowClient } from '../WorkflowClient'
import WorkflowActivityMonitorViewerComponent from '../Bpmn/WorkflowActivityMonitorViewerComponent'
import { ColumnOptionParsed, FilterOptionParsed, SubTokensOptions, QueryDescription, ColumnRequest } from '@framework/FindOptions';
import { useLocation, useParams } from "react-router";
import { newLite } from '@framework/Reflection';
import FilterBuilder from '@framework/SearchControl/FilterBuilder';
import ColumnBuilder from '@framework/SearchControl/ColumnBuilder';
import { useAPI, useAPIWithReload } from '@framework/Hooks'

export interface WorkflowActivityMonitorConfig {
  workflow: Lite<WorkflowEntity>;
  filters: FilterOptionParsed[];
  columns: ColumnOptionParsed[];
}

interface WorkflowActivityMonitorPageState {
  lastConfig: WorkflowActivityMonitorConfig;
  workflowActivityMonitor: WorkflowClient.WorkflowActivityMonitor;
}

export default function WorkflowActivityMonitorPage(): React.JSX.Element {
  const params = useParams() as { workflowId: string };

  var workflow = useAPI(() => {
    const lite = newLite(WorkflowEntity, params.workflowId);
    return Navigator.API.fillLiteModels(lite).then(() => lite);
  }, [params.workflowId]);

  const config = React.useMemo(() => workflow == null ? undefined : ({
    workflow: workflow,
    filters: [],
    columns: []
  }) as WorkflowActivityMonitorConfig, [workflow]);

  const [result, reloadResult] = useAPIWithReload<WorkflowActivityMonitorPageState | undefined>(() => {
    if (config == null)
      return Promise.resolve(undefined);

    const clone = JSON.parse(JSON.stringify(config)) as WorkflowActivityMonitorConfig;
    return WorkflowClient.API.workflowActivityMonitor(toRequest(config))
      .then(result => ({
        workflowActivityMonitor: result,
        lastConfig: clone,
      }));
  }, [config]);

  const workflowModel = useAPI(() => workflow == null ? Promise.resolve(undefined) : WorkflowClient.API.getWorkflowModel(workflow).then(wmi => wmi.model), [workflow]);

  return (
    <div>
      <h3 className="modal-title">
        {!config ? JavascriptMessage.loading.niceToString() : getToString(config.workflow)}
        {config && Navigator.isViewable(WorkflowEntity) &&
          <small>&nbsp;<a href={Navigator.navigateRoute(config.workflow)} target="blank"><FontAwesomeIcon icon="pencil" title={WorkflowActivityMonitorMessage.OpenWorkflow.niceToString()}/></a></small>}
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

function toRequest(conf: WorkflowActivityMonitorConfig): WorkflowClient.WorkflowActivityMonitorRequest {
  return {
    workflow: conf.workflow,
    filters: Finder.toFilterRequests(conf.filters),
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

export function WorkflowActivityMonitorConfigComponent(p: WorkflowActivityMonitorConfigComponentProps): React.JSX.Element | null {

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

