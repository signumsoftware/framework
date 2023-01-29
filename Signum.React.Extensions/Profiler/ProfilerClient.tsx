import * as React from 'react'
import { ajaxPost, ajaxGet, ajaxGetRaw, saveFile } from '@framework/Services';
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
      return ajaxPost({ url: "~/api/profilerHeavy/setEnabled/" + isEnabled }, undefined);
    }

    export function isEnabled(): Promise<boolean> {
      return ajaxGet({ url: "~/api/profilerHeavy/isEnabled" });
    }

    export function clear(): Promise<void> {
      return ajaxPost({ url: "~/api/profilerHeavy/clear" }, undefined);
    }

    export function entries(ignoreProfilerHeavyEntries: boolean): Promise<HeavyProfilerEntry[]> {
      return ajaxGet({ url: "~/api/profilerHeavy/entries?ignoreProfilerHeavyEntries=" + ignoreProfilerHeavyEntries });
    }

    export function details(key: string): Promise<HeavyProfilerEntry[]> {
      return ajaxGet({ url: "~/api/profilerHeavy/details/" + key });
    }

    export function stackTrace(key: string): Promise<StackTraceTS[]> {
      return ajaxGet({ url: "~/api/profilerHeavy/stackTrace/" + key });
    }

    export function download(indices?: string): void {
      ajaxGetRaw({ url: "~/api/profilerHeavy/download" + (indices ? ("?indices=" + indices) : "") })
        .then(response => saveFile(response));
    }

    export function upload(file: { fileName: string; content: string }): Promise<void> {
      return ajaxPost({ url: "~/api/profilerHeavy/upload" }, file);
    }
  }

  export module Times {

    export function clear(): Promise<void> {
      return ajaxPost({ url: "~/api/profilerTimes/clear" }, undefined);
    }

    export function fetchInfo(): Promise<TimeTrackerEntry[]> {
      return ajaxGet({ url: "~/api/profilerTimes/times" });
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


