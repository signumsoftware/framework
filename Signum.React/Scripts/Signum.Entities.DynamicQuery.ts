//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'


export const ColumnOptionsMode = new EnumType<ColumnOptionsMode>("ColumnOptionsMode");
export type ColumnOptionsMode =
    "Add" |
    "Remove" |
    "Replace";

export const FilterGroupOperation = new EnumType<FilterGroupOperation>("FilterGroupOperation");
export type FilterGroupOperation =
    "And" |
    "Or";

export const FilterOperation = new EnumType<FilterOperation>("FilterOperation");
export type FilterOperation =
    "EqualTo" |
    "DistinctTo" |
    "GreaterThan" |
    "GreaterThanOrEqual" |
    "LessThan" |
    "LessThanOrEqual" |
    "Contains" |
    "StartsWith" |
    "EndsWith" |
    "Like" |
    "NotContains" |
    "NotStartsWith" |
    "NotEndsWith" |
    "NotLike" |
    "IsIn" |
    "IsNotIn";

export const FilterType = new EnumType<FilterType>("FilterType");
export type FilterType =
    "Integer" |
    "Decimal" |
    "String" |
    "DateTime" |
    "Lite" |
    "Embedded" |
    "Boolean" |
    "Enum" |
    "Guid";

export const OrderType = new EnumType<OrderType>("OrderType");
export type OrderType =
    "Ascending" |
    "Descending";

export const PaginationMode = new EnumType<PaginationMode>("PaginationMode");
export type PaginationMode =
    "All" |
    "Firsts" |
    "Paginate";

export module QueryTokenMessage {
    export const _0As1 = new MessageKey("QueryTokenMessage", "_0As1");
    export const And = new MessageKey("QueryTokenMessage", "And");
    export const AnyEntity = new MessageKey("QueryTokenMessage", "AnyEntity");
    export const As0 = new MessageKey("QueryTokenMessage", "As0");
    export const Check = new MessageKey("QueryTokenMessage", "Check");
    export const Column0NotFound = new MessageKey("QueryTokenMessage", "Column0NotFound");
    export const Count = new MessageKey("QueryTokenMessage", "Count");
    export const Date = new MessageKey("QueryTokenMessage", "Date");
    export const DateTime = new MessageKey("QueryTokenMessage", "DateTime");
    export const Day = new MessageKey("QueryTokenMessage", "Day");
    export const DayOfWeek = new MessageKey("QueryTokenMessage", "DayOfWeek");
    export const DayOfYear = new MessageKey("QueryTokenMessage", "DayOfYear");
    export const DecimalNumber = new MessageKey("QueryTokenMessage", "DecimalNumber");
    export const Embedded0 = new MessageKey("QueryTokenMessage", "Embedded0");
    export const GlobalUniqueIdentifier = new MessageKey("QueryTokenMessage", "GlobalUniqueIdentifier");
    export const Hour = new MessageKey("QueryTokenMessage", "Hour");
    export const ListOf0 = new MessageKey("QueryTokenMessage", "ListOf0");
    export const Millisecond = new MessageKey("QueryTokenMessage", "Millisecond");
    export const Minute = new MessageKey("QueryTokenMessage", "Minute");
    export const Month = new MessageKey("QueryTokenMessage", "Month");
    export const MonthStart = new MessageKey("QueryTokenMessage", "MonthStart");
    export const Quarter = new MessageKey("QueryTokenMessage", "Quarter");
    export const QuarterStart = new MessageKey("QueryTokenMessage", "QuarterStart");
    export const WeekStart = new MessageKey("QueryTokenMessage", "WeekStart");
    export const HourStart = new MessageKey("QueryTokenMessage", "HourStart");
    export const MinuteStart = new MessageKey("QueryTokenMessage", "MinuteStart");
    export const SecondStart = new MessageKey("QueryTokenMessage", "SecondStart");
    export const MoreThanOneColumnNamed0 = new MessageKey("QueryTokenMessage", "MoreThanOneColumnNamed0");
    export const Number = new MessageKey("QueryTokenMessage", "Number");
    export const Of = new MessageKey("QueryTokenMessage", "Of");
    export const Second = new MessageKey("QueryTokenMessage", "Second");
    export const Text = new MessageKey("QueryTokenMessage", "Text");
    export const Year = new MessageKey("QueryTokenMessage", "Year");
    export const WeekNumber = new MessageKey("QueryTokenMessage", "WeekNumber");
    export const _0Steps1 = new MessageKey("QueryTokenMessage", "_0Steps1");
    export const Step0 = new MessageKey("QueryTokenMessage", "Step0");
    export const Length = new MessageKey("QueryTokenMessage", "Length");
    export const _0HasValue = new MessageKey("QueryTokenMessage", "_0HasValue");
    export const HasValue = new MessageKey("QueryTokenMessage", "HasValue");
    export const Modulo0 = new MessageKey("QueryTokenMessage", "Modulo0");
    export const _0Mod1 = new MessageKey("QueryTokenMessage", "_0Mod1");
    export const Null = new MessageKey("QueryTokenMessage", "Null");
    export const Not = new MessageKey("QueryTokenMessage", "Not");
    export const Distinct = new MessageKey("QueryTokenMessage", "Distinct");
}

export const SystemTimeMode = new EnumType<SystemTimeMode>("SystemTimeMode");
export type SystemTimeMode =
    "AsOf" |
    "Between" |
    "ContainedIn" |
    "All";

export const UniqueType = new EnumType<UniqueType>("UniqueType");
export type UniqueType =
    "First" |
    "FirstOrDefault" |
    "Single" |
    "SingleOrDefault" |
    "SingleOrMany" |
    "Only";

export namespace External {

    export module CollectionMessage {
        export const And = new MessageKey("CollectionMessage", "And");
        export const Or = new MessageKey("CollectionMessage", "Or");
        export const No0Found = new MessageKey("CollectionMessage", "No0Found");
        export const MoreThanOne0Found = new MessageKey("CollectionMessage", "MoreThanOne0Found");
    }
    
    export const DayOfWeek = new EnumType<DayOfWeek>("DayOfWeek");
    export type DayOfWeek =
        "Sunday" |
        "Monday" |
        "Tuesday" |
        "Wednesday" |
        "Thursday" |
        "Friday" |
        "Saturday";
    
}


