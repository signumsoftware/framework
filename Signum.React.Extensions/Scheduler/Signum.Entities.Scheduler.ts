//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from 'Framework/Signum.React/Scripts/Reflection' 

import * as Entities from 'Framework/Signum.React/Scripts/Signum.Entities' 

import * as Authorization from 'Extensions/Signum.React.Extensions/Authorization/Signum.Entities.Authorization' 
export const ApplicationEventLogEntity_Type = new Type<ApplicationEventLogEntity>("ApplicationEventLogEntity");
export interface ApplicationEventLogEntity extends Entities.Entity {
    machineName?: string;
    date?: string;
    globalEvent?: TypeEvent;
}

export const HolidayCalendarEntity_Type = new Type<HolidayCalendarEntity>("HolidayCalendarEntity");
export interface HolidayCalendarEntity extends Entities.Entity {
    name?: string;
    holidays?: Entities.MList<HolidayEntity>;
}

export module HolidayCalendarOperation {
    export const Save : Entities.ExecuteSymbol<HolidayCalendarEntity> = registerSymbol({ key: "HolidayCalendarOperation.Save" });
    export const Delete : Entities.DeleteSymbol<HolidayCalendarEntity> = registerSymbol({ key: "HolidayCalendarOperation.Delete" });
}

export const HolidayEntity_Type = new Type<HolidayEntity>("HolidayEntity");
export interface HolidayEntity extends Entities.EmbeddedEntity {
    date?: string;
    name?: string;
}

export interface IScheduleRuleEntity extends Entities.IEntity {
}

export interface ITaskEntity extends Entities.IEntity {
}

export const ScheduledTaskEntity_Type = new Type<ScheduledTaskEntity>("ScheduledTaskEntity");
export interface ScheduledTaskEntity extends Entities.Entity {
    rule?: IScheduleRuleEntity;
    task?: ITaskEntity;
    suspended?: boolean;
    machineName?: string;
    user?: Entities.Lite<Entities.Basics.IUserEntity>;
    applicationName?: string;
}

export const ScheduledTaskLogEntity_Type = new Type<ScheduledTaskLogEntity>("ScheduledTaskLogEntity");
export interface ScheduledTaskLogEntity extends Entities.Entity {
    scheduledTask?: ScheduledTaskEntity;
    user?: Entities.Lite<Entities.Basics.IUserEntity>;
    task?: ITaskEntity;
    startTime?: string;
    endTime?: string;
    machineName?: string;
    applicationName?: string;
    productEntity?: Entities.Lite<Entities.IEntity>;
    exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
}

export module ScheduledTaskOperation {
    export const Save : Entities.ExecuteSymbol<ScheduledTaskEntity> = registerSymbol({ key: "ScheduledTaskOperation.Save" });
    export const Delete : Entities.DeleteSymbol<ScheduledTaskEntity> = registerSymbol({ key: "ScheduledTaskOperation.Delete" });
}

export module SchedulerMessage {
    export const _0IsNotMultiple1 = new MessageKey("SchedulerMessage", "_0IsNotMultiple1");
    export const Each0Hours = new MessageKey("SchedulerMessage", "Each0Hours");
    export const Each0Minutes = new MessageKey("SchedulerMessage", "Each0Minutes");
    export const ScheduleRuleDailyEntity = new MessageKey("SchedulerMessage", "ScheduleRuleDailyEntity");
    export const ScheduleRuleDailyDN_Everydayat = new MessageKey("SchedulerMessage", "ScheduleRuleDailyDN_Everydayat");
    export const ScheduleRuleDayDN_StartingOn = new MessageKey("SchedulerMessage", "ScheduleRuleDayDN_StartingOn");
    export const ScheduleRuleHourlyEntity = new MessageKey("SchedulerMessage", "ScheduleRuleHourlyEntity");
    export const ScheduleRuleMinutelyEntity = new MessageKey("SchedulerMessage", "ScheduleRuleMinutelyEntity");
    export const ScheduleRuleWeekDaysEntity = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysEntity");
    export const ScheduleRuleWeekDaysDN_AndHoliday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_AndHoliday");
    export const ScheduleRuleWeekDaysDN_At = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_At");
    export const ScheduleRuleWeekDaysDN_ButHoliday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_ButHoliday");
    export const ScheduleRuleWeekDaysDN_Calendar = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Calendar");
    export const ScheduleRuleWeekDaysDN_F = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_F");
    export const ScheduleRuleWeekDaysDN_Friday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Friday");
    export const ScheduleRuleWeekDaysDN_Holiday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Holiday");
    export const ScheduleRuleWeekDaysDN_M = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_M");
    export const ScheduleRuleWeekDaysDN_Monday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Monday");
    export const ScheduleRuleWeekDaysDN_S = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_S");
    export const ScheduleRuleWeekDaysDN_Sa = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Sa");
    export const ScheduleRuleWeekDaysDN_Saturday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Saturday");
    export const ScheduleRuleWeekDaysDN_Sunday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Sunday");
    export const ScheduleRuleWeekDaysDN_T = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_T");
    export const ScheduleRuleWeekDaysDN_Th = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Th");
    export const ScheduleRuleWeekDaysDN_Thursday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Thursday");
    export const ScheduleRuleWeekDaysDN_Tuesday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Tuesday");
    export const ScheduleRuleWeekDaysDN_W = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_W");
    export const ScheduleRuleWeekDaysDN_Wednesday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Wednesday");
    export const ScheduleRuleWeeklyEntity = new MessageKey("SchedulerMessage", "ScheduleRuleWeeklyEntity");
    export const ScheduleRuleWeeklyDN_DayOfTheWeek = new MessageKey("SchedulerMessage", "ScheduleRuleWeeklyDN_DayOfTheWeek");
}

export module SchedulerPermission {
    export const ViewSchedulerPanel : Authorization.PermissionSymbol = registerSymbol({ key: "SchedulerPermission.ViewSchedulerPanel" });
}

export const ScheduleRuleDailyEntity_Type = new Type<ScheduleRuleDailyEntity>("ScheduleRuleDailyEntity");
export interface ScheduleRuleDailyEntity extends ScheduleRuleDayEntity {
}

export interface ScheduleRuleDayEntity extends Entities.Entity, IScheduleRuleEntity {
    startingOn?: string;
}

export const ScheduleRuleHourlyEntity_Type = new Type<ScheduleRuleHourlyEntity>("ScheduleRuleHourlyEntity");
export interface ScheduleRuleHourlyEntity extends Entities.Entity, IScheduleRuleEntity {
    eachHours?: number;
}

export const ScheduleRuleMinutelyEntity_Type = new Type<ScheduleRuleMinutelyEntity>("ScheduleRuleMinutelyEntity");
export interface ScheduleRuleMinutelyEntity extends Entities.Entity, IScheduleRuleEntity {
    eachMinutes?: number;
}

export const ScheduleRuleWeekDaysEntity_Type = new Type<ScheduleRuleWeekDaysEntity>("ScheduleRuleWeekDaysEntity");
export interface ScheduleRuleWeekDaysEntity extends ScheduleRuleDayEntity {
    monday?: boolean;
    tuesday?: boolean;
    wednesday?: boolean;
    thursday?: boolean;
    friday?: boolean;
    saturday?: boolean;
    sunday?: boolean;
    calendar?: HolidayCalendarEntity;
    holiday?: boolean;
}

export const ScheduleRuleWeeklyEntity_Type = new Type<ScheduleRuleWeeklyEntity>("ScheduleRuleWeeklyEntity");
export interface ScheduleRuleWeeklyEntity extends ScheduleRuleDayEntity {
    dayOfTheWeek?: External.DayOfWeek;
}

export const SimpleTaskSymbol_Type = new Type<SimpleTaskSymbol>("SimpleTaskSymbol");
export interface SimpleTaskSymbol extends Entities.Symbol, ITaskEntity {
}

export module TaskMessage {
    export const Execute = new MessageKey("TaskMessage", "Execute");
    export const Executions = new MessageKey("TaskMessage", "Executions");
    export const LastExecution = new MessageKey("TaskMessage", "LastExecution");
}

export module TaskOperation {
    export const ExecuteSync : Entities.ConstructSymbol_From<Entities.IEntity, ITaskEntity> = registerSymbol({ key: "TaskOperation.ExecuteSync" });
    export const ExecuteAsync : Entities.ExecuteSymbol<ITaskEntity> = registerSymbol({ key: "TaskOperation.ExecuteAsync" });
}

export enum TypeEvent {
    Start,
    Stop,
}
export const TypeEvent_Type = new EnumType<TypeEvent>("TypeEvent", TypeEvent);

export namespace External {

    export enum DayOfWeek {
        Sunday,
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
    }
    export const DayOfWeek_Type = new EnumType<DayOfWeek>("DayOfWeek", DayOfWeek);
    
}

