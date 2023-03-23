//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Reflection'
import * as Entities from '../../Signum.React/Signum.Entities'
import * as DynamicQuery from '../../Signum.React/Signum.DynamicQuery'
import * as Basics from '../../Signum.React/Signum.Basics'
import * as Operations from '../../Signum.React/Signum.Operations'
import * as UserAssets from '../Signum.UserAssets.React/Signum.Entities.UserAssets'
import * as Queries from '../Signum.UserAssets.React/Signum.UserAssets.Queries'


export const UserQueryEntity = new Type<UserQueryEntity>("UserQuery");
export interface UserQueryEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "UserQuery";
  query: DynamicQuery.QueryEntity;
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
  customDrilldowns: Entities.MList<Entities.Lite<Entities.Entity>>;
  guid: string /*Guid*/;
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
}

export module UserQueryOperation {
  export const Save : Operations.ExecuteSymbol<UserQueryEntity> = registerSymbol("Operation", "UserQueryOperation.Save");
  export const Delete : Operations.DeleteSymbol<UserQueryEntity> = registerSymbol("Operation", "UserQueryOperation.Delete");
}

export module UserQueryPermission {
  export const ViewUserQuery : Basics.PermissionSymbol = registerSymbol("Permission", "UserQueryPermission.ViewUserQuery");
}

