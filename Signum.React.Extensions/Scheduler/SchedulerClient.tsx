
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { Lite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { ScheduledTaskLogEntity, ScheduledTaskEntity, ScheduleRuleMinutelyEntity, ScheduleRuleMonthsEntity, 
    ScheduleRuleWeekDaysEntity, HolidayCalendarEntity, SchedulerPermission } from './Signum.Entities.Scheduler'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'


export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<Route path="scheduler">
        <Route path="view" getComponent={(loc, cb) => require(["./SchedulerPanelPage"], (Comp) => cb(null, Comp.default))}/>
    </Route>);

    Navigator.addSettings(new EntitySettings(ScheduledTaskEntity, e => new Promise(resolve => require(['./Templates/SchedulerTask'], resolve))));
    Navigator.addSettings(new EntitySettings(ScheduleRuleMinutelyEntity, e => new Promise(resolve => require(['./Templates/ScheduleRuleMinutely'], resolve))));
    Navigator.addSettings(new EntitySettings(ScheduleRuleWeekDaysEntity, e => new Promise(resolve => require(['./Templates/ScheduleRuleWeekDays'], resolve))));
    Navigator.addSettings(new EntitySettings(ScheduleRuleMonthsEntity, e => new Promise(resolve => require(['./Templates/ScheduleRuleMonths'], resolve))));
    Navigator.addSettings(new EntitySettings(HolidayCalendarEntity, e => new Promise(resolve => require(['./Templates/HolidayCalendar'], resolve))));
    
    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(SchedulerPermission.ViewSchedulerPanel),
        key: "SchedulerPanel",
        onClick: () => Promise.resolve(Navigator.currentHistory.createHref("/scheduler/view"))
    });
}


export module API {

    export function start(): Promise<void> {
        return ajaxPost<void>({ url: "/api/scheduler/start" }, null);
    }

    export function stop(): Promise<void> {
        return ajaxPost<void>({ url: "/api/scheduler/stop" }, null);
    }

    export function view(): Promise<SchedulerState> {
        return ajaxGet<SchedulerState>({ url: "/api/scheduler/view" });
    }
}


export interface SchedulerState
{
    Running: boolean;
    SchedulerMargin: string;
    NextExecution: string;
    Queue: SchedulerItemState[];
}

export interface SchedulerItemState
{
    ScheduledTask: Lite<ScheduledTaskEntity>;
    Rule: string;
    NextExecution: string;
}
