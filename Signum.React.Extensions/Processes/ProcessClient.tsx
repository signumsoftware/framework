
import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType  } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as ContextualOperations from '../../../Framework/Signum.React/Scripts/Operations/ContextualOperations'
import { ProcessState, ProcessEntity, ProcessPermission, PackageLineEntity, PackageEntity, PackageOperationEntity } from './Signum.Entities.Processes'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import { ImportRoute } from "../../../Framework/Signum.React/Scripts/AsyncImport";

import "./Processes.css"
import { DropdownItem, UncontrolledTooltip } from '../../../Framework/Signum.React/Scripts/Components';

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

    monkeyPatchCreateContextualMenuItem()

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

        const bsColor = coc.settings && coc.settings.color || Operations.autoColorFunction(coc.operationInfo);

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
                {bsColor && <span className={"icon empty-icon btn-" + bsColor}></span>}
                {text}
                <span className="fa fa-cog process-contextual-icon" aria-hidden={true} onClick={processOnClick}></span>

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