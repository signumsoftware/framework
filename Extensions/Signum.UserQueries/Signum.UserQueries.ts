//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as DynamicQuery from '../../Signum/React/Signum.DynamicQuery'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as UserAssets from '../Signum.UserAssets/Signum.UserAssets'
import * as Queries from '../Signum.UserAssets/Signum.UserAssets.Queries'
import * as Dashboard from '../Signum.Dashboard/Signum.Dashboard'


export const AutoUpdate: EnumType<AutoUpdate> = new EnumType<AutoUpdate>("AutoUpdate");
export type AutoUpdate =
  "None" |
  "InteractionGroup" |
  "Dashboard";

export const BigValuePartEntity: Type<BigValuePartEntity> = new Type<BigValuePartEntity>("BigValuePart");
export interface BigValuePartEntity extends Entities.Entity, Dashboard.IPartParseDataEntity, Dashboard.IPartEntity {
  Type: "BigValuePart";
  valueToken: Queries.QueryTokenEmbedded | null;
  userQuery: UserQueryEntity | null;
  requiresTitle: boolean;
  customBigValue: string | null;
  navigate: boolean;
  customUrl: string | null;
}

export const HealthCheckConditionEmbedded: Type<HealthCheckConditionEmbedded> = new Type<HealthCheckConditionEmbedded>("HealthCheckConditionEmbedded");
export interface HealthCheckConditionEmbedded extends Entities.EmbeddedEntity {
  Type: "HealthCheckConditionEmbedded";
  operation: DynamicQuery.FilterOperation;
  value: number;
}

export const HealthCheckEmbedded: Type<HealthCheckEmbedded> = new Type<HealthCheckEmbedded>("HealthCheckEmbedded");
export interface HealthCheckEmbedded extends Entities.EmbeddedEntity {
  Type: "HealthCheckEmbedded";
  failWhen: HealthCheckConditionEmbedded | null;
  degradedWhen: HealthCheckConditionEmbedded | null;
}

export const SystemTimeEmbedded: Type<SystemTimeEmbedded> = new Type<SystemTimeEmbedded>("SystemTimeEmbedded");
export interface SystemTimeEmbedded extends Entities.EmbeddedEntity {
  Type: "SystemTimeEmbedded";
  mode: DynamicQuery.SystemTimeMode;
  startDate: string | null;
  endDate: string | null;
  joinMode: DynamicQuery.SystemTimeJoinMode | null;
  timeSeriesUnit: DynamicQuery.TimeSeriesUnit | null;
  timeSeriesStep: number | null;
  timeSeriesMaxRowsPerStep: number | null;
  splitQueries: boolean;
}

export const UserQueryEntity: Type<UserQueryEntity> = new Type<UserQueryEntity>("UserQuery");
export interface UserQueryEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "UserQuery";
  query: Basics.QueryEntity;
  groupResults: boolean;
  entityType: Entities.Lite<Basics.TypeEntity> | null;
  hideQuickLink: boolean;
  includeDefaultFilters: boolean | null;
  owner: Entities.Lite<Entities.Entity> | null;
  displayName: string;
  appendFilters: boolean;
  refreshMode: DynamicQuery.RefreshMode;
  filters: Entities.MList<Queries.QueryFilterEmbedded>;
  orders: Entities.MList<Queries.QueryOrderEmbedded>;
  columnsMode: DynamicQuery.ColumnOptionsMode;
  columns: Entities.MList<Queries.QueryColumnEmbedded>;
  paginationMode: DynamicQuery.PaginationMode | null;
  elementsPerPage: number | null;
  systemTime: SystemTimeEmbedded | null;
  healthCheck: HealthCheckEmbedded | null;
  customDrilldowns: Entities.MList<Entities.Lite<Entities.Entity>>;
  guid: string /*Guid*/;
}

export const UserQueryLiteModel: Type<UserQueryLiteModel> = new Type<UserQueryLiteModel>("UserQueryLiteModel");
export interface UserQueryLiteModel extends Entities.ModelEntity {
  Type: "UserQueryLiteModel";
  displayName: string;
  query: Basics.QueryEntity;
  hideQuickLink: boolean;
}

export namespace UserQueryMessage {
  export const Edit: MessageKey = new MessageKey("UserQueryMessage", "Edit");
  export const CreateNew: MessageKey = new MessageKey("UserQueryMessage", "CreateNew");
  export const BackToDefault: MessageKey = new MessageKey("UserQueryMessage", "BackToDefault");
  export const ApplyChanges: MessageKey = new MessageKey("UserQueryMessage", "ApplyChanges");
  export const Use0ToFilterCurrentEntity: MessageKey = new MessageKey("UserQueryMessage", "Use0ToFilterCurrentEntity");
  export const Preview: MessageKey = new MessageKey("UserQueryMessage", "Preview");
  export const MakesThe0AvailableForCustomDrilldownsAndInContextualMenuWhenGrouping0: MessageKey = new MessageKey("UserQueryMessage", "MakesThe0AvailableForCustomDrilldownsAndInContextualMenuWhenGrouping0");
  export const MakesThe0AvailableAsAQuickLinkOf1: MessageKey = new MessageKey("UserQueryMessage", "MakesThe0AvailableAsAQuickLinkOf1");
  export const TheSelected0: MessageKey = new MessageKey("UserQueryMessage", "TheSelected0");
  export const Date: MessageKey = new MessageKey("UserQueryMessage", "Date");
  export const Pagination: MessageKey = new MessageKey("UserQueryMessage", "Pagination");
  export const _0CountOf1Is2Than3: MessageKey = new MessageKey("UserQueryMessage", "_0CountOf1Is2Than3");
}

export namespace UserQueryOperation {
  export const Save : Operations.ExecuteSymbol<UserQueryEntity> = registerSymbol("Operation", "UserQueryOperation.Save");
  export const Delete : Operations.DeleteSymbol<UserQueryEntity> = registerSymbol("Operation", "UserQueryOperation.Delete");
}

export const UserQueryPartEntity: Type<UserQueryPartEntity> = new Type<UserQueryPartEntity>("UserQueryPart");
export interface UserQueryPartEntity extends Entities.Entity, Dashboard.IPartEntity {
  Type: "UserQueryPart";
  userQuery: UserQueryEntity;
  isQueryCached: boolean;
  autoUpdate: AutoUpdate;
  allowSelection: boolean;
  showFooter: boolean;
  createNew: boolean;
  allowMaxHeight: boolean;
  requiresTitle: boolean;
}

export namespace UserQueryPermission {
  export const ViewUserQuery : Basics.PermissionSymbol = registerSymbol("Permission", "UserQueryPermission.ViewUserQuery");
}

export const ValueUserQueryElementEmbedded: Type<ValueUserQueryElementEmbedded> = new Type<ValueUserQueryElementEmbedded>("ValueUserQueryElementEmbedded");
export interface ValueUserQueryElementEmbedded extends Entities.EmbeddedEntity {
  Type: "ValueUserQueryElementEmbedded";
  label: string | null;
  userQuery: UserQueryEntity;
  isQueryCached: boolean;
  href: string | null;
}

export const ValueUserQueryListPartEntity: Type<ValueUserQueryListPartEntity> = new Type<ValueUserQueryListPartEntity>("ValueUserQueryListPart");
export interface ValueUserQueryListPartEntity extends Entities.Entity, Dashboard.IPartEntity {
  Type: "ValueUserQueryListPart";
  userQueries: Entities.MList<ValueUserQueryElementEmbedded>;
  requiresTitle: boolean;
}

