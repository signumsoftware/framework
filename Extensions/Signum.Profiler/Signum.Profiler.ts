//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'


export namespace ProfilerMessage {
  export const HeavyProfiler: MessageKey = new MessageKey("ProfilerMessage", "HeavyProfiler");
  export const Entry0Loading: MessageKey = new MessageKey("ProfilerMessage", "Entry0Loading");
  export const Entry0_: MessageKey = new MessageKey("ProfilerMessage", "Entry0_");
  export const Role: MessageKey = new MessageKey("ProfilerMessage", "Role");
  export const Time: MessageKey = new MessageKey("ProfilerMessage", "Time");
  export const Download: MessageKey = new MessageKey("ProfilerMessage", "Download");
  export const Update: MessageKey = new MessageKey("ProfilerMessage", "Update");
  export const AdditionalData: MessageKey = new MessageKey("ProfilerMessage", "AdditionalData");
  export const StackTrace: MessageKey = new MessageKey("ProfilerMessage", "StackTrace");
  export const NoStackTrace: MessageKey = new MessageKey("ProfilerMessage", "NoStackTrace");
  export const StackTraceOverview: MessageKey = new MessageKey("ProfilerMessage", "StackTraceOverview");
  export const AsyncStack: MessageKey = new MessageKey("ProfilerMessage", "AsyncStack");
  export const Namespace: MessageKey = new MessageKey("ProfilerMessage", "Namespace");
  export const Type: MessageKey = new MessageKey("ProfilerMessage", "Type");
  export const Method: MessageKey = new MessageKey("ProfilerMessage", "Method");
  export const FileLine: MessageKey = new MessageKey("ProfilerMessage", "FileLine");
}

export namespace ProfilerPermission {
  export const ViewTimeTracker : Basics.PermissionSymbol = registerSymbol("Permission", "ProfilerPermission.ViewTimeTracker");
  export const ViewHeavyProfiler : Basics.PermissionSymbol = registerSymbol("Permission", "ProfilerPermission.ViewHeavyProfiler");
  export const OverrideSessionTimeout : Basics.PermissionSymbol = registerSymbol("Permission", "ProfilerPermission.OverrideSessionTimeout");
}

export namespace TimeMessage {
  export const TimesLoading: MessageKey = new MessageKey("TimeMessage", "TimesLoading");
  export const Times: MessageKey = new MessageKey("TimeMessage", "Times");
  export const Reload: MessageKey = new MessageKey("TimeMessage", "Reload");
  export const Clear: MessageKey = new MessageKey("TimeMessage", "Clear");
  export const Bars: MessageKey = new MessageKey("TimeMessage", "Bars");
  export const Table: MessageKey = new MessageKey("TimeMessage", "Table");
  export const Average: MessageKey = new MessageKey("TimeMessage", "Average");
  export const Executed: MessageKey = new MessageKey("TimeMessage", "Executed");
  export const Total: MessageKey = new MessageKey("TimeMessage", "Total");
  export const NoDuration: MessageKey = new MessageKey("TimeMessage", "NoDuration");
  export const TimesOverview: MessageKey = new MessageKey("TimeMessage", "TimesOverview");
  export const Name: MessageKey = new MessageKey("TimeMessage", "Name");
  export const Entity: MessageKey = new MessageKey("TimeMessage", "Entity");
  export const Count: MessageKey = new MessageKey("TimeMessage", "Count");
  export const Min: MessageKey = new MessageKey("TimeMessage", "Min");
  export const Max: MessageKey = new MessageKey("TimeMessage", "Max");
  export const Last: MessageKey = new MessageKey("TimeMessage", "Last");
  export const TimeStatistics: MessageKey = new MessageKey("TimeMessage", "TimeStatistics");
}

