
import * as React from 'react'
import { RouteObject } from 'react-router'
import { OverlayTrigger, Tooltip, Dropdown } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic, classes } from '@framework/Globals';
import { ajaxPost, ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import { Lite, Entity, EntityPack, OperationMessage } from '@framework/Signum.Entities'
import { Operations, ContextualOperationContext, EntityOperationSettings, ContextualOperationSettings } from '@framework/Operations'
import { GraphExplorer, OperationType } from '@framework/Reflection'
import { ContextualOperations } from '@framework/Operations/ContextualOperations'
import { ProcessState, ProcessEntity, ProcessPermission, PackageLineEntity, PackageEntity, PackageOperationEntity, ProcessOperation, ProcessMessage } from './Signum.Processes'
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import { ImportComponent } from '@framework/ImportComponent'
import "./Processes.css"
import { ConstructSymbol_From, DeleteSymbol, ExecuteSymbol } from '@framework/Signum.Operations';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';
import { ContextualMenuItem } from '../../Signum/React/SearchControl/ContextualItems';

export namespace ProcessClient {
  
  export function start(options: { routes: RouteObject[], packages: boolean, packageOperations: boolean }) {
  
    ChangeLogClient.registerChangeLogModule("Signum.Processes", () => import("./Changelog"));
  
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
  
    options.routes.push({ path: "/processes/view", element: <ImportComponent onImport={() => import("./ProcessPanelPage")} /> });
  
    OmniboxSpecialAction.registerSpecialAction({
      allowed: () => AppContext.isPermissionAuthorized(ProcessPermission.ViewProcessPanel),
      key: "ProcessPanel",
      onClick: () => Promise.resolve("/processes/view")
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
  
  export const processOperationSettings: { [key: string]: ContextualOperationSettings<any> } = {};
  export function register<T extends Entity>(...settings: ContextualOperationSettings<T>[]) {
    settings.forEach(s => Dic.addOrThrow(processOperationSettings, s.operationSymbol, s));
  }
  
  function monkeyPatchCreateContextualMenuItem() {
  
    const base = ContextualOperationContext.prototype.createMenuItems;
  
    ContextualOperationContext.prototype.createMenuItems = function (this: ContextualOperationContext<any>): ContextualMenuItem[] {
  
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

      return [{
        fullText: this.operationInfo.niceName, menu: <ContextualOperations.OperationMenuItem coc={this}
          extraButtons={<span className="process-contextual-icon" onClick={processOnClick} ><FontAwesomeIcon icon="gear" title={ProcessMessage.ProcessSettings.niceToString()} /></span>} /> } as ContextualMenuItem];  
    };
  }
  
  function defaultConstructProcessFromMany(coc: ContextualOperationContext<Entity>, ...args: any[]) {
    var event = coc.event!;
  
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
          AppContext.navigate('/create/', { state: pack });
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
      return ajaxPost({ url: "/api/processes/constructFromMany/" + Operations.API.getOperationKey(operationKey) }, { lites: lites, args: args } as Operations.API.MultiOperationRequest);
    }
  
    export function start(): Promise<void> {
      return ajaxPost({ url: "/api/processes/start" }, undefined);
    }
  
    export function stop(): Promise<void> {
      return ajaxPost({ url: "/api/processes/stop" }, undefined);
    }
  
    export function view(): Promise<ProcessLogicState> {
      return ajaxGet({ url: "/api/processes/view" });
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
    log: string;
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
}
