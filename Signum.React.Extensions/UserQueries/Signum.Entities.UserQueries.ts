//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Scripts/Reflection'
import * as Entities from '../../Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../Signum.React/Scripts/Signum.Entities.Basics'
import * as DynamicQuery from '../../Signum.React/Scripts/Signum.Entities.DynamicQuery'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'


export const PinnedQueryFilterEmbedded = new Type<PinnedQueryFilterEmbedded>("PinnedQueryFilterEmbedded");
export interface PinnedQueryFilterEmbedded extends Entities.EmbeddedEntity {
  Type: "PinnedQueryFilterEmbedded";
  label: string | null;
  column: number | null;
  row: number | null;
  active: DynamicQuery.PinnedFilterActive;
  splitText: boolean;
}

export const QueryColumnEmbedded = new Type<QueryColumnEmbedded>("QueryColumnEmbedded");
export interface QueryColumnEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryColumnEmbedded";
  token: UserAssets.QueryTokenEmbedded;
  displayName: string | null;
  summaryToken: UserAssets.QueryTokenEmbedded | null;
  hiddenColumn: boolean;
}

export const QueryFilterEmbedded = new Type<QueryFilterEmbedded>("QueryFilterEmbedded");
export interface QueryFilterEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryFilterEmbedded";
  token: UserAssets.QueryTokenEmbedded | null;
  isGroup: boolean;
  groupOperation: DynamicQuery.FilterGroupOperation | null;
  operation: DynamicQuery.FilterOperation | null;
  valueString: string | null;
  pinned: PinnedQueryFilterEmbedded | null;
  dashboardBehaviour: DynamicQuery.DashboardBehaviour | null;
  indentation: number;
}

export const QueryOrderEmbedded = new Type<QueryOrderEmbedded>("QueryOrderEmbedded");
export interface QueryOrderEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryOrderEmbedded";
  token: UserAssets.QueryTokenEmbedded;
  orderType: DynamicQuery.OrderType;
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
  filters: Entities.MList<QueryFilterEmbedded>;
  orders: Entities.MList<QueryOrderEmbedded>;
  columnsMode: DynamicQuery.ColumnOptionsMode;
  columns: Entities.MList<QueryColumnEmbedded>;
  paginationMode: DynamicQuery.PaginationMode | null;
  elementsPerPage: number | null;
  customDrilldowns: Entities.MList<Entities.Lite<Entities.Entity>>;
  guid: string /*Guid*/;
}

export module UserQueryMessage {
  export const Edit = new MessageKey("UserQueryMessage", "Edit");
  export const CreateNew = new MessageKey("UserQueryMessage", "CreateNew");
  export const BackToDefault = new MessageKey("UserQueryMessage", "BackToDefault");
  export const ApplyChanges = new MessageKey("UserQueryMessage", "ApplyChanges");
  export const TheFilterOperation0isNotCompatibleWith1 = new MessageKey("UserQueryMessage", "TheFilterOperation0isNotCompatibleWith1");
  export const _0IsNotFilterable = new MessageKey("UserQueryMessage", "_0IsNotFilterable");
  export const Use0ToFilterCurrentEntity = new MessageKey("UserQueryMessage", "Use0ToFilterCurrentEntity");
  export const Preview = new MessageKey("UserQueryMessage", "Preview");
  export const MakesThe0AvailableInContextualMenuWhenGrouping0 = new MessageKey("UserQueryMessage", "MakesThe0AvailableInContextualMenuWhenGrouping0");
  export const MakesThe0AvailableAsAQuickLinkOf1 = new MessageKey("UserQueryMessage", "MakesThe0AvailableAsAQuickLinkOf1");
  export const TheSelected0 = new MessageKey("UserQueryMessage", "TheSelected0");
}

export module UserQueryOperation {
  export const Save : Entities.ExecuteSymbol<UserQueryEntity> = registerSymbol("Operation", "UserQueryOperation.Save");
  export const Delete : Entities.DeleteSymbol<UserQueryEntity> = registerSymbol("Operation", "UserQueryOperation.Delete");
}

export module UserQueryPermission {
  export const ViewUserQuery : Authorization.PermissionSymbol = registerSymbol("Permission", "UserQueryPermission.ViewUserQuery");
}


