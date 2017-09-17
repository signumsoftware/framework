
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { Lite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import {
    ScheduledTaskLogEntity, ScheduledTaskEntity, ScheduleRuleMinutelyEntity, ScheduleRuleMonthsEntity,
    ScheduleRuleWeekDaysEntity, HolidayCalendarEntity, SchedulerPermission, SchedulerTaskExceptionLineEntity
} from './Signum.Entities.Scheduler'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import { ImportRoute } from "../../../Framework/Signum.React/Scripts/AsyncImport";
import { ValueSearchControlLine } from '../../../Framework/Signum.React/Scripts/Search';


export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<ImportRoute path="~/scheduler/view" onImportModule={() => import("./SchedulerPanelPage")} />);

    Navigator.addSettings(new EntitySettings(ScheduledTaskEntity, e => import('./Templates/ScheduledTask')));
    Navigator.addSettings(new EntitySettings(ScheduleRuleMinutelyEntity, e => import('./Templates/ScheduleRuleMinutely')));
    Navigator.addSettings(new EntitySettings(ScheduleRuleWeekDaysEntity, e => import('./Templates/ScheduleRuleWeekDays')));
    Navigator.addSettings(new EntitySettings(ScheduleRuleMonthsEntity, e => import('./Templates/ScheduleRuleMonths')));
    Navigator.addSettings(new EntitySettings(HolidayCalendarEntity, e => import('./Templates/HolidayCalendar')));
    
    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(SchedulerPermission.ViewSchedulerPanel),
        key: "SchedulerPanel",
        onClick: () => Promise.resolve("~/scheduler/view")
    });

    var es = new EntitySettings(ScheduledTaskLogEntity, undefined);
    es.overrideView(vr => vr.insertAfterLine(a => a.exception, ctx => [
        <ValueSearchControlLine ctx={ctx} findOptions={{
            queryName: SchedulerTaskExceptionLineEntity,
            parentColumn: "SchedulerTaskLog",
            parentValue: ctx.value,
        }} />
    ]))
    Navigator.addSettings(es)
}


export module API {

    export function start(): Promise<void> {
        return ajaxPost<void>({ url: "~/api/scheduler/start" }, undefined);
    }

    export function stop(): Promise<void> {
        return ajaxPost<void>({ url: "~/api/scheduler/stop" }, undefined);
    }

    export function view(): Promise<SchedulerState> {
        return ajaxGet<SchedulerState>({ url: "~/api/scheduler/view" });
    }
}


export interface SchedulerState
{
    Running: boolean;
    SchedulerMargin: string;
    NextExecution: string;
    Queue: SchedulerItemState[];
    RunningTask: SchedulerRunningTaskState[];
}

export interface SchedulerItemState
{
    ScheduledTask: Lite<ScheduledTaskEntity>;
    Rule: string;
    NextDate: string;
}

export interface SchedulerRunningTaskState {
    SchedulerTaskLog: Lite<ScheduledTaskLogEntity>;
    StartTime: string;
    Remarks: string;
}