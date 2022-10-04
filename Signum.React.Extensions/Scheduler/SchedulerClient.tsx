
import * as React from 'react'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Operations from '@framework/Operations'
import { Lite } from '@framework/Signum.Entities'
import {
  ScheduledTaskLogEntity, ScheduledTaskEntity, ScheduleRuleMinutelyEntity, ScheduleRuleMonthsEntity,
  ScheduleRuleWeekDaysEntity, HolidayCalendarEntity, SchedulerPermission, SchedulerTaskExceptionLineEntity, ITaskOperation, ITaskMessage
} from './Signum.Entities.Scheduler'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import { ImportRoute } from "@framework/AsyncImport";
import { SearchValueLine } from '@framework/Search';

export function start(options: { routes: JSX.Element[] }) {
  options.routes.push(<ImportRoute path="~/scheduler/view" onImportModule={() => import("./SchedulerPanelPage")} />);

  Navigator.addSettings(new EntitySettings(ScheduledTaskEntity, e => import('./Templates/ScheduledTask')));
  Navigator.addSettings(new EntitySettings(ScheduleRuleMinutelyEntity, e => import('./Templates/ScheduleRuleMinutely')));
  Navigator.addSettings(new EntitySettings(ScheduleRuleWeekDaysEntity, e => import('./Templates/ScheduleRuleWeekDays')));
  Navigator.addSettings(new EntitySettings(ScheduleRuleMonthsEntity, e => import('./Templates/ScheduleRuleMonths')));
  Navigator.addSettings(new EntitySettings(HolidayCalendarEntity, e => import('./Templates/HolidayCalendar')));

  var group: Operations.EntityOperationGroup = {
    key: "execute",
    text: () => ITaskMessage.Execute.niceToString()
  };

  Operations.addSettings(new Operations.EntityOperationSettings(ITaskOperation.ExecuteAsync, {
    icon: "hourglass",
    iconColor: "#5499C7",
    group: group
  }));

  Operations.addSettings(new Operations.EntityOperationSettings(ITaskOperation.ExecuteSync, {
    icon: "bolt",
    iconColor: "#F1C40F",
    group: group
  }));

  OmniboxClient.registerSpecialAction({
    allowed: () => AuthClient.isPermissionAuthorized(SchedulerPermission.ViewSchedulerPanel),
    key: "SchedulerPanel",
    onClick: () => Promise.resolve("~/scheduler/view")
  });

  var es = new EntitySettings(ScheduledTaskLogEntity, undefined);
  es.overrideView(vr => vr.insertAfterLine(a => a.exception, ctx => [
    <SearchValueLine ctx={ctx} findOptions={{
      queryName: SchedulerTaskExceptionLineEntity,
      filterOptions: [{ token: SchedulerTaskExceptionLineEntity.token(e => e.schedulerTaskLog), value: ctx.value}],
    }} />
  ]))
  Navigator.addSettings(es)
}


export module API {

  export function start(): Promise<void> {
    return ajaxPost({ url: "~/api/scheduler/start" }, undefined);
  }

  export function stop(): Promise<void> {
    return ajaxPost({ url: "~/api/scheduler/stop" }, undefined);
  }

  export function view(): Promise<SchedulerState> {
    return ajaxGet({ url: "~/api/scheduler/view" });
  }
}


export interface SchedulerState {
  running: boolean;
  initialDelayMilliseconds: number | null;
  schedulerMargin: string;
  nextExecution: string;
  machineName: string;
  applicationName: string;
  queue: SchedulerItemState[];
  runningTask: SchedulerRunningTaskState[];
}

export interface SchedulerItemState {
  scheduledTask: Lite<ScheduledTaskEntity>;
  rule: string;
  nextDate: string;
}

export interface SchedulerRunningTaskState {
  schedulerTaskLog: Lite<ScheduledTaskLogEntity>;
  startTime: string;
  remarks: string;
}
