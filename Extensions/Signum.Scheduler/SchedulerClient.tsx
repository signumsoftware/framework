import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import { Operations, EntityOperationGroup, EntityOperationSettings } from '@framework/Operations'
import { Lite } from '@framework/Signum.Entities'
import {
  ScheduledTaskLogEntity, ScheduledTaskEntity, ScheduleRuleMinutelyEntity, ScheduleRuleMonthsEntity,
  ScheduleRuleWeekDaysEntity, SchedulerPermission, SchedulerTaskExceptionLineEntity, ITaskOperation, ITaskMessage,
  HolidayCalendarEntity
} from './Signum.Scheduler'
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'
import { ImportComponent } from '@framework/ImportComponent'
import { SearchValueLine } from '@framework/Search';
import { isPermissionAuthorized } from '@framework/AppContext';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';
import { HolidayCalendarClient } from './HolidayCalendarClient';
import { Constructor } from '@framework/Constructor';
import { Finder } from '@framework/Finder';

export namespace SchedulerClient {
  
  export function start(options: { routes: RouteObject[] }): void {
  
    ChangeLogClient.registerChangeLogModule("Signum.Scheduler", () => import("./Changelog"));
  
    options.routes.push({ path: "/scheduler/view", element: <ImportComponent onImport={() => import("./SchedulerPanelPage")} /> });
  
    Navigator.addSettings(new EntitySettings(ScheduledTaskEntity, e => import('./Templates/ScheduledTask')));
    Navigator.addSettings(new EntitySettings(ScheduleRuleMinutelyEntity, e => import('./Templates/ScheduleRuleMinutely')));
    Navigator.addSettings(new EntitySettings(ScheduleRuleWeekDaysEntity, e => import('./Templates/ScheduleRuleWeekDays')));
    Navigator.addSettings(new EntitySettings(ScheduleRuleMonthsEntity, e => import('./Templates/ScheduleRuleMonths')));

    Constructor.registerConstructor(ScheduleRuleWeekDaysEntity, async props => {

      var holidayCalendar = (await Finder.fetchEntities({
        queryName: HolidayCalendarEntity,
        filterOptions: [{ token: HolidayCalendarEntity.token(a => a.entity.isDefault), value: true }]
      })).firstOrNull();

      return ScheduleRuleWeekDaysEntity.New({ calendar: holidayCalendar, ...props });
    })
  
    var group: EntityOperationGroup = {
      key: "execute",
      text: () => ITaskMessage.Execute.niceToString()
    };
  
    Operations.addSettings(new EntityOperationSettings(ITaskOperation.ExecuteSync, {
      icon: "bolt",
      iconColor: "#F1C40F",
      group: group
    }));
  
    OmniboxSpecialAction.registerSpecialAction({
      allowed: () => isPermissionAuthorized(SchedulerPermission.ViewSchedulerPanel),
      key: "SchedulerPanel",
      onClick: () => Promise.resolve("/scheduler/view")
    });
  
    var es = new EntitySettings(ScheduledTaskLogEntity, undefined);
    es.overrideView(vr => vr.insertAfterLine(a => a.exception, ctx => [
      <SearchValueLine ctx={ctx} badgeColor="danger" isBadge="MoreThanZero" findOptions={{
        queryName: SchedulerTaskExceptionLineEntity,
        filterOptions: [{ token: SchedulerTaskExceptionLineEntity.token(e => e.schedulerTaskLog), value: ctx.value}],
      }} />
    ]))
    Navigator.addSettings(es);

    HolidayCalendarClient.start(options);
  }  
  
  export namespace API {
  
    export function start(): Promise<void> {
      return ajaxPost({ url: "/api/scheduler/start" }, undefined);
    }
  
    export function stop(): Promise<void> {
      return ajaxPost({ url: "/api/scheduler/stop" }, undefined);
    }
  
    export function view(): Promise<SchedulerState> {
      return ajaxGet({ url: "/api/scheduler/view", avoidNotifyPendingRequests: true });
    }
  }
  
  
  export interface SchedulerState {
    running: boolean;
    initialDelayMilliseconds: number | null;
    schedulerMargin: string;
    nextExecution: string;
    machineName: string;
    applicationName: string;
    serverTimeZone: string;
    serverLocalTime: string;
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
}
