import { MessageKey, Type } from '@framework/Reflection';
import * as Entities from '@framework/Signum.Entities';
import * as Basics from '@framework/Signum.Entities.Basics';
import * as DynamicQuery from '@framework/Signum.DynamicQuery';
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets';
import * as Authorization from '../Signum.Authorization/Signum.Entities.Authorization';
export declare const PinnedQueryFilterEmbedded: Type<PinnedQueryFilterEmbedded>;
export interface PinnedQueryFilterEmbedded extends Entities.EmbeddedEntity {
    Type: "PinnedQueryFilterEmbedded";
    label: string | null;
    column: number | null;
    row: number | null;
    active: DynamicQuery.PinnedFilterActive;
    splitText: boolean;
}
export declare const QueryColumnEmbedded: Type<QueryColumnEmbedded>;
export interface QueryColumnEmbedded extends Entities.EmbeddedEntity {
    Type: "QueryColumnEmbedded";
    token: UserAssets.QueryTokenEmbedded;
    displayName: string | null;
    summaryToken: UserAssets.QueryTokenEmbedded | null;
    hiddenColumn: boolean;
    combineRows: DynamicQuery.CombineRows | null;
}
export declare const QueryFilterEmbedded: Type<QueryFilterEmbedded>;
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
export declare const QueryOrderEmbedded: Type<QueryOrderEmbedded>;
export interface QueryOrderEmbedded extends Entities.EmbeddedEntity {
    Type: "QueryOrderEmbedded";
    token: UserAssets.QueryTokenEmbedded;
    orderType: DynamicQuery.OrderType;
}
export declare const UserQueryEntity: Type<UserQueryEntity>;
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
    guid: string;
}
export declare module UserQueryMessage {
    const Edit: MessageKey;
    const CreateNew: MessageKey;
    const BackToDefault: MessageKey;
    const ApplyChanges: MessageKey;
    const TheFilterOperation0isNotCompatibleWith1: MessageKey;
    const _0IsNotFilterable: MessageKey;
    const Use0ToFilterCurrentEntity: MessageKey;
    const Preview: MessageKey;
    const MakesThe0AvailableForCustomDrilldownsAndInContextualMenuWhenGrouping0: MessageKey;
    const MakesThe0AvailableAsAQuickLinkOf1: MessageKey;
    const TheSelected0: MessageKey;
}
export declare module UserQueryOperation {
    const Save: Entities.ExecuteSymbol<UserQueryEntity>;
    const Delete: Entities.DeleteSymbol<UserQueryEntity>;
}
export declare module UserQueryPermission {
    const ViewUserQuery: Authorization.PermissionSymbol;
}
//# sourceMappingURL=Signum.Entities.UserQueries.d.ts.map