import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet, ajaxGetRaw, saveFile } from '@framework/Services';
import { ProfilerPermission } from './Signum.Profiler'
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import { ImportComponent } from '@framework/ImportComponent'
import { isPermissionAuthorized } from '@framework/AppContext';
import { Lite } from '@framework/Signum.Entities';
import { UserEntity } from '../Signum.Authorization/Signum.Authorization';

export namespace ProfilerClient {
  
  export function start(options: { routes: RouteObject[] }): void {
    options.routes.push(
      { path: "/profiler/times", element: <ImportComponent onImport={() => import("./Times/TimesPage")} /> },
      { path: "/profiler/heavy", element: <ImportComponent onImport={() => import("./Heavy/HeavyListPage")} /> },
      { path: "/profiler/heavy/entry/:selectedIndex", element: <ImportComponent onImport={() => import("./Heavy/HeavyEntryPage")} /> }
    );
  
  
    OmniboxSpecialAction.registerSpecialAction({
      allowed: () => isPermissionAuthorized(ProfilerPermission.ViewHeavyProfiler),
      key: "ProfilerHeavy",
      onClick: () => Promise.resolve("/profiler/heavy")
    });
  
    OmniboxSpecialAction.registerSpecialAction({
      allowed: () => isPermissionAuthorized(ProfilerPermission.ViewTimeTracker),
      key: "ProfilerTimes",
      onClick: () => Promise.resolve("/profiler/times")
    });
  
    OmniboxSpecialAction.registerSpecialAction({
      allowed: () => isPermissionAuthorized(ProfilerPermission.OverrideSessionTimeout),
      key: "OverrideSessionTimeout",
      onClick: () => Promise.resolve("/profiler/overrideSessionTimeout")
    });
  
  }
  
  
  export module API {
  
    export module Heavy {
      export function setEnabled(isEnabled: boolean): Promise<void> {
        return ajaxPost({ url: "/api/profilerHeavy/setEnabled/" + isEnabled }, undefined);
      }
  
      export function isEnabled(): Promise<boolean> {
        return ajaxGet({ url: "/api/profilerHeavy/isEnabled" });
      }
  
      export function clear(): Promise<void> {
        return ajaxPost({ url: "/api/profilerHeavy/clear" }, undefined);
      }
  
      export function entries(ignoreProfilerHeavyEntries: boolean): Promise<HeavyProfilerEntry[]> {
        return ajaxGet({ url: "/api/profilerHeavy/entries?ignoreProfilerHeavyEntries=" + ignoreProfilerHeavyEntries });
      }
  
      export function details(key: string): Promise<HeavyProfilerEntry[]> {
        return ajaxGet({ url: "/api/profilerHeavy/details/" + key });
      }
  
      export function stackTrace(key: string): Promise<StackTraceTS[]> {
        return ajaxGet({ url: "/api/profilerHeavy/stackTrace/" + key });
      }
  
      export function download(indices?: string): void {
        ajaxGetRaw({ url: "/api/profilerHeavy/download" + (indices ? ("?indices=" + indices) : "") })
          .then(response => saveFile(response));
      }
  
      export function upload(file: { fileName: string; content: string }): Promise<void> {
        return ajaxPost({ url: "/api/profilerHeavy/upload" }, file);
      }
    }
  
    export module Times {
  
      export function clear(): Promise<void> {
        return ajaxPost({ url: "/api/profilerTimes/clear" }, undefined);
      }
  
      export function fetchInfo(): Promise<TimeTrackerEntry[]> {
        return ajaxGet({ url: "/api/profilerTimes/times" });
      }
    }
  }
  
  export interface StackTraceTS {
    color: string;
    fileName: string;
    lineNumber: number;
    method: string;
    type: string;
    namespace: string;
  }
  
  export interface HeavyProfilerEntry {
    beforeStart: number;
    start: number;
    end: number;
    elapsed: string;
    isFinished: boolean;
    role: string;
    color: string;
    depth: number;
    asyncDepth: number;
    additionalData: string;
    fullIndex: string;
    stackTrace: string;
    entries: HeavyProfilerEntry[];
  }
  
  export interface TimeTrackerTime {
    duration: number;
    date: string;
    url: string;
    user: Lite<UserEntity>;
  }
  
  export interface TimeTrackerEntry {
    identifier: string;
    count: number;
    averageDuration: number;
    totalDuration: number;
  
  
    max: TimeTrackerTime;
    max2?: TimeTrackerTime;
    max3?: TimeTrackerTime;
  
    min: TimeTrackerTime;
    last: TimeTrackerTime;
  }
}


