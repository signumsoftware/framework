
import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { Button, OverlayTrigger, Tooltip, MenuItem } from "react-bootstrap"
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType  } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as ContextualOperations from '../../../Framework/Signum.React/Scripts/Operations/ContextualOperations'
import { ProfilerPermission } from './Signum.Entities.Profiler'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'

export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<Route path="profiler">
        <Route path="times" getComponent={(loc, cb) => require(["./Times/TimesPage"], (Comp) => cb(null, Comp.default))}/>
        <Route path="heavyProfiler" getComponent={(loc, cb) => require(["./Heavy/HeavyProfilerPage"], (Comp) => cb(null, Comp.default)) }/>
        <Route path="overrideSessionTimeout" getComponent={(loc, cb) => require(["./SessionTimeout/SessionTimeoutPage"], (Comp) => cb(null, Comp.default)) }/>
    </Route>);

    //Navigator.addSettings(new EntitySettings(ProcessEntity, e => new Promise(resolve => require(['./Templates/Process'], resolve))));

    //if (options.packages || options.packageOperations) {
    //    Navigator.addSettings(new EntitySettings(PackageLineEntity, e => new Promise(resolve => require(['./Templates/PackageLine'], resolve))));
    //}

    //if (options.packages) {
    //    Navigator.addSettings(new EntitySettings(PackageEntity, e => new Promise(resolve => require(['./Templates/Package'], resolve))));
    //}

    //if (options.packageOperations) {
    //    Navigator.addSettings(new EntitySettings(PackageOperationEntity, e => new Promise(resolve => require(['./Templates/PackageOperation'], resolve))));
    //}

    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(ProfilerPermission.ViewHeavyProfiler),
        key: "ProfilerHeavy",
        onClick: () => Promise.resolve(Navigator.currentHistory.createHref("/profiler/heavyProfiler"))
    });
    
    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(ProfilerPermission.ViewTimeTracker),
        key: "ProfilerTimes",
        onClick: () => Promise.resolve(Navigator.currentHistory.createHref("/profiler/times"))
    });

    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(ProfilerPermission.OverrideSessionTimeout),
        key: "OverrideSessionTimeout",
        onClick: () => Promise.resolve(Navigator.currentHistory.createHref("/profiler/overrideSessionTimeout"))
    });

}


export module API {

    export module  Heavy {
        export function start(): Promise<void> {
            return ajaxPost<void>({ url: "/api/profilerHeavy/start" }, null);
        }

        export function stop(): Promise<void> {
            return ajaxPost<void>({ url: "/api/profilerHeavy/stop" }, null);
        }

        export function clear(): Promise<void> {
            return ajaxPost<void>({ url: "/api/profilerHeavy/clear" }, null);
        }

    }

    export module Times {

        export function clear(): Promise<void> {
            return ajaxPost<void>({ url: "/api/profilerTimes/clear" }, null);
        }

        export function fetchInfo(): Promise<TimeTrackerEntry[]> {
            return ajaxGet<TimeTrackerEntry[]>({ url: "/api/profilerTimes/times" });
        }
    }
    //export function view(): Promise<ProcessLogicState> {
    //    return ajaxGet<ProcessLogicState>({ url: "/api/processes/view" });
    //}
}


export interface TimeTrackerEntry {
    key: string;
    count: number;        
    averageTime: number;
    totalTime: number;

    lastTime: number;
    lastDate: string;
    
    maxTime: number;
    maxDate: string;
    
    minTime: number;
    minDate: string;
}


