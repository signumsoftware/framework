//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection' 

import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities' 

import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries' 

import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets' 

import * as Authorization from '../Authorization/Signum.Entities.Authorization' 

export const QueryColumnEntity_Type = new Type<QueryColumnEntity>("QueryColumnEntity");
export interface QueryColumnEntity extends Entities.EmbeddedEntity {
    token?: UserAssets.QueryTokenEntity;
    displayName?: string;
}

export const QueryFilterEntity_Type = new Type<QueryFilterEntity>("QueryFilterEntity");
export interface QueryFilterEntity extends Entities.EmbeddedEntity {
    token?: UserAssets.QueryTokenEntity;
    operation?: Entities.DynamicQuery.FilterOperation;
    valueString?: string;
}

export const QueryOrderEntity_Type = new Type<QueryOrderEntity>("QueryOrderEntity");
export interface QueryOrderEntity extends Entities.EmbeddedEntity {
    token?: UserAssets.QueryTokenEntity;
    orderType?: Entities.DynamicQuery.OrderType;
}

export const UserQueryEntity_Type = new Type<UserQueryEntity>("UserQuery");
export interface UserQueryEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
    query?: Entities.Basics.QueryEntity;
    entityType?: Entities.Lite<Entities.Basics.TypeEntity>;
    owner?: Entities.Lite<Entities.Entity>;
    displayName?: string;
    withoutFilters?: boolean;
    filters?: Entities.MList<QueryFilterEntity>;
    orders?: Entities.MList<QueryOrderEntity>;
    columnsMode?: Entities.DynamicQuery.ColumnOptionsMode;
    columns?: Entities.MList<QueryColumnEntity>;
    paginationMode?: Entities.DynamicQuery.PaginationMode;
    elementsPerPage?: number;
    guid?: string;
    shouldHaveElements?: boolean;
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

