
import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'

import { Route } from 'react-router'
import { Dic, classes } from '@framework/Globals';
import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From } from '@framework/Signum.Entities'
import { EntityOperationSettings } from '@framework/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType  } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import * as ContextualOperations from '@framework/Operations/ContextualOperations'
import { ProcessState, ProcessEntity, ProcessPermission, PackageLineEntity, PackageEntity, PackageOperationEntity, ProcessOperation, ProcessMessage } from './Signum.Entities.Processes'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import { ImportRoute } from "@framework/AsyncImport";

import "./Processes.css"
import { DropdownItem, UncontrolledTooltip } from '@framework/Components';

export function start(options: { routes: JSX.Element[], packages: boolean, packageOperations: boolean }) {

    Navigator.addSettings(new EntitySettings(ProcessEntity, e => import('./Templates/Process'), { isCreable : "Never" }));

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
        color: "warning"
    }));
}

export const processOperationSettings :{ [key: string]: Operations.ContextualOperationSettings<any> } = {}; 
export function register<T extends Entity>(...settings : Operations.ContextualOperationSettings<T>[]){
    settings.forEach(s => Dic.addOrThrow(processOperationSettings, s.operationSymbol.key, s));
}

function monkeyPatchCreateContextualMenuItem(){

    const base = ContextualOperations.MenuItemConstructor.createContextualMenuItem;

    ContextualOperations.MenuItemConstructor.createContextualMenuItem = (coc: Operations.ContextualOperationContext<Entity>, defaultClick: (coc: Operations.ContextualOperationContext<Entity>) => void) => {
        
        if(!Navigator.isViewable(PackageOperationEntity) )
            return base(coc, defaultClick);

        if(coc.operationInfo.operationType == OperationType.Constructor ||
            coc.operationInfo.operationType == OperationType.ConstructorFromMany)
            return base(coc, defaultClick);

        if(coc.context.lites.length <= 1)
            return base(coc, defaultClick);

        const processSettings = processOperationSettings[coc.operationInfo.key];
        if(processSettings != undefined){
            if(processSettings.isVisible && !processSettings.isVisible(coc))
                return base(coc, defaultClick);

            if(processSettings.hideOnCanExecute && coc.canExecute != undefined)
                return base(coc, defaultClick);
        }


        const text = coc.settings && coc.settings.text ? coc.settings.text() :
        coc.entityOperationSettings && coc.entityOperationSettings.text ? coc.entityOperationSettings.text() :
            coc.operationInfo.niceName;

        const color = coc.settings && coc.settings.color || coc.entityOperationSettings && coc.entityOperationSettings.color || Operations.autoColorFunction(coc.operationInfo);
        const icon = coc.settings && coc.settings.icon;

        const disabled = !!coc.canExecute;

        const onClick = (me: React.MouseEvent<any>) => {
            coc.event = me;
            coc.settings && coc.settings.onClick ? coc.settings!.onClick!(coc) : defaultClick(coc)
        }

        const processOnClick = (me: React.MouseEvent<any>) => {
            coc.event = me;
            processSettings && processSettings.onClick ? processSettings.onClick!(coc) : defaultConstructProcessFromMany(coc)
        }


        let innerRef: HTMLElement | null;

        return [
            <DropdownItem
                innerRef={r => innerRef = r}
                className={disabled ? "disabled" : undefined}
                onClick={disabled ? undefined : onClick}
                data-operation={coc.operationInfo.key}>
                {icon ? <FontAwesomeIcon icon={icon} className="icon" color={coc.settings && coc.settings.iconColor}/> :
                    color ? <span className={classes("icon", "empty-icon", "btn-" + color)}></span> : undefined}
                {(icon || color) && " "}
                {text}
                <span className="process-contextual-icon" onClick={processOnClick}><FontAwesomeIcon icon="cog"/></span>

            </DropdownItem>,
            coc.canExecute ? <UncontrolledTooltip target={() => innerRef!} placement="right">{coc.canExecute}</UncontrolledTooltip> : undefined
        ].filter(a => a != null);
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
            if (es && es.avoidPopup || event.ctrlKey || event.button == 1) {
                Navigator.history.push('~/create/', pack);
                return;
            }
            else {
                Navigator.navigate(pack);
            }
        }).done();
    }).done();
}

export module API {

    export function processFromMany<T extends Entity>(lites: Lite<T>[], operationKey: string | ExecuteSymbol<T> | DeleteSymbol<T> | ConstructSymbol_From<any, T>, args?: any[]): Promise<EntityPack<ProcessEntity>> {
        GraphExplorer.propagateAll(lites, args);
        return ajaxPost<EntityPack<ProcessEntity>>({ url: "~/api/processes/constructFromMany" }, { lites: lites, operationKey: Operations.API.getOperationKey(operationKey), args: args } as Operations.API.MultiOperationRequest);
    }

    export function start(): Promise<void> {
        return ajaxPost<void>({ url: "~/api/processes/start" }, undefined);
    }

    export function stop(): Promise<void> {
        return ajaxPost<void>({ url: "~/api/processes/stop" }, undefined);
    }

    export function view(): Promise<ProcessLogicState> {
        return ajaxGet<ProcessLogicState>({ url: "~/api/processes/view" });
    }
}


export interface ProcessLogicState {
    MaxDegreeOfParallelism: number;
    InitialDelayMiliseconds: number;
    Running: boolean;
    MachineName: string;
    JustMyProcesses: boolean;
    NextPlannedExecution: string;
    Executing: ExecutionState[];
}

export interface ExecutionState {
    Process: Lite<ProcessEntity>;
    State: ProcessState;
    IsCancellationRequested: boolean;
    Progress: number;
    MachineName: string;
    ApplicationName: string;
}