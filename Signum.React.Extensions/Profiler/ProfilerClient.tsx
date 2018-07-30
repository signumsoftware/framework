
import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '@framework/Globals';
import { ajaxPost, ajaxGet, ajaxGetRaw, saveFile } from '@framework/Services';
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From } from '@framework/Signum.Entities'
import { EntityOperationSettings } from '@framework/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import { ProfilerPermission } from './Signum.Entities.Profiler'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import { ImportRoute } from "@framework/AsyncImport";

export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(
        <ImportRoute path="~/profiler/times" onImportModule={() => import("./Times/TimesPage")} />,
        <ImportRoute path="~/profiler/heavy" exact onImportModule={() => import("./Heavy/HeavyListPage")} />,
        <ImportRoute path="~/profiler/heavy/entry/:selectedIndex" onImportModule={() => import("./Heavy/HeavyEntryPage")} />
    );


    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(ProfilerPermission.ViewHeavyProfiler),
        key: "ProfilerHeavy",
        onClick: () => Promise.resolve("~/profiler/heavy")
    });

    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(ProfilerPermission.ViewTimeTracker),
        key: "ProfilerTimes",
        onClick: () => Promise.resolve("~/profiler/times")
    });

    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(ProfilerPermission.OverrideSessionTimeout),
        key: "OverrideSessionTimeout",
        onClick: () => Promise.resolve("~/profiler/overrideSessionTimeout")
    });

}


export module API {

    export module Heavy {
        export function setEnabled(isEnabled: boolean): Promise<void> {
            return ajaxPost<void>({ url: "~/api/profilerHeavy/setEnabled/" + isEnabled }, undefined);
        }

        export function isEnabled(): Promise<boolean> {
            return ajaxGet<boolean>({ url: "~/api/profilerHeavy/isEnabled" });
        }

        export function clear(): Promise<void> {
            return ajaxPost<void>({ url: "~/api/profilerHeavy/clear" }, undefined);
        }

        export function entries(): Promise<HeavyProfilerEntry[]> {
            return ajaxGet<HeavyProfilerEntry[]>({ url: "~/api/profilerHeavy/entries" });
        }

        export function details(key: string): Promise<HeavyProfilerEntry[]> {
            return ajaxGet<HeavyProfilerEntry[]>({ url: "~/api/profilerHeavy/details/" + key });
        }

        export function stackTrace(key: string): Promise<StackTraceTS[]> {
            return ajaxGet<StackTraceTS[]>({ url: "~/api/profilerHeavy/stackTrace/" + key });
        }

        export function download(indices?: string): void {
            ajaxGetRaw({ url: "~/api/profilerHeavy/download" + (indices ? ("?indices=" + indices) : "") })
                .then(response => saveFile(response))
                .done();
        }

        export function upload(file: { fileName: string; content: string }): Promise<void> {
            return ajaxPost<void>({ url: "~/api/profilerHeavy/upload" }, file);
        }
    }

    export module Times {

        export function clear(): Promise<void> {
            return ajaxPost<void>({ url: "~/api/profilerTimes/clear" }, undefined);
        }

        export function fetchInfo(): Promise<TimeTrackerEntry[]> {
            return ajaxGet<TimeTrackerEntry[]>({ url: "~/api/profilerTimes/times" });
        }
    }
}

export interface StackTraceTS {
    Color: string;
    FileName: string;
    LineNumber: number;
    Method: string;
    Type: string;
    Namespace: string;
}

export interface HeavyProfilerEntry {
    BeforeStart: number;
    Start: number;
    End: number;
    Elapsed: string;
    IsFinished: boolean;
    Role: string;
    Color: string;
    Depth: number;
    AsyncDepth: number;
    AdditionalData: string;
    FullIndex: string;
    StackTrace: string;
    Entries: HeavyProfilerEntry[];
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


