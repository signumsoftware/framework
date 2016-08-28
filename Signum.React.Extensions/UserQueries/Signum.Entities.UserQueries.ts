//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as DynamicQuery from '../../../Framework/Signum.React/Scripts/Signum.Entities.DynamicQuery'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'


export const QueryColumnEntity = new Type<QueryColumnEntity>("QueryColumnEntity");
export interface QueryColumnEntity extends Entities.EmbeddedEntity {
    Type: "QueryColumnEntity";
    token?: UserAssets.QueryTokenEntity | null;
    displayName?: string | null;
}

export const QueryFilterEntity = new Type<QueryFilterEntity>("QueryFilterEntity");
export interface QueryFilterEntity extends Entities.EmbeddedEntity {
    Type: "QueryFilterEntity";
    token?: UserAssets.QueryTokenEntity | null;
    operation?: DynamicQuery.FilterOperation;
    valueString?: string | null;
}

export const QueryOrderEntity = new Type<QueryOrderEntity>("QueryOrderEntity");
export interface QueryOrderEntity extends Entities.EmbeddedEntity {
    Type: "QueryOrderEntity";
    token?: UserAssets.QueryTokenEntity | null;
    orderType?: DynamicQuery.OrderType;
}

export const UserQueryEntity = new Type<UserQueryEntity>("UserQuery");
export interface UserQueryEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
    Type: "UserQuery";
    query?: Basics.QueryEntity | null;
    entityType?: Entities.Lite<Basics.TypeEntity> | null;
    owner?: Entities.Lite<Entities.Entity> | null;
    displayName?: string | null;
    withoutFilters?: boolean;
    filters: Entities.MList<QueryFilterEntity>;
    orders: Entities.MList<QueryOrderEntity>;
    columnsMode?: DynamicQuery.ColumnOptionsMode;
    columns: Entities.MList<QueryColumnEntity>;
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
    export const Save : Entities.ExecuteSymbol<UserQueryEntity> = registerSymbol({ Type: "Operation", key: "UserQueryOperation.Save" });
    export const Delete : Entities.DeleteSymbol<UserQueryEntity> = registerSymbol({ Type: "Operation", key: "UserQueryOperation.Delete" });
}

export module UserQueryPermission {
    export const ViewUserQuery : Authorization.PermissionSymbol = registerSymbol({ Type: "Permission", key: "UserQueryPermission.ViewUserQuery" });
}


