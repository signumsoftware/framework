
import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { Button, OverlayTrigger, Tooltip, MenuItem } from "react-bootstrap"
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

require("./Processes.css");

export function start(options: { routes: JSX.Element[], packages: boolean, packageOperations: boolean }) {
  

    Navigator.addSettings(new EntitySettings(ProcessEntity, e => _import('./Templates/Process'), { isCreable : "Never" }));

    if (options.packages || options.packageOperations) {
        Navigator.addSettings(new EntitySettings(PackageLineEntity, e => _import('./Templates/PackageLine')));
    }

    if (options.packages) {
        Navigator.addSettings(new EntitySettings(PackageEntity, e => _import('./Templates/Package'), { isCreable: "Never" }));
    }

    if (options.packageOperations) {
        Navigator.addSettings(new EntitySettings(PackageOperationEntity, e => _import('./Templates/PackageOperation'), { isCreable: "Never" }));
    }

    options.routes.push(<ImportRoute path="~/processes/view" onImportModule={() => _import("./ProcessPanelPage")} />);
    
    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(ProcessPermission.ViewProcessPanel),
        key: "ProcessPanel",
        onClick: () => Promise.resolve(Navigator.currentHistory.createHref("~/processes/view"))
    });

    monkeyPatchCreateContextualMenuItem()

}

export const processOperationSettings :{ [key: string]: Operations.ContextualOperationSettings<any> } = {}; 
export function register<T extends Entity>(...settings : Operations.ContextualOperationSettings<T>[]){
    settings.forEach(s => Dic.addOrThrow(processOperationSettings, s.operationSymbol.key, s));
}

function monkeyPatchCreateContextualMenuItem(){

    const base = ContextualOperations.MenuItemConstructor.createContextualMenuItem;

    ContextualOperations.MenuItemConstructor.createContextualMenuItem = (coc: Operations.ContextualOperationContext<Entity>, defaultClick: (coc: Operations.ContextualOperationContext<Entity>) => void, key: any) => {
        
        if(!Navigator.isViewable(PackageOperationEntity) )
            return base(coc, defaultClick, key);

        if(coc.operationInfo.operationType == OperationType.Constructor ||
            coc.operationInfo.operationType == OperationType.ConstructorFromMany)
            return base(coc, defaultClick, key);

        if(coc.context.lites.length <= 1)
            return base(coc, defaultClick, key);

        const settings = processOperationSettings[coc.operationInfo.key];

        if(settings != undefined){
            if(settings.isVisible && !settings.isVisible(coc))
                return base(coc, defaultClick, key);

            if(settings.hideOnCanExecute && coc.canExecute != undefined)
                return base(coc, defaultClick, key);
        }


         const text = coc.settings && coc.settings.text ? coc.settings.text() :
        coc.entityOperationSettings && coc.entityOperationSettings.text ? coc.entityOperationSettings.text() :
            coc.operationInfo.niceName;

        const bsStyle = coc.settings && coc.settings.style || Operations.autoStyleFunction(coc.operationInfo);

        const disabled = !!coc.canExecute;

        const onClick = coc.settings && coc.settings.onClick ?
            (me: React.MouseEvent<any>) => coc.settings!.onClick!(coc) :
            (me: React.MouseEvent<any>) => defaultClick(coc);

        const menuItem = <MenuItem
            className={disabled ? "disabled" : undefined}
            onClick={disabled ? undefined : onClick}
            data-operation={coc.operationInfo.key}
            key={key}>
            {bsStyle && <span className={"icon empty-icon btn-" + bsStyle}></span>}
            {text}
            <span className="glyphicon glyphicon-cog process-contextual-icon" aria-hidden={true} onClick={me =>defaultConstructFromMany(coc, me)}></span>
            </MenuItem>;

        if (!coc.canExecute)
            return menuItem;

        const tooltip = <Tooltip id={"tooltip_" + coc.operationInfo.key.replace(".", "_") }>{coc.canExecute}</Tooltip>;

        return <OverlayTrigger placement="right" overlay={tooltip} >{menuItem}</OverlayTrigger>;


    };
}

function defaultConstructFromMany(coc: Operations.ContextualOperationContext<Entity>, event: React.MouseEvent<any>) {

    event.preventDefault();
    event.stopPropagation();

    if (!ContextualOperations.confirmInNecessary(coc))
        return;

    API.processFromMany<Entity>(coc.context.lites, coc.operationInfo.key).then(pack => {

        if (!pack || !pack.entity)
            return;

        const es = Navigator.getSettings(pack.entity.Type);
        if (es && es.avoidPopup || event.ctrlKey || event.button == 1) {
            Navigator.currentHistory.push('~/Create/', pack);
            return;
        }
        else {
            Navigator.navigate(pack);
        }
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