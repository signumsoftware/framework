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
import { ContextualItemsContext, ContextualMenuItem } from '../../Signum/React/SearchControl/ContextualItems';
import { SearchControlLoaded } from '@framework/Search';
import { Finder } from '@framework/Finder';
import { DateTime } from 'luxon';
import * as d3 from 'd3';


export namespace ProcessClient {

  export const processStateColor: Record<ProcessState, string> = {
    Created: "var(--bs-secondary)",
    Planned: "var(--bs-info)",
    Canceled: "var(--bs-secondary)",
    Queued: "var(--bs-info)",
    Executing: "var(--bs-primary)",
    Suspending: "var(--bs-warning)",
    Suspended: "var(--bs-warning)",
    Finished: "var(--bs-success)",
    Error: "var(--bs-danger)",
  };

  export function start(options: { routes: RouteObject[], packages: boolean, packageOperations: boolean }): void {

    ChangeLogClient.registerChangeLogModule("Signum.Processes", () => import("./Changelog"));

    const execEnd = (dto: ProcessDatesDTO, nowISO: string): string | null =>
      dto.executionEnd ?? dto.suspendDate ?? dto.exceptionDate ??
      ((dto.state === "Executing" || dto.state === "Suspending") ? nowISO : null);



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

    AppContext.clearSettingsActions.push(() => Dic.clear(processOperationSettings));

    Finder.formatRules.push({
      name: "ProcessDates",
      isApplicable: qt => qt.type.name === "ProcessDatesDTO",
      formatter: (column, sc) => {
        const nowISO = new Date().toISOString();

        const colIdx = sc?.state.resultTable?.columns.indexOf(column.fullKey) ?? -1;
        if (colIdx == -1)
          return new Finder.CellFormatter(() => null, true);

        const allDtos = sc!.state.resultTable!.rows.map(r => r.columns[colIdx] as ProcessDatesDTO | null).notNull();

        const scale = buildDateScale(allDtos.flatMap(dto => [
          dto.creationDate,
          dto.plannedDate,
          dto.cancelationDate,
          dto.queuedDate,
          dto.executionStart,
          execEnd(dto, nowISO),
        ]).notNull(), true);

        return new Finder.CellFormatter(cell => {
          if (!cell) return null;
          const dto = cell as ProcessDatesDTO;

          const creationPct = scale(dto.creationDate);
          const cancelPct = dto.cancelationDate ? scale(dto.cancelationDate) : null;
          const queuePct = dto.queuedDate && scale(dto.queuedDate);
          const barStartPct = dto.executionStart ? scale(dto.executionStart) : null;
          const barEndStr = execEnd(dto, nowISO);
          const barEndPct = barEndStr ? scale(barEndStr) : null;
          const color = processStateColor[dto.state];

          return (
            <OverlayTrigger placement="top" overlay={props => {
              const fmt = (d: string) => DateTime.fromISO(d).toLocaleString(DateTime.DATETIME_SHORT);
              return (
                <Tooltip id="date-gantt-tooltip" className="tooltip-process" {...props}>
                  {[
                    `${ProcessEntity.nicePropertyName(p => p.state)}: ${ProcessState.niceToString(dto.state)}`,
                    `${ProcessEntity.nicePropertyName(p => p.creationDate)}: ${fmt(dto.creationDate)}`,
                    dto.plannedDate && `${ProcessEntity.nicePropertyName(p => p.plannedDate)}: ${fmt(dto.plannedDate)}`,
                    dto.queuedDate && `${ProcessEntity.nicePropertyName(p => p.queuedDate)}: ${fmt(dto.queuedDate)}`,
                    dto.executionStart && `${ProcessEntity.nicePropertyName(p => p.executionStart)}: ${fmt(dto.executionStart)}`,
                    dto.executionEnd && `${ProcessEntity.nicePropertyName(p => p.executionEnd)}: ${fmt(dto.executionEnd)}`,
                    dto.suspendDate && `${ProcessEntity.nicePropertyName(p => p.suspendDate)}: ${fmt(dto.suspendDate)}`,
                    dto.exceptionDate && `${ProcessEntity.nicePropertyName(p => p.exceptionDate)}: ${fmt(dto.exceptionDate)}`,
                    dto.cancelationDate && `${ProcessEntity.nicePropertyName(p => p.cancelationDate)}: ${fmt(dto.cancelationDate)}`,
                  ].notNull().map((line, i) => <div key={i}>{line}</div>)}
                </Tooltip>
              );
            }}>
              <svg style={{ minWidth: "300px", width: "100%", height: "20px", overflow: "visible", display: "block" }} shapeRendering="crispEdges">
                {barEndPct !== null &&
                  <line x1={`${creationPct}%`} y1="50%" x2={`${barEndPct}%`} y2="50%" stroke="var(--bs-secondary-bg-subtle)" strokeWidth="1" />
                }
                <line x1={`${creationPct}%`} y1="15%" x2={`${creationPct}%`} y2="85%" stroke="var(--bs-secondary)" strokeWidth="1" />
                {queuePct !== null &&
                  <line x1={`${queuePct}%`} y1="15%" x2={`${queuePct}%`} y2="85%" stroke="var(--bs-info)" strokeWidth="1" />
                }
                {barStartPct !== null && barEndPct !== null &&
                  <rect x={`${barStartPct}%`} y="28%" width={`${Math.max(0.5, barEndPct - barStartPct)}%`} height="44%" fill={color} />
                }
                {cancelPct !== null &&
                  <text x={`${cancelPct}%`} y="50%" dy="2" textAnchor="middle" dominantBaseline="middle" fill="var(--bs-danger)" fontSize="12" fontWeight="bold">✕</text>
                }
              </svg>
            </OverlayTrigger>
          );
        }, true);
      },
    });

  }
  
  export const processOperationSettings: { [key: string]: ContextualOperationSettings<any> } = {};
  export function register<T extends Entity>(...settings: ContextualOperationSettings<T>[]): void {
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
          extraButtons={<span className="process-contextual-icon" onClick={processOnClick} title={ProcessMessage.ProcessSettings.niceToString()} ><FontAwesomeIcon aria-hidden={true} icon="gear"/></span>} /> } as ContextualMenuItem];  
    };
  }
  
  export async function defaultConstructProcessFromMany(coc: ContextualOperationContext<Entity>, ...args: any[]): Promise<void> {
    var event = coc.event!;

    event!.preventDefault();
    event.stopPropagation();

    const lites = coc.context.container instanceof SearchControlLoaded ?
      await coc.context.container.askAllLites(coc.context, coc.operationInfo.niceName) :
      coc.context.lites;

    if (lites == null)
      return;

    coc = ContextualOperations.cloneWithPrototype(coc, { context: { ...coc.context, lites } });

    var conf = await ContextualOperations.confirmInNecessary(coc);
    if (!conf)
      return;

    const pack = await API.processFromMany<Entity>(coc.context.lites, coc.operationInfo.key, args);
    if (!pack || !pack.entity)
      return;

    const es = Navigator.getSettings(pack.entity.Type);
    if (es?.avoidPopup || event.ctrlKey || event.button == 1) {
      AppContext.navigate('/create/', { state: pack });
      return;
    }
    else {
      await Navigator.view(pack);
    }
  }

  export namespace API {
  
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
      return ajaxGet({ url: "/api/processes/view", avoidNotifyPendingRequests: true });
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

export interface ProcessDatesDTO {
  creationDate: string /*DateTime*/;
  plannedDate: string /*DateTime*/ | null;
  cancelationDate: string /*DateTime*/ | null;
  queuedDate: string /*DateTime*/ | null;
  executionStart: string /*DateTime*/ | null;
  executionEnd: string /*DateTime*/ | null;
  suspendDate: string /*DateTime*/ | null;
  exceptionDate: string /*DateTime*/ | null;
  state: ProcessState;
}

export function buildDateScale(isoStrings: string[], max24Hours?: boolean): (d: string) => number {
  const dates = isoStrings.map(d => new Date(d));
  const minDate = d3.min(dates) ?? new Date();
  const maxDate = new Date(Math.max((d3.max(dates) ?? new Date()).getTime(), minDate.getTime() + 1));

  if (max24Hours && maxDate.getTime() - minDate.getTime() > 24 * 60 * 60 * 1000) {
    const toTimeOfDay = (d: Date) => new Date(1970, 0, 1, d.getHours(), d.getMinutes(), d.getSeconds(), d.getMilliseconds());
    const scale = d3.scaleTime<number>().domain([new Date(1970, 0, 1, 0, 0, 0), new Date(1970, 0, 1, 23, 59, 0)]).range([0, 100]).clamp(true);
    return d => scale(toTimeOfDay(new Date(d)));
  }

  const scale = d3.scaleTime<number>().domain([minDate, maxDate]).range([0, 100]).clamp(true);
  return d => scale(new Date(d));
}
