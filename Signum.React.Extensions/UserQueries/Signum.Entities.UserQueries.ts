//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as DynamicQuery from '../../../Framework/Signum.React/Scripts/Signum.Entities.DynamicQuery'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'


export const PinnedQueryFilterEmbedded = new Type<PinnedQueryFilterEmbedded>("PinnedQueryFilterEmbedded");
export interface PinnedQueryFilterEmbedded extends Entities.EmbeddedEntity {
  Type: "PinnedQueryFilterEmbedded";
  label?: string | null;
  column?: number | null;
  row?: number | null;
  disableOnNull?: boolean;
  splitText?: boolean;
}

export const QueryColumnEmbedded = new Type<QueryColumnEmbedded>("QueryColumnEmbedded");
export interface QueryColumnEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryColumnEmbedded";
  token?: UserAssets.QueryTokenEmbedded | null;
  displayName?: string | null;
}

export const QueryFilterEmbedded = new Type<QueryFilterEmbedded>("QueryFilterEmbedded");
export interface QueryFilterEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryFilterEmbedded";
  token?: UserAssets.QueryTokenEmbedded | null;
  isGroup?: boolean;
  groupOperation?: DynamicQuery.FilterGroupOperation | null;
  operation?: DynamicQuery.FilterOperation | null;
  valueString?: string | null;
  pinned?: PinnedQueryFilterEmbedded | null;
  indentation?: number;
}

export const QueryOrderEmbedded = new Type<QueryOrderEmbedded>("QueryOrderEmbedded");
export interface QueryOrderEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryOrderEmbedded";
  token?: UserAssets.QueryTokenEmbedded | null;
  orderType?: DynamicQuery.OrderType;
}

export const UserQueryEntity = new Type<UserQueryEntity>("UserQuery");
export interface UserQueryEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "UserQuery";
  query?: Basics.QueryEntity | null;
  groupResults?: boolean;
  entityType?: Entities.Lite<Basics.TypeEntity> | null;
  hideQuickLink?: boolean;
  owner?: Entities.Lite<Entities.Entity> | null;
  displayName?: string | null;
  appendFilters?: boolean;
  filters: Entities.MList<QueryFilterEmbedded>;
  orders: Entities.MList<QueryOrderEmbedded>;
  columnsMode?: DynamicQuery.ColumnOptionsMode;
  columns: Entities.MList<QueryColumnEmbedded>;
  searchOnLoad?: boolean;
  showFilterButton?: boolean;
  paginationMode?: DynamicQuery.PaginationMode | null;
  elementsPerPage?: number | null;
  guid?: string;
}

export module UserQueryMessage {
  export const AreYouSureToRemove0 = new MessageKey("UserQueryMessage", "AreYouSureToRemove0");
  export const Edit = new MessageKey("UserQueryMessage", "Edit");
  export const MyQueries = new MessageKey("UserQueryMessage", "MyQueries");
  export const RemoveUserQuery = new MessageKey("UserQueryMessage", "RemoveUserQuery");
  export const _0ShouldBeEmptyIf1IsSet = new MessageKey("UserQueryMessage", "_0ShouldBeEmptyIf1IsSet");
  export const _0ShouldBeNullIf1Is2 = new MessageKey("UserQueryMessage", "_0ShouldBeNullIf1Is2");
  export const _0ShouldBeSetIf1Is2 = new MessageKey("UserQueryMessage", "_0ShouldBeSetIf1Is2");
  export const UserQueries_CreateNew = new MessageKey("UserQueryMessage", "UserQueries_CreateNew");
  export const UserQueries_Edit = new MessageKey("UserQueryMessage", "UserQueries_Edit");
  export const UserQueries_UserQueries = new MessageKey("UserQueryMessage", "UserQueries_UserQueries");
  export const TheFilterOperation0isNotCompatibleWith1 = new MessageKey("UserQueryMessage", "TheFilterOperation0isNotCompatibleWith1");
  export const _0IsNotFilterable = new MessageKey("UserQueryMessage", "_0IsNotFilterable");
  export const Use0ToFilterCurrentEntity = new MessageKey("UserQueryMessage", "Use0ToFilterCurrentEntity");
  export const Preview = new MessageKey("UserQueryMessage", "Preview");
}

export module UserQueryOperation {
  export const Save : Entities.ExecuteSymbol<UserQueryEntity> = registerSymbol("Operation", "UserQueryOperation.Save");
  export const Delete : Entities.DeleteSymbol<UserQueryEntity> = registerSymbol("Operation", "UserQueryOperation.Delete");
}

export module UserQueryPermission {
  export const ViewUserQuery : Authorization.PermissionSymbol = registerSymbol("Permission", "UserQueryPermission.ViewUserQuery");
}


