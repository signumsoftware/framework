//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'


export const ColumnOptionsMode = new EnumType<ColumnOptionsMode>("ColumnOptionsMode");
export type ColumnOptionsMode =
  "Add" |
  "Remove" |
  "ReplaceAll" |
  "InsertStart" |
  "ReplaceOrAdd";

export const CombineRows = new EnumType<CombineRows>("CombineRows");
export type CombineRows =
  "EqualValue" |
  "EqualEntity";

export const DashboardBehaviour = new EnumType<DashboardBehaviour>("DashboardBehaviour");
export type DashboardBehaviour =
  "PromoteToDasboardPinnedFilter" |
  "UseAsInitialSelection" |
  "UseWhenNoFilters";

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
  "IsNotIn" |
  "ComplexCondition" |
  "FreeText";

export const FilterType = new EnumType<FilterType>("FilterType");
export type FilterType =
  "Integer" |
  "Decimal" |
  "String" |
  "DateTime" |
  "Time" |
  "Lite" |
  "Embedded" |
  "Model" |
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

export const PinnedFilterActive = new EnumType<PinnedFilterActive>("PinnedFilterActive");
export type PinnedFilterActive =
  "Always" |
  "WhenHasValue" |
  "Checkbox_Checked" |
  "Checkbox_Unchecked" |
  "NotCheckbox_Checked" |
  "NotCheckbox_Unchecked";

export const RefreshMode = new EnumType<RefreshMode>("RefreshMode");
export type RefreshMode =
  "Auto" |
  "Manual";

export const SystemTimeJoinMode = new EnumType<SystemTimeJoinMode>("SystemTimeJoinMode");
export type SystemTimeJoinMode =
  "Current" |
  "FirstCompatible" |
  "AllCompatible";

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
  "Only";

