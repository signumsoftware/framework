//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Scripts/Reflection'
import * as Entities from '../../Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../Signum.React/Scripts/Signum.Entities.Basics'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'
import * as Files from '../Files/Signum.Entities.Files'
import * as Scheduler from '../Scheduler/Signum.Entities.Scheduler'
import * as Signum from '../Basics/Signum.Entities.Basics'
import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries'
import * as Chart from '../Chart/Signum.Entities.Chart'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'


export const CachedQueryEntity = new Type<CachedQueryEntity>("CachedQuery");
export interface CachedQueryEntity extends Entities.Entity {
  Type: "CachedQuery";
  dashboard: Entities.Lite<DashboardEntity>;
  userAssets: Entities.MList<Entities.Lite<UserAssets.IUserAssetEntity>>;
  file: Files.FilePathEmbedded;
  numRows: number;
  numColumns: number;
  creationDate: string /*DateTime*/;
  queryDuration: number;
  uploadDuration: number;
}

export module CachedQueryFileType {
  export const CachedQuery : Files.FileTypeSymbol = registerSymbol("FileType", "CachedQueryFileType.CachedQuery");
}

export const CacheQueryConfigurationEmbedded = new Type<CacheQueryConfigurationEmbedded>("CacheQueryConfigurationEmbedded");
export interface CacheQueryConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "CacheQueryConfigurationEmbedded";
  timeoutForQueries: number;
  maxRows: number;
  autoRegenerateWhenOlderThan: number | null;
}

export const CombinedUserChartElementEmbedded = new Type<CombinedUserChartElementEmbedded>("CombinedUserChartElementEmbedded");
export interface CombinedUserChartElementEmbedded extends Entities.EmbeddedEntity {
  Type: "CombinedUserChartElementEmbedded";
  userChart: Chart.UserChartEntity;
  isQueryCached: boolean;
}

export const CombinedUserChartPartEntity = new Type<CombinedUserChartPartEntity>("CombinedUserChartPart");
export interface CombinedUserChartPartEntity extends Entities.Entity, IPartEntity {
  Type: "CombinedUserChartPart";
  userCharts: Entities.MList<CombinedUserChartElementEmbedded>;
  showData: boolean;
  allowChangeShowData: boolean;
  combinePinnedFiltersWithSameLabel: boolean;
  useSameScale: boolean;
  requiresTitle: boolean;
}

export const DashboardEmbedededInEntity = new EnumType<DashboardEmbedededInEntity>("DashboardEmbedededInEntity");
export type DashboardEmbedededInEntity =
  "None" |
  "Top" |
  "Bottom" |
  "Tab";

export const DashboardEntity = new Type<DashboardEntity>("Dashboard");
export interface DashboardEntity extends Entities.Entity, UserAssets.IUserAssetEntity, Scheduler.ITaskEntity {
  Type: "Dashboard";
  entityType: Entities.Lite<Basics.TypeEntity> | null;
  embeddedInEntity: DashboardEmbedededInEntity | null;
  owner: Entities.Lite<Entities.Entity> | null;
  dashboardPriority: number | null;
  autoRefreshPeriod: number | null;
  displayName: string;
  combineSimilarRows: boolean;
  cacheQueryConfiguration: CacheQueryConfigurationEmbedded | null;
  parts: Entities.MList<PanelPartEmbedded>;
  guid: string /*Guid*/;
  key: string | null;
}

export module DashboardMessage {
  export const CreateNewPart = new MessageKey("DashboardMessage", "CreateNewPart");
  export const DashboardDN_TitleMustBeSpecifiedFor0 = new MessageKey("DashboardMessage", "DashboardDN_TitleMustBeSpecifiedFor0");
  export const Preview = new MessageKey("DashboardMessage", "Preview");
  export const _0Is1InstedOf2In3 = new MessageKey("DashboardMessage", "_0Is1InstedOf2In3");
  export const Part0IsTooLarge = new MessageKey("DashboardMessage", "Part0IsTooLarge");
  export const Part0OverlapsWith1 = new MessageKey("DashboardMessage", "Part0OverlapsWith1");
  export const RowsSelected = new MessageKey("DashboardMessage", "RowsSelected");
  export const ForPerformanceReasonsThisDashboardMayShowOutdatedInformation = new MessageKey("DashboardMessage", "ForPerformanceReasonsThisDashboardMayShowOutdatedInformation");
  export const LasUpdateWasOn0 = new MessageKey("DashboardMessage", "LasUpdateWasOn0");
}

export module DashboardOperation {
  export const Save : Entities.ExecuteSymbol<DashboardEntity> = registerSymbol("Operation", "DashboardOperation.Save");
  export const RegenerateCachedQueries : Entities.ExecuteSymbol<DashboardEntity> = registerSymbol("Operation", "DashboardOperation.RegenerateCachedQueries");
  export const Clone : Entities.ConstructSymbol_From<DashboardEntity, DashboardEntity> = registerSymbol("Operation", "DashboardOperation.Clone");
  export const Delete : Entities.DeleteSymbol<DashboardEntity> = registerSymbol("Operation", "DashboardOperation.Delete");
}

export module DashboardPermission {
  export const ViewDashboard : Authorization.PermissionSymbol = registerSymbol("Permission", "DashboardPermission.ViewDashboard");
}

export const InteractionGroup = new EnumType<InteractionGroup>("InteractionGroup");
export type InteractionGroup =
  "Group1" |
  "Group2" |
  "Group3" |
  "Group4" |
  "Group5" |
  "Group6" |
  "Group7" |
  "Group8";

export interface IPartEntity extends Entities.Entity {
  requiresTitle: boolean;
}

export const LinkElementEmbedded = new Type<LinkElementEmbedded>("LinkElementEmbedded");
export interface LinkElementEmbedded extends Entities.EmbeddedEntity {
  Type: "LinkElementEmbedded";
  label: string;
  link: string;
}

export const LinkListPartEntity = new Type<LinkListPartEntity>("LinkListPart");
export interface LinkListPartEntity extends Entities.Entity, IPartEntity {
  Type: "LinkListPart";
  links: Entities.MList<LinkElementEmbedded>;
  requiresTitle: boolean;
}

export const PanelPartEmbedded = new Type<PanelPartEmbedded>("PanelPartEmbedded");
export interface PanelPartEmbedded extends Entities.EmbeddedEntity {
  Type: "PanelPartEmbedded";
  title: string | null;
  iconName: string | null;
  iconColor: string | null;
  row: number;
  startColumn: number;
  columns: number;
  interactionGroup: InteractionGroup | null;
  style: Signum.BootstrapStyle;
  content: IPartEntity;
}

export const UserChartPartEntity = new Type<UserChartPartEntity>("UserChartPart");
export interface UserChartPartEntity extends Entities.Entity, IPartEntity {
  Type: "UserChartPart";
  userChart: Chart.UserChartEntity;
  isQueryCached: boolean;
  showData: boolean;
  allowChangeShowData: boolean;
  createNew: boolean;
  autoRefresh: boolean;
  requiresTitle: boolean;
}

export const UserQueryPartEntity = new Type<UserQueryPartEntity>("UserQueryPart");
export interface UserQueryPartEntity extends Entities.Entity, IPartEntity {
  Type: "UserQueryPart";
  userQuery: UserQueries.UserQueryEntity;
  isQueryCached: boolean;
  renderMode: UserQueryPartRenderMode;
  allowSelection: boolean;
  showFooter: boolean;
  createNew: boolean;
  requiresTitle: boolean;
}

export const UserQueryPartRenderMode = new EnumType<UserQueryPartRenderMode>("UserQueryPartRenderMode");
export type UserQueryPartRenderMode =
  "SearchControl" |
  "BigValue";

export const UserTreePartEntity = new Type<UserTreePartEntity>("UserTreePart");
export interface UserTreePartEntity extends Entities.Entity, IPartEntity {
  Type: "UserTreePart";
  userQuery: UserQueries.UserQueryEntity;
  requiresTitle: boolean;
}

export const ValueUserQueryElementEmbedded = new Type<ValueUserQueryElementEmbedded>("ValueUserQueryElementEmbedded");
export interface ValueUserQueryElementEmbedded extends Entities.EmbeddedEntity {
  Type: "ValueUserQueryElementEmbedded";
  label: string | null;
  userQuery: UserQueries.UserQueryEntity;
  isQueryCached: boolean;
  href: string | null;
}

export const ValueUserQueryListPartEntity = new Type<ValueUserQueryListPartEntity>("ValueUserQueryListPart");
export interface ValueUserQueryListPartEntity extends Entities.Entity, IPartEntity {
  Type: "ValueUserQueryListPart";
  userQueries: Entities.MList<ValueUserQueryElementEmbedded>;
  requiresTitle: boolean;
}


