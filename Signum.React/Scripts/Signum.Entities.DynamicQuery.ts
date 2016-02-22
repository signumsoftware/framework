//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection' 

import * as Entities from './Signum.Entities' 
export enum ColumnOptionsMode {
    Add = "Add" as any,
    Remove = "Remove" as any,
    Replace = "Replace" as any,
}
export const ColumnOptionsMode_Type = new EnumType<ColumnOptionsMode>("ColumnOptionsMode", ColumnOptionsMode);

export enum FilterOperation {
    EqualTo = "EqualTo" as any,
    DistinctTo = "DistinctTo" as any,
    GreaterThan = "GreaterThan" as any,
    GreaterThanOrEqual = "GreaterThanOrEqual" as any,
    LessThan = "LessThan" as any,
    LessThanOrEqual = "LessThanOrEqual" as any,
    Contains = "Contains" as any,
    StartsWith = "StartsWith" as any,
    EndsWith = "EndsWith" as any,
    Like = "Like" as any,
    NotContains = "NotContains" as any,
    NotStartsWith = "NotStartsWith" as any,
    NotEndsWith = "NotEndsWith" as any,
    NotLike = "NotLike" as any,
    IsIn = "IsIn" as any,
    IsNotIn = "IsNotIn" as any,
}
export const FilterOperation_Type = new EnumType<FilterOperation>("FilterOperation", FilterOperation);

export enum FilterType {
    Integer = "Integer" as any,
    Decimal = "Decimal" as any,
    String = "String" as any,
    DateTime = "DateTime" as any,
    Lite = "Lite" as any,
    Embedded = "Embedded" as any,
    Boolean = "Boolean" as any,
    Enum = "Enum" as any,
    Guid = "Guid" as any,
}
export const FilterType_Type = new EnumType<FilterType>("FilterType", FilterType);

export enum OrderType {
    Ascending = "Ascending" as any,
    Descending = "Descending" as any,
}
export const OrderType_Type = new EnumType<OrderType>("OrderType", OrderType);

export enum PaginationMode {
    All = "All" as any,
    Firsts = "Firsts" as any,
    Paginate = "Paginate" as any,
}
export const PaginationMode_Type = new EnumType<PaginationMode>("PaginationMode", PaginationMode);

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
    export const MoreThanOneColumnNamed0 = new MessageKey("QueryTokenMessage", "MoreThanOneColumnNamed0");
    export const Number = new MessageKey("QueryTokenMessage", "Number");
    export const Of = new MessageKey("QueryTokenMessage", "Of");
    export const Second = new MessageKey("QueryTokenMessage", "Second");
    export const Text = new MessageKey("QueryTokenMessage", "Text");
    export const Year = new MessageKey("QueryTokenMessage", "Year");
    export const WeekNumber = new MessageKey("QueryTokenMessage", "WeekNumber");
    export const _0Steps1 = new MessageKey("QueryTokenMessage", "_0Steps1");
    export const Step0 = new MessageKey("QueryTokenMessage", "Step0");
}

export enum UniqueType {
    First = "First" as any,
    FirstOrDefault = "FirstOrDefault" as any,
    Single = "Single" as any,
    SingleOrDefault = "SingleOrDefault" as any,
    SingleOrMany = "SingleOrMany" as any,
    Only = "Only" as any,
}
export const UniqueType_Type = new EnumType<UniqueType>("UniqueType", UniqueType);

export namespace External {

    export module CollectionMessage {
        export const And = new MessageKey("CollectionMessage", "And");
        export const Or = new MessageKey("CollectionMessage", "Or");
        export const No0Found = new MessageKey("CollectionMessage", "No0Found");
        export const MoreThanOne0Found = new MessageKey("CollectionMessage", "MoreThanOne0Found");
    }
    
    export enum DayOfWeek {
        Sunday = "Sunday" as any,
        Monday = "Monday" as any,
        Tuesday = "Tuesday" as any,
        Wednesday = "Wednesday" as any,
        Thursday = "Thursday" as any,
        Friday = "Friday" as any,
        Saturday = "Saturday" as any,
    }
    export const DayOfWeek_Type = new EnumType<DayOfWeek>("DayOfWeek", DayOfWeek);
    
}

