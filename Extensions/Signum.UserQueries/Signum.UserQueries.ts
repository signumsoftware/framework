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


export const AutoUpdate = new EnumType<AutoUpdate>("AutoUpdate");
export type AutoUpdate =
  "None" |
  "InteractionGroup" |
  "Dashboard";

export const SystemTimeEmbedded = new Type<SystemTimeEmbedded>("SystemTimeEmbedded");
export interface SystemTimeEmbedded extends Entities.EmbeddedEntity {
  Type: "SystemTimeEmbedded";
  mode: DynamicQuery.SystemTimeMode;
  startDate: string | null;
  endDate: string | null;
  joinMode: DynamicQuery.SystemTimeJoinMode | null;
}

export const UserQueryEntity = new Type<UserQueryEntity>("UserQuery");
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
  customDrilldowns: Entities.MList<Entities.Lite<Entities.Entity>>;
  guid: string /*Guid*/;
}

export const UserQueryLiteModel = new Type<UserQueryLiteModel>("UserQueryLiteModel");
export interface UserQueryLiteModel extends Entities.ModelEntity {
  Type: "UserQueryLiteModel";
  displayName: string;
  query: Basics.QueryEntity;
  hideQuickLink: boolean;
}

export module UserQueryMessage {
  export const Edit = new MessageKey("UserQueryMessage", "Edit");
  export const CreateNew = new MessageKey("UserQueryMessage", "CreateNew");
  export const BackToDefault = new MessageKey("UserQueryMessage", "BackToDefault");
  export const ApplyChanges = new MessageKey("UserQueryMessage", "ApplyChanges");
  export const Use0ToFilterCurrentEntity = new MessageKey("UserQueryMessage", "Use0ToFilterCurrentEntity");
  export const Preview = new MessageKey("UserQueryMessage", "Preview");
  export const MakesThe0AvailableForCustomDrilldownsAndInContextualMenuWhenGrouping0 = new MessageKey("UserQueryMessage", "MakesThe0AvailableForCustomDrilldownsAndInContextualMenuWhenGrouping0");
  export const MakesThe0AvailableAsAQuickLinkOf1 = new MessageKey("UserQueryMessage", "MakesThe0AvailableAsAQuickLinkOf1");
  export const TheSelected0 = new MessageKey("UserQueryMessage", "TheSelected0");
  export const Date = new MessageKey("UserQueryMessage", "Date");
  export const Pagination = new MessageKey("UserQueryMessage", "Pagination");
}

export module UserQueryOperation {
  export const Save : Operations.ExecuteSymbol<UserQueryEntity> = registerSymbol("Operation", "UserQueryOperation.Save");
  export const Delete : Operations.DeleteSymbol<UserQueryEntity> = registerSymbol("Operation", "UserQueryOperation.Delete");
}

export const UserQueryPartEntity = new Type<UserQueryPartEntity>("UserQueryPart");
export interface UserQueryPartEntity extends Entities.Entity, Dashboard.IPartEntity {
  Type: "UserQueryPart";
  userQuery: UserQueryEntity;
  isQueryCached: boolean;
  renderMode: UserQueryPartRenderMode;
  aggregateFromSummaryHeader: boolean;
  autoUpdate: AutoUpdate;
  allowSelection: boolean;
  showFooter: boolean;
  createNew: boolean;
  allowMaxHeight: boolean;
  requiresTitle: boolean;
}

export const UserQueryPartRenderMode = new EnumType<UserQueryPartRenderMode>("UserQueryPartRenderMode");
export type UserQueryPartRenderMode =
  "SearchControl" |
  "BigValue";

export module UserQueryPermission {
  export const ViewUserQuery : Basics.PermissionSymbol = registerSymbol("Permission", "UserQueryPermission.ViewUserQuery");
  export const BackToDefaultQuery : Basics.PermissionSymbol = registerSymbol("Permission", "UserQueryPermission.BackToDefaultQuery");
}

export const ValueUserQueryElementEmbedded = new Type<ValueUserQueryElementEmbedded>("ValueUserQueryElementEmbedded");
export interface ValueUserQueryElementEmbedded extends Entities.EmbeddedEntity {
  Type: "ValueUserQueryElementEmbedded";
  label: string | null;
  userQuery: UserQueryEntity;
  isQueryCached: boolean;
  href: string | null;
}

export const ValueUserQueryListPartEntity = new Type<ValueUserQueryListPartEntity>("ValueUserQueryListPart");
export interface ValueUserQueryListPartEntity extends Entities.Entity, Dashboard.IPartEntity {
  Type: "ValueUserQueryListPart";
  userQueries: Entities.MList<ValueUserQueryElementEmbedded>;
  requiresTitle: boolean;
}

