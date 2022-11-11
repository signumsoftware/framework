
import * as React from 'react'
import { OverlayTrigger, Tooltip, Dropdown } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic, classes } from '@framework/Globals';
import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, OperationMessage } from '@framework/Signum.Entities'
import { ContextualOperationContext, EntityOperationSettings } from '@framework/Operations'
import { GraphExplorer, OperationType } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import * as ContextualOperations from '@framework/Operations/ContextualOperations'
import { ProcessState, ProcessEntity, ProcessPermission, PackageLineEntity, PackageEntity, PackageOperationEntity, ProcessOperation, ProcessMessage } from './Signum.Entities.Processes'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import { ImportRoute } from "@framework/AsyncImport";
import "./Processes.css"

export function start(options: { routes: JSX.Element[], packages: boolean, packageOperations: boolean }) {
  Navigator.addSettings(new EntitySettings(ProcessEntity, e => import('./Templates/Process'), { isCreable: "Never" }));

  if (options.packages || options.packageOperations) {
    Navigator.addSettings(new EntitySettings(PackageLineEntity, e => import('./Templates/PackageLine')));
  }

  if (options.packages) {
    Navigator.addSettings(new EntitySettings(PackageEntity, e => import('./Templates/Package'), { isCreable: "Never" }));
  }

  if (options.packageOperations) {
    Navigator.addSettings(new EntitySettings(PackageOperationEntity, e => import('./Templates/PackageOperation'), { isCreable: "Never" }));
  }

  options.routes.push(<ImportRoute path="~/processes/view" onImportModule={() => import("./ProcessPanelPage")} />);

  OmniboxClient.registerSpecialAction({
    allowed: () => AuthClient.isPermissionAuthorized(ProcessPermission.ViewProcessPanel),
    key: "ProcessPanel",
    onClick: () => Promise.resolve("~/processes/view")
  });

  monkeyPatchCreateContextualMenuItem();

  Operations.addSettings(new EntityOperationSettings(ProcessOperation.Cancel, {
    confirmMessage: ctx => ctx.entity.state == "Executing" || ctx.entity.state == "Suspending" ? ProcessMessage.SuspendIsTheSaferWayOfStoppingARunningProcessCancelAnyway.niceToString() : undefined,
    color: "danger",
    icon: "stop",
  }));

  Operations.addSettings(new EntityOperationSettings(ProcessOperation.Execute, {
    color: "success",
    icon: "play",
  }));

  Operations.addSettings(new EntityOperationSettings(ProcessOperation.Plan, {
    icon: "calendar",
  }));

  Operations.addSettings(new EntityOperationSettings(ProcessOperation.Suspend, {
    icon: "pause",
    color: "warning",
  }));

  Operations.addSettings(new EntityOperationSettings(ProcessOperation.Retry, {
    group: null,
    icon: "clone",
    color: "info",
  }));
}

export const processOperationSettings: { [key: string]: Operations.ContextualOperationSettings<any> } = {};
export function register<T extends Entity>(...settings: Operations.ContextualOperationSettings<T>[]) {
  settings.forEach(s => Dic.addOrThrow(processOperationSettings, s.operationSymbol, s));
}

function monkeyPatchCreateContextualMenuItem() {

  const base = ContextualOperationContext.prototype.createMenuItems;

  ContextualOperationContext.prototype.createMenuItems = function(this: Operations.ContextualOperationContext<any>): React.ReactElement[] {

    if (this.settings?.createMenuItems)
      return this.settings.createMenuItems(this);

    if (!Navigator.isViewable(PackageOperationEntity))
      return base.call(this);

    if (this.operationInfo.operationType == "Constructor" ||
      this.operationInfo.operationType == "ConstructorFromMany")
      return base.call(this);

    if (this.context.lites.length <= 1)
      return base.call(this);

    const processSettings = processOperationSettings[this.operationInfo.key];

    if (processSettings != undefined) {
      if (processSettings.isVisible && !processSettings.isVisible(this))
        return base.call(this);

      if (processSettings.hideOnCanExecute && this.canExecute != undefined)
        return base.call(this);
    }

    const processOnClick = (me: React.MouseEvent<any>) => {
      this.event = me;
      processSettings?.onClick ? processSettings.onClick!(this) : defaultConstructProcessFromMany(this)
    }

    return [
      <ContextualOperations.OperationMenuItem coc={this}
        extraButtons={<span className="process-contextual-icon" onClick={processOnClick}><FontAwesomeIcon icon="gear" /></span>} />
    ];
  };
}

function defaultConstructProcessFromMany(coc: Operations.ContextualOperationContext<Entity>, ...args: any[]) {
  var event = coc.event!;

  event.persist();
  event!.preventDefault();
  event.stopPropagation();

  ContextualOperations.confirmInNecessary(coc).then(conf => {
    if (!conf)
      return;

    API.processFromMany<Entity>(coc.context.lites, coc.operationInfo.key).then(pack => {

      if (!pack || !pack.entity)
        return;

      const es = Navigator.getSettings(pack.entity.Type);
      if (es?.avoidPopup || event.ctrlKey || event.button == 1) {
        AppContext.history.push('~/create/', pack);
        return;
      }
      else {
        Navigator.view(pack);
      }
    });
  });
}

export module API {

  export function processFromMany<T extends Entity>(lites: Lite<T>[], operationKey: string | ExecuteSymbol<T> | DeleteSymbol<T> | ConstructSymbol_From<any, T>, args?: any[]): Promise<EntityPack<ProcessEntity>> {
    GraphExplorer.propagateAll(lites, args);
    return ajaxPost({ url: "~/api/processes/constructFromMany" }, { lites: lites, operationKey: Operations.API.getOperationKey(operationKey), args: args } as Operations.API.MultiOperationRequest);
  }

  export function start(): Promise<void> {
    return ajaxPost({ url: "~/api/processes/start" }, undefined);
  }

  export function stop(): Promise<void> {
    return ajaxPost({ url: "~/api/processes/stop" }, undefined);
  }

  export function view(): Promise<ProcessLogicState> {
    return ajaxGet({ url: "~/api/processes/view" });
  }
}


export interface ProcessLogicState {
  maxDegreeOfParallelism: number;
  initialDelayMilliseconds: number | null;
  running: boolean;
  machineName: string;
  applicationName: string;
  justMyProcesses: boolean;
  nextPlannedExecution: string;
  executing: ExecutionState[];
}

export interface ExecutionState {
  process: Lite<ProcessEntity>;
  state: ProcessState;
  isCancellationRequested: boolean;
  progress: number;
  machineName: string;
  applicationName: string;
}
