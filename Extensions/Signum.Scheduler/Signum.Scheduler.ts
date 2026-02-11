//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Security from '../../Signum/React/Signum.Security'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as UserAssets from '../Signum.UserAssets/Signum.UserAssets'


export const HolidayCalendarEntity: Type<HolidayCalendarEntity> = new Type<HolidayCalendarEntity>("HolidayCalendar");
export interface HolidayCalendarEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "HolidayCalendar";
  guid: string /*Guid*/;
  name: string;
  fromYear: number | null;
  toYear: number | null;
  countryCode: string | null;
  subDivisionCode: string | null;
  isDefault: boolean;
  holidays: Entities.MList<HolidayEmbedded>;
}

export namespace HolidayCalendarMessage {
  export const ForImport01and2ShouldBeSet: MessageKey = new MessageKey("HolidayCalendarMessage", "ForImport01and2ShouldBeSet");
}

export namespace HolidayCalendarOperation {
  export const Save : Operations.ExecuteSymbol<HolidayCalendarEntity> = registerSymbol("Operation", "HolidayCalendarOperation.Save");
  export const ImportPublicHolidays : Operations.ExecuteSymbol<HolidayCalendarEntity> = registerSymbol("Operation", "HolidayCalendarOperation.ImportPublicHolidays");
  export const Delete : Operations.DeleteSymbol<HolidayCalendarEntity> = registerSymbol("Operation", "HolidayCalendarOperation.Delete");
}

export const HolidayEmbedded: Type<HolidayEmbedded> = new Type<HolidayEmbedded>("HolidayEmbedded");
export interface HolidayEmbedded extends Entities.EmbeddedEntity {
  Type: "HolidayEmbedded";
  date: string /*DateOnly*/;
  name: string | null;
}

export interface IScheduleRuleEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  startingOn: string /*DateTime*/;
}

export interface ITaskEntity extends Entities.Entity {
}

export namespace ITaskMessage {
  export const Execute: MessageKey = new MessageKey("ITaskMessage", "Execute");
  export const Executions: MessageKey = new MessageKey("ITaskMessage", "Executions");
  export const LastExecution: MessageKey = new MessageKey("ITaskMessage", "LastExecution");
  export const ExceptionLines: MessageKey = new MessageKey("ITaskMessage", "ExceptionLines");
}

export namespace ITaskOperation {
  export const ExecuteSync : Operations.ConstructSymbol_From<ScheduledTaskLogEntity, ITaskEntity> = registerSymbol("Operation", "ITaskOperation.ExecuteSync");
}

export const ScheduledTaskEntity: Type<ScheduledTaskEntity> = new Type<ScheduledTaskEntity>("ScheduledTask");
export interface ScheduledTaskEntity extends Entities.Entity {
  Type: "ScheduledTask";
  rule: IScheduleRuleEntity;
  task: ITaskEntity;
  suspended: boolean;
  machineName: string;
  user: Entities.Lite<Security.IUserEntity>;
  applicationName: string;
}

export const ScheduledTaskLogEntity: Type<ScheduledTaskLogEntity> = new Type<ScheduledTaskLogEntity>("ScheduledTaskLog");
export interface ScheduledTaskLogEntity extends Entities.Entity {
  Type: "ScheduledTaskLog";
  task: ITaskEntity;
  scheduledTask: ScheduledTaskEntity | null;
  user: Entities.Lite<Security.IUserEntity>;
  startTime: string /*DateTime*/;
  endTime: string /*DateTime*/ | null;
  machineName: string;
  applicationName: string;
  productEntity: Entities.Lite<Entities.Entity> | null;
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
  remarks: string | null;
}

export namespace ScheduledTaskLogOperation {
  export const CancelRunningTask : Operations.ExecuteSymbol<ScheduledTaskLogEntity> = registerSymbol("Operation", "ScheduledTaskLogOperation.CancelRunningTask");
}

export namespace ScheduledTaskMessage {
  export const State: MessageKey = new MessageKey("ScheduledTaskMessage", "State");
  export const InitialDelayMilliseconds: MessageKey = new MessageKey("ScheduledTaskMessage", "InitialDelayMilliseconds");
  export const SchedulerMargin: MessageKey = new MessageKey("ScheduledTaskMessage", "SchedulerMargin");
  export const MachineName: MessageKey = new MessageKey("ScheduledTaskMessage", "MachineName");
  export const ApplicationName: MessageKey = new MessageKey("ScheduledTaskMessage", "ApplicationName");
  export const ServerTimeZone: MessageKey = new MessageKey("ScheduledTaskMessage", "ServerTimeZone");
  export const ServerLocalTime: MessageKey = new MessageKey("ScheduledTaskMessage", "ServerLocalTime");
  export const NextExecution: MessageKey = new MessageKey("ScheduledTaskMessage", "NextExecution");
  export const InMemoryQueue: MessageKey = new MessageKey("ScheduledTaskMessage", "InMemoryQueue");
  export const RunningTasks: MessageKey = new MessageKey("ScheduledTaskMessage", "RunningTasks");
  export const AvailableTasks: MessageKey = new MessageKey("ScheduledTaskMessage", "AvailableTasks");
  export const Rule: MessageKey = new MessageKey("ScheduledTaskMessage", "Rule");
  export const NextDate: MessageKey = new MessageKey("ScheduledTaskMessage", "NextDate");
  export const ScheduledTask: MessageKey = new MessageKey("ScheduledTaskMessage", "ScheduledTask");
  export const Running: MessageKey = new MessageKey("ScheduledTaskMessage", "Running");
  export const Stopped: MessageKey = new MessageKey("ScheduledTaskMessage", "Stopped");
  export const None: MessageKey = new MessageKey("ScheduledTaskMessage", "None");
  export const SimpleStatus: MessageKey = new MessageKey("ScheduledTaskMessage", "SimpleStatus");
  export const ThereIsNoActiveScheduledTask: MessageKey = new MessageKey("ScheduledTaskMessage", "ThereIsNoActiveScheduledTask");
  export const ThereAreNoTasksRunning: MessageKey = new MessageKey("ScheduledTaskMessage", "ThereAreNoTasksRunning");
  export const SchedulerTaskLog: MessageKey = new MessageKey("ScheduledTaskMessage", "SchedulerTaskLog");
  export const StartTime: MessageKey = new MessageKey("ScheduledTaskMessage", "StartTime");
  export const Remarks: MessageKey = new MessageKey("ScheduledTaskMessage", "Remarks");
  export const Cancel: MessageKey = new MessageKey("ScheduledTaskMessage", "Cancel");
  export const SchedulePanel: MessageKey = new MessageKey("ScheduledTaskMessage", "SchedulePanel");
  export const Start: MessageKey = new MessageKey("ScheduledTaskMessage", "Start");
  export const Stop: MessageKey = new MessageKey("ScheduledTaskMessage", "Stop");
}

export namespace ScheduledTaskOperation {
  export const Save : Operations.ExecuteSymbol<ScheduledTaskEntity> = registerSymbol("Operation", "ScheduledTaskOperation.Save");
  export const Delete : Operations.DeleteSymbol<ScheduledTaskEntity> = registerSymbol("Operation", "ScheduledTaskOperation.Delete");
}

export namespace SchedulerMessage {
  export const Each0Minutes: MessageKey = new MessageKey("SchedulerMessage", "Each0Minutes");
  export const ScheduleRuleWeekDaysDN_AndHoliday: MessageKey = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_AndHoliday");
  export const ScheduleRuleWeekDaysDN_At: MessageKey = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_At");
  export const ScheduleRuleWeekDaysDN_ButHoliday: MessageKey = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_ButHoliday");
  export const ScheduleRuleWeekDaysDN_Mo: MessageKey = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Mo");
  export const ScheduleRuleWeekDaysDN_Tu: MessageKey = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Tu");
  export const ScheduleRuleWeekDaysDN_We: MessageKey = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_We");
  export const ScheduleRuleWeekDaysDN_Th: MessageKey = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Th");
  export const ScheduleRuleWeekDaysDN_Fr: MessageKey = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Fr");
  export const ScheduleRuleWeekDaysDN_Sa: MessageKey = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Sa");
  export const ScheduleRuleWeekDaysDN_Su: MessageKey = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Su");
  export const Day0At1In2: MessageKey = new MessageKey("SchedulerMessage", "Day0At1In2");
  export const TaskIsNotRunning: MessageKey = new MessageKey("SchedulerMessage", "TaskIsNotRunning");
  export const Holiday: MessageKey = new MessageKey("SchedulerMessage", "Holiday");
  export const SelectAll: MessageKey = new MessageKey("SchedulerMessage", "SelectAll");
}

export namespace SchedulerPermission {
  export const ViewSchedulerPanel : Basics.PermissionSymbol = registerSymbol("Permission", "SchedulerPermission.ViewSchedulerPanel");
}

export const SchedulerTaskExceptionLineEntity: Type<SchedulerTaskExceptionLineEntity> = new Type<SchedulerTaskExceptionLineEntity>("SchedulerTaskExceptionLine");
export interface SchedulerTaskExceptionLineEntity extends Entities.Entity {
  Type: "SchedulerTaskExceptionLine";
  elementInfo: string | null;
  schedulerTaskLog: Entities.Lite<ScheduledTaskLogEntity> | null;
  exception: Entities.Lite<Basics.ExceptionEntity>;
}

export const ScheduleRuleMinutelyEntity: Type<ScheduleRuleMinutelyEntity> = new Type<ScheduleRuleMinutelyEntity>("ScheduleRuleMinutely");
export interface ScheduleRuleMinutelyEntity extends Entities.Entity, IScheduleRuleEntity, UserAssets.IUserAssetEntity {
  Type: "ScheduleRuleMinutely";
  guid: string /*Guid*/;
  startingOn: string /*DateTime*/;
  eachMinutes: number;
  isAligned: boolean;
}

export const ScheduleRuleMonthsEntity: Type<ScheduleRuleMonthsEntity> = new Type<ScheduleRuleMonthsEntity>("ScheduleRuleMonths");
export interface ScheduleRuleMonthsEntity extends Entities.Entity, IScheduleRuleEntity, UserAssets.IUserAssetEntity {
  Type: "ScheduleRuleMonths";
  guid: string /*Guid*/;
  startingOn: string /*DateTime*/;
  january: boolean;
  february: boolean;
  march: boolean;
  april: boolean;
  may: boolean;
  june: boolean;
  july: boolean;
  august: boolean;
  september: boolean;
  october: boolean;
  november: boolean;
  december: boolean;
}

export const ScheduleRuleWeekDaysEntity: Type<ScheduleRuleWeekDaysEntity> = new Type<ScheduleRuleWeekDaysEntity>("ScheduleRuleWeekDays");
export interface ScheduleRuleWeekDaysEntity extends Entities.Entity, IScheduleRuleEntity, UserAssets.IUserAssetEntity {
  Type: "ScheduleRuleWeekDays";
  guid: string /*Guid*/;
  startingOn: string /*DateTime*/;
  monday: boolean;
  tuesday: boolean;
  wednesday: boolean;
  thursday: boolean;
  friday: boolean;
  saturday: boolean;
  sunday: boolean;
  calendar: HolidayCalendarEntity | null;
  holiday: boolean;
}

export const SimpleTaskSymbol: Type<SimpleTaskSymbol> = new Type<SimpleTaskSymbol>("SimpleTask");
export interface SimpleTaskSymbol extends Basics.Symbol, ITaskEntity {
  Type: "SimpleTask";
}

